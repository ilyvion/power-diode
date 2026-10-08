using DevTools.Testing;

namespace PowerDiode.Tests;

// Regression suite: the slider gizmos are recreated every GUI frame, and vanilla only shows a
// gizmo's tooltip once the same hash has been hovered for a while, so a hash that changed between
// frames meant the tooltips never appeared.
[TestFixture(TestType.MainMenu)]
internal sealed class SliderGizmoHashCodeTests
{
    private static readonly Func<CompPowerDiodeFeed, Gizmo>[] GizmoFactories =
    [
        feed => new Gizmo_SetDiodeWattage(feed),
        feed => new Gizmo_SetDiodeReserve(feed),
        feed => new Gizmo_SetDiodeOverflowThreshold(feed),
        feed => new Gizmo_SetDiodeTopUpThreshold(feed),
    ];

    // Built without PostMake/ID, as in CompPowerDiodeFeedSettingsTests, with the ID set directly.
    private static CompPowerDiodeFeed MakeFeedComp(int thingIDNumber)
    {
        var def = DefDatabase<ThingDef>.GetNamed("PowerDiode_FeedNode");
        var building = (Building)Activator.CreateInstance(def.thingClass);
        building.def = def;
        building.thingIDNumber = thingIDNumber;
        building.InitializeComps();
        return building.GetComp<CompPowerDiodeFeed>();
    }

    [Test]
    public static void SameGizmoOnSameFeedHasSameHash([Parameters(0, 1, 2, 3)] int gizmoIndex)
    {
        var feed = MakeFeedComp(1001);
        var makeGizmo = GizmoFactories[gizmoIndex];

        Expect.AreEqual(makeGizmo(feed).GetHashCode(), makeGizmo(feed).GetHashCode());
    }

    [Test]
    public static void SameGizmoOnDifferentFeedsHasDifferentHashes(
        [Parameters(0, 1, 2, 3)] int gizmoIndex
    )
    {
        var makeGizmo = GizmoFactories[gizmoIndex];

        Expect.AreNotEqual(
            makeGizmo(MakeFeedComp(1001)).GetHashCode(),
            makeGizmo(MakeFeedComp(1002)).GetHashCode()
        );
    }

    [Test]
    public static void DifferentGizmosOnSameFeedHaveDifferentHashes()
    {
        var feed = MakeFeedComp(1001);
        var hashes = GizmoFactories.Select(makeGizmo => makeGizmo(feed).GetHashCode()).ToList();

        Expect.AreEqual(hashes.Count, hashes.Distinct().Count());
    }
}
