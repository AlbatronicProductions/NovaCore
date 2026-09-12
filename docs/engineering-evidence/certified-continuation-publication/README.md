# Certified continuation publication candidate

**CERTIFIED CONTINUATION PUBLICATION CANDIDATE — READY FOR PROJECT CONTROL ACCEPTANCE**

Lead judgment: **PASS**. Unbanked on `codex/certified-continuation-publication`.
Accomplishment: **Atomically publish a certified private spacecraft continuation at canonical T**.

## Baseline and performance-stop disposition

HEAD/main/origin/main: `a4211617eef1e1d23bf8a4f8495388a35f9ed1dd`.
M14.16 implementation: `9091e08a03b04c28d639fa0bd4738240edf61848`.
Tag `m14.16-postimpact-terrain-clearance` targets
`dd3e5f2998c2148e675a0120abd599ef6f05cc4f`. All sixteen M14 tag objects/targets remain unchanged.

[Baseline](baseline.json) preserves the initial clean state and 331 source/test hashes.
[Original identity](identity.json) records the frozen production overrides.
Final verification matches **248 production files** (244 existing + four added).
Only tests and evidence changed during acceptance resumption.

Project Control accepted the performance architecture for continued correctness validation.
[Original results](results.json) and [initial plan](plan.md) preserve the earlier stop.
Their incomplete status is superseded by [final results](resume-results.json).
No publication timing campaign, optimization, lifecycle change or production correction
occurred during resumption.

## Responsibility and write-only commit

The specialized simulation publisher admits N == SourceStart, T == SourceEnd, checked
M14.15 endpoint and M14.16 EventFreeThroughTarget. Preparation and final applicability
checks occur within one owner-thread phase. After the first authoritative write, only
prepared paired segments/revision, clock/debt and fixed history record/count are assigned.
Independent review PASS: no allocation, growth, receipt/provider evaluation, normalization,
callback, yield, physics or ordinary refusal remains after that write.
This applies to declared ownership/current internal entry points, not arbitrary concurrency.
No rollback, scheduler or new readiness receipt was added.

## Replay and determinism

Nine variants per configuration reconstruct fresh M14.9–M14.16 evidence from serialized
source/history inputs: three equivalent debt-delivery sequences × observation cadences
0/30/144. Binary64 values use exact bits; the test-only rate converter invokes the existing
invariant constructor. The entire reconstructed prepared record, including endpoint bits
and provenance, is compared BEFORE entering the same normal publisher.
Seven changed endpoint/provenance/version/debt records refuse without mutation.
Final segments, clock/debt/revisions/history and observations match exactly.
Stored endpoint values alone never authorize installation. This is bounded test replay,
not a new runtime replay framework.

## Independent endpoint staleness and complete refusal snapshots

A coherent old root/initial/realization/endpoint chain survives a timeline change while
current outer expectations are refreshed. Both its endpoint read and publisher return
StaleEndpoint, independently of the separate chain-mismatch case. After success, checked
endpoint and clearance reads naturally refuse stale evidence.

Snapshots cover all craft definitions/properties/physical segments; ordinary/coupled/
publication history; clock/marker/revisions; pending/cancelled headers and all six internal
payload fields; used identities, next sequence, contact slots, occupancy and free-list.
Controls distinguish different force payloads under identical headers, including cancelled
events. Refusals preserve the full snapshot; success preserves full pending timeline and
exact prior ordinary history contents.

## Ownership, direct mutations and capacity

Wrong-thread and reentrant publication refuse without canonical movement and leave the
phase usable afterward. Clock/timeline/state/transaction mutation refuses while the phase
is held. A live change after an early checked read is rejected before commit.

Direct attitude, torque, force, translation and rotation changes without TimelineRevision
movement each yield StaleEndpoint. Mass has no in-place setter: genuine different-mass
authority is refused as ForeignEngine. This is not an executed in-place mass mutation.

Capacity zero and genuinely occupied one-slot history refuse before writes. The occupied
case publishes one craft, builds a fresh valid second-craft chain at the new canonical
time, then gets HistoryCapacityFailure while preserving the first record.
Revision overflow and insufficient/zero debt refuse; long.MaxValue debt subtracts exactly.

Fixed paired slots cannot lawfully disappear after genuine endpoint issuance. Different
authority, mismatched chain and upstream paired-admission checks cover that boundary.
No forged private proof or forced post-write failure was introduced.

## Event-kind matrix

Marker, ReplaceTrajectory, NoOpMarker, CelestialImpulse, RigidBodyTorque, SpacecraftForce
and SpacecraftContactImpulse at N, midpoint and inclusive T give **21 PendingEvent
refusals per configuration**. Events, payloads, state, clock/debt, revisions and history
remain unchanged. Successful publication schedules or consumes no synthetic event.

## Endpoint, clock/debt, revisions, history and observations

Coast installs exact position, linear velocity, quaternion and angular-velocity bits.
Both epochs and clock equal T. N=0, T=1,000,000, D=1,500,000 gives debt=500,000,
StateRevision 0→1, unchanged TimelineRevision and exactly one record.
Another success preserves rate 3/2, remainder, pause, prior marker history and an event at
T+1. Properties remain unchanged.

History is a preallocated 1,216-byte record of before/after values and provenance with
immutable string references, not provider authority. Observation is 312 bytes; private
prepared aggregate is 1,536 bytes. Journal cursor identifies the publication frontier.
Copied observations remain stable after later ordinary mutation; borrowed views expire.

## Real Florida Coast and Force

Unchanged terrain/requests: Coast .5 m gap, -1 m/s radial speed, attitude (0,0,.6,.8)
is EventFreeThroughTarget at 63 visits/depth 5. Publication passes exact paired-state/
clock/debt/history assertions. Force radial acceleration -.2 remains Unresolved /
DepartureUnproved at 25 visits/depth 24, refusing InvalidClearanceEvidence with full
nonmutation. No predecessor proof was weakened.

## Separate allocation matrix

Qualified checked 1 MiB boundaries; hard entry/exit failure; exact zero hot-path threshold.

| Path | Calls per configuration | Managed bytes |
|---|---:|---:|
| Prepare including history | 8 independent consuming windows | 0 |
| Final applicability recheck | 8 independent consuming windows | 0 |
| Write-only commit | 8 independent consuming windows | 0 |
| Successful normal publication | 8 fresh consuming fixtures | 0 |
| Copied observation | 32 | 0 |
| Each stale endpoint, stale clearance, timeline, debt, capacity refusal | 32 each | 0 |
| Each wrong-thread/reentrant refusal | 32 each | 0 |
| Replay publication installation | 8 | 0 |
| Engine/history setup | 8 | 11,512 per call |
| Fresh replay source/proof reconstruction | 8 | Debug 65,248; Release 65,176 per call |
| Test-only full JSON record comparison | 8 | 45,688 per call |
| Deliberate byte[128] control | 1 per matrix | 152 |

Consuming paths use eight fresh warmups; reconstruction uses two. Thread startup and
fixture setup are outside refusal windows. Legitimate setup allocations are separate;
no zero claim extends to all future contact/replay work.

## Performance, cadence, warp and headroom

Existing Release measurements retained unchanged. Phase and direct-total fixtures differ;
do not sum percentiles. See results.json for full measurement provenance.

| Responsibility | Median µs | P95 µs | P99 µs | Maximum µs |
|---|---:|---:|---:|---:|
| Preflight including record | 12.7 | 160.1 | 168.2 | 260.1 |
| Final recheck | 6.1 | 98.5 | 107.6 | 117.8 |
| History constructor subset | .5 | 1.3 | 2.0 | 2.3 |
| Write-only commit | .5 | 1.0 | 1.6 | 3.8 |
| Total publication | 18.7 | 254.3 | 273.4 | 296.3 |
| Copied observation | .3 | .6 | .8 | 1.0 |
| Source/proof reconstruction + publication | 1751.5 | 1988.4 | 1988.4 | 1988.4 |

Isolated responsibility **LOW IMPACT**; commit **NEGLIGIBLE IMPACT**.
Aggregate warp multiplicity **UNMEASURED / FUTURE INTEGRATION RESPONSIBILITY**.
The ~6.67 ms whole-frame target remains intact; no FPS guarantee, recurring maximum tail
or four-publications-per-frame claim follows. Publication has no production/sample
render-loop caller and performs no GPU work. Multiple future publications need measurement
under an actual operational caller. Existing predecessor regressions retain their own
timing assertions; the separate publication cost campaign was not rerun.

## KSA / BRUTAL and BEPU

Current installed hashes still match inspected source reconstruction:

| Assembly | SHA-256 |
|---|---|
| KSA.dll | A8A5164204EED962DF9D9025E99F523D9A23EE2CE6E802B18216A9DA35506D2F |
| Brutal.Concurrency.dll | FEDE16A5AC46D50E2E2E8D19687FF8835FDB75BA6630DB20C11E3969C7DF5B65 |
| BepuPhysics.dll | 77185E513BD530DEB322E41DD4ACDA9AD8E32E626CFA19492FB7481F25ED1FA7 |

KSA: 2026.9.7.5402+487c3f340de24c6a81037120b6d1129c045c5400.
Relevant installed reconstruction under
`build/ksa-residency-reference/assembly-source/KSA/`:
VehicleUpdateTask.cs:141, Vehicle.cs:2399–2507, Universe.cs:1666–1669,
Program.cs:2101–2106. This is not claimed as upstream Git source.

BRUTAL schedules; KSA applies staged vehicle/bubble results and then public time.
ADOPT staged result → later game-owned application. ADAPT checked NovaCore endpoint/
provenance. INTENTIONALLY DIFFER for endpoint-bit preservation, coherent state/time/
revision/history and no ordinary failure after writes. KSA application can normalize
attitude and perform later terrain/module work. Equivalent lifecycle timing **UNMEASURED**;
no speed ratio is claimed. No new historical research was needed for this resumption.

**BEPU GATE NOT REACHED**: no support/rest, manifold, friction, warm start, collision solver
or landing-gear constraint was added.

## Full validation and independent review

SDK 10.0.303; runtime .NET 10.0.12.

| Gate | Debug | Release |
|---|---:|---:|
| Final full solution build | PASS, 0 warnings/errors | PASS, 0 warnings/errors |
| Full Simulation | 48/48 PASS | 48/48 PASS |
| ReferenceFrames | 11/11 PASS | 11/11 PASS |
| Precision | PASS | PASS |
| Focused Simulation ownership + M14.2–M14.16 | 16/16 processes PASS | 16/16 processes PASS |
| Florida/headless candidate + predecessors | 15/15 processes PASS | 15/15 processes PASS |

Final matrix: **68 test processes**, all exit zero; two final full solution builds.
Earlier runs are not relabeled as final passes. Two full Debug attempts stopped at old
pre-binding borrow use after 13 and 27 groups respectively. Independent review approved
test-only expiration assertions/current-owner reacquisition in CelestialContractTests and
SpacecraftAttitudeIntegrationTests. Original warmups/workloads/hashes/zero thresholds remain
intact. Remaining Program.cs borrow sites were audited; no further mismatch was found.

Independent final source review PASS: 248 unchanged production identities, write-only
commit, pre-publication replay comparison, full snapshots, genuine capacity, ownership,
positive controls and the two test adaptations. Constructibility limits remain explicit.
Reviewer inspected source; lead collected execution results. Diff check passes with only
line-ending conversion warnings. No renderer/GPU campaign or allocation forensics ran.

## Reproduction and evidence

[Resumption plan/checkpoints](resume-acceptance.md), [final matrix](resume-results.json)
and [final test identity](acceptance-identity.json) supplement retained baseline, original
identity/timing and permanent test witnesses.

From E:\NovaCore, build each configuration:
`dotnet build NovaCore.sln -c <configuration> --no-restore -v quiet`.
Run Simulation, ReferenceFrames and Precision DLLs under
`tests/<project>/bin/<configuration>/net10.0/`.
Exact sixteen Simulation selectors and fifteen Graphics `--case=<name>` values are in
resume-results.json. Use fresh processes. Florida fixtures acquire existing production
datasets and fail if unavailable. Do not run the separate publication cost case for
correctness-only reproduction.

No raw stdout, copied comparison tree, profiler dump or bulk KSA source is retained.
One disposable 7,357-byte script remains: automatic deletion was previously rejected
before execution as `blocked by policy`; it was not retried. Reviewed manual cleanup:

```powershell
Remove-Item -LiteralPath 'E:\NovaCore\.codex\prepare_publication_guards.py' -Force
```

No staging, banking, ref/tag movement or milestone assignment. Stop for Project Control.
