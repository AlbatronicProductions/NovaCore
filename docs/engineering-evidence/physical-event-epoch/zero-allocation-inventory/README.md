# Simulation zero-allocation test-surface inventory

Recommendation: **SMALL CLASS MIGRATION**.

Final classification: **ZERO-ALLOCATION SURFACE — SMALL CLASS MIGRATION**.

Lead judgment: **PASS** for this static inventory and scope decision. This is not
candidate acceptance; the physical-event epoch candidate remains unbanked and its
acceptance matrix remains paused.

## Baseline and boundaries

Branch `codex/physical-event-epoch`; HEAD/main/origin/main/M14.5 are
`b53d48025cfb8142cb32a6fc6fceb6018e33cfcd`. M14.1–M14.5 annotated tag objects and
targets match the preceding identity record. All 210 recorded production C#
fingerprints match. Current Program.cs matches the post-servicing-correction hash
and PhysicalEventEpochTests.cs matches its accepted hash.

No source/test change, build, test process, profiler, observer, EventPipe, GC/JIT
trace, structural discriminator or cleanup was performed. Only this bounded
engineering-evidence package was added; retention budget is 128 KiB. Source
identities and final Git state are retained beside this report.

## Current validation stop

Prior full Debug run1 passed all37 registered groups. Prior Debug run2 passed11,
failed Canonical transaction groups, and left25 groups unreached. In both,
corrected servicing reported zero, checked entry/exit success, clock50000, debt0,
processed5000, pending0 and completed499/499; its positive control detected152.
Both full solution builds passed. No additional dynamic results are claimed here.

The canonical-group assertion combines three operands; **FAILING OPERAND UNKNOWN**.
Another old allocation-sensitive/composite gate surfaced under changed full-suite
history. Another allocation false positive has **not** been proven.

The servicing causal conclusion remains **CANDIDATE-TRIGGERED CLR
ALLOCATION-CONTEXT ACCOUNTING EFFECT**. Its zero-object counter increase was
exactly unchanged cumulative fields minus unused context changing8160→0. The
current audit does not reopen that resolved attribution.

## Complete inventory and counting convention

See [the complete inventory and per-gate warmup/status table](inventory.md) and
[all requested per-gate fields](inventory.json).

- **38 permanent exact-zero assertion sites protect42 measured counter windows.**
- **36 assertions /40 windows** belong to the default37-group suite.
- **2 assertions /2 windows** are retained opt-in performance contracts, outside
  the default suite. Parameterized state counts/modes/samples repeat those sites.
- **36 windows lack checked isolation:**31 ordinary allocation-only windows plus5
  windows combining allocation with timing duties.
- **6 windows have checked no-GC isolation:**2 with local positive controls,4 with
  indirect qualified-pattern evidence only.

One propagation assertion checks three windows; replacement and impulse assertions
each check two. Inventory rows split these windows explicitly so no operand is
lost. The term gate may otherwise hide these count differences.

There are98 per-thread-counter API call sites, accounting for49 before/after
measurement sites:42 zero-required,2 positive-control,5 report-only. There are7
checked GC entry/exit sites:6 for zero windows and the standalone servicing
positive-control region. Orchestration's positive control shares its region after
the zero window. See `api-accounting.json`.

No permanent total-allocation-counter gate, custom managed-object-observer gate,
unresolved custom allocation helper or externally linked allocation-test source
was found. Project files include the local seven C# files by SDK convention and
reference Simulation→Core/EphemerisFormat. These production dependencies contain
no allocation-measurement API/helper. Directory.Build.props does not inject one.

### Non-zero/report-only candidates explicitly accounted for

| Site | Responsibility | Handling |
|---|---|---|
| Program.cs:127 per-body orientation probes | Report nine bodies' allocation/evaluation; no zero assertion | Keep separate from the two real orientation gates on the same line |
| Program.cs:552 SolAnalytical construction | Report construction allocation; construction is allowed to allocate | Exclude from zero-workload migration |
| Program.cs:664 ephemeris catalog construction | Report construction copy allocation | Exclude |
| ContactResponseTests.cs:488 Performance | Report21 complete-transaction allocation samples, median/max; no zero assertion | Preserve ordinary-runtime qualification semantics |
| ContactResponseTests.cs:517 StoragePerformance | Report three storage modes' samples; no zero assertion | Preserve ordinary-runtime qualification semantics |
| Program.cs:1276 servicing control | Warmed known byte[128], require >0 | Positive control, not a zero gate |
| Program.cs:1419 orchestration control | Known byte[128] after zero measurement, require >0 | Positive control sharing checked region |

Phrases such as Program.cs:447's "allocation-free ID lookup" are functional lookup
checks, not additional counter windows; the real store allocation gate is row11.
Value-layout/no-managed-reference assertions likewise do not measure workload
allocation and remain untouched. Historical M14.2 native observer tooling in docs
is not compiled or called by permanent Simulation tests.

## Measurement families

| Family | Members (inventory IDs) | Shared Contract | Shared Measurement Weakness | Equivalent Migration? | Recommended Handling |
|---|---|---|---|---|---|
| Checked counter with local positive control (A) | 7,10 | Exact zero warmed servicing/orchestration; independent state/count results | Proven retirement effect excluded after checked entry; region availability still fail-closed | Already migrated | KEEP |
| Checked counter without local positive control (A) | 2,26,28,42 | Exact zero epochs/contact generation/policy; optional three-mode batch maximum | No raw-retirement weakness; direct per-gate positive validation absent | No isolation migration needed | KEEP; future common contract should carry deliberate-allocation proof; SPLIT row28's success predicate only when authorized |
| Ordinary raw per-thread zero counter (B) | 1,3–6,8–9,11–18,23–25,27,29–40 (**31 windows**) | Exact zero warmed managed workload after fixture allocation; preserve existing success/state/hash conditions | Same direct counter/zero/no-isolation mechanism as proven servicing weakness | **Yes, at measurement boundary**; production workloads remain individually owned | One bounded class migration, not sequential gate forensics; preserve every original outcome |
| Timed raw counter (E, hybrid) | 19–22,41 (**5 windows**) | Exact zero plus timed orientation/propagation/translation evaluation | Same raw accounting exposure; isolation can additionally alter timing interpretation | Not a blind copy of B | Explicit timing/measurement review in the same scope decision; preserve latency bar/samples and ordinary-runtime timing meaning |
| Report-only and controls | Seven sites above | Characterization or deliberate nonzero detection | Different threshold/purpose | No | Exclude from blanket zero migration |

"Small class" describes one bounded test-measurement responsibility, **not a claim
that only two or three gates are affected**. The weakness is widespread in this
test project. It does not justify a broad runtime/production refactor or a new
profiler framework. The31 ordinary windows provide a concrete non-timing migration
class; the5 timed windows must be explicitly scoped, not silently changed or left
to surprise a restarted acceptance matrix.

## Vulnerability classifications

**VULNERABLE TO PROVEN ALLOCATION-CONTEXT RETIREMENT EFFECT:** all36 B/E windows.
Each is a direct per-thread counter with exact-zero expectation, no enclosing
checked no-GC region, and ordinary allocation-context state capable of changing
during measurement. This is a static architectural vulnerability classification,
not evidence that each ever failed or that the canonical failure's allocation
operand failed. Before-epoch row1 and opt-in row41 share the mechanism too;
position after epochs is not the classification criterion.

**ROBUST against this specific effect:** rows7/10 have checked entry, bounded
counter window, finally exit, no fallback, exact-zero checks and deliberate local
allocation controls. This does not claim infinite stability or a completed
acceptance matrix.

**LIKELY ROBUST:** rows2/26/28/42 use the same fail-closed isolation but have no own
deliberate allocation control. Row28 still needs an independent success assertion;
row42 asserts the maximum of21 measured batches per mode is exactly zero. Do not
replace that with median/typical/best-sample behavior.

No discovered measurement needed POSSIBLY VULNERABLE or NOT ENOUGH INFORMATION
for its static architecture. Actual failure attribution remains unknown for the
canonical gate and untested for the other raw-counter gates.

## Allocation-relevant test order

- G1–3 establish basic time/rate tests; G4 speed presets is the only measured
  exact-zero gate **before epochs**. G5 ordering creates/shuffles header arrays.
- G6 epochs executes normalization→ordering→serialization→Allocation(). Setup
  and proof fixtures allocate; Allocation() warms all three modes then enters and
  exits its checked region. No region surrounds the rest of the suite.
- G7 timeline creates large preallocated queues/arrays and measures raw schedule/
  cancel; G8 clock and G9 host conversion also use raw counters.
- G10 debt service has the remaining raw single-call gate, then corrected checked
  long-duration servicing, then its independent checked positive region.
- G11 transaction execution and G12 canonical group construct5000-event fixtures
  and use raw counters. **G12 is the current stop.** G13 orchestration has its own
  checked region plus in-region positive control.
- G14–24 build celestial catalogs, sampled fixtures, trajectory/frame buffers,
  and spacecraft histories, then mostly measure raw warmed operations. G21/G22
  include timing-bearing windows. They inherit process history; short or passing
  measurements are not proof of isolation.
- G25 contact generation has checked isolation; G26 contact transaction does not,
  despite four preallocated warm batches. G27 isolated policy has checked isolation.
- G28–37 return to raw attitude/rotation/guidance/orbit/frame/transaction/arithmetic
  windows. Earlier successful GC exits do not protect these later windows.
- Opt-in epoch performance calls the same Allocation() site afterward; do not
  count it twice. Translation performance and isolated-response performance have
  their own additional zero sites. Contact-response performance paths report
  allocations without a zero bar. No explicit GC.Collect call exists in this scope.

This is a source-order map, not a claim that a particular collection occurs at a
particular point. Setup volume proves allocation opportunities, not GC causality.

## Composite assertions

Recommend **SPLIT** these19 distinct assertion sites (23 inventory rows):

- row1: zero + checksum guard;
- rows6/8/9: zero + completion and/or processed counts;
- rows11–16/18: zero + traversal/checksum/composition guards;
- rows19/20: zero + orientation success/checksum and, for lunar, latency bar;
- rows21–23: three zero deltas combined into one assertion;
- row25: zero + translation checksum;
- row28: zero + all policy evaluations succeeding;
- rows36–37: two zero deltas + committed replacement;
- rows38–39: two zero deltas + completed impulse execution;
- row41: zero + benchmark checksum.

All remaining19 assertion sites: **KEEP** their independent allocation assertion.
Future splitting must preserve exact thresholds and each original predicate;
success checks inside measured loops must also remain. No split was made here.

## Positive controls

PRESENT: servicing row7's separate checked-region known object and orchestration
row10's post-workload in-region known object. INDIRECT ONLY: other checked gates
inherit the qualified pattern's evidence but have no local deliberate control.
ABSENT: B/E have no dedicated control of their unisolated architecture. Merely
having a positive control elsewhere in the process does not fix their boundary.

A future common measurement contract should require a deliberate known-object
control using the same checked entry/counter/exit rules, without sharing a fake
result implementation. Keep the exact-zero workload assertion separate. Do not
add control allocations inside a production measurement or alter workload warmup.

## Program.cs:1365 static decomposition

Method: CanonicalGroupTests, group12. Exact assertion:

```csharp
Check(GC.GetAllocatedBytesForCurrentThread() == allocationBefore &&
      allocationResult.IsComplete && allocationEngine.ProcessedCount == 5_000,
      "preallocated canonical group execution allocates zero bytes");
```

- Allocation operand: current-thread counter equals the value opened at1363.
- Completion operand: allocationResult.IsComplete.
- Count operand: allocationEngine.ProcessedCount ==5000.
- Workload: one ExecuteCanonicalGroup on5000 pre-scheduled events.
- Warmup: one separate single-event canonical group; fixture construction follows.
- Isolation: **none**; no earlier region remains active across this call.
- Architecture: B, same essential raw-counter weakness as original servicing.
- Diagnostic shape: composite, recommend SPLIT; short-circuit evaluation does not
  tell us which operand failed from the existing exception.
- **FAILING OPERAND UNKNOWN.** No counter delta, result or count was recovered by
  static inspection; no dynamic instrumentation or attribution was attempted.

## Existing servicing migration

**KEEP AS IMPLEMENTED.** The migration correctly isolates its counter, preserves
setup/warmup/work, requires exact zero and all exact counts/state, fails closed
on entry/exit, and checks an independent real allocation. Both observed runs
passed this gate. Another later gate failing is no evidence to roll it back.
Full acceptance remains paused; this classification grants no banking authority.

## VERIFY A — completeness

Cross-checked all seven C# sources, project/import structure, API occurrences,
semantic allocation phrases, counter variables through their checks, helper
definitions/call sites, current37-group registrations and opt-in selectors.
Compared the retained M14.2 qualified pattern and confirmed its historical observer
is not a current test dependency. Raw-counter/GC searches of directly referenced
production projects found no hidden measurement helper.

The second pass caught SolAnalytical's construction counter (not a zero gate), the
two separately measured replacement/impulse paths, the three propagation windows,
and opt-in performance assertions. All98 per-thread calls reconcile to49 windows.
Seven entry/exit pairs reconcile to6 zero-window sites plus the standalone positive
control. No total-counter/custom-observer permanent gate remains unclassified.

## VERIFY B — class-migration red team

Strongest objection: the same API does not make all workloads equivalent. It does
not. Catalog construction permits allocation; report-only contact benchmarks
observe normal-runtime costs; lunar timing has an explicit latency bar; translation
performance records sample distributions. These are distinguished above.

The31 ordinary B windows share a narrower outcome: already-warmed managed work,
fixture allocation outside the counter, exact-zero hot-path requirement, and no
intent to measure GC itself. State mutations, rejections and successful event
counts remain their original production contracts. Timeline collection counts
are diagnostic reporting, not a requirement that a GC occur. A common checked
boundary can preserve these outcomes without changing production or warmup.

For the5 timed E windows, blindly inserting entry after a stopwatch starts could
charge GC-entry cost to timing, while suppressing GC during the timed interval
could change benchmark meaning. Require explicit preservation/separation of timing
and allocation responsibilities before including these in a migration. Do not
waive their zero checks, add retries, change latency bars or assume that one giant
region around the suite is equivalent.

**The class recommendation survived for B; timed exceptions are explicit.**
No gate intentionally requires allocation-context retirement or GC overlap as its
production outcome. No gate should be retired merely because it failed.

## Decision and stop

**SMALL CLASS MIGRATION**: authorize one bounded Simulation measurement-contract
correction covering the equivalent ordinary raw-counter class, independent
assertion splits and deliberate-allocation proof, with the timed exceptions scoped
explicitly before restarting acceptance. No further per-gate allocation forensics
is needed merely to establish this common static weakness. This recommendation
does not establish the cause of Program.cs:1365's observed failure.

No migration, new positive control, source change or acceptance run was made in
this inventory ticket. Existing2155 disposable files /193681965 bytes were not
touched. No comparison tree or raw diagnostics were created. Only concise audit
evidence is retained; cleanup stays deferred.

Reproduce this static inventory using rg over tests/NovaCore.Simulation.Tests for
GetAllocatedBytesForCurrentThread, GetTotalAllocatedBytes, NoGCRegion, allocation
phrases and counter variables; inspect each opening/closing pair and its check;
follow Run/Performance/helper calls and Program.cs registrations. Exclude bin/obj,
then verify the project imports and direct production references. No compile or
runtime execution is necessary.

**STOP FOR PROJECT CONTROL.**
