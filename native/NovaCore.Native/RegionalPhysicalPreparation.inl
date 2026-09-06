// Included after regional enablement and native buffer/descriptor helpers.
// All host mutations occur after the existing frame fence. No preparation wait,
// additional queue, or mutable descriptor may outlive that frame.
void InspectRegionalPreparation(App& a){
  auto& job=a.regionalPreparation[0];
  if(!job.fencePending)return;
  auto& control=*static_cast<RegionalPreparationControl*>(a.regionalPreparationMapped);
  if(job.generation!=a.productionBillboardGeneration||job.cursor!=a.productionBillboardVertexCount)
    throw std::runtime_error("regional pupil publication identity/completeness mismatch");
  std::swap(a.productionBillboardPhysicalBuffer,a.regionalScratchBuffer);
  std::swap(a.productionBillboardPhysicalMemory,a.regionalScratchMemory);
  std::swap(a.productionBillboardPhysicalMapped,a.regionalScratchMapped);
  std::swap(a.productionBillboardVertexCapacity,a.regionalScratchCapacity);
  a.regionalPublishedPupil=control.current;
  a.productionBillboardPreparedFrameIdentity=control.current.identity[0];
  UpdateProductionBillboardDescriptors(a,false);
  RegionalDescriptor(a,57,a.regionalScratchBuffer,VkDeviceSize(a.regionalScratchCapacity)*sizeof(NcSphericalBillboardPhysicalVertex));
  char message[384];std::snprintf(message,sizeof message,"NCSM1 staged pupil publication: generation=%llu; pupil=%u; vertices=%u; readinessMs=%.3f; fenceComplete=true; atomicFrameBoundary=true",(unsigned long long)job.generation,control.current.identity[0],job.cursor,std::chrono::duration<double,std::milli>(std::chrono::steady_clock::now()-job.started).count());a.Log(NC_LOG_ALWAYS,message);
  job={};
}

void UpdateRegionalPreparation(App& a){
  auto& control=*static_cast<RegionalPreparationControl*>(a.regionalPreparationMapped);
  auto& frames=*static_cast<NcProductionBillboardFrame*>(a.productionBillboardFrameMapped);
  if(!RegionalPhysicalEnabled(a)){
    a.regionalPreparation[0]={};
    for(auto& range:control.ranges)std::fill(std::begin(range),std::end(range),0u);
    return;
  }
  auto& current=a.regionalPreparation[0];auto& incoming=a.regionalPreparation[1];
  if(a.productionBillboardIncomingEnabled){
    // One physical replacement at a time. A topology transaction supersedes an
    // unpublished pupil refresh; its outgoing buffer is never partially changed.
    current={};
    if(incoming.generation!=a.productionBillboardIncomingGeneration){
      incoming={};incoming.generation=a.productionBillboardIncomingGeneration;
      incoming.active=true;incoming.started=std::chrono::steady_clock::now();
      control.incoming=frames.incoming;
    }
    frames.incoming=control.incoming;
    control.ranges[1][0]=incoming.cursor;control.ranges[1][2]=1u;
  }else{
    incoming={};std::fill(std::begin(control.ranges[1]),std::end(control.ranges[1]),0u);
    if(!current.active&&a.productionBillboardAuthoritative&&a.regionalReady[0]&&
       frames.current.metadata[0]&&frames.current.identity[0]!=a.productionBillboardPreparedFrameIdentity){
      if(frames.current.metadata[2]!=a.productionBillboardVertexCount)
        throw std::runtime_error("regional staged pupil topology mismatch");
      if(a.regionalScratchCapacity<a.productionBillboardVertexCapacity){
        DestroyHostBuffer(a,a.regionalScratchBuffer,a.regionalScratchMemory,a.regionalScratchMapped);
        a.regionalScratchCapacity=a.productionBillboardVertexCapacity;
        CreateHostBuffer(a,VkDeviceSize(a.regionalScratchCapacity)*sizeof(NcSphericalBillboardPhysicalVertex),VK_BUFFER_USAGE_STORAGE_BUFFER_BIT,a.regionalScratchBuffer,a.regionalScratchMemory,a.regionalScratchMapped,"regional pupil staging allocation failed");
        RegionalDescriptor(a,57,a.regionalScratchBuffer,VkDeviceSize(a.regionalScratchCapacity)*sizeof(NcSphericalBillboardPhysicalVertex));
      }
      current={};current.active=true;current.generation=a.productionBillboardGeneration;
      current.started=std::chrono::steady_clock::now();
      control.previous=a.regionalPublishedPupil;control.current=frames.current;
    }
  }
  control.ranges[0][0]=current.cursor;control.ranges[0][2]=current.active?1u:0u;
  // Rendering and culling retain the same published pupil throughout the job.
  frames.previous=a.regionalPublishedPupil;frames.current=a.regionalPublishedPupil;
}

bool RecordRegionalPreparation(App& a,VkCommandBuffer command,bool incoming){
  const uint32_t slot=incoming?1u:0u;auto& job=a.regionalPreparation[slot];
  if(!job.active||!a.regionalReady[slot])return false;
  auto& control=*static_cast<RegionalPreparationControl*>(a.regionalPreparationMapped);
  const uint32_t total=incoming?a.productionBillboardIncomingVertexCount:a.productionBillboardVertexCount;
  const auto& frame=incoming?control.incoming:control.current;
  if(frame.metadata[0]==0u||frame.metadata[2]!=total||frame.identity[2]>=18u||frame.identity[3]==0u||job.cursor>=total)
    throw std::runtime_error("regional preparation range/identity invalid");
  const uint32_t count=std::min(RegionalPreparationVertexBudget,total-job.cursor);
  control.ranges[slot][0]=job.cursor;control.ranges[slot][1]=count;
  vkCmdBindDescriptorSets(command,VK_PIPELINE_BIND_POINT_COMPUTE,a.pipelineLayout,0,1,&a.descriptor,0,nullptr);
  vkCmdWriteTimestamp(command,VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,a.regionalTimingQueries,2u+slot*2u);
  vkCmdBindPipeline(command,VK_PIPELINE_BIND_POINT_COMPUTE,incoming?a.productionBillboardIncomingPreparePipeline:a.productionBillboardPreparePipeline);
  vkCmdDispatch(command,(count+63u)/64u,1,1);
  vkCmdWriteTimestamp(command,VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,a.regionalTimingQueries,3u+slot*2u);
  a.regionalTimingRecorded[1u+slot]=true;
  VkMemoryBarrier barrier{VK_STRUCTURE_TYPE_MEMORY_BARRIER};barrier.srcAccessMask=VK_ACCESS_SHADER_WRITE_BIT;
  barrier.dstAccessMask=VK_ACCESS_SHADER_READ_BIT|VK_ACCESS_HOST_READ_BIT;
  vkCmdPipelineBarrier(command,VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT|VK_PIPELINE_STAGE_VERTEX_SHADER_BIT|VK_PIPELINE_STAGE_HOST_BIT,0,1,&barrier,0,nullptr,0,nullptr);
  job.cursor+=count;job.fencePending=job.cursor==total;
  char message[384];std::snprintf(message,sizeof message,"NCSM1 physical slice: incoming=%u; generation=%llu; pupil=%u; first=%u; count=%u; total=%u; complete=%u; currentOwner=%llu",slot,(unsigned long long)job.generation,frame.identity[0],job.cursor-count,count,total,job.fencePending?1u:0u,(unsigned long long)a.productionBillboardGeneration);a.Log(NC_LOG_ALWAYS,message);
  // Only the final incoming slice may record cull/compact and arm publication.
  return job.fencePending;
}
