# Project Control integrated manual acceptance

Status: **PASS - explicitly reported by Project Control**, across repeated fresh sessions. See manual-acceptance.json. The terminal endpoint hold after 128 intervals / 2 seconds is accepted harness behavior, not an X-induced loss of momentum. Instructions below are retained for reproduction. Use the isolated Release candidate; the accepted Triangle/Launcher deployment still contains M15.4.

From E:\NovaCore, run:
    pwsh -NoProfile -File docs/engineering-evidence/player-flight-controls-gauntlet/launch-player-controls.ps1

This launches the normal production candidate without observer overlays or console tracing into the one approved SRV free-flight proving route. It is not FL Launchpad. The physical bound remains128 intervals/2 seconds at1x. READY and the terminal camera view can be inspected without rushing; flight cannot be extended or restarted with Z.

1. Leave READY untouched for several seconds. Resources, vessel pose and physical window must remain unchanged. Inspect SRV follow, mouse drag orbit, wheel zoom, Earth/Moon focus, and F back to the same SRV. HOME stays unbound; FREE deferred.
2. Return to SRV. Hold WAQ, then press Z. Confirm realized main/exhaust, decreasing resources and coherent combined pitch/yaw/roll. Release WAQ after a short hold; new manual demand must clear while physical angular motion continues.
3. Press X promptly within the two-second episode, then briefly hold SDE. Confirm main/exhaust cutoff, continuing motion and opposite demand through physical RCS. Release all controls. No transform snap, velocity reset or stuck input is acceptable.
4. Confirm the terminal state remains fixed under another Z. Continue orbit/zoom and celestial/F inspection there; camera changes cannot retarget control or renew physics.
5. Repeat this same finite route as needed to inspect individual W/S pitch, A/D yaw and Q/E roll, chords, focus loss/return, or celestial-view control. Each repetition requires closing and relaunching a fresh candidate; never extend the current lifetime. For view/control separation, select Earth before ignition, briefly hold D during the episode, then F. For focus clearing, lose application focus during a hold, release while away, return and verify neutral until fresh keydown; engine latch is independent.

The automated integrated witness fits orbit/zoom, celestial focus, D and F inside one finite episode. Manual repetitions make each visual observation practical without changing the physical contract. Project Control has reported manual PASS; engineering automation was not substituted for acceptance. Accepted UNBANKED and ready for Project Control banking consideration; no milestone, commit, tag, push, banking or next front is authorized here.