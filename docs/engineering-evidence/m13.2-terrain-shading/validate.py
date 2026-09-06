"""Reproduce final candidate validation after removing temporary instrumentation.

Uses existing Graphics validation isolation and the earlier six-route probe recipe.
Does not modify production source or assets. Keeps compact results, not full logs.
"""
import hashlib,json,os,subprocess,sys,tempfile
from pathlib import Path

ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'build/m13.2-terrain-shading'
RECORDS=OUT/'final-validation.json'

def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()

def execute(label,args,env=None):
    p=subprocess.run(args,cwd=ROOT,env=env,capture_output=True,text=True,timeout=900)
    log=p.stdout+p.stderr
    summary=[line for line in log.splitlines() if line.startswith(('PASS','FAIL','Graphics ','Native GPU ','Launcher ','Asset:','Version:','Cache:','Path:','Status:','Bytes:','SHA-256:')) or any(s in line for s in ('specialization PASS','NCSM1 fragment shading:','P2S5C3 phase:','Desktop traversal PASS','Native identity:','Shader identity:','validation PASS','seating stability: frame=440','NCSM1 regional ready:','spherical billboard publication:','Test Run','tests passed','Build succeeded','Error(s)','Warning(s)','VUID-'))]
    summary.extend(line for line in log.splitlines() if ' PASS' in line or 'Window deployment:' in line or 'Window loaded module:' in line)
    records=json.loads(RECORDS.read_text()) if RECORDS.exists() else []
    records.append(dict(label=label,arguments=args,exitCode=p.returncode,stdoutUtf8Bytes=len(log.encode()),logSha256=hashlib.sha256(log.encode()).hexdigest(),validationMessages=list(dict.fromkeys(x for x in log.splitlines() if 'Vulkan validation [' in x)),summary=list(dict.fromkeys(summary))))
    RECORDS.write_text(json.dumps(records,indent=2)+'\n',encoding='utf-8')
    print(label,'PASS' if p.returncode==0 else 'FAIL',flush=True)
    if p.returncode:
        (OUT/(label+'-failed.log')).write_text(log,encoding='utf-8')
        raise RuntimeError(label+' failed; retained log')
    return log

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
    OUT.mkdir(parents=True,exist_ok=True)
    if mode=='build':
        for config,native in [('Debug','native-ninja'),('Release','native-ninja-release')]:
            command="& 'C:/Program Files/Microsoft Visual Studio/18/Community/Common7/Tools/Launch-VsDevShell.ps1' -Arch amd64 -HostArch amd64 -SkipAutomaticLocation\ncmake --build build/"+native+"\nexit $LASTEXITCODE"
            execute('native-build-'+config,['pwsh','-NoProfile','-Command',command])
            execute('managed-build-'+config,['dotnet','build','NovaCore.sln','-c',config,'--no-restore','-v','quiet'])
    elif mode=='tests':
        for config in ('Debug','Release'):
            exe=str(ROOT/f'tests/NovaCore.Graphics.Tests/bin/{config}/net10.0/NovaCore.Graphics.Tests.exe')
            for category in ('gpu',):
                env=os.environ.copy()
                # The established artifact-input mode preserves all topology
                # assertions; a TES optimization does not require regeneration.
                env['NOVACORE_P2S5F_ARTIFACT_INPUT']=str(ROOT/'assets/planetary-nested-scale-mesh')
                execute('graphics-'+config+'-'+category,[exe,'--category='+category],env)
            for selection in ['M12D-P2S5G','Florida facility support','Single canonical physical','Generation-4 physical renderer','Earth route convergence','Production material noise value preservation','Production window lifecycle']:
                execute('focused-'+config+'-'+selection.replace(' ','-'),[exe,'--test='+selection])
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
    elif mode=='transitions':
        with tempfile.TemporaryDirectory(prefix='layers-',dir=OUT) as d:
            env=canonical(Path(d))
            env.update(NOVACORE_WINDOW_CLIENT_WIDTH='3440',NOVACORE_WINDOW_CLIENT_HEIGHT='1440',NOVACORE_WINDOW_BORDERLESS='1')
            log=execute('full-orbital-surface-transitions',[str(ROOT/'samples/NovaCore.Triangle/bin/Release/net10.0/NovaCore.Triangle.exe'),'--scene=sol','--focus=earth','--physical-surface=m12d-natural-candidate','--solar-epoch=j2000','--p2s5c3-traversal','--benchmark-frames=10000','--log=startup,validation,vulkan'],env)
            assert 'P2S5C3 Desktop traversal PASS:' in log
            assert 'NCSM1 fragment shading: ordinary specialization' in log
            assert 'VUID-' not in log
