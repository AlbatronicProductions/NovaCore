using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;

// Evidence only. Immutable input parsing is cold; no derived duty/wrench is cached.
internal sealed class ExactInput(JsonElement input)
{
    private static Rational Read(JsonElement input, string name) => Rational.Parse(input.GetProperty(name).GetString()!);
    internal readonly Rational H = Read(input, "H"), Hp = Read(input, "h"), Mass = Read(input, "m0");
    internal readonly Rational Fx = Read(input, "fx"), Fy = Read(input, "fy"), E = Read(input, "e");
    internal readonly Rational Q = Read(input, "q"), Fuel = Read(input, "fuel");
    internal readonly Rational One = Rational.Parse("1"), MinusOne = Rational.Parse("-1");
    internal readonly Rational Inertia = Rational.Parse("2"), Gravity = Rational.Parse("981/100");
}

internal readonly record struct Prepared(float Ax, float Ay, float Wx, float Wy, float InverseMass, float Dt);

internal static class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Prepared Map(ExactInput input)
    {
        var duty = input.Hp / input.H;
        var fx = input.Fx * duty;
        var fy = input.Fy * duty;
        var torqueX = input.MinusOne * input.E * fy;
        var torqueY = input.E * fx;
        var ax = fx / input.Mass;
        var ay = fy / input.Mass - input.Gravity;
        var wx = torqueX / input.Inertia;
        var wy = torqueY / input.Inertia;
        return new((float)ax.Value, (float)ay.Value, (float)wx.Value, (float)wy.Value,
            (float)(input.One / input.Mass).Value, (float)input.H.Value);
    }

    // Control for the call/input-read/output-consumption boundary, NOT baseline A.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ReadOnlyControl(ExactInput input) => input.Hp.N.Sign + input.Mass.N.Sign;

    private static void Validate(Prepared value, JsonElement expected)
    {
        var p = expected.GetProperty("prepared");
        float[] actual = [value.Ax, value.Ay, value.Wx, value.Wy, value.InverseMass, value.Dt];
        float[] wanted = [p.GetProperty("linearAcceleration")[0].GetSingle(), p.GetProperty("linearAcceleration")[1].GetSingle(),
            p.GetProperty("angularAcceleration")[0].GetSingle(), p.GetProperty("angularAcceleration")[1].GetSingle(),
            p.GetProperty("inverseMass").GetSingle(), expected.GetProperty("backendDt").GetSingle()];
        for (var i = 0; i < actual.Length; i++)
            if (BitConverter.SingleToInt32Bits(actual[i]) != BitConverter.SingleToInt32Bits(wanted[i]))
                throw new InvalidOperationException($"Prepared component {i} differs from retained candidate bits");
    }

    private static int Main(string[] args)
    {
        if (args.Length != 3) throw new ArgumentException("inputs.json off-com-8.json result.json");
        var setupBefore = GC.GetAllocatedBytesForCurrentThread();
        using var inputs = JsonDocument.Parse(File.ReadAllText(args[0]));
        using var expected = JsonDocument.Parse(File.ReadAllText(args[1]));
        var row = inputs.RootElement.GetProperty("cases").EnumerateArray()
            .Single(r => r.GetProperty("input").GetProperty("name").GetString() == "off-com").GetProperty("input");
        var input = new ExactInput(row);
        var setupBytes = GC.GetAllocatedBytesForCurrentThread() - setupBefore;
        // Exact authority witnesses, outside mapping; no resource debit/publication.
        var debit = input.Q * input.Hp;
        var partition = input.Hp + (input.H - input.Hp);
        if (debit.N != input.Fuel.N || debit.D != input.Fuel.D || partition.N != input.H.N || partition.D != input.H.D)
            throw new InvalidOperationException("Retained exact input failed debit/partition checks");

        Prepared warm = default;
        var warmBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 128; i++) warm = Map(input);
        var warmBytes = GC.GetAllocatedBytesForCurrentThread() - warmBefore;
        Validate(warm, expected.RootElement);
        OrdinaryAllocationMeasurement.PositiveControl();
        _ = ReadOnlyControl(input);
        long readBytes;
        int readValue;
        using (var readMeasurement = new OrdinaryAllocationMeasurement("immutable-read-control-not-baseline-A"))
        {
            readValue = ReadOnlyControl(input);
            readBytes = readMeasurement.Complete();
        }
        if (readValue != 2) throw new InvalidOperationException("Control did not read expected input");
        OrdinaryAllocationMeasurement.RequireZero(readBytes, "immutable-read-control");

        var threadBefore = Environment.CurrentManagedThreadId;
        int[] gcBefore = [GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2)];
        long allocated;
        Prepared measured;
        // No reporting, validation, array creation, boxing or other diagnostics inside.
        using (var measurement = new OrdinaryAllocationMeasurement("C-off-com-event-mapping"))
        {
            measured = Map(input);
            allocated = measurement.Complete();
        }
        int[] gcAfter = [GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2)];
        var threadAfter = Environment.CurrentManagedThreadId;
        Validate(measured, expected.RootElement);
        var result = new
        {
            outcome = allocated == 0 ? "C_ALLOCATION_PASS_TIMING_NOT_QUALIFIED" : "STOP_WARMED_CANDIDATE_ALLOCATION_REVISE",
            boundary = "C: exact retained input read, duty/wrench/source-mass mapping, float projections; no solver",
            configuration = "Release", runtime = RuntimeInformation.FrameworkDescription,
            os = RuntimeInformation.OSDescription, architecture = RuntimeInformation.ProcessArchitecture.ToString(),
            processId = Environment.ProcessId, threadBefore, threadAfter, warmOperations = 128, measuredOperations = 1,
            allocationBytes = allocated, exactRequiredBytes = 0, entry = "PASS", exit = "PASS",
            immutableReadControlBytes = readBytes, gcBefore, gcAfter,
            setupManagedCounterBytes = setupBytes, setupBoundary = "read/parse two retained JSON files and immutable exact input construction; not retained storage",
            warmupRawCounterBytes = warmBytes, preparedBitsMatch = true,
            projectedBits = new[] { measured.Ax, measured.Ay, measured.Wx, measured.Wy, measured.InverseMass, measured.Dt }.Select(BitConverter.SingleToInt32Bits).ToArray(),
            solverCalls = 0, timingProcessesExecuted = 0,
            retainedStorageBytes = (long?)null, retainedStorageStatus = "NOT MEASURED: allocation-first stage only"
        };
        File.WriteAllText(args[2], JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }) + "\n");
        Console.WriteLine($"C_RESULT operations=1 bytes={allocated} bits=MATCH result={result.outcome}");
        return allocated == 0 ? 0 : 2;
    }
}
