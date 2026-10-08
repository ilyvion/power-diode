using DevTools.Testing;

namespace PowerDiode.Tests;

[TestFixture(TestType.Playing)]
internal sealed class OutletPowersConsumerMapTests
{
    private const int TickTimeout = 2500;
    private const int TicksPerFrame = 60;
    private const int FlappingWindowTicks = 2400;
    private const float BatterySelfDischargeWatts = 5f;

    private readonly List<Thing> spawned = [];
    private TimeSpeed previousTimeSpeed;
    private float previousMinWattage;
    private float previousMaxWattage;

    private static Map Map => Find.CurrentMap;

    private static IntVec3 Origin => Map.Center;

    [SetUp]
    public void PauseTime()
    {
        previousTimeSpeed = Find.TickManager.CurTimeSpeed;
        Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
        previousMinWattage = PowerDiodeMod.Settings.MinWattage;
        previousMaxWattage = PowerDiodeMod.Settings.MaxWattage;
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

    // A fully charged battery on the intake's network and nothing else; the outlet's network is
    // empty until lamps are added with SpawnSwitchedOffLamp.
    private (CompPowerBattery Battery, CompPowerDiodeFeed Feed) SpawnDiode(string outletDefName)
    {
        var batteryCell = Origin + IntVec3.West;
        var intakeCell = Origin;
        var outletCell = Origin + IntVec3.East;

        var battery = Spawn<Building>("Battery", batteryCell).GetComp<CompPowerBattery>();
        battery.SetStoredEnergyPct(1f);
        _ = Spawn<Building>("PowerDiode_DrawNode", intakeCell);
        if (outletDefName == "PowerDiode_WallFeedNode")
        {
            _ = Spawn<Building>("Wall", outletCell, ThingDefOf.Steel);
        }
        var outlet = Spawn<Building>(outletDefName, outletCell);
        return (battery, outlet.GetComp<CompPowerDiodeFeed>());
    }

    // Lamps go at least 7 cells east of the intake, within connection range of the outlet only, so
    // they can't join the intake's network.
    private CompPowerTrader SpawnSwitchedOffLamp(IntVec3 cell)
    {
        var lampPower = Spawn<Building>("StandingLamp", cell).GetComp<CompPowerTrader>();
        lampPower.PowerOn = false;
        return lampPower;
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

    private static void SetStoredEnergy(CompPowerBattery battery, float wattDays) =>
        battery.SetStoredEnergyPct(wattDays / battery.Props.storedEnergyMax);

    // The source battery charge below which the feed stops drawing from it.
    private static float SourceFloorWattDays(CompPowerDiodeFeed feed) =>
        feed.OperatingMode == PowerDiodeOperatingMode.Overflow
            ? feed.OverflowThresholdPercent / 100f * feed.SourceBatteryCapacityWattDays
            : PowerDiodeFlow.SourceReserveFloorWattDays(
                feed.OperatingMode,
                feed.ReserveWattDays,
                PowerDiodeMod.Settings.ReserveIsPercentage,
                feed.ReservePercent,
                feed.SourceBatteryCapacityWattDays
            );

    // Regression: an outlet whose network held nothing but a switched-off consumer fed it nothing,
    // and vanilla only switches a consumer on once its network already has the power for it, so
    // the consumer stayed off for good.
    [Test]
    public IEnumerator OutletSwitchesOnLampWithNoOtherPowerSourceOnItsNetwork(
        [Parameters("PowerDiode_FeedNode", "PowerDiode_WallFeedNode")] string outletDefName
    )
    {
        var (_, feed) = SpawnDiode(outletDefName);
        var lampPower = SpawnSwitchedOffLamp(Origin + (IntVec3.East * 7));
        Map.powerNetManager.UpdatePowerNetsAndConnections_First();

        Expect.IsNotNull(feed.Partner);
        Expect.ReferencesAreEqual(feed.PowerTrader.PowerNet, lampPower.PowerNet);
        Expect.GreaterThanOrEqualTo(feed.TargetWatts, -lampPower.PowerOutput);

        foreach (var frame in TickUntil(() => lampPower.PowerOn, TickTimeout))
        {
            yield return frame;
        }

        Expect.IsTrue(lampPower.PowerOn, "lamp powered");
        Expect.AreApproximatelyEqual(-lampPower.PowerOutput, feed.CurrentFlowWatts);
    }

    // While power flows, the intake reports feeding the outlet and the outlet reports receiving
    // from the intake.
    [Test]
    public IEnumerator InspectStringsDescribeFlowDirection()
    {
        var (_, feed) = SpawnDiode("PowerDiode_FeedNode");
        var lampPower = SpawnSwitchedOffLamp(Origin + (IntVec3.East * 7));
        Map.powerNetManager.UpdatePowerNetsAndConnections_First();

        Expect.IsNotNull(feed.Partner);
        var draw = feed.Partner!;

        foreach (var frame in TickUntil(() => lampPower.PowerOn, TickTimeout))
        {
            yield return frame;
        }
        Expect.IsTrue(lampPower.PowerOn, "lamp powered");

        var watts = feed.CurrentFlowWatts.ToString("F0", CultureInfo.InvariantCulture);
        Expect.AreEqual(
            "PowerDiode.Feeding".Translate(feed.parent.LabelCap, watts).ToString(),
            draw.CompInspectStringExtra()
        );
        Expect.AreEqual(
            "PowerDiode.ReceivingFrom".Translate(draw.parent.LabelCap, watts).ToString(),
            feed.CompInspectStringExtra()
        );
    }

    // A lamp that browns out because the intake's battery reached the outlet's reserve floor (or
    // overflow threshold) stays off while that battery slowly recharges, rather than switching back
    // on as soon as there's a sliver of charge above the floor and browning out again moments later.
    [Test]
    public IEnumerator LampBrownedOutAtSourceFloorStaysOffWhileSourceRecharges(
        [Parameters(PowerDiodeOperatingMode.OneWayValve, PowerDiodeOperatingMode.Overflow)]
            PowerDiodeOperatingMode mode
    )
    {
        var (battery, feed) = SpawnDiode("PowerDiode_FeedNode");
        feed.OperatingMode = mode;
        var lampPower = SpawnSwitchedOffLamp(Origin + (IntVec3.East * 7));
        Map.powerNetManager.UpdatePowerNetsAndConnections_First();

        Expect.IsNotNull(feed.Partner);
        var intakePower = feed.Partner!.PowerTrader;
        Expect.ReferencesAreEqual(feed.PowerTrader.PowerNet, lampPower.PowerNet);
        Expect.GreaterThanOrEqualTo(feed.TargetWatts, -lampPower.PowerOutput);

        // Let the feed record the source battery capacity before working out its floor.
        feed.CompTick();
        var floorWattDays = SourceFloorWattDays(feed);
        SetStoredEnergy(battery, floorWattDays + PowerDiodeFlow.RestartMarginWattDays + 5f);

        foreach (
            var frame in TickUntil(() => lampPower.PowerOn && intakePower.PowerOn, TickTimeout)
        )
        {
            yield return frame;
        }
        Expect.IsTrue(lampPower.PowerOn, "lamp powered above the restart margin");
        Expect.IsTrue(intakePower.PowerOn, "intake powered");

        // Charges the source battery at half the lamp's draw net of self-discharge, so it drains
        // while the lamp runs and recharges while it's off.
        var lampDrawWatts = -lampPower.PowerOutput;
        var trickleWattDays =
            ((lampDrawWatts / 2f) + BatterySelfDischargeWatts) * CompPower.WattsToWattDaysPerTick;
        SetStoredEnergy(battery, floorWattDays + 0.05f);

        var switchOffs = 0;
        var switchOns = 0;
        var wasOn = true;
        for (var ticks = 0; ticks < FlappingWindowTicks; ticks += TicksPerFrame)
        {
            for (var i = 0; i < TicksPerFrame; i++)
            {
                SetStoredEnergy(battery, battery.StoredEnergy + trickleWattDays);
                Find.TickManager.DoSingleTick();
                var isOn = lampPower.PowerOn;
                if (isOn != wasOn)
                {
                    if (isOn)
                    {
                        switchOns++;
                    }
                    else
                    {
                        switchOffs++;
                    }
                    wasOn = isOn;
                }
            }
            yield return null;
        }

        Expect.AreEqual(1, switchOffs, "lamp browned out once");
        Expect.AreEqual(0, switchOns, "lamp stayed off while the source recharged");
        Expect.GreaterThan(
            battery.StoredEnergy,
            floorWattDays + 0.25f,
            "source battery recharged above its floor"
        );
    }

    // With the wattage cap covering only one of two lamps, one lamp runs steadily and the other
    // stays off, rather than either flickering.
    [Test]
    public IEnumerator CapCoveringOneOfTwoLampsRunsOneSteadily()
    {
        PowerDiodeMod.Settings.MinWattage = 0f;
        var (_, feed) = SpawnDiode("PowerDiode_FeedNode");
        var firstLampPower = SpawnSwitchedOffLamp(Origin + (IntVec3.East * 7));
        var secondLampPower = SpawnSwitchedOffLamp(Origin + (IntVec3.East * 7) + IntVec3.North);
        Map.powerNetManager.UpdatePowerNetsAndConnections_First();

        var lampDrawWatts = -firstLampPower.PowerOutput;
        feed.TargetWatts = lampDrawWatts * 1.5f;
        Expect.IsNotNull(feed.Partner);
        Expect.ReferencesAreEqual(feed.PowerTrader.PowerNet, firstLampPower.PowerNet);
        Expect.ReferencesAreEqual(feed.PowerTrader.PowerNet, secondLampPower.PowerNet);
        Expect.AreApproximatelyEqual(lampDrawWatts * 1.5f, feed.TargetWatts);

        foreach (
            var frame in TickUntil(
                () => firstLampPower.PowerOn || secondLampPower.PowerOn,
                TickTimeout
            )
        )
        {
            yield return frame;
        }
        var runningLampPower = firstLampPower.PowerOn ? firstLampPower : secondLampPower;
        var idleLampPower = firstLampPower.PowerOn ? secondLampPower : firstLampPower;
        Expect.IsTrue(runningLampPower.PowerOn, "one lamp powered");

        var runningLampSwitchedOff = false;
        var idleLampSwitchedOn = false;
        for (var ticks = 0; ticks < FlappingWindowTicks; ticks += TicksPerFrame)
        {
            for (var i = 0; i < TicksPerFrame; i++)
            {
                Find.TickManager.DoSingleTick();
                runningLampSwitchedOff |= !runningLampPower.PowerOn;
                idleLampSwitchedOn |= idleLampPower.PowerOn;
            }
            yield return null;
        }

        Expect.IsFalse(runningLampSwitchedOff, "running lamp stayed on");
        Expect.IsFalse(idleLampSwitchedOn, "idle lamp stayed off");
        Expect.AreApproximatelyEqual(lampDrawWatts, feed.CurrentFlowWatts);
    }
}
