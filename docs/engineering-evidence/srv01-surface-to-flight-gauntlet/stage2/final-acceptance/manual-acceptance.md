# Stage 2 powered-supported SRV-01 manual acceptance

Status: **PASS — Project Control reported on 2026-09-17** for this powered route. Exact response and saved canonical result are retained in [manual-result.json](manual-result.json). Prior Stage 1 or M15.2 approvals were not substituted. The checklist below is the route actually issued.

Known residual before launch: the client can appear white/unpresented before the scene arrives. Targeted captures measured about 197–215 ms and one **16.67-second** interval. Its deeper cause is unresolved. It is not fixed or harmless; Project Control has separated it from Stage-2 powered-physics acceptance. Wait for the actual scene and READY before starting.

PowerShell 7, explicit cold launch:

```powershell
& 'E:\NovaCore\docs\engineering-evidence\srv01-surface-to-flight-gauntlet\stage2\final-acceptance\launch.ps1'
```

This checks the 24 candidate seals and native source, prepares a plain Release build if absent, and launches `srv01-powered-support`. It does not use diagnostic instrumentation, benchmark auto-start or auto-close. Press **Space** at READY. The seven-part canonical SRV-01 runs 1,200 exact intervals / 20 simulated seconds on the bounded slab.

Check all of the following:

- Actual assembled SRV-01 visible with coherent connected geometry and support.
- Readable READY → RUNNING → COMPLETED/HELD ENDPOINT status.
- Main engine powered at the unchanged stock 600 N; RCS off; attached, correctly directed main exhaust where not occluded by the slab.
- Fuel and oxidizer decrease from 30/45 kg toward **28.4375/42.65625 kg** at completion. This episode does not exhaust fuel.
- Move the camera: no detached parts, physical/visual transform mismatch, or camera-driven physics.
- Briefly hold the title bar if practical; bounded catch-up and coherent retained support afterward. Report any issue rather than repeating until it disappears.
- Coherent held final endpoint with no live plume after completion.

Sub-weight thrust is intentional: the body should remain supported. Visible ascent, departure or dramatic motion is neither expected nor required. Do not retune anything to create it. Camera: WASD/QE, mouse look, R reset, as in the existing route.

Close after inspection to flush the log. Report PASS or the specific defect. Startup observations remain a separate residual; any new Stage-2-specific correctness/performance problem still stops the campaign. No automated/manual PASS is inferred from the window merely opening.

Final independent Stage-2 red team and Project Control manual acceptance both PASS. Stage-3 promotion may now be evaluated under its own hard gate; no implementation follows from visual PASS alone. UNBANKED; no milestone assigned.
