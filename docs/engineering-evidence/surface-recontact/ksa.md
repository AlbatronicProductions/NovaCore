# Current KSA contact responsibility

Installed reference: `E:\Kitten Space Agency`, version `2026.9.22.5482`.
Read-only inspection; no KSA source/assets copied into NovaCore.

| Reference | SHA-256 |
|---|---|
| KSA.dll | CE43D022E7DAC9BEB2352B6106164BC1F521DDA1AE9D0B898B8DBA0C59A69723 |
| BepuPhysics.dll | 77185E513BD530DEB322E41DD4ACDA9AD8E32E626CFA19492FB7481F25ED1FA7 |

The installed `ConfigureContactManifold` (method token `060006EF`) and
`ResolveTerrainPairFriction` (`060006EE`) establish ordinary craft contact:
Coulomb friction 0.95, maximum penetration recovery speed 1.5 m/s, 30 Hz spring
with damping ratio 1. `PairMaterialProperties` has friction, recovery and spring
fields; no separate restitution coefficient or elastic `-e*v` target was found.
The recovery speed bounds penetration correction, not a requested bounce speed.

The installed child callback (`060006F0`) calls
`TryRejectBackfaceTerrainContact` (`060006F2`) and
`RecoveryPushViolatesAxis` (`060006F1`). For registered terrain only, it rejects
the entire child manifold when the A/B-corrected recovery direction has dot
product below 0.1 with the patch-up axis. Patch up comes from the terrain pose,
not a per-contact render normal. **ADOPT:** filter before native mesh reduction;
retain ordinary object/pad side contacts. **ADAPT:** NovaCore's acquired physical
Florida patch frame uses local +Y instead of the installed terrain's +Z.

The patch axis was traced through `UpdateAnchor` (`06000673`),
`BubbleTerrainGrid.EnsureBasis` / `Rebase` (`06002CC7` / `06002CC8`),
`GetBasisDirectionCcf` (`06002CD8`), and terrain patch initialization / pose
(`06000677` / `06000675`). It is the planetary radial axis of the retained,
quantized patch basis, not the local height-field slope. Height moves the origin
radially; it does not tilt the contact-filter axis. NovaCore therefore keeps its
authoritative local radial axis and does not tune the filter to a failing slope.

This filter closes the retained raw child-12/triangle-12728 witness: depth
0.0066400096 m and raw normal (0.9999999, -0.00044130764, -0.00013960742).
Parent feature 834145280 retained that depth after mesh smoothing changed its
normal to (0.018714862, 0.99982446, 0.000918608). The horizontal edge overlap was
therefore being treated as near-vertical spring compression. Concise raw lineage
is retained in `build/surface-recontact/terrain-match.log` pending final seal.

**ADOPT:** ordinary contact material/solver response, real aggregate collider,
mass/inertia-driven sliding, rocking, tipping, tumbling and recontact; collision
response remains separate from damage/destruction accounting.

**ADAPT:** deterministic NovaCore 64 Hz transaction clock, private native slices,
FP64 rotating-Earth canonical state, exact resources, immutable current terrain
authority and existing BEPU integration. Native solver caches are not save data.

`PartContactLoad.ApplyForVehicle` (`06001A1C`) / `StripWeight` (`06001ABB`) remove
weight once per craft. `ApplyForPair` (`06001A1E`) uses contact-point velocity
including angular velocity, touchdown episodes and vector accumulation; lost
contact resets the episode. `PartFailure.Detect` (`06001A35`) and
`PhysicsBubble.DetectStructuralFailure` (`06001CDB`) own separate damage and
end-frame destruction responsibilities. Damage accounting caps do not clamp
physical solver impulses or velocity. NovaCore does not implement destruction
in this ticket.

Authenticated current live-changelog was read in this campaign. Relevant history:

- [Revision 5354](https://discord.com/channels/1260011486735241329/1260112103134724146/1541296827129794571): impulse/dt is not frame invariant; arresting impulse is.
- Revisions 5356, 5357 and 5362: weight subtraction, child identity, deferred
  solved impulses, independent failure ownership and pair/episode accounting.
- [Revision 5411](https://discord.com/channels/1260011486735241329/1260112103134724146/1546294456427225169): per-part contact-point impulse, fresh recontact budget and vector sums prevent repeated pushout charging.
- [Revision 4970](https://discord.com/channels/1260011486735241329/1260112103134724146/1529244685745262844): recovery speed reduced to 1.5 m/s after embedding was fixed.
- Revision 4686: terrain normal filtering removes perpendicular triangle-edge contacts.

The current contact search included newer 5448/5468 entries; no superseding
ordinary-response mechanism was found. Installed source remains primary truth.
The history search for restitution returned no result; the positive installed
solver/material evidence, rather than that absence alone, decides this finding.

NovaCore intentionally retains physical continuation rather than KSA's examined
rails fallback that zeroes velocity. The positive requirement is Project
Control's explicit prohibition on fake landed-state / velocity-zeroing behavior.
