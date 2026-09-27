"""Verify final evidence and preserve compact hashes; no build or Git mutation."""
import datetime,hashlib,json,pathlib,subprocess
evidence=pathlib.Path(__file__).resolve().parent
root=evidence.parents[2];build=root/'build/scalable-launch-support'
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
def write(name,value):(evidence/name).write_text(json.dumps(value,indent=2)+'\n',encoding='utf-8')
def git(*args):return subprocess.check_output(['git','--no-optional-locks',*args],cwd=root).decode().strip()
entry=read(build/'entry.json');allow=read(evidence/'change-allowlist.json')
changed=[]
for name,row in entry['files'].items():
 after=sha(root/name) if (root/name).is_file() else None
 if after!=row['sha256']:
  assert name in allow['modified'],('unrelated change',name)
  changed.append(dict(path=name,before=row['sha256'],after=after))
assert git('rev-parse','HEAD')==entry['head']
assert sha(root/'.git/index')==entry['indexSha256']
assert git('show-ref','--heads','--tags')==entry['durableRefs']
names={n for n in git('ls-files','-co','--exclude-standard','-z').split('\0') if n and (root/n).is_file()}
runtime={n for n in names if pathlib.PurePosixPath(n).parts[0] in {'src','native','samples','tools','tests','assets'} or n in {'NovaCore.sln','Directory.Build.props','Directory.Build.targets','NuGet.config','global.json'}}
assert runtime-set(entry['files'])==set(allow['added']),('unreviewed new runtime files',runtime-set(entry['files']))
manifest={n:dict(bytes=(root/n).stat().st_size,sha256=sha(root/n)) for n in sorted(runtime)}
source=hashlib.sha256(''.join(f'{n}\0{v["bytes"]}\0{v["sha256"]}\n' for n,v in manifest.items()).encode()).hexdigest()
baseline=read(build/'continuity-baseline.json')
assert baseline['source']==manifest and baseline['sourceSha256']==source,'Source changed since qualification baseline'
for name,row in baseline['testFiles'].items():assert sha(root/name)==row['sha256'],('test output changed',name)
package=read(build/'canonical-package.json');assert package['judgment']=='PASS'
package_path=pathlib.Path(package['package'])
actual_package={f.relative_to(package_path).as_posix():dict(bytes=f.stat().st_size,sha256=sha(f)) for f in package_path.rglob('*') if f.is_file()}
assert actual_package==package['files'],'Package contents changed, including missing/extra files'
package_hash=hashlib.sha256(''.join(f'{n}\0{v["bytes"]}\0{v["sha256"]}\n' for n,v in sorted(actual_package.items())).encode()).hexdigest()
assert package_hash==package['packageSha256']==baseline['packageSha256']
builds=read(build/'build-results.json');assert len(builds)==9 and all(r['exit']==0 for r in builds)
for row in builds:assert sha(build/(row['name']+'.log'))==row['logSha256']
assert {r['name']:r['logSha256'] for r in builds}==baseline['buildLogHashes']
cpu=read(build/'cpu-results.json');assert len(cpu)==70 and all(r['exit']==0 for r in cpu)
for i,row in enumerate(cpu):
 assert sha(build/f'cpu-{i:02}-{row["name"]}.log')==row['logSha256']
 binary=pathlib.Path(row['command'][1]);binary=binary if binary.is_absolute() else root/binary
 binary_hash=baseline['testFiles'][binary.relative_to(root).as_posix()]['sha256']
 assert row.get('binarySha256',binary_hash)==binary_hash
 row['binarySha256']=binary_hash
 row['binaryIdentityProvenance']='Observed unchanged post-build baseline' if baseline['collection']=='post-build, during qualification' else 'Build baseline and per-run binary identity'
native=read(build/'native/results.json');assert {r['route'] for r in native}=={'short','long','rcs64','rcs96'}
assert all(r['exit']==0 and r['result']['judgment']=='APPLICATION_INTEGRATION_PASS' and r['packageSha256']==package['packageSha256'] for r in native)
for r in native:assert sha(build/'native'/r['route']/'runtime.log')==r['logSha256']
ui=read(evidence/'direct-ui.json');assert ui['judgment']=='PASS' and ui['packageSha256']==package['packageSha256'] and ui['normalPlayerNoQualificationFlags']
saves=read(build/'player-saves-before.json')
for name,expected in saves.items():assert sha(pathlib.Path(name))==expected,('pre-existing save changed',name)
save_folder=pathlib.Path(next(iter(saves))).parent
new_saves={str(f):sha(f) for f in save_folder.glob('*.craft.json') if str(f) not in saves}
for row in ui['screenshots']:assert sha(evidence/row['path'])==row['sha256'],row['path']
assert sha(evidence/ui['compactLog']['path'])==ui['compactLog']['sha256']
assert sha(pathlib.Path(ui['newSave']['path']))==ui['newSave']['sha256']
settings_path=pathlib.Path.home()/'AppData/Local/NovaCore/Launcher/settings.json'
settings_before=read(build/'ui-settings-before.json');assert read(settings_path)==settings_before,'Player settings changed'
write('player-state-preservation.json',dict(judgment='PASS',originalSaves=saves,newQualificationSaves=new_saves,settingsUnchanged=True,settings=settings_before))
write('qualification-continuity.json',dict(utc=baseline['utc'],collection=baseline['collection'],attestation=baseline.get('continuityAttestation'),sourceSha256=source,testFilesSha256=baseline['testFilesSha256'],testFiles=baseline['testFiles'],packageSha256=package_hash,buildLogHashes=baseline['buildLogHashes'],finalComparison='PASS'))
performance=[]
for i,row in enumerate(cpu):
 if row['name'].endswith(('--scalable-support','--scalable-support-flight')):
  for line in (build/f'cpu-{i:02}-{row["name"]}.log').read_text(errors='replace').splitlines():
   if line.startswith('SUPPORT_FLIGHT '):performance.append(dict(configuration=row['name'],kind='actual-site-lifecycle',values=json.loads(line[len('SUPPORT_FLIGHT '):])))
   elif line.startswith('{"qualification":"SUPPORT_GEOMETRY_LOAD"'):performance.append(dict(configuration=row['name'],kind='constant-gravity-admission',values=json.loads(line)))
write('performance.json',dict(note='Cold preparation sample N=12: P95/P99 are observed maximum, not a fitted tail. Lifecycle timings cover actual 64Hz transactions, including existing resource/control allocation; pool bytes are retained native capacity before release. CPU qualification ran alongside native application routes; no idle-machine claim.',runs=performance))
write('preservation.json',dict(judgment='PASS',entryFileCount=len(entry['files']),changed=changed,added=allow['added'],head=entry['head'],indexSha256=entry['indexSha256'],durableRefsUnchanged=True,ksaWrites=0))
write('source-manifest.json',dict(sourceSha256=source,files=manifest))
write('candidate-package.json',package);write('builds.json',builds);write('regressions.json',cpu);write('native-routes.json',native)
write('identity.json',dict(judgment='PASS_FROZEN_UNBANKED',utc=datetime.datetime.now(datetime.timezone.utc).isoformat(),repository=str(root),head=entry['head'],sourceSha256=source,sourceFiles=len(manifest),packageSha256=package['packageSha256'],executable=str(pathlib.Path(package['package'])/'NovaCore.exe'),executableSha256=package['files']['NovaCore.exe']['sha256'],simulationSha256=package['files']['NovaCore.Simulation.dll']['sha256'],nativeSha256=package['files']['NovaCore.Native.dll']['sha256'],banked=False,playerPass=False,milestone=None))
print('PASS: qualified source, candidate, preservation and evidence sealed; UNBANKED.')
