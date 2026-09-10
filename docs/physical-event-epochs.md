# Internal physical-event epochs

Unbanked candidate on `codex/physical-event-epoch`, based on banked M14.5
`b53d48025cfb8142cb32a6fc6fceb6018e33cfcd`. M14 remains open. No next milestone
number is assigned. [Validation and unresolved gate](engineering-evidence/physical-event-epoch/README.md).

## Authority and representation

Simulation's internal `PhysicalEventEpoch` owns only exact time identity.
Public `SimulationInstant` remains signed Int64 ticks at exactly 1,000,000 ticks
per second. Live timeline scheduling, clock execution, history and render inputs
remain canonical. No fractional event enters live execution through this type.

The immutable, reference-free value occupies 24 bytes: Int64 floor tick, UInt64
numerator, and UInt64 denominator-minus-one. The default value is canonical zero.
Its unique mathematical form is `floor + numerator/denominator`, with
`0 <= numerator < denominator <= UInt64.MaxValue`, reduced by GCD. Zero fraction
always has denominator one. Storing denominator-minus-one makes default safe;
serialized identity stores the actual denominator.

`TryCreate(Int64 ticks, Int128 numerator, Int128 denominator, out epoch)` first
checks denominator positivity and the UInt64 capacity, then uses signed division
and Euclidean borrow before adding the whole-tick component in checked Int128.
The final floor must fit Int64. Capacity is checked before reduction: an oversized
denominator is rejected even when its fraction could reduce into range.

Examples: `0 + 2/4 == 0 + 1/2`; `0 + 32768/32768 == 1`;
`0 - 1/2 == -1 + 1/2`; `-1 - 3/2 == -3 + 1/2`.
The retained impact counterexample `0 + 15625/32768` is exact. These widths cover
that demonstrated requirement with bounded machine arithmetic; they do not claim
to cover all future physical roots. BigInteger is used only as a test oracle.

The domain includes `Int64.MaxValue + fraction` when the floor fits. This is not
a public instant: `TryGetCanonicalInstant` succeeds only for zero fraction. No
floating constructor, approximate comparison or rounded public conversion exists.
Duration arithmetic is deferred until an execution consumer requires it.

## Ordering and arithmetic failures

Compare floors without subtraction, then compare fractions using checked UInt128
cross-products. Each factor is at most `2^64-1`, so each product is strictly below
`2^128`; comparison cannot overflow within the declared invariant. Negative
normalization avoids negating Int128.MinValue. Tick addition/carry is checked.

Factory failures distinguish invalid denominator, denominator capacity and tick
overflow. The output is default canonical zero on failure and must not be used
as a successful result. There is no wrapping, saturation or rounding.

`PhysicalEventOrderKey` compares epoch, signed priority, sequence, then event ID.
Sequence/ID come from their authority, not worker completion. It owns no payload,
sequence allocator, scheduling or publication. Existing integral headers embed
exactly; the live comparer still compares `SimulationInstant` first. Both paths
reuse the unchanged equal-time priority/sequence/ID helper.

## Binary identity and extension

Version 1 has a fixed 32-byte little-endian encoding:

| Offset | Field |
| --- | --- |
| 0–3 | ASCII `PHEP` |
| 4 | Version 1 |
| 5 | Kind: 0 canonical, 1 rational |
| 6–7 | Zero reserved bytes |
| 8–15 | Int64 floor |
| 16–23 | UInt64 numerator |
| 24–31 | UInt64 denominator |

Canonical kind requires numerator zero and denominator one; rational kind requires
a nonzero proper reduced fraction. The decoder accepts exactly 32 bytes and
rejects noncanonical encodings instead of normalizing them. Unsupported version
and kind return separate failures. A short write leaves its destination unchanged;
a longer destination receives only the first 32 bytes.

Golden vector for `-1 + 1/2`:
`5048455001010000FFFFFFFFFFFFFFFF01000000000000000200000000000000`.

These bytes identify an epoch for future replay/persistence. They do not rewrite
existing event history; future records must retain publication tick and physical
epoch as distinct fields. A lossy value hash is not persistent identity.

Certified/non-rational roots are unsupported, including unknown wire kind 2.
Future support needs a separate typed certificate and exact mixed-kind comparison
contract, with deliberate wire evolution. It cannot pass an approximate rational
through this version and call it a certified event.

## Physical-state boundary

Exact time identity is not exact contact-state representation. M14.5's qualified,
represented canonical-zero-gap admission is unchanged. There is no discovery,
root solve, event-local state, recovery, solver, grounding or automatic touchdown.

The intended future sequence is:

`canonical N → private event epoch → event-local candidate state → physical mutation
→ remaining interval → canonical publication N+1`.

This candidate implements only epoch identity and ordering in that sequence.

## Reference relationship

**ADAPT:** KSA's finite-time lattice and separate solver durations support separating
physical execution timing from presentation. **INTENTIONALLY DIFFER:** NovaCore
requires exact internal rational ordering beyond that reference; KSA does not
prove this contract.

NovaCore Mobile's historical propellant-exhaustion identity supports canonical
floor plus reduced fraction. Desktop contracts remain authoritative. Its known
prescribed-attitude fallback to canonical floor is not inherited. No reference
implementation is copied into this type.
