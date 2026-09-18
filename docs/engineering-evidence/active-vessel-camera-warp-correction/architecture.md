# Time-warp camera correction — unbanked

The entry trace (`warp-entry.json`, 600 display callbacks at each real supported rate) isolates presentation: the canonical prepared SRV observation is held unchanged. Root eye offset and root aiming orientation remain effectively fixed while Earth rotates. At 86400x local azimuth drifts 2.03145 rad, horizon vector changes 1.97198, and terrain exclusion displaces the eye by up to 23.2684 m relative to the original distance. Following material O alone does not follow its frame. Physics/support/rotation/time warp are not the cause.

## Current KSA authority and targeted authenticated history

Installed `E:\Kitten Space Agency\KSA.dll`: version `2026.9.10.5438+c9291b5dd3347e847020c8fb3dd11dfce6a728e9`, SHA256 `A03E98153C6F353AF6F280CD3BDC1C71C718008E2049F508DC6FBE54457A0AA8`. Fresh read-only ILSpy type extraction by the independent source verifier; line anchors refer to that type's extraction, not upstream source lines.

- `OrbitView` 5–11 stores reference frame, azimuth, elevation and distance. `OrbitController.GetFrame2Ecl` 233–270 resolves Surface from vehicle ENU, Orbit from LVLH, Parent from parent carousel, Stars from identity, Chase from vehicle attitude. These are distinct choices.
- `Vehicle.GetEnu2Cce` 3140–3162 derives up from current planet-relative position, east from pole cross position, north from up cross east. Surface view follows local horizon, not spacecraft attitude. Degenerate ENU returns null; controller retains last valid frame. This does not prove pole continuity.
- `OrbitController.OnFrame` 614–622 composes current reference basis with retained view angles; 649–652 publishes orientation and eye offset. Camera position storage (`Camera.PositionCce` 81–108, `PositionEcl` 110–130) is distinct from orbit orientation authority; the misleading LocalRotation name stores world orientation here.
- `Program.PrepareFrame` 2099–2104 applies completed workers; `Universe.ApplyOrbitSolvers` 1818–1832 and `ApplyVehicleSolvers` 1860–1863 publish completed state. `Celestial.UpdatePerFrameData` 589–603 uses state time. `Program.OnFrame` 2187 / `OnFrameViewports` 2527–2541 and `GameViewport.OnFrame` 116–119 update controller per display using player dt. No per-simulation-tick camera loop.
- `Vehicle.UpdateVehicleRegion` 3062–3094 owns region changes (parent/radius/atmosphere/periapsis, not contact boolean), changing the default camera frame only when the previous default remained selected. `VehicleRegionEx` 39–66 defaults Surface→Surface, LowOrbit→Orbit, HighOrbit→Parent.
- `OrbitController` 543–599 snapshots world pose on target/frame changes; 629–643 animates into the new frame. This is **not** evidence of exact angle rebase; NovaCore's no-jump requirement is stronger.

Authenticated live-changelog refreshed after the user restored sign-in on 2026-09-18. Targeted queries: `camera rotation` (10 results), `camera warp` (zero), `"reference frame"` (28 results, first page read); no broad archaeology. Relevant message anchors:

- [2025-08-27 region changes preserve user frame choices](https://discord.com/channels/1260011486735241329/1260112103134724146/1410180973274075199), directly revisited after sign-in.
- [2025-07-30 orbit frame rotation during astronomical focus changes](https://discord.com/channels/1260011486735241329/1260112103134724146/1400325421995659265); proposed future counterrotation in that message is intent, not current implementation proof.
- [2025-05-12 selectable orbit reference frames](https://discord.com/channels/1260011486735241329/1260112103134724146/1371684019342803120).
- [2025-02-21 planet rotation prescribed by time](https://discord.com/channels/1260011486735241329/1260112103134724146/1342405175884648448).
- [Revision 5068, camera vector frame naming](https://discord.com/channels/1260011486735241329/1260112103134724146/1531802587618213909).

Current source wins. History explains the separation of target, reference frame and time-driven rotation; it supplies no independent quantitative warp-performance claim.

## Bounded decision

**ADOPT:** target translation, selected reference basis, environmental body and player view are distinct responsibilities; reconstruct from current frame at display cadence. Surface view uses local horizon, not vessel attitude.

**ADAPT:** copied FP64 observation carries a fixed local basis in a named parent body's rotating frame (or presentation-root basis), plus canonical identity/generation, environmental body, O, physical epoch and display epoch. Solar resolves parent rotation from the same presentation snapshot. Existing Florida publisher uses its accepted site basis; Solar contains no Florida coordinates or Earth-specific rotation branch. Camera-relative subtraction and GPU transport remain unchanged.

The existing Solar orbit owner retains a full local quaternion and distance. Mouse yaw is about reference up, pitch about camera right; zoom changes distance. World eye is `O + Rframe * Qview * Z * distance`. Minimal-swing constraint aiming retains roll; the terrain constraint cannot be used as the horizon-stability oracle. Production input records demand, and copied refresh reconstructs once per display callback. Standalone input consumers can still request immediate realization.

A reference descriptor change is an explicit publisher-owned transition effective at one coherent display-boundary sample. Evaluate BOTH old and new frame rotations at that new epoch, then `Qnew = inverse(Rnew) * Rold * Qold`; rebase retained view even while viewing a celestial. Target translation is independent. Actual departure classification/publishing is downstream; no powered departure or frame menu is added. Full quaternion retains roll through arbitrary changes. Unknown parents, invalid rotations, stale display epoch/root/lifetime/publication fail to the existing environmental focus rather than inventing a transform.

**INTENTIONALLY DIFFER:** no automatic region classification or animated steering to a new default basis in this ticket. The user explicitly defers departure and requires pose-preserving transition; current KSA's animated default change is not copied. HOME remains unbound and FREE deferred.

Independent architecture review found this representable within the bounded current owner; final implementation still requires tests, performance and all 18 red-team attacks. No manual acceptance, milestone or banking is implied.
