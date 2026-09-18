# Active-vessel camera: failed HOME correction

Project Control reported **manual acceptance FAILED at Step 5 HOME**. Core vessel focus/orbit/zoom/celestial/refocus had passed the observations made before that step. The prior engineering pass is not a manual acceptance.

Selected outcome: **FREE CAMERA DEFERRED**. Reason: requires broader camera/navigation-frame architecture. HOME is unbound and the incomplete transition is removed. Core camera behavior remains the front's acceptance scope. Milestone NOT ASSIGNED; banking NOT AUTHORIZED.

- [Current KSA, authenticated live history, exact failure and frame decision](architecture-decision.md)
- [Implementation, ownership, bindings and permanent test contract](implementation.md)
- [Revised manual retest](manual-route.md)
- `home-failure.json`: pre-correction numerical trace, including first complete display frame.
- `entry-identity.json`: failing source, pre-existing evidence and Git identity seal.
- `build-candidate.ps1`, `validate.ps1`, `prepare-launcher.ps1`: reproducible correction build/check/deployment.

Engineering: **PASS**. Judgment: **PASS — FREE CAMERA DEFERRED; CORE ACTIVE-VESSEL CAMERA READY FOR MANUAL RETEST**. Manual retest remains **PENDING**.

Debug/Release builds passed with zero warnings/errors; all 94 regression gates passed. The stronger final F/reserved-bit test passed both configurations. Warmed follow/F allocation is exactly 0 B. Six complete renderer captures preserve matched Solar performance within the disclosed limits; [performance disposition](performance.md), [independent 20-attack review](verification.md), and `validation.json` retain tails and incomplete capture attempts. Uninstrumented deployment passed a 480-frame smoke run with exit 0 and empty stderr.

The old camera evidence remains historical with explicit supersession notices. The earlier first-playable-launch review is untouched. Temporary sources, logs and runtime outputs remain under `build/active-vessel-camera-free-correction`; no automatic cleanup. `identity.json` records final source, runtime, Git and remote seals.

Stop for Project Control. Do not open engine controls, departure, launch stack, SAS or navball.
