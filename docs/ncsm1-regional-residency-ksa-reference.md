# NCSM1 regional physical residency: KSA responsibility audit

> Historical investigation/slice record. Florida manual acceptance subsequently PASSED
> in the 2026-09-05 consolidation directive. Original measurements, rejected candidates
> and pending/blocked statuses below are preserved as chronology, not current status.
> Current architecture: [production consolidation](production-consolidation.md).

Status: reference inspection completed 2026-09-04, before implementing the
regional-residency correction. This is architectural evidence, not a corrected
Florida run or manual acceptance. The unbanked convergence candidate and the
historical [physical/rendered investigation](florida-physical-rendered-agreement.md)
remain unchanged by this audit.

## Evidence boundary

Inspected the installed `E:\Kitten Space Agency` tree read-only. It contains
shipped GLSL/XML and production managed assemblies, rather than the original
host-language repository. Host-side findings below come from fresh decompilation
of those assemblies; original comments, complete original naming and original
source line numbers are not available. No KSA executable was launched and no KSA
performance or visual acceptance was measured.

Assembly SHA-256 identities:

- `KSA.dll`: `A8A5164204EED962DF9D9025E99F523D9A23EE2CE6E802B18216A9DA35506D2F`
- `Planet.Render.Core.dll`: `6B9B3C3BD709BF198C6AB3EC6DAAEF7E468F593FB110187869A1D44272B92E3C`
- `Planet.Core.dll`: `2823B299051B91252453ACB4F1FFF5543B1F5473BC6F42C8D51D4958D3159ECE`

Local ignored evidence is under `build/ksa-residency-reference/`: `source/`
contains targeted type inspections; `assembly-source/` supports cross-reference
searches across the current KSA assembly. These files must not be committed,
redistributed, copied into implementation, or used as a runtime dependency.
This document records responsibilities and observations only.

Reproducible source locations in that local inspection (line numbers are from
the decompiled files, not original KSA source):

| Evidence file | Relevant members/lines |
| --- | --- |
| `source/KSA.Rendering.CelestialTextureStreamer.cs` | body/map registration 234; demand 415-508; budget/tier selection 522-666; height view fallback 319; publication/reset 796-884 |
| `source/KSA.Rendering.StreamingUploader.cs` | queue/worker 94-235; completion polling 113-148; copy/acquire submission 240-357 |
| `source/KSA.Rendering.TextureMipStreamSource.cs` | loaded CPU texture source and staging, 9-26 |
| `source/KSA.PlanetRenderer.cs` | preparation descriptors/dispatch/barriers 1770-1937; per-frame streamer tick 2143-2148 |
| `source/KSA.Program.cs` | asset loading/binding 940-976; post-load fence wait 1271-1290; uploader polling 2238-2239; normal terrain preparation call 4541 |
| `source/KSA.DecalModifierReference.cs` | asset registration 154-170; localized CPU physical evaluation 184-225 |
| `source/KSA.ProceduralModifiersRenderData.cs` | body modifier texture setup 162-214 |
| `assembly-source/KSA/GroundClutterRenderer.cs` | body/content-version dependent resnap 1086-1098 |
| `assembly-source/KSA/ClutterEcotypePhysicalData.cs` | physical clutter version invalidation 714-724 |
| `source/KSA.Rendering.FreeImagePool.cs`, `source/KSA.Rendering.RetiredResourceQueue.cs` | allocation reuse keys, deferred release/reuse and inactive-frame/fence checks |

Shipped source cross-checks under `E:\Kitten Space Agency\Content\Core`:
`Astronomicals.xml` launch-site modifiers around line 733;
`Shaders/Planet/TerrainMesh/PrepareModifiers.comp` height sampling around line 134;
`Shaders/Planet/ProceduralModifiersLibrary/Modifiers.glsl` localized decal
application around line 481. No source excerpts are incorporated here.

## What is equivalent

KSA has two relevant responsibilities, with different acquisition policies.

1. **Localized physical terrain inputs exist.** `DecalModifierReference` owns a
   geographic location, extent, height texture and composition parameters.
   `Astronomicals.xml` contains active Earth launch-site decal definitions,
   including a Cape Canaveral site. The CPU evaluator and the production modifier
   shader both restrict application by body-fixed location and local footprint.
   These are physical height modifications, not just fragment materials.
2. **Runtime height residency exists.** `CelestialTextureStreamer` owns separate
   height, diffuse and normal map states for each celestial body. The height
   input consumed by `PlanetRenderer.GenerateMeshData` comes through that
   streamer. Neither its demand generation nor its uploader requires
   `TerrainPatch` collision geometry.

The important limit: the inspected KSA implementation does **not** provide an
equivalent on-demand hierarchy of geographic regional residual records with a
complete-input gate for an immutable incoming mesh generation. Its geographic
decals are statically loaded assets; its dynamic height streaming unit is a
whole-body cubemap mip range. This conclusion follows the actual consumers and
asset paths, not merely a search for a class name.

## Localized data acquisition and preparation

`DecalModifierReference.OnDataLoad` registers its height texture through the
ordinary `TextureReference` asset machinery. `TextureReference.DoLoad` loads the
CPU texture asset and registers a binder. `ModLibrary.Bind` runs those binders
during startup; `TextureReference.Bind` creates and stages GPU textures.
`ProceduralModifiersRenderData.SetupCelestialTerrainMods` resolves body-owned
modifier texture references into a fixed descriptor array before normal terrain
rendering. There is no geographic load/unload request emitted by the decal
evaluator: location determines evaluation coverage, not asset residency.

The startup pipeline completes asset binding and waits for registered post-load
GPU work before entering normal play. Invalid/missing modifier inputs can use
KSA's asset fallback/validation behavior; this is not evidence of NovaCore's
stronger fail-closed physical-completeness contract.

The local physical responsibility is compatible with NovaCore at the ownership
boundary: a body owns physical geographic inputs and preparation consumes them.
KSA's decal composition, launch-site shaping, coordinates, units, precision and
constants are not migration instructions. NovaCore's pad and canonical terrain
remain unchanged.

## Dynamic streaming ownership and cadence

`PlanetRenderer.OnFrame` ticks `CelestialTextureStreamer`. For each body's maps,
the streamer computes needed texel density from apparent body size, camera
distance, viewport height and quality settings. Near a body's surface it requests
the finest allowed mip; an exporter can explicitly request full residency.
Requests cover complete cubemap mip ranges across faces, not the NCSM1 pupil or
individual physical sample directions. It can also warm other maps while budget
is available. Focus is not a strict no-demand boundary.

`TextureMipStreamSource` stages from the already loaded CPU texture asset.
`StreamingUploader` owns a priority queue and a background worker, builds upload
batches, records transfers and handles transfer-to-graphics queue acquisition.
This is GPU mip streaming; it does not establish an asynchronous regional
disk-record reader equivalent to NovaCore's pack I/O.

`StreamingUploader.Poll` exposes completion callbacks only after its gate pool
reports GPU completion. With a dedicated transfer queue, that gate covers the
graphics acquisition submission as well. `OnPromotionCompleted` then publishes
an image view restricted to resident mips and advances the map's content version.
Unuploaded mips do not become shader-visible through that published view.

## Readiness, reuse and invalidation

KSA keeps a coarse base texture available. `GetHeightView` returns the published
streamed view when available, otherwise the coarse body texture. Promotion
failure or budget-driven reallocation may reset to coarse. This protects against
sampling unuploaded memory; it does **not** prevent geometry prepared with less
than the full-resolution CPU height input from rendering.

`Program` calls `GenerateMeshData` in the normal per-frame rendering path.
That method binds the current height view, prepares base height/gradients,
evaluates modifiers, finalizes positions and normals, then applies compute and
draw barriers. The inspected terrain path regenerates frame-indexed render data;
it does not demonstrate NovaCore's retained current/incoming immutable generation
publication protocol.

Height content versions are advanced on promotion and reset. Cross-references
show `GroundClutterRenderer` and `ClutterEcotypePhysicalData` consuming them to
resnap or invalidate generated ground clutter over frame-resource rotation.
This is concrete evidence that cached physical derivatives must follow input
identity changes. It is not a proof of deterministic dataset-hash identity or an
atomic terrain replacement gate.

Published views/handles are retired through `RetiredResourceQueue`; images are
returned to `FreeImagePool` after retirement. That pool keys reusable allocations
by format, extent and mip count. Allocation reuse is distinct from valid content:
renting an image does not make its previous body's texels authoritative.

## Boundedness and its limits

The inspected streamer limits concurrent map streams and mip promotion steps.
It uses graphics-memory budgets and category priority, degrading non-height maps
before height where possible. The uploader has configured batch and in-flight
byte thresholds, worker backpressure and counters for upload bytes, batches and
stall time. This removes routine uploads from the frame thread.

These are observed controls, not a measured hard total-memory bound: the worker
can append a request that crosses a batch threshold; active-stream accounting
does not include every idle pooled image; the inspected image pool has no explicit
idle-byte eviction cap. KSA can degrade height under pressure. NovaCore must
measure its own record, queue, pinned-set and allocation bounds, and keep complete
physical data mandatory rather than importing those fallback semantics.

## Direct migration decision

| Responsibility | Decision | NovaCore consequence |
| --- | --- | --- |
| Body-owned geographic physical inputs, independent of collision/legacy mesh | Adopt responsibility | NCSM1 asks a physical-data service; old anchored submissions are unnecessary. |
| Separate map/input categories | Adopt responsibility | Regional residual readiness is separate from material residency. |
| Background acquisition, bounded uploads, completion before shader exposure | Adapt responsibility | Reuse generic pack integrity/I/O where suitable; expose required residuals only when GPU-visible. |
| Content-version invalidation of derived physical data | Adapt responsibility | Include body, terrain, dataset, record and physical-generation identity in NCSM1 preparation/reuse. |
| Recycle allocations after consumers retire | Adopt responsibility | Reuse storage without confusing storage identity with valid physical content. |
| Whole-body mip demand and eager local decal textures | Intentionally differ | Derive regional records from actual NCSM1 preparation coverage, including height samples for normals. Measure before choosing the residency capacity. |
| Coarse height fallback and later resnap | Intentionally differ | Unresolved required regional data blocks preparation/publication; the complete outgoing owner remains authoritative. |
| Per-frame full terrain preparation | Intentionally differ | Preserve accepted NCSM1 pupil reuse and P2S5G architecture, with complete physical input identity. |

The differences have measured NovaCore grounds: the existing live path omitted
the regional contribution and placed the center rendered surface approximately
14.109 m below canonical generation-4 height. A fallback that drops authoritative
regional inputs repeats that failure. The pack contains 859 records across four
levels, with approximately 119.74 MB of decoded residual payload and 210.31 MB of
total file data. Unconditional eager loading is therefore not justified by the
small statically bound decal case. Exact preparation-footprint demand and capacity
measurements remain required; these inventory sizes alone do not select a cache.

The resulting contract is `declare physical dependencies -> acquire -> make
GPU-visible -> prepare complete incoming geometry -> fence-complete atomic
publication -> reuse under matching identity`. KSA supplies compatible ownership,
upload and invalidation principles. NovaCore supplies the stronger physical
completeness and one-owner guarantees required by the established measurements.

No production correction or corrected parity result is claimed by this audit.
All migration telemetry, live GPU parity, 3x3 footprint, LOD/pupil/re-entry,
Solar isolation, automated regressions and manual acceptance gates remain open.

## NovaCore sample-set measurement after the reference inspection

The ignored `build/regional-residency-migration/demand/` probe reads the accepted
NCSM1 artifacts and previously captured actual pupil frames, reconstructs every
preparation direction, and selects the finest present regional catalog record
using the canonical CPU relaxed-cube inverse. It also includes the four base
normal height samples at the unchanged 9,774 m sampling radius. It reads catalog
metadata to count dependencies; it is not a corrected GPU parity test.

| Captured frame | NCSM1 level | Vertices | Vertex-height records | With normal samples | Decoded residual bytes |
| --- | ---: | ---: | ---: | ---: | ---: |
| 2 | 17 | 712,106 | 670 | 670 | 93,392,640 |
| 60 | 17 | 712,106 | 670 | 670 | 93,392,640 |
| 175 | 12 | 498,778 | 670 | 670 | 93,392,640 |
| 300 | 17 | 712,106 | 670 | 670 | 93,392,640 |
| 440 | 17 | 712,106 | 670 | 670 | 93,392,640 |

These observations justify neither loading all 859 hierarchy records nor
restricting residency to the old 256-layer material pool. They support declaring
and reusing the actual physical dependency set. Normal samples do not add unique
records in these particular whole-planet captures; they remain part of the
completeness contract for other pupils and levels.

This deliberately straightforward offline enumeration took approximately
6.8-9.9 seconds per captured state in the Debug probe. It is not suitable for
per-frame production demand. A production solution must avoid repeating that
work on the frame thread and demonstrate its own cadence/reuse and bounds.
No runtime demand algorithm or cache capacity is accepted merely by this count.
