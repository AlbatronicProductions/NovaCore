# Planetary rendering

NovaCore publishes immutable, root-resolved `PlanetaryPresentationSnapshot` data to Graphics. Graphics never owns a body's position, radius, hierarchy, ephemeris, or time.

One permanent sphere cannot scale from solar-system views to a launch site. Far-field rendering therefore uses a shared whole-body proxy with the physical radius applied only in presentation. Near-field rendering uses a normalized cube-sphere: six stable faces are addressed by `(face, level, x, y)`, recursively partitioned in child order 0–3. Cube coordinates are normalized to the unit sphere, avoiding polar singularities, then scaled by the presentation body's physical radius.

The current CPU policy selects near field when camera altitude divided by radius is at or below a configurable presentation threshold. It deterministically emits six root patches and recursively emits all child patches to a capped level as the camera approaches. This policy is not simulation identity and is a seam for a future compute-driven selector.

Astronomical body positions are never converted directly to FP32 model translations. The body center is first subtracted from the root-resolved camera position in doubles; patches are then expressed around this camera-relative center. Future layers can add terrain displacement, atmosphere, clouds, oceans, and GPU LOD without changing snapshot or patch identity contracts.

10A-2A-1 adds one shared deterministic 16x16 indexed `[0,1]²` grid and a 48-byte fixed-width native patch record. Face values exactly match `CubeSphereFace`; native validation borrows no caller memory.

10A-2A-2 gives the native Vulkan renderer one renderer-owned vertex/index allocation for that shared grid and a host-visible patch-record buffer. One instanced indexed draw maps each submitted record onto its cube face in the vertex shader, normalizes the cube position to the unit sphere, applies the body-local radius, and finally adds the camera-relative body center. The diagnostic scene submits the six level-zero roots with distinct solid colors. Back-face culling makes the debug body opaque without changing the existing triangle pipeline; depth, terrain, textures, lighting, atmosphere, and adaptive patch selection remain deferred.
