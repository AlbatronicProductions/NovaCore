# Matched banked/candidate timeline allocation discriminator

2026-09-09. **CANDIDATE-SPECIFIC COUNTER EFFECT.**

**NOT AUTHORIZED — CANDIDATE-SPECIFIC EFFECT**: the no-GC measurement boundary
was not applied. No permanent production or test file changed in this follow-up.

## Preserved authority

Canonical branch: `codex/contact-response-transaction`.
HEAD/main/origin/main: `35c2693e0f5ca01c02c4cc5e73f68323e66ed8e7`.
This is the README-only commit after the unchanged M14.3 tag
`m14.3-spacecraft-terrain-contact-observations`, target
`79978cf8b0fd783d43a661df6df53a2c0202af4a`.

All 14 candidate fingerprints in `../candidate-fingerprints.json` match before
and after: 12 production files and two permanent test files. The existing pending
candidate and staged state were preserved. No branch switch, staging, commit,
merge, tag, push, banking or solver work occurred.

## Matched preparation and authorization

The banked test only exposed a composite allocation/invariant assertion. Project
Control explicitly authorized **identical minimal reporting instrumentation in
disposable copies**, preserving permanent files, workload, warmup and measurement
boundary. This exception did not authorize a no-GC region or workload change.

Both disposable trees began from a Git archive of the same banked baseline:
Directory.Build.props, Core, EphemerisFormat, Simulation and Simulation.Tests.
Only the 12 candidate production files were overlaid in the candidate tree.
Both used the banked test driver and supporting test files, eliminating the new
candidate test group's setup as an A/B difference. Their reporting-enabled
Program.cs files had identical SHA256:

`59A8EC6369D1F3141D68C5A97B61C4E6FE7EB250EE416FE287B79A463025E42D`.

The driver preserves the original five preceding test groups and the complete
timeline test, stopping after that group. The allocation workload is unchanged:
20,000 preallocated capacity; schedule warmup IDs 1..100; measured schedule/cancel
IDs 101..10,000; the same per-thread counter calls and zero-byte threshold.
GC counts are captured outside the counter window. JSON reporting and metadata
inspection occur after the ending counter read. Reporting can perturb runtime/JIT
conditions; matching controls do not prove it is entirely nonperturbative.

SDK: 10.0.303. All four disposable builds passed without warnings/errors.
Runtime: .NET 10.0.11, Windows X64. Executable:
`C:/Program Files/dotnet/dotnet.exe`. All child processes used working directory
`E:/NovaCore` and inherited the same environment without overrides.
Environment SHA256 and relevant settings are in `environment.json`. No profiler,
no-GC region, tiering/PGO change or runtime-configuration change was used.

Invocation in every cell was the same form:

```
dotnet <variant>/tests/NovaCore.Simulation.Tests/bin/<configuration>/net10.0/NovaCore.Simulation.Tests.dll
```

The predeclared plan was executed exactly: Debug then Release, three repetitions
per configuration, alternating banked/candidate fresh processes. **12 runs total;
no added repetitions.** `results.json` retains exact paths, PIDs, assembly hashes,
environment identity, configuration, raw counter results and all requested counts.

## Results

Every row: invariants **PASS**, pending **100**, cancellations **9,900 / 9,900**.

| Production | Configuration | Run | PID | Allocated-counter bytes | GC before -> after | Event bytes |
|---|---|---:|---:|---:|---|---:|
| Banked | Debug | 1 | 42676 | 0 | (1,1,1) -> (1,1,1) | 112 |
| Banked | Debug | 2 | 42800 | 0 | (1,1,1) -> (1,1,1) | 112 |
| Banked | Debug | 3 | 36100 | 0 | (1,1,1) -> (1,1,1) | 112 |
| Candidate | Debug | 1 | 16152 | 8,104 | (2,2,2) -> (2,2,2) | 312 |
| Candidate | Debug | 2 | 42724 | 8,104 | (2,2,2) -> (2,2,2) | 312 |
| Candidate | Debug | 3 | 30152 | 8,104 | (2,2,2) -> (2,2,2) | 312 |
| Banked | Release | 1 | 39776 | 128 | (1,1,1) -> (1,1,1) | 112 |
| Banked | Release | 2 | 39744 | 128 | (1,1,1) -> (1,1,1) | 112 |
| Banked | Release | 3 | 34512 | 128 | (1,1,1) -> (1,1,1) | 112 |
| Candidate | Release | 1 | 34948 | 8,232 | (2,2,2) -> (2,2,2) | 312 |
| Candidate | Release | 2 | 38924 | 8,232 | (2,2,2) -> (2,2,2) | 312 |
| Candidate | Release | 3 | 37524 | 8,232 | (2,2,2) -> (2,2,2) | 312 |

Banked Debug processes exited 0. Nonzero-counter cases failed the unchanged
zero-byte requirement, exiting with the unhandled managed assertion result
(-532462766 from direct process execution). They still exposed complete independent
timeline results before the assertion. No failure was reclassified as a pass.

## Classification and limits

**CANDIDATE-SPECIFIC COUNTER EFFECT.**

Debug meets the specified zero-baseline/nonzero-candidate discriminator in all
three repetitions. Release has an additional stable 128-byte result shared by
both arms, with the candidate still exceeding baseline by **8,104 bytes in every
pair**. Release therefore does not literally satisfy a zero-byte baseline; its
nonzero results must not be collapsed into 'the same behavior' merely because
both fail. There was no zero/nonzero fluctuation within any configuration/arm.

The shared Release 128 bytes and the candidate-associated difference are both
unattributed. The reporting instrumentation is a possible contributor to the
Release result, not a demonstrated cause. No object, call stack, GC-accounting
event or production allocating instruction was identified. This result establishes
separation under the matched experiment, not an object-allocation diagnosis.

No 128-byte or 8,104-byte subtraction is applied to any test. Prior M14.2 context
retirement evidence cannot remove the present candidate-specific discriminator.
Unchanged collection counts do not exclude completion of an existing collection.

## Structural comparison and next payoff decision

Measured scheduled-event size: **112 -> 312 bytes**, a 200-byte increase.
At two 20,000-record arrays this represents **4,480,000 -> 12,480,000 element
bytes**, excluding headers and other storage. The candidate contains an inline
200-byte contact intent with managed reference fields; generic event copies and
GC layout also change. New closed routing and transaction methods are additional
known source differences. The measured loop uses marker events, not contact
response evaluation.

The size/layout difference is concrete. Its causal relationship to the counter
effect remains **UNPROVEN**; neither shrinking it nor changing a permanent test
boundary is authorized by this result.

Cheapest proposed next discriminator, **not executed**: one disposable layout-only
control retaining banked scheduling/cancellation behavior while matching the
candidate payload's size and reference layout. This could separate representation
effects from new transaction behavior without adopting any production redesign.
It would still not attribute managed objects versus runtime accounting.
Engineering estimate: roughly 15-30 minutes preparation/review and under one
minute for a similarly bounded six-process check, based on this run's duration.
The shared Release reporting result must remain visible in that design. Project
Control must re-run PAYOFF before authorizing it or a different discriminator.

## Boundary action and validation

**NOT AUTHORIZED — CANDIDATE-SPECIFIC EFFECT**.

- Permanent test change: none.
- Production source change: none.
- Deliberate allocation control: not run; Step 3 was not entered.
- Qualified affected-test 5+5 repeats: not run.
- Qualified full-Simulation 3+3 repeats: not run.
- Remaining full-solution/contact/translation/contact-generation/ReferenceFrames/
  Precision/Graphics gates: not rerun; their prior results remain in the parent
  report. This ticket's conditional 'after qualified pass' stage was not reached.
- Four disposable comparison builds: PASS.
- All original 14 candidate production/test fingerprints: unchanged.
- git diff --check: PASS; original line-ending warnings only.
- Staged diff: empty.

## Retention and reproduction

This follow-up has a separate **48 KiB retention budget** for this concise report,
results, environment identity and two reproduction scripts. The parent report and
its fingerprints retain their existing budget. No profiler output exists.

`prepare.ps1` applies only the authorized disposable observation/reporting change
to identical banked test drivers. `run-ab.ps1` implements the fixed 12-process plan
and records output without modifying child-process runtime settings. To reconstruct,
create a fresh ignored comparison directory, Git-archive the baseline paths listed
above into `baseline` and `candidate`, overlay the fingerprinted candidate source
files into `candidate`, place these scripts there, run prepare, and build both
configurations before running the fixed plan. This is a reproduction recipe, not
authorization for more runs now.

Disposable source/build trees and redundant stdout/stderr are reconstructible from
Git, the preserved unbanked candidate and these scripts. Their exact temporary
root was `E:/NovaCore/.codex/contact-allocation-discriminator`; it is not a runtime
or regression dependency. Retained result copies were hash-verified.

Automatic approval review rejected the guarded deletion command as **blocked by
policy** before execution. No alternate deletion method was attempted. The ignored,
untracked temporary directory remains: **697 files, 71,133,883 logical bytes**,
zero reparse points. It contains only this comparison's disposable source/build
copies and output. Nothing was disposed. Project Control may remove this exact
reviewed directory manually:

```powershell
Remove-Item -LiteralPath 'E:\NovaCore\.codex\contact-allocation-discriminator' -Recurse -Force
```

Lead judgment: **ESCALATE TO PROJECT CONTROL**.

**CONTACT RESPONSE TRANSACTION CANDIDATE — FOLLOW-UP REQUIRED**
