namespace PowerDiode;

// Which side of a diode lacks the batteries its operating mode gates the flow on.
internal enum PowerDiodeMissingBatteries
{
    None,
    Intake,
    Outlet,
}

internal static class PowerDiodeMissingBatteriesDetection
{
    // Overflow gates on the intake network's batteries and top-up on the outlet network's, so
    // without them that mode never feeds anything.
    internal static PowerDiodeMissingBatteries MissingBatteries(
        PowerDiodeOperatingMode mode,
        bool intakeHasBatteries,
        bool outletHasBatteries
    ) =>
        mode == PowerDiodeOperatingMode.Overflow && !intakeHasBatteries
            ? PowerDiodeMissingBatteries.Intake
        : mode == PowerDiodeOperatingMode.TopUp && !outletHasBatteries
            ? PowerDiodeMissingBatteries.Outlet
        : PowerDiodeMissingBatteries.None;
}
