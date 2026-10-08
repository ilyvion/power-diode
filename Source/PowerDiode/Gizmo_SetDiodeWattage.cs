namespace PowerDiode;

internal class Gizmo_SetDiodeWattage : Gizmo_Slider
{
    private readonly CompPowerDiodeFeed feed;

    private static float MinWatts => PowerDiodeMod.Settings.MinWattage;
    private static float MaxWatts => PowerDiodeMod.Settings.MaxWattage;

    internal static bool IsShown => DiodeSliderMath.HasRange(MinWatts, MaxWatts);

    protected override float Target
    {
        get => DiodeSliderMath.ToFraction(feed.TargetWatts, MinWatts, MaxWatts);
        set => feed.TargetWatts = DiodeSliderMath.FromFraction(value, MinWatts, MaxWatts);
    }

    protected override float ValuePercent =>
        Mathf.Clamp01(DiodeSliderMath.ToFraction(feed.CurrentFlowWatts, MinWatts, MaxWatts));

    protected override string Title => "PowerDiode.WattageCapGizmoTitle".Translate();

    protected override bool IsDraggable => true;

    protected override string BarLabel =>
        "PowerDiode.WattageCapBarLabel".Translate(
            feed.TargetWatts.ToString("F0", CultureInfo.InvariantCulture),
            MaxWatts.ToString("F0", CultureInfo.InvariantCulture)
        );

    protected override int Increments =>
        DiodeSliderMath.Increments(MaxWatts - MinWatts, feed.Props.wattageStepSize);

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
