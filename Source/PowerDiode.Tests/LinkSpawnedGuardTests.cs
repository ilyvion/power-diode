using DevTools.Testing;

namespace PowerDiode.Tests;

// The link gizmo's float menu captures its candidates when opened, so by the time an option is
// picked, either side may have been paired some other way; LinkSpawned must then do nothing.
[TestFixture(TestType.MainMenu)]
internal sealed class LinkSpawnedGuardTests
{
    // Built without PostMake/ID, as in CompPowerDiodeFeedSettingsTests. LinkSpawned returns
    // before touching the map when it refuses, so no map is needed.
    private static T MakeComp<T>(string defName)
        where T : ThingComp
    {
        var def = DefDatabase<ThingDef>.GetNamed(defName);
        var building = (Building)Activator.CreateInstance(def.thingClass);
        building.def = def;
        building.InitializeComps();
        return building.GetComp<T>();
    }

    private static CompPowerDiodeDraw MakeDraw() =>
        MakeComp<CompPowerDiodeDraw>("PowerDiode_DrawNode");

    private static CompPowerDiodeFeed MakeFeed() =>
        MakeComp<CompPowerDiodeFeed>("PowerDiode_FeedNode");

    private static void Pair(CompPowerDiodeDraw draw, CompPowerDiodeFeed feed)
    {
        draw.Partner = feed;
        feed.Partner = draw;
    }

    [Test]
    public static void RefusesWhenIntakeIsAlreadyPaired()
    {
        var draw = MakeDraw();
        var pairedFeed = MakeFeed();
        Pair(draw, pairedFeed);
        var otherFeed = MakeFeed();

        PowerDiodeLinking.LinkSpawned(draw, otherFeed);

        Expect.ReferencesAreEqual(pairedFeed, draw.Partner);
        Expect.ReferencesAreEqual(draw, pairedFeed.Partner);
        Expect.IsNull(otherFeed.Partner);
    }

    [Test]
    public static void RefusesWhenOutletIsAlreadyPaired()
    {
        var feed = MakeFeed();
        var pairedDraw = MakeDraw();
        Pair(pairedDraw, feed);
        var otherDraw = MakeDraw();

        PowerDiodeLinking.LinkSpawned(otherDraw, feed);

        Expect.ReferencesAreEqual(pairedDraw, feed.Partner);
        Expect.ReferencesAreEqual(feed, pairedDraw.Partner);
        Expect.IsNull(otherDraw.Partner);
    }
}
