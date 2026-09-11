# Evidence reproduction only. This ticket stopped after run 2; new execution requires Project Control authorization.
# Reuses the qualified native observer; no native profiler source or SDK files are duplicated.
param([ValidateSet('Prepare','Build','Run1','Run2','Run3','Restore')][string]$Phase)
function Prepare-ObservedRunner {
$ErrorActionPreference='Stop'
$scratch='E:\NovaCore\.codex\private-propagation-global-gate-observer'
if((git -C E:\NovaCore branch --show-current) -ne 'codex/root-linked-private-propagation'){throw 'Wrong candidate branch'}
if(Test-Path -LiteralPath $scratch){throw 'Scratch exists; no overwrite'}
$repo='E:\NovaCore'
$paths=@('tests/NovaCore.Simulation.Tests/Program.cs','tests/NovaCore.Simulation.Tests/OrdinaryAllocationMeasurement.cs','tests/NovaCore.Simulation.Tests/PhysicalEventEpochTests.cs','tests/NovaCore.Simulation.Tests/ContactGenerationTests.cs','tests/NovaCore.Simulation.Tests/IsolatedContactResponseTests.cs')
$paths+=@(Get-ChildItem -LiteralPath "$repo\tests\NovaCore.Simulation.Tests\bin\Release","$repo\tests\NovaCore.Simulation.Tests\obj\Release" -File -Recurse|ForEach-Object{[IO.Path]::GetRelativePath($repo,$_.FullName).Replace('\','/')})
$manifest=@()
foreach($path in $paths){$from=Join-Path $repo $path;$to=Join-Path "$scratch\backup" $path;New-Item -ItemType Directory -Force -Path (Split-Path $to)|Out-Null;Copy-Item -LiteralPath $from -Destination $to;$manifest+=[pscustomobject]@{path=$path;bytes=(Get-Item -LiteralPath $from).Length;sha256=(Get-FileHash -LiteralPath $from).Hash}}
$manifest|ConvertTo-Json|Set-Content -LiteralPath "$scratch\backup-manifest.json" -Encoding utf8
$instrumentation = @'
{"edits":[{"file":"Program.cs","old":"if (args.Contains(\"--private-propagation-only\", StringComparer.Ordinal))","newText":"GlobalGateAllocationObserver.Prepare();\n\nif (args.Contains(\"--private-propagation-only\", StringComparer.Ordinal))"},{"file":"Program.cs","old":"foreach (var (name, test) in tests) { test(); Console.WriteLine($\"PASS {name}\"); }","newText":"try\n{\n    foreach (var (name, test) in tests) { test(); Console.WriteLine($\"PASS {name}\"); }\n}\ncatch (Exception failure)\n{\n    // Diagnostic-only: qualify visibility in this same process after the stopped group.\n    Console.Error.WriteLine(\"GLOBAL_ORIGINAL_FAILURE \" + failure);\n    OrdinaryAllocationMeasurement.PositiveControl();\n    throw;\n}"},{"file":"OrdinaryAllocationMeasurement.cs","old":"        _before = GC.GetAllocatedBytesForCurrentThread();","newText":"        GlobalGateAllocationObserver.BeginWindow(gate);\n        _before = GC.GetAllocatedBytesForCurrentThread();"},{"file":"OrdinaryAllocationMeasurement.cs","old":"        try { bytes = GC.GetAllocatedBytesForCurrentThread() - _before; }","newText":"        try\n        {\n            bytes = GC.GetAllocatedBytesForCurrentThread() - _before;\n            GlobalGateAllocationObserver.EndWindow(bytes);\n        }"},{"file":"OrdinaryAllocationMeasurement.cs","old":"        GC.EndNoGCRegion(); // Failure propagates; no retry or ordinary-counter fallback.","newText":"        try { GlobalGateAllocationObserver.AbortIfActive(); }\n        finally { GC.EndNoGCRegion(); } // Original hard failure; no fallback."},{"file":"PhysicalEventEpochTests.cs","old":"            var before = GC.GetAllocatedBytesForCurrentThread();","newText":"            GlobalGateAllocationObserver.BeginWindow(\"physical-event-epoch\");\n            var before = GC.GetAllocatedBytesForCurrentThread();"},{"file":"PhysicalEventEpochTests.cs","old":"            allocated = GC.GetAllocatedBytesForCurrentThread() - before;","newText":"            allocated = GC.GetAllocatedBytesForCurrentThread() - before;\n            GlobalGateAllocationObserver.EndWindow(allocated);"},{"file":"ContactGenerationTests.cs","old":"            var before = GC.GetAllocatedBytesForCurrentThread();","newText":"            GlobalGateAllocationObserver.BeginWindow(\"contact-generation\");\n            var before = GC.GetAllocatedBytesForCurrentThread();"},{"file":"ContactGenerationTests.cs","old":"            bytes = GC.GetAllocatedBytesForCurrentThread() - before;","newText":"            bytes = GC.GetAllocatedBytesForCurrentThread() - before;\n            GlobalGateAllocationObserver.EndWindow(bytes);"},{"file":"IsolatedContactResponseTests.cs","old":"            var start = GC.GetAllocatedBytesForCurrentThread();","newText":"            GlobalGateAllocationObserver.BeginWindow(\"isolated-policy\");\n            var start = GC.GetAllocatedBytesForCurrentThread();"},{"file":"IsolatedContactResponseTests.cs","old":"            bytes = GC.GetAllocatedBytesForCurrentThread() - start;","newText":"            bytes = GC.GetAllocatedBytesForCurrentThread() - start;\n            GlobalGateAllocationObserver.EndWindow(bytes);"},{"file":"Program.cs","old":"        var stressBefore = GC.GetAllocatedBytesForCurrentThread();","newText":"        GlobalGateAllocationObserver.BeginWindow(\"long-duration-servicing\");\n        var stressBefore = GC.GetAllocatedBytesForCurrentThread();"},{"file":"Program.cs","old":"        servicingAllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - stressBefore;","newText":"        servicingAllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - stressBefore;\n        GlobalGateAllocationObserver.EndWindow(servicingAllocatedBytes);"},{"file":"Program.cs","old":"        var before = GC.GetAllocatedBytesForCurrentThread();","newText":"        GlobalGateAllocationObserver.BeginWindow(\"servicing-positive-control\");\n        var before = GC.GetAllocatedBytesForCurrentThread();"},{"file":"Program.cs","old":"        allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;","newText":"        allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;\n        GlobalGateAllocationObserver.EndWindow(allocatedBytes);"},{"file":"Program.cs","old":"        var allocationBefore = GC.GetAllocatedBytesForCurrentThread();","newText":"        GlobalGateAllocationObserver.BeginWindow(\"clock-orchestration\");\n        var allocationBefore = GC.GetAllocatedBytesForCurrentThread();"},{"file":"Program.cs","old":"        allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocationBefore;","newText":"        allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocationBefore;\n        GlobalGateAllocationObserver.EndWindow(allocatedBytes);"},{"file":"Program.cs","old":"        var controlBefore = GC.GetAllocatedBytesForCurrentThread();","newText":"        GlobalGateAllocationObserver.BeginWindow(\"orchestration-positive-control\");\n        var controlBefore = GC.GetAllocatedBytesForCurrentThread();"},{"file":"Program.cs","old":"        allocationControlBytes = GC.GetAllocatedBytesForCurrentThread() - controlBefore;","newText":"        allocationControlBytes = GC.GetAllocatedBytesForCurrentThread() - controlBefore;\n        GlobalGateAllocationObserver.EndWindow(allocationControlBytes);"},{"file":"PhysicalEventEpochTests.cs","old":"        finally { GC.EndNoGCRegion(); }","newText":"        finally { try { GlobalGateAllocationObserver.AbortIfActive(); } finally { GC.EndNoGCRegion(); } }"},{"file":"ContactGenerationTests.cs","old":"        finally { GC.EndNoGCRegion(); }","newText":"        finally { try { GlobalGateAllocationObserver.AbortIfActive(); } finally { GC.EndNoGCRegion(); } }"},{"file":"IsolatedContactResponseTests.cs","old":"        finally { GC.EndNoGCRegion(); }","newText":"        finally { try { GlobalGateAllocationObserver.AbortIfActive(); } finally { GC.EndNoGCRegion(); } }"},{"file":"Program.cs","old":"    finally { GC.EndNoGCRegion(); } // An unsuccessful exit throws and fails the test.","newText":"    finally { try { GlobalGateAllocationObserver.AbortIfActive(); } finally { GC.EndNoGCRegion(); } } // Original hard exit failure."},{"file":"Program.cs","old":"    finally { GC.EndNoGCRegion(); }\n    Console.WriteLine($\"SERVICING_CONTROL","newText":"    finally { try { GlobalGateAllocationObserver.AbortIfActive(); } finally { GC.EndNoGCRegion(); } }\n    Console.WriteLine($\"SERVICING_CONTROL"},{"file":"Program.cs","old":"    finally { GC.EndNoGCRegion(); }\n    // Observation occurs after","newText":"    finally { try { GlobalGateAllocationObserver.AbortIfActive(); } finally { GC.EndNoGCRegion(); } }\n    // Observation occurs after"}],"adapter":"using System.Runtime.InteropServices;\nusing System.Runtime.CompilerServices;\n\n// Temporary adapter to the retained qualified exact-object/context observer.\ninternal static class GlobalGateAllocationObserver\n{\n    [DllImport(\"profiler.dll\", EntryPoint=\"ConfigureCounter\")] private static extern int ConfigureCounter(nint entry);\n    [DllImport(\"profiler.dll\", EntryPoint=\"BeginWindow\")] private static extern void NativeBegin();\n    [DllImport(\"profiler.dll\", EntryPoint=\"EndWindow\")] private static extern void NativeEnd(long delta);\n    [DllImport(\"kernel32.dll\")] private static extern uint GetCurrentThreadId();\n    private static bool _active;\n    private static string _gate = \"\";\n    [MethodImpl(MethodImplOptions.NoInlining)] internal static void BeginWindow(string gate)\n    {\n        if (_active) throw new InvalidOperationException(\"Nested observer scope\");\n        _gate = gate; _active = true; NativeBegin();\n    }\n    [MethodImpl(MethodImplOptions.NoInlining)] internal static void EndWindow(long delta)\n    {\n        if (!_active) throw new InvalidOperationException(\"Missing observer scope\");\n        NativeEnd(delta); _active = false;\n        Console.WriteLine($\"GLOBAL_GATE gate={_gate}; counter={delta}; observer=CLOSED\");\n    }\n    internal static void AbortIfActive() { if (_active) EndWindow(-1); }\n    internal static void Prepare()\n    {\n        _ = GetCurrentThreadId();\n        if (Environment.GetEnvironmentVariable(\"CORECLR_ENABLE_PROFILING\") != \"1\")\n            throw new InvalidOperationException(\"Observer registration missing\");\n        NativeLibrary.SetDllImportResolver(typeof(GlobalGateAllocationObserver).Assembly,\n            (name, assembly, searchPath) => name == \"profiler.dll\" ? NativeLibrary.Load(Environment.GetEnvironmentVariable(\"CORECLR_PROFILER_PATH\")!) : 0);\n        var method = typeof(GC).GetMethod(nameof(GC.GetAllocatedBytesForCurrentThread))!;\n        RuntimeHelpers.PrepareMethod(method.MethodHandle);\n        var p = method.MethodHandle.GetFunctionPointer();\n        if (Marshal.ReadByte(p) == 0xff && Marshal.ReadByte(p, 1) == 0x25) p = Marshal.ReadIntPtr(p + 6 + Marshal.ReadInt32(p, 2));\n        if (ConfigureCounter(p) != 1) throw new InvalidOperationException(\"Unsupported native counter pattern; attribution aborted.\");\n        BeginWindow(\"observer-preparation\"); EndWindow(0);\n        Console.WriteLine($\"OBSERVER_READY runtime={RuntimeInformation.FrameworkDescription}; managedThread={Environment.CurrentManagedThreadId}; osThread={GetCurrentThreadId()}; pid={Environment.ProcessId}\");\n    }\n}"}
'@
[IO.File]::WriteAllText("$scratch\instrumentation.json",$instrumentation)
$ErrorActionPreference='Stop'
$scratch='E:\NovaCore\.codex\private-propagation-global-gate-observer'
$root='E:\NovaCore\tests\NovaCore.Simulation.Tests'
$plan=Get-Content -LiteralPath "$scratch\instrumentation.json" -Raw|ConvertFrom-Json
foreach($file in ($plan.edits.file|Select-Object -Unique)) {
    $path=Join-Path $root $file
    $backup=Join-Path "$scratch\backup\tests\NovaCore.Simulation.Tests" $file
    if((Get-FileHash -LiteralPath $path).Hash -ne (Get-FileHash -LiteralPath $backup).Hash){throw 'Unexpected source before instrumentation'}
    $text=[IO.File]::ReadAllText($path).Replace("`r`n","`n")
    foreach($edit in @($plan.edits|Where-Object{$_.file -eq $file})) {
        $pattern='(?m)^'+[regex]::Escape($edit.old)
        if([regex]::Matches($text,$pattern).Count -ne 1){throw "Non-unique hook: $file / $($edit.old)"}
        $replacement=$edit.newText
        $text=[regex]::Replace($text,$pattern,[Text.RegularExpressions.MatchEvaluator]{param($match) $replacement})
    }
    [IO.File]::WriteAllText($path,$text)
}
$adapter=Join-Path $root 'GlobalGateAllocationObserver.cs'
if(Test-Path -LiteralPath $adapter){throw 'Adapter already exists'}
[IO.File]::WriteAllText($adapter,$plan.adapter)
'Instrumented shared helper, five direct zero-required windows, two direct positive controls, and post-failure qualification'


}
function Invoke-ObservedProcess {
param([ValidateSet('run1','run2','run3')][string]$Label)
$ErrorActionPreference='Stop'
$scratch='E:\NovaCore\.codex\private-propagation-global-gate-observer'
if(Test-Path -LiteralPath "$scratch\$Label.started") { throw 'No repeat execution permitted' }
$psi=[Diagnostics.ProcessStartInfo]::new()
$psi.FileName='C:\Program Files\dotnet\dotnet.exe'
$psi.WorkingDirectory='E:\NovaCore'
$psi.UseShellExecute=$false
$psi.CreateNoWindow=$true
$psi.RedirectStandardOutput=$true
$psi.RedirectStandardError=$true
$psi.ArgumentList.Add('tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll')
$unexpected=@($psi.Environment.Keys|Where-Object{$_ -match '^(CORECLR|COMPlus)'})
if($unexpected.Count) { throw "Unexpected inherited diagnostic environment: $unexpected" }
$psi.Environment['CORECLR_ENABLE_PROFILING']='1'
$psi.Environment['CORECLR_PROFILER']='{61D15631-E78F-45D1-A00E-20FC6F1CED24}'
$psi.Environment['CORECLR_PROFILER_PATH']='E:\NovaCore\.codex\sas-allocation-classification\observer\profiler.dll'
$testHash=(Get-FileHash 'E:\NovaCore\tests\NovaCore.Simulation.Tests\bin\Release\net10.0\NovaCore.Simulation.Tests.dll').Hash
$productionHash=(Get-FileHash 'E:\NovaCore\tests\NovaCore.Simulation.Tests\bin\Release\net10.0\NovaCore.Simulation.dll').Hash
$begin=[DateTimeOffset]::Now
[IO.File]::WriteAllText("$scratch\$Label.started",$begin.ToString('O'))
$p=[Diagnostics.Process]::new();$p.StartInfo=$psi
if(-not $p.Start()){throw 'Process failed to start'}
$pidValue=$p.Id
$outTask=$p.StandardOutput.ReadToEndAsync();$errTask=$p.StandardError.ReadToEndAsync()
$p.WaitForExit()
$stdout=$outTask.GetAwaiter().GetResult();$stderr=$errTask.GetAwaiter().GetResult()
[IO.File]::WriteAllText("$scratch\$Label.stdout.txt",$stdout)
[IO.File]::WriteAllText("$scratch\$Label.stderr.txt",$stderr)
$result=[pscustomobject]@{label=$Label;pid=$pidValue;start=$begin.ToString('O');end=[DateTimeOffset]::Now.ToString('O');exit=$p.ExitCode;testHash=$testHash;productionHash=$productionHash;arguments=@($psi.ArgumentList);groups=@([regex]::Matches($stdout,'(?m)^PASS (.+)\r?$')|ForEach-Object{$_.Groups[1].Value.Trim()});stdoutBytes=[Text.Encoding]::UTF8.GetByteCount($stdout);stderrBytes=[Text.Encoding]::UTF8.GetByteCount($stderr)}
$result|ConvertTo-Json -Depth 4|Set-Content -LiteralPath "$scratch\$Label.json" -Encoding utf8
$result|ConvertTo-Json -Depth 4 -Compress
Write-Output $stdout
Write-Output $stderr

}
function Restore-Candidate {
$ErrorActionPreference='Stop'
$repo='E:\NovaCore'
$scratch='E:\NovaCore\.codex\private-propagation-global-gate-observer'
$manifest=@(Get-Content -LiteralPath "$scratch\backup-manifest.json" -Raw|ConvertFrom-Json)
foreach($item in $manifest) {
    $target=[IO.Path]::GetFullPath((Join-Path $repo $item.path))
    if(-not $target.StartsWith('E:\NovaCore\tests\NovaCore.Simulation.Tests\',[StringComparison]::OrdinalIgnoreCase)){throw 'Unexpected restoration path'}
    $backup=Join-Path "$scratch\backup" $item.path
    if((Get-FileHash -LiteralPath $backup).Hash -ne $item.sha256){throw "Corrupt backup: $($item.path)"}
}
$currentOutput=@(Get-ChildItem -LiteralPath "$repo\tests\NovaCore.Simulation.Tests\bin\Release","$repo\tests\NovaCore.Simulation.Tests\obj\Release" -File -Recurse|ForEach-Object{[IO.Path]::GetRelativePath($repo,$_.FullName).Replace('\','/')})
$extras=@($currentOutput|Where-Object{$_ -notin $manifest.path})
if($extras.Count){throw "Unexpected new build outputs require review: $extras"}
foreach($item in $manifest){Copy-Item -LiteralPath (Join-Path "$scratch\backup" $item.path) -Destination (Join-Path $repo $item.path)}
Remove-Item -LiteralPath 'E:\NovaCore\tests\NovaCore.Simulation.Tests\GlobalGateAllocationObserver.cs' -ErrorAction Stop
foreach($item in $manifest){if((Get-FileHash -LiteralPath (Join-Path $repo $item.path)).Hash -ne $item.sha256){throw "Restoration mismatch: $($item.path)"}}
Write-Output "RESTORED $($manifest.Count) exact source/build-output files; temporary adapter removed"
}
switch($Phase){
 'Prepare' { Prepare-ObservedRunner }
 'Build' { Set-Location -LiteralPath 'E:\NovaCore'; if(Get-ChildItem -LiteralPath 'E:\NovaCore\.codex\private-propagation-global-gate-observer' -Filter '*.started'){throw 'No builds during matrix'}; dotnet build tests/NovaCore.Simulation.Tests/NovaCore.Simulation.Tests.csproj -c Release --no-restore -p:BuildProjectReferences=false --nologo -v:minimal; if($LASTEXITCODE -ne 0){throw 'Build failed'} }
 'Run1' { Invoke-ObservedProcess -Label run1 }
 'Run2' { Invoke-ObservedProcess -Label run2 }
 'Run3' { Invoke-ObservedProcess -Label run3 }
 'Restore' { Restore-Candidate }
}
