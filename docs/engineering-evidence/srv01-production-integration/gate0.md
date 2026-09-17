# SRV-01 Gate 0 — independent jets in a reusable block

2026-09-17. Candidate only; no banking. Baseline: `09463ec6c323daf233205cb08306d38611c22e6e`.

## Decision

**A — one reusable RCS block owns four physical jet actuators. ADAPT.** Four placed blocks therefore own sixteen jets. Meshes do not create actuators: a new immutable physical definition explicitly authors each jet's identity, point, force axis and exact flow model. The historical four-single-jet design remains a separate qualification fixture, not the physical definition of the accepted four-horn asset.

Adopt nozzle force and current-COM moment accumulation. Adapt KSA's separate part, actuator, feed and observation ownership to NovaCore's exact shared-store accounting, immutable recorded command plan and existing exclusive canonical publication owner. No general force allocator is introduced. Six explicit two-jet sign commands are the supported control vocabulary; they are not a claim of arbitrary independent six-degree-of-freedom control. Every selected jet consumes propellant even when resultant force cancels.

## Actual KSA sources and official history

Directly inspected installation: `E:\Kitten Space Agency`, KSA.dll version `2026.9.10.5438`, 4,936,792 bytes, SHA256 `A03E98153C6F353AF6F280CD3BDC1C71C718008E2049F508DC6FBE54457A0AA8`. KSA writes: zero. Installed `Content/Core/CorePropulsionBAssets.xml:226–266` instantiates four separately identified thruster subparts in LargeA; `CorePropulsionBGameData.xml:3–34` gives each a controller/core/nozzle. Their parent feed is shared. This is implementation evidence, not inference from four visible horns.

Current binary method tokens: `FlightComputer.SelectJetsToFire` 06000985 (per-controller geometric pulse heuristic); `RocketNozzle.UpdateState` 0600155C (authored physical endpoint and opposite exhaust direction, separate FX endpoint); `VehicleUpdateState.UpdateActiveNozzles` 06001C59 (force and lever arm about current COM); `PartTree.Deserialize` 06001A8D (template reconstruction and module state). KSA positional subpart restoration is not adopted as reorder-safe identity. Its resource code is not evidence of NovaCore exact all-or-none shared-feed exhaustion.

The lead directly read the authenticated official Discord `#live-changelog`, not a NovaCore summary:

- [Revision 2905, 2025-11-24](https://discord.com/channels/1260011486735241329/1260112103134724146/1442679965236854785): controller, rocket core and nozzle responsibilities are distinct; a core may feed multiple nozzles.
- [Revision 5177, 2026-08-05](https://discord.com/channels/1260011486735241329/1260112103134724146/1534637795740750058): repeated jet-command execution caused excess response and fuel use. NovaCore keeps once-consumed canonical interval commands.
- [Revision 5434, 2026-09-13](https://discord.com/channels/1260011486735241329/1260112103134724146/1548873113746411602): reactant routing and consumption-order changes concern shared resource ownership. NovaCore retains its separately qualified exact species accounting.

Newer Discord entries are not represented as behavior of the installed 5438 binary. Source inspection establishes architecture, not runtime qualification of the new NovaCore design.

## New explicit physical authoring

The accepted Blender asset remains unchanged and visual-only. Body coordinates use the proper rotation `C(x,y,z)=(y,x,-z)`. Accepted instance rotations become explicitly authored NEW physical rotations `C R Cᵀ`; they are not claimed to equal the old canted single-jet frames. The existing independently authored dry mass, local COM and local inertia are retained in their NovaCore part-local frame. Assembly mass properties are recomputed and independently checked.

Jet endpoints in that part-local frame (metres), with force opposite the accepted socket's outward exhaust:

| Jet | Point | Unit force axis |
|---|---|---|
| top | (0,-0.193,-0.184) | (0,0,1) |
| bottom | (0,-0.193,0.184) | (0,0,-1) |
| left | (-0.184,-0.193,0) | (1,0,0) |
| right | (0.184,-0.193,0) | (-1,0,0) |

Each jet explicitly uses the retained ideal 22.5 N model and 3/2048 kg/s extent rate; these are NovaCore authoring, not KSA ratings or quantities inferred from mesh dimensions. The main remains 600 N. Two species stores feed seventeen consumers through thirty-four explicitly typed feed edges. Stable identity is the tuple (part instance, actuator), persisted through definition/version hashes and launch lineage. A sixteen-bit activity set replaces the historical four-bit usage; no bit aliases are admitted.

The new canonical part contribution order is capsule, tank, main, RCS01, RCS03, RCS02, RCS04. Opposite placements cancel by the declared binary64 accumulation order; no first moments or tensor terms are clamped to zero.

These are nominal independently authored physical properties, not a density model of the accepted artwork. In particular, the retained RCS local COM `(0.1,0,0)` lies outside the accepted visual housing's body-Y extent `[-0.3,-0.115]`. This candidate qualifies the explicit point-store assembly model and truthful nozzle transforms, not physical manufacture or mass-density realism of that housing. It does not infer or retune mass properties from mesh bounds. Individual nozzle endpoint bounds (local radius <=0.3 m and assembly radius <=1.3 m) are admitted before pair aggregation; enormous opposing moments cannot hide behind cancellation and erase smaller contributions.

Independent analytical results: dry mass 630 kg, first moment (734.4,0,0) kg m, origin inertia diagonal (190.47255,1496.626691666667,1496.626691666667). Dry/705 kg/730 kg COM x is 1.1657142857142857 / 1.0417021276595746 / 1.006027397260274 m. Dry/capacity transverse inertia is 640.5261202381 / 757.8001711187 kg m².

Explicit command pairs: +ROLL 01/left + 03/left; -ROLL 02/right + 04/right; +PITCH 01/bottom + 02/bottom; -PITCH 03/bottom + 04/bottom; +YAW 02/bottom + 03/bottom; -YAW 01/bottom + 04/bottom. Roll is a real zero-resultant-force couple, ±36.315 N m. Pitch/yaw produce 45 N axial force plus ±25.678582758789474 N m selected-axis moment. Neither force cancellation nor a command name substitutes for individual physical contributions. Current COM stays on X, so these particular rows' moments are COM-independent; general force/moment evaluation is not.

Minimum signed angular acceleration is approximately 0.03388569 rad/s², above the retained 0.005 bound. The unchanged two-second conservative envelope gives angular speed <1.588534 rad/s and material-origin speed <4.461350 m/s, within 1.72/4.65. This is cheap feasibility evidence, not the final numerical/runtime qualification.

## Lifetime and payoff

Retain the existing immutable 128-interval, two-second command plan, integer host credit, maximum-four-interval service budget, exact resource event split, paired canonical publication, receipt seal, refusal/invalidation, replay-validated save and copied presentation. No contact, editor, arbitrary vehicle allocator or new dynamics law is added. Reusable meshes/materials load once; frames only transform instances and present realized actuator state. Geometry never supplies physical mass or force authority.

Gate 0 permits bounded implementation because identities, feeds, forces, moments and supported commands are explicit and analytically feasible. Qualification must independently cover all sixteen endpoints, visual/physical mapping, save identity, exact consumption, updated mass/inertia and trajectories. The accepted artwork and historical physics evidence stay separately identified.
