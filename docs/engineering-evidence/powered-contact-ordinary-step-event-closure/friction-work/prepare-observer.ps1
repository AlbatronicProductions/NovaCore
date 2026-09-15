# Generates only disposable diagnostic source; does not run a world.
$ErrorActionPreference = 'Stop'
$repo = 'E:\NovaCore'
$ev = Join-Path $repo 'docs\engineering-evidence\powered-contact-ordinary-step-event-closure\friction-work'
$prior = Split-Path $ev
$scratch = Join-Path $repo 'build\powered-contact-friction-work'
if (Test-Path -LiteralPath $scratch) { throw 'Scratch already exists; inspect rather than overwrite' }
if ((Get-FileHash -LiteralPath (Join-Path $prior 'Program.cs')).Hash -ne '7DC5838082E1C4BEA99D270CBEB26A20E47CEA9999A15716A52EDCEF486B07C7') { throw 'Original harness changed' }
New-Item -ItemType Directory -Path $scratch | Out-Null
$raw = Join-Path $scratch 'ContactConvexTypes.upstream.cs.txt'
Invoke-WebRequest 'https://raw.githubusercontent.com/bepu/bepuphysics2/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/Constraints/Contact/ContactConvexTypes.cs' -OutFile $raw
if ((Get-FileHash -LiteralPath $raw).Hash -ne 'B259BD56D6EF9405FC98A38E97A9D6CA154CDA11CB908C901800298B7D771DA5') { throw 'Pinned source mismatch' }
$source = [IO.File]::ReadAllText($raw)
$start = $source.IndexOf('    public struct Contact4OneBodyFunctions :')
$end = $source.IndexOf('    /// <summary>', $start)
$original = $source.Substring($start, $end-$start)
$lines = $original -split "`n"
$out = [Collections.Generic.List[string]]::new()
$pending = ''
$calls = 0
foreach ($line in $lines) {
    $out.Add($line.Replace('Contact4OneBodyFunctions', 'ObservedContact4Functions').Replace('FrictionHelpers.', 'ObservedFrictionHelpers.'))
    if ($line -match 'public static void (WarmStart|Solve)\(') { $pending = $Matches[1] }
    elseif ($pending -and $line.Trim() -eq '{') {
        $out.Add('            WorkObserver.Begin(' + $(if($pending -eq 'WarmStart'){'true'}else{'false'}) + ');')
        $pending = ''
    }
    if ($line -match '^\s+(TangentFrictionOneBody|PenetrationLimitOneBody|TwistFrictionOneBody)\.(WarmStart|Solve)\(') {
        $row = if ($Matches[1] -eq 'TangentFrictionOneBody') { 0 } elseif ($Matches[1] -eq 'TwistFrictionOneBody') { 5 } else {
            if ($line -notmatch 'accumulatedImpulses.Penetration([0-3])') { throw 'Normal row parse failed' }; 1+[int]$Matches[1]
        }
        $out.Insert($out.Count-1,"            WorkObserver.Record($row, 0, wsvA, inertiaA, accumulatedImpulses);")
        $out.Add("            WorkObserver.Record($row, 1, wsvA, inertiaA, accumulatedImpulses);")
        $calls++
    }
}
if ($calls -ne 12) { throw 'Unexpected native call count' }
$instrumented = $out -join "`n"
$recovered = (($instrumented -split "`n" | Where-Object { $_ -notmatch '^\s+WorkObserver\.(Begin|Record)' }) -join "`n").Replace('ObservedContact4Functions','Contact4OneBodyFunctions').Replace('ObservedFrictionHelpers.','FrictionHelpers.')
if ($recovered -cne $original) { throw 'Arithmetic source changed' }
$helperAt = $source.IndexOf('in Vector3Wide offsetA0, in Vector3Wide offsetA1, in Vector3Wide offsetA2, in Vector3Wide offsetA3,')
$helperStart = $source.LastIndexOf('        [MethodImpl', $helperAt)
$helperEnd = $source.IndexOf("`n        }", $helperAt) + "`n        }".Length
$helper = $source.Substring($helperStart, $helperEnd-$helperStart)
$header = "using BepuPhysics;`nusing BepuPhysics.Constraints;`nusing BepuPhysics.Constraints.Contact;`nusing BepuUtilities;`nusing System.Numerics;`nusing System.Runtime.CompilerServices;`n"
$processor = 'internal sealed class ObservedContact4Processor : OneBodyContactTypeProcessor<Contact4OneBodyPrestepData, Contact4AccumulatedImpulses, ObservedContact4Functions> {}'
[IO.File]::WriteAllText((Join-Path $scratch 'ObservedFunctions.cs'), $header+$instrumented+"`ninternal static class ObservedFrictionHelpers {`n"+$helper+"`n}`n"+$processor)
Copy-Item -LiteralPath (Join-Path $ev 'Observer.cs') -Destination $scratch
$program = [IO.File]::ReadAllText((Join-Path $prior 'Program.cs'))
$target = '            simulation.Timestep(backendDt);'
if ([regex]::Matches($program,[regex]::Escape($target)).Count -ne 1) { throw 'Target step not unique' }
$replacement = @'
            var nativeProcessor = simulation.Solver.TypeProcessors[3];
            var observedProcessor = new ObservedContact4Processor();
            observedProcessor.Initialize(3);
            simulation.Solver.TypeProcessors[3] = observedProcessor;
            try { simulation.Timestep(backendDt); }
            finally { simulation.Solver.TypeProcessors[3] = nativeProcessor; }
'@
$program = $program.Replace($target, $replacement)
$write = '            File.WriteAllText(args[2],JsonSerializer.Serialize(result,Options)+Environment.NewLine);'
if (-not $program.Contains($write)) { throw 'Report anchor missing' }
$program = $program.Replace($write, $write+"`n            WorkObserver.Write(args[2]+"+'".trace.json");')
[IO.File]::WriteAllText((Join-Path $scratch 'Program.cs'), $program)
$project = @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><NovaCoreUsesBepu>true</NovaCoreUsesBepu><EnableDefaultCompileItems>false</EnableDefaultCompileItems><AllowUnsafeBlocks>true</AllowUnsafeBlocks></PropertyGroup>
  <ItemGroup><Compile Include="Program.cs"/><Compile Include="ObservedFunctions.cs"/><Compile Include="Observer.cs"/></ItemGroup>
</Project>
'@
[IO.File]::WriteAllText((Join-Path $scratch 'WorkProbe.csproj'),$project)
Write-Output 'Arithmetic source recovery PASS; 12 native call sites instrumented; no world executed.'
