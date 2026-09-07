"""Read-only provenance refresh; writes only this investigation's verification.json.
Run from the repository with python -B docs/engineering-evidence/ksa-terrain-architecture/verify.py.
No builds, capture, network, source modification or cleanup.
"""
import hashlib
import json
import math
from pathlib import Path
import subprocess

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
KSA = Path(r'E:\Kitten Space Agency')

def sha(path):
    with path.open('rb') as f:
        return hashlib.file_digest(f, 'sha256').hexdigest()

def git(*args):
    return subprocess.check_output(['git', *args], cwd=ROOT, text=True).strip()

def inventory(folder):
    return {p.relative_to(folder).as_posix(): {'bytes': p.stat().st_size, 'sha256': sha(p)}
            for p in sorted(folder.rglob('*')) if p.is_file()}

old = ROOT / 'docs/engineering-evidence/conservative-pre-refinement-visibility'
baseline = json.loads((old / 'baseline.json').read_text(encoding='utf-8'))
final = json.loads((old / 'final-identity.json').read_text(encoding='utf-8'))
result = {'head': git('rev-parse', 'HEAD'), 'main': git('rev-parse', 'main'),
          'originMain': git('rev-parse', 'origin/main'), 'branch': git('branch', '--show-current'),
          'status': git('status', '--short'), 'stagedDiff': git('diff', '--cached'),
          'trackedDiff': git('diff'), 'tags': {}, 'deployments': {}, 'ksa': {},
          'priorEvidence': {}, 'sourceInventory': {}}
for tag in ['m13.1-ncsm1-tes-hotpath', 'm13.2-ordinary-terrain-shading']:
    result['tags'][tag] = {'object': git('rev-parse', tag), 'type': git('cat-file', '-t', tag),
                           'commit': git('rev-parse', tag + '^{}')}
for config in ['Debug', 'Release']:
    runtime = ROOT / f'samples/NovaCore.Triangle/bin/{config}/net10.0'
    shaders = {p.name: sha(p) for p in sorted((runtime / 'shaders').glob('*.spv'))}
    native = sha(runtime / 'NovaCore.Native.dll')
    managed = sha(runtime / 'NovaCore.Triangle.dll')
    result['deployments'][config] = {'path': str(runtime), 'native': native, 'managed': managed,
        'matchesLastRestoredRuntime': native == final[config]['native'] and managed == final[config]['managed'],
        'shaders': shaders, 'allShadersMatchBanked': shaders == baseline['configurations'][config]['shaders']}
for name in ['KSA.dll', 'Planet.Core.dll', 'Planet.Render.Core.dll']:
    p = KSA / name
    result['ksa'][name] = {'bytes': p.stat().st_size, 'sha256': sha(p)}
for name in ['conservative-pre-refinement-visibility', 'post-m13.2-next-target']:
    result['priorEvidence'][name] = inventory(ROOT / 'docs/engineering-evidence' / name)
result['retiredScratchAbsent'] = not (ROOT / 'build/conservative-pre-refinement-visibility').exists()
result['retiredBytecodeAbsent'] = not (ROOT / 'docs/engineering-evidence/near-surface-performance/__pycache__').exists()
manifest = json.loads((HERE / 'source-files.json').read_text(encoding='utf-8'))
for group, paths in manifest.items():
    base = KSA if group == 'ksaInstalled' else ROOT
    result['sourceInventory'][group] = {name: sha(base / name) for name in paths}
result['representationAnalysis'] = {'method': 'IEEE binary spacing 2^(floor(log2(abs(x)))-fractionBits); not an end-to-end error proof',
    'fp32SpacingMetres': {str(x): 2.0 ** (math.floor(math.log2(x)) - 23) for x in [50, 1000, 10000, 6371008.8]},
    'fp64EarthSpacingMetres': 2.0 ** (math.floor(math.log2(6371008.8)) - 52)}
(HERE / 'verification.json').write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
assert result['head'] == result['main'] == result['originMain'] == '4accf92fd080c16cc8656080aa69def3fa65db53'
assert result['tags']['m13.1-ncsm1-tes-hotpath']['commit'] == 'fade1384c1c7df93d954e7223b1cc8f17db17f98'
assert result['tags']['m13.2-ordinary-terrain-shading']['commit'] == result['head']
assert all(v['type'] == 'tag' for v in result['tags'].values())
assert result['ksa']['KSA.dll']['sha256'] == 'a8a5164204eed962df9d9025e99f523d9a23ee2ce6e802b18216a9da35506d2f'
assert result['ksa']['Planet.Core.dll']['sha256'] == '2823b299051b91252453acb4f1fff5543b1f5473bc6f42c8d51d4958d3159ece'
assert result['ksa']['Planet.Render.Core.dll']['sha256'] == '6b9b3c3bd709bf198c6ab3ec6daaef7e468f593fb110187869a1d44272b92e3c'
assert not result['trackedDiff'] and not result['stagedDiff']
assert all(v['allShadersMatchBanked'] and v['matchesLastRestoredRuntime'] for v in result['deployments'].values())
assert result['retiredScratchAbsent'] and result['retiredBytecodeAbsent']
print('PASS: banked refs, empty tracked/staged diffs, both restored runtimes, 49+49 banked shaders, retired scratch absent.')
