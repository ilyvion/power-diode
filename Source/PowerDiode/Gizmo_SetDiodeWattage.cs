namespace PowerDiode;

internal class Gizmo_SetDiodeWattage : Gizmo_Slider
{
    private readonly CompPowerDiodeFeed feed;

    private static float SliderRangeWatts =>
        PowerDiodeMod.Settings.MaxWattage - PowerDiodeMod.Settings.MinWattage;

    protected override float Target
    {
        get => (feed.TargetWatts - PowerDiodeMod.Settings.MinWattage) / SliderRangeWatts;
        set => feed.TargetWatts = PowerDiodeMod.Settings.MinWattage + (value * SliderRangeWatts);
    }

    protected override float ValuePercent =>
        Mathf.Clamp01(
            (feed.CurrentFlowWatts - PowerDiodeMod.Settings.MinWattage) / SliderRangeWatts
        );

    protected override string Title => "PowerDiode.WattageCapGizmoTitle".Translate();

    protected override bool IsDraggable => true;

    protected override string BarLabel =>
        "PowerDiode.WattageCapBarLabel".Translate(
            feed.TargetWatts.ToString("F0", CultureInfo.InvariantCulture),
            PowerDiodeMod.Settings.MaxWattage.ToString("F0", CultureInfo.InvariantCulture)
        );

    protected override int Increments =>
        Mathf.Max(1, Mathf.RoundToInt(SliderRangeWatts / feed.Props.wattageStepSize));

    protected override bool DraggingBar
    {
        get => feed.draggingWattageBar;
        set => feed.draggingWattageBar = value;
    }

    internal Gizmo_SetDiodeWattage(CompPowerDiodeFeed feed)
    {
        this.feed = feed;
    }

    protected override string GetTooltip() => "PowerDiode.WattageCapTooltip".Translate();

    // See Gizmo_SetDiodeReserve.GetHashCode for why this override is needed.
    public override int GetHashCode() =>
        HashCode.Combine(typeof(Gizmo_SetDiodeWattage), feed.parent.thingIDNumber);
}
