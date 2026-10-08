using DevTools.Testing;

namespace PowerDiode.Tests;

[TestFixture(TestType.Playing)]
internal sealed class IntakeKeepsPowerForOwnConsumersMapTests
{
    private const int TickTimeout = 2500;
    private const int TicksPerFrame = 60;
    private const int FlappingWindowTicks = 2400;

    private readonly List<Thing> spawned = [];
    private bool lampSwitchedOff;
    private bool intakeSwitchedOff;
    private TimeSpeed previousTimeSpeed;
    private float previousMinWattage;
    private float previousMaxWattage;
    private float previousMinReserveWattDays;
    private bool previousReserveIsPercentage;

    private static Map Map => Find.CurrentMap;

    private static IntVec3 Origin => Map.Center;

    [SetUp]
    public void PauseTime()
    {
        previousTimeSpeed = Find.TickManager.CurTimeSpeed;
        Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
        previousMinWattage = PowerDiodeMod.Settings.MinWattage;
        previousMaxWattage = PowerDiodeMod.Settings.MaxWattage;
        previousMinReserveWattDays = PowerDiodeMod.Settings.MinReserveWattDays;
        previousReserveIsPercentage = PowerDiodeMod.Settings.ReserveIsPercentage;
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
        PowerDiodeMod.Settings.MinWattage = previousMinWattage;
        PowerDiodeMod.Settings.MaxWattage = previousMaxWattage;
        PowerDiodeMod.Settings.MinReserveWattDays = previousMinReserveWattDays;
        PowerDiodeMod.Settings.ReserveIsPercentage = previousReserveIsPercentage;
    }

    private T Spawn<T>(string defName, IntVec3 cell, ThingDef? stuff = null)
        where T : Thing
    {
        var thing = (T)
            GenSpawn.Spawn(
                ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed(defName), stuff),
                cell,
                Map
            );
        spawned.Add(thing);
        return thing;
    }

    // A refueled wood-fired generator and a switched-off lamp on the intake's network, and an empty
    // battery on the outlet's network wanting more than the generator's whole output. The lamp is 6
    // cells west of the intake, within connection range of the generator but not of the outlet.
    private (
        CompPowerPlant Generator,
        CompPowerTrader Lamp,
        CompPowerDiodeFeed Feed
    ) SpawnScenario()
    {
        var generator = Spawn<Building>("WoodFiredGenerator", Origin + (IntVec3.West * 2))
            .GetComp<CompPowerPlant>();
        var refuelable = generator.parent.GetComp<CompRefuelable>();
        refuelable.Refuel(refuelable.Props.fuelCapacity);
        generator.UpdateDesiredPowerOutput();

        _ = Spawn<Building>("PowerDiode_DrawNode", Origin);
        var feed = Spawn<Building>("PowerDiode_FeedNode", Origin + IntVec3.East)
            .GetComp<CompPowerDiodeFeed>();
        var sinkBattery = Spawn<Building>("Battery", Origin + (IntVec3.East * 2))
            .GetComp<CompPowerBattery>();
        sinkBattery.SetStoredEnergyPct(0f);

        var lamp = Spawn<Building>("StandingLamp", Origin + (IntVec3.West * 6))
            .GetComp<CompPowerTrader>();
        lamp.PowerOn = false;
        Map.powerNetManager.UpdatePowerNetsAndConnections_First();

        PowerDiodeMod.Settings.MinWattage = 0f;
        PowerDiodeMod.Settings.MaxWattage = Settings.DefaultMaxWattage;
        feed.TargetWatts = Settings.DefaultMaxWattage;

        Expect.IsNotNull(feed.Partner);
        var intake = feed.Partner!.PowerTrader;
        Expect.ReferencesAreEqual(intake.PowerNet, generator.PowerNet);
        Expect.ReferencesAreEqual(intake.PowerNet, lamp.PowerNet);
        Expect.ReferencesAreEqual(feed.PowerTrader.PowerNet, sinkBattery.PowerNet);
        Expect.IsTrue(generator.PowerOn, "generator on");
        Expect.IsFalse(lamp.PowerOn, "lamp off");
        Expect.GreaterThan(generator.PowerOutput, -lamp.PowerOutput);
        Expect.GreaterThanOrEqualTo(feed.TargetWatts, generator.PowerOutput);

        // Start with the diode already drawing, so vanilla can't switch the lamp on before it does.
        intake.PowerOn = true;
        feed.CompTick();

        return (generator, lamp, feed);
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

    // Ticks for FlappingWindowTicks, recording in lampSwitchedOff/intakeSwitchedOff whether either
    // was ever off.
    private IEnumerable TickWatchingForSwitchOff(CompPowerTrader lamp, CompPowerTrader intake)
    {
        lampSwitchedOff = false;
        intakeSwitchedOff = false;
        for (var ticks = 0; ticks < FlappingWindowTicks; ticks += TicksPerFrame)
        {
            for (var i = 0; i < TicksPerFrame; i++)
            {
                Find.TickManager.DoSingleTick();
                lampSwitchedOff |= !lamp.PowerOn;
                intakeSwitchedOff |= !intake.PowerOn;
            }
            yield return null;
        }
    }

    // Regression: the diode drew the intake network's whole surplus for the outlet, and vanilla only
    // switches a consumer on once its network already has the surplus for it, so a switched-off
    // consumer on a battery-less intake network stayed off for as long as the outlet wanted power.
    [Test]
    public IEnumerator IntakeLampSwitchesOnWithNoBatteryOnIntakeNetwork()
    {
        var (generator, lamp, feed) = SpawnScenario();

        foreach (var frame in TickUntil(() => lamp.PowerOn, TickTimeout))
        {
            yield return frame;
        }

        Expect.IsTrue(lamp.PowerOn, "intake lamp powered");

        foreach (var frame in TickWatchingForSwitchOff(lamp, feed.Partner!.PowerTrader))
        {
            yield return frame;
        }

        Expect.IsFalse(lampSwitchedOff, "intake lamp stayed on");
        Expect.IsFalse(intakeSwitchedOff, "intake stayed on");
        Expect.AreApproximatelyEqual(
            generator.PowerOutput + lamp.PowerOutput,
            feed.CurrentFlowWatts
        );
    }

    // Regression: vanilla won't switch anything on while a network's batteries hold between 0.1 Wd
    // and 5 Wd, and a diode whose source reserve floor (or overflow threshold) sat in that range
    // kept the intake's batteries there, so a switched-off consumer on the intake network stayed
    // off.
    [Test]
    public IEnumerator IntakeLampSwitchesOnWithIntakeBatteryFloorBelowVanillaSwitchOnCharge(
        [Parameters(PowerDiodeOperatingMode.OneWayValve, PowerDiodeOperatingMode.Overflow)]
            PowerDiodeOperatingMode mode
    )
    {
        PowerDiodeMod.Settings.MinReserveWattDays = 0f;
        PowerDiodeMod.Settings.ReserveIsPercentage = false;
        var (_, lamp, feed) = SpawnScenario();
        var sourceBattery = Spawn<Building>("Battery", Origin + (IntVec3.South * 2))
            .GetComp<CompPowerBattery>();
        sourceBattery.SetStoredEnergyPct(4.8f / sourceBattery.Props.storedEnergyMax);
        Map.powerNetManager.UpdatePowerNetsAndConnections_First();
        Expect.ReferencesAreEqual(feed.Partner!.PowerTrader.PowerNet, sourceBattery.PowerNet);
        Expect.ReferencesAreEqual(lamp.PowerNet, sourceBattery.PowerNet);

        feed.OperatingMode = mode;
        feed.ReserveWattDays = 2f;
        feed.OverflowThresholdPercent = 2f / sourceBattery.Props.storedEnergyMax * 100f;
        feed.CompTick();

        foreach (var frame in TickUntil(() => lamp.PowerOn, TickTimeout))
        {
            yield return frame;
        }

        Expect.IsTrue(lamp.PowerOn, "intake lamp powered");

        foreach (var frame in TickWatchingForSwitchOff(lamp, feed.Partner!.PowerTrader))
        {
            yield return frame;
        }

        Expect.IsFalse(lampSwitchedOff, "intake lamp stayed on");
        Expect.IsFalse(intakeSwitchedOff, "intake stayed on");
        Expect.GreaterThan(feed.CurrentFlowWatts, 0f, "diode still feeding");
    }

    // Regression: a switched-off consumer drawing more than the intake network's surplus only
    // switches on once the intake's batteries hold PowerNet.MinStoredEnergyToTurnOn, but the diode
    // kept taking the surplus that would have charged them there, so it stayed off.
    [Test]
    public IEnumerator IntakeLampDrawingMoreThanTheSurplusSwitchesOnOnceBatteryCharges()
    {
        PowerDiodeMod.Settings.MinReserveWattDays = 0f;
        PowerDiodeMod.Settings.ReserveIsPercentage = false;
        var (generator, lamp, feed) = SpawnScenario();
        lamp.PowerOutput = -(generator.PowerOutput + 200f);
        var sourceBattery = Spawn<Building>("Battery", Origin + (IntVec3.South * 2))
            .GetComp<CompPowerBattery>();
        sourceBattery.SetStoredEnergyPct(2f / sourceBattery.Props.storedEnergyMax);
        Map.powerNetManager.UpdatePowerNetsAndConnections_First();
        Expect.ReferencesAreEqual(feed.Partner!.PowerTrader.PowerNet, sourceBattery.PowerNet);
        Expect.ReferencesAreEqual(lamp.PowerNet, sourceBattery.PowerNet);

        feed.ReserveWattDays = 0f;
        feed.CompTick();
        Expect.AreEqual(0f, feed.CurrentFlowWatts, "surplus left to charge the intake battery");

        foreach (var frame in TickUntil(() => lamp.PowerOn, TickTimeout))
        {
            yield return frame;
        }

        Expect.IsTrue(lamp.PowerOn, "intake lamp powered");
    }
}
