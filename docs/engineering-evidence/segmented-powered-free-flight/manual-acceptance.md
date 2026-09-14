# Manual Release acceptance

Run in PowerShell 7:

```powershell
& 'E:\NovaCore\docs\engineering-evidence\segmented-powered-free-flight\launch.ps1'
```

This is an explicitly authored dry-8 kg uniform development cube, side sqrt(1.5) m, spherical inertia (2,2,2), central point fuel 1/128 kg, and one ideal body-X 8 N engine. No contact/slab/gravity exists. The gray grid is presentation-only. The episode is eight seconds; the initial fuel funds approximately five seconds of thrust. Exact resource accounting determines depletion; the remaining interval is coast.

Verify readable READY before Space. After Space verify RUNNING, actual output and throttle, decreasing fuel, visible acceleration, then EnabledNoFeed/zero throttle and coasting after depletion. Move the camera; physical motion must remain unchanged. Briefly delay a frame using the title bar and verify bounded recovery. COMPLETED must hold the final copied canonical endpoint. Close the window to save the bounded log and report PASS or the specific defect.

Numerical/presentation tests cannot grant this manual result. Log frame timing includes display/driver/host scheduling and deliberate user pauses; it is separate from warmed physical-operation timing. Relaunch is an explicit cold reset.

## Accepted manual observation: 2026-09-14

The user / Project Control directly observed the existing Release route and explicitly replied **PASS** to the question covering all eight requested observations. This is human acceptance, not an inference from numerical tests or process telemetry.

| Observation | User result |
|---|---|
| Visible physical acceleration | PASS |
| Requested versus actual throttle/engine distinction | PASS |
| Canonical fuel decrease | PASS |
| Exhaustion followed by no-thrust coast | PASS |
| Camera independence | PASS |
| Final held endpoint | PASS |
| Readable/truthful READY/RUNNING/COMPLETED status | PASS |
| No visible motion/resource/actuator mismatch | PASS |

No discrepancy was reported. Process 58676 exited normally (0), with empty stderr. Release identity and exact log hashes are in [final qualification](final-qualification.json). The displayed throttle field is the copied endpoint's actual throttle; the route submits its fixed full-throttle command at Start. The user accepted the visible distinction, while the final source verifier separately reviews canonical authority.

Concise existing log witnesses (display values are formatted, not full-precision canonical fingerprints):

```text
READY - Space starts 8s free flight; no gravity/contact
RUNNING - command accepted; awaiting first exact interval
RUNNING | actual=ProducingOutput throttle=100% fuel=7.500 g t=0.20s v=0.200m/s
RUNNING | actual=ProducingOutput throttle=100% fuel=0.312 g t=4.80s v=4.798m/s
RUNNING | actual=EnabledNoFeed throttle=0% fuel=0.000 g t=5.00s v=4.998m/s
COMPLETED - final canonical endpoint held | actual=EnabledNoFeed throttle=0% fuel=0.000 g t=8.00s v=4.998m/s
POWERED_LIVE_END frontier=480 clock=8000000 debt=3995 revision=480 history=480
```

The finite episode retained the final host sample's 3,995 excess debt ticks and held the endpoint; no further interval was serviced after frontier 480. Existing telemetry reported cold preparation 19.3077 ms and 1,441 whole callback intervals: median 5.5555 ms, P95 5.7121, P99 6.0194, max 14.3217. These include present/host scheduling and are **report-only observations**, not a new performance acceptance gate. The user accepted physical/authority agreement; no presentation optimization was performed.
