# Direct current KSA lifecycle and actual engineering history

Inspected 2026-09-15, before NovaCore correction design. Prior summaries supplied navigation
only. The actual installed DLL was decoded afresh to stdout; no copied source dump or build
tree was created. The authenticated live-changelog entries below were read through the
existing in-app Discord tab, not substituted with NovaCore documentation.

## Current available installation

Root: `E:\Kitten Space Agency`.

| File | Bytes | SHA-256 |
|---|---:|---|
| KSA.dll | 4,936,792 | A03E98153C6F353AF6F280CD3BDC1C71C718008E2049F508DC6FBE54457A0AA8 |
| BepuPhysics.dll | 880,640 | 77185E513BD530DEB322E41DD4ACDA9AD8E32E626CFA19492FB7481F25ED1FA7 |
| BepuUtilities.dll | 163,328 | E0A1528DED4EF8EFCB8002B99704CB8614B7DB840D2CB39B5E45EDA52D63BC68 |
| KSA.deps.json | 73,752 | 23FCAA825C40F828349310C8DC480F292F9BE65DE6C4D378B36FBA64F7EE37B3 |

KSA product: `2026.9.10.5438+c9291b5dd3347e847020c8fb3dd11dfce6a728e9`.
BEPU product: `2.5.0-beta.29+f73164bb3c9ca733eb3329f1f6b1cea4e216ece7`.
The actual available install is 5438; newer live-history entries are not assumed installed.

## Answer and ownership path

**YES**, KSA has an equivalent work-placement concept: owner-prepared staged buffers,
lazy successor construction, prepared nozzle/disturbance inputs, retained physics, ready/apply.
It is **not** a sealed immutable NovaCore-style authority packet.

| Responsibility | Current method / metadata token | Actual behavior and lifetime |
|---|---|---|
| Input preparation | Vehicle.PrepareWorker `06002F2E`; VehicleUpdateState.PrepareFromVehicle `06001C50` | Manual/control input copied to worker; update preparation precedes use |
| Per-update owner | VehicleUpdateData.Prepare `06001BBC` | Copies origin/kinematics/flight computer; clears optional successors and prepares module updaters |
| Main phase | VehicleUpdateTask.Run `06003116` | BeginVehicleUpdate; bubble preparation/stepping/postwork/readiness; finally EndVehicleUpdate |
| Shape phase | ConstraintSim.BeginVehicleUpdate/EndVehicleUpdate `0600067B/0600067C` | Interlocked registry phase protects shape-update ownership, not an independent revision lock on every field |
| Ready lifetime | PhysicsBubble.Prepare `06001BC4`; BeginStepFrame `06001BE1` | Prepare resets ready/failure; stepping a still-ready bubble is rejected |
| Property reuse | GetNewProps/GetReadOnlyProps `06001C46/06001C47` | First writable request copies source properties; later requests return retained staged value by ref |
| Module reuse | StateUpdater.Prepare/GetNewStates/GetReadOnlyStates `06003F45/06003F38/06003F37` | Retained capacity, source copied on first write; next preparation resets the staged-state marker |
| Control/module work | FullPhysicsPreStep `06001C07`; FullPhysicsStepUntil `06001C02`; UpdateModulesJob.ExecuteOnBubble `06004140` | Flight-computer wake boundary then module/nozzle preparation before physical consumption |
| Propulsion dependencies | Rocket.UpdateRockets/RocketsNeedUpdate `06001513/06001515` | No-change exit depends on firing, resource/gimbal/controller/core changes; otherwise availability/core/nozzle update |
| Native-ready input | UpdateActiveNozzles `06001C59` | Retains force, COM moment, flow, cutoff time/core association until next module/nozzle update |
| Constrained step | FullPhysicsConstrainedStep `06001C19` | Prepares Disturbances[0] once before constrained native loop; resource consumption and mass recomputation afterward |
| Native properties | PopulateBepuStatesFromKinematic `06001C33`; UpdateSimFromVehicle `0600068A`; PushBodyStateToSim `06000688`; CopyToBepu `06001B40` | Reuses world/handle, uploads staged properties AND kinematics; this upload is not NovaCore's no-reimport acknowledgement |
| Solver callback | PoseIntegratorCallbacks.IntegrateVelocity `060006BD` | Body-handle lookup/mask checks remain; reads staged inverse properties/disturbance, no exact resource-to-mass reconstruction |
| Resource successor | ConsumePropellantFromActiveNozzles `06001C5B/06001B46`; RecomputeMassProperties `06001B43` | No active nozzles returns without recomputation; otherwise consumes resource then derives new properties |
| Ready/apply | PublishResults `06001BF8`; ApplyResultsToVehicles `06001BEA`; ApplyResultsToMainThread `06003115`; UpdateFromTaskResultsUnsynchronized `06002F31` | Failed bubble not ready; unready apply refused; staged fields applied then synchronized/render work |
| Failure | Simulate `0600069D`; MarkFailed `06001BF5`; TryResetForPool `06000683` | Timestep exception poisons/rethrows; first failure recorded; poisoned sim cannot reset for pool |

The source properties are stable through the current constrained loop, not globally immutable.
GetReadOnlyProps can refer to an already mutable staged successor. Module/resource changes
invalidate derived properties; next update preparation resets optional successors. Native
pose/velocity changes every solve. Ready/apply is owner state, not an exact resource lease.

Consumer validation remains in KSA. ThrusterControllerGlobalState cache methods
`06001590/06001591/06001593` check orientation/COM/mass/pressure using tolerances. Those
tolerances cannot replace NovaCore's exact authority. ActiveNozzle.ComputeThrustMod
`06001BA7` uses float remaining-time/dt clamping, not NovaCore exact exhaustion arithmetic.
An abstract TryPrepareDrain method is not evidence of a sealed proposal on this actual path.

## Actual live-changelog history

Authoritative available source: authenticated Discord, **Kitten Space Agency / #live-changelog**,
server `1260011486735241329`, channel `1260112103134724146`. Channel description identifies
it as main-branch engineering commits which appear in a following build.

| Revision / date / author | Exact message | Directly read meaning | Limit |
|---|---|---|---|
| 4549 / 2026-06-05 / gravhoek-rw | [1512355160775721020](https://discord.com/channels/1260011486735241329/1260112103134724146/1512355160775721020) | Reuse active-nozzle flow; compute derivatives and consume resource once per large update rather than each collision step | Does not establish NovaCore exact canonical interval authority |
| 5177 / 2026-08-05 / gravhoek-rw | [1534637795740750058](https://discord.com/channels/1260011486735241329/1260112103134724146/1534637795740750058) | Repeated jet-command execution caused overshoot/chatter/excess propellant, particularly in warp | Reuse must not replay commands or resource effects |
| 5434 / 2026-09-13 / JPLRepoRocketwerkz | [1548873113746411602](https://discord.com/channels/1260011486735241329/1260112103134724146/1548873113746411602) | Shared flight/planner drain, flat buffers and once-derived craft chains; fixed reachability/sharing/drain errors and double-dispose pooled-graph ownership | Correct ownership and dependency invalidation are part of reuse, not optional checks |

These current direct reads reconcile with the installed ordinary constrained method bodies.
No message grants authority to change NovaCore physics or remove checks. No broad history
research, messages, reactions, or remote modifications were performed.

## Classification before NovaCore delta

| Responsibility | Classification | NovaCore distinction |
|---|---|---|
| Derived source properties held through constrained solve | ADOPT | Already accepted source-mass policy, unchanged |
| Nozzle/disturbance preparation for actual consuming lifetime | ADAPT | Keep exact powered duration and deterministic projection |
| Lazy staged successor / no-change work placement | ADAPT | Existing owner, genuine source proof; no tolerance-based authority |
| Retained world and ready/apply | ADAPT | Single physical consumption plus exact atomic canonical successor and acknowledgement |
| Failure poisoning/suppress readiness | ADAPT | Preserve committed/private-invalidated distinction |
| Exact revisions/resources/events/debt/history | INTENTIONALLY DIFFER | Explicit NovaCore requirement not supplied by current KSA ready flag |

No NovaCore validation is declared redundant merely from this analogy.

## Reproduce direct method inspection

Use the actual matching DLL, not a stored decompile:

```powershell
Get-FileHash -LiteralPath 'E:\Kitten Space Agency\KSA.dll' -Algorithm SHA256
dotnet 'C:\Users\Tyler\AppData\Local\Temp\ilspycmd-11.0.0.9375\tools\net10.0\any\ilspycmd.dll' `
  --disable-updatecheck -m 0x06001C19 'E:\Kitten Space Agency\KSA.dll'
```

Substitute the recorded member token to inspect each current responsibility. No output
directory switch is needed. Independently checked decisive IL hashes:

| Token | IL bytes | SHA-256 |
|---|---:|---|
| 06001BBC | 212 | 840782E247ECAC203B147A11670305716EF6733147B50209DF77FB0FFC4E55D5 |
| 06001C46 | 85 | 387021B916E7571BF3CA997B374ACBFAC9B453FEB781E24D14D68956679D3ED8 |
| 06003F38 | 110 | CE4DC5FE272A94636EC580EBD95D50B0B7DFB9F8FCAEDAA467398C45C539E3A3 |
| 06001C19 | 1210 | 042293A6AED45AEB5EDBA6189A118B1787352503142BCCD718739E7DE34CAAE8 |
| 060006BD | 1729 | F2C76657B851D8B13423979C03513AB95419269A84303240DF2347ADE1CBA08D |
| 06001BF8 | 25 | 323EAE54B42A7BDC7F4A63D03E46D4CA51B25DEF5654BC32B2F5ADC95626974B |
| 0600069D | 106 | 2FB3153A787D195A0A066C4368F62FB40EBDD6FC086DB092E838DA2400A9BAA4 |
