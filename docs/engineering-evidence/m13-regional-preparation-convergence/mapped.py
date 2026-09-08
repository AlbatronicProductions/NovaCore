"""One bounded mapped-source revision, private cheap gate before production ABI."""
import pathlib,sys,os,subprocess
import composition as c
base=c.instrument
def instrument(s):
    s=base(s)
    anchor='  VkBuffer regionalPreparationBuffer{},regionalScratchBuffer{};'
    assert s.count(anchor)==1
    s=s.replace(anchor,anchor+'\n  VkBuffer compositionMapBuffer{};VkDeviceMemory compositionMapMemory{};void* compositionMapMapped{};')
    s=s.replace('VkDescriptorSetLayoutBinding binds[46]{};','VkDescriptorSetLayoutBinding binds[47]{};')
    anchor='  binds[45]={58,VK_DESCRIPTOR_TYPE_STORAGE_BUFFER,1,VK_SHADER_STAGE_FRAGMENT_BIT,nullptr};'
    s=s.replace(anchor,anchor+'\n  binds[46]={59,VK_DESCRIPTOR_TYPE_STORAGE_BUFFER,1,VK_SHADER_STAGE_COMPUTE_BIT,nullptr};')
    s=s.replace('VK_DESCRIPTOR_TYPE_STORAGE_BUFFER,39','VK_DESCRIPTOR_TYPE_STORAGE_BUFFER,40')
    anchor='void CreateProductionBillboard(App &a){'
    s=s.replace(anchor,(c.HERE/'mapped.inl').read_text()+'\n'+anchor)
    anchor='  AcquireProductionBillboardIncomingWork(a,candidate->vertexCount,a.productionBillboardIncomingTriangleCount);'
    s=s.replace(anchor,anchor+'\n  CompositionMakeMap(a,*candidate);')
    anchor='  CreateGlobalTerrainPreparation(a);'
    s=s.replace(anchor,'  CreateHostBuffer(a,16ull+712106ull*4ull,VK_BUFFER_USAGE_STORAGE_BUFFER_BIT,a.compositionMapBuffer,a.compositionMapMemory,a.compositionMapMapped,"diagnostic source address map failed");\n'+anchor)
    s=s.replace('  CreateRegionalPhysical(a);','  CreateRegionalPhysical(a);\n  CompositionMapDescriptor(a);')
    s=s.replace('void DestroySubmission(App &a,bool destroyProductionBillboard=true) {','void DestroySubmission(App &a,bool destroyProductionBillboard=true) {\n  DestroyHostBuffer(a,a.compositionMapBuffer,a.compositionMapMemory,a.compositionMapMapped);')
    # Existing oracle marker[0] still counts all proposals. W identifies maps.
    s=s.replace('uint64_t negatives[4]{};','uint64_t negatives[4]{};std::array<uint32_t,12> mapped{};')
    s=s.replace('mismatch+=marker[1];','mismatch+=marker[1];mapped[i/65536]+=marker[3];')
    anchor='    if(mismatch)throw std::runtime_error("GPU exact direction reuse physical words differ");'
    s=s.replace(anchor,'    for(uint32_t i=0;i<total.size();i++)if(total[i]){char detail[384];std::snprintf(detail,sizeof detail,"Composition mapped oracle: generation=%llu; slice=%u; total=%u; sameIndex=%u; mapped=%u; fallback=%u",(unsigned long long)a.productionBillboardIncomingGeneration,i,total[i],reused[i]-mapped[i],mapped[i],total[i]-reused[i]);a.Log(NC_LOG_ALWAYS,detail);}\n'+anchor)
    return s
c.instrument=c.assess.instrument=instrument

def source(oracle):
    # Replay exactly the v2 basis/contribution proof generator, without running.
    template=(c.HERE/'composition.py').read_text()
    start=template.index('            s=copy_source(oracle)')
    end=template.index('            path=OUT/',start)
    snippet='\n'.join(line[12:] if line.startswith(' '*12) else line for line in template[start:end].splitlines())
    scope=dict(copy_source=c.copy_source,oracle=oracle,mode='D-oracle' if oracle else 'D')
    exec(snippet,scope);s=scope['s']
    anchor='bool sameBits(double a,double b)'
    s=s.replace(anchor,'layout(set=0,binding=59,std430) readonly buffer SourceMap{uvec4 identity;uint values[];} sourceMap;\n'+anchor)
    s=s.replace('  bool reusable=false;','  bool reusable=false;uint reuseSource=vertex;bool mappedReuse=false;')
    anchor='  if(reusable){PhysicalVertex value=publishedPhysical.values[vertex];' if not oracle else '#if NOVACORE_PREPARED_RENDER_TERRAIN'
    mapping='''
  if(!reusable && preparation.ranges[1].z!=0u && published.metadata.x!=0u &&
     sourceMap.identity.x==frame.metadata.z && sourceMap.identity.y==published.metadata.z &&
     vertex<sourceMap.identity.x && sameBits(radius,published.transition.y) &&
     sameBits(frame.east.x,published.east.x)&&sameBits(frame.east.y,published.east.y)&&sameBits(frame.east.z,published.east.z)&&
     sameBits(frame.north.x,published.north.x)&&sameBits(frame.north.y,published.north.y)&&sameBits(frame.north.z,published.north.z)&&
     sameBits(frame.up.x,published.up.x)&&sameBits(frame.up.y,published.up.y)&&sameBits(frame.up.z,published.up.z)&&
     regionalCatalog.entries[0].storage.w==0u){
    uint candidate=sourceMap.values[vertex];
    if(candidate<published.metadata.z && candidate!=vertex){
      dvec3 previous=canonicalDirection(publishedLattice.values[candidate].xyz,published);
      reusable=all(notEqual(direction,dvec3(0)))&&sameBits(direction.x,previous.x)&&sameBits(direction.y,previous.y)&&sameBits(direction.z,previous.z);
      if(reusable){reuseSource=candidate;mappedReuse=true;}
    }
  }
'''
    assert s.count(anchor)==1;s=s.replace(anchor,mapping+anchor)
    s=s.replace('publishedPhysical.values[vertex]','publishedPhysical.values[reuseSource]')
    if oracle:
        s=s.replace('uvec4(1u,exact?0u:1u,0u,0u)','uvec4(1u,exact?0u:1u,0u,mappedReuse?1u:0u)')
        # Compare each proposed source contribution, not the old same-index value.
        s=s.replace('vertex<pupilFrames.current.metadata.z','reuseSource<pupilFrames.current.metadata.z')
        s=s.replace('publishedLattice.values[vertex].xyz,pupilFrames.current','publishedLattice.values[reuseSource].xyz,pupilFrames.current')
    return s

def run(oracle=False):
    mode='D-mapped-oracle' if oracle else 'D-mapped'
    shader=c.HOST/'shaders'/(c.NAME+'.spv');saved=shader.read_bytes()
    try:
        path=c.OUT/(mode+'.comp');path.write_text(source(oracle))
        subprocess.run([str(pathlib.Path(os.environ['VULKAN_SDK'])/'Bin/glslc.exe'),'-I',str(c.SHADERS),str(path),'-o',str(shader)],check=True)
        env=c.assess.environment();env.update(NOVACORE_COMPOSITION_CONTROL='1',NOVACORE_COMPOSITION_MAPPED='1',NOVACORE_PERFORMANCE_FRAME_LOG='1',NOVACORE_EARTH_ROUTE_VALIDATION='regional',NOVACORE_PREP_DEVICE_LOCAL='1')
        if oracle:env['NOVACORE_PREP_GPU_ORACLE']='1'
        d=c.assess.execute(mode,['--scene=sol','--focus=earth','--surface-site=florida-launch','--physical-surface=m12d-natural-candidate','--solar-epoch=j2000','--benchmark-frames=3000','--log=startup,validation,vulkan'],env,'profile',c.HOST)
        assert any('logicalFrame=1000' in x for x in d['lines'])
    finally:shader.write_bytes(saved)

if __name__=='__main__':
    if sys.argv[1]=='build':c.build('private-host-v3')
    else:run(sys.argv[1]=='oracle')
