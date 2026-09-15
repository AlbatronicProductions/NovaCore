# Current KSA work ownership: direct installation and history inspection

2026-09-15. **NO EQUIVALENT KSA PRODUCTION RESPONSIBILITY FOUND.**
**RATIONALE UNPROVEN.** This is scoped to the inspected contact/backend,
post-step, structural load, impact, deformation and debug-consumption closure.
It is not a claim about every proprietary method or private test.

Actual root: `E:\Kitten Space Agency`. Actual installed build:
`2026.9.10.5438+c9291b5dd3347e847020c8fb3dd11dfce6a728e9`.

| Actual file | Bytes | SHA-256 |
|---|---:|---|
| KSA.dll | 4936792 | A03E98153C6F353AF6F280CD3BDC1C71C718008E2049F508DC6FBE54457A0AA8 |
| KSA.deps.json | 73752 | 23FCAA825C40F828349310C8DC480F292F9BE65DE6C4D378B36FBA64F7EE37B3 |
| BepuPhysics.dll | 880640 | 77185E513BD530DEB322E41DD4ACDA9AD8E32E626CFA19492FB7481F25ED1FA7 |
| BepuUtilities.dll | 163328 | E0A1528DED4EF8EFCB8002B99704CB8614B7DB840D2CB39B5E45EDA52D63BC68 |

The installed metadata survey covered 2516 types, 17706 method definitions,
17722 fields, 3318 properties and 11207 member references. Name searches
for work/energy/friction/dissipation/rolling/twist/impulse/contact were followed
by actual current method decoding, not used alone to assert absence. Existing
ilspycmd 11.0.0.9375 and direct PEReader/MetadataReader IL decoding were used.
Prior NovaCore KSA notes served only as navigation. No older decompilation
was substituted for the installed DLL.

## Current implementation anchors (KSA.dll)

Tokens and IL offsets below refer to that exact assembly.

| Type / method | Token; anchor | Current responsibility |
|---|---|---|
| NarrowPhaseCallbacks.ConfigureContactManifold / ResolveTerrainPairFriction | 060006C8 / 060006C7 | Pair friction, spring/recovery and manifold reporting; no work account |
| ConstraintSim.Simulate | 0600069D; 002F,0050,0063 | BEPU Timestep, clutter hits, PartContactLoad.Apply |
| ConstraintSim.HandleManifoldForVehicle | 060006A8; 004D,006E,0092,00D2,00E3 | Impact KE, clutter, part load, accumulated impulse magnitude, terrain contact |
| PartContactLoad.ReadPenetrationImpulse | 0600199A; 0052,00B5 | EnumerateAccumulatedImpulses then SumPenetrationImpulses |
| PartStructuralLimits.SumPenetrationImpulses | 06001A32; complete method | Sums absolute entries from index 2 to before final entry: excludes tangent pair and twist |
| PartContactLoad.StepMotionChange | 06001993; complete method | Before/after velocity change for momentum/load budget, not work |
| PartContactLoad.ApplyForPair | 06001994; 0232,026C,02F7,035E | Contact-point delta-v, touchdown momentum budget, impulse/area, deformation |
| PartContactLoadDebug.PublishFrame / DrawCheck / DrawStep | 060019A1 / 060019A8 / 060019A9 | Pressure and N*s momentum diagnostics; not friction joules |
| ConstraintSim.ComputeImpactKineticEnergy | 060006AA; 003B-0059,00BD,010F | 0.5*m*normal-closing-speed^2, not friction work |
| BubbleClutterStatics.ApplyPendingHits | 06002B50; 0058,00DB,00E0 | Peak impact KE against clutter break threshold |
| ConstraintSim.DetectTerrainContact | 060006AB; 007E,0151,01F7 | Terrain impulse, normal closing velocity and impact event |
| PoseIntegratorCallbacks.IntegrateVelocity | 060006BD; 01AD,01BB,01C9,024B | Terrain-impulse-dependent bounded angular damping; no energy ledger |
| FxDeformation.ReportContact | 06000A8E; 0070 | Pressure/crash-tolerance deformation presentation |
| KinematicStates.SleepEnergy | 06001B31; complete method | Squared motion measure, not physical friction dissipation |
| ConstraintSim.ReadBackFromSim | 0600068D; complete method | Copies solved state, updates situation/environment; no work publication |

Decisive method IL SHA-256:

- ReadPenetrationImpulse: `2D29A1CB5449DE97273C2CBBB3B639A3C51579E7DDA4602E037019978EBB5A3D`
- SumPenetrationImpulses: `686F2B097F68D2FBDF0FF7B4688FA370908DCED62ED62635082D6A04F4D14F14`
- ComputeImpactKineticEnergy: `105EF7BB516CEB62F5F6A4DC80CB64BFA82C2D9EE3A3DD252829450F4A4020F2`
- ApplyForPair: `1ACBE9EB4C8C5B6715DE0071EDFED9CD60AF254CA7A8716CEF905499C74C91E5`

KSA DOES have contact-related energy consequences. They are normal impact
and clutter consequences, alongside impulse/pressure/damping responsibilities.
None of these is the requested physical tangent/twist-work integral or a
validated solver friction-energy decomposition. No equivalent ready-to-adopt
mechanism satisfying this ticket was found. No new rationale is attributed
to KSA; no KSA mechanism was copied or modified.

## Actual available official history

The lead directly read the live Discord channel during this revision:
server 1260011486735241329, channel 1260112103134724146. The loaded history
included September 3-15 / revisions 5404-5446. Current installed code remains
build 5438; later visible history is not substituted for that code.

- [Revision 5411, September 6, 2026, 7:02 PM](https://discord.com/channels/1260011486735241329/1260112103134724146/1546294456427225169): per-part touchdown impulse budget at its own contact point; recontact reset and vector cancellation. Adjacent normal-load intent, not friction-work evidence.
- [Revision 5410, September 6, 2026, 6:03 PM](https://discord.com/channels/1260011486735241329/1260112103134724146/1546279808663822511): collider-volume/density crash rating. Adjacent damage intent, not friction work.

A bounded direct `in:live-changelog friction` search remained in Searching
state; it supplied no completed search result. The query was cleared. No
claim of a complete archive search or absence of all historical friction
discussion is made. Both the actual installation and actual available
history were inspected; the missing rationale remains UNPROVEN.
