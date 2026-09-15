# Narrow KSA convergence: current source versus official history

Current installed KSA.dll version 2026.9.7.5402+487c3f340de24c6a81037120b6d1129c045c5400, SHA-256 A8A5164204EED962DF9D9025E99F523D9A23EE2CE6E802B18216A9DA35506D2F, matches retained prior identity. Ten previously manifest-listed source files freshly matched; VehicleUpdateTask.cs and ConstraintSimPool.cs were additionally inspected/hashed. No proprietary code, constants/assets or decompile dump was copied into this package.

Private current source root: E:/NovaCore/build/ksa-residency-reference/assembly-source/KSA. This source is architectural evidence, not a NovaCore runtime dependency.

## CURRENT KSA SOURCE

| Responsibility | Finding / source anchor | NovaCore disposition |
|---|---|---|
| One spacecraft/module/resource owner | PhysicsBubble.cs:1404-1419 updates common modules/nozzles, then selects constrained/unconstrained integration on shared VehicleUpdateState; applies to same vehicle :664-690 | ADOPT common prepared input / same canonical successor responsibility |
| Game-owned ready/apply | VehicleUpdateTask.cs:141-176 owns preparation, stepping and result application | ADAPT to existing exclusive NovaCore owner; KSA does not establish NovaCore revision/debt/atomic semantics |
| External wrench and resource separation | PhysicsBubble.cs:1953-2035 computes disturbances, performs bounded contact chunks, then consumes/recomputes resource; VehicleUpdateState.cs:493 | ADAPT separation from prediction; resource update cadence intentionally differs |
| Mass/force cadence | PoseIntegratorCallbacks.cs:48 uses stored disturbances and properties; vehicle branch references outer Sim.SimStep.DeltaTime, not simply lane dt; mass recomputed after outer interval | INTENTIONALLY DIFFER: exact events/stage mass/authority require a new qualified bridge; do not copy cadence |
| Mass versus shapes | VehicleProperties.cs:64 writes local inverse properties; ConstraintSim.cs:261 and :424-455 separately synchronize body/shape/COM | ADAPT mass/shape separation; no inspected explicit changing-mass impulse reset/rescale |
| Natural contact loss | ConstraintSim.cs:896 examines awake constraints with game-specific exceptions; PhysicsBubble.cs:1443 updates contact state from fresh detection | ADAPT physical evidence; not a NovaCore finite-body clearance certificate or T>mg switch |
| Retained solver / recontact | PhysicsBubble.cs:1336 screens approach/clearance; :517-520 retains recently needed solver; :2352-2365 reuses/rents; :2340-2349 removes bodies on return; ConstraintSim.cs:166 clears caches/maps | ADAPT prepared resource reuse; do not promise perpetual body/feature/impulse continuity after pool return |
| Exact clock/resource/history | No inspected KSA equivalent proves NovaCore's integer lattice, exact exhaustion, revision/debt record or same-phase ack | INTENTIONALLY DIFFER, supported by banked NovaCore exact/tiny and transactional witnesses |

No propellant withdrawal/publication appears inside the inspected constrained callback. This does not prove arbitrary callback purity or safe repeated-callback time interpretation.

## OFFICIAL HISTORY / DESIGN INTENT

Installed official Content/Versions artifacts remain read-only and separately fingerprinted. These are historical intent, not substitutes for current source.

| Revision / date | Artifact anchor | Narrow relevance |
|---|---|---|
| 4135 / 2026-04-16 | v2026.4.X.4141.json:299 | Persistent vehicle clusters prepared physics arenas |
| 4548 / 2026-06-05 | v2026.6.X.4568.json:145 | Intentionally evaluate derivatives/propellant once per large update despite contact substeps |
| 4645,4658 / 2026-06-17 | v2026.6.X.4680.json:130 | Contact detection/rolling resistance and collision preflight |
| 5176 / 2026-08-05 | v2026.8.X.5261.json:72 | Repeated jet execution caused overshoot/excess fuel |
| 5220 / 2026-08-08 | v2026.8.X.5261.json:461 | Pooling/lifecycle |
| 5237 / 2026-08-09 | v2026.8.X.5261.json:616 | Stale resource handles after solver return |
| 5339 / 2026-08-20 | v2026.8.X.5348.json:702 | Physics-bubble ownership moved into VehicleUpdateTask |

The jet-history entry is **5176**, not the 5177 attribution in prior evidence. This package records the direct source correction; it does not edit the banked historical report.

## Convergence result

KSA supplies a concrete precedent for shared physical input, game-owned apply, and choosing constrained versus unconstrained private work. It does not justify matching NovaCore's exact numerical/atomic outcome by copying its outer-step mass/fuel cadence, contact flags, thresholds or pooling times.

No broad vehicle/situation/pooling/frame system is proposed. The narrower conclusion is one NovaCore canonical interval owner, distinct qualified consumers, reusable private resources, and explicit lifecycle acknowledgement.
