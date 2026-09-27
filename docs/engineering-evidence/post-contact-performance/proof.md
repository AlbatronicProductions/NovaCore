# Cause and bounded correction

Frozen native evidence established zero-contact continuation to approximately
222 m in the site frame, speed-driven16→256 native slices, repeated world/terrain
reconstruction and synchronous service of up to four canonical intervals.

The instrumented CPU baseline replays the **same preserved input bytes**
`8502d85afb64021f5666770525da605e1920848d3af54f6e629a84dd40ee7414`.
It required537 intervals and46,298 private native slices (including failed
refinement trials), four world recreations plus the initial imported world,
five disposals,16 prepared tiles beyond admission, and six mesh replacements.
Terrain footprint/preparation consumed6,109.512 ms; solver calls383.110 ms.
World-rebuild timing566.496 ms includes its retry solve and overlaps those
timers; these values must not be summed. Baseline cold max339.903 ms,76 calls>25 ms.
Raw counters/times are in `build/post-contact-performance/baseline.json`.

The owner is the conservative clearance fallback: outside full-weight grading,
it uses a broad radial terrain bound plus slab corner radius even for a craft
horizontally disjoint from the finite slab. That false risk retains the native
world; increasing speed then multiplies work that is no longer physically needed.

## Certificate

1. Preserve the existing bounds on all-actuator force/torque, angular energy,
   minimum inertia, rotating-frame gravity/Coriolis terms, and COM depletion.
2. Enclose every authored convex-hull vertex at the current endpoint with
   directed-rounding quaternion arithmetic. Their hull lies in the resulting box.
3. For each axis add `v_com * [0,h]` and symmetric expansion
   `a_bound*h²/2 + 2*comRate*h + radius*spinBound*h + 2*contactTolerance`.
   This encloses the **whole interval**, including rotated edges/faces and
   changing COM; it is not an endpoint-height test.
4. Require the expanded box to be strictly disjoint from the complete finite
   slab solid on at least one axis. No infinite pad plane or distant-corner
   radial veto substitutes for slab collision geometry.
5. Interval-transform the entire box into body-fixed coordinates. With a
   positive Up denominator, enclose every projected terrain ray. Inclusive floor
   indexing covers its entire closed footprint, including negative coordinates.
6. The existing source full-H interval expression supplies bounds on
   `Florida.Up·P − referenceRadius` for every point of each4 m tile. Source
   numerical allowance is projected by an outward bound on Up's norm. The
   full-plane branch uses the authored plane; arbitrary valid patch frames
   still project onto **Florida Up**, not their own radial axis.
7. Require the swept body's minimum Up height to exceed the maximum certified
   terrain height. Otherwise retain the prior broad proof or import BEPU before
   advancing the uncertified interval.

The4 mm expansion reserves more than existing terrain approximation, native
conversion and contact tolerances. Independently reconstructed canonical-basis
Gram error is1.0479533368621473e-16; its Earth-scale coordinate residual is below
1.18 nm and is covered by this existing numerical allowance. No threshold was
introduced for altitude, speed, body length or display rate.

The64-entry FIFO cache belongs to the immutable physical profile/site. A miss
computes the **same certificate** before eviction; saturation cannot change
consumer choice. Authority applicability is checked before every use. The1024
tile/local-coordinate boundary is the existing bounded contact domain; failure
uses the original conservative fallback, never an invented clear result.

Native world receipts cannot be reused after free-flight advances their
canonical frontier. The world is retired through the existing owner. Immutable
height certificates remain reusable independently of native buffers/handles.
Resources, mass, contact material, mesh quality, edge filtering, native slice
rules, control admission and the64 Hz publication cadence are unchanged.

## Measurements

`ConstructionWorkProbe` is opt-in and thread-scoped, never a decision input.
It counts intervals, actual attempted native slices, world lifecycle and terrain
preparation; owned timers separate resource, clearance, free-flight, slice
preparation, terrain, solver and publication work. The original baseline rebuild
timer includes retries; the final observer narrows it to world creation alone.
Publication includes retirement. Nested stage/service intervals are not additive.

The first corrected replay's217 endpoints match the baseline exactly for epoch,
sequence, position, velocity, angular velocity, mass, engine/RCS state and contact
counts. At interval217 the new certificate permits FreeFlight; the old route
continues native work to interval537. This is a lifecycle change after positive
separation, not altered contact forces or weakened geometry.

An additional corrected CPU replay continues through the **same537 intervals**
as the baseline. Native slices fall46,298→3,631, with169 terrain-contact endpoints
in both. Epochs, mass, controls and contact counts match throughout. After the
handoff, the existing qualified free-flight integrator replaces unnecessary native
subdivision: the8.390625s endpoint differs by about6.53 mm in position and2.27 mm/s
in velocity. Bit equality across different integrators is not claimed. The
permanent cadence test requires exact equality for the corrected integrator
across different display partitions. Raw timing comparisons include cold work
and process/JIT variation; they do not substitute for native whole-frame results.

`PostContactTiming` only copies already-computed renderer timing values into a
preallocated bounded array. GPU samples retain their original terrain-frame
identity. There are no new Vulkan calls, queries, buffers, waits or submissions;
file output occurs after the bounded run's shutdown wait. Mandatory recorder is
unchanged. Native measurement storage is qualification-only, not a production
performance mitigation.
