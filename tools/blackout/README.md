# Standing blackout witness — normal-play procedure

**Standing witness: PASS for synthetic long-run qualification.** The user confirms
that the previous real session successfully ARMED and remained healthy during play.
Its two helpers were launched with `duration=1200`; their simultaneous ~20-minute
expiry and the phone's `QuotaExceededError` are witness-tooling failures. The user
also reports MinimumRecorder still had Produced=Durable and a live observer. These
observations do not identify the original blackout cause, which remains unresolved.

## One-command real-run startup

Run this in PowerShell **before your next normal recorder-required NovaCore launch**:

```powershell
pwsh -NoProfile -File E:\NovaCore\tools\blackout\Start-Witness.ps1
```

1. Keep that terminal open. Open the existing bookmark directly in Samsung Internet
   on the same LAN. Refresh once to load this build; remove `?stress=1` for normal play.
   The page shows `bounded slot / 8`. The terminal prints the URL if a new bookmark is
   needed. Keep the S24 awake and this page visible.
2. Expect READY TO LAUNCH after a recent read-only capacity check. Use the existing
   recorder-required application startup. Before playing, require **ARMED**, both
   fresh-response counters increasing, **Storage readback OK**, and the expected
   recorder session UUID. A reservation and exact live session are checked after
   startup; CONNECTED alone is insufficient. If the prelaunch capacity check becomes
   older than 60 seconds, restart the witness before launching.
3. Leave the witness running throughout normal play. There is **no 20/30-minute or
   other automatic end time** in real/recovery mode. It has no inactivity or phone
   disconnect timeout. Ending the capture or helpers is an explicit operator action.
4. At blackout: press **BLACKOUT — both displays** immediately; leave the PC untouched
   for about ten seconds; observe both fresh post-marker counters; **EXPORT JSON**;
   confirm the file in Downloads (use Download JSON if needed). Reset only if necessary.
   Export stops this phone capture. Preserve the exact recorder session, witness output,
   application/session logs and original phone JSON before another launch.
5. At an ordinary session end, EXPORT JSON as well. Once the file is safely downloaded,
   expand Saved captures and select it with **Verify downloaded JSON and retire saved
   copy**. Exact file readback retires only the stopped browser copy. The downloaded
   file remains. Repeat file selection for older stopped captures to free their slots.
6. Stop helpers with Ctrl+C in their terminal. After a crash or closed terminal, use
   the exact startup artifact printed at launch:

```powershell
pwsh -NoProfile -File E:\NovaCore\tools\blackout\Stop-Witness.ps1 -Startup 'E:\NovaCore\build\blackout-post-m16\YOUR-RUN\startup.json'
```

If the primary PID has been reused, or primary startup died before publishing
`startup.json`, stop the orphan through its independent birth identity:

```powershell
pwsh -NoProfile -File E:\NovaCore\tools\blackout\Stop-Witness.ps1 -Startup 'E:\NovaCore\build\blackout-post-m16\YOUR-RUN\beacon-startup.json' -BeaconOnly
```

Never substitute a process-name kill. A second server cannot share the occupied port;
resolve the previous exact helper before restarting. The launcher starts no NovaCore
process and changes no runtime, recorder allocation, firewall, renderer, or driver settings.
A healthy already-running game is not adopted: start the witness before the session.

## Storage and recovery

New phone evidence uses IndexedDB, with **eight capture slots**, at most **2,048 rows
and 768 Ki characters per capture**, including bounded pinned evidence. Only newly
appended rows and small metadata change during normal writes; old ring rows are
removed within the same transaction. One write and one coalesced pending snapshot
bound a slow writer's queue. ARMED requires a completed transaction/readback of
recent evidence. An old write completing late cannot renew its freshness timestamp.

Each page load has a new page identity and slot. All eight protected slots being
occupied blocks a ninth capture visibly, while an existing admitted capture can
continue rolling. No active or unexported capture is evicted by age, quota pressure,
refresh or server restart. Clicking Export alone does not prove the download worked.
Retirement requires a sealed capture and a matching file selected from Downloads;
comparison ignores only JSON formatting/property order and top-level `exportWall`.
A stale/malformed/foreign file cannot retire newer or unrelated evidence.

If the original page was abandoned, close its tab first. In recovery, select its
snapshot, check **I closed the original capture page**, then **Freeze selected
abandoned snapshot**. This explicit action seals the stored revision and fences
late writes; it does not delete evidence. Export that snapshot, select the actual
matching download, then retire it. Do not use this action on a page still recording.

**Existing localStorage captures remain untouched and readable.** New captures do
not write more localStorage keys, even when the old quota is full. Legacy originals,
including malformed ones, remain available for raw export; failure to open IndexedDB
cannot hide legacy recovery. This tool deliberately does not auto-delete old V1/V2
localStorage records because their export state/active ownership cannot be proven.

To recover at the same bookmark:

```powershell
pwsh -NoProfile -File E:\NovaCore\tools\blackout\Start-Witness.ps1 -Recover
```

RECOVERY ONLY admits no new capture. Select the intended witness/page and Export
selected original. The same HTTP origin (address, port, browser/profile) is required.
The token is retained in `build/blackout-witness/bookmark.json`; normal ports are
58761/58762. `-Bind` selects a verified LAN address if automatic routing chooses wrong.

The loaded page exports even when both hosts are silent. A stuck browser write gets
at most three seconds to seal before a frozen RAM JSON is exported with explicit
unqualified storage status; it is not silently discarded or claimed durable. Reloading
while the server is offline remains unsupported. Do not refresh during a blackout.
Browser profile deletion, physical device loss, exhausted whole-device storage or
browser eviction are outside the bounded application-storage guarantee. IndexedDB
transaction/readback is not a phone physical-media/power-loss guarantee.

## What the phone must show

- **FIXTURE ARMED**: synthetic session, both responders fresh, healthy synthetic
  observer progress, committed local samples and working storage readback.
- **READY TO LAUNCH**: no live session; a recent read-only recorder quota/disk
  snapshot passed. This is not a reservation, native-run authorization, or ARMED.
- **ARMED**: exact real session; canonical process paths and birth times; sealed
  deployed file identities; normal session reservation metadata/allocation;
  Ready=1, Done=0, zero faults/drops; valid event checksum; positive Durable;
  advancing observer heartbeat and recent committed witness samples from that
  session; both independent responses fresh; visible phone page and storage OK.
- **CONNECTED / stale / UNQUALIFIED**: do not proceed onto the exposure route.

Sampler interval is one second. Each phone channel polls approximately one second
**after completion** of its previous request (request/JSON/storage time is additional).
Reply admission deadline is 1.8 seconds; displayed freshness and sampler/writer/observer
readiness age are three seconds. These are diagnostic policies, not new runtime fault
deadlines. `Produced == Durable` is not required. A healthy idle producer can be armed
when its observer still progresses. Prelaunch capacity expires after 60 host-monotonic
seconds. Restart the witness to refresh it; never substitute free disk space alone.

## Synthetic qualification and reproduction

The locally retained standing qualification evidence (excluded from Git) records the accelerated full-day browser run, receiver races, host lifetime/port/
process tests, fixed journal tests and independent export inspection. No NovaCore
was launched for those synthetic tests. The final file-picker correction subsequently
passed the user's physical Samsung Internet/S24 smoke: filename remained visible,
exact downloaded bytes verified, only the matching stopped IndexedDB capture retired,
and the downloaded JSON remained unchanged. This does not qualify blackout causality.
The [M16.1 disposition](../../docs/milestones/M16.1.md) retains this infrastructure
as DEV-ONLY, separate from production gameplay and any public distribution decision.

CPU-only server (standing by default; a finite deadline is fixture-only):

```powershell
pwsh -NoProfile -File E:\NovaCore\tools\blackout\Start-Witness.ps1 -Fixture
# Optional short synthetic session only:
pwsh -NoProfile -File E:\NovaCore\tools\blackout\Start-Witness.ps1 -Fixture -Duration 120
```

Reproducible tests (use fresh output directory names):

```powershell
python E:\NovaCore\tools\blackout\test_standing.py
python E:\NovaCore\tools\blackout\test_witness.py
node E:\NovaCore\tools\blackout\test_receiver.cjs E:\NovaCore\build\blackout-post-m16\NEW-receiver-test
python E:\NovaCore\tools\blackout\test_export.py E:\NovaCore\build\blackout-post-m16\NEW-receiver-test\synthetic-export.json
node E:\NovaCore\tools\blackout\test_standing.cjs E:\NovaCore\build\blackout-post-m16\NEW-day-test
```

The browser tests require Node.js, the `playwright` package and installed Edge.
Resolve `playwright` through Node's normal module search, or set `PLAYWRIGHT_MODULE`
to an explicit installation. They are developer tests, not player dependencies.
The standing test advances 86,400 logical polling cycles, waits for
real IndexedDB commits each cycle, and does not substitute a memory-only storage mock.
This is accelerated endurance, not 24 elapsed hours on the phone.

Local PC evidence remains two alternating checksum-protected 4-KiB slots. Retained
journal allocation stays 8 KiB; logical steady-state writing is about 4 KiB/second
(337.5 MiB/day), not zero I/O. Startup/output metadata is fixed per helper invocation.
The separate CPU beacon intentionally survives abrupt primary loss until explicitly
stopped. Both servers bound concurrent requests to eight, with socket timeouts.

## What the evidence can distinguish

| Observation with fresh post-marker nonce response | Supported branch |
| --- | --- |
| Main + CPU alive; observer heartbeat advances; Produced/latest renderer event remain static | Host/observer execution continued while producer/render evidence stopped. Inspect GPU/render/driver path; terminal fence marker is not causal proof. |
| Produced advances; Durable/observer heartbeat stop or observer exits | Recorder/persistence progress failure supported. Preserve fault/drop/process fields. |
| Main HTTP alive, sample serial static | Sampler stopped progressing; does not prove render or recorder stopped. |
| Samples advance, local committed serial stale/error | Witness local writer/persistence failed; phone-held replies remain useful. |
| Main silent, independent CPU answers a fresh challenge | Host execution continued; primary witness/source/process path needs reconstruction. It does not prove process death. |
| Both silent | Shared host/OS/driver/power or phone/LAN failure remains; broaden investigation. No unique kernel/hardware owner established. |

The two responders share this PC's OS, scheduler, networking, power and LAN. Positive
execution proof is strong; common silence cannot separate all external causes. Latest
recorder event is an observation, not a current thread stack or scanout completion.

The witness records evidence; it does not repair or identify the original terminal
fence wait. Preserve positive fresh-nonce evidence before assigning a causal owner.

Original incident analysis and raw captures remain local-only; historical blackout causality is UNRESOLVED.
