"""Read-only baseline inventory; writes only the new offline incident evidence."""
import datetime, hashlib, importlib.util, json, pathlib, subprocess
HERE = pathlib.Path(__file__).resolve().parent
ROOT = HERE.parents[2]
PRIOR = HERE.parent / 'm13.6-manual-failure'
spec = importlib.util.spec_from_file_location('prior_identity_helpers', PRIOR / 'identity.py')
helper = importlib.util.module_from_spec(spec)
spec.loader.exec_module(helper)

def collect():
    expected = json.loads((PRIOR / 'identity.json').read_text())
    data = dict(capturedUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(),
        refs={r:helper.git('rev-parse',r) for r in expected['refs']}, branch=helper.git('branch','--show-current'),
        status=subprocess.check_output(['git','status','--short'],cwd=ROOT,text=True),
        staged=helper.git('diff','--cached'),
        source={r:helper.sha(ROOT/r) for r in helper.git('diff','--name-only').splitlines()},
        trackedDiffSha256=hashlib.sha256(subprocess.check_output(['git','diff','--binary'],cwd=ROOT)).hexdigest(),
        priorPackages={}, displays=helper.displays(), deployment={}, assets={})
    assert data['refs']==expected['refs'] and data['source']==expected['source']
    assert data['trackedDiffSha256']==expected['trackedDiffSha256'] and not data['staged']
    for name in ['m13-final-exit','m13.6-manual-failure']:
        folder=HERE.parent/name
        data['priorPackages'][name]={p.name:dict(bytes=p.stat().st_size,sha256=helper.sha(p)) for p in sorted(folder.iterdir()) if p.is_file()}
    for config,row in expected['deployment'].items():
        folder=ROOT/f'samples/NovaCore.Triangle/bin/{config}/net10.0'
        actual=dict(native=helper.sha(folder/'NovaCore.Native.dll'),managed=helper.sha(folder/'NovaCore.Triangle.dll'),
                    executable=helper.sha(folder/'NovaCore.Triangle.exe'),shaders={p.name:helper.sha(p) for p in folder.rglob('*.spv')})
        assert all(actual[k]==row[k] for k in actual)
        data['deployment'][config]=actual
    for rel,row in expected['assets'].items():
        data['assets'][rel]=dict(bytes=(ROOT/rel).stat().st_size,sha256=helper.sha(ROOT/rel))
        assert data['assets'][rel]['bytes']==row['bytes'] and data['assets'][rel]['sha256']==row['sha256']
    data['budget']=dict(permanentBytes=4194304,temporaryCopyBytes=268435456,rawCaptureBytes=0)
    destination=HERE/'baseline.json'
    assert not destination.exists()
    destination.write_text(json.dumps(data,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(dict(refs=data['refs'],branch=data['branch'],preservedPackages={k:len(v) for k,v in data['priorPackages'].items()},
                         deployment='unchanged',source='unchanged',displays=[r for r in data['displays'] if r['attached']]),indent=2))

if __name__=='__main__':
    collect()
