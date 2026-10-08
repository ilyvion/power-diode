using DevTools.Testing;
using PowerDiode.Patch;

namespace PowerDiode.Tests;

[TestFixture(TestType.MainMenu)]
internal sealed class PowerNetMakerContiguousPowerBuildingsTests
{
    private static Building MakeDrawNode() => MakeBuilding("PowerDiode_DrawNode");

    private static Building MakeFeedNode() => MakeBuilding("PowerDiode_FeedNode");

    // ThingMaker.MakeThing's PostMake calls ThingIDMaker.GiveIDTo, which needs
    // Find.UniqueIDsManager - unavailable outside a loaded game/map, and these tests run at
    // the main menu. These tests only touch comp state, so constructing the ThingWithComps
    // directly and initializing its comps - skipping PostMake entirely - is enough.
    private static Building MakeBuilding(string defName)
    {
        var def = DefDatabase<ThingDef>.GetNamed(defName);
        var building = (Building)Activator.CreateInstance(def.thingClass);
        building.def = def;
        building.InitializeComps();
        return building;
    }

    [Test]
    public static void LinkedPairIsBlockedInBothDirections()
    {
        var draw = MakeDrawNode();
        var feed = MakeFeedNode();
        var drawComp = draw.GetComp<CompPowerDiodeDraw>();
        var feedComp = feed.GetComp<CompPowerDiodeFeed>();
        drawComp.Partner = feedComp;
        feedComp.Partner = drawComp;

        Expect.IsTrue(PowerNetMaker_ContiguousPowerBuildings.IsDiodeLinkEdge(draw, feed));
        Expect.IsTrue(PowerNetMaker_ContiguousPowerBuildings.IsDiodeLinkEdge(feed, draw));
    }

    [Test]
    public static void UnlinkedPairIsNotBlocked()
    {
        var draw = MakeDrawNode();
        var feed = MakeFeedNode();

        Expect.IsFalse(PowerNetMaker_ContiguousPowerBuildings.IsDiodeLinkEdge(draw, feed));
        Expect.IsFalse(PowerNetMaker_ContiguousPowerBuildings.IsDiodeLinkEdge(feed, draw));
    }

    // A draw node linked to some other feed node must not block an unrelated feed node
    // standing next to it - only the exact linked edge is blocked, nothing else.
    [Test]
    public static void PairLinkedToADifferentPartnerIsNotBlocked()
    {
        var draw = MakeDrawNode();
        var linkedFeed = MakeFeedNode();
        var unrelatedFeed = MakeFeedNode();
        var drawComp = draw.GetComp<CompPowerDiodeDraw>();
        var linkedFeedComp = linkedFeed.GetComp<CompPowerDiodeFeed>();
        drawComp.Partner = linkedFeedComp;
        linkedFeedComp.Partner = drawComp;

        Expect.IsFalse(PowerNetMaker_ContiguousPowerBuildings.IsDiodeLinkEdge(draw, unrelatedFeed));
    }
}
