"""Process-isolated baseline/candidate GPU probes. No production deployment changes."""
import sys
sys.dont_write_bytecode=True
import argparse,hashlib,json,os,pathlib,shutil,subprocess
ROOT=pathlib.Path(__file__).resolve().parents[3];HERE=pathlib.Path(__file__).resolve().parent
OUT=ROOT/'build/ksa-terrain-convergence';SOURCE=ROOT/'native/NovaCore.Native/shaders'
sys.path.insert(0,str(HERE.parent/'near-surface-performance'))
from analyze import analyze,fields
def sha(p):
    with open(p,'rb') as f:return hashlib.file_digest(f,'sha256').hexdigest()
def run(pose,mode,label,capture=False,daylight=False,dynamic=False,warp=False,normal=False):
    runtime=OUT/label;runtime.mkdir(parents=True,exist_ok=False)
    host=OUT/('host-baseline' if mode=='baseline' else 'host')
    if normal:host=ROOT/'samples/NovaCore.Triangle/bin/Release/net10.0'
    for p in host.iterdir():
        if p.is_file():shutil.copy2(p,runtime/p.name)
    shutil.copytree(host/'shaders',runtime/'shaders')
    (runtime/'earth-data').mkdir()
    os.link(ROOT/'assets/earth/runtime/earth_elevation_8192x4096.r16',runtime/'earth-data/earth_elevation_8192x4096.r16')
    variants={}
    for name in ['production_spherical_billboard_prepare.comp','production_spherical_billboard_incoming_prepare.comp','production_spherical_billboard.tese']:
        target=runtime/'shaders'/(name+'.spv')
        source=SOURCE/name
        if capture and name.endswith('.tese'):
            sys.path.insert(0,str(HERE.parent/'active-refinement-usefulness'))
            from shaders import tessellation
            content=tessellation(source.read_text(encoding='utf-8')).replace('id<16777216u','id<65536u').replace('if(samples.enabled!=0u){','if(samples.enabled!=0u && uint(gl_PrimitiveID)%127u==0u){')
            source=runtime/name;source.write_text(content,encoding='utf-8')
        subprocess.run([str(pathlib.Path(os.environ['VULKAN_SDK'])/'Bin/glslc.exe'),'-I',str(SOURCE),'-DNOVACORE_PREPARED_RENDER_TERRAIN='+('1' if mode=='candidate' else '0'),str(source),'-o',str(target)],check=True)
        variants[name]=sha(target)
    layers=runtime/'layers';(layers/'implicit').mkdir(parents=True);(layers/'explicit').mkdir()
    manifest=pathlib.Path(os.environ['VULKAN_SDK'])/'Bin/VkLayer_khronos_validation.json'
    layer=json.loads(manifest.read_text());layer['layer']['library_path']=str((manifest.parent/layer['layer']['library_path']).resolve())
    (layers/'explicit/validation.json').write_text(json.dumps(layer))
    env={k:v for k,v in os.environ.items() if not k.startswith(('VK_LAYER','VK_ADD_LAYER','VK_IMPLICIT_LAYER','VK_ADD_IMPLICIT_LAYER','VK_LOADER_LAYERS','VK_VALIDATION','VK_INSTANCE_LAYERS','NOVACORE_'))}
    env.update(VK_LAYER_PATH=str(layers/'explicit'),VK_IMPLICIT_LAYER_PATH=str(layers/'implicit'),VK_LAYER_SETTINGS_PATH=str(layers),VK_INSTANCE_LAYERS='VK_LAYER_KHRONOS_validation',
        NOVACORE_WINDOW_CLIENT_WIDTH='3440',NOVACORE_WINDOW_CLIENT_HEIGHT='1440',NOVACORE_WINDOW_BORDERLESS='1',NOVACORE_PERFORMANCE_FRAME_LOG='1',NOVACORE_PERFORMANCE_GEOMETRY='1',NOVACORE_P2S5F_DIRECTIONAL_ONLY='1',NOVACORE_P2S5G_FIXED_DIAGNOSTIC_TIME='1',NOVACORE_P2S5F_DIRECTIONAL_LEVEL='0' if pose=='orbital' else '17',NOVACORE_P2S5F_DIRECTIONAL_YAW_RADIANS='1.5707963267948966',NOVACORE_P2S5F_DIRECTIONAL_PITCH_RADIANS={'active':'-1.0','grazing':'-0.001'}.get(pose,'-0.035'),NOVACORE_SHADER_INFO=str(runtime))
    if pose in ['active','grazing','land','land-level14','florida-near','florida-core']:env['NOVACORE_PERFORMANCE_ALTITUDE_METRES']='10.004'
    if pose in ['land','land-level14','florida-near','florida-core']:
        env['NOVACORE_PERFORMANCE_GEOGRAPHY']={'land':'40,-105','land-level14':'40,-105','florida-near':'28.6084,-80.60318','florida-core':'28.6084,-80.6042'}[pose]
        env['NOVACORE_P2S5F_DIRECTIONAL_PITCH_RADIANS']='-1.0'
        if pose=='land-level14':env['NOVACORE_P2S5F_DIRECTIONAL_LEVEL']='14'
    if pose=='florida':env['NOVACORE_PERFORMANCE_FLORIDA']='1'
    if pose=='land-coarse':
        env['NOVACORE_PERFORMANCE_GEOGRAPHY']='40,-105'
        env['NOVACORE_P2S5F_DIRECTIONAL_LEVEL']='10'
    if daylight:env['NOVACORE_PERFORMANCE_EPOCH_SECONDS']='21600'
    if dynamic or warp:
        for key in list(env):
            if key.startswith(('NOVACORE_P2S5F_DIRECTIONAL_','NOVACORE_P2S5G_FIXED_','NOVACORE_PERFORMANCE_ALTITUDE','NOVACORE_PERFORMANCE_FLORIDA')):env.pop(key)
    if warp:env['NOVACORE_P2S5E_WARP_ONLY']='1'
    if capture:env.update(NOVACORE_TES_PARITY=str(runtime),NOVACORE_PARITY_FRAME='175')
    args=['--scene=sol','--focus=earth','--altitude=10.004','--solar-epoch=j2000','--physical-surface=m12d-natural-candidate','--p2s5c3-traversal','--benchmark-frames=1000','--log=startup,validation,vulkan']
    if dynamic or warp:args[6]='--benchmark-frames=20000'
    if normal and dynamic:args.remove('--altitude=10.004')
    if pose=='florida':args.append('--surface-site=florida-launch')
    logpath=runtime/'runtime.log'
    with logpath.open('w',encoding='utf-8') as log:
        proc=subprocess.Popen([str(runtime/'NovaCore.Triangle.exe'),*args],cwd=runtime,env=env,stdout=log,stderr=subprocess.STDOUT)
        try:code=proc.wait(timeout=300 if dynamic else 180)
        except subprocess.TimeoutExpired:
            proc.kill();proc.wait();raise
    log=logpath.read_text(encoding='utf-8')
    result={'label':label,'pose':pose,'mode':mode,'capture':capture,'daylight':daylight,'dynamic':dynamic,'exitCode':code,'runtime':str(runtime),'shaders':variants,'native':sha(runtime/'NovaCore.Native.dll'),'managed':sha(runtime/'NovaCore.Triangle.dll'),'logSha256':sha(logpath),'args':args,'environment':{k:v for k,v in env.items() if k.startswith(('NOVACORE_','VK_'))},'validation':[x for x in log.splitlines() if 'VUID-' in x or 'Validation Error' in x],'captureIdentity':[x for x in log.splitlines() if 'TES parity capture:' in x],'compiler':[x for x in log.splitlines() if 'AMD shader statistics:' in x]}
    try:
        if dynamic or warp:raise ValueError('dynamic uses phase summaries below; no fixed-pose timing claim')
        result['analysis']=analyze(log,label)
        rows=[fields(x) for x in log.splitlines() if 'P2S5F directional visibility:' in x][-100:]
        result['frameRows']=rows
        for key in ['gpuDetailedDrawMs','gpuTotalMs']:
            values=sorted(r[key] for r in rows);result['analysis']['metrics'][key]['p99']=values[98]
    except (ValueError,KeyError) as error:result['analysisError']=str(error)
    if dynamic or warp:
        result.pop('analysisError',None)
        result['phaseEvidence']=[x for x in log.splitlines() if any(k in x for k in ['P2S5C3 Desktop','P2S5C3 phase','P2S5C3 finest snap','P2S5C3 CPU timing:','P2S5C3 GPU timing:','P2S5C3 frame timing:','NCSM1 regional ready:','NCSM1 regional physical totals:','NCSM1 regional GPU work:'])]
    if warp:
        result['warp']=True
        result['warpEvidence']=[x for x in log.splitlines() if 'P2S5E' in x]
    if normal:result['normalNativeAndManaged']=True
    (HERE/(label+'.json')).write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({k:result[k] for k in ['label','exitCode','validation','compiler']}),flush=True)
    marker='P2S5E anchored warp diagnostic PASS:' if warp else ('P2S5C3 Desktop traversal PASS:' if dynamic else 'directional visibility PASS:')
    if code or result['validation'] or marker not in log or 'analysisError' in result:raise RuntimeError('probe failed; see retained record')
    if not (dynamic or warp):print(result['analysis']['metrics']['gpuDetailedDrawMs'],flush=True)
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('pose');p.add_argument('mode',choices=['baseline','baseline-corrected-clearance','candidate']);p.add_argument('label');p.add_argument('--capture',action='store_true');p.add_argument('--daylight',action='store_true');p.add_argument('--dynamic',action='store_true');p.add_argument('--warp',action='store_true');p.add_argument('--normal',action='store_true');run(**vars(p.parse_args()))
