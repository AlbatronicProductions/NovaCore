# Native witness — recorder integrity stop

**REVISE / STOP FOR PROJECT CONTROL. No relaunch.** One canonical player session
ran on 2026-09-27, process 47272, and exited normally with code 0. Mandatory
recorder recovery exposed overflow; no recorder architecture or code was changed
to rescue the result.

Session: `5612981f-e989-46d6-b9d5-1655112a9697`.
Recorder opened `2026-09-27T08:06:02.8706552Z`; process started 08:05:46 UTC;
shutdown completed approximately 08:17:12 UTC. 64,747 rendered frames.
Window: 3440×1440 borderless; normal UI chrome left an actual 3440×1322 game
render viewport. This is not a 3440×1440 render-extent claim.

## Actual route

Startup acknowledged three historical ambiguous sessions, then entered Solar
overview. Native dialog automation required keyboard navigation; the UI metadata
did not reliably expose the dialogs. That prolonged the overview/menu period.

Normal player Load flight opened the CPU-qualified short descending checkpoint.
The craft visibly settled feet-first on the pad and remained in continuing
`SURFACE CONTACT`. Normal Save flight wrote
`build/surface-recontact/native-short-grounded.ncflight.json`; loading that exact
file restored continuing pad contact. The long descending input also remained
upright in pad contact. The terrain input continued with contact telemetry and
near-rest velocity, but its visible ground was dark. Construction opened, then
Return to flight resumed that terrain scene. Normal Quit ended the process.

All four retained `MODULAR_FLIGHT_END` records report `failed=False reason=none`.
There was no live engine/RCS relaunch exercise. The session therefore does not
prove the entire live launch/contact/relaunch loop. Terrain loaded epoch zero;
pad inputs used epoch 18,000,000,000 microseconds. Lighting epochs differ, but
this is only a possible explanation of the dark view, not a causal conclusion.
It is neither positive visual terrain-contact qualification nor proven blackout.

## Durable evidence

| Field | Result |
|---|---:|
| Produced observed / durable / committed target | 1,230,976 / 1,230,976 / 1,230,976 |
| Checkpoint sequence | 1,217,824 |
| Corrupt | false |
| Faults | 65 = Overflow (1) + Protocol (64) |
| Dropped events | 3,383 |
| Unmatched operation ledger | 273 |
| Pending submission ledger | 38 |
| Live resource ledger | 0 |
| Clean / Complete | false / false |

The final successful Shutdown RETURN is persisted at sequence 1,230,976.
Produced/Durable equality proves the accepted-record tail drained; it does not
recover dropped events. `TERMINAL SUFFIX UNCERTAIN` is the correct result.
The incomplete pending ledger is not evidence that 38 actual GPU submissions
hung. Protocol faults may follow missing operation edges; no second independent
producer defect is claimed.

The earliest retained unmatched Frame record is sequence 693,269 / frame 36,357,
227.0770557 seconds after recorder open, and already carries Overflow. That is
approximately 08:09:49.948 UTC, before the first surface-flight input was loaded.
It is not necessarily the first lost event, and does not establish the overflow
cause. This campaign stops without opening a recorder correction front.

Retained app logs show continuing progress and normal exit, with no Vulkan error
identified. The bounded System/Application event scan (08:05:46–08:19:52.851 UTC)
found no matching GPU/display/hardware event or NovaCore crash. Event-log absence
and Present return do not prove physical display continuity; recorder loss also
prevents complete operation correlation.

## Preservation

Raw banks are pinned by the existing recorder retention mechanism at:

`C:\Users\Tyler\AppData\Local\NovaCore\MinimumRecorder\5612981fe98946d6b9d51655112a9697`

Compact local copies, OS query, app log and hash inventory are retained in
`E:\NovaCore\build\surface-recontact\native`. Original app log:

`C:\Users\Tyler\AppData\Local\NovaCore\Logs\20260927-080546-467-47272-b63211bfc119403ab77436cc9b3b2d96\segment-0.log`

Pre/post source, all 134 package files, settings, HEAD/index/refs and installed
KSA hashes match. Final independent review concurs with the stop and explicitly
separates recorder failure, offline physical correctness and visual ambiguity.
Blackout remains unresolved; banking and Player acceptance remain on hold.
