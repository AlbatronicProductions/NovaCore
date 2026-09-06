"""Render tables from retained measurements; no estimates silently fill gaps."""
import json,statistics
from pathlib import Path
HERE=Path(__file__).resolve().parent

def main():
    m=json.loads((HERE/'measurements.json').read_text())
    lines=['# Numerical results and next-target decision','',
      '**READY FOR M13.2 IMPLEMENTATION REVIEW**. No production correction is left and no milestone is assigned.','',
      '## Four fixed poses','',
      '| Pose | Terrain median / P95 ms | Patches | TES invocations | Clip query input / output | Fragment invocations |',
      '|---|---:|---:|---:|---:|---:|']
    for pose,name in zip('ABCD',['factor 1','active','grazing','Florida']):
        d=m['fixed-'+pose]['metrics'];v=lambda k:d[k]['median']
        lines.append(f"| {pose}: {name} | {v('gpuDetailedDrawMs'):.5f} / {d['gpuDetailedDrawMs']['p95']:.5f} | {v('tcsPatches'):,} | {v('refinedVertices'):,} | {v('clippingInputPrimitives'):,} / {v('clippingOutputPrimitives'):,} | {v('fragmentInvocations'):,} |")
    lines+=['','Every row has 100 aligned samples. Query counts are medians, not a full generated primitive inventory.',
      '', '## Final geometric chain','',
      '| Measurement | A: factor 1 | B: active |','|---|---:|---:|']
    a=m['geometry-A-ids']['captureAnalysis'];b=m['geometry-B-ids']['captureAnalysis']
    for label,key in [('Generated post-TES triangles','generatedPrimitives'),('Completely clipped inputs','homogeneousRejected'),('Clipped polygons surviving','postClipPolygons'),('CPU clip fan triangles','postClipFanTriangles'),('Front-facing clipped polygons','frontFacingPolygons'),('Opposite-facing clipped polygons','oppositeFacingPolygons'),('Primitives with early-depth-surviving fragments','earlyDepthSurvivingPrimitives'),('Primitives producing color','colorOutputPrimitives'),('Primitives producing zero fragments','zeroFragmentPrimitives'),('Front-facing survivors with no fragments','frontFacingZeroFragment'),('Non-helper early-depth-surviving invocations','earlyDepthSurvivingInvocations'),('Normal-path color output invocations','colorOutputInvocations'),('Final visible primitives','finalVisiblePrimitives'),('Final NCSM1 owner pixels','finalOwnerPixels'),('Distinct directions adjacent to final pixels','uniqueDirectionsAdjacentToFinalPixels'),('Baseline TES executions adjacent to final pixels','baselineTesExecutionsAdjacentToFinalPixels')]:
        lines.append(f'| {label} | {a[key]:,} | {b[key]:,} |')
    for label,fn in [('Clipped percentage',lambda x:100*x['homogeneousRejected']/x['generatedPrimitives']),('Zero-fragment percentage',lambda x:100*x['zeroFragmentPrimitives']/x['generatedPrimitives']),('Generated primitives / final owner pixel',lambda x:x['generatedPrimitivesPerOwnerPixel']),('Color output / final owner pixel',lambda x:x['finalColorOverdraw'])]:
        lines.append(f'| {label} | {fn(a):.6f} | {fn(b):.6f} |')
    for pose,x in [('A',a),('B',b)]:
        base=m[f'geometry-{pose}-base']['captureAnalysis']
        lines+=['',f"{pose}: baseline TES / final owner pixel = **{base['tesRecords']/x['finalOwnerPixels']:.6f}**; baseline executions adjacent to final visible primitives = **{100*x['baselineTesExecutionsAdjacentToFinalPixels']/base['tesRecords']:.4f}%**. Distinct sampled directions adjacent to those primitives = **{100*x['uniqueDirectionsAdjacentToFinalPixels']/base['uniqueDirections']:.4f}%**."]
    lines+=['',
      'Active GS ordinary-color control matches the TES-only baseline in physical records, depth, HDR and image exactly. Both ID controls preserve physical records and depth exactly; they intentionally alter color. Factor-1 face classification has one CPU winding/fragment disagreement; do not call the CPU winding count an exact hardware backface counter. Active has none. Active 7,065 triangles intersect a clipping plane; 2,876,035 reject trivially and one more rejects after polygon clipping. The CPU clip reconstruction and hardware clipper query are distinct.',
      '', '## Projected area in pixel²','',
      '| Population | Count | P10 / P50 / P90 | <0.25 / <0.5 / <1 / <2 percent |',
      '|---|---:|---|---|']
    for pose,x in [('A',a),('B',b)]:
        for label,key in [('clip survivors','clippedAreaDistribution'),('final visible primitives','finalVisibleAreaDistribution')]:
            d=x[key];q=d['area'];percent=' / '.join(f"{d['below'][str(t)]['percent']:.4f}" for t in [.25,.5,1.,2.])
            lines.append(f"| {pose} {label} | {d['count']:,} | {q['p10']:.4f} / {q['p50']:.4f} / {q['p90']:.4f} | {percent} |")
    lines+=['','Active clip-surviving geometry has median area 5.01 pixel² and only 1.57% below one pixel². The dominant measured noncontribution is outside-view geometry, not excessive visible subpixel refinement. No quality/range reduction follows. A tighter conservative pre-TES bound is an architectural possibility, but no bound, output parity, or saved ms has been proven. Vertex adjacency is not a safe deletion predicate.',
      '', '## Unique versus duplicated physical work','',
      '| Pose | Captured TES executions | Distinct directions/positions | Repeated directions | Excess executions in shared-patch direction groups | Near-field executions |',
      '|---|---:|---:|---:|---:|---:|']
    for pose in 'ABCD':
        d=m[f'parity-{pose}-normal']['captureAnalysis']
        lines.append(f"| {pose} | {d['tesRecords']:,} | {d['uniqueDirections']:,} | {d['duplicateDirectionEvaluations']:,} | {d['sharedPatchDuplicateEvaluations']:,} | {d['nearEvaluations']:,} |")
    lines+=['','Active has 1,997,482 distinct patch/barycentric records and 79,600 directions shared across patches. Approximately 31.1% of executions repeat a direction; about 3.3% are excess executions within direction groups spanning multiple patches. That group count includes same-patch replay as well as cross-patch duplication; it is not an exact pure-edge-only overhead. Repeated patch/barycentric evaluations account for much of the overall duplication. Costs are not proportional to these counts and no safely recoverable milliseconds are assigned. GLSL tessellation provides independent final sample evaluations; a cross-patch cache is a new responsibility, not a free elimination.',
      '', '## Output-preserving cost probes','',
      '| Probe / pose | Baseline terrain ms | Probe ms | Saving ms | Classification |','|---|---:|---:|---:|---|']
    for probe in ['fragment-diagnostics','fragment-ncsm1','no-facility']:
        for pose in 'AB':
            baseline=m['fixed-'+pose]['metrics']['gpuDetailedDrawMs']['median'];value=m[f'probe-{pose}-{probe}']['metrics']['gpuDetailedDrawMs']['median']
            classification='OUTPUT-ALTERING; no safe gain' if probe=='no-facility' else ('OUTPUT-PRESERVING; four-pose exact combined parity' if probe=='fragment-ncsm1' else ('OUTPUT-PRESERVING raster at B; physical-set comparison not retained' if pose=='B' else 'OUTPUT-PRESERVING intent; A parity not captured, not independently qualified'))
            lines.append(f'| {probe}, {pose} | {baseline:.5f} | {value:.5f} | {baseline-value:.5f} | {classification} |')
    gains=[]
    for i in range(1,4):
        baseline=m[f'repeat-{i}-normal']['metrics']['gpuDetailedDrawMs']['median'];value=m[f'repeat-{i}-fragment-ncsm1']['metrics']['gpuDetailedDrawMs']['median'];gains.append(baseline-value)
        lines.append(f'| Combined B repeat {i} | {baseline:.5f} | {value:.5f} | {baseline-value:.5f} | OUTPUT-PRESERVING |')
    for pose in 'CD':
        baseline=m[f'fixed-{pose}']['metrics']['gpuDetailedDrawMs']['median'];value=m[f'probe-{pose}-fragment-ncsm1']['metrics']['gpuDetailedDrawMs']['median']
        lines.append(f'| Combined {pose} | {baseline:.5f} | {value:.5f} | {baseline-value:.5f} | OUTPUT-PRESERVING |')
    lines+=['',f'Three active repeats recover **{min(gains):.5f}–{max(gains):.5f} ms**, median paired saving **{statistics.median(gains):.5f} ms**. These are ordinary draws with no TES/GS/fragment capture shader, not capture timings.',
      '', '## Exact four-pose preservation','',
      '| Pose | Prepared bytes | Physical record set | D32 / HDR / image changed bytes | Identity |','|---|---|---|---|---|']
    for pose in 'ABCD':
        normal=m[f'parity-{pose}-normal'];probe=m[f'parity-{pose}-fragment-ncsm1'];p=probe['captureAnalysis']['versusBaseline']
        assert normal['rawFiles']['prepared.bin']==probe['rawFiles']['prepared.bin']
        assert p['physicalRecordSetExact'] and all(p[k]['exact'] for k in ['depth','hdr','image'])
        lines.append(f'| {pose} | EXACT | EXACT | 0 / 0 / 0 | frame 175, generation 1, pupil 1, physical generation 4 |')
    lines+=['', '## Ranked opportunities','',
      '| Rank / responsibility | Measured contribution | Proven avoidable / expected ms | Parity confidence | Risk / KSA support |','|---|---|---|---|---|',
      f'| 1. Ordinary NCSM1 fragment specialization | ~26 ms active draw; inactive paths constrain compiled code | {min(gains):.3f}–{max(gains):.3f} ms active paired gain; ~0.46 ms factor 1 | High for four fixed poses; exact physical/depth/image | Low physical risk; bounded pipeline-selection/diagnostic-permutation implementation risk. KSA compiler/register and immutable-feature boundary supports ADAPT |',
      '| 2. Earlier conservative refinement rejection | 75.2% generated triangles fully clipped; 75.1% baseline TES executions not adjacent to final pixels | Not established | No proposed culling rule validated | Higher coverage/crack/precision risk; KSA conservative culling already adopted |',
      '| 3. Shared/replayed final physical samples | ~31.1% repeated directions; ~3.3% excess executions within shared-patch groups | Not established | No cache/output-reuse probe validated | High synchronization/key/authority risk; current KSA independently evaluates final samples |',
      '| 4. Near-field/material ablations | Prior controls show large, overlapping sensitivity | No safe saving established | OUTPUT-ALTERING | Violates physical or appearance contracts; not a milestone target |',
      '', '## M13.2 implementation-review recommendation','',
      '**Exact responsibility:** let the ordinary NCSM1 pipeline compile its known owner and disabled-diagnostic state as constants, preserving diagnostic and other-owner pipeline behavior. Expected active fixed-pose benefit is about 2.1 ms, roughly 8% of terrain draw time, subject to final production implementation validation.',
      '', '**Proposed title only:** NovaCore M13.2: Specialize ordinary terrain shading.',
      '', 'Preservation gates: unchanged H/gradients, prepared bytes, 50 m field, selected topology/factors, TES clip/body positions, regional/support data, one-owner coverage, exact D32/HDR/image and facility visibility. Keep generic/startup and diagnostic permutations reachable. Do not global-hardcode the isolated probe into the shared fragment module.',
      '', 'Implementation validation: repeat these four parity poses and three active timing pairs against the banked baseline; inspect compiled ordinary and diagnostic permutations; run focused material/geographic-specialization/physical/support/regional/P3 Graphics contracts, strict Vulkan, shader/dependency identity, owner/seam diagnostics and supported launcher routes. Check dynamic pupil/LOD handoff and Florida presentation before banking under the normal milestone acceptance process. This investigation itself requests no manual acceptance.',
      '', 'No production optimization remains. Stop for Project Control review. **READY FOR M13.2 IMPLEMENTATION REVIEW**.']
    (HERE/'results.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')

if __name__=='__main__':main()
