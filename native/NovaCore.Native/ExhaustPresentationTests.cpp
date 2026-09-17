#include "PresentationGpuTestDevice.h"
#include "NovaCoreNative.h"
#include <algorithm>
#include <cmath>
#include <filesystem>
#include <iostream>
// Executes the production volume and resolve shaders, including attachment
// blending and opaque-depth clipping. No CPU copy of the plume field exists.
struct ExhaustTest {
 Device d{VK_QUEUE_GRAPHICS_BIT}; static constexpr uint32_t W=96,H=96;
 VkImage images[5]{};VkDeviceMemory memories[5]{};VkImageView views[5]{};
 VkFormat formats[5]{VK_FORMAT_R16G16B16A16_SFLOAT,VK_FORMAT_D32_SFLOAT,VK_FORMAT_R16G16B16A16_SFLOAT,VK_FORMAT_R16_SFLOAT,VK_FORMAT_R32G32B32A32_SFLOAT};
 VkRenderPass pass{};VkFramebuffer fb{};VkPipeline volume{},resolve{};VkDescriptorSet set{};VkCommandBuffer cmd{};
 Device::Buffer frame,vertices,readback; std::string shaders;
 ExhaustTest(std::string dir):shaders(dir){
  for(int i=0;i<5;i++){
   VkImageCreateInfo c{VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO};c.imageType=VK_IMAGE_TYPE_2D;c.format=formats[i];c.extent={W,H,1};c.mipLevels=c.arrayLayers=1;c.samples=VK_SAMPLE_COUNT_1_BIT;c.tiling=VK_IMAGE_TILING_OPTIMAL;
   c.usage=(i==1?VK_IMAGE_USAGE_DEPTH_STENCIL_ATTACHMENT_BIT:VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT)|VK_IMAGE_USAGE_INPUT_ATTACHMENT_BIT|VK_IMAGE_USAGE_TRANSFER_SRC_BIT;
   Vk(vkCreateImage(d.device,&c,nullptr,&images[i]));VkMemoryRequirements r;vkGetImageMemoryRequirements(d.device,images[i],&r);uint32_t type=0;while(!(r.memoryTypeBits&(1u<<type)))type++;
   VkMemoryAllocateInfo m{VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO};m.allocationSize=r.size;m.memoryTypeIndex=type;Vk(vkAllocateMemory(d.device,&m,nullptr,&memories[i]));Vk(vkBindImageMemory(d.device,images[i],memories[i],0));
   VkImageViewCreateInfo v{VK_STRUCTURE_TYPE_IMAGE_VIEW_CREATE_INFO};v.image=images[i];v.viewType=VK_IMAGE_VIEW_TYPE_2D;v.format=c.format;v.subresourceRange={VkImageAspectFlags(i==1?VK_IMAGE_ASPECT_DEPTH_BIT:VK_IMAGE_ASPECT_COLOR_BIT),0,1,0,1};Vk(vkCreateImageView(d.device,&v,nullptr,&views[i]));
  }
  VkAttachmentDescription at[5]{};for(int i=0;i<5;i++){at[i].format=formats[i];at[i].samples=VK_SAMPLE_COUNT_1_BIT;at[i].loadOp=VK_ATTACHMENT_LOAD_OP_CLEAR;at[i].storeOp=VK_ATTACHMENT_STORE_OP_STORE;at[i].initialLayout=VK_IMAGE_LAYOUT_UNDEFINED;at[i].finalLayout=i==1?VK_IMAGE_LAYOUT_DEPTH_STENCIL_READ_ONLY_OPTIMAL:VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL;}
  VkAttachmentReference opaque{0,VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL},depth{1,VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL},depthRead{1,VK_IMAGE_LAYOUT_DEPTH_STENCIL_READ_ONLY_OPTIMAL};
  VkAttachmentReference accum[2]{{2,VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL},{3,VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL}},inputs[3]{{0,VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL},{2,VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL},{3,VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL}},output{4,VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL};
  uint32_t preserve=0;VkSubpassDescription sub[3]{};for(auto& s:sub)s.pipelineBindPoint=VK_PIPELINE_BIND_POINT_GRAPHICS;
  sub[0].colorAttachmentCount=1;sub[0].pColorAttachments=&opaque;sub[0].pDepthStencilAttachment=&depth;
  sub[1].inputAttachmentCount=1;sub[1].pInputAttachments=&depthRead;sub[1].colorAttachmentCount=2;sub[1].pColorAttachments=accum;sub[1].preserveAttachmentCount=1;sub[1].pPreserveAttachments=&preserve;
  sub[2].inputAttachmentCount=3;sub[2].pInputAttachments=inputs;sub[2].colorAttachmentCount=1;sub[2].pColorAttachments=&output;
  VkSubpassDependency dep[5]{{VK_SUBPASS_EXTERNAL,0,VK_PIPELINE_STAGE_TOP_OF_PIPE_BIT,VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT|VK_PIPELINE_STAGE_EARLY_FRAGMENT_TESTS_BIT,0,VK_ACCESS_COLOR_ATTACHMENT_WRITE_BIT|VK_ACCESS_DEPTH_STENCIL_ATTACHMENT_WRITE_BIT,0},
   {0,1,VK_PIPELINE_STAGE_LATE_FRAGMENT_TESTS_BIT|VK_PIPELINE_STAGE_EARLY_FRAGMENT_TESTS_BIT,VK_PIPELINE_STAGE_FRAGMENT_SHADER_BIT,VK_ACCESS_DEPTH_STENCIL_ATTACHMENT_WRITE_BIT,VK_ACCESS_INPUT_ATTACHMENT_READ_BIT,VK_DEPENDENCY_BY_REGION_BIT},
   {1,2,VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT,VK_PIPELINE_STAGE_FRAGMENT_SHADER_BIT,VK_ACCESS_COLOR_ATTACHMENT_WRITE_BIT,VK_ACCESS_INPUT_ATTACHMENT_READ_BIT,VK_DEPENDENCY_BY_REGION_BIT},
   {0,2,VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT,VK_PIPELINE_STAGE_FRAGMENT_SHADER_BIT,VK_ACCESS_COLOR_ATTACHMENT_WRITE_BIT,VK_ACCESS_INPUT_ATTACHMENT_READ_BIT,VK_DEPENDENCY_BY_REGION_BIT},
   {2,VK_SUBPASS_EXTERNAL,VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT|VK_PIPELINE_STAGE_FRAGMENT_SHADER_BIT,VK_PIPELINE_STAGE_TRANSFER_BIT,VK_ACCESS_COLOR_ATTACHMENT_WRITE_BIT|VK_ACCESS_INPUT_ATTACHMENT_READ_BIT,VK_ACCESS_TRANSFER_READ_BIT,0}};
  VkRenderPassCreateInfo pc{VK_STRUCTURE_TYPE_RENDER_PASS_CREATE_INFO};pc.attachmentCount=5;pc.pAttachments=at;pc.subpassCount=3;pc.pSubpasses=sub;pc.dependencyCount=5;pc.pDependencies=dep;Vk(vkCreateRenderPass(d.device,&pc,nullptr,&pass));
  VkFramebufferCreateInfo fc{VK_STRUCTURE_TYPE_FRAMEBUFFER_CREATE_INFO};fc.renderPass=pass;fc.attachmentCount=5;fc.pAttachments=views;fc.width=W;fc.height=H;fc.layers=1;Vk(vkCreateFramebuffer(d.device,&fc,nullptr,&fb));
  frame=d.Create(96+3*80,nullptr);readback=d.Create(W*H*26,nullptr,VK_BUFFER_USAGE_TRANSFER_DST_BIT);
  float points[8][3];for(int i=0;i<8;i++){points[i][0]=float(i&1);points[i][1]=(i&2)?1.f:-1.f;points[i][2]=(i&4)?1.f:-1.f;}
  const int indices[]{0,4,6,0,6,2,1,3,7,1,7,5,0,1,5,0,5,4,2,6,7,2,7,3,0,2,3,0,3,1,4,5,7,4,7,6};std::vector<float> v;for(auto i:indices)v.insert(v.end(),points[i],points[i]+3);vertices=d.Create(v.size()*4,v.data(),VK_BUFFER_USAGE_VERTEX_BUFFER_BIT);
  VkDescriptorSetLayoutBinding bindings[5]{{0,VK_DESCRIPTOR_TYPE_STORAGE_BUFFER,1,VK_SHADER_STAGE_VERTEX_BIT|VK_SHADER_STAGE_FRAGMENT_BIT,nullptr}};for(int i=1;i<5;i++)bindings[i]={uint32_t(i==1?7:57+i),VK_DESCRIPTOR_TYPE_INPUT_ATTACHMENT,1,VK_SHADER_STAGE_FRAGMENT_BIT,nullptr};
  VkDescriptorSetLayoutCreateInfo lc{VK_STRUCTURE_TYPE_DESCRIPTOR_SET_LAYOUT_CREATE_INFO};lc.bindingCount=5;lc.pBindings=bindings;Vk(vkCreateDescriptorSetLayout(d.device,&lc,nullptr,&d.layout));
  VkDescriptorPoolSize sizes[]{{VK_DESCRIPTOR_TYPE_STORAGE_BUFFER,1},{VK_DESCRIPTOR_TYPE_INPUT_ATTACHMENT,4}};VkDescriptorPoolCreateInfo dp{VK_STRUCTURE_TYPE_DESCRIPTOR_POOL_CREATE_INFO};dp.maxSets=1;dp.poolSizeCount=2;dp.pPoolSizes=sizes;Vk(vkCreateDescriptorPool(d.device,&dp,nullptr,&d.descriptors));
  VkDescriptorSetAllocateInfo sa{VK_STRUCTURE_TYPE_DESCRIPTOR_SET_ALLOCATE_INFO};sa.descriptorPool=d.descriptors;sa.descriptorSetCount=1;sa.pSetLayouts=&d.layout;Vk(vkAllocateDescriptorSets(d.device,&sa,&set));
  VkDescriptorBufferInfo bi{frame.buffer,0,336};VkDescriptorImageInfo ii[4]{};VkWriteDescriptorSet wr[5]{};for(int i=0;i<5;i++){wr[i].sType=VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET;wr[i].dstSet=set;wr[i].dstBinding=bindings[i].binding;wr[i].descriptorType=bindings[i].descriptorType;wr[i].descriptorCount=1;if(i==0)wr[i].pBufferInfo=&bi;else{ii[i-1].imageView=views[i-1];ii[i-1].imageLayout=i==2?VK_IMAGE_LAYOUT_DEPTH_STENCIL_READ_ONLY_OPTIMAL:VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL;wr[i].pImageInfo=&ii[i-1];}}vkUpdateDescriptorSets(d.device,5,wr,0,nullptr);
  VkPushConstantRange range{VK_SHADER_STAGE_VERTEX_BIT|VK_SHADER_STAGE_FRAGMENT_BIT,0,48};VkPipelineLayoutCreateInfo pl{VK_STRUCTURE_TYPE_PIPELINE_LAYOUT_CREATE_INFO};pl.setLayoutCount=1;pl.pSetLayouts=&d.layout;pl.pushConstantRangeCount=1;pl.pPushConstantRanges=&range;Vk(vkCreatePipelineLayout(d.device,&pl,nullptr,&d.pipelineLayout));
  volume=Pipeline("exhaust.vert","exhaust.frag",1);resolve=Pipeline("fullscreen.vert","exhaust_resolve.frag",2);
  VkCommandPoolCreateInfo cp{VK_STRUCTURE_TYPE_COMMAND_POOL_CREATE_INFO};cp.queueFamilyIndex=d.family;cp.flags=VK_COMMAND_POOL_CREATE_RESET_COMMAND_BUFFER_BIT;Vk(vkCreateCommandPool(d.device,&cp,nullptr,&d.pool));VkCommandBufferAllocateInfo ca{VK_STRUCTURE_TYPE_COMMAND_BUFFER_ALLOCATE_INFO};ca.commandPool=d.pool;ca.level=VK_COMMAND_BUFFER_LEVEL_PRIMARY;ca.commandBufferCount=1;Vk(vkAllocateCommandBuffers(d.device,&ca,&cmd));
  auto m=static_cast<float*>(frame.mapped)+8;m[0]=1;m[5]=-1;m[11]=-1;m[14]=.1f;
 }
 VkShaderModule Module(std::string name){std::ifstream f(shaders+"/"+name+".spv",std::ios::binary|std::ios::ate);Require(bool(f),"production shader absent");auto n=f.tellg();std::vector<uint32_t>w(size_t(n)/4);f.seekg(0);f.read((char*)w.data(),n);VkShaderModuleCreateInfo c{VK_STRUCTURE_TYPE_SHADER_MODULE_CREATE_INFO};c.codeSize=size_t(n);c.pCode=w.data();VkShaderModule m;Vk(vkCreateShaderModule(d.device,&c,nullptr,&m));return m;}
 VkPipeline Pipeline(std::string vs,std::string fs,int sub){
  auto v=Module(vs),f=Module(fs);VkPipelineShaderStageCreateInfo stages[2]{{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_VERTEX_BIT,v,"main"},{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_FRAGMENT_BIT,f,"main"}};
  VkVertexInputBindingDescription vb{0,12,VK_VERTEX_INPUT_RATE_VERTEX};VkVertexInputAttributeDescription va{0,0,VK_FORMAT_R32G32B32_SFLOAT,0};VkPipelineVertexInputStateCreateInfo vi{VK_STRUCTURE_TYPE_PIPELINE_VERTEX_INPUT_STATE_CREATE_INFO};if(sub==1){vi.vertexBindingDescriptionCount=1;vi.pVertexBindingDescriptions=&vb;vi.vertexAttributeDescriptionCount=1;vi.pVertexAttributeDescriptions=&va;}
  VkPipelineInputAssemblyStateCreateInfo ia{VK_STRUCTURE_TYPE_PIPELINE_INPUT_ASSEMBLY_STATE_CREATE_INFO};ia.topology=VK_PRIMITIVE_TOPOLOGY_TRIANGLE_LIST;VkViewport vp{0,0,float(W),float(H),0,1};VkRect2D sc{{0,0},{W,H}};VkPipelineViewportStateCreateInfo view{VK_STRUCTURE_TYPE_PIPELINE_VIEWPORT_STATE_CREATE_INFO};view.viewportCount=view.scissorCount=1;view.pViewports=&vp;view.pScissors=&sc;
  VkPipelineRasterizationStateCreateInfo rs{VK_STRUCTURE_TYPE_PIPELINE_RASTERIZATION_STATE_CREATE_INFO};rs.polygonMode=VK_POLYGON_MODE_FILL;rs.lineWidth=1;rs.cullMode=sub==1?VK_CULL_MODE_FRONT_BIT:VK_CULL_MODE_NONE;rs.frontFace=VK_FRONT_FACE_COUNTER_CLOCKWISE;VkPipelineMultisampleStateCreateInfo ms{VK_STRUCTURE_TYPE_PIPELINE_MULTISAMPLE_STATE_CREATE_INFO};ms.rasterizationSamples=VK_SAMPLE_COUNT_1_BIT;
  VkPipelineColorBlendAttachmentState ba[2]{};for(auto& b:ba){b.colorWriteMask=15;b.blendEnable=sub==1;b.srcColorBlendFactor=b.dstColorBlendFactor=b.srcAlphaBlendFactor=b.dstAlphaBlendFactor=VK_BLEND_FACTOR_ONE;}
  VkPipelineColorBlendStateCreateInfo blend{VK_STRUCTURE_TYPE_PIPELINE_COLOR_BLEND_STATE_CREATE_INFO};blend.attachmentCount=sub==1?2:1;blend.pAttachments=ba;VkGraphicsPipelineCreateInfo gp{VK_STRUCTURE_TYPE_GRAPHICS_PIPELINE_CREATE_INFO};gp.stageCount=2;gp.pStages=stages;gp.pVertexInputState=&vi;gp.pInputAssemblyState=&ia;gp.pViewportState=&view;gp.pRasterizationState=&rs;gp.pMultisampleState=&ms;gp.pColorBlendState=&blend;gp.layout=d.pipelineLayout;gp.renderPass=pass;gp.subpass=sub;VkPipeline result;Vk(vkCreateGraphicsPipelines(d.device,{},1,&gp,nullptr,&result));vkDestroyShaderModule(d.device,v,nullptr);vkDestroyShaderModule(d.device,f,nullptr);return result;
 }
 static float Half(uint16_t h){int sign=h>>15,e=(h>>10)&31,m=h&1023;float v=e?std::ldexp(1.f+m/1024.f,e-15):std::ldexp(float(m),-24);return sign?-v:v;}
 struct Result{std::vector<float> rgba,depth,accum;double depthSum{},lightSum{};};
 Result Draw(std::vector<NcRenderObject> objects,float opaque=0,float seedDepth=0,std::array<float,4> seedRadiance={}){
  if(!objects.empty())std::memcpy(static_cast<char*>(frame.mapped)+96,objects.data(),objects.size()*80);
  Vk(vkResetCommandBuffer(cmd,0));VkCommandBufferBeginInfo bc{VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO};Vk(vkBeginCommandBuffer(cmd,&bc));VkClearValue clear[5]{};clear[0].color={{.25f,.25f,.25f,1}};clear[1].depthStencil={opaque,0};for(int j=0;j<4;j++)clear[2].color.float32[j]=seedRadiance[j];clear[3].color.float32[0]=seedDepth;
  VkRenderPassBeginInfo rp{VK_STRUCTURE_TYPE_RENDER_PASS_BEGIN_INFO};rp.renderPass=pass;rp.framebuffer=fb;rp.renderArea={{0,0},{W,H}};rp.clearValueCount=5;rp.pClearValues=clear;vkCmdBeginRenderPass(cmd,&rp,VK_SUBPASS_CONTENTS_INLINE);vkCmdNextSubpass(cmd,VK_SUBPASS_CONTENTS_INLINE);
  vkCmdBindDescriptorSets(cmd,VK_PIPELINE_BIND_POINT_GRAPHICS,d.pipelineLayout,0,1,&set,0,nullptr);vkCmdBindPipeline(cmd,VK_PIPELINE_BIND_POINT_GRAPHICS,volume);VkDeviceSize offset=0;vkCmdBindVertexBuffers(cmd,0,1,&vertices.buffer,&offset);if(!objects.empty())vkCmdDraw(cmd,36,uint32_t(objects.size()),0,0);
  vkCmdNextSubpass(cmd,VK_SUBPASS_CONTENTS_INLINE);vkCmdBindPipeline(cmd,VK_PIPELINE_BIND_POINT_GRAPHICS,resolve);float push[12]{};push[3]=1;vkCmdPushConstants(cmd,d.pipelineLayout,VK_SHADER_STAGE_VERTEX_BIT|VK_SHADER_STAGE_FRAGMENT_BIT,0,48,push);vkCmdDraw(cmd,3,1,0,0);vkCmdEndRenderPass(cmd);
  VkDeviceSize start=0;for(auto i:{4,2,3}){VkBufferImageCopy c{};c.bufferOffset=start;c.imageSubresource={VK_IMAGE_ASPECT_COLOR_BIT,0,0,1};c.imageExtent={W,H,1};vkCmdCopyImageToBuffer(cmd,images[i],VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL,readback.buffer,1,&c);start+=W*H*(i==4?16:i==2?8:2);}
  VkMemoryBarrier mb{VK_STRUCTURE_TYPE_MEMORY_BARRIER};mb.srcAccessMask=VK_ACCESS_TRANSFER_WRITE_BIT;mb.dstAccessMask=VK_ACCESS_HOST_READ_BIT;vkCmdPipelineBarrier(cmd,VK_PIPELINE_STAGE_TRANSFER_BIT,VK_PIPELINE_STAGE_HOST_BIT,0,1,&mb,0,nullptr,0,nullptr);Vk(vkEndCommandBuffer(cmd));VkSubmitInfo si{VK_STRUCTURE_TYPE_SUBMIT_INFO};si.commandBufferCount=1;si.pCommandBuffers=&cmd;Vk(vkQueueSubmit(d.queue,1,&si,{}));Vk(vkQueueWaitIdle(d.queue));
  Result result;auto f=static_cast<float*>(readback.mapped);result.rgba.assign(f,f+W*H*4);auto halves=reinterpret_cast<uint16_t*>(f+W*H*4);for(unsigned i=0;i<W*H*4;i++)result.accum.push_back(Half(halves[i]));halves+=W*H*4;for(unsigned i=0;i<W*H;i++){result.depth.push_back(Half(halves[i]));result.depthSum+=result.depth.back();result.lightSum+=result.accum[4*i];}for(float x:result.rgba)Require(std::isfinite(x),"nonfinite resolved pixel");return result;
 }
 ~ExhaustTest(){vkDeviceWaitIdle(d.device);vkDestroyPipeline(d.device,volume,nullptr);vkDestroyPipeline(d.device,resolve,nullptr);vkDestroyFramebuffer(d.device,fb,nullptr);vkDestroyRenderPass(d.device,pass,nullptr);for(int i=0;i<5;i++){vkDestroyImageView(d.device,views[i],nullptr);vkDestroyImage(d.device,images[i],nullptr);vkFreeMemory(d.device,memories[i],nullptr);}}
};
NcRenderObject Object(uint32_t profile,uint32_t seed=0){NcRenderObject o{};o.position.high[0]=-.5f;o.position.high[2]=-3;o.transform.rotation[3]=1;o.transform.scale[0]=o.transform.scale[1]=o.transform.scale[2]=1;o.padding[0]=0;float speed=3072.f;std::memcpy(&o.padding[1],&speed,4);o.padding[2]=profile==1?0:seed+1;return o;}
float Tone(float x){float a=std::clamp(x*(2.51f*x+.03f)/(x*(2.43f*x+.59f)+.14f),0.f,1.f);return a<.0031308f?12.92f*a:1.055f*std::pow(a,1/2.4f)-.055f;}
int main(int argc,char**argv){try{
 Require(argc==2,"expected shader directory");ExhaustTest t(argv[1]);auto off=t.Draw({});auto main=t.Draw({Object(1)});auto jet=t.Draw({Object(2)});Require(main.lightSum>0&&main.depthSum==0,"pure main emission without self absorption");Require(jet.lightSum>0&&jet.depthSum>0,"separate RCS emission and extinction");Require(t.Draw({}).rgba==off.rgba,"off clears prior active frame");
 for(int n=0;n<3;n++){auto r=t.Draw({},0,float(n*std::log(2.0)));float expected=Tone(.25f*std::pow(.5f,float(n)));Require(std::abs(r.rgba[0]-expected)<.001,"optical depth must attenuate background exponentially");}
 Require(t.Draw({},0,0,{.1f,.2f,.3f,1}).rgba[0]>off.rgba[0],"emission independent of extinction");
 auto twice=t.Draw({Object(2),Object(2)});Require(std::abs(twice.depthSum-2*jet.depthSum)<.002*jet.depthSum,"duplicate emitter doubles optical depth");
 auto ab=t.Draw({Object(1),Object(2,4)}),ba=t.Draw({Object(2,4),Object(1)});float orderError=0;for(size_t i=0;i<ab.rgba.size();i++)orderError=std::max(orderError,std::abs(ab.rgba[i]-ba.rgba[i]));Require(orderError<.002,"OIT draw order");
 auto hidden=t.Draw({Object(2)},.1f),cut=t.Draw({Object(2)},.1f/3),behind=t.Draw({Object(2)},.1f/10);Require(hidden.rgba==off.rgba&&hidden.depthSum==0,"opaque before plume must occlude");Require(cut.depthSum>0&&cut.depthSum<jet.depthSum,"opaque through plume truncates ray");Require(behind.rgba==jet.rgba,"opaque behind plume preserves integration");
 auto axial=Object(2);axial.transform.rotation[1]=-std::sqrt(.5f);axial.transform.rotation[3]=std::sqrt(.5f);Require(t.Draw({axial}).lightSum>0,"axial volume view");auto inside=Object(2);inside.position.high[0]=-.4f;inside.position.high[2]=0;Require(t.Draw({inside}).lightSum>0,"camera inside volume");
 Require(Device::validationErrors==0,"Vulkan validation errors");std::cout<<"EXHAUST_GPU PASS pure-emission separate-extinction optical-depth-resolve off-clear order-independent depth-before/inside/behind axial inside-camera RCS_depth_sum="<<jet.depthSum<<" duplicate="<<twice.depthSum<<" order_max="<<orderError<<" validation=0\n";
 return 0;
 }catch(const std::exception&e){std::cerr<<"EXHAUST_GPU FAIL "<<e.what()<<'\n';return 1;}}
