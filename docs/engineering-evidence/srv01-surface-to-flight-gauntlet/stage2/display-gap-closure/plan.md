# Integrated display-gap causal closure

2026-09-17, Project Control ticket f0d2f555-6eb2-4849-8286-a4e4278c196b.
Stage 2 unqualified; Stage 3 closed. Preserve both original witnesses and the
completed correctness/allocation/storage/regression results in ../closure.

One bounded cycle: source semantics, temporary buffered diagnostics, at most two
unchanged 4,000-frame Release captures to reproduce/localize the gap, then a
classification. If one source-owned defect is positively proven, one correction
may be made and checked; otherwise stop. No replacement runs, speculative fixes,
new physics, solver changes, quality reductions or performance contract changes.

## Existing measurement semantics (source read before instrumentation)

Both clocks are monotonic Windows high-resolution counters: managed Stopwatch;
native std::chrono::steady_clock. The probe records QueryPerformanceCounter and
its frequency explicitly; managed records include Stopwatch.Frequency for cross-check.

`StockAssemblyDevelopmentScene.AdvanceLive` first calls UpdateTitle, then takes
the sample. The old timestamp is the previous sample (initially Start's timestamp).
Thus callback interval includes previous service/callback completion, intervening
render/submission/Present, outer message processing/scheduling, next frame fence
wait/input preparation and the next callback prefix/title. It excludes the next
service operation. This is not a callback-execution timer.

`RunRenderer` sets frameBegin after PeekMessage/TranslateMessage/DispatchMessage.
It measures Update + Draw. Update includes fence wait, inspection, input, actual
managed callback and validation/upload. Draw includes acquire, record, queue submit,
Present and any inline swapchain recreation. Recreation has its own nested message
pump. Outer message processing is outside this timer. Both run on the native host
thread; diagnostic delegate calls are not display updates.

Order: prior managed sample -> prior service -> prior callback exit -> native
post-callback preparation -> acquire/record/submit/Present/recreation -> frame end
-> outer message pump -> next frame begin -> fence/inspection/input -> next actual
callback entry -> UpdateTitle -> next legacy sample -> next credit/service.

## Probe discipline

Preallocated native and managed records only; flush after rendering has returned.
Record raw timestamps, actual OS thread identity, cumulative process/thread CPU,
process I/O counters, individual message dispatch/DefWindowProc spans, fence,
callback, acquire/record/submit/Present and recreation spans. Preserve all original
timestamps, logging, command line and 1280x720 route. Do not call observations inside
an owned phase. Record managed GC counts/pause-duration and thread allocation deltas,
credit, debt and before/after frontier; those counters alone do not establish cause.
Retain event neighborhoods of long intervals plus aggregate statistics, not bulk traces.

After the renderer stops, replay ordinary single-interval host credit through an
independent instance of the unchanged candidate and compare every recorded live
frontier's exact physical bits/resources/properties to that reference. This replay
is outside live timing and is not a different production evolution path.

Build instrumentation temporarily from exact source byte backups under
E:\NovaCore\build\srv01-display-gap-closure, restoring source bytes in finally and
verifying hashes before running the diagnostic executable. No diagnostic becomes
production. New KSA work is required only if a proven correction changes an
equivalent physics/publication ownership responsibility.
