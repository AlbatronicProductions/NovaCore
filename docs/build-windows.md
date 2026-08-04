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

The sample places the native DLL and compiled SPIR-V shaders beside the managed executable. It runs for approximately five seconds, prints the authoritative and encoded positions, GPU diagnostics, and average frame time, then shuts down deterministically. A/D and W/S move the managed camera's universe position while the render camera remains at zero. Close the window early to end it sooner.
