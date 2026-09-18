# Stage 1 manual gate — PENDING

No previous box, free-flight or SRV exhaust acceptance covers this new supported route.
Stage 2 is NOT OPEN. This ticket stops for Project Control here.

In PowerShell 7:

```powershell
& 'E:\NovaCore\docs\engineering-evidence\srv01-surface-to-flight-gauntlet\stage1\drift-closure\launch.ps1'
```

Exact executable: `E:\NovaCore\build\srv01-drift-closure\qualification\bin\NovaCore.Triangle\release\NovaCore.Triangle.exe`

Exact scenario: `--scene=srv01-supported-contact`. No benchmark/autostart flag.
The scene prepares before READY. Press **Space once** to start 20 simulated seconds.
Use **WASD/QE** for camera translation, **hold LMB or RMB and drag** to look,
**wheel** for camera speed,
and **R** to reset the camera. Close the window after inspecting COMPLETED.
Relaunch is an explicit cold restart; Space does not restart a completed world.

Check the following and return PASS or the specific defect:

1. READY, RUNNING and COMPLETED/HELD ENDPOINT are readable; READY has no elapsed debt.
2. The actual seven-part SRV-01 stands upright on the planar slab at its aft bell.
   Its collider is the declared eight-box conservative envelope, **not the exact
   curved/hollow visual surface**. The aft box has a square .52 m footprint while
   the rendered bell mouth is circular. Report a misleading visible mismatch.
3. Stable support, no visible lateral slide, COM jump, explosion, penetration,
   disappearance, or reconstruction. Sub-micrometre measured settling/drift is
   expected to look stationary; visible motion is not required for this OFF case.
4. Main and all RCS remain OFF. No plume. Fuel remains30 kg, oxidizer45 kg,
   total mass705 kg. No powered episode, liftoff, departure, or save/load support.
5. Move the camera; physics must remain unchanged. Briefly hold the title bar
   during RUNNING; note approximate timing/duration and bounded catch-up afterward.
6. Interval1200 completes at20,000,000 canonical ticks and holds the last copied
   endpoint. No motion extrapolation after completion. No lost backlog.
7. Observe startup/first service specifically: automated live runs recorded
   51.7–63.0 ms maximum contact-service callbacks and53.9–64.6 ms display callback
   intervals. Cause of each live maximum is unassigned. Independent cold first
   physical operations cost50.3–60.2 ms. These are disclosed limitations, not a
   claim that every frame met6.67 ms. Report whether the visible hitch is acceptable.

The route uses retained canonical endpoints only. Camera changes are presentation
only. The supported domain is a single upright, unpowered, finite episode on the
bounded slab; no Earth terrain or free-flight transition is implemented here.
