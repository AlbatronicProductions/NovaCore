> Historical fallback evidence. Original judgments and measurements remain unchanged. Old campaign commands/counts describe their original runs; current reproduction and retirement records are in [../../../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md](../../../powered-contact-ordinary-step-event-closure/REPRODUCTION-INDEX.md).

# First-coast measured payoff closure

**PASS — MEASURED PAYOFF CLOSED / CORRECTION RECOMMENDED. Recommendation: D, for the declared restricted domain.** No correction adoption, cache installation or full numerical-prerequisite resumption.

This is the newly authorized bounded measurement closure. The prior [causal report](../README.md), including its accurate historical missing-measurement limitation, remains unchanged. This directory supplies the actual measured payoff that was missing there.

## Baseline / scope

HEAD/main/origin/main/remote main: `49057fecceb0f725d5f551ec40e2780971b0d81d`. Branch: `codex/powered-contact-numerical-prerequisite`. Protected M14.21–M14.24/M15.0 refs remain unchanged; M15.0 commit `4607d8c802006d5e1a01c595ab608cf53a4dab6b`.

The predeclared [plan](plan.md) bounds one Debug correctness process and one fresh Release direct-cost/allocation process. SDK 10.0.303; runtime .NET 10.0.12. Debug and Release diagnostic builds each passed once, zero warnings/errors. All 2,104 pre-existing nonignored files are preserved; production files, permanent tests, previous diagnostics and previous evidence are unchanged. New source is diagnostic evidence only. No Blender work.

## Actual measured D payoff

Original retained `CoastPayoff.RemoveCommonResidual` and unchanged `PieceKernel.Solve` are source-linked. Every completed case is written with CreateNew, WriteThrough and Flush(true), then closed before proceeding. Physical FAIL data would be saved before stopping. All ten per-case records survive the completed process and later Release run; two expected refusals do not abort the matrix or invoke the solver.

| Case | Normal error N s | Linear error m/s | Angular error rad/s | Equation residual maximum | Outcome |
| --- | --- | --- | --- | --- | --- |
| [original-coast](results/01-coast-D.json) | 5.06884889617254e-9 | 6.809610599826729e-9 | 1.3569883411937979e-8 | 2.1580527354111273e-8 | PASS |
| [identical](results/05-identical-D.json) | 0.000039080373235522714 | 0.000007089481269170275 | 0.000010275614599611217 | 0.000026499440572674837 | PASS |
| [tiny-normal](results/06-tiny-normal-D.json) | 0.00003908045053979681 | 0.00000708949467611282 | 0.00001027563322384576 | 0.000026499479638061424 | PASS |
| [tangent-only](results/07-tangent-only-D.json) | 0.00003908037323541169 | 0.0000070894812691593466 | 0.000010275614599637702 | 0.000026499440572661054 | PASS |
| [lever-only](results/08-lever-only-D.json) | 0.000039109239025281184 | 0.0000070957622060732525 | 0.000010284029868298154 | 0.00002652315479979341 | PASS |

All physical bars remain **1e-4** in their respective units. Equation residuals are reported separately: normal/tangent rows use m/s, twist uses rad/s, so the seven-row maximum is a mixed-unit diagnostic numeric check. It is not another 1e-4 physical acceptance gate. Coupled references remain admissible and have residual <=1e-12. All five measured D trajectories have zero clamp counts.

Exact coast delta: **-0.01817377979607364 N s per normal**.

Initialized cache, order N0,N1,N2,N3,T0,T1,twist:
```text
[0.1577607402253669,0.1577629610118808,0.15776640437537087,0.15776470251291225,0.0000019601729285241626,-5.190218637876209e-7,-1.6760502216395334e-15]
```
Actual eight-sweep final cache:
```text
[0.157764620843008,0.15776283122828627,0.15776305424143047,0.15776430688165358,-0.0000015230503375353219,5.369488964785512e-7,1.6062289769863544e-15]
```
Normal/tangent cache units N s; twist N m s. Full initial/final velocity vectors, reference, all error operands, equation/seed hashes and current geometry are retained in [01-coast-D.json](results/01-coast-D.json). [Measured index](measured-results.json) links all five actual payoffs and the two admission-only witnesses.

These are newly measured candidate results, not the prior arithmetic reconstruction relabelled. Their agreement with that reconstruction is a consequence of unchanged equations.

## Ordinary sweep alternative

| Ordinary sweeps | Normal error N s | Linear error m/s | Angular error rad/s | Outcome |
| --- | --- | --- | --- | --- |
| 9 | 0.0001983637052881404 | 0.00006306617555079572 | 0.00011605795254128146 | FAIL |
| 10 | 0.00013603468940759367 | 0.00004625091261748741 | 0.00008608505945279062 | FAIL |
| 11 | 0.00009267637410592666 | 0.000033270019022461644 | 0.00006242086067353624 | PASS |

**Eleven is the smallest tested passing ordinary count for this exact coast.** Eight also fails; twelve was already measured passing and was not rerun. Counts 9, 10 and 11 use the same initial cache, equation/seed hash, geometry, mass, h, softness, friction and ordering, with D disabled.

Eleven's normal error is only approximately 7.32e-6 N s below the 1e-4 bar. Its equation residual is 1.1086606380708e-4, which is not a failure of the three physical-error gates. This is not a general minimum for other pieces or all possible sweep counts. See [sweep comparison](sweep-comparison.json).

## Expected friction refusals

[Moderate-normal](results/09-moderate-normal-refusal.json) and [normal-and-tangent](results/10-normal-and-tangent-refusal.json) remain **CurrentFrictionCap** refusals (enum value 4 in raw JSON). Each performs admission only: solver calls 0, reference calls 0, input nonmutation true.

Their tangent magnitudes are approximately 0.09002167204944703 N s and proposed caps approximately 0.08654382766644907 N s. Last-bit differences between rotated tangent representations are retained as measured. No clamp, cap increase, cache clearing or inadmissible D solve occurs. The scratch proposal is not installed anywhere.

## D domain and equation identity

D is exactly:
```text
r = A * acceptedGuess - rhs
q = (1,1,1,1,0,0,0)
delta = -(q^T*r)/(q^T*A*q)
D = acceptedGuess + delta*q
```

Required domain:
- the existing compatible retained four-normal/two-tangent/one-twist contact representation;
- accepted duration and restricted basis/cache preparation already complete;
- current finite physical equation inputs, positive mass/inertia and valid ordinary h/softness;
- finite, positive initial normals with current tangent and twist cache strictly inside their caps;
- finite current residual and finite positive q^T A q, with finite delta;
- after the common shift, finite positive normals and current tangent/twist feasibility remain valid.

q is deliberately fixed to this four-normal system. It is not a general contact-count or active-set policy. For the positive-mass/inertia current system, K=J M^-1 J^T and positive normal softness give a positive common denominator; the actual coast value is 2.4899770322354415. No arbitrary epsilon is added. Numerically degenerate/nonfinite results refuse. This is the ordinary captured duration domain; underflow/overflow-extreme h and tiny-event arithmetic are not qualified.

D receives current patch/free velocity/h/omega and the accepted guess. It forms exactly the current A/rhs terms internally, so their preparation cost is included in D timing; no hidden precomputed reference residual is supplied. Neither preparation API accepts an oracle or historical successor answer. The original D is executed before reference construction. It changes only the four normal guesses by one scalar and preserves tangent/twist and differential normal information. Baseline/current equation and seed hashes are unchanged before/after.

Feasible initialization is necessary admission, **not a theorem that every admitted future case passes eight sweeps**. Measured payoff covers the exact coast and four named synthetics; two rotated cases remain outside this proposal's domain. No friction fallback or D-plus-extra-sweeps combination is proposed.

## Allocation-safe diagnostic adapter

The original correctness API allocates a cloned cache; the original diagnostic D also clones and uses LINQ. Those APIs are not used inside timing/allocation windows.

[HotKernel.cs](HotKernel.cs) removes only storage conveniences: caller resets one existing double[7] buffer, D computes/checks its scalar shift in that buffer, then the original solver body executes with identical expressions, row order, weighted bias, denominator guard and clamps. Endpoint velocity reconstruction is unchanged. A mechanical text comparison checks the solver-body transformation; all seven output impulses, six velocity components and three clamp counts are bit-identical to the source-linked original, including fresh Release verification for all three timing arms.

No reference, JSON, reflection, LINQ, object creation or reporting occurs inside a timed/allocated call. This is diagnostic harness preparation, not a production solver correction.

## Direct cost — one fresh Release process

Process 8728; 4,096 fixed warm calls per arm; 4,096 **individual** measured calls per arm. Order rotates ABC/BCA/CAB per round. Every call starts from the same captured seed. Includes seed copy, D domain checks/current-equation preparation when applicable, solve and endpoint velocity reconstruction.

| Local operator | Median us | P95 us | P99 us | Maximum us |
| --- | --- | --- | --- | --- |
| Ordinary 8 | 2.9 | 3 | 3 | 5.9 |
| D + 8 | 3.4 | 3.5 | 3.5 | 10.7 |
| Ordinary 11 | 3.9 | 4 | 4 | 7 |

Absolute D+8 minus ordinary11 differences: median **-0.5 us**, P95 **-0.5 us**, P99 **-0.5 us**, maximum **+3.7 us**. D+8 versus failing ordinary8: median/P95/P99 **+0.5 us**, maximum **+4.8 us**. Ordinary11 versus ordinary8 median cost is +1.0 us.

Every chronological quartile-block median is stable at 2.9, 3.4 and 3.9 us for the respective arms. Timestamp frequency 10 MHz gives 0.1 us granularity. Raw timestamp-pair overhead: median 0, P95/P99/max 0.1 us. No subtraction. Nearest-rank P95/P99; arithmetic midpoint median.

**D has the worse observed maximum**, 10.7 versus 7.0 us. Its cause is unassigned; no repeat or trace campaign was used to explain or remove it. D is not the winner for every timing statistic. One local process does not establish whole-frame headroom, cross-machine cost, or a worst-tail guarantee. [timing.json](results/timing.json) is the original durable measurement.

## Allocation

Warmed D preparation + eight-sweep local operator, **4,096 calls: 0 managed bytes**. Checked no-GC entry PASS; exit PASS. The unchanged `OrdinaryAllocationMeasurement` uses the existing bounded 1 MiB reservation; counter opens after entry and closes before exit/reporting.

Independent NoInlining `byte[128]` positive control under the same helper: **152 bytes**, nonzero detection PASS. No subtraction/tolerance/retry. [allocation.json](results/allocation.json) retains actual operands.

This qualifies the caller-owned diagnostic local path only. It does not claim zero allocations for the original allocating diagnostic API, cold parsing/fixture construction, a retained world, or full prerequisite integration.

## Storage / lifecycle / payoff comparison

No additional historical state is inherently required. D uses current geometry/equations, current h/free velocity and the prepared cache. Its temporary scalar preparation result is a value type. The caller-owned work buffer has 56 payload bytes (seven doubles), shared by all three alternatives; runtime object-header size is not a retained-storage measurement here. No new persistent history, map, world binding or lifecycle resource is introduced.

Ordinary11 is simpler: change only an iteration count. D additionally needs explicit current-equation and pre/post-friction admission, and refusal handling in any future integration. Those conditions are small and local but real responsibilities.

| Criterion | D + 8 | Ordinary 11 |
| --- | --- | --- |
| Exact coast | Measured PASS, much greater accuracy margin | Measured PASS, normal close to bar |
| Four named synthetics | Measured PASS | Not tested at 11; do not infer |
| Two rotated-normal cases | Explicit admission refusal | Not qualified by this comparison |
| Median / P95 / P99 cost | 0.5 us lower | 0.5 us higher |
| Observed maximum | 10.7 us, worse | 7.0 us |
| D-path allocation | Qualified 0 local bytes | Same reusable solver storage; separate allocation result not claimed |
| Additional historical storage | None inherently required | None |
| Lifecycle complexity | Current residual + admissibility/refusal | Simpler iteration parameter |
| Future generality | Restricted and still requires broader qualification | No universal sufficiency of 11 established |

**Recommendation: D**, within the declared restricted domain. The measured accuracy improvement and four synthetic passes, favorable median/P95/P99 absolute cost, qualified local zero allocation and absence of new history justify choosing it for Project Control consideration despite the extra local checks. The worse measured maximum and unqualified outside-domain cases remain explicit limits. No automatic fallback to extra sweeps is part of this recommendation.

## Reproduction and retention

[reproduce.ps1 — retired record](../../../powered-contact-ordinary-step-event-closure/CONSOLIDATION-MANIFEST.md) verifies all prior retained input/artifact hashes, branch/HEAD and SDK before each phase. For a separately authorized fresh reproduction whose new output and results directories do not already exist:

```powershell
& 'docs/engineering-evidence/powered-contact-numerical-prerequisite/coast-accuracy/payoff/reproduce.ps1' -Phase BuildDebug
& 'docs/engineering-evidence/powered-contact-numerical-prerequisite/coast-accuracy/payoff/reproduce.ps1' -Phase Correctness
& 'docs/engineering-evidence/powered-contact-numerical-prerequisite/coast-accuracy/payoff/reproduce.ps1' -Phase BuildRelease
& 'docs/engineering-evidence/powered-contact-numerical-prerequisite/coast-accuracy/payoff/reproduce.ps1' -Phase Cost
```

Use a disposable copy/checkout for reproduction so retained case files remain immutable. Existing attempt files prevent overwrite or retry. Do not delete original evidence to make the script run.

Retain per-case numerical records, timing/allocation summaries, original diagnostic source, plans, hashes and this report. Raw timing sample arrays were not retained; the bounded workload/sampling code and original statistics are retained. [Verification](verification.md) records all 20 independent attacks. [Identity](identity.json) records source/result hashes, preservation, refs and disposable-output disposition.

The reviewed disposable build tree is `E:\NovaCore\build\powered-contact-coast-payoff`: 58 files, 4,321,667 bytes (generated project, Debug/Release bin/obj outputs). Numerical records live separately in this evidence directory. One cleanup attempt was rejected by automatic approval review before execution as `blocked by policy`; removed 0, no retry or workaround. This does not change the numerical payoff judgment.

Manual cleanup is limited to the reviewed path:
```powershell
Remove-Item -LiteralPath 'E:\NovaCore\build\powered-contact-coast-payoff' -Recurse -Force
```

## Judgment / stop

**PASS — MEASURED PAYOFF CLOSED / CORRECTION RECOMMENDED**

UNBANKED. STOP FOR PROJECT CONTROL. No correction adoption, full powered/coast continuation, tiny-event, selector, off-COM, long-support, M15 comparison, full allocation/performance, live powered contact, departure, manual acceptance or M15.1 assignment.
