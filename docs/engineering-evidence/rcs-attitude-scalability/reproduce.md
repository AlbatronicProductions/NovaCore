# Reproduction and candidate ownership

Run from the isolated development repository. `E:/NovaCore` is the preserved frozen source/candidate, not the destination for these writes. The managed worktree starts at the same HEAD plus the complete3735-file frozen working-source overlay; bare banked HEAD alone does not contain the unbanked stabilization work. `change.patch` is relative to that frozen working source. Do not treat it as a patch against banked HEAD.

## Build

In a Visual Studio2026 x64 developer PowerShell with the installed Vulkan SDK and CMake:

```powershell
cmake -S native/NovaCore.Native -B build/native-ninja -G Ninja -DCMAKE_BUILD_TYPE=Debug
cmake --build build/native-ninja
cmake -S native/NovaCore.Native -B build/native-ninja-release -G Ninja -DCMAKE_BUILD_TYPE=Release
cmake --build build/native-ninja-release
dotnet build NovaCore.sln -c Debug --no-incremental
dotnet build NovaCore.sln -c Release --no-incremental
python tools/verify-player-package.py --output build/rcs-scalability/canonical-package.json
```

No copied diagnostic DLL/runtime files are required. Both repository locators recognize Git worktrees. Native/shader/content deployment is performed by the existing build targets. Package verification covers all127 files and66 shaders. This is repository-layout development packaging, not standalone publication.

## CPU/permanent tests

```powershell
dotnet tests/NovaCore.Simulation.Tests/bin/Debug/net10.0/NovaCore.Simulation.Tests.dll --rcs-scalability build/rcs-scalability/debug-cpu.json
dotnet tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll --rcs-scalability build/rcs-scalability/release-cpu.json
dotnet tests/NovaCore.Graphics.Tests/bin/Debug/net10.0/NovaCore.Graphics.Tests.dll --modular-viewport
dotnet tests/NovaCore.Graphics.Tests/bin/Release/net10.0/NovaCore.Graphics.Tests.dll --modular-viewport
```

This also generates the explicit qualification-only catalog for the96-jet native witness. The saved JSON is regeneratable and is not added to production assets. `regressions.json` and `legacy-regressions.json` retain exact commands and tails for affected construction, resource, control, frame and camera tests. Replace recorded development-root prefixes with the current checkout path. Do not rebuild outputs while a test/player process owns those files.

## Native unified lifecycle

The authoritative terrain packages are explicit inputs. Point `NOVACORE_ASSET_CACHE` or `--cache` at a verified cache containing manifest IDs `earth-surface-v5` and `earth-florida-m12`; the observed cache is E:/NovaCore/.novacore/cache/terrain/v1. It is used read-only. No KSA directory or diagnostic extraction is a runtime input.

```powershell
python docs/engineering-evidence/rcs-attitude-scalability/qualify-native.py --cache E:/NovaCore/.novacore/cache/terrain/v1 --output build/rcs-scalability/native --catalog96 build/rcs-scalability/qualification-catalog.json
```

Run native routes sequentially after CPU qualification finishes. Short/long preserve the existing 1280x720 lifecycle route (1280x602 game viewport). RCS64/96 request 3440x1440 borderless; unified product chrome leaves a measured 3440x1322 game viewport. This is not a full 3440x1440 render-extent claim. They use 16 seconds of actual thrust-driven ascent to keep the measured coast window clear of terrain and hold Q during three 256-frame coast windows. No kinematic lift/teleport/support enlargement. Normal startup settings and the user's craft library are not overwritten by qualification mode. Final resize checks still exercise windowed 1280x720 and editor return.

The exact development player entry is `tools/NovaCore.App/bin/Release/net10.0-windows/NovaCore.exe`. For ordinary development launch set the documented terrain cache in that process environment and start this executable from the worktree. Do not overwrite the frozen E:/NovaCore canonical executable without Project Control disposition.

## Evidence and retention

Permanent evidence budget:2MiB. Retain compact reports, current KSA boundary identities, exact source/candidate hashes, regression/native summaries, performance distributions and reproduction. Build directories/logs/qualification-generated catalog remain reproducible local outputs; no bulk capture is required. Source and permanent tests remain in this worktree. KSA writes0; no commit, tag, push, banking or milestone assignment.

`frozen-entry.json` records the exact pre-change working-source/package baseline. `source-manifest.json` records the qualified runtime/test/asset/build inputs, and `candidate-package.json` records every deployed file. `change.patch` is a text review patch against the frozen working source, not the banked HEAD. The actual isolated source tree is retained. `seal-evidence.py` verifies preservation and refreshes seals only after all four routes in `build/rcs-scalability/native-qualified` pass. It performs no build, Git mutation or deployment.
