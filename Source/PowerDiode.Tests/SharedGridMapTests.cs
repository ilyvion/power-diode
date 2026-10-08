using DevTools.Testing;

namespace PowerDiode.Tests;

[TestFixture(TestType.Playing)]
internal sealed class SharedGridMapTests
{
    private const int TickTimeout = 2500;
    private const int TicksPerFrame = 60;

    private readonly List<Thing> spawned = [];
    private TimeSpeed previousTimeSpeed;

    private static Map Map => Find.CurrentMap;

    private static IntVec3 Origin => Map.Center;

    [SetUp]
    public void PauseTime()
    {
        previousTimeSpeed = Find.TickManager.CurTimeSpeed;
        Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
    }

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
        Find.TickManager.CurTimeSpeed = previousTimeSpeed;
    }

    private T Spawn<T>(string defName, IntVec3 cell)
        where T : Thing
    {
        var thing = (T)
            GenSpawn.Spawn(
                ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed(defName)),
                cell,
                Map
            );
        spawned.Add(thing);
        return thing;
    }

    // A fully charged battery on the intake's network; the outlet's network is empty.
    private CompPowerDiodeFeed SpawnDiode()
    {
        var battery = Spawn<Building>("Battery", Origin + IntVec3.West).GetComp<CompPowerBattery>();
        battery.SetStoredEnergyPct(1f);
        _ = Spawn<Building>("PowerDiode_DrawNode", Origin);
        return Spawn<Building>("PowerDiode_FeedNode", Origin + IntVec3.East)
            .GetComp<CompPowerDiodeFeed>();
    }

    // Conduits north of the intake and the outlet, next to the battery's upper cell, joining the
    // intake's and the outlet's networks. Returns the conduit north of the intake.
    private Building SpawnConduitsJoiningBothSides()
    {
        var middle = Spawn<Building>("PowerConduit", Origin + IntVec3.North);
        _ = Spawn<Building>("PowerConduit", Origin + IntVec3.East + IntVec3.North);
        return middle;
    }

    private static IEnumerable TickUntil(Func<bool> done, int timeoutTicks)
    {
        for (var ticks = 0; !done() && ticks < timeoutTicks; ticks += TicksPerFrame)
        {
            for (var i = 0; i < TicksPerFrame; i++)
            {
                Find.TickManager.DoSingleTick();
            }
            yield return null;
        }
    }

    [Test]
    public IEnumerator ConnectionJoiningBothSidesDisablesTheDiodeUntilItIsRemoved()
    {
        var feed = SpawnDiode();
        var lampPower = Spawn<Building>("StandingLamp", Origin + (IntVec3.East * 7))
            .GetComp<CompPowerTrader>();
        lampPower.PowerOn = false;
        Map.powerNetManager.UpdatePowerNetsAndConnections_First();

        Expect.IsNotNull(feed.Partner);
        var draw = feed.Partner!;

        foreach (var frame in TickUntil(() => lampPower.PowerOn, TickTimeout))
        {
            yield return frame;
        }
        Expect.IsTrue(lampPower.PowerOn, "lamp powered");
        Expect.GreaterThan(feed.CurrentFlowWatts, 0f, "flowing before the sides are joined");

        var middleConduit = SpawnConduitsJoiningBothSides();
        Map.powerNetManager.UpdatePowerNetsAndConnections_First();
        Expect.ReferencesAreEqual(feed.PowerTrader.PowerNet, draw.PowerTrader.PowerNet);
        Find.TickManager.DoSingleTick();

        Expect.IsTrue(feed.IsSharedGridDegenerate, "shared grid detected");
        Expect.AreEqual(0f, feed.CurrentFlowWatts);
        Expect.AreEqual(0f, feed.PowerTrader.PowerOutput);
        Expect.AreEqual(0f, draw.PowerTrader.PowerOutput);
        var sharedGridOnFeed = "PowerDiode.SharedGrid".Translate(draw.parent.LabelCap).ToString();
        var sharedGridOnDraw = "PowerDiode.SharedGrid".Translate(feed.parent.LabelCap).ToString();
        Expect.AreEqual(sharedGridOnFeed, feed.CompInspectStringExtra());
        Expect.AreEqual(sharedGridOnDraw, draw.CompInspectStringExtra());

        middleConduit.Destroy();
        Map.powerNetManager.UpdatePowerNetsAndConnections_First();
        Expect.ReferencesAreNotEqual(feed.PowerTrader.PowerNet, draw.PowerTrader.PowerNet);
        Find.TickManager.DoSingleTick();

        Expect.IsFalse(feed.IsSharedGridDegenerate, "shared grid cleared");
    }

    [Test]
    public void PairedDiodeWithNothingToFeedShowsIdleOnBothSides()
    {
        var feed = SpawnDiode();
        Map.powerNetManager.UpdatePowerNetsAndConnections_First();

        Expect.IsNotNull(feed.Partner);
        var draw = feed.Partner!;
        Find.TickManager.DoSingleTick();

        Expect.AreEqual(0f, feed.CurrentFlowWatts);
        Expect.AreEqual(
            "PowerDiode.LinkedIdle".Translate(draw.parent.LabelCap).ToString(),
            feed.CompInspectStringExtra()
        );
        Expect.AreEqual(
            "PowerDiode.LinkedIdle".Translate(feed.parent.LabelCap).ToString(),
            draw.CompInspectStringExtra()
        );
    }

    [Test]
    public void UnpairedIntakeAndOutletShowNotLinked()
    {
        var draw = Spawn<Building>("PowerDiode_DrawNode", Origin).GetComp<CompPowerDiodeDraw>();
        var feed = Spawn<Building>("PowerDiode_FeedNode", Origin + (IntVec3.East * 3))
            .GetComp<CompPowerDiodeFeed>();
        Map.powerNetManager.UpdatePowerNetsAndConnections_First();

        Expect.IsNull(draw.Partner);
        Expect.IsNull(feed.Partner);
        var notLinked = "PowerDiode.NotLinked".Translate().ToString();
        Expect.AreEqual(notLinked, feed.CompInspectStringExtra());
        Expect.AreEqual(notLinked, draw.CompInspectStringExtra());
    }
}
