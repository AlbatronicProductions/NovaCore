# Bounded implementation gate

Banked baseline: `180eaf150ba5db6364e17dd48336690778f058f9`.
Branch: `codex/post-m13.3-next-performance-target`. Unbanked; no milestone assigned.

## Responsibility

Old: evaluate three procedural material-noise bands when their footprint permits
them, including fragments where the complete land-detail contribution is zero.

New: evaluate those same bands only when their existing footprint condition and
the complete land-detail contribution are both positive. Keep the original
equations and derivative execution for every contributing fragment/helper.

Removed work: FP64, derivative-free procedural hash/noise evaluation whose result
is subsequently multiplied by zero. No physical, geometry, tessellation, density,
selection, preparation, publication, residency, facility or lighting change.

Production file: `native/NovaCore.Native/shaders/production_terrain_material.glsl`.
Only the shared fragment-material helper changes. No new flag or dual authority.

## Required outcomes and future fidelity

- Same geographic albedo, material response, shading normal and detail weight.
- Footprint derivatives precede conditional noise; height-normal derivatives stay
  after its reconvergence. No early return or subgroup assumption.
- All positive contribution values retain their original computation, including
  arbitrarily small values; no approximate-zero threshold.
- Prepared physical bytes, oriented submitted triangle multiset, D32, HDR and
  final presentation agree. Parallel compaction does not promise buffer order.
- Preserve hardware TES, shared-edge factors, density, current geometry and the
  capacity for future KSA-class authored displacement/silhouette detail.
- Continuous gameplay H and M13.3 render/query relationship remain unchanged.

## Fourteen conditions before production edits

1. One winner: zero-contribution procedural material noise.
2. Exact-output bounded probes establish avoidability; cheap-fragment/factor-one
   ablations are sensitivities only and are excluded from recoverable savings.
3. Replacement is three strict positive-contribution conditions.
4. No comparable proven competitor: cheap-shading refinement sensitivity is
   0.79044 ms; remaining TES reconstruction is live; preparation is not the
   settled active-specific excess. Material/refinement interaction is measured.
5. Diagnostic-collection-off active total GPU: 17.11309 -> 5.96024 ms,
   11.15285 ms net GPU signal, exceeding 1.5 ms.
6. This is a small shader change; its signal also exceeds 3 ms.
7. Required outputs are listed above, without weakening fidelity.
8. Exact D32/HDR/image proof passes active, inland, synthetic mixed coastal quads,
   grazing and confirmed Florida. Prepared bytes and oriented triangles agree.
9. Two instrumented and clean active comparisons agree on roughly 11 ms saving.
10. ADAPT KSA contribution-aware material evaluation; do not copy ocean hiding.
11. No product decision remains: zero contribution already defines these outputs;
    future tessellation is explicitly retained.
12. Exactly one derivative-free fragment responsibility changes.
13. Root is the sole production writer. All discovery workers are read-only.
14. Independent quality and performance verification follows implementation.

The initial Florida A/B differed by one D32/HDR pixel, with identical final image.
Both subsequent banked A/A captures and a repeat A/B have the candidate's exact
full attachment hash. This proves pre-existing inter-run variation; it does not
prove a specific compaction/raster cause. Preserve that evidence and limitation.

Tests KEEP: existing noise/value preservation, material synthesis, physical
authority, surface interface, facility support, residency, window and routes.
No old expectation is relaxed; no test or fixture retires. The retained paired
GPU reproduction additionally tests real attachments and synthetic mixed masks.
No implementation revise has occurred. Full validation is pending at this gate.
