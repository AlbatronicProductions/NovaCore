"""Reproduce final candidate validation after removing temporary instrumentation.

Uses existing Graphics validation isolation and the earlier six-route probe recipe.
Does not modify production source or assets. Keeps compact results, not full logs.
"""
import hashlib,json,os,subprocess,sys,tempfile
from pathlib import Path

ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'build/tes-address-validation'
RECORDS=OUT/'final-validation.json'

def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()

def execute(label,args,env=None):
    p=subprocess.run(args,cwd=ROOT,env=env,capture_output=True,text=True,timeout=900)
    log=p.stdout+p.stderr
    summary=[line for line in log.splitlines() if line.startswith(('PASS','FAIL','Graphics ','Native GPU ','Launcher ','Asset:','Version:','Cache:','Path:','Status:','Bytes:','SHA-256:')) or any(s in line for s in ('TES geographic specialization PASS','Native identity:','Shader identity:','validation PASS','seating stability: frame=440','NCSM1 regional ready:','spherical billboard publication:','Test Run','tests passed','Build succeeded','Error(s)','Warning(s)','VUID-'))]
    records=json.loads(RECORDS.read_text()) if RECORDS.exists() else []
    records.append(dict(label=label,arguments=args,exitCode=p.returncode,stdoutUtf8Bytes=len(log.encode()),logSha256=hashlib.sha256(log.encode()).hexdigest(),summary=list(dict.fromkeys(summary))))
    RECORDS.write_text(json.dumps(records,indent=2)+'\n',encoding='utf-8')
    print(label,'PASS' if p.returncode==0 else 'FAIL',flush=True)
    if p.returncode:
        (OUT/(label+'-failed.log')).write_text(log,encoding='utf-8')
        raise RuntimeError(label+' failed; retained log')

def canonical(directory):
    (directory/'implicit').mkdir();(directory/'explicit').mkdir()
    manifest=Path(os.environ['VULKAN_SDK'])/'Bin/VkLayer_khronos_validation.json'
    data=json.loads(manifest.read_text());data['layer']['library_path']=str((manifest.parent/data['layer']['library_path']).resolve())
    (directory/'explicit/validation.json').write_text(json.dumps(data))
    env={k:v for k,v in os.environ.items() if not k.startswith(('NOVACORE_','VK_LAYER','VK_ADD_LAYER','VK_IMPLICIT_LAYER','VK_ADD_IMPLICIT_LAYER','VK_LOADER_LAYERS','VK_VALIDATION','VK_INSTANCE_LAYERS'))}
    env.update(VK_LAYER_PATH=str(directory/'explicit'),VK_IMPLICIT_LAYER_PATH=str(directory/'implicit'),VK_INSTANCE_LAYERS='VK_LAYER_KHRONOS_validation',VK_LAYER_SETTINGS_PATH=str(directory))
    return env

if __name__=='__main__':
    mode=sys.argv[1]
    if mode=='build':
        for config,native in [('Debug','native-ninja'),('Release','native-ninja-release')]:
            command="& 'C:/Program Files/Microsoft Visual Studio/18/Community/Common7/Tools/Launch-VsDevShell.ps1' -Arch amd64 -HostArch amd64 -SkipAutomaticLocation\ncmake --build build/"+native+"\nexit $LASTEXITCODE"
            execute('native-build-'+config,['pwsh','-NoProfile','-Command',command])
            execute('managed-build-'+config,['dotnet','build','NovaCore.sln','-c',config,'--no-restore','-v','quiet'])
    elif mode=='tests':
        for config in ('Debug','Release'):
            exe=str(ROOT/f'tests/NovaCore.Graphics.Tests/bin/{config}/net10.0/NovaCore.Graphics.Tests.exe')
            for category in ('headless','gpu'):
                env=os.environ.copy()
                # The established artifact-input mode preserves all topology
                # assertions; a TES optimization does not require regeneration.
                env['NOVACORE_P2S5F_ARTIFACT_INPUT']=str(ROOT/'assets/planetary-nested-scale-mesh')
                execute('graphics-'+config+'-'+category,[exe,'--category='+category],env)
            execute('regional-window-'+config,[exe,'--test=Live NCSM1 regional physical residency'])
            execute('native-gpu-'+config,[exe,'--native-gpu'])
        execute('launcher-Release',[str(ROOT/'tests/NovaCore.Launcher.Tests/bin/Release/net10.0-windows/NovaCore.Launcher.Tests.exe')])
        for asset in ('earth-surface-v5','earth-florida-m12'):
            execute('asset-'+asset,[str(ROOT/'tools/NovaCore.AssetTool/bin/Release/net10.0/NovaCore.AssetTool.exe'),'verify',asset])
    elif mode=='routes':
        # Exact retained Package 2 recipe; uses the deployed launcher assembly.
        source=json.loads((ROOT/'docs/engineering-evidence/graphics-validation/package-2.json').read_text())
        def find(v):
            if isinstance(v,dict):
                if 'routeSmokeRecipe' in v:return v['routeSmokeRecipe']
                for x in v.values():
                    result=find(x)
                    if result:return result
            if isinstance(v,list):
                for x in v:
                    result=find(x)
                    if result:return result
        route=OUT/'route';route.mkdir(exist_ok=True)
        recipe=find(source);assert recipe
        (route/'Program.cs').write_text(recipe.replace('const string root = "E:/NovaCore";', 'const string root = '+json.dumps(str(ROOT).replace('\\','/'))+';'),encoding='utf-8')
        assembly=ROOT/'tools/NovaCore.Launcher/bin/Release/net10.0-windows/NovaCore.Launcher.dll'
        (route/'route.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0-windows</TargetFramework><PlatformTarget>x64</PlatformTarget><UseWindowsForms>true</UseWindowsForms></PropertyGroup><ItemGroup><Reference Include="NovaCore.Launcher"><HintPath>'+str(assembly)+'</HintPath></Reference></ItemGroup></Project>')
        execute('route-probe-build',['dotnet','build',str(route/'route.csproj'),'-c','Release','-v','quiet'])
        with tempfile.TemporaryDirectory(prefix='layers-',dir=OUT) as d:
            execute('six-routes-Florida-smoke',[str(route/'bin/Release/net10.0-windows/route.exe'),'--smoke'],canonical(Path(d)))
    elif mode=='post-smoke':
        evidence=ROOT/'docs/engineering-evidence/tes-address-removal/validation.json'
        record=json.loads(evidence.read_text())
        plan=next(p for p in record['launcherRoutes'] if p['preset']=='Florida Launch Site')
        with tempfile.TemporaryDirectory(prefix='final-layers-',dir=OUT) as d:
            env=canonical(Path(d));env.update(plan['EnvironmentVariables'])
            args=[plan['FileName'],*plan['Arguments'],'--benchmark-frames=120','--log=startup,validation,vulkan']
            proc=subprocess.run(args,cwd=plan['WorkingDirectory'],env=env,capture_output=True,text=True,timeout=120)
            log=proc.stdout+proc.stderr
        assert proc.returncode==0 and 'VUID-' not in log
        assert 'NCSM1 regional ready:' in log and 'spherical billboard publication:' in log
        assert 'ordinary specialization (inverse disabled)' in log
        assert not any(x in log for x in ('zeroOwner=1','overlapOwner=1','staleGenerationDraws=1','invalidDraws=1'))
        record['postCleanupAttemptSmoke']=dict(passResult=True,frames=120,arguments=args,exitCode=proc.returncode,logSha256=hashlib.sha256(log.encode()).hexdigest(),rawUtf8Bytes=len(log.encode()),vuidCount=0,manualAcceptance=False,summary=[s for s in log.splitlines() if any(k in s for k in ('Runtime fingerprint:','TES geographic address:','NCSM1 regional ready:','spherical billboard publication:','Average frame time:'))])
        assets=[]
        for asset in ('earth-surface-v5','earth-florida-m12'):
            proc=subprocess.run([str(ROOT/'tools/NovaCore.AssetTool/bin/Release/net10.0/NovaCore.AssetTool.exe'),'verify',asset],cwd=ROOT,capture_output=True,text=True)
            assert proc.returncode==0
            assets.append(dict(asset=asset,exitCode=0,verification=proc.stdout.splitlines()))
        record['postCleanupAssets']=assets
        evidence.write_text(json.dumps(record,indent=2)+'\n',encoding='utf-8')
        print('Post-cleanup-attempt Florida smoke PASS: 120 frames, ready sole owner, zero VUID; both terrain packs verified',flush=True)
    elif mode=='identity':
        data=[]
        for config,native in [('Debug','native-ninja'),('Release','native-ninja-release')]:
            expected=ROOT/'build'/native
            for relative in (f'samples/NovaCore.Triangle/bin/{config}/net10.0',f'tests/NovaCore.Graphics.Tests/bin/{config}/net10.0'):
                deploy=ROOT/relative
                assert sha(expected/'NovaCore.Native.dll')==sha(deploy/'NovaCore.Native.dll')
                if relative.startswith('samples/'):
                    names={Path(p.replace('\\','/')).name for p in json.loads((ROOT/'docs/engineering-evidence/tes-address-removal/baseline.json').read_text())['deployedShaderHashes']}
                    assert names=={p.name for p in (deploy/'shaders').glob('*.spv')}
                    for name in names:assert sha(expected/'shaders'/name)==sha(deploy/'shaders'/name),name
                    shaderDirectory=deploy/'shaders'
                else:
                    # Graphics proof tests select their matching native build's
                    # shader directory; the test project deploys only the DLL.
                    shaderDirectory=expected/'shaders'
                record=dict(configuration=config,directory=str(deploy),nativeSha256=sha(deploy/'NovaCore.Native.dll'),shaderDirectory=str(shaderDirectory),shaders={p.name:sha(p) for p in shaderDirectory.glob('*.spv')})
                if relative.startswith('samples/'):
                    oracle=ROOT/'assets/earth/runtime/earth_elevation_8192x4096.r16'
                    assert sha(oracle)==sha(deploy/'earth-data'/oracle.name)
                    record.update(managedSha256=sha(deploy/'NovaCore.Triangle.dll'),executableSha256=sha(deploy/'NovaCore.Triangle.exe'),elevationOracleSha256=sha(oracle))
                    baseline=json.loads((ROOT/'docs/engineering-evidence/tes-address-removal/baseline.json').read_text())['deployedShaderHashes']
                    changed=[Path(k.replace('\\','/')).name for k,v in baseline.items() if record['shaders'][Path(k.replace('\\','/')).name]!=v]
                    assert changed==['production_spherical_billboard.tese.spv'],changed
                    record['changedProductionShaders']=changed
                data.append(record)
        (OUT/'deployment.json').write_text(json.dumps(data,indent=2)+'\n')
        print('Deployment identity PASS: Debug and Release, sample and Graphics tests',flush=True)
    else:raise ValueError(mode)
