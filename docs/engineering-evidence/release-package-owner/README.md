# Release native package owner — PASS, unbanked

2026-09-25 local / 2026-09-26 UTC. Current repository: `E:\NovaCore`, branch `main`, HEAD `8c189b28ce2a68f97de734d1589acb500c41fd99`. This is the bounded shader-package repair requested by Project Control, not a milestone or product acceptance.

## What happened and why

The clean canonical Release package now contains **127 files and all 66 current native-target SPIR-V shaders**, without a manual shader or DLL copy. The unified application passed its existing application integration driver and a separate direct visual check.

Three project files owned conflicting publication behavior:

- `samples/NovaCore.Triangle/NovaCore.Triangle.csproj` copied a hard-coded 52-shader subset after Build. These files were not declared as content and therefore did not flow to referencing projects. Its cleanup also removed shaders outside that subset from its own output.
- `tools/NovaCore.ConstructionEditor/NovaCore.ConstructionEditor.csproj` and `tools/NovaCore.App/NovaCore.App.csproj` each copied the native DLL and a shader wildcard after Build, defaulting to `build/modular-player-revision/native-release`. At inspection that older directory contained only `NovaCore.Native.dll`; its shader glob was empty and the copy silently did nothing.
- The actual current Release build directory was `build/native-ninja-release`, containing 66 shaders. The App -> ConstructionEditor -> Triangle reference chain existed but could not propagate the custom copies as content.

The exact correction is in [owner-correction.patch](owner-correction.patch): Triangle now declares the native DLL and the entire selected native shader directory as linked `Content`, with `CopyToOutputDirectory` and `CopyToPublishDirectory` set to `PreserveNewest`. It selects `native-ninja-release` for Release and `native-ninja` otherwise, while honoring an explicit `NativeBuildDirectory` override. A missing DLL or empty shader directory fails before build/content collection. App and ConstructionEditor no longer have competing native paths or copy targets; both receive the renderer's native content through project references. Triangle's existing elevation copy remains a separate unchanged operation.

Native compilation remains a prerequisite: build the matching CMake native target before building managed code. This change packages native artifacts; it does not silently compile native code or certify the freshness of an arbitrary user-supplied native directory. The 66 count is observed evidence, not a new hard-coded production constant.

## Verification

Before rebuilding, the task recorded 3,598 source/reference hashes and preserved 21 output directories: the native Release directory plus Release `bin` and `obj` directories for all ten projects in the App reference graph. They were moved, not deleted, to `E:\NovaCore\build\release-package-owner\before`. The original three project files are also preserved there. See [preserved-output-paths.json](preserved-output-paths.json). Existing NuGet restore metadata and package caches were retained; compiler outputs were rebuilt from empty Release locations.

| Check | Result |
| --- | --- |
| Fresh native Release configure/build, target `NovaCore.Native` | PASS; 66 shaders generated |
| Ordinary `dotnet build tools/NovaCore.App/NovaCore.App.csproj -c Release --disable-build-servers` | PASS; zero warnings/errors; no native-directory override |
| Canonical package files vs preserved 127-file candidate | 127; zero missing paths; zero extra paths |
| Canonical shaders vs fresh native target | 66/66; zero missing, extra, or SHA-256 mismatches |
| Native DLL vs fresh native target | Exact SHA-256 match |
| Triangle / ConstructionEditor / App reference-chain outputs | Each has the same 66 shaders and native DLL |
| Asset, web UI, and license content vs preserved candidate | All 24 files byte-identical |
| Missing native DLL / empty shader directory | Both ordinary App builds fail with the intended error |
| Configuration selection | Debug/Release defaults and explicit override verified |
| Existing application driver | `APPLICATION_INTEGRATION_PASS`, 392 checks, 3,808 frames, exit 0 |
| Direct visual/input check | PASS; six thumbnails, placed 3D core, Undo back to saved/empty, exit 0 |
| Source and candidate preservation | Only three intended project files changed; all 127 preserved candidate files unchanged |
| `git diff --check` | PASS |

The canonical manifest is [canonical-manifest.json](canonical-manifest.json), summarized in [canonical-summary.json](canonical-summary.json). Its 27 hash differences from the preserved Debug candidate are binaries, symbols, configuration, and generated static-web metadata; they are not missing content. All shader hashes match that candidate as well as the fresh native build. Relative to the manually repaired Release package observed at entry, only the native DLL and generated `NovaCore.ConstructionEditor.staticwebassets.endpoints.json` differ; no runtime source was edited.

Canonical executable:

`E:\NovaCore\tools\NovaCore.App\bin\Release\net10.0-windows\NovaCore.exe`

Executable SHA-256: `4a14ff8b27d9bd4d42867b4179679f02afb9d6647b2aa07f05e74d8b59f581c8`.

Loaded and packaged native DLL SHA-256: `79719b00774a7a3683d5c1820efda249ef3848adab1a4f00285b89ec1e50243a`.

### Bounded application smoke

The existing `--qualify-editor` driver ran through the canonical `NovaCore.exe`, using an isolated craft-library beneath this evidence directory. It exercised real application controls and the hosted renderer: root and socket placement; symmetry preview/commit/refusal; mesh submission; Undo/Redo; fill consumables; save/load exact bytes; launch of the same craft to supported Florida flight; Z ignition, W attitude request, X cutoff/coast; retained-design return and flight resume; camera input; resize/fullscreen transitions; and window/renderer ownership. It reported 12 craft parts and the exact native DLL hash above. [application-smoke.json](application-smoke.json) retains the result; [smoke-stdout.log](smoke-stdout.log) retains the stages and runtime output. The run took approximately 29 seconds from process start to the logged successful exit.

Computer Use independently observed the rendered craft and exhaust during powered flight ([powered-flight.jpg](powered-flight.jpg)). A separate ordinary launch used the saved fullscreen settings: Start NovaCore -> New vehicle -> command-core card -> viewport placement. All six thumbnails and the placed core were visible ([construction-models.jpg](construction-models.jpg)); clicking Undo restored `0 parts ... saved / empty`, then the app closed with exit 0. No user craft was loaded, overwritten, or deleted.

This is engineering smoke evidence. The driver deliberately records `playerPass=false`. It does not grant manual Player acceptance, certify Earth rendering in every condition, qualify a performance contract, or prove full physical/flight correctness beyond its existing checks. No simulation, renderer, shaders, assets, or unified-app runtime architecture were modified.

### Additional publish probe and its limit

`dotnet publish` to a new task-local output succeeded and propagated all 66 shaders plus the matching native DLL. See [publish-native-summary.json](publish-native-summary.json). An exploratory comparison of the entire publish directory against the build candidate found 121 rather than 127 files: the two ConstructionEditor static-web metadata files and four `wwwroot` diagnostic-browser files were absent. The initial broader verification failure remains recorded in [publish-summary.json](publish-summary.json). It was not treated as full-package PASS; only the native-content publish contract is qualified. The canonical build output contains all six of those files. Diagnostic-browser publish parity was not changed or opened as a new work front.

The package owner consumes all shaders actually present in the selected native output. It rejects an entirely empty set, but does not independently detect a partially deleted/stale native build. The clean CMake build and exact manifest comparisons above are the completeness/freshness evidence for this candidate. Clean outputs are also required for exact set comparison after a future shader retirement; this task did not add broad output-directory pruning.

## Reproduce

The native directory is already configured for x64 MSVC Release with Ninja. In an x64 Visual Studio developer environment:

```powershell
Set-Location E:\NovaCore
cmake --build build/native-ninja-release --target NovaCore.Native --parallel 2
if ($LASTEXITCODE -ne 0) { throw 'Native build failed.' }
dotnet build tools/NovaCore.App/NovaCore.App.csproj -c Release --disable-build-servers
if ($LASTEXITCODE -ne 0) { throw 'Application build failed.' }
& .\docs\engineering-evidence\release-package-owner\verify-package.ps1
```

For a new native directory, configure it first with `cmake -S native/NovaCore.Native -B build/native-ninja-release -G Ninja -DCMAKE_BUILD_TYPE=Release` in that same developer environment. The recorded configuration and build logs retain this run's compiler identity and generated targets. `preserve-and-clean.ps1` records the one-time clean-output preparation used here and intentionally refuses to rerun over the existing backup; do not delete that backup to reuse the script.

To repeat the application smoke, close other NovaCore instances and provide a new result path (the driver writes a result and sibling craft-library):

```powershell
$smokeResult = Join-Path 'E:\NovaCore\build\release-package-owner' ('smoke-' + [Guid]::NewGuid().ToString('N') + '\result.json')
& 'E:\NovaCore\tools\NovaCore.App\bin\Release\net10.0-windows\NovaCore.exe' --qualify-editor $smokeResult
```

Allow at most 180 seconds; PASS requires the result file's `APPLICATION_INTEGRATION_PASS`, its native hash matching the package, and a logged exit code 0. Absence of a result is not PASS.

## Disposition and next action

**PASS — bounded canonical Release native-package repair and smoke.** Source and evidence remain unbanked. No commit, tag, push, merge, historical-tag movement, cleanup deletion, or new milestone occurred. Stop here for Project Control review/acceptance; no further implementation front is opened.
