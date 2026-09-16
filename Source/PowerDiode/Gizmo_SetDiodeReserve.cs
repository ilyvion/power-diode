namespace PowerDiode;

internal class Gizmo_SetDiodeReserve : Gizmo_Slider
{
    private readonly CompPowerDiodeFeed feed;

    private static bool IsPercentMode => PowerDiodeMod.Settings.ReserveIsPercentage;

    private static float SliderRangeWattDays =>
        PowerDiodeMod.Settings.MaxReserveWattDays - PowerDiodeMod.Settings.MinReserveWattDays;

    protected override float Target
    {
        get =>
            IsPercentMode
                ? feed.ReservePercent / 100f
                : (feed.ReserveWattDays - PowerDiodeMod.Settings.MinReserveWattDays)
                    / SliderRangeWattDays;
        set
        {
            if (IsPercentMode)
            {
                feed.ReservePercent = value * 100f;
            }
            else
            {
                feed.ReserveWattDays =
                    PowerDiodeMod.Settings.MinReserveWattDays + (value * SliderRangeWattDays);
            }
        }
    }

    protected override float ValuePercent =>
        IsPercentMode
            ? feed.SourceBatteryStoredPercent
            : Mathf.Clamp01(
                (feed.SourceBatteryStoredWattDays - PowerDiodeMod.Settings.MinReserveWattDays)
                    / SliderRangeWattDays
            );

    protected override string Title => "PowerDiode.ReserveGizmoTitle".Translate();

    protected override bool IsDraggable => true;

    protected override string BarLabel =>
        IsPercentMode
            ? "PowerDiode.PercentBarLabel".Translate(
                feed.ReservePercent.ToString("F0", CultureInfo.InvariantCulture)
            )
            : "PowerDiode.ReserveBarLabel".Translate(
                feed.ReserveWattDays.ToString("F0", CultureInfo.InvariantCulture)
            );

    protected override int Increments =>
        IsPercentMode
            ? Mathf.Max(1, Mathf.RoundToInt(100f / feed.Props.percentStepSize))
            : Mathf.Max(
                1,
                Mathf.RoundToInt(SliderRangeWattDays / feed.Props.reserveWattDaysStepSize)
            );

    protected override bool DraggingBar
    {
        get => feed.draggingReserveBar;
        set => feed.draggingReserveBar = value;
    }

    internal Gizmo_SetDiodeReserve(CompPowerDiodeFeed feed)
    {
        this.feed = feed;
    }

    protected override string GetTooltip() =>
        IsPercentMode
            ? "PowerDiode.ReservePercentTooltip".Translate()
            : "PowerDiode.ReserveTooltip".Translate();

    // A fresh Gizmo_SetDiodeReserve is created every GUI frame, so the default
    // identity-based hash code changes every frame too. TooltipHandler.TipRegion keys its
    // hover-delay tracking by this hash, so a changing value means the tooltip's initial
    // delay never elapses and it never shows. Deriving from the feed's parent instead keeps
    // it stable across frames.
    public override int GetHashCode() =>
        HashCode.Combine(typeof(Gizmo_SetDiodeReserve), feed.parent.thingIDNumber);
}
