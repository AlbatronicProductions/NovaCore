$ErrorActionPreference = 'Stop'
$comparisonRoot = $PSScriptRoot
$driverPath = Join-Path $comparisonRoot 'baseline/tests/NovaCore.Simulation.Tests/Program.cs'
$driver = [IO.File]::ReadAllText($driverPath)
$loop = 'foreach (var (name, test) in tests) { test(); Console.WriteLine($"PASS {name}"); }'
if (!$driver.Contains($loop)) { throw 'Expected banked suite loop missing' }
$driver = $driver.Replace($loop, 'foreach (var (name, test) in tests) { test(); Console.WriteLine($"PASS {name}"); if (name == "Timeline topology") break; }')
$start = '    var before = GC.GetAllocatedBytesForCurrentThread();' + "`n" + '    for (ulong id = 101; id <= 10_000; id++)'
$driver = $driver.Replace("`r`n", "`n")
if (!$driver.Contains($start)) { throw 'Expected allocation window missing' }
$driver = $driver.Replace($start, '    var gcBefore = (GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2));' + "`n" + $start)
$assert = '    Check(GC.GetAllocatedBytesForCurrentThread() == before && allocatedTimeline.ValidateInvariants(), "preallocated timeline operations allocate zero bytes");'
if (!$driver.Contains($assert)) { throw 'Expected banked assertion missing' }
$report = @'
    var measuredBytes = GC.GetAllocatedBytesForCurrentThread() - before;
    var gcAfter = new[] { GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2) };
    var valid = allocatedTimeline.ValidateInvariants();
    Console.WriteLine("AB_RESULT " + System.Text.Json.JsonSerializer.Serialize(new {
        allocatedBytes = measuredBytes, timelineInvariants = valid,
        pendingEvents = allocatedTimeline.PendingCount, completedCancellations = allocatedTimeline.CancelledCount,
        expectedCancellations = 9900, gcBefore = new[] { gcBefore.Item1, gcBefore.Item2, gcBefore.Item3 }, gcAfter, processId = Environment.ProcessId,
        runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
        architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
        testAssembly = typeof(Program).Assembly.Location,
        simulationAssembly = typeof(SimulationTimeline).Assembly.Location,
        configuration = typeof(Program).Assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyConfigurationAttribute), false)
            .Cast<System.Reflection.AssemblyConfigurationAttribute>().Single().Configuration,
        eventRecordBytes = System.Runtime.CompilerServices.Unsafe.SizeOf<ScheduledSimulationEvent>() }));
    Check(measuredBytes == 0, "preallocated timeline operations allocate zero bytes");
    Check(valid, "preallocated timeline invariants");
'@
$driver = $driver.Replace($assert, $report)
foreach ($variant in @('baseline','candidate')) {
    [IO.File]::WriteAllText((Join-Path $comparisonRoot "$variant/tests/NovaCore.Simulation.Tests/Program.cs"), $driver)
}
Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $comparisonRoot 'baseline/tests/NovaCore.Simulation.Tests/Program.cs'), (Join-Path $comparisonRoot 'candidate/tests/NovaCore.Simulation.Tests/Program.cs')
