# Installed source provenance: initial snapshot and closing recheck

Both required evidence classes were available and directly inspected: actual installed binaries and authenticated retained Discord history. KSA is read-only. The initial readable/IL review below is build 5402. Final identity checking found a different current build 5438; its fresh decoded-method evidence is in ksa-current-lifecycle.md. The old source hashes/tokens below are NOT relabeled as current. Source-gate disposition depends on that recheck.

Initially inspected root: E:\Kitten Space Agency
Build: 2026.9.7.5402+487c3f340de24c6a81037120b6d1129c045c5400
Backend assembly versions: 2.5.0.0
Backend product: 2.5.0-beta.29+f73164bb3c9ca733eb3329f1f6b1cea4e216ece7

| Installed file | Bytes | SHA-256 |
|---|---:|---|
| KSA.dll | 4798552 | A8A5164204EED962DF9D9025E99F523D9A23EE2CE6E802B18216A9DA35506D2F |
| BepuPhysics.dll | 880640 | 77185E513BD530DEB322E41DD4ACDA9AD8E32E626CFA19492FB7481F25ED1FA7 |
| BepuUtilities.dll | 163328 | E0A1528DED4EF8EFCB8002B99704CB8614B7DB840D2CB39B5E45EDA52D63BC68 |
| KSA.deps.json | 73657 | FD627C9AE4D23928F4608BF91A72A75166806871D4C721F465E2A1208531A9FD |

Readable complete source root: E:\NovaCore\build\ksa-residency-reference\assembly-source\KSA. This is decompiled installed code, not possession of the private KSA development repository. Hashes of the 24 readable files used in this review are in identity.json. Both reviewers independently read installed IL with PEReader/MetadataReader; they did not execute KSA, rebuild/decompile a new tree or modify installed files.

## New lifecycle claim → installed method evidence

All metadata tokens in this file belong to the initially inspected 5402 KSA.dll above. IL offsets are hexadecimal. IL SHA-256 hashes are of GetILBytes(), not the full PE method-body header. Current 5438 tokens/operand resolutions are separately retained in ksa-current-lifecycle.md.

| Responsibility | Type.Method / token | Decisive installed IL |
|---|---|---|
| Flight-computer realization | FlightComputer.ComputeControl 06000943; CommandEngineThrottles 06000944 | Manual engine/throttle → command fields |
| Module/nozzle preparation | PhysicsBubble.UpdateModulesJob.ExecuteOnBubble 06003FC1; VehicleUpdateState.UpdateModules 06001BCB; UpdateActiveNozzles 06001BCC | Module updates then active nozzle force/torque/flow |
| Availability | Combustor.ComputePropellantAvailable 060014E4 | 000E ResourceAvailable |
| Reactant availability | ResourceManager.FindReactantsNodeGroup 06002448 | 0078 TryGetMoleAndState; 008F Mass, positive comparison; no interval demand parameter |
| Liquid conditions | Rocket.UpdateRockets 060014BE; RocketCore.UpdateState 060014CE | availability 0124/015D; engine update 0389; unavailable core zeros realized state; core conditions 0211 |
| Nozzle conditions | RocketNozzle.UpdateState 06001502 | 0042 ComputeConditions; 004C ComputePerformance; 0095 GetActualThrustTime |
| Command fraction | ActiveNozzle.ComputeThrustMod 06001B21 | subtraction 0008; division 000C; conv.r4 000D; clamp 0018 |
| Physics input ordering | PhysicsBubble.FullPhysicsStepUntil 06001B75 | BEPU population 07F9; module job creation 0912; constrained 095E |
| Outer/inner step ordering | PhysicsBubble.FullPhysicsConstrainedStep 06001B8C | derivatives 009D; Simulate 0296; ReadBack 02D6; RecordTimestep 031E; consume 0364; UpdateVehicle 0388 |
| Native dt and poison | ConstraintSim.Simulate 06000696 | Timestep 002F; exception poison write 003C |
| Callback input cadence | PoseIntegratorCallbacks.IntegrateVelocity 060006B4 | disturbances 00AE; staged inverse mass 00C4/inertia 00EA; dt factors 011D/013B |
| Mass upload | VehicleProperties.CopyToBepu 06001ABA | local inertia 0002; inverse mass write 000F; tensor 0025 |
| Nominal requested drain | VehicleProperties.ConsumePropellantFromActiveNozzles 06001AC0 | modifier 002F; core consumption 0060; recompute 007E |
| Ignored shortage feedback | Combustor.ConsumePropellant 060014E5 | MassChange 000B; pop 0010; ret 0011; actual-amount local unused |
| Bounded actual consumption | ResourceManager.ConsumeNodeGroup 06002440; Mole.ConsumeStored 0600144B | new state 004B / ConsumeStored 006A; insufficient mole path stores zero 0044 and returns prior amount |
| Solid cutoff | SolidMotor.UpdateState 06001576 | remaining mass 007A; previous flow 00C6; threshold Max 00DA; duration 0195 |
| Copy-on-write resource/mass | VehicleUpdateState.GetNewMoleStates 06001BB8 / GetNewProps 06001BB9; StateUpdater.GetNewStates 06003DED | new mole buffer 0022; read-only props 0027/new props 0039; old state copied 0048 |
| Staged drain caller | VehicleUpdateState.ConsumePropellantFromActiveNozzles 06001BCE | new moles 001D; new props 0024; drain 0038 |
| Mass recompute | VehicleProperties.RecomputeMassProperties 06001ABD | actual propellant properties 0003; inverse properties 002C; propellant mass 0042 |
| Live application | Vehicle.UpdateFromTaskResultsUnsynchronized 06002E4C | kinematics 0036; props 0062; flight-computer copy 039E; parts application 03AB; no exception region/rollback in method |
| Module state installation | StateList.UpdateBulkStates 06003DD1 | staged-to-destination copy 00EA |

Initial5402 source line anchors and bounded snapshot conclusions are in ksa-5402-snapshot.md. Current5438 resolved-method evidence is in ksa-current-lifecycle.md. Earlier accepted retained-cache/feature/friction facts are reused from the prior paired-response review; no fresh exact-event capability is inferred from them.

## Core IL hash witnesses

| Method token | IL SHA-256 |
|---|---|
| 06001B8C | A56AEAA88060E844CE7CF5995D5A38E5C76A96AA009D5FC7A84962846DA1C29A |
| 06000696 | 6FB84DEC13002F937BA3DF8B21B9AA9936821397FAA793F15366E46D390D2C3D |
| 06001B75 | FE37C794B3FBAA416B9E5F8AAA6FD763081DD3B34C02B49334A5CEF4EB4B4B26 |
| 060006B4 | 226668F4C35A39EACB75892AF9098CA5F5702C8BFED6444A0299D2AAD3AEFC90 |
| 06002E4C | 1781B6D6AA32BC6E3F49972F46CCB85842061D4723846EE8C120C4753AE35FAA |
| 060014E4 | 1115CD061769866995CA44484D1CF000CA2F86E38C94E9D56FC707287012DFB5 |
| 060014E5 | 877D4A9D8493160DAB6875AC4974626E6EE7C2472B6217CD4F73A31131F76061 |
| 06002448 | 2FC11CBD12E951C4FAEA613FD239F70FA7770D0CCB0D0A05CF54BDE30888DD6C |
| 06002440 | 09BA943AF784C603BBF2D82A327D475C63F8FF554AC0CEA9EEC5B7089F0A3B41 |
| 0600144B | 18C8C64E85CDD2E362209A4677F67D76806D23F63F3863D83328D48C337A50F9 |
| 060014CE | 37FDA105C4BDE5A009B52BCE3AD37646C5B86956A488A3DAAA02342406B01CE2 |
| 06001576 | E13AFFA3636DF8917926080D9C41B23AF0B56136C55C7CDE762F4342C0D6FA9B |
| 060014BE | 7B2FC5E3273F92CF73738B40E6D742B1E58107BB35BE6896454C04D5D0E8F07D |
| 06001502 | 4F8AD6A0553272067841E90A49165D5BDB4D1BAD5988EA32A521ADF2A5B87180 |
| 06001B21 | 6E445BBD2F46531C70448E4B111BA57D18B1AD6A99C958851CEFEB148FA25D56 |
| 06001AC0 | BA7E1C2947DBD4C648E0D288878110087FD2AD4DF96CC3BB3111CD5DBD14B707 |
| 06001ABD | 049B48CFF78A04E8DD5FF3ED2BE37B68418204D5233E6A96614EDD0530312B75 |
| 06001BB8 | 9610F4A925DBC08461D4294DF22F853BB0B356763B145440937991745FDA279B |
| 06001BB9 | 752830B7A5E5E7E81EED83EF3971AD924558870D848B37A64456FDA952AB3090 |
| 06001BCE | A99134A044055CC17BC03C81CE87F004B2FDE7F228E045DCC287B5006A8359FB |
| 06003DED | CD1854889EDE6E31265B9723E314C8583B961C030535B54753D2F018A8141899 |
| 06003DD1 | 0C5C1EC508DD58EE834C16D1084277611CD6D350E99F2E038F740A1D527D37E0 |

No raw IL or bulk decompilation is retained in this package. Exact installed bytes, method identities, source hashes and concise decisive offsets make the review reproducible. If a later KSA build differs, redo only affected claims; do not relabel current evidence as newer behavior.
