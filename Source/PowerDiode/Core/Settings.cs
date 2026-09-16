namespace PowerDiode;

[HotSwappable]
internal class Settings : ModSettings
{
    internal const float DefaultMinWattage = 0f;
    internal const float DefaultMaxWattage = 5000f;
    internal const float DefaultMinReserveWattDays = 10f;
    internal const float DefaultMaxReserveWattDays = 500f;

    private const float WattageFieldCeiling = 1_000_000f;
    private const float ReserveWattDaysFieldCeiling = 1_000_000f;

    public float MinWattage = DefaultMinWattage;
    public float MaxWattage = DefaultMaxWattage;
    public float MinReserveWattDays = DefaultMinReserveWattDays;
    public float MaxReserveWattDays = DefaultMaxReserveWattDays;
    public bool ReserveIsPercentage;

    private static string minWattageBuffer = DefaultMinWattage.ToString(
        CultureInfo.InvariantCulture
    );
    private static string maxWattageBuffer = DefaultMaxWattage.ToString(
        CultureInfo.InvariantCulture
    );
    private static string minReserveWattDaysBuffer = DefaultMinReserveWattDays.ToString(
        CultureInfo.InvariantCulture
    );
    private static string maxReserveWattDaysBuffer = DefaultMaxReserveWattDays.ToString(
        CultureInfo.InvariantCulture
    );

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref MinWattage, "minWattage", DefaultMinWattage);
        Scribe_Values.Look(ref MaxWattage, "maxWattage", DefaultMaxWattage);
        Scribe_Values.Look(ref MinReserveWattDays, "minReserveWattDays", DefaultMinReserveWattDays);
        Scribe_Values.Look(ref MaxReserveWattDays, "maxReserveWattDays", DefaultMaxReserveWattDays);
        Scribe_Values.Look(ref ReserveIsPercentage, "reserveIsPercentage");
    }

    public static void DoSettingsWindowContents(Rect inRect)
    {
        var settings = PowerDiodeMod.Settings;
        var listing = new Listing_Standard();
        listing.Begin(inRect);
        listing.verticalSpacing = 5f;

        _ = listing.Label("PowerDiode.Settings.WattageSectionLabel".Translate());
        listing.TextFieldNumericLabeled(
            "PowerDiode.Settings.MinWattage".Translate(),
            ref settings.MinWattage,
            ref minWattageBuffer,
            0f,
            settings.MaxWattage
        );
        listing.TextFieldNumericLabeled(
            "PowerDiode.Settings.MaxWattage".Translate(),
            ref settings.MaxWattage,
            ref maxWattageBuffer,
            settings.MinWattage,
            WattageFieldCeiling
        );

        listing.Gap();

        _ = listing.Label("PowerDiode.Settings.ReserveSectionLabel".Translate());
        listing.TextFieldNumericLabeled(
            "PowerDiode.Settings.MinReserveWattDays".Translate(),
            ref settings.MinReserveWattDays,
            ref minReserveWattDaysBuffer,
            0f,
            settings.MaxReserveWattDays
        );
        listing.TextFieldNumericLabeled(
            "PowerDiode.Settings.MaxReserveWattDays".Translate(),
            ref settings.MaxReserveWattDays,
            ref maxReserveWattDaysBuffer,
            settings.MinReserveWattDays,
            ReserveWattDaysFieldCeiling
        );
        listing.CheckboxLabeled(
            "PowerDiode.Settings.ReserveIsPercentage".Translate(),
            ref settings.ReserveIsPercentage,
            "PowerDiode.Settings.ReserveIsPercentageTooltip".Translate()
        );

        listing.End();
    }
}
