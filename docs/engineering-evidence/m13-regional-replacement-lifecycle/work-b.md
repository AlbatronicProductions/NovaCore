# Work B — banked CPU, map and resource lifecycle

Inspection baseline: M13.4 `047ae479b33831eae1c0dfa3f37c657a7f70148f`.
This worker performed source inspection and read retained scalar evidence only;
no production changes, builds or GPU runs. Fresh event measurements belong to
the lead's baseline run. Missing CPU timings below are unknown, not zero.

## Production versus rejected private prototypes

**Normal M13.4 does not construct the private rational reuse map, scan mapped
indices with the private vertex validator, copy prepared physical values using
that map, or use the private device-local work-buffer selection override.**
Searches of the current native/managed production trees found no
`CompositionMakeMap`, `residualIndicesValidated`, private residual-validation
mode or capability predicate integration.

The managed topology adapter does build a parent-to-child vertex map once while
adapting its immutable scale library (`PlanetaryNestedScaleMeshRuntimeAdapter.cs:80`).
That is distinct from the rejected prototype's synchronous per-replacement
rational map. The current native submission does not transport that parent map.

The retained private evidence reports:

| Responsibility | Private measured cost | Frequency in that prototype | Normal M13.4 frequency |
|---|---|---|---|
| Rational map construction | Mean 247.5213 ms; peak 370.2112 ms | 36 constructions in mapped composition run | Absent |
| Scan `topology.indexMapped` for vertex-validator qualification | 183.8632–451.4311 ms | First encounter of each of ten resident topology resources in vertex run | Absent |
| Exact physical-copy path | Earlier GPU oracle established reusable records | Incoming slices in rejected copy prototypes | Absent; normal preparation evaluates full physical result |
| Device-local physical working-buffer preference | Earlier placement control was faster | Private allocator override | Absent; production requests host-visible coherent memory |
| CPU capability predicates | 91 checks, zero failures | Private model test | Not integrated into native runtime |

These CPU stalls constrain any future implementation of those mechanics. Their
removal is **not** a saving from banked production and cannot explain its normal
transition frames. The prior 77.41 ms mean/134.97 ms peak publication-duration
summary is also a private-run measurement that excludes startup/map construction;
it is not a fresh normal-production latency baseline.

## Frequency and ownership

| Responsibility | Current banked frequency | Required/current justification | Event or frame cost available here | Potential boundary; no candidate selected |
|---|---|---|---|---|
| Topology family/hash/count validation | On accepting a new incoming submission | New generation must refer to a supported resident identity | Fresh CPU scope needed | Incoming acceptance |
| Index range and referenced physical-value checks | Inside current cull each displayed frame; inside final incoming cull | Current implementation combines validation and view-dependent classification | GPU scopes are Work A | Do not reopen final-block optimization in this ticket |
| Managed parent-map construction | Scale-library adaptation, outside recurring native replacement | Immutable topology mapping | Not measured here | Already library-owned |
| Private rational map / mapped-index scan | Absent | No production consumer | No production cost | Future map should use persistent CPU topology data if separately justified |
| Current visibility list | Every authoritative displayed frame | Depends on current camera position, view/projection, body orientation and TES support | Fresh GPU/current-cull scope | No evidence incoming causes extra current culls |
| Current compaction | After current visibility each displayed frame | Converts that frame's visible set into its draw list | Fresh GPU/current-compact scope | Persistence requires a proved unchanged view/visibility identity |
| Incoming visibility/compaction | Once at final incoming slice in banked path | Current publication readiness consumes it | Prior final-block result retained separately | Explicitly not reopened |
| Full incoming physical preparation | Once per incoming physical generation, 65,536 vertices per eligible displayed frame | Complete frozen incoming pupil and regional dependency footprint | Per-slice GPU timers exist; host orchestration needs split | Necessary total work versus avoidable work is lead's causal question |
| Current pupil refresh | Only when desired pupil differs and no incoming topology replacement owns preparation | Separate scratch preserves the old published pupil until complete | Existing current-preparation timer | No partial writes to current physical buffer |
| Prepared-value reuse | Absent | No accepted production reuse capability | Not a production saving to assign without new candidate | Requires actual identity/lifetime integration |
| Topology GPU allocation/upload | First use of each family/hash/count resource, at most 18 resident resources | Immutable lattice/index storage persists across replacements | Fresh allocation/upload scope and byte counters | Already resident-resource owned |
| Physical/visibility/compact work allocation | When no retained spare fits capacity | Current/incoming work and a spare persist; capacity reuse is separate from physical-value reuse | Fresh allocation-versus-spare scope | No evidence of per-slice allocation |
| Buffer map/unmap | Map on allocation; unmap on destruction | Persistent coherent mapping | Fresh allocation scope | Not per frame or per publication |
| Descriptor creation | Initial submission setup and swapchain recreation | Descriptor set owns current bindings | Event count/time needed | No replacement-driven descriptor reconstruction observed in source |
| Descriptor updates | Incoming acceptance, publication/pupil swap, allocation growth, recreation | Actual resource/range changed | Count/time needed | Seven billboard writes per current/incoming update; distinct from binding a descriptor set into a command buffer |
| Frame/control upload | Each displayed frame | Camera, presentation, pupil and dispatch controls change | Fresh `Upload` and regional-control scopes | No large topology/physical upload implied by the word upload |
| Regional identity/readiness processing | Every update; I/O/upload only for missing records | Immutable dataset check and complete physical dependencies | Fresh Matches/upload/control split | Residency persists; no per-frame disk reload implied |
| Publication readiness/bookkeeping | Guard called each update, substantive work only when incoming fence pending | Complete physical generation and valid draw data must precede ownership change | Fresh guard-hit/event scope | No physical-buffer copy at swap |
| Frame fence wait | Every displayed update | Single in-flight submission protects mapped writes, descriptor changes and resource reuse | Existing CPU fence timing | Necessary current architecture dependency; avoidability requires proof |

KSA frequency/ownership is intentionally left to independent Work C. No KSA
frequency is inferred from an allocation type or earlier approximate comparison.

## Concrete resource lifetime

All work-buffer memory flags below are production **requests**. `Memory` selects
the first compatible type; it does not promise a particular heap on every GPU.
Retained RX 6800 XT baseline A heap records selected type 1, properties 6
(`HOST_VISIBLE|HOST_COHERENT`), heap 0 without `DEVICE_LOCAL` for physical work.
That is prior measured hardware evidence, not a new heap capture.

| Resource | Creation / memory / mapping | Owner and reads | Writes / fence | Publication, invalidation and recycling |
|---|---|---|---|---|
| Managed topology and parent map | Immutable scale library in managed memory | Managed library; pinned native lease for submitted lattice/indices | Built during adaptation; no GPU writer | Arrays remain library-owned; native lease released after acknowledgement |
| Native lattice/index buffers and mapped index representation | `AcquireProductionBillboardTopology`; host-visible coherent; mapped once; host memcpy on first residency | Renderer resident topology pool; current/incoming alias topology read-only | No normal replacement rewrite after upload | Reused by family/hash/count; survives publication and resize; destroyed at renderer teardown; no separate recurring mapped-index reconstruction |
| Current prepared physical buffer | Allocated as incoming, then becomes current; host-visible coherent | Sole current physical owner, read by GPU cull/render | Frozen through incoming preparation; no in-place partial replacement | After final fence/readiness, outgoing allocation moves to spare; content identity changes before later reuse |
| Incoming prepared physical buffer | Retained spare if large enough, otherwise fresh allocation; mapped persists | Incoming generation only, invisible | GPU writes contiguous ranges after dependency completeness; single frame fence between host mutations | Entire allocation transferred to current after completion; no physical memcpy at swap |
| Regional pupil scratch | Distinct persistent/growing host-visible coherent buffer | Staged same-topology pupil refresh; current remains authoritative | GPU writes at bounded slice size; complete final fence before swap | Current/scratch buffer, memory, pointer and capacity swap together; incoming topology job cancels unpublished refresh |
| Visibility buffer | Allocated with work set; host-visible coherent mapping | Current or incoming work owner | GPU current cull writes per frame; incoming cull only on final banked slice | Current/incoming/spare ownership transfers with work set |
| Compacted index buffer | Same work allocation lifecycle; storage plus index-buffer usage | Current indexed draw; incoming readiness | GPU compact writes after cull barrier; barrier exposes index/indirect reads | Content is camera-specific, unlike immutable topology indices; rebuilt before each current draw |
| Indirect/counter buffers | Same work allocation lifecycle; persistently mapped | GPU draw; host readiness/telemetry after fence | Host seeds generation/count metadata; GPU reset/cull/compact writes dynamic results | Moved with work set; reset before current use; no raw mapped pointer is a durable generation token |
| Regional residual payload/catalog | Renderer lifetime; host-visible coherent; placeholder payload grows once to bounded dataset size | GPU physical preparation reads complete geographic dependency set | Worker reads/verifies records; host uploads residuals and resident flags after fence | Immutable dataset identity; resident entries are retained; changed data requires new renderer generation |
| Regional demand/control | Renderer lifetime; host-visible coherent; mapped | GPU demand and physical dispatch controls; CPU reads demand mask after fence | Bounded demand slices and host control updates | Job identity includes generation/topology/pupil; full readiness before preparation; current pupil stays published |
| Production material staging | Renderer-lifetime host-visible coherent transfer-source buffer | Transfer commands read pending batches | Host converts/copies completed material payloads after prior fence | Bounded pending uploads; not incoming physical staging |
| Production material image arrays | Renderer-lifetime device-local allocations, not host mapped | Terrain material shaders | Explicit staging-to-image copies and image barriers when pending | Resident material lookup published after fence; separate from physical regional residual authority |
| Prepared-physical device-local override | Not present in banked implementation | Prior private experiment only | Same mapped coherent roles under private allocator preference | Must not be described as current production placement |
| Descriptor set / pupil-frame buffer | Startup and recreation; frame buffer mapped | All commands in the one in-flight submission | Host update after fence; command-buffer binding does not reconstruct resources | Resize waits device idle, rebuilds descriptor/frame resources and rebinds surviving work; future reuse capability needs refreshed binding epoch |

`Destroy` calls `vkDeviceWaitIdle` before native resources are freed. Resize also
waits device idle and calls `DestroySubmission(a,false)`, preserving billboard
work/topology while recreating submission/frame resources. Region catalog and
scratch survive this submission recreation. These boundaries must be honored by
any future map/source capability.

## Current-owner independence

Physical ownership is independent: incoming preparation uses distinct physical,
visibility, compact and indirect buffers; current physical values and published
pupil are retained. `UpdateRegionalPreparation` suppresses an unpublished current
pupil refresh when incoming topology preparation owns the transaction.

Scheduling is **not** independent. Both paths are recorded on the same graphics
queue in one command buffer. Current reset/cull/compact precedes incoming work;
the terrain draw follows compute work. The next frame's host update waits the
ordinary fence covering that whole submission. There is no separate incoming
queue/fence or CPU preparation wait inside `RecordRegionalPreparation`, but its
GPU work contributes to the submitted frame and can extend that common wait.
This establishes dependencies, not measured hardware contention or a safe
alternative queue design.

Current visibility and compaction run even when incoming does not exist. Their
inputs include changing camera/view/body orientation; frozen physical pupil does
not make visibility immutable. Therefore an unchanged physical source does not
justify removing current culling during a moving-camera return. Incoming presence
does not itself trigger an additional current visibility pass or mid-job current
descriptor update. Host publication/pupil-swap events do change current bindings.

Host-coherent buffers avoid explicit flush/invalidate calls in this path. The
host-to-shader and compute-to-consumer barriers still establish visibility and
ordering. A barrier is not an allocation, mapping operation, or CPU fence wait.

## Minimal host instrumentation for fresh banked event

Use one scalar row per event-relative frame, tagged with recorded/native/GPU
frame and source/incoming generation, topology, actual published/frozen pupil,
slice cursor/count and submission serial. Do not align CPU cost with the next
frame merely because its GPU timing is read there.

Split the existing `Update` aggregate into consecutive exclusive scopes:

1. Existing frame fence wait.
2. `InspectRegionalPhysical` (split staged pupil swap only on a real event).
3. `InspectProductionBillboardPublication` (include guard-hit flag and actual swap).
4. Remaining inspections.
5. Managed callback.
6. `CreateProductionBillboard`.
7. `Validate`.
8. `Upload`.
9. `PrepareProductionUploads`.
10. `UpdateRegionalPhysical`.

Keep existing acquire, command recording, submit, present and whole-host-frame
scopes. `validationUpload` currently groups items 6–10; it is not a measurement
of diagnostics alone. Compute unattributed host residual from disjoint scopes;
do not add parent and child timers.

Only if those scopes reveal a material cost, add event-only children:

- Incoming creation: topology lookup versus first upload; work acquisition
  spare-hit versus allocation; initialized/uploaded bytes; seven-write descriptor
  update count and duration. Allocation/map/memset children need not run each frame.
- Regional update: immutable `Matches` check, completed payload copy bytes/count,
  catalog/control memcpy, readiness/job bookkeeping. Avoid calling the I/O worker's
  decode time a main-thread cost.
- Publication: readiness inspection, outgoing-to-spare bookkeeping, descriptor
  update, native acknowledgement. Record predicate-ready time separately from
  final GPU submission completion and first visible new-owner frame.

Frame counters should report zero where work did not occur: topology upload,
allocation, map/unmap, descriptor update, regional payload bytes and pending
material-upload bytes. Record actual resource handles plus native lifetime
serials where possible; handles alone can be recycled.

### Existing total-GPU exclusion

`RecordProductionUploads(a,c)` occurs **before** timestamp 0 of the existing
GPU-total measurement (`NovaCoreNative.cpp:1924`, `:1927`). Material transfers and
their barriers are therefore outside that historical total. Log actual pending
uploads and use a separate bounded transfer scope or explicitly report this
exclusion. The call's existence does not prove transfers occurred; warm hierarchy
residency may make it a no-op. Regional residual uploads instead use host memcpy.

## Publication latency and qualification boundary

A source-level timeline is: demand/new-generation selection; asynchronous managed
ready/submission; native acceptance/resources; regional dependency completion;
first bounded preparation slice; subsequent slices; final slice plus banked
incoming readiness work; completed frame fence; host readiness/swap; first draw.

The preparation job's existing `started` timestamp is set after native incoming
creation. It excludes earlier managed scheduling, topology/work acquisition and
any private map builder inserted before that point. Consequently its duration
cannot establish end-to-end replacement latency. Measure each boundary rather
than inferring idle time as duration minus GPU sums; GPU/CPU work and presentation
may overlap, and fence wait is already inside host update.

The retained **91 passing capability tests are model tests only**. Actual native
allocation/content/binding epoch integration remains missing. Required execution
controls include high-32-bit generation mismatch, overwritten/recycled source,
invalid map extent/address epoch, basis/radius changes, regional and support
identity changes including normal neighbors, cancelled partial jobs, withheld
final fence, invalid/incomplete physical data and resize/rebinding. A valid
destination may recompute when reuse metadata fails; invalid actual destination
state must cancel/restart while preserving the current owner.

## Worker judgment

The source already gives immutable topology and allocated work storage lifetimes
longer than one replacement. It does not support attributing the private map or
index-scan stalls to normal production, or claiming current visibility is rebuilt
solely because incoming exists. The shared submission/fence and any measured
per-frame host work remain legitimate subjects of fresh event correlation.

**No single avoidable production CPU/resource responsibility is proven by this
read-only audit.** Its cost/payoff cannot yet be projected under the 8.33 ms gate.
Fresh lead measurements and Work C's independent KSA comparison are required.

## Source anchors

- `native/NovaCore.Native/NovaCoreNative.cpp`: 641 memory selection; 767 validation;
  1086 material images; 1128/1137 material staging/transfers; 1551 allocation/mapping;
  1592/1600 incoming/spare; 1603 descriptor update; 1609 topology residency;
  1625 incoming acceptance; 1689 submission resources; 1792 frame upload;
  1906 current/incoming work; 2027 resize; 2226 submit; 2242 teardown;
  2280 publication; 2319 update timing ownership.
- `RegionalPhysicalPreparation.inl`: 4 pupil publication; 25 transaction ownership;
  70 bounded physical recording.
- `RegionalPhysicalResidency.inl`: 8 allocation; 44 fence-complete inspection;
  76 host update/dependency handling; 139 demand recording.
- `RegionalPhysicalResidency.h`: 56 immutable dataset match; 63 demand;
  70 completeness; 76 uploaded residency publication; 80 worker loop.
- `src/NovaCore.Graphics/PlanetaryProductionSphericalBillboardMovingRuntime.cs`:
  135 update, 206 submit, 220 background generation, 277 native acknowledgement.
- `src/NovaCore.Graphics/PlanetaryNestedScaleMeshRuntimeAdapter.cs`: 80 parent map.
- `samples/NovaCore.Triangle/Program.cs`: 234 native lease acknowledgement;
  340 pinned submission lease.
- `shaders/production_nested_scale_mesh_cull.comp`: camera/view-dependent current
  visibility; `production_spherical_billboard_incoming_prepare.comp`: complete
  incoming physical evaluation, with no accepted reuse path.
