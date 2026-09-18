"""Consolidate existing output only. Does not build, run simulations or change source."""
from pathlib import Path
import hashlib, json, re, shutil

r=Path('E:/NovaCore'); e=Path(__file__).parent; b=r/'build/srv01-stage5-stock'
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def read(n): return (b/n).read_text(encoding='utf-8-sig')
def write(n,data): (e/n).write_text(json.dumps(data,indent=2)+'\n')
def records(text,prefix): return [json.loads(x[len(prefix):]) for x in text.splitlines() if x.startswith(prefix)]
physical={}
for config in ['debug','release']:
    text=read(config+'-final-qualification.log')
    settled=[{k:float(v) for k,v in re.findall(r'(height|drift|speed|angular)=([^ ]+)',line)} for line in text.splitlines() if line.startswith('FLORIDA_SITE_SETTLED')]
    assert len(settled)==600
    physical[config]=dict(peakMetres=float(re.search(r'peakPenetration=([^ ]+)',text)[1]),supportIntervals=len(settled),
        settledMaxima={key:max(row[key] for row in settled) for key in settled[0]},
        witnesses=[line for line in text.splitlines() if line.startswith(('FLORIDA_SITE_FRAME','FLORIDA_STOCK_IDENTITY','FLORIDA_SITE_CHEAP','FLORIDA_INERTIAL','FLORIDA_SCHEDULE'))],
        allocation=read(config+'-final-allocation.log').splitlines(),
        presentation=[line for line in read(config+'-final-presentation.log').splitlines() if line.startswith(('ORDINARY_','FLORIDA_PRESENTATION'))])
costs=[]
for i in range(1,4):
    text=read(f'release-final-costs-{i}.log')
    costs.append(dict(process=i,performance=records(text,'FLORIDA_PERFORMANCE ')[0],storage=records(text,'FLORIDA_STORAGE ')[0],
        finalNativeWitness=[x for x in text.splitlines() if x.startswith('FLORIDA_STORAGE_FINAL')][0]))
live=read('live-frame-1.log')
runner=(r/'tests/NovaCore.Simulation.Tests/Program.cs').read_text().split('var tests = new (string Name, Action Test)[]',1)[1].split('foreach (var (name, test)',1)[0]
groups=re.findall(r'^\s*\("([^"]+)",',runner,re.M)
assert len(groups)==80
for config in ['debug','release']:
    lines=read(config+'-simulation-full.log').splitlines()
    assert all('PASS '+name in lines for name in groups)
shutil.copyfile(b/'live-frame-1.log',e/'first-live-process.txt')
runInventory=[]
for p in sorted(b.glob('*.log')):
    text=p.read_text(encoding='utf-8-sig')
    runInventory.append(dict(file=p.name,bytes=p.stat().st_size,sha256=sha(p),
        recordedResult='BUILD FAILURE' if 'Build FAILED.' in text else 'LIVE PERFORMANCE STOP' if p.name=='live-frame-1.log' else 'COMPLETED; SEE WITNESSES',
        witnesses=[x for x in text.splitlines() if x.startswith(('PASS ','FAIL ','Build ','    0 Warning','    0 Error','ORDINARY_','FLORIDA_PERFORMANCE','FLORIDA_STORAGE','FLORIDA_PRESENTATION_STORAGE'))]))
write('results.json',dict(physical=physical,completeOperationProcesses=costs,
    presentationStorage=[x for x in read('release-presentation-storage.log').splitlines() if x.startswith('FLORIDA_PRESENTATION_STORAGE')],
    live=dict(processesExecuted=1,furtherProcesses='NOT RUN: material unclassified tail',manual='NOT RUN: performance stop',
        status='REVISE',cause='UNCLASSIFIED',
        witness=[x for x in live.splitlines() if x.startswith(('STOCK_ASSEMBLY_END','STOCK_ASSEMBLY_FRAME','SRV01_LIVE_SERVICE','SRV01_COLD','[native] Average frame','[native] Frame pacing','[native] Fence wait','[native] CPU timings','[native] GPU timing averages'))]),
    fullSimulationGroups={c:len(groups) for c in ['debug','release']},
    stoppedGates=['Further live populations','Manual Florida acceptance','Focused Launcher regression','Final Debug compilation of subsequently added presentation-storage-only test method'],
    buildHistory=['Initial new test double/string comparison CS0019 corrected before tests','Existing Stage4 native path absent; reused actual Stage3 native outputs','Debug and Release full solutions passed 0 warnings/errors','Later new presentation-storage-only test compiled/executed Release; Debug was not repeated after live stop'],
    runInventory=runInventory))
names=['srv01-display-gap-population','srv01-startup-first-present','srv01-stage2-manual','srv01-stage3','srv01-stage4','srv01-stage5','srv01-stage5-closure','srv01-stage5-stock']
keep={'srv01-stage3':'KEEP NOW: native DLLs/shaders used by the current build; retain pending next decision',
      'srv01-stage5':'KEEP NOW: exact previous failing-population binaries directly referenced by the retained causal harness',
      'srv01-stage5-stock':'KEEP NOW: current stopped executable and logs for Project Control; prepared manual route not authorized to run'}
inventory=[]
for name in names:
    path=r/'build'/name;files=[p for p in path.rglob('*') if p.is_file()]
    assert not any(p.is_symlink() for p in path.rglob('*'))
    inventory.append(dict(path=str(path),files=len(files),bytes=sum(p.stat().st_size for p in files),disposition=keep.get(name,'DISPOSE: results and exact source/reproduction inputs retained outside build')))
write('cleanup-inventory.json',dict(paths=inventory,removedFiles=0,removedBytes=0,
    totalFiles=sum(x['files'] for x in inventory),totalBytes=sum(x['bytes'] for x in inventory),
    newRoot=str(b),newFiles=inventory[-1]['files'],newBytes=inventory[-1]['bytes']))
print('Collected existing results; 1 live stop retained; no new execution. Counts:',[(x['path'].split('\\')[-1],x['files'],x['bytes']) for x in inventory])
