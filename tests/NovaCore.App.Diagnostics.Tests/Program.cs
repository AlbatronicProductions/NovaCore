using NovaCore.App;

if (args.Length != 1) throw new ArgumentException("Pass an evidence output directory.");
var root = Path.GetFullPath(args[0]);
int checks = 0;
void Check(bool condition, string message) { checks++; if (!condition) throw new Exception(message); }
string ReadLogs(string directory) => string.Concat(Directory.GetFiles(directory, "segment-*.log").Select(path =>
{
    using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    using var reader = new StreamReader(stream);
    return reader.ReadToEnd();
}));

using (var log = new SessionLog(root, background: false, segmentBytes: 1024))
{
    using var original = new StringWriter();
    var tee = new SessionLog.TeeWriter(original, log);
    tee.WriteLine("native present failed: VkResult=-4");
    tee.Write("split "); tee.Write('x'); tee.Write(" span".AsSpan());
    Check(original.ToString().Contains("split x span"), "Original diagnostic destination lost output");
    tee.Flush();
    Check(ReadLogs(log.DirectoryPath).Contains("present failed: VkResult=-4"), "Native failure not retained");
    Check(ReadLogs(log.DirectoryPath).Contains("split x span"), "Partial output not retained");
    Check(File.Exists(Path.Combine(log.DirectoryPath, "session.txt")), "Missing session identity");
    // Log floods cannot accumulate unbounded managed memory; dropped evidence is explicit.
    log.Write(new string('z', 100_000)); log.Flush();
    Check(ReadLogs(log.DirectoryPath).Contains("droppedCharacters="), "Overflow not reported");
    for (var i = 0; i < 12; i++) { log.WriteLine(new string('a', 2048)); log.Flush(); }
    log.WriteLine("latest failure survives rotation"); log.Flush();
    Check(Directory.GetFiles(log.DirectoryPath, "segment-*.log").Length == 4, "Rotation is not bounded");
    Check(ReadLogs(log.DirectoryPath).Contains("latest failure survives rotation"), "Newest evidence lost");
    Check(Directory.GetFiles(log.DirectoryPath, "segment-*.log").All(p => new FileInfo(p).Length < 101_000), "Segment exceeded bounded batch overshoot");
    Check(log.Failure is null, "Unexpected I/O failure");
}
string finalDirectory;
using (var log = new SessionLog(root, background: false))
{
    finalDirectory = log.DirectoryPath;
    Parallel.For(0, 100, i => log.WriteLine($"concurrent-message-{i:D3}"));
    log.WriteLine("Session ended; exitCode=0");
}
var final = ReadLogs(finalDirectory);
for (var i = 0; i < 100; i++) Check(final.Contains($"concurrent-message-{i:D3}"), "Concurrent message lost");
Check(final.Contains("Session ended; exitCode=0"), "Dispose did not drain final marker");
using (var log = new SessionLog(root))
{
    log.WriteLine("background checkpoint before exit");
    Check(SpinWait.SpinUntil(() => ReadLogs(log.DirectoryPath).Contains("background checkpoint before exit"), 5000),
        "Background flush did not retain an active session");
}
using (var log = new SessionLog(root, background: false, segmentBytes: 1024))
{
    log.WriteLine(new string('b', 2048)); log.Flush();
    // Simulate an unavailable next segment, without changing OS permissions.
    Directory.CreateDirectory(Path.Combine(log.DirectoryPath, "segment-1.log"));
    log.WriteLine("rotation blocked"); log.Flush();
    Check(log.Failure is not null, "Storage failure not reported");
    log.WriteLine("must not throw into the application"); log.Flush();
}
using (var log = new SessionLog(root, background: false))
{
    for (var i = 0; i < 100; i++) log.WriteLine("warmup");
    log.Flush();
    var before = GC.GetAllocatedBytesForCurrentThread();
    for (var i = 0; i < 100; i++) log.WriteLine("render-thread-capture");
    Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed capture allocated on the calling thread");
}
Console.WriteLine($"PASS {checks} diagnostic checks; no window or GPU created");
