# One bounded Stage 5 recovery cycle

Authority: Project Control's b093f681 recovery instruction. Production source is sealed by the preceding identity.json. No production change is planned. Accepted KSA identity remains unchanged; reuse the accepted lifecycle evidence.

First observe the SAME Sky press_key mechanism for W and Q in READY. A diagnostic copy records actual WM_KEYDOWN/WM_KEYUP receipt, before/after held bits, monotonic wall ticks, and actual NcHostEvent frame numbers. A fixed native ring (8,192 records) flushes after window destruction; it adds no sleeps, event injection, sampling changes or synchronous per-frame output. Managed observation uses a fixed 8,192-snapshot buffer and cold shutdown serialization. Overflow fails qualification. The production input handler, frame construction, admission, service and renderer remain the same code; generated source diffs and hashes are retained.

The external diagnostic-only C# driver uses Windows SendInput with distinct down/up calls, verifies insertion counts and exact candidate foreground process, logs each action, and releases injected keys in finally. Its own focus-sink window supplies real focus loss. It is never a runtime dependency. The user explicitly authorizes this separate helper; it replaces the prior tap-only automation limitation without changing gameplay semantics.

Predeclared schedule (milliseconds from first Z, independent of results):

- Each W/S/A/D/Q/E: Z at 0; axis down at 150; axis up at 500 (**350 ms hold**); X at 900. At least two held native snapshots required.
- WA, WQ, AQ, WAQ: Z at 0; X at 100; chord down at 150; chord up at 500 (**350 ms hold**). This exercises the physical RCS union after main cutoff.
- WS, AD, QE: Z at 0; X at 100; first key down at 150; opposite down at 300; opposite up at 650 (**350 ms opposition**); first up at 1000. Cancellation and remaining-key restoration must be observed.
- Focus W and WAQ: Z at 0; chord down at 150; focus sink at 500; return candidate at 700 while the physical keys remain down; chord up at 900; fresh chord down at 1000; up at 1350; X at 1450. Require held before loss, neutral during loss and return, then fresh demand only after fresh down.
- Celestial Earth and Moon: focus selection at READY with a 100 ms digit hold/release, then normal W/WAQ main-ON schedule; F held 700–800. View changes must leave controlled identity and demand authority intact.
- Every finite run observes terminal after at least 2 seconds, then injects Z once to verify no restart. Close the owned candidate after collecting the terminal snapshot. READY tap probes close without ignition.

Windows scheduling jitter is recorded, not adjusted after results. Missing the planned boundary or fewer than two held frames invalidates that trial; it cannot be relabeled as success. No physical episode exceeds the existing 128 intervals/2 seconds. Helper timeout is a harness failure, not permission to extend physics.

Physical validation restores the actual captured canonical save and reads all 128 history records. Reuse the independent Stage 4 named-nozzle/quaternion oracle for masks, force, COM torque, finite slew and unique-jet exact debit. Check signed angular response, zero/remaining demand at release, and momentum continuity without direct state writes. Replay the captured admitted stream with different host-credit fragmentation and compare canonical history/state. No synthetic request is labeled native evidence.

Stage 5 promotes only after all named runs, focus/control separation, focused Z/X/replay/allocation regressions and independent 22-attack review pass. Otherwise return to Project Control. Stage 6 remains unopened until that decision.
