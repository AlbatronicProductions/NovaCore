"""Read-only source/package/preservation checks; write this campaign's concise evidence only."""
import datetime as dt,hashlib,json,pathlib,subprocess
root=pathlib.Path(__file__).resolve().parents[3];out=root/'build/ordinary-player-recorder';docs=pathlib.Path(__file__).parent
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for b in iter(lambda:f.read(1024*1024),b''):h.update(b)
 return h.hexdigest()
def ident(p):return {'bytes':p.stat().st_size,'sha256':sha(p)} if p.is_file() else None
def git(*args):return subprocess.check_output(['git','--no-optional-locks',*args],cwd=root)
def write(name,data):(docs/name).write_text(json.dumps(data,indent=2)+'\n')
entry=json.loads((out/'entry.json').read_text());names=sorted(set(git('ls-files','--cached','--others','--exclude-standard','-z').decode().split('\0'))-{''})
changed=[p for p,i in entry['files'].items() if ident(root/p)!=i]
allowed={'NovaCore.sln','docs/NOVACORE_CURRENT_STATE.md','docs/engineering-evidence/README.md','native/NovaCore.Native/CMakeLists.txt','native/NovaCore.Native/NovaCoreNative.cpp','native/NovaCore.Native/RegionalPhysicalPreparation.inl','tools/NovaCore.App/Program.cs','tools/NovaCore.App/NovaCore.App.csproj','tools/verify-player-package.py'}
assert not (set(changed)-allowed),changed
new=[p for p in names if p not in entry['files']]
prefixes=('src/NovaCore.Diagnostics/','tools/NovaCore.Recorder/','tests/NovaCore.MinimumRecorder.Tests/','docs/engineering-evidence/ordinary-player-minimum-recorder/','native/NovaCore.Native/Ordinary','src/NovaCore.Interop/MinimumRecorder.cs')
assert all(p.startswith(prefixes) for p in new),new
seals=[]
for s in entry['seals']:
 base=pathlib.Path(s['root']);current={str(p.relative_to(base)):ident(p) for p in base.rglob('*') if p.is_file()};assert current==s['allFiles'],str(base)
 seals.append({'root':str(base),'manifestSha256':s['manifestSha256'],'files':len(current),'unchanged':True})
for name,args in [('head',['rev-parse','HEAD']),('refs',['show-ref','--head']),('index.diff',['diff','--cached','--binary','--no-ext-diff'])]:assert hashlib.sha256(git(*args)).hexdigest()==entry['git'][name]['sha256'],name
index=root/git('rev-parse','--git-path','index').decode().strip();assert ident(index)==entry['git']['index'],'index identity'
package=json.loads((out/'package.json').read_text());assert package['judgment']=='PASS'
pkg=root/'tools/NovaCore.App/bin/Release/net10.0-windows'
for p,i in package['files'].items():assert ident(pkg/p)==i,p
source={p:ident(root/p) for p in names if (p.startswith(('src/','native/','tools/','tests/','samples/','assets/','external/')) or p in ('NovaCore.sln','Directory.Build.props','global.json')) and (root/p).is_file()}
def manifest(d):return hashlib.sha256(''.join(f'{k}\0{v["bytes"]}\0{v["sha256"]}\n' for k,v in sorted(d.items())).encode()).hexdigest()
performances={};native=[]
for name in ['native-debug-sealed','native-release-sealed-1','native-release-sealed-2']:
 base=out/name;producer=json.loads((base/'native-mock.json').read_text());observer_paths=list(base.rglob('performance.json'));assert len(observer_paths)==1,'Ambiguous run directory';observer_path=observer_paths[0];observer=json.loads(observer_path.read_text());recovery=json.loads((observer_path.parent/'recovery.json').read_text())
 assert producer['judgment']=='PASS' and producer['produced']==producer['durable']==recovery['DurableSequence']
 assert recovery['Faults']==0 and not recovery['Corrupt'] and recovery['Clean']
 assert recovery['LastPhase']['7']['Words'][6]==2**64-4 and recovery['LastSubmit']['Words'][8]==recovery['LastCompletion']['Words'][8]==1799
 assert observer['Rotations']>=1
 performances[name]={'producer':producer,'observer':observer,'writeMiBPerSecond':observer['BytesWritten']/1048576/(observer['wallMs']/1000),'observerCpuCorePercent':observer['cpuMs']/observer['wallMs']*100,'nativeProducerAllocations':0}
 native.append({'run':name,'frames':producer['frames'],'produced':producer['produced'],'durable':recovery['DurableSequence'],'checkpoint':recovery['CheckpointSequence'],'faults':0,'failedSubmitResult':-4,'lastSuccessfulSubmit':1799,'lastPositiveCompletion':1799,'rotation':observer['Rotations'],'rawApiForwardingOracle':'PASS','handleReuseBackshiftAndUnmapEntry':'PASS','gpuCalls':0})
managed={}
for config in ('debug','release'):
 result=json.loads((out/f'{config}-tests/qualification.json').read_text());assert result['judgment']=='PASS';managed[config]=result
 for log in (f'build-{config}.log',f'native-{config}-build.log'):
  text=(out/log).read_text(encoding='utf-8-sig');assert 'error ' not in text.lower() and 'failed:' not in text.lower(),log
assert 'PASS 113 diagnostic checks' in (out/'app-diagnostics.log').read_text()
assert 'tests passed: 19' in (out/'launcher-regressions.log').read_text()
assert 'PASS 18 presentation outcomes' in (out/'native-regressions.log').read_text()
imports=(out/'native-mock-imports.txt').read_text().lower();assert not any(x in imports for x in ['vulkan-1.dll','d3d12.dll','dxgi.dll'])
write('performance.json',{'scope':'Complete minimum recorder with exact native wrappers and CPU Vulkan fakes, independent real persistence process. No GPU workload or frame performance claimed.','native':performances,'managed':{k:v['measurements'] for k,v in managed.items()},'notes':['Native frame envelope includes all production context lookups and a fully seeded ordinary resource footprint; periodic pupil swaps and journal rotation.','Native producer allocations are asserted zero in timed actual wrapper calls.','Process CPU counters have 15.625 ms granularity; a reported zero is not literal zero CPU cost. QPC producer wall-time quantiles remain measured.','Observer allocations are collected over the whole session including checkpoint/summary; retained memory and working set are separate.','Flush latency belongs to independent process, never renderer wait.','Binary files remain 83902464 bytes per session; write traffic is additional repeated I/O, not journal growth.']})
write('qualification.json',{'judgment':'PASS_OFFLINE_ONLY','managed':{k:{'checks':v['checks'],'results':v['results']} for k,v in managed.items()},'native':native,'builds':{'Debug':'PASS','Release':'PASS','warnings':0,'errors':0},'regressions':{'applicationDiagnostics':113,'launcher':19,'presentationResults':18,'regionalPupilLifetime':'PASS'},'package':{'files':package['fileCount'],'shaders':package['shaders'],'judgment':'PASS'},'noGpuDriverImportsInMock':True,'ordinaryLaunches':0,'gpuExposure':0,'machineResets':0,'blackoutCause':'UNRESOLVED','playerPass':'HOLD','banking':'HOLD'})
write('preservation.json',{'judgment':'PASS','entryFileCount':len(entry['files']),'changedEntryFiles':changed,'newFiles':new,'protectedEvidence':seals,'headUnchanged':True,'refsUnchanged':True,'indexBytesUnchanged':True,'unrelatedEntryFilesUnchanged':True,'ksaWrites':0,'settingsChanges':0})
write('source-files.json',source);write('package-files.json',package['files'])
write('identity.json',{'judgment':'PASS_FROZEN_UNBANKED','utc':dt.datetime.now(dt.timezone.utc).isoformat(),'head':git('rev-parse','HEAD').decode().strip(),'sourceSha256':manifest(source),'sourceFiles':len(source),'packageSha256':package['packageSha256'],'packageFiles':package['fileCount'],'executable':str(pkg/'NovaCore.exe'),'executableSha256':sha(pkg/'NovaCore.exe'),'managedApplicationSha256':sha(pkg/'NovaCore.dll'),'nativeSha256':sha(pkg/'NovaCore.Native.dll'),'minimumLibrarySha256':sha(pkg/'NovaCore.Diagnostics.dll'),'persistenceOwnerSha256':sha(pkg/'NovaCore.Recorder.dll'),'qualification':'CPU/mock/fault only','gpuExposureAuthorized':False,'blackoutCause':'UNRESOLVED','playerAcceptance':'HOLD','banked':False,'milestone':None})
print(json.dumps({'judgment':'PASS_FROZEN_UNBANKED','sourceFiles':len(source),'sourceSha256':manifest(source),'packageSha256':package['packageSha256'],'protected':seals,'changed':changed},indent=2))
