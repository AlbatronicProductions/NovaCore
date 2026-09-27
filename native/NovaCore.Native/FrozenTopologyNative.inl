// Diagnostic topology activation lifecycle. Copies use the existing graphics
// command buffer and its existing fence; no new submit, queue wait or retry.
void EmitFrozenTopology(App& a,uint64_t operation,const nc::frozen::TopologyWitness& t){
  std::array<uint64_t,nc::frozen::TWordCount+1> values{};values[0]=operation;
  std::copy(t.authority.begin(),t.authority.end(),values.begin()+1);
  a.causal.EmitWords(Phase(29),Kind::Info,0,values);
}
void AcquireFrozenTopology(App& a,const ProductionBillboardTopologyResource& resource){
  if(!FrozenAdmissionsEnabled(a))return;
  using namespace nc::frozen;
  const uint64_t bytes=uint64_t(resource.indexCount)*4;
  if(!resource.indexBirth||!bytes||bytes>SlotBytes)throw std::runtime_error("frozen topology diagnostic capacity/incarnation");
  for(auto& t:a.frozen.topologies)if(t.state!=Empty&&t.authority[TSource]==uint64_t(resource.index)&&t.authority[TSourceBirth]==resource.indexBirth){
    if(t.authority[THash]!=resource.hash||t.authority[TFamily]!=resource.family||t.authority[TVertices]!=resource.vertexCount||t.authority[TTriangles]*3!=resource.indexCount)throw std::runtime_error("frozen immutable topology identity changed");
    return;
  }
  for(size_t i=0;i<TopologyCount;++i){auto& t=a.frozen.topologies[i];
    if(!t.Retirable(uint64_t(a.productionBillboardIndexBuffer),uint64_t(a.productionBillboardIncomingIndexBuffer)))continue;
    if(t.state!=Empty){EmitFrozenTopology(a,4,t);void* mapped=t.mapped;DestroyHostBuffer(a,a.frozenTopologyBuffers[i],a.frozenTopologyMemory[i],mapped);}
    t.authority={};t.mapped=nullptr;t.state=Empty;
    void* mapped=nullptr;CreateHostBuffer(a,bytes,VK_BUFFER_USAGE_TRANSFER_DST_BIT,a.frozenTopologyBuffers[i],a.frozenTopologyMemory[i],mapped,"frozen topology readback allocation failed",nc::MappedBufferUse::TerrainRequestKeys);
    t.mapped=static_cast<uint8_t*>(mapped);
    auto& w=t.authority;w[TIdentity]=++a.frozen.nextTopologyIdentity;w[TSource]=uint64_t(resource.index);w[TSourceBirth]=resource.indexBirth;
    w[THash]=resource.hash;w[TFamily]=resource.family;w[TVertices]=resource.vertexCount;w[TTriangles]=resource.indexCount/3;
    w[TSection]=Indices;w[TBytes]=bytes;w[TStride]=4;w[TDestination]=uint64_t(a.frozenTopologyBuffers[i]);w[TDestinationBirth]=a.causalBufferBirth;
    t.state=Allocated;EmitFrozenTopology(a,1,t);return;
  }
  throw std::runtime_error("frozen topology cache has no unpinned completed owner");
}
void RecordFrozenTopologies(App& a,VkCommandBuffer command){
  if(!FrozenAdmissionsEnabled(a))return;using namespace nc::frozen;
  // Startup backfills at most one owner; runtime acquisition is blocked while
  // incoming terrain exists. Recreation preserves that owner. Fail closed if
  // a future lifecycle change breaks the observer's single-activation contract.
  if(std::count_if(a.frozen.topologies.begin(),a.frozen.topologies.end(),[](const auto& t){return t.state==Allocated;})>1)throw std::runtime_error("multiple topology activations in one frame");
  for(size_t i=0;i<TopologyCount;++i){auto& t=a.frozen.topologies[i];if(t.state!=Allocated)continue;
    LARGE_INTEGER begin,end,frequency;QueryPerformanceCounter(&begin);QueryPerformanceFrequency(&frequency);
    auto& w=t.authority;w[TFrame]=a.causal.frame.load();w[TCommand]=uint64_t(command);w[TRecordSerial]=a.causal.LastSerial()+1;
    EmitFrozenTopology(a,2,t); // Entered recording boundary, even if a Vulkan call never returns.
    CausalGpuLabel label(a,command,"Diagnostic topology lifetime GPU witness",true);
    vkCmdResetQueryPool(command,a.frozenTopologyQueries,uint32_t(i*2),2);
    vkCmdWriteTimestamp(command,VK_PIPELINE_STAGE_BOTTOM_OF_PIPE_BIT,a.frozenTopologyQueries,uint32_t(i*2));
    VkMemoryBarrier before{VK_STRUCTURE_TYPE_MEMORY_BARRIER};before.srcAccessMask=VK_ACCESS_HOST_WRITE_BIT;before.dstAccessMask=VK_ACCESS_TRANSFER_READ_BIT;
    vkCmdPipelineBarrier(command,VK_PIPELINE_STAGE_HOST_BIT,VK_PIPELINE_STAGE_TRANSFER_BIT,0,1,&before,0,nullptr,0,nullptr);
    VkBufferCopy copy{0,0,w[TBytes]};vkCmdCopyBuffer(command,VkBuffer(w[TSource]),a.frozenTopologyBuffers[i],1,&copy);
    VkMemoryBarrier after{VK_STRUCTURE_TYPE_MEMORY_BARRIER};after.srcAccessMask=VK_ACCESS_TRANSFER_WRITE_BIT;after.dstAccessMask=VK_ACCESS_HOST_READ_BIT;
    vkCmdPipelineBarrier(command,VK_PIPELINE_STAGE_TRANSFER_BIT,VK_PIPELINE_STAGE_HOST_BIT,0,1,&after,0,nullptr,0,nullptr);
    vkCmdWriteTimestamp(command,VK_PIPELINE_STAGE_BOTTOM_OF_PIPE_BIT,a.frozenTopologyQueries,uint32_t(i*2+1));
    t.state=Recorded;QueryPerformanceCounter(&end);
    a.causal.Emit(Phase(29),Kind::Info,0,{5,w[TIdentity],w[TFrame],nc::causal::Recorder::Bits(double(end.QuadPart-begin.QuadPart)*1000/double(frequency.QuadPart))});
  }
}
void CompleteFrozenTopologies(App& a){
  if(!a.frozen.Active())return;using namespace nc::frozen;
  for(size_t i=0;i<TopologyCount;++i){auto& t=a.frozen.topologies[i];if(t.state!=nc::frozen::Submitted)continue;
    LARGE_INTEGER begin,end,frequency;QueryPerformanceCounter(&begin);QueryPerformanceFrequency(&frequency);auto& w=t.authority;
    if(w[TFrame]!=a.causal.completed.load()||!w[TSubmission])throw std::runtime_error("frozen topology completion identity");
    std::array<uint64_t,2> ticks{};
    const auto result=vkGetQueryPoolResults(a.device,a.frozenTopologyQueries,uint32_t(i*2),2,sizeof(ticks),ticks.data(),sizeof(uint64_t),VK_QUERY_RESULT_64_BIT);
    if(result!=VK_SUCCESS||ticks[1]<ticks[0])throw std::runtime_error("frozen topology query not ready at completed fence");
    w[TCompleted]=a.causal.completed.load();w[TCompletedSubmission]=w[TSubmission];w[TCompleteSerial]=a.causal.LastSerial()+1;
    w[TGpuCost]=nc::causal::Recorder::Bits(double(ticks[1]-ticks[0])*a.timestampPeriodNanoseconds/1e6);
    QueryPerformanceCounter(&end);w[TCpuCost]=nc::causal::Recorder::Bits(double(end.QuadPart-begin.QuadPart)*1000/double(frequency.QuadPart));
    t.state=Proven;EmitFrozenTopology(a,3,t);
  }
}
void DestroyFrozenTopologies(App& a){
  using namespace nc::frozen;
  // Called only after the existing GPU idle and Writer.Stop. Reserved/cancelled
  // frame slots can still pin a witness but no thread may read it now.
  for(auto& s:a.frozen.slots)if(s.topology){s.topology->pins.fetch_sub(1,std::memory_order_release);s.topology=nullptr;}
  for(size_t i=0;i<TopologyCount;++i){auto& t=a.frozen.topologies[i];
    if(t.pins.load())throw std::runtime_error("frozen topology writer pin escaped shutdown");
    if(t.state!=Empty)EmitFrozenTopology(a,4,t);
    void* mapped=t.mapped;DestroyHostBuffer(a,a.frozenTopologyBuffers[i],a.frozenTopologyMemory[i],mapped);t.mapped=nullptr;t.state=Empty;
  }
  if(a.frozenTopologyQueries){vkDestroyQueryPool(a.device,a.frozenTopologyQueries,nullptr);a.frozenTopologyQueries={};}
}
