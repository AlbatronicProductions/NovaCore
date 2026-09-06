# Authored facility-to-terrain Sun visibility — unbanked candidate

> Historical investigation/slice record. Florida manual acceptance subsequently PASSED
> in the 2026-09-05 consolidation directive. Original measurements, rejected candidates
> and pending/blocked statuses below are preserved as chronology, not current status.
> Current architecture: [production consolidation](production-consolidation.md).

> Subsequent manual terrain-stability failure is addressed by the bounded
> [physical material receiver migration](terrain-material-surface-convergence.md).
> Facility occlusion, geometry, support and lighting terms remain unchanged.
> The measurements below describe the completed occlusion ticket.


Florida manual acceptance remains **FAIL** until the new launcher candidate is
physically accepted. Earth-route convergence remains **UNBANKED**. This change
addresses the accepted P4 contact-lighting cause; it does not reopen physical
support, placement, TES, material coordinates, residency, or publication ownership.

The continuous pixel-provenance investigation followed 525 consecutive approach,
grazing, return and retreat frames. Supported NCSM1 terrain owned the apparent-gap
pixels, but received direct/specular sunlight through the unchanged facility.
Foundation lighting and material coordinates alone were not sufficient. Those
investigations remain historical evidence, not implementation instructions to
change geometry again.

## Existing-path audit and KSA boundary

No suitable production directional shadow or authored-facility caster system
existed. NCSM1 visibility buffers are camera culling/compaction, ordinary scene
depth resolves camera visibility, and terrain ambient occlusion is material
response. None owns facility-to-Sun visibility. The prior ray-box mask under
`build/florida-pixel-provenance` is isolated diagnostic evidence.

**ADAPT RESPONSIBILITY** from KSA: caster definition, terrain receiver and direct
light visibility are distinct responsibilities. KSA's particular authored static
launch-site mesh participation in its Sun caster pass was not verified. This is
not claimed as direct implementation equivalence. No KSA code/assets/constants
were copied; the completed physical-support investigation was not repeated.

## Authored caster and lifecycle

`AuthoredFacilityGeometry.h` now supplies the exact same four boxes to the existing
launchpad mesh builder and the analytical caster builder. A shared unit foundation
box uses the existing 64×48×6.835569277405739 m scale. Vertex winding, dimensions,
colors and placement are unchanged. Caster indices 0–3 retain authored box order;
index 4 is the foundation. There are five active boxes, capacity eight, and one
active facility definition per submission. This is a bounded static-facility
responsibility, not a dynamic scene traversal or spacecraft shadow system.

The immutable 160-byte input carries body, facility and authored object identity,
geometry set/version, FP64 body-fixed origin and right-handed local basis,
foundation transform scale and finite ray reach. Florida is registered through
its existing site identity. The shader has no latitude/longitude or Florida check.
Another site/body can reuse the same definition; another authored geometry set
requires explicit registration beside its mesh definition.

The scene creates this input when the site is initialized. Native geometry is
validated and cached by the complete definition; identity, transform, scale or
version changes invalidate it. Camera, Sun and pupil changes do not. One persistent
480-byte fragment input buffer carries the cached boxes and the current localized
camera/bounds. Per-frame work uses stack storage and a fixed-size copy. No physical
preparation or residency data is invalidated.

The input pointer occupies the prior reserved eight bytes at frame offset 792;
the submission remains 800 bytes and existing offsets are unchanged. Managed and
native layouts are checked. This deployment uses freshly rebuilt matching
managed/native artifacts; the new field is not advertised as a guarantee for an
arbitrary old client with uninitialized reserved bytes.

## Precision, receiver and Sun authority

The facility origin/basis remain authoritative FP64 body-fixed data. Native code
subtracts the split high/low body camera from that origin and projects the delta
in FP64 before transport. Inside the conservative receiver region, the fragment
localizes its **actual final post-TES camera-relative position** (`-viewDirection`)
in FP64, then converts to facility-scale FP32 for intersections. It does not use
the separate material sampling coordinate and does not evaluate a replacement H.

The existing production body-space Sun direction is transformed into that same
facility frame. It continues to follow simulation time and body orientation; no
fixed light vector or camera-derived light is introduced. The receiver is active
only for the matching body with authoritative NCSM1 ownership.

## Finite influence and intersections

The authored policy is a maximum **2,048 m receiver-to-caster ray length**. This
deliberately bounds the initial near-field facility responsibility at very low
Sun angles and keeps local FP32 coordinate quantization below approximately half
a millimetre for this facility. It is not the 128×112 m grading footprint, a
physical terrain bound, or an unlimited horizon-shadow claim. Shadows beyond
that finite reach are outside this policy; extending it requires explicit review.

A conservative body-relative bound rejects distant fragments before Sun/frame
math. Its radius is the finite reach plus the authored corner extent. Directed
rounding and a derived FP32 arithmetic guard expand only this rejection bound.
Within it, a tighter local AABB is the authored aggregate bound swept opposite
the **current** Sun direction for the finite reach. This is conservative for all
box intersections under the stated policy, including low-angle projections.
Outside it, the caster loop does not execute. Inactive facilities bypass both.

Each box uses a finite slab intersection. Parallel axes test interval membership;
other axes intersect entry/exit intervals. Positive-length overlap blocks the
Sun; a zero-length contact alone does not. There is no inflated caster, embedded
receiver, depth bias or arbitrary dark area. CPU FP64 and GPU FP32 references
are compared, with exactly grazing transformed cases checked against a documented
arithmetic interval rather than silently adding an intersection epsilon.

## Lighting and preserved contracts

`planetary_production.frag` evaluates the original `PlanetLighting` expression.
For clear receivers it returns that exact result. For blocked receivers it keeps
the original ambient and emissive contributions while removing direct diffuse
and direct specular. Material color, roughness, normals, ambient policy, exposure
and tone mapping remain unchanged. Hard shadows are intentional in this bounded
first implementation; no soft-shadow, terrain-self-shadow or atmospheric system
is added.

No physical H, support modifier, GPU preparation, TES, culling, compaction,
indirect draw, current/incoming publication, camera clearance or facility
transform changes through this path. The only shader input extension is a
fragment storage binding; the accepted P2S5G stage interface is unchanged.

## Validation and acceptance

Evidence and the complete current validation report are retained under
`build/facility-light-occlusion`. Permanent coverage includes caster ABI/identity,
unchanged canonical support/placement, finite intersections, clear/blocked
lighting, Sun/camera/orientation cases, invalid definitions and inactive bounds.
Continuous GPU captures retain original and corrected lighting at the same final
fragment, plus actual D32, primitive mapping, physical position and pupil identity.

The 525 native 3440x1440 captures (frames 240-764) preserve every recorded ROI
D32 value bit-for-bit against the pre-correction run. Camera, draw counts,
publication identity and TES factor bounds also match in every frame. All 52
other deployed shader binaries, including geometry/preparation/culling/compaction,
are unchanged; only `planetary_production.frag.spv` changes. The authored box
builder receives byte-identical arguments in the same order.

Visibility agrees with the independent CPU reference for 17,641,184 sampled
terrain receivers (1,911,538 blocked). Clear presentation differs by at most one
8-bit display step. There are no sampled owner changes across 45 L17 pupils.
Compaction primitive ordinals are not stable identifiers: exact-depth/HDR record
resolution selects alternate coplanar triangles in some frames. Recorded relative
position differences are at most 0.3281 mm, with zero D32 difference. This is a
provenance precision limit; complete compacted-index order is not claimed identical.

Independent Vulkan tests cover 102 intersection/lighting cases. One hundred have
exact CPU/GPU classification agreement; two exactly grazing transformed cases
fall within the documented 1.99127 mm FP32 arithmetic boundary interval. No
intersection bias is introduced. A live rebuilt Debug candidate passes regional
residency and physical parity across L8-L17, 43 captures and 40 pupils. P3 retains
its 65,536-vertex slice limit and complete-before-publication gate: 37 publications
and 268 delayed slices preserve the outgoing owner.

Four separate uninstrumented retained-frame runs use 400 frames each, with
904,053 indices, 712,106 vertices, generation 1/pupil 28 and no incoming preparation.
The mean of run medians increases from 50.0903 to 50.9201 ms for the terrain draw
(+0.8298 ms, 1.66%) and 53.5894 to 54.4143 ms for total GPU time. CPU post-callback
submission medians are effectively unchanged (1.9517 versus 1.9506 ms). The terrain
timer includes unchanged TES work; there is no standalone fragment-only timer.
Capture counters measure 8.061 billion caster tests over 2.148 billion terrain
fragments, with 76.23% entering the Sun-aware bound and at most five tests per
fragment. Outside the conservative bounds no caster loop executes; this is not
a claim of literally zero shader branch/register cost. Instrumented capture
timings are not performance evidence.

The fresh candidate passes facility, route, launcher (15), P2S5G interface and
production-renderer regressions. Enabled Vulkan validation reports no new VUID;
the existing external-memory import VUID 00645 remains (two occurrences in the
main live run). The Release runtime and all 53 deployed shaders match the fresh
build. Diagnostic sources are archived, isolated entry directories restored,
and capture launch scripts disabled.

The close shadow is hard and can display as black under the unchanged low
ambient response. Its silhouette follows authored geometry and the Sun; there
is no grading-footprint darkening. Manual assessment of that appearance, arbitrary
orbits/time changes, and the deliberate 2 km finite-reach policy remains required.
The continuous capture exercises L17 pupil changes; broader LOD transitions are
covered by the separate live regression, not claimed from that movie.

Website-side review and normal **Florida Launch Site** launcher acceptance are
required. Do not bank convergence based on numerical or automated rendering tests.
