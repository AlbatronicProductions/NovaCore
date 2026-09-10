using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;

internal static class PhysicalEventEpochTests
{
    private static int _sink;
    private static void Require(bool value, string contract)
    { if (!value) throw new InvalidOperationException("Physical event epoch: " + contract); }

    private static PhysicalEventEpoch Epoch(long floor, Int128 numerator = default, Int128 denominator = default)
    {
        var status = PhysicalEventEpoch.TryCreate(floor, numerator, denominator == 0 ? 1 : denominator, out var epoch);
        Require(status == PhysicalEventEpochStatus.Success, "fixture construction");
        return epoch;
    }

    internal static void Run()
    {
        Normalization();
        Ordering();
        Serialization();
        Allocation();
    }

    private static void Normalization()
    {
        Require(Unsafe.SizeOf<PhysicalEventEpoch>() == 24, "compact 24-byte value");
        Require(!RuntimeHelpers.IsReferenceOrContainsReferences<PhysicalEventEpoch>(), "no managed references");
        Require(!typeof(PhysicalEventEpoch).IsPublic, "internal identity");
        Require(default(PhysicalEventEpoch) == Epoch(0), "default is canonical zero");
        foreach (var tick in new[] { long.MinValue, -1L, 0L, 1L, long.MaxValue })
        {
            var value = PhysicalEventEpoch.FromCanonical(new(tick));
            Require(value.IsCanonical && value.Numerator == 0 && value.Denominator == 1 &&
                value.TryGetCanonicalInstant(out var restored) && restored.Ticks == tick, "canonical embedding/extraction");
        }
        Require(Epoch(0, 2, 4) == Epoch(0, 1, 2), "reduced equality");
        Require(Epoch(0, 2, 4).GetHashCode() == Epoch(0, 1, 2).GetHashCode(), "equal hash");
        Require(Epoch(0, 32768, 32768) == Epoch(1), "carry");
        Require(Epoch(0, -1, 2) == Epoch(-1, 1, 2), "negative floor");
        Require(Epoch(-1, -3, 2) == Epoch(-3, 1, 2), "negative borrow");
        Require(Epoch(long.MaxValue, -1, 2) == Epoch(long.MaxValue - 1, 1, 2), "upper bound borrow");
        var impact = Epoch(0, 15625, 32768);
        Require(impact.FloorTicks == 0 && impact.Numerator == 15625 && impact.Denominator == 32768,
            "exact retained impact fraction");
        Require(!impact.TryGetCanonicalInstant(out var absent) && absent == default, "no rounded public conversion");
        Require(Epoch(long.MaxValue, 1, 2).CompareTo(Epoch(long.MaxValue)) > 0, "floor-bounded domain, no false ceiling");
        var maximum = (Int128)ulong.MaxValue;
        Require(Epoch(long.MinValue, maximum - 1, maximum).Denominator == ulong.MaxValue, "full UInt64 denominator");
        foreach (var invalid in new[] { Int128.MinValue, (Int128)(-1), (Int128)0 })
            Require(PhysicalEventEpoch.TryCreate(0, 1, invalid, out var failed) == PhysicalEventEpochStatus.InvalidDenominator &&
                failed == default, "invalid denominator rejection");
        Require(PhysicalEventEpoch.TryCreate(0, 1, maximum + 1, out var capacity) == PhysicalEventEpochStatus.DenominatorCapacityExceeded &&
            capacity == default, "denominator capacity");
        foreach (var tuple in new (long Tick, Int128 N, Int128 D)[]
        {
            (long.MaxValue, 1, 1), (long.MinValue, -1, maximum),
            (1, Int128.MaxValue, 1), (-1, Int128.MinValue, 1),
            (0, Int128.MaxValue, 1), (0, Int128.MinValue, 1),
        })
            Require(PhysicalEventEpoch.TryCreate(tuple.Tick, tuple.N, tuple.D, out var failed) == PhysicalEventEpochStatus.TickOverflow &&
                failed == default, "checked tick/intermediate overflow");

        // Independent arbitrary-precision oracle belongs only to tests. It also exercises cancellation
        // where an improper numerator is outside Int64 but the final normalized floor is representable.
        var floors = new[] { long.MinValue, -17L, 0L, 19L, long.MaxValue };
        var numerators = new[] { Int128.MinValue, (Int128)long.MinValue - 1, (Int128)(-7), (Int128)0,
            (Int128)7, (Int128)long.MaxValue + 1, Int128.MaxValue };
        var denominators = new[] { (Int128)1, (Int128)2, (Int128)3, maximum - 1, maximum };
        var checks = 0;
        foreach (var floor in floors) foreach (var n in numerators) foreach (var d in denominators)
        {
            CheckAgainstOracle(floor, n, d); checks++;
        }
        var random = new Random(710423);
        for (var i = 0; i < 1000; i++)
        {
            CheckAgainstOracle(random.NextInt64(), random.NextInt64(long.MinValue, long.MaxValue), random.NextInt64(1, long.MaxValue));
            checks++;
        }
        Console.WriteLine($"Physical epochs: {checks} exact BigInteger normalization controls; error=0; impact=0+15625/32768 ticks");
    }

    private static void CheckAgainstOracle(long floor, Int128 numerator, Int128 denominator)
    {
        var d = BigInteger.CreateChecked(denominator);
        var total = (BigInteger)floor * d + BigInteger.CreateChecked(numerator);
        var expectedFloor = BigInteger.DivRem(total, d, out var remainder);
        if (remainder.Sign < 0) { expectedFloor--; remainder += d; }
        var status = PhysicalEventEpoch.TryCreate(floor, numerator, denominator, out var actual);
        if (expectedFloor < long.MinValue || expectedFloor > long.MaxValue)
        { Require(status == PhysicalEventEpochStatus.TickOverflow && actual == default, "oracle overflow"); return; }
        var divisor = BigInteger.GreatestCommonDivisor(remainder, d);
        Require(status == PhysicalEventEpochStatus.Success && actual.FloorTicks == (long)expectedFloor &&
            (BigInteger)actual.Numerator == remainder / divisor && (BigInteger)actual.Denominator == d / divisor,
            "independent exact normalization oracle");
    }

    private static void Ordering()
    {
        var maximum = (Int128)ulong.MaxValue;
        var highA = Epoch(0, maximum - 1, maximum);
        var highB = Epoch(0, maximum - 2, maximum - 1);
        Require(highA.CompareTo(highB) > 0 && highB.CompareTo(highA) < 0, "UInt128 products differ by one above Int128 range");
        var epochs = new[] { Epoch(long.MinValue), Epoch(-1, 1, 2), Epoch(0), Epoch(0, 1, 4),
            Epoch(0, 15625, 32768), Epoch(0, 1, 2), highB, highA, Epoch(1), Epoch(1, 1, 3), Epoch(long.MaxValue, 1, 2) };
        foreach (var a in epochs) foreach (var b in epochs)
        {
            var an = (BigInteger)a.FloorTicks * a.Denominator + a.Numerator;
            var bn = (BigInteger)b.FloorTicks * b.Denominator + b.Numerator;
            var expected = (an * b.Denominator).CompareTo(bn * a.Denominator);
            Require(Math.Sign(a.CompareTo(b)) == Math.Sign(expected), "independent exact comparison oracle");
            Require((a.CompareTo(b) == 0) == (a == b), "value/comparison equality");
        }
        var keys = new[]
        {
            new PhysicalEventOrderKey(Epoch(0, 3, 4), int.MinValue, new(1), new(30)),
            new PhysicalEventOrderKey(Epoch(0, 1, 4), int.MaxValue, new(8), new(10)),
            new PhysicalEventOrderKey(Epoch(1), 0, new(1), new(40)),
            new PhysicalEventOrderKey(Epoch(1, 1, 4), int.MinValue, new(1), new(50)),
            new PhysicalEventOrderKey(Epoch(0, 2, 4), 0, new(9), new(23)),
            new PhysicalEventOrderKey(Epoch(0, 1, 2), -1, new(99), new(20)),
            new PhysicalEventOrderKey(Epoch(0, 1, 2), 0, new(8), new(22)),
            new PhysicalEventOrderKey(Epoch(0, 1, 2), 0, new(8), new(21)),
        };
        var expectedIds = new ulong[] { 10, 20, 21, 22, 23, 30, 40, 50 };
        var random = new Random(81617);
        for (var replay = 0; replay < 50; replay++)
        {
            // Shuffle arrival order while retaining producer-issued sequence and stable ID.
            for (var i = keys.Length - 1; i > 0; i--) { var j = random.Next(i + 1); (keys[i], keys[j]) = (keys[j], keys[i]); }
            keys.AsSpan().Sort();
            Require(keys.Select(k => k.Id.Value).SequenceEqual(expectedIds), "subevent replay/interleaving/tie order");
        }

        var headers = new List<SimulationEventHeader>();
        foreach (var tick in new[] { long.MinValue, -1L, 0L, long.MaxValue })
        foreach (var priority in new[] { int.MinValue, 0, int.MaxValue })
        foreach (var sequence in new[] { 1UL, ulong.MaxValue })
        foreach (var id in new[] { 1UL, ulong.MaxValue })
            headers.Add(new(new(id), new(tick), priority, new(sequence), SimulationEventKind.Marker));
        foreach (var a in headers) foreach (var b in headers)
        {
            // Independent pre-candidate comparator, not the shared production helper.
            var expected = a.Time.Ticks.CompareTo(b.Time.Ticks);
            if (expected == 0) expected = a.Priority.CompareTo(b.Priority);
            if (expected == 0) expected = a.Sequence.Value.CompareTo(b.Sequence.Value);
            if (expected == 0) expected = a.Id.Value.CompareTo(b.Id.Value);
            Require(Math.Sign(SimulationEventHeaderComparer.Compare(a, b)) == Math.Sign(expected) &&
                Math.Sign(PhysicalEventOrderKey.FromCanonical(a).CompareTo(PhysicalEventOrderKey.FromCanonical(b))) == Math.Sign(expected),
                "banked integral comparator parity");
        }
        Console.WriteLine($"Physical epochs: 50 shuffled replay orders; {headers.Count * headers.Count} integral comparator controls PASS");
    }

    private static void Serialization()
    {
        Span<byte> bytes = stackalloc byte[PhysicalEventEpoch.SerializedSize];
        Span<byte> again = stackalloc byte[PhysicalEventEpoch.SerializedSize];
        var example = Epoch(-1, 1, 2);
        Require(example.TryWrite(bytes), "encode");
        Require(Convert.ToHexString(bytes) == "5048455001010000FFFFFFFFFFFFFFFF01000000000000000200000000000000", "wire golden vector");
        foreach (var value in new[] { default(PhysicalEventEpoch), Epoch(long.MinValue), Epoch(long.MaxValue, 1, 2),
            Epoch(-1, 1, 2), Epoch(0, 15625, 32768), Epoch(0, (Int128)ulong.MaxValue - 1, ulong.MaxValue) })
        {
            value.TryWrite(bytes);
            Require(PhysicalEventEpoch.TryRead(bytes, out var restored) == PhysicalEventEpochStatus.Success && restored == value,
                "exact roundtrip");
            restored.TryWrite(again);
            Require(bytes.SequenceEqual(again), "roundtrip identical bytes");
        }
        Span<byte> shortBuffer = stackalloc byte[31]; shortBuffer.Fill(0xA5);
        Require(!example.TryWrite(shortBuffer) && shortBuffer.IndexOfAnyExcept((byte)0xA5) == -1, "short write unchanged");
        foreach (var length in new[] { 0, 31, 33 })
            Require(PhysicalEventEpoch.TryRead(new byte[length], out var failed) == PhysicalEventEpochStatus.InvalidEncoding && failed == default,
                "exact encoded length");

        for (var mode = 0; mode < 12; mode++)
        {
            example.TryWrite(bytes);
            var expected = PhysicalEventEpochStatus.InvalidEncoding;
            switch (mode)
            {
                case 0: bytes[0] ^= 1; break;
                case 1: bytes[4] = 2; expected = PhysicalEventEpochStatus.UnsupportedVersion; break;
                case 2: bytes[5] = 2; expected = PhysicalEventEpochStatus.UnsupportedKind; break;
                case 3: bytes[6] = 1; break;
                case 4: BinaryPrimitives.WriteUInt64LittleEndian(bytes[24..], 0); break;
                case 5: BinaryPrimitives.WriteUInt64LittleEndian(bytes[16..], 2); break;
                case 6: BinaryPrimitives.WriteUInt64LittleEndian(bytes[16..], 2); BinaryPrimitives.WriteUInt64LittleEndian(bytes[24..], 4); break;
                case 7: bytes[5] = 0; break;
                case 8: BinaryPrimitives.WriteUInt64LittleEndian(bytes[16..], 0); break;
                case 9: bytes[5] = 0; BinaryPrimitives.WriteUInt64LittleEndian(bytes[16..], 0); break;
                case 10: bytes[5] = 255; expected = PhysicalEventEpochStatus.UnsupportedKind; break;
                case 11: bytes[7] = 1; break;
            }
            Require(PhysicalEventEpoch.TryRead(bytes, out var rejected) == expected && rejected == default, "malformed/certified encoding refusal");
        }
    }

    private static PhysicalEventOrderKey[] Workload()
    {
        var result = new PhysicalEventOrderKey[128];
        for (var i = 0; i < result.Length; i++)
            result[i] = new(Epoch((i % 3) - 1, 127 - i, 131), i % 7, new((ulong)i + 1), new((ulong)i + 1));
        return result;
    }

    private static int Batch(int mode, int count, PhysicalEventOrderKey[] source, PhysicalEventOrderKey[] scratch)
    {
        var sum = 0;
        if (mode == 0)
            for (var i = 0; i < count; i++) sum += source[i & 127].Epoch.CompareTo(source[(i * 17 + 3) & 127].Epoch);
        else if (mode == 1)
            for (var i = 0; i < count; i++)
            {
                var status = PhysicalEventEpoch.TryCreate(-19, (Int128)(i - 700), 32768, out var epoch);
                sum ^= (int)status ^ epoch.GetHashCode();
            }
        else
            for (var i = 0; i < count; i++)
            {
                source.CopyTo(scratch, 0);
                scratch.AsSpan().Sort();
                sum ^= (int)scratch[i & 127].Id.Value;
            }
        return sum;
    }

    private static void Allocation()
    {
        var source = Workload(); var scratch = new PhysicalEventOrderKey[source.Length];
        for (var mode = 0; mode < 3; mode++) _sink = Batch(mode, 4096, source, scratch);
        Require(GC.TryStartNoGCRegion(1 << 20, disallowFullBlockingGC: true), "existing test-only GC accounting boundary");
        long allocated;
        try
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var mode = 0; mode < 3; mode++) _sink = Batch(mode, 1024, source, scratch);
            Span<byte> bytes = stackalloc byte[PhysicalEventEpoch.SerializedSize];
            for (var i = 0; i < 1024; i++)
            {
                source[i & 127].Epoch.TryWrite(bytes);
                _sink ^= (int)PhysicalEventEpoch.TryRead(bytes, out _);
            }
            allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        }
        finally { GC.EndNoGCRegion(); }
        Require(allocated == 0, $"warmed compare/construction/sort/serialization allocates {allocated} bytes, expected zero");
        Console.WriteLine($"Physical epochs: warmed combined managed allocation={allocated} bytes; sink={_sink}");
    }

    internal static void Performance()
    {
        var source = Workload(); var scratch = new PhysicalEventOrderKey[source.Length];
        var samples = new double[21];
        for (var mode = 0; mode < 3; mode++)
        {
            var count = mode == 2 ? 128 : 16384;
            for (var warm = 0; warm < 4; warm++) _sink = Batch(mode, count, source, scratch);
            for (var sample = 0; sample < samples.Length; sample++)
            {
                var start = Stopwatch.GetTimestamp();
                _sink = Batch(mode, count, source, scratch);
                samples[sample] = (Stopwatch.GetTimestamp() - start) * (1e9 / Stopwatch.Frequency) / count;
            }
            Array.Sort(samples);
            Console.WriteLine(FormattableString.Invariant($"Physical epoch mode={mode} count={count} samples=21 medianNs={samples[10]:F3} p95Ns={samples[19]:F3} p99Ns={samples[20]:F3}"));
        }
        Allocation();
    }
}
