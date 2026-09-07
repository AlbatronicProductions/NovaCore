# Independent and discovery reviews

All workers were read-only; the lead alone edited production and ran builds/GPU
work. Reviews are bounded to this candidate, not acceptance or banking.

## Discovery

Pipeline reviewer identified overlapping timestamp scopes: `candidateDraw` is a
complete VS/TCS/TES/raster/fragment draw, not isolated TES time. `cullCompact`
includes regional demand and physical preparation. `materialsOverlays` includes
terrain and cannot be added to its draw time. Uploads precede the total timestamp.

Refinement reviewer proved affine prepared geometry under M13.3, but disproved
automatic varying equivalence under reduced factors: normalization followed by
raster interpolation changes normal/direction fields. No accepted generic
angular/image error budget justified a lower-factor implementation. Cheap shading
leaves only 0.79044 ms sensitivity to factor-one at the active pose. These facts
reject tessellation retirement as this candidate, without judging future need.

KSA reviewer verified current `2026.9.7.5402+487c3f340de24c6a81037120b6d1129c045c5400`
source. KSA's final TES adds authored material displacement, blending up to four
materials at LOD 2, with bounded near-camera fade. NovaCore must retain future
equivalent geometry capacity. KSA skips noncontributing materials and uses
precomputed gradients around conditional projection sampling. Classification:
**ADAPT contribution-aware material evaluation**, keeping NovaCore's geographic
surface visible and preserving its FP64 identity and derivative contract.

KSA's ocean-hiding return is not equivalent to NovaCore's current visible ocean
shading and is not adopted. No KSA source/assets were copied into production.

Verified KSA provenance (SHA-256):

- KSA.dll: `a8a5164204eed962df9d9025e99f523d9a23ee2ce6e802b18216a9da35506d2f`
- Planet/TessellationFuncs.glsl: `308ee11e81ec65757c4514e485dfc6ffbf7cb2fb76e6467ca267e6980eb4172e`
- Planet/PlanetTessEvaluation.tese: `80c3168561c468d3b255d635b31c1e0163ed8331c21e1340b8d490525bc93b27`
- Planet/Planet.frag: `69173727f94b0a85e91ef5a9989503144d614ffb2a165f920ada722c9253ae20`

## VERIFY A: independent architecture/quality verifier

**PASS** on the actual production diff, SHA-256
`521435c525b1538f730fda69fb89b62ec10846c7cd870f966a2ee9076a93b598`.

Strongest attack: an ocean early return could invalidate coastal/helper
derivatives and incorrectly zero `detailWeight`, which still controls downstream
roughness/specular. Actual implementation does neither. Only three derivative-free
noise branches change; all positive/partial contributions retain their original
arithmetic, footprint derivatives remain before the branches, and height-normal
derivatives remain after their merge. No TES/physical/facility ownership changes.

The verifier independently compared identical baseline binary/shader/environment
identities across the Florida captures and confirmed the pre-existing one-pixel
inter-run variant. Confirmed Florida, active, grazing, inland and synthetic mixed
mask A/B captures match complete D32/HDR/image bytes. Its precise historical
raster cause remains unclassified, rather than assumed to be compaction order.

Finite valid Earth-domain equivalence is supported. Invalid/NaN-domain equivalence
is not claimed. The review found no bounded production defect to revise.

## VERIFY B: independent performance verifier

**PASS** on the settled-performance claim. Full normal-runtime/dynamic gates are
reported separately in the final report.

The verifier checked all 100 rows in each final pair: frame/timing identity,
generation, level/family, camera, orientation, projection, compacted triangle
count and maximum tessellation factors match. Only the fragment SPIR-V changes
among 49 files. Active retains 2,060 patches, approximately 2.779 million TES
invocations and 4,953,599 reported fragment invocations.

Final active total GPU: 17.12243 -> 5.95516 ms, saving 11.16727 ms. An additional
normal deployed native-DLL control gives 17.13441 -> 5.95512 ms, saving 11.17929 ms.
The measured candidate fragment hash equals deployed Release:
`8c59838abb3d7ee6418695ea40f044ca1f04f5c3c9216296d5cc19bba6110d6b`.

Five matched CPU pairs contain 100 aligned warm samples. Active host median
19.7566 -> 8.2947 ms is principally fence reduction, 17.1001 -> 5.8715 ms.
No CPU, demand, preparation, upload or memory migration offsets the gain.
Florida preserves 670 requests/loads, 163,158,696 read bytes, 93,392,640 uploaded
bytes and 119,737,728 allocated bytes.

Limits retained by the verifier:

- The compiled change as a whole wins; fragment VGPR 85 -> 78 can affect scheduling.
  Do not assign every saved millisecond exclusively to raw FP64 instructions.
- Active is entirely zero land contribution. Florida and fully contributing land
  do not receive the same benefit; M13 remains open.
- Quiet timing disables additional collection, not ordinary production queries.
  The normal-native control independently verifies that instrumentation is not
  the source of the gain.
- Global maxima in paired CPU runs occur during frame-one recreation, but other
  startup fence stalls exist: active candidate frame 9, 62.8733 ms total/59.8414
  fence; grazing frame 9, 76.8988/73.7231; factor-one frame 8, 48.9566/45.6168.
  They precede NCSM1 publication (generation/pupil/query identity zero). The
  underlying stall is **UNCLASSIFIED**. Do not infer a candidate causal defect.
- No hitch-free claim, retrospective classification of quiet maxima, or attribution
  of the old approximately 181 ms host frame is justified.

## Later normal-runtime gates and final review boundary

Normal L0-L17 traversal and warp passed. The additional normal-native 90-yaw by
9-pitch sweep contains 809 matched queried frames, 34-842. The performance verifier
checked cameraBody/bodyOrientation/viewProjection, frame/timing identity,
generation, topology family/level, compaction, TCS patches and maximum factors.
Only the fragment shader changes. Total GPU median/P95/P99 falls from
10.10768/17.15025/18.34591 to 5.64156/6.88716/7.47668 ms. Difference of medians is
4.46612 ms; median paired saving is 4.58476 ms. Do not conflate the two statistics.
TES counters differ by -94 across about 1.924 billion; clipping output by +66
across about 320.879 million; fragments by +6,712 across about 2.009 billion
(+0.000334%). Their precise variation is not classified. This is not a meaningful
workload reduction, nor a bit-identical counter claim.

Not every frame improves: frame 689 candidate total 6.26396 vs 3.30332 ms, with
zero fragments and slightly faster terrain. Its cull/preparation scope rises
5.45672 vs 2.49352 ms; underlying cause remains UNCLASSIFIED. The sweep is at
generation 1/L17 and does not replace the separate publication/LOD traversal.

**Final quality verifier: ESCALATE TO PROJECT CONTROL; M13.4 CANDIDATE — FOLLOW-UP
REQUIRED.** Both stock Release headless full runs fail the unchanged compound
SurfaceAnchor precision/allocation assertion (78/79). Focused stock retry,
twenty private predicate probes and the private full suite pass. Diagnostic
logging changes the executed test binary; it proves sensitivity, not cause.
The shader is not executed by this headless test. The material change still
passes its quality/performance review, but the stock gate remains unresolved.
No JIT, allocation or shader attribution is justified. No further material
revision or GPU capture is recommended. A bounded follow-up must identify the
stock failing predicate and obtain an unchanged full-suite pass without weakening
expectations; Project Control owns that responsibility boundary.
