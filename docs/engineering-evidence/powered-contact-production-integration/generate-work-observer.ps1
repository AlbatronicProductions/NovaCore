# Reproduce the permanent test-only row observer. Never executes a world.
# Retains the accepted friction-work generator's exact source-recovery check.
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$scratch = Join-Path $repo 'build/powered-contact-production-integration'
New-Item -ItemType Directory -Force -Path $scratch | Out-Null
$raw = Join-Path $scratch 'ContactConvexTypes.upstream.cs.txt'
if (-not (Test-Path -LiteralPath $raw)) {
    Invoke-WebRequest 'https://raw.githubusercontent.com/bepu/bepuphysics2/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/Constraints/Contact/ContactConvexTypes.cs' -OutFile $raw
}
if ((Get-FileHash -LiteralPath $raw).Hash -ne 'B259BD56D6EF9405FC98A38E97A9D6CA154CDA11CB908C901800298B7D771DA5') { throw 'Pinned source mismatch' }
$source = [IO.File]::ReadAllText($raw)
$start = $source.IndexOf('    public struct Contact4OneBodyFunctions :')
$end = $source.IndexOf('    /// <summary>', $start)
$original = $source.Substring($start, $end-$start)
$out = [Collections.Generic.List[string]]::new()
$pending = ''; $calls = 0
foreach ($line in ($original -split "`n")) {
    $out.Add($line.Replace('Contact4OneBodyFunctions','PoweredObservedFunctions').Replace('FrictionHelpers.','PoweredObservedFrictionHelpers.'))
    if ($line -match 'public static void (WarmStart|Solve)\(') { $pending = $Matches[1] }
    elseif ($pending -and $line.Trim() -eq '{') {
        $out.Add('            PoweredWorkRecorder.Begin(' + $(if ($pending -eq 'WarmStart') {'true'} else {'false'}) + ');'); $pending = ''
    }
    if ($line -match '^\s+(TangentFrictionOneBody|PenetrationLimitOneBody|TwistFrictionOneBody)\.(WarmStart|Solve)\(') {
        $row = if ($Matches[1] -eq 'TangentFrictionOneBody') {0} elseif ($Matches[1] -eq 'TwistFrictionOneBody') {5} else {
            if ($line -notmatch 'accumulatedImpulses.Penetration([0-3])') { throw 'Normal row parse failed' }; 1+[int]$Matches[1]
        }
        $out.Insert($out.Count-1,"            PoweredWorkRecorder.Record($row, 0, wsvA, inertiaA, accumulatedImpulses);")
        $out.Add("            PoweredWorkRecorder.Record($row, 1, wsvA, inertiaA, accumulatedImpulses);"); $calls++
    }
}
if ($calls -ne 12) { throw 'Unexpected native call count' }
$instrumented = $out -join "`n"
$recovered = (($instrumented -split "`n" | Where-Object { $_ -notmatch '^\s+PoweredWorkRecorder\.(Begin|Record)' }) -join "`n").Replace('PoweredObservedFunctions','Contact4OneBodyFunctions').Replace('PoweredObservedFrictionHelpers.','FrictionHelpers.')
if ($recovered -cne $original) { throw 'Arithmetic source changed' }
$helperAt = $source.IndexOf('in Vector3Wide offsetA0, in Vector3Wide offsetA1, in Vector3Wide offsetA2, in Vector3Wide offsetA3,')
$helperStart = $source.LastIndexOf('        [MethodImpl', $helperAt)
$helperEnd = $source.IndexOf("`n        }", $helperAt) + "`n        }".Length
$helper = $source.Substring($helperStart, $helperEnd-$helperStart)
$header = @'
// TEST OBSERVER ONLY. Generated from BEPU commit f73164bb3c9ca733eb3329f1f6b1cea4e216ece7.
// Copyright Bepu Entertainment LLC; Apache-2.0 license: external/bepu/2.5.0-beta.29/LICENSE.txt.
// Native arithmetic recovered byte-for-byte after removing recording calls and reversing names.
using BepuPhysics;
using BepuPhysics.Constraints;
using BepuPhysics.Constraints.Contact;
using BepuUtilities;
using System.Numerics;
using System.Runtime.CompilerServices;

'@
$processor = 'internal sealed class PoweredObservedProcessor : OneBodyContactTypeProcessor<Contact4OneBodyPrestepData, Contact4AccumulatedImpulses, PoweredObservedFunctions> {}'
$output = $header+$instrumented+"`ninternal static class PoweredObservedFrictionHelpers {`n"+$helper+"`n}`n"+$processor+"`n"
# Upstream recovery above precedes output-only normalization of ten trailing sequences.
if ([regex]::Matches($output, '(?m)[ \t]+(?=\r?$)').Count -ne 10) { throw 'Unexpected output whitespace count' }
$output = [regex]::Replace($output, '(?m)[ \t]+(?=\r?$)', '')
[IO.File]::WriteAllText((Join-Path $repo 'tests/NovaCore.Simulation.Tests/PoweredContactTests.NativeRows.cs'), $output)
Write-Output 'Native arithmetic recovery PASS; 12 row call sites observed; no world executed.'
