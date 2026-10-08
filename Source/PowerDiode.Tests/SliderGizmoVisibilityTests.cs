using DevTools.Testing;

namespace PowerDiode.Tests;

[TestFixture(TestType.MainMenu)]
internal sealed class SliderGizmoVisibilityTests
{
    private float originalMinWattage;
    private float originalMaxWattage;
    private float originalMinReserve;
    private float originalMaxReserve;
    private bool originalReserveIsPercentage;

    [SetUp]
    public void SaveSettings()
    {
        var settings = PowerDiodeMod.Settings;
        originalMinWattage = settings.MinWattage;
        originalMaxWattage = settings.MaxWattage;
        originalMinReserve = settings.MinReserveWattDays;
        originalMaxReserve = settings.MaxReserveWattDays;
        originalReserveIsPercentage = settings.ReserveIsPercentage;
    }

    [TearDown]
    public void RestoreSettings()
    {
        var settings = PowerDiodeMod.Settings;
        settings.MinWattage = originalMinWattage;
        settings.MaxWattage = originalMaxWattage;
        settings.MinReserveWattDays = originalMinReserve;
        settings.MaxReserveWattDays = originalMaxReserve;
        settings.ReserveIsPercentage = originalReserveIsPercentage;
    }

    // Built without PostMake/ID, as in CompPowerDiodeFeedSettingsTests. The feed gets a partner
    // so CompGetGizmosExtra skips the link gizmo, which is the only part that needs a map.
    private static CompPowerDiodeFeed MakePairedFeedComp()
    {
        var feed = MakeBuilding("PowerDiode_FeedNode").GetComp<CompPowerDiodeFeed>();
        feed.Partner = MakeBuilding("PowerDiode_DrawNode").GetComp<CompPowerDiodeDraw>();
        return feed;
    }

    private static Building MakeBuilding(string defName)
    {
        var def = DefDatabase<ThingDef>.GetNamed(defName);
        var building = (Building)Activator.CreateInstance(def.thingClass);
        building.def = def;
        building.InitializeComps();
        return building;
    }

    [Test]
    public static void WattageSliderIsHiddenWhenMinEqualsMax()
    {
        var settings = PowerDiodeMod.Settings;
        var feed = MakePairedFeedComp();

        settings.MinWattage = 0f;
        settings.MaxWattage = 5000f;
        Expect.IsTrue(feed.CompGetGizmosExtra().OfType<Gizmo_SetDiodeWattage>().Any());

        settings.MinWattage = 1000f;
        settings.MaxWattage = 1000f;
        Expect.IsFalse(feed.CompGetGizmosExtra().OfType<Gizmo_SetDiodeWattage>().Any());
    }

    [Test]
    public static void ReserveSliderIsHiddenWhenMinEqualsMaxInWattDayMode()
    {
        var settings = PowerDiodeMod.Settings;
        var feed = MakePairedFeedComp();
        feed.OperatingMode = PowerDiodeOperatingMode.OneWayValve;
        settings.ReserveIsPercentage = false;

        settings.MinReserveWattDays = 10f;
        settings.MaxReserveWattDays = 500f;
        Expect.IsTrue(feed.CompGetGizmosExtra().OfType<Gizmo_SetDiodeReserve>().Any());

        settings.MinReserveWattDays = 50f;
        settings.MaxReserveWattDays = 50f;
        Expect.IsFalse(feed.CompGetGizmosExtra().OfType<Gizmo_SetDiodeReserve>().Any());
    }

    [Test]
    public static void ReserveSliderIsShownInPercentModeEvenWhenWattDayMinEqualsMax()
    {
        var settings = PowerDiodeMod.Settings;
        var feed = MakePairedFeedComp();
        feed.OperatingMode = PowerDiodeOperatingMode.OneWayValve;
        settings.ReserveIsPercentage = true;
        settings.MinReserveWattDays = 50f;
        settings.MaxReserveWattDays = 50f;

        Expect.IsTrue(feed.CompGetGizmosExtra().OfType<Gizmo_SetDiodeReserve>().Any());
    }
}
