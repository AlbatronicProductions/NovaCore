"""Narrow CPU request-poll timing; unchanged banked behavior, no raw readbacks."""
import inspect,sys
import exit as e
a=e.assess;base=a.instrument
def instrument(s):
    s=base(s)
    anchor='  uint64_t cpuTimingSamples{};'
    assert s.count(anchor)==1;s=s.replace(anchor,anchor+'\n  double exitCompleteMs{},exitQueueMs{};')
    anchor='  CompleteProductionUploads(a);QueueProductionRequests(a);'
    assert s.count(anchor)==1
    s=s.replace(anchor,'''  const auto pollStart=std::chrono::steady_clock::now();CompleteProductionUploads(a);const auto pollReady=std::chrono::steady_clock::now();QueueProductionRequests(a);const auto pollEnd=std::chrono::steady_clock::now();a.exitCompleteMs=std::chrono::duration<double,std::milli>(pollReady-pollStart).count();a.exitQueueMs=std::chrono::duration<double,std::milli>(pollEnd-pollReady).count();''')
    anchor='  PrepareProductionUploads(a);'
    assert s.count(anchor)==1
    s=s.replace(anchor,anchor+'''
  if(a.performanceFrameLog){char row[320];std::snprintf(row,sizeof row,"Performance request polling: frame=%llu; completeMs=%.6f; queueMs=%.6f; requests=%llu; uploads=%llu; pending=%u; records=%u",(unsigned long long)a.frame,a.exitCompleteMs,a.exitQueueMs,(unsigned long long)a.productionRequests,(unsigned long long)a.productionUploads,a.productionPendingUploads,a.productionPack?a.productionPack->RecordCount():0u);a.Log(NC_LOG_ALWAYS,row);}
''')
    anchor='allocation.memoryTypeIndex=Memory(a,requirements.memoryTypeBits,VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT|VK_MEMORY_PROPERTY_HOST_COHERENT_BIT);'
    assert s.count(anchor)==1
    s=s.replace(anchor,anchor+'''
  if(std::strcmp(failure,"terrain residency key buffer failed")==0){if(std::getenv("NOVACORE_EXIT_CACHED_KEYS"))allocation.memoryTypeIndex=Memory(a,requirements.memoryTypeBits,VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT|VK_MEMORY_PROPERTY_HOST_COHERENT_BIT|VK_MEMORY_PROPERTY_HOST_CACHED_BIT);VkPhysicalDeviceMemoryProperties p{};vkGetPhysicalDeviceMemoryProperties(a.physical,&p);for(uint32_t i=0;i<p.memoryTypeCount;i++){char row[256];std::snprintf(row,sizeof row,"Poll memory type: index=%u; flags=%u; heap=%u; compatible=%u; selected=%u; bytes=%llu",i,p.memoryTypes[i].propertyFlags,p.memoryTypes[i].heapIndex,(requirements.memoryTypeBits&(1u<<i))?1u:0u,allocation.memoryTypeIndex,(unsigned long long)size);a.Log(NC_LOG_ALWAYS,row);}}
''')
    return s
a.instrument=instrument
source=inspect.getsource(a.execute) if False else (e.PRIOR/'assess.py').read_text()
source=source[source.index('def execute('):source.index('\ndef fixed(')]
source=source.replace('    prep={k:',"    markers+=['Residual GPU:','Performance publication:','Performance request polling:','Poll memory type:']\n    prep={k:")
exec(source,a.__dict__)
if __name__=='__main__':
    cmd=sys.argv[1]
    if cmd.startswith('build'):
        suffix='-cached' if cmd=='build-cached' else ''
        source=inspect.getsource(a.build).replace('os.link(ROOT/', 'oracle.exists() or os.link(ROOT/').replace('timing-host.patch','poll'+suffix+'-host.patch').replace("write('private-host',", "write('private-poll"+suffix+"-host',")
        exec(source,a.__dict__);a.build()
    else:
        pose=sys.argv[2] if len(sys.argv)>2 else cmd;env=a.environment()
        if cmd.startswith('cached'):env['NOVACORE_EXIT_CACHED_KEYS']='1'
        env.update(NOVACORE_P2S5F_DIRECTIONAL_ONLY='1',NOVACORE_P2S5G_FIXED_DIAGNOSTIC_TIME='1',NOVACORE_P2S5F_DIRECTIONAL_LEVEL='0' if pose=='orbital' else '17',NOVACORE_P2S5F_DIRECTIONAL_YAW_RADIANS='1.5707963267948966',NOVACORE_P2S5F_DIRECTIONAL_PITCH_RADIANS='-0.035',NOVACORE_PERFORMANCE_FRAME_LOG='1')
        if pose=='florida':env['NOVACORE_PERFORMANCE_FLORIDA']='1'
        args=['--scene=sol','--focus=earth','--altitude=10.004','--solar-epoch=j2000','--physical-surface=m12d-natural-candidate','--p2s5c3-traversal','--benchmark-frames=1000','--log=startup,validation,vulkan']
        if pose=='florida':args.append('--surface-site=florida-launch')
        a.execute(cmd+'-'+pose if len(sys.argv)>2 else 'poll-'+pose,args,env,'profile',e.HOST)
