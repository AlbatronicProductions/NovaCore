# Manual retest — pending Project Control

The old white qualification slab and full facility acceptances do not apply. No manual PASS is claimed.

## Existing GUI entry

PowerShell 7:

~~~powershell
& 'E:\NovaCore\docs\engineering-evidence\srv01-surface-to-flight-gauntlet\stage5\simplified-slab\launch.ps1' Launcher
~~~

Select **FL Launchpad -> Play**. This runs the current uninstrumented Release sample from its normal launcher path, with **--scene=sol --focus=earth --surface-site=florida-launch --physical-surface=m12d-natural-candidate** plus existing window/diagnostic settings.

Expect the Solar world, authenticated Florida terrain, **one bottom slab**, and stock SRV-01 supported centrally on its top. There must be no elevated cube, blue/orange arms, duplicate proxy floor, or ground-placed craft. The top is the authored base/footing union, not a visual relocation independent of contact.

The bounded contact owner automatically starts after preparation. Main/RCS remain OFF; stores stay at 30 kg fuel and 45 kg oxidizer. At 1,200 intervals it holds the copied endpoint. **The GUI does not close or become the manual harness.** Solar camera/planet browsing remains available:

- Existing 1–0 focus selection; switch away and back.
- R for Solar map/home and wheel for continuous zoom.
- Existing mouse orbit/surface controls, E surface-relative attach/detach and WASD navigation.
- Existing Solar pause/rate presentation controls retain their old responsibility; they do not authorize spacecraft warp or contact clock mutation.

Check coherent support/placement and no sinking, floating or lateral offset. Camera and planet changes must not alter the physical result. Current Solar lighting depends on its current epoch; the manual view below has fixed J2000 lighting. Do not treat the Solar display clock as the spacecraft's exact episode clock.

## Bounded manual harness

~~~powershell
& 'E:\NovaCore\docs\engineering-evidence\srv01-surface-to-flight-gauntlet\stage5\simplified-slab\launch.ps1' Manual
~~~

Same canonical site/slab/spacecraft construction, deterministic J2000 start. Wait for **READY**, then press **Space**. Inspect the one slab, centred contact, stock shape, OFF actuators, unchanged resources, stable 20-second episode and final held endpoint. Camera uses mouse look/WASD/QE, R resets. Brief title-bar delay may pause display messages; release should recover with bounded retained-debt servicing.

This route holds celestial display/lighting at READY and remains a bounded qualification consumer. It is not the GUI Solar runtime.

Both routes use checked binaries listed in manual-binary-seals.json. No benchmark auto-close flag or timing observer is present. Close windows after inspection. Report each route's PASS/FAIL or specific defect, including focus/zoom/overview on the GUI. Stage 6 remains CLOSED regardless of automated results.

If reviewed outputs have been removed, rebuild with build.ps1 Release and prepare-launcher.ps1. A changed build requires refreshed binary identity qualification; do not silently bypass the retained seals.
