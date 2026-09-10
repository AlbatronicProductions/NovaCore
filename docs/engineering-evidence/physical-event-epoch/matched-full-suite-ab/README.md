# Matched full-suite A/B allocation discriminator

2026-09-09 EDT (runs 2026-09-10 00:30:23–00:30:46 UTC).

**STABLE CANDIDATE-SPECIFIC ALLOCATION-COUNTER SEPARATION**.
Lead judgment: **ESCALATE TO PROJECT CONTROL**.
No production/test correction or measurement migration performed. Candidate remains
unbanked, with full Simulation acceptance uncleared.

## Baseline and comparison definition

HEAD/main/origin/main: `b53d48025cfb8142cb32a6fc6fceb6018e33cfcd`, banked M14.5,
tag `m14.5-isolated-analytical-contact-response`.
Branch: `codex/physical-event-epoch`. M14.1–M14.5 tag targets reverified unchanged.
All five accepted source/test fingerprints match the [candidate report](../README.md).
All 14 pre-existing changed/untracked candidate files matched their preflight
fingerprints after the matrix, including documentation and prior evidence.

Project Control explicitly clarified the suite difference before execution:
**preserve both full suites and common-group order**. Baseline has 36 groups;
candidate has 37, including its new epoch group before the target gate. Omitting
that group would not reproduce the candidate/full-suite combination at issue.
The common 36 groups were verified to retain identical relative order. This is
an association test of the complete candidate, including its test registration;
it does not isolate production changes from test-order effects.

Two fresh disposable trees were created under
`E:\NovaCore\.codex\physical-epoch-matched-ab\baseline` and `candidate`.
Both began with `git archive HEAD` of Directory.Build.props, Core, EphemerisFormat,
Simulation and Simulation.Tests. Candidate received only the five accepted
source/test overlays. All copying and both Debug builds finished before the matrix.
Both builds passed with zero warnings/errors. Independent output directories were
used; no build ran during the six executions.

## Reporter, runtime and environment controls

The previously qualified reporting block was applied identically to both disposable
Program.cs files, plus the same unused isolation selector. Reversing those two
edits reproduced each state's original test file after newline normalization.
The target warmup, 499 measured calls, workload, zero-byte threshold and exact
clock/debt/count assertions were unchanged. Scalar snapshots precede the counter;
report formatting follows the closing counter read. The original composite
assertion still halts execution on a nonzero delta.

Reporter block SHA-256:
`5722E8A1F82D1B453110D6179D1B813B7DE7D165E62F89B6E7AB42C8A050BBC2`.
The [reporting recipe](../RETENTION.md#reproduction-without-disposable-patches)
preserves the operand and insertion contract; the historical reporter hash is retained above.
No new no-GC boundary, GC/JIT instrumentation, profiler, callbacks, runtime flags,
tiering/PGO changes or extra warmup were introduced. Existing full-suite accounting
regions, including the candidate epoch group's existing test region, were retained.
The target debt-service measurement remained outside any newly introduced region.

.NET SDK **10.0.303**, MSBuild **18.6.14**, Windows x64. Each child used
`C:\Program Files\dotnet\dotnet.exe` and loaded
`C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.12\coreclr.dll`.
Loaded CLR paths were read from each process's module list externally, without
attaching a debugger or changing the test process. Both runtimeconfig files were
identical, SHA-256 `256DDA2706E1151BB95987B6B5BE610BF68D64E058285B19E7E958222FC90414`.

Each process used the same inherited environment snapshot, verified against
SHA-256 `B204C3C0C4F4BF5BD52F35DF5890EEC985DE607F7EF68BC4B17E8E5000BFE336`.
No profiler or runtime-tuning variables were present. Environment values/secrets
are not retained. Working directory was `E:\NovaCore`; application arguments were
empty in every run. The only command path difference selected the baseline or
candidate test assembly. The same runner started a fresh process, awaited its exit
and checked results before starting the next. It refused a second invocation.

Full test orders, runtime identity and environment digest: [preflight.json](preflight.json).
Per-run operands, UTC timestamps, PIDs, exit codes and CLR paths: [matrix.json](matrix.json).

## Exact matrix

| Sequence | State | Allocation bytes | Clock | Debt | Processed | Pending | Completed |
| ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | Baseline | 0 | 50000 | 0 | 5000 | 0 | 499 |
| 2 | Candidate | 8160 | 50000 | 0 | 5000 | 0 | 499 |
| 3 | Baseline | 0 | 50000 | 0 | 5000 | 0 | 499 |
| 4 | Candidate | 8160 | 50000 | 0 | 5000 | 0 | 499 |
| 5 | Baseline | 0 | 50000 | 0 | 5000 | 0 | 499 |
| 6 | Candidate | 8160 | 50000 | 0 | 5000 | 0 | 499 |

Expected final values: 0 allocated bytes, 50,000 clock ticks, zero debt, 5,000
processed events, zero pending events and 499 completed measured calls. All five
non-allocation contracts passed in every run. Before each measured loop: clock
100, debt 0, processed 10, pending 4,990. Managed thread ID was 2 throughout.

Fresh PIDs in order: **41204, 35208, 39960, 2828, 24876, 46756**.
Baseline exit codes were 0; all 36 baseline groups completed in each execution.
Candidate exit codes were -532462766, exclusively the expected target allocation
assertion. Each candidate run passed nine groups, failed the target group and did
not reach the remaining 27. No unrelated suite failure or non-allocation failure
occurred. Exactly six executions; no early allocation stop and no hidden retry.

## Interpretation and next decision

Baseline: **0 / 0 / 0**. Candidate: **8,160 / 8,160 / 8,160**.

The candidate/full-suite combination shows stable separation across these three
matched pairs. This is a reproducible candidate-associated distinction, not proof
of an allocating object, runtime accounting mechanism, or defective production
method. Test registration and production changes were not independently varied.
Three pairs do not establish behavior on all machines or future runs.

Comparison quality: PASS. Reporter identical; runtime/environment/configuration
matched; clarified test order preserved; six unique fresh processes; builds
completed beforehand; output hashes checked before each run; no state switching
in the canonical checkout; no profiler, retry or environment drift observed.

**STOP — candidate-specific cause requires Project Control decision.**
Do not apply no-GC or migrate the permanent measurement boundary on this evidence.
No allocation attribution or implementation was attempted. The historical 8,160-byte
precedent is not treated as the mechanism for this observation.

## Reproduction identity

The disposable runner is `.codex/physical-epoch-matched-ab/run-matrix.ps1`.
It is diagnostic output, not a permanent runtime/test dependency. The controlled
sequence was B → C → B → C → B → C, each invoking the state's Debug test DLL
through the same dotnet executable with no test selector. Both source trees and
the runner remain available for Project Control review; rerunning is not authorized
by this ticket. Retained reporter source, manifests and this procedure allow later
reconstruction without preserving comparison binaries permanently.

| Artifact | SHA-256 |
| --- | --- |
| Baseline instrumented Program.cs | `7F1A8572265FD826F377718C3539F78E0EB1F62CC8E0613FD93C32157092336A` |
| Candidate instrumented Program.cs | `EA5E18C328387F867C1A9D34CACD99D461BBF503EBEE7E928E4E58E7C6115E86` |
| Baseline Simulation.Tests.dll | `BC50CF3C395F382AB270E9CEC5ADECA67E9EE20AACF1DDDE0125CAD09E2F7923` |
| Candidate Simulation.Tests.dll | `5F52D9A0A0E755318E8269CB8C623D14A03AFCF32A578CED0905A59D630098E4` |
| Baseline Simulation.dll | `765D19932F4B03F39967818F19C0AC15996B9A454A4465DFBCE768AAD9C796A9` |
| Candidate Simulation.dll | `A6BC742B54E116BA9720F9C1F3685497C804CFA821A9208700911ADDA3D9158F` |

## Evidence and Git boundary

Retain this concise report, matrix.json and preflight.json. No canonical production,
test, current-facing documentation or prior evidence file was changed in this
ticket; only these three evidence files were added.

New disposable output: `.codex/physical-epoch-matched-ab`, **550 files /
42,878,848 logical bytes**. Existing disposable output: `.codex/physical-epoch-gate`,
**361 files / 38,126,642 bytes**, preserved unchanged. Combined: **911 files /
81,005,490 bytes**. These are comparison source copies, independent builds,
runner/reporting setup and bounded logs. They are not active runtime dependencies.
No deletion was attempted; the previous policy rejection was not retried.
After review, Project Control may manually remove only these two disposable trees.

Final `git diff --check`: PASS, line-ending warnings only. Nothing staged,
committed, tagged, merged, pushed, banked or assigned a milestone number.
