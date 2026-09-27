# Current KSA reference and causal evidence

Read-only installed reference: `E:\Kitten Space Agency\KSA.dll`, version `2026.9.22.5482`, product suffix `40faabcc2a54baeb11b7b9f21beade6f7e6b524e`, SHA-256 `ce43d022e7dac9beb2352b6106164bc1f521dda1ae9d0b898b8dba0c59a69723`. Current method bodies inspected with ILSpy 11.0.0.9375; no proprietary source/assets are retained in this package. User explicitly authorized installed evidence for now when authenticated history was unavailable. No fresh live-history or interactive-KSA experiment claim is made. KSA writes: zero.

## Current mechanisms

| Responsibility | Installed evidence | NovaCore classification |
|---|---|---|
| Full craft traversal | `VehicleEditor.HandleConnectorConnections` and `HandleSnapping` traverse `EditingSpace.AllParts`, excluding the held subtree/preview; no root-only depth boundary | ADOPT instance-scoped traversal; existing NovaCore compiled flat list already meets this |
| Endpoint ownership/occupancy | `Part.Connector.Parent`, individual `Connection`, `CanConnect`, `PositionVehicleAsmb` and parent assembly transforms | ADOPT; no definition-global occupancy was found in NovaCore |
| Radial RCS host | `Content/Core/CorePropulsionBGameData.xml` RCS part entries declare `ToSurface`; `HandleSnapping` chooses the ray-hit eligible host instance, surface normal and mount-point alignment | ADAPT host/mount ownership through NovaCore's accepted explicit radial sockets |
| Symmetry | `HandleSiblingConnectors` and surface symmetry paths derive members from the selected host and create/bind distinct instances | ADAPT deterministic authored 1/2/4/8 placement sets and atomic commit |
| Axial/engine attachment | General compatible connector/occupancy paths, not first-tank or engine-name special cases | ADOPT generic endpoint eligibility |
| Marker presentation | Inspected connector candidate/render paths use connector transforms/screenspace, without NovaCore's own-host triangle veto | ADAPT marker presentation separated from physical mating pose; no unverified claim about KSA's complete GPU depth policy |
| RCS attachment vs exhaust | RCS data declares separate solid collider, connector/feed wiring, nozzle exhaust location/direction and plume reference. Inspected surface alignment/attachment and connector compatibility paths do not introduce an expanding exhaust-volume placement veto | ADAPT this responsibility split; correct original NovaCore content, copy no KSA values/art |
| Arbitrary free-surface editing | KSA supports surface hit placement; accepted NovaCore Part Standard/editor scope uses authored host socket sets | INTENTIONALLY DIFFER, supported by `modular-craft-first-playable/accepted-plan.md` and permanent compatibility/symmetry tests; not broadened here |

The absence finding is bounded to the inspected equivalent attachment/compatibility paths and RCS data. It is not a claim that KSA has no exhaust-related physics anywhere, nor that an allowed editor placement guarantees physically harmless firing.

## Hypotheses killed before changing code

- Definition-global occupancy: false. Existing keys contain both instance and endpoint IDs.
- Root-only traversal: false. Original `RebuildSockets` visits every compiled part.
- Missing recursive pose composition: false. Document poses are already in assembly coordinates; composing them recursively again would be wrong. Endpoint pose is instance assembly pose composed with authored local frame.
- Commit bypass/preview disagreement: not demonstrated. Original model accepts the axial recipe. The frontend can hide the candidate before preparation is reached.
- Own-host visibility veto: reproduced. `SocketVisible` raycasts the host itself; a hit more than `sqrt(3)*0.065/2` m ahead of the endpoint hides both marker and pick target. Pitch +0.2 gives a permanent witness. Support mesh can similarly hide the engine connector.
- Symmetry contamination: instance occupancy remains sound. Passing the global radial count to an axial request caused an independent frontend refusal; request count now follows endpoint kind.

## Why the old RCS rule is not a general placement contract

The authoring generator declared the 4 m length, 10 mm aperture radius and 3 degree half-angle as an **assumption**, explicitly not a solved plume/thermal model. The earlier Gate-4 report qualified two specific twelve-part articles, not arbitrary tank chains. No current-KSA equivalent placement restriction or new plume derivation supports treating this envelope as ordinary solid attachment clearance.

Short-tank radius is 0.6 m. RCS nozzle axes are 0.15 m above that surface. The assumed expanding radius reaches the tank skin after `(0.15-0.01)/tan(3 degrees) = 2.6713591363 m`, although the solid nozzle hardware and its centreline remain outside the tank. Longer stacks therefore fail even with correct host identities and transforms.

Independent old-volume witness, in assembly metres: core origin `(0,0,0)`, three short-tank origins `(-1.5,0,0)`, `(-3,0,0)`, `(-4.5,0,0)`; first radial-0 RCS origin `(-0.75,0.6,0)`. Point `(-4.4,0.58,0)` lies inside the third tank and the RETIRED negative-X expanding envelope. The generator names that aft exhaust `exhaust-axial-positive` because actuator force is positive X; exhaust direction is opposite force. This is real overlap of the assumed volume, not proof of real plume impingement, heating or unsafe attachment.

Correction therefore belongs in RCS authoring, not SAT tolerances, connector occupancy or a per-part validator exemption. Solid body/nozzle collision remains required. Runtime nozzle direction, force, resource usage and plume presentation remain separate unchanged responsibilities. This correction makes no new plume-heating or obstruction-physics claim.
