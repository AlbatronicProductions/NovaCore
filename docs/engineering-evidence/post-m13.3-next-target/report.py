"""Build the human-readable review from retained measurements, without GPU work."""
import json,pathlib,sys
sys.dont_write_bytecode=True
from records import read
P=pathlib.Path(__file__).resolve().parent
def table(headers,rows):
    return '\n| '+' | '.join(headers)+' |\n| '+' | '.join(['---']*len(headers))+' |\n'+''.join('| '+' | '.join(str(x) for x in row)+' |\n' for row in rows)+'\n'
def main():
    s=read(P/'summary.json');v=read(P/'validation.json');sections=[]
    def add(text):sections.append(text.strip()+'\n')
    add('''# Post-M13.3: contribution-aware terrain material evaluation

**Lead judgment: ESCALATE TO PROJECT CONTROL.**
**Technical classification: M13.4 CANDIDATE — FOLLOW-UP REQUIRED.**
The shader passes its independent quality/performance review; the stock Release
headless SurfaceAnchor validation gate remains unresolved. See `closeout.md`.

Unbanked production candidate for Project Control. The only production change is
seven inserted/four removed lines in `native/NovaCore.Native/shaders/production_terrain_material.glsl`.
The shader skips three procedural noise evaluations exactly where their final
land-detail contribution is zero. It changes no physical, render or gameplay
contract. **Hardware tessellation, factors, density and future high-fidelity
displacement/silhouette capacity are preserved. M13 remains open.**

## Baseline

HEAD, main, origin/main and peeled M13.3 tag:
`180eaf150ba5db6364e17dd48336690778f058f9`.
The supplied ticket transposed the last characters; Git established the value above.
M13.3 annotated tag `m13.3-prepared-physical-terrain`, object
`bb08cba20947f863391649e68e2e4fe7035520db`.
M13.1: `fade1384c1c7df93d954e7223b1cc8f17db17f98`, `m13.1-ncsm1-tes-hotpath`.
M13.2: `4accf92fd080c16cc8656080aa69def3fa65db53`, `m13.2-ordinary-terrain-shading`.
Development branch: `codex/post-m13.3-next-performance-target`.
Initial main was clean with no staged diff. The initial identity record was written
after creating this evidence directory, so its sole untracked entry is this package.
No existing milestone or main moved; no staging, commit or tag occurred.

Hardware: AMD Radeon RX 6800 XT; native 3440 x 1440; Vulkan SDK 1.4.357.0.
Exact 49-shader, native, managed and asset identities: `baseline-identity.json`.
Final deployment: `deployment.json`. Every performance pair uses equal warm-up,
100 final samples (frames 113–212), fixed J2000 lighting/time and matched camera,
publication, pupil, topology and refinement conditions. Additional profiling and
capture are disabled for final GPU timing. Existing production queries remain.

## WHAT / HOW / BAR / PAYOFF

The active-refinement excess is predominantly fragment material work interacting
with dense primitive rasterization. The measured winner is **procedural material
noise with zero final contribution**, not excessive hardware tessellation by itself.
Exact-zero gating removes that work before evaluation; all nonzero values keep
the same arithmetic, including arbitrarily small contributions.

The fourteen implementation conditions passed before production edits; see
`implementation-boundary.md`. Measured settled active net GPU recovery is
11.16727 ms (MAJOR), with an independent normal-native control of 11.17929 ms.
The standing minimum was 1.5 ms. Output-changing diagnostic savings were not
treated as recoverable quality-preserving gains.

## WORK A: current pipeline decomposition

Fresh active 2 x 2 diagnostic terrain-draw medians, milliseconds:
''')
    add(table(['Fragment','Accepted factors','Force factor one'],[['Production',14.48428,5.97604],['Cheap diagnostic',1.53844,.74800]]))
    add('''The full-fragment factor sensitivity is 8.50824 ms; cheap-fragment
factor sensitivity is only 0.79044 ms. Their 7.71780 ms interaction is nonadditive.
The cheap shader consumes every fragment input; VS/TCS machine instructions stay
identical and TES retains all 101 FP64-named instructions, excluding a trivial
dead-input elimination explanation. Fragment/helper utilization is a plausible
mechanism, not a directly measured occupancy attribution.

Timestamp scopes overlap: `candidateDraw` covers VS/TCS/TES/raster/fragment;
`materialsOverlays` includes terrain; `cullCompact` includes demand and preparation.
They must not be summed or mislabeled as independent TES timing. Uploads precede
the total timestamp; paired CPU/preparation accounting below protects that boundary.

## WORK B: fresh baseline refinement and geometry

These initial instrumented medians establish workload; final quieter A/B below
is the performance acceptance comparison. Clip input is measured before clipping;
clip output/fragment counts are reported hardware counters, not estimates of
unique useful geometry. Factor-one and cheap-fragment ablations change outputs.
''')
    add(table(['Pose','Terrain ms','Total ms','P95','P99','Patches','TES invocations','Clip input median','Clip output','Fragments','Inner factor bins'],[[r['pose'],f"{r['terrain']['median']:.5f}",f"{r['gpu']['median']:.5f}",f"{r['gpu']['p95']:.5f}",f"{r['gpu']['p99']:.5f}",r['tcsPatches'],r['tesInvocations'],r['clipInput']['median'],r['clipOutput'],r['fragments'],r['factorBins']] for r in s['baselineWorkload']]))
    add('''Inner bins follow the existing telemetry's (1,2,4,8,16,32,64) groups.
Active has 2,060 patches, approximately 2.779 million TES invocations and
4,953,599 fragments. Grazing combines many factor-one patches with a smaller
high-factor region. Force-one active retains 2,060 patches but falls to 6,180 TES
invocations, 404 clipping-output primitives and 4,953,600 fragments. That comparison
does not establish permission to reduce tessellation: interpolated normal/direction
fields and future material displacement remain meaningful responsibilities.
Small invocation-count variation occurs with unchanged parallel submission; its
precise origin is not established. The
oriented triangle set and prepared bytes are the geometry invariants, not raw
compaction buffer order. No false-reject or conservative-culling claim is made.

## WORK C: KSA responsibility

**ADAPT** contribution-aware material evaluation. Current local KSA version
2026.9.7.5402+487c3f340de24c6a81037120b6d1129c045c5400 skips zero-weight materials
and computes gradients before conditional projection sampling. Its TES retains
authored material displacement, including up to four-material blending at LOD 2
and bounded near-camera fade. NovaCore retains the capacity for that equivalent
future geometric responsibility. KSA ocean hiding is not adopted: NovaCore's
currently visible surface still needs its original shading outputs.
Verified source/DLL hashes and independent reasoning are in `reviews.md`.
No KSA source or assets were copied. No broad historical comparison was repeated.

## TES and fragment audits

Selected production TES: wave32, 48 VGPR, 32 SGPR, 4,608 bytes LDS, zero scratch;
101 FP64-named / 150 FP32-named instructions (static ISA mnemonic count).
The retained ISA has 546 machine-instruction lines excluding `s_code_end` padding.
Its live work includes prepared interpolation, normalization, projection and
varying/address transport. This candidate changes none of it: VS/TCS/TES ISA is
byte-identical. Per-invocation TES cost is not separately timed; factor-one
under cheap shading bounds a combined geometry/raster sensitivity, not pure TES.

All 4,953,599 terrain pixels in the active mask capture have zero land-detail
contribution. Three FP64 noise bands were nevertheless evaluated. Guarding them
preserves all material outputs. Fragment VGPR falls 85 to 78, SGPR remains 53,
scratch remains zero. Static FP64 code remains for contributing land; the gain
comes from the compiled dynamic path, including possible scheduling effects.
Do not attribute each saved millisecond exclusively to raw instruction count.
Exact mask capture and AMD ISA/resources are retained as small records.

## Candidate ranking
''')
    add(table(['Responsibility','Measured sensitivity / net','Proven avoidable','Risk / KSA','Decision'],[
        ['Zero-contribution noise','11.16727 ms net active GPU','Same full attachments','Low; ADAPT weighted evaluation','Winner; MAJOR'],
        ['Reduce factors','8.50824 ms full shader, only .79044 ms cheap shader','Not proven with equivalent varyings/future displacement','Quality contract changes; KSA retains TES','Reject for this candidate'],
        ['Remaining TES normalization','101 live FP64 mnemonics','No safe dead operation proven','Physical/varying precision','No qualifying implementation'],
        ['Early visibility','No new safe multi-ms proof','Not established','Prior final conservative-bound limitations remain','Do not reopen'],
        ['Preparation/residency','Startup sliced work; no measured warm active excess','Not this settled deficit','Publication/physical ownership','Preserve']]))
    add('''## Implementation and cheap validation

Old execution condition: each noise band's footprint permits evaluation.
New condition: that footprint condition AND `landDetail > 0.0`.
The unchanged land-detail expression moves before the three evaluations.
Footprint derivatives remain before the branch; height-normal derivatives remain
after it. There is no early return, approximate threshold, reduced detail weight,
subgroup requirement, new flag, data upload, bridge or duplicate authority.

Tests KEEP: all existing geometry, physical authority, material, facility,
residency, topology, seam, window and route tests. MIGRATE/SPLIT/RETIRE: none.
No expectation was weakened. No production implementation revision occurred.

Cheap active timing with added collection off: 17.11309 to 5.96024 ms total GPU.
Exact attachment proofs on active, grazing, inland, mixed coastal/helper lanes
and confirmed Florida passed before full validation. No added CPU or memory work.
The go gate therefore passed; only this one responsibility was implemented.

## Full performance: final 100-sample paired runs

All values are milliseconds. Gain is total GPU; percentages are total GPU gain.
Negative budget distance means headroom. Tiny orbital/Florida changes are noise.
''')
    rows=[]
    for r in s['performance']:
        a=r['baselineGpu'];b=r['candidateGpu'];ta=r['baselineTerrain']['median'];tb=r['candidateTerrain']['median']
        rows.append([r['pose'],f'{ta:.5f}',f'{tb:.5f}',f'{ta-tb:.5f}',f'{(ta-tb)/ta*100:.2f}%',f"{a['median']:.5f}",f"{b['median']:.5f}",f"{a['p95']:.5f}/{b['p95']:.5f}",f"{a['p99']:.5f}/{b['p99']:.5f}",f"{r['savedGpuMs']:.5f}",f"{100*r['savedGpuMs']/a['median']:.2f}%",f"{r['candidateAbove8_33']:+.5f}",f"{r['candidateAbove11_11']:+.5f}"])
    add(table(['Pose','Terrain A','Terrain B','Terrain gain','Terrain %','GPU A','GPU B','GPU P95 A/B','GPU P99 A/B','GPU gain','GPU %','B minus 8.33','B minus 11.11'],rows))
    add('''Normal deployed native-DLL control: active 17.13441 to 5.95512 ms,
11.17929 ms saved, with the same fixed-pose managed driver. This rejects temporary
native instrumentation as the source of the gain. Fully contributing inland land
remains approximately 17.42 ms total GPU and Florida remains above 8.33 ms.
This is not a universal ocean/land speedup or a completed M13 performance campaign.
The 11.11 ms future full-featured floor cannot be certified by terrain-only GPU
timing. Orbital P95/P99 rose by about .085/.096 ms at a sub-1 ms median; controls
show no material regression, but these small tail differences are not hidden.

## CPU, preparation, synchronization and memory

Paired diagnostic collection is separate from final GPU timing. CPU update
contains fence wait; columns are overlapping responsibilities, not additive costs.
GPU frame N is waited/inspected during update N+1; record/submit belongs to N.
''')
    add(table(['Pose','Host median A/B','Update A/B','Fence A/B','Record A/B','Submit A/B','Present A/B','Acquire A/B'],[[r['pose']]+[f"{r['baseline']['host_'+k]['median']:.4f}/{r['candidate']['host_'+k]['median']:.4f}" for k in ['total','update','fenceWait','record','submit','present','acquire']] for r in s['cpu']]))
    add(table(['Pose','Mode','Demand slices / sum ms','Incoming slices / sum ms','Current slices','Logged warm slices'],[[r['pose'],r['mode'],f"{r['groups']['demand']['slices']} / {r['groups']['demand']['sumMs']:.5f}",f"{r['groups']['incomingPhysicalPreparation']['slices']} / {r['groups']['incomingPhysicalPreparation']['sumMs']:.5f}",r['groups']['currentPhysicalPreparation']['slices'],sum(g['settledSlices'] for g in r['groups'].values())] for r in s['preparation']]))
    add('''The active host median falls 19.7566 to 8.2947 ms, principally fence
wait 17.1001 to 5.8715 ms. No cost migrates into CPU, preparation or memory.
Demand is logged above .01 ms; absence alone does not prove zero sub-threshold
work. Recorded cumulative dispatch/publication counters remain unchanged. Current
preparation is already ready; incoming preparation is sliced before the warm
window. Divide incoming total by 11 for about 3.73 ms/slice in near regimes;
this startup sum must not be added to each settled frame.

Florida A/B preserves 670 requests/loads, 163,158,696 read bytes, 93,392,640 upload
bytes and 119,737,728 allocated bytes. Active has zero regional loads/uploads and
four allocated bytes for its empty demand representation. The regional catalog
contains 670 possible records; that is not active residency. Budget 124,895,232
bytes, bounded ready/upload queues and publication identities remain unchanged.

## Physical / render / gameplay outcome

M13.3 remains: render interpolates prepared full relief; gameplay queries
continuous H. No obsolete per-TES physical authority returns. Full attachment
comparisons validate D32, HDR and final presentation on active, grazing, land,
synthetic mixed masks and repeated Florida. Prepared vertices and the oriented
triangle multiset match exactly. No tolerance or quality expectation changes.

The first active comparison incorrectly required raw compaction ordering; its
false result remains recorded alongside the corrected geometry invariant.
The first Florida A/B differed at one D32/HDR pixel, but not final image. Two
subsequent banked A/A captures and the repeated A/B are fully identical to that
candidate capture, proving baseline inter-run variation. The precise old raster
cause remains UNCLASSIFIED; compaction is not assumed to explain that pixel.
Invalid/NaN domain equivalence is not claimed; valid captured values are finite.

## Dynamic validation and frame pacing
''')
    for label in ['normal-full-transitions','normal-warp']:
        matches=[r for r in v if r['label']==label]
        if matches:
            r=matches[-1];add('### '+label+'\n\nExit '+str(r['exitCode'])+'.\n\n```text\n'+'\n'.join(x for x in r['lines'] if any(t in x for t in ['Desktop traversal PASS:','anchored warp diagnostic PASS:','GPU timing: total','frame timing:']))+'\n```')
    if (P/'motion-candidate.json').exists():
        a=read(P/'motion-baseline.json');b=read(P/'motion-candidate.json')
        add('''The additional normal-native 90-yaw by 9-pitch sweep at 10.004 m
crosses active refinement, grazing and sky views. It retains 809 aligned queried
frames per side (810 pose steps; initial query latency excluded). No bulk capture.
This is a mixed dynamic distribution, not another settled-pose benchmark.
''')
        add(table(['Metric','Baseline median/P95/P99','Candidate median/P95/P99'],[[k,' / '.join(f"{a['analysis']['metrics'][k][q]:.5f}" for q in ['median','p95','p99']),' / '.join(f"{b['analysis']['metrics'][k][q]:.5f}" for q in ['median','p95','p99'])] for k in ['gpuDetailedDrawMs','gpuTotalMs']]))
    add('''The full normal traversal covers L0–L17, surface/orbit return, ten
pupil snaps and three reversals. The Graphics live Florida regression additionally
covers current/incoming regional preparation, L16/L17, focus-away/return, support
and drain. Warp holds 1x, 600x and 7,776,000x with no additional publication or
topology upload. Its particular camera reports zero visible terrain primitives;
it proves cadence/ownership, not high-refinement warp shading throughput.

The oldest approximately 181 ms M13.3 host event remains UNCLASSIFIED.
Measured initial five-pose frame-one spikes around 183–204 ms are dominated by
swapchain recreation; active frame 1 is 203.9621 ms, of which 187.0621 is recreate.
AMD ISA extraction stalls in diagnostic record work are excluded from final timing.
Other measured cold startup fence stalls remain unclassified: active candidate
frame 9 62.8733/59.8414 ms total/fence; grazing frame 9 76.8988/73.7231;
factor-one frame 8 48.9566/45.6168. They precede publication/query identity.
Normal dynamic maxima 194.199 and 210.892 ms lack full per-frame correlation;
do not retrospectively assign them to recreation or the candidate. No hitch-free
claim is made. Aggregate transition distributions and publication spikes above
are reported separately from settled savings.

## Builds and regression results

Native Debug/Release builds passed; managed Debug/Release solution builds passed
with zero warnings/errors. Deployment checks compare each configuration's native
DLL and all 49 shaders to its own build, not another configuration. Normal Release
fragment SHA-256 is `8c59838abb3d7ee6418695ea40f044ca1f04f5c3c9216296d5cc19bba6110d6b`,
equal to the measured candidate. Source hash:
`521435c525b1538f730fda69fb89b62ec10846c7cd870f966a2ee9076a93b598`.
''')
    add(table(['Run','Exit','Summary'],[[r['label'],r['exitCode'],'; '.join(x for x in r['lines'] if x.startswith(('Graphics ','Native GPU ','Launcher route','Florida bounded','Launcher tests','Status:')))] for r in v]))
    add('''Strict canonical Vulkan validation remains enabled; no VUID filter or
production layer behavior changes. The initial native CPU invocation omitted its
required pack argument; corrected invocation passes. This was a runner error, not
a residency failure. A first motion-summary attempt failed on mixed unavailable
metric values; parsing now records unavailable samples explicitly. Neither changed
production or test expectations. All raw failed-run logs are hashed and important
failure details are retained in compact records.

The Release headless SurfaceAnchor compound assertion is an unresolved gate.
Original and confirmation full runs each report 78/79, while unchanged
focused canonical retry, twenty private predicate probes and the private full
suite pass. Added logging changes the executed test binary and does not resolve
the stock failure. The original
exception hides which condition failed; do not call it JIT/allocation without
predicate evidence. This test does not execute the changed fragment shader.
`anchor-predicate-probe.json`, `headless-failure.txt` and validation records
preserve the distinction. No unrelated production/test correction was attempted.

## VERIFY A / VERIFY B / revision

Independent quality verifier: PASS on the actual material change and exact
attachment/derivative/geometry contract. Its strongest attack was coastal helper
derivatives and downstream roughness; the code preserves both. Independent
performance verifier: PASS on the settled claim, including all 100 samples,
49-shader identity, normal-native control and paired CPU/residency accounting.
No production defect was found to revise. Their full boundaries and limitations
are in `reviews.md`. The later headless gate is not silently covered by those
earlier candidate-specific PASS judgments.

## Retirement / storage / review boundary

Old unnecessary execution retires in the sole material helper; contributing-land
code remains required current production. No dual path, compatibility bridge,
fixture or TES responsibility retires. Project Control acceptance is the banking
condition; no main or milestone promotion was performed.

Reproduction: `reproduction.md`. All full numerical rows are retained losslessly
in columns; ISA, runtime/input hashes, failures and reproduction source are bounded.
No raw attachment, geometry buffer, compiled private host or bulk archive is a
permanent fixture. Permanent budget is 10 MiB; scratch raw set budget is 512 MiB.
Cleanup scope, actual bytes and final status are recorded in `closeout.json`.
Test-internal transient byte churn was not metered and is not invented.

Final lead judgment, technical classification and complete Git status are in
`closeout.md`. No M13.4 title or tag is assigned before all acceptance gates pass.
''')
    (P/'README.md').write_text('\n'.join(sections),encoding='utf-8')
if __name__=='__main__':main()
