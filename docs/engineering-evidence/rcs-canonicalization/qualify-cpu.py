"""Run the qualified CPU/regression inventory from canonical source only."""
import hashlib,json,os,pathlib,subprocess,time
root=pathlib.Path(__file__).resolve().parents[3]
evidence=root/'docs/engineering-evidence/rcs-canonicalization'
prior=root/'docs/engineering-evidence/rcs-attitude-scalability'
out=root/'build/rcs-canonicalization';out.mkdir(parents=True,exist_ok=True)
environment=os.environ.copy();environment['NOVACORE_ASSET_CACHE']=str(root/'.novacore/cache/terrain/v1')
empty=out/'empty-implicit-layers';empty.mkdir(exist_ok=True);environment['VK_IMPLICIT_LAYER_PATH']=str(empty)
runs=[]
for cfg in ['Debug','Release']:
 runs.append(dict(name=cfg+'--rcs-scalability',command=['dotnet',str(root/f'tests/NovaCore.Simulation.Tests/bin/{cfg}/net10.0/NovaCore.Simulation.Tests.dll'),'--rcs-scalability',str(out/(cfg.lower()+'-cpu.json'))]))
old='C:/Users/Tyler/.codex/worktrees/rcs-scalability/NovaCore'
seen=set()
for file in ['regressions.json','legacy-regressions.json','supplemental-regressions.json']:
 for row in json.loads((prior/file).read_text(encoding='utf-8-sig')):
  command=[x.replace('\\','/').replace(old,root.as_posix()) for x in row['command']]
  key=tuple(command)
  if key in seen:continue
  seen.add(key);runs.append(dict(name=row.get('name',pathlib.Path(command[1]).parts[-3]+command[-1]),command=command))
results=[]
for index,row in enumerate(runs):
 log=out/f'cpu-{index:02}-{row["name"]}.log';start=time.monotonic()
 with log.open('w',encoding='utf-8') as f:result=subprocess.run(row['command'],cwd=root,env=environment,stdout=f,stderr=subprocess.STDOUT)
 row.update(exit=result.returncode,seconds=time.monotonic()-start,logSha256=hashlib.sha256(log.read_bytes()).hexdigest(),tail=log.read_text(encoding='utf-8',errors='replace').splitlines()[-6:])
 results.append(row);(out/'cpu-results.json').write_text(json.dumps(results,indent=2)+'\n',encoding='utf-8')
 print(index,row['name'],result.returncode,round(row['seconds'],2),flush=True)
 if result.returncode:raise SystemExit(1)
