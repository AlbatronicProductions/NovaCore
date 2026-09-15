# Current banked retained-contact lifecycle

Architecture audit at 49057fecceb0f725d5f551ec40e2780971b0d81d. Source observations, not new implementation.

| Stage / source anchor | Owner and lifetime | Mutable/private state and authority boundary | Contract versus mechanism |
|---|---|---|---|
| LocalContactSource.cs:143-180 | Canonical engine supplies copied source; source survives episode | Captures paired state, mass, timeline, initial epoch/end; source validates exact engine/config/state | Strict external-mutation refusal is contract; frozen mass/force are current admitted model |
| LocalContactSource.cs:118-120 | Configuration/article author | Article requires exact authored mass and principal inertia; box requires uniform-box inertia | Prevents silently relabelling authored wet/dry mass |
| LocalContactWorld.cs:101-120 | Owner thread, cold creation | FP64 root subtraction, constant moving-frame velocity subtraction, orientation/angular conversion, checked local FP32 import | FP64 canonical authority/bounded reduction contract; one cold import |
| LocalContactWorld.cs:53-86 | Private retained world | Pool, Simulation, dynamic BodyHandle, fixed shapes/slab; inverse mass/inertia installed once; sleep disabled by negative threshold | Same retained solver resources contract; SolveDescription(8,1) and capacities qualified mechanism |
| LocalContactWorld.Article.cs; EngineeringContactArticle.cs:43-56,69-84 | Article owns authored geometry/dry interpretation; world owns shape buffers | Three uniform boxes total 1,000 kg; assembly COM (0,0,0.1); body offsets subtract COM once | Existing article remains unchanged; resource attachment has no permission to reinterpret it |
| LocalContactCallbacks.cs:48-64; LocalContactWorld.cs:112 | Private callback | Acceleration = captured root force / source mass rotated to fixed local frame, then v += a*dt | No hidden gravity; qualification encodes gravity as constant source force with fixed mass; no engine torque path |
| LocalContactCallbacks.cs:21-43 | BEPU narrow phase + NovaCore material/selection callback | Child manifolds feed coverage, parent receives selected rows; dry friction .5, 30 Hz spring, recovery 2 m/s | Keep material/coverage qualification; contact flags are diagnostic, not canonical grounded authority |
| CompoundContactCoverage.cs:44-113; CompoundContactSelector.cs:31-103 | World-owned preallocated scratch; NovaCore selects four rows | Stable child/features; deepest/imminent/kinetic-response coverage; reads current inverse mass/inertia and frozen acceleration | Geometry unchanged by selection; mass and prediction DO affect scores |
| Pinned DefaultTimestepper.cs:28-42 | BEPU private step | Sleep/predict/collide/solve/optimize; feature matching caches and constraint impulses retained | Geometry/manifold refresh and warm-start lifetimes are distinct; see BEPU assessment |
| LocalContactWorld.cs:124-162 | Owner-only private interval | Enforces next exact T(n); marks unsafe before Timestep, restores eligibility only after valid export; one pending receipt | Post-step failure poisons continuation; no canonical mutation by solver |
| LocalContactWorld.cs:149-160 | Private export | Reconstructs root position using ORIGINAL source epoch; FP64 exported paired values with frontier | Endpoint only proves sampled state; no reimport after publication |
| LocalContactWorld.Publication.cs:14-35,61-106 | Expected binding under canonical owner | Immutable engine/definition/properties/timeline/episode; mutable paired expected state/revision/clock/frontier/pending | External mutation rejected; receipt sealed to owner/generation/frontier |
| SimulationTransactionEngine.PersistentContact.cs:61-126 | Existing exclusive publication phase | Prepare state/clock/debt/history/ack, recheck, fixed writes, fixed acknowledgement | One physical StateRevision/history entry; TimelineRevision unchanged; no fuel/actual-actuator write exists |
| LocalContactWorld.Publication.cs:118-135 | Same-phase private acknowledgement | Updates only expected paired state/revision/clock/frontier; no body or manifold write | Canonical commit survives failed ack; continuation invalidates |
| SimulationTransactionEngine.ContactServicing.cs:18-61,73-106 | Same engine phase | Once-only host credit; at most four exact intervals; debt/event/history checks before stepping | Host samples do not advance physics themselves; current path has no command/engine/resource consumption |
| LocalContactWorld.cs:185-203; ContactDevelopmentScene.cs | Cold owner retirement / copied presentation | Dispose explicitly removes static/shapes/body/world; finite episode holds last observation | No automatic departure/free-flight consumer or recontact lifecycle |

## Original schedule and source state

T(n)=T0+floor(n*1,000,000/60), derived from the original captured source epoch (LocalContactSource.cs:182-190). Publication never resets T0 or moving-origin epoch. Only one outstanding endpoint is permitted. Host credit advances expected clock accounting; physical publication advances expected paired state. The same phase owns both operations.

M14.18 private-only stepping remains available; M14.19 publication is opt-in. M14.17 certified-clearance continuation is a separate applicability/proof contract and is not a route for a BEPU receipt.

## Actual integration gaps

1. Captured force and installed inverse mass are frozen. Merely assigning a new inverse mass would fail expected-property validation and leave force/selection predictions inconsistent.
2. Nonzero canonical body torque is rejected at capture. Full engine moment cannot be attached to the existing constant-acceleration callback as an unrelated later impulse.
3. Publication stores old mass/inertia/force metadata with the new pose; it neither spends resource nor publishes actual hardware.
4. Current M14.21 geometry is 1,000 kg asymmetric inertia. It is outside the M15.0 dry-8/spherical/no-environment physical admission. This is a declared-model gap, not a broken banked article.
5. Current contact counts/MaximumDepth do not establish separating support or a collision-free next interval. Contact loss is not implemented as M15 consumer selection.
6. The selector is not purely geometric: inverse mass/inertia and v+a*dt+omega cross r enter ranking. Keep its four-row algorithm; a powered adapter must supply consistent predicted motion, including torque, and requalify those inputs. Do not silently keep its cold gravity-only acceleration.

These are concrete convergence responsibilities. None authorizes weakening old validators or changing production in this investigation.
