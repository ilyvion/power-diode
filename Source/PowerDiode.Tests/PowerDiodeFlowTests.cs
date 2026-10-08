using DevTools.Testing;

namespace PowerDiode.Tests;

[TestFixture(TestType.MainMenu)]
internal sealed class PowerDiodeFlowTests
{
    [Test]
    public static void FlowIsZeroWhenSinkHasNoDeficitAndNoBatteryHeadroom()
    {
        var flow = PowerDiodeFlow.ComputeFlowWatts(
            capWatts: 500f,
            sinkNetBalanceExclSelf: 200f,
            sourceNetBalanceExclSelf: 1000f,
            sinkBatteryHeadroomWatts: 0f,
            sourceBatteryReserveWatts: 0f
        );
        Expect.AreEqual(0f, flow);
    }

    [Test]
    public static void FlowMatchesSinkDeficitWhenBelowCapAndSourceSurplus()
    {
        var flow = PowerDiodeFlow.ComputeFlowWatts(
            capWatts: 500f,
            sinkNetBalanceExclSelf: -150f,
            sourceNetBalanceExclSelf: 1000f,
            sinkBatteryHeadroomWatts: 0f,
            sourceBatteryReserveWatts: 0f
        );
        Expect.AreEqual(150f, flow);
    }

    [Test]
    public static void FlowIsClampedToCapEvenWithHugeDeficitAndSurplus()
    {
        var flow = PowerDiodeFlow.ComputeFlowWatts(
            capWatts: 500f,
            sinkNetBalanceExclSelf: -5000f,
            sourceNetBalanceExclSelf: 5000f,
            sinkBatteryHeadroomWatts: 0f,
            sourceBatteryReserveWatts: 0f
        );
        Expect.AreEqual(500f, flow);
    }

    [Test]
    public static void FlowIsLimitedBySourceSurplusEvenWhenSinkWantsMore()
    {
        var flow = PowerDiodeFlow.ComputeFlowWatts(
            capWatts: 500f,
            sinkNetBalanceExclSelf: -400f,
            sourceNetBalanceExclSelf: 120f,
            sinkBatteryHeadroomWatts: 0f,
            sourceBatteryReserveWatts: 0f
        );
        Expect.AreEqual(120f, flow);
    }

    [Test]
    public static void FlowIsZeroWhenSourceHasNoSurplus()
    {
        var flow = PowerDiodeFlow.ComputeFlowWatts(
            capWatts: 500f,
            sinkNetBalanceExclSelf: -400f,
            sourceNetBalanceExclSelf: 0f,
            sinkBatteryHeadroomWatts: 0f,
            sourceBatteryReserveWatts: 0f
        );
        Expect.AreEqual(0f, flow);
    }

    [Test]
    public static void FlowIsZeroWhenSourceIsInDeficit()
    {
        var flow = PowerDiodeFlow.ComputeFlowWatts(
            capWatts: 500f,
            sinkNetBalanceExclSelf: -400f,
            sourceNetBalanceExclSelf: -50f,
            sinkBatteryHeadroomWatts: 0f,
            sourceBatteryReserveWatts: 0f
        );
        Expect.AreEqual(0f, flow);
    }

    // The diode was explicitly created to charge batteries: unfilled battery capacity on the
    // sink network counts as demand on its own, even with no consumer deficit at all.
    [Test]
    public static void BatteryHeadroomAloneDrivesFlowUpToCap()
    {
        var flow = PowerDiodeFlow.ComputeFlowWatts(
            capWatts: 300f,
            sinkNetBalanceExclSelf: 50f,
            sourceNetBalanceExclSelf: 1000f,
            sinkBatteryHeadroomWatts: 300f,
            sourceBatteryReserveWatts: 0f
        );
        Expect.AreEqual(300f, flow);
    }

    [Test]
    public static void BatteryHeadroomIsStillLimitedBySourceSurplus()
    {
        var flow = PowerDiodeFlow.ComputeFlowWatts(
            capWatts: 300f,
            sinkNetBalanceExclSelf: 0f,
            sourceNetBalanceExclSelf: 75f,
            sinkBatteryHeadroomWatts: 300f,
            sourceBatteryReserveWatts: 0f
        );
        Expect.AreEqual(75f, flow);
    }

    [Test]
    public static void ConsumerDeficitAndBatteryHeadroomCombineButStillClampToCap()
    {
        var flow = PowerDiodeFlow.ComputeFlowWatts(
            capWatts: 200f,
            sinkNetBalanceExclSelf: -50f,
            sourceNetBalanceExclSelf: 1000f,
            sinkBatteryHeadroomWatts: 200f,
            sourceBatteryReserveWatts: 0f
        );
        Expect.AreEqual(200f, flow);
    }

    [Test]
    public static void ExactZeroBoundariesProduceNoFlow()
    {
        var flow = PowerDiodeFlow.ComputeFlowWatts(
            capWatts: 500f,
            sinkNetBalanceExclSelf: 0f,
            sourceNetBalanceExclSelf: 0f,
            sinkBatteryHeadroomWatts: 0f,
            sourceBatteryReserveWatts: 0f
        );
        Expect.AreEqual(0f, flow);
    }

    // A source network holding only a battery (no generator, no other consumer) reports a zero
    // net balance - PowerNet tracks batteries separately from the generators/consumers that feed
    // into that balance - so the stored reserve is the only thing that can drive flow here.
    [Test]
    public static void SourceBatteryReserveAloneDrivesFlowUpToCap()
    {
        var flow = PowerDiodeFlow.ComputeFlowWatts(
            capWatts: 300f,
            sinkNetBalanceExclSelf: -400f,
            sourceNetBalanceExclSelf: 0f,
            sinkBatteryHeadroomWatts: 0f,
            sourceBatteryReserveWatts: 300f
        );
        Expect.AreEqual(300f, flow);
    }

    [Test]
    public static void SourceBatteryReserveIsStillLimitedBySinkDeficit()
    {
        var flow = PowerDiodeFlow.ComputeFlowWatts(
            capWatts: 300f,
            sinkNetBalanceExclSelf: -75f,
            sourceNetBalanceExclSelf: 0f,
            sinkBatteryHeadroomWatts: 0f,
            sourceBatteryReserveWatts: 300f
        );
        Expect.AreEqual(75f, flow);
    }

    // A healthily charged battery should back a diode's draw at full strength, ramped down only
    // over the final ReactionMarginTicks ticks' worth of watt-days as it nears empty, rather than
    // either an unthrottled instant cutoff or a flat conversion that throttles it far below what
    // it can actually sustain.
    [Test]
    public static void BatterySustainableWattsRampsStoredEnergyOverReactionMargin()
    {
        var watts = PowerDiodeFlow.BatterySustainableWatts(1f);
        var expected = 1f / (PowerDiodeFlow.ReactionMarginTicks * CompPower.WattsToWattDaysPerTick);
        Expect.AreEqual(expected, watts);
    }

    [Test]
    public static void BatterySustainableWattsSubtractsReserveBufferFirst()
    {
        var watts = PowerDiodeFlow.BatterySustainableWatts(15f, reserveBufferWattDays: 10f);
        var expected = 5f / (PowerDiodeFlow.ReactionMarginTicks * CompPower.WattsToWattDaysPerTick);
        Expect.AreEqual(expected, watts);
    }

    // Once the network's total stored energy is at or below the reserve buffer, the battery is
    // treated as having nothing left to give - the reserve is never dipped into.
    [Test]
    public static void BatterySustainableWattsIsZeroAtOrBelowReserveBuffer()
    {
        Expect.AreEqual(
            0f,
            PowerDiodeFlow.BatterySustainableWatts(10f, reserveBufferWattDays: 10f)
        );
        Expect.AreEqual(0f, PowerDiodeFlow.BatterySustainableWatts(5f, reserveBufferWattDays: 10f));
    }

    [Test]
    public static void BatterySustainableWattsIsZeroForNoStoredOrAcceptableEnergy()
    {
        var watts = PowerDiodeFlow.BatterySustainableWatts(0f);
        Expect.AreEqual(0f, watts);
    }

    [Test]
    public static void BatterySustainableWattsClampsNegativeWattDaysToZero()
    {
        var watts = PowerDiodeFlow.BatterySustainableWatts(-5f);
        Expect.AreEqual(0f, watts);
    }

    [Test]
    public static void SourceSurplusAndBatteryReserveCombineButStillClampToCap()
    {
        var flow = PowerDiodeFlow.ComputeFlowWatts(
            capWatts: 200f,
            sinkNetBalanceExclSelf: -1000f,
            sourceNetBalanceExclSelf: 50f,
            sinkBatteryHeadroomWatts: 0f,
            sourceBatteryReserveWatts: 200f
        );
        Expect.AreEqual(200f, flow);
    }

    [Test]
    public static void SourceReserveFloorUsesAbsoluteWattDaysInOneWayValveModeByDefault()
    {
        var floor = PowerDiodeFlow.SourceReserveFloorWattDays(
            mode: PowerDiodeOperatingMode.OneWayValve,
            reserveWattDays: 50f,
            reserveIsPercentage: false,
            reservePercent: 20f,
            sourceBatteryCapacityWattDays: 1000f
        );
        Expect.AreEqual(50f, floor);
    }

    [Test]
    public static void SourceReserveFloorUsesPercentOfCapacityInOneWayValveModeWhenEnabled()
    {
        var floor = PowerDiodeFlow.SourceReserveFloorWattDays(
            mode: PowerDiodeOperatingMode.OneWayValve,
            reserveWattDays: 50f,
            reserveIsPercentage: true,
            reservePercent: 20f,
            sourceBatteryCapacityWattDays: 1000f
        );
        Expect.AreEqual(200f, floor);
    }

    [Test]
    public static void SourceReserveFloorIsZeroInOverflowMode()
    {
        var floor = PowerDiodeFlow.SourceReserveFloorWattDays(
            mode: PowerDiodeOperatingMode.Overflow,
            reserveWattDays: 999f,
            reserveIsPercentage: false,
            reservePercent: 999f,
            sourceBatteryCapacityWattDays: 1000f
        );
        Expect.AreEqual(0f, floor);
    }

    [Test]
    public static void SourceReserveFloorIsZeroInTopUpMode()
    {
        var floor = PowerDiodeFlow.SourceReserveFloorWattDays(
            mode: PowerDiodeOperatingMode.TopUp,
            reserveWattDays: 50f,
            reserveIsPercentage: true,
            reservePercent: 20f,
            sourceBatteryCapacityWattDays: 1000f
        );
        Expect.AreEqual(0f, floor);
    }

    [Test]
    public static void OverflowGateIsZeroAtOrBelowThreshold()
    {
        Expect.AreEqual(
            0f,
            PowerDiodeFlow.OverflowGateFraction(
                capWatts: 500f,
                overflowThresholdPercent: 80f,
                sourceBatteryStoredWattDays: 800f,
                sourceBatteryCapacityWattDays: 1000f
            )
        );
        Expect.AreEqual(
            0f,
            PowerDiodeFlow.OverflowGateFraction(
                capWatts: 500f,
                overflowThresholdPercent: 80f,
                sourceBatteryStoredWattDays: 600f,
                sourceBatteryCapacityWattDays: 1000f
            )
        );
    }

    [Test]
    public static void OverflowGateIsFullyOpenOnceWellAboveThreshold()
    {
        var gate = PowerDiodeFlow.OverflowGateFraction(
            capWatts: 1f,
            overflowThresholdPercent: 80f,
            sourceBatteryStoredWattDays: 1000f,
            sourceBatteryCapacityWattDays: 1000f
        );
        Expect.AreEqual(1f, gate);
    }

    [Test]
    public static void OverflowGateIsZeroWhenCapWattsIsZero()
    {
        var gate = PowerDiodeFlow.OverflowGateFraction(
            capWatts: 0f,
            overflowThresholdPercent: 80f,
            sourceBatteryStoredWattDays: 1000f,
            sourceBatteryCapacityWattDays: 1000f
        );
        Expect.AreEqual(0f, gate);
    }

    [Test]
    public static void OverflowGateMarginRaisesTheThreshold()
    {
        var gate = PowerDiodeFlow.OverflowGateFraction(
            capWatts: 1f,
            overflowThresholdPercent: 80f,
            sourceBatteryStoredWattDays: 804f,
            sourceBatteryCapacityWattDays: 1000f,
            marginWattDays: 5f
        );
        Expect.AreEqual(0f, gate);
    }

    [Test]
    public static void SourceSupplyCombinesSurplusAndBatteryReserveUpToCap()
    {
        Expect.AreEqual(
            150f,
            PowerDiodeFlow.SourceSupplyWatts(
                capWatts: 500f,
                sourceNetBalanceExclSelf: 50f,
                sourceBatteryReserveWatts: 100f
            )
        );
        Expect.AreEqual(
            120f,
            PowerDiodeFlow.SourceSupplyWatts(
                capWatts: 120f,
                sourceNetBalanceExclSelf: 50f,
                sourceBatteryReserveWatts: 100f
            )
        );
    }

    [Test]
    public static void SourceSupplyIgnoresSourceDeficit()
    {
        var supply = PowerDiodeFlow.SourceSupplyWatts(
            capWatts: 500f,
            sourceNetBalanceExclSelf: -200f,
            sourceBatteryReserveWatts: 100f
        );
        Expect.AreEqual(100f, supply);
    }

    // Regression: a switched-off consumer on an outlet's network with no other power source was
    // never counted as demand, so the outlet fed nothing and vanilla never switched it on.
    [Test]
    public static void SwitchedOffConsumerCountsAsDemandWhenSupplyCoversIt()
    {
        var balance = PowerDiodeFlow.SinkBalanceWithStartableConsumers(
            sinkNetBalanceExclSelf: 0f,
            switchedOffDrawWatts: [30f],
            restartSupplyWatts: 100f
        );
        Expect.AreEqual(-30f, balance);
    }

    [Test]
    public static void SwitchedOffConsumerIsLeftOutWhenSupplyCannotCoverIt()
    {
        var balance = PowerDiodeFlow.SinkBalanceWithStartableConsumers(
            sinkNetBalanceExclSelf: -50f,
            switchedOffDrawWatts: [60f],
            restartSupplyWatts: 100f
        );
        Expect.AreEqual(-50f, balance);
    }

    [Test]
    public static void SwitchedOffConsumersAreAddedSmallestFirstWhileSupplyLasts()
    {
        var balance = PowerDiodeFlow.SinkBalanceWithStartableConsumers(
            sinkNetBalanceExclSelf: 0f,
            switchedOffDrawWatts: [80f, 30f, 40f],
            restartSupplyWatts: 100f
        );
        Expect.AreEqual(-70f, balance);
    }

    [Test]
    public static void SinkSurplusCountsTowardsSwitchedOffConsumers()
    {
        var balance = PowerDiodeFlow.SinkBalanceWithStartableConsumers(
            sinkNetBalanceExclSelf: 50f,
            switchedOffDrawWatts: [120f],
            restartSupplyWatts: 70f
        );
        Expect.AreEqual(-70f, balance);
    }

    [Test]
    public static void TopUpGateIsZeroAtOrAboveThreshold()
    {
        Expect.AreEqual(
            0f,
            PowerDiodeFlow.TopUpGateFraction(
                capWatts: 500f,
                topUpThresholdPercent: 20f,
                sinkBatteryStoredWattDays: 200f,
                sinkBatteryCapacityWattDays: 1000f
            )
        );
        Expect.AreEqual(
            0f,
            PowerDiodeFlow.TopUpGateFraction(
                capWatts: 500f,
                topUpThresholdPercent: 20f,
                sinkBatteryStoredWattDays: 800f,
                sinkBatteryCapacityWattDays: 1000f
            )
        );
    }

    [Test]
    public static void TopUpGateIsFullyOpenWhenWellBelowThreshold()
    {
        var gate = PowerDiodeFlow.TopUpGateFraction(
            capWatts: 1f,
            topUpThresholdPercent: 20f,
            sinkBatteryStoredWattDays: 0f,
            sinkBatteryCapacityWattDays: 1000f
        );
        Expect.AreEqual(1f, gate);
    }

    [Test]
    public static void TopUpGateIsZeroWhenCapWattsIsZero()
    {
        var gate = PowerDiodeFlow.TopUpGateFraction(
            capWatts: 0f,
            topUpThresholdPercent: 20f,
            sinkBatteryStoredWattDays: 0f,
            sinkBatteryCapacityWattDays: 1000f
        );
        Expect.AreEqual(0f, gate);
    }
}
