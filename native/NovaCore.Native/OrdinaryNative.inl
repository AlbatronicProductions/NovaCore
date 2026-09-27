// Included AFTER legacy wrapper definitions. Calls below retain those exact
// forwarding functions; minimum enablement never changes causal.Active().
namespace minimum=nc::minimum;
template<class F> VkResult MinimumCall(minimum::Phase phase,F&& call,uint64_t action=0,uint64_t kind=0,uint64_t handle=0,uint64_t related=0){
  auto* m=minimum::recorder.get();const auto op=m?m->Op():0;minimum::Event e{};if(m){minimum::MeasurementScope cost(m->measurement);e=m->Make(phase,minimum::Enter,0,op);e.w[13]=handle;e.w[14]=handle?m->Id(kind,handle):0;e.w[15]=related;e.w[16]=related?m->Id(minimum::Memory,related):0;e.w[17]=action;e.w[18]=kind;m->Emit(e);}
  auto r=call();if(m){minimum::MeasurementScope cost(m->measurement);e.w[5]=minimum::Return;e.w[6]=uint64_t(r);m->Emit(e);}if(r<0)minimum::ErrorResult(r);return r;
}
void MinimumSync(App& a){if(auto* m=minimum::recorder.get()){minimum::MeasurementScope cost(m->measurement);m->terrain=a.frame;m->current=a.productionBillboardGeneration;m->incoming=a.productionBillboardIncomingGeneration;}}
void MinimumGeneration(App& a,uint64_t reason){if(auto* m=minimum::recorder.get()){minimum::MeasurementScope cost(m->measurement);MinimumSync(a);auto e=m->Make(minimum::Publication);e.w[17]=reason;e.w[18]=a.productionBillboardIncomingTopologyHash;e.w[19]=a.productionBillboardIncomingTopologyFamily;m->Emit(e);}}
void MinimumPublication(App& a,uint64_t reason){if(auto* m=minimum::recorder.get()){minimum::MeasurementScope cost(m->measurement);MinimumSync(a);++m->publication;auto e=m->Make(minimum::Publication);e.w[13]=uint64_t(a.productionBillboardPhysicalBuffer);e.w[14]=m->Id(minimum::Buffer,e.w[13]);e.w[17]=reason;e.w[18]=a.productionBillboardPreparedFrameIdentity;e.w[19]=a.productionBillboardTopologyHash;e.w[20]=a.productionBillboardTopologyFamily;m->Emit(e);}}
void MinimumContext(App& a){
  auto* m=minimum::recorder.get();if(!m)return;minimum::MeasurementScope cost(m->measurement);MinimumSync(a);
  // Roles follow actual descriptor bindings. Resource identity is independent of
  // topology generation, including physical/scratch pupil swaps within a generation.
  VkBuffer buffers[]{a.productionBillboardPhysicalBuffer,a.productionBillboardIndexBuffer,a.productionBillboardVisibilityBuffer,a.productionBillboardCompactedBuffer,a.productionBillboardIndirectBuffer,a.productionBillboardCounterBuffer,a.productionBillboardLatticeBuffer,
    a.productionBillboardIncomingPhysicalBuffer,a.productionBillboardIncomingIndexBuffer,a.productionBillboardIncomingVisibilityBuffer,a.productionBillboardIncomingCompactedBuffer,a.productionBillboardIncomingIndirectBuffer,a.productionBillboardIncomingCounterBuffer,a.productionBillboardIncomingLatticeBuffer,
    a.productionBillboardFrameBuffer,a.regionalCatalogBuffer,a.regionalPayloadBuffer,a.regionalDemandBuffer,a.regionalPreparationBuffer,a.regionalScratchBuffer};
  for(size_t i=0;i<std::size(buffers);i++)m->Role(38+i,minimum::Buffer,uint64_t(buffers[i]));
  m->Role(1,minimum::Buffer,uint64_t(a.submissionBuffer));m->Role(2,minimum::Buffer,uint64_t(a.patchBuffer));
  m->Role(5,minimum::Queue,uint64_t(a.graphicsQueue));m->Role(6,minimum::Queue,uint64_t(a.presentQueue));m->Role(7,minimum::Swapchain,uint64_t(a.swapchain));
  m->Role(3,minimum::Image,uint64_t(a.sceneColor));m->Role(4,minimum::Image,uint64_t(a.sceneDepth));
  VkBuffer other[]{a.gpuInputBuffer,a.gpuWorkBuffer,a.gpuNodeBuffer,a.gpuControlBuffer,a.terrainKeyBuffer,a.terrainSampleBuffer,a.terrainPatchSlotBuffer,a.planetaryPresentationBuffer,a.productionLayerLookupBuffer,a.physicalOracleBuffer,a.naturalGlobalPreparedBuffer,a.facilityVisibilityBuffer,a.orbitBuffer,a.previousOrbitBuffer,a.bodyForwardBuffer,a.targetDirectionBuffer,a.productionStagingBuffer};
  for(size_t i=0;i<std::size(other);i++)m->Role(60+i,minimum::Buffer,uint64_t(other[i]));
  for(size_t i=0;i<a.productionImages.size();i++)m->Role(80+i,minimum::Image,uint64_t(a.productionImages[i]));
  for(size_t i=0;i<2;i++)m->Role(83+i,minimum::Image,uint64_t(a.exhaustTargets[i]));
  const Mesh* meshes[]{&a.triangle,&a.floridaLaunchPad,&a.floridaLaunchFoundation,&a.contactQualificationBody,&a.contactQualificationSupport,&a.floridaSupportSlab,&a.planetaryPatch,&a.distantPlanetary,&a.stellarSun,&a.planetaryRing};
  for(size_t i=0;i<std::size(meshes);i++){m->Role(90+i*2,minimum::Buffer,uint64_t(meshes[i]->vb));m->Role(91+i*2,minimum::Buffer,uint64_t(meshes[i]->ib));}
}
VkResult MinimumAllocate(VkDevice d,const VkMemoryAllocateInfo* p,const VkAllocationCallbacks* c,VkDeviceMemory* out){auto r=MinimumCall(minimum::Resource,[&]{return vkAllocateMemory(d,p,c,out);},1,minimum::Memory);if(r==VK_SUCCESS&&minimum::recorder)minimum::recorder->Birth(minimum::Memory,uint64_t(*out),{p->allocationSize,p->memoryTypeIndex});return r;}
void MinimumFree(VkDevice d,VkDeviceMemory m,const VkAllocationCallbacks* c){minimum::Scope scope(minimum::Resource,2,minimum::Memory,uint64_t(m));vkFreeMemory(d,m,c);if(minimum::recorder)minimum::recorder->Retire(minimum::Memory,uint64_t(m));}
VkResult MinimumBuffer(VkDevice d,const VkBufferCreateInfo* p,const VkAllocationCallbacks* c,VkBuffer* out){auto r=MinimumCall(minimum::Resource,[&]{return vkCreateBuffer(d,p,c,out);},1,minimum::Buffer);if(r==VK_SUCCESS&&minimum::recorder)minimum::recorder->Birth(minimum::Buffer,uint64_t(*out),{p->size,p->usage});return r;}
void MinimumDestroyBuffer(VkDevice d,VkBuffer h,const VkAllocationCallbacks* c){minimum::Scope scope(minimum::Resource,2,minimum::Buffer,uint64_t(h));vkDestroyBuffer(d,h,c);if(minimum::recorder)minimum::recorder->Retire(minimum::Buffer,uint64_t(h));}
VkResult MinimumImage(VkDevice d,const VkImageCreateInfo* p,const VkAllocationCallbacks* c,VkImage* out){auto r=MinimumCall(minimum::Resource,[&]{return vkCreateImage(d,p,c,out);},1,minimum::Image);if(r==VK_SUCCESS&&minimum::recorder)minimum::recorder->Birth(minimum::Image,uint64_t(*out),{p->extent.width,p->extent.height,p->extent.depth,p->arrayLayers,p->mipLevels,uint64_t(p->format),p->usage});return r;}
void MinimumDestroyImage(VkDevice d,VkImage h,const VkAllocationCallbacks* c){minimum::Scope scope(minimum::Resource,2,minimum::Image,uint64_t(h));vkDestroyImage(d,h,c);if(minimum::recorder)minimum::recorder->Retire(minimum::Image,uint64_t(h));}
VkResult MinimumBindBuffer(VkDevice d,VkBuffer b,VkDeviceMemory m,VkDeviceSize offset){auto r=MinimumCall(minimum::Resource,[&]{return vkBindBufferMemory(d,b,m,offset);},3,minimum::Buffer,uint64_t(b),uint64_t(m));if(r==VK_SUCCESS&&minimum::recorder)minimum::recorder->Association(3,minimum::Buffer,uint64_t(b),uint64_t(m),offset);return r;}
VkResult MinimumBindImage(VkDevice d,VkImage b,VkDeviceMemory m,VkDeviceSize offset){auto r=MinimumCall(minimum::Resource,[&]{return vkBindImageMemory(d,b,m,offset);},3,minimum::Image,uint64_t(b),uint64_t(m));if(r==VK_SUCCESS&&minimum::recorder)minimum::recorder->Association(3,minimum::Image,uint64_t(b),uint64_t(m),offset);return r;}
VkResult MinimumMap(VkDevice d,VkDeviceMemory m,VkDeviceSize offset,VkDeviceSize size,VkMemoryMapFlags flags,void** out){auto r=MinimumCall(minimum::Resource,[&]{return vkMapMemory(d,m,offset,size,flags,out);},4,minimum::Memory,uint64_t(m),uint64_t(m));if(r==VK_SUCCESS&&minimum::recorder)minimum::recorder->Association(4,minimum::Memory,uint64_t(m),uint64_t(m),offset,size);return r;}
void MinimumUnmap(VkDevice d,VkDeviceMemory m){minimum::Scope scope(minimum::Resource,5,minimum::Memory,uint64_t(m));vkUnmapMemory(d,m);if(minimum::recorder)minimum::recorder->Association(5,minimum::Memory,uint64_t(m),uint64_t(m));}
void MinimumQueue(VkDevice d,uint32_t family,uint32_t index,VkQueue* out){vkGetDeviceQueue(d,family,index,out);if(auto* m=minimum::recorder.get())if(!m->Existing(minimum::Queue,uint64_t(*out)))m->Birth(minimum::Queue,uint64_t(*out),{family,index});}
void MinimumPoolDestroy(VkDevice d,VkCommandPool pool,const VkAllocationCallbacks* c){minimum::Scope scope(minimum::Resource);vkDestroyCommandPool(d,pool,c);if(minimum::recorder)minimum::recorder->RetireChildren(uint64_t(pool));}
VkResult MinimumCommands(VkDevice d,const VkCommandBufferAllocateInfo* p,VkCommandBuffer* out){auto r=MinimumCall(minimum::Resource,[&]{return vkAllocateCommandBuffers(d,p,out);},1,minimum::Command);if(r==VK_SUCCESS&&minimum::recorder)for(uint32_t i=0;i<p->commandBufferCount;i++)minimum::recorder->Birth(minimum::Command,uint64_t(out[i]),{uint64_t(p->commandPool),uint64_t(p->level)});return r;}
void MinimumFreeCommands(VkDevice d,VkCommandPool pool,uint32_t n,const VkCommandBuffer* c){minimum::Scope scope(minimum::Resource);vkFreeCommandBuffers(d,pool,n,c);if(minimum::recorder)for(uint32_t i=0;i<n;i++)minimum::recorder->Retire(minimum::Command,uint64_t(c[i]));}
VkResult MinimumBegin(VkCommandBuffer c,const VkCommandBufferBeginInfo* p){auto* m=minimum::recorder.get();auto op=m?m->Op():0;minimum::Event e{};if(m){minimum::MeasurementScope cost(m->measurement);e=m->Make(minimum::Record,minimum::Enter,0,op);e.w[13]=uint64_t(c);e.w[14]=m->Id(minimum::Command,uint64_t(c));e.w[17]=m->Recording(uint64_t(c),true);e.w[18]=1;m->Emit(e);}auto r=vkBeginCommandBuffer(c,p);if(m){minimum::MeasurementScope cost(m->measurement);e.w[5]=minimum::Return;e.w[6]=uint64_t(r);m->Emit(e);}return r;}
VkResult MinimumEnd(VkCommandBuffer c){auto* m=minimum::recorder.get();minimum::Event e{};if(m){minimum::MeasurementScope cost(m->measurement);e=m->Make(minimum::Record,minimum::Enter,0,m->Op());e.w[13]=uint64_t(c);e.w[14]=m->Id(minimum::Command,uint64_t(c));e.w[17]=m->Recording(uint64_t(c));e.w[18]=2;m->Emit(e);}auto r=vkEndCommandBuffer(c);if(m){minimum::MeasurementScope cost(m->measurement);e.w[5]=minimum::Return;e.w[6]=uint64_t(r);m->Emit(e);}return r;}
VkResult MinimumFenceCreate(VkDevice d,const VkFenceCreateInfo* p,const VkAllocationCallbacks* c,VkFence* out){auto r=MinimumCall(minimum::Resource,[&]{return vkCreateFence(d,p,c,out);},1,minimum::FenceKind);if(r==VK_SUCCESS&&minimum::recorder)minimum::recorder->Birth(minimum::FenceKind,uint64_t(*out),{p->flags});return r;}
void MinimumFenceDestroy(VkDevice d,VkFence f,const VkAllocationCallbacks* c){minimum::Scope scope(minimum::Resource,2,minimum::FenceKind,uint64_t(f));vkDestroyFence(d,f,c);if(minimum::recorder)minimum::recorder->Retire(minimum::FenceKind,uint64_t(f));}
VkResult MinimumSwapCreate(VkDevice d,const VkSwapchainCreateInfoKHR* p,const VkAllocationCallbacks* c,VkSwapchainKHR* out){auto r=MinimumCall(minimum::Resource,[&]{return vkCreateSwapchainKHR(d,p,c,out);},1,minimum::Swapchain);if(r==VK_SUCCESS&&minimum::recorder)minimum::recorder->Birth(minimum::Swapchain,uint64_t(*out),{p->imageExtent.width,p->imageExtent.height,uint64_t(p->presentMode),uint64_t(p->oldSwapchain)});return r;}
void MinimumSwapDestroy(VkDevice d,VkSwapchainKHR s,const VkAllocationCallbacks* c){minimum::Scope scope(minimum::Resource,2,minimum::Swapchain,uint64_t(s));vkDestroySwapchainKHR(d,s,c);if(minimum::recorder)minimum::recorder->Retire(minimum::Swapchain,uint64_t(s));}
VkResult MinimumAcquire(VkDevice d,VkSwapchainKHR s,uint64_t timeout,VkSemaphore sem,VkFence f,uint32_t* image){auto* m=minimum::recorder.get();minimum::Event e{};if(m){minimum::MeasurementScope cost(m->measurement);e=m->Make(minimum::Acquire,minimum::Enter,0,m->Op());e.w[13]=uint64_t(s);e.w[14]=m->Id(minimum::Swapchain,uint64_t(s));m->Emit(e);}auto r=vkAcquireNextImageKHR(d,s,timeout,sem,f,image);if(m){minimum::MeasurementScope cost(m->measurement);e.w[5]=minimum::Return;e.w[6]=uint64_t(r);e.w[17]=(r==VK_SUCCESS||r==VK_SUBOPTIMAL_KHR)?*image:UINT64_MAX;m->Emit(e);}return r;}
VkResult MinimumSubmit(VkQueue queue,uint32_t count,const VkSubmitInfo* info,VkFence fence){
  auto* m=minimum::recorder.get();minimum::Event e{};if(m){minimum::MeasurementScope cost(m->measurement);if(gApp)MinimumContext(*gApp);e=m->Make(minimum::Submit,minimum::Enter,0,m->Op());e.w[8]=++m->submission;e.w[13]=uint64_t(fence);e.w[14]=m->Id(minimum::FenceKind,uint64_t(fence));e.w[15]=count&&info[0].commandBufferCount?uint64_t(info[0].pCommandBuffers[0]):0;e.w[16]=m->Id(minimum::Command,e.w[15]);e.w[17]=m->Recording(e.w[15]);e.w[18]=uint64_t(queue);e.w[19]=count;e.w[20]=gApp?gApp->productionBillboardPreparedFrameIdentity:0;e.w[21]=gApp?gApp->productionBillboardCullFrameIdentity:0;e.w[22]=gApp?gApp->productionBillboardRasterFrameIdentity:0;e.w[23]=gApp?gApp->productionBillboardTopologyHash:0;e.w[24]=gApp?gApp->productionBillboardIncomingTopologyHash:0;e.w[25]=gApp?gApp->productionBillboardIncomingTopologyFamily:0;e.w[26]=gApp?gApp->regionalPreparation[1].generation:0;e.w[27]=m->Id(minimum::Queue,uint64_t(queue));m->Emit(e);}
  auto r=vkQueueSubmit(queue,count,info,fence);if(m){minimum::MeasurementScope cost(m->measurement);e.w[5]=minimum::Return;e.w[6]=uint64_t(r);m->Emit(e);if(r==VK_SUCCESS)m->Submitted(e,uint64_t(queue));}return r;
}
VkResult MinimumPresent(VkQueue queue,const VkPresentInfoKHR* p){auto* m=minimum::recorder.get();minimum::Event e{};if(m){minimum::MeasurementScope cost(m->measurement);e=m->Make(minimum::Present,minimum::Enter,0,m->Op());e.w[13]=p->swapchainCount?uint64_t(p->pSwapchains[0]):0;e.w[14]=m->Id(minimum::Swapchain,e.w[13]);e.w[17]=p->swapchainCount?p->pImageIndices[0]:UINT64_MAX;e.w[18]=uint64_t(queue);m->Emit(e);}auto r=vkQueuePresentKHR(queue,p);if(m){minimum::MeasurementScope cost(m->measurement);e.w[5]=minimum::Return;e.w[6]=uint64_t(r);m->Emit(e);}return r;}
VkResult MinimumWait(VkDevice d,uint32_t n,const VkFence* f,VkBool32 all,uint64_t timeout){auto* m=minimum::recorder.get();minimum::Event e{};if(m){minimum::MeasurementScope cost(m->measurement);e=m->Make(minimum::Fence,minimum::Enter,0,m->Op());e.w[13]=n?uint64_t(f[0]):0;e.w[14]=m->Id(minimum::FenceKind,e.w[13]);e.w[17]=n;e.w[18]=all;e.w[19]=timeout;m->Emit(e);}auto r=vkWaitForFences(d,n,f,all,timeout);if(m){minimum::MeasurementScope cost(m->measurement);e.w[5]=minimum::Return;e.w[6]=uint64_t(r);m->Emit(e);if(r==VK_SUCCESS&&(all||n==1))for(uint32_t i=0;i<n;i++)m->Complete(uint64_t(f[i]),m->Id(minimum::FenceKind,uint64_t(f[i])));}return r;}
VkResult MinimumIdle(VkDevice d){auto r=MinimumCall(minimum::DeviceIdle,[&]{return vkDeviceWaitIdle(d);});if(r==VK_SUCCESS&&minimum::recorder)minimum::recorder->Complete(0,0,0,true);return r;}
VkResult MinimumQueueIdle(VkQueue q){auto r=MinimumCall(minimum::QueueIdle,[&]{return vkQueueWaitIdle(q);});if(r==VK_SUCCESS&&minimum::recorder)minimum::recorder->Complete(0,0,uint64_t(q));return r;}
#undef vkAllocateMemory
#undef vkFreeMemory
#undef vkCreateBuffer
#undef vkDestroyBuffer
#undef vkCreateImage
#undef vkDestroyImage
#undef vkQueueSubmit
#undef vkQueuePresentKHR
#undef vkDeviceWaitIdle
#undef vkQueueWaitIdle
#undef vkWaitForFences
#define vkAllocateMemory MinimumAllocate
#define vkFreeMemory MinimumFree
#define vkCreateBuffer MinimumBuffer
#define vkDestroyBuffer MinimumDestroyBuffer
#define vkCreateImage MinimumImage
#define vkDestroyImage MinimumDestroyImage
#define vkBindBufferMemory MinimumBindBuffer
#define vkBindImageMemory MinimumBindImage
#define vkMapMemory MinimumMap
#define vkUnmapMemory MinimumUnmap
#define vkAllocateCommandBuffers MinimumCommands
#define vkFreeCommandBuffers MinimumFreeCommands
#define vkBeginCommandBuffer MinimumBegin
#define vkEndCommandBuffer MinimumEnd
#define vkCreateFence MinimumFenceCreate
#define vkDestroyFence MinimumFenceDestroy
#define vkCreateSwapchainKHR MinimumSwapCreate
#define vkDestroySwapchainKHR MinimumSwapDestroy
#define vkAcquireNextImageKHR MinimumAcquire
#define vkQueueSubmit MinimumSubmit
#define vkQueuePresentKHR MinimumPresent
#define vkWaitForFences MinimumWait
#define vkDeviceWaitIdle MinimumIdle
#define vkQueueWaitIdle MinimumQueueIdle

#define vkGetDeviceQueue MinimumQueue
#define vkDestroyCommandPool MinimumPoolDestroy

#undef vkDestroyDevice
void MinimumDestroyDevice(VkDevice device,const VkAllocationCallbacks* c){minimum::Scope scope(minimum::Resource);vkDestroyDevice(device,c);if(minimum::recorder)minimum::recorder->RetireDevice();}
#define vkDestroyDevice MinimumDestroyDevice
