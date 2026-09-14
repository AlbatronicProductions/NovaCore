# Predeclared Release clock-allocation stability campaign

Plan recorded before execution: exactly eight fresh full Release Simulation processes, stopping early only if a nonzero clock allocation reproduces. Same candidate bytes, command, working directory, group order, clock gate, helper and runtime configuration. No rebuild, source/permanent-test edit, instrumentation, warmup change, runtime-setting change, baseline control, standalone test or manual acceptance.

Passing processes retain concise facts and UTF-8 captured-output SHA-256 only; exact stdout/stderr is retained only on failure. A later pass cannot invalidate the original 12,336-byte witness. Eight passes would be bounded characterization, not proof of impossibility or a correction.

The [structured plan and results](clock-allocation-stability.json) records the 1,974-file candidate input-tree digest, individual Release closure fingerprints, existing identity-manifest fingerprint, external environment and registered group order. The active evidence package is excluded from the input-tree digest so recording results does not change the measured input identity. No executable input is excluded by that rule.

Status at plan creation was NOT YET EXECUTED. Final status: **8/8 PASS. REVISE — STABILITY CHARACTERIZATION COMPLETE.** Candidate remains UNBANKED; manual acceptance DEFERRED.

## Eight fresh processes

All times are UTC on 2026-09-14. Each process used .NET 10.0.12, exited 0 with empty stderr, completed all 67 registered groups and immediately followed Timeline topology when entering the clock group.

| Process | PID | Start UTC | End UTC | Full result | Clock bytes | Entry / exit |
|---|---|---|---|---|---|---|
| 1 | 3772 | 14:20:30.625 | 14:20:37.209 | 67/67 PASS | 0 | PASS / PASS |
| 2 | 37788 | 14:20:37.541 | 14:20:44.198 | 67/67 PASS | 0 | PASS / PASS |
| 3 | 30436 | 14:20:52.285 | 14:20:58.795 | 67/67 PASS | 0 | PASS / PASS |
| 4 | 6028 | 14:20:59.101 | 14:21:05.749 | 67/67 PASS | 0 | PASS / PASS |
| 5 | 65516 | 14:21:15.598 | 14:21:22.232 | 67/67 PASS | 0 | PASS / PASS |
| 6 | 32564 | 14:21:22.533 | 14:21:29.230 | 67/67 PASS | 0 | PASS / PASS |
| 7 | 42180 | 14:21:39.889 | 14:21:46.536 | 67/67 PASS | 0 | PASS / PASS |
| 8 | 42868 | 14:21:46.842 | 14:21:53.535 | 67/67 PASS | 0 | PASS / PASS |

Clock values: **0 / 0 / 0 / 0 / 0 / 0 / 0 / 0 bytes**. Every process was fresh; none was a retry. No ninth campaign process was run.

The harness's pre-existing timeline report was the same in each process: allocated=0; valid=True; pending=100; cancelled=9900; GC-before=(9,8,8); GC-after=(10,9,9). These are existing upstream harness facts, not new clock subwindow instrumentation or proof of the original failure mechanism.

## Controlled identity and environment

- HEAD/main/origin/main/remote main: `89a432ce3b96a5dcb0751cfdc26bfd6776c883a1`.
- Branch: `codex/segmented-powered-free-flight`.
- OS: Microsoft Windows 10.0.26200; x64; 24 logical processors (both runtime-visible and OS-reported).
- SDK 10.0.303; each loaded `Microsoft.NETCore.App/10.0.12/coreclr.dll`, file version `10,0,1226,42308`, runtime commit `95017c711e6afc1085133d440e42b4bd78155701`.
- Working directory `E:\NovaCore`; command `C:\Program Files\dotnet\dotnet.exe tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll`.
- Relevant inherited variables: DOTNET_ROOT=`C:\Program Files\dotnet`, DOTNET_ROOT(x86)=`C:\Program Files (x86)\dotnet`. No COMPlus, CORECLR/COR profiler or other DOTNET-prefixed settings were present. None were changed.
- GC mode: **UNAVAILABLE externally**. The runtimeconfig contains no explicit System.GC.Server setting; no default-mode inference or GC probe was substituted.

Input-tree SHA-256 before/after (1,974 files):

`092C10C32BC0D219F0D6A1F080FF6D89EBDA53294608AE009D3286F995C59144`

Release test DLL:

`FBE5389E45F80F63014B939B13976F630D7B9DDE59604B29B7F0FCE976E312FF`

Release test apphost executable (the launch route uses dotnet plus the DLL):

`825450211100EFDD200B43CC12F8E67244CDF357DEC0E497E35F9AE9A21C6C8D`

Release production Simulation DLL:

`3CB3491662FA24E91E304E50349EDD059CA5D2D5E57AD9264D553038455F65E2`

Postflight verified all 13 files in the recorded Release test deployment directory, all 46 existing candidate/architecture/binary fingerprints and the full input-tree digest unchanged. All 64 tag references, including M14.21-M14.24, remain unchanged. No source/permanent-test changes, staging, commit, tag, baseline tree, standalone test or Blender work occurred.

## Conclusion and limits

**The original failure did not recur in nine total authorized reproduction/stability processes after the original failure: one earlier follow-up plus this eight-process campaign.**

The original 12,336-byte witness remains valid. This bounded characterization shows it did not recur readily under these fresh-process executions; it does not establish mathematical impossibility, a fix, an allocation-accounting mechanism, or candidate exoneration. Cause remains **UNRESOLVED**. No allocation attribution or correction was attempted.

No full passing stdout/stderr logs were written to disk. A total of 353,455 UTF-8 bytes of passing stdout was captured transiently and reduced to the retained facts/hashes; stderr was empty. The two small stability files and this README index update are the only new evidence changes. Previous evidence remains intact, and the combined current evidence package remains within its 192 KiB budget.

Exact reproduction recipe, retained for provenance rather than authorization to extend the campaign:

```powershell
Set-Location -LiteralPath 'E:\NovaCore'
dotnet tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll
```

The external launcher uses inherited environment and captures stdout/stderr; one read-only loaded-CoreCLR module inventory records runtime identity. It adds no target instrumentation. Eight sequential fresh instances constituted this campaign, with a predeclared immediate stop on nonzero clock bytes. No builds occurred before or during it.

**STOP FOR PROJECT CONTROL. Do not resume manual acceptance or remaining qualification automatically. Do not bank or assign a milestone.**
