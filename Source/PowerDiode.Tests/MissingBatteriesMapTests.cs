using DevTools.Testing;

namespace PowerDiode.Tests;

[TestFixture(TestType.Playing)]
internal sealed class MissingBatteriesMapTests
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

    // An intake with an outlet east of it, and no batteries on either side.
    private CompPowerDiodeFeed SpawnDiode(PowerDiodeOperatingMode mode)
    {
        _ = Spawn<Building>("PowerDiode_DrawNode", Origin);
        var feed = Spawn<Building>("PowerDiode_FeedNode", Origin + IntVec3.East)
            .GetComp<CompPowerDiodeFeed>();
        feed.OperatingMode = mode;
        Map.powerNetManager.UpdatePowerNetsAndConnections_First();
        return feed;
    }

    private void SpawnIntakeBattery() => _ = Spawn<Building>("Battery", Origin + IntVec3.West);

    private void SpawnOutletBattery() =>
        _ = Spawn<Building>("Battery", Origin + (IntVec3.East * 2));

    [Test]
    public void OverflowWithNoIntakeBatteriesIsReportedOnTheIntakeUntilOneIsBuilt()
    {
        var feed = SpawnDiode(PowerDiodeOperatingMode.Overflow);
        Expect.IsNotNull(feed.Partner);
        var draw = feed.Partner!;
        SpawnOutletBattery();
        Map.powerNetManager.UpdatePowerNetsAndConnections_First();

        Expect.AreEqual(PowerDiodeMissingBatteries.Intake, feed.MissingBatteries);
        Expect.AreEqual(
            "PowerDiode.NoIntakeBatteries"
                .Translate(feed.parent.LabelCap, PowerDiodeOperatingMode.Overflow.Label())
                .ToString(),
            draw.CompInspectStringExtra()
        );
        Expect.AreEqual(
            "PowerDiode.LinkedIdle".Translate(draw.parent.LabelCap).ToString(),
            feed.CompInspectStringExtra()
        );

        SpawnIntakeBattery();
        Map.powerNetManager.UpdatePowerNetsAndConnections_First();

        Expect.AreEqual(PowerDiodeMissingBatteries.None, feed.MissingBatteries);
    }

    [Test]
    public void TopUpWithNoOutletBatteriesIsReportedOnTheOutletUntilOneIsBuilt()
    {
        var feed = SpawnDiode(PowerDiodeOperatingMode.TopUp);
        Expect.IsNotNull(feed.Partner);
        var draw = feed.Partner!;
        SpawnIntakeBattery();
        Map.powerNetManager.UpdatePowerNetsAndConnections_First();

        Expect.AreEqual(PowerDiodeMissingBatteries.Outlet, feed.MissingBatteries);
        Expect.AreEqual(
            "PowerDiode.NoOutletBatteries"
                .Translate(draw.parent.LabelCap, PowerDiodeOperatingMode.TopUp.Label())
                .ToString(),
            feed.CompInspectStringExtra()
        );
        Expect.AreEqual(
            "PowerDiode.LinkedIdle".Translate(feed.parent.LabelCap).ToString(),
            draw.CompInspectStringExtra()
        );

        SpawnOutletBattery();
        Map.powerNetManager.UpdatePowerNetsAndConnections_First();

        Expect.AreEqual(PowerDiodeMissingBatteries.None, feed.MissingBatteries);
    }

    [Test]
    public void OneWayValveWithNoBatteriesReportsNothingMissing()
    {
        var feed = SpawnDiode(PowerDiodeOperatingMode.OneWayValve);
        Expect.IsNotNull(feed.Partner);

        Expect.AreEqual(PowerDiodeMissingBatteries.None, feed.MissingBatteries);
    }

    // A conduit north of the intake and the outlet joins both sides, so the shared-grid state is
    // reported instead.
    [Test]
    public void SharedGridReportsNothingMissing()
    {
        var feed = SpawnDiode(PowerDiodeOperatingMode.Overflow);
        Expect.IsNotNull(feed.Partner);
        _ = Spawn<Building>("PowerConduit", Origin + IntVec3.North);
        _ = Spawn<Building>("PowerConduit", Origin + IntVec3.East + IntVec3.North);
        Map.powerNetManager.UpdatePowerNetsAndConnections_First();

        Expect.AreEqual(PowerDiodeMissingBatteries.None, feed.MissingBatteries);
    }
}
