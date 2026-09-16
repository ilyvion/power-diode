using RimTestRedux;

namespace PowerDiode.Tests;

[TestSuite]
internal static class PowerDiodeFlowTests
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
        Assert.That(flow).Is.EqualTo(0f);
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
        Assert.That(flow).Is.EqualTo(150f);
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
        Assert.That(flow).Is.EqualTo(500f);
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
        Assert.That(flow).Is.EqualTo(120f);
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
        Assert.That(flow).Is.EqualTo(0f);
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
        Assert.That(flow).Is.EqualTo(0f);
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
        Assert.That(flow).Is.EqualTo(300f);
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
        Assert.That(flow).Is.EqualTo(75f);
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
        Assert.That(flow).Is.EqualTo(200f);
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
        Assert.That(flow).Is.EqualTo(0f);
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
        Assert.That(flow).Is.EqualTo(300f);
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
        Assert.That(flow).Is.EqualTo(75f);
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
        Assert.That(watts).Is.EqualTo(expected);
    }

    [Test]
    public static void BatterySustainableWattsSubtractsReserveBufferFirst()
    {
        var watts = PowerDiodeFlow.BatterySustainableWatts(15f, reserveBufferWattDays: 10f);
        var expected = 5f / (PowerDiodeFlow.ReactionMarginTicks * CompPower.WattsToWattDaysPerTick);
        Assert.That(watts).Is.EqualTo(expected);
    }

    // Once the network's total stored energy is at or below the reserve buffer, the battery is
    // treated as having nothing left to give - the reserve is never dipped into.
    [Test]
    public static void BatterySustainableWattsIsZeroAtOrBelowReserveBuffer()
    {
        Assert
            .That(PowerDiodeFlow.BatterySustainableWatts(10f, reserveBufferWattDays: 10f))
            .Is.EqualTo(0f);
        Assert
            .That(PowerDiodeFlow.BatterySustainableWatts(5f, reserveBufferWattDays: 10f))
            .Is.EqualTo(0f);
    }

    [Test]
    public static void BatterySustainableWattsIsZeroForNoStoredOrAcceptableEnergy()
    {
        var watts = PowerDiodeFlow.BatterySustainableWatts(0f);
        Assert.That(watts).Is.EqualTo(0f);
    }

    [Test]
    public static void BatterySustainableWattsClampsNegativeWattDaysToZero()
    {
        var watts = PowerDiodeFlow.BatterySustainableWatts(-5f);
        Assert.That(watts).Is.EqualTo(0f);
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
        Assert.That(flow).Is.EqualTo(200f);
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
        Assert.That(floor).Is.EqualTo(50f);
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
        Assert.That(floor).Is.EqualTo(200f);
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
        Assert.That(floor).Is.EqualTo(0f);
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
        Assert.That(floor).Is.EqualTo(0f);
    }

    [Test]
    public static void OverflowGateIsZeroAtOrBelowThreshold()
    {
        Assert
            .That(
                PowerDiodeFlow.OverflowGateFraction(
                    capWatts: 500f,
                    overflowThresholdPercent: 80f,
                    sourceBatteryStoredWattDays: 800f,
                    sourceBatteryCapacityWattDays: 1000f
                )
            )
            .Is.EqualTo(0f);
        Assert
            .That(
                PowerDiodeFlow.OverflowGateFraction(
                    capWatts: 500f,
                    overflowThresholdPercent: 80f,
                    sourceBatteryStoredWattDays: 600f,
                    sourceBatteryCapacityWattDays: 1000f
                )
            )
            .Is.EqualTo(0f);
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
        Assert.That(gate).Is.EqualTo(1f);
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
        Assert.That(gate).Is.EqualTo(0f);
    }

    [Test]
    public static void TopUpGateIsZeroAtOrAboveThreshold()
    {
        Assert
            .That(
                PowerDiodeFlow.TopUpGateFraction(
                    capWatts: 500f,
                    topUpThresholdPercent: 20f,
                    sinkBatteryStoredWattDays: 200f,
                    sinkBatteryCapacityWattDays: 1000f
                )
            )
            .Is.EqualTo(0f);
        Assert
            .That(
                PowerDiodeFlow.TopUpGateFraction(
                    capWatts: 500f,
                    topUpThresholdPercent: 20f,
                    sinkBatteryStoredWattDays: 800f,
                    sinkBatteryCapacityWattDays: 1000f
                )
            )
            .Is.EqualTo(0f);
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
        Assert.That(gate).Is.EqualTo(1f);
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
        Assert.That(gate).Is.EqualTo(0f);
    }
}
