"""Bounded isolated reproduction. Timing runs have no capture shaders enabled."""
import sys
sys.dont_write_bytecode=True
import argparse,hashlib,json,os,shutil,subprocess,tempfile
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent.parent/'active-refinement-usefulness'))
import shaders
import probe_shader
import physical_shader
ROOT=Path(__file__).resolve().parents[3]
HERE=Path(__file__).resolve().parent
OUT=ROOT/'build/conservative-pre-refinement-visibility'
DEPLOYED=ROOT/'samples/NovaCore.Triangle/bin/Release/net10.0'
SOURCE=ROOT/'native/NovaCore.Native/shaders'
sys.path.insert(0,str(HERE.parent/'near-surface-performance'))
from analyze import analyze

def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()

def run(pose,probe,label,capture='none',callback=None,diagnostic=None,capture_frame=175,route=None):
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
        if probe=='oracle':
            compile('production_spherical_billboard.tesc',probe_shader.control((SOURCE/'production_spherical_billboard.tesc').read_text()))
            tes=probe_shader.evaluation(tes)
        if probe in ['dual','dual-control','dual-selftest','physical','physical-control','physical-selftest']:shutil.copy2(runtime/'shaders/production_spherical_billboard.tesc.spv',runtime/'shaders/parity_control.tesc.spv')
        if probe in ['dual','dual-control','dual-selftest','physical','physical-control','physical-selftest']:
            comparison=(HERE/'attachment_compare.comp').read_text()
            if probe=='dual-selftest':comparison=comparison.replace('#version 460','#version 460\n#define DIAGNOSTIC_COMPARE_SELF_TEST')
            compile('attachment_compare.comp',comparison)
        if probe in ['reject','dual','physical']:compile('production_spherical_billboard.tesc',probe_shader.rejection((SOURCE/'production_spherical_billboard.tesc').read_text()))
        if probe in ['physical','physical-control','physical-selftest']:
            compare=physical_shader.variant(tes,True)
            if probe=='physical-selftest':compare=compare.replace('  atomicAdd(physicalResult.values[0],1u);','  if(gl_PrimitiveID==0&&gl_TessCoord.x==1.0)current.clip.x=uintBitsToFloat(floatBitsToUint(current.clip.x)^1u);\n  atomicAdd(physicalResult.values[0],1u);')
            compile('production_spherical_billboard.tese',compare)
            compile('physical_control.tese',physical_shader.variant(tes,False))
            table=(HERE/'physical_table.comp').read_text().replace('#include "physical_compare.glsl"',(HERE/'physical_compare.glsl').read_text())
            compile('physical_table.comp',table)
        if capture in ['tes','gs']:compile('production_spherical_billboard.tese',shaders.tessellation(tes))
        elif probe=='oracle':compile('production_spherical_billboard.tese',tes)
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
        env['NOVACORE_DIAGNOSTIC_HOLD_DRIVER']='1'
        if probe=='oracle':env['NOVACORE_VISIBILITY_PROBE']='1'
        if probe in ['dual','dual-control','dual-selftest','physical','physical-control','physical-selftest']:
            env['NOVACORE_DUAL_PARITY']='1';env['NOVACORE_TES_PARITY']=str(runtime)
        if probe in ['physical','physical-control','physical-selftest']:env['NOVACORE_DUAL_PHYSICAL']='1'
        if route=='motion':env['NOVACORE_VISIBILITY_FLORIDA_MOTION']='1'
        if route and route!='motion':
            for key in list(env):
                if key.startswith(('NOVACORE_P2S5F_DIRECTIONAL','NOVACORE_PERFORMANCE_ALTITUDE','NOVACORE_PERFORMANCE_FLORIDA','NOVACORE_P2S5G_FIXED')):del env[key]
            if route=='warp':env['NOVACORE_P2S5E_WARP_ONLY']='1'
            if route=='return':env['NOVACORE_P2S5F_ZERO_VISIBLE_ONLY']='1'
        if diagnostic:env['NOVACORE_SURFACE_DIAGNOSTIC']=diagnostic
        if capture=='gs':env['NOVACORE_PRIMITIVE_CAPTURE']='1'
        if capture!='none':
            env['NOVACORE_TES_PARITY']=str(runtime);env['NOVACORE_PARITY_FRAME']=str(capture_frame)
        args=['--scene=sol','--focus=earth','--altitude=10.004','--solar-epoch=j2000','--physical-surface=m12d-natural-candidate','--p2s5c3-traversal','--benchmark-frames=1000','--log=startup,validation,vulkan']
        if pose=='D':args.append('--surface-site=florida-launch')
        if route:args=[a.replace('--benchmark-frames=1000','--benchmark-frames=10000') for a in args]
        # Stream to durable scratch instead of retaining the only log in a parent pipe.
        log_path=OUT/(label+'.log')
        with log_path.open('w',encoding='utf-8') as stream:
            p=subprocess.Popen([str(runtime/'NovaCore.Triangle.exe'),*args],cwd=runtime,env=env,stdout=stream,stderr=subprocess.STDOUT,text=True)
            (OUT/(label+'-running.json')).write_text(json.dumps(dict(pid=p.pid,runtime=str(runtime),nativeSha256=sha(runtime/'NovaCore.Native.dll'),variants=identity),indent=2))
            try:p.wait(timeout=900 if route else 240)
            except subprocess.TimeoutExpired:
                p.kill();p.wait();raise
        log=log_path.read_text(encoding='utf-8')
        if p.returncode or ('PASS:' not in log if route else 'directional visibility PASS:' not in log) or 'VUID-' in log:raise RuntimeError(label+' runtime/validation failure '+str(p.returncode))
        if probe!='baseline':
            assert ('NCSM1 fragment shading: full diagnostic specialization' if diagnostic else 'NCSM1 fragment shading: ordinary specialization') in log
        result=analyze(log,label) if not route else dict(metrics={},provenance=[x for x in log.splitlines() if any(t in x for t in ("PASS:","phase:","finest snap:","publication:","warp phase"))])
        result['dualPhysical']=[x for x in log.splitlines() if 'Dual physical:' in x]
        result['dualParity']=[x for x in log.splitlines() if 'Dual parity:' in x]
        result['visibilityOracle']=[x for x in log.splitlines() if 'Visibility oracle:' in x]
        result['driverLifetime']=[x for x in log.splitlines() if 'pinned' in x.lower()]
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
        print(label,result['metrics'].get('gpuDetailedDrawMs','traversal complete'),flush=True)
        return result

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('pose',choices=list('ABCDE'));p.add_argument('probe',choices=['normal','oracle','reject','dual','dual-control','dual-selftest','physical','physical-control','physical-selftest']);p.add_argument('label')
    args=p.parse_args();run(**vars(args))
