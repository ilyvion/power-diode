namespace PowerDiode;

[HotSwappable]
internal class CompPowerDiodeFeed : ThingComp
{
    private float targetWatts;
    private float reserveWattDays;

    internal CompPowerDiodeDraw? Partner { get; set; }

    internal CompProperties_PowerDiodeFeed Props => (CompProperties_PowerDiodeFeed)props;

    internal float TargetWatts
    {
        get => targetWatts;
        set => targetWatts = Mathf.Clamp(value, 0f, Props.maxAllowedWattage);
    }

    // How many watt-days of stored energy on the draw node's power net batteries are kept
    // untouchable - once their total stored energy nears this floor, PowerDiodeFlow ramps this
    // diode's draw down to 0 rather than letting the batteries actually run dry.
    internal float ReserveWattDays
    {
        get => reserveWattDays;
        set =>
            reserveWattDays = Mathf.Clamp(
                value,
                Props.minReserveWattDays,
                Props.maxReserveWattDays
            );
    }

    internal float CurrentFlowWatts { get; private set; }

    // The draw node's power net batteries' total stored energy, cached each tick for the reserve
    // gizmo to show alongside the ReserveWattDays threshold it's set against.
    internal float SourceBatteryStoredWattDays { get; private set; }

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
            targetWatts = Props.maxAllowedWattage;
            reserveWattDays = Props.minReserveWattDays;
        }
        if (Partner == null)
        {
            PowerDiodeLinking.TryLinkFeedNode(this);
        }
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref targetWatts, "targetWatts", Props.maxAllowedWattage);
        Scribe_Values.Look(ref reserveWattDays, "reserveWattDays", Props.minReserveWattDays);
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
            IsSharedGridDegenerate = false;
            PowerTrader.PowerOutput = 0f;
            partner.PowerTrader.PowerOutput = 0f;
            return;
        }

        if (PowerDiodeSharedGridDetection.IsSharedGrid(sinkNet, sourceNet))
        {
            CurrentFlowWatts = 0f;
            SourceBatteryStoredWattDays = 0f;
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
        var sinkBatteryHeadroomWatts = PowerDiodeFlow.BatterySustainableWatts(sinkAcceptWattDays);
        var sourceBatteryReserveWatts = PowerDiodeFlow.BatterySustainableWatts(
            SourceBatteryStoredWattDays,
            ReserveWattDays
        );

        CurrentFlowWatts = PowerDiodeFlow.ComputeFlowWatts(
            TargetWatts,
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
        yield return new Gizmo_SetDiodeReserve(this);
        yield return new Gizmo_SetDiodeWattage(this);
    }
}
