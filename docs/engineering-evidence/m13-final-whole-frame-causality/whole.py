"""Bounded whole-frame controls; normal source/deployment restored after build."""
import inspect,json,os,pathlib,subprocess,sys
sys.dont_write_bytecode=True
HERE=pathlib.Path(__file__).resolve().parent;ROOT=HERE.parents[2]
PRIOR=HERE.parent/'m13-exit-assessment'
sys.path.insert(0,str(PRIOR));import assess
OUT=ROOT/'build/m13-final-whole-frame-causality';HOST=OUT/'host'
SHADERS=ROOT/'native/NovaCore.Native/shaders'
assess.HERE=HERE;assess.OUT=OUT;assess.HOST=HOST
base=assess.instrument
EXTENDED=False

# Adapt only the preceding adjacent timestamp instrumentation, not its private
# reuse, memory-placement or incoming-validation implementation.
s=(HERE.parent/'m13-residual-regional-frames/residual.py').read_text()
s=s[s.index('def instrument(s):'):s.index('    # A separate private causal control')]
s=s.replace('def instrument(s):','def split_timing(s):').replace('s=m.residual_mapped(s)','s=base(s)')+'    return s\n'
scope=dict(HERE=HERE,base=base);exec(s,scope);split_timing=scope['split_timing']
def instrument(s):
    s=split_timing(s)
    anchor='allocation.memoryTypeIndex=Memory(a,requirements.memoryTypeBits,VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT|VK_MEMORY_PROPERTY_HOST_COHERENT_BIT);'
    assert s.count(anchor)==1
    s=s.replace(anchor,anchor+'''
    const bool bulk=std::strcmp(failure,"incoming production billboard physical buffer failed")==0||std::strcmp(failure,"regional pupil staging allocation failed")==0||std::strcmp(failure,"incoming production billboard visibility buffer failed")==0||std::strcmp(failure,"incoming production billboard compacted buffer failed")==0||std::strcmp(failure,"resident production billboard lattice buffer failed")==0||std::strcmp(failure,"resident production billboard index buffer failed")==0;
    const char*placement=std::getenv("NOVACORE_WHOLE_BULK_LOCAL");
    const bool physicalOnly=std::strcmp(failure,"incoming production billboard physical buffer failed")==0||std::strcmp(failure,"regional pupil staging allocation failed")==0;
    if(bulk&&placement&&(std::strcmp(placement,"physical")!=0||physicalOnly))allocation.memoryTypeIndex=Memory(a,requirements.memoryTypeBits,VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT|VK_MEMORY_PROPERTY_HOST_COHERENT_BIT|VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT);
    if(bulk){VkPhysicalDeviceMemoryProperties props{};vkGetPhysicalDeviceMemoryProperties(a.physical,&props);const auto&type=props.memoryTypes[allocation.memoryTypeIndex];char row[512];std::snprintf(row,sizeof row,"Whole heap: label=%s; bytes=%llu; type=%u; properties=%u; heap=%u",failure,(unsigned long long)size,allocation.memoryTypeIndex,type.propertyFlags,type.heapIndex);a.Log(NC_LOG_ALWAYS,row);}
''')
    if EXTENDED:
        from capture import instrument_capture
        s=instrument_capture(s)
        anchor='void CreateProductionBillboard(App &a){'
        s=s.replace(anchor,(HERE.parent/'m13-regional-preparation-convergence/correlation.inl').read_text()+'\n'+anchor)
        s=s.replace('  UpdateRegionalPhysical(a);','  UpdateRegionalPhysical(a);\n  CompositionCorrelation(a);')
        anchor='  NcHostEvent e{NC_UPDATE_FRAME, NC_LOG_NONE, nullptr, in, a.submission};'
        s=s.replace(anchor,'  if(std::getenv("NOVACORE_COMPOSITION_CONTROL")){in={};in.viewportWidthPixels=a.extent.width;in.viewportHeightPixels=a.extent.height;}\n'+anchor)
    return s
assess.instrument=instrument
s=inspect.getsource(assess.execute).replace("    prep={k:","    markers+=['Residual GPU:','Whole heap:','AMD shader statistics:','NCSM1 physical slice:','Composition ','Pixel parity:']\n    prep={k:")
exec(s,assess.__dict__)

def baseline():
    d=assess.verify_deployment();old=json.loads((HERE.parent/'m13-regional-replacement-lifecycle/baseline.json').read_bytes())
    assert d['deployment']==old['deployment'] and d['assets']==old['assets']
    assert not subprocess.check_output(['git','diff','--name-only'],cwd=ROOT).strip()
    assert not subprocess.check_output(['git','diff','--cached','--name-only'],cwd=ROOT).strip()
    d.update(main=subprocess.check_output(['git','rev-parse','main'],cwd=ROOT,text=True).strip(),m13_4=subprocess.check_output(['git','rev-parse','m13.4-zero-contribution-terrain-material-noise^{}'],cwd=ROOT,text=True).strip(),budgetBytes=12*1024*1024,rawCaptureAllowed=False)
    assess.write('baseline',d)

def fixed(pose,label,noise=False,local=False,isa=False):
    env=assess.environment()
    env.update(NOVACORE_P2S5F_DIRECTIONAL_ONLY='1',NOVACORE_P2S5G_FIXED_DIAGNOSTIC_TIME='1',NOVACORE_P2S5F_DIRECTIONAL_LEVEL='17',NOVACORE_P2S5F_DIRECTIONAL_YAW_RADIANS='1.5707963267948966',NOVACORE_P2S5F_DIRECTIONAL_PITCH_RADIANS={'active':'-1.0','inland':'-1.0','grazing':'-0.001'}.get(pose,'-0.035'),NOVACORE_PERFORMANCE_FRAME_LOG='1',NOVACORE_PERFORMANCE_CLIPPING='1')
    if pose in ['active','grazing']:env['NOVACORE_PERFORMANCE_ALTITUDE_METRES']='10.004'
    if pose=='inland':env.update(NOVACORE_PERFORMANCE_ALTITUDE_METRES='50',NOVACORE_PERFORMANCE_GEOGRAPHY='40,-105')
    if pose=='florida':env['NOVACORE_PERFORMANCE_FLORIDA']='1'
    if local:env['NOVACORE_WHOLE_BULK_LOCAL']='physical' if 'physical' in label else '1'
    if 'capture' in label:env['NOVACORE_PIXEL_PARITY']=str(OUT)
    if isa:
        dest=HERE/(label+'-compiler');dest.mkdir();env['NOVACORE_SHADER_INFO']=str(dest)
    args=['--scene=sol','--focus=earth','--altitude=10.004','--solar-epoch=j2000','--physical-surface=m12d-natural-candidate','--p2s5c3-traversal','--benchmark-frames=1000','--log=startup,validation,vulkan']
    if pose=='florida':args.append('--surface-site=florida-launch')
    fragment=HOST/'shaders/planetary_production.frag.spv';saved=fragment.read_bytes()
    try:
        if noise:
            material=(SHADERS/'production_terrain_material.glsl').read_text()
            begin=material.index('  dvec3 point=(bodyMetres+offset)/scaleMetres;')
            end=material.index('\n}',begin)
            material=material[:begin]+'''  // Sensitivity only: deliberately different field, all function inputs live.
  dvec3 point=(bodyMetres+offset)/scaleMetres;
  return .5+.001*float(dot(fract(point),dvec3(weights)));'''+material[end:]
            (OUT/'production_terrain_material.glsl').write_text(material)
            path=OUT/'planetary_production.frag';path.write_bytes((SHADERS/path.name).read_bytes())
            subprocess.run([str(pathlib.Path(os.environ['VULKAN_SDK'])/'Bin/glslc.exe'),'-I',str(OUT),'-I',str(SHADERS),str(path),'-o',str(fragment)],check=True)
        result=assess.execute(label,args,env,'profile',HOST)
    finally:fragment.write_bytes(saved)
    if pose=='active':assert result['metrics']['maximumOuterTesFactor']['min']==64
    return result

def regional(label):
    env=assess.environment()
    env.update(NOVACORE_COMPOSITION_CONTROL='1',NOVACORE_PERFORMANCE_FRAME_LOG='1',NOVACORE_EARTH_ROUTE_VALIDATION='regional')
    if 'local' in label:env['NOVACORE_WHOLE_BULK_LOCAL']='1'
    args=['--scene=sol','--focus=earth','--surface-site=florida-launch','--physical-surface=m12d-natural-candidate','--solar-epoch=j2000','--benchmark-frames=3000','--log=startup,validation,vulkan']
    d=assess.execute(label,args,env,'profile',HOST)
    assert any('logicalFrame=1000' in x for x in d['lines'])
    return d

if __name__=='__main__':
    cmd=sys.argv[1]
    if cmd=='baseline':baseline()
    elif cmd.startswith('build'):
        if cmd!='build':
            source=inspect.getsource(assess.build).replace('os.link(ROOT/', 'oracle.exists() or os.link(ROOT/').replace('timing-host.patch',cmd+'-host.patch').replace("write('private-host',", "write('private-host-"+cmd+"',")
            if cmd in ['build-v3','build-candidate','build-candidate-final']:
                EXTENDED=True
                source=source.replace("'samples/NovaCore.Triangle/ProductionBillboardDesktopTraversal.cs']","'samples/NovaCore.Triangle/ProductionBillboardDesktopTraversal.cs','samples/NovaCore.Triangle/EarthRouteValidation.cs','samples/NovaCore.Triangle/Program.cs']")
                old=(HERE.parent/'m13-regional-preparation-convergence/composition.py').read_text()
                extra=old[old.index('        path=ROOT/names[1];s=path.read_text();'):old.index("        (HERE/(label+'.patch'))")]
                extra=extra.replace('names[2]','names[3]').replace('names[1]','names[2]')
                anchor="        (HERE/'"+cmd+"-host.patch')"
                assert source.count(anchor)==1;source=source.replace(anchor,extra+anchor)
            if cmd.startswith('build-candidate'):
                source='\n'.join(line for line in source.splitlines() if not line.strip().startswith('for n,b in original.items():assert b.replace'))+'\n'
                source=source.replace("    assert not subprocess.check_output(['git','diff','--name-only'],cwd=ROOT,text=True).strip()", "    assert all((ROOT/n).read_bytes()==b for n,b in original.items())")
            if EXTENDED:source=source.replace("'noCaptureInstrumentation':True", "'captureInstrumentationAvailable':True,'captureEnabledOnlyByExplicitProcessEnvironment':True")
            exec(source,assess.__dict__)
        assess.build()
    elif cmd.startswith('regional'):regional(cmd)
    else:fixed(sys.argv[2],cmd,noise='noise' in cmd,local='local' in cmd,isa='isa' in cmd)
