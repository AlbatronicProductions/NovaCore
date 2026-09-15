# Event-preparation allocation revision — semantic stop

2026-09-15. **REVISE FAILED. UNBANKED. STOP FOR PROJECT CONTROL.**

## WHAT HAPPENED

The 1,936-byte warmed mapper allocation was assigned to its rational arithmetic
and final projections with matching original, instrumented and summed scope
counters. ONE diagnostic class-B reuse of unchanged banked fixed-workspace
numerics was then evaluated. Its exact input ledger and all six FP32 outputs
match, but **four existing binary64 projections differ by one ULP each**.

The ticket's semantic hard stop was applied. Corrected warmed allocation checks,
determinism qualification and the A/B/C timing campaign were **NOT RUN**. No
second arithmetic rescue, production edit, solver execution or banking occurred.
This disproves the tested composition under the frozen bitwise gate; it does
not prove all banked reuse or all allocation-free formulations impossible.

Retained: [predeclared plan/boundary classification](plan.md),
[cause measurements](allocation-cause.json), [allocation transcript](attribution-witness.txt),
[reuse decision](reuse-decision.md), [semantic comparison](corrected-semantics.json),
[stop transcript](correction-witness.txt), [identity](identity.json),
[attribution harness](Attribution.cs), [single reuse attempt](Correction.cs),
[reproduction script](reproduce.ps1).

## ALLOCATION CAUSE

One fresh Release attribution process used the unchanged old Rational type and
Map body. After 128 warm calls each, the qualified checked helper measured:

| Observation | Bytes |
|---|---:|
| Deliberate byte[128] control | 152 |
| Empty scope-counter/input-read control | 0 |
| Original complete mapper | 1,936 |
| Scope-instrumented mapper, complete window | 1,936 |
| Sum of individual scope deltas | 1,936 |

All entry/exit operations passed. Instrumentation reads the counter and stores
longs in stack storage. There is no delegate, boxing, formatting, LINQ, array
construction or reference comparison inside the scopes. Outputs are consumed and
checked after measurement. The original and instrumented FP32 results agree.

| Scope, each called once per map | Bytes |
|---|---:|
| hp/H | 128 |
| Fx*duty | 128 |
| Fy*duty | 128 |
| -1*e | 0 |
| (-e)*effectiveFy | 128 |
| e*effectiveFx | 128 |
| effectiveFx/mass | 192 |
| effectiveFy/mass | 192 |
| ay-gravity | 240 |
| torqueX/inertia | 128 |
| torqueY/inertia | 128 |
| ax.Value | 64 |
| ay.Value | 64 |
| wx.Value | 0 |
| wy.Value | 64 |
| 1/mass | 160 |
| inverseMass.Value | 64 |
| H.Value | 0 |

Arithmetic/reduction contributes **1,680 bytes**; four allocating Value calls
contribute **256 bytes**. There are 12 Rational arithmetic calls (six divisions,
five multiplications, one subtraction), of which 11 allocate for this input;
six Value calls, of which four allocate. No unassigned helper/dispatch/reporting
remainder exists at this boundary.

The Rational value wrapper itself is not a heap object. Its operators construct
BigInteger products, GCD and reduced quotients; Value constructs shifted integers.
Actual loaded `System.Runtime.Numerics.dll` is
`10.0.12+95017c711e6afc1085133d440e42b4bd78155701`. Cold reflection/IL inspection
shows BigInteger._bits is **System.UInt32[]**, and its span-result constructors
contain `newarr`/`ToArray` allocation sites. This identifies array-backed runtime
arithmetic ownership, together with the exact dynamic scope deltas above.

**Individual object count, constructor branch hits and allocation stacks were
not observed.** The table counts arithmetic calls, not objects. No profiler,
EventPipe or runtime configuration changes were needed. No allocation is assigned
to banked M14.24 or to exact arithmetic as a general requirement.

## BOUNDARY CLASSIFICATION

Production-required outcomes: exact duration/input read, hp/H event weighting,
effective force/torque, source-mass treatment, and physical double/float projections.
They must remain in any production-representative hot measurement.

Evidence-only mechanisms: arbitrary-precision Rational construction, GCD
normalization and Rational operators. Those types were never a banked production
ownership commitment. The unboxed Prepared/Projected value returns are not the
allocation source.

Setup/reference/diagnostics: string/JSON parsing, immutable raw input/constants,
reference/debit validation, report arrays and formatting. The old input-holder
object is cold. Lambdas occur in JSON selection/report construction, not Map.
Removing the required mapping arithmetic would have been invalid; replacing
its evidence representation was the authorized correction being assessed.

The full operation classification is in [plan.md](plan.md). No required work
was hidden before the measurement window and no per-event answers were cached.

## CURRENT BANKED NUMERICS

Directly inspected current production files:

- `src/NovaCore.Simulation/Spacecraft/Resources/PropellantInteger.cs`:
  34 inline ulong limbs, bounded exact add/subtract and multiply-by-ulong;
  `PropellantDuration` is a ratio **in ticks**, never a spending capability.
- `src/NovaCore.Simulation/Spacecraft/Resources/FinitePropellantSegmentation.cs`:
  exact available/flow comparison, consumed/successor units and duration ratios;
  its Calculate result does not independently grant canonical authority.
- `src/NovaCore.Simulation/Spacecraft/Actuation/PoweredFlightNumerics.cs`:
  TryRatio normalizes and rounds using two stackalloc buffers of 35 words;
  TrySeconds applies the 1,000,000 ticks/second denominator factor.
  PoweredBinaryScale retains an independent exponent through MultiplyDivide.
- `tests/NovaCore.Simulation.Tests/PoweredFreeFlightTests.cs`:
  existing arithmetic coverage includes positive duration with zero ranged
  seconds but a nonzero final weighted impulse. Read only; not rerun here.

These primitives use fixed workspace, not growing arbitrary precision. They
were linked unchanged into the diagnostic assembly; no accessibility/API edit
was needed. TryRatio's proper 53-bit ratio rounding is different from the old
Rational.Value operand-truncation/cast/division procedure. Source-mass/configuration
projection also changes where rounding occurs. Reuse therefore needed the explicit
comparison; absence of allocation alone could not establish semantic equivalence.

## KSA EQUIVALENCE

**NO GENUINELY EQUIVALENT KSA EXACT-EVENT MAPPER RESPONSIBILITY.**

This is the scoped distinction explicitly authorized by this ticket, using the
accepted [current-source closure](../../../powered-contact-event-lifecycle-convergence/ksa-current-lifecycle.md)
and [actual-history provenance](../../../powered-contact-event-lifecycle-convergence/ksa-live-changelog.md).
The accepted installation identity is `E:\Kitten Space Agency`, build
`2026.9.10.5438+c9291b5dd3347e847020c8fb3dd11dfce6a728e9`; history was the actual
Discord live-changelog channel 1260112103134724146, server 1260011486735241329.
No fresh source/history inspection is claimed in this revision. No new equivalent
KSA responsibility, performance rationale, or claim about all KSA event handling
was invented. Exact NovaCore resource/subnormal authority remains an intentional
product difference. KSA files and Blender were untouched by this work.

## CORRECTION

ONE class-B **reuse composition**, diagnostic only:

- Cold raw integer import uses existing fixed multiply/add, with overflow refusal.
- Per call, construct exact tick-ratio duration from hp seconds and form duty
  using the unchanged rational H. No rounded 16,666/16,667-tick substitution.
- Project source mass/physical configuration through banked TryRatio.
- Apply weighted force/mass and torque/inertia through PoweredBinaryScale,
  retaining the independent duty exponent until the weighted result.
- Return six double projections and their six FP32 projections.

Ordinary H, source mass, exact raw inputs, solver policy and authoritative ledger
remain unchanged. No BEPU/native step occurred. The proposed map has no Rational
call, pool, cache, callback, shared mutable workspace or per-event answer store.
Its composition moves rounding boundaries; it was **not adopted** after the
comparison failed. This is a failed candidate reuse, not a banked arithmetic defect.

## SEMANTIC EQUIVALENCE

The frozen off-COM witness uses exact lever **1/10**, gravity **981/100**,
source mass **295152709018955354385/36893488147419103232**, exact hp
**4803839602528529/576460752303423488**, and ordinary H **1/60** seconds.
Exact imported duration/source-mass integers, H, and fuel-debit equality passed
the comparison. Neither path spends fuel or publishes canonical state.

| Projection | Old binary64 | Proposed binary64 | Binary64 result | FP32 result |
|---|---:|---:|---|---|
| ax | 2.999951172669716 | 2.9999511726697157 | FAIL, 1 ULP | IDENTICAL |
| ay | -5.810065103107045 | -5.810065103107046 | FAIL, 1 ULP | IDENTICAL |
| wx | -1.5999999999999999 | -1.6 | FAIL, 1 ULP | IDENTICAL |
| wy | 1.2 | 1.2000000000000002 | FAIL, 1 ULP | IDENTICAL |
| inverse mass | 0.12499796552790482 | Same | IDENTICAL | IDENTICAL |
| H | 0.016666666666666666 | Same | IDENTICAL | IDENTICAL |

All exact bit integers are retained in [corrected-semantics.json](corrected-semantics.json).
FP32 identities alone cannot pass this ticket's existing-binary64 clause. No
tolerance, force special case, rounding patch or alternate arithmetic followed.
The banked primitives remain valid for their own accepted numerical contract;
this result does not establish a missing production API or authorize changing one.

## ALLOCATION / RETAINED STATE / DETERMINISM

| Required corrected gate | Result |
|---|---|
| Immutable-input control | NOT RUN — semantic stop (old attribution control: 0) |
| Positive allocation control | NOT RUN — semantic stop (attribution control: 152) |
| Three isolated warmed single calls | NOT RUN — semantic stop |
| One bounded repeated-call check | NOT RUN — semantic stop |
| Corrected exact-zero result | **NOT QUALIFIED**, not a measured nonzero result |
| Repeated same-process corrected double/float determinism | NOT RUN — semantic stop |

The strengthened checker for all six double and float bit patterns exists on the
hypothetical success branch, but no execution reached it. Do not infer zero
allocation from stack-oriented source alone.

Source/lifetime audit: corrected FixedInput is a readonly value struct containing
raw fixed integers and signs; passed by `in`. Map uses local value results and
banked stack spans. It adds no static state, heap-owned input holder, cache, pool,
thread-local storage, generation owner or mutable shared workspace. Diagnostic
JSON/oracle objects and Attribution.Names exist outside the proposed map. No
retained solver/history state is introduced. Dynamic retained-byte size/growth
was not measured after the semantic stop and no storage qualification is claimed.

## PERFORMANCE CAMPAIGN

**NOT RESUMED.** Baseline A, candidate B processes 1/2/3, preparation C timing,
tiny D, tails and solver/performance/state-growth measurements are all NOT RUN.
The hard pass gate was not met. Accepted ordinary ownership remains one H step,
one substep, eight iterations in prior source/evidence; this ticket ran zero
solver calls. Candidate A was neither rerun nor retired; its numerical-only
timing remains NOT DIRECTLY COMPARABLE to an unmeasured native-world B.
The prior structural comparison is unchanged, not promoted to payoff acceptance.

## RED TEAM

Independent read-only review **PASS on attribution integrity and the
STOP / REVISE FAILED decision**, not on corrected qualification.

The verifier checked exact scope reconciliation, original-source extraction,
current numerical implementations, complete weighted-product handling, H units,
raw input import, bit comparisons and the absence of hidden hot Rational state.
It flagged early ratio/mass/configuration rounding before execution; the sole
comparison then demonstrated four one-ULP differences. The repeat checker was
strengthened before that sole run to compare six double and float bit patterns.

No helper allocation is reassigned to production; no required mapping is moved
outside the boundary; no pool/cache hides work; no witness value is special-cased;
no exact authority/physical bar is relaxed; no production edit is made for access;
no equivalent current primitive is ignored; no KSA-equivalent mechanism is invented.
No allocation average or no-GC tolerance is used. Warmed corrected zero, repeated
determinism and retained growth are explicitly unmeasured. Timing did not start;
Candidate A and production acceptance are not claimed. This one composition's
failure is not generalized to all possible fixed-workspace reuse.

## SOURCE / EVIDENCE

HEAD/main/origin/main/remote main remain
`49057fecceb0f725d5f551ec40e2780971b0d81d`; branch
`codex/powered-contact-numerical-prerequisite`. M14.21–M14.24, M15.0 and all
65 historical tag objects are protected. All **2,457 pre-existing file bytes**
are compared by SHA-256 aggregate before/after; only this new child is excluded
from the closing comparison. Details and current Git checks: [identity.json](identity.json).

Two Release diagnostic builds passed, zero warnings/errors. Exactly two fresh
processes executed: attribution, then the single semantic reuse comparison.
No full solution, permanent test suite, native solver, performance campaign,
renderer, KSA or Blender execution occurred. No production/permanent-test edits,
staging, commit, push, merge, tag, milestone assignment or banking.

Reproduction, PowerShell 7 (writes fresh disposable subdirectories only):

```powershell
$script = 'E:\NovaCore\docs\engineering-evidence\powered-contact-ordinary-step-event-closure\performance-payoff\allocation-closure\reproduce.ps1'
& $script -Mode Attribution
# Inspect the reconciled cause before the one separately gated comparison:
& $script -Mode Correction
```

The script refuses existing output, preserves source/helper/input pins, and links
current banked source. Expected corrected executable status is 3 with
STOP_SEMANTIC_MISMATCH_REVISE_FAILED; it is a semantic refusal, not a build failure.
No command overwrites retained result evidence. Do not execute another correction
as a response to this reproduced stop.

## DISPOSABLE CLEANUP

Reviewed new scratch root:
`E:\NovaCore\build\powered-contact-event-allocation-closure`.
**72 files / 3,178,060 bytes**, all rebuildable diagnostic outputs, copied runtime
dependencies, generated exact source extracts, project files and duplicate results.
The useful measurements/transcripts are retained here. Reproduction comes from
this child plus unchanged pinned parent/production/helper files.

| Exact disposable subdirectory | Files | Bytes |
|---|---:|---:|
| `E:\NovaCore\build\powered-contact-event-allocation-closure\attribution` | 36 | 1,561,824 |
| `E:\NovaCore\build\powered-contact-event-allocation-closure\correction` | 36 | 1,616,236 |

The full bin/obj/generated-file composition is in identity.json. The resolved
root is within `E:\NovaCore\build` and has no reparse points. Existing previously
reported `E:\NovaCore\build\powered-contact-ordinary-step-performance-payoff`
is currently **absent: 0 files / 0 bytes**. No deletion retry was attempted there.

The new scratch is left for the reviewed manual cleanup below. **Removed in this
ticket: 0 files / 0 bytes. Remaining: 72 files / 3,178,060 bytes.** No automated
deletion was attempted or claimed blocked in this ticket. Cleanup does not change
the engineering judgment. This review grants no broader deletion target.

Exact copy/paste-ready manual cleanup:

```powershell
Remove-Item -LiteralPath 'E:\NovaCore\build\powered-contact-event-allocation-closure' -Recurse -Force
```

Non-destructive verification:

```powershell
$paths = @(
    'E:\NovaCore\build\powered-contact-event-allocation-closure\attribution',
    'E:\NovaCore\build\powered-contact-event-allocation-closure\correction'
)
foreach ($p in $paths) {
    if (Test-Path -LiteralPath $p) {
        $f = @(Get-ChildItem -LiteralPath $p -File -Recurse -Force)
        [pscustomobject]@{ Path = $p; Exists = $true; Files = $f.Count; Bytes = ($f | Measure-Object Length -Sum).Sum }
    } else {
        [pscustomobject]@{ Path = $p; Exists = $false; Files = 0; Bytes = 0 }
    }
}
```

## JUDGMENT / WHAT HAPPENS NEXT

**REVISE FAILED.** Allocation ownership is assigned, but the sole banked-numerics
reuse composition fails the frozen binary64 identity requirement. All six FP32
outputs matching does not override that failure.

Project Control must decide the next explicitly bounded numerical responsibility
against this projection-semantic evidence. No production API delta is proven
necessary, and no second mapper or relaxed bit contract is authorized here.
Performance/payoff qualification and production integration remain pending.

**UNBANKED. STOP FOR PROJECT CONTROL.**
