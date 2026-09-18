# Current KSA convergence — finite Florida support ownership

Inspected directly during this correction, before implementation. This is internal architecture evidence; no KSA asset or source is shipped.

## Current production directory

Root: E:\Kitten Space Agency. KSA.dll: 4,936,792 bytes; file version 2026.9.10.5438; product 2026.9.10.5438+c9291b5dd3347e847020c8fb3dd11dfce6a728e9.
SHA-256: A03E98153C6F353AF6F280CD3BDC1C71C718008E2049F508DC6FBE54457A0AA8. Rechecked unchanged after implementation.

Directly decompiled current method bodies:

| Responsibility | Direct source evidence | Disposition |
|---|---|---|
| Authored presentation versus explicit colliders | StaticObjectTemplate Models/Colliders/subobjects; StaticObject.BuildCollisionShape 06002854; AddColliders 06002855; BoxColliderTemplate.CreateShapeInto 060013A5 | ADOPT distinct physical ownership; ADAPT one analytic NovaCore box |
| Site/frame placement | ConstraintSim.ResolveLaunchPads 06000691; UpdateStaticObjectCollider 06000692; LocationReference.GetAxesCcf 0600034F; UpdateStaticObjectRenderData 06000350; StaticObject.UpdateRenderData 06002851 | ADOPT common body-fixed pose for presentation and contact; ADAPT authenticated NovaCore site |
| Retention and removal | Static.ResolveAll 0600284E, Dispose 06002850; RemoveStaticObject 06000693; RemoveVehicle 06000687; UpdateSim 0600068A; TryResetPool 06000683; ReturnConstraintSim 06001C25 | ADOPT retained ownership; preserve existing NovaCore generation/frontier invalidation |
| Ground and facility coexistence | PhysicsStates.GetDesiredBubFrame 06001B67; UpdateTerrainForNextStep 0600068C; BepuHandles.IsGroundSurface 0600066D recognizes terrain OR facility | ADAPT facility/slab contact owner; terrain authenticates grading, does not substitute for slab contact |
| Spawn metadata | Vehicle.GetLaunchPadHeight 06002F83 and spawn 06002F82 combine GroundOffset/SurfaceHeight and body rotation | INTENTIONALLY DIFFER: do not treat scalar spawn metadata as a collider or copy its heuristic; exact authored corner/material-origin placement |

Current launchpad configuration separates authored visible models and box collider entries. Collider creation composes authored transforms into retained shapes. Static world updates derive pose from body-fixed site axes and ground offset relative to the bubble; rendering derives pose from the same site axes. No evidence implies arbitrary visible mesh triangles automatically supply contact.

Inspected dependency identities:
BepuPhysics 77185E513BD530DEB322E41DD4ACDA9AD8E32E626CFA19492FB7481F25ED1FA7;
BepuUtilities E0A1528DED4EF8EFCB8002B99704CB8614B7DB840D2CB39B5E45EDA52D63BC68.
Configuration files under E:\Kitten Space Agency\Content\Core:
CoreLaunchPadAAssets.xml 9A4A8F01858F1C98C1AE6E4C575481CAB308D1334165B5717A1637C27D16BA0D;
CoreLaunchPadAGameData.xml EBCF9277B08FB565878D03240123E8E9AC6705E11F41F3CA78AF4D8303F3C3BE;
Astronomicals.xml EE086709E91E29C3712847A6657DE2AE0CCA628726D0F662227AC6C365F0FE0F.

## Direct live engineering history

Authenticated Discord browser, channel [live-changelog](https://discord.com/channels/1260011486735241329/1260112103134724146), searched/read directly for launchpad, launch pad and static objects. Not inferred from NovaCore reports or public web descriptions.

- [Build 5091, July 30](https://discord.com/channels/1260011486735241329/1260112103134724146/1532237253848465569): placeholder pad, spawn height and vehicle-pad collision; pad resting is ground contact; clutter excluded.
- [5095, July 30](https://discord.com/channels/1260011486735241329/1260112103134724146/1532286038184165589): pad presentation and celestial rotation correction.
- [5330, August 19](https://discord.com/channels/1260011486735241329/1260112103134724146/1539843520976785461): static-object/render pipeline and first real pad replace placeholder.
- [5338, August 20](https://discord.com/channels/1260011486735241329/1260112103134724146/1540029182623481867): flattened decals, no curvature and excess mound removed.
- [5366, August 26](https://discord.com/channels/1260011486735241329/1260112103134724146/1542305493136056443): CPU terrain/MeanRadius fixes spawning through terrain independently of render data; fallback discussed.
- [5382, September 1](https://discord.com/channels/1260011486735241329/1260112103134724146/1544200205769900113): resolve pad poses once per pass rather than per vehicle microstep.
- [5398, September 1](https://discord.com/channels/1260011486735241329/1260112103134724146/1544550397111177258): static subobject shader stride issue.

History describes engineering intent and evolution; current installed 5438 method bodies establish implementation. The history also contains newer 5450 entries; they are not misrepresented as installed 5438 source.

## NovaCore boundary

Reuse the existing ordinary retained world and one finite static. The existing NovaCore authored base and footing form one contiguous rectangular volume with a common footprint, axes and material authority. Share that declaration between the contact source and rendering. Preserve the original episode, exact clock/resource/revision ownership, publication and copied observation. Do not introduce KSA vehicle pooling, infinite ground fallback, assets, collider dimensions or spawn constants.
