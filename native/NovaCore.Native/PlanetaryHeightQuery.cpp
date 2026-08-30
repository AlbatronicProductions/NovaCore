#include "PlanetaryHeightQuery.h"
#include "ProductionCubeSurface.h"
#include "LocalTerrainPack.h"

#include <vulkan/vulkan.h>
#include <algorithm>
#include <array>
#include <chrono>
#include <cmath>
#include <cstring>
#include <fstream>
#include <stdexcept>
#include <string>
#include <vector>

static_assert(sizeof(NcPlanetaryHeightQuery)==96);
static_assert(offsetof(NcPlanetaryHeightQuery,anchorHigh)==0);
static_assert(offsetof(NcPlanetaryHeightQuery,anchorLow)==16);
static_assert(offsetof(NcPlanetaryHeightQuery,localDelta)==32);
static_assert(offsetof(NcPlanetaryHeightQuery,oracleUv)==48);
static_assert(offsetof(NcPlanetaryHeightQuery,identity)==64);
static_assert(offsetof(NcPlanetaryHeightQuery,metadata)==80);
static_assert(sizeof(NcPlanetaryHeightResult)==224);
static_assert(offsetof(NcPlanetaryHeightResult,faceUv)==32);
static_assert(offsetof(NcPlanetaryHeightResult,oracleAndTerrainV5Height)==48);
static_assert(offsetof(NcPlanetaryHeightResult,localAndPhysicalHeight)==64);
static_assert(offsetof(NcPlanetaryHeightResult,baseAndModifierHeight)==80);
static_assert(offsetof(NcPlanetaryHeightResult,modifierHeights)==96);
static_assert(offsetof(NcPlanetaryHeightResult,finalGradient)==112);
static_assert(offsetof(NcPlanetaryHeightResult,physicalNormalAndWeight)==128);
static_assert(offsetof(NcPlanetaryHeightResult,reconstructedXY)==144);
static_assert(offsetof(NcPlanetaryHeightResult,reconstructedZAndLength)==160);
static_assert(offsetof(NcPlanetaryHeightResult,globalIdentity)==176);
static_assert(offsetof(NcPlanetaryHeightResult,localIdentity)==192);
static_assert(offsetof(NcPlanetaryHeightResult,source)==208);

namespace {
constexpr uint32_t OracleWidth=8192,OracleHeight=4096,StoredExtent=264;

void Check(VkResult result,const char* message){if(result!=VK_SUCCESS)throw std::runtime_error(message);}
std::vector<uint32_t> ReadWords(const char* path){
  if(!path||!*path)throw std::runtime_error("height query path missing");std::ifstream input(path,std::ios::binary|std::ios::ate);
  if(!input)throw std::runtime_error("height query file unavailable");const auto bytes=input.tellg();
  if(bytes<=0||bytes%4)throw std::runtime_error("height query file size invalid");input.seekg(0);
  std::vector<uint32_t> result(static_cast<size_t>(bytes)/4);if(!input.read(reinterpret_cast<char*>(result.data()),bytes))throw std::runtime_error("height query file read failed");return result;
}
std::vector<uint32_t> ReadSpirv(const char* path){return ReadWords(path);}

std::vector<uint8_t> DecodeBc4(const std::vector<uint8_t>& blocks){
  if(blocks.size()!=nc::localterrain::Bc4Bytes)throw std::runtime_error("local BC4 payload size mismatch");
  std::vector<uint8_t> result(StoredExtent*StoredExtent);std::array<uint8_t,8> palette{};const uint32_t blocksPerRow=StoredExtent/4;
  for(uint32_t block=0;block<blocks.size()/8;block++){
    const auto* source=blocks.data()+block*8;palette[0]=source[0];palette[1]=source[1];
    if(palette[0]>palette[1])for(uint32_t index=1;index<7;index++)palette[index+1]=uint8_t(((7-index)*palette[0]+index*palette[1]+3)/7);
    else{for(uint32_t index=1;index<5;index++)palette[index+1]=uint8_t(((5-index)*palette[0]+index*palette[1]+2)/5);palette[6]=0;palette[7]=255;}
    uint64_t indices=0;for(uint32_t index=0;index<6;index++)indices|=uint64_t(source[index+2])<<(8*index);
    const uint32_t blockX=block%blocksPerRow,blockY=block/blocksPerRow;
    for(uint32_t pixel=0;pixel<16;pixel++)result[(blockY*4+pixel/4)*StoredExtent+blockX*4+pixel%4]=palette[(indices>>(3*pixel))&7u];
  }return result;
}
void PackU16(const std::vector<uint16_t>& source,std::vector<uint32_t>& target){
  const size_t start=target.size();target.resize(start+(source.size()+1)/2);
  for(size_t index=0;index<source.size();index++)target[start+index/2]|=uint32_t(source[index])<<((index&1)*16);
}
void PackU8(const std::vector<uint8_t>& source,std::vector<uint32_t>& target){
  const size_t start=target.size();target.resize(start+(source.size()+3)/4);
  for(size_t index=0;index<source.size();index++)target[start+index/4]|=uint32_t(source[index])<<((index&3)*8);
}

struct LocalMetadata{uint32_t face,level,x,y;uint32_t payloadIndex,detailFrequency,payloadVersion,reserved;};
struct QueryConstants{uint32_t queryCount,oracleWidth,oracleHeight,globalMaximumLevel;uint32_t globalWordsPerRecord,localWordsPerRecord,localRecordCount,terrainVersion;float localMinimum,localMaximum,pad0,pad1;};
struct Buffer{VkBuffer buffer{};VkDeviceMemory memory{};void* mapped{};VkDeviceSize bytes{};};

VKAPI_ATTR VkBool32 VKAPI_CALL DebugCallback(VkDebugUtilsMessageSeverityFlagBitsEXT severity,VkDebugUtilsMessageTypeFlagsEXT,const VkDebugUtilsMessengerCallbackDataEXT*,void* user){
  if(severity&VK_DEBUG_UTILS_MESSAGE_SEVERITY_ERROR_BIT_EXT)(*static_cast<uint32_t*>(user))++;return VK_FALSE;
}

struct Context{
  VkInstance instance{};VkDebugUtilsMessengerEXT messenger{};VkPhysicalDevice physical{};VkDevice device{};VkQueue queue{};uint32_t queueFamily{};
  VkDescriptorSetLayout descriptorLayout{};VkPipelineLayout pipelineLayout{};VkPipeline pipeline{};VkDescriptorPool descriptorPool{};VkCommandPool commandPool{};VkFence fence{};VkQueryPool queryPool{};
  std::array<Buffer,6> buffers{};uint32_t validationErrors{};float timestampPeriod{};
  ~Context(){
    if(device)vkDeviceWaitIdle(device);for(auto& value:buffers){if(value.mapped)vkUnmapMemory(device,value.memory);if(value.buffer)vkDestroyBuffer(device,value.buffer,nullptr);if(value.memory)vkFreeMemory(device,value.memory,nullptr);}
    if(queryPool)vkDestroyQueryPool(device,queryPool,nullptr);if(fence)vkDestroyFence(device,fence,nullptr);if(commandPool)vkDestroyCommandPool(device,commandPool,nullptr);if(descriptorPool)vkDestroyDescriptorPool(device,descriptorPool,nullptr);if(pipeline)vkDestroyPipeline(device,pipeline,nullptr);if(pipelineLayout)vkDestroyPipelineLayout(device,pipelineLayout,nullptr);if(descriptorLayout)vkDestroyDescriptorSetLayout(device,descriptorLayout,nullptr);if(device)vkDestroyDevice(device,nullptr);
    if(messenger&&instance){auto destroy=reinterpret_cast<PFN_vkDestroyDebugUtilsMessengerEXT>(vkGetInstanceProcAddr(instance,"vkDestroyDebugUtilsMessengerEXT"));if(destroy)destroy(instance,messenger,nullptr);}if(instance)vkDestroyInstance(instance,nullptr);
  }
};
uint32_t MemoryType(VkPhysicalDevice physical,uint32_t bits){VkPhysicalDeviceMemoryProperties properties{};vkGetPhysicalDeviceMemoryProperties(physical,&properties);for(uint32_t index=0;index<properties.memoryTypeCount;index++)if((bits&(1u<<index))&&(properties.memoryTypes[index].propertyFlags&(VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT|VK_MEMORY_PROPERTY_HOST_COHERENT_BIT))==(VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT|VK_MEMORY_PROPERTY_HOST_COHERENT_BIT))return index;throw std::runtime_error("coherent query memory unavailable");}
void CreateBuffer(Context& c,Buffer& value,VkDeviceSize bytes){
  value.bytes=std::max<VkDeviceSize>(bytes,4);VkBufferCreateInfo create{VK_STRUCTURE_TYPE_BUFFER_CREATE_INFO};create.size=value.bytes;create.usage=VK_BUFFER_USAGE_STORAGE_BUFFER_BIT;create.sharingMode=VK_SHARING_MODE_EXCLUSIVE;Check(vkCreateBuffer(c.device,&create,nullptr,&value.buffer),"height query buffer failed");VkMemoryRequirements requirements{};vkGetBufferMemoryRequirements(c.device,value.buffer,&requirements);VkMemoryAllocateInfo allocation{VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO};allocation.allocationSize=requirements.size;allocation.memoryTypeIndex=MemoryType(c.physical,requirements.memoryTypeBits);Check(vkAllocateMemory(c.device,&allocation,nullptr,&value.memory),"height query memory failed");Check(vkBindBufferMemory(c.device,value.buffer,value.memory,0),"height query bind failed");Check(vkMapMemory(c.device,value.memory,0,value.bytes,0,&value.mapped),"height query map failed");std::memset(value.mapped,0,static_cast<size_t>(value.bytes));
}
bool HasLayer(const char* name){uint32_t count=0;vkEnumerateInstanceLayerProperties(&count,nullptr);std::vector<VkLayerProperties> values(count);vkEnumerateInstanceLayerProperties(&count,values.data());return std::any_of(values.begin(),values.end(),[&](const auto& value){return std::strcmp(value.layerName,name)==0;});}
void CreateContext(Context& c){
  const bool validation=HasLayer("VK_LAYER_KHRONOS_validation");const char* layer="VK_LAYER_KHRONOS_validation";const char* extension=VK_EXT_DEBUG_UTILS_EXTENSION_NAME;
  VkApplicationInfo app{VK_STRUCTURE_TYPE_APPLICATION_INFO};app.pApplicationName="NovaCore physical height query";app.apiVersion=VK_API_VERSION_1_2;
  VkInstanceCreateInfo instance{VK_STRUCTURE_TYPE_INSTANCE_CREATE_INFO};instance.pApplicationInfo=&app;if(validation){instance.enabledLayerCount=1;instance.ppEnabledLayerNames=&layer;instance.enabledExtensionCount=1;instance.ppEnabledExtensionNames=&extension;}Check(vkCreateInstance(&instance,nullptr,&c.instance),"height query Vulkan instance failed");
  if(validation){VkDebugUtilsMessengerCreateInfoEXT debug{VK_STRUCTURE_TYPE_DEBUG_UTILS_MESSENGER_CREATE_INFO_EXT};debug.messageSeverity=VK_DEBUG_UTILS_MESSAGE_SEVERITY_ERROR_BIT_EXT;debug.messageType=VK_DEBUG_UTILS_MESSAGE_TYPE_GENERAL_BIT_EXT|VK_DEBUG_UTILS_MESSAGE_TYPE_VALIDATION_BIT_EXT|VK_DEBUG_UTILS_MESSAGE_TYPE_PERFORMANCE_BIT_EXT;debug.pfnUserCallback=DebugCallback;debug.pUserData=&c.validationErrors;auto create=reinterpret_cast<PFN_vkCreateDebugUtilsMessengerEXT>(vkGetInstanceProcAddr(c.instance,"vkCreateDebugUtilsMessengerEXT"));if(create)Check(create(c.instance,&debug,nullptr,&c.messenger),"height query validation messenger failed");}
  uint32_t physicalCount=0;Check(vkEnumeratePhysicalDevices(c.instance,&physicalCount,nullptr),"height query physical enumeration failed");if(!physicalCount)throw std::runtime_error("height query physical device missing");std::vector<VkPhysicalDevice> devices(physicalCount);Check(vkEnumeratePhysicalDevices(c.instance,&physicalCount,devices.data()),"height query physical enumeration failed");
  for(auto device:devices){VkPhysicalDeviceFeatures features{};vkGetPhysicalDeviceFeatures(device,&features);if(!features.shaderFloat64)continue;uint32_t familyCount=0;vkGetPhysicalDeviceQueueFamilyProperties(device,&familyCount,nullptr);std::vector<VkQueueFamilyProperties> families(familyCount);vkGetPhysicalDeviceQueueFamilyProperties(device,&familyCount,families.data());for(uint32_t index=0;index<familyCount;index++)if(families[index].queueFlags&VK_QUEUE_COMPUTE_BIT){c.physical=device;c.queueFamily=index;break;}if(c.physical)break;}if(!c.physical)throw std::runtime_error("height query shaderFloat64 compute device unavailable");
  VkPhysicalDeviceProperties properties{};vkGetPhysicalDeviceProperties(c.physical,&properties);c.timestampPeriod=properties.limits.timestampPeriod;
  float priority=1;VkDeviceQueueCreateInfo queue{VK_STRUCTURE_TYPE_DEVICE_QUEUE_CREATE_INFO};queue.queueFamilyIndex=c.queueFamily;queue.queueCount=1;queue.pQueuePriorities=&priority;VkPhysicalDeviceFeatures features{};features.shaderFloat64=VK_TRUE;VkDeviceCreateInfo device{VK_STRUCTURE_TYPE_DEVICE_CREATE_INFO};device.queueCreateInfoCount=1;device.pQueueCreateInfos=&queue;device.pEnabledFeatures=&features;Check(vkCreateDevice(c.physical,&device,nullptr,&c.device),"height query device failed");vkGetDeviceQueue(c.device,c.queueFamily,0,&c.queue);
}
}

NcResult RunPlanetaryHeightQueries(const NcPlanetaryHeightQuery* queries,uint32_t count,NcPlanetaryHeightResult* results,const NcPlanetaryHeightQueryAssets* assets,NcPlanetaryHeightQueryMetrics* metrics){
  if(!queries||!count||count>4096||!results||!assets||assets->size!=sizeof(NcPlanetaryHeightQueryAssets)||assets->version!=1||!metrics||metrics->size!=sizeof(NcPlanetaryHeightQueryMetrics))return NC_INVALID_ARGUMENT;
  const auto started=std::chrono::steady_clock::now();try{
    if(!assets->elevationOraclePathUtf8||!assets->productionTerrainPathUtf8||!assets->localTerrainPathUtf8||!assets->computeShaderPathUtf8)return NC_INVALID_ARGUMENT;
    auto oracle=ReadWords(assets->elevationOraclePathUtf8);if(oracle.size()!=size_t(OracleWidth)*OracleHeight/2)throw std::runtime_error("elevation oracle dimensions mismatch");
    nc::production::Pack globalPack;std::string error;if(!globalPack.Open(assets->productionTerrainPathUtf8,error)||!globalPack.IsProductionLayout())throw std::runtime_error("terrain-v5 query pack invalid: "+error);
    const uint32_t maximumLevel=globalPack.MaximumLevel(),cells=1u<<maximumLevel,globalRecordCount=6u*cells*cells;std::vector<uint32_t> globalWords;globalWords.reserve(size_t(globalRecordCount)*StoredExtent*StoredExtent/2);
    for(uint32_t face=0;face<6;face++)for(uint32_t ordinal=0;ordinal<cells*cells;ordinal++){
      uint32_t x=0,y=0;for(uint32_t bit=0;bit<12;bit++){x|=((ordinal>>(2*bit))&1u)<<bit;y|=((ordinal>>(2*bit+1))&1u)<<bit;}
      nc::production::Payload payload;if(!globalPack.Read({6,5,face,maximumLevel,x,y},payload,error))throw std::runtime_error("terrain-v5 query payload invalid: "+error);PackU16(payload.elevation,globalWords);
    }
    nc::localterrain::Pack localPack;if(!localPack.Open(assets->localTerrainPathUtf8,error)||!localPack.IsProductionLayout())throw std::runtime_error("local-v2 query pack invalid: "+error);
    std::vector<LocalMetadata> localMetadata;std::vector<uint32_t> localWords;localMetadata.reserve(localPack.RecordCount());localWords.reserve(size_t(localPack.RecordCount())*StoredExtent*StoredExtent/4);
    for(const auto& record:localPack.Records()){
      nc::localterrain::Payload payload;if(!localPack.Read(record.id,payload,error))throw std::runtime_error("local-v2 query payload invalid: "+error);const auto decoded=DecodeBc4(payload.elevationBc4);localMetadata.push_back({record.id.face,record.id.level,record.id.x,record.id.y,static_cast<uint32_t>(localMetadata.size()),record.id.detailFrequency,record.id.payloadVersion,0});PackU8(decoded,localWords);
    }
    Context context;CreateContext(context);CreateBuffer(context,context.buffers[0],sizeof(NcPlanetaryHeightQuery)*count);CreateBuffer(context,context.buffers[1],sizeof(NcPlanetaryHeightResult)*count);CreateBuffer(context,context.buffers[2],oracle.size()*4);CreateBuffer(context,context.buffers[3],globalWords.size()*4);CreateBuffer(context,context.buffers[4],localMetadata.size()*sizeof(LocalMetadata));CreateBuffer(context,context.buffers[5],localWords.size()*4);
    std::memcpy(context.buffers[0].mapped,queries,sizeof(NcPlanetaryHeightQuery)*count);std::memset(context.buffers[1].mapped,0xcd,sizeof(NcPlanetaryHeightResult)*count);std::memcpy(context.buffers[2].mapped,oracle.data(),oracle.size()*4);std::memcpy(context.buffers[3].mapped,globalWords.data(),globalWords.size()*4);std::memcpy(context.buffers[4].mapped,localMetadata.data(),localMetadata.size()*sizeof(LocalMetadata));std::memcpy(context.buffers[5].mapped,localWords.data(),localWords.size()*4);
    std::array<VkDescriptorSetLayoutBinding,6> bindings{};for(uint32_t index=0;index<bindings.size();index++){bindings[index].binding=index;bindings[index].descriptorType=VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;bindings[index].descriptorCount=1;bindings[index].stageFlags=VK_SHADER_STAGE_COMPUTE_BIT;}VkDescriptorSetLayoutCreateInfo descriptorLayout{VK_STRUCTURE_TYPE_DESCRIPTOR_SET_LAYOUT_CREATE_INFO};descriptorLayout.bindingCount=bindings.size();descriptorLayout.pBindings=bindings.data();Check(vkCreateDescriptorSetLayout(context.device,&descriptorLayout,nullptr,&context.descriptorLayout),"height query descriptor layout failed");
    VkPushConstantRange range{VK_SHADER_STAGE_COMPUTE_BIT,0,sizeof(QueryConstants)};VkPipelineLayoutCreateInfo pipelineLayout{VK_STRUCTURE_TYPE_PIPELINE_LAYOUT_CREATE_INFO};pipelineLayout.setLayoutCount=1;pipelineLayout.pSetLayouts=&context.descriptorLayout;pipelineLayout.pushConstantRangeCount=1;pipelineLayout.pPushConstantRanges=&range;Check(vkCreatePipelineLayout(context.device,&pipelineLayout,nullptr,&context.pipelineLayout),"height query pipeline layout failed");
    auto spirv=ReadSpirv(assets->computeShaderPathUtf8);VkShaderModuleCreateInfo moduleCreate{VK_STRUCTURE_TYPE_SHADER_MODULE_CREATE_INFO};moduleCreate.codeSize=spirv.size()*4;moduleCreate.pCode=spirv.data();VkShaderModule module{};Check(vkCreateShaderModule(context.device,&moduleCreate,nullptr,&module),"height query shader module failed");VkPipelineShaderStageCreateInfo stage{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO};stage.stage=VK_SHADER_STAGE_COMPUTE_BIT;stage.module=module;stage.pName="main";VkComputePipelineCreateInfo pipeline{VK_STRUCTURE_TYPE_COMPUTE_PIPELINE_CREATE_INFO};pipeline.stage=stage;pipeline.layout=context.pipelineLayout;const auto pipelineResult=vkCreateComputePipelines(context.device,{},1,&pipeline,nullptr,&context.pipeline);vkDestroyShaderModule(context.device,module,nullptr);Check(pipelineResult,"height query compute pipeline failed");
    VkDescriptorPoolSize poolSize{VK_DESCRIPTOR_TYPE_STORAGE_BUFFER,6};VkDescriptorPoolCreateInfo pool{VK_STRUCTURE_TYPE_DESCRIPTOR_POOL_CREATE_INFO};pool.maxSets=1;pool.poolSizeCount=1;pool.pPoolSizes=&poolSize;Check(vkCreateDescriptorPool(context.device,&pool,nullptr,&context.descriptorPool),"height query descriptor pool failed");VkDescriptorSetAllocateInfo allocate{VK_STRUCTURE_TYPE_DESCRIPTOR_SET_ALLOCATE_INFO};allocate.descriptorPool=context.descriptorPool;allocate.descriptorSetCount=1;allocate.pSetLayouts=&context.descriptorLayout;VkDescriptorSet descriptor{};Check(vkAllocateDescriptorSets(context.device,&allocate,&descriptor),"height query descriptor allocation failed");std::array<VkDescriptorBufferInfo,6> infos{};std::array<VkWriteDescriptorSet,6> writes{};for(uint32_t index=0;index<6;index++){infos[index]={context.buffers[index].buffer,0,context.buffers[index].bytes};writes[index]={VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET,nullptr,descriptor,index,0,1,VK_DESCRIPTOR_TYPE_STORAGE_BUFFER,nullptr,&infos[index],nullptr};}vkUpdateDescriptorSets(context.device,writes.size(),writes.data(),0,nullptr);
    VkCommandPoolCreateInfo commandPool{VK_STRUCTURE_TYPE_COMMAND_POOL_CREATE_INFO};commandPool.queueFamilyIndex=context.queueFamily;Check(vkCreateCommandPool(context.device,&commandPool,nullptr,&context.commandPool),"height query command pool failed");VkCommandBufferAllocateInfo commandAllocate{VK_STRUCTURE_TYPE_COMMAND_BUFFER_ALLOCATE_INFO};commandAllocate.commandPool=context.commandPool;commandAllocate.level=VK_COMMAND_BUFFER_LEVEL_PRIMARY;commandAllocate.commandBufferCount=1;VkCommandBuffer command{};Check(vkAllocateCommandBuffers(context.device,&commandAllocate,&command),"height query command allocation failed");VkFenceCreateInfo fence{VK_STRUCTURE_TYPE_FENCE_CREATE_INFO};Check(vkCreateFence(context.device,&fence,nullptr,&context.fence),"height query fence failed");VkQueryPoolCreateInfo queryPool{VK_STRUCTURE_TYPE_QUERY_POOL_CREATE_INFO};queryPool.queryType=VK_QUERY_TYPE_TIMESTAMP;queryPool.queryCount=2;Check(vkCreateQueryPool(context.device,&queryPool,nullptr,&context.queryPool),"height query timestamps failed");
    QueryConstants constants{count,OracleWidth,OracleHeight,maximumLevel,StoredExtent*StoredExtent/2,StoredExtent*StoredExtent/4,localPack.RecordCount(),5,localPack.ResidualMinimum(),localPack.ResidualMaximum(),0,0};VkCommandBufferBeginInfo begin{VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO};begin.flags=VK_COMMAND_BUFFER_USAGE_ONE_TIME_SUBMIT_BIT;Check(vkBeginCommandBuffer(command,&begin),"height query begin failed");vkCmdResetQueryPool(command,context.queryPool,0,2);VkMemoryBarrier hostToCompute{VK_STRUCTURE_TYPE_MEMORY_BARRIER};hostToCompute.srcAccessMask=VK_ACCESS_HOST_WRITE_BIT;hostToCompute.dstAccessMask=VK_ACCESS_SHADER_READ_BIT;vkCmdPipelineBarrier(command,VK_PIPELINE_STAGE_HOST_BIT,VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,0,1,&hostToCompute,0,nullptr,0,nullptr);vkCmdWriteTimestamp(command,VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,context.queryPool,0);vkCmdBindPipeline(command,VK_PIPELINE_BIND_POINT_COMPUTE,context.pipeline);vkCmdBindDescriptorSets(command,VK_PIPELINE_BIND_POINT_COMPUTE,context.pipelineLayout,0,1,&descriptor,0,nullptr);vkCmdPushConstants(command,context.pipelineLayout,VK_SHADER_STAGE_COMPUTE_BIT,0,sizeof constants,&constants);const uint32_t groups=(count+63u)/64u;vkCmdDispatch(command,groups,1,1);vkCmdWriteTimestamp(command,VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,context.queryPool,1);VkMemoryBarrier computeToHost{VK_STRUCTURE_TYPE_MEMORY_BARRIER};computeToHost.srcAccessMask=VK_ACCESS_SHADER_WRITE_BIT;computeToHost.dstAccessMask=VK_ACCESS_HOST_READ_BIT;vkCmdPipelineBarrier(command,VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,VK_PIPELINE_STAGE_HOST_BIT,0,1,&computeToHost,0,nullptr,0,nullptr);Check(vkEndCommandBuffer(command),"height query end failed");VkSubmitInfo submit{VK_STRUCTURE_TYPE_SUBMIT_INFO};submit.commandBufferCount=1;submit.pCommandBuffers=&command;Check(vkQueueSubmit(context.queue,1,&submit,context.fence),"height query submit failed");Check(vkWaitForFences(context.device,1,&context.fence,VK_TRUE,10'000'000'000ull),"height query wait failed");
    std::memcpy(results,context.buffers[1].mapped,sizeof(NcPlanetaryHeightResult)*count);std::array<uint64_t,2> ticks{};Check(vkGetQueryPoolResults(context.device,context.queryPool,0,2,sizeof ticks,ticks.data(),sizeof(uint64_t),VK_QUERY_RESULT_64_BIT|VK_QUERY_RESULT_WAIT_BIT),"height query timestamps unavailable");
    metrics->version=1;metrics->queryCount=count;metrics->dispatchGroups=groups;metrics->validationErrors=context.validationErrors;metrics->globalRecordCount=globalRecordCount;metrics->localRecordCount=localPack.RecordCount();metrics->reserved=0;metrics->gpuMilliseconds=double(ticks[1]-ticks[0])*context.timestampPeriod/1'000'000.0;metrics->cpuMilliseconds=std::chrono::duration<double,std::milli>(std::chrono::steady_clock::now()-started).count();return context.validationErrors?NC_FAILURE:NC_SUCCESS;
  }catch(...){return NC_FAILURE;}
}
