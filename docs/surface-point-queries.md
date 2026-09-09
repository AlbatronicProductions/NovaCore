# Canonical physical surface-point queries

This is an **unbanked Surface Interaction / Launch Foundation candidate** on
`codex/surface-contact-query`. M13 remains closed; M13.6 remains the latest banked
production milestone. No subsequent milestone number is assigned.

## Responsibility

`IPhysicalSurfacePointQuery` in Core supplies immutable body-fixed FP64 natural
terrain points to future gameplay. It does not implement contact generation,
collision response, a resting state, flight, or a finite pad collider.

The CPU adapter `PlanetaryPhysicalSurfacePointQuery` remains beside the existing
canonical physical-height oracle in Graphics. Application composition acquires it
and supplies the Core interface to a consumer. Simulation gains no Graphics
reference. The query does not initialize Vulkan or use camera, mesh, TES, pupil,
LOD, render normals, GPU residency, or GPU readback. Its physical inputs come from
the same complete generation-4 `H(bodyDirection)` that defines rendered terrain.
Moving the existing physical oracle between assemblies is outside this candidate.

## Acquisition, identity and lifetime

Call `TryAcquire(bodyId, trustedRepositoryRoot, out query)` after normal CPU data
loading. The root is the application's trusted content configuration, not a
security sandbox. The factory reads the fixed production regional manifest; it
does not accept an arbitrary caller digest as production authorization.

Earth (body 6) is supported with terrain source 2/version 5 and physical generation
4. Body 0 is invalid; other nonzero bodies are unsupported by this provider. This
does not change those bodies' existing rendering or celestial support.

Both verified global elevation and the complete regional CPU package must be
published before acquisition succeeds, including for queries outside Florida.
Outside package coverage the canonical regional contribution is zero. An absent
package is not equivalent to an absent sector within a complete verified package.

The regional loader records SHA-256 and byte count from the exact bytes it decoded
in the snapshot that wins publication. Acquisition compares those facts and its
body, version, format, record count and level bounds with the production manifest.
A later successful `TryLoad` call cannot relabel an earlier fixture snapshot.
Global loading already verifies the fixed global digest.

Authority identity contains body, terrain source/version, physical generation,
reference radius, global/regional content digests, natural composition identity,
facility-support definition identity and normal query policy version. A result
also contains its canonical normalized direction. Paths, time, camera and render
generation numbers never enter physical identity.

The existing CPU datasets publish once and never mutate or unload. An acquired
provider retains this lifetime contract and captures generation 4 explicitly;
changes to the ambient renderer generation cannot alter its answers. There is no
per-query loading, allocation, polling or filesystem access. Acquisition can read
the small manifest and allocate. A failed acquisition supplies no live provider;
retry acquisition after publication. Previously returned results never mutate.

Consumers must match the complete authority identity before reusing a result.
An intentional content/composition revision requires a new authority and rejection
of mismatched old results. Future reloadable datasets must implement immutable
snapshot retention before relaxing today's once-only lifetime. The current API
does not promise hot replacement or a stale-result notification service.

## Result and failure semantics

Only `Ready` authorizes use of the returned position, height and normal. Default
results are not ready. Input must be finite and unit length within the existing
SurfaceAnchor squared-length tolerance; it is then normalized in FP64.

| Status | Meaning |
|---|---|
| `RequiredDataNotReady` | Required complete CPU terrain data is not published. |
| `Ready` | Complete verified physical authority and numerically qualified normal. |
| `InvalidInput` | Invalid body zero, content-root argument or direction. |
| `UnsupportedBody` | This provider owns no physical authority for the requested body. |
| `AuthorityUnavailable` | Missing/malformed manifest or nonfinite physical result. |
| `AuthorityMismatch` | Published bytes/header do not match current configured authority. |
| `NormalUnqualified` | The bounded local numerical normal could not be qualified. |

Failures contain no substitute contact point or radial/shading normal. The API is
a point sample, not a closest-point, signed-distance, swept-volume or penetration
query. Canonical facility grading participates in H exactly as before; Florida's
authored pad top and finite foundation solid are separate future collision inputs.

## Qualified normal

The adapter evaluates full H at a bounded FP64 tangent stencil. It derives the
radial-surface normal using `n = normalize(d - R/(R+H) * tangentGradient(H))`.
The factor accounts for the distinction between reference-sphere distance and
displaced radius. A normal map, existing wide prepared normal, or render triangle
does not participate.

Initial spacing is `R * cbrt(FP64 machine epsilon)`, from central-difference
truncation/cancellation scaling. At most 20 halvings compare consecutive central
derivatives, quadratic derivatives from both sides, a 45-degree rotated basis,
and a finer non-dyadic `h/3` stencil. Each agreement must be within one eighth of
the existing `5e-5 rad` physical-normal ceiling. The returned normal is from the
fine stencil; its distance is exposed for independent verification, not as a
contact footprint. The bound is at most 489 full-H evaluations per query.

Qualification is staged: a rejected cardinal comparison advances the same
refinement history without evaluating the rotated/fine stencils; a rejected
rotated comparison skips only the fine stencil. Every acceptance predicate,
sample coordinate and accepted result remains unchanged. Measured representative
regional work falls from 273 to 113 H evaluations; Florida's first accepted
refinement remains 33. This is query-local rejection ordering, not memoization.

This is a fallible numerical qualification, not a mathematical certificate of
differentiability everywhere. Clamps, geographic interpolation knots and polar
ill-conditioning can make a unique resolved normal unavailable. The provider
returns `NormalUnqualified` instead of averaging a detected crease or inventing
support. A future contact system must explicitly handle such a result before it
can claim whole-surface contact coverage. It must not convert failure into radial
Up or continue from an unchecked stale sample.

## Validation and cost boundary

The [candidate evidence](engineering-evidence/surface-point-query/README.md)
records readiness tests, 564 physical probes, independent derivative checks,
Debug/Release validation and fresh exact Florida/inland raster parity.

Warmed Release queries allocate zero bytes. The
[bounded performance qualification](engineering-evidence/surface-point-query/performance-qualification.md)
compares the original and revised production assemblies in balanced fresh
processes. A regional four-leg footprint costs 0.346–0.366 ms for four points,
0.693–0.695 ms for eight, and 1.377–1.395 ms for sixteen (medians). The original
sixteen-point footprint costs 2.926–2.958 ms under those same controlled JIT
conditions. These are CPU costs, not GPU cost added to a rendered frame.
The first query includes JIT cost and is substantially slower. Existing
`TryAcquire` performs manifest work and allocates; acquire once after publication,
not once per point. Only warmed `Query` has the zero-allocation contract.

The measured recovery provides useful room for bounded initial contact sampling.
It does not establish arbitrary multi-craft capacity. Future contact scheduling
must budget frequency, unresolved-normal cost and the rest of simulation against
actual CPU headroom. This candidate adds neither caching nor a contact scheduler.
