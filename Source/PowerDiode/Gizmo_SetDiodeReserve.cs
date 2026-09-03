namespace PowerDiode;

internal class Gizmo_SetDiodeReserve : Gizmo_Slider
{
    private readonly CompPowerDiodeFeed feed;

    private static bool draggingBar;

    private float SliderRangeWattDays =>
        feed.Props.maxReserveWattDays - feed.Props.minReserveWattDays;

    protected override float Target
    {
        get => (feed.ReserveWattDays - feed.Props.minReserveWattDays) / SliderRangeWattDays;
        set => feed.ReserveWattDays = feed.Props.minReserveWattDays + (value * SliderRangeWattDays);
    }

    protected override float ValuePercent =>
        Mathf.Clamp01(
            (feed.SourceBatteryStoredWattDays - feed.Props.minReserveWattDays) / SliderRangeWattDays
        );

    protected override string Title => "PowerDiode.ReserveGizmoTitle".Translate();

    protected override bool IsDraggable => true;

    protected override string BarLabel =>
        "PowerDiode.ReserveBarLabel".Translate(
            feed.ReserveWattDays.ToString("F0", CultureInfo.InvariantCulture)
        );

    protected override int Increments =>
        Mathf.Max(1, Mathf.RoundToInt(SliderRangeWattDays / feed.Props.reserveWattDaysStepSize));

    protected override bool DraggingBar
    {
        get => draggingBar;
        set => draggingBar = value;
    }

    internal Gizmo_SetDiodeReserve(CompPowerDiodeFeed feed)
    {
        this.feed = feed;
    }

    protected override string GetTooltip() => "PowerDiode.ReserveTooltip".Translate();
}
