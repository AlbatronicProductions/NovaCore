// Created only for the supervised diagnostic path. Descriptor sets belong to
// the same two capture slots, and are updated only after successful reservation.
void CreateFrozenPacking(App& a){
  std::array<VkDescriptorSetLayoutBinding,7> bindings{};
  for(uint32_t i=0;i<7;++i)bindings[i]={i,VK_DESCRIPTOR_TYPE_STORAGE_BUFFER,1,VK_SHADER_STAGE_COMPUTE_BIT,nullptr};
  VkDescriptorSetLayoutCreateInfo layout{VK_STRUCTURE_TYPE_DESCRIPTOR_SET_LAYOUT_CREATE_INFO};layout.bindingCount=7;layout.pBindings=bindings.data();
  a.Check(vkCreateDescriptorSetLayout(a.device,&layout,nullptr,&a.frozenPackDescriptorLayout),"frozen pack descriptor layout");
  VkPushConstantRange push{VK_SHADER_STAGE_COMPUTE_BIT,0,sizeof(nc::frozen::PackParameters)};
  VkPipelineLayoutCreateInfo pipelineLayout{VK_STRUCTURE_TYPE_PIPELINE_LAYOUT_CREATE_INFO};pipelineLayout.setLayoutCount=1;pipelineLayout.pSetLayouts=&a.frozenPackDescriptorLayout;pipelineLayout.pushConstantRangeCount=1;pipelineLayout.pPushConstantRanges=&push;
  a.Check(vkCreatePipelineLayout(a.device,&pipelineLayout,nullptr,&a.frozenPackLayout),"frozen pack pipeline layout");
  VkShaderModule shader=Shader(a,"shaders/frozen_capture_pack.comp.spv");
  VkComputePipelineCreateInfo pipeline{VK_STRUCTURE_TYPE_COMPUTE_PIPELINE_CREATE_INFO};pipeline.layout=a.frozenPackLayout;
  pipeline.stage={VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_COMPUTE_BIT,shader,"main"};
  auto result=vkCreateComputePipelines(a.device,{},1,&pipeline,nullptr,&a.frozenPackPipeline);vkDestroyShaderModule(a.device,shader,nullptr);a.Check(result,"frozen pack compute pipeline");
  VkDescriptorPoolSize size{VK_DESCRIPTOR_TYPE_STORAGE_BUFFER,7*uint32_t(nc::frozen::SlotCount)};
  VkDescriptorPoolCreateInfo pool{VK_STRUCTURE_TYPE_DESCRIPTOR_POOL_CREATE_INFO};pool.maxSets=uint32_t(nc::frozen::SlotCount);pool.poolSizeCount=1;pool.pPoolSizes=&size;
  a.Check(vkCreateDescriptorPool(a.device,&pool,nullptr,&a.frozenPackPool),"frozen pack pool");
  std::array<VkDescriptorSetLayout,nc::frozen::SlotCount> layouts{};layouts.fill(a.frozenPackDescriptorLayout);
  VkDescriptorSetAllocateInfo allocate{VK_STRUCTURE_TYPE_DESCRIPTOR_SET_ALLOCATE_INFO};allocate.descriptorPool=a.frozenPackPool;allocate.descriptorSetCount=uint32_t(layouts.size());allocate.pSetLayouts=layouts.data();
  a.Check(vkAllocateDescriptorSets(a.device,&allocate,a.frozenPackSets.data()),"frozen pack sets");
  for(size_t i=0;i<nc::frozen::SlotCount;++i)CreateHostBuffer(a,nc::frozen::PackMetadataBytes,VK_BUFFER_USAGE_STORAGE_BUFFER_BIT|VK_BUFFER_USAGE_TRANSFER_DST_BIT,a.frozenPackMetadataBuffers[i],a.frozenPackMetadataMemory[i],a.frozenPackMetadataMapped[i],"frozen pack metadata",nc::MappedBufferUse::TerrainRequestKeys);
}
void DestroyFrozenPacking(App& a){
  for(size_t i=0;i<nc::frozen::SlotCount;++i)DestroyHostBuffer(a,a.frozenPackMetadataBuffers[i],a.frozenPackMetadataMemory[i],a.frozenPackMetadataMapped[i]);
  if(a.frozenPackPool)vkDestroyDescriptorPool(a.device,a.frozenPackPool,nullptr);
  if(a.frozenPackPipeline)vkDestroyPipeline(a.device,a.frozenPackPipeline,nullptr);
  if(a.frozenPackLayout)vkDestroyPipelineLayout(a.device,a.frozenPackLayout,nullptr);
  if(a.frozenPackDescriptorLayout)vkDestroyDescriptorSetLayout(a.device,a.frozenPackDescriptorLayout,nullptr);
  a.frozenPackPool={};a.frozenPackPipeline={};a.frozenPackLayout={};a.frozenPackDescriptorLayout={};
}
void RecordFrozenPacking(App& a,VkCommandBuffer command,size_t slot,const nc::frozen::Header& h){
  auto parameters=nc::frozen::PackingParameters(h);
  const VkBuffer sources[]{a.productionBillboardPhysicalBuffer,a.productionBillboardVisibilityBuffer,a.productionBillboardCompactedBuffer,a.productionBillboardCounterBuffer,a.productionBillboardIndirectBuffer,a.frozenBuffers[slot],a.frozenPackMetadataBuffers[slot]};
  const VkDeviceSize sizes[]{h.sections[0].bytes,h.sections[2].bytes,h.sections[3].bytes,168,20,nc::frozen::SlotBytes,nc::frozen::PackMetadataBytes};
  std::array<VkDescriptorBufferInfo,7> buffers{};std::array<VkWriteDescriptorSet,7> writes{};
  for(uint32_t i=0;i<7;++i){buffers[i]={sources[i],0,sizes[i]};writes[i]={VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET};writes[i].dstSet=a.frozenPackSets[slot];writes[i].dstBinding=i;writes[i].descriptorCount=1;writes[i].descriptorType=VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;writes[i].pBufferInfo=&buffers[i];}
  vkUpdateDescriptorSets(a.device,uint32_t(writes.size()),writes.data(),0,nullptr);
  vkCmdBindPipeline(command,VK_PIPELINE_BIND_POINT_COMPUTE,a.frozenPackPipeline);
  vkCmdBindDescriptorSets(command,VK_PIPELINE_BIND_POINT_COMPUTE,a.frozenPackLayout,0,1,&a.frozenPackSets[slot],0,nullptr);
  vkCmdPushConstants(command,a.frozenPackLayout,VK_SHADER_STAGE_COMPUTE_BIT,0,sizeof(parameters),parameters.data());
  // Fixed bounded workgroups; grid-stride loops cover the exact used ranges.
  vkCmdDispatch(command,256,1,1);
}
