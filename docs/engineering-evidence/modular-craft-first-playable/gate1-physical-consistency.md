# Gate 1 physical consistency and controlled decoding

This bounded correction enforces the already accepted coaxial annular-cylinder, proportional-spatial-depletion model. It changes neither gameplay balance nor the physical model. The former `1e-10 * max(1, magnitude)` volume/tensor comparison admitted dimensionally inconsistent small stores; the permanent admission audit retains both original counterexamples.

## Derived numerical contract

Dimensions and density are authoritative exact FP64 input values. For outer radius ro, inner radius ri, and length L:

- Volume V = pi (ro-ri)(ro+ri)L.
- Specific axial inertia Kxx = (ro²+ri²)/2.
- Specific transverse inertia Kyy = Kzz = (ro²+ri²)/4 + L²/12.
- Analytic off-diagonal entries are exactly zero.
- Stored mass must agree with density × the same volume enclosure.

`PartStandardPhysicalConsistency` encloses pi and every arithmetic result outward with adjacent representable values. Nonnegative addition/product and positive division use monotone endpoint operations; the difference uses exact FP64 radii directly before outward rounding. The factored radial area avoids subtracting separately rounded squared radii, so even an adjacent-representable-radius annulus can remain resolvable. Derived positive physical enclosures must remain finite, normal and noncollapsed. Intentional zero inner radius and zero central off-diagonals are permitted. Unsupported numerical evaluation is refused explicitly, not repaired by a clamp or a minimum vehicle dimension.

Each authored derived value is assigned its adjacent-value enclosure, conservatively containing one final rounding cell. This is not permission for coarse manual rounding or arbitrary accumulated calculation error. The calculated volume is intersected with the authored usable-volume enclosure before multiplication by density, so capacity and volume share one surviving physical enclosure. Every tensor diagonal is checked independently in its own units. There is no unit-sized epsilon or copied tolerance from exported visual transforms.

The .NET adjacent-value operations follow [Math.BitIncrement](https://learn.microsoft.com/en-us/dotnet/api/system.math.bitincrement?view=net-10.0); the reviewer checked their directed-enclosure use against [floating-point rounding principles](https://docs.oracle.com/cd/E19957-01/806-3568/ncg_goldberg.html). No external data or library is required at runtime.

## Independent evidence

Permanent rational oracle tests decode exact IEEE mantissas/exponents, use a fixed 50-decimal lower/upper pi enclosure, and evaluate the *unfactored* difference of squares in exact BigInteger arithmetic. Correct nearest-even rounding is determined by ordered IEEE encoding search and exact midpoint comparison; no approximate quotient decides the result. Both pi endpoints must round to the same reference result.

The suite covers powers-of-two length scales from 2^-150 to 2^150, cylinders/annuli, density variation, the planned .32/.64 m³ stores using full-precision dimensions, adjacent-radius annuli, capacity/tensor inconsistency across scales, forbidden off-diagonals, nonfinite/underflow/collapsed evaluation, and rejection of prose-rounded .815 m as proof of exact .32 m³ volume. The original moderate-density 1,000× capacity mismatch and extreme-density witness now refuse. Source/catalog bytes remain unchanged on refusal.

## Other demonstrated Gate 1 defects in this closure

The former dry aggregate comparison had the same unit-sized tolerance floor: a 1e-12 kg constituent could admit 1e-15 kg aggregate metadata, and a 1e-9 tensor could admit 1e-12. V2 now uses a cold exact dyadic reference; v1 keeps its original reader and arithmetic. With U=2^1074, each finite input x is represented by integer X=xU. Define scaled M=sum(mi), S=sum(mi ri), J=sum(Ii U² + mi P(ri)), with P(v)=|v|² identity - vvᵀ. Aggregate quantities in units of 1/U are M, S/M, and (JM-P(S))/(U² M). Midpoint comparisons accept exactly the finite FP64 nearest-even result, including signed components and subnormal ties. Exact results outside finite FP64 range refuse before endpoint comparisons. This aggregate contract is stricter than the conservative pi/operation enclosures used for analytic stores.

This construction performs every signed product and cancellation exactly. For the existing 4096-region bound, |X|<2^2098, M<2^2110, |S|<2^4208, |J|<2^6308, and |JM-P(S)|<2^8419. Comparing the scaled rational directly against FP64 midpoints avoids redundant powers of U; one additional carry bit bounds doubled numerators. BigInteger is cold metadata preparation only, not runtime resource scratch or another mass-state owner. No later expression exceeds this conservative bound. Invalid finite authored aggregates are bounded too.

Permanent tests independently reconstruct the centre first using reduced rationals, then sum translated tensors. They cover the original small-value defects, zero-COM mismatch, signed off-diagonals, exact midpoint ties and adjacent operands, maximum finite mass and one-quantum overflow, common translations up to ±MaxValue, small separation at a large common offset, order invariance, and all 4096 permitted regions at maximal coordinates. The v2 path never invokes the old cancellation-prone floating aggregate first.

The shared JSON converters now check object kind, component count, required coordinate names, numeric token kind and finite values before access. Malformed vectors/quaternions/scalars/exact quantities/time values follow controlled JSON/validation refusal rather than leaking property-access/type exceptions. This enforces strict decoding; serialization of accepted data is unchanged.

Socket-group membership is a set and is sorted canonically. Ordered sockets inside a placement set remain ordered, because their rotational placement semantics are significant. The permanent permutation test changes only group membership enumeration and proves unchanged catalog identity.

Earlier resource/placement corrections remain: exact catalog resource resolution; complete used-resource dependency sealing; early null/identity/count/member/frame validation with definition/field diagnostics. The old catalog and banked stock schemas still refuse reinterpretation. These are definition/admission changes; no CraftDocument or runtime-flight implementation is included.

An independent oversized-string witness proved that direct catalog compilation could publish bytes its own bounded loader refused. Publication and loading now share MaximumCatalogBytes=16,000,000 (the existing reader limit), measured on actual canonical serialized bytes. Permanent tests exercise one below, exactly at, one above, Unicode escaping expansion, roundtrip identity and refusal immutability. This enforces the existing bounded catalog contract without inventing a resource-name-specific threshold.

Three remaining floating sign predicates were independently disproved and corrected in the same bounded package. (1) A symmetric inertia with exact negative central second-moment determinant passed the rounded determinant check. V2 now applies exact Sylvester criteria to I and trace(I) identity - 2I, preserving the existing nondegenerate volumetric-body policy. (2) Identical stores at X=1e20 could evade overlap because X±L/2 collapsed. Axial overlap now means exactly 2|Xa-Xb| < La+Lb; radial overlap uses direct strict endpoint comparisons. Touching remains allowed. (3) Exactly coplanar metre-scale vertices produced a spurious determinant -2^-56. Exact rank uses one edge, the first nonparallel edge and a nonzero scalar triple product in linear time. No minimum volume in arbitrary units is invented. Geometric conditioning for finite runtime physics remains Gate 8 responsibility; valid definition metadata alone never confers flight admission. These checks share the exact dyadic decoder; their bounded degree-three expressions are smaller than the aggregate bound above. Legacy stock and v1 tensor arithmetic remain unchanged.
