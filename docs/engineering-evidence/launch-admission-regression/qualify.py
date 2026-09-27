"""Bounded launch-readiness qualification. Native routes run sequentially."""
import argparse,hashlib,json,os,pathlib,subprocess,time
p=argparse.ArgumentParser();p.add_argument('--native',action='store_true');p.add_argument('--output',type=pathlib.Path,required=True);a=p.parse_args()
root=pathlib.Path(__file__).resolve().parents[3];out=a.output.resolve();out.mkdir(parents=True,exist_ok=True)
env=os.environ.copy();layers=out/'empty-implicit-layers';layers.mkdir(exist_ok=True);env['VK_IMPLICIT_LAYER_PATH']=str(layers)
runs=[]
if a.native:
    for name,tank in [('short','nc.tank.short-2'),('long','nc.tank.long-2'),('two-short','deep'),('construction','stabilization')]:
        result=out/name/'result.json';result.parent.mkdir(parents=True,exist_ok=True)
        runs.append((name,[str(root/'tools/NovaCore.App/bin/Release/net10.0-windows/NovaCore.exe'),'--qualify-editor',str(result),'--qualification-tank',tank],result))
else:
    for r in json.loads((root/'docs/engineering-evidence/florida-pad-authority/regressions.json').read_text()):
        command=[s.replace('E:\\NovaCore',str(root)) for s in r['command']]
        command=[s.replace('\\shaders\\facility_visibility_test','\\test-shaders\\facility_visibility_test') for s in command]
        runs.append((r['name'],command,None))
    for cfg in ['Debug','Release']:
        for flag in ['--launch-readiness','--modular-stabilization','--florida-pad-flight-envelope']:
            runs.append((cfg+flag,['dotnet',str(root/f'tests/NovaCore.Graphics.Tests/bin/{cfg}/net10.0/NovaCore.Graphics.Tests.dll'),flag],None))
        for flag in ['--modular-gate7','--modular-gate3','--modular-gate6']:
            runs.append((cfg+flag,['dotnet',str(root/f'tests/NovaCore.Simulation.Tests/bin/{cfg}/net10.0/NovaCore.Simulation.Tests.dll'),flag],None))
summary=[]
for name,command,result in runs:
    log=out/(name+'.log');start=time.monotonic()
    with log.open('w',encoding='utf-8') as f:
        run=subprocess.run(command,cwd=root,env=env,stdout=f,stderr=subprocess.STDOUT)
    lines=log.read_text(encoding='utf-8',errors='replace').splitlines()
    row={'name':name,'command':command,'exit':run.returncode,'seconds':time.monotonic()-start,'logSha256':hashlib.sha256(log.read_bytes()).hexdigest(),'tail':lines[-8:]}
    if result:row['result']=json.loads(result.read_text()) if result.exists() else {'judgment':'MISSING'}
    summary.append(row);(out/'results.json').write_text(json.dumps(summary,indent=2)+'\n')
    print(name,'PASS' if run.returncode==0 else 'FAIL',round(row['seconds'],2),'seconds',flush=True)
    if run.returncode or result and row['result'].get('judgment') in ('FAIL','MISSING'):raise SystemExit(1)
