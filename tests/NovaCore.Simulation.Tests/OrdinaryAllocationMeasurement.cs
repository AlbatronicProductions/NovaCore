using System.Runtime.CompilerServices;

// Test-only measurement of warmed managed work, not a production GC policy.
// Reuse the qualified 1 MiB reservation: fixtures are already allocated and the
// workload's allowance remains exactly zero. Do not scale this by iteration count.
internal ref struct OrdinaryAllocationMeasurement
{
    private const long ReservationBytes = 1L << 20;
    private readonly string _gate;
    private readonly long _before;
    private bool _active;

    internal OrdinaryAllocationMeasurement(string gate)
    {
        _gate = gate;
        _active = false;
        if (!GC.TryStartNoGCRegion(ReservationBytes, disallowFullBlockingGC: true))
            throw new InvalidOperationException($"{gate}: no-GC measurement entry failed");
        _active = true;
        _before = GC.GetAllocatedBytesForCurrentThread();
    }

    // Close the counter before cleanup or reporting; return the exact delta.
    // The using declaration at each call site also guarantees cleanup if work throws.
    internal long Complete()
    {
        if (!_active) throw new InvalidOperationException($"{_gate}: measurement is not active");
        long bytes;
        try { bytes = GC.GetAllocatedBytesForCurrentThread() - _before; }
        finally { Dispose(); }
        Console.WriteLine($"ORDINARY_ALLOCATION gate={_gate} bytes={bytes} entry=PASS exit=PASS");
        return bytes;
    }

    public void Dispose()
    {
        if (!_active) return;
        _active = false;
        GC.EndNoGCRegion(); // Failure propagates; no retry or ordinary-counter fallback.
    }

    internal static void RequireZero(long bytes, string gate)
    {
        if (bytes != 0) throw new InvalidOperationException($"{gate}: expected zero managed allocation, actual={bytes}");
    }

    internal static void PositiveControl()
    {
        GC.KeepAlive(AllocateControl()); // Warm only the independent control body.
        using var measurement = new OrdinaryAllocationMeasurement("positive-control");
        var value = AllocateControl();
        var bytes = measurement.Complete();
        GC.KeepAlive(value);
        if (bytes <= 0) throw new InvalidOperationException($"positive-control: expected nonzero allocation, actual={bytes}");
        Console.WriteLine($"ORDINARY_CONTROL type=System.Byte[] length=128 bytes={bytes}");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte[] AllocateControl() => new byte[128];
}
