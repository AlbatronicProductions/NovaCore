"""Bind build inputs and test outputs without rebuilding or changing Git state."""
import argparse,datetime,hashlib,json,pathlib,subprocess
root=pathlib.Path(__file__).resolve().parents[3]
out=root/'build/scalable-launch-support';out.mkdir(parents=True,exist_ok=True)
p=argparse.ArgumentParser();p.add_argument('mode',choices=['before-build','after-build','current']);a=p.parse_args()
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
names=subprocess.check_output(['git','--no-optional-locks','ls-files','-co','--exclude-standard','-z'],cwd=root).decode().split('\0')
source={n:dict(bytes=(root/n).stat().st_size,sha256=sha(root/n)) for n in sorted(set(names)) if n and (root/n).is_file() and (pathlib.PurePosixPath(n).parts[0] in {'src','native','samples','tools','tests','assets'} or n in {'NovaCore.sln','Directory.Build.props','Directory.Build.targets','NuGet.config','global.json'})}
digest=lambda files:hashlib.sha256(''.join(f'{n}\0{v["bytes"]}\0{v["sha256"]}\n' for n,v in sorted(files.items())).encode()).hexdigest()
now=datetime.datetime.now(datetime.timezone.utc).isoformat()
if a.mode=='before-build':
 (out/'build-inputs.json').write_text(json.dumps(dict(utc=now,sourceSha256=digest(source),files=source),indent=2)+'\n');print('Build inputs captured');raise SystemExit()
if a.mode=='after-build':
 before=json.loads((out/'build-inputs.json').read_text());assert before['files']==source,'Source changed during build'
binaries={f.relative_to(root).as_posix():dict(bytes=f.stat().st_size,sha256=sha(f)) for project in (root/'tests').glob('NovaCore.*.Tests') for configuration in ['Debug','Release'] for f in (project/'bin'/configuration).rglob('*') if f.is_file()}
builds=json.loads((out/'build-results.json').read_text(encoding='utf-8-sig'));assert len(builds)==9 and all(r['exit']==0 for r in builds)
for row in builds:assert sha(out/(row['name']+'.log'))==row['logSha256']
package=json.loads((out/'canonical-package.json').read_text(encoding='utf-8-sig'))
record=dict(utc=now,collection='post-build, during qualification' if a.mode=='current' else 'immediately after build',sourceSha256=digest(source),source=source,testFiles=binaries,testFilesSha256=digest(binaries),packageSha256=package['packageSha256'],buildLogHashes={r['name']:r['logSha256'] for r in builds})
if a.mode=='current':record['continuityAttestation']='Root was the sole production writer. No runtime/test source or binary changed from final build start through this baseline capture. Earlier-started qualification rows inherit this observed unchanged baseline; these are not represented as launch-time hashes. Only evidence documents were edited.'
else:record['preBuildUtc']=before['utc']
(out/'continuity-baseline.json').write_text(json.dumps(record,indent=2)+'\n',encoding='utf-8')
print('Candidate continuity baseline captured:',record['collection'],record['sourceSha256'],len(binaries),'test output files')
