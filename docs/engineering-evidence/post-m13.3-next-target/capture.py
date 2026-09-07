"""Optional one-frame attachment readback. No TES/GS buffers or shader features.

Transient budget 512 MiB, one reusable output path; never used for final timing.
"""
from pathlib import Path
def instrument_capture(s):
    def rep(a,b):
        nonlocal s
        assert s.count(a)==1,(s.count(a),a[:100]);s=s.replace(a,b,1)
    rep('  uint32_t surfaceDiagnostic{};','''  uint32_t surfaceDiagnostic{};
  const char* parityDirectory=std::getenv("NOVACORE_PIXEL_PARITY");
  VkBuffer parityPixels{};VkDeviceMemory parityPixelMemory{};void* parityPixelMapped=nullptr;
  bool parityRecorded=false,parityWritten=false;
  const uint64_t parityFrame=175;
''')
    rep('image.usage=VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT|VK_IMAGE_USAGE_INPUT_ATTACHMENT_BIT;','image.usage=VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT|VK_IMAGE_USAGE_INPUT_ATTACHMENT_BIT|(a.parityDirectory?VK_IMAGE_USAGE_TRANSFER_SRC_BIT:0);')
    rep('depth.usage=VK_IMAGE_USAGE_DEPTH_STENCIL_ATTACHMENT_BIT;','depth.usage=VK_IMAGE_USAGE_DEPTH_STENCIL_ATTACHMENT_BIT|(a.parityDirectory?VK_IMAGE_USAGE_TRANSFER_SRC_BIT:0);')
    rep('  ci.imageUsage = VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT;','  ci.imageUsage = VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT|(a.parityDirectory?VK_IMAGE_USAGE_TRANSFER_SRC_BIT:0);')
    rep('  VkAttachmentReference sceneColor{0,VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL};','  if(a.parityDirectory){attachments[0].storeOp=VK_ATTACHMENT_STORE_OP_STORE;attachments[2].storeOp=VK_ATTACHMENT_STORE_OP_STORE;}\n  VkAttachmentReference sceneColor{0,VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL};')
    rep('void DestroySubmission(App &a,bool destroyProductionBillboard=true) {','void DestroySubmission(App &a,bool destroyProductionBillboard=true) {\n  DestroyHostBuffer(a,a.parityPixels,a.parityPixelMemory,a.parityPixelMapped);')
    rep('  CreateRegionalPhysical(a);','  if(a.parityDirectory)CreateHostBuffer(a,VkDeviceSize(a.extent.width)*a.extent.height*16,VK_BUFFER_USAGE_TRANSFER_DST_BIT,a.parityPixels,a.parityPixelMemory,a.parityPixelMapped,"single parity frame");\n  CreateRegionalPhysical(a);')
    old=(Path(__file__).resolve().parent.parent/'post-m13.2-next-target/instrumentation.patch').read_text()
    begin=old.index('+  if(a.parityDirectory&&a.frame==a.parityFrame){')
    end=old.index('\n   a.Check(vkEndCommandBuffer',begin)
    code='\n'.join(line[1:] for line in old[begin:end].splitlines() if line.startswith('+'))+'\n'
    rep('  a.Check(vkEndCommandBuffer(c), "command end failed");',code+'  a.Check(vkEndCommandBuffer(c), "command end failed");')
    rep('  InspectRegionalPhysical(a);','''  if(a.parityDirectory&&a.parityRecorded&&!a.parityWritten){
    auto save=[&](const char*name,const void*data,size_t bytes){std::ofstream out(std::string(a.parityDirectory)+name,std::ios::binary);out.write(static_cast<const char*>(data),bytes);if(!out)throw std::runtime_error("pixel parity write failed");};
    save("/pixels.bin",a.parityPixelMapped,size_t(a.extent.width)*a.extent.height*16);
    save("/prepared.bin",a.productionBillboardPhysicalMapped,size_t(a.productionBillboardVertexCount)*sizeof(NcSphericalBillboardPhysicalVertex));
    auto* draw=static_cast<VkDrawIndexedIndirectCommand*>(a.productionBillboardIndirectMapped);save("/selected.bin",a.productionBillboardCompactedMapped,size_t(draw->indexCount)*sizeof(uint32_t));
    char row[512];std::snprintf(row,sizeof row,"Pixel parity: frame=%llu; width=%u; height=%u; generation=%llu; pupil=%u; physicalGeneration=%u; vertices=%u",(unsigned long long)a.frame,a.extent.width,a.extent.height,(unsigned long long)a.productionBillboardGeneration,a.productionBillboardRasterFrameIdentity,a.submission->physicalSurfaceGeneration,a.productionBillboardVertexCount);a.Log(NC_LOG_ALWAYS,row);a.parityWritten=true;
  }
  InspectRegionalPhysical(a);''')
    return s
