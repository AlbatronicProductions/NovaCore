> SUPERSEDED after Project Control manual acceptance FAILED at Step 5 HOME. FREE is now DEFERRED; HOME is unbound. This file records the prior candidate, not current acceptance. Use [the correction package](../active-vessel-camera-free-correction/README.md) and [revised manual route](../active-vessel-camera-free-correction/manual-route.md).

# Project Control manual route

Required: **YES**. Status: **PENDING**. Engineering tests cannot supply this acceptance.

After `prepare-launcher.ps1`, open the existing launcher:

`E:\NovaCore\tools\NovaCore.Launcher\bin\Release\net10.0-windows\NovaCore.Launcher.exe`

Select **FL Launchpad → Play**. The launcher still resolves the existing route:

```text
--scene=sol --focus=earth --surface-site=florida-launch --physical-surface=m12d-natural-candidate
```

The normal sample at `E:\NovaCore\samples\NovaCore.Triangle\bin\Release\net10.0\NovaCore.Triangle.exe` is the uninstrumented candidate. The performance overlay is not used here.

1. Confirm stock SRV-01 is visible, centered above the finite Florida support slab, engine/RCS off. Its initial camera focus is the vessel.
2. Press **F**, drag with the existing mouse camera gesture, and wheel in/out. The view should remain aimed at the craft. At ground-obstructed angles, terrain exclusion may move the eye while preserving aim.
3. Press **4** for Earth, **5** for Moon, **1** for Sun, or **6** for Mars. Existing Solar drag/zoom/navigation should remain available.
4. Press **F**. The vessel target and prior vessel viewing distance/orientation should return.
5. Press **Home**. Use **WASD/QE**, mouse look, and wheel speed. The camera should move independently while SRV remains the active unchanged spacecraft.
6. Press **F** again. Repeat celestial/free/refocus transitions and inspect for clipping, jitter, jumps or lost terrain.
7. Observe the existing bounded contact episode reach its held endpoint after 20 physical seconds. No camera action ignites, consumes fuel, rotates, resets or launches the craft. Solar browsing time remains independently owned.
8. Report acceptance or the exact failed step. Record the deployed candidate identity in `identity.json`/deployment summary when accepting.

The isolated `srv01-florida-support` diagnostic is not the player camera route. The existing moving SRV proof is exposed as the permanent copied-observation test; no new moving Florida/launch route is exposed by this ticket.

The startup message mentioning previous support/presentation acceptance refers to the banked M15.3 support route, not acceptance of this camera change.
