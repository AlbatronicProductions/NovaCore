# Work B - exact M13.5/M13.6 key-buffer contract

Offline source/contract audit, 2026-09-08. No GPU runs, builds, production edits, Windows-event access or independent external research were performed. Both earlier evidence packages remain unchanged. The lead verified the narrowly requested Vulkan specification references below. Source facts, retained control observations and unknown failed-process state are distinguished throughout.

## Baseline and actual delta

HEAD and origin/main remain `d4baab6940a57a46e478b98e36f5e45d1c4b558f`, the banked M13.5 source; branch `codex/m13-final-exit`. The tracked candidate changes exactly `MappedBufferMemory.h`, `MappedBufferMemoryTests.cpp` and `NovaCoreNative.cpp`. There are no staged changes. Companion JSON records the current/bank file hashes, normalized comparison, exact diff hash and relevant source identities.

The allocation is one storage buffer containing 8192 slots of 12 uint32 words: **393,216 logical bytes**, descriptor set 0 / binding 8, offset 0 and that full range. It has no buffer-device-address usage or address query. Logical slot indices and geographic key fields are not GPU virtual addresses.

| Property | Bank M13.5 | Candidate M13.6 | Proven boundary |
| --- | --- | --- | --- |
| Selection rule | First compatible HOST_VISIBLE + HOST_COHERENT type | Prefer a compatible type also HOST_CACHED; retain the original first-compatible coherent host fallback | Compatibility is checked against this buffer's `memoryTypeBits`; no new noncoherent type is allowed |
| Retained observed selection | Type 1, flags 6, heap 0 | Type 3, flags 14, heap 0 | Earlier controlled and actual-default parity traces; not the failed process's allocation record |
| Host visibility / coherence | Required | Required | Flags 6 = HOST_VISIBLE + HOST_COHERENT; flags 14 add HOST_CACHED |
| GPU/device locality | Not required | Not required for this role | Neither observed type includes DEVICE_LOCAL. Another compatible platform can have a different topology |
| Device-coherent AMD extension flag | Not required or selected by this role | Not required or selected by this role | HOST_COHERENT is not AMD DEVICE_COHERENT |
| Initialization order | Create buffer -> allocate -> bind -> map -> zero -> seed | Create buffer -> allocate -> map -> bind -> zero -> seed | This order changes because the call enters the existing preferred-memory allocator; it is not a per-frame remapping change |
| Mapping | Offset 0, logical buffer size, persistent | Same range and persistence | Allocation uses driver-reported `requirements.size`, which need not equal the logged logical bytes |
| Binding / layout | Storage buffer, exclusive sharing, binding offset 0 | Same logical contract | Does not prove equal VkDeviceMemory handle, CPU pointer, GPU VA, physical pages or driver mapping |
| Failed allocation/map | Original host allocation path reports failure | Existing preferred allocator can release a failed tentative allocation and retry the different compatible fallback | Capacity/map failures only; device-lost/unknown errors remain fatal. A failed bind is cleaned up and reported, not retried as a capacity failure |
| Lifetime after initialization | Renderer-lifetime key allocation | Same | Resize retains it; zoom and NCSM1 publication do not replace it |

The retained compatibility mask is 0xF (types 0-3); the selected traces report heap 0 for both host types. A shared heap index does **not** establish identical physical backing or address. Type eligibility describes legal access properties, not a platform-level placement or fault diagnosis. The failed process's allocation handles, actual map result, GPU mapping and chosen fallback are absent from this offline source proof.

## Answers to the eight explicit questions

### 1. Can M13.6 alter the GPU-visible address?

**Yes, it can alter allocation/binding identity; equality is not established.** The selected memory type changes, and each run creates a new buffer and memory allocation. The driver may assign different GPU virtual mappings and physical backing. Source preserves binding index/range and shader indexing, not a numerical GPU address. NovaCore does not obtain or retain a raw buffer device address for this object. Even two runs selecting the same type would not imply identical addresses.

There is no new pointer arithmetic, pointer serialization, address-based identity, rebinding of an already-bound key buffer, or descriptor offset change. Descriptor creation receives the actual VkBuffer handle (`NovaCoreNative.cpp:1767-1769`). Address equality is unnecessary for correct descriptor use, but source cannot exclude a platform/driver issue associated with different backing.

### 2. Can it alter data visibility to the GPU?

**The physical cache/memory path can change; the required visibility contract does not.** Both selections require host-visible, host-coherent memory. Host initialization and publication writes occur before queue submission; `Record` includes a HOST_WRITE -> SHADER_READ barrier targeting the relevant compute/graphics stages (`:1952`). Normal mapped updates follow the prior frame-fence wait (`:2342`, `:2399-2407`). No barrier, access mask, shader, descriptor or submission ordering changed in the candidate.

That is a contract proof, not evidence that the failed driver executed it correctly. HOST_CACHED may change implementation behavior, latency and backing. It does not permit stale GPU reads under a correctly satisfied visibility contract. There is no basis here to add a flush, relax synchronization or claim actual GPU visibility was observed at the hang.

### 3. Can CPU caching produce stale values under the actual synchronization contract?

**No newly missing visibility dependency is demonstrated for the normal Florida key consumers.** Exact field ownership is decisive:

| Words / bytes within each 48-byte slot | Normal host owner and use | Production-v5 GPU use |
| --- | --- | --- |
| 0-7 / bytes 0-31 | Host seeds body/version/validity and face/level/x/y. Host request/upload code reads these fields | Immutable production readiness bypasses these key reads; generic helper branches contain key reads but are not this production branch |
| 8 / bytes 32-35 | Initialized to zero; the host request poller does not read it | Selector writes last-used terrain frame when that selector executes |
| 9 / bytes 36-39 | Initialized to zero; the host request poller does not read it | Selector reads/writes transition start frame |
| 10 / bytes 40-43 | Host publishes payload layer+1; polling and upload matching read it | Immutable production readiness/binding checks bypass the key-table fallback |
| 11 / bytes 44-47 | Host request/upload state: initial 0, queued 1, prepared 2, failed 3; host reads it | No production selector write |

`QueueProductionRequests` performs two complete slot scans but reads only words 0-7/10-11 (`:1124-1127`). `CompleteProductionUploads`, bootstrap and the render-thread upload preparer publish host-owned state (`:1109-1136`, `:1150-1185`). The I/O worker exchanges its own queue/ready payloads under a mutex; it is not handed the mapped key pointer. An I/O mutex is not being mistaken for Vulkan synchronization.

The production shader's `ProductionReady` is a level bound (`planetary_select.comp:72`). `ProductionPatchBindingCurrent` returns the canonical ordinal binding before the mutable-key fallback (`:98-112`). Production selector writes are confined to words 8-9 (`:129`); the complete hierarchy is bootstrapped before the first submitted presentation. After NCSM1 is authoritative and enabled, `gpuPlanetary` is false and the only production pipeline that declares this key buffer is not dispatched (`NovaCoreNative.cpp:1936-1956`). Regional demand, NCSM1 physical preparation, cull, TES and fragments do not consume binding 8 as this key table.

Host coherence removes explicit host cache-management calls; it does not make concurrent unsynchronized access safe. A fence alone is not a universal GPU-write-to-host visibility guarantee. Here the normal CPU poller does not consume the two GPU-written words, and ordinary host-owned values remain ordinary host writes/reads. Sharing a cache line does not invalidate those coherent-memory guarantees. No CPU stale-key result was measured in the failed process.

The generic non-production selector can write all three uvec4 records (`planetary_select.comp:144-145`). That is a distinct branch and must not be silently promoted into a failing Florida path. Nor does this audit prove every possible malformed input, route switch or generic-scene synchronization correct. The incident's final controls/owner were not captured, so source reachability is conditional on the intended production Earth route remaining selected.

### 4. Can it change map lifetime?

**The successful map begins earlier relative to bind; the steady-state lifetime remains renderer-wide.** Bank maps after binding; candidate maps before binding. Candidate may also attempt a preferred map, free it on failure, and map a fallback. Both successfully zero/seed only after the buffer is bound and mapped. Neither path remaps on each frame, zoom, LOD or pupil publication.

Mapping VkDeviceMemory before binding this buffer is allowed by the combined valid-usage constraints: mapping requires appropriate host-visible memory/range and no existing mapping; binding requires a compatible allocation, valid size/alignment and no prior binding. Neither requires bind-before-map. This joint specification inference establishes legality, not driver exoneration. [vkMapMemory](https://docs.vulkan.org/refpages/latest/refpages/source/vkMapMemory.html), [vkBindBufferMemory](https://docs.vulkan.org/refpages/latest/refpages/source/vkBindBufferMemory.html)

### 5. Can it change flush/invalidate requirements?

**Not for the compatible types admitted by this policy.** Both require HOST_COHERENT, so adding HOST_CACHED does not create an explicit `vkFlushMappedMemoryRanges` / `vkInvalidateMappedMemoryRanges` requirement. No such key-buffer calls exist or were removed. Synchronization and host/device-domain requirements remain; coherence is not a replacement for them. HOST_CACHED and HOST_COHERENT are compatible properties, not contradictory choices. [Vulkan memory-property definitions](https://docs.vulkan.org/refpages/latest/refpages/source/VkMemoryPropertyFlagBits.html)

The selector excludes cached-but-noncoherent memory. Existing tests exercise that exclusion and preserve the ordinary coherent fallback. This review reads those tests; it does not rerun them or infer a live hardware pass from a selection-policy unit test.

### 6. Can it alter destruction/reuse timing?

**Not the successful path's ordering or key ownership; yes, wall-clock timing and failed-attempt cleanup can differ.** `CreateTerrainResidency` returns if the key buffer already exists (`:1589`). `Recreate` destroys/rebuilds submission descriptors and swapchain resources but retains terrain residency (`:2035-2074`). It rebinds the existing key buffer into the new descriptor set. Zoom/generation retirement never calls `DestroyTerrainResidency`.

Final destruction waits device idle, destroys submission descriptors, joins production I/O, then unmaps/destroys/frees terrain residency (`:2263-2285`, `:1094-1095`, `:1599-1602`, `:1575-1576`). The idle return is unchecked in both versions: do not claim teardown after device loss is independently proved safe. Candidate fallback introduces only startup failure-attempt cleanup at this callsite. Reduced CPU cost can move otherwise unchanged work and eventual teardown earlier in wall-clock time; it cannot be dismissed merely because lifecycle source is unchanged.

### 7. Can it cause the GPU to wait on CPU-visible memory differently?

**It can change memory-access cost or implementation behavior when accessed; it adds no wait-on-key protocol.** There is no shader spin waiting for host publication, key-backed semaphore, new queue, new fence or host-to-GPU event. Production readiness is immutable/canonical, not a retry loop waiting for a CPU key value. The remaining shader loops traverse bounded scene/cache work, not a host-progress handshake.

Actual memory transaction/translation latency can differ between legal types, and the source cannot exclude a driver fault involving their mappings. That is different from a new explicit GPU synchronization wait. The normal application waits its prior frame fence and swapchain acquisition; present waits the acquired image's render-finished semaphore (`:2214-2261`, `:2342`). No wait argument references key memory. Once NCSM1 owns Earth, the key buffer remains allocated and CPU-scanned but its selector dispatch is absent. Descriptor binding by itself does not prove a GPU load occurs.

### 8. Can it plausibly affect zoom-out generation transitions?

**Indirectly, yes; a direct key-to-NCSM1 transition dependency is not found.** CPU scan reduction changes the elapsed-time spacing of input, I/O polling, preparation submission, fence inspection and publication. A different persistent mapping can also alter driver memory state when a first-visit LOD requires another allocation. These are plausible causal avenues, not measured causes.

Wheel input changes camera altitude/direction, then managed LOD/pupil selection and one incoming generation. The normal Earth eligibility rule has no altitude cutoff (`Program.cs:231-279`). The native current generation stays authoritative while incoming topology/physical work is prepared, including separate regional physical dependencies. Final publication follows the existing frame fence and readiness checks; current work becomes a bounded spare. None of those functions reads a key-table slot to choose NCSM1 level, generation, pupil, residency readiness or retirement.

Regional physical data has its own catalog/payload/demand/preparation bindings 53-57 (`RegionalPhysicalResidency.inl:20-24`), derived from the current/incoming physical domain. Its catalog identity is source content and geography, not an allocation address. It must not be conflated with this global 8192-slot material/elevation request table. The key poller still executes before `UpdateRegionalPhysical`, but chronological adjacency is not an ownership dependency.

## Transition and synchronization map

| Stage | Key-table participation | Independent transition responsibility / incident limit |
| --- | --- | --- |
| Sustained near-surface | Same allocation; two host scans; host-owned valid/payload state | Current NCSM1 geometry renders; no key reallocation per frame |
| Wheel / zoom | No key write or map operation in input policy | Camera altitude/direction change after prior frame completion |
| LOD / pupil selection | No key-table input | Schmitt/adjacent-level selector; one transaction; exact last incident state unavailable |
| Incoming generation | No key-based readiness gate | Immutable topology reuse/allocation plus bounded incoming work |
| Regional demand/preparation | Different buffers and geographic/content identity | Incomplete incoming stays unpublished; one physical replacement at a time |
| Retirement / publication | Key remains resident | Fence-qualified current/incoming swap; spare handling and managed acknowledgment |
| Host upload / submission | Poller runs before recording; original host-memory barrier retained | Previous graphics fence -> host updates -> record -> submit; timing can change without an ordering change |
| GPU / present | Key selector absent with authoritative NCSM1; no key semaphore | Single graphics fence, image acquisition and per-image presentation semaphores; no incident-level last-operation proof |
| Resize / teardown | Resize retains key; final teardown destroys it | Separate swapchain/lifetime path; ordinary zoom does not request recreation |

## Accumulated live bounds and existing observations

These are implementation bounds and retained short-control observations, not new telemetry from the failed five-minute session:

- Key allocation: one 393,216-byte logical buffer, one successful persistent map, one memory object; candidate retries can create a discarded startup attempt. Descriptor rebuild does not multiply this allocation.
- NCSM1: at most 18 retained immutable topology resources; current plus incoming **or** spare (at most two five-buffer mutable work sets), plus one high-water pupil scratch buffer. First visits may grow retained capacity; publication count is not a live-object count.
- Regional: two dependency states, at most 896 catalog records and 124,895,232 payload bytes; current 859-record catalog allocates 119,737,728 bytes once needed. One I/O worker, eight ready records and eight uploads per frame. No zoom-out purge/reallocation cycle.
- Retained baseline/candidate full traversals each have 58 publications and 18 topology uploads; regional traversals each have 37 publications and 10 uploads, with 670 contributing records requested/loaded and 93,392,640 bytes uploaded into 119,737,728 allocated bytes. Those matched aggregate counts do not assert exact per-frame geographic equivalence or five-minute stability.
- Full-traversal regional demand differs in those old runs (eight records versus zero), so they are not an exact incident replacement. Warp controls have one publication and no regional payload allocation beyond the placeholder.
- The retained journals omit detailed live work/allocation records. They do not supply a failed-process resource trend, actual pointer/address history, outstanding shader PC, cache contents or last zoom generation. Neither a bounded container nor successful short controls rule out the reported driver/system failure.

Companion JSON references prior summaries and hashes rather than copying full journals. The previous package's broad description of a shared GPU-key table must be read with the exact production-branch distinction above: CPU polling persists, but authoritative NCSM1 does not dynamically consume that key table on the GPU.

## Offline conclusion

No definite M13.6 cache-visibility, mapping-lifetime, destruction or explicit-wait defect is demonstrated. This is **not candidate exoneration**. The legal new memory type, map-before-bind initialization order, potentially different address/backing and changed wall-clock cadence remain real differences that source cannot correlate with the incident. A source correction or rollback-as-causal-proof is not justified by this audit alone.

The lead's incident/dump classification must remain separate from this contract analysis. The missing proof is the failed process's effective memory identity and final operation/workload state, or another sufficiently discriminating piece of existing incident evidence. No GPU reproduction is requested by this worker; M13.6 remains unbanked for Project Control.

Created/retained: this report and its compact JSON. Disposable output: zero. No old package, production source, build output or Git index was changed.
