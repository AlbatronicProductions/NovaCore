# KSA reference: visibility and final terrain work

Read locally and in the authenticated official Discord on 2026-09-06. This is an
architectural comparison, not a matched KSA/NovaCore benchmark. No KSA source is
copied into this package. The installed assemblies still match the identities
recorded in [the prior source audit](../near-surface-performance/README.md):
KSA.dll `a8a5164204eed962df9d9025e99f523d9a23ee2ce6e802b18216a9da35506d2f`,
Planet.Render.Core.dll `6b9b3c3bd709bf198c6ab3ec6daaef7e468f593fb110187869a1d44272b92e3c`,
Planet.Core.dll `2823b299051b91252453acb4f1fff5543b1f5473bc6f42c8d51d4958d3159ece`.
The corresponding local decompiled source was inspected again, along with the
installed GLSL. `source-identities.json` fingerprints the actual files read.

## Current responsibilities

`PlanetRenderer.GenerateMeshData` runs vertex preparation, modifier dispatches,
finalization and normals with explicit barriers. Per-frame buffers hold prepared
positions/normals and other render inputs; culling consumes those displaced
positions, then compacts indices for an indirect tessellated draw.
`PlanetFrustumCulling.GetMaxTesselationDisplacement` derives its tolerance from
the largest authored ground/slope material displacement scale. Its shader tests
an enclosing triangle sphere against the frustum, planet and, when relevant,
opaque ocean. It does not establish fine-grained rejection inside the tessellator.

`PlanetTessEvaluation.tese` interpolates prepared geometry, returns before fine
displacement outside the configured range, and evaluates material height textures
at final samples inside it. Current shared tessellation uses fractional odd
spacing, bounded shared-edge factors and perspective-aware screen length.
The material-selection machinery and texture-based physical representation are
not NovaCore's analytic generation-4 H contract. No exact cross-TES physical cache
or safe intra-tessellator primitive rejection was found in these responsibilities.
Preparation storage must not be confused with persistent validity: the inspected
render loop still invokes preparation per frame.

## Relevant history, read live

| Date / official record | Engineering change | Effect on this decision |
|---|---|---|
| [2025-06-16](https://discord.com/channels/1260011486735241329/1260112103134724146/1384241537331761153) | Height/normal prepasses separated expensive preparation from draw shaders; discussion considered later reuse, while preparation still ran per frame | NovaCore already separates prepared physical data from bounded final refinement; do not infer a completed final-sample cache |
| [2025-08-20](https://discord.com/channels/1260011486735241329/1260112103134724146/1407745827572682962) | GPU terrain frustum culling followed by a close-camera correctness fix using maximum tessellation displacement | ADAPT: demand an authoritative displaced bound; deleting tolerance because a few poses pass repeats the rejected pattern |
| [2025-09-09 release](https://discord.com/channels/1260011486735241329/1406840455093682258/1414823440275214430) | Shared edge-tessellation heuristic and culling dispatch-limit correction | Already reflected in responsibility boundaries; does not authorize changing NovaCore factors |
| [2026-02-13 release](https://discord.com/channels/1260011486735241329/1406840455093682258/1471799579442675783) | BC4 displacement texture conversion to reduce bandwidth/storage | INTENTIONALLY DIFFER: not an equivalent optimization for NovaCore's exact analytic near field |
| [2026-05-22](https://discord.com/channels/1260011486735241329/1260112103134724146/1507470527944589412), [release record](https://discord.com/channels/1260011486735241329/1406840455093682258/1509824024811798588) | Top-material selection, register reduction, and correction of TES/fragment blending disagreement | ADAPT pressure reduction only with parity. M13.2 already removed unnecessary ordinary shading responsibility; material pruning is not a new TES authority proof |
| [2026-08-20 dev update](https://discord.com/channels/1260011486735241329/1296653251902443551/1540130329652887642) | Reported terrain precision improvement from roughly 0.4 m to micrometres | Preserve NovaCore precision; approximate cached samples are not justified by performance |
| [2026-08-22 release](https://discord.com/channels/1260011486735241329/1406840455093682258/1540893451112026197) | Reduced old 220 m refinement range to 50 m after changes to terrain detail | NovaCore already uses 50 m. This is a superseded KSA tuning pattern NovaCore does not currently repeat |

Searches included live-changelog terrain culling, terrain reuse, terrain prepass;
version-history terrain and tessellation; dev-updates terrain and frustum. The
reuse/prepass conjunction searches returned no results; the June preparation
record was read directly. Dev-updates frustum returned unrelated shadow/lighting
work and was excluded. These scoped searches are not an exhaustive claim about
all KSA development history.

For A, adopt the ownership boundary, adapt a bound only after proving containment;
KSA supplies no measured tighter bound for NovaCore's analytic modifiers. For B,
adopt preparation/final-sample separation already present, intentionally differ on
physical representation and deterministic publication. Neither current source nor
history establishes a production-ready sample cache or a safe zero-envelope test.
