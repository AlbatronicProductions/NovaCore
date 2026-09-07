"""Bounded isolated reproduction. Timing runs have no capture shaders enabled."""
import sys
sys.dont_write_bytecode=True
import argparse,hashlib,json,os,shutil,subprocess,tempfile
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent.parent/'active-refinement-usefulness'))
import shaders
ROOT=Path(__file__).resolve().parents[3]
HERE=Path(__file__).resolve().parent
OUT=ROOT/'build/post-m13.2-next-target'
DEPLOYED=ROOT/'samples/NovaCore.Triangle/bin/Release/net10.0'
SOURCE=ROOT/'native/NovaCore.Native/shaders'
sys.path.insert(0,str(HERE.parent/'near-surface-performance'))
from analyze import analyze

def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()

def run(pose,probe,label,capture='none',callback=None,diagnostic=None,capture_frame=175):
    OUT.mkdir(parents=True,exist_ok=True)
    expected=json.loads((HERE/'baseline.json').read_text())['shaders']
    assert all(sha(DEPLOYED/'shaders'/name)==value for name,value in expected.items() )
    with tempfile.TemporaryDirectory(prefix='isolated-',dir=OUT) as tmp:
        runtime=Path(tmp).resolve();assert runtime.is_relative_to(OUT.resolve())
        for p in DEPLOYED.iterdir():
            if p.is_file():shutil.copy2(p,runtime/p.name)
        shutil.copytree(DEPLOYED/'shaders',runtime/'shaders')
        (runtime/'earth-data').mkdir()
        for p in (DEPLOYED/'earth-data').iterdir():
            if p.is_file():os.link(p,runtime/'earth-data'/p.name)
        identity={}
        def compile(name,source):
            p=runtime/name;p.write_text(source)
            target=runtime/'shaders'/(name+'.spv')
            subprocess.run([str(Path(os.environ['VULKAN_SDK'])/'Bin/glslc.exe'),'-I',str(SOURCE),str(p),'-o',str(target)],check=True)
            identity[name]=dict(source=sha(p),spirv=sha(target))
        fragment=(SOURCE/'planetary_production.frag').read_text()
        tes=(SOURCE/'production_spherical_billboard.tese').read_text()
        if probe=='defer-base':
            terms=[line for line in tes.splitlines(True) if 'double baseHeight=' in line or 'vec3 baseNormal=' in line]
            assert len(terms)==2
            for term in terms:tes=tes.replace(term,'')
            tes=tes.replace('  double height=max', ''.join(terms)+'  double height=max')
        if capture in ['tes','gs']:compile('production_spherical_billboard.tese',shaders.tessellation(tes))
        elif probe!='normal':compile('production_spherical_billboard.tese',tes)
        if capture=='gs':
            compile('primitive_capture.geom',shaders.geometry(tes))
            compile('planetary_production.frag',shaders.fragment_capture(fragment))
        layers=runtime/'layers';(layers/'implicit').mkdir(parents=True);(layers/'explicit').mkdir()
        manifest=Path(os.environ['VULKAN_SDK'])/'Bin/VkLayer_khronos_validation.json'
        data=json.loads(manifest.read_text());data['layer']['library_path']=str((manifest.parent/data['layer']['library_path']).resolve())
        (layers/'explicit/validation.json').write_text(json.dumps(data))
        env={k:v for k,v in os.environ.items() if not k.startswith(('VK_LAYER','VK_ADD_LAYER','VK_IMPLICIT_LAYER','VK_ADD_IMPLICIT_LAYER','VK_LOADER_LAYERS','VK_VALIDATION','VK_INSTANCE_LAYERS','NOVACORE_'))}
        env.update(VK_LAYER_PATH=str(layers/'explicit'),VK_IMPLICIT_LAYER_PATH=str(layers/'implicit'),VK_LAYER_SETTINGS_PATH=str(layers),VK_INSTANCE_LAYERS='VK_LAYER_KHRONOS_validation',NOVACORE_WINDOW_CLIENT_WIDTH='3440',NOVACORE_WINDOW_CLIENT_HEIGHT='1440',NOVACORE_WINDOW_BORDERLESS='1',NOVACORE_PERFORMANCE_FRAME_LOG='1',NOVACORE_PERFORMANCE_GEOMETRY='1',NOVACORE_P2S5F_DIRECTIONAL_ONLY='1',NOVACORE_P2S5G_FIXED_DIAGNOSTIC_TIME='1',NOVACORE_P2S5F_DIRECTIONAL_LEVEL='0' if pose=='E' else '17',NOVACORE_P2S5F_DIRECTIONAL_YAW_RADIANS='1.5707963267948966',NOVACORE_P2S5F_DIRECTIONAL_PITCH_RADIANS={'B':'-1.0','C':'-0.001'}.get(pose,'-0.035'),NOVACORE_SHADER_INFO=str(runtime))
        if pose in ['B','C']:env['NOVACORE_PERFORMANCE_ALTITUDE_METRES']='10.004'
        if pose=='D':env['NOVACORE_PERFORMANCE_FLORIDA']='1'
        if diagnostic:env['NOVACORE_SURFACE_DIAGNOSTIC']=diagnostic
        if capture=='gs':env['NOVACORE_PRIMITIVE_CAPTURE']='1'
        if capture!='none':
            env['NOVACORE_TES_PARITY']=str(runtime);env['NOVACORE_PARITY_FRAME']=str(capture_frame)
        args=['--scene=sol','--focus=earth','--altitude=10.004','--solar-epoch=j2000','--physical-surface=m12d-natural-candidate','--p2s5c3-traversal','--benchmark-frames=1000','--log=startup,validation,vulkan']
        if pose=='D':args.append('--surface-site=florida-launch')
        try:p=subprocess.run([str(runtime/'NovaCore.Triangle.exe'),*args],cwd=runtime,env=env,capture_output=True,text=True,timeout=240)
        except subprocess.TimeoutExpired as e:
            (OUT/(label+'-failed.log')).write_bytes((e.stdout or b'')+(e.stderr or b''));raise
        log=p.stdout+'\nSTDERR\n'+p.stderr
        (OUT/(label+'.log')).write_text(log,encoding='utf-8')
        if p.returncode or 'directional visibility PASS:' not in log or 'VUID-' in log:raise RuntimeError(label+' runtime/validation failure '+str(p.returncode))
        if probe!='baseline':
            assert ('NCSM1 fragment shading: full diagnostic specialization' if diagnostic else 'NCSM1 fragment shading: ordinary specialization') in log
        result=analyze(log,label)
        result['captureFrameOwner']=[x for x in log.splitlines() if f'Earth focus frame: frame={capture_frame};' in x]
        result.update(diagnostic=diagnostic,pipelineIdentity=[x for x in log.splitlines() if 'specialization' in x],fragmentSha256=sha(runtime/'shaders/planetary_production.frag.spv'),pose=pose,probe=probe,captureMode=capture,classification='MEASUREMENT BASELINE' if probe=='normal' else 'OUTPUT-PRESERVING INTENT; parity must be checked',shaderVariants=identity,nativeSha256=sha(runtime/'NovaCore.Native.dll'),managedSha256=sha(runtime/'NovaCore.Triangle.dll'),arguments=args,environment={k:v for k,v in env.items() if k.startswith('NOVACORE_')},amdStatistics=[x for x in log.splitlines() if 'AMD shader statistics:' in x],captureIdentity=[x for x in log.splitlines() if 'TES parity capture:' in x])
        for path in runtime.glob('stage-*.isa'):
            target=OUT/(label+'-'+path.name);shutil.copy2(path,target)
        if capture!='none':
            result['rawFiles']={p.name:dict(bytes=p.stat().st_size,sha256=sha(p)) for p in runtime.glob('*.bin')}
            (OUT/(label+'.json')).write_text(json.dumps(result,indent=2)+'\n')
            if callback:result['captureAnalysis']=callback(runtime,result)
        (OUT/(label+'.json')).write_text(json.dumps(result,indent=2)+'\n')
        assert all(sha(DEPLOYED/'shaders'/name)==value for name,value in expected.items() )
        print(label,result['metrics']['gpuDetailedDrawMs'],flush=True)
        return result

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('pose',choices=list('ABCDE'));p.add_argument('probe',choices=['normal','defer-base']);p.add_argument('label')
    args=p.parse_args();run(**vars(args))
