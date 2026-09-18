# Reproducing the qualified Stage-4 checkpoint

Stage 4 passed before the downstream Stage-5 draft. The current working tree is not that checkpoint. Do not run the old collector against current source and overwrite the qualification identities.

## Exact inputs

Start from commit `ab14e3e0f8b53bf5e8bf580a99aea3f2b6fd21f5` in an independent workspace. Overlay the 35 paths in `identity.json`:

1. Use the current file only when its SHA-256 equals the Stage-4 seal.
2. Otherwise use its exact entry in `../stage5/pre-stage5-overlap.zip`.
3. Require every resulting file to match its seal before building. Do not include the three new Stage-5 source/test files.

The Stage-5 closeout identity records which of these sources currently supplies each sealed input. The archive also holds banked files changed only in Stage 5. Those already exist correctly in the baseline; they are not extra Stage-4 changes. Preserve binary/text bytes when extracting.

The existing verified repository BEPU binaries, terrain assets and native toolchain remain their original dependencies. No KSA files are build inputs. Directly re-inspecting the KSA comparison requires the installation and authenticated channel recorded in [ksa-chain.md](ksa-chain.md).

## Build and focused routes

Use PowerShell 7, the repository's .NET SDK and installed VS C++/CMake/Ninja/Vulkan prerequisites. In the independent workspace, initialize the installed x64 VS environment, set `VULKAN_SDK` to its installed path and build native Debug and Release under `build/srv01-stage4/native-debug` and `native-release`:

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Community\Common7\Tools\Launch-VsDevShell.ps1' -Arch amd64 -HostArch amd64 -SkipAutomaticLocation
$env:VULKAN_SDK = 'C:\VulkanSDK\1.4.357.0'
cmake -S native/NovaCore.Native -B build/srv01-stage4/native-debug -G Ninja -DCMAKE_BUILD_TYPE=Debug
cmake --build build/srv01-stage4/native-debug
cmake -S native/NovaCore.Native -B build/srv01-stage4/native-release -G Ninja -DCMAKE_BUILD_TYPE=Release
cmake --build build/srv01-stage4/native-release
dotnet build NovaCore.sln -c Debug --artifacts-path build/srv01-stage4/artifacts -p:ContinuousIntegrationBuild=true -p:NativeBuildDirectory=srv01-stage4/native-debug --nologo -v:q
dotnet build NovaCore.sln -c Release --artifacts-path build/srv01-stage4/artifacts -p:ContinuousIntegrationBuild=true -p:NativeBuildDirectory=srv01-stage4/native-release --nologo -v:q
dotnet build/srv01-stage4/artifacts/bin/NovaCore.Simulation.Tests/debug/NovaCore.Simulation.Tests.dll --assembly-development-validation
dotnet build/srv01-stage4/artifacts/bin/NovaCore.Simulation.Tests/release/NovaCore.Simulation.Tests.dll --assembly-development-validation
```

During the original run, existing native outputs under `srv01-stage3/native-debug` and `native-release` were reused. The commands above rebuild them instead of depending on disposable predecessors.

Run each Simulation executable without arguments for its full 80-group suite. Run ReferenceFrames, Precision and BepuDependency executables in both configurations. Affected Graphics routes are `--assembly-presentation`, `--assembly-powered-support`, `--certified-continuation-regression`, and `--powered-contact-presentation`; do not substitute the unrelated default graphics campaign. Launcher tests infer the repository by walking five parents from their output directory: use their ordinary `tests/.../bin/{configuration}/net10.0` layout or an equivalent five-parent location. The deeper common artifacts layout is not supported by that existing harness.

Stage-2 routes: `--assembly-powered-contact-cheap`, `--assembly-powered-contact-physical`. Stage-3 route: `--assembly-departure-validation`. Exact retained witnesses are in `validation.json`.

## Performance and sizing

Invoke the Release Simulation executable with `--assembly-development-performance` in three separate fresh processes. The route performs 128 warm operations and 1024 measured complete operations for each of wet powered, interior exhaustion and dry populations. Preserve first results and tails; no retry-to-green. Timed regions are separate from exact-allocation qualification. `measurements.json` contains all nine original rows, GC and cold/harness cost context.

`python docs/engineering-evidence/srv01-surface-to-flight-gauntlet/stage4/derive-profile.py` recomputes the sizing from current declared mission inputs. This is analytical sizing, not a flight simulation. Do not silently substitute the historical rejected profile.

No new qualification was executed while writing these instructions. Stage 5 is stopped at a material physical failure; a new run requires Project Control's next scope.
