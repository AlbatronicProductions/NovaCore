# Residual regional-frame resource qualification review

This is a private proof review against banked M13.4, commit
`047ae479b33831eae1c0dfa3f37c657a7f70148f`. It does not qualify a production
implementation, assign M13.5, or authorize banking.

The current M13 closure limit is **8.33 ms**, not the earlier 11.11 ms diagnostic
threshold. The vertex-validation traversal completed with exit 0, no recorded
errors and 37 publications. Nevertheless, **37 of its 44 selected transition
frames exceed 8.33 ms**. Their GPU mean is 9.051957 ms, P95 10.69556 ms and
peak 10.98624 ms. This is a failed closure gate. No full production suite was
run for this private correction.

## Final-slice and publication chronology

The prior mapped-address evidence identifies the following six final slices.
These are identities from that recorded run, not native frame numbers to reuse
unconditionally in a fresh run.

| Logical frame | GPU frame | Source to destination generation | Source to destination pupil | Final slice: first + count | First new-owner logical frame |
|---:|---:|---|---|---|---:|
| 343 | 446 | 16 to 17 | 25 to 26 | 589824 + 57178 | 344 |
| 355 | 458 | 17 to 18 | 26 to 27 | 655360 + 41050 | 356 |
| 367 | 470 | 18 to 19 | 27 to 28 | 655360 + 56746 | 368 |
| 723 | 826 | 34 to 35 | 45 to 46 | 589824 + 57178 | 724 |
| 735 | 838 | 35 to 36 | 46 to 47 | 655360 + 41050 | 736 |
| 747 | 850 | 36 to 37 | 47 to 48 | 655360 + 56746 | 748 |

The earlier `Prep publication: pupil=` field records the **source published
pupil**, because `m13-regional-preparation-blocker/oracle.inl:11` reads
`regionalPublishedPupil` before the swap. Target pupil identity comes from the
physical-slice record and the incoming frame bytes.

Live source establishes this order:

1. `Update` waits the prior frame fence before inspecting or changing resources
   (`native/NovaCore.Native/NovaCoreNative.cpp:2321`).
2. Current rendering work and incoming preparation are recorded in one graphics
   command buffer. `RecordRegionalPreparation` advances a contiguous bounded
   range; only its final incoming slice returns true
   (`RegionalPhysicalPreparation.inl:70`).
3. That return permits incoming reset, camera-dependent cull and compaction
   (`NovaCoreNative.cpp:1911`). Intermediate incoming slices omit this work.
4. Queue submission attaches the ordinary frame fence; only fully recorded
   incoming work arms the incoming publication flag (`NovaCoreNative.cpp:2226`).
5. The next `Update` waits that fence, validates readiness, transfers incoming
   resource ownership to current, retains outgoing work as spare and updates
   current descriptors (`NovaCoreNative.cpp:2280`).
6. The next `Record` runs current reset/cull/compact again, before the first
   indirect draw of the newly published physical generation
   (`NovaCoreNative.cpp:1906`, `:1991`).

Thus the six prior failures correlate with final incoming work. The host swap
occurs after those GPU frames complete. It does not copy the physical buffer.
Correlation alone does not assign the measured residual to the swap, barriers,
or individual dispatches; use this ticket's separate timing evidence for cost.

## Current resource state machine

| Resource/state | Creation and owner | Read/write permission | Fence, invalidation and reassignment |
|---|---|---|---|
| Managed prepared generation | One background task produces a generation; coordinator moves it through ready and submitted | Native submission lease pins its arrays; no physical CPU array is supplied for native GPU preparation | Lease remains until native publication acknowledgement, then is disposed; native topology owns its copied data |
| Resident topology | Native `AcquireProductionBillboardTopology` copies lattice/indices once for family, hash and counts | Immutable GPU reads; current and incoming may share the resident topology | Survives work-buffer swaps and swapchain recreation; destroyed at renderer teardown |
| Incoming work | `AcquireProductionBillboardIncomingWork` takes a sufficiently large spare or allocates work buffers | GPU writes destination slices; current owner remains readable and visible | Complete dependencies and frozen incoming pupil required; final submitted fence and readiness required before publication |
| Current physical data | First complete incoming publication establishes it | GPU reads physical data for current cull/rendering; it remains unchanged during incoming replacement | Full replacement transfers outgoing storage to spare; same-topology pupil refresh instead swaps with regional scratch after the fence |
| Regional scratch | Separate renderer-owned allocation for staged current-pupil preparation | GPU writes while published current storage remains unchanged | Incoming topology replacement cancels unpublished current refresh; successful refresh swaps buffer, memory, mapping and capacity together |
| Spare | Outgoing current work retained after publication | No authority; can be overwritten as a future incoming destination | Capacity reuse does not imply content reuse; a content epoch must advance before reassignment |
| Mapped work allocation | `CreateHostBuffer` creates, binds, maps and initializes coherent storage | Host mutation follows the existing frame fence; GPU permission follows its current role | Mapping persists through slices and publication; `DestroyHostBuffer` unmaps, destroys and frees on retirement/reallocation |
| Regional catalog/payload | Residency worker reads/verifies immutable records; host copies completed residuals and publishes resident flags | Prepared physical work starts only after its complete geographic dependency footprint is available | Different geographic dataset identity requires renderer-generation replacement; residency growth alone is not a new dataset |
| Descriptor/control resources | Native submission creation binds the actual buffers | Host updates descriptors/control after completed frame work | Resize waits device idle, recreates submission/frame resources and rebinds retained physical resources; any capability binding epoch must refresh |
| Teardown | Native renderer owns Vulkan destruction | No further readers/writers permitted | `Destroy` waits device idle before freeing resources; managed leases are cleaned up after native runtime return |

Relevant live owners:

- `src/NovaCore.Graphics/PlanetaryProductionSphericalBillboardMovingRuntime.cs:206`
  (submission), `:220` (one background generation), `:277` (native acknowledgement).
- `samples/NovaCore.Triangle/Program.cs:234` (acknowledged lease disposal), `:340`
  (pinned native submission ownership).
- `native/NovaCore.Native/NovaCoreNative.cpp:1551` (allocation/mapping), `:1592`
  (incoming/spare), `:1609` (resident topology), `:2027` (resize), `:2242` (teardown).
- `native/NovaCore.Native/RegionalPhysicalPreparation.inl:4` (pupil publication),
  `:25` (transaction freezing and current retention).
- `native/NovaCore.Native/RegionalPhysicalResidency.h:56` and
  `RegionalPhysicalResidency.inl:82` (immutable dataset identity and upload ownership).

The rejected placement prototype chooses device-local, host-visible coherent
physical work storage where available; it is not a new staging-copy architecture.
Current/incoming/spare roles move the same allocation handles. The existing
topology map is a separate host-visible diagnostic resource. Neither its raw
pointer nor a recycled Vulkan handle is sufficient lifetime identity.

## Implemented predicate model and its limits

`capability.hpp` contains shared C++20 predicates, not an implementation of native
resource ownership. `capability_test.cpp` compiled under MSVC 19.51.36252 with
`/std:c++20 /EHsc /W4 /WX /O2` and executed **91 checks, zero failures**. Both
compilation and execution returned 0. `capability-test.json` records identities
and explicitly states `actualNativeBindingAllocationEpochIntegrationProven=false`.

The offered token is compared against independently supplied expected state:

- Full 64-bit source/destination publication generations.
- Renderer, binding and transaction epochs.
- Source/destination physical allocation, content epoch, lattice identity,
  topology family/hash/counts, and exact 160-byte pupil-frame representations.
- Physical-domain body/generation/radius and immutable oracle, regional,
  support and preparation identities.
- Map allocation, mapping epoch, content version/hash, topology pair and extents.
- Native source/destination/map liveness, capacities, binding/read permissions,
  completed submission serials and retained current owner.

The decisions deliberately differ:

| Condition | Required action |
|---|---|
| Valid transaction/source/map capability | `AllowReuse`: evaluate a guarded map entry; this is not permission for an unchecked copy |
| Stale offered token or unusable source/map, with valid actual destination | `Recompute`: evaluate complete destination H and normal |
| Invalid actual destination, dependency state, fence or job | `CancelRetainCurrent`: do not dispatch fallback into invalid memory |

`CanPublish` additionally requires matching physical destination and transaction,
complete prepared/validated vertex counts, no invalid positions/normals, an
actually completed final submission, and a recorded final slice. It remains an
**additional physical gate**, not a replacement for existing draw readiness.

The native private timing/vertex control does **not** establish that real
allocation/descriptor epochs populate and consume these predicates. The 91 tests
exercise model states. They do not execute Vulkan, validate a real GPU map
address, compare physical GPU words or demonstrate production correctness.

## Remaining execution-level negative controls

Before any future reuse implementation can ship, consume the actual guards and
deliberately exercise:

1. Basis/radius mismatch; exact direction and signed-zero cases at mapped entries.
2. Source and destination generation mismatch, including equal low 32 bits but
   different high 32 bits; publication must not acknowledge a stale token.
3. Current to spare to incoming reuse of an allocation, overwritten content epoch,
   cancelled partial preparation, and source lifetime ending before use.
4. Invalidated mapping epoch/address, stale map contents, wrong topology pair,
   wrong orientation of parent mapping, truncated map extent, sentinel and
   out-of-range source indices. Use valid bound storage to test rejection; do not
   deliberately submit freed Vulkan resources.
5. Same-direction regional dataset revision, including a normal-sampling neighbor
   whose residual changes. A center-height-only check is insufficient.
6. Same-direction facility/support revision and preparation/oracle identity changes.
7. Final fence withheld, incomplete preparation, skipped/duplicated ranges, invalid
   position and invalid normal. Finite old values are not completion evidence.
8. Descriptor recreation/resize during preparation, followed by independently
   refreshed bindings or safe cancellation; raw pointer survival proves nothing.
9. Restart after a rejected transaction: no stale map, completed-count carryover,
   owner gap, overlap, stale publication, or write to current physical values.

Invalid optional reuse metadata must reach unchanged full evaluation for a valid
destination. Invalid actual destination state must retain the current owner and
cancel/restart. Existing immutable runtime asset policy need not be weakened to
test identity changes: use private fixtures and controlled token/resource epochs.

## What the vertex validator preserves

The former incoming cull first checked every triangle's indices and its referenced
physical XYZ/normal data, then performed camera-dependent rejection and built an
indirect list. Its physical tests were finite XYZ, finite normal XYZ, and exactly
`normal.w == 1`.

The private vertex control moves immutable index-range validation to topology
acquisition and checks every physical vertex once on the GPU, including records
copied by reuse. This preserves those predicates and additionally catches invalid
unreferenced vertices. It does not validate body.w, reserved words or unit-normal
length; the former incoming cull did not validate them either. The physical-word
oracle is a separate responsibility.

The reduction count in counter 23 and invalid count in counter 3 are real validator
outputs; skipped cull/compaction must not be reported as `cullReady` or
`compactReady`. The control reports `cameraListPrepared=false`. Current reset,
cull, compaction and their barriers still execute before the newly published
generation's draw.

Qualification caveats:

- The matching resident topology's index-validation proof should be explicitly
  consumed with its topology/lattice allocation identity at publication. An
  unrelated boolean or merely observing an earlier successful scan is insufficient.
- Full vertex validity cannot distinguish correct finite data from finite stale
  data. Sequential range ownership, generation/lifetime guards and GPU equality
  evidence remain mandatory.
- Separating physical publication from camera-specific draw readiness is a real
  contract change. The old `ProductionBillboardPublicationReadiness` also requires
  complete triangle accounting, compaction and valid indirect arguments. Do not
  bypass it using fabricated zero-visible counts.

### CPU index-validation cost

The vertex traversal's ten topology qualification records measured
**183.8632 to 451.4311 ms** for scans of 1,806,864 to 4,272,624 indices. The private
code scans `topology.indexMapped`, i.e. mapped native GPU storage. This synchronous
path is not an acceptable production qualification design even though each
resident topology is scanned only once.

A future bounded implementation should validate the immutable **CPU source** at
its upload ownership boundary, retain proof with the exact resident allocation,
and transport/reuse the already checked result. It must measure that path; this
review does not claim its cost has been recovered. The synchronous rational map
construction from the earlier prototype is likewise still not production-ready.

### Counter 23 telemetry caveat

Counter 23 also carries current-render maximum outer TES factor. Native `Update`
publishes before `InspectAnchoredPipelineStatistics`, and that inspector reads
current counter storage. At the publication-inspection boundary, the new
vertex-validation count can therefore be interpreted as float TES telemetry until
current reset clears it. This does not itself change rendered geometry, but exact
TES-factor claims must account for the boundary or use separately identified
storage. GPU pipeline query counts and this counter interpretation are distinct.

## Narrow KSA comparison

No new broad KSA scan was performed. Earlier verified local production evidence in
`../ksa-terrain-convergence/ksa-provenance.json` and the retained terrain
architecture review supports device-local per-frame terrain work resources,
ordered prepare/modifier/finalize/normal/cull work, and completion-governed
streamed publication/resource lifetime.

- **ADAPT:** explicit buffer lifetime and completion-governed availability.
- **INTENTIONALLY DIFFER:** NovaCore requires complete authoritative regional
  physical preparation and atomic retained-current publication; it cannot adopt
  incomplete physical data as a substitute merely because streamed visual data
  can use coarser presentation elsewhere.
- No reviewed KSA source establishes NovaCore's exact current/incoming camera-list
  readiness contract, validates this private replacement, or proves a particular
  transition timing budget. No KSA time-slicing or prepared-result reuse claim is
  inferred from allocation choice.

## Qualification result

**CPU capability model: PASS (91/91).**

**Actual native map/allocation/binding lifetime integration: NOT QUALIFIED.**

**Private vertex traversal: completed, 37 publications; closure FAIL at 8.33 ms.**

No production implementation or full production validation is claimed. This
review preserves the concrete resource obligations for Project Control without
promoting the rejected composition or expanding the performance scope.
