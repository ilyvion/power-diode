namespace PowerDiode;

internal enum PowerDiodeOperatingMode
{
    OneWayValve,
    Overflow,
    TopUp,
}

internal static class PowerDiodeOperatingModeExtensions
{
    internal static Texture2D Icon(this PowerDiodeOperatingMode mode) =>
        mode switch
        {
            PowerDiodeOperatingMode.OneWayValve => Resources.ModeOneWayValveIcon,
            PowerDiodeOperatingMode.Overflow => Resources.ModeOverflowIcon,
            PowerDiodeOperatingMode.TopUp => Resources.ModeTopUpIcon,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
        };

    internal static string Label(this PowerDiodeOperatingMode mode) =>
        mode switch
        {
            PowerDiodeOperatingMode.OneWayValve => "PowerDiode.Mode.OneWayValve".Translate(),
            PowerDiodeOperatingMode.Overflow => "PowerDiode.Mode.Overflow".Translate(),
            PowerDiodeOperatingMode.TopUp => "PowerDiode.Mode.TopUp".Translate(),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
        };

    internal static string Description(this PowerDiodeOperatingMode mode) =>
        mode switch
        {
            PowerDiodeOperatingMode.OneWayValve => "PowerDiode.Mode.OneWayValve.Desc".Translate(),
            PowerDiodeOperatingMode.Overflow => "PowerDiode.Mode.Overflow.Desc".Translate(),
            PowerDiodeOperatingMode.TopUp => "PowerDiode.Mode.TopUp.Desc".Translate(),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
        };
}
