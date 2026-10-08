using DevTools.Testing;

namespace PowerDiode.Tests;

// Regression suite: `targetWatts`/`reserveWattDays` default to the current
// PowerDiodeMod.Settings value, so without forceSave a saved value equal to that default is
// omitted from the save file. Loading it back then re-evaluates the (now possibly different)
// live setting as the value instead of the one actually saved. These tests drive a real Scribe
// save/load cycle (via ilyvion.Laboratory's CustomStream Scribe helpers, so no on-disk save file
// is needed), changing the relevant setting between save and load, and assert the saved value
// survives regardless.
[TestFixture(TestType.MainMenu)]
internal sealed class CompPowerDiodeFeedScribeTests
{
    private sealed class FeedHarness : IExposable
    {
        public readonly CompPowerDiodeFeed Comp = new();

        public void ExposeData() => Comp.PostExposeData();
    }

    private static MemoryStream Save(IExposable target) => ScribeRoundTrip.Save(target);

    private static void Load(MemoryStream memory, IExposable target) =>
        ScribeRoundTrip.Load(memory, target);

    [Test]
    public static void TargetWattsSurvivesMaxWattageChangeBetweenSaveAndLoad()
    {
        var settings = PowerDiodeMod.Settings;
        var originalMax = settings.MaxWattage;
        try
        {
            settings.MaxWattage = 200f;
            var saveHarness = new FeedHarness { Comp = { TargetWatts = 200f } };
            using var memory = Save(saveHarness);

            settings.MaxWattage = 500f;

            var loadHarness = new FeedHarness();
            Load(memory, loadHarness);

            Expect.AreEqual(200f, loadHarness.Comp.TargetWatts);
        }
        finally
        {
            settings.MaxWattage = originalMax;
        }
    }

    [Test]
    public static void ReserveWattDaysSurvivesMinReserveWattDaysChangeBetweenSaveAndLoad()
    {
        var settings = PowerDiodeMod.Settings;
        var originalMin = settings.MinReserveWattDays;
        try
        {
            settings.MinReserveWattDays = 20f;
            var saveHarness = new FeedHarness { Comp = { ReserveWattDays = 20f } };
            using var memory = Save(saveHarness);

            settings.MinReserveWattDays = 50f;

            var loadHarness = new FeedHarness();
            Load(memory, loadHarness);

            Expect.AreEqual(20f, loadHarness.Comp.ReserveWattDays);
        }
        finally
        {
            settings.MinReserveWattDays = originalMin;
        }
    }
}
