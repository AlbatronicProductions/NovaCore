// Independent diagnostic readback storage. The worker never reads live buffers.
void CreateFrozenCapture(App& a){
  if(!a.causal.Active())return;
  wchar_t output[32768];auto count=GetEnvironmentVariableW(L"NOVACORE_FROZEN_CAPTURE",output,32768);
  if(!count)return;if(count>=32768)throw std::runtime_error("frozen output path too long");
  auto authority=[](const wchar_t* variable,const std::string& actual){
    wchar_t expected[32768];auto n=GetEnvironmentVariableW(variable,expected,32768);
    if(!n||n>=32768||_wcsicmp(std::filesystem::absolute(std::filesystem::path(expected)).lexically_normal().c_str(),
      std::filesystem::absolute(std::filesystem::u8path(actual)).lexically_normal().c_str()))throw std::runtime_error("frozen physical authority path differs from sealed manifest");
  };
  authority(L"NOVACORE_FROZEN_ASSET_PRODUCTION",a.productionTerrainPath);authority(L"NOVACORE_FROZEN_ASSET_REGIONAL",a.localTerrainPath);authority(L"NOVACORE_FROZEN_ASSET_ORACLE",a.elevationOraclePath);
  VkPhysicalDeviceProperties properties{};vkGetPhysicalDeviceProperties(a.physical,&properties);
  a.frozenDeviceIdentity={properties.vendorID,properties.deviceID,properties.driverVersion};
  if(properties.limits.maxStorageBufferRange<nc::frozen::SlotBytes||properties.limits.maxComputeWorkGroupInvocations<256||properties.limits.maxComputeWorkGroupSize[0]<256||properties.limits.maxComputeWorkGroupCount[0]<256)throw std::runtime_error("frozen pack device limits unsupported");
  VkQueryPoolCreateInfo timing{VK_STRUCTURE_TYPE_QUERY_POOL_CREATE_INFO};timing.queryType=VK_QUERY_TYPE_TIMESTAMP;timing.queryCount=2;
  a.Check(vkCreateQueryPool(a.device,&timing,nullptr,&a.frozenTimingQueries),"frozen timing query creation failed");
  for(size_t i=0;i<nc::frozen::SlotCount;++i){void* mapped=nullptr;
    CreateHostBuffer(a,nc::frozen::SlotBytes,VK_BUFFER_USAGE_TRANSFER_DST_BIT|VK_BUFFER_USAGE_STORAGE_BUFFER_BIT,a.frozenBuffers[i],a.frozenMemory[i],mapped,"frozen readback allocation failed",nc::MappedBufferUse::TerrainRequestKeys);
    a.frozen.slots[i].mapped=static_cast<uint8_t*>(mapped);
  }
  CreateFrozenPacking(a);a.frozen.Start(std::filesystem::path(output));
  timing.queryCount=uint32_t(nc::frozen::TopologyCount*2);a.Check(vkCreateQueryPool(a.device,&timing,nullptr,&a.frozenTopologyQueries),"topology timing query creation failed");
  for(const auto& resource:a.productionBillboardTopologyResources)AcquireFrozenTopology(a,resource);
  a.causal.Emit(Phase::FrozenCapture,Kind::Info,0,{1,nc::frozen::SlotCount,nc::frozen::SlotBytes,nc::frozen::CadenceMs});
}
void DestroyFrozenCapture(App& a){
  a.frozen.Stop();
  // A worker may first fail while Stop drains its final ready slot. Preserve
  // that exact storage boundary before releasing diagnostic resources.
  if(a.causal.Active()&&a.frozen.failure.load()==1){
    a.causal.Emit(Phase::FrozenCapture,Kind::Info,-7001,{9,a.frozen.failureIdentity.load(),a.frozen.failureFrame.load(),a.frozen.failureOperation.load(),a.frozen.failureNativeError.load()});
    a.causal.Text(Phase::FrozenCapture,-7001,a.frozen.FailureText());
  }
  DestroyFrozenTopologies(a);
  DestroyFrozenPacking(a);
  if(a.frozenTimingQueries){vkDestroyQueryPool(a.device,a.frozenTimingQueries,nullptr);a.frozenTimingQueries={};}
  for(size_t i=0;i<nc::frozen::SlotCount;++i){void* mapped=a.frozen.slots[i].mapped;
    DestroyHostBuffer(a,a.frozenBuffers[i],a.frozenMemory[i],mapped);a.frozen.slots[i].mapped=nullptr;
  }
  if(a.causal.Active())a.causal.Emit(Phase::FrozenCapture,Kind::Info,a.frozen.failure.load()?-7001:0,
    {4,a.frozen.lastDurableIdentity.load(),a.frozen.lastDurableFrame.load(),a.frozen.lastWriteMilliseconds.load(),a.frozen.failure.load()});
}
void BeginRecordingCallTrace(App& a,VkCommandBuffer command){
  if(a.recordingTrace.Claimed()||!a.causal.Active()||!a.frozen.Active()||!a.frozen.AdmissionsEnabled()||!a.anchoredPipelineStatisticsFrameSubmitted)return;
  a.recordingTrace.Start(a.causal,{uint64_t(command),a.productionBillboardGeneration,a.productionBillboardIncomingGeneration,
    a.productionBillboardTopologyHash,a.productionBillboardPreparedFrameIdentity,a.productionBillboardCullFrameIdentity,a.productionBillboardRasterFrameIdentity,
    a.productionBillboardVertexCount,a.productionBillboardTriangleCount,uint64_t(a.productionBillboardPhysicalBuffer),uint64_t(a.productionBillboardIndexBuffer),
    uint64_t(a.productionBillboardVisibilityBuffer),uint64_t(a.productionBillboardCompactedBuffer),uint64_t(a.productionBillboardCounterBuffer),uint64_t(a.productionBillboardIndirectBuffer),
    uint64_t(a.frozenBuffers[0]),uint64_t(a.frozenBuffers[1]),uint64_t(a.fence),a.frozen.slots[0].state.load(),a.frozen.slots[0].header[nc::frozen::Identity],
    a.frozen.slots[1].state.load(),a.frozen.slots[1].header[nc::frozen::Identity],a.causal.submissionSequence,uint64_t(a.frozenTimingQueries),
    uint64_t(a.productionBillboardFencePending),uint64_t(a.productionBillboardIncomingFencePending),uint64_t(a.productionBillboardAuthoritative),a.productionBillboardTopologyFamily});
}
nc::frozen::Header FrozenFrameAuthority(App& a){
  using namespace nc::frozen;using RO=nc::causal::RecordingOp;auto& trace=a.recordingTrace;
  Header h;trace.Call(RO::HeaderBegin,[&]{h.Begin();});LARGE_INTEGER now;trace.Call(RO::HeaderClock,[&]{QueryPerformanceCounter(&now);});
  trace.Enter(RO::AuthorityScalars,uint64_t(a.productionBillboardFrameBuffer),uint64_t(a.productionBillboardFrameMapped),sizeof(NcProductionBillboardFrame),uint64_t(a.productionBillboardFrameMemory));
  using AS=nc::causal::AuthorityScalar;
  trace.Scalar(AS::Frame,nc::frozen::Frame,[&]{h[nc::frozen::Frame]=a.causal.frame.load();});
  trace.Scalar(AS::RecordQpc,nc::frozen::RecordQpc,[&]{h[nc::frozen::RecordQpc]=now.QuadPart;});
  trace.Scalar(AS::Swap,nc::frozen::Swap,[&]{h[nc::frozen::Swap]=a.causal.swap.load();});
  trace.Scalar(AS::Generation,nc::frozen::Generation,[&]{h[nc::frozen::Generation]=a.productionBillboardGeneration;});
  trace.Scalar(AS::IncomingGeneration,nc::frozen::IncomingGeneration,[&]{h[nc::frozen::IncomingGeneration]=a.productionBillboardIncomingGeneration;});
  trace.Scalar(AS::TopologyHash,nc::frozen::TopologyHash,[&]{h[nc::frozen::TopologyHash]=a.productionBillboardTopologyHash;});
  trace.Scalar(AS::TopologyFamily,nc::frozen::TopologyFamily,[&]{h[nc::frozen::TopologyFamily]=a.productionBillboardTopologyFamily;});
  trace.Scalar(AS::PreparedPupil,nc::frozen::PreparedPupil,[&]{h[nc::frozen::PreparedPupil]=a.productionBillboardPreparedFrameIdentity;});
  trace.Scalar(AS::CullPupil,nc::frozen::CullPupil,[&]{h[nc::frozen::CullPupil]=a.productionBillboardCullFrameIdentity;});
  trace.Scalar(AS::RasterPupil,nc::frozen::RasterPupil,[&]{h[nc::frozen::RasterPupil]=a.productionBillboardRasterFrameIdentity;});
  trace.Scalar(AS::Vertices,nc::frozen::Vertices,[&]{h[nc::frozen::Vertices]=a.productionBillboardVertexCount;});
  trace.Scalar(AS::Triangles,nc::frozen::Triangles,[&]{h[nc::frozen::Triangles]=a.productionBillboardTriangleCount;});
  trace.Scalar(AS::Draws,nc::frozen::Draws,[&]{h[nc::frozen::Draws]=a.causal.draws;});
  trace.Scalar(AS::Dispatches,nc::frozen::Dispatches,[&]{h[nc::frozen::Dispatches]=a.causal.dispatches;});
  trace.Scalar(AS::Groups,nc::frozen::Groups,[&]{h[nc::frozen::Groups]=a.causal.groups;});
  trace.Scalar(AS::Width,nc::frozen::Width,[&]{h[nc::frozen::Width]=a.extent.width;});
  trace.Scalar(AS::Height,nc::frozen::Height,[&]{h[nc::frozen::Height]=a.extent.height;});
  trace.Scalar(AS::PhysicalGeneration,nc::frozen::PhysicalGeneration,[&]{h[nc::frozen::PhysicalGeneration]=a.submission->physicalSurfaceGeneration;});
  trace.Scalar(AS::TerrainVersion,nc::frozen::TerrainVersion,[&]{h[nc::frozen::TerrainVersion]=a.submission->planetaryGpu.terrainVersion;});
  trace.Scalar(AS::IncomingVertices,nc::frozen::IncomingVertices,[&]{h[nc::frozen::IncomingVertices]=a.productionBillboardIncomingVertexCount;});
  trace.Scalar(AS::IncomingTriangles,nc::frozen::IncomingTriangles,[&]{h[nc::frozen::IncomingTriangles]=a.productionBillboardIncomingTriangleCount;});
  trace.Scalar(AS::IncomingTopologyHash,nc::frozen::IncomingTopologyHash,[&]{h[nc::frozen::IncomingTopologyHash]=a.productionBillboardIncomingTopologyHash;});
  trace.Scalar(AS::Flags,nc::frozen::Flags,[&]{h[nc::frozen::Flags]=a.submission->productionBillboardFlags;});
  trace.Returned(RO::AuthorityScalars);
  trace.Call(RO::IncomingPupilRead,[&]{h[IncomingPreparedPupil]=static_cast<const NcProductionBillboardFrame*>(a.productionBillboardFrameMapped)->incoming.identity[0];},
    uint64_t(a.productionBillboardFrameBuffer),uint64_t(a.productionBillboardFrameMapped),sizeof(NcProductionBillboardFrame),uint64_t(a.productionBillboardFrameMemory));
  trace.Enter(RO::AuthorityResources);
  h[PhysicalHandle]=uint64_t(a.productionBillboardPhysicalBuffer);h[IndexHandle]=uint64_t(a.productionBillboardIndexBuffer);
  h[VisibilityHandle]=uint64_t(a.productionBillboardVisibilityBuffer);h[CompactedHandle]=uint64_t(a.productionBillboardCompactedBuffer);
  h[CounterHandle]=uint64_t(a.productionBillboardCounterBuffer);h[IndirectHandle]=uint64_t(a.productionBillboardIndirectBuffer);
  h[VertexCapacity]=a.productionBillboardVertexCapacity;h[TriangleCapacity]=a.productionBillboardTriangleCapacity;
  h[ResidencyBytes]=a.productionBillboardTopologyResidentBytes;h[WorkingBytes]=a.productionBillboardWorkingBytes;
  h[SurfaceMode]=a.submission->planetarySurfaceMode;
  for(const auto& resource:a.productionBillboardTopologyResources)if(resource.index==a.productionBillboardIndexBuffer)h[Reserved55]=resource.indexBirth;
  h[Reserved60]=a.frozenDeviceIdentity[0];h[Reserved61]=a.frozenDeviceIdentity[1];h[Reserved62]=a.frozenDeviceIdentity[2];
  trace.Returned(RO::AuthorityResources);
  trace.Call(RO::BulkPhysical,[&]{h.Bulk(Physical,h[Vertices]*sizeof(NcSphericalBillboardPhysicalVertex),64);});
  trace.Call(RO::BulkIndices,[&]{h.Bulk(Indices,h[Triangles]*12,4);});
  trace.Call(RO::BulkVisibility,[&]{h.Bulk(Visibility,h[Triangles]*4,4);});
  trace.Call(RO::BulkCompacted,[&]{h.Bulk(Compacted,h[Triangles]*12,4);});
  trace.Call(RO::BulkCounters,[&]{h.Bulk(Counters,ProductionBillboardCounterCount*4,4);});
  trace.Call(RO::BulkIndirect,[&]{h.Bulk(Indirect,sizeof(VkDrawIndexedIndirectCommand),4);});
  trace.Call(RO::InputCamera,[&]{h.Input(Camera,a.mapped,sizeof(NcCameraData));},uint64_t(a.submissionBuffer),uint64_t(a.mapped),sizeof(NcCameraData),uint64_t(a.submissionMemory));
  trace.Call(RO::InputGpu,[&]{h.Input(GpuInput,a.gpuInputMapped,sizeof(NcPlanetaryGpuConstants));},uint64_t(a.gpuInputBuffer),uint64_t(a.gpuInputMapped),sizeof(NcPlanetaryGpuConstants),uint64_t(a.gpuInputMemory));
  trace.Call(RO::InputPresentation,[&]{h.Input(Presentation,a.planetaryPresentationMapped,sizeof(NcPlanetaryPresentation));},uint64_t(a.planetaryPresentationBuffer),uint64_t(a.planetaryPresentationMapped),sizeof(NcPlanetaryPresentation),uint64_t(a.planetaryPresentationMemory));
  trace.Call(RO::InputPupils,[&]{h.Input(Pupils,a.productionBillboardFrameMapped,sizeof(NcProductionBillboardFrame));},uint64_t(a.productionBillboardFrameBuffer),uint64_t(a.productionBillboardFrameMapped),sizeof(NcProductionBillboardFrame),uint64_t(a.productionBillboardFrameMemory));
  trace.Call(RO::InputPreparation,[&]{h.Input(Preparation,a.regionalPreparationMapped,sizeof(RegionalPreparationControl));},uint64_t(a.regionalPreparationBuffer),uint64_t(a.regionalPreparationMapped),sizeof(RegionalPreparationControl),uint64_t(a.regionalPreparationMemory));
  trace.Call(RO::InputPublishedPupil,[&]{h.Input(PublishedPupil,&a.regionalPublishedPupil,sizeof(a.regionalPublishedPupil));},0,uint64_t(&a.regionalPublishedPupil),sizeof(a.regionalPublishedPupil));
  trace.Enter(RO::ResidencyScalars);
  const uint64_t residency[]{a.regionalScratchCapacity,a.regionalPayloadBytes,a.regionalPreparation[0].cursor,a.regionalPreparation[1].cursor,
    a.regionalPreparation[0].generation,a.regionalPreparation[1].generation,uint64_t(a.regionalReady[0]),uint64_t(a.regionalReady[1]),
    a.productionBillboardTopologyResources.size(),a.productionBillboardTopologyResidentBytes,a.productionBillboardTopologyPeakResidentBytes,
    a.productionBillboardWorkAllocations,a.productionBillboardWorkReuses,a.productionBillboardWorkingBytes,a.productionBillboardPeakWorkingBytes,
    a.productionBillboardSpareVertexCapacity,a.productionBillboardSpareTriangleCapacity,uint64_t(a.productionBillboardIncomingPhysicalBuffer),
    uint64_t(a.productionBillboardSparePhysicalBuffer),uint64_t(a.regionalScratchBuffer),a.frozen.lastDurableIdentity.load(),a.frozen.lastDurableFrame.load(),
    a.regionalJobs[0].generation,a.regionalJobs[1].generation,a.regionalJobs[0].topology,a.regionalJobs[1].topology,
    a.regionalJobs[0].phase,a.regionalJobs[1].phase,a.regionalJobs[0].cursor,a.regionalJobs[1].cursor};
  trace.Returned(RO::ResidencyScalars);
  trace.Call(RO::InputResidency,[&]{h.Input(Residency,residency,sizeof(residency));},0,uint64_t(residency),sizeof(residency));
  trace.Enter(RO::CatalogScan,uint64_t(a.regionalCatalogBuffer),uint64_t(a.regionalCatalogMapped),sizeof(nc::regionalphysical::Catalog),uint64_t(a.regionalCatalogMemory));
  std::array<uint32_t,32> catalogState{};const auto& catalog=*static_cast<const nc::regionalphysical::Catalog*>(a.regionalCatalogMapped);
  catalogState[0]=catalog.count;catalogState[1]=catalog.physicalGeneration;catalogState[2]=catalog.terrainVersion;
  if(catalog.count>nc::regionalphysical::MaximumRecords)throw std::runtime_error("frozen regional catalog capacity");
  for(uint32_t i=0;i<catalog.count;++i)if(catalog.entries[i].resident){catalogState[4+i/32]|=1u<<(i%32);++catalogState[3];}
  trace.Returned(RO::CatalogScan);
  trace.Call(RO::InputCatalog,[&]{h.Input(CatalogResidency,catalogState.data(),sizeof(catalogState));},0,uint64_t(catalogState.data()),sizeof(catalogState));
  trace.Call(RO::InputDemand,[&]{h.Input(DemandInputs,a.regionalDemandMapped,sizeof(RegionalDemandBuffer));},uint64_t(a.regionalDemandBuffer),uint64_t(a.regionalDemandMapped),sizeof(RegionalDemandBuffer),uint64_t(a.regionalDemandMemory));return h;
}
void RecordFrozenCapture(App& a,VkCommandBuffer command){
  if(!a.frozen.Active()||!a.anchoredPipelineStatisticsFrameSubmitted)return;
  if(!FrozenAdmissionsEnabled(a)){
    PollFrozenDrain(a);
    a.causal.Emit(Phase::FrozenCapture,Kind::Info,a.frozen.failure.load()?-7001:0,{5,a.frozen.lastDurableIdentity.load(),a.frozen.lastDurableFrame.load(),a.frozen.lastWriteMilliseconds.load(),a.frozen.failure.load(),a.frozen.lastWriteMicroseconds.load(),a.frozen.lastWriteCpu100ns.load()});
    return;
  }
  using RO=nc::causal::RecordingOp;auto& trace=a.recordingTrace;
  LARGE_INTEGER cpuStart,cpuEnd,frequency;trace.Call(RO::CaptureClockStart,[&]{QueryPerformanceCounter(&cpuStart);});trace.Call(RO::CaptureClockFrequency,[&]{QueryPerformanceFrequency(&frequency);});
  auto header=FrozenFrameAuthority(a);auto now=trace.Call(RO::ReserveClock,[&]{return GetTickCount64();});
  auto* slot=trace.Call(RO::ReserveSlot,[&]{return a.frozen.Reserve(now);});
  if(trace.Active())trace.Slots(a.frozen.slots[0].state.load(),a.frozen.slots[0].header[nc::frozen::Identity],a.frozen.slots[1].state.load(),a.frozen.slots[1].header[nc::frozen::Identity]);
  if(slot){trace.Enter(RO::SlotHeader);auto id=slot->header[nc::frozen::Identity];slot->header=header;slot->header[nc::frozen::Identity]=id;slot->header[nc::frozen::HeaderFlags]=1;
    slot->header[nc::frozen::CaptureTransport]=4;
    slot->topology=&a.frozen.PinTopology(slot->header);
    const bool reuseIndices=true;
    slot->header[nc::frozen::IndexReadbackFrame]=slot->topology->authority[nc::frozen::TFrame];
    // Actual byte count is GPU-produced, unavailable until this submission completes.
    slot->header[nc::frozen::GpuCopiedBytes]=0;
    header=slot->header;trace.Returned(RO::SlotHeader);CausalGpuLabel label(a,command,"Diagnostic lossless packing + readback (included in frame cost)",true);
    trace.Call(RO::ResetQueries,[&]{vkCmdResetQueryPool(command,a.frozenTimingQueries,0,2);},uint64_t(a.frozenTimingQueries),0,2);
    trace.Call(RO::TimestampBefore,[&]{vkCmdWriteTimestamp(command,VK_PIPELINE_STAGE_BOTTOM_OF_PIPE_BIT,a.frozenTimingQueries,0);},uint64_t(a.frozenTimingQueries),0);
    const auto index=size_t(slot-a.frozen.slots.data());
    VkMemoryBarrier before{VK_STRUCTURE_TYPE_MEMORY_BARRIER};before.srcAccessMask=VK_ACCESS_SHADER_WRITE_BIT|VK_ACCESS_HOST_WRITE_BIT|VK_ACCESS_TRANSFER_WRITE_BIT;
    before.dstAccessMask=VK_ACCESS_SHADER_READ_BIT|VK_ACCESS_SHADER_WRITE_BIT;
    trace.Call(RO::BarrierBefore,[&]{vkCmdFillBuffer(command,a.frozenPackMetadataBuffers[index],0,nc::frozen::PackMetadataBytes,0);vkCmdPipelineBarrier(command,VK_PIPELINE_STAGE_ALL_COMMANDS_BIT|VK_PIPELINE_STAGE_HOST_BIT,VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,0,1,&before,0,nullptr,0,nullptr);});
    const VkBuffer sources[]{a.productionBillboardPhysicalBuffer,a.productionBillboardIndexBuffer,a.productionBillboardVisibilityBuffer,
      a.productionBillboardCompactedBuffer,a.productionBillboardCounterBuffer,a.productionBillboardIndirectBuffer};
    for(size_t i=0;i<6;++i){const auto& section=header.sections[i];if(!section.bytes)continue;
      VkBufferCopy copy{0,section.offset-nc::frozen::HeaderBytes,section.bytes};
      // Preserve the outer interruption identities. One raw-integer diagnostic
      // dispatch transports all changing evidence; topology retains its own GPU witness.
      trace.Call(RO(uint64_t(RO::CopyPhysical)+i),[&]{if(i==0)RecordFrozenPacking(a,command,index,header);},uint64_t(sources[i]),uint64_t(a.frozenBuffers[index]),copy.dstOffset,i==1?0:copy.size);
    }
    VkMemoryBarrier after{VK_STRUCTURE_TYPE_MEMORY_BARRIER};after.srcAccessMask=VK_ACCESS_SHADER_WRITE_BIT;after.dstAccessMask=VK_ACCESS_HOST_READ_BIT;
    trace.Call(RO::BarrierAfter,[&]{vkCmdPipelineBarrier(command,VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,VK_PIPELINE_STAGE_HOST_BIT,0,1,&after,0,nullptr,0,nullptr);});
    trace.Call(RO::TimestampAfter,[&]{vkCmdWriteTimestamp(command,VK_PIPELINE_STAGE_BOTTOM_OF_PIPE_BIT,a.frozenTimingQueries,1);},uint64_t(a.frozenTimingQueries),1);
    trace.Call(RO::ScheduledMarker,[&]{a.causal.Emit(Phase::FrozenCapture,Kind::Info,0,{2,id,header[nc::frozen::Frame],header[nc::frozen::PayloadBytes],header[nc::frozen::Generation],header[nc::frozen::PreparedPupil]});});
  }
  trace.Call(RO::AuthorityMarker,[&]{a.causal.Bytes(Phase::FrameAuthority,header[nc::frozen::Frame],&header,sizeof(header));});
  trace.Call(RO::DurableMarker,[&]{a.causal.Emit(Phase::FrozenCapture,Kind::Info,a.frozen.failure.load()?-7001:0,
    {5,a.frozen.lastDurableIdentity.load(),a.frozen.lastDurableFrame.load(),a.frozen.lastWriteMilliseconds.load(),a.frozen.failure.load(),a.frozen.lastWriteMicroseconds.load(),a.frozen.lastWriteCpu100ns.load()});});
  trace.Call(RO::CaptureClockEnd,[&]{QueryPerformanceCounter(&cpuEnd);});
  trace.Call(RO::CostMarker,[&]{a.causal.Emit(Phase::FrozenCapture,Kind::Info,0,{7,header[nc::frozen::Frame],header[nc::frozen::Identity],nc::causal::Recorder::Bits(double(cpuEnd.QuadPart-cpuStart.QuadPart)*1000/double(frequency.QuadPart))});});
}
void CompleteFrozenCapture(App& a){
  if(!a.frozen.Active())return;
  CompleteFrozenTopologies(a);
  using namespace nc::frozen;
  for(auto& slot:a.frozen.slots)if(slot.state.load(std::memory_order_acquire)==InFlight){
    LARGE_INTEGER cpuStart,cpuEnd,frequency;QueryPerformanceCounter(&cpuStart);QueryPerformanceFrequency(&frequency);
    auto& h=slot.header;
    if(h[Frame]!=a.causal.completed.load()||h[Frame]!=a.anchoredPipelineStatisticsTerrainFrame||h[Generation]!=a.productionBillboardGeneration){a.frozen.failure=3;continue;}
    std::array<uint64_t,4> query{};
    const auto result=vkGetQueryPoolResults(a.device,a.anchoredPipelineStatistics,0,1,sizeof(query),query.data(),sizeof(query),VK_QUERY_RESULT_64_BIT);
    if(result!=VK_SUCCESS){a.frozen.failure=4;continue;}
    std::array<uint64_t,2> ticks{};
    const auto timing=vkGetQueryPoolResults(a.device,a.frozenTimingQueries,0,2,sizeof(ticks),ticks.data(),sizeof(uint64_t),VK_QUERY_RESULT_64_BIT);
    if(timing!=VK_SUCCESS||ticks[1]<ticks[0]){a.frozen.failure=5;continue;}
    // Existing completed-frame ownership proves these GPU copies are ready.
    // No CPU read of the live terrain buffers occurs here or on the worker.
    if(h[CaptureTransport]){
      if(h[CaptureTransport]!=4||!slot.topology||!slot.topology->Matches(h)||h[CompactedHandle]!=uint64_t(a.productionBillboardCompactedBuffer)||h[IndexHandle]!=uint64_t(a.productionBillboardIndexBuffer)||h[PhysicalHandle]!=uint64_t(a.productionBillboardPhysicalBuffer)||h[VisibilityHandle]!=uint64_t(a.productionBillboardVisibilityBuffer)||h[CounterHandle]!=uint64_t(a.productionBillboardCounterBuffer)||h[IndirectHandle]!=uint64_t(a.productionBillboardIndirectBuffer)||h[Vertices]!=a.productionBillboardVertexCount||h[Triangles]!=a.productionBillboardTriangleCount){a.frozen.failure=6;continue;}
      const auto index=size_t(&slot-a.frozen.slots.data());auto* metadata=static_cast<const uint32_t*>(a.frozenPackMetadataMapped[index]);
      const bool packed=CompletePacking(h,slot.mapped,metadata);
      a.causal.Emit(Phase::FrozenCapture,Kind::Info,packed?0:-7001,{10,h[Identity],h[Frame],index,uint64_t(a.frozenPackMetadataBuffers[index]),metadata[0],metadata[1],metadata[2],metadata[3],metadata[4],metadata[5],metadata[6],metadata[7],metadata[8],metadata[9],metadata[10],metadata[11],metadata[12],metadata[13],metadata[14],metadata[15]});
      if(!packed){a.frozen.failure=7;continue;}
      const auto bytes=CompactedPrefixBytes(h,slot.mapped);const auto& section=h.sections[3];
      if(section.type!=Compacted||bytes>section.bytes||h[Triangles]>a.productionBillboardTriangleCapacity||section.offset<HeaderBytes||section.offset-HeaderBytes>SlotBytes-bytes){a.frozen.failure=6;continue;}
      // The immutable topology pin remains owned through worker publication.
      a.causal.Emit(Phase::FrozenCapture,Kind::Info,0,{8,h[Identity],h[Frame],h[GpuCopiedBytes],0,h[IndexReadbackFrame],h[CompactedHandle],h[IndexHandle],size_t(&slot-a.frozen.slots.data())});
    }
    h[Clipping]=query[0];h[Fragments]=query[1];h[Tcs]=query[2];h[Tes]=query[3];
    LARGE_INTEGER now;QueryPerformanceCounter(&now);h[CompleteQpc]=now.QuadPart;h[Completed]=a.causal.completed.load();h[CompletedSubmission]=h[Submission];
    a.causal.Emit(Phase::FrozenCapture,Kind::Info,0,{3,h[Identity],h[Frame],h[Submission],h[Generation],h[PreparedPupil],h[Tcs],h[Tes]});
    QueryPerformanceCounter(&cpuEnd);
    a.causal.Emit(Phase::FrozenCapture,Kind::Info,0,{6,h[Identity],h[Frame],h[PayloadBytes],nc::causal::Recorder::Bits(double(ticks[1]-ticks[0])*a.timestampPeriodNanoseconds/1e6),nc::causal::Recorder::Bits(double(cpuEnd.QuadPart-cpuStart.QuadPart)*1000/double(frequency.QuadPart))});
    slot.state.store(Ready,std::memory_order_release);
  }
}
