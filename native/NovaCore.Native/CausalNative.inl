// Diagnostic-only observation of the existing renderer. No alternate renderer.
using nc::causal::Phase;
using nc::causal::Kind;
struct CausalScope {
  App& app;Phase phase;int exceptions=std::uncaught_exceptions();
  CausalScope(App& a,Phase p):app(a),phase(p){a.causal.Emit(p,Kind::Begin);}
  ~CausalScope(){app.causal.Emit(phase,Kind::End,std::uncaught_exceptions()>exceptions?INT64_MIN:0);}
};
struct CausalGpuLabel {
  App& app;VkCommandBuffer command;bool trace;
  CausalGpuLabel(App& a,VkCommandBuffer c,const char* text,bool recordingTrace=false):app(a),command(c),trace(recordingTrace){
    if(a.causal.Active()&&a.beginLabel){VkDebugUtilsLabelEXT label{VK_STRUCTURE_TYPE_DEBUG_UTILS_LABEL_EXT};label.pLabelName=text;label.color[1]=.6f;label.color[3]=1;
      if(trace)a.recordingTrace.Call(nc::causal::RecordingOp::CaptureLabelBegin,[&]{a.beginLabel(c,&label);});else a.beginLabel(c,&label);}}
  ~CausalGpuLabel(){if(app.causal.Active()&&app.endLabel){
    if(trace)app.recordingTrace.Call(nc::causal::RecordingOp::CaptureLabelEnd,[&]{app.endLabel(command);});else app.endLabel(command);}}
};
void CausalFault(App& a,VkResult failure) {
  if(!a.causal.Active())return;
  a.causal.Emit(Phase::Fault,Kind::Info,failure);
  if(failure!=VK_ERROR_DEVICE_LOST||a.faultCollected)return;
  a.faultCollected=true;
  if(!a.getFault){a.causal.Text(Phase::Fault,0,"VK_EXT_device_fault unavailable");return;}
  a.causal.Emit(Phase::Fault,Kind::Begin);
  VkDeviceFaultCountsEXT counts{VK_STRUCTURE_TYPE_DEVICE_FAULT_COUNTS_EXT};
  VkResult result=a.getFault(a.device,&counts,nullptr);
  a.causal.Emit(Phase::Fault,Kind::Info,result,{counts.addressInfoCount,counts.vendorInfoCount,counts.vendorBinarySize});
  if(result!=VK_SUCCESS){a.causal.Emit(Phase::Fault,Kind::End,result);return;}
  std::array<VkDeviceFaultAddressInfoEXT,64> addresses{};
  std::array<VkDeviceFaultVendorInfoEXT,64> vendors{};
  counts.addressInfoCount=std::min(counts.addressInfoCount,64u);counts.vendorInfoCount=std::min(counts.vendorInfoCount,64u);counts.vendorBinarySize=0;
  VkDeviceFaultInfoEXT info{VK_STRUCTURE_TYPE_DEVICE_FAULT_INFO_EXT};info.pAddressInfos=addresses.data();info.pVendorInfos=vendors.data();
  result=a.getFault(a.device,&counts,&info);a.causal.Text(Phase::Fault,result,info.description);
  for(uint32_t i=0;i<std::min(counts.addressInfoCount,64u);i++)a.causal.Emit(Phase::Fault,Kind::Info,0,{1,i,uint64_t(addresses[i].addressType),addresses[i].reportedAddress,addresses[i].addressPrecision});
  for(uint32_t i=0;i<std::min(counts.vendorInfoCount,64u);i++){a.causal.Emit(Phase::Fault,Kind::Info,0,{2,i,vendors[i].vendorFaultCode,vendors[i].vendorFaultData});a.causal.Text(Phase::Fault,0,vendors[i].description);}
  a.causal.Emit(Phase::Fault,Kind::End,result);
}
void CausalDeviceOptions(App& a,std::vector<const char*>& extensions,VkDeviceCreateInfo& create,VkPhysicalDeviceFaultFeaturesEXT& fault) {
  if(!a.causal.Active())return;
  uint32_t count=0;vkEnumerateDeviceExtensionProperties(a.physical,nullptr,&count,nullptr);
  std::vector<VkExtensionProperties> properties(count);vkEnumerateDeviceExtensionProperties(a.physical,nullptr,&count,properties.data());
  auto supports=[&](const char* name){return std::any_of(properties.begin(),properties.end(),[&](const auto& p){return std::strcmp(name,p.extensionName)==0;});};
  a.memoryBudget=supports(VK_EXT_MEMORY_BUDGET_EXTENSION_NAME);
  if(a.memoryBudget)extensions.push_back(VK_EXT_MEMORY_BUDGET_EXTENSION_NAME);
  if(supports(VK_EXT_DEVICE_FAULT_EXTENSION_NAME)){
    auto getFeatures=(PFN_vkGetPhysicalDeviceFeatures2)vkGetInstanceProcAddr(a.instance,"vkGetPhysicalDeviceFeatures2");
    if(getFeatures){VkPhysicalDeviceFeatures2 features{VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_FEATURES_2};features.pNext=&fault;getFeatures(a.physical,&features);
      if(fault.deviceFault){fault.deviceFaultVendorBinary=VK_FALSE;extensions.push_back(VK_EXT_DEVICE_FAULT_EXTENSION_NAME);create.pNext=&fault;}}
  }
  a.causal.Emit(Phase::Capabilities,Kind::Info,0,{uint64_t(a.memoryBudget),uint64_t(fault.deviceFault),uint64_t(fault.deviceFaultVendorBinary)});
}
void CausalBudget(App& a){
  if(!a.causal.Active()||GetTickCount64()<a.nextBudgetSample)return;
  a.nextBudgetSample=GetTickCount64()+1000;
  a.causal.Emit(Phase::Capabilities,Kind::Info,0,{uint64_t(a.memoryBudget),uint64_t(a.getFault!=nullptr),0});
  if(!a.memoryBudget)return;
  auto fn=(PFN_vkGetPhysicalDeviceMemoryProperties2)vkGetInstanceProcAddr(a.instance,"vkGetPhysicalDeviceMemoryProperties2");
  if(!fn){a.memoryBudget=false;a.causal.Text(Phase::Capabilities,0,"memory budget query unavailable");return;}
  CausalScope scope(a,Phase::MemoryBudget);
  VkPhysicalDeviceMemoryBudgetPropertiesEXT budget{VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_MEMORY_BUDGET_PROPERTIES_EXT};
  VkPhysicalDeviceMemoryProperties2 properties{VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_MEMORY_PROPERTIES_2};properties.pNext=&budget;fn(a.physical,&properties);
  for(uint32_t heap=0;heap<properties.memoryProperties.memoryHeapCount;heap++)
    a.causal.Emit(Phase::MemoryBudget,Kind::Info,0,{heap,budget.heapUsage[heap],budget.heapBudget[heap],properties.memoryProperties.memoryHeaps[heap].size});
}
void CausalSnapshot(App& a,bool gpuCompleted){
  if(!a.causal.Active())return;
  const auto& s=*a.submission;const auto& g=s.planetaryGpu;auto bits=nc::causal::Recorder::Bits;
  const auto* counters=gpuCompleted&&a.causal.completed.load()!=0&&a.productionBillboardCounterMapped?static_cast<const uint32_t*>(a.productionBillboardCounterMapped):nullptr;
  const auto* frame=static_cast<const NcProductionBillboardFrame*>(a.productionBillboardFrameMapped);
  // Small continuous authority survives deliberate heavyweight admission stop.
  a.causal.Emit(Phase::Snapshot,Kind::Info,0,{8,a.productionBillboardPreparedFrameIdentity,a.productionBillboardCullFrameIdentity,a.productionBillboardRasterFrameIdentity,
    a.productionBillboardGeneration,a.productionBillboardIncomingGeneration,a.productionBillboardTopologyHash,a.productionBillboardTopologyFamily,
    frame?frame->incoming.identity[0]:0,a.regionalPreparation[0].generation,a.regionalPreparation[0].cursor,a.regionalPreparation[1].generation,a.regionalPreparation[1].cursor});
  a.causal.Emit(Phase::Snapshot,Kind::Info,0,{uint64_t(gpuCompleted),a.extent.width,a.extent.height,uint64_t(s.planetarySurfaceMode),
    bits(double(g.cameraBodyHighX)+g.cameraBodyLowX),bits(double(g.cameraBodyHighY)+g.cameraBodyLowY),bits(double(g.cameraBodyHighZ)+g.cameraBodyLowZ),
    bits(g.surfaceAltitudeMetres),bits(g.viewForwardX),bits(g.viewForwardY),bits(g.viewForwardZ),g.maximumLevel,s.planetaryPatchCount,
    frame?frame->current.identity[2]:UINT32_MAX,a.productionBillboardTriangleCount,counters?counters[0]:0,counters?counters[23]:0,
    a.productionBillboardVertexCount,a.productionBillboardGeneration,a.productionBillboardIncomingGeneration,
    uint64_t(a.productionBillboardPhysicalBuffer),uint64_t(a.productionBillboardIncomingPhysicalBuffer),uint64_t(a.productionBillboardSparePhysicalBuffer),
    a.productionBillboardVertexCapacity,a.productionBillboardTriangleCapacity,a.productionBillboardIncomingVertexCapacity,a.productionBillboardIncomingTriangleCapacity,
    a.causal.draws,a.causal.dispatches,a.causal.groups,bits(a.lastGpuTimingMs[0]),a.lastGpuTimingFrame,
    a.anchoredTessellationControlPatches,a.anchoredTessellationEvaluationInvocations,a.anchoredClippingPrimitives,a.anchoredFragmentShaderInvocations,
    uint64_t(a.productionBillboardAuthoritative),uint64_t(a.productionBillboardFencePending),uint64_t(a.productionBillboardIncomingFencePending),
    uint64_t(s.planetaryPresentation.bodyIdLow)|(uint64_t(s.planetaryPresentation.bodyIdHigh)<<32)});
  std::array<uint32_t,32> levels{};
  for(uint32_t i=0;i<s.planetaryPatchCount;i++)if(s.planetaryPatches[i].level<levels.size())levels[s.planetaryPatches[i].level]++;
  for(uint32_t i=0;i<levels.size();i++)if(levels[i])a.causal.Emit(Phase::Snapshot,Kind::Info,0,{2,i,levels[i]});
  if(counters&&frame)a.causal.Emit(Phase::Snapshot,Kind::Info,0,{3,frame->current.identity[2],counters[0],counters[5],counters[8],counters[1],counters[3],counters[2]});
  a.causal.Emit(Phase::Snapshot,Kind::Info,0,{4,a.productionBillboardTopologyResources.size(),
    a.productionBillboardTopologyResidentBytes,a.productionBillboardTopologyPeakResidentBytes,
    a.productionBillboardWorkAllocations,a.productionBillboardWorkReuses,a.productionBillboardWorkingBytes,a.productionBillboardPeakWorkingBytes,
    a.productionBillboardSpareVertexCapacity,a.productionBillboardSpareTriangleCapacity,a.regionalScratchCapacity,uint64_t(a.regionalScratchBuffer),a.regionalPayloadBytes,
    a.regionalPreparation[0].cursor,a.regionalPreparation[1].cursor,uint64_t(counters!=nullptr),uint64_t(a.anchoredPipelineStatisticsFrameSubmitted),
    a.productionBillboardTopologyUploads,a.productionBillboardTopologyReuseHits});
  if(counters)a.causal.Emit(Phase::Snapshot,Kind::Info,0,{5,counters[4],counters[23],counters[24],counters[25],counters[26],counters[27],counters[28],counters[29],counters[30],counters[31],counters[33],counters[34]});
}

// Allocation / buffer handle lifetimes and draw/dispatch submission are observed
// at the real Vulkan call sites. Resource event serial is the creation identity;
// a recycled Vulkan handle is not mistaken for the old resource incarnation.
VkResult CausalAllocate(VkDevice d,const VkMemoryAllocateInfo* p,const VkAllocationCallbacks* c,VkDeviceMemory* out){auto r=vkAllocateMemory(d,p,c,out);if(gApp)gApp->causal.Emit(Phase::Resource,Kind::Info,r,{1,r==VK_SUCCESS?uint64_t(*out):0,p->allocationSize,p->memoryTypeIndex});return r;}
void CausalFree(VkDevice d,VkDeviceMemory m,const VkAllocationCallbacks* c){if(gApp)gApp->causal.Emit(Phase::Resource,Kind::Info,0,{2,uint64_t(m)});vkFreeMemory(d,m,c);}
VkResult CausalBuffer(VkDevice d,const VkBufferCreateInfo* p,const VkAllocationCallbacks* c,VkBuffer* out){auto r=vkCreateBuffer(d,p,c,out);if(gApp){gApp->causal.Emit(Phase::Resource,Kind::Info,r,{3,r==VK_SUCCESS?uint64_t(*out):0,p->size,p->usage});gApp->causalBufferBirth=r==VK_SUCCESS?gApp->causal.LastSerial():0;}return r;}
void CausalDestroyBuffer(VkDevice d,VkBuffer b,const VkAllocationCallbacks* c){if(gApp)gApp->causal.Emit(Phase::Resource,Kind::Info,0,{4,uint64_t(b)});vkDestroyBuffer(d,b,c);}
VkResult CausalImage(VkDevice d,const VkImageCreateInfo* p,const VkAllocationCallbacks* c,VkImage* out){auto r=vkCreateImage(d,p,c,out);if(gApp)gApp->causal.Emit(Phase::Resource,Kind::Info,r,{5,r==VK_SUCCESS?uint64_t(*out):0,p->extent.width,p->extent.height,p->extent.depth,p->arrayLayers,p->mipLevels,uint64_t(p->format)});return r;}
void CausalDestroyImage(VkDevice d,VkImage i,const VkAllocationCallbacks* c){if(gApp)gApp->causal.Emit(Phase::Resource,Kind::Info,0,{6,uint64_t(i)});vkDestroyImage(d,i,c);}
void CausalDispatch(VkCommandBuffer c,uint32_t x,uint32_t y,uint32_t z){if(gApp&&gApp->causal.Active()){gApp->causal.dispatches++;gApp->causal.groups+=uint64_t(x)*y*z;gApp->causal.Emit(Phase::Dispatch,Kind::Info,0,{uint64_t(c),x,y,z});}vkCmdDispatch(c,x,y,z);}
void CausalDispatchIndirect(VkCommandBuffer c,VkBuffer b,VkDeviceSize offset){if(gApp&&gApp->causal.Active()){gApp->causal.dispatches++;gApp->causal.Emit(Phase::Dispatch,Kind::Info,0,{uint64_t(c),UINT64_MAX,uint64_t(b),offset});}vkCmdDispatchIndirect(c,b,offset);}
void CausalDraw(VkCommandBuffer c,uint32_t count,uint32_t instances,uint32_t first,uint32_t instance){if(gApp&&gApp->causal.Active()){gApp->causal.draws++;gApp->causal.Emit(Phase::Draw,Kind::Info,0,{0,uint64_t(c),count,instances,first,instance});}vkCmdDraw(c,count,instances,first,instance);}
void CausalDrawIndexed(VkCommandBuffer c,uint32_t count,uint32_t instances,uint32_t first,int32_t offset,uint32_t instance){if(gApp&&gApp->causal.Active()){gApp->causal.draws++;gApp->causal.Emit(Phase::Draw,Kind::Info,0,{1,uint64_t(c),count,instances,first,uint64_t(offset),instance});}vkCmdDrawIndexed(c,count,instances,first,offset,instance);}
void CausalDrawIndirect(VkCommandBuffer c,VkBuffer b,VkDeviceSize offset,uint32_t count,uint32_t stride){if(gApp&&gApp->causal.Active()){gApp->causal.draws++;gApp->causal.Emit(Phase::Draw,Kind::Info,0,{2,uint64_t(c),uint64_t(b),offset,count,stride});}vkCmdDrawIndexedIndirect(c,b,offset,count,stride);}
VkResult CausalSubmit(VkQueue queue,uint32_t count,const VkSubmitInfo* info,VkFence fence){
  if(gApp)gApp->causal.Emit(Phase::Submit,Kind::Begin,0,{uint64_t(queue),count,uint64_t(fence),info&&count&&info[0].commandBufferCount?uint64_t(info[0].pCommandBuffers[0]):0,++gApp->causal.submissionSequence});
  auto result=vkQueueSubmit(queue,count,info,fence);
  if(gApp&&result==VK_SUCCESS&&fence!=VK_NULL_HANDLE&&fence==gApp->fence)gApp->startup.Submitted(gApp->causal.frame.load());
  if(gApp){if(result==VK_SUCCESS&&fence==gApp->fence){gApp->causal.submitted=gApp->causal.frame.load();gApp->frozen.Submitted(gApp->causal.frame.load(),gApp->causal.submissionSequence);}gApp->causal.Emit(Phase::Submit,Kind::End,result,{uint64_t(queue),uint64_t(fence),gApp->causal.submissionSequence});}return result;
}
VkResult CausalPresent(VkQueue queue,const VkPresentInfoKHR* info){if(gApp)gApp->causal.Emit(Phase::Present,Kind::Begin,0,{uint64_t(queue),info->swapchainCount,info->pImageIndices?info->pImageIndices[0]:UINT32_MAX});auto result=vkQueuePresentKHR(queue,info);if(gApp){gApp->frozen.Presented(gApp->causal.frame.load(),result);gApp->causal.Emit(Phase::Present,Kind::End,result);}return result;}
VkResult CausalIdle(VkDevice device){if(gApp)gApp->causal.Emit(Phase(22),Kind::Begin);auto result=vkDeviceWaitIdle(device);if(gApp){if(result==VK_SUCCESS)gApp->causal.completed=gApp->causal.submitted.load();gApp->causal.Emit(Phase(22),Kind::End,result);}return result;}
VkResult CausalQueueIdle(VkQueue queue){if(gApp)gApp->causal.Emit(Phase(23),Kind::Begin,0,{uint64_t(queue)});auto result=vkQueueWaitIdle(queue);if(gApp)gApp->causal.Emit(Phase(23),Kind::End,result);return result;}
VkResult CausalWait(VkDevice device,uint32_t count,const VkFence* fences,VkBool32 all,uint64_t timeout){
  if(gApp)gApp->causal.Emit(Phase::FenceApi,Kind::Begin,0,{count,count?uint64_t(fences[0]):0,all,timeout});
  const auto result=vkWaitForFences(device,count,fences,all,timeout);
  if(gApp)gApp->causal.Emit(Phase::FenceApi,Kind::End,result);return result;
}
#define vkAllocateMemory CausalAllocate
#define vkFreeMemory CausalFree
#define vkCreateBuffer CausalBuffer
#define vkDestroyBuffer CausalDestroyBuffer
#define vkCreateImage CausalImage
#define vkDestroyImage CausalDestroyImage
#define vkCmdDispatch CausalDispatch
#define vkCmdDispatchIndirect CausalDispatchIndirect
#define vkCmdDraw CausalDraw
#define vkCmdDrawIndexed CausalDrawIndexed
#define vkCmdDrawIndexedIndirect CausalDrawIndirect
#define vkQueueSubmit CausalSubmit
#define vkQueuePresentKHR CausalPresent
#define vkDeviceWaitIdle CausalIdle
#define vkQueueWaitIdle CausalQueueIdle
#define vkWaitForFences CausalWait
