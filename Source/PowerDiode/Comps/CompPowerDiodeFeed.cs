namespace PowerDiode;

[HotSwappable]
internal class CompPowerDiodeFeed : ThingComp
{
    private const float DefaultReservePercent = 10f;
    private const float DefaultOverflowThresholdPercent = 80f;
    private const float DefaultTopUpThresholdPercent = 20f;

    private float targetWatts;
    private float reserveWattDays;
    private float reservePercent;
    private float overflowThresholdPercent;
    private float topUpThresholdPercent;
    private PowerDiodeOperatingMode operatingMode;

    // Gizmo_SetDiodeWattage/Gizmo_SetDiodeReserve/Gizmo_SetDiodeOverflowThreshold/
    // Gizmo_SetDiodeTopUpThreshold are recreated every GUI frame, so their drag state can't live
    // on the gizmo itself; it's kept here instead, per building. Only one of
    // draggingReserveBar/draggingOverflowBar/draggingTopUpBar is ever shown at a time (they're
    // mode-exclusive), but each gets its own field rather than sharing one, since nothing ties
    // their lifetimes together.
    internal bool draggingWattageBar;
    internal bool draggingReserveBar;
    internal bool draggingOverflowBar;
    internal bool draggingTopUpBar;

    internal CompPowerDiodeDraw? Partner { get; set; }

    internal CompProperties_PowerDiodeFeed Props => (CompProperties_PowerDiodeFeed)props;

    internal float TargetWatts
    {
        get => targetWatts;
        set =>
            targetWatts = Mathf.Clamp(
                value,
                PowerDiodeMod.Settings.MinWattage,
                PowerDiodeMod.Settings.MaxWattage
            );
    }

    // How many watt-days of stored energy on the draw node's power net batteries are kept
    // untouchable - once their total stored energy nears this floor, PowerDiodeFlow ramps this
    // diode's draw down to 0 rather than letting the batteries actually run dry. Only used in
    // OneWayValve mode when Settings.ReserveIsPercentage is false.
    internal float ReserveWattDays
    {
        get => reserveWattDays;
        set =>
            reserveWattDays = Mathf.Clamp(
                value,
                PowerDiodeMod.Settings.MinReserveWattDays,
                PowerDiodeMod.Settings.MaxReserveWattDays
            );
    }

    // Percentage-of-capacity equivalent of ReserveWattDays, used in OneWayValve mode instead when
    // Settings.ReserveIsPercentage is true.
    internal float ReservePercent
    {
        get => reservePercent;
        set => reservePercent = Mathf.Clamp(value, 0f, 100f);
    }

    // Overflow mode only: the draw node's power net batteries must be charged above this
    // percentage of their total capacity before this outlet feeds anything at all.
    internal float OverflowThresholdPercent
    {
        get => overflowThresholdPercent;
        set => overflowThresholdPercent = Mathf.Clamp(value, 0f, 100f);
    }

    // TopUp mode only: this outlet stops feeding once the feed node's power net batteries reach
    // this percentage of their total capacity.
    internal float TopUpThresholdPercent
    {
        get => topUpThresholdPercent;
        set => topUpThresholdPercent = Mathf.Clamp(value, 0f, 100f);
    }

    internal PowerDiodeOperatingMode OperatingMode
    {
        get => operatingMode;
        set => operatingMode = value;
    }

    internal float CurrentFlowWatts { get; private set; }

    // The draw node's power net batteries' total stored/max energy, cached each tick for the
    // reserve/overflow gizmos to show alongside the threshold they're set against.
    internal float SourceBatteryStoredWattDays { get; private set; }
    internal float SourceBatteryCapacityWattDays { get; private set; }

    // The feed node's own power net batteries' total stored/max energy, cached each tick for the
    // top-up gizmo to show alongside the threshold it's set against.
    internal float SinkBatteryStoredWattDays { get; private set; }
    internal float SinkBatteryCapacityWattDays { get; private set; }

    internal float SourceBatteryStoredPercent =>
        SourceBatteryCapacityWattDays <= 0f
            ? 0f
            : Mathf.Clamp01(SourceBatteryStoredWattDays / SourceBatteryCapacityWattDays);

    internal float SinkBatteryStoredPercent =>
        SinkBatteryCapacityWattDays <= 0f
            ? 0f
            : Mathf.Clamp01(SinkBatteryStoredWattDays / SinkBatteryCapacityWattDays);

    // True when the feed node's own power net and its partner draw node's power net are the same
    // PowerNet - i.e. some other connection already joins the two sides the diode is supposed to
    // keep separate, making the diode's own flow redundant (and, if it kept feeding, a pointless
    // net-zero self-loop). Detected live every tick rather than cached, since a connection can
    // appear or disappear at any time (a new conduit built or removed elsewhere on the map).
    internal bool IsSharedGridDegenerate { get; private set; }

    internal CompPowerTrader PowerTrader
    {
        get
        {
            field ??= parent.GetComp<CompPowerTrader>();
            return field;
        }
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (!respawningAfterLoad)
        {
            targetWatts = PowerDiodeMod.Settings.MaxWattage;
            reserveWattDays = PowerDiodeMod.Settings.MinReserveWattDays;
            reservePercent = DefaultReservePercent;
            overflowThresholdPercent = DefaultOverflowThresholdPercent;
            topUpThresholdPercent = DefaultTopUpThresholdPercent;
            operatingMode = PowerDiodeOperatingMode.OneWayValve;
        }
        if (Partner == null)
        {
            PowerDiodeLinking.TryLinkFeedNode(this);
        }
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref targetWatts, "targetWatts", PowerDiodeMod.Settings.MaxWattage);
        Scribe_Values.Look(
            ref reserveWattDays,
            "reserveWattDays",
            PowerDiodeMod.Settings.MinReserveWattDays
        );
        Scribe_Values.Look(ref reservePercent, "reservePercent", DefaultReservePercent);
        Scribe_Values.Look(
            ref overflowThresholdPercent,
            "overflowThresholdPercent",
            DefaultOverflowThresholdPercent
        );
        Scribe_Values.Look(
            ref topUpThresholdPercent,
            "topUpThresholdPercent",
            DefaultTopUpThresholdPercent
        );
        Scribe_Values.Look(ref operatingMode, "operatingMode", PowerDiodeOperatingMode.OneWayValve);
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        Unlink();
        base.PostDeSpawn(map, mode);
    }

    internal void Unlink()
    {
        if (Partner != null)
        {
            Partner.Partner = null;
            Partner.PowerTrader.PowerOutput = 0f;
            Partner = null;
        }
        CurrentFlowWatts = 0f;
        SourceBatteryStoredWattDays = 0f;
        SourceBatteryCapacityWattDays = 0f;
        SinkBatteryStoredWattDays = 0f;
        SinkBatteryCapacityWattDays = 0f;
        IsSharedGridDegenerate = false;
        PowerTrader.PowerOutput = 0f;
    }

    public override void CompTick()
    {
        base.CompTick();
        var partner = Partner;
        if (partner == null || !parent.Spawned || !partner.parent.Spawned)
        {
            CurrentFlowWatts = 0f;
            SourceBatteryStoredWattDays = 0f;
            SourceBatteryCapacityWattDays = 0f;
            SinkBatteryStoredWattDays = 0f;
            SinkBatteryCapacityWattDays = 0f;
            IsSharedGridDegenerate = false;
            PowerTrader.PowerOutput = 0f;
            return;
        }

        var sinkNet = PowerTrader.PowerNet;
        var sourceNet = partner.PowerTrader.PowerNet;
        if (sinkNet == null || sourceNet == null)
        {
            CurrentFlowWatts = 0f;
            SourceBatteryStoredWattDays = 0f;
            SourceBatteryCapacityWattDays = 0f;
            SinkBatteryStoredWattDays = 0f;
            SinkBatteryCapacityWattDays = 0f;
            IsSharedGridDegenerate = false;
            PowerTrader.PowerOutput = 0f;
            partner.PowerTrader.PowerOutput = 0f;
            return;
        }

        if (PowerDiodeSharedGridDetection.IsSharedGrid(sinkNet, sourceNet))
        {
            CurrentFlowWatts = 0f;
            SourceBatteryStoredWattDays = 0f;
            SourceBatteryCapacityWattDays = 0f;
            SinkBatteryStoredWattDays = 0f;
            SinkBatteryCapacityWattDays = 0f;
            IsSharedGridDegenerate = true;
            PowerTrader.PowerOutput = 0f;
            partner.PowerTrader.PowerOutput = 0f;
            return;
        }
        IsSharedGridDegenerate = false;

        var sinkBalanceExclSelf = NetBalanceExcluding(sinkNet, PowerTrader);
        var sourceBalanceExclSelf = NetBalanceExcluding(sourceNet, partner.PowerTrader);
        var sinkAcceptWattDays = sinkNet.batteryComps.Sum(battery =>
            Math.Max(0f, battery.AmountCanAccept)
        );
        SourceBatteryStoredWattDays = sourceNet.batteryComps.Sum(battery =>
            Math.Max(0f, battery.StoredEnergy)
        );
        SourceBatteryCapacityWattDays = sourceNet.batteryComps.Sum(battery =>
            battery.Props.storedEnergyMax
        );
        SinkBatteryStoredWattDays = sinkNet.batteryComps.Sum(battery =>
            Math.Max(0f, battery.StoredEnergy)
        );
        SinkBatteryCapacityWattDays = sinkNet.batteryComps.Sum(battery =>
            battery.Props.storedEnergyMax
        );

        var sourceReserveFloorWattDays = PowerDiodeFlow.SourceReserveFloorWattDays(
            OperatingMode,
            ReserveWattDays,
            PowerDiodeMod.Settings.ReserveIsPercentage,
            ReservePercent,
            SourceBatteryCapacityWattDays
        );

        var sinkBatteryHeadroomWatts = PowerDiodeFlow.BatterySustainableWatts(sinkAcceptWattDays);
        var sourceBatteryReserveWatts = PowerDiodeFlow.BatterySustainableWatts(
            SourceBatteryStoredWattDays,
            sourceReserveFloorWattDays
        );

        var modeGateFraction = OperatingMode switch
        {
            PowerDiodeOperatingMode.Overflow => PowerDiodeFlow.OverflowGateFraction(
                TargetWatts,
                OverflowThresholdPercent,
                SourceBatteryStoredWattDays,
                SourceBatteryCapacityWattDays
            ),
            PowerDiodeOperatingMode.TopUp => PowerDiodeFlow.TopUpGateFraction(
                TargetWatts,
                TopUpThresholdPercent,
                SinkBatteryStoredWattDays,
                SinkBatteryCapacityWattDays
            ),
            PowerDiodeOperatingMode.OneWayValve => 1f,
            _ => throw new ArgumentOutOfRangeException(nameof(OperatingMode), OperatingMode, null),
        };

        CurrentFlowWatts = PowerDiodeFlow.ComputeFlowWatts(
            TargetWatts * modeGateFraction,
            sinkBalanceExclSelf,
            sourceBalanceExclSelf,
            sinkBatteryHeadroomWatts,
            sourceBatteryReserveWatts
        );

        PowerTrader.PowerOutput = CurrentFlowWatts;
        partner.PowerTrader.PowerOutput = -CurrentFlowWatts;
    }

    private static float NetBalanceExcluding(PowerNet net, CompPowerTrader self)
    {
        var balance = 0f;
        foreach (var comp in net.powerComps)
        {
            if (comp != self && comp.PowerOn)
            {
                balance += comp.PowerOutput;
            }
        }
        return balance;
    }

    public override string CompInspectStringExtra() =>
        Partner == null ? "PowerDiode.NotLinked".Translate()
        : IsSharedGridDegenerate ? "PowerDiode.SharedGrid".Translate(Partner.parent.LabelCap)
        : CurrentFlowWatts <= 0f ? "PowerDiode.LinkedIdle".Translate(Partner.parent.LabelCap)
        : "PowerDiode.Feeding".Translate(
            Partner.parent.LabelCap,
            CurrentFlowWatts.ToString("F0", CultureInfo.InvariantCulture)
        );

    public override void PostDraw()
    {
        base.PostDraw();
        var partner = Partner;
        if (partner == null)
        {
            return;
        }
        PowerDiodeConnectorOverlay.DrawConnectorNub(
            parent,
            partner.parent.Position - parent.Position
        );
        if (IsSharedGridDegenerate)
        {
            PowerDiodeOverlay.DrawSharedGridOverlay(parent);
        }
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (var gizmo in base.CompGetGizmosExtra())
        {
            yield return gizmo;
        }
        yield return CreateModeGizmo();
        switch (OperatingMode)
        {
            case PowerDiodeOperatingMode.Overflow:
                yield return new Gizmo_SetDiodeOverflowThreshold(this);
                break;
            case PowerDiodeOperatingMode.TopUp:
                yield return new Gizmo_SetDiodeTopUpThreshold(this);
                break;
            case PowerDiodeOperatingMode.OneWayValve:
            default:
                yield return new Gizmo_SetDiodeReserve(this);
                break;
        }
        yield return new Gizmo_SetDiodeWattage(this);
    }

    private Command_Action CreateModeGizmo() =>
        new()
        {
            defaultLabel = "PowerDiode.OperatingModeGizmoLabel".Translate(OperatingMode.Label()),
            defaultDesc = OperatingMode.Description(),
            icon = OperatingMode.Icon(),
            action = () =>
                Find.WindowStack.Add(
                    new FloatMenu([
                        .. Enum.GetValues(typeof(PowerDiodeOperatingMode))
                            .Cast<PowerDiodeOperatingMode>()
                            .Select(mode => new FloatMenuOption(
                                mode.Label(),
                                () => OperatingMode = mode
                            )),
                    ])
                ),
        };
}
