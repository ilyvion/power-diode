using DevTools.Testing;

namespace PowerDiode.Tests;

[TestFixture(TestType.Playing)]
internal sealed class WallMountedDiodeMapTests
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
    }

    private T Spawn<T>(ThingDef def, IntVec3 cell, ThingDef? stuff = null)
        where T : Thing
    {
        var thing = (T)GenSpawn.Spawn(ThingMaker.MakeThing(def, stuff), cell, Map);
        spawned.Add(thing);
        return thing;
    }

    private Building SpawnWall(IntVec3 cell) =>
        Spawn<Building>(ThingDefOf.Wall, cell, ThingDefOf.Steel);

    private void SpawnWalls(params IntVec3[] cells)
    {
        foreach (var cell in cells)
        {
            _ = SpawnWall(cell);
        }
    }

    private static bool CanPlaceAt(string defName, IntVec3 cell) =>
        GenConstruct
            .CanPlaceBlueprintAt_NewTemp(
                DefDatabase<ThingDef>.GetNamed(defName),
                cell,
                Rot4.North,
                Map
            )
            .Accepted;

    [Test]
    public void WallIntakeCanOnlyBePlacedInAWall()
    {
        var wallCell = Origin;
        var openCell = Origin + IntVec3.East;
        SpawnWalls(wallCell);

        Expect.IsTrue(CanPlaceAt("PowerDiode_WallDrawNode", wallCell));
        Expect.IsFalse(CanPlaceAt("PowerDiode_WallDrawNode", openCell));
    }

    [Test]
    public void WallIntakeCanBePlacedInAWallWithAConduitUnderIt()
    {
        var cell = Origin;
        SpawnWalls(cell);
        _ = Spawn<Building>(ThingDefOf.PowerConduit, cell);

        Expect.IsTrue(CanPlaceAt("PowerDiode_WallDrawNode", cell));
    }

    [Test]
    public void WallOutletStillNeedsAnAdjacentUnpairedIntake()
    {
        var intakeCell = Origin;
        var outletCell = Origin + IntVec3.East;
        var farCell = Origin + (IntVec3.East * 3);
        SpawnWalls(intakeCell, outletCell, farCell);
        _ = Spawn<Building>(DefDatabase<ThingDef>.GetNamed("PowerDiode_WallDrawNode"), intakeCell);

        Expect.IsTrue(CanPlaceAt("PowerDiode_WallFeedNode", outletCell));
        Expect.IsFalse(CanPlaceAt("PowerDiode_WallFeedNode", farCell));
    }

    [Test]
    public void SpawningInAWallKeepsTheWallAndReplacesTheConduit()
    {
        var cell = Origin;
        var wall = SpawnWall(cell);
        var conduit = Spawn<Building>(ThingDefOf.PowerConduit, cell);

        var intake = Spawn<Building>(
            DefDatabase<ThingDef>.GetNamed("PowerDiode_WallDrawNode"),
            cell
        );

        Expect.IsTrue(wall.Spawned);
        Expect.ReferencesAreEqual<Building>(wall, cell.GetEdifice(Map));
        Expect.IsTrue(conduit.Destroyed);
        Expect.IsTrue(intake.Spawned);
    }

    [Test]
    public void WallIntakePairsWithEitherOutletAndKeepsTheirNetsSeparate(
        [Parameters("PowerDiode_FeedNode", "PowerDiode_WallFeedNode")] string outletDefName
    )
    {
        var intakeCell = Origin;
        var outletCell = Origin + IntVec3.East;
        var outletDef = DefDatabase<ThingDef>.GetNamed(outletDefName);
        SpawnWalls(intakeCell);
        if (!outletDef.IsEdifice())
        {
            SpawnWalls(outletCell);
        }
        var intake = Spawn<Building>(
            DefDatabase<ThingDef>.GetNamed("PowerDiode_WallDrawNode"),
            intakeCell
        );
        var outlet = Spawn<Building>(outletDef, outletCell);
        Map.powerNetManager.UpdatePowerNetsAndConnections_First();

        var draw = intake.GetComp<CompPowerDiodeDraw>();
        var feed = outlet.GetComp<CompPowerDiodeFeed>();
        Expect.ReferencesAreEqual(feed, draw.Partner);
        Expect.ReferencesAreEqual(draw, feed.Partner);
        Expect.IsNotNull(intake.PowerComp.PowerNet);
        Expect.IsNotNull(outlet.PowerComp.PowerNet);
        Expect.ReferencesAreNotEqual(intake.PowerComp.PowerNet, outlet.PowerComp.PowerNet);
    }
}
