using DevTools.Testing;

namespace PowerDiode.Tests;

[TestFixture(TestType.MainMenu)]
internal sealed class SettingsScribeTests
{
    private sealed class Empty : IExposable
    {
        public void ExposeData() { }
    }

    [Test]
    public static void EveryFieldSurvivesARoundTrip()
    {
        var saved = new Settings
        {
            MinWattage = 100f,
            MaxWattage = 3000f,
            MinReserveWattDays = 20f,
            MaxReserveWattDays = 400f,
            ReserveIsPercentage = true,
        };
        var loaded = new Settings();

        ScribeRoundTrip.Load(ScribeRoundTrip.Save(saved), loaded);

        Expect.AreEqual(100f, loaded.MinWattage);
        Expect.AreEqual(3000f, loaded.MaxWattage);
        Expect.AreEqual(20f, loaded.MinReserveWattDays);
        Expect.AreEqual(400f, loaded.MaxReserveWattDays);
        Expect.IsTrue(loaded.ReserveIsPercentage);
    }

    [Test]
    public static void AnEmptySaveLoadsTheDefaults()
    {
        var loaded = new Settings
        {
            MinWattage = 100f,
            MaxWattage = 3000f,
            MinReserveWattDays = 20f,
            MaxReserveWattDays = 400f,
            ReserveIsPercentage = true,
        };

        ScribeRoundTrip.Load(ScribeRoundTrip.Save(new Empty()), loaded);

        Expect.AreEqual(Settings.DefaultMinWattage, loaded.MinWattage);
        Expect.AreEqual(Settings.DefaultMaxWattage, loaded.MaxWattage);
        Expect.AreEqual(Settings.DefaultMinReserveWattDays, loaded.MinReserveWattDays);
        Expect.AreEqual(Settings.DefaultMaxReserveWattDays, loaded.MaxReserveWattDays);
        Expect.IsFalse(loaded.ReserveIsPercentage);
    }
}
