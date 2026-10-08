using DevTools.Testing;

namespace PowerDiode.Tests;

[TestFixture(TestType.Playing)]
internal sealed class DiodeLinkPreviewMapTests
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

    private Building Spawn(string defName, IntVec3 cell) => (Building)SpawnThing(defName, cell);

    private Thing SpawnThing(string defName, IntVec3 cell)
    {
        var thing = GenSpawn.Spawn(
            ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed(defName)),
            cell,
            Map
        );
        spawned.Add(thing);
        return thing;
    }

    private Building SpawnPairedIntake(IntVec3 intakeCell, IntVec3 outletCell)
    {
        var intake = Spawn("PowerDiode_DrawNode", intakeCell);
        _ = Spawn("PowerDiode_FeedNode", outletCell);
        Expect.IsNotNull(intake.GetComp<CompPowerDiodeDraw>().Partner);
        return intake;
    }

    private Building SpawnPairedOutlet(IntVec3 outletCell, IntVec3 intakeCell)
    {
        _ = Spawn("PowerDiode_DrawNode", intakeCell);
        var outlet = Spawn("PowerDiode_FeedNode", outletCell);
        Expect.IsNotNull(outlet.GetComp<CompPowerDiodeFeed>().Partner);
        return outlet;
    }

    private static Thing? PartnerFor(string defName, IntVec3 cell, Thing? thing = null) =>
        PlaceWorker_ShowDiodeLink.LinkTargetFor(
            DefDatabase<ThingDef>.GetNamed(defName),
            cell,
            Map,
            thing
        );

    [Test]
    public static void AllDiodeBuildingsShowTheirLinkWhilePlacedOrInstalled()
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
            Expect.IsTrue(
                def.PlaceWorkers.Any(worker => worker is PlaceWorker_ShowDiodeLink),
                defName
            );
            Expect.IsTrue(def.drawPlaceWorkersWhileInstallBlueprintSelected, defName);
            foreach (var generatedDef in new[] { def.blueprintDef, def.frameDef })
            {
                Expect.IsTrue(generatedDef.drawPlaceWorkersWhileSelected, generatedDef.defName);
                Expect.IsTrue(
                    generatedDef.PlaceWorkers.Any(worker => worker is PlaceWorker_ShowDiodeLink),
                    generatedDef.defName
                );
            }
        }
    }

    [Test]
    public void SelectedBuildBlueprintOrFrameLinksToTheAdjacentUnpairedIntake()
    {
        var intake = Spawn("PowerDiode_DrawNode", Origin);
        var outletDef = DefDatabase<ThingDef>.GetNamed("PowerDiode_FeedNode");
        var cell = Origin + IntVec3.East;

        foreach (var generatedDef in new[] { outletDef.blueprintDef, outletDef.frameDef })
        {
            var blueprintOrFrame = SpawnThing(generatedDef.defName, cell);

            Expect.ReferencesAreEqual(
                intake,
                PlaceWorker_ShowDiodeLink.LinkTargetFor(generatedDef, cell, Map, blueprintOrFrame),
                generatedDef.defName
            );

            blueprintOrFrame.Destroy();
        }
    }

    [Test]
    public void SelectedBuiltDiodeShowsNoLink()
    {
        var intake = Spawn("PowerDiode_DrawNode", Origin);
        var outlet = Spawn("PowerDiode_FeedNode", Origin + IntVec3.East);
        Expect.IsNotNull(outlet.GetComp<CompPowerDiodeFeed>().Partner);

        Expect.IsNull(PartnerFor("PowerDiode_FeedNode", outlet.Position, outlet));
        Expect.IsNull(PartnerFor("PowerDiode_DrawNode", intake.Position, intake));
    }

    // GenAdj.CardinalDirections checks North before South, so the paired intake is reached
    // first in one arrangement and last in the other.
    [Test]
    public void OutletPreviewLinksToTheAdjacentUnpairedIntake(
        [Parameters("PowerDiode_FeedNode", "PowerDiode_WallFeedNode")] string outletDefName
    )
    {
        foreach (var pairedDir in new[] { IntVec3.North, IntVec3.South })
        {
            var unpairedDir = pairedDir == IntVec3.North ? IntVec3.South : IntVec3.North;
            var cell = Origin + (IntVec3.East * (pairedDir == IntVec3.North ? 0 : 5));
            _ = SpawnPairedIntake(cell + pairedDir, cell + (pairedDir * 2));
            var unpairedIntake = Spawn("PowerDiode_DrawNode", cell + unpairedDir);

            Expect.ReferencesAreEqual(unpairedIntake, PartnerFor(outletDefName, cell));
        }
    }

    [Test]
    public void IntakePreviewLinksToTheAdjacentUnpairedOutlet(
        [Parameters("PowerDiode_DrawNode", "PowerDiode_WallDrawNode")] string intakeDefName
    )
    {
        foreach (var pairedDir in new[] { IntVec3.North, IntVec3.South })
        {
            var unpairedDir = pairedDir == IntVec3.North ? IntVec3.South : IntVec3.North;
            var cell = Origin + (IntVec3.East * (pairedDir == IntVec3.North ? 0 : 5));
            _ = SpawnPairedOutlet(cell + pairedDir, cell + (pairedDir * 2));
            var formerIntake = Spawn("PowerDiode_DrawNode", cell + (unpairedDir * 2));
            var unpairedOutlet = Spawn("PowerDiode_FeedNode", cell + unpairedDir);
            _ = formerIntake.MakeMinified();

            Expect.ReferencesAreEqual(unpairedOutlet, PartnerFor(intakeDefName, cell));
        }
    }

    [Test]
    public void PreviewShowsNoLinkNextToOnlyPairedBuildings()
    {
        _ = SpawnPairedIntake(Origin + IntVec3.North, Origin + (IntVec3.North * 2));
        _ = SpawnPairedOutlet(Origin + IntVec3.South, Origin + (IntVec3.South * 2));

        Expect.IsNull(PartnerFor("PowerDiode_FeedNode", Origin));
        Expect.IsNull(PartnerFor("PowerDiode_DrawNode", Origin));
    }

    [Test]
    public void ReinstallPreviewLinksBackToTheMovedBuildingsOwnPartner()
    {
        var intake = Spawn("PowerDiode_DrawNode", Origin);
        var outlet = Spawn("PowerDiode_FeedNode", Origin + IntVec3.East);

        Expect.ReferencesAreEqual(
            intake,
            PartnerFor("PowerDiode_FeedNode", Origin + IntVec3.North, outlet)
        );
        Expect.ReferencesAreEqual(
            outlet,
            PartnerFor("PowerDiode_DrawNode", outlet.Position + IntVec3.East, intake)
        );
    }

    [Test]
    public void DrawingTheGhostNextToAPartnerSucceeds()
    {
        var intake = Spawn("PowerDiode_DrawNode", Origin);
        var outletDef = DefDatabase<ThingDef>.GetNamed("PowerDiode_FeedNode");

        new PlaceWorker_ShowDiodeLink().DrawGhost(
            outletDef,
            intake.Position + IntVec3.East,
            Rot4.North,
            Color.white
        );
    }
}
