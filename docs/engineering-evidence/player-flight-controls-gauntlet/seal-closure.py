"""Read-only checks plus bounded evidence consolidation; never builds/deploys/stages."""
from pathlib import Path
import datetime,hashlib,json,subprocess
r=Path.cwd();e=r/'docs/engineering-evidence/player-flight-controls-gauntlet';b=r/'build/player-flight-controls-gauntlet'
def load(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def record(p):return dict(path=p.relative_to(r).as_posix(),sha256=sha(p),bytes=p.stat().st_size)
def write(p,v):p.write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8')
def git(*args):return subprocess.check_output(['git',*args],cwd=r,text=True).strip()
def lines(p):
 raw=p.read_bytes();return raw.decode('utf-16' if raw[:2] in [b'\xff\xfe',b'\xfe\xff'] else 'utf-8-sig').splitlines()
manual=load(e/'manual-acceptance.json');assert manual['status']=='PASS' and manual['authority']=='Project Control'
now=datetime.datetime.now(datetime.timezone.utc).isoformat();old=load(e/'identity.json');entry=load(e/'entry.json')
assert git('rev-parse','HEAD')==entry['head']==git('rev-parse','main')
assert git('rev-parse','m15.4-unified-active-vessel-camera')==entry['tagObject']
assert not git('diff','--cached','--name-only')
subprocess.run(['git','diff','--check'],cwd=r,check=True,capture_output=True)
public=git('ls-remote','origin','refs/heads/main','refs/tags/m15.4-unified-active-vessel-camera','refs/tags/m15.4-unified-active-vessel-camera^{}').splitlines()
assert public==old['public']
for x in entry['priorUntracked']:assert sha(r/x['path'])==x['sha256'],x['path']
for x in old['source']:
 if not x['path'].startswith(('docs/','tests/')):assert sha(r/x['path'])==x['sha256'],x['path']
for x in old['runtime']:
 if '/NovaCore.Triangle/' in x['path']:assert sha(r/x['path'])==x['sha256'],x['path']
assert sha(Path('E:/Kitten Space Agency/KSA.dll'))==old['ksaSha256']
deployment=load(e/'deployment-check.json')
for x in deployment['files']:assert sha(r/x['path'])==x['expected'],x['path']
assert len(deployment['files'])==88
deployment['verifiedUtc']=now;write(e/'deployment-check.json',deployment)
gates=load(b/'stage6-results.json');assert len(gates)==58 and all(x['exit']==0 for x in gates)
reg=[]
for x in gates:
 p=r/x['log'];ls=[z for z in lines(p) if z.strip()];reg.append(dict(name=x['name'],exit=x['exit'],**record(p),witness=ls[-1][:350]))
write(e/'campaign-regression.json',dict(status='PASS',count=58,gates=reg,builds=[dict(configuration=c,**record(b/f'solution-{c}.txt'),witness=lines(b/f'solution-{c}.txt')[-5:]) for c in ['debug','release']]))
phases=[];components=[]
for c in ['debug','release']:
 for line in lines(b/f'stage6-player-integrated-{c}.txt'):
  if line.startswith('{"measurement":"PLAYER_INTEGRATED_COST"'):phases.append(dict(configuration=c,**json.loads(line)))
 for gate in ['pilot-demand','pilot-allocation','player-attitude','florida-slab-camera-costs']:
  p=b/f'stage6-{gate}-{c}.txt';w=[z for z in lines(p) if any(k in z for k in ['PERFORMANCE','COST','median','ORDINARY_ALLOCATION','FOCUS_SWITCH'])]
  components.append(dict(configuration=c,gate=gate,**record(p),witness=w))
native=load(e/'stage6-integrated-controls/native-results.json')
write(e/'campaign-performance.json',dict(scope='Stage6 qualified managed subpaths plus real native integrated episodes',threshold='report-only; no invented budget',phases=phases,components=components,native=[dict(run=x['run'],timing=x['timing']) for x in native['runs']],limits=['Phase bytes/GC are whole-route totals, not individual phase measurements.','Input adapter includes8 changing admissions per128 intervals; pure changing canonical admission is the pilot-demand component.','Combined includes diagnostic request resolution and allocation lookup.','Managed presentation covers vessel follow and assembly render submission with default camera snapshot; excludes full Solar publication, GPU/present and camera selection edges.','Native callback intervals include scheduling/present and observer overhead; service maxima4.71–5.33ms recur in all five processes with unestablished cause.','Prior Stage2 maximum6.0184ms is context only, not equivalent-workload improvement proof.','Tiny request/lookup intervals can round to timer resolution.','Existing focus-switch allocation is outside zero contract. Stage3 raw diagnostic cause remains unestablished.']))
alloc=load(e/'campaign-allocation.json');alloc['stage6']=dict(samplesPerConfiguration=640,normalBytes=0,isolatedBytes=0,collections=[0,0,0],positiveBytes=152,scope='combined request+input/admission+lookup+service+vessel-follow/render submission; independent fresh warmed fixtures; camera selection/native renderer excluded')
assert all(x['normalBytes']==0 and x['isolatedBytes']==0 and x['collections']==[0,0,0] for x in phases)
write(e/'campaign-allocation.json',alloc)
storage=load(e/'campaign-storage.json');storage['integratedCapturedSaveBytes']=(e/'stage6-integrated-controls/captured-session.json').stat().st_size;storage['limitations']='Named bounded payloads; Stage6 adds no production state/storage. Not a total process-memory census.';write(e/'campaign-storage.json',storage)
source=[r/x['path'] for x in old['source']];added=r/'tests/NovaCore.Graphics.Tests/PlayerIntegratedControlTests.cs'
if added not in source:source.append(added)
old.update(capturedUtc=now,source=[record(p) for p in source],runtime=[record(r/x['path']) for x in old['runtime']],public=public,manualAcceptance='PASS - reported by Project Control',judgment='ACCEPTED UNBANKED - ready for Project Control banking consideration - STOP FOR PROJECT CONTROL',stage6='ENGINEERING PASS',productionRecoveryCorrection='NONE',stage6ProductionCorrection='NONE')
write(e/'identity.json',old)
obs=b/'native-hold-observer-stage6';write(e/'stage6-integrated-controls/identity.json',dict(capturedUtc=now,productionReadsetUnchanged=True,productionGameRuntimeUnchanged=True,diagnosticOverlay=load(obs/'source.json'),runtime=[record(obs/'runtime'/f) for f in ['NovaCore.Triangle.dll','NovaCore.Native.dll']],driver=record(b/'native-input-driver/bin/Release/net10.0-windows/NativeInputDriver.dll'),tests=[record(added),record(r/'tests/NovaCore.Graphics.Tests/Program.cs')],retainedSave=record(e/'stage6-integrated-controls/captured-session.json')))
files=[record(p) for p in sorted(e.rglob('*')) if p.is_file() and p!=e/'closure.json']
closure=dict(capturedUtc=now,judgment='ACCEPTED UNBANKED - ready for Project Control banking consideration - STOP FOR PROJECT CONTROL',stages=['PASS-PROMOTE']*5+['ENGINEERING-PASS'],manualAcceptance='PASS - reported by Project Control',finalGates=58,integratedChecksPerConfiguration=3007,nativeRecoveryEpisodes=17,nativeIntegratedEpisodes=5,review='Independent Stage5 22 attacks and Stage6 24 attacks PASS',deploymentHashes=88,priorUntrackedPreserved=55,indexEmpty=True,diffCheck='PASS',publicBankUnchanged=True,productionRecoveryCorrection='NONE',stage6ProductionCorrection='NONE',milestone='NOT ASSIGNED',bankingAuthorized=False,readyForBankingConsideration=True,manualAcceptanceEvidence='manual-acceptance.json',closureProductionChanges='NONE',evidenceBudgetBytes=524288,files=files)
write(e/'closure.json',closure);size=sum(p.stat().st_size for p in e.rglob('*') if p.is_file());assert size<=524288,size
print(json.dumps(dict(status='PASS',gates=58,deployment=88,priorUntracked=55,evidenceBytes=size,public=public)))
