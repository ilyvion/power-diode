namespace PowerDiode.Tests;

// Drives a real Scribe save/load cycle in memory, via ilyvion.Laboratory's CustomStream Scribe
// helpers, so no on-disk save file is needed.
internal static class ScribeRoundTrip
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

    internal static MemoryStream Save(IExposable target)
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

    internal static void Load(MemoryStream memory, IExposable target)
    {
        using var reader = new StreamReader(memory);
        CustomStreamReaderScribeLoader.InitLoading(reader);
        try
        {
            Scribe.loader.curParent = target;
            target.ExposeData();
            Scribe.loader.crossRefs.RegisterForCrossRefResolve(target);
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
}
