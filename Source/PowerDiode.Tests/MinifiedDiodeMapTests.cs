using DevTools.Testing;

namespace PowerDiode.Tests;

[TestFixture(TestType.Playing)]
internal sealed class MinifiedDiodeMapTests
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

    // Takes the building out of a minified thing and spawns it, the way installing one does.
    private static Building Install(MinifiedThing minified, IntVec3 cell)
    {
        var building = (Building)minified.InnerThing;
        minified.InnerThing = null;
        return (Building)GenSpawn.Spawn(building, cell, Map);
    }

    private static AcceptanceReport CanInstallAt(
        Thing toInstall,
        IntVec3 cell,
        Thing thingToIgnore
    ) =>
        GenConstruct.CanPlaceBlueprintAt_NewTemp(
            toInstall.def,
            cell,
            Rot4.North,
            Map,
            thingToIgnore: thingToIgnore,
            thing: toInstall
        );

    [Test]
    public static void AllDiodeBuildingsAreMinifiable()
    {
        foreach (
            var defName in new[]
            {
                "PowerDiode_DrawNode",
                "PowerDiode_FeedNode",
                "PowerDiode_WallDrawNode",
                "PowerDiode_WallFeedNode",
            }
        )
        {
            var def = DefDatabase<ThingDef>.GetNamed(defName);
            Expect.IsTrue(def.Minifiable, defName);
            Expect.IsNotNull(def.installBlueprintDef, defName);
        }
    }

    [Test]
    public void UninstallingTheOutletUnpairsBothSides()
    {
        var intake = Spawn("PowerDiode_DrawNode", Origin);
        var outlet = Spawn("PowerDiode_FeedNode", Origin + IntVec3.East);
        var draw = intake.GetComp<CompPowerDiodeDraw>();
        var feed = outlet.GetComp<CompPowerDiodeFeed>();
        Expect.ReferencesAreEqual(feed, draw.Partner);

        _ = outlet.MakeMinified();

        Expect.IsNull(draw.Partner);
        Expect.IsNull(feed.Partner);
    }

    [Test]
    public void UninstallingTheIntakeUnpairsBothSides()
    {
        var intake = Spawn("PowerDiode_DrawNode", Origin);
        var outlet = Spawn("PowerDiode_FeedNode", Origin + IntVec3.East);
        var draw = intake.GetComp<CompPowerDiodeDraw>();
        var feed = outlet.GetComp<CompPowerDiodeFeed>();
        Expect.ReferencesAreEqual(draw, feed.Partner);

        _ = intake.MakeMinified();

        Expect.IsNull(draw.Partner);
        Expect.IsNull(feed.Partner);
    }

    [Test]
    public void MinifiedOutletCanOnlyBeInstalledNextToAnUnpairedIntake()
    {
        _ = Spawn("PowerDiode_DrawNode", Origin);
        _ = Spawn("PowerDiode_DrawNode", Origin + (IntVec3.East * 4));
        _ = Spawn("PowerDiode_FeedNode", Origin + (IntVec3.East * 5));
        var minified = ThingMaker
            .MakeThing(DefDatabase<ThingDef>.GetNamed("PowerDiode_FeedNode"))
            .MakeMinified();
        var outlet = minified.InnerThing;

        Expect.IsTrue(CanInstallAt(outlet, Origin + IntVec3.East, minified).Accepted);
        Expect.IsFalse(CanInstallAt(outlet, Origin + (IntVec3.North * 3), minified).Accepted);
        Expect.IsFalse(
            CanInstallAt(outlet, Origin + (IntVec3.East * 4) + IntVec3.North, minified).Accepted
        );
    }

    [Test]
    public void OutletCanBeReinstalledNextToItsOwnIntake()
    {
        _ = Spawn("PowerDiode_DrawNode", Origin);
        var outlet = Spawn("PowerDiode_FeedNode", Origin + IntVec3.East);

        Expect.IsTrue(CanInstallAt(outlet, Origin + IntVec3.North, outlet).Accepted);
    }

    [Test]
    public void OutletCanNotBeReinstalledNextToAnotherOutletsIntake()
    {
        _ = Spawn("PowerDiode_DrawNode", Origin);
        var outlet = Spawn("PowerDiode_FeedNode", Origin + IntVec3.East);
        var otherIntakeCell = Origin + (IntVec3.East * 4);
        _ = Spawn("PowerDiode_DrawNode", otherIntakeCell);
        _ = Spawn("PowerDiode_FeedNode", otherIntakeCell + IntVec3.East);

        Expect.IsFalse(CanInstallAt(outlet, otherIntakeCell + IntVec3.North, outlet).Accepted);
    }

    [Test]
    public void ReinstalledOutletPairsWithTheAdjacentIntakeAndKeepsItsSettings()
    {
        _ = Spawn("PowerDiode_DrawNode", Origin);
        var outlet = Spawn("PowerDiode_FeedNode", Origin + IntVec3.East);
        var newIntake = Spawn("PowerDiode_DrawNode", Origin + (IntVec3.East * 4));
        var feed = outlet.GetComp<CompPowerDiodeFeed>();
        var settings = PowerDiodeMod.Settings;
        feed.TargetWatts = Mathf.Lerp(settings.MinWattage, settings.MaxWattage, 0.5f);
        feed.ReserveWattDays = Mathf.Lerp(
            settings.MinReserveWattDays,
            settings.MaxReserveWattDays,
            0.5f
        );
        feed.ReservePercent = 42f;
        feed.OverflowThresholdPercent = 55f;
        feed.TopUpThresholdPercent = 33f;
        feed.OperatingMode = PowerDiodeOperatingMode.TopUp;
        var expectedTargetWatts = feed.TargetWatts;
        var expectedReserveWattDays = feed.ReserveWattDays;

        var reinstalled = Install(outlet.MakeMinified(), newIntake.Position + IntVec3.East);

        Expect.ReferencesAreEqual(
            reinstalled.GetComp<CompPowerDiodeFeed>(),
            newIntake.GetComp<CompPowerDiodeDraw>().Partner
        );
        Expect.ReferencesAreEqual(newIntake.GetComp<CompPowerDiodeDraw>(), feed.Partner);
        Expect.AreEqual(expectedTargetWatts, feed.TargetWatts);
        Expect.AreEqual(expectedReserveWattDays, feed.ReserveWattDays);
        Expect.AreEqual(42f, feed.ReservePercent);
        Expect.AreEqual(55f, feed.OverflowThresholdPercent);
        Expect.AreEqual(33f, feed.TopUpThresholdPercent);
        Expect.AreEqual(PowerDiodeOperatingMode.TopUp, feed.OperatingMode);
    }

    [Test]
    public void ReinstalledIntakePairsWithTheAdjacentUnpairedOutlet()
    {
        var intake = Spawn("PowerDiode_DrawNode", Origin);
        var outlet = Spawn("PowerDiode_FeedNode", Origin + IntVec3.East);

        var reinstalled = Install(intake.MakeMinified(), outlet.Position + IntVec3.East);

        var draw = reinstalled.GetComp<CompPowerDiodeDraw>();
        var feed = outlet.GetComp<CompPowerDiodeFeed>();
        Expect.ReferencesAreEqual(feed, draw.Partner);
        Expect.ReferencesAreEqual(draw, feed.Partner);
    }

    private (CompPowerDiodeDraw Draw, CompPowerDiodeFeed Feed) SpawnPair(
        IntVec3 intakeCell,
        IntVec3 outletCell
    )
    {
        var draw = Spawn("PowerDiode_DrawNode", intakeCell).GetComp<CompPowerDiodeDraw>();
        var feed = Spawn("PowerDiode_FeedNode", outletCell).GetComp<CompPowerDiodeFeed>();
        Expect.ReferencesAreEqual(feed, draw.Partner);
        return (draw, feed);
    }

    private static void ExpectPaired(CompPowerDiodeDraw draw, CompPowerDiodeFeed feed)
    {
        Expect.ReferencesAreEqual(feed, draw.Partner);
        Expect.ReferencesAreEqual(draw, feed.Partner);
    }

    // GenAdj.CardinalDirections checks North before South, so this picks which of two
    // neighbours of a cell the linking code reaches first.
    private static (IntVec3 PairedDir, IntVec3 UnpairedDir) NeighbourDirections(
        bool pairedNeighbourCheckedFirst
    ) =>
        pairedNeighbourCheckedFirst
            ? (IntVec3.North, IntVec3.South)
            : (IntVec3.South, IntVec3.North);

    [Test]
    public void IntakeNextToOnlyAPairedOutletStaysUnpaired()
    {
        var (draw, feed) = SpawnPair(Origin, Origin + IntVec3.East);

        var newDraw = Spawn("PowerDiode_DrawNode", Origin + (IntVec3.East * 2))
            .GetComp<CompPowerDiodeDraw>();

        Expect.IsNull(newDraw.Partner);
        ExpectPaired(draw, feed);
    }

    [Test]
    public void IntakeBetweenAPairedAndAnUnpairedOutletPairsWithTheUnpairedOne(
        [Parameters(true, false)] bool pairedNeighbourCheckedFirst
    )
    {
        var (pairedDir, unpairedDir) = NeighbourDirections(pairedNeighbourCheckedFirst);
        var cell = Origin;
        var (pairedDraw, pairedFeed) = SpawnPair(cell + (pairedDir * 2), cell + pairedDir);
        var (formerDraw, unpairedFeed) = SpawnPair(cell + (unpairedDir * 2), cell + unpairedDir);
        _ = formerDraw.parent.MakeMinified();
        Expect.IsNull(unpairedFeed.Partner);

        var newDraw = Spawn("PowerDiode_DrawNode", cell).GetComp<CompPowerDiodeDraw>();

        ExpectPaired(newDraw, unpairedFeed);
        ExpectPaired(pairedDraw, pairedFeed);
    }

    [Test]
    public void OutletBetweenAPairedAndAnUnpairedIntakePairsWithTheUnpairedOne(
        [Parameters(true, false)] bool pairedNeighbourCheckedFirst
    )
    {
        var (pairedDir, unpairedDir) = NeighbourDirections(pairedNeighbourCheckedFirst);
        var cell = Origin;
        var (pairedDraw, pairedFeed) = SpawnPair(cell + pairedDir, cell + (pairedDir * 2));
        var unpairedDraw = Spawn("PowerDiode_DrawNode", cell + unpairedDir)
            .GetComp<CompPowerDiodeDraw>();
        var minified = ThingMaker
            .MakeThing(DefDatabase<ThingDef>.GetNamed("PowerDiode_FeedNode"))
            .MakeMinified();
        Expect.IsTrue(CanInstallAt(minified.InnerThing, cell, minified).Accepted);

        var newFeed = Spawn("PowerDiode_FeedNode", cell).GetComp<CompPowerDiodeFeed>();

        ExpectPaired(unpairedDraw, newFeed);
        ExpectPaired(pairedDraw, pairedFeed);
    }

    [Test]
    public void OutletReinstalledBetweenAPairedAndAnUnpairedIntakePairsWithTheUnpairedOne(
        [Parameters(true, false)] bool pairedNeighbourCheckedFirst
    )
    {
        var (pairedDir, unpairedDir) = NeighbourDirections(pairedNeighbourCheckedFirst);
        var cell = Origin;
        var (pairedDraw, pairedFeed) = SpawnPair(cell + pairedDir, cell + (pairedDir * 2));
        var unpairedDraw = Spawn("PowerDiode_DrawNode", cell + unpairedDir)
            .GetComp<CompPowerDiodeDraw>();
        var farCell = cell + (IntVec3.East * 5);
        var (oldDraw, movedFeed) = SpawnPair(farCell, farCell + IntVec3.East);
        Expect.IsTrue(CanInstallAt(movedFeed.parent, cell, movedFeed.parent).Accepted);

        _ = Install(movedFeed.parent.MakeMinified(), cell);

        Expect.IsNull(oldDraw.Partner);
        ExpectPaired(unpairedDraw, movedFeed);
        ExpectPaired(pairedDraw, pairedFeed);
    }

    [Test]
    public void NewlyBuiltOutletStartsWithDefaultSettings()
    {
        _ = Spawn("PowerDiode_DrawNode", Origin);
        var feed = Spawn("PowerDiode_FeedNode", Origin + IntVec3.East)
            .GetComp<CompPowerDiodeFeed>();

        Expect.AreEqual(PowerDiodeMod.Settings.MaxWattage, feed.TargetWatts);
        Expect.AreEqual(PowerDiodeMod.Settings.MinReserveWattDays, feed.ReserveWattDays);
        Expect.AreEqual(PowerDiodeOperatingMode.OneWayValve, feed.OperatingMode);
    }
}
