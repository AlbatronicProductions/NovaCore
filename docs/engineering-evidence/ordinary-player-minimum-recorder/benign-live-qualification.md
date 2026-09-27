# One benign live GPU attempt — REVISE / incomplete acceptance

2026-09-26 local evening (2026-09-27 UTC). Project Control authorized exactly one
bounded run of the frozen canonical NovaCore.exe. One launch, zero relaunches;
no rebuild, source/package edit, settings change, GPU diagnostic injection,
close-Earth/horizon route, construction/flight, Surface Recontact or banking.

## Actual route and disposition

Startup configuration → Start NovaCore → held Solar overview → Menu → Quit.
The two automation `4` key attempts, including after clicking the native viewport,
did not establish Earth focus. Source confirms that 4 is the Earth selector, but
screenshots, logs and generation records show that this run remained the Solar
overview. The cause of that input outcome is not proven. No retry is authorized
by this report. Computer interaction used the computer-use skill's supported
`@oai/sky` APIs; no custom input injection was introduced.

The real Vulkan interval covered 15,185 frames over approximately 84.5 seconds at
native 3440×1440 borderless presentation (render viewport 3440×1322). It showed no
observed display instability, CPU/GPU progress loss, Vulkan error, overflow,
watermark inconsistency or resource mismatch. A separate read-only mapping
watchdog was armed before launch and did not invoke its emergency stop.

**Requested live gate: REVISE, not PASS.** The benign Solar-overview recorder
sanity checks passed, but the requested Earth hold/generation behavior was not
exercised. Clean-shutdown wording also violates the new acceptance requirement.
Incremental live recorder timing/allocation and a matched performance-regression
comparison are unavailable from this unchanged candidate.

## Recorder and durability results

- Session `f967ea2a-be4b-4f55-b586-6bbfa2543a31`, producer PID 27088.
- Recorder was initialized/durable at sequence 1 on the startup screen before
  Start NovaCore/GPU initialization. No heavy-capture flag was supplied.
- Produced = Consumed = Durable = **291,769** at normal process exit; Done=1.
- Every live record's checksum, sequence and commit validated independently.
- All Frame → Update → Draw entry/return ordering and operation pairing passed.
- **15,248 successful submissions and positive completions**: 63 bootstrap
  submissions plus 15,185 ordinary frames; 15,185 successful present returns.
- 262 resource births with valid bindings/roles and retirement; zero final live
  resources, pending submissions or open operations. Handle incarnation checks
  passed. Generation/publication tuples remained (0,0,0); no Earth proof claimed.
- Faults=0, Dropped=0, Corrupt=false, Clean=true; **11 journal rotations**.
- Head revision 23,310, checkpoint 282,107, durable sequence 291,769. All 9,662
  records retained after the final checkpoint match the independent live copy
  byte-for-byte; checkpoint/head/page digests validate.
- Sampled maximum Produced−Durable was 561 events and drained fully. Both
  watermarks were monotonic. The independent helper completed after normal Quit.

`OrdinaryRecovery.Terminal` in OrdinaryJournal.cs unconditionally emits
"TERMINAL SUFFIX UNCERTAIN" even for this proven closed session. Final durable
event 291,769 is Shutdown.Return(result=0), with no loss or unmatched operation.
The blanket message is false uncertainty against the new clean-shutdown bar.
This is a reporting defect, not demonstrated persistence failure. Future bounded
correction should distinguish proven clean closure from unclean/incomplete/faulted
recovery while retaining the physical-display disclaimer in every case. **No
correction was applied during this frozen-package qualification.**

## Performance and evidence limits

| Measurement | Observed |
|---|---:|
| Whole-loop average | 5.566 ms (~179.7 FPS) |
| Native last-8,192-frame p50 / P95 / P99 / max | 5.515 / 5.842 / 5.955 / 6.521 ms |
| All-run frame-start cadence p50 / P95 / P99 / max | 5.551 / 5.8742 / 5.9867 / 137.5956 ms |
| All-run Frame scope maximum | 137.4594 ms, first frame |
| Next-largest Frame scope | 36.6913 ms, frame 3,116 |
| Frame scopes above 10 ms | 2 of 15,185; both completed normally |
| Existing GPU timing whole-run mean | 0.827 ms |
| Existing CPU average update / record / submit / present | 5.230 / 0.040 / 0.020 / 0.185 ms |
| Persistence CPU | 4,140.625 ms over 120,572.7145 ms; 3.43% of one core |
| Persistence cumulative allocations / retained managed heap | 548,104,792 / 562,992 bytes |
| Persistence peak working set | 59,604,992 bytes |
| Persistence write traffic | 193,936,886 bytes; 1.534 MiB/s across session |
| Fixed journal/checkpoint storage | 83,902,464 bytes; 11 rotations |
| Last 4,096 helper commit p50 / P95 / P99 / max | 1.5773 / 1.854 / 5.098 / 9.746 ms |

The first-frame scope includes resize work; frame 3,116 is dominated by Update.
These completed isolated outliers are not proven stalls or recorder regressions.
Do not replace the whole-run maximum with the smaller final-window maximum.
No matched recorder-disabled run was authorized or performed. The real route has
no live recorder-only timing/allocation counters, so the earlier Release fixture
producer quantiles (median 10.4–10.6 μs, P99 26.5–28.8 μs, zero timed allocations)
remain **offline evidence**, not live measurements. Event-pair timings measure
whole instrumented operations, not incremental recorder cost. No new GPU query,
feature, extension, buffer usage, submit, fence or wait was added for measurement.

## Identity, preservation and review

All 951 source and 134 package files matched before and after. Settings bytes,
Git HEAD, refs and index also matched. Historical offline identity remains intact:

- Source SHA-256: `80a84411b5d6f580c60226eb30f937b6659786547afb7944a61db6e44fff6c18`
- Package SHA-256: `3a75c475862d9c7d208c72ea3cbd47833b67d5c74634a869f6e93466c809703e`

Read-only native reviewer independently reconstructed the full event stream and
confirmed the measured route/performance limits. Read-only durability reviewer
confirmed the clean terminal witness and the unconditional wording defect.

Retained evidence: `build/ordinary-player-recorder/benign-live-20260926/` contains
pre/post identity snapshots, original session metadata, independent live records,
sampled watermarks/process counters, copied journal/checkpoints/recovery/performance,
application logs, analysis.json and retained-hashes.json. Original LocalAppData
session/logs are preserved. This unresolved gate's evidence has not been deleted.
`analyze.py` regenerates the offline validation without launching the application;
`preflight.py` rechecks the frozen manifests. `watch.py` is the single-attempt
read-only watchdog, with a consumed attempt guard; **do not rerun a live attempt**
from these files without Project Control authorization.

Next decision: authorize a bounded reporting correction and settle benign-route
input/measurement coverage before considering another live qualification. No
production correction or further live exposure is implied by this recommendation.

**Blackout UNRESOLVED · Player acceptance HOLD · UNBANKED. STOP FOR PROJECT CONTROL.**
