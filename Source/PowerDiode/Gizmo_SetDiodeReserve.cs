namespace PowerDiode;

internal class Gizmo_SetDiodeReserve : Gizmo_Slider
{
    private readonly CompPowerDiodeFeed feed;

    private static bool draggingBar;

    private static float SliderRangeWattDays =>
        PowerDiodeMod.Settings.MaxReserveWattDays - PowerDiodeMod.Settings.MinReserveWattDays;

    protected override float Target
    {
        get =>
            (feed.ReserveWattDays - PowerDiodeMod.Settings.MinReserveWattDays)
            / SliderRangeWattDays;
        set =>
            feed.ReserveWattDays =
                PowerDiodeMod.Settings.MinReserveWattDays + (value * SliderRangeWattDays);
    }

    protected override float ValuePercent =>
        Mathf.Clamp01(
            (feed.SourceBatteryStoredWattDays - PowerDiodeMod.Settings.MinReserveWattDays)
                / SliderRangeWattDays
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
