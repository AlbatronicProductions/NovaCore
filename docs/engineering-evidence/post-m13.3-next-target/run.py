"""One private host, bounded JSON evidence, no raw capture, no production edits.

All controls are diagnostic sensitivities, not recoverable-savings claims.
"""
import argparse,hashlib,json,math,os,pathlib,subprocess,sys,struct
sys.dont_write_bytecode=True
from prepare import ROOT,HERE,OUT,HOST,BASE,sha
sys.path.insert(0,str(HERE.parent/'near-surface-performance'))
from analyze import analyze,fields
SOURCE=ROOT/'native/NovaCore.Native/shaders'
DEPLOYED=ROOT/'samples/NovaCore.Triangle/bin/Release/net10.0'
def stats(v):
    v=sorted(v)
    return None if not v else {'n':len(v),'median':v[math.ceil(.5*len(v))-1],'p95':v[math.ceil(.95*len(v))-1],'p99':v[math.ceil(.99*len(v))-1],'min':v[0],'max':v[-1]}
def run(pose,label,cheap=False,one=False,quiet=False,isa=False,zero=False,capture=False,mixed=False,mask=False,normal_native=False,grid=False):
    assert not (HERE/(label+'.json')).exists(),'Refuse to overwrite evidence'
    # Restore the only shader changed by a preceding private probe.
    fragment=HOST/'shaders/planetary_production.frag.spv'
    banked=OUT/'banked-planetary_production.frag.spv'
    if not banked.exists():
        baseline=json.loads((HERE/'baseline-identity.json').read_text())['deployment']['Release']['shaders'][fragment.name]
        path=OUT/'baseline-source';path.mkdir(parents=True,exist_ok=True)
        for name in ['production_terrain_material.glsl','planetary_production.frag']:
            (path/name).write_bytes(subprocess.check_output(['git','show',BASE+':native/NovaCore.Native/shaders/'+name],cwd=ROOT))
        subprocess.run([str(pathlib.Path(os.environ['VULKAN_SDK'])/'Bin/glslc.exe'),'-I',str(path),'-I',str(SOURCE),str(path/'planetary_production.frag'),'-o',str(banked)],check=True)
        assert sha(banked)==baseline,'Pinned baseline compilation differs from recorded compiler output'
    fragment.write_bytes(banked.read_bytes())
    if cheap:
        source=(SOURCE/'planetary_production.frag').read_text()
        source=source[:source.index('void main()')]+'''void main(){vec3 v=color.rgb+normal+lightDirection+response.xyz+viewDirection+bodyDirection+vec3(terrainHeight)+bodyCameraHigh+bodyCameraLow+localDetail.xyz+vec3(productionUv,productionTransition.x)+vec3(topologyCoordinate,productionTransition.y)+vec3(material,productionLayer)+vec3(productionAddress.xyz)+vec3(productionAddress.w);outColor=vec4(fract(abs(v)*.001),color.a+response.w+localDetail.w);}'''
        path=OUT/'cheap.frag';path.write_text(source)
        subprocess.run([str(pathlib.Path(os.environ['VULKAN_SDK'])/'Bin/glslc.exe'),'-I',str(SOURCE),str(path),'-o',str(fragment)],check=True)
    if zero or mixed or mask:
        assert not cheap
        bankedMaterial=subprocess.check_output(['git','show',BASE+':native/NovaCore.Native/shaders/production_terrain_material.glsl'],cwd=ROOT).decode()
        material=bankedMaterial
        needle='  float mesoRaw=(mesoAttenuation>0.0||normalMesoAttenuation>0.0)?'
        assert material.count(needle)==1
        material=material.replace(needle,'  float landDetail=result.detailWeight*smoothstep(.45,.55,landMask);\n'+needle)
        material=material.replace('mesoRaw=(mesoAttenuation>0.0||normalMesoAttenuation>0.0)?','mesoRaw=(landDetail>0.0&&(mesoAttenuation>0.0||normalMesoAttenuation>0.0))?')
        material=material.replace('microRaw=(microAttenuation>0.0||normalMicroAttenuation>0.0)?','microRaw=(landDetail>0.0&&(microAttenuation>0.0||normalMicroAttenuation>0.0))?')
        material=material.replace('broadRaw=broadAttenuation>0.0?','broadRaw=(landDetail>0.0&&broadAttenuation>0.0)?')
        last=material.rindex('  float landDetail=result.detailWeight*smoothstep(.45,.55,landMask);')
        material=material[:last]+material[last:].replace('  float landDetail=result.detailWeight*smoothstep(.45,.55,landMask);','',1)
        (OUT/'production_terrain_material.glsl').write_text(material if zero else bankedMaterial)
        path=OUT/'planetary_production.frag';content=(SOURCE/path.name).read_text()
        if mixed:
            # Alternating zero, partial and positive mask lanes exercise coastal
            # derivative/helper participation. This is a synthetic proof only.
            needle='  if((diagnostic&8u)!=0u){outColor=vec4(visible.albedo,1.0);return;}'
            assert content.count(needle)==1
            content=content.replace(needle,'  visible.land=vec4(0.0,0.46,0.51,1.0)[(int(gl_FragCoord.x)+int(gl_FragCoord.y))%4];\n'+needle)
        if mask:
            needle='  if((diagnostic&8u)!=0u){outColor=vec4(visible.albedo,1.0);return;}'
            assert content.count(needle)==1
            content=content.replace(needle,'  if(ordinaryNcsm1){float m=TerrainDetailWeight(max(length(bodyCameraHigh+bodyCameraLow)-bodyRadius,0.0))*smoothstep(.45,.55,visible.land);outColor=vec4(m>0.0?1.0:0.0,m>0.0&&m<1.0?1.0:0.0,m,1);return;}\n'+needle)
        path.write_text(content)
        subprocess.run([str(pathlib.Path(os.environ['VULKAN_SDK'])/'Bin/glslc.exe'),'-I',str(OUT),'-I',str(SOURCE),str(path),'-o',str(fragment)],check=True)
    layers=OUT/'layers';(layers/'implicit').mkdir(parents=True,exist_ok=True);(layers/'explicit').mkdir(exist_ok=True)
    manifest=pathlib.Path(os.environ['VULKAN_SDK'])/'Bin/VkLayer_khronos_validation.json'
    layer=json.loads(manifest.read_text());layer['layer']['library_path']=str((manifest.parent/layer['layer']['library_path']).resolve())
    (layers/'explicit/validation.json').write_text(json.dumps(layer))
    env={k:v for k,v in os.environ.items() if not k.startswith(('VK_LAYER','VK_ADD_LAYER','VK_IMPLICIT_LAYER','VK_ADD_IMPLICIT_LAYER','VK_LOADER_LAYERS','VK_VALIDATION','VK_INSTANCE_LAYERS','NOVACORE_'))}
    env.update(VK_LAYER_PATH=str(layers/'explicit'),VK_IMPLICIT_LAYER_PATH=str(layers/'implicit'),VK_LAYER_SETTINGS_PATH=str(layers),VK_INSTANCE_LAYERS='VK_LAYER_KHRONOS_validation',
        NOVACORE_WINDOW_CLIENT_WIDTH='3440',NOVACORE_WINDOW_CLIENT_HEIGHT='1440',NOVACORE_WINDOW_BORDERLESS='1',NOVACORE_P2S5F_DIRECTIONAL_ONLY='1',NOVACORE_P2S5G_FIXED_DIAGNOSTIC_TIME='1',
        NOVACORE_P2S5F_DIRECTIONAL_LEVEL='0' if pose=='orbital' else '17',NOVACORE_P2S5F_DIRECTIONAL_YAW_RADIANS='1.5707963267948966',NOVACORE_P2S5F_DIRECTIONAL_PITCH_RADIANS={'active':'-1.0','grazing':'-0.001'}.get(pose,'-0.035'))
    if not quiet:env.update(NOVACORE_PERFORMANCE_FRAME_LOG='1',NOVACORE_PERFORMANCE_CLIPPING='1')
    else:env['NOVACORE_PERFORMANCE_COUNTERS_OFF']='1'
    if pose in ['active','grazing','land','coast']:env['NOVACORE_PERFORMANCE_ALTITUDE_METRES']='10.004'
    if pose in ['land','coast']:
        env['NOVACORE_PERFORMANCE_GEOGRAPHY']='40,-105' if pose=='land' else '28.6084,-80.60318'
        env['NOVACORE_P2S5F_DIRECTIONAL_PITCH_RADIANS']='-1.0'
    if pose=='florida':env['NOVACORE_PERFORMANCE_FLORIDA']='1'
    if grid:env['NOVACORE_P2S5F_DIRECTIONAL_GRID']='1'
    if one:env['NOVACORE_P2S5C3_COVERAGE_DIAGNOSTIC']='force-tes1'
    if capture:env['NOVACORE_PIXEL_PARITY']=str(OUT)
    if isa:
        destination=HERE/(label+'-compiler');destination.mkdir();env['NOVACORE_SHADER_INFO']=str(destination)
    args=['--scene=sol','--focus=earth','--altitude=10.004','--solar-epoch=j2000','--physical-surface=m12d-natural-candidate','--p2s5c3-traversal','--benchmark-frames=1000','--log=startup,validation,vulkan']
    if pose=='florida':args.append('--surface-site=florida-launch')
    native=HOST/'NovaCore.Native.dll';savedNative=None
    if normal_native:
        assert quiet and not (capture or isa),'Normal native control cannot use private instrumentation'
        savedNative=native.read_bytes();native.write_bytes((DEPLOYED/native.name).read_bytes())
    measuredNative=sha(native)
    try:
        result=subprocess.run([str(HOST/'NovaCore.Triangle.exe'),*args],cwd=HOST,env=env,capture_output=True,text=True,timeout=180)
    finally:
        if savedNative is not None:native.write_bytes(savedNative)
    log=result.stdout+'\nSTDERR\n'+result.stderr
    validation=[line for line in log.splitlines() if 'VUID-' in line or 'Validation Error' in line]
    if result.returncode or validation or 'directional visibility PASS:' not in log:
        (OUT/(label+'-failed.log')).write_text(log,encoding='utf-8')
        raise RuntimeError((label,result.returncode,validation))
    analysis=analyze(log,label)
    frameRows=[fields(x) for x in log.splitlines() if 'P2S5F directional visibility:' in x]
    if not grid:frameRows=frameRows[-100:]
    if grid:
        assert len(frameRows)>=800 and all(x['submittedFrame']==x['timingFrame'] for x in frameRows)
        analysis.update(samples=len(frameRows),firstFrame=frameRows[0],lastFrame=frameRows[-1])
    cpu=[fields(x) for x in log.splitlines() if 'Performance CPU frame:' in x]
    host=[fields(x) for x in log.splitlines() if 'Performance host frame:' in x]
    draws=[fields(x) for x in log.splitlines() if 'Performance draw frame:' in x]
    clipping=[fields(x) for x in log.splitlines() if 'Performance geometry frame:' in x][-100:]
    gpu=[fields(x) for x in log.splitlines() if 'Performance GPU frame:' in x]
    byFrame={r['frame']:r for r in host}
    for key in ['update','fenceWait','inspection','hostCallback','validationUpload','acquire','record','submit','present','recreate','total','unattributed']:
        # GPU frame N is waited/inspected by Update N+1; Draw N belongs to N.
        shift=1 if key in ['update','fenceWait','inspection','hostCallback','validationUpload'] else 0
        analysis['metrics']['host_'+key]=stats([byFrame[r['submittedFrame']+shift][key] for r in frameRows if r['submittedFrame']+shift in byFrame])
    byGpu={r['geometryFrame']:r for r in host if r['geometryFrame']==r['gpuFrame']}
    analysis['metrics']['clippingInputPrimitives']=stats([byGpu[r['submittedFrame']]['clippingInput'] for r in frameRows if r['submittedFrame'] in byGpu]) if not quiet else None
    analysis['unavailableMetricSamples']={}
    for key in frameRows[0]:
        values=[r[key] for r in frameRows if isinstance(r[key],(float,int)) and math.isfinite(r[key])]
        if values:
            analysis['metrics'][key]=stats(values)
            if len(values)!=len(frameRows):analysis['unavailableMetricSamples'][key]=len(frameRows)-len(values)
    preparation={}
    for kind in ['demand','currentPhysicalPreparation','incomingPhysicalPreparation']:
        rows=[fields(x) for x in log.splitlines() if f'NCSM1 regional GPU work: kind={kind};' in x]
        preparation[kind]={'sumMs':sum(r['ms'] for r in rows),'slices':len(rows),'cost':stats([r['ms'] for r in rows]),'rows':rows}
    record={'label':label,'pose':pose,'cheapFragment':cheap,'zeroContributionNoise':zero,'forceOne':one,'instrumentationOff':quiet,'exitCode':result.returncode,'args':args,'env':{k:v for k,v in env.items() if k.startswith(('NOVACORE_','VK_'))},
        'native':measuredNative,'managed':sha(HOST/'NovaCore.Triangle.dll'),'shaderHashes':{p.name:sha(p) for p in (HOST/'shaders').glob('*.spv')},'logSha256':hashlib.sha256(log.encode()).hexdigest(),'logBytes':len(log.encode()),
        'validation':validation,'warnings':[x for x in log.splitlines() if 'WARNING' in x],
        'identity':[x for x in log.splitlines() if any(k in x for k in ['GPU:','AMD shader statistics:','directional pose:','regional physical totals:','regional ready:','regional demand:','seating:','regional publication:','CPU timing:','GPU timing:','frame timing:','Frame pacing:','Fence wait pacing:','frames=','Earth Vulkan submission:'])],
        'analysis':analysis,'frameRows':frameRows,'cpuRows':cpu,'drawRows':draws,'hostRows':host,'gpuRows':gpu,'clippingRows':clipping,'preparation':preparation}
    if capture:
        record['capture']={'identity':[x for x in log.splitlines() if 'Pixel parity:' in x],
            'files':{name:{'bytes':(OUT/name).stat().st_size,'sha256':sha(OUT/name)} for name in ['pixels.bin','prepared.bin','selected.bin']}}
        # Existing parallel GPU compaction does not promise draw-order identity.
        # Preserve each oriented triple and compare the exact submitted multiset.
        triangles=sorted(struct.iter_unpack('<III',(OUT/'selected.bin').read_bytes()))
        canonical=b''.join(struct.pack('<III',*t) for t in triangles)
        record['capture']['selectedOrientedMultisetSha256']=hashlib.sha256(canonical).hexdigest()
        assert record['capture']['identity']
        record['syntheticMixedMask']=mixed
        record['maskDiagnostic']=mask
    (HERE/(label+'.json')).write_text(json.dumps(record,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({'label':label,'terrain':analysis['metrics']['gpuDetailedDrawMs'],'total':analysis['metrics']['gpuTotalMs'],'TES':analysis['metrics'].get('tesInvocations'),'validation':validation,'recordBytes':(HERE/(label+'.json')).stat().st_size}),flush=True)
    return record
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('pose');p.add_argument('label');p.add_argument('--cheap',action='store_true');p.add_argument('--one',action='store_true');p.add_argument('--quiet',action='store_true');p.add_argument('--isa',action='store_true');p.add_argument('--zero',action='store_true');p.add_argument('--capture',action='store_true');p.add_argument('--mixed',action='store_true');p.add_argument('--mask',action='store_true');p.add_argument('--normal-native',action='store_true');p.add_argument('--grid',action='store_true');run(**vars(p.parse_args()))
