# Reproduction commands

Run from `E:\NovaCore` in a Visual Studio x64 Developer PowerShell with CMake,
Ninja, the Vulkan SDK, .NET 10 and Python 3 on PATH. Do not run a canonical
application while replacing its build output.

The recorded host used MSVC initialization at
`C:\Program Files\Microsoft Visual Studio\18\Community\VC\Auxiliary\Build\vcvars64.bat`
and Ninja at
`C:/Program Files/Microsoft Visual Studio/18/Community/Common7/IDE/CommonExtensions/Microsoft/CMake/Ninja/ninja.exe`.
Local `build/shader-deployment/native-build.cmd` performed that initialization,
then the configure/build commands below. CMake's explicit `CMAKE_MAKE_PROGRAM`
argument selected that Ninja path. Output was redirected to the named logs.

## Canonical configurations

```powershell
cmake -S native/NovaCore.Native -B build/native-ninja -G Ninja -DCMAKE_BUILD_TYPE=Debug
cmake --build build/native-ninja --target NovaCore.Native
dotnet build tools/NovaCore.App/NovaCore.App.csproj -c Debug --nologo
python tools/verify-player-package.py --configuration Debug --output build/shader-deployment/package-debug.json

cmake -S native/NovaCore.Native -B build/native-ninja-release -G Ninja -DCMAKE_BUILD_TYPE=Release
cmake --build build/native-ninja-release --target NovaCore.Native
dotnet build tools/NovaCore.App/NovaCore.App.csproj -c Release --nologo
python tools/verify-player-package.py --configuration Release --output build/shader-deployment/package-release.json
```

## Clean construction

The recorded roots below did not exist before this run. For another clean native
trial, select a fresh root name rather than deleting existing work. `NativeBuildDirectory`
is relative to the repository's `build/` directory.

```powershell
cmake -S native/NovaCore.Native -B build/shader-deployment/clean-native-debug -G Ninja -DCMAKE_BUILD_TYPE=Debug
cmake --build build/shader-deployment/clean-native-debug --target NovaCore.Native
dotnet clean tools/NovaCore.App/NovaCore.App.csproj -c Debug --nologo
dotnet build tools/NovaCore.App/NovaCore.App.csproj -c Debug --nologo -p:NativeBuildDirectory=shader-deployment/clean-native-debug
python tools/verify-player-package.py --configuration Debug --native build/shader-deployment/clean-native-debug --output build/shader-deployment/clean-package-debug.json

cmake -S native/NovaCore.Native -B build/shader-deployment/clean-native-release -G Ninja -DCMAKE_BUILD_TYPE=Release
cmake --build build/shader-deployment/clean-native-release --target NovaCore.Native
dotnet clean tools/NovaCore.App/NovaCore.App.csproj -c Release --nologo
dotnet build tools/NovaCore.App/NovaCore.App.csproj -c Release --nologo -p:NativeBuildDirectory=shader-deployment/clean-native-release
python tools/verify-player-package.py --configuration Release --native build/shader-deployment/clean-native-release --output build/shader-deployment/clean-package-release.json
```

## Native test order, canonical restoration and incremental repeat

```powershell
cmake --build build/native-ninja --target NovaCoreSurfaceMaterialCoordinatesTests NovaCoreFacilityVisibilityTests NovaCoreStellarProjectionTests
cmake --build build/native-ninja-release --target NovaCoreSurfaceMaterialCoordinatesTests NovaCoreFacilityVisibilityTests NovaCoreStellarProjectionTests
dotnet run --project tests/NovaCore.Graphics.Tests -c Debug -- --native-gpu
dotnet run --project tests/NovaCore.Graphics.Tests -c Release -- --native-gpu

dotnet build tools/NovaCore.App/NovaCore.App.csproj -c Debug --nologo
python tools/verify-player-package.py --configuration Debug --output build/shader-deployment/test-order-package-Debug.json
dotnet build tools/NovaCore.App/NovaCore.App.csproj -c Release --nologo
python tools/verify-player-package.py --configuration Release --output build/shader-deployment/test-order-package-Release.json

dotnet build tools/NovaCore.App/NovaCore.App.csproj -c Debug --nologo
python tools/verify-player-package.py --configuration Debug --output build/shader-deployment/incremental-package-Debug.json
dotnet build tools/NovaCore.App/NovaCore.App.csproj -c Release --nologo
python tools/verify-player-package.py --configuration Release --output build/shader-deployment/incremental-package-Release.json

dotnet run --project tests/NovaCore.Player.Tests -c Debug
dotnet run --project tests/NovaCore.Player.Tests -c Release
python tools/test-runtime-shader-deployment.py
```

The two Graphics `--native-gpu` commands currently exit with an inherited facility
path failure. Record that exit; do not label those complete harness runs PASS.
The surface and stellar results and subsequent package checks are separately
reported in the evidence. Existing production output directories are not manually
contaminated: the regression script creates checked scratch copies under
`build/shader-deployment-tests` for its mutations and confines cleanup there.

The initial ambient native attempt was
`build/native-ninja-release/NovaCoreSurfaceMaterialCoordinatesTests.exe build/native-ninja-release/test-shaders/surface_material_coordinates_test.comp.spv`.
It failed on ambient Vulkan layer registration, prompting the existing canonical
harness commands above. Its failure is preserved in `logs/native-test-release.log`.

## Runtime and source checks

Launch `tools/NovaCore.App/bin/Release/net10.0-windows/NovaCore.exe` ordinarily.
Perform the physical UI sequence documented in README; the accepted existing save
is local user data and is not generated or overwritten by the deployment tests.

```powershell
git diff --check
git status --short
git rev-parse HEAD 'm16.2^{commit}'
git diff --stat
```

No commit/tag/push commands are part of this reproduction or authorization.
