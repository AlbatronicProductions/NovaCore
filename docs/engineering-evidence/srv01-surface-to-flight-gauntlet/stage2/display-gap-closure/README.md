# Stage 2 integrated display-gap closure — STOP FOR PROJECT CONTROL

2026-09-17. **J — CAUSE UNRESOLVED. Stage 2 UNQUALIFIED; Stage 3 CLOSED.**

The authorized bounded investigation established the timer semantics and added
temporary buffered boundary instrumentation. Two predeclared captures completed;
neither reproduced the original large gap. No causal production correction is
justified. No third diagnostic run, replacement acceptance run, manual acceptance
or new campaign stage followed. No production correction was made.

## Original witnesses remain authoritative

| Original capture | Display callback interval max | Native frame max | Live service max |
|---|---:|---:|---:|
| 1 | **595.898 ms** | 59.259 ms | 57.7075 ms |
| 2 | **1,228.1696 ms** | 64.332 ms | 62.5022 ms |

Both completed 1,200 exact publications. Their concise observations and raw-log
SHA-256 identities remain in [the prior closure measurements](../closure/measurements.json).
They are not discarded or replaced by the better diagnostic captures.

## Measurement semantics

The managed measurement is between `AdvanceLive` sample points **after UpdateTitle**.
The initial timestamp is taken by Start. Subsequent intervals include the previous
service/callback completion, native rendering, acquire/submit/Present, possible
recreation, outer message dispatch and scheduling, next fence/inspection/input and
the next callback's prefix/title update. The next service is not in that interval.
It is therefore neither exact callback duration nor pure CPU execution time.

The native frame timer starts **after** outer message dispatch, and encloses Update
and Draw. Update waits on the previous frame's fence, inspects results, prepares
input, invokes the managed callback, then validates/uploads. Draw acquires an image,
records, submits, presents and can recreate the swapchain inline. Recreation's
nested message pump is inside that native interval. Outer message dispatch is not.

Managed Stopwatch and the diagnostic Windows QueryPerformanceCounter both reported
**10,000,000 ticks/second**. The trace carries raw common-clock values and OS thread
IDs. Normal native timers use monotonic steady_clock. Native CPU timing labels are
wall durations; the probe separately reads process and host-thread CPU counters.

## Timeline and process/thread activity

Correct order:

`previous sample -> previous service -> callback exit -> post-callback native work
-> acquire/record/submit/Present -> frame end -> outer message pump -> next frame
begin -> fence/inspection/input -> callback entry -> title -> next sample -> service`.

The original 595/1228 ms intervals span this whole chain. No original per-boundary
timestamps exist, so this investigation cannot place those two gaps in a narrower
span retrospectively. Different aggregate maxima alone cannot prove host inactivity.

The *smaller diagnostic* maxima are localized: their previous callbacks took
60.2077/56.9235 ms wall with approximately 62.5/62.5 ms process and render-thread
CPU increments (coarse Windows CPU accounting). The following callback-exit to
entry gaps were only **1.0064/0.8080 ms**, with zero reported CPU increments.
Previous complete service took 59.1858/56.0599 ms. This identifies first-service
work in those diagnostic captures; it **does not explain the original anomaly**.
Raw timestamps and surrounding native events are retained in `measurements.json`.

## Diagnostic captures, not replacement qualification

Same Release powered scene, stock configuration, 1280x720, 4,000 frames and existing
logging. Instrumented source was restored before either executable ran. Original
metric boundaries and solver/resource behavior were not changed. Instrumentation
itself can perturb scheduling/code layout; non-reproduction is not exoneration.

| Diagnostic run | Callback median/P95/P99/max ms | Native median/P95/P99/max ms |
|---|---|---|
| 1 | 5.5542 / 5.7236 / 5.8713 / 61.2229 | 5.539 / 5.835 / 5.965 / 61.119 |
| 2 | 5.5587 / 5.7101 / 5.8189 / 57.7287 | 5.542 / 5.817 / 5.941 / 57.660 |

There were 4,000 update callbacks/render iterations in each capture. Native record
counts were 81,442/81,490, with no buffer overflow. Only initial/long-span event
neighborhoods were emitted. One post-run buffered native row in each log interleaved
with managed summary output and was excluded; no causal claim relies on it. This
occurred during flushing after rendering, not during the measured episode. Native
overall average including post-run dump time is not used as performance evidence.

## Backlog and recovery

| Diagnostic run | Total credit ticks | Published ticks | Retained debt | Max publications/callback | Sampled live endpoints matching single-interval reference |
|---|---:|---:|---:|---:|---:|
| 1 | 20,003,521 | 20,000,000 | 3,521 | 3 | 3,585 |
| 2 | 20,003,029 | 20,000,000 | 3,029 | 3 | 3,586 |

At the longest observed diagnostic re-entry, prior debt plus new credit was
1,616+61,223 and 1,969+57,729 ticks. Both serviced three intervals, frontier 2 to 5,
retaining 12,839 and 9,698 ticks. Each then issued its normal next native Present.
There was no render loop per serviced interval. The pre-existing four-interval
maximum was preserved.

Post-render comparison used the unchanged ordinary candidate with one exact interval
of credit per operation. Every recorded live frontier matched physical endpoint
bits, stores, mass properties, StateRevision and history count. This does not claim
identical host-credit/debt records or compare every intermediate history entry.
Earlier permanent schedule tests remain the full per-frontier/history witness.
Recovery from the **original** 595/1228 ms gap was not newly traced; original end
results and source bounds must not be inflated into a new causal recovery trace.

## CPU, GPU/fence, GC, asset/I/O and window responsibility

No responsibility-level CPU campaign is justified: the target long gap did not
occur. The traced first-service work is much smaller and was already disclosed as
cold work. No optimization was attempted.

Acquire, record, queue submit, Present, fence, recreation, nested/outer dispatch and
DefWindowProc were stamped. There is no reproduced 0.6–1.2 second wait to attribute
to a GPU resource. CPU fence wall duration is not asserted to equal GPU work.

GC generation counts remained [1,1,1] across each largest diagnostic interval.
Cumulative pause ticks stayed 13,430/13,430 and 15,470/15,470 respectively. These are
observations of these two captures only; they cannot exclude or prove a GC pause in
the originals. Allocation counters cover title handling plus service and include
cold/title allocations. They do not replace the separately qualified exact-zero
warmed owner/physics path gates.

Process I/O, window message IDs, callback CPU and recreation spans were collected.
Initial native preparation/first-use activity precedes episode Start; its presence
does not explain an original post-Start gap. No shader, file, resize, focus, harness
or scheduling cause is inferred from coincidence or absence of reproduction.

**Engine work cost, player-visible stall and host scheduling remain distinct.**
The smaller probes do not waive the earlier integrated stall. Specifically,
`HOST GAP PROVEN` is **not** established for the original two witnesses, and neither
is a 0.6–1.2 second NovaCore CPU/GPU execution cost.

## KSA, correction, regression and acceptance

New KSA responsibility required: **NO**. This pass only measures this host/presentation
boundary. No retained physics, ready/apply, property-publication or render ownership
correction was made. The prior current-directory/authenticated-history review remains
in [the closure KSA record](../closure/ksa.md).

Correction: **None**. The original source files were restored byte-for-byte after
the diagnostic build using finally cleanup. All 24 candidate source/test seals and
the original native source hash match the completed closure. Earlier Debug/Release
physics, authority, allocation, storage, full builds and 78/78 Simulation groups
are preserved; expensive qualification was not rerun for a temporary probe.

Integrated performance: **NOT QUALIFIED**. Root cause: **UNRESOLVED**.
Manual acceptance required: **YES**. Status: **PENDING**; no manual run was requested
after this stop, and Stage 1 approval is not reused as Stage 2 approval.

## Independent red team

Independent review confirmed source restoration and sampled-endpoint replay scope.
It rejected substituting native timings for callback gaps, claiming host/GPU/GC/I/O
causation without a reproduced span, and treating diagnostic times as replacement
acceptance. Both original witnesses remain. No third capture/cherry-picking,
quality reduction, skipped publication, new ownership, missing required KSA change,
manual self-acceptance, early Stage 3 or banking occurred. The attack concerning
the unexplained repeating original hitch remains **OPEN**, blocking promotion.

## Reproduction and evidence hygiene

`build-probe.ps1` inserts the two retained diagnostic fragments into exact byte
backups, builds the isolated executable, and restores both source files in finally.
The first diagnostic native compile rejected a volatile argument passed to std::min;
a local snapshot fixed the probe, with source restored on both attempts. No test
failure or physics correction was hidden. Both successful captures used:

```powershell
$env:NOVACORE_WINDOW_CLIENT_WIDTH='1280'
$env:NOVACORE_WINDOW_CLIENT_HEIGHT='720'
& 'E:\NovaCore\build\srv01-display-gap-closure\artifacts\bin\NovaCore.Triangle\release\NovaCore.Triangle.exe' '--scene=srv01-powered-support' '--log=renderer,vulkan' '--benchmark-frames=4000'
```

These are retained reproduction instructions, **not permission for an extra run**.
Source/log/probe identities, timelines and exact results are retained. Raw build
trees/logs are disposable after this preservation; see [the identical retained inventory](../closure/cleanup.json) and `cleanup.md`
for exact paths, counts, bytes, commands and final dispositions. The old blocked
`srv01-powered-support` root is not retried.

Baseline/HEAD/main/origin/main/remote remain
`ab14e3e0f8b53bf5e8bf580a99aea3f2b6fd21f5`; branch
`codex/srv01-supported-contact-admission`. All 67 historical tags unchanged.
Nothing staged, committed, tagged, pushed or banked. No milestone assigned.

**JUDGMENT: STOP FOR PROJECT CONTROL.** Preserve the unqualified Stage 2 candidate.
Do not open Stage 3 or perform another diagnosis/correction without further direction.
