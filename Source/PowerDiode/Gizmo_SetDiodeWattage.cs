namespace PowerDiode;

internal class Gizmo_SetDiodeWattage : Gizmo_Slider
{
    private readonly CompPowerDiodeFeed feed;

    private static bool draggingBar;

    protected override float Target
    {
        get => feed.TargetWatts / feed.Props.maxAllowedWattage;
        set => feed.TargetWatts = value * feed.Props.maxAllowedWattage;
    }

    protected override float ValuePercent => feed.CurrentFlowWatts / feed.Props.maxAllowedWattage;

    protected override string Title => "PowerDiode.WattageCapGizmoTitle".Translate();

    protected override bool IsDraggable => true;

    protected override string BarLabel =>
        "PowerDiode.WattageCapBarLabel".Translate(
            feed.TargetWatts.ToString("F0", CultureInfo.InvariantCulture),
            feed.Props.maxAllowedWattage.ToString("F0", CultureInfo.InvariantCulture)
        );

    protected override int Increments =>
        Mathf.Max(1, Mathf.RoundToInt(feed.Props.maxAllowedWattage / feed.Props.wattageStepSize));

    protected override bool DraggingBar
    {
        get => draggingBar;
        set => draggingBar = value;
    }

    internal Gizmo_SetDiodeWattage(CompPowerDiodeFeed feed)
    {
        this.feed = feed;
    }

    protected override string GetTooltip() => "PowerDiode.WattageCapTooltip".Translate();
}
