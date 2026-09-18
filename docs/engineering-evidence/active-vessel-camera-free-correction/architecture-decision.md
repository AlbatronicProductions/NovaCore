# FREE camera correction: architecture gate

2026-09-18. Project Control's manual Step 5 failed. Engineering tests from the previous camera package did not qualify player acceptance. This decision precedes the production correction. FREE CAMERA: **DEFERRED**. No milestone or banking authority.

## Current KSA: complete ownership chain

Read-only independent inspection freshly decompiled the installed `E:/Kitten Space Agency/KSA.dll`, version `2026.9.10.5438+c9291b5dd3347e847020c8fb3dd11dfce6a728e9`, SHA-256 `A03E98153C6F353AF6F280CD3BDC1C71C718008E2049F508DC6FBE54457A0AA8`. The anchors below are line numbers from fresh `ilspycmd -t KSA.Type` output, not old cached revision 5402. Source wins over later announcements.

| Responsibility | Current owner / evidence |
|---|---|
| Controller / camera | GameViewport 30–42,87–98,284–311 owns one BaseCamera for Orbit/Fly/IVA/Fixed, plus separate MapCamera. SetCameraMode switches controller hooks; it neither unfollows nor reparents nor moves position. |
| View target / position parent | Camera.PositionCce 81–108 transforms LocalPosition through Following.GetBodyFixed2Ecl unless NoRotation. PositionEcl 110–130 adds followed position. Following supplies a position frame even when Fly has no look-at target. |
| Active vehicle | Camera.SetFollow 597–612 changes Program.ControlledVehicle only when requested. OrbitController.CanChangeControl 112 uses ViewportRegistry.IsMainCamera 97–100. |
| Environment | Camera.TryGetSurfaceClampPositionEcl 636–652 and Program.FindNearbyCelestial 5050–5052 select nearby celestial from eye position. Program 2633–2649 separately supplies camera.NearbyCelestial to rendering. |
| Body rotation / epoch | Celestial.GetBodyFixed2Ecl 174–176 returns rotating CCF→CCE; UpdatePerFrameData 589–603 evaluates published epoch and parent composition. Vehicle.GetBodyFixed2Ecl 1299–1301 returns Body2Cce. |
| Free orientation | FlyController.CacheOffset 865–876 saves inverse current Surface frame times local look. OnFrame 721–734 reapplies current celestial Surface frame times retained offset. The gameplay resolver uses this only for celestial following; vehicle-following Fly uses identity orientation frame, despite retaining vehicle-relative position. |
| Free translation | FlyController.OnFrame 661–697 combines camera forward/right/up, normalizes and scales by player duration and speed. Camera.Translate 186–195 inverse-transforms movement into the followed body's frame before storing LocalPosition. |
| Free speed / wheel | FlyController.GetCurrentSpeed 118–131 applies speed, multiplier, scroll exponent, sprint. OnScroll 484–493 changes speed exponent, not dolly distance. Constants and bindings are not imported. |
| Explicit detachment | Camera.Unfollow 615–625 preserves world position, clears following, restores position, optionally clears control. This is distinct from selecting Free. |
| Orbit / follow / zoom | OrbitController.OnFrame 487–663 resolves identity, retained OrbitView, COM/part offset, reference frame and smoothing. OnScroll 400–428 changes DistancePower; final distance uses target radius at 617. |
| Map / refocus | Separate MapCamera uses NoRotation; MapController.OnSwitchOn 571–601 and OnFrame 135–306 implement map pan/zoom and control toggle. Center 268–284 clears pan, then may follow PreviouslyControlledVehicle with changeControl:false. No universal F-equivalent is claimed. |
| Switching / detached frame changes | Controller switching preserves Following. SetFollow is deliberate focus, not a generic pose-preserving reparent. Orbit interpolates its frame changes. Arbitrary frame/target switching while Fly is not proven continuous by this inspection. |
| Retirement | Universe.DestroyVehicleFromEvent 1902–1924 hands view to debris/wreckage/parent without changing unrelated control; HandOffCameras 1962–1987 selects surviving neighbour or parent. Controlled ownership clears only for its destroyed owner at 1941–1944. |
| Surface→orbit | Vehicle.UpdateVehicleRegion 3062–3094 changes reference only if the existing reference equals the old region default. VehicleRegionEx maps Surface→Surface, LowOrbit→Orbit, HighOrbit→Parent. OrbitController interpolates changes. |
| Cadence / precision / origin | Program.PrepareFrame 2099–2114 applies completed publications/input; OnFrameViewports 2527–2541 runs display camera work. GameViewport.OnFrame 116–119 runs controller then camera preparation. Camera.GetPositionEgo 231–244 subtracts FP64 positions, with same-bubble path; Camera.OnFrame 482–490 creates orientation-only view matrices before float packing. |

World position = followed position + followed body rotation × local position. Celestial Fly orientation = current Surface rotation × retained look offset. This composes a frame; it does not cancel planetary motion.

Fresh IL witnesses: FlyController.OnFrame token 06000A2F SHA A4808205C84CDFED089C3B2BDA16F6338CA5FA249863EA9E84E0DC26B823482A; CacheOffset 06000A35 SHA 3BE32E28859783B482A08D203966173B948CA048A1B2C3584DC2D670FB3C3416; Camera.Translate 06000233 SHA C7B62B60A940F4F6CE6B3E6B13CB8154A265F8CAC93B84028AD91B3D2F1022C4; Unfollow 06000263 SHA C7488D78FC04BFECF8F430A7ED5FF313655D2747AD78B50E9165B078530EA990; GameViewport.SetCameraMode 060021B2 SHA 6A00932851D98C08C9174ED3F790238FA5BEEA1D716E125259A83D6E566DCD23; Vehicle.UpdateVehicleRegion 06002F53 SHA 77559B3A94CE1D7FCA42EACFD2E9086D8A220E92B4462750DC0F09075EC6EEE3.

## Authenticated live history

Read through the signed-in Discord browser, without posting or changing settings, on 2026-09-18. Searched `in:live-changelog camera`, `"free"`, `camera frame`, `floating`, and followed the previously identified routing/region messages. Current live channel observed through revision 5460; installed assembly remains revision 5438. Later announcements are not treated as installed code. Search results are scoped observations, not proof of exhaustive absence.

- [2025-07-07 surface following in Fly](https://discord.com/channels/1260011486735241329/1260112103134724146/1391637541051961426): celestial-following Free rotates with the planet surface; this was explicitly added for local video/screenshot use. This history directly explains the missing lifecycle responsibility.
- [4352, 2026-05-11 orientation correction](https://discord.com/channels/1260011486735241329/1260112103134724146/1503444159518740712): Free orientation was corrected, not reduced to stationary root coordinates.
- [4449, 2026-05-20 Fly movement](https://discord.com/channels/1260011486735241329/1260112103134724146/1506801601493602395): separate translate and pitch/yaw input confirms controller-specific movement semantics.
- [4340, 2026-05-10 relative precision](https://discord.com/channels/1260011486735241329/1260112103134724146/1502896282006454373): local floating physics origin, camera GetPositionEgo avoiding unnecessary ECL roundtrips, and local-common-frame transforms reduce distant judder.
- [2025-08-27 region transitions](https://discord.com/channels/1260011486735241329/1260112103134724146/1410180973274075199): preserve deliberate user reference choices when vehicle region changes.
- [5375, 2026-08-31 viewport routing](https://discord.com/channels/1260011486735241329/1260112103134724146/1544008206777323561): fixes secondary-camera control theft, control loss on another viewed craft's destruction, drag/input ownership, and per-viewport map/celestial state. Re-read in full, including surrounding history.
- [5369 retirement](https://discord.com/channels/1260011486735241329/1260112103134724146/1542677676798640140): destroyed-view handoff to surviving debris. [5359 disposed-target correction](https://discord.com/channels/1260011486735241329/1260112103134724146/1542009653645279314) establishes the preceding retirement issue.
- [5327 FP64 anchor correction](https://discord.com/channels/1260011486735241329/1260112103134724146/1539694656890077246): terrain anchors derive from double matrices before float packing.

Other observed camera search results: 5192 corrected docking guidance retaining a world-baked orientation; 5072 added COM interpolation; 5068 clarified coordinate-axis names; 5103 added home-body fallback when all vehicles disappear; 4987 corrected editor-only root-follow/drag conflict. The 2025-07-30 entry distinguished Orbit and Map astronomical double-click offsets and their frame rotation, and added faster zoom. The 2025-07-07 speed entry changed an impractical default; neither that number nor KSA bindings justify masking NovaCore's missing frame owner. Historical editor behaviors are not flight qualification.

## Cheap first proof and root cause

`home-failure.json` records actual current Solar input → display advance → copied focus refresh → DebugCameraInput/FreeCameraController → final terrain constraint → Solar update, using the Florida prepared canonical observation. No physical servicing occurs in this diagnostic; every sample verifies canonical observation equality. `entry-identity.json` seals failing sources; the disposable probe contains no production edit.

Before HOME: target SceneObject 201, active 201, environment Earth 6, FocusIndex 3, material O focus, root frame 1. Root eye (-26507847897.61519,132752305564.35219,57558844350.861435), 24 m from O. CameraMode.Free is a pre-existing low-level label even during Solar orbit; Solar's _freeCamera and target/authority fields actually select placement. Authority Inertial describes orbit attitude, while orbit still carries its target's translation.

HOME preserves that numeric eye/orientation immediately, sets _freeCamera, clears SurfaceRelative/anchor/look state, and falsely labels view BodyCenter(Earth). It skips orbit placement/publication carry. Environment/index stay Earth/3. No camera velocity is retained: the free controller integrates camera-local directional displacement in fixed root coordinates at default 0.1 m/s (maximum 64). Wheel changes speed, no longer target distance. Root eye is also the relative render origin. Translation basis and complete body-local eye/site/Earth transforms are captured in JSON.

After 16.667 ms Earth translates 504.7769476 m; site rotation alone contributes 6.7978604 m. Before final constraint, site distance grows to 489.9311048 m while root eye stays unchanged. A right input contributes only 0.0016675 m. The final terrain exclusion pushes the eye 401.6628620 m from its initial location, leaving site distance 268.3265087 m and near clip about 0.20002 m. After 10.0002 s, site distance is 171976.0554 m. Speed wheel reaches the unchanged 64 m/s maximum; it cannot restore local coherence. F restores the retained 24 m vessel view.

Thus the immediate key operation is numerically continuous, but the first complete display callback is not locally coherent. Missing Earth orbital translation dominates this sample; rotation also matters. A terrain constraint amplifies the apparent jump. This is not an FP32 transport collapse or physical vessel mutation. The previous test omitted the final constraint and incorrectly blessed an unchanged root eye after Solar advanced.

## Required frame and outcome

| Option | Evidence and outcome |
|---|---|
| A inertial | KSA explicit Unfollow, not its Free transition. Earth/site motion is expected. Fails requested local inspection. |
| B body-fixed | KSA celestial-following Fly supports it. Requires explicit environmental-body parent, entry pose rebasing and ongoing orientation/position evaluation. |
| C site/tangent | Existing NovaCore SurfaceCameraAuthority has FP64 attach/evaluate math; KSA has ENU orbital framing. Neither establishes the requested detached site 6DOF controller. NovaCore's current surface route is Earth-only, tangent/pitch-independent movement, E attach and wheel dolly, and claims SurfaceAnchor target. Reusing it unchanged violates NONE/FREE and input semantics. |
| D frame-preserving | Correct lifecycle principle: change controller while retaining an explicitly owned presentation frame. Near Florida that requires an Earth/site frame; retaining vessel tracking is not detachment, and retaining fixed root eye is not surface coherence. |

For a future surface-inspection FREE mode, inherit an explicitly defined Earth/site presentation frame and preserve world pose on entry (D with B or a qualified C). Position and orientation, frame changes, NONE/FREE target, environment lifetime, 6DOF input and refocus must have one coherent owner. Current NovaCore primitives do not supply that contract. Choosing or constructing it now would broaden this front.

Selected outcome: **DEFER**. Remove the newly added incoherent HOME bridge, including its private mode state/host dispatch and enum binding; preserve the existing FreeCameraController, DebugCameraInput, surface-camera authority, celestial navigator and all core vessel-camera functions. Do not change speed, planet motion, offsets or physics. Adopt KSA ownership separation and display/FP64 preparation; adapt to one canonical copied observation; intentionally defer FREE/map expansion and choose F independently of KSA bindings.

Additional authenticated searches for `dolly` and `refocus` returned no results. This is a terminology/search limit; current Orbit scroll and Map Center source owners above establish the implemented responsibilities.
