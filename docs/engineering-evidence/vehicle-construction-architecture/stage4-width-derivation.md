# Stage4 arithmetic contract — analytical derivation before correction

Project Control authorized one focused arithmetic-bound correction after the historical stop. This derivation was recorded before changing production code. The historical stop and Stage0–3 evidence remain retained.

## Operands and conventions

Let P be InitiallyPositive (0..256), r be Fuel.RateBits, E be the current cumulative fuel event count (0..P), and D(E)=1+E*r. Write D=D(P). Existing snapshot admission permits positive denominators of at most D(E) ordinary binary magnitude bits. The existing stored-integer admission is I=2200+P*r=D+2199; retain it unchanged for saved-state compatibility.

Finite nonnegative binary64 x decoded as x*2^1074 has at most2098 magnitude bits: (2^53−1)<<2045. Quantity decoding additionally multiplies by1,000,000 and has at most2118 bits. Positive Int64 ticks have at most63 bits. These are the existing PropellantInteger decoder's operands, not estimates of typical values.

BigInteger is signed, arbitrary precision; no fixed integer cast, shift/truncation or approximate arithmetic is introduced. For positive values GetBitLength is magnitude width; it excludes the sign bit. For negative values it is shortest two's-complement width excluding sign. Distinguish absolute magnitude capacity from signed representation width. Microsoft references: [GetBitLength](https://learn.microsoft.com/en-us/dotnet/api/system.numerics.biginteger.getbitlength?view=net-10.0), [BigInteger parsing/representation](https://learn.microsoft.com/en-us/dotnet/api/system.numerics.biginteger?view=net-10.0).

## Original operation order and reachability

Original order forms LCM(source energy denominator, fuel denominator), then combines the first activity denominator before checking the joint bound. Three independently bounded factors can therefore reach3D(E) bits. Each later iteration is preceded by a successful joint check, so no fourth unchecked factor follows.

Fuel snapshot normalization matters: E=P means all fuel stores are zero and its denominator normalizes to1. Three full factors require E<P. At P4/r1078, E3 permits D(3)=3235. The three largest distinct D(3)-bit integers, M=2^3235−1, M−1 and M−2, are pairwise coprime (M and M−2 are odd and differ by2). Their product has9705bits. Unit inventory/charge numerators prevent normalization; activity1/(M−2) is <=1tick. Thus the old9695bit witness was not the maximum. Another reachable original branch at E=P has two D(P)-bit factors. These explain the defect; changing the declared capacity to either observed number is unjustified.

## Single bounded correction and corrected operation order

After each existing activity validation, check the accumulated denominator before composing that activity's denominator. Retain the existing per-activity post-composition and final checks. In particular the initial two-denominator LCM is checked before a third factor can be multiplied. Placing the guard after activity validation preserves diagnostic precedence for a malformed first activity; the validation constructs only ticks*d, bounded below. The final check handles an empty activity vector. This changes only how early an invalid composition is refused, before copies, energy arithmetic or state mutation. A valid composition cannot be rejected earlier: LCM never decreases when additional positive denominators are included.

Every subsequent LCM now starts with an admitted D(E)-bit operand and one separately validated D(E)-bit operand. Division by gcd cannot increase either operand; their product requires at most2D(E)<=2D magnitude bits. This is an exact worst-case width for the admitted operand class: for D>=3, M=2^D−1 and M−1 are coprime, and M(M−1) has2D bits. It is reachable before refusal with E=P, normalized zero fuel/Df1, unit battery numerator/DsM, and first activity1/(M−1), ticks1. No source is mutated.

## Every remaining arithmetic family

|Expression/lifecycle|Maximum width or relational bound|Why no extra growth is hidden|
|---|---|---|
|D(E), I, lengths, bus indices|bounded Int32 arithmetic; checked width/length derivation|P<=256; compiled fuel admission bounds r; module count<=512; document sizes remain below Int32 capacity|
|Initial/activity LCM a/gcd(a,b)*b|2D magnitude bits|Both input widths checked before composition; new initial guard prevents three-factor accumulation|
|Activity interval comparison ticks*d|D+63 bits|Performed only after denominator and numerator width/sign checks|
|Admitted activity numerator n|n<=ticks*d; also n has <=I bits|This correlation is stronger than treating n and d as independent after validation|
|Capacity*denominator; scaled source charge|D+2118 bits|q<=capacity*sourceD and sourceD divides jointD, so q*(jointD/sourceD)<=capacity*jointD|
|Left intermediate Rate*n|D+2161 bits|Rate has<=2098bits; n<=ticks*d; ticks<=63bits|
|Final engine Rate*n*(jointD/d)|D+2161 bits|Exact divisibility and n<=ticks*d give result<=Rate*ticks*jointD; do not multiply two independent denominator maxima|
|Solar/load Rate*ticks*jointD|D+2161 bits|Same bound; left intermediate Rate*ticks needs<=2161bits|
|room=capacity*jointD-charge|<=capacity*jointD|All charges are nonnegative and within capacity|
|amount=min(room,remaining)|<=both nonnegative operands|No mixture or divisor introduced|
|charge+amount, charge-amount|0..capacity*jointD|The min relationship prevents a carry beyond capacity; signed negation has magnitude<=amount|
|remaining-amount, requested-remaining|0..requested|Monotonic transfer; no accumulation over modules into a larger global numerator|
|GCD, division, normalization, InQ/Delivered ratio|No increase in operand magnitude|Exact nonnegative gcd/division; denominators positive|
|Save hex formatting|Current admitted integer width plus sign nibble|Formatting does not increase integer values|
|Load hex parsing before canonical/sign/width rejection|See parser bound below|Malformed but length-admitted inputs are included, not assumed canonical|

I=D+2199 dominates D+2161, D+2118, D+63 and incoming admitted stored numerators. No addition requires an unaccounted carry: the Transfer relationships bound the result, rather than assuming arbitrary independent addends. There is no other rational cross-product construction in this power path. Fuel solver arithmetic is unchanged and retains its separate Stage3 proof.

## Parser, sign and exact representation capacity

The existing parser admits H=floor(I/4)+2 hexadecimal characters before calling signed BigInteger.Parse. Let W=4H. A positive malformed string beginning7 followed by H−1 f digits reaches W−1 magnitude bits before width rejection. A negative malformed string beginning8 followed by H−1 zeros equals−2^(W−1), whose absolute magnitude needs W bits and whose signed two's-complement representation needs W bits. This is reached before the nonnegative check. Examples at I2200: H552, W2208. Canonicalization and rejection do not erase that temporary construction.

Therefore the corrected uniform contract is:

- **scratch absolute-magnitude capacity S=max(2D,W)**;
- **signed two's-complement capacity=max(2D+1,W)**;
- BigInteger's sign-and-magnitude representation must support S magnitude bits plus its separate sign; a generic fixed sign-magnitude implementation would reserve S+1 bits;
- byte storage for a fixed two's-complement interchange value is ceil(signed capacity/8). No new fixed buffer is introduced here.

Both dominating families are reachable: coprime LCM for2D, negative parser boundary for W. If D=1, parser width dominates, so the harmless two-bit LCM envelope does not determine the maximum. The contract is a uniform operand-class bound, not an attempt to minimize storage for catalogs lacking particular module roles. Primitive BigInteger implementation working buffers and GC bytes are measured separately; S bounds NovaCore expression values, not total process memory.

The existing1,000,000-bit admission limit remains a **magnitude** limit. Old admission implies D<=500000. Then I<=502199 and W<=502204, so max(2D,W)<=1,000,000 for every previously admitted network. A required1,000,001-bit signed representation at the exact magnitude cap must not be mistaken for a newly invalid network. No definition/resource values or saved numerical results change.

## Required adversarial qualification

Reconstruct independently the original three-factor maximum and the corrected pairwise maximum. Exercise maximal admitted valid time/rate/capacity arithmetic, exact pairwise-width refusal, one-below/at magnitude-cap relationships, parser positive/negative width boundaries, canonical largest accepted I-bit integers where semantics permit, and unchanged source snapshots on every refusal. Repeat existing Stage4 semantics and Stage0–3/SRV gates. An additional materially different arithmetic defect requires Project Control stop, not another rescue.
