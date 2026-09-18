# Stage 2 integrated display-gap population discrimination

**C. NONREPRODUCIBLE / ATTRIBUTION STILL UNRESOLVED.** Stage 2 remains **UNQUALIFIED**, Stage 3 **CLOSED**, milestone **NOT ASSIGNED**, **UNBANKED**. Stop for Project Control.

## What happened

Exactly nine fresh processes completed in the predeclared order A1 B1 C1 B2 C2 A2 C3 A3 B3. A = powered supported SRV-01, B = unpowered supported SRV-01, C = banked free-flight behavior exercised in the current candidate executable. No D, repeats, dropped results or replacements. All workload/correctness checks valid, with the resolution-description deviation explicitly retained.

Total: 36,000 native frames, 35,991 full-capture after-title intervals, 200.4709508 seconds sampled display exposure. Each route contributed 11,997 intervals and 12,000 native frames. No interval reached the predeclared diagnostic 250 ms screen. No probabilistic event-rate bound is justified from these correlated, nonrandom desktop runs.

## Current repository truth

HEAD/main/origin/main/remote main: `ab14e3e0f8b53bf5e8bf580a99aea3f2b6fd21f5`. Branch: `codex/srv01-supported-contact-admission`. Preflight retained 24/24 source/test seals, 67 historical tags, no staged paths. The isolated observer build passed with zero warnings/errors. Both temporarily patched files were restored byte-for-byte before every capture. No retained production, test, rendering or pacing change. Final closing checks are in identity.json.

## Original retained witnesses

| Witness | Display max ms | Native max ms |
|---|---:|---:|
| Original 1 |595.898|59.259|
| Original 2 |1228.1696|64.332|
| Earlier diagnostic 1 |61.2229|61.119|
| Earlier diagnostic 2 |57.7287|57.660|

The original cause remains unresolved. This comparison neither fixes nor invalidates those failures. Historical ../closure and ../display-gap-closure are preserved.

## Environment / protocol / measurement

Windows 11 Pro 26200, ALBATRONIC007 local console, Release .NET 10.0.303, RX 6800 XT Vulkan 1.4, FIFO windowed **960 x 540**. The predeclared plan incorrectly said 1280 x 720. protocol-note.md records the correction after A1; all nine retained the actual default dimensions and logs confirm them. No replacement or environment change. These runs use default logging, whereas earlier detailed captures requested renderer/Vulkan logs. No exact historical-harness equivalence is claimed. No intentional camera/input interaction; OS background activity and driver caches are not perfectly controlled.

Existing legacy display/service timers are unchanged and retained in captures.json. Legacy sampling ends with the episode. The separate full-capture after-title observer includes held endpoints for equal callback exposure: A/B simulate 20 seconds, C simulates two seconds and then holds. Active-physics exposure is not equal. Observer timestamps/value copies are preallocated; no hot logging. Native per-frame samples still measure Update+Draw after outer messages. Native overall-average wall time is excluded because post-run dumping extends it. See environment.md and reproduce.md.

## Fresh captures

All times ms: **median / P95 / P99 / max**. Nearest rank. Zero-based indices. Display interval i ends at callback i and crosses parts of native frames i-1/i.

| Capture | Full-capture display | Native frame | Display max index | Native max index | Max service | Result |
|---|---|---|---:|---:|---:|---|
|A1|5.5551 / 5.7480 / 5.9322 / 57.0934|5.5495 / 5.8413 / 5.9868 / 56.7079|2|1|55.1669|VALID; completed|
|B1|5.5564 / 5.7336 / 5.9158 / 58.8710|5.5498 / 5.8256 / 5.9688 / 58.5364|2|1|57.0175|VALID; completed|
|C1|5.5569 / 5.7771 / 5.9763 / 34.5078|5.5557 / 5.8170 / 5.9894 / 36.1608|1|0|6.3934|VALID; completed|
|B2|5.5556 / 5.7524 / 5.9514 / 57.2307|5.5556 / 5.8412 / 5.9852 / 56.9449|2|1|55.4517|VALID; completed|
|C2|5.5571 / 5.7289 / 5.9414 / 41.0411|5.5557 / 5.8014 / 5.9441 / 44.4730|1|0|6.7400|VALID; completed|
|A2|5.5554 / 5.7241 / 5.9122 / 60.6113|5.5500 / 5.8309 / 5.9826 / 60.2434|2|1|58.3643|VALID; completed|
|C3|5.5576 / 5.7070 / 5.9433 / 35.5977|5.5585 / 5.8033 / 5.9649 / 38.4776|1|0|6.4624|VALID; completed|
|A3|5.5568 / 5.7097 / 5.8971 / 58.7853|5.5481 / 5.8194 / 5.9290 / 58.5512|2|1|56.8478|VALID; completed|
|B3|5.5545 / 5.7286 / 5.9191 / 52.7728|5.5512 / 5.8182 / 5.9497 / 52.4966|2|1|51.2772|VALID; completed|

All global maxima occurred in the first eight callbacks. All samples remain in headline distributions. Eight largest intervals, neighboring native samples, phases/frontiers and recovery are retained per capture, not only events above the diagnostic screen.

| Capture | PID | Start (EDT) | End (EDT) | Display exposure seconds |
|---|---:|---|---|---:|
|A1|26108|2026-09-17T17:36:02.5434069-04:00|2026-09-17T17:36:42.6958814-04:00|22.2889185|
|B1|57120|2026-09-17T17:38:21.2265197-04:00|2026-09-17T17:38:44.4508367-04:00|22.2931906|
|C1|68544|2026-09-17T17:39:20.9929524-04:00|2026-09-17T17:39:44.0810319-04:00|22.2413543|
|B2|34168|2026-09-17T17:39:55.7634689-04:00|2026-09-17T17:40:18.9693903-04:00|22.2867861|
|C2|43708|2026-09-17T17:41:02.3295057-04:00|2026-09-17T17:41:25.4220648-04:00|22.2475265|
|A2|63972|2026-09-17T17:41:33.5239352-04:00|2026-09-17T17:41:56.7382050-04:00|22.2932565|
|C3|39508|2026-09-17T17:42:10.4609080-04:00|2026-09-17T17:42:33.5570005-04:00|22.2441794|
|A3|45228|2026-09-17T17:42:47.6453109-04:00|2026-09-17T17:43:10.8632164-04:00|22.2919029|
|B3|66836|2026-09-17T17:43:28.8175991-04:00|2026-09-17T17:43:52.0122751-04:00|22.2838360|

Process duration includes preparation, reporting and reference replay. A1 total duration was 40.1525 seconds with 22.2889 seconds sampled display exposure; no attribution of the non-display remainder is attempted.

## Population summary

| Route | Display median/P95/P99/max | Native median/P95/P99/max | Active-to-active intervals | Held-to-held intervals |
|---|---|---|---:|---:|
|A|5.5557 / 5.7266 / 5.9077 / 60.6113|5.5492 / 5.8293 / 5.9705 / 60.2434|10759|1232|
|B|5.5553 / 5.7384 / 5.9212 / 58.8710|5.5523 / 5.8297 / 5.9744 / 58.5364|10760|1231|
|C|5.5571 / 5.7381 / 5.9520 / 41.0411|5.5564 / 5.8051 / 5.9718 / 44.4730|1064|10927|

Two phase-boundary intervals per process account for the difference between total and active/held subsets. C cold READY-to-active maxima are not in its active-to-active subset. Maximum after the descriptive first eight callbacks: 8.0598 ms (A1, held endpoint). This is not a discarded-sample acceptance result.

## Event classification / causal boundary

No materially similar extraordinary gap recurred in any route. There is no fresh giant event to classify as outer-host, mixed or execution dominated. A/B cold maxima end at callback 2: the preceding first service is wholly inside each interval and accounts for over 80%, so these observed cold maxima are CALLBACK EXECUTION DOMINATED. That does not explain the historical 600-1200 ms gaps. C maxima end at callback 1 alongside native startup frame 0; exact subdivision is UNKNOWN. Similar durations alone do not establish a native or outer-host cause.

The sum of adjacent native durations only bounds their possible contribution. max(0, display-nativePrevious-nativeCurrent) is a conservative outside-native lower bound, not an exact host-gap measurement. Stage-2-specific causation: UNRESOLVED / NOT PROVEN. Broader cross-route giant-gap responsibility: UNRESOLVED / NOT PROVEN. Shared small cold costs do not prove a shared giant-stall mechanism.

## Simulation correctness and backlog

All 36,000 sampled copied endpoints matched a same-route single-interval replay performed after rendering: motion bits, exact stores, complete mass/COM/inertia, StateRevision, TimelineRevision and history count. This verifies partition consistency, not an independent physics algorithm. Existing sealed qualification remains authoritative.

| Capture | Publications/revisions/history | Admitted ticks | Published ticks | Retained debt | Max pubs/callback | Peak sampled penetration m | Final sampled support error m |
|---|---:|---:|---:|---:|---:|---:|---:|
|A1|1200|20005203|20000000|5203|4|8.7434929210816748e-05|7.1248042009308676e-05|
|B1|1200|20004049|20000000|4049|3|9.573634067638892e-05|7.8054523743986692e-05|
|C1|128|2002108|2000000|2108|2|0|0|
|B2|1200|20002778|20000000|2778|3|9.573634067638892e-05|7.8054523743986692e-05|
|C2|128|2002686|2000000|2686|2|0|0|
|A2|1200|20004022|20000000|4022|3|8.7434929210816748e-05|7.1248042009308676e-05|
|C3|128|2005040|2000000|5040|2|0|0|
|A3|1200|20002807|20000000|2807|3|8.7434929210816748e-05|7.1248042009308676e-05|
|B3|1200|20000412|20000000|412|4|9.573634067638892e-05|7.8054523743986692e-05|

C contact geometry is not applicable. Every route conserved credit minus published duration = debt, with at most four publications/callback. Contact generation/body/shape stayed unchanged; pending=false and invalidated=false. TimelineRevision remained zero. One native frame followed each callback, independent of publication count. No giant-gap recovery occurred. Observed cold-delay recovery neighborhoods are retained in captures.json.

A final stores: fuel 28.4375 kg, oxidizer 42.65625 kg; mass 701.09375 kg; COM x 1.0475061288165812 m. B: 30/45 kg, 705 kg, COM x 1.0417021276595744 m. C: 29.9521484375/44.92822265625 kg, 704.88037109375 kg, COM x 1.0418789203343042 m. A does not exhaust fuel in this episode; no depletion/departure claim.

Independent transformed-corner checks on sampled A/B endpoints pass the retained 20 mm penetration and 122 micrometre settled support bounds. Live sampling can skip intermediate endpoints during multi-publication callbacks; it does not replace the retained all-interval 600/600 support qualification.

## Runtime observations

GC generation 0/1/2 deltas were 0/0/0 in all nine captures before replay. No per-gap process CPU or managed allocation attribution was collected. Existing native averages: fence about 5.04-5.07 ms, present 0.21-0.22 ms, GPU total 0.08-0.10 ms. These averages do not attribute individual gaps or prove spare compute. Observer cold/JIT costs and storage are not asserted to satisfy production allocation/storage contracts.

## Acceptance-contract interpretation / KSA / judgment

Current ../closure/plan.md retains integrated display quality. Historical standalone 50/100 microsecond budgets are superseded and were not reapplied. Latest Project Control judgment C requires stopping unless a clear exception already resolves retained nonreproducing anomalies. Lead and independent review found none. The original two stalls remain an open blocker.

**C. NONREPRODUCIBLE / ATTRIBUTION STILL UNRESOLVED.**

NO NEW KSA RESPONSIBILITY OPENED. No physics/publication ownership cause was established. No optimization, new population, manual acceptance or Stage 3 work. Stage 2 UNQUALIFIED; Stage 3 CLOSED; milestone NOT ASSIGNED; UNBANKED. **STOP FOR PROJECT CONTROL.**

## Evidence / reproduction / cleanup

This report, captures.json, plan, protocol deviation, environment, preflight/final identities, bounded observer/build/capture/analyzer scripts, reproduction instructions, independent red team and cleanup report are retained. Earlier four reviewed scratch roots were absent; no deletion was retried. Exact new disposable root, counts and manual/non-destructive commands are in cleanup.md. Previous evidence packets were not rewritten.
