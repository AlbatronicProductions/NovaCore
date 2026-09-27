import json,pathlib,subprocess,hashlib,sys
root=pathlib.Path.cwd();out=root/'build/stabilization-bank-gauntlet/regressions';out.mkdir(parents=True,exist_ok=True)
results=json.loads((out/'results.json').read_text()) if '--resume' in sys.argv and (out/'results.json').exists() else []
def run(name,args):
    path=out/(name+'.log')
    if any(x['name']==name and x['command']==args and x['exit']==0 and path.exists() and x['sha256']==hashlib.sha256(path.read_bytes()).hexdigest() for x in results):
        print(name,'retained PASS',flush=True);return
    with path.open('w') as f:r=subprocess.run(args,stdout=f,stderr=subprocess.STDOUT)
    lines=path.read_text(errors='replace').splitlines()
    results.append(dict(name=name,command=args,exit=r.returncode,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),summary=lines[-10:]))
    (out/'results.json').write_text(json.dumps(results,indent=2))
    print(name, 'PASS' if r.returncode==0 else 'FAIL',flush=True)
    if r.returncode:print('\n'.join(lines[-20:]));sys.exit(r.returncode)
for c,b in [('Debug','native-ninja'),('Release','native-ninja-release')]:
    for n in ['PreparedSubmissionTests','RegionalPupilLifetimeTests','TerrainTessellationTests','PresentationResultTests']:run(c+'-'+n,[str(root/'build'/b/('NovaCore'+n+'.exe'))])
    run(c+'-startup-guard',[str(root/'build'/b/'NovaCoreCausalRecorderMock.exe'),'--test-startup-guard'])
    run(c+'-observer-build',['dotnet','build','tools/NovaCore.Causal.Observer/NovaCore.Causal.Observer.csproj','-c',c])
    for flag in ['--test-production-frame','--test-adaptive','--test-first-alarm']:
        run(c+flag,['dotnet',str(root/'tools/NovaCore.Causal.Observer/bin'/c/'net10.0-windows/NovaCore.Causal.Observer.dll'),flag,str(out/(c+flag+'-output'))])
    for flag in ['--modular-connectors','--modular-stabilization']:run(c+flag,['dotnet',str(root/'tests/NovaCore.Graphics.Tests/bin'/c/'net10.0/NovaCore.Graphics.Tests.dll'),flag])
flags=['--modular-gate1','--modular-gate1-redteam','--modular-gate1-closure','--modular-gate1-admission-audit','--modular-gate1-numerics','--modular-gate1-json','--modular-gate1-aggregate','--modular-gate1-size','--modular-gate1-predicates','--modular-gate2','--modular-gate3','--modular-gate4','--modular-gate5-operations','--modular-gate6','--modular-gate7','--modular-gate8','--modular-gate8-oracle']
for flag in flags:run(flag[2:],['dotnet','tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll',flag])
run('srv01-integration',['dotnet','tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll','--srv01-integration'])
for flag in ['--modular-greybox-assets','--modular-viewport','--modular-gate9','--modular-gate10-dynamics','--modular-gate10-flight','--modular-gate11','--modular-gate12-application','--modular-editor-regressions']:
    run(flag[2:],['dotnet','tests/NovaCore.Graphics.Tests/bin/Release/net10.0/NovaCore.Graphics.Tests.dll',flag])
for name in ['Simulation','ReferenceFrames','Camera','Launcher','Precision']:
    framework='net10.0-windows' if name=='Launcher' else 'net10.0'
    run(name,['dotnet',f'tests/NovaCore.{name}.Tests/bin/Release/{framework}/NovaCore.{name}.Tests.dll'])
run('app-diagnostics-build',['dotnet','build','tests/NovaCore.App.Diagnostics.Tests/NovaCore.App.Diagnostics.Tests.csproj','-c','Release'])
run('app-diagnostics',['dotnet','tests/NovaCore.App.Diagnostics.Tests/bin/Release/net10.0/NovaCore.App.Diagnostics.Tests.dll',str(out/'diagnostic-output')])
