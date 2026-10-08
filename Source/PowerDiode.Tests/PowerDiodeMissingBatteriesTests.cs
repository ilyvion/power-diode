using DevTools.Testing;

namespace PowerDiode.Tests;

[TestFixture(TestType.MainMenu)]
internal sealed class PowerDiodeMissingBatteriesTests
{
    [Test]
    public static void OverflowNeedsIntakeBatteries()
    {
        Expect.AreEqual(
            PowerDiodeMissingBatteries.Intake,
            PowerDiodeMissingBatteriesDetection.MissingBatteries(
                PowerDiodeOperatingMode.Overflow,
                intakeHasBatteries: false,
                outletHasBatteries: true
            )
        );
        Expect.AreEqual(
            PowerDiodeMissingBatteries.None,
            PowerDiodeMissingBatteriesDetection.MissingBatteries(
                PowerDiodeOperatingMode.Overflow,
                intakeHasBatteries: true,
                outletHasBatteries: false
            )
        );
    }

    [Test]
    public static void TopUpNeedsOutletBatteries()
    {
        Expect.AreEqual(
            PowerDiodeMissingBatteries.Outlet,
            PowerDiodeMissingBatteriesDetection.MissingBatteries(
                PowerDiodeOperatingMode.TopUp,
                intakeHasBatteries: true,
                outletHasBatteries: false
            )
        );
        Expect.AreEqual(
            PowerDiodeMissingBatteries.None,
            PowerDiodeMissingBatteriesDetection.MissingBatteries(
                PowerDiodeOperatingMode.TopUp,
                intakeHasBatteries: false,
                outletHasBatteries: true
            )
        );
    }

    [Test]
    public static void OneWayValveNeedsNoBatteries() =>
        Expect.AreEqual(
            PowerDiodeMissingBatteries.None,
            PowerDiodeMissingBatteriesDetection.MissingBatteries(
                PowerDiodeOperatingMode.OneWayValve,
                intakeHasBatteries: false,
                outletHasBatteries: false
            )
        );
}
