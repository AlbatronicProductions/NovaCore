# Reproduction only after a new Project Control authorization; this ticket's run budget is exhausted.
# The three functions contain the instrumentation, run, and restoration steps executed for this report.
# No profiler source is duplicated here; see the sibling report for retained observer provenance.
param([ValidateSet('Prepare','Build','Control','Run1','Run2','Run3','Restore')][string]$Phase)
function Prepare-ObservedRunner {
$ErrorActionPreference = 'Stop'
$repo = 'E:\NovaCore'
$scratch = 'E:\NovaCore\.codex\private-propagation-real-runner-observer'
if ((git -C $repo branch --show-current) -ne 'codex/root-linked-private-propagation') { throw 'Wrong branch' }
if (Test-Path -LiteralPath "$scratch\backup") { throw 'Backup already exists; do not repeat preparation' }
$paths = @('tests/NovaCore.Simulation.Tests/Program.cs','tests/NovaCore.Simulation.Tests/OrdinaryAllocationMeasurement.cs')
$paths += @(Get-ChildItem -LiteralPath "$repo\tests\NovaCore.Simulation.Tests\bin\Release","$repo\tests\NovaCore.Simulation.Tests\obj\Release" -File -Recurse | ForEach-Object { [IO.Path]::GetRelativePath($repo,$_.FullName).Replace('\','/') })
$manifest = @()
foreach ($relative in $paths) {
    $from = Join-Path $repo $relative
    $to = Join-Path "$scratch\backup" $relative
    New-Item -ItemType Directory -Force -Path (Split-Path $to) | Out-Null
    Copy-Item -LiteralPath $from -Destination $to
    $manifest += [pscustomobject]@{path=$relative;sha256=(Get-FileHash -LiteralPath $from).Hash;bytes=(Get-Item -LiteralPath $from).Length}
}
$manifest | ConvertTo-Json | Set-Content -LiteralPath "$scratch\backup-manifest.json" -Encoding utf8
$adapter = @'
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

// Temporary adapter to the retained qualified exact-object/context observer.
internal static class RealRunnerAllocationObserver
{
    [DllImport("profiler.dll", EntryPoint="ConfigureCounter")] private static extern int ConfigureCounter(nint entry);
    [DllImport("profiler.dll", EntryPoint="BeginWindow")] private static extern void NativeBegin();
    [DllImport("profiler.dll", EntryPoint="EndWindow")] private static extern void NativeEnd(long delta);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [MethodImpl(MethodImplOptions.NoInlining)] internal static void BeginWindow() => NativeBegin();
    [MethodImpl(MethodImplOptions.NoInlining)] internal static void EndWindow(long delta) => NativeEnd(delta);
    internal static void Prepare()
    {
        _ = GetCurrentThreadId();
        if (Environment.GetEnvironmentVariable("CORECLR_ENABLE_PROFILING") != "1")
            throw new InvalidOperationException("Observer registration missing");
        NativeLibrary.SetDllImportResolver(typeof(RealRunnerAllocationObserver).Assembly,
            (name, assembly, searchPath) => name == "profiler.dll" ? NativeLibrary.Load(Environment.GetEnvironmentVariable("CORECLR_PROFILER_PATH")!) : 0);
        var method = typeof(GC).GetMethod(nameof(GC.GetAllocatedBytesForCurrentThread))!;
        RuntimeHelpers.PrepareMethod(method.MethodHandle);
        var p = method.MethodHandle.GetFunctionPointer();
        if (Marshal.ReadByte(p) == 0xff && Marshal.ReadByte(p, 1) == 0x25) p = Marshal.ReadIntPtr(p + 6 + Marshal.ReadInt32(p, 2));
        if (ConfigureCounter(p) != 1) throw new InvalidOperationException("Unsupported native counter pattern; attribution aborted.");
        BeginWindow(); EndWindow(0);
        Console.WriteLine($"OBSERVER_READY runtime={RuntimeInformation.FrameworkDescription}; managedThread={Environment.CurrentManagedThreadId}; osThread={GetCurrentThreadId()}; pid={Environment.ProcessId}");
    }
}
'@
[IO.File]::WriteAllText("$repo\tests\NovaCore.Simulation.Tests\RealRunnerAllocationObserver.cs", $adapter)
$programPath = "$repo\tests\NovaCore.Simulation.Tests\Program.cs"
$program = [IO.File]::ReadAllText($programPath)
$needle = 'if (args.Contains("--private-propagation-only", StringComparer.Ordinal))'
if (($program.Split($needle)).Count -ne 2) { throw 'Unexpected Program prelude' }
$program = $program.Replace($needle, "RealRunnerAllocationObserver.Prepare();`r`n`r`n$needle")
[IO.File]::WriteAllText($programPath, $program)
$helperPath = "$repo\tests\NovaCore.Simulation.Tests\OrdinaryAllocationMeasurement.cs"
$helper = [IO.File]::ReadAllText($helperPath)
$helper = $helper.Replace('    private bool _active;', "    private bool _active;`r`n    private bool _observed;")
$helper = $helper.Replace('        _gate = gate;', ('        _gate = gate;' + "`r`n" + '        _observed = gate is "speed-presets" or "attitude-integration" or "positive-control";'))
$helper = $helper.Replace('        _before = GC.GetAllocatedBytesForCurrentThread();', "        if (_observed) RealRunnerAllocationObserver.BeginWindow();`r`n        _before = GC.GetAllocatedBytesForCurrentThread();")
$helper = $helper.Replace('        try { bytes = GC.GetAllocatedBytesForCurrentThread() - _before; }', @'
        try
        {
            bytes = GC.GetAllocatedBytesForCurrentThread() - _before;
            if (_observed) { RealRunnerAllocationObserver.EndWindow(bytes); _observed = false; }
        }
'@)
$helper = $helper.Replace('        GC.EndNoGCRegion(); // Failure propagates; no retry or ordinary-counter fallback.', @'
        try { if (_observed) { RealRunnerAllocationObserver.EndWindow(-1); _observed = false; } }
        finally { GC.EndNoGCRegion(); } // Original hard failure and cleanup contract.
'@)
[IO.File]::WriteAllText($helperPath, $helper)
Write-Output "BACKUP files=$($manifest.Count) bytes=$(($manifest|Measure-Object bytes -Sum).Sum)"

}
function Invoke-ObservedProcess {
param([ValidateSet('control','run1','run2','run3')][string]$Label)
$ErrorActionPreference='Stop'
$scratch='E:\NovaCore\.codex\private-propagation-real-runner-observer'
if(Test-Path -LiteralPath "$scratch\$Label.started") { throw 'No repeat execution permitted' }
$psi=[Diagnostics.ProcessStartInfo]::new()
$psi.FileName='C:\Program Files\dotnet\dotnet.exe'
$psi.WorkingDirectory='E:\NovaCore'
$psi.UseShellExecute=$false
$psi.CreateNoWindow=$true
$psi.RedirectStandardOutput=$true
$psi.RedirectStandardError=$true
$psi.ArgumentList.Add('tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll')
if($Label -eq 'control') { $psi.ArgumentList.Add('--ordinary-allocation-control') }
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
$scratch='E:\NovaCore\.codex\private-propagation-real-runner-observer'
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
Remove-Item -LiteralPath 'E:\NovaCore\tests\NovaCore.Simulation.Tests\RealRunnerAllocationObserver.cs' -ErrorAction Stop
foreach($item in $manifest){if((Get-FileHash -LiteralPath (Join-Path $repo $item.path)).Hash -ne $item.sha256){throw "Restoration mismatch: $($item.path)"}}
Write-Output "RESTORED $($manifest.Count) exact source/build-output files; temporary adapter removed"
}
switch ($Phase) {
 'Prepare' { Prepare-ObservedRunner }
 'Build' { Set-Location -LiteralPath 'E:\NovaCore'; dotnet build tests/NovaCore.Simulation.Tests/NovaCore.Simulation.Tests.csproj -c Release --no-restore -p:BuildProjectReferences=false --nologo -v:minimal; if($LASTEXITCODE -ne 0){throw 'Build failed'} }
 'Control' { Invoke-ObservedProcess -Label control }
 'Run1' { Invoke-ObservedProcess -Label run1 }
 'Run2' { Invoke-ObservedProcess -Label run2 }
 'Run3' { Invoke-ObservedProcess -Label run3 }
 'Restore' { Restore-Candidate }
}
