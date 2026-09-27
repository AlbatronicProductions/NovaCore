# Shared design boundary

Schema novacore.vehicle-design/1 retains design ID/revision, used-dependency digest, explicit ordered instance IDs/definition revisions/digests/poses, root and independent selected control part, connections, service links, initial configuration, symmetry and action metadata. The dependency digest pins exact used part references and referenced resource definitions, permitting unrelated catalog growth. Canonical save/load is identical for stock and editor-produced designs. A command-capable part does not become the root by implication.

Initial quantities/charge are authored configuration. Evolving runtime quantities will live separately under simulation transaction ownership; they never rewrite definitions or this initial configuration. Current files do not yet establish runtime publication (Stage6).

Offline source template novacore.construction-authoring-template/1 contains AUTHOR placeholders. Only the explicit --author-construction-design command resolves them and emits a validated canonical design. Normal Load refuses this schema, wrong dependency digest, unknown revision or altered part digest. There is no DLV repair-on-load path.

The Python content importer uses engineering poses and constituent-owned definitions. It may know the development article; the generic compiler never selects behavior by its name or part counts. Current development-design.json is8153 bytes, distinct from canonical runtime dynamic snapshots.
