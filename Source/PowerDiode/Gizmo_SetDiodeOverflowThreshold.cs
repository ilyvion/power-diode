namespace PowerDiode;

internal class Gizmo_SetDiodeOverflowThreshold : Gizmo_Slider
{
    private readonly CompPowerDiodeFeed feed;

    protected override float Target
    {
        get => feed.OverflowThresholdPercent / 100f;
        set => feed.OverflowThresholdPercent = value * 100f;
    }

    protected override float ValuePercent => feed.SourceBatteryStoredPercent;

    protected override string Title => "PowerDiode.OverflowThresholdGizmoTitle".Translate();

    protected override bool IsDraggable => true;

    protected override string BarLabel =>
        "PowerDiode.PercentBarLabel".Translate(
            feed.OverflowThresholdPercent.ToString("F0", CultureInfo.InvariantCulture)
        );

    protected override int Increments =>
        Mathf.Max(1, Mathf.RoundToInt(100f / feed.Props.percentStepSize));

    protected override bool DraggingBar
    {
        get => feed.draggingOverflowBar;
        set => feed.draggingOverflowBar = value;
    }

    internal Gizmo_SetDiodeOverflowThreshold(CompPowerDiodeFeed feed)
    {
        this.feed = feed;
    }

    protected override string GetTooltip() => "PowerDiode.OverflowThresholdTooltip".Translate();

    // See Gizmo_SetDiodeReserve.GetHashCode for why this override is needed.
    public override int GetHashCode() =>
        HashCode.Combine(typeof(Gizmo_SetDiodeOverflowThreshold), feed.parent.thingIDNumber);
}
