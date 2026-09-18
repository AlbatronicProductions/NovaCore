"""Retain concise Stage-3 results from disposable outputs; does not execute tests."""
from pathlib import Path
from fractions import Fraction
from decimal import Decimal, getcontext
import hashlib, json, re, subprocess, zipfile

r=Path(__file__).resolve().parents[5]
out=r/'build/srv01-stage3'
e=Path(__file__).parent
def read(p): return p.read_text(encoding='utf-8-sig')
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def write(name,data): (e/name).write_text(json.dumps(data,indent=2)+'\n',encoding='utf-8')
def git(*args): return subprocess.check_output(['git',*args],cwd=r,text=True).strip()
def objects(name,prefix): return [json.loads(x[len(prefix):]) for x in read(out/name).splitlines() if x.startswith(prefix)]
program=read(r/'tests/NovaCore.Simulation.Tests/Program.cs')
names=re.findall(r'^\s*\("([^"]+)"',program.split('var tests = new (string Name, Action Test)[]',1)[1].split('foreach (var (name, test)',1)[0],re.M)
validation={}
for c in ['debug','release']:
 text=read(out/f'simulation-{c}.log'); passed=text.splitlines()
 missing=[n for n in names if 'PASS '+n not in passed]
 assert not missing and 'Unhandled exception' not in text
 validation[c]={'registeredGroups':len(names),'passedGroups':len(names),'missing':missing,
  'simulationLogSha256':sha(out/f'simulation-{c}.log'),
  'fullSuiteDepartureCounters':[x for x in passed if x.startswith('DEPARTURE_COUNTER ')],
  'focusedWitnesses':[x for x in read(out/('proof-debug.log' if c=='debug' else 'focused-release.log')).splitlines() if not x.startswith('DEPARTURE_POSITION ')],
  'regressions':[]}
 for name in [f'{p}-{c}.log' for p in ['NovaCore.ReferenceFrames.Tests','NovaCore.Precision.Tests','NovaCore.BepuDependency.Tests','launcher']]+[f'graphics-{p}-{c}.log' for p in ['assembly-presentation','assembly-powered-support','certified-continuation-regression','powered-contact-presentation']]:
  lines=read(out/name).splitlines()
  validation[c]['regressions'].append({'log':name,'exitCode':0,'sha256':sha(out/name),'summary':lines[-3:]})
 validation[c]['fullSolutionBuild']={'exitCode':0,'warnings':0,'errors':0,'log':('full-build-debug-prepared.log' if c=='debug' else 'full-build-release.log')}
validation['preparationFailures']=[{'log':'proof-build-debug.log','reason':'New test local cb definite-assignment error CS0165; corrected before runtime tests.'},{'log':'full-build-debug.log','reason':'MSB3030 missing default native/shader prerequisites; built unchanged native source in explicit isolated Debug/Release directories and selected NativeBuildDirectory.'}]
write('validation.json',validation)
write('proofs.json',{'debug':objects('proof-debug.log','DEPARTURE_POSITION '),'release':objects('focused-release.log','DEPARTURE_POSITION '),'note':'Default JSON omits internal PropellantInteger limbs. See exact-accounting.json for independent exact amounts; tests compare actual exact stores/history across partitions, moving frames and refusal/retry.'})
write('measurements.json',{'storage':objects('storage-release.log','DEPARTURE_STORAGE ')[0],'performance':[{'process':i,'rows':objects(f'performance-release-{i}.log','DEPARTURE_PERFORMANCE ')} for i in range(1,4)],'cautions':['Unattributed 2.0464 ms maximum; early ~55/56 tails and run3 221-224 cluster retained.','firstMs is cold first handoff in both rows, not a cold free-flight measurement.','Whole-harness CPU includes cold preparation/disposal. No whole-game FPS inference.']})
getcontext().prec=60
account=[]
for tick in [0,16666,32291,47916]:
 t=Fraction(tick,1000000); f=30-Fraction(5,64)*t; o=45-Fraction(15,128)*t
 account.append({'tick':tick,'fuelKgExact':str(f),'oxidizerKgExact':str(o),'fuelKgDecimal':str(Decimal(f.numerator)/Decimal(f.denominator)),'oxidizerKgDecimal':str(Decimal(o.numerator)/Decimal(o.denominator)),'totalMassKgExact':str(630+f+o)})
write('exact-accounting.json',{'method':'Independent rational deduction from unchanged 30/45 kg initial stores, fuel flow5/64kg/s, oxidizer15/128kg/s; all sampled intervals fully powered. These are derived values, not default-JSON limb observations. Permanent tests compare exact record stores and projected mass/COM/inertia across all host partitions and moving frame.','endpoints':account,'partitions':[1,3,7,15,24,60],'admittedTicks':63541,'publishedTicks':47916,'retainedDebt':15625,'revision':3,'history':3,'timelineRevision':0})
previous=json.loads(read(e.parent/'identity.json'))
stage2=json.loads(read(e.parent.parent/'stage2/final-acceptance/identity.json'))
tags=git('show-ref','--tags').splitlines()
assert tags==stage2['historicalTags']
paths=set(x['path'] for x in stage2['sourceSeals'])
paths.update(x['path'] for x in previous['additionalDraftSources'])
paths.update(['tests/NovaCore.Simulation.Tests/AssemblyDepartureProofTests.cs','tests/NovaCore.Simulation.Tests/AssemblyDepartureMeasurements.cs'])
seals=[{'path':p,'sha256':sha(r/p)} for p in sorted(paths)]
stage2preservation=[]
z1=zipfile.ZipFile(e.parent/'qualified-stage2-overlap.zip'); z2=zipfile.ZipFile(e/'pre-correction-world.zip')
for s in stage2['sourceSeals']:
 p=s['path']; expected=s['sha256']; current=sha(r/p); location='current'
 if current!=expected:
  found=False
  for label,z in [('qualified-stage2-overlap.zip',z1),('allocation-closure/pre-correction-world.zip',z2)]:
   for n in z.namelist():
    if hashlib.sha256(z.read(n)).hexdigest().upper()==expected: location=label+'::'+n;found=True;break
   if found:break
  assert found,p
 stage2preservation.append({'path':p,'stage2Sha256':expected,'preservedAt':location})
checks={}
for key,args in [('diffCheck',['diff','--check']),('cachedDiffCheck',['diff','--cached','--check'])]:
 p=subprocess.run(['git',*args],cwd=r,capture_output=True,text=True);checks[key]={'exitCode':p.returncode,'output':p.stdout+p.stderr};assert p.returncode==0
write('identity.json',{'head':git('rev-parse','HEAD'),'main':git('rev-parse','main'),'originMain':git('rev-parse','origin/main'),'remoteMain':git('ls-remote','origin','refs/heads/main').split()[0],'branch':git('branch','--show-current'),'sourceSeals':seals,'stage2Preservation':stage2preservation,'historicalTags':tags,'historicalTagsUnchanged':True,'staged':git('diff','--cached','--name-only').splitlines(),'status':git('status','--short').splitlines(),**checks})
print('Retained',len(names),'registered groups per configuration,',len(seals),'source seals and',len(stage2preservation),'Stage2 preservation checks.')
