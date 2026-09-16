using RimTestRedux;

namespace PowerDiode.Tests;

[TestSuite]
internal static class CompPowerDiodeFeedSettingsTests
{
    // ThingMaker.MakeThing's PostMake calls ThingIDMaker.GiveIDTo, which needs
    // Find.UniqueIDsManager - unavailable outside a loaded game/map, and RimTest Redux runs at
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
            Assert.That(feed.TargetWatts).Is.EqualTo(100f);

            feed.TargetWatts = 1000f;
            Assert.That(feed.TargetWatts).Is.EqualTo(200f);
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
            Assert.That(feed.ReserveWattDays).Is.EqualTo(20f);

            feed.ReserveWattDays = 1000f;
            Assert.That(feed.ReserveWattDays).Is.EqualTo(300f);
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
        Assert.That(feed.ReservePercent).Is.EqualTo(0f);

        feed.ReservePercent = 150f;
        Assert.That(feed.ReservePercent).Is.EqualTo(100f);
    }

    [Test]
    public static void OverflowThresholdPercentIsClampedTo0To100()
    {
        var feed = MakeFeedComp();

        feed.OverflowThresholdPercent = -10f;
        Assert.That(feed.OverflowThresholdPercent).Is.EqualTo(0f);

        feed.OverflowThresholdPercent = 150f;
        Assert.That(feed.OverflowThresholdPercent).Is.EqualTo(100f);
    }

    [Test]
    public static void TopUpThresholdPercentIsClampedTo0To100()
    {
        var feed = MakeFeedComp();

        feed.TopUpThresholdPercent = -10f;
        Assert.That(feed.TopUpThresholdPercent).Is.EqualTo(0f);

        feed.TopUpThresholdPercent = 150f;
        Assert.That(feed.TopUpThresholdPercent).Is.EqualTo(100f);
    }
}
