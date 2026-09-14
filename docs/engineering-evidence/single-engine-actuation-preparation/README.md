# Single-engine actuation preparation

Disposition: **PASS; UNBANKED; stop for Project Control.** No milestone is assigned.

The candidate implements the accepted engine-direct first slice: canonical command transitions → private preparation → sealed conditional body-wrench preview. It applies no force or torque, advances no canonical actuator state/revision, consumes no fuel and changes no contact world.

Baseline HEAD/main/origin/main/remote main: `c70ccfacf3607d1342396c702682781b42bb525e`. Branch: `codex/single-engine-actuation-preparation`. M14.22 and M14.21 identities and all 62 historical tag refs are preserved in [identity.json](identity.json). The previous architecture evidence remains untouched in [spacecraft-actuator-architecture](../spacecraft-actuator-architecture/README.md); the current implementation ticket specifically authorizes private evaluation-cursor advancement on successful sealing and Ignite → proposed Enabled even when hardware is unavailable.

## Result and responsibility

- Optional transaction-owner capture stores every committed Ignite/Shutdown record before the next command can obscure it. It does not reconstruct history from the latest snapshot.
- A seven-edge maximum unconsumed lag is enforced independently of command-queue drain/refill. Admission reserves captured plus queued edges; commit rechecks before any writes. Saturation returns explicit Capacity. Nonengine commands and reserved neutralization consume no edge slots.
- Immutable engine definition validates identity/version, SI FP64 mount, robustly normalized axis, positive finite maximum thrust and effective exhaust velocity, and explicit availability.
- Preparation consumes ordered edges into a private Off/Enabled latch and seals one exact next original 60 Hz interval. All validation and finite arithmetic precede private cursor/proposal writes.
- Preview is copied value data marked `ProposedIfApplied`, with body force/moment, required feed demand, source revisions/cursors and exact start/end. It contains no live state references or root-space constant-force claim.
- Explicit retirement releases a proposal without physical application or cursor rewind. Same-interval retry is refused after successful sealing; pre-seal refusal preserves the pending range for a valid attempt.

Detailed source, lifecycle, mathematical, authority and refusal contracts: [contract.md](contract.md).

## Qualification

The bounded plan was declared before final execution: Debug and Release full builds; six focused gates per configuration; one full Simulation process per configuration; ReferenceFrames, Precision and verified BEPU dependency checks in both configurations; one Release 256-warm/4,096-sample cost process. No qualification failure or retry occurred.

| Gate | Debug | Release |
|---|---|---|
| Full solution build | PASS, 0 warnings/errors | PASS, 0 warnings/errors |
| New definition/math/capture/authority/determinism | PASS | PASS |
| Six exact allocation windows | 0 bytes each | 0 bytes each |
| Positive control, one `byte[128]` | 152 bytes | 152 bytes |
| M14.22 cheap, accepted-stream replay, allocation | PASS | PASS |
| Full Simulation | 63/63 groups | 63/63 groups |
| ReferenceFrames / Precision / BEPU dependency | PASS | PASS |

The full suites retain M14.17 certified continuation, M14.18 staging, M14.19 publication and M14.20 servicing, including their refusal/allocation tests. Existing test workloads and thresholds were not edited.

M14.21 comparison uses an independent unactuated `ContactSequence` as the physical oracle, then prepares and explicitly retires three-edge/nonzero-throttle proposals before each unchanged contact interval. Both configurations match all 1,200 motion endpoints bit-for-bit in each case:

| Case | Peak independent geometry penetration (m) | Peak interval | Final support | Endpoint bits |
|---|---:|---:|---:|---:|
| Centered | 0.005819714882528193 | 34 | 600/600 | 1,200/1,200 |
| Tilted | 0.006945546380241474 | 35 | 600/600 | 1,200/1,200 |
| Moving-frame tilted | 0.006945746950922316 | 35 | 600/600 | 1,200/1,200 |

Each sequence preserves 400 short + 800 long intervals, 20,000,000 ticks, 1,200 physical revisions/history records, zero remaining debt, unchanged TimelineRevision and world generation. These are existing unpowered contact trajectories; no powered-contact capability is claimed.

## Cost, allocation and storage

Release ordinary-runtime proposal measurement, one process, 256 warm + 4,096 measured:

| Median ms | P95 ms | P99 ms | Maximum ms |
|---:|---:|---:|---:|
| 0.0023 | 0.0024 | 0.0024 | 0.0050 |

Scope includes three-edge consumption, proposal preparation, copied preview and cheap result/source observation checks. Command input commits, closure, retirement and test clock advancement are outside timing. This is preparation cost, not future propulsion dynamics cost. No timing ceiling was requested, and no no-GC boundary or runtime override wraps timing.

The separate unchanged checked allocation helper requires exactly zero for capture, preparation, no-edge preparation, preview, duplicate/refusal, and 1,024 complete operations. Its 1 MiB temporary reservation is measurement isolation, not an allocation allowance. Entry/exit failure is fatal; the independent known-object positive control detects 152 bytes. No subtraction, tolerance, retry, profiler or attribution campaign was used.

Cold retained graph: **1,128 bytes / five objects** (definition, authority, storage, seal, seven-edge array), plus one 8-byte reference in the transaction engine. Total accounted graph + reference: **1,136 bytes**. Embedded value sizes are 224 bytes of edge payload, 88 bytes of definition values and 328 bytes of preview. These are components of the retained objects, not additional totals. Measurement counts allocation while cold-binding after factory/binding warmup; every new object remains retained. Existing canonical/command storage is excluded. There is no growing history and no per-proposal allocation.

Exact structured receipts: [validation.json](validation.json). Independent 18-point attack review and one bounded correction: [verification.md](verification.md). Reproduction: [reproduce.ps1](reproduce.ps1).

## Source and evidence preservation

Seven candidate source/test paths are fingerprinted in identity.json. Only two of 1,913 pre-existing tracked files changed: three optional command-owner hooks and seven test-runner registration lines. The other 1,911 tracked files and all nine pre-existing untracked evidence files remain byte-identical. No Blender, renderer, contact solver, dependency, physical definition or physical publisher source changed. Nothing was staged, committed, pushed, tagged, merged or banked.

Evidence budget: 128 KiB; this package contains only the report, contract, structured results/identities, verifier and reproduction script. No diagnostic build tree, profiler copy or raw stdout file was created. Ordinary ignored build outputs remain reusable development outputs; unrelated pre-existing scratch/evidence was preserved. No cleanup or additional responsibility is required to interpret these results.

Next: Project Control review only. Physical application, actuator publication and finite propellant remain separate, unimplemented responsibilities.
