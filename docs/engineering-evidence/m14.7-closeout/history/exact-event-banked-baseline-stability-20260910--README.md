# Banked-baseline Release stability discriminator

**BANKED BASELINE RELEASE STABLE — 5 / 5 CLEAN**

**Lead judgment: PASS** for this bounded baseline discriminator. Candidate E remains unbanked and its failed 5/5 acceptance remains uncleared.

## Baseline and purity

- Banked/reconciled main: `00f226096b61a117184464b5619e464d39789574`.
- M14.6: `m14.6-physical-event-epochs` -> `d0b9f9d69d55b8ba85a7b24a44f93e03cea2dcf6`.
- Canonical candidate branch remained `codex/exact-event-spacecraft-motion`; HEAD/main/origin and M14.1–M14.6 tag targets unchanged.
- SDK 10.0.303; all five children loaded Microsoft.NETCore.App 10.0.12 from the same coreclr.dll path as the failed Candidate E acceptance.

A **fresh Git archive export directly from the banked SHA** was used, not a candidate-derived subtraction or reused comparison tree. The export contains the complete 165-file Simulation source/project/resource dependency closure: Directory.Build.props, NovaCore.Core, NovaCore.EphemerisFormat, NovaCore.Simulation and NovaCore.Simulation.Tests. It is an equivalent isolated source export, not a registered Git worktree or full-repository asset checkout.

Every exported file was checked against its banked Git blob. One matched raw bytes; 164 textual files matched after accounting for standard CRLF working-tree conversion. The initial raw-blob-only verifier rejected the first CRLF file; this was resolved before any build/run by verifying Git-equivalent text without rewriting source. No content difference or extra source file was accepted.

No Candidate E PhysicalEventDuration, SpacecraftPhysicalEventMotion/test, exact-event motion API, documentation changes, observer/probe hooks or component-control substitutions were present. Candidate code and observer types were also absent from built assembly metadata. No candidate assembly was executed by the metadata checks.

Banked tests retain 37 groups. Their order exactly matches the first 37 groups of the failed candidate suite; Candidate E's added last group is correctly absent. OrdinaryAllocationMeasurement matches the accepted helper, including its 1 MiB checked region, entry/exit failure behavior and exact-zero threshold. No source, helper, warmup, ordering, reservation or runtime configuration was modified.

## Why this discriminator has payoff

Candidate normal acceptance was 2/5 clean, with SAS 12,336 twice and attitude integration 12,336 once. Historical candidate witnesses also included impulse evaluation. Yet the subsequent unchanged candidate arm passed 3/3 while its canonical-restored control failed SAS twice. No necessary production component was isolated. Prior attributed candidates passed without exposing a 12,336 population.

A current clean baseline population was therefore needed before attributing more changes to Candidate E. This ticket ran only banked baseline processes and did not resume candidate acceptance or forensics.

## Build and execution conditions

One normal Release build of the baseline Simulation test project and dependencies: **PASS, zero warnings/errors**. No builds during the matrix.

Each child used:

```powershell
& 'C:\Program Files\dotnet\dotnet.exe' 'E:\NovaCore\.codex\exact-event-banked-baseline\B\tests\NovaCore.Simulation.Tests\bin\Release\net10.0\NovaCore.Simulation.Tests.dll'
```

Working directory: the exported baseline root, `E:\NovaCore\.codex\exact-event-banked-baseline\B`. As in normal candidate acceptance, this was the full suite with an absolute assembly argument, no selector and repository-root-relative working-directory semantics.

The complete inherited environment hash matched the failed candidate acceptance exactly:
`E6BA15A56660573C429F8C664E2291B655AE4B755F62B28837051D17AE7A7B2B`.
Runtimeconfig hash also matched. No environment overrides, profiler registrations, observer, GC/JIT trace, special pre-touch or initialization. The parent captured normal stdout/stderr and read the loaded runtime module path; no diagnostic code was added to the child.

## Five-run baseline matrix

| Run | PID | Result | First failure | Allocation gate | Bytes | Entry | Exit | Other contracts |
|---|---:|---|---|---|---:|---|---|---|
| 1 | 12396 | 37/37 PASS | None | All zero-required gates passed | 0 | PASS | PASS | All PASS |
| 2 | 42148 | 37/37 PASS | None | All zero-required gates passed | 0 | PASS | PASS | All PASS |
| 3 | 47132 | 37/37 PASS | None | All zero-required gates passed | 0 | PASS | PASS | All PASS |
| 4 | 28184 | 37/37 PASS | None | All zero-required gates passed | 0 | PASS | PASS | All PASS |
| 5 | 49356 | 37/37 PASS | None | All zero-required gates passed | 0 | PASS | PASS | All PASS |

Five distinct fresh processes, no retries or selective reruns. Exit code 0 in all five. All sources and binaries remained fixed. Each run loaded `C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.12\coreclr.dll`.

## Baseline allocation pattern

Every run independently reported zero for all **31 zero-required ordinary checked windows**. Other permanent allocation and correctness assertions also passed as part of the complete 37-group suite. All checked no-GC entry/exit requirements passed; failures propagate under the unchanged banked tests.

The existing deliberate positive control reported **152 bytes** in each run, as required. It is not a zero-required production window. No extra control was added.

No nonzero ordinary production-allocation witness, 12,336 event, no-GC failure or non-allocation correctness failure occurred. Normal assertion results do not provide object-level attribution, and none is claimed.

## Comparison to retained Candidate E evidence

| Population | Clean / total | Conditions |
|---|---:|---|
| Current banked baseline | **5/5** | This ticket, normal Release |
| Candidate E final acceptance | **2/5** | Earlier normal Release; SAS twice and attitude once at 12,336 |
| Candidate E component-discriminator arm | **3/3** | Earlier normal Release disposable candidate |
| Candidate-derived canonical-restoration control | **1/3** | Earlier normal Release; SAS twice at 12,336 |
| Attributed Candidate E | **2/2** | Earlier observer-enabled runs; not clean acceptance substitutes |

These earlier populations were not rerun. They do not form a new simultaneous randomized comparison; environment/runtime identity was matched here, but temporal process/machine-state variation remains a limitation.

## Final baseline classification

**BANKED BASELINE RELEASE STABLE — 5 / 5 CLEAN**

This reports the declared five-run outcome, not indefinite reliability or proof that a baseline failure is impossible.

## Candidate E implication

Candidate state remains associated with worse Release acceptance in the observed matrices. The clean banked reference strengthens that association, but does **not** establish direct allocation by candidate production code, a necessary component, a CLR mechanism or a test bug.

Candidate E's hard acceptance requirement remains **5/5 clean Release passes**. Its previous 2/5 result is not waived or replaced. It remains UNBANKED / FOLLOW-UP REQUIRED. No candidate run or performance qualification occurred here.

## VERIFY A — baseline purity

Exact banked SHA and all Git blob identities verified. No candidate code/tests/docs/control edits or observer types. The normal helper, 37-group order and 1 MiB reservation are unchanged. Five distinct PIDs; fixed binary/source hashes; same runtimeconfig, loaded CLR and full environment hash as the failed candidate acceptance. One build before execution, no retries or instrumentation. Comparison purity/quality: PASS.

## VERIFY B — causal language

Strongest challenge: the signal is intermittent and previously varied between normal/attributed/disposable populations. Five clean baseline runs cannot prove that Candidate E production code allocates or that the baseline can never reproduce. The bounded conclusion is association in the observed acceptance results, with mechanism still unresolved. No stronger causal claim is made.

## VERIFY C — acceptance standard

Exact-zero thresholds remain unchanged. No tolerance, retries, 4/5 interpretation or banking waiver. Baseline PASS is not candidate PASS. The prior candidate acceptance failure remains authoritative until its required acceptance is separately cleared.

## Recommended next Project Control decision

Reassess Candidate E mechanism/payoff using these facts together: current baseline 5/5 clean; candidate acceptance 2/5; component subtraction inconclusive; prior global observer non-reproducing/perturbing. Decide the next bounded responsibility explicitly. No new discriminator, attribution or implementation is started automatically.

## Evidence

Retain this report, `preflight.json`, `controls.json`, `matrix.json`, `result.json`, `prepare.ps1` and `matrix.ps1`. They preserve banked identity/purity, configuration and source/binary hashes, every normal gate/group result, process identities, failure absence and reproducibility. Large traces/dumps were not created.

The preparation script exports directly from the banked SHA and verifies the full build closure. It refuses an existing destination unless explicitly checking the existing export; that option never overwrites source. The matrix script refuses an already-started matrix and enforces exact environment/runtimeconfig matching. Any future reproduction requires new authorization and a reviewed fresh destination; do not rerun the completed matrix.

## Disposable output

| Reviewed path | Files | Logical bytes |
|---|---:|---:|
| E:\NovaCore\.codex\exact-event-sas-ab | 557 | 44,067,060 |
| E:\NovaCore\.codex\exact-event-attribution | 290 | 21,478,493 |
| E:\NovaCore\.codex\exact-event-components | 552 | 39,650,561 |
| E:\NovaCore\.codex\exact-event-banked-baseline | 279 | 24,247,109 |
| Total | **1,678** | **129,443,223** |

No cleanup occurred; no blocked deletion retried. All four dedicated scratch roots resolve inside E:\NovaCore\.codex, with no discovered reparse points. They contain disposable source exports, builds, archives and ordinary logs; canonical source/assets/Git/evidence are excluded.

After Project Control accepts the retained result, the consolidated **manual** command is:

```powershell
Remove-Item -LiteralPath `
  'E:\NovaCore\.codex\exact-event-sas-ab', `
  'E:\NovaCore\.codex\exact-event-attribution', `
  'E:\NovaCore\.codex\exact-event-components', `
  'E:\NovaCore\.codex\exact-event-banked-baseline' `
  -Recurse -Force
```

External NovaCore-Research evidence packages are retained and not included.

## Git

Candidate untouched: all 14 accepted canonical fingerprints and all 167 canonical source-closure fingerprints unchanged. Baseline's 165 source hashes and built binary hashes unchanged during/after all runs. Branch remains codex/exact-event-spacecraft-motion; original 8 modified tracked files and 6 untracked candidate files, zero staged. HEAD/main/origin and M14.1–M14.6 unchanged. `git diff --check` PASS, existing LF/CRLF conversion warnings only. The isolated export has no edits relative to banked content; its integrity is checked against Git blobs rather than misrepresenting it as a registered clean worktree.

No stage, commit, merge, tag, push, bank or milestone assignment.

STOP FOR PROJECT CONTROL.
