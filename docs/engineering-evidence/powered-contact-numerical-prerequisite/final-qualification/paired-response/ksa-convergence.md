> Historical fallback evidence. Original judgments and measurements remain unchanged. Old campaign commands/counts describe their original runs; current reproduction and retirement records are in [../../../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md](../../../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md).

# Mandatory current KSA and live-history review

## Sources actually inspected

Current installation: **`E:\Kitten Space Agency`**. Installed `KSA.dll` product version **2026.9.7.5402+487c3f340de24c6a81037120b6d1129c045c5400**. The lead located this directory; the read-only investigator hashed its actual assemblies/dependency manifest and inspected decisive method IL directly with PEReader/MetadataReader, without executing KSA. This is current installed production evidence, not an inference from a NovaCore report.

| Actual installed file | Bytes | SHA-256 |
|---|---:|---|
| KSA.dll | 4,798,552 | A8A5164204EED962DF9D9025E99F523D9A23EE2CE6E802B18216A9DA35506D2F |
| BepuPhysics.dll | 880,640 | 77185E513BD530DEB322E41DD4ACDA9AD8E32E626CFA19492FB7481F25ED1FA7 |
| BepuUtilities.dll | 163,328 | E0A1528DED4EF8EFCB8002B99704CB8614B7DB840D2CB39B5E45EDA52D63BC68 |
| KSA.deps.json | 73,657 | FD627C9AE4D23928F4608BF91A72A75166806871D4C721F465E2A1208531A9FD |

Both backend assemblies are version 2.5.0.0, product **2.5.0-beta.29+f73164bb3c9ca733eb3329f1f6b1cea4e216ece7**. The actual installed dependency manifest also identifies beta.29. Exact pinned backend source was inspected at that commit, not latest unrelated upstream.

Readable complete decompilation root: **`E:\NovaCore\build\ksa-residency-reference\assembly-source\KSA`**. Its twelve previously recorded contact-path source hashes were freshly checked against the retained manifest, and the installed assembly still matches the assembly from which that evidence was produced. Decisive current DLL calls were inspected independently of the retained text. The decompilation is a readable aid to the same installed binary, not a claim to possess KSA's private source repository or tests.

Actual retained engineering history: authenticated Discord **Kitten Space Agency / live-changelog**, server **1260011486735241329**, channel **1260112103134724146**. The lead directly opened/read these messages in the existing in-app browser and used the channel's search for the June entry. No local NovaCore summary or installed version JSON substituted for this gate. No messages, reactions or history were changed; no bulk export was made.

| History message actually read | Date / revision shown in Discord | Relevant meaning |
|---|---|---|
| [1494540216482926693](https://discord.com/channels/1260011486735241329/1260112103134724146/1494540216482926693) | 2026-04-16 / 4136 | Persistent vehicle update clusters prepare ownership for retained BEPU arenas. |
| [1512355160775721020](https://discord.com/channels/1260011486735241329/1260112103134724146/1512355160775721020) | 2026-06-05 / 4549 | Derivatives and propellant consumption occur once per outer update despite multiple collision-solving pieces. |
| [1524556268151374037](https://discord.com/channels/1260011486735241329/1260112103134724146/1524556268151374037) | 2026-07-08 / 4866 | Collision pre-pass avoids excessive speculative contacts at high warp; regime selection uses actual propulsion availability. |
| [1524584112093007943](https://discord.com/channels/1260011486735241329/1260112103134724146/1524584112093007943) | 2026-07-08 / 4867 | Angular velocity must survive CCI/CCF transitions correctly. |
| [1524639263658999972](https://discord.com/channels/1260011486735241329/1260112103134724146/1524639263658999972) | 2026-07-09 / 4874 | Rolling resistance uses inverse inertia, fades near rest and must not hold a body against gravity. |
| [1534637795740750058](https://discord.com/channels/1260011486735241329/1260112103134724146/1534637795740750058) | 2026-08-05 / 5177 | Repeated execution of jet commands within physics caused overshoot/chatter/excess consumption, especially under warp. |

These actual Discord revision labels supersede off-by-one labels found in old installed-version summaries. Current implementation versus these lifecycle/history statements: **CONSISTENT**. History establishes intent and past defects, not a proof of current numerical accuracy or tiny-event support.

## Current responsibility ownership

Anchors below are in the verified readable KSA source root.

| Responsibility | Current implementation / anchor |
|---|---|
| Solver ownership / iterations | `ConstraintSim.cs:190-197`: retained Simulation, SolveDescription(8,1). `851-858`: float Timestep. No adaptive contact residual loop found. |
| Powered and coast contact | `PhysicsBubble.cs:1953-2036`: one constrained lifecycle, outer derivatives/nozzles, bounded pieces at most 1/60 second, native readback, outer propellant consumption. |
| Force/inertia | `PoseIntegratorCallbacks.cs:48-72`: game double loads/properties produce changes reduced to native FP32. |
| Contact preparation | `NarrowPhaseCallbacks.cs:174-207`: material, observations and filtering; backend owns solver manifold/cache. |
| Game body application | `ConstraintSim.cs:373-421`; `KinematicStates.cs:25-60`; `PhysicsBubble.cs:664-699`; `Vehicle.cs:2399-2409`: read backend body state, prepare/ready/apply game state. |
| Reuse / invalidation | `PhysicsBubble.cs:504-520,2340-2365`; `ConstraintSim.cs:166-187,834-863`: retained/pool lifecycle, stale removal, poison on backend exception. |
| Regime selection | `PhysicsBubble.cs:1184-1420`; `ConstraintSim.cs:899-929`: activity/contact/flight-computer/situation drives constrained eligibility. |
| Contact aggregate used by game | `ConstraintSim.cs:1022-1075` reads FP32 BodyTerrainAccumulatedImpulse; `PoseIntegratorCallbacks.cs:74-93` uses it in bounded angular damping. Thus impulses can affect future behavior; they are not universally diagnostic. |

Current DLL IL anchors independently checked: ConstraintSim constructor RVA198332 (219bytes), Simulate RVA205764 (106), PushBodyStateToSim RVA199208 (233), UpdateVehicleFromSim RVA200108 (241); PhysicsBubble.FullPhysicsConstrainedStep RVA886200 (1210), ApplyResultsToVehicles RVA873380 (270); PoseIntegratorCallbacks.IntegrateVelocity RVA210064 (1729); KinematicStates.FromBepu RVA852084 (119). Identity hashes are retained in identity.json. No installed KSA code was executed.

Pinned [BEPU contact representation](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/Constraints/Contact/ContactConvexTypes.cs#L92): FP32 vector-wide tangent, four normals and twist reside in backend accumulated-impulse buffers. Warm start and normal/tangent/twist solves use the current geometry and bounds (lines787-822).

Pinned [constraint update](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/CollisionDetection/NarrowPhaseConstraintUpdate.cs#L82): normal impulses follow feature IDs/redistribution; same-type constraints update in place, changed types replace, new pairs begin without prior impulse. [Accessor lines26-75](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/CollisionDetection/ContactConstraintAccessor.cs#L26) leave same-type tangent/twist history in the backend. Its friction-history comment is an assumption/TODO, not a proof of irrelevance.

[TangentFrictionOneBody.cs:46-73](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/Constraints/Contact/TangentFrictionOneBody.cs#L46) projects to the current friction magnitude; [TwistFrictionOneBody.cs:27-66](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/Constraints/Contact/TwistFrictionOneBody.cs#L27) clamps to a symmetric current cap. Saturation is ordinary solver state. Float dt and iteration ownership are in [Solver_Solve.cs:1415-1482](https://github.com/bepu/bepuphysics2/blob/f73164bb3c9ca733eb3329f1f6b1cea4e216ece7/BepuPhysics/Solver_Solve.cs#L1415). No KSA vehicle call to backend impulse-scaling APIs was found; existence of such APIs is not evidence of current usage.

## Equivalence and mandatory gate judgment

**PARTIAL. Both mandatory source categories were available and inspected.** Source access is complete for this bounded review; responsibility equivalence is not total.

KSA's inspected production path has no equivalent scaled high-precision sidecar, exact 2^-1075 duration or one-ULP paired-cache admission. An equivalent shipped KSA validation suite is **UNAVAILABLE**; do not claim that no such test exists anywhere.

NovaCore's positive exact event is zero when projected to double or float. Its current scaled cache is genuinely read by subsequent preparation. This is a relevant precision/domain difference. KSA's ordinary float handoff cannot be substituted while claiming unchanged exact-event semantics. Conversely canonical revision/debt uniqueness does not imply that one-ULP cache identity is necessary.

| Responsibility | Convergence classification | Positive reason |
|---|---|---|
| Retained solver / manifold / ordinary warm-start ownership | ADOPT as presumptive ordinary boundary | Same contact continuation purpose; backend owns approximate solver state. |
| Game-owned force/property input and solved body application | ADAPT | Same physical ownership purpose; NovaCore canonical authority/transaction differs. |
| Friction saturation and current-bound projection | ADAPT | Equivalent contact state, but frozen diagnostic admission remains unchanged here. |
| Positive exact tiny duration and exhaustion order | ADAPT | Installed KSA float dt does not preserve this required domain. No exact identity bar follows automatically. |
| Revision/debt/history and same-phase acknowledgement | INTENTIONALLY DIFFER at publication boundary | Explicit NovaCore authority requirements; not a justification for private cache precision by association. |
| Paired private-response test | No equivalent KSA contract demonstrated | Requires NovaCore outcome/ownership evidence below. |

Independent reviewers agreed: no conclusive KSA redirect before attribution. The authorized numerical phases therefore proceeded. Recommended convergence now: split the proof obligations by ownership; retain exact tiny-event authority and physically meaningful continuation, while using KSA's solver-private versus game-authoritative distinction. No cache lifetime, mechanism, solver count or test was changed.
