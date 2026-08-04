# Build on Windows 11

Required tools: .NET 10 SDK, Visual Studio 2026 with Desktop development with C++ and Windows 11 SDK, CMake 4.4+, and the LunarG Vulkan SDK with validation layers and `glslc`.

Open a Visual Studio x64 Developer PowerShell, then run:

```powershell
cmake -S native/NovaCore.Native -B build/native-ninja -G Ninja
cmake --build build/native-ninja
dotnet build NovaCore.sln -c Debug
dotnet run --project tests/NovaCore.Precision.Tests -c Debug
dotnet run --project samples/NovaCore.Triangle -c Debug
```

The sample places the native DLL and compiled SPIR-V shaders beside the managed executable. It runs until the window is closed, then reports average frame time and shuts down deterministically. A/D and W/S move the managed camera's universe position while the render camera remains at zero.

Logging is selected with repeated or comma-separated `--log=` values, for example `--log=input,precision` or `--log=vulkan --log=renderer`. Valid categories are `startup`, `vulkan`, `precision`, `input`, `renderer`, `validation`, and `all`. `--verbose-input` remains a temporary compatibility alias for `--log=input` and may be removed later.
