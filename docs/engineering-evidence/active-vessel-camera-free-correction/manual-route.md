# Revised Project Control manual retest

**PENDING. FREE CAMERA: DEFERRED.** The prior route failed at HOME. This checklist replaces it; HOME is unbound and is not an acceptance step. Celestial focus is a separate capability.

Use the uninstrumented Release deployment from `prepare-launcher.ps1`; its hashes are recorded by `identity.json`. Open:

`E:\NovaCore\tools\NovaCore.Launcher\bin\Release\net10.0-windows\NovaCore.Launcher.exe`

1. Select **FL Launchpad → Play**. This remains `--scene=sol --focus=earth --surface-site=florida-launch --physical-surface=m12d-natural-candidate`.
2. Confirm SRV-01 is initially focused above the finite Florida support slab, with engine and RCS off.
3. Drag with the existing mouse gesture to orbit SRV-01. It should remain the view target; ground exclusion can constrain the eye while preserving aim.
4. Wheel in/out. Focus should stay on SRV-01, without clipping, jitter or loss of local context.
5. Select **4 Earth, 5 Moon, 1 Sun, 6 Mars**. Confirm the existing celestial orbit/zoom behavior.
6. Press **F**. SRV-01 and the retained vessel orbit distance/direction should return, with Earth as its environment.
7. Repeat celestial → F transitions, including after the support episode is held.
8. Verify these camera actions never ignite, reset, rotate, consume fuel or otherwise command/move SRV-01. Ordinary simulation remains independently owned.
9. Observe the existing bounded support episode complete normally at its held 20-physical-second endpoint. Record PASS or the exact failed step against the deployed identity.

The sample deployment is `E:\NovaCore\samples\NovaCore.Triangle\bin\Release\net10.0\NovaCore.Triangle.exe`. Diagnostic observer builds are not used for acceptance. No new engine, departure, launch-stack, SAS or navball route is opened. Engineering results do not substitute for this player retest. Stop for Project Control afterward.
