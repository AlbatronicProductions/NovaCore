# Exact finite-resource arithmetic and internal exhaustion

## Chosen exactness

Exact means **exact arithmetic of the accepted binary64 initial-mass and M14.23 required-flow bit values, with the exact integer canonical interval**, under a constant-flow first model. It does not mean real engine parameters are mathematically exact, nor that future physical integration has no numerical error.

M14.23 already evaluates `thrust = throttle × maximumThrust`, body force/moment and `q = thrust / effectiveExhaustSpeed` in FP64. That stored q is the accepted input. Do not independently replace it with an unrounded symbolic thrust/exhaust ratio. Resource authority does not evaluate `q * doubleDt`, `M / q` or `M - doubleCost` in FP64.

Negative nonzero, nonfinite, wrong-source or incompatible values refuse before writes. Treat numeric +0 and −0 as zero for resource arithmetic and canonicalize newly authored zero resource amount; preserve the genuine proposal's original bits/provenance for source identity. Zero projected values are never a substitute for exact amount/segment flags.

Positive proposed thrust with q==0 is incompatible finite-feed demand: M14.23 permits division underflow while checking finiteness. The resource layer must refuse that source without modifying the banked engine or pretending it is fuel-free. Legitimate Off/unavailable/zero-throttle zero-wrench q==0 remains valid. This compatibility failure is different from valid q>0 exhaustion at an arbitrarily small exact time.

## One fixed scalar amount, not repeated rounded subtraction

Let `S=1,000,000 ticks/second`, and `Q=1/(S·2^1074) kilograms`. Every nonnegative finite binary64 value x is an exact integer multiple of `2^-1074`.

- Initial resource units: `U0 = initialMass · S · 2^1074`.
- Flow integer: `V = q · 2^1074`.
- Required resource units over n canonical ticks: `C = V · n`.
- Whole-powered successor: `U' = U − C`, only after proving U>=C.
- Interior exhaustion consumes exactly U and yields U'=0.

These conversions **decode IEEE-754 bits and use bounded integer shifts/multiplication**. The displayed enormous products are mathematical definitions, never floating runtime operations. No rounding to a coarser mass quantum occurs: Q exactly contains every first-model admitted initial mass and every q times integer-tick debit.

Remaining U is the canonical scalar; a copied binary64 kilograms value is labelled an approximation and cannot roundtrip into authority. `HasUsablePropellant` is U>0, including residuals below binary64 kilograms range. Ordinary successive intervals may vary q at canonical boundaries; exact debit preserves `U0 = remainingU + sum(committedConsumptionUnits)` for any such sequence. Preparation alone never changes U.

No-refuel/one-resource constant-flow intervals are closed under this encoding. Future routing across multiple resources or time-varying demand must prove arithmetic closure and bounds for its own declared model; it cannot silently round new fractional debits into v1. Extensible resource/model identity and ownership remain stable without asserting unlimited arithmetic capability.

## Capacity proof

For finite binary64 x, `0<=x<2^1024`, so `x·2^1074<2^2098`. Therefore:

| Quantity | Strict upper bound | Maximum significant bits |
|---|---:|---:|
| V, decoded flow | 2^2098 | 2098 |
| U0, largest source amount | 2^2118 | 2118 |
| V·n, positive signed-Int64 n | 2^2161 | 2161 |

**34 UInt64 limbs = 2176 bits = 272 bytes** suffice per integer, including a whole-interval demand larger than any finite binary64 mass. Current M14.23 n is only 16,666 or 16,667; the wider signed-duration bound is proved, not an arbitrary oversized allocation. Subtraction cannot increase U. There is no accumulation-history growth or mid-episode precision expansion.

The first slice requires fixed-width decode, compare, multiply by a positive integer tick count and nonnegative subtract. Event identity stores a source-bound ratio; it does not require allocating BigInteger or normalizing large GCDs on every interval. Bounded loops over fixed limbs are O(1) with respect to episode length. This is an allocation-free-capable design, not yet a measured implementation.

Do not use the 34-limb bound for arbitrary cross products without proof. Later general ratio ordering can compare exact cross products; source-constrained U1V2 needs at most 4216 bits, and generic 34×34-limb multiplication needs 68 limbs. No general event queue/comparator is needed in the one-resource first slice.

## Exhaustion cases and segment endpoints

Input interval is `[a,b]`, exact n=b−a>0 on the original canonical schedule. Do not reset T0 or alter its 16,666/16,667 sequence.

| Condition | Powered portion | Unpowered portion | Consumed units | Successor / event |
|---|---|---|---:|---|
| Zero realized demand, q=0 and zero wrench | absent | full interval | 0 | unchanged; no exhaustion |
| U=0, positive compatible demand | absent | full interval | 0 | zero; EmptyFeed, not a new exhaustion event |
| U>C | full interval | absent | C | positive U−C; no exhaustion |
| U=C>0 | full interval | absent | U | zero at b; endpoint exhaustion only |
| 0<U<C | `[a,a+U/V)` | `[a+U/V,b]` | U | zero; one interior exhaustion witness |

At an interior event, feed is empty and the right-hand wrench is zero; the left limit is powered. Integration uses half-open powered history and the unpowered remainder; isolated endpoint force value does not add impulse. For equality U=C the entire interval receives the original powered history, the endpoint resource is zero, and the next interval starts empty. There is no epsilon comparison or clamp.

Exact powered offset in ticks is `tau=U/V`; exact remainder is `(nV−U)/V`, so their sum is exactly n. In seconds divide each rational by S, without requiring a nonzero binary64 conversion. For full powered/empty cases use exact integer duration n or zero and explicit segment kind. Omit zero-length segments by exact comparisons only. Maximum is two fixed value slots, never a list or heap event.

The powered segment retains the M14.23 BODY force and moment; the unpowered segment has zero engine wrench. External gravity or other separately owned forces are not erased by the word unpowered. No averaging into a reduced force over the whole interval. Future dynamics rotates the body wrench consistently with evolving orientation and consumes the corresponding mass evolution.

## Resource-owned event identity

Choose a typed **resource-owned rational offset witness**, not a fractional public tick and not a fabricated contact-root certificate. Existing `PhysicalEventEpoch` deliberately caps its reduced denominator at UInt64 and cannot hold all legitimate depletion ratios. Preserve its historic encoding/callers unchanged.

The exhaustion witness includes stable resource/engine/mass-law/numeric-model identity, source ResourceRevision and exact U, source engine-proposal value provenance/command cursor, interval start/end/index and exact V. `InteriorExhaustion` identifies the boundary `start + U/V`. The un-reduced U and V are deterministic because their unit encoding and source facts are canonical; equivalent unrelated source ratios need not alias the same event identity. Opaque live proposal seals are private dependencies and never history payload.

Source-bound comparison to the interval end uses U versus Vn, not rounded epoch construction. The public clock publishes only b. No fractional StateRevision, public timer, pending timeline insertion or event consumption occurs in preparation. A copied witness means **would exhaust if applied**. Only later joint physical commit records its occurrence once.

Resource revision, source physical revision/frontier, interval and single issued application lease reject replay. A canceled and reissued preview may describe the same deterministic would-event; it has a new private lease, not a second historical event. Event time equality alone never grants authority.

## Cheap exact arithmetic witnesses

These were checked with Python standard-library Fraction/int arithmetic; no production implementation, dynamics run or formal benchmark. Reproduce with `python docs/engineering-evidence/finite-propellant-architecture/arithmetic-witness.py`.

1. **Rounded subtraction stalls:** M=1 kg, q=2^-60 kg/s, n=16,666. FP64 cost is about 1.4455450725314734e-20 kg, but `fl(1−cost)==1`. Repeating subtraction never spends fuel. The exact ledger debits a positive integer every time; no tolerance fixes the rounded design.
2. **Legitimate positive duration rounds to zero:** M=2^-1074 kg, q=2 kg/s. Exact seconds=2^-1075 and ticks=15,625/2^1069 (denominator needs 1070 bits); binary64 M/q is zero. At T=6000 N and exhaust speed=3000 m/s, exact impulse rounds to a representably nonzero 1.482e-320 N s. Casting duration to zero before multiplying loses a real model contribution.
3. **False equality:** q=1 kg/s, n=16,666 and M=fl(16,666/1,000,000). Floating demand equals M, but exact successor is positive `291/1125899906842624000000` kg. Correct classification is fully powered with residual, not endpoint empty.
4. **True equality:** q=15,625 kg/s, n=16,666 and M=260.40625 kg exactly satisfy U=Vn. Entire interval powered; target empty.
5. **Ordinary fractional exhaustion:** M=1/128 kg, q=3 kg/s gives tau=15,625/6 ticks within a 16,666-tick interval. No public tick rounding is needed.
6. **Source compatibility:** T=2^-1074 N, exhaust speed=2 m/s gives stored q=0 despite positive thrust. Refuse finite-resource realization of this incompatible demand; do not misclassify it as the valid tiny-duration example.

The script also checks the worst-case integer bit bounds and exact repeated tiny-debit conservation. Test-only arbitrary-precision arithmetic is an independent oracle; it is not the proposed production representation.

## Future integration obligation

Preserving an exact event is necessary, not a powered-trajectory qualification. Current RK4/constant-mass/BEPU paths are not proved to consume arbitrary source-bound rational durations or changing mass.

A future admitted consumer must handle supported tiny positive powered segments even when a displayed binary64 duration is zero, using qualified scaled products/ratio evaluation, precision bounds and the correct equations. It may not abort merely because exhaustion is fractional or demand the event be representable by current UInt64/FP64 duration convenience types. Nor may it silently omit that powered segment: the nonzero-impulse witness disproves general equivalence. Exact identity and numerical error guarantees remain separate responsibilities.

The first nonphysical segmenter must succeed on these exact cases now; later physical admission must wait for its corresponding integration contract. No production path starts an interval assuming old-mass dynamics and discovers halfway through that supported depletion cannot continue.
