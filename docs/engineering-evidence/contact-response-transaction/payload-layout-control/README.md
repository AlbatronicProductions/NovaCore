# Disposable timeline payload-layout causal control

2026-09-09. **STRUCTURAL TIMELINE PAYLOAD LAYOUT EFFECT.**

**CONTACT RESPONSE TRANSACTION CANDIDATE — FOLLOW-UP REQUIRED.**
Lead judgment: **ESCALATE TO PROJECT CONTROL**.

## Authority and preservation

- Canonical branch: `codex/contact-response-transaction`.
- HEAD/main/origin/main: `35c2693e0f5ca01c02c4cc5e73f68323e66ed8e7`.
- M14.3 tag `m14.3-spacecraft-terrain-contact-observations` remains at
  `79978cf8b0fd783d43a661df6df53a2c0202af4a`.
- All 14 accepted candidate fingerprints matched before and after: 12 production
  files and two permanent tests. Production/permanent-test changes this ticket: **0**.
- No staging, commit, branch switch, merge, tag, push, banking, agents or profiling.

The prior matched experiment remains the comparator; it was not rerun:

| Arm | Debug bytes, runs 1/2/3 | Release bytes, runs 1/2/3 |
|---|---|---|
| Banked | 0 / 0 / 0 | 128 / 128 / 128 |
| Candidate | 8104 / 8104 / 8104 | 8232 / 8232 / 8232 |

## Exactly what changed in the disposable control

`prepare-layout.ps1` archives the banked commit into a fresh ignored control tree.
Only two source paths differ from the prior banked tree:

1. `SimulationEventPayload.cs`: add a get-only `InertTimelinePayloadLayout`
   property after `ForceRoot`, initialized to `default`. All original constructors'
   inputs, admission logic, payload kinds, factories and compatibility branches remain.
2. New `InertTimelinePayloadLayout.cs`: two inert readonly positional record structs.
   The outer record has the candidate intent's constituent field types and order:
   `SpacecraftId`, `StateRevision`, `SimulationInstant`, `ReferenceFrameId`, two
   `Double3` values, nested provenance, and `uint`. Nested provenance uses the
   same banked `ContactFeatureIdentity`, `PhysicalSurfaceAuthorityIdentity`, and
   `ulong` types/order as the candidate provenance record.

The nested banked identity types carry the same three string-reference slots:
geometry content digest and the two terrain dataset digests. All remain null in
the unused default field. No additional objects are constructed to create size.
The inert records have no validity checks, impulse evaluation, response methods,
history, event routing or transaction implementation. Their compiler-generated
record members are not called by the marker workload. No reflection invokes
contact types. Source search found no candidate `ContactImpulse`/`ContactResponse`
implementation in the control.

This reproduces the constituent value/reference structure and observed total size,
not a separately measured numeric GC-descriptor/field-offset map. The experiment
does not isolate total size from reference scanning or generic value-copy effects.

The generic timeline, scheduling/cancellation methods, event kinds/header and
transaction engine are banked bytes. Only the two paths above differ under `src`.
The existing banked M14.3 contact-observation capability remains; candidate contact
response, paired response mutation and coupled contact history are absent.

## Matching and execution

The previously authorized disposable reporting driver was copied unchanged:
SHA256 `59A8EC6369D1F3141D68C5A97B61C4E6FE7EB250EE416FE287B79A463025E42D`.
It preserves the first five banked groups and the complete timeline group, then
stops. Capacity 20,000; warmup schedules IDs 1..100; measured IDs 101..10,000 are
scheduled/cancelled with the same checks, ordering, counter API and window.
Reporting occurs after the ending counter; GC-before tuple is outside the window.
No extra warmup, no-GC guard, profiler, tiering or PGO changes were used.

Debug and Release control builds passed with zero warnings/errors. Exactly six
fresh processes ran: Debug 1..3 then Release 1..3. No additional repetitions.
The runner rejects an invalid size or timeline/count result rather than extending
the matrix. The prior baseline/candidate matrix was not rerun.

Invocation:

```text
C:\Program Files\dotnet\dotnet.exe E:\NovaCore\.codex\contact-payload-layout-control\control\tests\NovaCore.Simulation.Tests\bin\<Debug|Release>\net10.0\NovaCore.Simulation.Tests.dll
```

Working directory `E:\NovaCore`; .NET 10.0.11; X64; SDK 10.0.303.
Environment hash matches the prior experiment exactly:
`B204C3C0C4F4BF5BD52F35DF5890EEC985DE607F7EF68BC4B17E8E5000BFE336`.
Exact process, runtime, configuration, assembly path/hash and counter fields are
retained in `results.json`; runtime settings are in `environment.json`.

## Six control results

| Configuration | Run | PID | Counter bytes | Invariants | Pending | Cancellations | GC counts before -> after | Record bytes |
|---|---:|---:|---:|---|---:|---|---|---:|
| Debug | 1 | 26160 | 8104 | PASS | 100 | 9900 / 9900 | (2,2,2) -> (2,2,2) | 312 |
| Debug | 2 | 16036 | 8104 | PASS | 100 | 9900 / 9900 | (2,2,2) -> (2,2,2) | 312 |
| Debug | 3 | 39500 | 8104 | PASS | 100 | 9900 / 9900 | (2,2,2) -> (2,2,2) | 312 |
| Release | 1 | 31612 | 8232 | PASS | 100 | 9900 / 9900 | (2,2,2) -> (2,2,2) | 312 |
| Release | 2 | 40776 | 8232 | PASS | 100 | 9900 / 9900 | (2,2,2) -> (2,2,2) | 312 |
| Release | 3 | 35276 | 8232 | PASS | 100 | 9900 / 9900 | (2,2,2) -> (2,2,2) | 312 |

The unchanged zero-byte assertion fails in all six processes after reporting
(exit -532462766). These are successful observations of the blocking counter
effect, **not passing zero-allocation tests**.

Scheduled-event size: banked **112**, candidate **312**, inert control **312** bytes.

## Causal conclusion and limits

**STRUCTURAL TIMELINE PAYLOAD LAYOUT EFFECT.**

Under the matched conditions, inert generic payload structure is sufficient to
reproduce the candidate's exact totals and its +8,104-byte difference from banked
behavior in both configurations. Candidate contact-response execution is not
required. The structural change is causally implicated.

This does not identify managed objects, an allocating instruction, or a runtime
accounting mechanism. Unchanged collection counts do not prove that no existing
collection completed. The shared Release 128 bytes remain unattributed.
Identical reporting can still perturb runtime conditions; no claim extends beyond
the tested driver/environment. The effect is not declared harmless.

No no-GC guard, permanent test change, payload redesign, extra validation campaign
or further discriminator was undertaken.

## Project Control architecture question — deliberately unanswered

Should a relatively rare contact-response event enlarge every generic scheduled
event from 112 to 312 bytes and introduce managed references into that universal
payload?

Project Control must choose whether to accept this representation and separately
qualify allocation measurement, or reconsider payload ownership. That decision
must weigh timeline memory, copy cost, GC scanning/layout, identity/lifetime,
allocation behavior, event frequency and alternative ownership. This experiment
does not choose between those options.

## Evidence lifecycle

Concise retention budget: 32 KiB for this report, results, environment,
source-difference inventory and reproduction scripts. Copies were SHA256-verified.
No bulk logs, binaries or full source copies are added to permanent evidence.
To reproduce later, run retained `prepare-layout.ps1` in a fresh ignored directory,
using the hash-verified prior disposable driver (or reconstruct that driver using
the preceding experiment's retained recipe), build both configurations, then run
`run-layout.ps1`. This recipe does not authorize further runs now.

Disposable inventory (logical bytes; no cleanup retried):

| Exact directory | Files | Bytes |
|---|---:|---:|
| `E:\NovaCore\.codex\contact-allocation-discriminator` | 697 | 71,133,883 |
| `E:\NovaCore\.codex\contact-payload-layout-control` | 352 | 37,160,094 |
| Total | 1,049 | 108,293,977 |

Both directories are Git ignored, contain no Git-tracked files and have zero
reparse points. They contain disposable comparison source/build/output only.
New disposable output: 352 files / 37,160,094 bytes. Disposed: 0. The previous
automatic deletion rejection was not retried. Project Control may perform the
following consolidated manual cleanup after deciding the architecture outcome:

```powershell
Remove-Item -LiteralPath 'E:\NovaCore\.codex\contact-allocation-discriminator', 'E:\NovaCore\.codex\contact-payload-layout-control' -Recurse -Force
```

Final `git diff --check`: PASS (existing line-ending warnings only). Staged diff
empty. Canonical production/test fingerprints unchanged. Candidate remains unbanked.

**STOP FOR PROJECT CONTROL.**
