using DevTools.Testing;

namespace PowerDiode.Tests;

[TestFixture(TestType.MainMenu)]
internal sealed class WallMountedDiodeDefTests
{
    private static ThingDef WallDrawNode =>
        DefDatabase<ThingDef>.GetNamed("PowerDiode_WallDrawNode");

    private static ThingDef WallFeedNode =>
        DefDatabase<ThingDef>.GetNamed("PowerDiode_WallFeedNode");

    [Test]
    public static void WallVariantsAreNonEdificeTransmitters()
    {
        foreach (var def in new[] { WallDrawNode, WallFeedNode })
        {
            Expect.IsFalse(def.IsEdifice(), def.defName);
            Expect.IsTrue(def.EverTransmitsPower, def.defName);
        }
    }

    [Test]
    public static void WallVariantsHaveTheDiodeComps()
    {
        Expect.IsNotNull(WallDrawNode.GetCompProperties<CompProperties_PowerDiodeDraw>());
        Expect.IsNotNull(WallFeedNode.GetCompProperties<CompProperties_PowerDiodeFeed>());
    }

    [Test]
    public static void WallOutletKeepsThePairingPlaceWorker()
    {
        var placeWorkers = WallFeedNode.PlaceWorkers;
        Expect.IsTrue(placeWorkers.Any(worker => worker is PlaceWorker_PowerDiodeFeed));
        Expect.IsTrue(placeWorkers.Any(worker => worker is PlaceWorker_InWall));
    }

    [Test]
    public static void WallVariantsShareADropdownWithTheirFloorVariant()
    {
        Expect.ReferencesAreEqual(
            DefDatabase<ThingDef>.GetNamed("PowerDiode_DrawNode").designatorDropdown,
            WallDrawNode.designatorDropdown
        );
        Expect.ReferencesAreEqual(
            DefDatabase<ThingDef>.GetNamed("PowerDiode_FeedNode").designatorDropdown,
            WallFeedNode.designatorDropdown
        );
        Expect.ReferencesAreNotEqual(
            WallDrawNode.designatorDropdown,
            WallFeedNode.designatorDropdown
        );
    }

    [Test]
    public static void InWallPlaceWorkerAllowsPlacingOverWallsAndConduitsOnly()
    {
        var placeWorker = new PlaceWorker_InWall();

        Expect.IsTrue(placeWorker.ForceAllowPlaceOver(ThingDefOf.Wall));
        Expect.IsTrue(placeWorker.ForceAllowPlaceOver(ThingDefOf.PowerConduit));
        Expect.IsFalse(placeWorker.ForceAllowPlaceOver(ThingDefOf.Battery));
    }

    [Test]
    public static void WallVariantsCanBePlacedOverWallsAndConduitsButNotOtherTransmitters()
    {
        foreach (var def in new[] { WallDrawNode, WallFeedNode })
        {
            Expect.IsTrue(GenConstruct.CanPlaceBlueprintOver(def, ThingDefOf.Wall), def.defName);
            Expect.IsTrue(
                GenConstruct.CanPlaceBlueprintOver(def, ThingDefOf.PowerConduit),
                def.defName
            );
            Expect.IsFalse(
                GenConstruct.CanPlaceBlueprintOver(def, ThingDefOf.Battery),
                def.defName
            );
        }
    }
}
