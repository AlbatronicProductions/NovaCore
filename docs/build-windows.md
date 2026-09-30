# Build on Windows 11

The toolchain below builds the current source, including the accepted **M16.2**
candidate. M16.2 is unbanked; **M16.1 — Planetary Rendering & Terrain Lifecycle
Convergence** remains the latest banked checkout. Player/public-release PASS remains unassigned.
This is a repository-layout development build, not a standalone installer.

The unified entry is `tools/NovaCore.App/bin/Release/net10.0-windows/NovaCore.exe`.
Run it without arguments for fullscreen Configuration → Loading → Gameplay →
construction → Florida launch → flight/control → return to the retained design.
See the [player walkthrough and controls](../README.md#try-novacore).
`NovaCore.Launcher.exe` is the legacy scenario/engineering tool.

## Get the source

For a new source checkout, with Git and Git LFS installed:

```powershell
git clone --branch m16.1 https://github.com/AlbatronicProductions/NovaCore.git
cd NovaCore
git lfs install
git lfs pull
```

The tag checkout is intentionally detached at the banked source revision. Run the
commands below from that repository root. Project Control's canonical development
tree remains `E:\NovaCore`; source builders can use their own checkout location.
No private prospective-source export, isolated worktree executable or test catalog
is needed for the normal application.

The `m16.1` clone command reproduces that bank, not the uncommitted M16.2 candidate
currently in `E:\NovaCore`. Do not substitute an unpublished `m16.2` tag. Candidate
qualification and exact inputs are recorded in the
[M16.2 preparation receipt](engineering-evidence/m16.2-bank-preparation/README.md).

Both `earth-surface-v5` and `earth-florida-m12` must be built and verified before
starting Earth/Solar scenes, even away from Florida. Runtime does not download
missing terrain, and no prebuilt terrain download is currently configured. See
the [terrain setup guide](terrain-assets.md) for cache preparation and recovery.

## Development instrumentation and status

Performance optimization is ongoing; stable 150 FPS is open and historical
blackout causality is unresolved. Earlier qualification reports retain their
original pre-bank identities; the [current engineering state](NOVACORE_CURRENT_STATE.md)
records the bank and all open acceptance boundaries.

The [bounded recorder-storage policy](engineering-evidence/minimum-recorder-bounded-storage/README.md)
is part of this development build. Startup performs maintenance as needed,
reserves a full session within the 512 MiB hard cap, starts MinimumRecorder,
then launches the game. Preventive maintenance begins at 400 MiB. Unique protected
evidence may exceptionally force an explicit unrecorded launch; that run cannot
satisfy mandatory-recorder qualification. MinimumRecorder remains temporary
qualification infrastructure. Its public KEEP / DEV-ONLY / RETIRE disposition is
undecided. Do not package runtime journals, dumps/captures, bulk or unselected
generated evidence. The source bank does not grant player-release acceptance.

## Toolchain and build

Required tools:

- Git with Git LFS, plus Ninja
- .NET 10 SDK
- Visual Studio 2026 with Desktop development with C++ and Windows 11 SDK
- CMake 4.4 or later
- LunarG Vulkan SDK, including validation layers and `glslc`
- PowerShell 7 for scripts; Python 3 for package verification (not player runtime)
- Vulkan-capable graphics driver, VC++ x64 runtime, .NET 10 Desktop and ASP.NET
  runtimes (included with the installed SDK development environment)

Qualified toolchain: .NET SDK 10.0.303, MSVC 14.51.36231 and Vulkan SDK 1.4.357.0.
Use a Visual Studio x64 Developer PowerShell so MSVC, Windows SDK and Ninja are
on PATH. No KSA installation, NAIF toolkit or Python package is a player dependency.

Regenerating the production Earth terrain additionally requires the pinned
NumPy/Pillow environment documented in `assets/earth/PROVENANCE.md`. Set
`NOVACORE_PYTHON` when that interpreter is not the default `python` on `PATH`.

Open a Visual Studio x64 Developer PowerShell, then run:

```powershell
dotnet run --project tools/NovaCore.AssetTool -- status earth-surface-v5
dotnet run --project tools/NovaCore.AssetTool -- status earth-florida-m12
# Required once for Earth/Solar production scenes on a fresh cache:
dotnet run --project tools/NovaCore.AssetTool -- build earth-surface-v5
# Florida M12 regional source and production refinement:
pwsh tools/earth_data/acquire_florida_m12.ps1
dotnet run --project tools/NovaCore.AssetTool -- build earth-florida-m12
dotnet run --project tools/NovaCore.AssetTool -- verify earth-surface-v5
dotnet run --project tools/NovaCore.AssetTool -- verify earth-florida-m12

cmake -S native/NovaCore.Native -B build/native-ninja -G Ninja -DCMAKE_BUILD_TYPE=Debug
cmake --build build/native-ninja
dotnet build NovaCore.sln -c Debug

dotnet run --project tests/NovaCore.Precision.Tests -c Debug
dotnet run --project tests/NovaCore.Graphics.Tests -c Debug
dotnet run --project tests/NovaCore.ReferenceFrames.Tests -c Debug
dotnet run --project tests/NovaCore.Camera.Tests -c Debug

dotnet run --project samples/NovaCore.Triangle -c Debug -- --objects=1000 --log=camera
dotnet run --project samples/NovaCore.Triangle -c Debug -- --scene=frames
```

For Release, configure/build `build/native-ninja-release` with
`-DCMAKE_BUILD_TYPE=Release`, then run `dotnet build NovaCore.sln -c Release`.
Both solution configurations map all 24 projects. A Release build must report
actual project outputs; MSB4121 warnings do not constitute a successful build.

The normal build/package/run sequence after terrain preparation is:

```powershell
cmake -S native/NovaCore.Native -B build/native-ninja -G Ninja -DCMAKE_BUILD_TYPE=Debug
cmake --build build/native-ninja
dotnet build NovaCore.sln -c Debug
cmake -S native/NovaCore.Native -B build/native-ninja-release -G Ninja -DCMAKE_BUILD_TYPE=Release
cmake --build build/native-ninja-release --target NovaCore.Native
dotnet build tools/NovaCore.App -c Release
python tools/verify-player-package.py --output build/player-package-verification.json
& ./tools/NovaCore.App/bin/Release/net10.0-windows/NovaCore.exe
```

The final M16.1 preflight rebuilt native Release from source, rebuilt NovaCore.App
through its project references, verified the package and ran CPU integration/startup
smoke checks. No historical DLL/shader copying or KSA dependency was used.
See the [publication verification record](milestones/M16.1-publication.json).

The current source-derived package contains 68 shaders, the native DLL, managed
dependencies, starter/SRV assets and third-party notices. Native content flows
through project references; do not hand-copy DLLs or shaders. The verifier checks
the CMake target output closure and current source content, not an old candidate.

To reproduce with existing terrain distribution bytes, use the hash-validating
installer instead of regenerating terrain. Both inputs must match the checked-in
manifests; `--cache` is an explicit disposable destination:

```powershell
dotnet run --project tools/NovaCore.AssetTool -c Release -- install earth-surface-v5 --source C:/distribution/earth-surface-v5.nccube --cache C:/NovaCoreCache
dotnet run --project tools/NovaCore.AssetTool -c Release -- install earth-florida-m12 --source C:/distribution/earth-florida-m12.nccube --cache C:/NovaCoreCache
$env:NOVACORE_ASSET_CACHE='C:/NovaCoreCache'
dotnet run --project tools/NovaCore.AssetTool -c Release -- verify earth-surface-v5
dotnet run --project tools/NovaCore.AssetTool -c Release -- verify earth-florida-m12
```

Runtime also retains repository topology/elevation assets. Keep the repository
root marker and those source assets. A portable copied EXE is not the product
package. Full diagnostic-web publish parity is outside this qualification.

The separate offline NAIF regression requires its own rebuildable shim:

```powershell
cmake -S native/NovaCore.CSpiceShim -B external/naif/build/cspice-shim -G Ninja -DCMAKE_BUILD_TYPE=Release
cmake --build external/naif/build/cspice-shim
dotnet run --project tests/NovaCore.NaifEphemerisAdapter.Tests -c Release
```

The shim uses the pinned local CSPICE source bundle and is never a runtime
dependency. The solution does not implicitly configure native projects.

The sample copies the native DLL and compiled SPIR-V shaders beside the managed executable. It remains open until the window closes, reports average frame time during shutdown, and releases resources deterministically.

The sample does not copy the heavy terrain-v5 `.nccube` payloads. Earth and
Solar resolve the required global manifest and regional M12 manifest to
verified content-addressed runtime-cache paths and pass those explicit paths to
native code. Set `NOVACORE_ASSET_CACHE` or
use the asset tool's `--cache <path>` option to relocate the disposable cache.
See [terrain-assets.md](terrain-assets.md) for status, verify, fetch, install,
regenerate, interruption recovery, and fresh-clone behavior.

`--objects=1`, `--objects=100`, `--objects=1000`, and `--objects=10000` select the grid demonstration count. `--scene=grid` is the default; `--scene=frames` resolves ECL, ORB, CCE, CCI, and CCF demonstration markers through a managed reference-frame snapshot.

Logging uses repeated or comma-separated `--log=` values, for example `--log=input,precision` or `--log=vulkan --log=renderer`. Valid categories are `startup`, `vulkan`, `precision`, `input`, `renderer`, `validation`, `camera`, and `all`. `--verbose-input` remains a temporary compatibility alias for `--log=input`.

## Graphics validation contract

For this stabilization campaign, Project Control permits automated integration,
direct visual/runtime evidence and reproducibility to satisfy the acceptance bar
without mandatory manual retest. Historical manual gates below retain their
original scope; Engineering does not declare manual Player PASS.


Run from the repository root. Build native and managed code in the same
configuration first, including the Triangle sample for window tests. The Graphics
runner starts a fresh process for each case: published elevation datasets and
native contexts must not leak between fixtures. It continues after a failure,
returns a nonzero exit code, and reports passes, failures, skips and excluded cases.
An unknown filter is an error. `--list` lists every permanent case and category.

```powershell
dotnet run --project tests/NovaCore.Graphics.Tests -c Debug -- --list
dotnet run --project tests/NovaCore.Graphics.Tests -c Debug -- --category=headless
dotnet run --project tests/NovaCore.Graphics.Tests -c Debug -- --category=gpu
dotnet run --project tests/NovaCore.Graphics.Tests -c Debug -- --category=window
# Omit the category to run all cases; repeat with -c Release.
# Same controlled child-process environment for both native GPU executables:
dotnet run --project tests/NovaCore.Graphics.Tests -c Debug -- --native-gpu
# Separate ambient compatibility diagnostic (nonzero remains a failure):
dotnet run --project tests/NovaCore.Graphics.Tests -c Debug -- --ambient --native-gpu
dotnet run --project tests/NovaCore.Graphics.Tests -c Debug -- --ambient --test="Production window lifecycle"
```

| Category | Prerequisites and meaning |
|---|---|
| HEADLESS (`headless`) | Windows x64, matching native DLL and its Vulkan loader dependency, source/shaders and required fixtures/assets. No GPU operations or visible window. Includes CPU numerical, ABI, source-contract and SPIR-V checks; a test name containing GPU does not imply a GPU dispatch. Suitable for provisioned Windows headless CI. |
| GPU-NO-WINDOW (`gpu`) | Vulkan device with required FP64/compute/raster features, compiled shaders and Khronos validation layer. Proofs render into existing offscreen attachments; no Win32 surface. Suitable for GPU CI. |
| VISIBLE-WINDOW AUTOMATED (`window`) | Interactive Windows desktop, compatible Vulkan GPU, matching sample deployment, global/Florida assets. Uses the production Win32/surface/swapchain path. This is not hidden/headless execution. Avoid interacting with these windows during a run. |
| MANUAL | Visual quality and player-facing continuous Florida approach remain physical acceptance responsibilities. API lifecycle tests cannot replace that gate. A validation-only change does not automatically reopen the accepted P2S5H gate. |

Debug tests resolve the Debug native DLL; Release tests resolve Release. Before
running a case, the harness compares the deployed SHA-256 with the selected native
build, loads that exact path, and checks the actual loaded module. No native PATH
fallback is accepted. Test shader paths use the same configuration. Window tests
also verify the sample DLL and all source-derived deployed shader hashes (currently 66) against that build.

The current Graphics runner lists 116 managed cases. Full Graphics validation
means those cases plus the four native cases below pass in both configurations
with their stated prerequisites. Category
exclusions are not passes or skips. There are no unconditional skipped cases.
Missing GPU/layer/assets are actionable failures, not silent environment skips.
Headless-only success must be reported as headless-only.

Native cases are explicit CMake targets excluded from the default build:

```powershell
cmake --build build/native-ninja --target NovaCoreRegionalPhysicalTests NovaCoreFacilityVisibilityTests NovaCoreSurfaceMaterialCoordinatesTests NovaCoreStellarProjectionTests
build/native-ninja/NovaCoreRegionalPhysicalTests.exe <verified-Florida-nccube-path>
build/native-ninja/NovaCoreFacilityVisibilityTests.exe build/native-ninja/test-shaders/facility_visibility_test.comp.spv
build/native-ninja/NovaCoreSurfaceMaterialCoordinatesTests.exe build/native-ninja/shaders/surface_material_coordinates_test.comp.spv
build/native-ninja/NovaCoreStellarProjectionTests.exe build/native-ninja/shaders/stellar_glow.vert.spv
# Repeat using build/native-ninja-release.
```

Canonical GPU/window validation runs **unelevated**, with process-local Vulkan
loader discovery: an empty implicit manifest directory and a single explicit SDK
Khronos validation manifest pointing to the installed DLL. `VULKAN_SDK` (or
`VK_SDK_PATH`) selects the SDK. Inherited layer activation and validation-disable
settings are cleared in the test process; `VK_INSTANCE_LAYERS` requests Khronos in
both Debug and Release. A loader-visible foreign layer or missing validation is a
failure. No registry state, driver selection, production launcher environment or
global application setting is changed. Temporary manifests are removed at teardown.
HEADLESS-only runs do not establish a Vulkan environment.

`--ambient` preserves actual layer discovery and activation, reports registry and
manifest/binary existence (presence is not proof of loading), and runs the same
strict checks. Window tests record loaded modules. For loader call-chain evidence,
set `VK_LOADER_DEBUG=error,warn,layer` in the diagnostic shell; for error caller and
object evidence set `NOVACORE_VULKAN_CALLSTACK=1`. Remove those diagnostic overrides
after use. An ambient failure is reported independently of canonical regression
status; it is never converted into a pass. See [Package 2](graphics-validation-package-2.md).

The regional native case is CPU-only. The three presentation cases require Vulkan
FP64 and Khronos validation in **both** configurations, including instance creation
and device teardown. Their errors fail the executable. Ordinary runtime code
requests validation in Debug; canonical tests also enable it in Release through
the loader environment and check the loaded module. The runtime's Release message
describes its compiled request policy, not the loader's forced layer chain.

M16.2 also requires the explicit `tests/NovaCore.Player.Tests` project and
`NovaCorePlayerGpuMemoryTests` native target; the solution/default native build
does not select both automatically. The latter is a CPU-only owner/aggregation
test. Build it in each native configuration and run the resulting executable.
Use `tools/verify-player-package.py --output <receipt.json>` after builds and
native test-target generation. Its source-derived runtime shader inventory must
match the package exactly; stale retired outputs or test-only shaders are failures.
The current M16.2 preparation records such a failure and is **REVISE**, not a
package PASS. See its [receipt](engineering-evidence/m16.2-bank-preparation/README.md).
Managed GPU proof/query contexts enable the layer when available; preflight
requires it. Direct native commands above inherit ambient discovery; use the
Graphics runner's `--native-gpu` entry for canonical regression status. Current
M16.2 preparation found that runner still supplies the old `shaders/` path for
the facility test, so it fails despite CMake producing the file in `test-shaders/`.
That test-harness correction remains pending Project Control; do not count the
direct command as a replacement PASS for the failing mandatory runner.

The sole accepted window warning is the exact SDK message
`WARNING-Shader-OutputNotConsumed` for vertex output Location 11 Component 0,
with no matching fragment input. The shared vertex shader emits a uint terrain
layer for the production fragment shader; the generic fragment pair may legally
discard it. The warning remains logged. Other IDs, locations or severities fail.
In particular, `memoryTypeIndex-00645` is **not** allowlisted. The observed KMT
import error is a measured OBS capture-layer interference: its invalid allocation
is called by `graphics-hook64.dll`, and disappears when that Vulkan layer is
disabled. Stale Epic manifests separately produce loader discovery errors.
Neither error is allowlisted. Package 1 observations remain in the
[historical validation review](graphics-window-validation.md).

The window lifecycle test checks windowed and borderless startup twice each,
actual module identity, client/swapchain extents, resize, minimized zero extent,
restore, submitted frames and normal close/teardown. Checkpoints have a 60-second
failure deadline, not retries for a passing result. The production application has
no exclusive-fullscreen or live window-mode transition API; no such capability is
invented for tests. The regional regression exercises 1,000 scripted frames plus
outside-region and non-Earth isolation and removes its disposable readbacks on
success. Failures retain their bounded GUID directory for investigation.
The wheel-isolation regression delivers a real Win32 wheel message in ordinary
and regional-probe modes: ordinary scrolling must still zoom, while the probe
must consume no desktop wheel input, matching its existing key/look isolation.

The grid/frames startup regression exercises the same lifecycle with an explicit
generation-4 option and no Earth asset contract. Native physical-oracle allocation,
descriptor publication and preparation dispatch require the production terrain
owner. Generic submissions leave that unused descriptor unbound; no fake Earth
asset or fallback height is supplied. Other shared resource allocations remain
outside this bounded ownership correction.

`NOVACORE_P2S5F_ARTIFACT_INPUT=assets/planetary-nested-scale-mesh` (prefer an
absolute path) uses the existing checked-in 18-scale library for topology
validation. Without it the test regenerates all 18 scales in memory and is much
slower. `NOVACORE_P2S5F_EXHAUSTIVE_DETERMINISM=1` additionally compares regenerated
bytes. State which mode ran; reading fixtures does not prove regeneration.
`NOVACORE_P2S2_ARTIFACT_OUTPUT`, `NOVACORE_P2S5B_ARTIFACT_OUTPUT` and
`NOVACORE_P2S5F_ARTIFACT_OUTPUT` opt into diagnostic artifacts and are not normal
validation prerequisites. Apply the [diagnostic output policy](diagnostic-output-policy.md).
