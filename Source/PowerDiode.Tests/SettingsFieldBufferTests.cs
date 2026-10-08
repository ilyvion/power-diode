using DevTools.Testing;

namespace PowerDiode.Tests;

[TestFixture(TestType.MainMenu)]
internal sealed class SettingsFieldBufferTests
{
    // Regression guard: the settings window's text fields used to keep showing the default values
    // after a restart, since a non-null buffer is never refreshed from the loaded value.
    [Test]
    public static void LoadingSettingsClearsTheTextFieldBuffers()
    {
        Settings.minWattageBuffer = "0";
        Settings.maxWattageBuffer = "5000";
        Settings.minReserveWattDaysBuffer = "10";
        Settings.maxReserveWattDaysBuffer = "500";

        var saved = new Settings
        {
            MinWattage = 100f,
            MaxWattage = 3000f,
            MinReserveWattDays = 20f,
            MaxReserveWattDays = 400f,
        };
        var loaded = new Settings();
        ScribeRoundTrip.Load(ScribeRoundTrip.Save(saved), loaded);

        Expect.AreEqual(3000f, loaded.MaxWattage);
        Expect.IsNull(Settings.minWattageBuffer);
        Expect.IsNull(Settings.maxWattageBuffer);
        Expect.IsNull(Settings.minReserveWattDaysBuffer);
        Expect.IsNull(Settings.maxReserveWattDaysBuffer);
    }
}
