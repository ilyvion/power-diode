using DevTools.Testing;

namespace PowerDiode.Tests;

[TestFixture(TestType.MainMenu)]
internal sealed class CompPowerDiodeFeedDragStateTests
{
    // ThingMaker.MakeThing's PostMake calls ThingIDMaker.GiveIDTo, which needs
    // Find.UniqueIDsManager - unavailable outside a loaded game/map, and these tests run at
    // the main menu. These tests only touch comp state, so constructing the ThingWithComps
    // directly and initializing its comps - skipping PostMake entirely - is enough.
    private static CompPowerDiodeFeed MakeFeedComp()
    {
        var def = DefDatabase<ThingDef>.GetNamed("PowerDiode_FeedNode");
        var building = (Building)Activator.CreateInstance(def.thingClass);
        building.def = def;
        building.InitializeComps();
        return building.GetComp<CompPowerDiodeFeed>();
    }

    // Regression guard: Gizmo_SetDiodeReserve/Gizmo_SetDiodeWattage read/write their
    // DraggingBar state through these fields on the feed rather than through a static field on
    // the gizmo class. A static field is shared by every instance of that gizmo class drawn in
    // a frame - since the gizmos are recreated every frame, multi-selecting two diode outlets
    // would make one outlet's slider drag visually bleed into the other's.
    [Test]
    public static void DragStateIsIndependentPerBuilding()
    {
        var feedA = MakeFeedComp();
        var feedB = MakeFeedComp();

        feedA.draggingReserveBar = true;
        Expect.IsFalse(feedB.draggingReserveBar);

        feedA.draggingWattageBar = true;
        Expect.IsFalse(feedB.draggingWattageBar);

        feedA.draggingOverflowBar = true;
        Expect.IsFalse(feedB.draggingOverflowBar);

        feedA.draggingTopUpBar = true;
        Expect.IsFalse(feedB.draggingTopUpBar);
    }
}
