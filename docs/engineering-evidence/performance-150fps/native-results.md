# Single Stage A native witness

Canonical NovaCore.exe; source/package identities in results.json. Started
2026-09-27T16:05:46.1039673Z; PID 43524; wall 11.2868419 s; exit 0; no forced stop.
Actual logged extent 3440x1322 throughout; saved display configuration 3440x1440.
No resolution, tessellation, terrain, contact or recorder-coverage reduction.
Native exposure marker is consumed. Do not repeat this run to gather averages.

| Flight measurement (ms) | Median | P95 | P99 | Max | >6.6667 / count | Longest run |
|---|---:|---:|---:|---:|---:|---:|
| Whole-frame cadence | 10.6952 | 16.2504 | 31.0350 | 1056.4140 | 377/377 | 377 |
| GPU total | 8.90160 | 9.73857 | 11.22792 | 11.45100 | 357/377 | 357 |
| Managed physical service | 0.6176 | 3.9623 | 12.1393 | 287.0025 | 5/376 | 2 |
| Native fence/completion scope | 8.5964 | 9.6636 | 11.0659 | 11.2786 | 341/377 | 195 |
| Grounded terrain work | 0.6354 | 0.8095 | 1.5045 | 2.2870 | 0/216 | 0 |
| Grounded physical service | 2.7897 | 4.1360 | 6.3597 | 13.2132 | 2/216 | 1 |
| Post-handoff cadence, zero native slices | 7.7445 | 9.3876 | 10.7216 | 12.1165 | 102/102 | 102 |
| Post-handoff GPU, zero native slices | 7.11784 | 8.03116 | 8.44120 | 8.58610 | 82/102 | 82 |

Whole-frame median/P95/P99 headroom: -4.02853/-9.58373/-24.36833 ms.
The fence scope includes completion/capture bookkeeping; it is not exclusive GPU
waiting time. CPU and GPU overlap and are not summed. Native Update+Draw is CPU
wall critical-path occupancy including waits, not sampled active CPU consumption.
Timestamp sample owners are complete and unique for all 377 flight frames.
Full-window GPU coverage is 721/723: missing owner IDs [1, 723] are disclosed
separately at startup/shutdown. Both lie outside the 377 flight owners.
Cadence at N closes N-1 -> N, so analysis joins it to the preceding work owner.

Stage A before/after attribution uses the identical 537-row offline replay:
216 contact rows without world creation, terrain median/P95/P99/max
12.8276/15.2812/15.4985/15.8945 -> 0.4429/0.4779/0.7445/0.7499 ms.
Every physical Before/After and debt/admission/retirement value matches exactly.
The two native populations have different display-frame counts and phase mix;
their aggregate medians are not an isolated CPU optimization experiment.

Debt: 4,425,224 us admitted; 283 * 15,625 = 4,421,875 us retired; final remainder
3,349 us. Zero reconciliation or consecutive-row discontinuities. Grounded
DebtAfter never exceeds 15,541 us. The 283.8791 ms reimport transition creates
serviceable debt on frames 619-623, drained by frame 624. Maximum before-service
debt is 303,146 us. No admitted interval was dropped or simulated at display rate.

Physical coverage: terrain contact frame348/sequence5; grounded rest frame469/
sequence102; grounded RCS with resource consumption frame471/sequence104; release
sequence108; ignition first observed sequence174; handoff frame620/sequence219;
final sequence283, exactly 64 further FreeFlight steps. These are bounded physical
phase witnesses, not indefinite landing or display-stability qualification.

Outlier classification:

- Initial restore callback: 1,034.0298 ms, entire cadence 1,056.4140 ms. Its automatic
  trigger is qualification-owned, but it invokes normal production Load/Restore.
  Classify the work as ONE-TIME PRODUCTION TRANSITION with unresolved subcosts.
  Its total is retained; it is not an excluded qualification-only cost.
- World reimport frame618: 283.8791 ms world stage / 287.0025 ms service:
  ONE-TIME PRODUCTION TRANSITION in this finite run, not general proof it never repeats.
- Frame459: slice preparation 8.5597 ms / service 12.1393 ms: UNRESOLVED.
- Frame564: solver 6.2071 ms / service 7.8151 ms: retained transition-region
  outlier; no OS/GC exclusion is justified by aggregate collection counts.
- Repeated ~8.07 ms detailed terrain draw while grounded: PRODUCTION STEADY-STATE.
  It independently defeats the frame contract. GPU cull/compaction is ~0.725 ms
  in that held draw regime; draw attribution is not fragment-only timing.

Recorder producer sample median/P95/P99/max: 19.7/29.3/31.8/42.5 us, timed
allocations zero. Qualification observer/instrumentation costs are retained in
results.json, not inferred as subtraction from another run. Recorder recovery:
COMPLETE, 17,183 records, 786 submissions/completions, 723 presents, 278 resource
births and retirements, no dropped events/faults/open operations/pending work.
Present-return cadence independently reconciles the slow frame population; this
is CPU API-return timing, not physical monitor scanout.

The OS query returned no events at its requested error/warning levels. The native
window closed normally. Screenshot forwarding failed during the short exposure,
so direct visual no-blackout evidence is unavailable. No retry was taken. This
does not close the original blackout or authorize ordinary exploration.
