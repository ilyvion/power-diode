using RimTestRedux;

namespace PowerDiode.Tests;

// Regression suite: `targetWatts`/`reserveWattDays` default to the current
// PowerDiodeMod.Settings value, so without forceSave a saved value equal to that default is
// omitted from the save file. Loading it back then re-evaluates the (now possibly different)
// live setting as the value instead of the one actually saved. These tests drive a real Scribe
// save/load cycle (via ilyvion.Laboratory's CustomStream Scribe helpers, so no on-disk save file
// is needed), changing the relevant setting between save and load, and assert the saved value
// survives regardless.
[TestSuite]
internal static class CompPowerDiodeFeedScribeTests
{
    private sealed class NonClosingStream(Stream inner) : Stream
    {
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => inner.CanSeek;
        public override bool CanWrite => inner.CanWrite;
        public override long Length => inner.Length;

        public override long Position
        {
            get => inner.Position;
            set => inner.Position = value;
        }

        public override void Flush() => inner.Flush();

        public override int Read(byte[] buffer, int offset, int count) =>
            inner.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);

        public override void SetLength(long value) => inner.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) =>
            inner.Write(buffer, offset, count);
    }

    private sealed class FeedHarness : IExposable
    {
        public readonly CompPowerDiodeFeed Comp = new();

        public void ExposeData() => Comp.PostExposeData();
    }

    private static MemoryStream Save(IExposable target)
    {
        var memory = new MemoryStream();
        using (var nonClosing = new NonClosingStream(memory))
        {
            CustomStreamScribeSaver.InitSaving(nonClosing, "root");
            try
            {
                target.ExposeData();
                Scribe.saver.FinalizeSaving();
            }
            finally
            {
                if (Scribe.mode != LoadSaveMode.Inactive)
                {
                    Scribe.ForceStop();
                }
            }
        }
        memory.Position = 0;
        return memory;
    }

    private static void Load(MemoryStream memory, IExposable target)
    {
        using var reader = new StreamReader(memory);
        CustomStreamReaderScribeLoader.InitLoading(reader);
        try
        {
            Scribe.loader.curParent = target;
            target.ExposeData();
            Scribe.loader.FinalizeLoading();
        }
        finally
        {
            if (Scribe.mode != LoadSaveMode.Inactive)
            {
                Scribe.ForceStop();
            }
        }
    }

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

            Assert.That(loadHarness.Comp.TargetWatts).Is.EqualTo(200f);
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

            Assert.That(loadHarness.Comp.ReserveWattDays).Is.EqualTo(20f);
        }
        finally
        {
            settings.MinReserveWattDays = originalMin;
        }
    }
}
