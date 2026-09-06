# KSA reference: final refinement and compiler responsibility

Inspected 2026-09-06. Installed source fingerprints are in `ksa-source-identities.json`.
No KSA implementation or assets are copied. This is a responsibility comparison,
not a matched KSA GPU benchmark.

Current `PlanetTessEvaluation.tese` interpolates prepared world position and normal,
resolves biome/control/biplanar inputs, and returns the prepared representation
outside the configured near range. Inside that range it independently resolves
material displacement at each final TES sample. There is no shared-edge physical
result cache in this path. `Common/Planet.glsl` distinguishes **four candidate
height samples** (`TOP_K=4`) from **three final material contributors**
(`TOP_K_SAMPLE=3`). The historical material reduction must not be described as
only three height samples in current TES.

`PlanetFrustumCulling.comp` bounds prepared triangles with displacement tolerance,
rejects conservative frustum/planet/ocean occlusion, and compacts indices before
refinement. `TessellationFuncs.glsl` derives edge-consistent, perspective-skew-aware
factors bounded to 1..64. Backface and depth-prepass occlusion extensions remain
TODOs there. KSA does not establish an already-proven solution that discards every
noncontributing refined triangle while preserving NovaCore's exact geometry.

NovaCore already separates prepared base geometry from bounded near-field TES
evaluation and performs conservative visibility before tessellation. It does not
resemble a superseded KSA design that needlessly reevaluates the whole base field
at every refined sample. Its fine field is analytic FP64 height/gradient authority;
KSA's material-driven texture displacement is not an interchangeable physical
oracle. Shared edge-factor agreement is not shared evaluated-vertex storage.

## Relevant official developer history

The register-related entries and June 2025 preparation entry were read live in
the authenticated official changelog during this investigation. The other
conservative-visibility/shared-edge/range findings are the directly linked,
previously verified record retained in `../m13-next-target/ksa-comparison.md`.

| Record | Bottleneck and correction | Relevance |
|---|---|---|
| [2025-06-16](https://discord.com/channels/1260011486735241329/1260112103134724146/1384241537331761153) | Height/normal work separated into prepared buffers; less frequent preparation was a possibility, while it still ran each frame | Prepared/raster boundary already exists in NovaCore; not evidence of a shipped cross-sample cache |
| [2025-08-20](https://discord.com/channels/1260011486735241329/1260112103134724146/1407745827572682962) | Expensive invisible refinement prompted conservative GPU culling with displacement tolerance | Already adopted; a tighter bound needs an exact coverage proof |
| [2025-09-07 / 2323](https://discord.com/channels/1260011486735241329/1260112103134724146/1414342991958511627) | Shared perspective-skew edge heuristics | Protect edge agreement; does not prove physical-result reuse |
| [2025-09-17 / 2385](https://discord.com/channels/1260011486735241329/1260112103134724146/1417891260819308597) | Early rejection when ocean certainly hides terrain; immediate biome accumulation to reduce live temporaries/register pressure | Work usefulness and compiler pressure are distinct, measurable responsibilities |
| [2026-03-09 / 3783](https://discord.com/channels/1260011486735241329/1260112103134724146/1480717570612138015) | Terrain register use reduced from 126 to 113, with a possible small performance improvement | Register changes are evidence, not guaranteed occupancy or milliseconds |
| [2026-05-22 / 4472](https://discord.com/channels/1260011486735241329/1260112103134724146/1507470527944589412) | Fewer final material contributors and reduced register use | Profile compiled ordinary material work; do not truncate NovaCore H or material quality |
| [2026-08-10 / 5245](https://discord.com/channels/1260011486735241329/1260112103134724146/1536393965304156232) | Exhaust used feature specialization permutations instead of per-instance branching | Compiler-boundary analogy only; its reported gain is not terrain evidence or a NovaCore estimate |
| [2026-08-12 / 5276](https://discord.com/channels/1260011486735241329/1260112103134724146/1537087719317053502) | Denser base mesh allowed retuning the old 220 m refinement range to 50 m | NovaCore already uses the accepted 50 m contract; preserve it |

**ADOPT** compile-time specialization of immutable ordinary-pipeline state when
measured output parity proves the responsibility equivalent. **ADAPT** compiler
register/live-temporary lessons to NovaCore's actual AMD ISA and repeated timings.
**INTENTIONALLY DIFFER** on FP64 physical H, authoritative regional readiness,
one-owner publication and exact 50 m geometry. Do not copy KSA material truncation,
arbitrary culling tolerances or cross-patch synchronization into NovaCore.
