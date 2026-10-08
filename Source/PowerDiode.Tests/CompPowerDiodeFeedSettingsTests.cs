using DevTools.Testing;

namespace PowerDiode.Tests;

[TestFixture(TestType.MainMenu)]
internal sealed class CompPowerDiodeFeedSettingsTests
{
    // ThingMaker.MakeThing's PostMake calls ThingIDMaker.GiveIDTo, which needs
    // Find.UniqueIDsManager - unavailable outside a loaded game/map, and these tests run at
    // the main menu. These tests only touch comp state, so constructing the ThingWithComps
    // directly and initializing its comps - skipping PostMake entirely - is enough.
    private static CompPowerDiodeFeed MakeFeedComp()
    {
        var def = DefDatabase<ThingDef>.GetNamed("PowerDiode_FeedNode");
        var building = (Building)Activator.CreateInstance(def.thingClass);
        building.def = def;
        building.InitializeComps();
        return building.GetComp<CompPowerDiodeFeed>();
    }

    [Test]
    public static void TargetWattsIsClampedToSettingsRange()
    {
        var settings = PowerDiodeMod.Settings;
        var originalMin = settings.MinWattage;
        var originalMax = settings.MaxWattage;
        try
        {
            settings.MinWattage = 100f;
            settings.MaxWattage = 200f;
            var feed = MakeFeedComp();

            feed.TargetWatts = 50f;
            Expect.AreEqual(100f, feed.TargetWatts);

            feed.TargetWatts = 1000f;
            Expect.AreEqual(200f, feed.TargetWatts);
        }
        finally
        {
            settings.MinWattage = originalMin;
            settings.MaxWattage = originalMax;
        }
    }

    [Test]
    public static void ReserveWattDaysIsClampedToSettingsRange()
    {
        var settings = PowerDiodeMod.Settings;
        var originalMin = settings.MinReserveWattDays;
        var originalMax = settings.MaxReserveWattDays;
        try
        {
            settings.MinReserveWattDays = 20f;
            settings.MaxReserveWattDays = 300f;
            var feed = MakeFeedComp();

            feed.ReserveWattDays = 5f;
            Expect.AreEqual(20f, feed.ReserveWattDays);

            feed.ReserveWattDays = 1000f;
            Expect.AreEqual(300f, feed.ReserveWattDays);
        }
        finally
        {
            settings.MinReserveWattDays = originalMin;
            settings.MaxReserveWattDays = originalMax;
        }
    }

    [Test]
    public static void ReservePercentIsClampedTo0To100()
    {
        var feed = MakeFeedComp();

        feed.ReservePercent = -10f;
        Expect.AreEqual(0f, feed.ReservePercent);

        feed.ReservePercent = 150f;
        Expect.AreEqual(100f, feed.ReservePercent);
    }

    [Test]
    public static void OverflowThresholdPercentIsClampedTo0To100()
    {
        var feed = MakeFeedComp();

        feed.OverflowThresholdPercent = -10f;
        Expect.AreEqual(0f, feed.OverflowThresholdPercent);

        feed.OverflowThresholdPercent = 150f;
        Expect.AreEqual(100f, feed.OverflowThresholdPercent);
    }

    [Test]
    public static void TopUpThresholdPercentIsClampedTo0To100()
    {
        var feed = MakeFeedComp();

        feed.TopUpThresholdPercent = -10f;
        Expect.AreEqual(0f, feed.TopUpThresholdPercent);

        feed.TopUpThresholdPercent = 150f;
        Expect.AreEqual(100f, feed.TopUpThresholdPercent);
    }
}
