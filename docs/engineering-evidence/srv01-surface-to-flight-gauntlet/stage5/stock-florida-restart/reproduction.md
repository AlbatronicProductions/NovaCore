# Reproduction after a Project Control decision

This is retained reproducibility, **not authorization to continue past the performance stop**. No further run is required for the current report. Original log hashes, result witnesses and source seals are in `results.json`, `preflight.json`, `candidate-seals.json` and `final-verification.json`. The complete first live aggregate log is `first-live-process.txt` (unchanged bytes).

Use current sealed candidate source on baseline `ab14e3e0f8b53bf5e8bf580a99aea3f2b6fd21f5`. To independently reconstruct it, start from that baseline and overlay prior Stage4's 35 sealed files, then the prior Stage5 14 inputs (the prior source/result map specifies the additional and overlapping paths), then these seven current files. Verify all source hashes rather than trusting overlay order. The three modified sample predecessors are retained in `prior-sample-inputs.zip`; all previous checkpoint locations are enumerated in `final-verification.json`. Never restore historical inputs over the current candidate.

Existing exact repository BEPU binaries, authored stock catalog and SRV-01 visual assets, global Earth elevation and the regional `earth-florida-m12` terrain-v5 dataset are required. KSA is source-reference evidence only and not a build dependency. Do not substitute the development-propulsion resource profile.

Read-only checks, no application launch:

```powershell
Set-Location -LiteralPath 'E:\NovaCore'
python docs/engineering-evidence/srv01-surface-to-flight-gauntlet/stage5/stock-florida-restart/finalize.py
& 'E:\NovaCore\docs\engineering-evidence\srv01-surface-to-flight-gauntlet\stage5\stock-florida-restart\launch.ps1' -PrepareOnly
```

Actual successful build configuration used the retained Stage3 native DLL/shaders:

```powershell
dotnet build NovaCore.sln -c Debug --artifacts-path build/srv01-stage5-stock/artifacts -p:ContinuousIntegrationBuild=true -p:NativeBuildDirectory=srv01-stage3/native-debug --nologo -v:q
dotnet build NovaCore.sln -c Release --artifacts-path build/srv01-stage5-stock/artifacts -p:ContinuousIntegrationBuild=true -p:NativeBuildDirectory=srv01-stage3/native-release --nologo -v:q
```

For a future independent rebuild without retained native output, use the installed x64 VS development shell, CMake/Ninja and Vulkan SDK as described in `../../stage4/reproduction.md`. Build `native/NovaCore.Native` in an isolated output directory and set `NativeBuildDirectory` to that exact path relative to `build`. Do not assume the historical Stage4 native directory exists.

Focused Graphics executable, Debug and Release, one mode per fresh process:

```powershell
dotnet build/srv01-stage5-stock/artifacts/bin/NovaCore.Graphics.Tests/release/NovaCore.Graphics.Tests.dll --assembly-florida-qualification
dotnet build/srv01-stage5-stock/artifacts/bin/NovaCore.Graphics.Tests/release/NovaCore.Graphics.Tests.dll --assembly-florida-allocation
dotnet build/srv01-stage5-stock/artifacts/bin/NovaCore.Graphics.Tests/release/NovaCore.Graphics.Tests.dll --assembly-florida-presentation
dotnet build/srv01-stage5-stock/artifacts/bin/NovaCore.Graphics.Tests/release/NovaCore.Graphics.Tests.dll --assembly-florida-presentation-storage
```

The storage-only final mode was run only in Release. Substitute `debug` for the three preceding modes for their executed Debug equivalents. `--assembly-florida-site-cheap` is the retained initial mass/site/frame/physical gate. `--assembly-florida-costs` executes one 128-warm/1024-measured Release complete-operation population and final native storage check; the current report contains exactly three final independent processes, not averaged retries.

Full Simulation binaries take no arguments. Its `--assembly-departure-validation` route preserves Stage3. ReferenceFrames, Precision and BepuDependency binaries take no arguments. Focused Graphics regression routes were `--assembly-presentation`, `--assembly-powered-support`, `--certified-continuation-regression` and `--powered-contact-presentation`. No unrelated default graphics/GPU campaign was invoked. Launcher remains a stopped gate; its existing harness needs a five-parent output layout, not the deeper artifacts layout.

The one executed live population was:

```powershell
& 'E:\NovaCore\build\srv01-stage5-stock\artifacts\bin\NovaCore.Triangle\release\NovaCore.Triangle.exe' --scene=srv01-florida-support --benchmark-frames=4000 --log=renderer,vulkan
```

This benchmark mode autostarts and exits; it is **not manual acceptance**. The actual manual recipe omits benchmark flags, waits at READY for Space and records its log on close. It is blocked pending Project Control in `manual-acceptance.md` / `launch.ps1`. Do not grant manual PASS from the automated completion.

`collect-results.py` only consolidates already-existing logs. It never runs a solver/application. `finalize.py` checks current refs/seals and writes a verification record; it never stages or banks. After disposable logs are removed, the retained JSON/log witnesses still contain the measurements and source identities; do not expect the collector to work without its disposable inputs.
