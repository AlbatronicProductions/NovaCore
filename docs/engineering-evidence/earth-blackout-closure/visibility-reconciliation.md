# Retained visibility reconciliation — REVISE, UNBANKED

2026-09-22. **Blackout cause UNRESOLVED. Manual Player acceptance ON HOLD. No live GPU exposure during this revision. No resumed stage is authorized by this report.**

The historical population discrepancy is **not fully reconciled**. The retained journal supports a precise frame/counter reconciliation and the session log exposes a production pupil-publication starvation defect. That defect is corrected and qualified offline. The historical session did not retain the full prepared pupil or physical vertex buffer needed to replay its visibility membership exactly; neither the correction nor a newly reconstructed pupil supplies those missing inputs.

## What the retained frame actually submitted

Source: `build/earth-blackout-closure/live-06-horizon-latched`, PID 19912, session start 2026-09-22T17:02:44.7104133Z. Its journal SHA-256 is `0b5f2fc91bd8ec35225007b3d00787da0bf705893191136e3a19579a4276a429`. The original session files remain unchanged.

The read-only audit verifies all 131,072 retained records: serials 611181–742252, no bad checksums or gaps, 16.0140661 seconds. It joins **2,841 completed frames, 13360–16200**, using each GPU query's own frame identity, rather than independently comparing maximum counters. In this window, snapshots on either side of publication inspection agree; publication 18 and its physical buffer handle remain constant.

Every joined frame satisfies input accounting, counter agreement, three compacted indices per visible triangle, TCS patches equal to retained triangles, factor-one TES work, zero invalid triangles and zero compaction overflow. Frame 13360's submit and present precede the retained window; these are marked unavailable. The final submitted/completed identity is 16201, but it has no joined final cull/query snapshot. The last fully comparable frame is **16200**, not 16201.

| Frame 16200 fact | Retained value |
|---|---:|
| Actual viewport | 3440 × 1322 |
| Body-camera position, metres | (2475961.9596511647, 5845362.651630774, −540520.1753491238) |
| Recorded surface altitude | 83.27394104003906 m |
| Recorded forward vector | (−0.26269954442977905, −0.11941663920879364, −0.9574594497680664) |
| Current publication / incoming publication | 18 / 0 |
| Topology | L17, NCSM1, hash `0xBCE444AFFB2D713B` from publication log |
| Physical vertices / source triangles | 712,106 / 1,424,208 |
| Horizon rejected / screen rejected | 240,035 / 373,731 |
| Retained / TCS patches | 810,442 / 810,442 |
| Compacted indices / TES invocations | 2,431,326 / 2,431,326 |
| Maximum outer / inner factor | 1 / 0.9998999834060669 |
| Clipping output primitives | 78,221 |
| Invalid / overflow | 0 / 0 |
| Submit / present | Both retained; successful |

The earlier CPU model uses this exact recorded camera position. It normalizes the recorded forward and constructs a new camera basis and a new pupil with `Resolve(default, camera.Normalized(), topology)`. Its 398,565 retained triangles belong to that reconstructed mesh and its separate screen-cone model. That value is **not a demonstrated upper bound on the historical live mesh**.

At the matching frame, the visible difference is **+411,877**, composed arithmetically of **2 fewer horizon rejections and 411,875 fewer screen rejections** in the live counters. That accounting is exact. Attribution of those individual triangles to pupil lifetime, physical preparation, projection or floating-point differences remains unavailable. A matching publication number, topology size, or nearly equal horizon count does not prove matching geometry.

The old model used the recorded aspect ratio 3440/1322 while evaluating tessellation at heights 1440 and 2160. Both give the same factor-one counts here. It did not retain the actual projection matrix; changing the viewport or choosing a different reconstructed basis to fit the live count would not constitute reconciliation.

## Production defect: completed dependency work loses its pupil

**Observed in the retained session log:** generation 18 was prepared as incoming pupil **30**, in 11 slices covering exactly 712,106 vertices. The publication log confirms its fence-complete promotion. After that, the complete session log contains **543 completed current dependency checks**, with pupil identities from **32 through 766**, all with empty, resolved regional dependency masks. There are **zero current physical-preparation slices and zero staged pupil publications for generation 18**. Its retained scratch capacity also remains at the preceding L16 size, 696,410 vertices. The absence of a larger scratch allocation follows from never entering the preparation path; it is not an allocation failure witness.

**Source-proven mechanism:** `UpdateRegionalPhysical` previously pinned a demand job's pupil only until its dependency mask was complete. If the managed camera supplied a newer pupil on that completion frame, the code reset the completed job to the newer target, reset its cursor, and made readiness false before `UpdateRegionalPreparation` could consume the completed target. Near-ground demand takes 22 slices at the existing 32,768-vertex slice budget. Repeated movement can therefore keep rechecking dependencies while indefinitely rendering the old prepared pupil. An empty dependency mask does not mean the entire regional catalog is resident, so the all-resident bypass does not cure this case.

**Offline reproduction:** the production dependency-target function was extracted without changing its old condition and executed in a bounded CPU harness. With the finest topology and a new pupil every tick, 2,000 ticks produced **2,000 demand slices, zero preparation slices, zero publications, and final published pupil 30**. The regression failed before the correction. This is a production scheduling defect, not a synthetic tessellation singularity.

**Owner correction:** `RegionalPupilLifetime.h::ResolveDependencyTarget`, called directly by `UpdateRegionalPhysical`, now retains a checked pupil until its physical preparation has been published. Current-pupil release uses the renderer's prepared-pupil identity, which the regional path advances after the existing publication fence. Incoming work retains its dependency target for its topology transaction. A generation or topology change invalidates the old lease, and the fully resident path retains its existing behavior. Rendering and culling continue to use the published pupil during preparation.

The same 2,000-tick test now produces **60 publications, 660 preparation slices, and final pupil 1978**, with 1,340 demand slices. The current/incoming owner arrangement, fence order, topology, culling, physical authority and rendering quality are unchanged. The existing slice budgets are shared with the regression; their values are unchanged. No tessellation or visibility clamp was added.

This correction permits previously starved physical work to proceed. It can change the visible population and GPU cost. Offline progress is **not** qualification of that cost, live visual behavior, or the blackout route.

## Exact replay availability

| Required identity/state | Historical evidence | Qualification |
|---|---|---|
| Completed/submitted frame and camera position | Journal | Observed; frame 16200 matched |
| Forward direction | Float input recorded as doubles | Observed; does not encode roll or the full projection |
| Publication and topology | Journal plus publication log | Observed; ordinal 18 and L17 topology identified |
| Prepared pupil identity | Incoming slice log plus no subsequent preparation/publication | Pupil 30 supported by lifecycle trace; not a full frozen pupil payload |
| Pupil basis, retained lattice frame and offsets | Not recorded | Exact reconstruction unavailable |
| Exact mapped projection, body orientation and all culling constants | Not recorded | Shader-input replay unavailable |
| Prepared physical positions/normals | No full buffer capture | CPU/GPU preparation parity unavailable |
| Compacted triangle membership | Only counts recorded | Cannot identify the 411,877 differing triangles |

The pre-existing opt-in `RegionalPhysicalProbe` was not used for this session. It is not a retroactive recovery path. Its current implementation emits selected surface samples after publication inspection, requires all contributing regional records resident, and does not persist all physical vertices or compacted membership. Those requirements make it insufficient for this retained empty-dependency session and for a complete frozen replay without further diagnostic work.

An exact future capture must associate the completed command submission with its **pre-publication** current/incoming topology hashes, resource incarnations, prepared/cull/raster identities, complete pupil bytes, exact mapped camera/GPU/presentation inputs, physical vertices/normals, source topology and compacted membership/counters. Publication inspection can swap the buffers after the frame fence; reading the new owner and labelling it with the old completed frame is invalid. Capture must use bounded reserved storage and an off-render-thread writer, with completeness/hash checks and fail-closed rejection of mixed or partial generations. No such capture was obtained in this revision, and no capture route was launched.

## Validation and preservation

Evidence root: `build/earth-blackout-closure/visibility-reconciliation`.

| Offline check | Result |
|---|---|
| Production dependency-target regression, Debug and Release | Pass; 23 bounded moving-camera schedules, delayed readiness, current/incoming completion, generation/topology invalidation and all-resident path |
| Original starvation condition | Fails before correction, retained in `lifetime-before.log` |
| Native, Graphics-test and application builds | Debug and Release pass |
| Corrected horizon sweep, Release | 876 states; 606,961,728 triangle/state evaluations; 144 old-invalid retained cases, zero corrected-invalid factors |
| Corrected horizon sweep, Debug | 24 states; 22,460,448 evaluations; 42 old-invalid retained cases, zero corrected-invalid factors |
| Shared production scalar replay | Release 816,489 edges, Debug 51,159; exact agreement |
| Ordinary/singularity scalar regressions | 100,000 unchanged ordinary cases and 2,400 singular-neighborhood cases in each configuration |
| Observer CPU mocks | Normal, workload and cleanup-linger pass; simulated device loss, fence stall and ring overwrite stop as intended |
| Retained journal audit | 2,841 joined frames; no counter inconsistencies; exact replay gate remains closed |

The scheduling harness executes the shared production dependency-target function. It supplies explicit synthetic demand/preparation completion events; it does not execute GPU preparation or prove GPU fence behavior. The horizon sweep retains its previously documented shader-model and canonical CPU physical-authority limits. The accepted tessellation implementation and regression sources are byte-identical to this revision's entry snapshot.

`final-audit/audit.json` and `completed-frames.jsonl` retain the per-frame evidence and source hashes. `visibility-witness.json` beside this report contains a compact witness. The audit tool exits 3 deliberately because historical frozen replay prerequisites are absent. The preliminary `retained-audit` incorrectly treated the rolling window's missing first submit/present as failures; `final-audit` supersedes that derived audit and marks them unknown. Original live files are unchanged.

The first attempt to rerun the horizon test stopped at the existing native-deployment identity guard after rebuilding the DLL. Refreshing the isolated managed outputs restored identity consistency; no test or GPU route bypassed that guard.

Current preservation and build hashes are in `visibility-reconciliation/preservation.json` and the refreshed `current-task-manifest.json`. HEAD, index and refs are preserved; the ordinary manual-test executable and unrelated existing files are unchanged. KSA writes, system/driver changes and live GPU exposure are zero. No commit, tag, push, bank or milestone advance occurred.

## Project Control stop

**REVISE — a production lifetime defect is corrected offline, but the historical frozen visibility discrepancy remains unresolved.** Recovery diagnostics pass their CPU checks; the earlier live observer stop is still a failed stage. Real device-loss recovery and machine-wide-blackout preservation are untested. Neither incident PASS outcome nor external-cause escalation is supported.

Further exposure requires Project Control review and a capture plan that closes the missing-input boundary. This report does not permit a close-Earth reproduction. The accepted [tessellation correction](horizon-submission-boundary.md) remains in place; blackout cause and manual acceptance remain unresolved/on hold.
