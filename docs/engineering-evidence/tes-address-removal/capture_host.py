"""Temporary, opt-in parity instrumentation; apply only in a reproduction checkout."""
from pathlib import Path

p = Path('native/NovaCore.Native/NovaCoreNative.cpp')
s = p.read_text(encoding='utf-8')
def change(old, new):
    global s
    assert s.count(old) == 1, old[:100]
    s = s.replace(old, new)

change('  uint32_t surfaceDiagnostic{};', '''  uint32_t surfaceDiagnostic{};
  const char* parityDirectory=std::getenv("NOVACORE_TES_PARITY");
  VkBuffer parityPixels{},paritySamples{};VkDeviceMemory parityPixelMemory{},paritySampleMemory{};
  void *parityPixelMapped=nullptr,*paritySampleMapped=nullptr;
  bool parityRecorded=false,parityWritten=false;
  static constexpr VkDeviceSize ParitySampleBytes=16+262144ull*128;
''')
change('image.usage=VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT|VK_IMAGE_USAGE_INPUT_ATTACHMENT_BIT;', 'image.usage=VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT|VK_IMAGE_USAGE_INPUT_ATTACHMENT_BIT|(a.parityDirectory?VK_IMAGE_USAGE_TRANSFER_SRC_BIT:0);')
change('depth.usage=VK_IMAGE_USAGE_DEPTH_STENCIL_ATTACHMENT_BIT;', 'depth.usage=VK_IMAGE_USAGE_DEPTH_STENCIL_ATTACHMENT_BIT|(a.parityDirectory?VK_IMAGE_USAGE_TRANSFER_SRC_BIT:0);')
change('ci.imageUsage = VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT;', 'ci.imageUsage = VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT|(a.parityDirectory?VK_IMAGE_USAGE_TRANSFER_SRC_BIT:0);')
change('  VkAttachmentReference sceneColor{0,', '''  if(a.parityDirectory){attachments[0].storeOp=VK_ATTACHMENT_STORE_OP_STORE;attachments[2].storeOp=VK_ATTACHMENT_STORE_OP_STORE;}
  VkAttachmentReference sceneColor{0,''')
change('  dl.bindingCount = uint32_t(activeBindings.size());', '''  if(a.parityDirectory){for(auto& b:activeBindings)if(b.binding==41)b.stageFlags|=VK_SHADER_STAGE_TESSELLATION_EVALUATION_BIT;activeBindings.push_back({59,VK_DESCRIPTOR_TYPE_STORAGE_BUFFER,1,VK_SHADER_STAGE_TESSELLATION_EVALUATION_BIT,nullptr});}
  dl.bindingCount = uint32_t(activeBindings.size());''')
change('VkDescriptorPoolSize ps[3]{{VK_DESCRIPTOR_TYPE_STORAGE_BUFFER,39}', 'VkDescriptorPoolSize ps[3]{{VK_DESCRIPTOR_TYPE_STORAGE_BUFFER,a.parityDirectory?40u:39u}')
change('  CreateRegionalPhysical(a);', '''  if(a.parityDirectory){
    CreateHostBuffer(a,VkDeviceSize(a.extent.width)*a.extent.height*16,VK_BUFFER_USAGE_TRANSFER_DST_BIT,a.parityPixels,a.parityPixelMemory,a.parityPixelMapped,"parity pixels");
    CreateHostBuffer(a,App::ParitySampleBytes,VK_BUFFER_USAGE_STORAGE_BUFFER_BIT|VK_BUFFER_USAGE_TRANSFER_DST_BIT,a.paritySamples,a.paritySampleMemory,a.paritySampleMapped,"parity TES samples");
    VkDescriptorBufferInfo info{a.paritySamples,0,App::ParitySampleBytes};VkWriteDescriptorSet write{VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET};write.dstSet=a.descriptor;write.dstBinding=59;write.descriptorCount=1;write.descriptorType=VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;write.pBufferInfo=&info;vkUpdateDescriptorSets(a.device,1,&write,0,nullptr);
  }
  CreateRegionalPhysical(a);''')
change('void DestroySubmission(App &a,bool destroyProductionBillboard=true) {', '''void DestroySubmission(App &a,bool destroyProductionBillboard=true) {
  DestroyHostBuffer(a,a.parityPixels,a.parityPixelMemory,a.parityPixelMapped);DestroyHostBuffer(a,a.paritySamples,a.paritySampleMemory,a.paritySampleMapped);''')
change('  a.Check(vkBeginCommandBuffer(c, &bi), "command begin failed");', '''  a.Check(vkBeginCommandBuffer(c, &bi), "command begin failed");
  if(a.parityDirectory){uint32_t header[4]{0,a.frame==175?1u:0u,0,0};vkCmdUpdateBuffer(c,a.paritySamples,0,16,header);VkMemoryBarrier barrier{VK_STRUCTURE_TYPE_MEMORY_BARRIER};barrier.srcAccessMask=VK_ACCESS_TRANSFER_WRITE_BIT;barrier.dstAccessMask=VK_ACCESS_SHADER_READ_BIT|VK_ACCESS_SHADER_WRITE_BIT;vkCmdPipelineBarrier(c,VK_PIPELINE_STAGE_TRANSFER_BIT,VK_PIPELINE_STAGE_TESSELLATION_EVALUATION_SHADER_BIT,0,1,&barrier,0,nullptr,0,nullptr);}
''')
change('  a.Check(vkEndCommandBuffer(c), "command end failed");', '''  if(a.parityDirectory&&a.frame==175){
    const VkDeviceSize pixels=VkDeviceSize(a.extent.width)*a.extent.height;
    auto copy=[&](VkImage image,VkImageLayout layout,VkImageAspectFlags aspect,VkDeviceSize offset){
      VkImageMemoryBarrier b{VK_STRUCTURE_TYPE_IMAGE_MEMORY_BARRIER};b.srcAccessMask=VK_ACCESS_MEMORY_READ_BIT|VK_ACCESS_MEMORY_WRITE_BIT;b.dstAccessMask=VK_ACCESS_TRANSFER_READ_BIT;b.oldLayout=layout;b.newLayout=VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL;b.srcQueueFamilyIndex=VK_QUEUE_FAMILY_IGNORED;b.dstQueueFamilyIndex=VK_QUEUE_FAMILY_IGNORED;b.image=image;b.subresourceRange={aspect==VK_IMAGE_ASPECT_DEPTH_BIT?VkImageAspectFlags(VK_IMAGE_ASPECT_DEPTH_BIT|VK_IMAGE_ASPECT_STENCIL_BIT):aspect,0,1,0,1};
      vkCmdPipelineBarrier(c,VK_PIPELINE_STAGE_ALL_COMMANDS_BIT,VK_PIPELINE_STAGE_TRANSFER_BIT,0,0,nullptr,0,nullptr,1,&b);
      VkBufferImageCopy region{};region.bufferOffset=offset;region.imageSubresource={aspect,0,0,1};region.imageExtent={a.extent.width,a.extent.height,1};vkCmdCopyImageToBuffer(c,image,VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL,a.parityPixels,1,&region);
      b.srcAccessMask=VK_ACCESS_TRANSFER_READ_BIT;b.dstAccessMask=VK_ACCESS_MEMORY_READ_BIT;b.oldLayout=VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL;b.newLayout=layout;vkCmdPipelineBarrier(c,VK_PIPELINE_STAGE_TRANSFER_BIT,VK_PIPELINE_STAGE_ALL_COMMANDS_BIT,0,0,nullptr,0,nullptr,1,&b);
    };
    copy(a.sceneDepth,VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL,VK_IMAGE_ASPECT_DEPTH_BIT,0);
    copy(a.sceneColor,VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL,VK_IMAGE_ASPECT_COLOR_BIT,pixels*4);
    copy(a.images[image],VK_IMAGE_LAYOUT_PRESENT_SRC_KHR,VK_IMAGE_ASPECT_COLOR_BIT,pixels*12);
    VkMemoryBarrier host{VK_STRUCTURE_TYPE_MEMORY_BARRIER};host.srcAccessMask=VK_ACCESS_TRANSFER_WRITE_BIT|VK_ACCESS_SHADER_WRITE_BIT;host.dstAccessMask=VK_ACCESS_HOST_READ_BIT;vkCmdPipelineBarrier(c,VK_PIPELINE_STAGE_ALL_COMMANDS_BIT,VK_PIPELINE_STAGE_HOST_BIT,0,1,&host,0,nullptr,0,nullptr);a.parityRecorded=true;
  }
  a.Check(vkEndCommandBuffer(c), "command end failed");''')
change('  const auto fenceEnd=std::chrono::steady_clock::now();', '''  const auto fenceEnd=std::chrono::steady_clock::now();
  if(a.parityDirectory&&a.parityRecorded&&!a.parityWritten){
    auto save=[&](const char*name,const void*bytes,size_t size){std::ofstream f(std::string(a.parityDirectory)+name,std::ios::binary);f.write(static_cast<const char*>(bytes),size);if(!f)throw std::runtime_error("parity output failed");};
    const uint32_t samples=*static_cast<const uint32_t*>(a.paritySampleMapped);if(samples>262144)throw std::runtime_error("parity TES sample overflow");
    save("/pixels.bin",a.parityPixelMapped,size_t(a.extent.width)*a.extent.height*16);save("/tes.bin",a.paritySampleMapped,16+size_t(samples)*128);
    save("/prepared.bin",a.productionBillboardPhysicalMapped,size_t(a.productionBillboardVertexCount)*sizeof(NcSphericalBillboardPhysicalVertex));
    char row[512];std::snprintf(row,sizeof row,"TES parity capture: frame=%llu; width=%u; height=%u; samples=%u; prepared=%u; generation=%llu; pupil=%u; physicalGeneration=%u; diagnostic=%u",(unsigned long long)a.frame,a.extent.width,a.extent.height,samples,a.productionBillboardVertexCount,(unsigned long long)a.productionBillboardGeneration,a.productionBillboardRasterFrameIdentity,a.submission->physicalSurfaceGeneration,a.surfaceDiagnostic);a.Log(NC_LOG_ALWAYS,row);a.parityWritten=true;
  }''')
p.write_text(s,encoding='utf-8',newline='\n')
