# Stage 2 bounded implementation gate

2026-09-17. Stage 1 manual acceptance is PASS; source seals 19/19 and 67 tags match. Stage 2 implements powered support on the same FourHorn assembly, stock 600 N main engine, zero gimbal and RCS off. All 1,200 recorded intervals demand main thrust. No geometry, mass law, rating, gravity, solver iterations or substeps change. The 20-second stock episode cannot exhaust: expected fuel 28.4375 kg, oxidizer 42.65625 kg, total mass 701.09375 kg. Exhaustion/departure are not claimed by this qualification.

## Direct current KSA and history

Read-only inspection of `E:\Kitten Space Agency`, installed build `2026.9.10.5438+c9291b5dd3347e847020c8fb3dd11dfce6a728e9`. KSA.dll SHA-256 `A03E98153C6F353AF6F280CD3BDC1C71C718008E2049F508DC6FBE54457A0AA8`; BepuPhysics.dll `77185E513BD530DEB322E41DD4ACDA9AD8E32E626CFA19492FB7481F25ED1FA7`; BepuUtilities.dll `E0A1528DED4EF8EFCB8002B99704CB8614B7DB840D2CB39B5E45EDA52D63BC68`. Installed BEPU version is 2.5.0-beta.29+f73164bb3c9ca733eb3329f1f6b1cea4e216ece7.

Method-token reproduction: `dotnet <ilspycmd.dll> --disable-updatecheck -m 0x06000688 'E:\Kitten Space Agency\KSA.dll'` (substitute tokens below). Read installed binaries; do not retain decompiled proprietary source.

| Responsibility | Current source | Disposition |
|---|---|---|
| Propulsion before ordinary solve | FullPhysicsStepUntil 06001C02; UpdateActiveNozzles 06001C59; ComputeDerivatives 06001B8F; IntegrateVelocity 060006BD | ADAPT canonical recorded commands and prepared input; no resource mutation in callback |
| Source properties through solve, one debit afterward | FullPhysicsConstrainedStep 06001C19; ConsumePropellant 06001C5B/06001B46 | ADOPT ordering; ADAPT exact two-store successor and atomic publication |
| Canonical derived properties | RecomputeMassProperties 06001B43; CopyToBepu 06001B40 | ADAPT existing FP64 assembly mass/COM/full tensor, never collider-derived mass |
| Same native body and shape | PushBodyStateToSim 06000688; UpdateShape 0600068E; UpdateColliderCompoundLeaves 06002F19/06002F18 | ADOPT in-place property/child updates and normal BEPU feature/cache handling |
| Material-origin continuity | KinematicStates From/Copy 06001B32/06001B34 copy directly; source chain has no COM compensation. ShiftColliderCompound 06002F1A has no direct callers found | ADAPT: preserve NovaCore's banked material-O law; do not copy uncompensated geometry shift |
| Cache ownership | BEPU UpdateConstraint 06000892 and RedistributeImpulses 06000890 | ADOPT normal feature-based persistence; never promise immutable manifolds or introduce custom cache transport |

Actual authenticated Discord live-changelog entries inspected again for this responsibility:

- [4549](https://discord.com/channels/1260011486735241329/1260112103134724146/1512355160775721020): nozzle flow reuse and one resource/derivative update per outer constrained interval.
- [4428](https://discord.com/channels/1260011486735241329/1260112103134724146/1506430180674113696): COM changes must refresh body bounds; material/body geometry distinction.
- [5174](https://discord.com/channels/1260011486735241329/1260112103134724146/1534461444010016902): firing engines, landed-state admission, feed readiness and shutdown are distinct responsibilities.
- [5434](https://discord.com/channels/1260011486735241329/1260112103134724146/1548873113746411602): reactant reachability/drain correctness and resource graph lifetime; one shared drain path. Current history also exposes later 5439-5448 changes, including ground-clutter COM/hull fixes. They are not represented as installed vessel behavior or superseding the inspected owner chain.

Directory/source and live history are separate evidence. No NovaCore summary substitutes for either. Gate: PASS for this bounded responsibility.

## One lifecycle adaptation

Existing assembly authority remains the sole exact two-store owner. Add a contact-specific duration admission over the same arithmetic core; preserve the free-flight 15,625-tick guard and its evaluator unchanged. Hold source mass/COM/inertia through the ordinary retained solve. Prepare the exact resource and derived-property successor before mutation; do not run the free-flight motion evaluator.

Export the solved material O using source COM. Then refresh numerical coordinates of the same body to successor COM: `p1=p0+R(c1-c0)`; `v1=v0+omega cross R(c1-c0)`; child offsets are freshly projected from authored `rO-c1`. Fixed native basis B remains unchanged; R=qNative*B. Orientation is not reset. This algebra preserves material points and rigid point velocities. No generic c-dot, momentum rescaling, new mass law, world recreation or canonical pose reimport.

All fallible mass/projection checks belong before mutation where possible. After a solver/refresh mutation, any failure invalidates continuation. Private refresh occurs once during preparation and is sealed to the pending typed successor; a precommit publication refusal preserves that proposal. Retry does not step, spend, refresh or reconstruct again. Final canonical state/stores/mass/actuator/revision/clock/debt/history writes and private acknowledgement remain in the same owner phase. Postcommit acknowledgement failure retains canonical authority and invalidates the private continuation.

Cheap witnesses precede the long campaign: exact resource outcome, source properties, actual native geometry/velocity invariance under COM refresh (including angular velocity), same handles, retry/nonmutation, and unchanged unpowered/free-flight gates. Physical support and the existing 20 mm penetration / 61 micrometre final-window drift bars remain. Keep 600/600 final support, host-partition determinism, exact zero allocation, 8 MiB retained storage and existing complete-operation timing ceilings. Stop on material unexpected failure; one bounded correction only after causal proof.

Independent read-only red team: PASS for bounded implementation, conditional on actual FP32 geometry/velocity and lifetime witnesses. No new resource/mass authority required. Cold per-frontier resource-table precomputation is deferred: no measured need, and a full table alone could exceed storage headroom. Stage 2 is not yet qualified.
