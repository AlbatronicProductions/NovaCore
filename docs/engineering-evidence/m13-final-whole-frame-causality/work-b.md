# Work B — required versus avoidable terrain work

Baseline inspected: M13.4 `047ae479b33831eae1c0dfa3f37c657a7f70148f`.
This worker read current production source and retained/fresh scalar evidence.
No production edits, builds, GPU executions or diagnostic cleanup were performed.
The accompanying JSON fingerprints the inspected source and initial controls.
The lead's later repeated controls and Work A own the final performance verdict.

## Responsibility map

| Responsibility | Required current outcome | Future fidelity | What is avoidable or already optimized | Measured limit / decision |
|---|---|---|---|---|
| Full physical terrain preparation | Complete authoritative regional/global/facility height and normal for the published pupil | Preserve physical authority independently from presentation detail | M13.3 already moves full relief before TES; prior exact reuse, placement and final-validation probes do not establish one production scheduling fix | Do not reopen rejected regional combinations without new whole-frame evidence |
| TES interpolation, physical receiver and hardware factors | Preserve current prepared-surface interpolation and precision | Needed capacity for future authored displacement, silhouette and near-camera geometric relief | M13.1 removes unneeded geographic address work; M13.3 removes repeated full physical evaluation; factor reduction changes capacity and workloads | Active factor-64 frames can be cheaper than almost-factor-one inland frames; invocation count is not a time measurement |
| Geographic payload lookup/filtering | Current macro albedo and land/base-height classification | Required material authority and filtering | Ordinary NCSM1 already uses its actual receiver; bootstrap parent/child paths are specialized out | No proven duplicate production lookup to retire |
| Presentation biome weights | Seven material-library response weights, derived from FP32 climate/base-height data | Future richer material selection must preserve geographic ownership | Already a presentation-only FP32 path; current top-four selection is not canonical physical evaluation | A constant-weight control would be sensitivity only; no exact >=1.5 ms replacement identified |
| Positive material noise | Body-fixed band-limited albedo, roughness, AO and height-derived shading normal | Detail frequency and continuous geographic identity must remain available; current fragment relief is not authored TES displacement | M13.4 already skips exact zero contributions and shares four-corner hash prefixes | Fresh Florida noise control has large sensitivity, but changes contributing outputs; its entire delta is not avoidable work |
| Facility sun visibility | Authored structure occludes direct terrain light at the correct receiver | Must preserve accepted Florida contact presentation | Existing count-zero, receiver AABB and conservative swept-bound rejection already precede precise box tests | No evidence of >=1.5 ms redundant facility work; removing it changes accepted lighting |
| Current cull and compaction | Correct camera-dependent visible index list and validation for the sole current owner | Future displacement support must remain conservative | Per-frame decision is required when the camera/view/body orientation changes; immutable geometric subexpressions are possible lower-frequency work, but not a proven win | Current cull about 2.3 ms in initial banked controls; compact is separate. Caching all visibility is not automatically safe |
| GPU-hot working-buffer placement | Same physical/topology/visibility/compacted bytes, mappings and read/write ownership | Independent of detail equations and TES density | Current allocator requests host-visible coherent memory, with no device-local preference; first compatible type can be system RAM | Fresh isolated placement control is a credible single responsibility. Repeat total-frame and CPU/memory qualification still required |
| Frame/resource synchronization | Readers see complete data; mapped writes and resource reuse occur after fences | Must preserve atomic publication and current-owner retention | Broad barriers may have narrower theoretical scope, but duration is not proof they can be removed | Prior post-fence publication bookkeeping is tiny; no single safe scheduling saving established |
| CPU request/upload preparation | Discover and publish available assets and controls | Preserve bounded streaming | Existing shared material request/cache work costs about 2 ms even with no image copies | It is measured CPU work, not a >=1.5 ms proven GPU saving; do not silently combine unrelated material-cache optimization with placement |

## Positive material work: expensive is not synonymous with disposable

`production_terrain_material.glsl:25–47` already prepares shared hash-prefix
components for the four corners of each value-noise lookup. This is present in
banked M13.4, not a new candidate. Lines 86–93 select two biplanar projections,
with three in the narrow ambiguous-axis region. Lines 219–239 independently
attenuate broad, meso and micro bands from smooth local derivatives, and skip
noise only when complete land detail or that band's contributions are zero.

Positive values feed geographic color modulation, roughness, AO and shading
normal at lines 253–258. Replacing them with a constant, lowering precision,
reducing bands, changing projection weights or moving nonlinear evaluation to
coarse vertices changes a contributing result. None is justified merely because
the FP64 hash is costly. A precomputed texture/cache would additionally need
exact cell identity, filtering, derivatives, residency and precision contracts;
that is not an established small replacement in the inspected source.

The fresh `whole.py` noise control keeps body coordinates, scale, offset and
projection weights live but substitutes a different field. Florida initial
total GPU medians are 11.06856 ms banked versus 6.83248 ms under that control;
draw medians are 8.04804 versus 4.11872 ms. This identifies substantial material
sensitivity. It does not prove 3.93 ms of production-safe draw savings: the field,
derived normal, register pressure and compiler scheduling change. Interface and
upstream ISA checks are required even for that bounded sensitivity claim.

Useful bounded control separation is noise value synthesis versus biome weight
selection versus full cheap fragment. Preserve required inputs as live values,
derivative participation and depth behavior, inspect actual compiled interfaces,
and call output-changing results sensitivities. Do not promote broad
baseline-minus-cheap-fragment cost to exact material time or achievable recovery.

`planetary_production.frag:4` already uses early fragment tests. Its immutable
ordinary-pipeline specialization (line 155) removes bootstrap/diagnostic
responsibilities. Material reads use explicit derivatives, and the receiver is
reconstructed from camera high/low plus the final surface-relative position
(lines 260–278). The remaining ordinary work cannot be dismissed as the old
diagnostic or analytic-shell path.

## TES and visibility: protect the actual contract

The prepared-render branch in `production_spherical_billboard.tese:74–82`
interpolates the published relief and supplies zero extra local displacement.
It explicitly says authored TES height maps do not yet exist. Hardware
tessellation and `production_spherical_billboard.tesc:21–44` still preserve
screen-sized shared-edge factors up to 64. Current implementation and future
capacity must be described separately.

The historical M13 exit assessment gives inland 27,756 TES invocations and
9.73524 ms total versus active refinement about 2,779,255 invocations and
5.96372 ms total. Fresh initial draw medians are inland 6.78580 ms versus active
3.41180 ms. These facts reject invocation count as a sufficient explanation for
the positive-land excess. They do not time TES alone or prove all additional
tessellation is free.

Native `RecordProductionBillboardWork` at `NovaCoreNative.cpp:1913` selects
`production_nested_scale_mesh_cull.comp` for NCSM1. The generic spherical cull
is not the relevant production geometry predicate. The NCSM1 cull checks
indices, prepared positions/normals, horizon support, current camera distance,
view cone and frustum support before writing visibility. Compaction consumes
that frame's list and emits current draw indices. Frozen physical vertices do
not imply frozen camera visibility.

Two bounded source leads were reported, without measured avoidable payoff:

1. `production_nested_scale_mesh_cull.comp:124` evaluates a full point-to-triangle
   distance before selecting `screenTesSupport`. If `tesEnvelope` were exactly
   zero and directional telemetry disabled, that result would not affect any
   output. However, the generator still derives the envelope from
   `ComposedBounds().NearHeight` (`PlanetaryNestedScaleMeshTopologyGenerator.cs:408`),
   the adapter forwards it and native submission copies it to counter 9. No
   zero-envelope override was found. Do not confuse zero actual TES displacement
   with a proven zero live cull envelope. Changing that metadata could change
   rejected primitives and needs separate proof.
2. With a positive envelope, the existing enclosing center/radius could cheaply
   prove some triangles are farther than the 50 m support radius and avoid the
   full distance calculation for those triangles. A valid implementation needs
   a conservative outward-rounded bound against the exact squared threshold
   `2500.000001`, fallback on every uncertain/near case, unchanged diagnostic
   counts and exact visibility/primitive parity. No ad hoc epsilon, sphere-center
   approximation or prior unrelated traversal substitutes for that proof.

An ordinary zero-envelope control would require
`NOVACORE_PERFORMANCE_COUNTERS_OFF=1` for both A and B in the existing private
host. The default fixed route enables directional telemetry at sentinel −1006,
which consumes `tesActive` in counters 21/22 and therefore legitimately retains
the distance evaluation. Counter-off timing and diagnostic-on correctness serve
different purposes. The mathematical distance lead remains contingent on fresh
cost/payoff; it is not a selected milestone.

## Single coherent placement opportunity

The initial `local-florida.json` control changes allocation preference for six
explicit GPU-hot labels only: physical preparation output, pupil scratch,
visibility, compacted indices, immutable lattice and immutable indices.
It introduces no rational map, prepared-value reuse or mapped-index validator.
The physical algorithms, staging cadence and publication boundary remain the
baseline paths. Current/incoming/scratch and resident topology share one reason
for the preference: GPU producers/consumers repeatedly use their contents while
CPU access is infrequent or bounded by the existing fence.

Initial Florida total median is 8.88184 ms versus 11.06856 ms; cull/compact
interval is 1.12084 versus 2.66440 ms. These first runs justify repeat controls,
including reverse order, exact matched workload and separate CPU timing. The
2.18672 ms total difference must not yet be called exact recovery: draw and
outside intervals also differ and clocks/order were not independently controlled
in this worker's inspection. The 1.54356 ms cull/compact difference is likewise
a measured interval difference, not a guaranteed saving on every route/GPU.

A production-safe contract, if the lead proves the payoff, is a preference for a
compatible `DEVICE_LOCAL|HOST_VISIBLE|HOST_COHERENT` memory type while retaining
the existing coherent host-visible fallback. Preserve persistent mapping,
allocation capacity reuse, complete physical/readiness predicates, descriptor
rebinding after fences, current-owner independence, resize and teardown.
Do not move counters, frame controls, regional catalogs or material staging
merely because they are host-visible. The prototype's strict required memory
type is a diagnostic capability test, not a portable production fallback policy.

Qualification must cover devices without a compatible local mapped type,
allocation failure/budget policy, retained topology plus current/incoming/scratch
memory pressure, cold CPU uploads, ongoing mapped writes, test/readback consumers,
warm GPU performance, publication transitions and exact physical/raster results.
Host reads of device-local mapped buffers may cost more; prior private CPU map
and index scans are absent from production but permanent test/tool consumers
still need appropriate verification. A lower GPU timer that introduces a normal
CPU stall fails the net-benefit gate.

## Ranking and continuation limit

| Lead | Proven avoidable portion now | Potential category | Risk / next proof |
|---|---|---|---|
| GPU-hot terrain working-set memory preference | Initial isolated signal; repeated net recovery not yet established by this worker | Meaningful if >=1.5 ms repeats | Bounded allocation preference, fallback, memory budget, CPU upload/readback and exact outputs |
| Positive material evaluation replacement | No exact current-output replacement demonstrated | High sensitivity; recoverable portion unknown | Changing the field is quality reduction; broad caching/precision redesign is speculative |
| Conservative rejection before near-support distance | Mathematical opportunity identified; no timed candidate | Unknown, bounded above by current cull time | Rounding, thresholds, diagnostic semantics, whole-frame payoff and exact primitive proof |
| Persistent triangle bounds / visibility reuse | Repetition identified, savings not measured | Unknown | New buffer/lifetime and full invalidation contract; do not grow this into a second speculative project |
| Regional scheduling/reuse combinations | Earlier private partial wins, no current single owner fix | Retained rejected evidence | Do not repeat without fresh dominant causal evidence |
| CPU material request work | ~2 ms existing shared CPU scope; avoidable fraction unknown | Unknown CPU payoff | Not the GPU material field, not a justified combination with placement |

The new ticket makes >=1.5 ms safe, causal, net payoff mandatory and getting a
normal failure below 8.33 ms preferable. It does not require inventing another
milestone when no candidate meets that bar. Florida remaining above 8.33 ms
after an initial placement control neither automatically rejects a meaningful
winner nor certifies success. The final decision requires the lead's repeated
whole-frame and correctness evidence. This review does not promote a production
candidate or claim M13 closure by itself.
