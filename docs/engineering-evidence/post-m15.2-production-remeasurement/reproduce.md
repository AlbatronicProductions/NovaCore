# Reproduce the bounded current-production audit

Source: main `ab14e3e0f8b53bf5e8bf580a99aea3f2b6fd21f5`; source inputs and built
binary identities are in identity.json. Release .NET10, Visual Studio18/MSVC,
VulkanSDK1.4.357.0, AMD RX6800XT, Windows, 1280×720, native FIFO presentation.
No runtime compiler/GC knobs or physics settings changed. No profiler/EventPipe.
Native Release uses its existing disabled-validation policy. This is a production
measurement, not a new Vulkan validation campaign.

All generated output belongs to the single disposable directory below. Do not
overwrite an existing investigation directory. No accepted assets are regenerated.

```powershell
Set-Location -LiteralPath 'E:\NovaCore'
$ErrorActionPreference = 'Stop'
$out = 'E:\NovaCore\build\post-m15.2-production-remeasurement'
if (Test-Path -LiteralPath $out) { throw 'Review existing output first' }
New-Item -ItemType Directory -Path $out | Out-Null
& 'C:\Program Files\Microsoft Visual Studio\18\Community\Common7\Tools\Launch-VsDevShell.ps1' -Arch amd64 -HostArch amd64 | Out-Null
$env:VULKAN_SDK = 'C:\VulkanSDK\1.4.357.0'
$env:PATH = "$env:VULKAN_SDK\Bin;$env:PATH"
$ninja = 'C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\CMake\Ninja\ninja.exe'
cmake -S native/NovaCore.Native -B "$out/native-release" -G Ninja -DCMAKE_BUILD_TYPE=Release "-DCMAKE_MAKE_PROGRAM=$ninja"
if ($LASTEXITCODE) { throw 'Configure failed' }
cmake --build "$out/native-release" --target NovaCore.Native
if ($LASTEXITCODE) { throw 'Native build failed' }
foreach ($project in @('samples/NovaCore.Triangle/NovaCore.Triangle.csproj','tests/NovaCore.Simulation.Tests/NovaCore.Simulation.Tests.csproj','tests/NovaCore.Graphics.Tests/NovaCore.Graphics.Tests.csproj')) {
    dotnet build $project -c Release --artifacts-path "$out/artifacts" -p:NativeBuildDirectory=post-m15.2-production-remeasurement/native-release
    if ($LASTEXITCODE) { throw 'Managed build failed' }
}
$env:NOVACORE_WINDOW_CLIENT_WIDTH='1280'
$env:NOVACORE_WINDOW_CLIENT_HEIGHT='720'
$app="$out/artifacts/bin/NovaCore.Triangle/release/NovaCore.Triangle.exe"
foreach ($run in 1..3) {
    & $app --scene=stock-assembly --benchmark-frames=480 --log=renderer,vulkan > "$out/baseline-$run.log" 2>&1
    if ($LASTEXITCODE) { throw 'Runtime failed' }
}
$sim="$out/artifacts/bin/NovaCore.Simulation.Tests/release/NovaCore.Simulation.Tests.exe"
foreach ($entry in @(@('--assembly-cost','operations'),@('--assembly-allocation','allocation'),@('--srv01-integration','srv01-correctness'),@('--assembly-storage','storage'))) {
    & $sim $entry[0] > "$out/$($entry[1]).log" 2>&1
    if ($LASTEXITCODE) { throw 'Measurement or correctness failed' }
}
& "$out/artifacts/bin/NovaCore.Graphics.Tests/release/NovaCore.Graphics.Tests.exe" --assembly-presentation > "$out/presentation.log" 2>&1
if ($LASTEXITCODE) { throw 'Presentation checks failed' }
```

Do not run another GPU workload simultaneously. User observed a current autoplay
window and reported PASS. For a separate optional interactive view, invoke the same
app without `--benchmark-frames`; Space begins the two-second episode. A human must
describe actual interactions; the automatic route must not be claimed as personal
Space/camera acceptance.

## Phase/tail observation, separate from untouched headline

`prepare-diagnostic.py` creates copies of the native source and sample host only,
inside the disposable root. It never edits repository production files. It adds:

- fixed native rows of existing cumulative CPU counters and preceding GPU values;
- fixed managed rows of copied observations, timestamps and allocation/GC counters;
- a 60-frame READY delay and calls equivalent to the existing contact Space actions
  at frames60/90/120; optional deterministic **camera-only** orbit;
- output after the native loop, with explicit stdout flush before managed report.

No changed command schedule, resource/force/solver setting, shader, or physics
algorithm. No production dependency on diagnostic code. Measurement overhead is
not subtracted. Canonical Simulation/Graphics library source remains original.

```powershell
python docs/engineering-evidence/post-m15.2-production-remeasurement/prepare-diagnostic.py
if ($LASTEXITCODE) { throw 'Diagnostic generation failed' }
cmake -S "$out/diagnostic-native-source" -B "$out/diagnostic-native" -G Ninja -DCMAKE_BUILD_TYPE=Release "-DCMAKE_MAKE_PROGRAM=$ninja"
if ($LASTEXITCODE) { throw 'Diagnostic configure failed' }
cmake --build "$out/diagnostic-native" --target NovaCore.Native
if ($LASTEXITCODE) { throw 'Diagnostic build failed' }
dotnet build "$out/diagnostic-host/NovaCore.Triangle.csproj" -c Release --artifacts-path "$out/diagnostic-artifacts" -p:NativeBuildDirectory=post-m15.2-production-remeasurement/diagnostic-native
if ($LASTEXITCODE) { throw 'Diagnostic host failed' }
$probe="$out/diagnostic-artifacts/bin/NovaCore.Triangle/release/NovaCore.Triangle.exe"
foreach ($run in 1..3) {
    & $probe --scene=stock-assembly --benchmark-frames=540 --log=renderer,vulkan > "$out/diagnostic-$run.log" 2>&1
    if ($LASTEXITCODE) { throw 'Diagnostic runtime failed' }
}
$priorCamera=$env:NOVACORE_AUDIT_CAMERA
try {
    $env:NOVACORE_AUDIT_CAMERA='1'
    & $probe --scene=stock-assembly --benchmark-frames=540 --log=renderer,vulkan > "$out/diagnostic-camera.log" 2>&1
    if ($LASTEXITCODE) { throw 'Camera runtime failed' }
} finally { $env:NOVACORE_AUDIT_CAMERA=$priorCamera }
& $probe --scene=powered-contact --benchmark-frames=4000 --log=renderer,vulkan > "$out/diagnostic-contact.log" 2>&1
if ($LASTEXITCODE) { throw 'Contact runtime failed' }
python docs/engineering-evidence/post-m15.2-production-remeasurement/summarize.py
if ($LASTEXITCODE) { throw 'Summary failed' }
```

Only existing box contact is exercised by the last command. A held-powered frame
does not mean another physical interval was solved. GPU result read in frame n+1
belongs to prior draw n; first resize-invalid result and final unavailable result
are omitted. CPU update is inclusive. Native metric excludes message-pump work;
callback intervals include scheduling. Percentile policies are named in JSON.

Mechanical capture history: first diagnostic batch completed its physical routes,
but C `printf` buffering interleaved one native row with managed shutdown output.
Those captures were rejected by the parser and excluded, not retried performance
failures. One diagnostic-only `fflush` at post-loop output fixed transport; the
whole bounded diagnostic batch was repeated once. No engineering correction.

Final preservation checks: `git diff --check`, zero tracked/staged changes,
main/remote/tag refs unchanged, bounded input seals unchanged, preexisting local
review files unchanged. The evidence-only additions remain untracked/unbanked.
Cleanup is specified in cleanup.md. Do not run broad qualification or open another
front merely to reproduce this audit.
