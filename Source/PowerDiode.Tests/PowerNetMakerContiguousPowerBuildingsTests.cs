using PowerDiode.Patch;
using RimTestRedux;

namespace PowerDiode.Tests;

[TestSuite]
internal static class PowerNetMakerContiguousPowerBuildingsTests
{
    private static Building MakeDrawNode() => MakeBuilding("PowerDiode_DrawNode");

    private static Building MakeFeedNode() => MakeBuilding("PowerDiode_FeedNode");

    // ThingMaker.MakeThing's PostMake calls ThingIDMaker.GiveIDTo, which needs
    // Find.UniqueIDsManager - unavailable outside a loaded game/map, and RimTest Redux runs at
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

        Assert.That(PowerNetMaker_ContiguousPowerBuildings.IsDiodeLinkEdge(draw, feed)).Is.True();
        Assert.That(PowerNetMaker_ContiguousPowerBuildings.IsDiodeLinkEdge(feed, draw)).Is.True();
    }

    [Test]
    public static void UnlinkedPairIsNotBlocked()
    {
        var draw = MakeDrawNode();
        var feed = MakeFeedNode();

        Assert.That(PowerNetMaker_ContiguousPowerBuildings.IsDiodeLinkEdge(draw, feed)).Is.False();
        Assert.That(PowerNetMaker_ContiguousPowerBuildings.IsDiodeLinkEdge(feed, draw)).Is.False();
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

        Assert
            .That(PowerNetMaker_ContiguousPowerBuildings.IsDiodeLinkEdge(draw, unrelatedFeed))
            .Is.False();
    }
}
