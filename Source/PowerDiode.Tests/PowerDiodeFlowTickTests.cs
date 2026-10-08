using DevTools.Testing;

namespace PowerDiode.Tests;

[TestFixture(TestType.MainMenu)]
internal sealed class PowerDiodeFlowTickTests
{
    // A one-way valve with no reserve, wanting 1000 W on its sink network and with nothing on its
    // source network.
    private static readonly FlowTickInputs Baseline = new(
        Mode: PowerDiodeOperatingMode.OneWayValve,
        TargetWatts: 1000f,
        ReserveWattDays: 0f,
        ReserveIsPercentage: false,
        ReservePercent: 10f,
        OverflowThresholdPercent: 80f,
        TopUpThresholdPercent: 20f,
        SourceRawBalanceExclSelf: 0f,
        SourceSwitchedOffDrawWatts: [],
        SinkRawBalanceExclSelf: -1000f,
        SinkSwitchedOffDrawWatts: [],
        SinkAcceptWattDays: 0f,
        SourceStoredWattDays: 0f,
        SourceCapacityWattDays: 0f,
        SinkStoredWattDays: 0f,
        SinkCapacityWattDays: 0f
    );

    [Test]
    public static void SourceConsumerTheSurplusCoversKeepsBatteryAtVanillaSwitchOnCharge()
    {
        var inputs = Baseline with
        {
            SourceRawBalanceExclSelf = 50f,
            SourceSwitchedOffDrawWatts = [50f],
            SourceStoredWattDays = 4f,
            SourceCapacityWattDays = 1000f,
        };

        Expect.AreEqual(0f, PowerDiodeFlow.TickFlowWatts(inputs));
        Expect.AreEqual(
            1000f,
            PowerDiodeFlow.TickFlowWatts(inputs with { SourceSwitchedOffDrawWatts = [] })
        );
    }

    // Regression: a switched-off source consumer drawing more than the source surplus never
    // switched back on, since the diode took the surplus the source batteries needed to reach
    // vanilla's switch-on charge.
    [Test]
    public static void SourceConsumerTheSurplusCannotCoverHoldsBackSurplusBelowVanillaSwitchOnCharge()
    {
        var inputs = Baseline with
        {
            SourceRawBalanceExclSelf = 300f,
            SourceSwitchedOffDrawWatts = [500f],
            SourceStoredWattDays = 2f,
            SourceCapacityWattDays = 600f,
        };

        Expect.AreEqual(0f, PowerDiodeFlow.TickFlowWatts(inputs));
    }

    [Test]
    public static void SourceConsumerTheSurplusCannotCoverKeepsBatteryAtVanillaSwitchOnCharge()
    {
        var inputs = Baseline with
        {
            SourceRawBalanceExclSelf = 300f,
            SourceSwitchedOffDrawWatts = [500f],
            SourceStoredWattDays = 5f,
            SourceCapacityWattDays = 600f,
        };

        Expect.AreEqual(300f, PowerDiodeFlow.TickFlowWatts(inputs));
        Expect.AreEqual(
            1000f,
            PowerDiodeFlow.TickFlowWatts(inputs with { SourceSwitchedOffDrawWatts = [] })
        );
    }

    [Test]
    public static void NoSurplusIsHeldBackWhenSourceBatteriesCannotHoldVanillaSwitchOnCharge()
    {
        var inputs = Baseline with
        {
            SourceRawBalanceExclSelf = 300f,
            SourceSwitchedOffDrawWatts = [500f],
            SourceStoredWattDays = 2f,
            SourceCapacityWattDays = 4f,
        };

        Expect.AreEqual(300f, PowerDiodeFlow.TickFlowWatts(inputs));
        Expect.AreEqual(
            300f,
            PowerDiodeFlow.TickFlowWatts(
                inputs with
                {
                    SourceStoredWattDays = 0f,
                    SourceCapacityWattDays = 0f,
                }
            )
        );
    }

    // Overflow's restart margin keeps a switched-off sink consumer from counting as demand until
    // the source batteries are RestartMarginWattDays above the threshold, but the sink's existing
    // deficit is still fed.
    [Test]
    public static void OverflowRestartMarginLeavesOutSinkConsumerButStillFeedsSinkDeficit()
    {
        var inputs = Baseline with
        {
            Mode = PowerDiodeOperatingMode.Overflow,
            OverflowThresholdPercent = 80f,
            SourceStoredWattDays = 802f,
            SourceCapacityWattDays = 1000f,
            SinkRawBalanceExclSelf = -50f,
            SinkSwitchedOffDrawWatts = [100f],
        };

        Expect.AreEqual(50f, PowerDiodeFlow.TickFlowWatts(inputs));
        Expect.AreEqual(
            150f,
            PowerDiodeFlow.TickFlowWatts(inputs with { SourceStoredWattDays = 806f })
        );
    }

    [Test]
    public static void OneWayValveCountsSinkConsumerWithinOverflowRestartMargin()
    {
        var inputs = Baseline with
        {
            SourceStoredWattDays = 802f,
            SourceCapacityWattDays = 1000f,
            SinkRawBalanceExclSelf = -50f,
            SinkSwitchedOffDrawWatts = [100f],
        };

        Expect.AreEqual(150f, PowerDiodeFlow.TickFlowWatts(inputs));
    }

    [Test]
    public static void OneWayValveReserveUsesPercentageOrWattDaysPerSetting()
    {
        var inputs = Baseline with
        {
            ReserveWattDays = 50f,
            ReservePercent = 10f,
            SourceStoredWattDays = 100f,
            SourceCapacityWattDays = 1000f,
            SinkRawBalanceExclSelf = -200f,
        };

        Expect.AreEqual(200f, PowerDiodeFlow.TickFlowWatts(inputs));
        Expect.AreEqual(
            0f,
            PowerDiodeFlow.TickFlowWatts(inputs with { ReserveIsPercentage = true })
        );
    }

    [Test]
    public static void ZeroTargetFeedsNothingInAnyMode()
    {
        var inputs = Baseline with
        {
            TargetWatts = 0f,
            SourceRawBalanceExclSelf = 1000f,
            SourceStoredWattDays = 1000f,
            SourceCapacityWattDays = 1000f,
            SinkAcceptWattDays = 1000f,
            SinkCapacityWattDays = 1000f,
        };

        foreach (var mode in Enum.GetValues(typeof(PowerDiodeOperatingMode)))
        {
            Expect.AreEqual(
                0f,
                PowerDiodeFlow.TickFlowWatts(inputs with { Mode = (PowerDiodeOperatingMode)mode }),
                mode.ToString()
            );
        }
    }
}
