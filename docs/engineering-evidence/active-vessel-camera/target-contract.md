> SUPERSEDED after Project Control manual acceptance FAILED at Step 5 HOME. FREE is now DEFERRED; HOME is unbound. This file records the prior candidate, not current acceptance. Use [the correction package](../active-vessel-camera-free-correction/README.md) and [revised manual route](../active-vessel-camera-free-correction/manual-route.md).

# Target contract

`SceneObjectFocusObservation` is an 88-byte value containing canonical spacecraft ID, presentation lifetime generation, environmental body ID, FP64 material origin with root frame, publication revision, physical observation epoch, display epoch and availability status.

The ID comes from `session.Launch.Spacecraft.Id`, not a mesh or scene slot. A monotonically assigned presentation generation is established once when StockAssemblyDevelopmentScene is constructed. It is not serialized physical state and does not change canonical replay or hashes.

Explicit cold binding resolves the environmental body once against the existing ten-body Solar presentation. A live binding cannot silently be replaced, even by a different canonical ID. Refresh is constant-size value validation/copy and never scans a registry. There is one active target slot and no history or growing list.

Prepared, Active and Held observations are available. Failed, disposed, wrong-root, wrong-epoch, wrong-ID, wrong-generation, wrong-environment or regressing-publication observations retire the cached binding. If it was viewed, Solar deterministically focuses its environmental body. Free/celestial views retain their current mode. Delayed valid observations cannot resurrect the retired binding; a new lifetime requires explicit cold binding.

Refocus saves/restores a single three-double vessel view tuple. Initial distance is 24 m with the existing Florida viewing direction when available. Celestial switches retain the prior vessel tuple. Terrain exclusion may displace the actual eye; orbit demand is retained, and the actual eye is aimed back at the material origin after exclusion.
