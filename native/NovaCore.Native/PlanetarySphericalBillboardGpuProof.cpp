#include "PlanetarySphericalBillboardGpuProof.h"
#include <algorithm>
#include <array>
#include <chrono>
#include <cmath>
#include <cstdint>
#include <cstdio>
#include <cstring>
#include <fstream>
#include <memory>
#include <mutex>
#include <stdexcept>
#include <vector>
#include <vulkan/vulkan.h>

namespace {
constexpr uint32_t BindingCount = 12, TimestampCount = 6, MaximumFrames = 3;
struct Buffer {
  VkBuffer buffer{};
  VkDeviceMemory memory{};
  VkDeviceSize bytes{};
  VkDeviceSize allocationBytes{};
  void *mapped{};
};
struct FrameResources {
  Buffer positions, normals, visibility, visibleIndices, indirect, counters,
      readback;
  VkDescriptorSet descriptor{};
  VkCommandBuffer command{};
  VkFence fence{};
  VkQueryPool queries{};
  bool submitted{};
};
struct PushConstants {
  uint32_t baseVertexCount, baseTriangleCount, workVertexCount,
      workTriangleCount;
  float bodyRadius, cameraDistance, tanHalfFov, aspect;
  uint32_t topologyLevel, frameIndex, outputIndexCapacity, reserved;
  uint32_t coordinateEncoding, latticeScale;
};
struct Context {
  VkInstance instance{};
  VkDebugUtilsMessengerEXT messenger{};
  VkPhysicalDevice physical{};
  VkDevice device{};
  VkQueue queue{};
  uint32_t queueFamily{};
  float timestampPeriod{};
  VkDescriptorSetLayout descriptorLayout{};
  VkPipelineLayout pipelineLayout{};
  VkDescriptorPool descriptorPool{};
  VkPipeline resetPipeline{}, preparePipeline{}, normalPipeline{},
      cullPipeline{}, compactPipeline{}, graphicsPipeline{};
  VkCommandPool commandPool{};
  VkRenderPass renderPass{};
  VkImage image{};
  VkDeviceMemory imageMemory{};
  VkDeviceSize imageAllocationBytes{};
  VkImageView imageView{};
  VkFramebuffer framebuffer{};
  Buffer localVertices, indices, neighborOffsets, neighbors, physicalPositions,
      physicalNormals;
  Buffer incomingVertices, incomingIndices, incomingNeighborOffsets,
      incomingNeighbors;
  std::array<FrameResources, MaximumFrames> frames{};
  uint32_t maximumVertices{}, maximumTriangles{}, frameCount{}, extent{},
      validationErrors{}, activeLevel{}, baseVertices{}, baseTriangles{},
      neighborOffsetCount{}, neighborCount{}, coordinateEncoding{}, latticeScale{};
  uint32_t incomingLevel{}, incomingVerticesCount{}, incomingTriangles{},
      incomingNeighborOffsetCount{}, incomingNeighborCount{},
      incomingCoordinateEncoding{}, incomingLatticeScale{}, incomingReadiness{},
      publications{}, deferredRetirements{}, zeroOwnerFrames{},
      overlapOwnerFrames{}, staleGenerationDraws{};
  uint64_t incomingTopologyHash{}, incomingTopologyBytes{};
  uint64_t topologyHash{}, topologyUploads{}, topologyBytesUploaded{},
      activeTopologyBytes{}, frameWrites{}, cullingDispatches{},
      indirectSubmissions{}, replacements{}, frameWaits{};
  uint32_t physicalGeneration{}, terrainDataGeneration{},
      preparedPhysicalSamples{}, physicalPreparationDispatches{},
      physicalReuseCount{}, staleGenerationRejections{},
      nonFinitePhysicalOutputs{};
  double setupMilliseconds{}, uploadMilliseconds{};
  double directionDecodeMaximumErrorRadians{};
  uint32_t readiness{};
  ~Context();
};
std::mutex Guard;
std::unique_ptr<Context> Current;

void Check(VkResult result, const char *message) {
  if (result != VK_SUCCESS)
    throw std::runtime_error(message);
}
std::vector<uint32_t> ReadWords(const char *path) {
  if (!path || !*path)
    throw std::runtime_error("billboard proof shader path missing");
  std::ifstream stream(path, std::ios::binary | std::ios::ate);
  if (!stream)
    throw std::runtime_error("billboard proof shader unavailable");
  const auto bytes = stream.tellg();
  if (bytes <= 0 || bytes % 4)
    throw std::runtime_error("billboard proof shader invalid");
  stream.seekg(0);
  std::vector<uint32_t> result(static_cast<size_t>(bytes) / 4);
  stream.read(reinterpret_cast<char *>(result.data()), bytes);
  if (!stream)
    throw std::runtime_error("billboard proof shader read failed");
  return result;
}
VKAPI_ATTR VkBool32 VKAPI_CALL
DebugCallback(VkDebugUtilsMessageSeverityFlagBitsEXT severity,
              VkDebugUtilsMessageTypeFlagsEXT,
              const VkDebugUtilsMessengerCallbackDataEXT *data, void *user) {
  if (severity & VK_DEBUG_UTILS_MESSAGE_SEVERITY_ERROR_BIT_EXT) {
    ++*static_cast<uint32_t *>(user);
    std::fprintf(stderr, "P2S3 Vulkan validation: %s\n",
                 data && data->pMessage ? data->pMessage : "unknown error");
  }
  return VK_FALSE;
}
uint32_t MemoryType(VkPhysicalDevice physical, uint32_t bits,
                    VkMemoryPropertyFlags required) {
  VkPhysicalDeviceMemoryProperties properties{};
  vkGetPhysicalDeviceMemoryProperties(physical, &properties);
  for (uint32_t i = 0; i < properties.memoryTypeCount; i++)
    if ((bits & (1u << i)) &&
        (properties.memoryTypes[i].propertyFlags & required) == required)
      return i;
  throw std::runtime_error("billboard proof memory type unavailable");
}
void DestroyBuffer(VkDevice device, Buffer &value) {
  if (value.mapped)
    vkUnmapMemory(device, value.memory);
  if (value.buffer)
    vkDestroyBuffer(device, value.buffer, nullptr);
  if (value.memory)
    vkFreeMemory(device, value.memory, nullptr);
  value = {};
}
void CreateBuffer(Context &c, Buffer &value, VkDeviceSize requested,
                  VkBufferUsageFlags usage) {
  value.bytes = std::max<VkDeviceSize>(requested, 4);
  VkBufferCreateInfo create{VK_STRUCTURE_TYPE_BUFFER_CREATE_INFO};
  create.size = value.bytes;
  create.usage = usage | VK_BUFFER_USAGE_STORAGE_BUFFER_BIT;
  create.sharingMode = VK_SHARING_MODE_EXCLUSIVE;
  Check(vkCreateBuffer(c.device, &create, nullptr, &value.buffer),
        "billboard proof buffer creation failed");
  VkMemoryRequirements requirements{};
  vkGetBufferMemoryRequirements(c.device, value.buffer, &requirements);
  VkMemoryAllocateInfo allocation{VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO};
  allocation.allocationSize = requirements.size;
  allocation.memoryTypeIndex =
      MemoryType(c.physical, requirements.memoryTypeBits,
                 VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT |
                     VK_MEMORY_PROPERTY_HOST_COHERENT_BIT);
  Check(vkAllocateMemory(c.device, &allocation, nullptr, &value.memory),
        "billboard proof buffer allocation failed");
  value.allocationBytes = allocation.allocationSize;
  Check(vkBindBufferMemory(c.device, value.buffer, value.memory, 0),
        "billboard proof buffer bind failed");
  Check(vkMapMemory(c.device, value.memory, 0, value.bytes, 0, &value.mapped),
        "billboard proof buffer map failed");
  std::memset(value.mapped, 0, static_cast<size_t>(value.bytes));
}
VkPipeline CreateComputePipeline(Context &c, const char *path) {
  auto words = ReadWords(path);
  VkShaderModuleCreateInfo moduleCreate{
      VK_STRUCTURE_TYPE_SHADER_MODULE_CREATE_INFO};
  moduleCreate.codeSize = words.size() * 4;
  moduleCreate.pCode = words.data();
  VkShaderModule module{};
  Check(vkCreateShaderModule(c.device, &moduleCreate, nullptr, &module),
        "billboard proof compute module failed");
  VkPipelineShaderStageCreateInfo stage{
      VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO};
  stage.stage = VK_SHADER_STAGE_COMPUTE_BIT;
  stage.module = module;
  stage.pName = "main";
  VkComputePipelineCreateInfo create{
      VK_STRUCTURE_TYPE_COMPUTE_PIPELINE_CREATE_INFO};
  create.stage = stage;
  create.layout = c.pipelineLayout;
  VkPipeline pipeline{};
  const auto result =
      vkCreateComputePipelines(c.device, {}, 1, &create, nullptr, &pipeline);
  vkDestroyShaderModule(c.device, module, nullptr);
  Check(result, "billboard proof compute pipeline failed");
  return pipeline;
}
VkShaderModule CreateModule(Context &c, const char *path) {
  auto words = ReadWords(path);
  VkShaderModuleCreateInfo create{VK_STRUCTURE_TYPE_SHADER_MODULE_CREATE_INFO};
  create.codeSize = words.size() * 4;
  create.pCode = words.data();
  VkShaderModule module{};
  Check(vkCreateShaderModule(c.device, &create, nullptr, &module),
        "billboard proof graphics module failed");
  return module;
}
void UpdateDescriptors(Context &c) {
  for (uint32_t frameIndex = 0; frameIndex < c.frameCount; frameIndex++) {
    auto &f = c.frames[frameIndex];
    std::array<Buffer *, BindingCount> buffers{
        &c.localVertices, &c.indices,   &c.neighborOffsets, &c.neighbors,
        &f.positions,     &f.normals,   &f.visibleIndices,  &f.indirect,
        &f.counters,      &f.visibility, &c.physicalPositions,
        &c.physicalNormals};
    std::array<VkDescriptorBufferInfo, BindingCount> infos{};
    std::array<VkWriteDescriptorSet, BindingCount> writes{};
    for (uint32_t i = 0; i < BindingCount; i++) {
      infos[i] = {buffers[i]->buffer, 0, buffers[i]->bytes};
      writes[i] = {VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET,
                   nullptr,
                   f.descriptor,
                   i,
                   0,
                   1,
                   VK_DESCRIPTOR_TYPE_STORAGE_BUFFER,
                   nullptr,
                   &infos[i],
                   nullptr};
    }
    vkUpdateDescriptorSets(c.device, BindingCount, writes.data(), 0, nullptr);
  }
}
void CreateVulkan(Context &c) {
  const char *layer = "VK_LAYER_KHRONOS_validation";
  const char *extension = VK_EXT_DEBUG_UTILS_EXTENSION_NAME;
  uint32_t layerCount = 0;
  vkEnumerateInstanceLayerProperties(&layerCount, nullptr);
  std::vector<VkLayerProperties> layers(layerCount);
  vkEnumerateInstanceLayerProperties(&layerCount, layers.data());
  const bool validation =
      std::any_of(layers.begin(), layers.end(), [&](const auto &value) {
        return std::strcmp(value.layerName, layer) == 0;
      });
  VkApplicationInfo app{VK_STRUCTURE_TYPE_APPLICATION_INFO};
  app.pApplicationName = "NovaCore P2S3 spherical billboard proof";
  app.apiVersion = VK_API_VERSION_1_2;
  VkInstanceCreateInfo instance{VK_STRUCTURE_TYPE_INSTANCE_CREATE_INFO};
  instance.pApplicationInfo = &app;
  if (validation) {
    instance.enabledLayerCount = 1;
    instance.ppEnabledLayerNames = &layer;
    instance.enabledExtensionCount = 1;
    instance.ppEnabledExtensionNames = &extension;
  }
  Check(vkCreateInstance(&instance, nullptr, &c.instance),
        "billboard proof Vulkan instance failed");
  if (validation) {
    VkDebugUtilsMessengerCreateInfoEXT debug{
        VK_STRUCTURE_TYPE_DEBUG_UTILS_MESSENGER_CREATE_INFO_EXT};
    debug.messageSeverity = VK_DEBUG_UTILS_MESSAGE_SEVERITY_ERROR_BIT_EXT;
    debug.messageType = VK_DEBUG_UTILS_MESSAGE_TYPE_GENERAL_BIT_EXT |
                        VK_DEBUG_UTILS_MESSAGE_TYPE_VALIDATION_BIT_EXT |
                        VK_DEBUG_UTILS_MESSAGE_TYPE_PERFORMANCE_BIT_EXT;
    debug.pfnUserCallback = DebugCallback;
    debug.pUserData = &c.validationErrors;
    auto create = reinterpret_cast<PFN_vkCreateDebugUtilsMessengerEXT>(
        vkGetInstanceProcAddr(c.instance, "vkCreateDebugUtilsMessengerEXT"));
    if (create)
      Check(create(c.instance, &debug, nullptr, &c.messenger),
            "billboard proof debug messenger failed");
  }
  uint32_t count = 0;
  Check(vkEnumeratePhysicalDevices(c.instance, &count, nullptr),
        "billboard proof physical enumeration failed");
  std::vector<VkPhysicalDevice> devices(count);
  Check(vkEnumeratePhysicalDevices(c.instance, &count, devices.data()),
        "billboard proof physical enumeration failed");
  for (auto device : devices) {
    VkPhysicalDeviceFeatures features{};
    vkGetPhysicalDeviceFeatures(device, &features);
    if (!features.shaderFloat64) continue;
    uint32_t familyCount = 0;
    vkGetPhysicalDeviceQueueFamilyProperties(device, &familyCount, nullptr);
    std::vector<VkQueueFamilyProperties> families(familyCount);
    vkGetPhysicalDeviceQueueFamilyProperties(device, &familyCount,
                                             families.data());
    for (uint32_t i = 0; i < familyCount; i++)
      if ((families[i].queueFlags &
           (VK_QUEUE_GRAPHICS_BIT | VK_QUEUE_COMPUTE_BIT |
            VK_QUEUE_TRANSFER_BIT)) ==
          (VK_QUEUE_GRAPHICS_BIT | VK_QUEUE_COMPUTE_BIT |
           VK_QUEUE_TRANSFER_BIT)) {
        c.physical = device;
        c.queueFamily = i;
        break;
      }
    if (c.physical)
      break;
  }
  if (!c.physical)
    throw std::runtime_error(
        "billboard proof graphics/compute device unavailable");
  VkPhysicalDeviceProperties properties{};
  vkGetPhysicalDeviceProperties(c.physical, &properties);
  c.timestampPeriod = properties.limits.timestampPeriod;
  float priority = 1;
  VkDeviceQueueCreateInfo queue{VK_STRUCTURE_TYPE_DEVICE_QUEUE_CREATE_INFO};
  queue.queueFamilyIndex = c.queueFamily;
  queue.queueCount = 1;
  queue.pQueuePriorities = &priority;
  VkDeviceCreateInfo device{VK_STRUCTURE_TYPE_DEVICE_CREATE_INFO};
  device.queueCreateInfoCount = 1;
  device.pQueueCreateInfos = &queue;
  VkPhysicalDeviceFeatures features{};
  features.shaderFloat64 = VK_TRUE;
  device.pEnabledFeatures = &features;
  Check(vkCreateDevice(c.physical, &device, nullptr, &c.device),
        "billboard proof device failed");
  vkGetDeviceQueue(c.device, c.queueFamily, 0, &c.queue);
}
void CreateRenderTarget(Context &c) {
  VkImageCreateInfo image{VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO};
  image.imageType = VK_IMAGE_TYPE_2D;
  image.format = VK_FORMAT_R8G8B8A8_UNORM;
  image.extent = {c.extent, c.extent, 1};
  image.mipLevels = 1;
  image.arrayLayers = 1;
  image.samples = VK_SAMPLE_COUNT_1_BIT;
  image.tiling = VK_IMAGE_TILING_OPTIMAL;
  image.usage =
      VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT | VK_IMAGE_USAGE_TRANSFER_SRC_BIT;
  image.initialLayout = VK_IMAGE_LAYOUT_UNDEFINED;
  Check(vkCreateImage(c.device, &image, nullptr, &c.image),
        "billboard proof image failed");
  VkMemoryRequirements requirements{};
  vkGetImageMemoryRequirements(c.device, c.image, &requirements);
  VkMemoryAllocateInfo allocation{VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO};
  allocation.allocationSize = requirements.size;
  allocation.memoryTypeIndex =
      MemoryType(c.physical, requirements.memoryTypeBits,
                 VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT);
  Check(vkAllocateMemory(c.device, &allocation, nullptr, &c.imageMemory),
        "billboard proof image memory failed");
  c.imageAllocationBytes = allocation.allocationSize;
  Check(vkBindImageMemory(c.device, c.image, c.imageMemory, 0),
        "billboard proof image bind failed");
  VkImageViewCreateInfo view{VK_STRUCTURE_TYPE_IMAGE_VIEW_CREATE_INFO};
  view.image = c.image;
  view.viewType = VK_IMAGE_VIEW_TYPE_2D;
  view.format = image.format;
  view.subresourceRange.aspectMask = VK_IMAGE_ASPECT_COLOR_BIT;
  view.subresourceRange.levelCount = 1;
  view.subresourceRange.layerCount = 1;
  Check(vkCreateImageView(c.device, &view, nullptr, &c.imageView),
        "billboard proof image view failed");
  VkAttachmentDescription attachment{};
  attachment.format = image.format;
  attachment.samples = VK_SAMPLE_COUNT_1_BIT;
  attachment.loadOp = VK_ATTACHMENT_LOAD_OP_CLEAR;
  attachment.storeOp = VK_ATTACHMENT_STORE_OP_STORE;
  attachment.initialLayout = VK_IMAGE_LAYOUT_UNDEFINED;
  attachment.finalLayout = VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL;
  VkAttachmentReference reference{0, VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL};
  VkSubpassDescription subpass{};
  subpass.pipelineBindPoint = VK_PIPELINE_BIND_POINT_GRAPHICS;
  subpass.colorAttachmentCount = 1;
  subpass.pColorAttachments = &reference;
  VkSubpassDependency dependency{VK_SUBPASS_EXTERNAL,
                                 0,
                                 VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT,
                                 VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT,
                                 0,
                                 VK_ACCESS_COLOR_ATTACHMENT_WRITE_BIT};
  VkRenderPassCreateInfo pass{VK_STRUCTURE_TYPE_RENDER_PASS_CREATE_INFO};
  pass.attachmentCount = 1;
  pass.pAttachments = &attachment;
  pass.subpassCount = 1;
  pass.pSubpasses = &subpass;
  pass.dependencyCount = 1;
  pass.pDependencies = &dependency;
  Check(vkCreateRenderPass(c.device, &pass, nullptr, &c.renderPass),
        "billboard proof render pass failed");
  VkFramebufferCreateInfo framebuffer{
      VK_STRUCTURE_TYPE_FRAMEBUFFER_CREATE_INFO};
  framebuffer.renderPass = c.renderPass;
  framebuffer.attachmentCount = 1;
  framebuffer.pAttachments = &c.imageView;
  framebuffer.width = c.extent;
  framebuffer.height = c.extent;
  framebuffer.layers = 1;
  Check(vkCreateFramebuffer(c.device, &framebuffer, nullptr, &c.framebuffer),
        "billboard proof framebuffer failed");
}
VkPipeline CreateGraphicsPipeline(Context &c, const char *vertexPath,
                                  const char *fragmentPath) {
  auto vertex = CreateModule(c, vertexPath),
       fragment = CreateModule(c, fragmentPath);
  std::array<VkPipelineShaderStageCreateInfo, 2> stages{};
  stages[0] = {VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,
               nullptr,
               0,
               VK_SHADER_STAGE_VERTEX_BIT,
               vertex,
               "main"};
  stages[1] = {VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,
               nullptr,
               0,
               VK_SHADER_STAGE_FRAGMENT_BIT,
               fragment,
               "main"};
  VkPipelineVertexInputStateCreateInfo vertexInput{
      VK_STRUCTURE_TYPE_PIPELINE_VERTEX_INPUT_STATE_CREATE_INFO};
  VkPipelineInputAssemblyStateCreateInfo assembly{
      VK_STRUCTURE_TYPE_PIPELINE_INPUT_ASSEMBLY_STATE_CREATE_INFO};
  assembly.topology = VK_PRIMITIVE_TOPOLOGY_TRIANGLE_LIST;
  VkPipelineViewportStateCreateInfo viewport{
      VK_STRUCTURE_TYPE_PIPELINE_VIEWPORT_STATE_CREATE_INFO};
  viewport.viewportCount = 1;
  viewport.scissorCount = 1;
  VkPipelineRasterizationStateCreateInfo raster{
      VK_STRUCTURE_TYPE_PIPELINE_RASTERIZATION_STATE_CREATE_INFO};
  raster.polygonMode = VK_POLYGON_MODE_FILL;
  raster.cullMode = VK_CULL_MODE_NONE;
  raster.frontFace = VK_FRONT_FACE_COUNTER_CLOCKWISE;
  raster.lineWidth = 1;
  VkPipelineMultisampleStateCreateInfo samples{
      VK_STRUCTURE_TYPE_PIPELINE_MULTISAMPLE_STATE_CREATE_INFO};
  samples.rasterizationSamples = VK_SAMPLE_COUNT_1_BIT;
  VkPipelineColorBlendAttachmentState blendAttachment{};
  blendAttachment.colorWriteMask = 0xf;
  VkPipelineColorBlendStateCreateInfo blend{
      VK_STRUCTURE_TYPE_PIPELINE_COLOR_BLEND_STATE_CREATE_INFO};
  blend.attachmentCount = 1;
  blend.pAttachments = &blendAttachment;
  std::array<VkDynamicState, 2> dynamics{VK_DYNAMIC_STATE_VIEWPORT,
                                         VK_DYNAMIC_STATE_SCISSOR};
  VkPipelineDynamicStateCreateInfo dynamic{
      VK_STRUCTURE_TYPE_PIPELINE_DYNAMIC_STATE_CREATE_INFO};
  dynamic.dynamicStateCount = static_cast<uint32_t>(dynamics.size());
  dynamic.pDynamicStates = dynamics.data();
  VkGraphicsPipelineCreateInfo create{
      VK_STRUCTURE_TYPE_GRAPHICS_PIPELINE_CREATE_INFO};
  create.stageCount = 2;
  create.pStages = stages.data();
  create.pVertexInputState = &vertexInput;
  create.pInputAssemblyState = &assembly;
  create.pViewportState = &viewport;
  create.pRasterizationState = &raster;
  create.pMultisampleState = &samples;
  create.pColorBlendState = &blend;
  create.pDynamicState = &dynamic;
  create.layout = c.pipelineLayout;
  create.renderPass = c.renderPass;
  VkPipeline pipeline{};
  const auto result =
      vkCreateGraphicsPipelines(c.device, {}, 1, &create, nullptr, &pipeline);
  vkDestroyShaderModule(c.device, vertex, nullptr);
  vkDestroyShaderModule(c.device, fragment, nullptr);
  Check(result, "billboard proof graphics pipeline failed");
  return pipeline;
}
void FillMetrics(const Context &c, NcSphericalBillboardProofMetrics &m) {
  m.size = sizeof m;
  m.version = 1;
  m.activeLevel = c.activeLevel;
  m.readiness = c.readiness;
  m.baseVertexCount = c.baseVertices;
  m.baseTriangleCount = c.baseTriangles;
  m.validationErrors = c.validationErrors;
  m.topologyUploadCount = static_cast<uint32_t>(c.topologyUploads);
  m.frameOutputWriteCount = static_cast<uint32_t>(c.frameWrites);
  m.cullingDispatchCount = static_cast<uint32_t>(c.cullingDispatches);
  m.indirectSubmissionCount = static_cast<uint32_t>(c.indirectSubmissions);
  m.topologyReplacementCount = static_cast<uint32_t>(c.replacements);
  m.runtimeTopologyGenerationCount = 0;
  m.pipelineCreationCount = 6;
  m.shaderModuleCreationCount = 7;
  m.topologyHash = c.topologyHash;
  m.topologyBytesUploaded = c.topologyBytesUploaded;
  m.activeTopologyBytes = c.activeTopologyBytes;
  m.immutableVertexBytes = c.localVertices.allocationBytes;
  m.immutableIndexBytes = c.indices.allocationBytes;
  m.immutableAdjacencyBytes =
      c.neighborOffsets.allocationBytes + c.neighbors.allocationBytes;
  const auto &f = c.frames[0];
  m.framePositionBytes = f.positions.allocationBytes * c.frameCount;
  m.frameNormalBytes = f.normals.allocationBytes * c.frameCount;
  m.frameVisibilityBytes = f.visibility.allocationBytes * c.frameCount;
  m.frameCompactedIndexBytes = f.visibleIndices.allocationBytes * c.frameCount;
  m.frameIndirectBytes = f.indirect.allocationBytes * c.frameCount;
  m.frameCounterBytes = f.counters.allocationBytes * c.frameCount;
  m.temporaryScratchBytes =
      f.readback.allocationBytes * c.frameCount + c.imageAllocationBytes;
  m.totalAllocatedBytes = m.immutableVertexBytes + m.immutableIndexBytes +
                          m.immutableAdjacencyBytes + m.framePositionBytes +
                          m.frameNormalBytes + m.frameVisibilityBytes +
                          m.frameCompactedIndexBytes + m.frameIndirectBytes +
                          m.frameCounterBytes + m.temporaryScratchBytes;
  m.physicalGeneration = c.physicalGeneration;
  m.terrainDataGeneration = c.terrainDataGeneration;
  m.preparedPhysicalSamples = c.preparedPhysicalSamples;
  m.physicalPreparationDispatchCount = c.physicalPreparationDispatches;
  m.physicalReuseCount = c.physicalReuseCount;
  m.staleGenerationRejections = c.staleGenerationRejections;
  m.nonFinitePhysicalOutputs = c.nonFinitePhysicalOutputs;
  m.immutablePhysicalBytes = c.physicalPositions.allocationBytes +
                             c.physicalNormals.allocationBytes;
  m.directionDecodeMaximumErrorRadians = c.directionDecodeMaximumErrorRadians;
  m.incomingLevel = c.incomingLevel;
  m.incomingReadiness = c.incomingReadiness;
  m.publicationCount = c.publications;
  m.deferredRetirementCount = c.deferredRetirements;
  m.incomingTopologyHash = c.incomingTopologyHash;
  m.incomingTopologyBytes = c.incomingTopologyBytes;
  m.selectedIncomingBytes = c.activeTopologyBytes + c.incomingTopologyBytes;
  m.zeroOwnerFrames = c.zeroOwnerFrames;
  m.overlapOwnerFrames = c.overlapOwnerFrames;
  m.staleGenerationDraws = c.staleGenerationDraws;
  m.totalAllocatedBytes += m.immutablePhysicalBytes;
  m.setupMilliseconds = c.setupMilliseconds;
  m.topologyUploadMilliseconds = c.uploadMilliseconds;
}
Context::~Context() {
  if (device)
    vkDeviceWaitIdle(device);
  for (auto &f : frames) {
    if (f.queries)
      vkDestroyQueryPool(device, f.queries, nullptr);
    if (f.fence)
      vkDestroyFence(device, f.fence, nullptr);
    DestroyBuffer(device, f.positions);
    DestroyBuffer(device, f.normals);
    DestroyBuffer(device, f.visibility);
    DestroyBuffer(device, f.visibleIndices);
    DestroyBuffer(device, f.indirect);
    DestroyBuffer(device, f.counters);
    DestroyBuffer(device, f.readback);
  }
  DestroyBuffer(device, localVertices);
  DestroyBuffer(device, indices);
  DestroyBuffer(device, neighborOffsets);
  DestroyBuffer(device, neighbors);
  DestroyBuffer(device, physicalPositions);
  DestroyBuffer(device, physicalNormals);
  DestroyBuffer(device, incomingVertices);
  DestroyBuffer(device, incomingIndices);
  DestroyBuffer(device, incomingNeighborOffsets);
  DestroyBuffer(device, incomingNeighbors);
  if (graphicsPipeline)
    vkDestroyPipeline(device, graphicsPipeline, nullptr);
  if (compactPipeline)
    vkDestroyPipeline(device, compactPipeline, nullptr);
  if (cullPipeline)
    vkDestroyPipeline(device, cullPipeline, nullptr);
  if (normalPipeline)
    vkDestroyPipeline(device, normalPipeline, nullptr);
  if (preparePipeline)
    vkDestroyPipeline(device, preparePipeline, nullptr);
  if (resetPipeline)
    vkDestroyPipeline(device, resetPipeline, nullptr);
  if (framebuffer)
    vkDestroyFramebuffer(device, framebuffer, nullptr);
  if (imageView)
    vkDestroyImageView(device, imageView, nullptr);
  if (image)
    vkDestroyImage(device, image, nullptr);
  if (imageMemory)
    vkFreeMemory(device, imageMemory, nullptr);
  if (renderPass)
    vkDestroyRenderPass(device, renderPass, nullptr);
  if (commandPool)
    vkDestroyCommandPool(device, commandPool, nullptr);
  if (descriptorPool)
    vkDestroyDescriptorPool(device, descriptorPool, nullptr);
  if (pipelineLayout)
    vkDestroyPipelineLayout(device, pipelineLayout, nullptr);
  if (descriptorLayout)
    vkDestroyDescriptorSetLayout(device, descriptorLayout, nullptr);
  if (device)
    vkDestroyDevice(device, nullptr);
  if (messenger && instance) {
    auto destroy = reinterpret_cast<PFN_vkDestroyDebugUtilsMessengerEXT>(
        vkGetInstanceProcAddr(instance, "vkDestroyDebugUtilsMessengerEXT"));
    if (destroy)
      destroy(instance, messenger, nullptr);
  }
  if (instance)
    vkDestroyInstance(instance, nullptr);
}
} // namespace

NcResult InitializeSphericalBillboardGpuProof(
    const NcSphericalBillboardProofAssets *assets,
    NcSphericalBillboardProofMetrics *metrics) {
  if (!assets || assets->size != sizeof(*assets) || assets->version != 1 ||
      !metrics || metrics->size != sizeof(*metrics) ||
      assets->frameResourceCount != 3 || !assets->maximumVertexWorkItems ||
      !assets->maximumTriangleWorkItems ||
      assets->maximumVertexWorkItems > 500000 ||
      assets->maximumTriangleWorkItems > 1000000 || assets->renderExtent < 32 ||
      assets->renderExtent > 512)
    return NC_INVALID_ARGUMENT;
  std::lock_guard lock(Guard);
  if (Current)
    return NC_INVALID_ARGUMENT;
  const auto started = std::chrono::steady_clock::now();
  try {
    auto c = std::make_unique<Context>();
    c->maximumVertices = assets->maximumVertexWorkItems;
    c->maximumTriangles = assets->maximumTriangleWorkItems;
    c->frameCount = assets->frameResourceCount;
    c->extent = assets->renderExtent;
    CreateVulkan(*c);
    std::array<VkDescriptorSetLayoutBinding, BindingCount> bindings{};
    for (uint32_t i = 0; i < BindingCount; i++) {
      bindings[i].binding = i;
      bindings[i].descriptorType = VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;
      bindings[i].descriptorCount = 1;
      bindings[i].stageFlags =
          VK_SHADER_STAGE_COMPUTE_BIT | VK_SHADER_STAGE_VERTEX_BIT;
    }
    VkDescriptorSetLayoutCreateInfo descriptorLayout{
        VK_STRUCTURE_TYPE_DESCRIPTOR_SET_LAYOUT_CREATE_INFO};
    descriptorLayout.bindingCount = BindingCount;
    descriptorLayout.pBindings = bindings.data();
    Check(vkCreateDescriptorSetLayout(c->device, &descriptorLayout, nullptr,
                                      &c->descriptorLayout),
          "billboard proof descriptor layout failed");
    VkPushConstantRange range{VK_SHADER_STAGE_COMPUTE_BIT |
                                  VK_SHADER_STAGE_VERTEX_BIT,
                              0, sizeof(PushConstants)};
    VkPipelineLayoutCreateInfo pipelineLayout{
        VK_STRUCTURE_TYPE_PIPELINE_LAYOUT_CREATE_INFO};
    pipelineLayout.setLayoutCount = 1;
    pipelineLayout.pSetLayouts = &c->descriptorLayout;
    pipelineLayout.pushConstantRangeCount = 1;
    pipelineLayout.pPushConstantRanges = &range;
    Check(vkCreatePipelineLayout(c->device, &pipelineLayout, nullptr,
                                 &c->pipelineLayout),
          "billboard proof pipeline layout failed");
    VkDescriptorPoolSize poolSize{VK_DESCRIPTOR_TYPE_STORAGE_BUFFER,
                                  BindingCount * c->frameCount};
    VkDescriptorPoolCreateInfo pool{
        VK_STRUCTURE_TYPE_DESCRIPTOR_POOL_CREATE_INFO};
    pool.maxSets = c->frameCount;
    pool.poolSizeCount = 1;
    pool.pPoolSizes = &poolSize;
    Check(vkCreateDescriptorPool(c->device, &pool, nullptr, &c->descriptorPool),
          "billboard proof descriptor pool failed");
    std::array<VkDescriptorSetLayout, MaximumFrames> layouts{
        c->descriptorLayout, c->descriptorLayout, c->descriptorLayout};
    VkDescriptorSetAllocateInfo allocate{
        VK_STRUCTURE_TYPE_DESCRIPTOR_SET_ALLOCATE_INFO};
    allocate.descriptorPool = c->descriptorPool;
    allocate.descriptorSetCount = c->frameCount;
    allocate.pSetLayouts = layouts.data();
    std::array<VkDescriptorSet, MaximumFrames> sets{};
    Check(vkAllocateDescriptorSets(c->device, &allocate, sets.data()),
          "billboard proof descriptor allocation failed");
    CreateBuffer(*c, c->localVertices, 16, VK_BUFFER_USAGE_TRANSFER_DST_BIT);
    CreateBuffer(*c, c->indices, 4,
                 VK_BUFFER_USAGE_TRANSFER_DST_BIT |
                     VK_BUFFER_USAGE_INDEX_BUFFER_BIT);
    CreateBuffer(*c, c->neighborOffsets, 4, VK_BUFFER_USAGE_TRANSFER_DST_BIT);
    CreateBuffer(*c, c->neighbors, 4, VK_BUFFER_USAGE_TRANSFER_DST_BIT);
    CreateBuffer(*c, c->physicalPositions, 32,
                 VK_BUFFER_USAGE_TRANSFER_DST_BIT);
    CreateBuffer(*c, c->physicalNormals, 16,
                 VK_BUFFER_USAGE_TRANSFER_DST_BIT);
    for (uint32_t i = 0; i < c->frameCount; i++) {
      auto &f = c->frames[i];
      f.descriptor = sets[i];
      CreateBuffer(*c, f.positions, VkDeviceSize(c->maximumVertices) * 16,
                   VK_BUFFER_USAGE_VERTEX_BUFFER_BIT);
      CreateBuffer(*c, f.normals, VkDeviceSize(c->maximumVertices) * 16,
                   VK_BUFFER_USAGE_VERTEX_BUFFER_BIT);
      CreateBuffer(*c, f.visibility, VkDeviceSize(c->maximumTriangles) * 4, 0);
      CreateBuffer(*c, f.visibleIndices, VkDeviceSize(c->maximumTriangles) * 12,
                   VK_BUFFER_USAGE_INDEX_BUFFER_BIT);
      CreateBuffer(*c, f.indirect, sizeof(VkDrawIndexedIndirectCommand),
                   VK_BUFFER_USAGE_INDIRECT_BUFFER_BIT);
      CreateBuffer(*c, f.counters, 32, 0);
      CreateBuffer(*c, f.readback, VkDeviceSize(c->extent) * c->extent * 4,
                   VK_BUFFER_USAGE_TRANSFER_DST_BIT);
    }
    UpdateDescriptors(*c);
    CreateRenderTarget(*c);
    c->resetPipeline = CreateComputePipeline(*c, assets->resetShaderPathUtf8);
    c->preparePipeline =
        CreateComputePipeline(*c, assets->prepareShaderPathUtf8);
    c->normalPipeline = CreateComputePipeline(*c, assets->normalShaderPathUtf8);
    c->cullPipeline = CreateComputePipeline(*c, assets->cullShaderPathUtf8);
    c->compactPipeline =
        CreateComputePipeline(*c, assets->compactShaderPathUtf8);
    c->graphicsPipeline = CreateGraphicsPipeline(
        *c, assets->vertexShaderPathUtf8, assets->fragmentShaderPathUtf8);
    VkCommandPoolCreateInfo commandPool{
        VK_STRUCTURE_TYPE_COMMAND_POOL_CREATE_INFO};
    commandPool.flags = VK_COMMAND_POOL_CREATE_RESET_COMMAND_BUFFER_BIT;
    commandPool.queueFamilyIndex = c->queueFamily;
    Check(
        vkCreateCommandPool(c->device, &commandPool, nullptr, &c->commandPool),
        "billboard proof command pool failed");
    VkCommandBufferAllocateInfo commandAllocate{
        VK_STRUCTURE_TYPE_COMMAND_BUFFER_ALLOCATE_INFO};
    commandAllocate.commandPool = c->commandPool;
    commandAllocate.level = VK_COMMAND_BUFFER_LEVEL_PRIMARY;
    commandAllocate.commandBufferCount = c->frameCount;
    std::array<VkCommandBuffer, MaximumFrames> commands{};
    Check(
        vkAllocateCommandBuffers(c->device, &commandAllocate, commands.data()),
        "billboard proof command allocation failed");
    for (uint32_t i = 0; i < c->frameCount; i++) {
      auto &f = c->frames[i];
      f.command = commands[i];
      VkFenceCreateInfo fence{VK_STRUCTURE_TYPE_FENCE_CREATE_INFO};
      Check(vkCreateFence(c->device, &fence, nullptr, &f.fence),
            "billboard proof fence failed");
      VkQueryPoolCreateInfo query{VK_STRUCTURE_TYPE_QUERY_POOL_CREATE_INFO};
      query.queryType = VK_QUERY_TYPE_TIMESTAMP;
      query.queryCount = TimestampCount;
      Check(vkCreateQueryPool(c->device, &query, nullptr, &f.queries),
            "billboard proof query pool failed");
    }
    c->setupMilliseconds = std::chrono::duration<double, std::milli>(
                               std::chrono::steady_clock::now() - started)
                               .count();
    FillMetrics(*c, *metrics);
    Current = std::move(c);
    return metrics->validationErrors ? NC_FAILURE : NC_SUCCESS;
  } catch (...) {
    Current.reset();
    return NC_FAILURE;
  }
}

NcResult UploadSphericalBillboardGpuProofTopology(
    const NcSphericalBillboardProofTopology *topology,
    NcSphericalBillboardProofMetrics *metrics) {
  if (!topology || topology->size != sizeof(*topology) ||
      topology->version != 1 ||
      (topology->formatVersion != 1 && topology->formatVersion != 2) ||
      topology->generatorVersion != 1 || !topology->vertices ||
      !topology->indices || !topology->neighborOffsets ||
      !topology->neighbors || !topology->vertexCount || !topology->indexCount ||
      topology->indexCount % 3 ||
      topology->neighborOffsetCount != topology->vertexCount + 1 ||
      !topology->topologyHash || topology->reserved2 > 1 || !metrics ||
      metrics->size != sizeof(*metrics))
    return NC_INVALID_ARGUMENT;
  if ((topology->formatVersion == 1 && topology->reserved0 != 0) ||
      (topology->formatVersion == 2 &&
       (topology->reserved0 != 1 || topology->reserved1 == 0)))
    return NC_INVALID_ARGUMENT;
  std::lock_guard lock(Guard);
  if (!Current)
    return NC_INVALID_ARGUMENT;
  auto &c = *Current;
  if (topology->vertexCount > c.maximumVertices ||
      topology->indexCount / 3 > c.maximumTriangles ||
      topology->neighborOffsets[0] != 0 ||
      topology->neighborOffsets[topology->vertexCount] !=
          topology->neighborCount)
    return NC_INVALID_ARGUMENT;
  for (uint32_t i = 0; i < topology->indexCount; i++)
    if (topology->indices[i] >= topology->vertexCount)
      return NC_INVALID_ARGUMENT;
  for (uint32_t i = 0; i < topology->vertexCount; i++) {
    if (topology->neighborOffsets[i] > topology->neighborOffsets[i + 1])
      return NC_INVALID_ARGUMENT;
    for (uint32_t n = topology->neighborOffsets[i];
         n < topology->neighborOffsets[i + 1]; n++)
      if (topology->neighbors[n] >= topology->vertexCount)
        return NC_INVALID_ARGUMENT;
  }
  if (c.topologyHash == topology->topologyHash &&
      c.activeLevel == topology->level) {
    FillMetrics(c, *metrics);
    return NC_SUCCESS;
  }
  const auto started = std::chrono::steady_clock::now();
  try {
    if (topology->reserved2 == 1) {
      if (!c.topologyHash || c.incomingTopologyHash)
        return NC_INVALID_ARGUMENT;
      CreateBuffer(c, c.incomingVertices,
                   VkDeviceSize(topology->vertexCount) * sizeof(NcSphericalBillboardProofVertex),
                   VK_BUFFER_USAGE_TRANSFER_DST_BIT);
      CreateBuffer(c, c.incomingIndices, VkDeviceSize(topology->indexCount) * 4,
                   VK_BUFFER_USAGE_TRANSFER_DST_BIT | VK_BUFFER_USAGE_INDEX_BUFFER_BIT);
      CreateBuffer(c, c.incomingNeighborOffsets,
                   VkDeviceSize(topology->neighborOffsetCount) * 4,
                   VK_BUFFER_USAGE_TRANSFER_DST_BIT);
      CreateBuffer(c, c.incomingNeighbors, VkDeviceSize(topology->neighborCount) * 4,
                   VK_BUFFER_USAGE_TRANSFER_DST_BIT);
      std::memcpy(c.incomingVertices.mapped, topology->vertices,
                  topology->vertexCount * sizeof(NcSphericalBillboardProofVertex));
      std::memcpy(c.incomingIndices.mapped, topology->indices, topology->indexCount * 4);
      std::memcpy(c.incomingNeighborOffsets.mapped, topology->neighborOffsets,
                  topology->neighborOffsetCount * 4);
      std::memcpy(c.incomingNeighbors.mapped, topology->neighbors, topology->neighborCount * 4);
      c.incomingLevel = topology->level;
      c.incomingVerticesCount = topology->vertexCount;
      c.incomingTriangles = topology->indexCount / 3;
      c.incomingNeighborOffsetCount = topology->neighborOffsetCount;
      c.incomingNeighborCount = topology->neighborCount;
      c.incomingCoordinateEncoding = topology->reserved0;
      c.incomingLatticeScale = topology->reserved1;
      c.incomingTopologyHash = topology->topologyHash;
      c.incomingTopologyBytes = c.incomingVertices.bytes + c.incomingIndices.bytes +
                                c.incomingNeighborOffsets.bytes + c.incomingNeighbors.bytes;
      c.topologyBytesUploaded += c.incomingTopologyBytes;
      c.topologyUploads++;
      c.incomingReadiness = 1;
      c.uploadMilliseconds = std::chrono::duration<double, std::milli>(
                                 std::chrono::steady_clock::now() - started).count();
      FillMetrics(c, *metrics);
      return c.validationErrors ? NC_FAILURE : NC_SUCCESS;
    }
    Check(vkDeviceWaitIdle(c.device),
          "billboard proof replacement wait failed");
    if (c.topologyHash)
      c.replacements++;
    DestroyBuffer(c.device, c.localVertices);
    DestroyBuffer(c.device, c.indices);
    DestroyBuffer(c.device, c.neighborOffsets);
    DestroyBuffer(c.device, c.neighbors);
    CreateBuffer(c, c.localVertices,
                 VkDeviceSize(topology->vertexCount) *
                     sizeof(NcSphericalBillboardProofVertex),
                 VK_BUFFER_USAGE_TRANSFER_DST_BIT);
    CreateBuffer(c, c.indices, VkDeviceSize(topology->indexCount) * 4,
                 VK_BUFFER_USAGE_TRANSFER_DST_BIT |
                     VK_BUFFER_USAGE_INDEX_BUFFER_BIT);
    CreateBuffer(c, c.neighborOffsets,
                 VkDeviceSize(topology->neighborOffsetCount) * 4,
                 VK_BUFFER_USAGE_TRANSFER_DST_BIT);
    CreateBuffer(c, c.neighbors, VkDeviceSize(topology->neighborCount) * 4,
                 VK_BUFFER_USAGE_TRANSFER_DST_BIT);
    std::memcpy(c.localVertices.mapped, topology->vertices,
                topology->vertexCount *
                    sizeof(NcSphericalBillboardProofVertex));
    std::memcpy(c.indices.mapped, topology->indices, topology->indexCount * 4);
    std::memcpy(c.neighborOffsets.mapped, topology->neighborOffsets,
                topology->neighborOffsetCount * 4);
    std::memcpy(c.neighbors.mapped, topology->neighbors,
                topology->neighborCount * 4);
    UpdateDescriptors(c);
    c.activeLevel = topology->level;
    c.coordinateEncoding = topology->reserved0;
    c.latticeScale = topology->reserved1;
    c.baseVertices = topology->vertexCount;
    c.baseTriangles = topology->indexCount / 3;
    c.neighborOffsetCount = topology->neighborOffsetCount;
    c.neighborCount = topology->neighborCount;
    c.topologyHash = topology->topologyHash;
    c.activeTopologyBytes = c.localVertices.bytes + c.indices.bytes +
                            c.neighborOffsets.bytes + c.neighbors.bytes;
    c.topologyBytesUploaded += c.activeTopologyBytes;
    c.topologyUploads++;
    c.physicalGeneration = 0;
    c.terrainDataGeneration = 0;
    c.preparedPhysicalSamples = 0;
    c.readiness = 1;
    c.uploadMilliseconds = std::chrono::duration<double, std::milli>(
                               std::chrono::steady_clock::now() - started)
                               .count();
    FillMetrics(c, *metrics);
    return c.validationErrors ? NC_FAILURE : NC_SUCCESS;
  } catch (...) {
    return NC_FAILURE;
  }
}

NcResult PublishSphericalBillboardPhysicalSurface(
    const NcSphericalBillboardPhysicalSurface *surface,
    NcSphericalBillboardProofMetrics *metrics) {
  if (!surface || surface->size != sizeof(*surface) || surface->version != 1 ||
      !surface->vertices || !surface->vertexCount ||
      !surface->physicalGeneration || !surface->terrainDataGeneration ||
      !surface->expectedTopologyHash || !metrics ||
      metrics->size != sizeof(*metrics))
    return NC_INVALID_ARGUMENT;
  std::lock_guard lock(Guard);
  if (!Current) return NC_INVALID_ARGUMENT;
  auto &c = *Current;
  if (!(c.readiness & 1) || surface->expectedTopologyHash != c.topologyHash ||
      surface->vertexCount != c.baseVertices) {
    c.staleGenerationRejections++;
    FillMetrics(c, *metrics);
    return NC_INVALID_ARGUMENT;
  }
  if (c.physicalGeneration == surface->physicalGeneration &&
      c.terrainDataGeneration == surface->terrainDataGeneration &&
      c.preparedPhysicalSamples == surface->vertexCount) {
    c.physicalReuseCount += surface->vertexCount;
    FillMetrics(c, *metrics);
    return NC_SUCCESS;
  }
  for (uint32_t i = 0; i < surface->vertexCount; i++) {
    const auto &v = surface->vertices[i];
    if (!std::isfinite(v.bodyFixed[0]) || !std::isfinite(v.bodyFixed[1]) ||
        !std::isfinite(v.bodyFixed[2]) || !std::isfinite(v.bodyFixed[3]) ||
        !std::isfinite(v.normal[0]) || !std::isfinite(v.normal[1]) ||
        !std::isfinite(v.normal[2]) || v.normal[3] != 1.0f) {
      c.nonFinitePhysicalOutputs++;
      FillMetrics(c, *metrics);
      return NC_INVALID_ARGUMENT;
    }
  }
  Buffer nextPhysicalPositions{};
  Buffer nextPhysicalNormals{};
  try {
    CreateBuffer(c, nextPhysicalPositions,
                 VkDeviceSize(surface->vertexCount) * 32,
                 VK_BUFFER_USAGE_TRANSFER_DST_BIT);
    CreateBuffer(c, nextPhysicalNormals,
                 VkDeviceSize(surface->vertexCount) * 16,
                 VK_BUFFER_USAGE_TRANSFER_DST_BIT);
    auto *positions = static_cast<double *>(nextPhysicalPositions.mapped);
    auto *normals = static_cast<float *>(nextPhysicalNormals.mapped);
    for (uint32_t i = 0; i < surface->vertexCount; i++) {
      std::memcpy(positions + i * 4, surface->vertices[i].bodyFixed, 32);
      std::memcpy(normals + i * 4, surface->vertices[i].normal, 16);
    }
    Check(vkDeviceWaitIdle(c.device),
          "billboard physical publication wait failed");
    DestroyBuffer(c.device, c.physicalPositions);
    DestroyBuffer(c.device, c.physicalNormals);
    c.physicalPositions = nextPhysicalPositions;
    c.physicalNormals = nextPhysicalNormals;
    nextPhysicalPositions = {};
    nextPhysicalNormals = {};
    UpdateDescriptors(c);
    c.physicalGeneration = surface->physicalGeneration;
    c.terrainDataGeneration = surface->terrainDataGeneration;
    c.preparedPhysicalSamples = surface->vertexCount;
    c.physicalPreparationDispatches++;
    c.readiness = 3;
    FillMetrics(c, *metrics);
    return c.validationErrors ? NC_FAILURE : NC_SUCCESS;
  } catch (...) {
    DestroyBuffer(c.device, nextPhysicalPositions);
    DestroyBuffer(c.device, nextPhysicalNormals);
    return NC_FAILURE;
  }
}

NcResult
RunSphericalBillboardGpuProofFrame(const NcSphericalBillboardProofFrame *frame,
                                   NcSphericalBillboardProofMetrics *metrics) {
  if (!frame || frame->size != sizeof(*frame) || frame->version != 1 ||
      !frame->workVertexCount || !frame->workTriangleCount ||
      !std::isfinite(frame->bodyRadiusMetres) ||
      !std::isfinite(frame->cameraDistanceMetres) ||
      frame->bodyRadiusMetres <= 0 ||
      frame->cameraDistanceMetres <= frame->bodyRadiusMetres ||
      !std::isfinite(frame->verticalTanHalfFov) ||
      frame->verticalTanHalfFov <= 0 || !std::isfinite(frame->aspectRatio) ||
      frame->aspectRatio <= 0 || !metrics || metrics->size != sizeof(*metrics))
    return NC_INVALID_ARGUMENT;
  std::lock_guard lock(Guard);
  if (!Current)
    return NC_INVALID_ARGUMENT;
  auto &c = *Current;
  if (frame->reserved2 == 1) {
    if (!c.incomingTopologyHash || frame->expectedTopologyHash != c.incomingTopologyHash ||
        c.incomingReadiness != 1) {
      c.staleGenerationDraws++;
      FillMetrics(c, *metrics);
      return NC_INVALID_ARGUMENT;
    }
    try {
      for (auto &pending : c.frames)
        if (pending.submitted)
          Check(vkWaitForFences(c.device, 1, &pending.fence, VK_TRUE, 10'000'000'000ull),
                "billboard incoming publication fence wait failed");
      DestroyBuffer(c.device, c.localVertices);
      DestroyBuffer(c.device, c.indices);
      DestroyBuffer(c.device, c.neighborOffsets);
      DestroyBuffer(c.device, c.neighbors);
      c.localVertices = c.incomingVertices; c.incomingVertices = {};
      c.indices = c.incomingIndices; c.incomingIndices = {};
      c.neighborOffsets = c.incomingNeighborOffsets; c.incomingNeighborOffsets = {};
      c.neighbors = c.incomingNeighbors; c.incomingNeighbors = {};
      c.activeLevel = c.incomingLevel;
      c.baseVertices = c.incomingVerticesCount;
      c.baseTriangles = c.incomingTriangles;
      c.neighborOffsetCount = c.incomingNeighborOffsetCount;
      c.neighborCount = c.incomingNeighborCount;
      c.coordinateEncoding = c.incomingCoordinateEncoding;
      c.latticeScale = c.incomingLatticeScale;
      c.topologyHash = c.incomingTopologyHash;
      c.activeTopologyBytes = c.incomingTopologyBytes;
      c.incomingLevel = c.incomingVerticesCount = c.incomingTriangles = 0;
      c.incomingNeighborOffsetCount = c.incomingNeighborCount = 0;
      c.incomingCoordinateEncoding = c.incomingLatticeScale = 0;
      c.incomingTopologyHash = c.incomingTopologyBytes = 0;
      c.incomingReadiness = 0;
      c.publications++;
      c.deferredRetirements++;
      c.physicalGeneration = 0;
      c.terrainDataGeneration = 0;
      c.preparedPhysicalSamples = 0;
      c.readiness = 1;
      UpdateDescriptors(c);
    } catch (...) {
      c.zeroOwnerFrames++;
      return NC_FAILURE;
    }
  }
  if ((frame->reserved0 && (frame->reserved0 != c.physicalGeneration ||
                            frame->reserved1 != c.terrainDataGeneration))) {
    c.staleGenerationRejections++;
    FillMetrics(c, *metrics);
    return NC_INVALID_ARGUMENT;
  }
  if (!(c.readiness & 1) || frame->expectedTopologyHash != c.topologyHash ||
      frame->workVertexCount > c.maximumVertices ||
      frame->workTriangleCount > c.maximumTriangles ||
      frame->workVertexCount < c.baseVertices)
    return NC_INVALID_ARGUMENT;
  const auto started = std::chrono::steady_clock::now();
  auto &f = c.frames[frame->frameIndex % c.frameCount];
  try {
    if (f.submitted) {
      Check(vkWaitForFences(c.device, 1, &f.fence, VK_TRUE, 10'000'000'000ull),
            "billboard proof frame fence wait failed");
      c.frameWaits++;
    }
    Check(vkResetFences(c.device, 1, &f.fence),
          "billboard proof frame fence reset failed");
    Check(vkResetCommandBuffer(f.command, 0),
          "billboard proof command reset failed");
    std::memset(f.readback.mapped, 0, static_cast<size_t>(f.readback.bytes));
    PushConstants pc{c.baseVertices,
                     c.baseTriangles,
                     frame->workVertexCount,
                     frame->workTriangleCount,
                     static_cast<float>(frame->bodyRadiusMetres),
                     static_cast<float>(frame->cameraDistanceMetres),
                     frame->verticalTanHalfFov,
                     frame->aspectRatio,
                     c.activeLevel,
                     frame->frameIndex,
                     c.maximumTriangles * 3,
                     c.physicalGeneration,
                     c.coordinateEncoding,
                     c.latticeScale};
    VkCommandBufferBeginInfo begin{VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO};
    begin.flags = VK_COMMAND_BUFFER_USAGE_ONE_TIME_SUBMIT_BIT;
    Check(vkBeginCommandBuffer(f.command, &begin),
          "billboard proof begin failed");
    vkCmdResetQueryPool(f.command, f.queries, 0, TimestampCount);
    vkCmdBindDescriptorSets(f.command, VK_PIPELINE_BIND_POINT_COMPUTE,
                            c.pipelineLayout, 0, 1, &f.descriptor, 0, nullptr);
    vkCmdPushConstants(f.command, c.pipelineLayout,
                       VK_SHADER_STAGE_COMPUTE_BIT | VK_SHADER_STAGE_VERTEX_BIT,
                       0, sizeof pc, &pc);
    vkCmdBindPipeline(f.command, VK_PIPELINE_BIND_POINT_COMPUTE,
                      c.resetPipeline);
    vkCmdDispatch(f.command, 1, 1, 1);
    VkMemoryBarrier resetBarrier{VK_STRUCTURE_TYPE_MEMORY_BARRIER};
    resetBarrier.srcAccessMask = VK_ACCESS_SHADER_WRITE_BIT;
    resetBarrier.dstAccessMask =
        VK_ACCESS_SHADER_READ_BIT | VK_ACCESS_SHADER_WRITE_BIT;
    vkCmdPipelineBarrier(f.command, VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,
                         VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT, 0, 1,
                         &resetBarrier, 0, nullptr, 0, nullptr);
    vkCmdWriteTimestamp(f.command, VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,
                        f.queries, 0);
    vkCmdBindPipeline(f.command, VK_PIPELINE_BIND_POINT_COMPUTE,
                      c.preparePipeline);
    vkCmdDispatch(f.command, (frame->workVertexCount + 63) / 64, 1, 1);
    vkCmdWriteTimestamp(f.command, VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,
                        f.queries, 1);
    VkMemoryBarrier preparedBarrier{VK_STRUCTURE_TYPE_MEMORY_BARRIER};
    preparedBarrier.srcAccessMask = VK_ACCESS_SHADER_WRITE_BIT;
    preparedBarrier.dstAccessMask = VK_ACCESS_SHADER_READ_BIT;
    vkCmdPipelineBarrier(f.command, VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,
                         VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT, 0, 1,
                         &preparedBarrier, 0, nullptr, 0, nullptr);
    vkCmdBindPipeline(f.command, VK_PIPELINE_BIND_POINT_COMPUTE,
                      c.normalPipeline);
    vkCmdDispatch(f.command, (frame->workVertexCount + 63) / 64, 1, 1);
    vkCmdWriteTimestamp(f.command, VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,
                        f.queries, 2);
    vkCmdPipelineBarrier(f.command, VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,
                         VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT, 0, 1,
                         &preparedBarrier, 0, nullptr, 0, nullptr);
    vkCmdBindPipeline(f.command, VK_PIPELINE_BIND_POINT_COMPUTE,
                      c.cullPipeline);
    vkCmdDispatch(f.command, (frame->workTriangleCount + 63) / 64, 1, 1);
    vkCmdWriteTimestamp(f.command, VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,
                        f.queries, 3);
    vkCmdPipelineBarrier(f.command, VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,
                         VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT, 0, 1,
                         &preparedBarrier, 0, nullptr, 0, nullptr);
    vkCmdBindPipeline(f.command, VK_PIPELINE_BIND_POINT_COMPUTE,
                      c.compactPipeline);
    vkCmdDispatch(f.command, (frame->workTriangleCount + 63) / 64, 1, 1);
    vkCmdWriteTimestamp(f.command, VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,
                        f.queries, 4);
    VkMemoryBarrier drawBarrier{VK_STRUCTURE_TYPE_MEMORY_BARRIER};
    drawBarrier.srcAccessMask = VK_ACCESS_SHADER_WRITE_BIT;
    drawBarrier.dstAccessMask = VK_ACCESS_INDIRECT_COMMAND_READ_BIT |
                                VK_ACCESS_INDEX_READ_BIT |
                                VK_ACCESS_SHADER_READ_BIT;
    vkCmdPipelineBarrier(f.command, VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,
                         VK_PIPELINE_STAGE_DRAW_INDIRECT_BIT |
                             VK_PIPELINE_STAGE_VERTEX_INPUT_BIT |
                             VK_PIPELINE_STAGE_VERTEX_SHADER_BIT,
                         0, 1, &drawBarrier, 0, nullptr, 0, nullptr);
    if (frame->renderEnabled) {
      VkClearValue clear{};
      clear.color = {{0, 0, 0, 1}};
      VkRenderPassBeginInfo pass{VK_STRUCTURE_TYPE_RENDER_PASS_BEGIN_INFO};
      pass.renderPass = c.renderPass;
      pass.framebuffer = c.framebuffer;
      pass.renderArea.extent = {c.extent, c.extent};
      pass.clearValueCount = 1;
      pass.pClearValues = &clear;
      vkCmdBeginRenderPass(f.command, &pass, VK_SUBPASS_CONTENTS_INLINE);
      VkViewport viewport{
          0, 0, static_cast<float>(c.extent), static_cast<float>(c.extent),
          0, 1};
      VkRect2D scissor{{0, 0}, {c.extent, c.extent}};
      vkCmdSetViewport(f.command, 0, 1, &viewport);
      vkCmdSetScissor(f.command, 0, 1, &scissor);
      vkCmdBindPipeline(f.command, VK_PIPELINE_BIND_POINT_GRAPHICS,
                        c.graphicsPipeline);
      vkCmdBindDescriptorSets(f.command, VK_PIPELINE_BIND_POINT_GRAPHICS,
                              c.pipelineLayout, 0, 1, &f.descriptor, 0,
                              nullptr);
      vkCmdPushConstants(f.command, c.pipelineLayout,
                         VK_SHADER_STAGE_COMPUTE_BIT | VK_SHADER_STAGE_VERTEX_BIT,
                         0, sizeof pc, &pc);
      vkCmdBindIndexBuffer(f.command, f.visibleIndices.buffer, 0,
                           VK_INDEX_TYPE_UINT32);
      vkCmdDrawIndexedIndirect(f.command, f.indirect.buffer, 0, 1,
                               sizeof(VkDrawIndexedIndirectCommand));
      vkCmdEndRenderPass(f.command);
      VkBufferImageCopy copy{};
      copy.imageSubresource.aspectMask = VK_IMAGE_ASPECT_COLOR_BIT;
      copy.imageSubresource.layerCount = 1;
      copy.imageExtent = {c.extent, c.extent, 1};
      vkCmdCopyImageToBuffer(f.command, c.image,
                             VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL,
                             f.readback.buffer, 1, &copy);
      c.indirectSubmissions++;
    }
    vkCmdWriteTimestamp(f.command, VK_PIPELINE_STAGE_BOTTOM_OF_PIPE_BIT,
                        f.queries, 5);
    VkMemoryBarrier hostBarrier{VK_STRUCTURE_TYPE_MEMORY_BARRIER};
    hostBarrier.srcAccessMask =
        VK_ACCESS_SHADER_WRITE_BIT | VK_ACCESS_TRANSFER_WRITE_BIT;
    hostBarrier.dstAccessMask = VK_ACCESS_HOST_READ_BIT;
    vkCmdPipelineBarrier(
        f.command,
        VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT | VK_PIPELINE_STAGE_TRANSFER_BIT,
        VK_PIPELINE_STAGE_HOST_BIT, 0, 1, &hostBarrier, 0, nullptr, 0, nullptr);
    Check(vkEndCommandBuffer(f.command), "billboard proof command end failed");
    VkSubmitInfo submit{VK_STRUCTURE_TYPE_SUBMIT_INFO};
    submit.commandBufferCount = 1;
    submit.pCommandBuffers = &f.command;
    Check(vkQueueSubmit(c.queue, 1, &submit, f.fence),
          "billboard proof submit failed");
    f.submitted = true;
    Check(vkWaitForFences(c.device, 1, &f.fence, VK_TRUE, 10'000'000'000ull),
          "billboard proof completion wait failed");
    std::array<uint64_t, TimestampCount> ticks{};
    Check(vkGetQueryPoolResults(c.device, f.queries, 0, TimestampCount,
                                sizeof ticks, ticks.data(), sizeof(uint64_t),
                                VK_QUERY_RESULT_64_BIT |
                                    VK_QUERY_RESULT_WAIT_BIT),
          "billboard proof timestamps unavailable");
    const auto *draw =
        static_cast<const VkDrawIndexedIndirectCommand *>(f.indirect.mapped);
    const auto *counters = static_cast<const uint32_t *>(f.counters.mapped);
    uint64_t checksum = 14695981039346656037ull;
    if (frame->renderEnabled) {
      const auto *pixels = static_cast<const uint8_t *>(f.readback.mapped);
      for (size_t i = 0; i < static_cast<size_t>(f.readback.bytes); i++) {
        checksum ^= pixels[i];
        checksum *= 1099511628211ull;
      }
    } else
      checksum = 0;
    c.frameWrites++;
    c.cullingDispatches++;
    c.readiness = c.physicalGeneration ? 63u : 7u;
    FillMetrics(c, *metrics);
    metrics->workVertexCount = frame->workVertexCount;
    metrics->workTriangleCount = frame->workTriangleCount;
    metrics->preparedVertices = frame->workVertexCount;
    metrics->visibleTriangles = draw->indexCount / 3;
    metrics->backfaceRejected = counters[1];
    metrics->frustumRejected = counters[2];
    metrics->invalidRejected = counters[4];
    metrics->overflowCount = counters[3];
    metrics->indirectIndexCount = draw->indexCount;
    metrics->indirectDrawCount = frame->renderEnabled ? 1u : 0u;
    metrics->invalidCommands =
        (draw->instanceCount != 1 || draw->firstIndex || draw->vertexOffset ||
         draw->firstInstance || draw->indexCount > c.maximumTriangles * 3)
            ? 1u
            : 0u;
    metrics->frameSlot = frame->frameIndex % c.frameCount;
    metrics->frameWaitCount = static_cast<uint32_t>(c.frameWaits);
    metrics->pixelChecksum = checksum;
    if (c.coordinateEncoding == 1 && !c.physicalGeneration) {
      const auto *lattice = static_cast<const int32_t *>(c.localVertices.mapped);
      const auto *prepared = static_cast<const float *>(f.positions.mapped);
      double maximum = 0.0;
      for (uint32_t i = 0; i < c.baseVertices; ++i) {
        const double ex = double(lattice[i * 4 + 0]) / double(c.latticeScale);
        const double ey = double(lattice[i * 4 + 1]) / double(c.latticeScale);
        const double ez = double(lattice[i * 4 + 2]) / double(c.latticeScale);
        const double el = std::sqrt(ex * ex + ey * ey + ez * ez);
        double ax = prepared[i * 4 + 0];
        double ay = prepared[i * 4 + 1];
        double az = prepared[i * 4 + 2] + frame->cameraDistanceMetres;
        const double al = std::sqrt(ax * ax + ay * ay + az * az);
        if (el > 0.0 && al > 0.0) {
          const double dot = std::clamp((ex * ax + ey * ay + ez * az) / (el * al), -1.0, 1.0);
          maximum = std::max(maximum, std::acos(dot));
        }
      }
      c.directionDecodeMaximumErrorRadians = std::max(c.directionDecodeMaximumErrorRadians, maximum);
      metrics->directionDecodeMaximumErrorRadians = c.directionDecodeMaximumErrorRadians;
    }
    const auto milliseconds = [&](uint32_t a, uint32_t b) {
      return double(ticks[b] - ticks[a]) * c.timestampPeriod / 1'000'000.0;
    };
    metrics->preparationMilliseconds = milliseconds(0, 1);
    metrics->normalMilliseconds = milliseconds(1, 2);
    metrics->cullingMilliseconds = milliseconds(2, 3);
    metrics->compactionMilliseconds = milliseconds(3, 4);
    metrics->drawMilliseconds = milliseconds(4, 5);
    metrics->gpuTotalMilliseconds = milliseconds(0, 5);
    metrics->cpuFrameMilliseconds =
        std::chrono::duration<double, std::milli>(
            std::chrono::steady_clock::now() - started)
            .count();
    return (metrics->validationErrors || metrics->invalidCommands ||
            metrics->overflowCount || !metrics->visibleTriangles ||
            (frame->renderEnabled && !metrics->pixelChecksum))
               ? NC_FAILURE
               : NC_SUCCESS;
  } catch (...) {
    return NC_FAILURE;
  }
}

NcResult ShutdownSphericalBillboardGpuProof() {
  std::lock_guard lock(Guard);
  Current.reset();
  return NC_SUCCESS;
}
