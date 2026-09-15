# Reproduction

Use branch codex/powered-contact-production-integration, SDK10.0.303/runtime10.0.12,
accepted BEPU bytes and banked native Debug/Release prerequisites. No profiler, tiering,
PGO, solver, GC policy or physics override. All new scratch is under one exact root.

```powershell
Set-Location -LiteralPath 'E:\NovaCore'
$out='E:\NovaCore\build\powered-contact-final-qualification'
dotnet build NovaCore.sln -c Debug --artifacts-path "$out\artifacts" --nologo
if($LASTEXITCODE -ne 0){throw 'Debug build failed'}
dotnet build NovaCore.sln -c Release --artifacts-path "$out\artifacts" --nologo
if($LASTEXITCODE -ne 0){throw 'Release build failed'}
dotnet "$out\artifacts\bin\NovaCore.Simulation.Tests\debug\NovaCore.Simulation.Tests.dll"
if($LASTEXITCODE -ne 0){throw 'Debug suite failed'}
dotnet "$out\artifacts\bin\NovaCore.Simulation.Tests\release\NovaCore.Simulation.Tests.dll"
if($LASTEXITCODE -ne 0){throw 'Release suite failed'}
```

Explicit final characterization route: --powered-contact-characterization. Original
historical --powered-contact-cost and --powered-contact-control-cost retain the old
diagnostic failure thresholds and are NOT final acceptance routes. Three final fresh
processes already completed; do not repeat them merely to lower a tail.

Other focused Simulation routes: --powered-contact-authority, --powered-contact-storage,
--powered-contact-allocation, --powered-free-flight-arithmetic, --powered-free-flight-schedules,
--powered-free-flight-allocation. Run ReferenceFrames, Precision and BepuDependency test
executables in both configurations. Graphics focused routes --powered-contact-presentation
and --powered-free-flight-presentation require the linked scenes and existing native closure.
Full Simulation includes protected M14.21 article, M14.22 commands, M14.23 actuator,
M14.24 resources, M15.0 physics/authority and the seven new contact groups.

Manual/live frame evidence: launch.ps1 and visible-route.md. One unchanged supported
fixture; no pre-funding later wall time. Keep the first33,333ticks' explicit inspection
inputs distinct from subsequent live elapsed-host samples. No tests/benchmarks should run
during measured live motion. Existing validation completed while this window remained READY.

Final logs are disposable after concise JSON/results and manual frame witnesses are
retained. Do not remove the build while the manual window is open or acceptance is pending.
