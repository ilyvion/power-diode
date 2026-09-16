namespace PowerDiode;

internal class Gizmo_SetDiodeTopUpThreshold : Gizmo_Slider
{
    private readonly CompPowerDiodeFeed feed;

    protected override float Target
    {
        get => feed.TopUpThresholdPercent / 100f;
        set => feed.TopUpThresholdPercent = value * 100f;
    }

    protected override float ValuePercent => feed.SinkBatteryStoredPercent;

    protected override string Title => "PowerDiode.TopUpThresholdGizmoTitle".Translate();

    protected override bool IsDraggable => true;

    protected override string BarLabel =>
        "PowerDiode.PercentBarLabel".Translate(
            feed.TopUpThresholdPercent.ToString("F0", CultureInfo.InvariantCulture)
        );

    protected override int Increments =>
        Mathf.Max(1, Mathf.RoundToInt(100f / feed.Props.percentStepSize));

    protected override bool DraggingBar
    {
        get => feed.draggingTopUpBar;
        set => feed.draggingTopUpBar = value;
    }

    internal Gizmo_SetDiodeTopUpThreshold(CompPowerDiodeFeed feed)
    {
        this.feed = feed;
    }

    protected override string GetTooltip() => "PowerDiode.TopUpThresholdTooltip".Translate();

    // See Gizmo_SetDiodeReserve.GetHashCode for why this override is needed.
    public override int GetHashCode() =>
        HashCode.Combine(typeof(Gizmo_SetDiodeTopUpThreshold), feed.parent.thingIDNumber);
}
