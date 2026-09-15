# Attribution result and ONE proposed class-B reuse

Declared after attribution, before corrected execution. No production change.

## Cause assigned

One original warmed map: 1,936 bytes. One scope-instrumented map: 1,936 bytes.
Sum of 18 local scopes: 1,936. Empty scoped-counter control: zero. The arithmetic
operators/normalizing Rational constructors contribute 1,680 bytes; four Value
projections contribute 64 bytes each, 256 total. Twelve Rational arithmetic calls
(six divisions, five multiplies, one subtraction), eleven allocating in this
witness; six Value calls, four allocating. No helper/report/delegate/object
construction remainder. Scope-level allocation attribution, not object-count tracing.

Actual loaded System.Runtime.Numerics 10.0.12+95017c711e6afc1085133d440e42b4bd78155701
stores BigInteger._bits as UInt32[]. Its span-result constructors have newarr/
ToArray IL sites. Rational operators create integer numerator/denominator products,
GCD and normalized quotients; Value creates shifted integers. These array-backed
results explain allocation ownership. Per-object count and individual constructor
branch hits are UNOBSERVED, not inferred from 1,936.

## Current source reuse

PropellantInteger: 34 inline ulong limbs (2176 bits), exact bounded add/subtract
and multiply-by-ulong; no growing BigInteger owner. PropellantDuration stores a
ratio IN TICKS. FinitePropellantSegmentation.Calculate returns consumed units /
flow units as that duration and keeps ledger/event authority exact.

PoweredFlightNumerics.TryRatio uses two 35-word stack spans to round a normalized
ratio ties-to-even; TrySeconds adds the 1,000,000 denominator factor. No heap
workspace. PoweredBinaryScale retains an independent exponent and evaluates
the complete weighted value/divisor before returning to ranged binary64.
The existing tiny-value production tests rely on that distinction.

## Single correction design

Class B, diagnostic only: link the unchanged PropellantInteger.cs and
PoweredFlightNumerics.cs. Import raw exact integer operands cold into their
fixed representation by bounded multiply/add. Do not cache event answers.
The corrected Map:

1. converts exact hp seconds to PropellantDuration ticks using exact *1,000,000;
2. constructs exact duty input from that duration and the unchanged rational H,
   then calls TryRatio; H is NOT rounded to integer 16,666/16,667 ticks;
3. projects source mass/configuration using the existing ratio primitive;
4. uses the duty's MultiplyDivide for weighted force/mass and torque/inertia,
   with the independent exponent retained through the weighted operation;
5. projects the unchanged ordinary H for BEPU input and returns six doubles
   and their six FP32 projections.

No source mass evolution, solver call, resource write, cache, pool, per-event
memoization, mutable shared workspace or new arithmetic algorithm. All source
values remain immutable; stack/value workspace is transient. Exact ledger inputs
are retained alongside this derived physical observation.

## Semantic gate before allocation/performance

The old mapper holds exact rational expressions through projection, including
lever 1/10 and gravity 981/100. Its Value implementation truncates operands to
55 bits and casts/divides. Banked TryRatio performs proper 53-bit ratio rounding;
mass/configuration projection moves some expression rounding boundaries. This
is NOT assumed equivalent. Compare against the original map on the frozen off-COM
input, recording all six old/new double and float bits. A mismatch is a decisive
failed reuse witness: STOP, no custom class-C second rescue, no allocation
qualification or timing. This experiment does not adopt any changed numerical
semantics as accepted candidate behavior.

Only if bits match: 128 warm calls, unchanged positive/read controls, three
isolated single calls and one 128-call check, exact-zero delta each; repeated
same-process bit comparison and independent review. Any failure stops. The
previous A/B/C timing campaign may resume only after that full hard gate passes.
