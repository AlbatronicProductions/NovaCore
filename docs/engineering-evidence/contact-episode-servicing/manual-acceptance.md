# Contact development visual acceptance

Follow-up acceptance: **PASS for centered and tilted**, explicitly reconfirmed by Project Control after the Unicode correction. READY, RUNNING and COMPLETED are all readable; physics, camera independence, brief title-bar delay recovery and final hold remain correct. READY waits for **Space** before starting host timing. The remaining issue is the [UNRESOLVED tail classification](status-tail-follow-up.md), not manual status acceptance. Historical acceptance below is preserved as provenance.

Status: **Manual acceptance PASS**, explicitly reported by Project Control on 2026-09-13 for both centered and tilted cases.

User's acceptance: "Centered: PASS. Tilted: PASS. Settling/support stable, camera movement did not affect physics, no teleport/explosion observed, brief frame delay recovered boundedly, and final held endpoint behaved correctly. Manual acceptance PASS."

The agent's separate live check found that the native window still displays only `N`, so the attempted scenario title/status update remains unqualified. See [candidate report](README.md). The behavioral acceptance above is retained; it is not treated as an implicit waiver of the visible-status requirement. No further manual rerun has been requested. The procedure below is retained for reproduction.

From PowerShell 7, run separately:

```powershell
& 'E:\NovaCore\docs\engineering-evidence\contact-episode-servicing\launch.ps1' centered
& 'E:\NovaCore\docs\engineering-evidence\contact-episode-servicing\launch.ps1' tilted
```

The launcher also offers **Development — Canonical Contact / Centered** and **Development — Canonical Contact / Tilted**. This is the qualification-only 2 × 1 × 1 m box on a 128 × 2 × 128 m slab, whose upper face is y=0. Neither object is a production spacecraft or facility.

Each launch is an explicit cold reset. Preparation completes with zero debt before native elapsed-host sampling starts. Observe the box fall, rotate in the tilted case, settle and remain supported. Use the existing camera controls (WASD/QE and mouse look); R resets only the camera. Camera movement must not change the physical trajectory. Briefly hold/drag the window title bar during the episode to exercise a delayed frame; release and check bounded catch-up and completion. This intentionally disturbed run is not an undisturbed frame-performance measurement.

After 20 simulated seconds / 1,200 exact intervals, the title must say **Completed — final canonical endpoint held** and the pose must remain fixed. A failure must remain explicit in the title/log while holding the last committed endpoint. There is no extrapolation or interpolation between endpoints. Relaunch to restart.

Report separately for centered and tilted: dimensions/pose credible; contact/support stable; no teleport/explosion/reconstruction; no avoidable creation hitch during continuation; camera independence; delayed-frame recovery; final hold. The console log records readiness, terminal frontier/clock/debt/revision/history, and callback-to-callback frame distribution. The launch script saves logs under the ignored `build/contact-episode-servicing-live` directory after the window closes.
