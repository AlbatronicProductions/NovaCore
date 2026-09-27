using System.Diagnostics;
using System.Text;

namespace NovaCore.App;

/// <summary>Bounded in-memory capture; disk writes and memory sampling run off the render thread.</summary>
internal sealed class SessionLog : TextWriter
{
    private const int BufferCharacters = 32 * 1024;
    private readonly object pendingLock = new(), diskLock = new();
    private char[] pending = new char[BufferCharacters], draining = new char[BufferCharacters];
    private int pendingCount;
    private long dropped;
    private readonly long segmentBytes;
    private readonly System.Threading.Timer? timer;
    private StreamWriter writer;
    private int segment;
    private bool disposed;
    private long nextMemorySample;
    public string DirectoryPath { get; }
    public string? Failure { get; private set; }
    public override Encoding Encoding => Encoding.UTF8;

    internal SessionLog(string root, bool background = true, long segmentBytes = 2 * 1024 * 1024)
    {
        if (segmentBytes < 1024) throw new ArgumentOutOfRangeException(nameof(segmentBytes));
        this.segmentBytes = segmentBytes;
        DirectoryPath = Path.Combine(root, $"{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}-{Environment.ProcessId}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(DirectoryPath);
        File.WriteAllText(Path.Combine(DirectoryPath, "session.txt"),
            $"NovaCore session started UTC={DateTime.UtcNow:O}\nprocess={Environment.ProcessPath}\n" +
            $"base={AppContext.BaseDirectory}\nos={Environment.OSVersion}\nframework={Environment.Version}\n" +
            $"appModule={typeof(SessionLog).Module.ModuleVersionId}\n" +
            "Four rotating log segments; batch timestamps establish ordering. Missing Session ended is not a crash diagnosis.\n");
        writer = OpenSegment();
        if (background) timer = new System.Threading.Timer(_ => Flush(), null, 1000, 1000);
    }

    private StreamWriter OpenSegment() => new(new FileStream(
        Path.Combine(DirectoryPath, $"segment-{segment}.log"), FileMode.Create,
        FileAccess.Write, FileShare.Read, 4096), new UTF8Encoding(false));

    public override void Write(char value)
    {
        Span<char> single = stackalloc char[1]; single[0] = value; Write(single);
    }
    public override void Write(string? value) { if (value is not null) Write(value.AsSpan()); }
    public override void WriteLine(string? value)
    {
        lock (pendingLock) { Write(value); Write(NewLine); }
    }
    public override void Write(ReadOnlySpan<char> value)
    {
        lock (pendingLock)
        {
            if (disposed || Failure is not null) return;
            var length = Math.Min(value.Length, pending.Length - pendingCount);
            value[..length].CopyTo(pending.AsSpan(pendingCount));
            pendingCount += length;
            dropped += value.Length - length;
        }
    }

    public override void Flush()
    {
        lock (diskLock)
        {
            if (disposed || Failure is not null) return;
            int count; long lost;
            lock (pendingLock)
            {
                (pending, draining) = (draining, pending);
                count = pendingCount; lost = dropped; pendingCount = 0; dropped = 0;
            }
            try
            {
                if (writer.BaseStream.Length >= segmentBytes)
                {
                    writer.Dispose(); segment = (segment + 1) % 4; writer = OpenSegment();
                }
                writer.WriteLine($"\n[batch UTC={DateTime.UtcNow:O}]");
                writer.Write(draining.AsSpan(0, count));
                if (lost != 0) writer.WriteLine($"\n[diagnostic buffer overflow; droppedCharacters={lost}]");
                if (Environment.TickCount64 >= nextMemorySample)
                {
                    nextMemorySample = Environment.TickCount64 + 10_000;
                    using var process = Process.GetCurrentProcess();
                    writer.WriteLine($"\n[process heartbeat privateBytes={process.PrivateMemorySize64}; workingSetBytes={process.WorkingSet64}]");
                }
                writer.Flush();
                // Background flush improves survival of a reset; storage hardware can still lose the last batch.
                ((FileStream)writer.BaseStream).Flush(flushToDisk: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
            {
                Failure = ex.Message; // A diagnostic storage failure must not crash the render thread.
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            timer?.Dispose();
            lock (diskLock)
            {
                if (disposed) return;
                Flush();
                lock (pendingLock) disposed = true;
                try { writer.Dispose(); } catch (IOException ex) { Failure ??= ex.Message; }
            }
        }
        base.Dispose(disposing);
    }

    internal sealed class TeeWriter(TextWriter original, SessionLog log) : TextWriter
    {
        public override Encoding Encoding => original.Encoding;
        public override void Write(char value) { original.Write(value); log.Write(value); }
        public override void Write(string? value) { original.Write(value); log.Write(value); }
        public override void Write(ReadOnlySpan<char> value) { original.Write(value); log.Write(value); }
        public override void WriteLine(string? value) { original.WriteLine(value); log.WriteLine(value); }
        public override void Flush() { original.Flush(); log.Flush(); }
    }
}
