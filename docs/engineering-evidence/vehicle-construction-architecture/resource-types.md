# Resource ownership

ResourceTypeData provides logical ID, revision and display name. Part definitions bind stores and mixture components to resource IDs; saved design dependency identity pins the complete used resource records as well as exact part revisions/digests. Unrelated resource/library additions therefore preserve a saved design, while changes to a used definition do not silently reinterpret it.

StoreData owns capacity and a physical datum. ConstructionStoreKey identifies a particular store on a particular design instance. Three instances of one tank definition own three independent inventories. Geometry references own volume/inertia separately from mesh bounds. Initial configuration is design data; evolving quantities belong to immutable snapshots under canonical simulation publication, not the definition library.

Propellant accounting represents mass, not chemical state or pressure simulation. The model supports arbitrary authored species and integer mixture ratios without hard-coded LOX/RP-1/MMH/NTO/helium branches. Current DLV content is development data and static binding does not qualify its flight consumption limits.
