# Physical and numerical bound derivation

This is diagnostic reasoning against `4accf92fd080c16cc8656080aa69def3fa65db53`, not a new physical authority. The test operates in the TCS on the actual accepted VS clip corners. It does not rebuild an undisplaced planetary triangle. Prepared positions, base heights, the current camera matrix, stored body quaternion, and the current NCSM1 displacement contract are its inputs. No new horizon test is included.

## Physical responsibility inventory

| Authority | Current range / construction | Increment after the chosen prepared triangle |
|---|---|---|
| Global signed elevation | R16 decode [-11,000, 9,000] m; bilinear interpolation; `planetary_physical_authority.glsl` and `EarthElevationDataset.cs` | 0; already in the physical corners |
| Regional residual | Actual immutable Florida pack: all 859 record ranges [-128, 127.99609375] m, verified by record headers and complete pack SHA-256; bilinear interpolation times boundary weight in [0,1] | 0; readiness gate completes the authoritative footprint before publication |
| Geographic recomposition | max(0, signed global + selected residual), with no fallback for a missing selected record | 0 |
| Generation-4 macro | Current largest raw amplitude 34 m; existing family metadata absolute envelope about 36.51 m; field-derived absolute raw value <= amplitude*sqrt(3)/2 before rounding | 0 |
| Generation-4 meso | Largest raw amplitude 21 m, current abs(linear)+abs(ridge)<=1; sqrt(h*h+.01*.01)-.01 <= abs(h); existing metadata envelope about 36.37 m | 0 |
| Facility grading | Convex blend of natural base and authored tangent plane, plane reference height 15.134892258793116 m; bounded support 64/56 m inner extents + 128 m transitions | 0; terrain preparation already evaluates this blend |
| Fine near field | Largest current amplitude 11 m; family blend, facility attenuation and 40–50 m fade; proof below | [-E,+E], E derived below |
| Nonnegative final height | max(0, interpolatedBase+near)-interpolatedBase | Cannot increase displacement magnitude for nonnegative base; explicit roundoff and guard below |
| Body radius / presentation | Earth reference 6,371,008.8 m, body-fixed preparation and the exact stored presentation quaternion | No extra spherical reconstruction; body transform included in clip support |
| Pupil, LOD and morph | Published prepared vertices are the accepted current geometry; TES interpolates these vertices and adds the bounded near term | No separate TES morph displacement; each frame is checked against its submitted generation |
| terrain-v5 materials / local presentation channels | Appearance inputs; their geographic physical responsibility is the canonical global/residual path above | No additional final position displacement |

Thus a global mountain-height guess is neither necessary nor appropriate. Changing any base authority changes the prepared corners consumed by the bound. An incomplete regional footprint cannot be made ready by visibility classification.

## Near-field bound independent of geographic sampling

Current field gradients are permutations of `(0, +/-1/sqrt(5), +/-2/sqrt(5))` and have unit norm. With quintic fade F(t)=6t^5-15t^4+10t^3, the eight tensor weights are nonnegative and sum to one in exact arithmetic. For one axis:

`g(t) = t^2 + (1-2t) F(t)`

`1/4 - g(t) = (t-1/2)^2 * (12[t(1-t)]^2 + 4t(1-t) + 1) >= 0`.

Cauchy–Schwarz and Jensen therefore give:

`abs(sum w_c dot(gradient_c, fraction-c)) <= sqrt(sum w_c |fraction-c|^2) <= sqrt(3)/2`.

This holds for every hash/gradient choice and every fractional coordinate, not just measured terrain. Domain warps, rotation, geography and family selection change which valid cell/gradients are evaluated; they do not expand this value bound. The largest near amplitude is 11 m, so the exact-arithmetic absolute bound is 9.526279 m. Family blending is convex and both support and range factors attenuate the result.

`envelope.py` explicitly overbounds rounding. It uses binary32 u=2^-23 even for the FP64 field, avoiding an assumption of FP64 accuracy for extended operations. Seven-operation quintic error is bounded using the sum of absolute polynomial coefficients, 31. It propagates weight-product, corner-dot, eight-corner accumulation, family blend, support product and range-fade errors. The derived value is 9.536803967727625 m; its upward binary32 encoding is **9.536805152893066 m**. This is a mathematical operation bound, not an empirical safety margin. The script emits the intermediate values.

The TCS adds a per-patch bound on FP64 base-height addition/subtraction cancellation using the actual maximum input base height and the interpolation error. Negative input base heights and invalid/insufficient contract envelopes fail closed to MUST KEEP. This derivation assumes the accepted published VS/physical inputs and camera transform are finite; the per-TES oracle additionally fails on a nonfinite final position. It does not certify arbitrary corrupt input buffers. The existing 11.8125865 m NCSM1 metadata must cover the derived bound. No metadata or H implementation is changed.

## Spatial and projection bound

Let C0,C1,C2 be the actual VS clip positions, lambda the accepted nonnegative triangle barycentrics, R the linear rotation represented by the exact stored quaternion, M the stored camera matrix, d a unit body direction and delta the final near displacement. The accepted TES computes:

`C = fl(sum lambda_i C_i + M*(R*(d*delta),0))`.

Its nominal hull is the convex hull of the actual clip corners. The additional spatial set is a displacement ball of radius E in body coordinates, transformed by R and M. This is deliberately independent of the angular-cone prototype. For any Vulkan clip half-space row L, exact support is:

`max_i L(C_i) + E * ||(L*M*R).xyz||`.

The oracle adds independently derived rounding bounds for base-clip interpolation, normalized direction length, displacement conversion, quaternion rotation, matrix multiplication and final addition. It evaluates the supporting row for the exact stored quaternion, so it does not assume the encoded quaternion is perfectly unit length.

The final implementation uses outward interval operations for the bound itself, rather than the angular prototype's aggregate gamma(2048) allowance. Basic FP64 operation results are moved outward by one representable double. Possible denormal flushing is included with the smallest normal value. The square-root allowance follows the Vulkan extended-operation inverse-square-root/division accuracy. `BGamma(n,u)=2*n*u` is an upward bound on n*u/(1-n*u) for the operation counts here. FP32 interpolation uses eight operations (five arithmetic operations plus coordinate representation allowance), rotation uses 31 including conversion, matrix rows seven, final addition one; underflow terms are included separately.

A negative outward-rounded upper bound for any one clip half-space proves every generated TES position, and therefore every interpolated generated triangle, lies outside that half-space. Borderline, invalid and unproven patches remain MUST KEEP. Plane order is x+w, w-x, y+w, w-y, z, w-z; with NovaCore reverse Z the z-only plane is the far boundary, not the near plane.

## Oracle evidence and its limits

No patch is removed during the oracle phase. TCS publishes its six bounds through patch outputs and writes a separate diagnostic record. Every final TES invocation checks its actual final clip position against all six bounds and records whether it crosses the proposed rejecting plane. Counters are cleared without overlapping the frame-header update. The host waits for the exact submission fence, verifies frame and generation, and inspects before swapchain recreation can release buffers. Raw positions are not archived per frame.

Classification by common outside half-space is sufficient for a safe reject. Patches clipped only by a combination of different half-spaces may remain uncertain. Wholly-inside is geometric clip classification, not proof every triangle owns a final pixel. Final depth/HDR/image parity and measured cost remain separate gates; oracle success cannot substitute for them.

The earlier cone prototype, the original 11.81 m isotropic trial, and the final field-derived isotropic bound have distinct shader identities and result labels. None may inherit another version's traversal result.

Specification references: [Vulkan tessellation](https://docs.vulkan.org/spec/latest/chapters/tessellation.html), [Vulkan SPIR-V floating-point requirements](https://docs.vulkan.org/spec/latest/appendices/spirvenv.html). No production correctness or portability claim beyond the completed validation matrix is made by this derivation alone.

## Diagnostic rejection and exact comparison

The diagnostic rejection variant first computes the unchanged outer/inner factors. It evaluates the proven bound only if the maximum outer factor is greater than one. For a proven rejected patch it writes zero tessellation levels; all other patches retain the original factors and original outputs. The active-only condition is a subset of the all-patch oracle, not a new visibility bound. The production VS, TES and fragment modules are unchanged during qualified performance measurements.

Exact attachment validation renders both variants from the same prepared buffers, compacted indices, camera, lighting and publication in one command buffer. Each render independently clears its depth/color attachments. A separate compute comparison examines every byte of D32 depth, HDR and final color, then publishes small counters after a fence. Identical-output and deliberately changed-byte controls validate that comparator. These two-render runs are not performance measurements.

The physical comparator records the baseline's complete 128-byte post-TES records into a bounded 4,194,304-entry GPU buffer, builds an 8,388,608-slot lookup table, and matches candidate executions by original patch ID and the exact barycentric bits within the same submitted draw. It compares all 112 physical/clip/near-field bytes exactly. Missing keys, any different physical field, conflicting baseline duplicates, table exhaustion or sample overflow fail the run. Culled patches have no candidate executions. Prepared buffers and draw inputs are shared and immutable between the two renders. These records are not written to a per-frame disk archive. An injected physical-record bit change must fail the negative control.
