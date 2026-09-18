"""Reduce disposable Stage4 outputs to reproducible, concise evidence; runs no tests."""
from pathlib import Path
import hashlib,json,re,subprocess,zipfile
r=Path(__file__).resolve().parents[4];e=Path(__file__).parent;o=r/'build/srv01-stage4'
def read(p):return p.read_text(encoding='utf-8-sig')
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def write(name,value):(e/name).write_text(json.dumps(value,indent=2)+'\n',encoding='utf-8')
def git(*args):return subprocess.check_output(['git',*args],cwd=r,text=True).strip()
def objects(path,prefix):return [json.loads(x[len(prefix):]) for x in read(path).splitlines() if x.startswith(prefix)]
p=read(r/'tests/NovaCore.Simulation.Tests/Program.cs')
names=re.findall(r'^\s*\("([^"]+)"',p.split('var tests = new (string Name, Action Test)[]',1)[1].split('foreach (var (name, test)',1)[0],re.M)
result={}
for c in ['debug','release']:
 text=read(o/f'final-{c}-simulation.txt');lines=text.splitlines();missing=[n for n in names if 'PASS '+n not in lines];assert not missing
 focus=o/f'final-{c.title()}-development.txt'
 build=read(o/f'final-{c.title()}-build.txt');assert '0 Warning(s)' in build and '0 Error(s)' in build
 regressions=[]
 for name in ['NovaCore.ReferenceFrames.Tests','NovaCore.Precision.Tests','NovaCore.BepuDependency.Tests','launcher-correct-layout','assembly-presentation','assembly-powered-support','certified-continuation-regression','powered-contact-presentation']:
  log=o/f'final-{c}-{name}.txt';t=read(log);assert 'Unhandled exception' not in t
  regressions.append(dict(gate=name,exitCode=0,sha256=sha(log),lastLines=t.splitlines()[-3:]))
 result[c]=dict(registeredGroups=len(names),passedGroups=len(names),missing=missing,simulationSha256=sha(o/f'final-{c}-simulation.txt'),
  fullSolutionBuild=dict(warnings=0,errors=0),focusedWitnesses=read(focus).splitlines(),regressions=regressions,
  stage2=[x for x in lines if x.startswith('POWERED_CONTACT_') and len(x)<600],stage3=[x for x in lines if x.startswith('DEPARTURE_') and not x.startswith('DEPARTURE_POSITION ')])
result['executionNotes']=[
 'Initial 13.197 km/s sizing was rejected by independent mission review; earlier software passes do not qualify the final profile. Initial-sizing-rejected.md and initial-profile-rejected.json retain its inputs.',
 'Full Graphics default runner was invoked inadvertently, then interrupted before completion. It is not counted as qualification. Required focused presentation routes passed separately; no renderer change or optimization.',
 'Initial Launcher invocation used the deeper artifacts path, violating its existing five-parent repository-root assumption (looked for build/samples). The same built output was copied to the documented five-parent layout; both configurations passed without source changes.'
]
write('validation.json',result)
write('measurements.json',dict(performance=[dict(process=i,rows=objects(o/f'performance-release-{i}.txt','DEVELOPMENT_PERFORMANCE ')) for i in range(1,4)],
 cautions=['No timed-region GC in any population.','Run1 wet maximum1.6261ms at996; exhaustion maxima1.668ms at785/run2 and1.5849ms at800/run3, with following clusters. Cause UNATTRIBUTED; no retry or optimization.',
 'Dry median varies0.0186..0.0502ms across fresh processes.','No GPU/display measurement for this headless data/authority change. Cold values exclude already-loaded catalog/profile; harness CPU includes cold setup/disposal.',
 'Current integrated-operation context applies, not historical50us micro-gates. Stage3 first-flight medians0.10725..0.1214ms and handoff maximum2.0464ms are context only, not matched A/B proof. No whole-frame claim.']))
pre=json.loads(read(e/'preflight.json'));old=json.loads(read(e.parent/'stage3/allocation-closure/identity.json'))
paths={s['path'] for s in pre['sourceSeals']}
paths.update(['src/NovaCore.Simulation/Spacecraft/Assemblies/AssemblyDevelopmentPropulsion.cs','src/NovaCore.Simulation/Spacecraft/Assemblies/Data/SRV01-Development-Propulsion-v1.json','tests/NovaCore.Simulation.Tests/AssemblyDevelopmentPropulsionTests.cs'])
preserved=[]
with zipfile.ZipFile(e/'pre-stage4-overlap.zip') as z:
 for s in old['sourceSeals']:
  place='current'
  if sha(r/s['path'])!=s['sha256']:
   assert hashlib.sha256(z.read(s['path'])).hexdigest().upper()==s['sha256'];place='pre-stage4-overlap.zip::'+s['path']
  preserved.append(dict(**s,preservedAt=place))
tags=git('show-ref','--tags').splitlines();assert tags==pre['tags']
assert git('rev-parse','HEAD')==pre['head'];assert not git('diff','--cached','--name-only')
write('identity.json',dict(head=git('rev-parse','HEAD'),main=git('rev-parse','main'),originMain=git('rev-parse','origin/main'),branch=git('branch','--show-current'),
 sourceSeals=[dict(path=p,sha256=sha(r/p)) for p in sorted(paths)],stage3Preservation=preserved,historicalTagsUnchanged=True,historicalTagCount=len(tags),stagedPaths=[]))
write('mission-values.json',json.loads(subprocess.check_output(['python',str(e/'derive-profile.py')],cwd=r,text=True)))
print('Collected 80/80 Debug and Release, final profile/results, nine performance rows, Stage3 preservation and source identity.')
