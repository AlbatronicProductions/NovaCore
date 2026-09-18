"""Consolidate existing final regression output; never runs or changes a test."""
from pathlib import Path
import hashlib, json, re, subprocess
ROOT=Path(__file__).resolve().parents[3]
OUT=Path(__file__).resolve().parent
BUILD=ROOT/'build/srv01-foundation-final'
HIST=ROOT/'docs/engineering-evidence/srv01-surface-to-flight-gauntlet'
def read(p): return json.loads(p.read_text(encoding='utf-8-sig'))
def save(name,data): (OUT/name).write_text(json.dumps(data,indent=2)+'\n',encoding='utf-8')
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def lines(p): return p.read_text(encoding='utf-8-sig').splitlines()
rows=read(BUILD/'validation-results.json')
assert len(rows)==76 and all(x['exit']==0 for x in rows)
records=[]
for row in rows:
    p=ROOT/row['log']; text=lines(p)
    # Retain explicit outcomes/measurements, not bulk runtime logs or repeated poses.
    selected=[s for s in text if (s.startswith(('PASS ','FAIL ','ORDINARY_ALLOCATION','FLORIDA_','ASSEMBLY_','POWERED_','DEPARTURE_','DEVELOPMENT_','Published','Build','    0 ')) or 'tests passed' in s.lower() or 'positive control' in s.lower() or 'ALLOCATION' in s or 'STORAGE' in s) and not s.startswith(('FLORIDA_SITE_STEP','FLORIDA_PRESENTATION_READY'))]
    records.append(dict(name=row['name'],exit=row['exit'],sha256=sha(p),witness=selected))
builds=[]
for cfg in ['debug','release']:
    p=BUILD/f'full-build-{cfg}.txt';text='\n'.join(lines(p))
    assert '0 Warning(s)' in text and '0 Error(s)' in text
    builds.append(dict(configuration=cfg,sha256=sha(p),result=text.strip()))
block=(ROOT/'tests/NovaCore.Simulation.Tests/Program.cs').read_text().split('var tests = new (string Name, Action Test)[]')[1].split('\n};')[0]
groups=re.findall(r'^    \("([^"]+)"',block,re.M)
for cfg in ['debug','release']:
    assert all('PASS '+g in lines(BUILD/f'Simulation-{cfg}.txt') for g in groups)
    assert 'NovaCore launcher tests passed: 18' in lines(BUILD/f'Launcher-{cfg}.txt')
save('regression.json',dict(builds=builds,simulationRegisteredGroups=len(groups),launcherRegisteredGroups=18,executedGates=len(records),allPass=True,gates=records,
    preRegressionBuilds='Initial Debug and status-only Debug/Release compiles also passed; final builds followed removal of the unused oversized-contact factory. No failed runtime gate or retry.'))
def select(names, pattern):
    return [dict(gate=r['name'],witness=[s for s in r['witness'] if re.search(pattern,s)]) for r in records if any(n in r['name'] for n in names)]
save('final-physical-results.json',dict(scope='Final source, Debug and Release; stock Florida and preserved earlier bounded domains kept distinct.',
    stages=select(['Simulation-','florida-slab-correctness-','florida-slab-solar-'],r'PHYSICAL|THRUST|DEPARTURE_FIRST|DEPARTURE_POSITION|DEPARTURE_FRAME|DEVELOPMENT_CHEAP|FLORIDA_SLAB_|FLORIDA_STOCK_IDENTITY|FLORIDA_INERTIAL|FLORIDA_SOLAR')))
save('determinism.json',dict(domain='Same build/machine, matching canonical frontiers; no cross-platform bit claim.',
    witnesses=select(['Simulation-','florida-slab-correctness-','florida-slab-solar-'],r'SCHEDULE|DETERMINISM|PRIVATE_NATIVE|INERTIAL_ORACLE|FLORIDA_SOLAR')))
save('allocation.json',dict(contract='Exactly zero warmed managed bytes; checked no-GC entry/exit; deliberate allocation positive control remains independent.',
    witnesses=select(['Simulation-','allocation-','presentation-'],r'ALLOCATION|positive control')))
save('storage.json',dict(limitBytes=8388608,method='Final compiled candidate. Florida presentation scene advances 1200 intervals and builds submissions before GC-retained/native/GPU accounting. Construction upper bounds for other owners are labelled, not called exact retained managed sizes.',
    exclusions='Shared terrain datasets, common renderer/device/driver resources, total process working set.',
    witnesses=select(['storage-'],r'STORAGE')))
entry=read(OUT/'entry-source-seals.json');final=[];changes=[]
for x in entry:
    p=ROOT/x['path'];current=dict(path=x['path'],bytes=p.stat().st_size,sha256=sha(p));final.append(current)
    if current['sha256']!=x['sha256']:changes.append(dict(path=x['path'],before=x['sha256'],after=current['sha256']))
assert {x['path'] for x in changes}=={'samples/NovaCore.Triangle/Program.cs','samples/NovaCore.Triangle/StockAssemblyDevelopmentScene.cs','src/NovaCore.Simulation/Spacecraft/Assemblies/AssemblyContactProfile.cs'},changes
save('identity.json',dict(baseline='ab14e3e0f8b53bf5e8bf580a99aea3f2b6fd21f5',branch='codex/srv01-supported-contact-admission',inputCount=len(final),closeoutSourceChanges=changes,files=final))
hist=read(HIST/'stage5/simplified-slab/results.json')
perf=[]
j=read(HIST/'stage1/drift-closure/performance.json')
for i in range(1,4):perf.append(dict(stage=1,process=i,population='supported contact',result=j[f'candidate{i}']))
for x in read(HIST/'stage2/closure/measurements.json')['isolated']:perf.append(dict(stage=2,process=x['run'],population='powered support',result=x['data']))
for stage,f in [(3,'stage3/allocation-closure/measurements.json'),(4,'stage4/measurements.json')]:
    for process in read(HIST/f)['performance']:
        for x in process['rows']:perf.append(dict(stage=stage,process=process['process'],population=x['population'],result=x))
for x in hist['performance']:perf.append(dict(stage=5,process=x['run'],population='stock Florida slab',result=x['result']))
save('performance.json',dict(reliedOnRetainedEvidence=True,newTimingCampaign=False,
    equivalence='Only three closeout source edits: status strings and removal of an uncalled rejected-profile factory. Stock computations, tested workloads, solver policy and numerical order are unchanged. Historical checkpoint timings remain labelled, never recast as fresh timing of the new assembly image.',
    operations=perf,finalSlabLive=hist['live']))
print('Consolidated',len(records),'gates;',len(final),'source/input seals;',len(changes),'bounded closeout source edits')
