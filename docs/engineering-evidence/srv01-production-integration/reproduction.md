# SRV-01 and final plume-method reproduction

Run from `E:\NovaCore` in PowerShell 7 with the x64 Visual Studio developer
environment loaded. Qualified tools: VS 18 Community/MSVC 14.51.36231, Ninja,
Vulkan SDK 1.4.357.0, .NET 10 (runtime 10.0.12), Python 3.11 standard library.
This is an unbanked source candidate. Check final-identities.json before relying
on retained results. All commands below write rebuildable output only beneath
`E:\NovaCore\build\srv01-integration`. Do not regenerate the accepted GLBs.

## Build and focused correction gates

```powershell
Set-Location -LiteralPath 'E:\NovaCore'
$ErrorActionPreference = 'Stop'
$out = 'E:\NovaCore\build\srv01-integration'
$ninja = 'C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\CMake\Ninja\ninja.exe'
foreach ($configuration in @('Debug','Release')) {
    $lower = $configuration.ToLowerInvariant()
    $native = "$out/native-$lower"
    cmake -S native/NovaCore.Native -B $native -G Ninja "-DCMAKE_BUILD_TYPE=$configuration" "-DCMAKE_MAKE_PROGRAM=$ninja"
    if ($LASTEXITCODE -ne 0) { throw 'Native configure failed' }
    cmake --build $native --target NovaCore.Native NovaCorePreparedSubmissionTests NovaCoreExhaustPresentationTests
    if ($LASTEXITCODE -ne 0) { throw 'Native build failed' }
    dotnet build NovaCore.sln -c $configuration --artifacts-path "$out/artifacts" "-p:NativeBuildDirectory=srv01-integration/native-$lower"
    if ($LASTEXITCODE -ne 0) { throw 'Solution build failed' }
    & "$native/NovaCorePreparedSubmissionTests.exe"
    if ($LASTEXITCODE -ne 0) { throw 'Capacity gate failed' }
    $oldLayerPath = $env:VK_IMPLICIT_LAYER_PATH
    try {
        $emptyLayers = "$out/exhaust-correction/empty-implicit-layers"
        New-Item -ItemType Directory -Force -Path $emptyLayers | Out-Null
        if (Get-ChildItem -LiteralPath $emptyLayers -Force) { throw 'Expected empty layer-discovery directory' }
        $env:VK_IMPLICIT_LAYER_PATH = $emptyLayers
        & "$native/NovaCoreExhaustPresentationTests.exe" "$native/shaders"
        if ($LASTEXITCODE -ne 0) { throw 'Production exhaust GPU gate failed' }
    } finally { $env:VK_IMPLICIT_LAYER_PATH = $oldLayerPath }
    $graphics = "$out/artifacts/bin/NovaCore.Graphics.Tests/$lower/NovaCore.Graphics.Tests.exe"
    & $graphics --assembly-presentation
    if ($LASTEXITCODE -ne 0) { throw 'Presentation gate failed' }
    foreach ($case in @('Transport layout','Powered free-flight canonical presentation','Powered contact canonical presentation','Contact development canonical presentation')) {
        & $graphics "--case=$case"
        if ($LASTEXITCODE -ne 0) { throw "Graphics gate failed: $case" }
    }
    $simulation = "$out/artifacts/bin/NovaCore.Simulation.Tests/$lower/NovaCore.Simulation.Tests.exe"
    foreach ($gate in @('--srv01-integration','--assembly-production','--assembly-allocation')) {
        & $simulation $gate
        if ($LASTEXITCODE -ne 0) { throw "Simulation gate failed: $gate" }
    }
}
```

The process-local layer path avoids the host's absent Epic overlay manifests;
the native test explicitly enables Khronos validation and still requires zero
errors. It does not disable validation or modify the registry. Empty-directory
creation is not permission to suppress an actual API error.

## Protected regression and measurements

For each built configuration, run the Simulation executable with these existing
single arguments, one fresh process per route. Stop on any nonzero exit:

```text
--finite-propellant-cheap
--engine-preparation-cheap
--spacecraft-command-cheap
--powered-free-flight-arithmetic
--powered-free-flight-physics
--powered-free-flight-authority
--powered-free-flight-schedules
--powered-free-flight-allocation
--powered-contact-cheap
--powered-contact-physics
--powered-contact-work
--powered-contact-authority
--powered-contact-sequences
--powered-contact-lifecycle
--powered-contact-allocation
```

The full Simulation suite is that executable with no argument. Independently run
the built ReferenceFrames, Precision and BepuDependency executables with no argument.
Full Graphics is not claimed: the registered focused cases above are the corrected
boundary. The legacy `grid` and `frames` routes were each run for 16 frames in both
configurations using the actual Triangle executable with
`--scene=grid` (or `frames`) `--benchmark-frames=16 --log=renderer,vulkan`.

The launcher tests assume exactly five ancestors to the repository. To reproduce
their accepted isolated deployment, copy the **complete built launcher-test closure**
from `artifacts/bin/NovaCore.Launcher.Tests/<configuration>/` into
`build/srv01-integration/launcher/<configuration>/net10.0/`, then execute
`NovaCore.Launcher.Tests.exe` there. No source/path assertion is weakened.

The existing Simulation `--assembly-cost` route runs the declared 16 populations,
128 warm / 1,024 measured samples, including A/B, preparation/publication,
backlog and explicit save/restore boundaries. Three fresh Release processes were
retained in results.json; do not add/subtract their percentiles or compare the
different banked free-flight workload as if it were equivalent. Storage uses
`--assembly-storage`; cold traffic and forced-GC retained estimates are labeled.

For the final renderer's three fresh serial 480-frame Release processes:

```powershell
1..3 | ForEach-Object {
    & 'E:\NovaCore\docs\engineering-evidence\srv01-production-integration\launch.ps1' -Benchmark
    if ($LASTEXITCODE -ne 0) { throw 'Frame qualification failed' }
}
```

Run without `-Benchmark` for the required human READY → Space → main/gimbal/RCS →
COMPLETED check. Close afterward to retain the log. See manual-acceptance.md.
Performance is at 1280×720 on AMD RX 6800 XT; compare whole-frame distributions,
GPU averages and live service separately, retaining maxima and cold preparation.

## Independent trajectory evidence

The historical oracle-report.md records the original generator location. Its
retained path-independent adaptation is reproduce-oracle.py; the former scratch
path is no longer required. Exact authored input and 24 schedule hashes are
checked by the script. Run twice and compare the generated reference files:

```powershell
python docs/engineering-evidence/srv01-production-integration/reproduce-oracle.py --output-directory build/srv01-integration/proof
python docs/engineering-evidence/srv01-production-integration/reproduce-oracle.py --output-directory build/srv01-integration/proof --suffix=-repeat
Get-FileHash -Algorithm SHA256 build/srv01-integration/proof/Assembly-FourHorn-Reference-Endpoints*.json
```

It uses direct coupled 6×6 Newton/Euler equations, Decimal 60 digits, exact
rational event boundaries and 4×/8× refinement; no C# evaluator is imported.
The retained test fixture is tests/NovaCore.Simulation.Tests/Data/Assembly-FourHorn-Reference-Endpoints.json.

## Source provenance and cleanup

exhaust-method.md supplies current KSA file/IL witnesses and direct official
Discord history links. inspect-current-ksa.ps1 decodes named types/methods
read-only from the installed DLL, e.g. `-Types ExhaustGasDynamics -List`.
Hashes bind the inspected installation, not future updates. No proprietary source,
texture or mesh is needed by NovaCore at build/run time.

Review disposable-inventory.json and cleanup.md before deleting the exact single
owned output root. No broad wildcard or historical source removal is required.
