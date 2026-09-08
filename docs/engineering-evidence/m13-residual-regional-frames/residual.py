"""Private residual scopes adapted from retained proof; normal deployment preserved."""
import pathlib,sys,os,json,inspect,subprocess
HERE=pathlib.Path(__file__).resolve().parent;ROOT=HERE.parents[2]
PRIOR=HERE.parent/'m13-regional-preparation-convergence'
sys.path.insert(0,str(PRIOR));import mapped as m
c=m.c;assess=c.assess
OUT=ROOT/'build/m13-residual-regional-frames';HOST=OUT/'host'
# Deliberately reuse archived source while writing ONLY this ticket's output.
c.PRIOR_CONV=PRIOR;m.PRIOR=PRIOR
source=inspect.getsource(m.base).replace('def instrument(', 'def residual_base(').replace("(HERE/'correlation.inl')","(PRIOR_CONV/'correlation.inl')")
exec(source,c.__dict__);m.base=c.residual_base
source=inspect.getsource(m.instrument).replace('def instrument(','def residual_mapped(').replace("(c.HERE/'mapped.inl')","(PRIOR/'mapped.inl')")
exec(source,m.__dict__)
source=inspect.getsource(m.source).replace("(c.HERE/'composition.py')","(PRIOR/'composition.py')")
exec(source,m.__dict__)
c.HERE=assess.HERE=c.g.HERE=HERE;c.OUT=assess.OUT=c.g.OUT=OUT;c.HOST=assess.HOST=c.g.HOST=HOST

def instrument(s):
    s=m.residual_mapped(s)
    anchor='  VkQueryPool regionalTimingQueries{};bool regionalTimingRecorded[3]{};'
    assert s.count(anchor)==1;s=s.replace(anchor,anchor+'\n  VkQueryPool residualQueries{};bool residualRecorded[2]{};uint64_t residualRecordFrame{};')
    anchor='a.Check(vkCreateQueryPool(a.device,&qi,nullptr,&a.regionalTimingQueries),"regional timestamp pool failed");'
    s=s.replace(anchor,anchor+'qi.queryCount=16;a.Check(vkCreateQueryPool(a.device,&qi,nullptr,&a.residualQueries),"residual query pool failed");')
    anchor='    if(a.regionalTimingQueries)vkDestroyQueryPool(a.device,a.regionalTimingQueries,nullptr);'
    s=s.replace(anchor,anchor+'\n    if(a.residualQueries)vkDestroyQueryPool(a.device,a.residualQueries,nullptr);')
    anchor='void RecordProductionBillboardWork(App &a,VkCommandBuffer c,bool incoming){'
    s=s.replace(anchor,(HERE/'timing.inl').read_text()+'\n'+anchor)
    anchor='  vkCmdResetQueryPool(c,a.regionalTimingQueries,0,6);'
    s=s.replace(anchor,anchor+'\n  vkCmdResetQueryPool(c,a.residualQueries,0,16);a.residualRecordFrame=a.frame;')
    s=s.replace('  InspectRegionalPhysical(a);','  ResidualInspect(a);\n  InspectRegionalPhysical(a);')
    # Instrument only the final reset/cull/compact block, leaving dispatches intact.
    start=s.index('void RecordProductionBillboardWork(');end=s.index('\nconstexpr bool ProductionBillboardPresentationEnabled',start)
    block=s[start:end]
    anchor='  vkCmdBindDescriptorSets(c,VK_PIPELINE_BIND_POINT_COMPUTE,a.pipelineLayout,0,1,&a.descriptor,0,nullptr);vkCmdBindPipeline(c,VK_PIPELINE_BIND_POINT_COMPUTE,incoming?a.productionBillboardIncomingResetPipeline'
    assert block.count(anchor)==1;block=block.replace(anchor,'  ResidualStamp(a,c,incoming,0);\n'+anchor)
    block=block.replace('vkCmdDispatch(c,1,1,1);VkMemoryBarrier resetBarrier','vkCmdDispatch(c,1,1,1);ResidualStamp(a,c,incoming,1);VkMemoryBarrier resetBarrier')
    block=block.replace('0,1,&resetBarrier,0,nullptr,0,nullptr);const uint32_t topologyFamily','0,1,&resetBarrier,0,nullptr,0,nullptr);ResidualStamp(a,c,incoming,2);const uint32_t topologyFamily')
    block=block.replace('vkCmdDispatch(c,(triangles+63u)/64u,1,1);VkMemoryBarrier cullBarrier','vkCmdDispatch(c,(triangles+63u)/64u,1,1);ResidualStamp(a,c,incoming,3);VkMemoryBarrier cullBarrier')
    block=block.replace('0,1,&cullBarrier,0,nullptr,0,nullptr);vkCmdBindPipeline','0,1,&cullBarrier,0,nullptr,0,nullptr);ResidualStamp(a,c,incoming,4);vkCmdBindPipeline')
    block=block.replace('vkCmdDispatch(c,(triangles+63u)/64u,1,1);VkMemoryBarrier compactBarrier','vkCmdDispatch(c,(triangles+63u)/64u,1,1);ResidualStamp(a,c,incoming,5);VkMemoryBarrier compactBarrier')
    block=block.replace('0,1,&compactBarrier,0,nullptr,0,nullptr);if(incoming)','0,1,&compactBarrier,0,nullptr,0,nullptr);ResidualStamp(a,c,incoming,6);if(incoming)')
    s=s[:start]+block+s[end:]
    # A separate private causal control consumes real triangle-validation counts.
    # It never fabricates visible/rejected/compacted counts for skipped work.
    anchor='ResidualStamp(a,c,incoming,4);vkCmdBindPipeline'
    replacement='''ResidualStamp(a,c,incoming,4);
    if(incoming&&std::getenv("NOVACORE_RESIDUAL_VALIDATE_ONLY")){
      VkMemoryBarrier ready{VK_STRUCTURE_TYPE_MEMORY_BARRIER};ready.srcAccessMask=VK_ACCESS_SHADER_WRITE_BIT;ready.dstAccessMask=VK_ACCESS_HOST_READ_BIT;
      vkCmdPipelineBarrier(c,VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,VK_PIPELINE_STAGE_HOST_BIT,0,1,&ready,0,nullptr,0,nullptr);
      ResidualStamp(a,c,incoming,5);ResidualStamp(a,c,incoming,6);a.productionBillboardIncomingWorkRecorded=true;return;
    }
    vkCmdBindPipeline'''
    assert s.count(anchor)==1;s=s.replace(anchor,replacement)
    # One bounded revision moves immutable index qualification to its owner.
    # Reused native topology storage keeps this proof; no repeated index scan.
    s=s.replace('  uint64_t hash{},bytes{};','  uint64_t hash{},bytes{};bool residualIndicesValidated{};')
    anchor='  AcquireProductionBillboardIncomingWork(a,candidate->vertexCount,a.productionBillboardIncomingTriangleCount);'
    extra='''
  if(std::getenv("NOVACORE_RESIDUAL_VERTEX_VALIDATION")&&!topology.residualIndicesValidated){
    const auto begin=std::chrono::steady_clock::now();const auto* indices=static_cast<const uint32_t*>(topology.indexMapped);
    for(uint32_t index=0;index<topology.indexCount;index++)if(indices[index]>=topology.vertexCount)throw std::runtime_error("residual topology index range invalid");
    topology.residualIndicesValidated=true;char row[384];std::snprintf(row,sizeof row,"Residual topology qualification: hash=%llu; indices=%u; vertices=%u; valid=true; cpuMs=%.6f",(unsigned long long)topology.hash,topology.indexCount,topology.vertexCount,std::chrono::duration<double,std::milli>(std::chrono::steady_clock::now()-begin).count());a.Log(NC_LOG_ALWAYS,row);
  }
'''
    assert s.count(anchor)==1;s=s.replace(anchor,extra+anchor)
    # Only validator workgroups change, never current-owner camera work.
    s=s.replace('vkCmdBindPipeline(c,VK_PIPELINE_BIND_POINT_COMPUTE,cullPipeline);vkCmdDispatch(c,(triangles+63u)/64u,1,1);',
      'vkCmdBindPipeline(c,VK_PIPELINE_BIND_POINT_COMPUTE,cullPipeline);vkCmdDispatch(c,((incoming&&std::getenv("NOVACORE_RESIDUAL_VERTEX_VALIDATION")?a.productionBillboardIncomingVertexCount:triangles)+63u)/64u,1,1);')
    s=s.replace('counters[23]==a.productionBillboardIncomingTriangleCount&&counters[3]==0u;',
      'counters[23]==(std::getenv("NOVACORE_RESIDUAL_VERTEX_VALIDATION")?a.productionBillboardIncomingVertexCount:a.productionBillboardIncomingTriangleCount)&&counters[3]==0u;')
    s=s.replace('validatedElements=%u; invalidElements=%u;', 'validatedElements=%u; invalidElements=%u;')
    s=s.replace('a.productionBillboardCullFrameIdentity=a.productionBillboardPreparedFrameIdentity;if(RegionalPhysicalEnabled(a)&&publishedFrame)',
      'a.productionBillboardCullFrameIdentity=residualValidation?0u:a.productionBillboardPreparedFrameIdentity;if(RegionalPhysicalEnabled(a)&&publishedFrame)')
    anchor='  if(!readiness.Ready()){'
    replacement='''  const bool residualValidation=std::getenv("NOVACORE_RESIDUAL_VALIDATE_ONLY")&&RegionalPhysicalEnabled(a);
  const bool residualReady=readiness.fenceComplete&&topologyReady&&topologyFamilyValid&&physicalReady&&generationCoherent&&counters&&
    counters[5]==a.productionBillboardIncomingTriangleCount&&counters[23]==(std::getenv("NOVACORE_RESIDUAL_VERTEX_VALIDATION")?a.productionBillboardIncomingVertexCount:a.productionBillboardIncomingTriangleCount)&&counters[3]==0u;
  if(residualValidation?!residualReady:!readiness.Ready()){'''
    assert s.count(anchor)==1;s=s.replace(anchor,replacement)
    # Preserve truthful diagnostic publication reporting in this control.
    anchor='a.Log(NC_LOG_ALWAYS,message);\n}\nvoid Update(App &a, float dt)'
    replacement='''if(residualValidation){std::snprintf(message,sizeof message,"Residual publication: generation=%llu; validatedElements=%u; invalidElements=%u; physicalReady=true; normalsReady=true; finalFence=true; atomicFrameBoundary=true; cameraListPrepared=false; currentCullRequiredBeforeDraw=true",(unsigned long long)a.productionBillboardGeneration,counters[23],counters[3]);}a.Log(NC_LOG_ALWAYS,message);
}
void Update(App &a, float dt)'''
    assert s.count(anchor)==1;s=s.replace(anchor,replacement)
    anchor='if(candidateRequested){RecordProductionBillboardWork(a,c,false);RecordProductionBillboardWork(a,c,true);}'
    assert s.count(anchor)==1;s=s.replace(anchor,anchor+'ResidualState(a);')
    return s
c.instrument=assess.instrument=instrument
# Preserve new rows in the same scalar journal, with no geometry/readback capture.
original=(HERE.parent/'m13-exit-assessment/assess.py').read_text()
start=original.index('def execute(');end=original.index('\ndef fixed(',start)
src=original[start:end].replace('    prep={k:',"    markers+=['Prep GPU oracle','Prep heap','Prep placement','Prep publication','NCSM1 physical slice','NCSM1 staged pupil publication','Composition ','Residual ']\n    prep={k:")
exec(src,assess.__dict__)

def run(label):
    shader=HOST/'shaders'/(c.NAME+'.spv');saved=shader.read_bytes()
    validator=HOST/'shaders/production_nested_scale_mesh_incoming_cull.comp.spv';saved_validator=validator.read_bytes()
    try:
        path=OUT/(label+'.comp');path.write_text(m.source(False))
        subprocess.run([str(pathlib.Path(os.environ['VULKAN_SDK'])/'Bin/glslc.exe'),'-I',str(c.SHADERS),str(path),'-o',str(shader)],check=True)
        env=assess.environment();env.update(NOVACORE_COMPOSITION_CONTROL='1',NOVACORE_COMPOSITION_MAPPED='1',NOVACORE_PERFORMANCE_FRAME_LOG='1',NOVACORE_EARTH_ROUTE_VALIDATION='regional',NOVACORE_PREP_DEVICE_LOCAL='1')
        if label.startswith('validation'):
            vertex='vertices' in label
            subprocess.run([str(pathlib.Path(os.environ['VULKAN_SDK'])/'Bin/glslc.exe'),str(HERE/('incoming_validate_vertices.comp' if vertex else 'incoming_validate.comp')),'-o',str(validator)],check=True)
            env['NOVACORE_RESIDUAL_VALIDATE_ONLY']='1'
            if vertex:env['NOVACORE_RESIDUAL_VERTEX_VALIDATION']='1'
        d=assess.execute(label,['--scene=sol','--focus=earth','--surface-site=florida-launch','--physical-surface=m12d-natural-candidate','--solar-epoch=j2000','--benchmark-frames=3000','--log=startup,validation,vulkan'],env,'profile',HOST)
        assert any('logicalFrame=1000' in x for x in d['lines'])
    finally:shader.write_bytes(saved);validator.write_bytes(saved_validator)

if __name__=='__main__':
    command=sys.argv[1]
    if command=='baseline':
        d=assess.verify_deployment();old=json.loads((PRIOR/'baseline.json').read_bytes());assert d['deployment']==old['deployment'] and d['assets']==old['assets']
        d.update(budgetBytes=8*1024*1024,rawCaptureAllowed=False,priorEvidenceReadOnly=True)
        assess.write('baseline',d)
    elif command.startswith('build'):c.build('private-timing-host'+command[5:])
    else:run(command)
