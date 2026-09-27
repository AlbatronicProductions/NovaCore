# Immutable exported content

ConstructionAsset separates logical ID/revision/content SHA256, a relative locator, metres, glTF=(E.Y,-E.Z,-E.X), fixed material origin, required unique named nodes and provenance. A mutable path alone is never identity. BLEND files are not runtime dependencies.

ConstructionAssetLibrary verifies hash and bounded embedded GLB2 envelope at cold session resolution, rejects external URI dependencies, verifies each reference's required-node contract even on cache hits, and retains an immutable byte snapshot. Later path changes cannot replace that snapshot; a fresh resolver rejects changed bytes. This verifies content identity and required nodes, not full renderer admission.

Capacity admission is128MiB per content file and512MiB per session, at most1024 snapshots. Current largest accepted part is17949856 bytes; total seven-part content is30449892 bytes. These are storage bounds, not invented performance budgets. Catalog reading is separately bounded16MB.

Offline import pins current source GLBs at E:/NovaCore-Blender-Visual-StepB/assets/visual/Parts. Sidecar ROOT maps to exported Part.<asset-definition>; other local names map to qualified exported names. Socket nodes are explicitly required. Sidecar reserved helper nodes are not assumed to exist in GLB. Small FP32 frame transport errors are orthonormalized within1e-6; analytical tensor off-diagonal roundoff is symmetrized within1e-8. Neither edits the accepted sources or engineering datums.

