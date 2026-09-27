using System.Text.Json;

namespace NovaCore.Diagnostics;

/// <summary>Pre-write bounds for every session file; journal banks never grow.</summary>
public static class OrdinaryStorage
{
    public const int SmallMetadataBytes=4096, SessionMetadataBytes=1024*1024;
    // 1024 open +64 pending +21 phases +4 last events. Each JSON event has
    // 30 20-digit words, bounded indentation/properties; 1284 bytes/event and
    // 8192 bytes of map/scalar envelope fit below 2 MiB. Tested at maximal state.
    public const int RecoveryMetadataBytes=2*1024*1024, FailureBytes=65536;
    public const long MaximumSessionBytes=2*OrdinaryJournal.BankBytes+2*OrdinaryJournal.PageBytes+
        SessionMetadataBytes+RecoveryMetadataBytes+6L*SmallMetadataBytes+FailureBytes;
    public static long Maximum(string name)=>name switch {
        "bank0.bin" or "bank1.bin"=>OrdinaryJournal.BankBytes,
        "head0.bin" or "head1.bin"=>OrdinaryJournal.PageBytes,
        "ownership.lock"=>0,
        "session.json"=>SessionMetadataBytes,
        "recovery.json"=>RecoveryMetadataBytes,
        "observer-failure.txt"=>FailureBytes,
        "observer.json" or "performance.json" or "finalized.json" or "termination.json" or
        "preserve.pin" or "raw-preserve.json"=>SmallMetadataBytes,
        _=>throw new InvalidDataException("Unowned recorder session filename")};
    internal static void Write(string path,byte[] bytes,FileMode mode=FileMode.CreateNew)
    {
        if(bytes.LongLength>Maximum(Path.GetFileName(path)))throw new InvalidDataException("Recorder metadata exceeds qualified storage bound: "+Path.GetFileName(path));
        using var f=new FileStream(path,mode,FileAccess.Write,FileShare.Read);
        f.Write(bytes);f.Flush(true);
    }
    internal static void Json(string path,object value)=>Write(path,JsonSerializer.SerializeToUtf8Bytes(value));
}

public sealed class RecorderStorageExhaustedException(string message):IOException(message);

/// <summary>Player authority is independent of qualification coverage.</summary>
public static class RecorderLaunchPolicy
{
    public const string Exhausted="RECORDER STORAGE EXHAUSTED / MAINTENANCE REQUIRED";
    public static bool RequiresCoverage(IEnumerable<string> args)=>args.Any(a=>a.StartsWith("--qualify",StringComparison.Ordinal)||a.StartsWith("--qualification",StringComparison.Ordinal));
    public static void Continue(bool coverage,bool qualification,Action launch)
    {
        if(!coverage&&qualification)throw new RecorderStorageExhaustedException(Exhausted+" — recorder-required qualification refused before GPU startup.");
        launch();
    }
    public static void Start<T>(Func<T> open,Action<T> configure,bool qualification,Action launch,Action<string> warning) where T:class
    {
        T? recorder=null;
        try{recorder=open();}catch(RecorderStorageExhaustedException e){warning(e.Message+"; UNRECORDED — no recorder-qualified coverage.");}
        if(recorder is not null)configure(recorder);
        Continue(recorder is not null,qualification,launch);
    }
}
