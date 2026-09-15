# Binary64 projection ownership / contract gate

Date: 2026-09-15. Source-only audit. **B. REQUIRED OUTCOME / WRONG METRIC.**
Lead judgment: **PASS — CONTRACT RESOLVED.** Independent verifier: **PASS**.

No production, mapper, test, arithmetic or threshold change. No build, allocation
recheck, physical execution or timing run in this gate. The preceding
[stopped correction](../allocation-closure/README.md) remains unchanged evidence;
this report does not turn its unexecuted allocation/performance gates into passes.

## Baseline and scope

Branch: `codex/powered-contact-numerical-prerequisite`.
HEAD/main/origin/main/remote main:
`49057fecceb0f725d5f551ec40e2780971b0d81d`.
M15.0: `4607d8c802006d5e1a01c595ab608cf53a4dab6b`.
The 65 historical tags and all 2,468 pre-existing tracked/untracked source and
evidence files are protected by the [identity record](identity.json).

The four names belong to an evidence executable. The
[original project](../../OrdinaryClosure.csproj) compiles only its Program.cs.
The [Simulation project](../../../../../src/NovaCore.Simulation/NovaCore.Simulation.csproj)
does not reference that executable. Source/project searches found no reference
from production or permanent tests to this ordinary-step mapper or its later
attribution/correction implementation. No banked ordinary powered-contact path
currently owns these four projected values.

## Value trace

Sources: [original world Program.cs](../../Program.cs), lines 14–40, 66–81,
126–131, 163–165, 184–201; [first performance mapper](../Program.cs), lines 16–51;
[later projection capture](../allocation-closure/Attribution.cs), lines 7–10,
20–41, 111–114; [later correction comparison](../allocation-closure/Correction.cs),
lines 53–113.

The original `Program.Main` locals named ax, ay, wx and wy at line 128 are
**Rational**, not double fields. Their unnamed `Rational.Value` getter results
are the four binary64 projections at issue. The getter at lines 28–35 shifts
numerator/denominator operands toward 55 bits, casts/divides them, then scales
the result. Each getter result is immediately cast to float at line 163.

| Value | Rational producer in Main | Binary64 producer / lifetime | Immediate consumer | Classification |
|---|---|---|---|---|
| ax | effective Fx / source mass | Rational.Value; one conversion expression | `(float)ax.Value` → InputState.Linear.X | **D. NATIVE / BEPU TRANSPORT INTERMEDIATE** |
| ay | effective Fy / source mass − 981/100 | Rational.Value; one conversion expression | `(float)ay.Value` → InputState.Linear.Y | **D. NATIVE / BEPU TRANSPORT INTERMEDIATE** |
| wx | (−1 × lever × effective Fy) / inertia | Rational.Value; one conversion expression | `(float)wx.Value` → InputState.Angular.X | **D. NATIVE / BEPU TRANSPORT INTERMEDIATE** |
| wy | (lever × effective Fx) / inertia | Rational.Value; one conversion expression | `(float)wy.Value` → InputState.Angular.Y | **D. NATIVE / BEPU TRANSPORT INTERMEDIATE** |

Effective force is the exact source force multiplied by exact hp/H. These
classifications describe each original projection's functional role; its owner
is the evidence-only executable, not NovaCore canonical state. Later retained
copies of those doubles are separate diagnostic observations, described below.

For **each of the four original binary64 results**, the complete ownership flags
are identical:

| Property | Source finding |
|---|---|
| Owner | Main's evidence-only input preparation; getter result has no independent owner |
| Lifetime | Immediate double-to-float conversion; no retained binary64 field |
| Stored | No original double storage. InputState retains only Vector3/FP32 components |
| Published | No. This executable performs zero canonical publications |
| Compared | No original double comparison; the performance mapper compares resulting FP32 bits |
| Hashed | No. The report hashes the BEPU assembly, not these values |
| Cached | No. Captured source/endpoint caches contain solver impulses/features, not these doubles |
| Admission/refusal | No branch receives these double results; physical bars receive native output/error quantities |
| Canonical state | No consumer; there is no canonical engine/publication in this mapper |
| Native input use | Yes, solely the explicit FP32 conversions listed above |

Every original downstream path is through that conversion:

1. InputState.Linear/Angular are Vector3 fields (line 40).
2. Integrator.IntegrateVelocity reads their FP32 X/Y/Z components into
   Vector<float> linear/angular velocity updates (lines 66–81).
3. The ordinary `simulation.Timestep(backendDt)` consumes that integration and
   solver state. Subsequent body pose/velocity, contact and impulse snapshots
   contain native outputs. They do not recover or retain the discarded doubles.
4. Snapshot bits, independent endpoint-error/penetration/impulse bars and JSON
   reporting observe those outputs. The prepared acceleration report at line
   196 reads the same Vector3 floats through V(), which returns float arrays.

The exact debit and duration-partition checks at lines 130–131 consume separate
exact rationals. Reported exact force/torque strings retain rational provenance,
not the four binary64 results. The angular acceptance bar uses H.Value, and
reference names wx/wy at line 184 are endpoint angular-velocity oracle values;
neither is a consumer of the four acceleration projections. Inverse mass and H
are the other two consumed projections and remain within the six-FP32-bit gate.

The first performance mapper similarly returns `Prepared`, a record of **six
floats**, and checks SingleToInt32Bits against the retained world result. It has
no binary64 result field or bit-identity gate.

## Rational path ownership and later diagnostics

The allocating Rational mapper has **no production authority**. It is an
evidence implementation used to investigate ordinary-step input closure.

The later `Projected(double Ax, double Ay, double Wx, double Wy, ...)` value type
in Attribution.cs deliberately retains the getter results. Trace returns it;
Floats casts it to Prepared; Values constructs diagnostic arrays; Finish writes
binary64 values/bits and FP32 bits. Correction.cs compares these copies using
DoubleToInt64Bits/SameBits and emits the semantic-mismatch stop. These copies,
serialization and comparisons are **F. DIAGNOSTIC / EVIDENCE ONLY**. They neither
admit a canonical transaction nor create production authority through a hash,
cache or retained continuation binding. The later warmed comparison branches
exist in source but were not reached in the stopped correction run.

The prior stop correctly enforced the then-issued binary64 clause. Preserving
that historical stop does not require treating the diagnostic clause as a
production ownership contract.

## Current production countercheck

Current [LocalContactWorld](../../../../../src/NovaCore.Simulation/Spacecraft/Contact/Staging/LocalContactWorld.cs)
and [callbacks](../../../../../src/NovaCore.Simulation/Spacecraft/Contact/Staging/LocalContactCallbacks.cs)
use a different banked path: canonical source force/mass and frame transforms
are checked before FP32 acceleration reaches LocalContactIntegrator. Native
body results are reconstructed into a staged paired endpoint; publication and
acknowledgement bind that endpoint and canonical authority. No reference to the
evidence Rational mapper, Projected or its four binary64 results exists there.
This report does not relax those source checks or endpoint FP64 contracts.

## KSA equivalence

**NO GENUINELY EQUIVALENT KSA RESPONSIBILITY.** The particular responsibility is
NovaCore exact-resource/event-derived ratio projection before ordinary retained
BEPU input. The accepted source comparison separates that exact authority from
KSA's game-owned derivative/resource cadence; ordinary retained solver ownership
alone does not make the projection contract equivalent.

The [accepted direct-source closure](../../../powered-contact-event-lifecycle-convergence/ksa-current-lifecycle.md)
and [accepted live-changelog record](../../../powered-contact-event-lifecycle-convergence/ksa-live-changelog.md)
remain provenance for that distinction. Their recorded production root is
`E:\Kitten Space Agency`, build
`2026.9.10.5438+c9291b5dd3347e847020c8fb3dd11dfce6a728e9`; their recorded history
source is Discord server 1260011486735241329/channel 1260112103134724146.
This gate makes no fresh KSA runtime/history review claim and no universal claim
that KSA has no double intermediates. The latest ticket's NO-equivalence branch
applies: no broad rediscovery is needed to decide NovaCore ownership here.

## Banked numerics authority

**YES, within their existing production responsibility.**

| Banked implementation | Authority established by current source |
|---|---|
| [PropellantInteger](../../../../../src/NovaCore.Simulation/Spacecraft/Resources/PropellantInteger.cs) | 34 inline ulong limbs; exact bounded ledger arithmetic; TryToKilograms is a nearest-even observation, not the resource authority |
| PropellantDuration, same file lines 159–167 | Exact numerator/denominator duration **in ticks**; no independent clock or spending capability |
| [FinitePropellantSegmentation](../../../../../src/NovaCore.Simulation/Spacecraft/Resources/FinitePropellantSegmentation.cs) | Exact flow/duration/debit and successor arithmetic determine exhaustion; no use of the four diagnostic projections |
| [PoweredBinaryScale](../../../../../src/NovaCore.Simulation/Spacecraft/Actuation/PoweredFlightNumerics.cs), lines 6–19 | Rounded significand plus independent exponent; forms weighted product/quotient before returning to ranged binary64 |
| TryRatio, same file lines 31–59 | Two fixed 35-word stack spans; normalized ratio, guard/sticky information and nearest-even 53-bit significand |
| TrySeconds, same file lines 28–29 | Reuses TryRatio with the existing 1,000,000 ticks-per-second conversion |

[PoweredFreeFlightTests](../../../../../tests/NovaCore.Simulation.Tests/PoweredFreeFlightTests.cs),
lines 23–35 and 50–54, permanently check independent exact rounding-cell
membership/ties and a tiny duration whose ranged seconds are zero while its
weighted velocity contribution survives. This is source inspection of accepted
tests, not a new execution. The
[banked physics contract](../../../segmented-powered-free-flight/physics-contract.md)
records the same numerical responsibility.

Scope matters. [PoweredFlightEvaluator](../../../../../src/NovaCore.Simulation/Spacecraft/Actuation/PoweredFlightEvaluator.cs)
uses TrySeconds for validity and step-count decisions (lines 48–50), and its
weighted calculations produce/check FP64 endpoint state (lines 76–84).
[Powered-flight transactions](../../../../../src/NovaCore.Simulation/Transactions/SimulationTransactionEngine.PoweredFreeFlight.cs)
prepare that endpoint/history (lines 193–212) and install canonical state/history
(lines 251–255). **Those** binary64 results are production relevant. They are
not downstream consumers of the four evidence projections.

The old Rational getter's operand truncation/cast/division sequence does not
override the accepted ratio primitive. Conversely, accepting that primitive
does not automatically qualify every new expression composition or contact
integration. Existing source mass, force, endpoint and admission contracts remain.

## Required outcome and contract classification

**B. REQUIRED OUTCOME / WRONG METRIC.**

Hold the exact ledger, six consumed FP32 bit patterns, native prestate/order,
physical endpoint and canonical state fixed. Changing any of the four original
pre-cast double results cannot be observed by a production consumer: there is
no such consumer after the conversion. Only the later diagnostic captures and
binary64 comparison change. This is a dataflow/lifetime conclusion, not an
error-tolerance argument.

The retained correction witness has exact ledger equality and all six consumed
FP32 patterns equal:
`[1077935923,-1061557234,-1077097267,1067030938,1040187119,1015580809]`.
Its four double mismatches and stop remain recorded. No corrected physical run,
corrected zero-allocation result or performance result is newly proven here.

Required next-scope invariants are exact ledger equality, existing fixed-workspace
numerical semantics, six consumed FP32 bit identities and physical/BEPU results.
Canonical FP64 state, provenance, admission and continuation contracts remain
independently authoritative.

## Red team and independent verification

The independent read-only verifier separately traced the original getter, casts,
integrator, first performance mapper, later diagnostic comparisons, production
numerics and canonical transaction consumers. Result: **PASS**, contract B;
each original projection class D, later captured copies class F.

Strongest objection: a double could influence a branch, canonical endpoint or
cache even if its final float matches. Current free-flight source demonstrates
that risk in general. The trace disproves it for these four results specifically:
their sole original use is an immediate float cast; the original project has no
canonical publication, and production does not reference it. Exact-ledger and
physical-result checks use separate quantities. This objection **survived review
without defeating B**; it limits the result to the identified projection boundary.

A second objection is that a passing FP32 comparison alone cannot certify a new
integration. Agreed: physical results remain mandatory; source equivalence and
this audit are not substituted for outstanding authorized qualification.

## What happens next

Project Control may authorize migration of the diagnostic bit contract, preserving
the exact ledger and six FP32 inputs, and one exact-zero allocation recheck of the
already retained banked-reuse path. Preserve physical/BEPU results. Resume the
frozen performance/payoff campaign only after that zero-byte gate passes. No
mapper #2, new numerical mechanism, production edit or recheck is performed here.

**PASS — CONTRACT RESOLVED. UNBANKED. STOP FOR PROJECT CONTROL.**

## Reproduction and cleanup

Reproduce this audit by reading the linked producer/consumer locations and:

```powershell
Set-Location -LiteralPath 'E:\NovaCore'
rg -n '\.Value|state.Linear|state.Angular|IntegrateVelocity|prepared=' docs/engineering-evidence/powered-contact-ordinary-step-event-closure/Program.cs
rg -n 'Projected|SameBits|binary64|doubleEqual|floatEqual' docs/engineering-evidence/powered-contact-ordinary-step-event-closure/performance-payoff/allocation-closure
rg -n 'TryRatio|TrySeconds|MultiplyDivide' src/NovaCore.Simulation tests/NovaCore.Simulation.Tests/PoweredFreeFlightTests.cs
rg -n 'OrdinaryClosure|RetainedMapper|performance-payoff|allocation-closure' src tests Directory.Build.props Directory.Build.targets -g '*.cs' -g '*.csproj' -g '*.props' -g '*.targets'
git status --short
git diff --check
```

The last source/project reference search is expected to return no matches. No
dynamic test is needed to reproduce ownership. Source hashes and file/ref
preservation checks are retained in identity.json.

This source-only gate created **zero disposable files**. The preceding correction
left these reviewed, reproducible outputs; retained result/source/script copies
already preserve their useful evidence. No deletion was attempted in either gate.

| Exact disposable path | Files | Bytes |
|---|---:|---:|
| E:\NovaCore\build\powered-contact-event-allocation-closure\attribution | 36 | 1,561,824 |
| E:\NovaCore\build\powered-contact-event-allocation-closure\correction | 36 | 1,616,236 |
| Parent total: E:\NovaCore\build\powered-contact-event-allocation-closure | 72 | 3,178,060 |

The reviewed parent is inside E:\NovaCore\build and contains no reparse points.
Exact manual cleanup (only this disposable parent):

```powershell
Remove-Item -LiteralPath 'E:\NovaCore\build\powered-contact-event-allocation-closure' -Recurse -Force
```

Non-destructive verification:

```powershell
Test-Path -LiteralPath 'E:\NovaCore\build\powered-contact-event-allocation-closure'
```

After removal the result should be False. The older
`E:\NovaCore\build\powered-contact-ordinary-step-performance-payoff` is already
absent; no deletion retry or cause of its disappearance is asserted.
