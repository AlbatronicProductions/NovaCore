> Lead closeout addendum: final guarded-source live tests passed in Debug and Release (128.759 / 113.893 seconds); six launcher routes, 500-frame Florida smoke, 1,701-frame L0-L17 traversal, 779-frame warp and 240-frame post-cleanup Florida smoke all passed. The pending gates named in the review checkpoint below are now resolved. No correctness blocker remains. See validation-summary.json and closeout.json; the review chronology and limits below are preserved.

# Verify A — GPU-hot buffer placement contract and cheap parity gate

This is an adversarial architecture review of the emerging allocation-placement
candidate. It is not a declaration of production readiness. The lead owns GPU
execution, implementation, full validation and final judgment. This worker wrote
only this review and `placement_parity.py`; it did not build or run the GPU.

**Latest review status: no remaining correctness blocker found.** The actual
production-default candidate, including the diagnostic snapshot and empty-index
guard, passes fixed-input Florida and inland parity against retained pre-change
hashes. The measured private capture host is
`3fcd4b19550dc69213ddaf7744a811d0874750ff317ee34559daf8b7a5ed7ab0`;
all production shader hashes match baseline and no placement-control environment
variable is present. `candidate-florida-parity.json` and
`candidate-inland-parity.json` each show exact full prepared records, oriented
submitted triangles, D32/HDR/image hashes and matched frame inputs.

The Debug diagnostic timeout is resolved by the bounded snapshot revision:
the unchanged complete window category passes 4/4, zero skips, in 158.034 seconds
against the unchanged 600-second child timeout. It retains 43 live captures,
1,569 checked vertices, levels 8–17 and 40 pupils. Release window passes 4/4 in
118.931 seconds with 43 captures and 1,584 checked vertices. The completed
managed suite reports Debug and Release each 79 headless, 8 GPU/no-window and
4 window passes, zero failures/skips after the resolved retry. Both native
memory-policy tests pass all 14 checks. The initial Debug 3/4 result remains in
`validation.json`, and `probe-revision-proof.json` preserves its classification.

The final empty-index guard was added after that full-suite run. Final rebuilt
normal Debug/Release focused live tests and remaining route/dynamic gates are
lead-owned and pending at this review checkpoint. The earlier observations below
retain chronology; they do not override these completed checks or claim those
final pending gates have already passed.

Cheap output gate inspected: **Florida and inland PASS**, each on its first
baseline/local pair at frame 175 and 3440×1440. No baseline repeat was needed.
Full prepared 64-byte records, exact oriented submitted triangle multisets,
D32, HDR and final image all match; both runs have zero changed pixels in all
three attachments and no input-identity differences. Raw compacted index order
differs in Florida, which the exact multiset test correctly preserves as a
non-authoritative ordering distinction. See `placement-florida-parity.json` and
`placement-inland-parity.json` for identities and hashes. The companion
`verify-a.json` fingerprints the reviewed reports.

There is **no architectural blocker** to the lead's proposed explicit
`MappedBufferUse::TerrainGpuWorkingSet` design, subject to the failure/fallback
conditions below and subsequent implementation validation. The lead also
reported matched regional recovery of about 2.45 ms GPU with about 0.047 ms
additional non-fence CPU time; Work A owns that performance attribution. These
positive cheap gates do not substitute for full production validation.

## Bounded responsibility

The coherent responsibility is the physical terrain working set repeatedly
consumed or produced by the GPU: prepared physical output, staged pupil output,
visibility, compacted draw indices, immutable topology lattice and immutable
topology indices. Current, incoming and retained spare instances inherit their
resource role. Moving their backing memory preference is one placement change,
not prepared-value reuse, scheduling or terrain mathematics.

The private prototype identifies six roles by allocation error strings. That is
adequate to select a controlled experiment, but production policy should be an
explicit private allocation intent at the owner. Use a small typed intent or
dedicated helper/default parameter, not a public API, environment flag or string
match on a diagnostic message. All other host-buffer callers retain their
existing behavior. No new role should be included simply because it is mapped.

Do not expand the set to regional catalogs/payloads, frame controls, counters,
material staging, capture buffers or general application resources. They have
different CPU-read/write and publication frequencies. Do not use this ticket to
reorganize the allocator or introduce a general residency manager.

## Required selection and fallback

Preserve `HOST_VISIBLE|HOST_COHERENT` and the buffer's existing
`memoryTypeBits` compatibility requirement. For the explicitly selected GPU-hot
intent, prefer a compatible type additionally marked `DEVICE_LOCAL`. If none is
available, use the existing compatible host-visible coherent selection. A device
without local mapped coherent memory is supported by that fallback; it should
not need a software/device-specific special case. A diagnostic control that
requires local memory and throws if absent is not the production contract.

Compatibility must be checked before preference. Never select a memory type
solely by heap size, property equality or a known AMD index. Preserve the current
failure when no type meets the required properties. Allocation/bind/map failures
must remain observable. Decide explicitly whether supported-local allocation
failure retries a compatible fallback or remains a reported allocation failure;
do not imply that a type-selection fallback guarantees recovery from VRAM
exhaustion. If implementing retry, clean up the unsuccessful allocation and
retain normal device-loss and validation reporting. No such retry is proven by
the diagnostic selector alone.

The proposed production policy explicitly retries the original compatible
host-coherent type when preferred allocation/mapping fails with recognized
memory-capacity errors. Mapping the tentative allocation before binding permits
it to be freed before retry. Do not store tentative handles or pointers in the
live resource until the attempt succeeds. Failed mapping must free its memory;
a failure after a successful mapping must unmap before freeing. Binding failure,
device loss and validation failure are not broad retry categories. If fallback
and preferred types are identical, as can happen on UMA, do not perform a
redundant second attempt. Both attempts failing must preserve an observable
failure. Exercise this control flow through bounded helper tests rather than
exhausting real GPU memory to manufacture the error.

Keep allocation size, usage, count and alignment unchanged. The lead reports
about 96.8 MB of selected buffers at the fixed L17 pose and no new allocations;
this moves existing bytes between eligible memory types. It does not prove peak
VRAM occupancy for the complete route: retained topology levels, current,
incoming and spare capacities can coexist. Record those actual allocations and
the selected heap capacity, especially across L0–L17 and replacement traversal.
No new cache eviction or budget manager is justified without a measured failure.

## Lifetimes that must remain identical

Native `CreateHostBuffer` creates, allocates, binds and persistently maps a
coherent buffer. `AcquireProductionBillboardTopology` fills immutable lattice and
index storage once per resident topology identity. Incoming work reuses adequate
spare capacity or allocates a distinct work set. Placement must not turn a
resource handle, mapped address or capacity into a content-validity token.

Before mapped writes and descriptor changes, `Update` waits for the preceding
single in-flight submission. Current cull/render reads the existing current
physical allocation while incoming preparation writes its distinct destination.
The final incoming submission must still finish and satisfy complete physical,
normal, topology and draw-readiness checks before `InspectProductionBillboard`
changes the sole current owner. Publication moves the resource set and keeps
the outgoing set as a spare; no content-copy fast path is introduced. Same-
topology pupil staging retains its distinct complete-before-swap behavior.

Descriptor resource/range ownership, GPU barriers and frame-fence sequence are
unchanged by the candidate. Resize waits for device idle and recreates frame/
descriptor resources while resident terrain resources survive. Teardown waits
before unmapping and freeing. Tests must cover these actual paths on the changed
memory type. A placement change does not justify removing a fence because the
new allocation is device-local, or adding a new queue/scheduling dependency.

Earlier capability-model checks and private exact-copy oracles are not live
qualification of a new reuse implementation. Conversely, the proposed placement
does not introduce reuse-map tokens or generation-based content copying; their
unimplemented integration is not a new prerequisite for preserving the existing
production state machine. Qualify the allocation intent, properties, bytes and
existing lifetime that actually change.

## CPU/readback attacks

Coherent mapping removes explicit flush/invalidate requirements for these
allocations; it does not promise that CPU access is equally fast on every heap.
Cold topology upload and any initial CPU physical upload may write across the
host/device link. Production readback, regression tooling and capture helpers
may read device-local mapped storage more slowly. Separate cold uploads and
diagnostic bulk reads from normal repeated frame costs, but do not conceal a
repeating gameplay stall behind that distinction.

Normal M13.4 has no private synchronous rational-map construction or mapped-index
scan. Those rejected prototype stalls must not be reintroduced or counted as
production savings. Existing per-frame host control/counter paths are outside
the six-role selection, helping preserve bounded CPU update behavior. Measure
the actual normal CPU update, fence, record, submit and publication windows after
placement, with warm and transition cases. The captured full prepared readback
is deliberately excluded from final performance measurement.

## Bounded retained parity runner

The lead may run, after `whole.py build-v3`:

```powershell
python -B docs/engineering-evidence/m13-final-whole-frame-causality/placement_parity.py florida placement-florida
```

Other supported poses are `inland`, `active` and `grazing`; use a fresh label for
each. The runner does not build, alter production, regenerate assets or delete
files. It enables the existing one-frame capture and raw input correlation, then
calls `whole.fixed` for baseline and the six-role bulk-local control sequentially.
It uses the same three `OUT` paths: `pixels.bin`, `prepared.bin`, `selected.bin`.
No second raw folder or permanent raw archive is created.

The default is native frame 175 at 3440×1440. The capture is inspected after its
fence and before subsequent regional/publication inspection. The runner requires
one matching capture marker, fresh output-file timestamps, exact lengths and a
matching raw `Composition frame` identity. It compares runtime and all shader
hashes, scenario arguments, environment apart from placement/capture output,
camera/GPU/presentation/lighting/published/incoming input bits, generation,
topology, pupil capture identity and hardware geometry counts.

It hashes every byte of the full current prepared 64-byte records and preserves
the selected index triples as an exact oriented multiset. Sorting only the list
of triples permits nondeterministic compaction order while retaining triangle
winding, duplicate multiplicity and index membership. It also records raw index
order equality and verifies indices lie within the prepared vertex count.

The three attachment regions are D32, RGBA16F HDR and four-byte presentation.
Byte comparisons count changed pixels and retain bounded numerical/byte examples
and SHA-256 hashes. Finite depth/HDR values are required. Work is chunked to
avoid creating multiple full-resolution floating-point difference arrays.

Only if A/B has any mismatch does the recipe run one additional baseline A/A
capture. Both A/A and repeated-baseline/B comparisons are retained. An A/A
difference does not automatically pardon a candidate difference; the original
A/B pass remains false and the lead must classify it from causal evidence.
Failure to obtain fresh capture or exact frame input metadata stops the recipe
instead of accepting stale bytes.

## Evidence limits and blocking conditions

### Actual production diff reviewed

The implemented candidate adds `MappedBufferMemory.h`, its bounded native test,
the `CreateHostBuffer` terrain-intent branch and exactly the six intended call
sites, including pupil scratch in `RegionalPhysicalPreparation.inl`. The ordinary
host branch is unchanged. Resource sizes, usage flags, count/capacity bookkeeping,
descriptor operations, barriers, publication and destruction paths are unchanged.
No shader edit, probe environment selection or error-message string match entered
production.

`SelectMappedMemoryTypes` filters both required properties and compatibility bits
before selecting the first local preference and original host fallback.
`AllocateMappedMemory` retries only device-memory exhaustion, host-memory
exhaustion and mapping failure; it preserves device-loss/non-capacity results,
the final failure and the no-compatible-type result. A shared UMA preferred/
fallback index is attempted once.

The actual allocation lambda frees a successful tentative allocation if mapping
fails. A failed allocation owns no successful mapping to free. After a successful
map, binding failure unmaps/frees the tentative resource and throws; it is not
retried. Only successful bind publishes the memory and pointer into the live
resource, followed by the existing zero initialization. The created buffer
remains owned by the caller's resource fields for ordinary error teardown.
No concrete cleanup or lifetime defect was found in this diff.

The source test contains 14 policy checks covering selection, compatibility,
coherence exclusion, no eligible type, preferred success, all three capacity
fallbacks, device loss, fallback failure and UMA single-attempt behavior. The
lead runs the Debug/Release target. These are selection/retry model tests, not
mocked Vulkan free/unmap/bind callback tests; cleanup above is source inspection.

The initial exact captures exercised the private six-role allocation selector
with identical production shaders. They do not by themselves fingerprint the
newly built production-default allocator binary. Final validation must preserve
that distinction and identify the actual implemented/deployed candidate. Its
reported fresh Florida timing of 9.19036 ms is performance evidence, not an
attachment-parity capture. No actual-production qualification claim is inferred
from the prototype's successful pixels alone. The subsequently completed
production-default capture comparison identified in the latest status above
closes this fixed-pose implementation-identity gap.

### Bounded revision: CPU diagnostic readback ownership

The lead's actual Debug full-suite live regional child progressed very slowly
with high CPU usage, reaching roughly frame 467 after nine minutes. Probe files
were appearing at 10–30 second intervals around generations. These are reported
live validation observations, not normal terrain frame timings or an independently
sampled CPU call stack. They identify a real diagnostic-consumer regression that
the initial fixed-pose GPU gate did not exercise.

`RegionalPhysicalProbe.inl` enables its work only when
`NOVACORE_REGIONAL_PHYSICAL_PROBE` is set. After readiness, frame, generation and
pupil gates, it formerly aliased mapped physical, compacted-index and source-
index memory directly. It scans every selected triangle, repeatedly loading
its three physical vertices, then scans source triangles while matching the 17
probe results. Those repeated scalar/random CPU reads now cross the local
mapped-memory interface rather than using ordinary CPU memory. This is the
specific ownership mismatch; the fact that a buffer is legally coherent/mapped
does not make it suitable for a repeated CPU analysis loop.

The reviewed revision creates three temporary CPU vectors after the unchanged
capture gates, copies the exact completed physical records, `draw->indexCount`
compacted indices and `triangleCount * 3` source indices once, and points the
unchanged loops at those vectors. Sparse lattice reads used for at most 17×3
reported vertices remain mapped. Capture runs after the existing frame fence;
no new GPU synchronization, dispatch, allocation intent, geometry, equation,
sample selection, file schema, assertion or timeout is changed. The snapshots
are local to one opt-in capture and are freed when it returns. No permanent
CPU physical mirror or normal-frame copy was added.

The final robustness guard skips the compacted-index `memcpy` when its vector
is empty. A fully culled current owner is legitimate:
`ProductionBillboardPublicationReadiness::CompactionComplete`, `IndirectValid`
and `ValidZeroVisiblePublication` expressly permit zero visible triangles and
zero indirect indices. Its existing triangle loop already executes zero times,
leaving samples unfound. The guard therefore avoids passing an empty vector's
possibly null pointer to `memcpy` without changing any valid capture result.

Temporary CPU bytes are exactly `vertexCount * 64 + drawIndexCount * 4 +
triangleCount * 12`, plus vector overhead. For the reviewed Florida capture this
is 63,321,656 bytes. Allocation sizes remain bounded by the existing current
topology/draw counts. The revision does not retain another raw archive. Source
review found no changed diagnostic result or lifetime responsibility; the lead
must rerun the unchanged live regression to establish that this fixes the
observed delay. Increasing the 600-second test timeout or weakening its physical,
residency, pupil/level, facility, owner or publication assertions is unnecessary
and is not part of the revision.

The unchanged full-window rerun now establishes that the snapshot removes the
observed timeout, as recorded in the latest status. It does not prove raw live
before/after frame JSON identity: the live route uses elapsed simulation time,
and retained common-frame files have differing camera, orientation and matrix
inputs. `probe-revision-proof.json` explicitly records those differences.
Canonical live assertions remain the same; fixed-input physical and attachment
identity is established by the separate production-default captures.

The remaining CPU consumer audit found two other explicit one-shot diagnostic
scans in `NovaCoreNative.cpp`: the raster lineage trace gated by diagnostic bit
8192 and `!productionBillboardRasterTraceLogged` (around lines 2117–2150), and
the projected physical-vertex trace gated by submission bit 128 and
`!productionBillboardProjectionTraceLogged` (around lines 2151–2167). Their
mapped scalar scans can also be slower when explicitly invoked. This is a
bounded diagnostic readback limitation, not a recurring ordinary-render cost;
their source was not changed in this revision. If a required test exercises and
fails one, apply the same measured CPU-snapshot ownership principle in a bounded
follow-up rather than silently skipping its evidence.

Other normal accesses to the selected buffer fields are creation-time
zeroing/upload, handle/pointer ownership movement, sparse/null/count checks and
GPU consumption. Normal readiness/statistics reads the unchanged host-intent
counter and indirect buffers. No analogous repeated normal-production bulk CPU
scan was found in this focused audit. This conclusion does not claim that all
future diagnostic consumers will automatically have good CPU performance.

One exact fixed frame does not qualify movement, publication, regional/support
transitions, resize or repeated creation/destruction. Full prepared bytes cover
the current published representation, not every unpublished incoming state.
D32/HDR/image and the exact submitted triangle multiset do not record every
individual post-TES vertex. Production shaders staying byte-identical plus the
unchanged draw inputs constrain that boundary, but should not be called a newly
instrumented post-TES vertex oracle.

Block promotion for mismatched runtime/shader/input identity, changed full
physical bytes, changed oriented submitted geometry, unexplained attachment
differences, unsupported-memory failure without the required fallback, invalid
coherence/compatibility selection, resource lifetime or Vulkan errors, missing/
overlapping/stale ownership, an introduced repeating CPU stall, or failure to
reproduce meaningful net performance. The permanent report should also state
any remaining >8.33 ms frames even if this ticket's >=1.5 ms meaningful-payoff
criterion ultimately passes. No qualification result is claimed until the lead
executes and reviews these gates.
