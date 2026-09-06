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
