using DevTools.Testing;

namespace PowerDiode.Tests;

[TestFixture(TestType.Playing)]
internal sealed class StunnedBatteryMapTests
{
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

    // A full battery on each side of a paired diode.
    private (CompPowerDiodeFeed Feed, CompPowerBattery Source, CompPowerBattery Sink) SpawnDiode()
    {
        var sourceBattery = Spawn<Building>("Battery", Origin + IntVec3.West)
            .GetComp<CompPowerBattery>();
        sourceBattery.SetStoredEnergyPct(1f);
        _ = Spawn<Building>("PowerDiode_DrawNode", Origin);
        var feed = Spawn<Building>("PowerDiode_FeedNode", Origin + IntVec3.East)
            .GetComp<CompPowerDiodeFeed>();
        var sinkBattery = Spawn<Building>("Battery", Origin + (IntVec3.East * 2))
            .GetComp<CompPowerBattery>();
        sinkBattery.SetStoredEnergyPct(1f);
        Map.powerNetManager.UpdatePowerNetsAndConnections_First();

        Expect.IsNotNull(feed.Partner);
        Expect.ReferencesAreEqual(feed.Partner!.PowerTrader.PowerNet, sourceBattery.PowerNet);
        Expect.ReferencesAreEqual(feed.PowerTrader.PowerNet, sinkBattery.PowerNet);

        return (feed, sourceBattery, sinkBattery);
    }

    private static void StunWithEmp(CompPowerBattery battery)
    {
        battery
            .parent.GetComp<CompStunnable>()
            .StunHandler.Notify_DamageApplied(new DamageInfo(DamageDefOf.EMP, 10f));
        Expect.IsTrue(battery.StunnedByEMP, "battery stunned by EMP");
    }

    [Test]
    public void UnstunnedBatteriesCountTheirStoredEnergy()
    {
        var (feed, source, sink) = SpawnDiode();

        feed.CompTick();

        Expect.AreEqual(source.StoredEnergy, feed.SourceBatteryStoredWattDays, "source stored");
        Expect.AreEqual(sink.StoredEnergy, feed.SinkBatteryStoredWattDays, "sink stored");
    }

    // Regression: a stunned battery was counted as holding its stored energy, while vanilla's
    // PowerNet.CurrentStoredEnergy counts it as empty.
    [Test]
    public void StunnedSourceBatteryCountsAsEmpty()
    {
        var (feed, source, _) = SpawnDiode();
        StunWithEmp(source);

        feed.CompTick();

        Expect.AreEqual(0f, feed.SourceBatteryStoredWattDays, "source stored");
        Expect.AreEqual(
            source.PowerNet.CurrentStoredEnergy(),
            feed.SourceBatteryStoredWattDays,
            "matches vanilla"
        );
        Expect.AreEqual(
            source.Props.storedEnergyMax,
            feed.SourceBatteryCapacityWattDays,
            "source capacity"
        );
    }

    [Test]
    public void StunnedSinkBatteryCountsAsEmpty()
    {
        var (feed, _, sink) = SpawnDiode();
        StunWithEmp(sink);

        feed.CompTick();

        Expect.AreEqual(0f, feed.SinkBatteryStoredWattDays, "sink stored");
        Expect.AreEqual(
            sink.PowerNet.CurrentStoredEnergy(),
            feed.SinkBatteryStoredWattDays,
            "matches vanilla"
        );
        Expect.AreEqual(
            sink.Props.storedEnergyMax,
            feed.SinkBatteryCapacityWattDays,
            "sink capacity"
        );
    }
}
