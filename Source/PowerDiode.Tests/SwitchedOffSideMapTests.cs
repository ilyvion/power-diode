using DevTools.Testing;

namespace PowerDiode.Tests;

[TestFixture(TestType.Playing)]
internal sealed class SwitchedOffSideMapTests
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

    // A full battery on the intake's network and an empty one on the outlet's network, with both
    // sides switched on and the diode already feeding.
    private CompPowerDiodeFeed SpawnFeedingDiode()
    {
        var sourceBattery = Spawn<Building>("Battery", Origin + IntVec3.West)
            .GetComp<CompPowerBattery>();
        sourceBattery.SetStoredEnergyPct(1f);
        _ = Spawn<Building>("PowerDiode_DrawNode", Origin);
        var feed = Spawn<Building>("PowerDiode_FeedNode", Origin + IntVec3.East)
            .GetComp<CompPowerDiodeFeed>();
        var sinkBattery = Spawn<Building>("Battery", Origin + (IntVec3.East * 2))
            .GetComp<CompPowerBattery>();
        sinkBattery.SetStoredEnergyPct(0f);
        Map.powerNetManager.UpdatePowerNetsAndConnections_First();

        Expect.IsNotNull(feed.Partner);
        Expect.ReferencesAreEqual(feed.Partner!.PowerTrader.PowerNet, sourceBattery.PowerNet);
        Expect.ReferencesAreEqual(feed.PowerTrader.PowerNet, sinkBattery.PowerNet);

        feed.PowerTrader.PowerOn = true;
        feed.Partner.PowerTrader.PowerOn = true;
        feed.CompTick();
        Expect.GreaterThan(feed.CurrentFlowWatts, 0f, "feeding with both sides on");

        return feed;
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

    // Regression: the outlet kept feeding while vanilla had switched the intake off, so the
    // outlet's network gained power the intake's network never paid for.
    [Test]
    public void SwitchedOffIntakeFeedsNothing()
    {
        var feed = SpawnFeedingDiode();
        var intake = feed.Partner!.PowerTrader;

        intake.PowerOn = false;
        feed.CompTick();

        Expect.AreEqual(0f, feed.CurrentFlowWatts, "no flow");
        Expect.AreEqual(0f, feed.PowerTrader.PowerOutput, "outlet output");
        Expect.AreEqual(0f, intake.PowerOutput, "intake output");
        Expect.AreEqual(
            0f,
            feed.PowerTrader.PowerNet.CurrentEnergyGainRate(),
            "outlet network gains nothing"
        );
    }

    [Test]
    public void SwitchedOffOutletFeedsNothing()
    {
        var feed = SpawnFeedingDiode();
        var intake = feed.Partner!.PowerTrader;

        feed.PowerTrader.PowerOn = false;
        feed.CompTick();

        Expect.AreEqual(0f, feed.CurrentFlowWatts, "no flow");
        Expect.AreEqual(0f, feed.PowerTrader.PowerOutput, "outlet output");
        Expect.AreEqual(0f, intake.PowerOutput, "intake output");
        Expect.AreEqual(
            0f,
            intake.PowerNet.CurrentEnergyGainRate(),
            "intake network loses nothing"
        );
    }

    // Vanilla switches the idle intake back on, and the diode resumes feeding.
    [Test]
    public IEnumerator SwitchedOffIntakeResumesFeedingOnceSwitchedOn()
    {
        var feed = SpawnFeedingDiode();
        var intake = feed.Partner!.PowerTrader;

        intake.PowerOn = false;
        feed.CompTick();
        Expect.AreEqual(0f, feed.CurrentFlowWatts, "no flow while off");

        foreach (
            var frame in TickUntil(() => intake.PowerOn && feed.CurrentFlowWatts > 0f, TickTimeout)
        )
        {
            yield return frame;
        }

        Expect.IsTrue(intake.PowerOn, "intake switched back on");
        Expect.GreaterThan(feed.CurrentFlowWatts, 0f, "feeding again");
    }
}
