# Reproduction and reviewed cleanup

Use `E:\NovaCore-ExactEvent-Additive`, branch
`codex/exact-event-spacecraft-motion-additive`, main/HEAD
`00f226096b61a117184464b5619e464d39789574`, and the unbanked candidate fingerprints
in this package. Preserve `E:\NovaCore` unchanged. SDK 10.0.303; net10.0 normal patch
roll-forward to installed Microsoft.NETCore.App 10.0.12. No profiler, EventPipe,
tiering/PGO or global runtime/GC override. Tests alone use the existing checked
allocation helper. No interactive or renderer/GPU work is required.

## Declared execution sequence

1. Reconcile inventory rows 19/20/21/22/41 against the two test files. Verify the
   332 production hashes and unchanged helper; verify original group ordering.
2. Lunar-only representative: one Debug and one Release process, each including
   the existing positive control. Stop at first failure. In this ticket this ran
   before the other four migrations; snapshot hashes and results are retained.
3. Finish only equivalent gates. Build Debug/Release solution. Focused whole family:
   one fresh process per configuration, stop on first failure. Translation's opt-in
   performance method is explicitly included by this selector.
4. Fresh final Release: exactly five normal full-suite processes, no build between
   runs and no retries. Stop on first failure. Earlier matrices do not count.
5. Only after 5/5, one bounded Candidate E performance process. No indefinite tuning.

```powershell
Set-Location -LiteralPath 'E:\NovaCore-ExactEvent-Additive'
git status --short
git rev-parse HEAD main origin/main
dotnet --version
dotnet --list-runtimes
Get-ChildItem Env: | Where-Object Name -Match '^(CORECLR|COR_|COMPlus|DOTNET_)'
function Invoke-DotNetGate {
    & dotnet @args
    if ($LASTEXITCODE -ne 0) { throw "Build/test gate failed; stop without retry" }
}
Invoke-DotNetGate build NovaCore.sln -c Debug --nologo
Invoke-DotNetGate build NovaCore.sln -c Release --nologo

# Run once per configuration, respecting failures at every step:
Invoke-DotNetGate tests/NovaCore.Simulation.Tests/bin/Debug/net10.0/NovaCore.Simulation.Tests.dll --lunar-timed-only
Invoke-DotNetGate tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll --lunar-timed-only
Invoke-DotNetGate tests/NovaCore.Simulation.Tests/bin/Debug/net10.0/NovaCore.Simulation.Tests.dll --timed-measurements-only
Invoke-DotNetGate tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll --timed-measurements-only

# Exactly five fresh processes; a nonzero exit terminates the matrix.
for ($run = 1; $run -le 5; $run++) {
    Invoke-DotNetGate tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll
}
Invoke-DotNetGate tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll --exact-event-motion-performance
git diff --check
```

Native build prerequisites, if absent in another clean checkout, follow
[the earlier additive recipe](../exact-event-additive-motion/reproduce.md). No
copy of the old build tree is needed. Parent-side stdout capture is optional;
do not insert reporting into measured loops.

## Witness interpretation

`TIMING` reports the original normal-runtime timing. Only lunar has a hard latency
bound. `ORDINARY_ALLOCATION gate=timed-*` reports exact delta and successful entry/exit.
`TIMED_RESULT` independently reports success/checksum/iteration predicates.
`ORDINARY_CONTROL` deliberately reports nonzero bytes and must not be counted as
a failed zero-allocation gate. Translation counts 1/256 share one source window.
The normal runner has 38 groups; nested test messages add extra PASS lines, so
count exact registered group names, not every line starting with PASS.

The before-edit Program can be reconstructed from the banked file plus the earlier
additive candidate's three focused selectors and appended group; translation's
before-edit file is banked source. Candidate exact-event test/production files
retain their prior hashes. The two temporary before-* copies are not permanent
implementation authority. No reproduction of the 12,336 mechanism is prescribed.

## Consolidated manual cleanup — not executed

After Project Control accepts the technical result, review the literal paths and
run this one command if authorized. It contains only the seven inspected diagnostic
scratch directories. It excludes candidate source, docs, assets, native/managed
development builds and Git data. Do not broaden it or retry blocked automatic deletion.

```powershell
Remove-Item -LiteralPath `
  'E:\NovaCore\.codex\exact-event-sas-ab', `
  'E:\NovaCore\.codex\exact-event-attribution', `
  'E:\NovaCore\.codex\exact-event-components', `
  'E:\NovaCore\.codex\exact-event-banked-baseline', `
  'E:\NovaCore\.codex\exact-event-second-components', `
  'E:\NovaCore-ExactEvent-Additive\.codex\exact-event-additive-validation', `
  'E:\NovaCore-ExactEvent-Additive\.codex\timed-measurement-validation' `
  -Recurse -Force
```
