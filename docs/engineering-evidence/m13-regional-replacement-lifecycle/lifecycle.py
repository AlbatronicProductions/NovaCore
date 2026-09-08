"""Private event instrumentation; all normal source/deployment bytes restored."""
import inspect,json,os,pathlib,subprocess,sys
HERE=pathlib.Path(__file__).resolve().parent;ROOT=HERE.parents[2]
PRIOR=HERE.parent/'m13-residual-regional-frames'
sys.path.insert(0,str(PRIOR));import residual as r
c=r.c;assess=r.assess
OUT=ROOT/'build/m13-regional-replacement-lifecycle';HOST=OUT/'host'
HERE.mkdir(exist_ok=True)
c.HERE=assess.HERE=c.g.HERE=HERE
c.OUT=assess.OUT=c.g.OUT=OUT;c.HOST=assess.HOST=c.g.HOST=HOST

# Reuse only timestamp/identity scaffolding for the production-semantics host.
# The separate reference host retains the rejected prior control exactly.
src=inspect.getsource(r.instrument).replace('def instrument(', 'def bank_timing(')
src=src.replace('s=m.residual_mapped(s)','s=c.residual_base(s)')
start=src.index('    # A separate private causal control')
end=src.index("    anchor='if(candidateRequested)",start)
src=src[:start]+src[end:]
scope=dict(r.__dict__,HERE=HERE,c=c)
exec(src,scope);bank_timing=scope['bank_timing']
REFERENCE=False

def instrument(s):
    s=r.instrument(s) if REFERENCE else bank_timing(s)
    anchor='uint32_t Memory(App &a, uint32_t bits, VkMemoryPropertyFlags props) {'
    assert s.count(anchor)==1;s=s.replace(anchor,(HERE/'host.inl').read_text()+'\n'+anchor)
    scopes={
      'void CreateHostBuffer(App &a,VkDeviceSize size,VkBufferUsageFlags usage,VkBuffer &buffer,VkDeviceMemory &memory,void *&mapped,const char *failure) {':'allocateMap',
      'void AcquireProductionBillboardIncomingWork(App &a,uint32_t vertices,uint32_t triangles){':'incomingWorkAcquire',
      'void UpdateProductionBillboardDescriptors(App &a,bool incoming){':'descriptorUpdate',
      'ProductionBillboardTopologyResource& AcquireProductionBillboardTopology(App &a,const NcProductionSphericalBillboardSubmission &candidate,bool &reused){':'topologyAcquire',
      'void CreateProductionBillboard(App &a){':'candidateCreate',
      'void InspectProductionBillboardPublication(App &a){':'publicationInspection',
    }
    for anchor,label in scopes.items():
        assert s.count(anchor)==1,anchor
        extra='LifeScope lifecycleScope(a,"'+label+'"'+(',size,failure' if label=='allocateMap' else '')+');'
        s=s.replace(anchor,anchor+'\n  '+extra)
    for call,label in [('InspectRegionalPhysical(a);','regionalInspection'),('PrepareProductionUploads(a);','prepareMaterialUploads'),('UpdateRegionalPhysical(a);','regionalUpdate')]:
        anchor='  '+call;assert s.count(anchor)==1,call
        s=s.replace(anchor,'  {LifeScope lifecycleScope(a,"'+label+'");'+call+'}')
    anchor='  RecordProductionUploads(a,c);';assert s.count(anchor)==1
    s=s.replace(anchor,'  LifeTransfers(a);\n'+anchor)
    # Diagnostic references omit incoming GPU work only. Current rendering,
    # final fences and publication predicates remain unchanged.
    anchor='RecordProductionBillboardWork(a,c,true);'
    assert s.count(anchor)==1
    s=s.replace(anchor,'if(!lifecycleReferenceTick)'+anchor)
    return s
c.instrument=assess.instrument=instrument

# Add an optional reference tick before a work tick at the same logical pose.
# This intentionally doubles route latency; it is not a production scheduler.
src=inspect.getsource(c.build).replace('def build(', 'def lifecycle_build(')
anchor='''        path=ROOT/names[2];s=path.read_text();anchor='solarScene.ApplyPresentationInput'''
extra='''        s=path.read_text()
        s=s.replace('    private bool _compositionStarted;', '''+repr('''    private bool _compositionStarted;
    private bool _lifecycleWorkTick;
    private static readonly bool LifecyclePairs=Environment.GetEnvironmentVariable("NOVACORE_LIFECYCLE_PAIRS") is not null;
    internal bool LifecycleSkipCoordinator=>LifecyclePairs&&_compositionStarted&&!_lifecycleWorkTick;
    [System.Runtime.InteropServices.DllImport("NovaCore.Native",EntryPoint="ncLifecycleReference")]
    private static extern void LifecycleReference(int value);''')+''')
        s=s.replace('        _frame++;', '''+repr('''        if(Composition&&LifecyclePairs){
            if(_lifecycleWorkTick){_lifecycleWorkTick=false;LifecycleReference(0);return;}
            _lifecycleWorkTick=true;LifecycleReference(1);
        }
        _frame++;''')+''')
        s=s.replace('Composition&&_frame>=1000;', 'Composition&&_frame>=1000&&(!LifecyclePairs||!_lifecycleWorkTick);')
        path.write_text(s)
'''
assert src.count(anchor)==1;src=src.replace(anchor,extra+anchor)
anchor="        path.write_text(s)\n        (HERE/(label+'.patch'))"
extra="""        s=s.replace('    var runtime=state.ProductionBillboardRuntime;if(runtime is null)return;', '    if(state.RouteValidation?.LifecycleSkipCoordinator==true)return;\\n    var runtime=state.ProductionBillboardRuntime;if(runtime is null)return;')
"""
assert src.count(anchor)==1;src=src.replace(anchor,extra+anchor)
exec(src,c.__dict__)
original=(HERE.parent/'m13-exit-assessment/assess.py').read_text()
start=original.index('def execute(');end=original.index('\ndef fixed(',start)
src=original[start:end].replace('    prep={k:',"    markers+=['Prep heap','Prep placement','Prep publication','NCSM1 physical slice','NCSM1 staged pupil publication','Composition ','Residual ','Lifecycle ']\n    prep={k:")
exec(src,assess.__dict__)

def run(label):
    env=assess.environment()
    args=['--scene=sol','--focus=earth','--surface-site=florida-launch','--physical-surface=m12d-natural-candidate','--solar-epoch=j2000','--benchmark-frames=3000','--log=startup,validation,vulkan']
    if label=='deployed-route':
        args[5]='--benchmark-frames=1000'
        env['NOVACORE_EARTH_ROUTE_VALIDATION']='regional'
        return assess.execute(label,args,env,'normal',ROOT/'samples/NovaCore.Triangle/bin/Release/net10.0')
    env.update(NOVACORE_COMPOSITION_CONTROL='1',NOVACORE_PERFORMANCE_FRAME_LOG='1',NOVACORE_EARTH_ROUTE_VALIDATION='regional')
    if 'paired' in label:env['NOVACORE_LIFECYCLE_PAIRS']='1'
    if label.startswith('reference'):
        env.update(NOVACORE_COMPOSITION_MAPPED='1',NOVACORE_PREP_DEVICE_LOCAL='1',NOVACORE_RESIDUAL_VALIDATE_ONLY='1',NOVACORE_RESIDUAL_VERTEX_VALIDATION='1')
        shader=HOST/'shaders'/(c.NAME+'.spv');validator=HOST/'shaders/production_nested_scale_mesh_incoming_cull.comp.spv'
        saved=shader.read_bytes();savedv=validator.read_bytes()
        try:
            path=OUT/(label+'.comp');path.write_text(r.m.source(False))
            glslc=str(pathlib.Path(os.environ['VULKAN_SDK'])/'Bin/glslc.exe')
            subprocess.run([glslc,'-I',str(c.SHADERS),str(path),'-o',str(shader)],check=True)
            subprocess.run([glslc,str(PRIOR/'incoming_validate_vertices.comp'),'-o',str(validator)],check=True)
            result=assess.execute(label,args,env,'profile',HOST)
        finally:shader.write_bytes(saved);validator.write_bytes(savedv)
    else:result=assess.execute(label,args,env,'profile',HOST)
    assert any('logicalFrame=1000' in x for x in result['lines'])
    return result

if __name__=='__main__':
    label=sys.argv[1]
    if label.startswith('build'):
        REFERENCE='reference' in label
        c.lifecycle_build('private-'+label)
    else:run(label)
