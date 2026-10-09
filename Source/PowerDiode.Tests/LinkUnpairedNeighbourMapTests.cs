using DevTools.Testing;

namespace PowerDiode.Tests;

[TestFixture(TestType.Playing)]
internal sealed class LinkUnpairedNeighbourMapTests
{
    private readonly List<Thing> spawned = [];

    private static Map Map => Find.CurrentMap;

    private static IntVec3 Origin => Map.Center;

    [TearDown]
    public void DestroySpawned()
    {
        foreach (var thing in spawned)
        {
            if (!thing.Destroyed)
            {
                thing.Destroy();
            }
        }
        spawned.Clear();
        _ = Find.WindowStack.TryRemove(typeof(FloatMenu), doCloseSound: false);
    }

    private Building Spawn(string defName, IntVec3 cell)
    {
        var thing = (Building)
            GenSpawn.Spawn(
                ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed(defName)),
                cell,
                Map
            );
        spawned.Add(thing);
        return thing;
    }

    private static Command_Action? LinkGizmo(ThingComp comp, string labelKey) =>
        comp.CompGetGizmosExtra()
            .OfType<Command_Action>()
            .FirstOrDefault(gizmo => gizmo.defaultLabel == labelKey.Translate());

    private static Command_Action? IntakeLinkGizmo(CompPowerDiodeDraw draw) =>
        LinkGizmo(draw, "PowerDiode.LinkToOutlet");

    private static Command_Action? OutletLinkGizmo(CompPowerDiodeFeed feed) =>
        LinkGizmo(feed, "PowerDiode.LinkToIntake");

    private static void ExpectPaired(CompPowerDiodeDraw draw, CompPowerDiodeFeed feed)
    {
        Expect.ReferencesAreEqual(feed, draw.Partner);
        Expect.ReferencesAreEqual(draw, feed.Partner);
    }

    // An intake and an outlet left side by side, both unpaired: the intake paired with another
    // outlet when it was built, and that outlet has since been uninstalled.
    private (CompPowerDiodeDraw Draw, CompPowerDiodeFeed Feed) SpawnStrandedNeighbours() =>
        SpawnStrandedNeighbours(Origin);

    private (CompPowerDiodeDraw Draw, CompPowerDiodeFeed Feed) SpawnStrandedNeighbours(
        IntVec3 origin
    )
    {
        var draw = Spawn("PowerDiode_DrawNode", origin).GetComp<CompPowerDiodeDraw>();
        var formerFeed = Spawn("PowerDiode_FeedNode", origin + IntVec3.East)
            .GetComp<CompPowerDiodeFeed>();
        ExpectPaired(draw, formerFeed);
        var feed = Spawn("PowerDiode_FeedNode", origin + IntVec3.West)
            .GetComp<CompPowerDiodeFeed>();
        Expect.IsNull(feed.Partner);
        _ = formerFeed.parent.MakeMinified();
        Expect.IsNull(draw.Partner);
        return (draw, feed);
    }

    [Test]
    public void StrandedNeighboursBothOfferToLink()
    {
        var (draw, feed) = SpawnStrandedNeighbours();

        Expect.IsNotNull(IntakeLinkGizmo(draw));
        Expect.IsNotNull(OutletLinkGizmo(feed));
    }

    [Test]
    public void IntakeLinkGizmoPairsTheNeighbours()
    {
        var (draw, feed) = SpawnStrandedNeighbours();

        IntakeLinkGizmo(draw)!.action();

        ExpectPaired(draw, feed);
        Expect.IsNull(IntakeLinkGizmo(draw));
        Expect.IsNull(OutletLinkGizmo(feed));
    }

    [Test]
    public void OutletLinkGizmoPairsTheNeighbours()
    {
        var (draw, feed) = SpawnStrandedNeighbours();

        OutletLinkGizmo(feed)!.action();

        ExpectPaired(draw, feed);
    }

    [Test]
    public void LinkingSplitsTheNeighboursIntoSeparatePowerNets()
    {
        var (draw, feed) = SpawnStrandedNeighbours();
        Map.powerNetManager.UpdatePowerNetsAndConnections_First();
        Expect.ReferencesAreEqual(draw.PowerTrader.PowerNet, feed.PowerTrader.PowerNet);

        IntakeLinkGizmo(draw)!.action();
        Map.powerNetManager.UpdatePowerNetsAndConnections_First();

        Expect.IsNotNull(draw.PowerTrader.PowerNet);
        Expect.IsNotNull(feed.PowerTrader.PowerNet);
        Expect.ReferencesAreNotEqual(draw.PowerTrader.PowerNet, feed.PowerTrader.PowerNet);
    }

    [Test]
    public void PairedDiodesDoNotOfferToLink()
    {
        var draw = Spawn("PowerDiode_DrawNode", Origin).GetComp<CompPowerDiodeDraw>();
        var feed = Spawn("PowerDiode_FeedNode", Origin + IntVec3.East)
            .GetComp<CompPowerDiodeFeed>();
        ExpectPaired(draw, feed);

        Expect.IsNull(IntakeLinkGizmo(draw));
        Expect.IsNull(OutletLinkGizmo(feed));
    }

    [Test]
    public void UnpairedIntakeNextToOnlyPairedOutletsDoesNotOfferToLink()
    {
        _ = Spawn("PowerDiode_DrawNode", Origin + IntVec3.East);
        var pairedFeed = Spawn("PowerDiode_FeedNode", Origin + (IntVec3.East * 2))
            .GetComp<CompPowerDiodeFeed>();
        Expect.IsNotNull(pairedFeed.Partner);
        var draw = Spawn("PowerDiode_DrawNode", Origin + (IntVec3.East * 3))
            .GetComp<CompPowerDiodeDraw>();
        Expect.IsNull(draw.Partner);

        Expect.IsNull(IntakeLinkGizmo(draw));
    }

    [Test]
    public void IntakeBetweenTwoUnpairedOutletsCanLinkToEither([Parameters(0, 1)] int chosenIndex)
    {
        var (draw, westFeed) = SpawnStrandedNeighbours();
        var eastFeed = Spawn("PowerDiode_FeedNode", Origin + IntVec3.East)
            .GetComp<CompPowerDiodeFeed>();
        // Spawning pairs the new outlet with the unpaired intake, so unpair it again.
        eastFeed.Unlink();
        var candidates = PowerDiodeLinking.UnpairedAdjacentFeedNodes(draw);
        Expect.AreEqual(2, candidates.Count);
        Expect.IsTrue(candidates.Contains(westFeed));
        Expect.IsTrue(candidates.Contains(eastFeed));

        var chosen = candidates[chosenIndex];
        PowerDiodeLinking.LinkSpawned(draw, chosen);

        ExpectPaired(draw, chosen);
        Expect.IsNull(candidates[1 - chosenIndex].Partner);
    }

    private static string OptionLabel(CompPowerDiodeFeed feed, Rot4 direction) =>
        "PowerDiode.LinkTargetOption".Translate(feed.parent.LabelCap, direction.ToStringHuman());

    [Test]
    public void IntakeLinkGizmoMenuLinksTheChosenOutlet([Parameters(true, false)] bool chooseWest)
    {
        var (draw, westFeed) = SpawnStrandedNeighbours();
        var eastFeed = Spawn("PowerDiode_FeedNode", Origin + IntVec3.East)
            .GetComp<CompPowerDiodeFeed>();
        // Spawning pairs the new outlet with the unpaired intake, so unpair it again.
        eastFeed.Unlink();

        IntakeLinkGizmo(draw)!.action();

        var menu = Find.WindowStack.WindowOfType<FloatMenu>();
        Expect.IsNotNull(menu);
        if (menu == null)
        {
            return;
        }
        Expect.AreEqual(2, menu.options.Count);
        var westOption = menu.options.FirstOrDefault(option =>
            option.Label == OptionLabel(westFeed, Rot4.West)
        );
        var eastOption = menu.options.FirstOrDefault(option =>
            option.Label == OptionLabel(eastFeed, Rot4.East)
        );
        Expect.IsNotNull(westOption, "west option");
        Expect.IsNotNull(eastOption, "east option");
        if (westOption == null || eastOption == null)
        {
            return;
        }

        var (chosenOption, chosen, other) = chooseWest
            ? (westOption, westFeed, eastFeed)
            : (eastOption, eastFeed, westFeed);
        chosenOption.action();

        ExpectPaired(draw, chosen);
        Expect.IsNull(other.Partner);
    }

    // Each selected building has its own candidates, so their link gizmos must stay separate
    // rather than merge into one button whose menu only links one of them.
    [Test]
    public void LinkGizmosOfSeveralSelectedDiodesDoNotGroup()
    {
        var (draw, feed) = SpawnStrandedNeighbours();
        var (otherDraw, otherFeed) = SpawnStrandedNeighbours(Origin + (IntVec3.North * 5));

        Expect.IsFalse(IntakeLinkGizmo(draw)!.GroupsWith(IntakeLinkGizmo(otherDraw)!), "intakes");
        Expect.IsFalse(OutletLinkGizmo(feed)!.GroupsWith(OutletLinkGizmo(otherFeed)!), "outlets");
    }
}
