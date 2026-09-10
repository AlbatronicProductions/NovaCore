# Reproduction and handoff

This is a recipe, **not authorization to resume after the recorded final failure**.
Project Control must decide whether another run has payoff. Do not retry runs to
manufacture acceptance. Do not touch the preserved old worktree's source or branch.

Use the candidate files identified by `identity.json`, baseline
`00f226096b61a117184464b5619e464d39789574`, and the new worktree only:

```powershell
Set-Location -LiteralPath 'E:\NovaCore-ExactEvent-Additive'
git status --short
git branch --show-current
git rev-parse HEAD main origin/main
dotnet --version
dotnet --list-runtimes
Get-ChildItem Env: | Where-Object Name -Match '^(CORECLR|COR_|COMPlus|DOTNET_)'
```

Banked files must match `baseline.json`. `Program.cs` differs only by three
selectors and the appended 38th group. Preserve existing workload/warmup/ordering.
No profiler or runtime tuning variables. Use SDK 10.0.303 and default .NET 10.0.12.

Compile focused implementation and execute its independent oracle once:

```powershell
dotnet build tests/NovaCore.Simulation.Tests/NovaCore.Simulation.Tests.csproj -c Release --nologo
dotnet tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll --exact-event-motion-only
```

Early matrix: exactly three fresh processes using the following command, stop on
first failure, no builds between runs. This ticket completed all three successfully.

```powershell
dotnet tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll
```

For full solution deployment on a clean checkout, in Visual Studio x64 Developer
PowerShell prepare unchanged native dependencies locally (do not copy old build trees):

```powershell
cmake -S native/NovaCore.Native -B build/native-ninja -G Ninja -DCMAKE_BUILD_TYPE=Debug
cmake --build build/native-ninja --target NovaCore.Native --parallel 4
cmake -S native/NovaCore.Native -B build/native-ninja-release -G Ninja -DCMAKE_BUILD_TYPE=Release
cmake --build build/native-ninja-release --target NovaCore.Native --parallel 4
dotnet build NovaCore.sln -c Debug --nologo
dotnet build NovaCore.sln -c Release --nologo
```

Only after early 3/3, the focused selectors (each Debug and Release) are:
`--exact-event-motion-only`, `--translation-only`, `--rigid-rotation-only`,
`--contact-only`, `--contact-response-only`, `--isolated-contact-only`,
`--physical-event-epoch-only`. Run the ReferenceFrames and Precision test DLLs in
each configuration, plus one full Debug Simulation process. These completed here.

Final matrix: exactly five fresh normal Release full-Simulation processes, no
arguments, no build between runs, first failure stops everything. Here **run 1
failed**, so only one process was executed. Direct DLL execution exits nonzero
with the unchanged assertion. The stdout witness is retained with its original
whole-log hash in `validation.json`; there is no attribution instrumentation.

Only a future fully accepted sequence permits the prepared bounded performance
selector `--exact-event-motion-performance`: 41 samples × 256 calls, 4,096 warmups,
analytical and RK4 fixtures, canonical wrapper/rational/batch32 cases, followed by
separate checked allocation measurements (8,192 calls each) and positive control.
It was **not run** here. Timing is per spacecraft evaluation, including batch members.

## Reviewed manual scratch cleanup

Run only after Project Control accepts the technical disposition and reviews these
literal directories. All six were inspected and classified as disposable build,
comparison or log output. This command was **not executed**. It does not include
old source, candidate files, docs or the new native/managed development builds.

```powershell
Remove-Item -LiteralPath `
  'E:\NovaCore\.codex\exact-event-sas-ab', `
  'E:\NovaCore\.codex\exact-event-attribution', `
  'E:\NovaCore\.codex\exact-event-components', `
  'E:\NovaCore\.codex\exact-event-banked-baseline', `
  'E:\NovaCore\.codex\exact-event-second-components', `
  'E:\NovaCore-ExactEvent-Additive\.codex\exact-event-additive-validation' `
  -Recurse -Force
```

No automatic deletion retry or cleanup has occurred. Keep existing old concise
evidence at `E:\NovaCore\docs\engineering-evidence\exact-event-spacecraft-motion`.
