"""Normal candidate deployment and supported regression surface, lead-run only."""
import importlib.util,json,pathlib,subprocess,sys,time
import exit as whole
ROOT,HERE,OUT=whole.ROOT,whole.HERE,whole.OUT
prior=HERE.parent/'post-m13.3-next-target/validate.py'
spec=importlib.util.spec_from_file_location('prior_validation',prior)
v=importlib.util.module_from_spec(spec);spec.loader.exec_module(v)
v.HERE=HERE;v.OUT=OUT
original_execute=v.execute
def execute(*args,**kwargs):
    start=time.perf_counter()
    try:return original_execute(*args,**kwargs)
    finally:
        path=HERE/'validation.json';records=json.loads(path.read_bytes())
        if records and records[-1]['label']==args[0]:
            records[-1]['wallSeconds']=time.perf_counter()-start
            path.write_text(json.dumps(records,indent=2)+'\n')
v.execute=execute
def builds():
    for configuration,folder in [('Debug','native-ninja'),('Release','native-ninja-release')]:
        command="& 'C:/Program Files/Microsoft Visual Studio/18/Community/Common7/Tools/Launch-VsDevShell.ps1' -Arch amd64 -HostArch amd64 -SkipAutomaticLocation\ncmake --build build/"+folder+" --target NovaCore.Native NovaCoreMappedBufferMemoryTests NovaCoreRegionalPhysicalTests NovaCoreFacilityVisibilityTests NovaCoreSurfaceMaterialCoordinatesTests --parallel 4\nexit $LASTEXITCODE"
        v.execute(configuration+'-native-build',['pwsh','-NoProfile','-Command',command])
        v.execute(configuration+'-managed-build',['dotnet','build','NovaCore.sln','-c',configuration,'--no-restore','-v','quiet'],timeout=600)
        v.execute(configuration+'-mapped-memory-policy',[ROOT/'build'/folder/'NovaCoreMappedBufferMemoryTests.exe'])
    deployment()
def deployment():
    data=whole.assess.verify_deployment();baseline=json.loads((HERE/'baseline.json').read_bytes())
    assert data['assets']==baseline['assets']
    for config in ['Debug','Release']:
        assert data['deployment'][config]['shaders']==baseline['deployment'][config]['shaders']
    label='candidate-deployment-final' if (HERE/'candidate-deployment-revised.json').exists() else ('candidate-deployment-revised' if (HERE/'candidate-deployment.json').exists() else 'candidate-deployment')
    whole.assess.write(label,data)

if __name__=='__main__':
    command=sys.argv[1]
    path=HERE/'validation.json';start=len(json.loads(path.read_bytes())) if path.exists() else 0
    if command=='builds':builds()
    elif command=='deployment':deployment()
    elif command=='restore-normal':
        shell="& 'C:/Program Files/Microsoft Visual Studio/18/Community/Common7/Tools/Launch-VsDevShell.ps1' -Arch amd64 -HostArch amd64 -SkipAutomaticLocation\ncmake --build build/native-ninja-release --target NovaCore.Native --parallel 4\nexit $LASTEXITCODE"
        v.execute('Release-normal-intermediate-rebuild',['pwsh','-NoProfile','-Command',shell])
        v.execute('Release-normal-redeploy',['dotnet','build','samples/NovaCore.Triangle/NovaCore.Triangle.csproj','-c','Release','--no-restore','-v','quiet'])
        deployment()
    else:v.main(command)
    records=json.loads((HERE/'validation.json').read_bytes())
    assert all(r['exitCode']==0 and not r['validationErrors'] for r in records[start:])

