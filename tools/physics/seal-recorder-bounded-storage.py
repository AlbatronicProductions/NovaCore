"""Read-only source/package and raw-to-capsule identity verification. No GPU."""
import argparse, hashlib, io, json, os, pathlib, subprocess, zipfile

p = argparse.ArgumentParser()
p.add_argument('--output', type=pathlib.Path, required=True)
p.add_argument('--before', action='store_true')
a = p.parse_args()
repo = pathlib.Path(__file__).resolve().parents[2]
entry = json.loads((repo/'build/recorder-bounded-storage/entry.json').read_text())
root = pathlib.Path(os.environ['LOCALAPPDATA'])/'NovaCore/MinimumRecorder'
def sha(data): return hashlib.sha256(data).hexdigest()
def identity(path): return {'bytes': path.stat().st_size, 'sha256': sha(path.read_bytes())}
def git(*args): return subprocess.check_output(['git', *args], cwd=repo, env={**os.environ,'GIT_OPTIONAL_LOCKS':'0'}).decode().strip()
runtime = {str(f.relative_to(root)): identity(f) for f in sorted(root.rglob('*')) if f.is_file()}
before = entry['runtimeBefore']
if a.before:
    assert runtime == before, 'Runtime evidence changed since entry'
    a.output.write_text(json.dumps({'verifiedFiles': len(runtime), 'bytes': sum(v['bytes'] for v in runtime.values()),'unchanged': True}, indent=2)+'\n')
    print(a.output.read_text())
    raise SystemExit(0)

original_sessions = sorted({n.split('\\')[0] for n in before if '\\' in n})
retained, retired, capsules = [], [], []
for sid in original_sessions:
    expected = {n.split('\\',1)[1]: v for n,v in before.items() if n.startswith(sid+'\\')}
    if (root/sid).is_dir():
        actual = {f.name: identity(f) for f in (root/sid).iterdir() if f.is_file()}
        assert actual == expected, f'Retained raw changed: {sid}'
        retained.append({'session':sid,'bytes':sum(v['bytes'] for v in actual.values()),'originalFilesUnchanged':len(actual)})
    else:
        path=root/f'capsule-{sid}.bin'; data=path.read_bytes()
        assert hashlib.sha256(data[:-32]).digest()==data[-32:]
        with zipfile.ZipFile(io.BytesIO(data[:-32])) as z:
            actual={n:{'bytes':len(b),'sha256':sha(b)} for n in z.namelist() if n!='evidence.json' for b in [z.read(n)]}
            assert actual==expected, f'Lossless capsule mismatch: {sid}'
            evidence=json.loads(z.read('evidence.json'))
        tool=repo/'tools/NovaCore.Recorder/bin/Release/net10.0-windows/NovaCore.Recorder.exe'
        parsed=json.loads(subprocess.check_output([str(tool),'recover-runtime-capsule',sid],cwd=repo).decode())
        capsules.append({'session':sid,**identity(path),'originalFilesPreserved':len(actual),'recovery':parsed,'classification':evidence['baseClassification']})
        retired.append({'session':sid,'bytes':sum(v['bytes'] for v in expected.values()),'replacement':path.name})

all_files={n:sha((repo/n).read_bytes()) for n in sorted(set(git('ls-files','--cached','--others','--exclude-standard','-z').split('\0'))-{''}) if (repo/n).is_file()}
production={n:v for n,v in all_files.items() if n.startswith(('src/','native/','samples/','assets/','external/','tools/NovaCore.')) or n in ('Directory.Build.props','Directory.Build.targets','Directory.Packages.props','NovaCore.sln','global.json','NuGet.Config')}
package_path=repo/'tools/NovaCore.App/bin/Release/net10.0-windows'
package={f.relative_to(package_path).as_posix():identity(f) for f in sorted(package_path.rglob('*')) if f.is_file()}
head=git('rev-parse','HEAD'); refs=git('show-ref'); index_sha=sha((repo/'.git/index').read_bytes())
assert head==entry['head'] and refs==entry['refs'].strip() and index_sha==entry['index'], 'Git authority changed'
removed=[n for n in entry['files'] if n not in all_files]; assert not removed, removed
changed={n:{'before':v,'after':all_files[n]} for n,v in entry['files'].items() if all_files[n]!=v}
assert not any(n.startswith(('native/','src/NovaCore.Simulation/','src/NovaCore.Physics/','src/NovaCore.Graphics/','assets/')) for n in changed), 'Unrelated physical/render source changed'
after_bytes=sum(v['bytes'] for v in runtime.values()); reservation=87138304
outstanding=sum(max(0,reservation-v['bytes']) for v in retained)
result={'judgment':'PASS','gpuExposure':0,'runtimeRoot':str(root),'beforeBytes':sum(v['bytes'] for v in before.values()),'afterBytes':after_bytes,
 'reclaimedBytes':sum(v['bytes'] for v in before.values())-after_bytes,'retainedRaw':retained,'retiredRaw':retired,'capsules':capsules,
 'indexesCreated':len(list(root.glob('ordinary-forensic-index.bin'))),'capBytes':536870912,'maximumNewSessionBytes':reservation,
 'controlPublicationReservationBytes':1048576,'retainedSessionOutstandingBytes':outstanding,
 'physicalHeadroomBytes':536870912-after_bytes,'committedHeadroomBytes':536870912-after_bytes-outstanding,
 'headroomAfterNewSessionAndControlBytes':536870912-after_bytes-outstanding-reservation-1048576,
 'git':{'head':head,'indexSha256':index_sha,'allRefsCount':len(refs.splitlines()),'allRefsSha256':sha((refs+'\n').encode()),'stagedNames':git('diff','--cached','--name-only')},
 'sourceSha256':sha(json.dumps(production,sort_keys=True,separators=(',',':')).encode()),'runtimeSourceFiles':len(production),
 'packageSha256':sha(''.join(f'{n}\0{v["bytes"]}\0{v["sha256"]}\n' for n,v in sorted(package.items())).encode()),
 'packageCount':len(package),'packageBytes':sum(v['bytes'] for v in package.values()),'executable':package['NovaCore.exe'],
 'changedEntryFiles':changed,'addedFiles':{n:v for n,v in all_files.items() if n not in entry['files']},
 'runtimeFiles':runtime,'runtimeSourceManifest':production,'packageManifest':package,'workingFileManifest':all_files}
assert after_bytes<=536870912 and result['headroomAfterNewSessionAndControlBytes']>=0
a.output.write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps({k:v for k,v in result.items() if k not in ('runtimeFiles','runtimeSourceManifest','packageManifest','workingFileManifest','changedEntryFiles','addedFiles','capsules')},indent=2))
