using DevTools.Testing;

namespace PowerDiode.Tests;

[TestFixture(TestType.Playing)]
internal sealed class MapEdgeLinkingMapTests
{
    private readonly List<Thing> spawned = [];

    private static Map Map => Find.CurrentMap;

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
    }

    private T Spawn<T>(string defName, IntVec3 cell)
        where T : ThingComp
    {
        var thing = (ThingWithComps)
            GenSpawn.Spawn(
                ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed(defName)),
                cell,
                Map
            );
        spawned.Add(thing);
        return thing.GetComp<T>();
    }

    // Prefers open, unroofed cells, so spawning doesn't wipe rock holding up a roof.
    private static bool IsClear(IntVec3 cell) => cell.Standable(Map) && !cell.Roofed(Map);

    // The neighbour of an edge cell along the x axis, which is always in bounds.
    private static IntVec3 InwardNeighbour(IntVec3 edgeCell) =>
        edgeCell + (edgeCell.x == 0 ? IntVec3.East : IntVec3.West);

    private static IntVec3 WestEdgeCell()
    {
        foreach (var z in Enumerable.Range(0, Map.Size.z))
        {
            var cell = new IntVec3(0, 0, z);
            if (IsClear(cell) && IsClear(InwardNeighbour(cell)))
            {
                return cell;
            }
        }
        return new IntVec3(0, 0, Map.Center.z);
    }

    private static IntVec3 Corner()
    {
        IntVec3[] corners =
        [
            new(0, 0, 0),
            new(0, 0, Map.Size.z - 1),
            new(Map.Size.x - 1, 0, 0),
            new(Map.Size.x - 1, 0, Map.Size.z - 1),
        ];
        return corners.FirstOrDefault(cell => IsClear(cell) && IsClear(InwardNeighbour(cell)));
    }

    // The intake spawns last, so it's the one searching past the map edge for a partner.
    [Test]
    public void IntakeOnTheMapEdgePairsWithItsNeighbour()
    {
        var intakeCell = WestEdgeCell();
        var feed = Spawn<CompPowerDiodeFeed>("PowerDiode_FeedNode", InwardNeighbour(intakeCell));
        Expect.IsNull(feed.Partner);

        var draw = Spawn<CompPowerDiodeDraw>("PowerDiode_DrawNode", intakeCell);

        Expect.ReferencesAreEqual(feed, draw.Partner);
        Expect.ReferencesAreEqual(draw, feed.Partner);
    }

    [Test]
    public void CornerIntakeListsItsOnlyUnpairedNeighbour()
    {
        var intakeCell = Corner();
        var draw = Spawn<CompPowerDiodeDraw>("PowerDiode_DrawNode", intakeCell);
        Expect.IsEmpty(PowerDiodeLinking.UnpairedAdjacentFeedNodes(draw));
        var feed = Spawn<CompPowerDiodeFeed>("PowerDiode_FeedNode", InwardNeighbour(intakeCell));
        // Spawning pairs the outlet with the unpaired intake, so unpair it again.
        feed.Unlink();

        var candidates = PowerDiodeLinking.UnpairedAdjacentFeedNodes(draw);

        Expect.AreEqual(1, candidates.Count);
        Expect.IsTrue(candidates.Contains(feed));
    }
}
