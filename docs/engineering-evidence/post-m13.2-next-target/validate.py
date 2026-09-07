"""Final validation of restored banked production, with bounded result retention."""
import sys,json,os,tempfile,importlib.util,hashlib,subprocess
sys.dont_write_bytecode=True
from pathlib import Path
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[2];OUT=ROOT/'build/post-m13.2-next-target'
spec=importlib.util.spec_from_file_location('banked_validation',HERE.parent/'m13.2-terrain-shading/validate.py')
v=importlib.util.module_from_spec(spec);spec.loader.exec_module(v)
v.OUT=OUT;v.RECORDS=OUT/'final-validation.json'
if __name__=='__main__':
    if sys.argv[1]=='build':
        v.execute('Release-native-build',['pwsh','-NoProfile','-Command',"& 'C:/Program Files/Microsoft Visual Studio/18/Community/Common7/Tools/Launch-VsDevShell.ps1' -Arch amd64 -HostArch amd64 -SkipAutomaticLocation\ncmake --build build/native-ninja-release\nexit $LASTEXITCODE"])
        for p in ['samples/NovaCore.Triangle/NovaCore.Triangle.csproj','tests/NovaCore.Graphics.Tests/NovaCore.Graphics.Tests.csproj']:
            v.execute('Release-build-'+Path(p).stem,['dotnet','build',p,'-c','Release','--no-restore','-v','quiet'])
    elif sys.argv[1]=='tests':
        exe=str(ROOT/'tests/NovaCore.Graphics.Tests/bin/Release/net10.0/NovaCore.Graphics.Tests.exe')
        env=os.environ.copy();env['NOVACORE_P2S5F_ARTIFACT_INPUT']=str(ROOT/'assets/planetary-nested-scale-mesh')
        v.execute('Graphics-GPU-Release',[exe,'--category=gpu'],env)
        for name in ['M12D-P2S5G','Florida facility support','Single canonical physical','Generation-4 physical renderer','Earth route convergence','Production material noise value preservation','Production window lifecycle']:
            v.execute('focused-'+name,[exe,'--test='+name],env)
        v.execute('launcher-Release',[str(ROOT/'tests/NovaCore.Launcher.Tests/bin/Release/net10.0-windows/NovaCore.Launcher.Tests.exe')])
        for asset in ['earth-surface-v5','earth-florida-m12']:
            v.execute('asset-'+asset,[str(ROOT/'tools/NovaCore.AssetTool/bin/Release/net10.0/NovaCore.AssetTool.exe'),'verify',asset])
        v.execute('diff-check',['git','diff','--check'])
    elif sys.argv[1]=='traversal':
        with tempfile.TemporaryDirectory(prefix='layers-',dir=OUT) as d:
            env=v.canonical(Path(d));env.update(NOVACORE_WINDOW_CLIENT_WIDTH='3440',NOVACORE_WINDOW_CLIENT_HEIGHT='1440',NOVACORE_WINDOW_BORDERLESS='1')
            log=v.execute('restored-full-traversal',[str(ROOT/'samples/NovaCore.Triangle/bin/Release/net10.0/NovaCore.Triangle.exe'),'--scene=sol','--focus=earth','--physical-surface=m12d-natural-candidate','--solar-epoch=j2000','--p2s5c3-traversal','--benchmark-frames=10000','--log=startup,validation,vulkan'],env)
            assert 'P2S5C3 Desktop traversal PASS:' in log and 'VUID-' not in log
    elif sys.argv[1]=='smoke':
        with tempfile.TemporaryDirectory(prefix='layers-',dir=OUT) as d:
            env=v.canonical(Path(d));env.update(NOVACORE_WINDOW_CLIENT_WIDTH='3440',NOVACORE_WINDOW_CLIENT_HEIGHT='1440',NOVACORE_WINDOW_BORDERLESS='1')
            exe=str(ROOT/'samples/NovaCore.Triangle/bin/Release/net10.0/NovaCore.Triangle.exe')
            args=['--scene=sol','--focus=earth','--altitude=10.004','--physical-surface=m12d-natural-candidate','--solar-epoch=j2000','--log=startup,validation,vulkan']
            log=v.execute('restored-Florida-smoke',[exe,*args,'--surface-site=florida-launch','--benchmark-frames=350'],env)
            assert 'VUID-' not in log and 'NCSM1 fragment shading: ordinary specialization' in log
            env.update(NOVACORE_P2S5F_DIRECTIONAL_ONLY='1',NOVACORE_P2S5F_DIRECTIONAL_LEVEL='17',NOVACORE_P2S5F_DIRECTIONAL_YAW_RADIANS='1.5707963267948966',NOVACORE_P2S5F_DIRECTIONAL_PITCH_RADIANS='-0.035',NOVACORE_P2S5G_FIXED_DIAGNOSTIC_TIME='1')
            log=v.execute('restored-factor1-control',[exe,*args,'--p2s5c3-traversal','--benchmark-frames=350'],env)
            assert 'directional visibility PASS:' in log and 'VUID-' not in log
            sys.path.insert(0,str(HERE.parent/'near-surface-performance'))
            from analyze import analyze
            (HERE/'restored-factor1.json').write_text(json.dumps(analyze(log,'restored-factor1'),indent=2)+'\n')
    elif sys.argv[1]=='identity':
        baseline=json.loads((HERE/'baseline.json').read_text());report={}
        for config,build in [('Debug','native-ninja'),('Release','native-ninja-release')]:
            runtime=ROOT/f'samples/NovaCore.Triangle/bin/{config}/net10.0';expected=baseline['configurations'][config]
            shader={p.name:v.sha(p) for p in (runtime/'shaders').glob('*.spv')}
            assert shader==expected['shaders']
            assert v.sha(runtime/'NovaCore.Native.dll')==v.sha(ROOT/f'build/{build}/NovaCore.Native.dll')
            for p in (runtime/'shaders').glob('*.spv'):
                assert v.sha(p)==v.sha(ROOT/f'build/{build}/shaders'/p.name)
                subprocess.run([str(Path(os.environ['VULKAN_SDK'])/'Bin/spirv-val.exe'),str(p)],check=True,capture_output=True)
            report[config]=dict(runtime=str(runtime),native=v.sha(runtime/'NovaCore.Native.dll'),managed=v.sha(runtime/'NovaCore.Triangle.dll'),shaderCount=len(shader),shadersEqualBanked=True,spirvValidation='PASS',nativeMatchesBuild=True,nativeMatchesStartingBanked=v.sha(runtime/'NovaCore.Native.dll')==expected['native'])
        report['launcher']=str(ROOT/'tools/NovaCore.Launcher/bin/Release/net10.0-windows/NovaCore.Launcher.exe');assert Path(report['launcher']).exists()
        (HERE/'final-identity.json').write_text(json.dumps(report,indent=2)+'\n')
        print(json.dumps(report),flush=True)
