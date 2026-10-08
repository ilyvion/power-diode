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
    private bool pairingSaved;

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

    // Read live from both sides' power nets, so it's right even before the first tick after a
    // spawn or load.
    internal PowerDiodeMissingBatteries MissingBatteries
    {
        get
        {
            var outletNet = PowerTrader.PowerNet;
            var intakeNet = Partner?.PowerTrader.PowerNet;
            return
                outletNet == null
                || intakeNet == null
                || PowerDiodeSharedGridDetection.IsSharedGrid(outletNet, intakeNet)
                ? PowerDiodeMissingBatteries.None
                : PowerDiodeMissingBatteriesDetection.MissingBatteries(
                    OperatingMode,
                    intakeHasBatteries: intakeNet.batteryComps.Count > 0,
                    outletHasBatteries: outletNet.batteryComps.Count > 0
                );
        }
    }

    internal CompPowerTrader PowerTrader
    {
        get
        {
            field ??= parent.GetComp<CompPowerTrader>();
            return field;
        }
    }

    // Defaults are set once, when the comp is created, so a reinstalled outlet keeps its settings.
    public override void Initialize(CompProperties props)
    {
        base.Initialize(props);
        targetWatts = PowerDiodeMod.Settings.MaxWattage;
        reserveWattDays = PowerDiodeMod.Settings.MinReserveWattDays;
        reservePercent = DefaultReservePercent;
        overflowThresholdPercent = DefaultOverflowThresholdPercent;
        topUpThresholdPercent = DefaultTopUpThresholdPercent;
        operatingMode = PowerDiodeOperatingMode.OneWayValve;
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (Partner == null && !(respawningAfterLoad && pairingSaved))
        {
            PowerDiodeLinking.TryLinkFeedNode(this);
        }
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(
            ref targetWatts,
            "targetWatts",
            PowerDiodeMod.Settings.MaxWattage,
            forceSave: true
        );
        Scribe_Values.Look(
            ref reserveWattDays,
            "reserveWattDays",
            PowerDiodeMod.Settings.MinReserveWattDays,
            forceSave: true
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

        PowerDiodeLinking.ExposePairingSaved(ref pairingSaved);
        var partnerThing = Partner?.parent;
        Scribe_References.Look(ref partnerThing, "partner");
        if (
            Scribe.mode == LoadSaveMode.ResolvingCrossRefs
            && partnerThing?.GetComp<CompPowerDiodeDraw>() is { } draw
        )
        {
            PowerDiodeLinking.LinkLoaded(draw, this);
        }
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

        CurrentFlowWatts = PowerDiodeFlow.TickFlowWatts(
            new(
                Mode: OperatingMode,
                TargetWatts: TargetWatts,
                ReserveWattDays: ReserveWattDays,
                ReserveIsPercentage: PowerDiodeMod.Settings.ReserveIsPercentage,
                ReservePercent: ReservePercent,
                OverflowThresholdPercent: OverflowThresholdPercent,
                TopUpThresholdPercent: TopUpThresholdPercent,
                SourceRawBalanceExclSelf: NetBalanceExcluding(sourceNet, partner.PowerTrader),
                SourceSwitchedOffDrawWatts:
                [
                    .. SwitchedOffDrawWatts(sourceNet, partner.PowerTrader),
                ],
                SinkRawBalanceExclSelf: NetBalanceExcluding(sinkNet, PowerTrader),
                SinkSwitchedOffDrawWatts: [.. SwitchedOffDrawWatts(sinkNet, PowerTrader)],
                SinkAcceptWattDays: sinkNet.batteryComps.Sum(battery =>
                    Math.Max(0f, battery.AmountCanAccept)
                ),
                SourceStoredWattDays: SourceBatteryStoredWattDays,
                SourceCapacityWattDays: SourceBatteryCapacityWattDays,
                SinkStoredWattDays: SinkBatteryStoredWattDays,
                SinkCapacityWattDays: SinkBatteryCapacityWattDays
            )
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

    // The draw of each consumer vanilla PowerNet.PowerNetTick would switch on if the network had
    // the surplus for it.
    private static IEnumerable<float> SwitchedOffDrawWatts(PowerNet net, CompPowerTrader self) =>
        net
            .powerComps.Where(comp =>
                comp != self
                && !comp.PowerOn
                && comp.PowerOutput < 0f
                && FlickUtility.WantsToBeOn(comp.parent)
                && !comp.parent.IsBrokenDown()
            )
            .Select(comp => -comp.PowerOutput);

    public override string CompInspectStringExtra() =>
        Partner == null ? "PowerDiode.NotLinked".Translate()
        : IsSharedGridDegenerate ? "PowerDiode.SharedGrid".Translate(Partner.parent.LabelCap)
        : MissingBatteries == PowerDiodeMissingBatteries.Outlet
            ? "PowerDiode.NoOutletBatteries".Translate(
                Partner.parent.LabelCap,
                OperatingMode.Label()
            )
        : CurrentFlowWatts <= 0f ? "PowerDiode.LinkedIdle".Translate(Partner.parent.LabelCap)
        : "PowerDiode.ReceivingFrom".Translate(
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
        else if (MissingBatteries == PowerDiodeMissingBatteries.Outlet)
        {
            PowerDiodeOverlay.DrawNoBatteriesOverlay(parent);
        }
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (var gizmo in base.CompGetGizmosExtra())
        {
            yield return gizmo;
        }
        if (Partner == null)
        {
            var linkGizmo = PowerDiodeLinkGizmo.Create(
                parent,
                PowerDiodeLinking.UnpairedAdjacentDrawNodes(this),
                "PowerDiode.LinkToIntake".Translate(),
                "PowerDiode.LinkToIntakeDesc".Translate(),
                draw => PowerDiodeLinking.LinkSpawned(draw, this)
            );
            if (linkGizmo != null)
            {
                yield return linkGizmo;
            }
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
                if (Gizmo_SetDiodeReserve.IsShown)
                {
                    yield return new Gizmo_SetDiodeReserve(this);
                }
                break;
        }
        if (Gizmo_SetDiodeWattage.IsShown)
        {
            yield return new Gizmo_SetDiodeWattage(this);
        }
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
