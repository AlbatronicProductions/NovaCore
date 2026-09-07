"""Bounded normal-deployment regression; keeps summaries and hashes, not raw logs."""
import hashlib,json,os,pathlib,subprocess,sys
sys.dont_write_bytecode=True
from prepare import ROOT,HERE,OUT,sha
def execute(label,args,env=None,timeout=300):
    result=subprocess.run(list(map(str,args)),cwd=ROOT,env=env,capture_output=True,text=True,timeout=timeout)
    log=result.stdout+result.stderr
    record={'label':label,'args':list(map(str,args)),'exitCode':result.returncode,'logBytes':len(log.encode()),'logSha256':hashlib.sha256(log.encode()).hexdigest(),
        'lines':[x for x in log.splitlines() if x.startswith(('PASS','FAIL','Graphics ','Native GPU','Launcher ','Asset:','Status:','Path:','SHA-256:')) or any(t in x for t in [' PASS','Native identity:','Shader identity:','VUID-','Window deployment:','Window loaded module:','timing:','regional ready:','regional physical totals:','spherical billboard publication:','seating stability: frame=440','fragment shading:','P2S5C3 phase:','P2S5C3 finest snap:','P2S5E'])]}
    record['lines'] += [x for x in log.splitlines() if any(t in x for t in ['Average frame time:','Frame pacing:','Fence wait pacing:','CPU timings:','altitude checkpoint:','Desktop traversal PASS:','GPU workload:','Runtime fingerprint:','Canonical SurfaceAnchor terrain:']) and x not in record['lines']]
    record['validationErrors']=[x for x in log.splitlines() if 'VUID-' in x or 'Validation Error' in x]
    target=HERE/'validation.json';records=json.loads(target.read_text()) if target.exists() else [];records.append(record);target.write_text(json.dumps(records,indent=2)+'\n',encoding='utf-8')
    print(label,'PASS' if result.returncode==0 else 'FAIL',flush=True)
    if result.returncode:
        (OUT/(label+'-failed.log')).write_text(log,encoding='utf-8');raise RuntimeError(label)
    return log
def canonical():
    directory=OUT/'layers';(directory/'implicit').mkdir(parents=True,exist_ok=True);(directory/'explicit').mkdir(exist_ok=True)
    manifest=pathlib.Path(os.environ['VULKAN_SDK'])/'Bin/VkLayer_khronos_validation.json';data=json.loads(manifest.read_text());data['layer']['library_path']=str((manifest.parent/data['layer']['library_path']).resolve());(directory/'explicit/validation.json').write_text(json.dumps(data))
    env={k:v for k,v in os.environ.items() if not k.startswith(('NOVACORE_','VK_LAYER','VK_ADD_LAYER','VK_IMPLICIT_LAYER','VK_ADD_IMPLICIT_LAYER','VK_LOADER_LAYERS','VK_VALIDATION','VK_INSTANCE_LAYERS'))}
    env.update(VK_LAYER_PATH=str(directory/'explicit'),VK_IMPLICIT_LAYER_PATH=str(directory/'implicit'),VK_LAYER_SETTINGS_PATH=str(directory),VK_INSTANCE_LAYERS='VK_LAYER_KHRONOS_validation')
    return env
def main(mode):
    if mode in ('tests','resume-tests'):
        for configuration,folder in [('Debug','native-ninja'),('Release','native-ninja-release')]:
            env=canonical();env['NOVACORE_P2S5F_ARTIFACT_INPUT']=str(ROOT/'assets/planetary-nested-scale-mesh')
            exe=ROOT/f'tests/NovaCore.Graphics.Tests/bin/{configuration}/net10.0/NovaCore.Graphics.Tests.exe'
            if mode!='resume-tests' or configuration!='Debug':
                for category in ['headless','gpu','window']:execute(configuration+'-'+category,[exe,'--category='+category],env,900)
                execute(configuration+'-native-gpu',[exe,'--native-gpu'],env)
            # The two Vulkan executables above already receive their shader paths
            # through --native-gpu. Only the residency executable is a CPU test.
            pack=ROOT/'.novacore/cache/terrain/v1/sha256/c4/c45c6d94e004e1a2927dc65d405a347b1800c619b22b2eb6b3543f3c445d3afe.nccube'
            execute(configuration+'-regional-cpu',[ROOT/'build'/folder/'NovaCoreRegionalPhysicalTests.exe',pack],env)
        execute('launcher-Release',[ROOT/'tests/NovaCore.Launcher.Tests/bin/Release/net10.0-windows/NovaCore.Launcher.Tests.exe'])
        for asset in ['earth-surface-v5','earth-florida-m12']:execute('asset-'+asset,[ROOT/'tools/NovaCore.AssetTool/bin/Release/net10.0/NovaCore.AssetTool.exe','verify',asset])
    elif mode=='dynamic':
        env=canonical();env.update(NOVACORE_WINDOW_CLIENT_WIDTH='3440',NOVACORE_WINDOW_CLIENT_HEIGHT='1440',NOVACORE_WINDOW_BORDERLESS='1')
        args=[ROOT/'samples/NovaCore.Triangle/bin/Release/net10.0/NovaCore.Triangle.exe','--scene=sol','--focus=earth','--physical-surface=m12d-natural-candidate','--solar-epoch=j2000','--p2s5c3-traversal','--benchmark-frames=20000','--log=startup,validation,vulkan']
        log=execute('normal-full-transitions',args,env,360)
        assert 'P2S5C3 Desktop traversal PASS:' in log and 'VUID-' not in log
        env['NOVACORE_P2S5E_WARP_ONLY']='1'
        log=execute('normal-warp',args,env,180);assert 'P2S5E anchored warp diagnostic PASS:' in log and 'VUID-' not in log
    elif mode=='routes':
        source=json.loads((HERE.parent/'graphics-validation/package-2.json').read_text())
        def find(value):
            if isinstance(value,dict):
                if 'routeSmokeRecipe' in value:return value['routeSmokeRecipe']
                for item in value.values():
                    result=find(item)
                    if result:return result
            if isinstance(value,list):
                for item in value:
                    result=find(item)
                    if result:return result
        recipe=find(source);assert recipe
        path=OUT/'route';path.mkdir(parents=True,exist_ok=True);(path/'Program.cs').write_text(recipe,encoding='utf-8')
        assembly=ROOT/'tools/NovaCore.Launcher/bin/Release/net10.0-windows/NovaCore.Launcher.dll'
        (path/'route.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0-windows</TargetFramework><PlatformTarget>x64</PlatformTarget><UseWindowsForms>true</UseWindowsForms></PropertyGroup><ItemGroup><Reference Include="NovaCore.Launcher"><HintPath>'+str(assembly)+'</HintPath></Reference></ItemGroup></Project>')
        execute('route-probe-build',['dotnet','build',path/'route.csproj','-c','Release','-v','quiet'])
        execute('six-routes-and-Florida-smoke',[path/'bin/Release/net10.0-windows/route.exe','--smoke'],canonical())
if __name__=='__main__':main(sys.argv[1])
