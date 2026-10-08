using System.Text;
using System.Text.RegularExpressions;
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

    private static FeedHarness MakeHarness(
        PowerDiodeOperatingMode mode,
        float reservePercent,
        float overflowThresholdPercent,
        float topUpThresholdPercent
    ) =>
        new()
        {
            Comp =
            {
                OperatingMode = mode,
                ReservePercent = reservePercent,
                OverflowThresholdPercent = overflowThresholdPercent,
                TopUpThresholdPercent = topUpThresholdPercent,
            },
        };

    private static void ExpectFields(
        FeedHarness harness,
        PowerDiodeOperatingMode mode,
        float reservePercent,
        float overflowThresholdPercent,
        float topUpThresholdPercent
    )
    {
        Expect.AreEqual(mode, harness.Comp.OperatingMode, "operating mode");
        Expect.AreEqual(reservePercent, harness.Comp.ReservePercent, "reserve percent");
        Expect.AreEqual(
            overflowThresholdPercent,
            harness.Comp.OverflowThresholdPercent,
            "overflow threshold"
        );
        Expect.AreEqual(
            topUpThresholdPercent,
            harness.Comp.TopUpThresholdPercent,
            "top-up threshold"
        );
    }

    [Test]
    public static void NonDefaultModeAndPercentagesSurviveRoundTrip()
    {
        using var memory = Save(MakeHarness(PowerDiodeOperatingMode.TopUp, 42f, 35f, 65f));

        var loadHarness = MakeHarness(PowerDiodeOperatingMode.OneWayValve, 10f, 80f, 20f);
        Load(memory, loadHarness);

        ExpectFields(loadHarness, PowerDiodeOperatingMode.TopUp, 42f, 35f, 65f);
    }

    // Default values are left out of the save, so they must load back as the same defaults.
    [Test]
    public static void DefaultModeAndPercentagesSurviveRoundTrip()
    {
        using var memory = Save(MakeHarness(PowerDiodeOperatingMode.OneWayValve, 10f, 80f, 20f));

        var loadHarness = MakeHarness(PowerDiodeOperatingMode.TopUp, 42f, 35f, 65f);
        Load(memory, loadHarness);

        ExpectFields(loadHarness, PowerDiodeOperatingMode.OneWayValve, 10f, 80f, 20f);
    }

    [Test]
    public static void SaveWithoutModeOrPercentagesLoadsDefaults()
    {
        using var saved = Save(MakeHarness(PowerDiodeOperatingMode.TopUp, 42f, 35f, 65f));
        using var reader = new StreamReader(saved);
        var xml = reader.ReadToEnd();
        const string FieldsPattern =
            @"<(operatingMode|reservePercent|overflowThresholdPercent|topUpThresholdPercent)>[^<]*</\1>";
        Expect.AreEqual(4, Regex.Matches(xml, FieldsPattern).Count, "fields in the save");
        using var edited = new MemoryStream(
            Encoding.UTF8.GetBytes(Regex.Replace(xml, FieldsPattern, ""))
        );

        var loadHarness = MakeHarness(PowerDiodeOperatingMode.TopUp, 42f, 35f, 65f);
        Load(edited, loadHarness);

        ExpectFields(loadHarness, PowerDiodeOperatingMode.OneWayValve, 10f, 80f, 20f);
    }
}
