# Ordinary-step event closure: bounded performance/payoff qualification

2026-09-15. **REVISE — PERFORMANCE REVISE. UNBANKED. STOP FOR PROJECT CONTROL.**

## WHAT HAPPENED

The first warmed allocation gate measured **1,936 bytes for ONE event-mapping
operation**, against exact zero. The candidate arithmetic and its projected
inputs match the retained off-COM witness. The declared allocation-first stop
was applied immediately: no A/B timing, further candidate process, solver run,
tiny-event measurement, optimization or allocation forensics followed.

This is a failure of the retained **evidence-only Rational/BigInteger mapper**
as a proposed hot-path mechanism. It is not evidence that the banked M14.24
fixed-limb resource ledger allocates, that exact arithmetic inherently allocates,
or that the ordinary-step physical closure is wrong. Earlier bounded physical
and ordered discrete friction-work findings remain accepted and unchanged.

[Plan](plan.md), [exact result](allocation-result.json),
[control witness](allocation-witness.txt), [harness](Program.cs),
[reproduction](reproduce.ps1), [identity and closeout](identity.json).

## CURRENT PERFORMANCE CONTRACT

The current [project state](../../../NOVACORE_CURRENT_STATE.md), standing
[engineering rules](../../../../ENGINEERING_RULES.md), and this issued ticket
establish:

- Preferred whole-frame headroom: **6.67–6.94 ms**, approximately 150 FPS class.
  The 8.33 ms terrain, 11.11 ms preferred fully featured game and 16.67 ms Ultra
  sustained targets have different scopes; none grants this mapper a frame budget.
- Warmed candidate-specific managed allocation: **exactly zero**. No allowance,
  subtraction, known-byte exception, averaging or retry.
- Timing uses normal runtime behavior; allocation uses a separate checked
  temporary no-GC window with entry/exit failures propagated.
- **NO NUMERIC SUBSYSTEM BUDGET EXISTS** for this isolated event mapper.
  Existing M14.18–M14.20 complete-operation ceilings of median 0.05 / P95 0.10 /
  P99 0.25 / max 0.50 ms remain applicable to their qualified integrated paths.
  They do not become a newly invented local mapper bar. See the
  [M14.20 boundary and measurements](../../contact-episode-servicing/README.md).

No whole-engine FPS or absolute runtime compatibility is inferred here.

## MEASUREMENT BOUNDARIES

| Boundary | Required work | Executed |
|---|---|---|
| A — baseline | Same retained native fixture, ordinary H, 8 iterations / 1 substep; no event-specific preparation | NOT RUN — allocation stop |
| B — candidate | Exact input read + duty/effective wrench/source-mass projection + same ordinary native step | NOT RUN — allocation stop |
| C — event preparation | Read immutable exact retained ledger values; hp/H; duty-scaled force; torque; mass/inertia divisions; gravity; backend input projections | ONE allocation operation after 128 warm calls; timing NOT RUN |
| D — tiny event | Optional exact-positive event cost | NOT RUN — allocation stop |

The harness mechanically extracts the **unchanged full Rational type** from
[the retained source](../Program.cs) and links the unchanged
[qualified helper](../../../../tests/NovaCore.Simulation.Tests/OrdinaryAllocationMeasurement.cs).
Input strings/JSON parsing, immutable raw inputs/constants, exact debit/partition
checks, reference checks and reporting are outside C. Duty, force, torque,
acceleration and source-mass projection are recomputed inside every Map call.
Nothing derived is cached. These are repeated reads/maps of one immutable
authorized witness, not repeated live ledger debit or canonical publication.

`Map` returns a value struct. All six consumed FP32 output bit patterns match
the accepted prepared acceleration, inverse mass and ordinary dt. Formatting,
arrays, LINQ, validation and serialization happen after the counter closes.
The read-only control is a harness control, **not baseline A**.

One fresh Release process ran, PID 49600, SDK 10.0.303, .NET 10.0.12, x64,
Windows 10.0.26200. No profiler, temporary friction dispatch, tracing, tiering,
PGO or GC environment changes. Two diagnostic builds passed with zero warnings
and errors: initial build and a pre-run reporting-only clarification. No broad
correctness/build campaign was run.

## SOLVER WORK

No solver was invoked by this allocation probe. The unchanged source and
accepted off-COM result establish **one ordinary Timestep / one substep / eight
velocity iterations per tested outer interval**, after 120 cold preparation
steps. The retained stage list has one collision-refresh observation before
that solve. No event-specific second refresh/solve/substep or manual cache pass
exists. These are accepted source/witness counts, not newly measured performance
counts; internal collision tasks are not counted independently.

Neither old read/report callbacks nor the friction-work temporary slot dispatch
is present in the measured program. No solver settings, physical thresholds,
mass treatment, timestep or contact rows changed.

## ALLOCATION / STATE

| Observation | Result |
|---|---:|
| Cold JSON reads/parsing and immutable exact-input setup, raw current-thread counter | 215,952 bytes |
| 128 warm Map calls, raw counter (context only) | 247,808 bytes |
| Independent byte[128] positive control, checked helper | 152 bytes; PASS |
| Immutable-input read/return control, checked helper | 0 bytes; PASS |
| ONE warmed C Map call, checked helper | **1,936 bytes; FAIL** |
| C required allocation | **0 bytes** |
| Checked entry / exit | PASS / PASS |
| Measured managed thread | 2 → 2 |
| Prepared projected output bits | MATCH, all six |

The raw warm total happens to equal 128 × 1,936 = 247,808; it is supplementary
context, not a replacement allocation gate or an allowance. The decisive result
is the single checked warmed call. No per-type object count, stack, individual
BigInteger operator attribution or runtime accounting mechanism was collected.
Do not invent that attribution from the source or byte magnitude.

GC generation counters were [2,2,2] before the wider region-entry/measurement/
exit bracket and [3,3,3] afterward. That bracket includes no-GC entry and helper
reporting. It is **not** the measured counter window and does not prove a GC
inside the workload. The helper opens its counter after successful entry,
closes before exit, and propagates any region failure. No claim is made about
the exact cause/timing of the wider bracket's collection.

State ownership remains distinct:

| State | Ownership / disposition |
|---|---|
| Exact resource/event ledger | Already banked M14.24 responsibility; current `PropellantInteger` uses 34 inline ulong limbs. This probe does not execute that ledger API. |
| Candidate event preparation | Immutable source values plus transient duty/wrench/mass arithmetic. The retained mapper uses BigInteger-backed rationals; its warmed mechanism fails the bar. |
| Native body/manifold/warm starts | Ordinary retained BEPU ownership, unchanged; not constructed by C. |
| Probe-only state | JSON documents, exact input holder, copied projected bits and final result; no solver/history state. |

No new long-lived candidate-specific solver history is demonstrated or required
by this bounded map. **Retained byte size/growth and native-world setup cost:
NOT MEASURED**, because the allocation stop fired first. The 215,952-byte setup
counter is not retained heap size. No 8 MiB qualification is claimed here.

## BASELINE / CANDIDATE / PREPARATION PERFORMANCE

All values below are intentionally unavailable, rather than zero:

| Boundary / fresh timing process | Median | P95 | P99 | Max |
|---|---|---|---|---|
| A baseline | NOT RUN | NOT RUN | NOT RUN | NOT RUN |
| B process 1 | NOT RUN | NOT RUN | NOT RUN | NOT RUN |
| B process 2 | NOT RUN | NOT RUN | NOT RUN | NOT RUN |
| B process 3 | NOT RUN | NOT RUN | NOT RUN | NOT RUN |
| C preparation timing | NOT RUN | NOT RUN | NOT RUN | NOT RUN |

**TAIL ANALYSIS:** no timing samples exist, so maxima, repeating tails,
synchronization/native dominance and baseline delta are unqualified. No FPS,
speedup or low-latency claim. The minimum three timing processes are a PASS
requirement; the explicit first-allocation stop prevents reaching that campaign.

Before any later matched timing campaign, its retained-fixture lifetime must
be specified: thousands of repeated powered intervals could leave the slab or
change event/resource/contact history. Resetting pose/cache each sample would
be a different lifecycle. This was identified statically, not investigated here.

## CANDIDATE A COMPARISON

**NOT DIRECTLY COMPARABLE.** Retained A D+12 timing covers numerical admission,
transport, duration/load/D/current-feasibility/sweeps and endpoint-proposal
export. It excludes native world, solver refresh, installation and publication.
Boundary B would include a real native step; no B measurements exist.

Historical values, microseconds, preserved from the
[original report](../../powered-contact-numerical-prerequisite/final-qualification/active-set-convergence/README.md):

| D+12 process | Median | P95 | P99 | Max |
|---|---:|---:|---:|---:|
| 1 | 59.8 | 66.4 | 101.0 | 122.5 |
| 2 | 60.4 | 62.9 | 93.2 | 119.2 |
| 3 | 59.5 | 63.8 | 97.6 | 123.4 |

The no-D process-2 maximum remains 827.8 us. Its cause is not assigned here.
No A rerun, altered result, favorable speedup comparison or retirement.

## STRUCTURAL PAYOFF

The bounded ordinary candidate needs the existing exact ledger, event fraction,
effective force/torque, one source-mass representation and ordinary retained
BEPU ownership. Its demonstrated solver ownership is materially simpler than A.
This prospective mechanism comparison does **not** grant runtime/payoff PASS.

| A mechanism / responsibility | Ordinary-step classification | Reason / retained responsibility |
|---|---|---|
| Event-sized constrained pieces | NOT NEEDED FOR EVENT CLOSURE | Effective input over one ordinary H; no hp-sized solve |
| Historical producing-lever cache provenance | NOT NEEDED FOR EVENT CLOSURE | No manually transported cross-piece response; native contact geometry remains |
| Generalized manual basis transport | NOT NEEDED FOR EVENT CLOSURE | BEPU owns ordinary manifolds/warm starts |
| Event-piece cache duration provenance | NOT NEEDED FOR EVENT CLOSURE | Exact ledger hp/H remains, but no transported event-cache duration |
| Duration cache scaling | NOT NEEDED FOR EVENT CLOSURE | No resized backend interval/cache |
| Common-normal load correction | NOT NEEDED FOR EVENT CLOSURE | No explicit previous/current event-piece correction |
| D operator | NOT NEEDED FOR EVENT CLOSURE | Ordinary native solve; no custom correction solve |
| Historical/current custom friction feasibility transport | NOT NEEDED FOR EVENT CLOSURE | Native ordinary friction replaces experimental cross-piece machinery |
| Native friction feasibility/contact constraints | STILL NEEDED FOR ORDINARY CONTACT | Tangent/twist/normal constraints and native solver ownership remain |
| Experimental 12-sweep policy | NOT NEEDED FOR EVENT CLOSURE | Qualified ordinary 8/1 retained, no added sweep |
| Active-friction tiny-piece machinery | NOT NEEDED FOR EVENT CLOSURE | Tiny exact authority stays in ledger; no tiny backend duration |
| Paired private-response qualification/integration | UNRESOLVED | Copied endpoint/canonical admission must still be designed against real ownership; native body outputs do not prove that integration |

No mechanism, evidence tree or Candidate A code has been retired.

## KSA CONVERGENCE

This ticket expressly permits reuse of accepted build-5438 lifecycle findings
because it introduces no new mechanism. Reused provenance is the actual prior
inspection of `E:\Kitten Space Agency`, build
`2026.9.10.5438+c9291b5dd3347e847020c8fb3dd11dfce6a728e9`, and actual Discord
live-changelog channel 1260112103134724146 / server 1260011486735241329.
See [resolved source closure](../../powered-contact-event-lifecycle-convergence/ksa-current-lifecycle.md)
and [history identities](../../powered-contact-event-lifecycle-convergence/ksa-live-changelog.md).
This is authorized evidence reuse, **not a fresh installation/history review**.
Retained solver ownership is the accepted convergence direction. No new KSA
allocation, batching, performance, resource-cost or lifetime claim is made.
Any undocumented performance motivation remains **RATIONALE UNPROVEN**.

## RED TEAM

Independent read-only verifier reviewed the plan, extraction, mapping boundary,
generated type, controls and actual result. **PASS on evidence integrity and
the STOP/REVISE decision; no performance acceptance.**

The strongest objection was that setup/precomputed inputs could hide candidate
work. Raw immutable values alone are prepared cold; duty/wrench/mass divisions
and projections remain inside Map, with exact original operators and verification
of all six FP32 bit patterns. The helper's control is not misreported as A. Positive
control 152 and read control zero support the measurement boundary. Object/stack
attribution is explicitly absent. Wider GC counts are not labeled workload GC.

Timing attacks (JIT/first touch, cherry-picking three processes, concealed max,
baseline mismatch, whole-frame inference) cannot yield PASS because no timings
were taken. State growth is marked unmeasured. Solver quality, exact authority,
physical bars, old candidate/evidence and KSA claims are unchanged. The friction
observer/dispatch is not referenced. No optimization or production integration
occurred. Future fixture-lifetime comparability remains an open design condition.

## SOURCE / EVIDENCE / DISPOSABLE CLEANUP

HEAD/main/origin/main/remote main: `49057fecceb0f725d5f551ec40e2780971b0d81d`.
Branch: `codex/powered-contact-numerical-prerequisite`. Protected M15.0 commit:
`4607d8c802006d5e1a01c595ab608cf53a4dab6b`. All 65 historical tag objects and
all 2,450 starting file fingerprints remain protected. Closing checks are in
[identity.json](identity.json). No production/permanent-test/tracked-file edits,
staging, commit, tag, push, merge, banking, renderer or Blender work.

Retain this small evidence child plus the unchanged parent witnesses. Reproduce
using PowerShell 7: `& '.\reproduce.ps1'` from this child, or its absolute path.
The script pins inputs/source/helper and refuses an existing scratch directory.
Expected exit 2 is the recorded allocation stop, not a technical harness failure.
Reproduction writes only disposable output; it never overwrites retained results.

New disposable root: `E:\NovaCore\build\powered-contact-ordinary-step-performance-payoff`.
Inventory before cleanup: **35 files / 1,536,559 bytes**. Composition:

| Exact path | Files | Bytes |
|---|---:|---:|
| `E:\NovaCore\build\powered-contact-ordinary-step-performance-payoff\bin` | 9 | 1,260,501 |
| `E:\NovaCore\build\powered-contact-ordinary-step-performance-payoff\obj` | 22 | 272,652 |
| `E:\NovaCore\build\powered-contact-ordinary-step-performance-payoff\PreparationAllocation.csproj` | 1 | 487 |
| `E:\NovaCore\build\powered-contact-ordinary-step-performance-payoff\Rational.cs` | 1 | 1,365 |
| `E:\NovaCore\build\powered-contact-ordinary-step-performance-payoff\allocation-result.json` | 1 | 1,155 |
| `E:\NovaCore\build\powered-contact-ordinary-step-performance-payoff\allocation.stdout.txt` | 1 | 399 |

The generated project/type/build/result are reproduced by this child's script
plus pinned parent/helper source. `allocation.stdout.txt` is the console transcript
captured during the sole run; the script prints those controls but does not itself
save that transcript. Useful result/control bytes were copied here before cleanup.
The resolved root is inside the intended build directory and contains no reparse
points. Prior `E:\NovaCore\build\powered-contact-friction-work` is absent (0 files /
0 bytes); no deletion was attempted there. The sole reviewed new-root cleanup was
rejected before execution by automatic approval review: **blocked by policy**.
No retry or workaround occurred. **Removed: 0 files / 0 bytes. Remaining: 35 files /
1,536,559 bytes. Manual Project Control cleanup required**, as recorded in
identity.json. Cleanup does not alter the engineering judgment.

Exact reviewed manual cleanup command (this new root only):

```powershell
Remove-Item -LiteralPath 'E:\NovaCore\build\powered-contact-ordinary-step-performance-payoff' -Recurse -Force
```

Non-destructive verification, including a remaining-content count if present:

```powershell
$p = 'E:\NovaCore\build\powered-contact-ordinary-step-performance-payoff'
if (Test-Path -LiteralPath $p) {
    $f = @(Get-ChildItem -LiteralPath $p -File -Recurse)
    [pscustomobject]@{ Path = $p; Exists = $true; Files = $f.Count; Bytes = ($f | Measure-Object Length -Sum).Sum }
} else {
    [pscustomobject]@{ Path = $p; Exists = $false; Files = 0; Bytes = 0 }
}
```

## JUDGMENT / WHAT HAPPENS NEXT

**REVISE.** The unchanged retained mapper fails exact-zero allocation before
runtime compatibility can be qualified. The structural simplification remains
promising, but the combined performance/payoff PASS bar is not met.

Return the mapper's **1,936-byte warmed preparation** to Project Control for one
bounded mechanism decision. Do not infer an approved optimization or proceed to
production integration architecture under the PASS-only rule. No additional
measurement, forensics, solver change, Candidate A retirement, test migration,
M15.1 assignment or Florida work is authorized by this result.

**UNBANKED. STOP FOR PROJECT CONTROL.**
