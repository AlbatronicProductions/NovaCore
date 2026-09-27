# Stage B — measured GPU owner; no correction promoted

Entry gate passed: Stage A recurring footprint work is bounded below budget,
while the same native witness has 357/377 over-budget GPU frames and complete
sample coverage. Even with zero native contact slices, 82/102 GPU samples exceed
6.6667 ms. A contact-only correction cannot close this gap.

Current production source selects one detailed published terrain owner and one
indirect draw. During held contact, detailed draw is approximately 8.07 ms; a
sample at submitted frame567 contains 1,424,208 input triangles, 2,611 selected
patches, 324,564 refined vertices and 4,484,449 fragment invocations. This identifies
the draw as dominant but does not isolate one shader expression or prove any of
the contributing work disposable. Tessellation invocation count alone is not time.

The retained M13 analyses were checked before proposing work:
[whole-frame Work B](../m13-final-whole-frame-causality/work-b.md),
[final-exit Work B](../m13-final-exit/work-b.md). The current source already has
early fragment tests, immutable ordinary specialization, prepared physical relief,
FP32 presentation biome weights, common biplanar inputs, exact-zero contribution
skipping, shared hash prefixes and role-specific mapped-memory placement.
None is a new correction. Prior changed-noise measurements are sensitivity only:
they changed contributing color/roughness/AO/normal values and cannot establish
safe recoverable milliseconds. No historical workload was rerun.

Current installed KSA 2026.9.22.5482 (same DLL hash as README) was reinspected:

| Owner | Responsibility | Disposition |
|---|---|---|
| PlanetRenderer.GenerateMeshData 0600213B | Prepares positions/modifiers/normals/color before optional compaction | ADOPT preparation before presentation; already present |
| Render 0600213D | Consumes prepared buffers with selected indexed/indirect draw | ADOPT published ownership; already present |
| BuildShaderStages 06002134 | Chooses tessellated stage topology at pipeline construction | ADOPT immutable mode; already present |
| Current Planet.frag | Shared biplanar/derivative data and filtered contributing materials | ADOPT invariants; ADAPT NovaCore FP64 body-fixed field |

No general unchanged-frame mesh-cache early return was found in the inspected
GenerateMeshData. KSA's authored textures do not supply an exact replacement for
NovaCore's analytic material field. KSA files/assets were neither written nor copied.

Authenticated live-changelog was refreshed through revision 5506. The focused
material history search confirmed
[4472](https://discord.com/channels/1260011486735241329/1260112103134724146/1507470527944589412)
material contribution culling/register work;
[4766](https://discord.com/channels/1260011486735241329/1260112103134724146/1521261988888711210)
scale/blending stability;
[5338](https://discord.com/channels/1260011486735241329/1260112103134724146/1540029182623481867)
prepared terrain descriptor ownership for launch-site sampling; and
[5474](https://discord.com/channels/1260011486735241329/1260112103134724146/1551946955490463857)
grass roughness/clutter lighting. The visible newer 5505 biome sampling and 5506
plume-volume caching do not establish a replacement analytic terrain-material
contract. No superseding mechanism was established in this bounded check.

Judgment: REVISE, independently proven terrain-draw owner remains. No new bounded
output-preserving dominant correction was established from this measurement and
source/history evidence. Changing precision, positive detail, filtering, density
or tessellation to obtain a number would violate the active contract.

The subsequent final one-attempt authorization was evaluated in
[final-gpu-payoff.md](final-gpu-payoff.md). It supplied implementation permission
but its strict quantitative payoff gate was not met. No correction or native
verification followed. The discussion below describes possible future policy,
not a request to broaden this final attempt.

Project Control decision if pursuing a different representation: authorize an
explicit material-equivalence study for sampled/cached analytic detail, including
cell identity, interpolation, derivatives, precision, residency and invalidation,
before implementation. Alternatively retain the current representation and define
a bounded diagnostic isolation exposure to establish a narrower exact-preserving
candidate. Neither option has been implemented or GPU-qualified here. This is
not a claim that representation change is necessary, nor proof the current field
cannot meet 150 FPS. Do not silently expand the current side correction into a
material system redesign. Frozen candidate and all failed-contract measurements
return to Project Control; no Stage B native exposure was taken.
