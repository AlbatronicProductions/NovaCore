# Build on Windows 11

Required tools:

- .NET 10 SDK
- Visual Studio 2026 with Desktop development with C++ and Windows 11 SDK
- CMake 4.4 or later
- LunarG Vulkan SDK, including validation layers and `glslc`

Open a Visual Studio x64 Developer PowerShell, then run:

```powershell
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

The sample copies the native DLL and compiled SPIR-V shaders beside the managed executable. It remains open until the window closes, reports average frame time during shutdown, and releases resources deterministically.

`--objects=1`, `--objects=100`, `--objects=1000`, and `--objects=10000` select the grid demonstration count. `--scene=grid` is the default; `--scene=frames` resolves ECL, ORB, CCE, CCI, and CCF demonstration markers through a managed reference-frame snapshot.

Logging uses repeated or comma-separated `--log=` values, for example `--log=input,precision` or `--log=vulkan --log=renderer`. Valid categories are `startup`, `vulkan`, `precision`, `input`, `renderer`, `validation`, `camera`, and `all`. `--verbose-input` remains a temporary compatibility alias for `--log=input`.
