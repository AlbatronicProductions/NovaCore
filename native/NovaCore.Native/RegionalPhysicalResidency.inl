// Included inside the native App implementation after its buffer helpers.
void RegionalDescriptor(App& a,uint32_t binding,VkBuffer buffer,VkDeviceSize bytes){
  if(!a.descriptor)return;VkDescriptorBufferInfo info{buffer,0,bytes};VkWriteDescriptorSet write{VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET};
  write.dstSet=a.descriptor;write.dstBinding=binding;write.descriptorCount=1;write.descriptorType=VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;write.pBufferInfo=&info;
  vkUpdateDescriptorSets(a.device,1,&write,0,nullptr);
}
void CreateRegionalPhysical(App& a){
  using namespace nc::regionalphysical;
  if(!a.regionalCatalogBuffer){
    if(a.localPack)a.regionalPhysical=std::make_unique<Residency>(*a.localPack);
    CreateHostBuffer(a,sizeof(Catalog),VK_BUFFER_USAGE_STORAGE_BUFFER_BIT,a.regionalCatalogBuffer,a.regionalCatalogMemory,a.regionalCatalogMapped,"regional physical catalog allocation failed");
    std::memset(a.regionalCatalogMapped,0,sizeof(Catalog));
    CreateHostBuffer(a,4,VK_BUFFER_USAGE_STORAGE_BUFFER_BIT,a.regionalPayloadBuffer,a.regionalPayloadMemory,a.regionalPayloadMapped,"regional physical placeholder failed");
    CreateHostBuffer(a,sizeof(RegionalDemandBuffer),VK_BUFFER_USAGE_STORAGE_BUFFER_BIT,a.regionalDemandBuffer,a.regionalDemandMemory,a.regionalDemandMapped,"regional physical demand allocation failed");
    std::memset(a.regionalDemandMapped,0,sizeof(RegionalDemandBuffer));
    CreateHostBuffer(a,sizeof(RegionalPreparationControl),VK_BUFFER_USAGE_STORAGE_BUFFER_BIT,a.regionalPreparationBuffer,a.regionalPreparationMemory,a.regionalPreparationMapped,"regional preparation control allocation failed");
    std::memset(a.regionalPreparationMapped,0,sizeof(RegionalPreparationControl));
    CreateHostBuffer(a,sizeof(NcSphericalBillboardPhysicalVertex),VK_BUFFER_USAGE_STORAGE_BUFFER_BIT,a.regionalScratchBuffer,a.regionalScratchMemory,a.regionalScratchMapped,"regional pupil placeholder failed");
  }
  RegionalDescriptor(a,53,a.regionalCatalogBuffer,sizeof(Catalog));
  RegionalDescriptor(a,54,a.regionalPayloadBuffer,a.regionalPayloadBytes);
  RegionalDescriptor(a,55,a.regionalDemandBuffer,sizeof(RegionalDemandBuffer));
  RegionalDescriptor(a,56,a.regionalPreparationBuffer,sizeof(RegionalPreparationControl));
  RegionalDescriptor(a,57,a.regionalScratchBuffer,VkDeviceSize(std::max(1u,a.regionalScratchCapacity))*sizeof(NcSphericalBillboardPhysicalVertex));
}
void DestroyRegionalPhysical(App& a){
  if(a.regionalPhysical){const auto& r=*a.regionalPhysical;char message[1024];
    std::snprintf(message,sizeof message,"NCSM1 regional physical totals: requests=%llu; hits=%llu; misses=%llu; loaded=%llu; readBytes=%llu; uploadedBytes=%llu; residentHighWater=%llu; allocatedBytes=%llu; maximumBytes=%llu; queueHighWater=%u; completionCapacity=%u; payloadWriter=workerWriteOnce; publicationBatches=%llu; maximumPublicationRecords=%u; workerCopyMs=%.3f; demandDispatches=%llu; dependencyDelayMsMax=%.3f; recordReadDecodeVerifyMs=%.3f; contributingRecords=%u; anchoredDemandRequired=false",(unsigned long long)r.requests,(unsigned long long)r.hits,(unsigned long long)r.misses,(unsigned long long)r.loaded,(unsigned long long)r.readBytes,(unsigned long long)r.uploadBytes,(unsigned long long)r.loaded,(unsigned long long)a.regionalPayloadBytes,(unsigned long long)nc::regionalphysical::MaximumPayloadBytes,r.queueHighWater,nc::regionalphysical::MaximumRecords,(unsigned long long)r.publicationBatches,r.maximumPublicationRecords,r.workerCopyMs,(unsigned long long)a.regionalDemandDispatches,a.regionalDependencyDelayMs,r.transcodeMs,r.contributingRecords);a.Log(NC_LOG_ALWAYS,message);}
  // Join the write-once payload owner before releasing its mapped allocation.
  a.regionalPhysical.reset();
  DestroyHostBuffer(a,a.regionalPreparationBuffer,a.regionalPreparationMemory,a.regionalPreparationMapped);
  DestroyHostBuffer(a,a.regionalScratchBuffer,a.regionalScratchMemory,a.regionalScratchMapped);
  DestroyHostBuffer(a,a.regionalCatalogBuffer,a.regionalCatalogMemory,a.regionalCatalogMapped);
  DestroyHostBuffer(a,a.regionalPayloadBuffer,a.regionalPayloadMemory,a.regionalPayloadMapped);
  DestroyHostBuffer(a,a.regionalDemandBuffer,a.regionalDemandMemory,a.regionalDemandMapped);
}
bool RegionalPhysicalEnabled(const App& a){
  const auto& p=a.submission->planetaryPresentation;
  return a.regionalPhysical&&(a.submission->productionBillboardFlags&1u)&&p.bodyIdLow==6u&&p.bodyIdHigh==0u&&
    a.submission->physicalSurfaceGeneration==4u&&a.submission->planetarySurfaceMode==NC_PLANETARY_SURFACE_PRODUCTION_CUBE&&
    (a.productionBillboardTopologyFamily==NC_PLANETARY_PRODUCTION_TOPOLOGY_NESTED_SCALE_MESH_NCSM1||
     a.productionBillboardIncomingTopologyFamily==NC_PLANETARY_PRODUCTION_TOPOLOGY_NESTED_SCALE_MESH_NCSM1);
}
#include "RegionalPhysicalPreparation.inl"
void InspectRegionalPhysical(App& a){
  InspectRegionalPreparation(a);
  // Caller has waited for the frame fence. Demand readback and subsequent
  // resident-catalog publication are render-owned. Worker payload writes touch
  // only unpublished, disjoint ranges; authoritative draws cannot read them.
  if(!a.regionalPhysical)return;
  for(uint32_t slot=0;slot<3;++slot)if(a.regionalTimingRecorded[slot]){
    uint64_t ticks[2]{};a.Check(vkGetQueryPoolResults(a.device,a.regionalTimingQueries,slot*2,2,sizeof ticks,ticks,sizeof(uint64_t),VK_QUERY_RESULT_64_BIT),"regional physical GPU timing readback failed");
    double ms=double(ticks[1]-ticks[0])*a.timestampPeriodNanoseconds/1e6;
    a.regionalTimingRecorded[slot]=false;
    if(slot!=0u||ms>.01){char message[256];std::snprintf(message,sizeof message,"NCSM1 regional GPU work: kind=%s; ms=%.6f; frame=%llu",slot==0?"demand":slot==1?"currentPhysicalPreparation":"incomingPhysicalPreparation",ms,(unsigned long long)a.frame);a.Log(NC_LOG_ALWAYS,message);}
  }
  auto* demand=static_cast<RegionalDemandBuffer*>(a.regionalDemandMapped);
  for(uint32_t slot=0;slot<2;++slot){auto& job=a.regionalJobs[slot];if(job.phase==1){
    job.mask=demand->masks[slot];
    // Every slice may start I/O, but readiness requires the complete footprint.
    a.regionalPhysical->Require(job.mask);
    job.phase=job.cursor==job.frame.metadata[2]?2u:0u;
    if(job.phase!=2u)continue;
    uint32_t count=0;for(uint32_t word:job.mask)count+=std::popcount(word);
    char message[384];std::snprintf(message,sizeof message,"NCSM1 regional demand: incoming=%u; pupil=%u; generation=%llu; required=%u; anchoredPatches=%u; unresolved=%u; demandMs=%.3f",slot,job.frame.identity[0],(unsigned long long)job.generation,count,0u,a.regionalPhysical->Complete(job.mask)?0u:1u,std::chrono::duration<double,std::milli>(std::chrono::steady_clock::now()-job.started).count());a.Log(NC_LOG_ALWAYS,message);
  }}
}
void UpdateRegionalPhysical(App& a){
  using namespace nc::regionalphysical;
  PostContactTiming::Lap lap(a.postContactTiming.rows.empty()?nullptr:a.postContactTiming.regional.data());
  auto* frames=static_cast<NcProductionBillboardFrame*>(a.productionBillboardFrameMapped);
  const bool enabled=RegionalPhysicalEnabled(a);
  if(!a.regionalPhysical){
    if((a.submission->productionBillboardFlags&1u)&&a.submission->physicalSurfaceGeneration==4u&&
       a.submission->planetaryPresentation.bodyIdLow==6u&&a.productionBillboardIncomingGpuPreparation)
      throw std::runtime_error("NCSM1 physical preparation requires its authoritative regional dataset");
    return;
  }
  // Assets are immutable for this renderer lifetime. A different data identity
  // requires a fresh cache and full generation, not repair of published vertices.
  if(enabled&&!a.regionalPhysical->Matches(*a.localPack,a.submission->physicalSurfaceGeneration))
    throw std::runtime_error("NCSM1 regional physical identity changed; full renderer-generation replacement required");
  lap.Part(0);
  if(a.regionalPayloadBytes==4&&a.regionalPhysical->requests){
      VkDeviceSize bytes=VkDeviceSize(a.regionalPhysical->catalog.count)*nc::localterrain::R16Bytes;
      VkPhysicalDeviceProperties properties{};vkGetPhysicalDeviceProperties(a.physical,&properties);
      if(bytes>MaximumPayloadBytes||bytes>properties.limits.maxStorageBufferRange)throw std::runtime_error("regional physical payload exceeds bounded GPU storage range");
      DestroyHostBuffer(a,a.regionalPayloadBuffer,a.regionalPayloadMemory,a.regionalPayloadMapped);
      lap.Part(1);
      CreateHostBuffer(a,bytes,VK_BUFFER_USAGE_STORAGE_BUFFER_BIT,a.regionalPayloadBuffer,a.regionalPayloadMemory,a.regionalPayloadMapped,"regional physical payload allocation failed",nc::MappedBufferUse::Host,a.postContactTiming.rows.empty()?nullptr:a.postContactTiming.allocation.data());
      lap.Part(2);
      a.regionalPayloadBytes=bytes;RegionalDescriptor(a,54,a.regionalPayloadBuffer,bytes);
      lap.Part(3);
      a.regionalPhysical->BindPayload(a.regionalPayloadMapped,bytes);
      lap.Part(4);
  }
  a.regionalPhysical->PublishCompleted();
  lap.Part(5);
  auto catalog=a.regionalPhysical->catalog;catalog.physicalGeneration=enabled?4u:0u;
  catalog.entries[0].reserved=enabled&&std::getenv("NOVACORE_REGIONAL_PHYSICAL_PROBE")&&a.regionalPhysical->AllContributingResident()?1u:0u;
  std::memcpy(a.regionalCatalogMapped,&catalog,sizeof catalog);
  lap.Part(6);
  if(!enabled){UpdateRegionalPreparation(a);lap.Part(8);return;}
  auto* demand=static_cast<RegionalDemandBuffer*>(a.regionalDemandMapped);
  for(uint32_t slot=0;slot<2;++slot){
    bool exists=slot?a.productionBillboardIncomingEnabled:a.productionBillboardAuthoritative;
    auto& target=slot?frames->incoming:frames->current;auto& job=a.regionalJobs[slot];
    if(!exists||!target.metadata[0])continue;
    if(slot&&a.regionalPreparation[1].active&&a.regionalPreparation[1].generation==a.productionBillboardIncomingGeneration)
      target=static_cast<RegionalPreparationControl*>(a.regionalPreparationMapped)->incoming;
    uint64_t generation=slot?a.productionBillboardIncomingGeneration:a.productionBillboardGeneration;
    uint64_t topology=slot?a.productionBillboardIncomingTopologyHash:a.productionBillboardTopologyHash;
    bool allResident=a.regionalPhysical->AllContributingResident();
    bool ready=ResolveDependencyTarget(target,job,*demand,slot,generation,topology,allResident,
      a.productionBillboardPreparedFrameIdentity,[&](const auto& mask){return a.regionalPhysical->Complete(mask);});
    a.regionalReady[slot]=ready;
    if(ready&&job.frame.metadata[0]&&!job.logged){job.logged=true;double delay=std::chrono::duration<double,std::milli>(std::chrono::steady_clock::now()-job.started).count();a.regionalDependencyDelayMs=std::max(a.regionalDependencyDelayMs,delay);
      char message[448];std::snprintf(message,sizeof message,"NCSM1 regional ready: incoming=%u; pupil=%u; dependencyDelayMs=%.3f; requests=%llu; resident=%llu; uploadedBytes=%llu; complete=true; allContributing=%u; demandComplete=%u; gpuVisibility=hostBarrierBeforePrepare",slot,target.identity[0],delay,(unsigned long long)a.regionalPhysical->requests,(unsigned long long)a.regionalPhysical->loaded,(unsigned long long)a.regionalPhysical->uploadBytes,allResident?1u:0u,job.phase==2?1u:0u);a.Log(NC_LOG_ALWAYS,message);
    }
    if(!slot){
      // Preserve the exact last prepared pupil during preflight/acquisition.
      // A complete dependency snapshot may enter staged physical preparation;
      // UpdateRegionalPreparation keeps rendering the published pupil meanwhile.
      frames->previous=a.regionalPublishedPupil;
      if(!ready)frames->current=a.regionalPublishedPupil;
    }
  }
  lap.Part(7);
  UpdateRegionalPreparation(a);
  lap.Part(8);
}
void RecordRegionalPhysicalDemand(App& a,VkCommandBuffer command){
  if(!RegionalPhysicalEnabled(a))return;
  bool recorded=false;
  for(uint32_t slot=0;slot<2;++slot){auto& job=a.regionalJobs[slot];
    const bool exists=slot?a.productionBillboardIncomingEnabled:a.productionBillboardAuthoritative;
    if(!exists||a.regionalReady[slot]||job.phase!=0||!job.frame.metadata[0])continue;
    if(!recorded){vkCmdWriteTimestamp(command,VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,a.regionalTimingQueries,0);recorded=true;}
    vkCmdBindDescriptorSets(command,VK_PIPELINE_BIND_POINT_COMPUTE,a.pipelineLayout,0,1,&a.descriptor,0,nullptr);
    vkCmdBindPipeline(command,VK_PIPELINE_BIND_POINT_COMPUTE,slot?a.regionalIncomingDemandPipeline:a.regionalDemandPipeline);
    const uint32_t count=std::min(RegionalDemandVertexBudget,job.frame.metadata[2]-job.cursor);
    static_cast<RegionalDemandBuffer*>(a.regionalDemandMapped)->offsets[slot]=job.cursor;
    vkCmdDispatch(command,(count+63u)/64u,1,1);job.cursor+=count;job.phase=1;++a.regionalDemandDispatches;
  }
  if(recorded){vkCmdWriteTimestamp(command,VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,a.regionalTimingQueries,1);a.regionalTimingRecorded[0]=true;}
  VkMemoryBarrier barrier{VK_STRUCTURE_TYPE_MEMORY_BARRIER};barrier.srcAccessMask=VK_ACCESS_SHADER_WRITE_BIT;barrier.dstAccessMask=VK_ACCESS_HOST_READ_BIT;
  vkCmdPipelineBarrier(command,VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,VK_PIPELINE_STAGE_HOST_BIT,0,1,&barrier,0,nullptr,0,nullptr);
}
