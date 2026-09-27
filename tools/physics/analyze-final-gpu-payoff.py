"""Read-only recorded-stage payoff bounds. Counterfactuals are not GPU results."""
import csv
import importlib.util
import json
import re
from pathlib import Path

root = Path(__file__).resolve().parents[2]
stage = root / 'build/performance-150fps/stage-a'
spec = importlib.util.spec_from_file_location('analysis', Path(__file__).with_name('analyze-post-contact.py'))
analysis = importlib.util.module_from_spec(spec)
spec.loader.exec_module(analysis)
frames = [{k: float(v) for k, v in f.items()} for f in csv.DictReader((stage / 'native-timing.csv').open())]
by_id = {int(f['loop']): f for f in frames}
assert all(int(f['terrain']) == int(f['loop']) for f in frames), 'Terrain/loop identity requires explicit remapping'
rows = json.loads((stage / 'native.json').read_text())['rows']
samples = {int(f['gpuFrame']): f for f in frames if f['gpuSample'] > 0}
published_start = min(int(m) for m in re.findall(
    rb'Earth Vulkan submission: terrainFrame=(\d+);[^\n]*candidateIndirectDraw=1;',
    (stage / 'driver.log').read_bytes()))
out = {'ceilingMs': analysis.CEILING, 'groups': {},
       'firstLogConfirmedDetailedSubmission': published_start,
       'judgment': 'PAYOFF GATE NOT MET - no GPU production correction or new native verification',
       'limits': [
           'Values use the existing single Stage A witness, not another exposure.',
           'Zero-cost stages are deliberately impossible best-case sensitivity bounds, not implementations.',
           'Per-frame credit is limited to the existing fence/completion wall scope and uses its completed GPU owner.',
           'Fence scope includes completion/capture bookkeeping, so this credit is optimistic.',
           'Recorded schedule is held fixed; debt/scheduling feedback and clocks are not predicted.',
           'Subtracting percentiles of different scopes is not used to derive these distributions.',
           'An entire draw is not redundant. No safe recoverable material milliseconds are established.']}

for name, predicate in {
    'all': lambda r: True,
    'grounded': lambda r: r['Physical']['TerrainContacts'] > 0,
    'restingNoActuation': lambda r: r['Physical']['TerrainContacts'] > 0 and not r['Physical']['Main']
        and r['Physical']['Jets'] == 0 and r['Physical']['Velocity']['LengthSquared'] < .000004
        and r['Physical']['Angular']['LengthSquared'] < .000004,
    'heldPublishedTerrain': lambda r: r['Physical']['TerrainContacts'] > 0 and not r['Physical']['Main']
        and r['DisplayFrame'] >= published_start and samples[r['DisplayFrame']]['gpuCandidate'] > 0,
    'postHandoffNoNativeSlices': lambda r: r['Physical']['Consumer'] == 0 and r['Physical']['World'] == 0
        and r['Physical']['Sequence'] > 0 and r.get('Work') is not None and r['Work']['Slices'] == 0
}.items():
    selected = [r for r in rows if predicate(r)]
    ids = [r['DisplayFrame'] for r in selected]
    fs = [by_id[i] for i in ids]
    assert all(i + 1 in by_id and i in samples for i in ids), 'Incomplete selected coverage'
    cadence = [by_id[i + 1]['cadenceMs'] for i in ids]
    gpu = [samples[i] for i in ids]
    s = lambda values: analysis.stats(values, frame_ids=ids)
    result = {
        'frames': len(ids),
        'cadenceMs': s(cadence),
        'requiredRecoveryMs': s(max(0, v - analysis.CEILING) for v in cadence),
        'gpuMs': s(f['gpuTotal'] for f in gpu),
        'fenceCompletionMs': s(f['fenceMs'] for f in fs),
        'cpuWorkOutsideFenceWallMs': s(f['frameMs'] - f['fenceMs'] for f in fs),
        'recordedCadenceOutsideFenceWallMs': s(v - f['fenceMs'] for v, f in zip(cadence, fs)),
        'drawMs': s(f['gpuCandidate'] for f in gpu),
        'detailedDrawIncludingFillMs': s(f['gpuDetailed'] for f in gpu),
        'cullCompactMs': s(f['gpuCull'] for f in gpu),
        'preSurfaceMs': s(f['gpuPreSurface'] for f in gpu),
        'hypotheticalZeroCullGpuMs': s(f['gpuTotal'] - f['gpuCull'] for f in gpu),
        # f.gpu* belongs to the completed predecessor awaited in this CPU frame,
        # unlike gpu above, which describes the submission owned by this frame.
        'hypotheticalZeroCullCadenceMs': s(v - min(f['fenceMs'], f['gpuCull']) for v, f in zip(cadence, fs)),
        'hypotheticalZeroDetailedDrawCadenceMs': s(v - min(f['fenceMs'], f['gpuCandidate']) for v, f in zip(cadence, fs)),
        'establishedSafeRecoverableDrawMs': None,
    }
    out['groups'][name] = result

target = root / 'docs/engineering-evidence/performance-150fps/final-gpu-payoff.json'
target.write_text(json.dumps(out, indent=2) + '\n')
print('FINAL_GPU_PAYOFF_GATE_NOT_MET; production writes=0; GPU exposure=0')
for n in ('grounded', 'restingNoActuation', 'postHandoffNoNativeSlices'):
    g = out['groups'][n]
    print(n, 'zeroCullGPU', g['hypotheticalZeroCullGpuMs']['median'],
          'zeroCullCadence', g['hypotheticalZeroCullCadenceMs']['median'],
          'over', g['hypotheticalZeroCullCadenceMs']['over6667'], '/', g['frames'])
