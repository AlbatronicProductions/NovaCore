"""Seal this campaign against its captured frozen working-source baseline.

Run only after native qualification. Reads production/runtime files; writes this
evidence directory. Does not stage, commit, mutate Git metadata, or build.
"""
import datetime,difflib,hashlib,json,math,pathlib,re,shutil,subprocess

evidence=pathlib.Path(__file__).resolve().parent
root=evidence.parents[2]
build=root/'build/rcs-scalability'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
def write(name,data):
    (evidence/name).write_text(json.dumps(data,indent=2)+'\n',encoding='utf-8')
def git(*args,cwd=root):
    return subprocess.check_output(['git',*args],cwd=cwd).decode().strip()
entry=read(build/'entry.json');frozen=pathlib.Path(entry['source'])
package=read(build/'canonical-package.json')
assert package['judgment']=='PASS'
assert all(sha(pathlib.Path(package['package'])/k)==v['sha256'] for k,v in package['files'].items())
runs=[x for x in read(build/'native-qualified/results.json') if x['route'].startswith('rcs')]
preservation_runs=build/'native-preservation/results.json'
runs += [x for x in read(preservation_runs if preservation_runs.exists() else build/'native-qualified/results.json') if not x['route'].startswith('rcs')]
assert {x['route'] for x in runs}=={'short','long','rcs64','rcs96'}
assert all(x['exit']==0 and x['result']['judgment']=='APPLICATION_INTEGRATION_PASS' for x in runs)
assert all(sum(w['rcsFrames'] for w in x['windows'])>=768 for x in runs if x['route'].startswith('rcs'))
source_mismatch=[k for k,v in entry['files'].items() if not (frozen/k).is_file() or sha(frozen/k)!=v['sha256']]
package_mismatch=[k for k,v in entry['frozenPackage']['files'].items() if not (pathlib.Path(entry['frozenPackage']['package'])/k).is_file() or sha(pathlib.Path(entry['frozenPackage']['package'])/k)!=v['sha256']]
preservation=dict(sourceFiles=len(entry['files']),sourceMismatches=source_mismatch,packageMismatches=package_mismatch,
    headUnchanged=git('rev-parse','HEAD',cwd=frozen)==entry['head'],refsUnchanged=git('show-ref',cwd=frozen)==entry['refs'].strip(),
    indexUnchanged=sha(frozen/'.git/index')==entry['indexSha256'],frozenPackageSha256=entry['frozenPackage']['packageSha256'])
preservation['judgment']='PASS' if not source_mismatch and not package_mismatch and all(preservation[k] for k in ['headUnchanged','refsUnchanged','indexUnchanged']) else 'FAIL'
write('frozen-preservation.json',preservation);assert preservation['judgment']=='PASS'
names=set(git('ls-files','-co','--exclude-standard','-z').split('\0'))
names={n for n in names if n and (root/n).is_file()}
selected={n for n in names if pathlib.PurePosixPath(n).parts[0] in {'src','native','samples','tools','tests','assets'} or n in {'NovaCore.sln','Directory.Build.props','Directory.Build.targets','NuGet.config','global.json'}}
files={n:dict(sha256=sha(root/n),bytes=(root/n).stat().st_size) for n in sorted(selected)}
source_sha=hashlib.sha256(json.dumps(files,sort_keys=True,separators=(',',':')).encode()).hexdigest()
write('source-manifest.json',dict(sourceSha256=source_sha,files=files))
changed=[];patch=[]
for n in sorted(names|set(entry['files'])):
    if n.startswith('docs/engineering-evidence/rcs-attitude-scalability/'):continue
    before=entry['files'].get(n,{}).get('sha256');after=sha(root/n) if (root/n).is_file() else None
    if before==after:continue
    changed.append(dict(path=n,before=before,after=after))
    old=(frozen/n).read_text(encoding='utf-8-sig') if before else ''
    new=(root/n).read_text(encoding='utf-8-sig') if after else ''
    patch.extend(difflib.unified_diff(old.splitlines(keepends=True),new.splitlines(keepends=True),fromfile='a/'+n if before else '/dev/null',tofile='b/'+n if after else '/dev/null'))
write('change-inventory.json',changed)
with (evidence/'change.patch').open('w',encoding='utf-8',newline='\n') as f:f.writelines(patch)
write('candidate-package.json',package)
write('native-results.json',runs)
write('frozen-entry.json',entry)
for source,target in [('debug-cpu.json','debug-cpu.json'),('release-cpu.json','release-cpu.json'),('regressions/results.json','regressions.json'),('legacy/results.json','legacy-regressions.json')]:shutil.copyfile(build/source,evidence/target)
def distribution(values):
    values=sorted(values)
    return None if not values else {k:values[min(len(values)-1,math.ceil(q*len(values))-1)] for k,q in [('median',.5),('p95',.95),('p99',.99),('max',1)]}
gpu=[]
for run in runs:
    log=pathlib.Path(run['command'][run['command'].index('--qualify-editor')+1]).parent/'runtime.log';assert sha(log)==run['logSha256']
    samples=[];health=[];submitted=[];whole=[];cpu=[];input_trace=[];step=-1
    for line in log.read_text(encoding='utf-8',errors='replace').splitlines():
        m=re.search(r'APPLICATION_QUALIFY step=(\d+)',line)
        if m:step=int(m[1])
        if 'GPU timings:' in line:
            fields={k:float(v) for k,v in re.findall(r'(\w+)=([\d.]+)',line)};samples.append(dict(step=step,**fields))
        if line.startswith('MODULAR_FRAME '):whole.append(line)
        if 'CPU timings:' in line:cpu.append(dict(step=step,**{k:float(v) for k,v in re.findall(r'(\w+)=([\d.]+)',line)}))
        if line.startswith('MODULAR_INPUT '):input_trace.append(line)
        m=re.search(r'Renderer health: completedFrame=(\d+); extent=(\d+x\d+).*fenceMs=([\d.]+)',line)
        if m:health.append(dict(step=step,completedFrame=int(m[1]),extent=m[2],fenceMs=float(m[3])))
        m=re.search(r'GPU anchored refinement: submittedFrame=(\d+)',line)
        if m:submitted.append(int(m[1]))
    gpu.append(dict(route=run['route'],scope='Periodic asynchronous GPU queries labelled by latest host step; not frame-aligned CPU/GPU subtraction.',
        samples=samples,health=health,cpuPeriodic=cpu,initialFlight4096IncludingCold=whole,inputTrace=input_trace,submittedCount=len(submitted),submittedFirst=submitted[0] if submitted else None,submittedLast=submitted[-1] if submitted else None,
        submittedMonotone=all(a<b for a,b in zip(submitted,submitted[1:])),completedMonotone=all(a['completedFrame']<b['completedFrame'] for a,b in zip(health,health[1:])),
        phaseTotalMs={phase:distribution([s['total'] for s in samples if s['step'] in steps]) for phase,steps in [('supported',{18}),('powered',{19,20}),('coast',{21})]}))
write('native-gpu-samples.json',gpu)
builds=[]
for cfg in ['debug','release']:
    path=build/f'final-build-{cfg}.log';text=path.read_text(encoding='utf-8-sig');assert 'Build succeeded.' in text and '0 Error(s)' in text
    builds.append(dict(configuration=cfg,sha256=sha(path),output=text.strip()))
write('builds.json',builds)
write('identity.json',dict(judgment='ENGINEERING_PASS_INTEGRATION_PASS_UNBANKED',sealedUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(),developmentRepository=str(root),
    frozenRepository=str(frozen),head=entry['head'],sourceSha256=source_sha,packageSha256=package['packageSha256'],candidateExecutable=str(pathlib.Path(package['package'])/'NovaCore.exe'),
    frozenPackageSha256=entry['frozenPackage']['packageSha256'],milestone=None,banked=False,playerPass=False,sourceCount=len(files),changedFileCount=len(changed)))
print(json.dumps(dict(sourceSha256=source_sha,packageSha256=package['packageSha256'],sourceFiles=len(files),changedFiles=len(changed),preservation=preservation['judgment']),indent=2))
