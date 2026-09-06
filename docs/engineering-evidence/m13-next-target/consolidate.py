"""Consolidate this investigation's bounded numerical evidence; never deletes files."""
import hashlib
import json
from pathlib import Path
import sys
sys.dont_write_bytecode = True
from analyze_cadence import cadence

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / 'build/m13-next-target'
HERE = Path(__file__).resolve().parent

def main():
    files = sorted(p for p in OUT.iterdir() if p.is_file())
    inventory = [dict(path=str(p.relative_to(ROOT)), bytes=p.stat().st_size,
                      sha256=hashlib.sha256(p.read_bytes()).hexdigest()) for p in files]
    runs = []
    cadenceResults = []
    for path in files:
        if path.suffix == '.json':
            value = json.loads(path.read_text(encoding='utf-8'))
            if isinstance(value, dict) and 'metrics' in value:
                # The baseline owns the common 49-shader hash set; preserve variant identity.
                shaders = value.pop('deployedShaderHashes', None)
                if isinstance(shaders,dict):
                    baseline = json.loads((HERE/'baseline.json').read_text())['shaders']
                    assert {Path(k).name: v for k,v in shaders.items()} == baseline
                elif shaders:
                    value['provenanceLimitation']='Component probe runner shadowed the shader-hash variable with its replacement needle; per-run hash set unavailable. Source recipe and before/after deployment hashes retained.'
                runs.append(value)
        if path.suffix == '.log' and not path.stem.endswith('-failed') and path.stem.startswith(('near-','cadence-','florida-','copy-')):
            value = cadence(path)
            identities = value.pop('identityRows')
            value['identityBounds'] = [identities[0], identities[-1]] if identities else []
            value['observedCurrentLevels'] = sorted({x['currentLevel'] for x in identities}) if identities and 'currentLevel' in identities[0] else []
            cadenceResults.append(value)
    for name, value in [('measurements.json', runs), ('cadence.json', cadenceResults), ('disposable-manifest.json', inventory)]:
        (HERE/name).write_text(json.dumps(value,indent=2)+'\n',encoding='utf-8')
    for name in ['copy-parity.json','final-validation.log']:
        (HERE/name).write_bytes((OUT/name).read_bytes())
    print('Consolidated',len(runs),'runs and',len(cadenceResults),'cadence windows; disposable bytes',sum(x['bytes'] for x in inventory))

if __name__ == '__main__':
    main()
