# Build on Windows 11

Required tools:

- .NET 10 SDK
- Visual Studio 2026 with Desktop development with C++ and Windows 11 SDK
- CMake 4.4 or later
- LunarG Vulkan SDK, including validation layers and `glslc`

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

cmake -S native/NovaCore.Native -B build/native-ninja -G Ninja
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
Both solution configurations map all 20 projects. A Release build must report
actual project outputs; MSB4121 warnings do not constitute a successful build.

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
also verify the sample DLL and all 49 deployed shader hashes against that build.

Full automated validation means all 90 managed cases, plus the three native cases
below, pass in both configurations with their stated prerequisites. Category
exclusions are not passes or skips. There are no unconditional skipped cases.
Missing GPU/layer/assets are actionable failures, not silent environment skips.
Headless-only success must be reported as headless-only.

Native cases are explicit CMake targets excluded from the default build:

```powershell
cmake --build build/native-ninja --target NovaCoreRegionalPhysicalTests NovaCoreFacilityVisibilityTests NovaCoreSurfaceMaterialCoordinatesTests
build/native-ninja/NovaCoreRegionalPhysicalTests.exe <verified-Florida-nccube-path>
build/native-ninja/NovaCoreFacilityVisibilityTests.exe build/native-ninja/shaders/facility_visibility_test.comp.spv
build/native-ninja/NovaCoreSurfaceMaterialCoordinatesTests.exe build/native-ninja/shaders/surface_material_coordinates_test.comp.spv
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

The regional native case is CPU-only. The two presentation cases require Vulkan
FP64 and Khronos validation in **both** configurations, including instance creation
and device teardown. Their errors fail the executable. Ordinary runtime code
requests validation in Debug; canonical tests also enable it in Release through
the loader environment and check the loaded module. The runtime's Release message
describes its compiled request policy, not the loader's forced layer chain.
Managed GPU proof/query contexts enable the layer when available; preflight
requires it. Direct native commands above inherit ambient discovery; use the
Graphics runner's `--native-gpu` entry for canonical regression status.

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
