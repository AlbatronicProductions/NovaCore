# Verify A — fresh host costs and replacement lifetime

This review supplements `work-b.md` with the fresh measurements in
`bank-event.json` and the corrected `reference-paired-coordinator.json`.
`verify-a.json` retains the exact 64 logical-frame rows, six generation joins,
scope distributions, resource comparisons, journal hashes and evidence limits.
No production source, build, GPU run or deployment was changed by this verifier.

The measured banked path does not expose a recurring resource-construction or
publication-bookkeeping stall capable of explaining its GPU target failure.
The substantial synchronous map construction is private-prototype work, absent
from normal M13.4. The banked frame's dominant host duration is waiting for the
previous GPU submission; treating that wait as avoidable CPU computation would
misclassify the dependency. The existing material-upload preparation scope has
about 2 ms of real CPU work, but these observations do not establish a GPU-floor
correction or authorize unrelated material optimization.

## Frame and event association

The six incoming generations are 17, 18, 19, 35, 36 and 37. Their banked native
record-frame ranges are 437–446, 448–458, 460–470, 817–826, 828–838 and 840–850.
They cover logical frames 334–343, 345–355, 357–367, 714–723, 725–735 and 737–747:
64 frames in all. The corrected paired run covers exactly those logical ranges.

Host row `frame=F` describes the host work recording F. Its `gpuFrame` normally
reports F−1, whose completed timers are being inspected. CPU scopes emitted
before `Upload` can still carry the preceding frame label. This review therefore
groups pending scopes by their log order through the next `Lifecycle transfers`
record, which identifies the frame about to be recorded. It does not join CPU
work to the completed GPU frame merely because it appears on the same host row.

Scope parents contain their children. Candidate creation includes resource
acquisition and descriptor work; host update includes its fence wait, callback
and preparation scopes. Their sums are not independent contributions.

## Banked CPU measurements

All values below are milliseconds. Counts and distributions refer to the 64
selected frames; six incoming creations occur inside that window. A zero
allocation count is an observed absence in this warm window, not a claim that
the renderer never allocates at startup or growth.

| Scope | Calls | Mean per call | P95 | Peak |
|---|---:|---:|---:|---:|
| Allocate/map | 0 | 0 | 0 | 0 |
| Candidate-create guard and six creations | 64 | 0.004002 | 0.0363 | 0.0510 |
| Topology acquire | 6 | 0.000267 | 0.0003 | 0.0003 |
| Incoming work acquire | 6 | 0.000117 | 0.0002 | 0.0002 |
| Descriptor update | 6 | 0.027767 | 0.0308 | 0.0308 |
| Regional update | 64 | 0.016953 | 0.0186 | 0.0478 |
| Regional inspection | 64 | 0.017270 | 0.0446 | 0.0510 |
| Publication inspection during preparation | 64 | 0.000138 | 0.0003 | 0.0004 |
| Prepare material uploads | 64 | 2.016497 | 2.2624 | 2.3970 |

The 64 publication-inspection calls above are preparation-frame guards. The
actual ownership swaps occur on the following updates and are measured below.
The private rational map and private mapped-index scan are absent from this
production path; zero production calls must not be replaced with their earlier
private 247–451 ms costs.

| Host scope | Mean | P95 | Peak |
|---|---:|---:|---:|
| Full host frame | 16.765900 | 18.1896 | 18.3462 |
| Fence wait | 13.679123 | 15.0282 | 15.2418 |
| Host frame minus fence wait | 3.086777 | 3.5142 | 3.8081 |
| Host callback | 0.168908 | 0.2332 | 0.7061 |
| Validation/upload | 2.131184 | 2.3877 | 2.5447 |
| Command recording | 0.275283 | 0.3579 | 0.4824 |
| Host unattributed remainder | 0.010466 | 0.0223 | 0.0282 |

The same banked GPU frame window measures mean 14.270402 ms, P95 15.62185 ms and
peak 16.95828 ms. The host waits on the single in-flight submission before
mutating mapped controls, descriptors or reusable resources. This establishes
dependency waiting, not an independent 13.68 ms CPU workload that can be removed
without proving a different safe ownership arrangement.

`PrepareProductionUploads` is an existing shared per-frame CPU responsibility.
Its 2.0165 ms mean is measured, not unknown. Its inner request/cache-processing
contributions were not individually timed here. All 1,000 banked transfer rows
report zero pending material uploads, zero buffer-to-image copy commands and
zero copied bytes. All 2,000 corrected paired rows do likewise. Although the
general GPU-total timer starts after material transfer recording, the warm
event has no material-image transfer workload hidden before that timer. This
does not assert absence of initial asset uploads outside the measured interval.

## Creation, preparation and publication

| Incoming generation | Slices | Native job start to publication marker | Creation begin to publication end | Actual publication inspection | Post-fence checkpoint to publication end |
|---|---:|---:|---:|---:|---:|
| 17 | 10 | 159.9007 | 162.0059 | 0.0641 | 0.0732 |
| 18 | 11 | 189.5042 | 191.5829 | 0.0747 | 0.0902 |
| 19 | 11 | 196.1915 | 198.2742 | 0.0584 | 0.0730 |
| 35 | 10 | 161.3922 | 163.6822 | 0.0788 | 0.1043 |
| 36 | 11 | 187.9235 | 189.9969 | 0.0668 | 0.0852 |
| 37 | 11 | 198.1856 | 200.1941 | 0.0600 | 0.0751 |

The post-fence checkpoint is the start of regional inspection after the frame
wait and earlier GPU-query inspection. It is **not** the exact instant the GPU
finished. GPU-completion-to-host-publication latency was not independently
timestamped; the table must not be cited as that missing measurement. Regional
inspection end to publication-inspection begin is 0.0030–0.0050 ms in these
events, giving no evidence of a substantial extra host ready queue at that point.

Creation end to first regional-update begin is 1.9038–2.1345 ms, with the ordinary
material preparation scope between them. The native job clock begins after
incoming creation; neither native interval includes earlier managed scheduling
before native acceptance. A 10/11-slice event spans displayed frames and shared
fences, so its elapsed duration is not an active GPU preparation sum.

The production chronology inspected in `work-b.md` remains consistent with these
events: resource capacity/topology is acquired once; a separate incoming work
set receives bounded physical slices; current physical ownership stays live;
the final banked slice records its existing validation/cull/compact work; the
submission fence completes; readiness inspection then transfers the completed
allocation into current ownership and retains the outgoing work as a spare.
Publication changes ownership and bindings without copying the full physical
buffer on the CPU.

## Corrected paired control

Use `reference-paired-coordinator.json`. The earlier `reference-paired.json`
advanced the managed coordinator twice per logical pair and shifted generation
windows. It is superseded for event matching and is not used for the following
claims.

The corrected run advances the coordinator once per logical pair. Across all
64 pairs, the raw camera, GPU, presentation, lighting, published-frame and
incoming-frame bits match. Current/incoming generations, topology identities,
cursor, active state and dependency completeness match. Current/incoming
physical allocation and memory handles, map allocation handles, TCS patches
and TES invocations also match. Every observed source physical allocation is
distinct from its incoming destination. Each reference tick retains the cursor
and records no incoming work.

This control skips incoming command recording. It retains the normal frame
fence, current visibility/compaction and draw. A reference update can legitimately
publish a **preceding work tick** after that tick's final fence; all six selected
events publish on the subsequent reference update. It does not complete new
incoming work itself. Thus the correct claim is absence of incoming work on
reference recording, not absence of any publication during reference updates.

The private prototype's six synchronous rational-map constructions occur on
reference updates: 336.2120, 365.7897, 390.5058, 350.9619, 354.0282 and 367.6284 ms.
The corresponding work updates mostly execute the candidate guard. These costs
are private source-creation work, not normal M13.4 or a meaningful measure of
reference rendering CPU cost. Private creation-to-publication durations become
547.2261–653.1717 ms, while the native job clock alone reads 206.3516–260.6766 ms.
The latter excludes map construction. Added reference frames also deliberately
change wall-clock latency. Neither interval is production publication latency.

Warm topology/work allocations are reused in the selected private window as
well. Its work GPU mean/P95/peak is 8.676622/10.62408/10.92796 ms; reference is
8.254868/9.02848/9.79276 ms. These are paired diagnostic results, not production
qualification or a proof that the 8.33 ms closure gate is met. Work A owns the
detailed whole-frame causal decomposition and the limits of reference ordering.

## Lifetime proof and remaining limits

The live source establishes distinct current/incoming/scratch owners, host
mutation after the prior fence, complete-destination readiness before the
ownership swap, retained spare capacity, and device-idle teardown/resize
boundaries. It also establishes that per-frame current cull is needed for the
current camera/view/body orientation; frozen physical positions alone do not
authorize reusing old visibility data. Calling that cull duplicate merely because
an incoming job exists would not prove its removal safe.

The fresh handle and identity checks strengthen the event join; they are not a
new complete GPU-content oracle. This ticket did not compare every current
physical word across each control pair or execute destructive lifetime negative
controls. Simultaneous current and incoming work plus barriers also does not,
by itself, identify hardware contention or quantify a removable scheduling cost.

The retained capability model's 91 checks passed, but its generation, allocation,
binding-epoch, map, frame/domain, fence and validation predicates have **not**
been integrated with actual native allocation/binding state. If a future exact
reuse/resource candidate is proposed, it still needs live negative controls for
full 64-bit generation changes, recycled allocation addresses, stale or dead
maps, basis/radius changes, regional/support revision changes, cancellation,
partial completion, withheld final fences, invalid normals, resize/rebinding,
and missing or overlapping ownership. Model tests are not that qualification.
No new reuse candidate is being promoted here, so those unimplemented private
contracts are not being presented as defects in unchanged banked M13.4.

This review supplies no implementation winner and no full-suite qualification.
The existing production owner and fidelity boundaries remain unchanged. Measured
resource acquisition and publication bookkeeping are too small here to explain
the failed GPU gate; the corrected control still has tail frames above 8.33 ms.
Closure requires the lead's measured whole-frame decision, with no substitution
of a looser frame budget or private prototype CPU stalls for production evidence.
