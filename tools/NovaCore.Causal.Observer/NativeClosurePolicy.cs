internal static class NativeClosurePolicy
{
    // A managed callback can unwind out of the native lease and still leave a
    // zero process exit code. A timed route needs the actual native cleanup proof.
    internal static string? Check(bool started,long cleanup,long rendererStopped,long submitted,long completed,ulong liveBytes)=>
        started&&(cleanup==0||rendererStopped==0||submitted!=completed||liveBytes!=0)
        ?"native route lacks completed renderer cleanup and released allocation proof":null;
}
