using DevTools.Testing;
using PowerDiode.Patch;

namespace PowerDiode.Tests;

[TestFixture(TestType.Playing)]
internal sealed class LinkedTransmitterSameNetMapTests
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

    private static bool RestrictToSameNet(IntVec3 c, Thing parent, bool result)
    {
        Graphic_LinkedTransmitter_ShouldLinkWith.RestrictToSameNet(c, parent, ref result);
        return result;
    }

    // A conduit, a linked intake and outlet, and another conduit, in a row from west to east.
    private (Building WestConduit, Building Intake, Building Outlet) SpawnLinkedRow()
    {
        var westConduit = Spawn("PowerConduit", Origin + IntVec3.West);
        var intake = Spawn("PowerDiode_DrawNode", Origin);
        var outlet = Spawn("PowerDiode_FeedNode", Origin + IntVec3.East);
        _ = Spawn("PowerConduit", Origin + (IntVec3.East * 2));
        Map.powerNetManager.UpdatePowerNetsAndConnections_First();
        Expect.ReferencesAreEqual(
            outlet.GetComp<CompPowerDiodeFeed>(),
            intake.GetComp<CompPowerDiodeDraw>().Partner
        );
        return (westConduit, intake, outlet);
    }

    [Test]
    public void ConduitStillLinksToTheIntakeOnItsOwnNet()
    {
        var (westConduit, intake, _) = SpawnLinkedRow();

        Expect.IsTrue(RestrictToSameNet(intake.Position, westConduit, true));
    }

    [Test]
    public void LinkedIntakeDoesNotLinkAcrossToItsOutlet()
    {
        var (_, intake, outlet) = SpawnLinkedRow();

        Expect.IsFalse(RestrictToSameNet(outlet.Position, intake, true));
        Expect.IsFalse(RestrictToSameNet(intake.Position, outlet, true));
    }

    [Test]
    public void UnlinkedNeighboursShareANetAndStillLink()
    {
        var intake = Spawn("PowerDiode_DrawNode", Origin);
        var formerOutlet = Spawn("PowerDiode_FeedNode", Origin + IntVec3.East);
        var outlet = Spawn("PowerDiode_FeedNode", Origin + IntVec3.West);
        _ = formerOutlet.MakeMinified();
        Map.powerNetManager.UpdatePowerNetsAndConnections_First();
        Expect.IsNull(intake.GetComp<CompPowerDiodeDraw>().Partner);
        Expect.IsNull(outlet.GetComp<CompPowerDiodeFeed>().Partner);
        Expect.ReferencesAreEqual(
            Map.powerNetGrid.TransmittedPowerNetAt(intake.Position),
            Map.powerNetGrid.TransmittedPowerNetAt(outlet.Position)
        );

        Expect.IsTrue(RestrictToSameNet(outlet.Position, intake, true));
    }

    [Test]
    public void FalseIsNeverTurnedTrue()
    {
        var (westConduit, intake, _) = SpawnLinkedRow();

        Expect.IsFalse(RestrictToSameNet(intake.Position, westConduit, false));
    }

    // Confirms the Harmony patch is applied: vanilla links to any cell with a power net.
    [Test]
    public void LinkedTransmitterGraphicDoesNotLinkAcrossTheDiode()
    {
        var (westConduit, intake, outlet) = SpawnLinkedRow();
        var graphic = new Graphic_LinkedTransmitter(westConduit.Graphic);

        Expect.IsTrue(graphic.ShouldLinkWith(intake.Position, westConduit));
        Expect.IsFalse(graphic.ShouldLinkWith(outlet.Position, intake));
    }
}
