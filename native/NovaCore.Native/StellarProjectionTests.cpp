#include "PresentationGpuTestDevice.h"
#include <algorithm>
#include <chrono>
#include <cmath>
#include <filesystem>
#include <iostream>
// Offscreen production-shader regression. No window, scene mutation or runtime
// hook. Frame is camera-relative P*R; the represented source is split high/low
// exactly as in the production submission. Pixel comparisons are linear HDR
// values.
struct Renderer {
  Device d{VK_QUEUE_GRAPHICS_BIT};
  unsigned w = 860, h = 360;
  VkImage image{};
  VkDeviceMemory memory{};
  VkImageView view{};
  VkRenderPass pass{};
  VkFramebuffer fb{};
  VkDescriptorSet set{};
  VkCommandBuffer cmd{};
  VkQueryPool queries{};
  Device::Buffer frame, body, readback, vertices;
  VkPipeline pipes[4]{};
  std::string shaders;
  float period{};
  unsigned sphereCount{};
  Renderer(std::string dir, unsigned width, unsigned height)
      : w(width), h(height), shaders(dir) {
    VkPhysicalDeviceProperties prop;
    vkGetPhysicalDeviceProperties(d.physical, &prop);
    period = prop.limits.timestampPeriod;
    std::cout << "GPU " << prop.deviceName << " viewport " << w << "x" << h
              << '\n';
    VkImageCreateInfo ic{VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO};
    ic.imageType = VK_IMAGE_TYPE_2D;
    ic.format = VK_FORMAT_R32G32B32A32_SFLOAT;
    ic.extent = {w, h, 1};
    ic.mipLevels = ic.arrayLayers = 1;
    ic.samples = VK_SAMPLE_COUNT_1_BIT;
    ic.tiling = VK_IMAGE_TILING_OPTIMAL;
    ic.usage =
        VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT | VK_IMAGE_USAGE_TRANSFER_SRC_BIT;
    Vk(vkCreateImage(d.device, &ic, nullptr, &image));
    VkMemoryRequirements rq;
    vkGetImageMemoryRequirements(d.device, image, &rq);
    VkPhysicalDeviceMemoryProperties mp;
    vkGetPhysicalDeviceMemoryProperties(d.physical, &mp);
    unsigned type = 0;
    while (!(rq.memoryTypeBits & (1u << type)))
      type++;
    VkMemoryAllocateInfo ma{VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO};
    ma.allocationSize = rq.size;
    ma.memoryTypeIndex = type;
    Vk(vkAllocateMemory(d.device, &ma, nullptr, &memory));
    Vk(vkBindImageMemory(d.device, image, memory, 0));
    VkImageViewCreateInfo vi{VK_STRUCTURE_TYPE_IMAGE_VIEW_CREATE_INFO};
    vi.image = image;
    vi.viewType = VK_IMAGE_VIEW_TYPE_2D;
    vi.format = ic.format;
    vi.subresourceRange = {VK_IMAGE_ASPECT_COLOR_BIT, 0, 1, 0, 1};
    Vk(vkCreateImageView(d.device, &vi, nullptr, &view));
    VkAttachmentDescription at{};
    at.format = ic.format;
    at.samples = VK_SAMPLE_COUNT_1_BIT;
    at.loadOp = VK_ATTACHMENT_LOAD_OP_CLEAR;
    at.storeOp = VK_ATTACHMENT_STORE_OP_STORE;
    at.initialLayout = VK_IMAGE_LAYOUT_UNDEFINED;
    at.finalLayout = VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL;
    VkAttachmentReference ref{0, VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL};
    VkSubpassDescription sub{};
    sub.pipelineBindPoint = VK_PIPELINE_BIND_POINT_GRAPHICS;
    sub.colorAttachmentCount = 1;
    sub.pColorAttachments = &ref;
    VkSubpassDependency deps[2]{};
    deps[0] = {VK_SUBPASS_EXTERNAL,
               0,
               VK_PIPELINE_STAGE_TOP_OF_PIPE_BIT,
               VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT,
               0,
               VK_ACCESS_COLOR_ATTACHMENT_WRITE_BIT,
               0};
    deps[1] = {0,
               VK_SUBPASS_EXTERNAL,
               VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT,
               VK_PIPELINE_STAGE_TRANSFER_BIT,
               VK_ACCESS_COLOR_ATTACHMENT_WRITE_BIT,
               VK_ACCESS_TRANSFER_READ_BIT,
               0};
    VkRenderPassCreateInfo pc{VK_STRUCTURE_TYPE_RENDER_PASS_CREATE_INFO};
    pc.attachmentCount = 1;
    pc.pAttachments = &at;
    pc.subpassCount = 1;
    pc.pSubpasses = &sub;
    pc.dependencyCount = 2;
    pc.pDependencies = deps;
    Vk(vkCreateRenderPass(d.device, &pc, nullptr, &pass));
    VkFramebufferCreateInfo fi{VK_STRUCTURE_TYPE_FRAMEBUFFER_CREATE_INFO};
    fi.renderPass = pass;
    fi.attachmentCount = 1;
    fi.pAttachments = &view;
    fi.width = w;
    fi.height = h;
    fi.layers = 1;
    Vk(vkCreateFramebuffer(d.device, &fi, nullptr, &fb));
    frame = d.Create(96, nullptr);
    body = d.Create(192, nullptr);
    readback =
        d.Create(size_t(w) * h * 16, nullptr, VK_BUFFER_USAGE_TRANSFER_DST_BIT);
    std::vector<float> mesh;
    auto point = [](float t, float p) {
      return std::array<float, 3>{std::sin(t) * std::cos(p), std::cos(t),
                                  std::sin(t) * std::sin(p)};
    };
    for (int i = 0; i < 16; i++)
      for (int j = 0; j < 32; j++) {
        auto a = point(i * 3.14159265f / 16, j * 6.2831853f / 32),
             b = point((i + 1) * 3.14159265f / 16, j * 6.2831853f / 32),
             c = point(i * 3.14159265f / 16, (j + 1) * 6.2831853f / 32),
             e = point((i + 1) * 3.14159265f / 16, (j + 1) * 6.2831853f / 32);
        for (auto p : {a, b, c, c, b, e})
          mesh.insert(mesh.end(), p.begin(), p.end());
      }
    sphereCount = unsigned(mesh.size() / 3);
    vertices = d.Create(mesh.size() * 4, mesh.data(),
                        VK_BUFFER_USAGE_VERTEX_BUFFER_BIT);
    VkDescriptorSetLayoutBinding bindings[]{
        {0, VK_DESCRIPTOR_TYPE_STORAGE_BUFFER, 1,
         VK_SHADER_STAGE_VERTEX_BIT | VK_SHADER_STAGE_FRAGMENT_BIT, nullptr},
        {6, VK_DESCRIPTOR_TYPE_STORAGE_BUFFER, 1, VK_SHADER_STAGE_VERTEX_BIT,
         nullptr}};
    VkDescriptorSetLayoutCreateInfo lc{
        VK_STRUCTURE_TYPE_DESCRIPTOR_SET_LAYOUT_CREATE_INFO};
    lc.bindingCount = 2;
    lc.pBindings = bindings;
    Vk(vkCreateDescriptorSetLayout(d.device, &lc, nullptr, &d.layout));
    VkDescriptorPoolSize ps{VK_DESCRIPTOR_TYPE_STORAGE_BUFFER, 2};
    VkDescriptorPoolCreateInfo dp{
        VK_STRUCTURE_TYPE_DESCRIPTOR_POOL_CREATE_INFO};
    dp.maxSets = 1;
    dp.poolSizeCount = 1;
    dp.pPoolSizes = &ps;
    Vk(vkCreateDescriptorPool(d.device, &dp, nullptr, &d.descriptors));
    VkDescriptorSetAllocateInfo da{
        VK_STRUCTURE_TYPE_DESCRIPTOR_SET_ALLOCATE_INFO};
    da.descriptorPool = d.descriptors;
    da.descriptorSetCount = 1;
    da.pSetLayouts = &d.layout;
    Vk(vkAllocateDescriptorSets(d.device, &da, &set));
    VkDescriptorBufferInfo infos[]{{frame.buffer, 0, 96},
                                   {body.buffer, 0, 192}};
    VkWriteDescriptorSet wr[2]{};
    for (int i = 0; i < 2; i++) {
      wr[i].sType = VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET;
      wr[i].dstSet = set;
      wr[i].dstBinding = bindings[i].binding;
      wr[i].descriptorCount = 1;
      wr[i].descriptorType = VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;
      wr[i].pBufferInfo = &infos[i];
    }
    vkUpdateDescriptorSets(d.device, 2, wr, 0, nullptr);
    VkPushConstantRange range{
        VK_SHADER_STAGE_VERTEX_BIT | VK_SHADER_STAGE_FRAGMENT_BIT, 0, 48};
    VkPipelineLayoutCreateInfo pl{
        VK_STRUCTURE_TYPE_PIPELINE_LAYOUT_CREATE_INFO};
    pl.setLayoutCount = 1;
    pl.pSetLayouts = &d.layout;
    pl.pushConstantRangeCount = 1;
    pl.pPushConstantRanges = &range;
    Vk(vkCreatePipelineLayout(d.device, &pl, nullptr, &d.pipelineLayout));
    VkCommandPoolCreateInfo cp{VK_STRUCTURE_TYPE_COMMAND_POOL_CREATE_INFO};
    cp.queueFamilyIndex = d.family;
    cp.flags = VK_COMMAND_POOL_CREATE_RESET_COMMAND_BUFFER_BIT;
    Vk(vkCreateCommandPool(d.device, &cp, nullptr, &d.pool));
    VkCommandBufferAllocateInfo ca{
        VK_STRUCTURE_TYPE_COMMAND_BUFFER_ALLOCATE_INFO};
    ca.commandPool = d.pool;
    ca.level = VK_COMMAND_BUFFER_LEVEL_PRIMARY;
    ca.commandBufferCount = 1;
    Vk(vkAllocateCommandBuffers(d.device, &ca, &cmd));
    VkQueryPoolCreateInfo qi{VK_STRUCTURE_TYPE_QUERY_POOL_CREATE_INFO};
    qi.queryType = VK_QUERY_TYPE_TIMESTAMP;
    qi.queryCount = 6;
    Vk(vkCreateQueryPool(d.device, &qi, nullptr, &queries));
  }
  VkShaderModule Module(std::string path) {
    std::ifstream f(path, std::ios::binary | std::ios::ate);
    Require(bool(f), path.c_str());
    auto n = f.tellg();
    std::vector<uint32_t> words(size_t(n) / 4);
    f.seekg(0);
    f.read((char *)words.data(), n);
    VkShaderModuleCreateInfo mi{VK_STRUCTURE_TYPE_SHADER_MODULE_CREATE_INFO};
    mi.codeSize = size_t(n);
    mi.pCode = words.data();
    VkShaderModule m;
    Vk(vkCreateShaderModule(d.device, &mi, nullptr, &m));
    return m;
  }
  void Pipeline(int index, std::string vs, std::string fs) {
    auto v = Module(vs), f = Module(fs);
    VkPipelineShaderStageCreateInfo stages[2]{};
    for (auto &s : stages)
      s.sType = VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO;
    stages[0].stage = VK_SHADER_STAGE_VERTEX_BIT;
    stages[0].module = v;
    stages[1].stage = VK_SHADER_STAGE_FRAGMENT_BIT;
    stages[1].module = f;
    stages[0].pName = stages[1].pName = "main";
    VkVertexInputBindingDescription bind{0, 12, VK_VERTEX_INPUT_RATE_VERTEX};
    VkVertexInputAttributeDescription attr{0, 0, VK_FORMAT_R32G32B32_SFLOAT, 0};
    VkPipelineVertexInputStateCreateInfo input{
        VK_STRUCTURE_TYPE_PIPELINE_VERTEX_INPUT_STATE_CREATE_INFO};
    if (index == 2) {
      input.vertexBindingDescriptionCount = 1;
      input.pVertexBindingDescriptions = &bind;
      input.vertexAttributeDescriptionCount = 1;
      input.pVertexAttributeDescriptions = &attr;
    }
    VkPipelineInputAssemblyStateCreateInfo assembly{
        VK_STRUCTURE_TYPE_PIPELINE_INPUT_ASSEMBLY_STATE_CREATE_INFO};
    assembly.topology = VK_PRIMITIVE_TOPOLOGY_TRIANGLE_LIST;
    VkViewport viewport{0, 0, float(w), float(h), 0, 1};
    VkRect2D scissor{{0, 0}, {w, h}};
    VkPipelineViewportStateCreateInfo vp{
        VK_STRUCTURE_TYPE_PIPELINE_VIEWPORT_STATE_CREATE_INFO};
    vp.viewportCount = vp.scissorCount = 1;
    vp.pViewports = &viewport;
    vp.pScissors = &scissor;
    VkPipelineRasterizationStateCreateInfo rs{
        VK_STRUCTURE_TYPE_PIPELINE_RASTERIZATION_STATE_CREATE_INFO};
    rs.polygonMode = VK_POLYGON_MODE_FILL;
    rs.cullMode = VK_CULL_MODE_NONE;
    rs.lineWidth = 1;
    VkPipelineMultisampleStateCreateInfo ms{
        VK_STRUCTURE_TYPE_PIPELINE_MULTISAMPLE_STATE_CREATE_INFO};
    ms.rasterizationSamples = VK_SAMPLE_COUNT_1_BIT;
    VkPipelineColorBlendAttachmentState ba{};
    ba.colorWriteMask = 15;
    ba.blendEnable = index == 1 || index == 3;
    ba.srcColorBlendFactor = VK_BLEND_FACTOR_SRC_ALPHA;
    ba.dstColorBlendFactor = VK_BLEND_FACTOR_ONE_MINUS_SRC_ALPHA;
    ba.srcAlphaBlendFactor = VK_BLEND_FACTOR_ONE;
    ba.dstAlphaBlendFactor = VK_BLEND_FACTOR_ONE_MINUS_SRC_ALPHA;
    VkPipelineColorBlendStateCreateInfo blend{
        VK_STRUCTURE_TYPE_PIPELINE_COLOR_BLEND_STATE_CREATE_INFO};
    blend.attachmentCount = 1;
    blend.pAttachments = &ba;
    VkGraphicsPipelineCreateInfo gp{
        VK_STRUCTURE_TYPE_GRAPHICS_PIPELINE_CREATE_INFO};
    gp.stageCount = 2;
    gp.pStages = stages;
    gp.pVertexInputState = &input;
    gp.pInputAssemblyState = &assembly;
    gp.pViewportState = &vp;
    gp.pRasterizationState = &rs;
    gp.pMultisampleState = &ms;
    gp.pColorBlendState = &blend;
    gp.layout = d.pipelineLayout;
    gp.renderPass = pass;
    Vk(vkCreateGraphicsPipelines(d.device, {}, 1, &gp, nullptr, &pipes[index]));
    vkDestroyShaderModule(d.device, v, nullptr);
    vkDestroyShaderModule(d.device, f, nullptr);
  }
  void Setup(std::string glow) {
    Pipeline(0, shaders + "/fullscreen.vert.spv",
             shaders + "/space_background.frag.spv");
    Pipeline(1, glow, shaders + "/stellar_glow.frag.spv");
    Pipeline(2, shaders + "/stellar_sun.vert.spv",
             shaders + "/stellar_sun.frag.spv");
    Pipeline(3, shaders + "/solar_marker.vert.spv",
             shaders + "/solar_marker.frag.spv");
  }
  void Pose(double yaw, double pitch, double distance = 1.496e11) {
    float *p = (float *)body.mapped;
    std::memset(p, 0, 192);
    p[0] = 0;
    p[1] = 0;
    p[2] = float(distance);
    p[3] = 6.957e8f;
    p[4] = 1;
    p[5] = .91f;
    p[6] = .68f;
    uint32_t flags = 0x30000001u;
    std::memcpy(p + 11, &flags, 4);
    p[46] = float(distance - double(p[2]));
    float *m = (float *)frame.mapped;
    std::memset(m, 0, 96);
    m += 8;
    double cy = cos(yaw), sy = sin(yaw), cx = cos(pitch), sx = sin(pitch),
           f = 1 / tan(3.141592653589793 / 6);
    double rotation[16] = {cy, sx * sy,  -cx * sy, 0, 0, cx, sx, 0,
                           sy, -sx * cy, cx * cy,  0, 0, 0,  0,  1};
    double pr[16] = {f * h / w, 0, 0, 0, 0, -f, 0, 0, 0, 0, 0, -1, 0, 0, 1, 0};
    for (int col = 0; col < 4; col++)
      for (int row = 0; row < 4; row++)
        for (int k = 0; k < 4; k++)
          m[col * 4 + row] += float(pr[k * 4 + row] * rotation[col * 4 + k]);
  }
  std::array<double, 6> Draw(unsigned mask, bool copy) {
    Vk(vkResetCommandBuffer(cmd, 0));
    VkCommandBufferBeginInfo bi{VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO};
    Vk(vkBeginCommandBuffer(cmd, &bi));
    vkCmdResetQueryPool(cmd, queries, 0, 6);
    VkClearValue clear{};
    VkRenderPassBeginInfo ri{VK_STRUCTURE_TYPE_RENDER_PASS_BEGIN_INFO};
    ri.renderPass = pass;
    ri.framebuffer = fb;
    ri.renderArea = {{0, 0}, {w, h}};
    ri.clearValueCount = 1;
    ri.pClearValues = &clear;
    vkCmdWriteTimestamp(cmd, VK_PIPELINE_STAGE_TOP_OF_PIPE_BIT, queries, 0);
    vkCmdBeginRenderPass(cmd, &ri, VK_SUBPASS_CONTENTS_INLINE);
    vkCmdBindDescriptorSets(cmd, VK_PIPELINE_BIND_POINT_GRAPHICS,
                            d.pipelineLayout, 0, 1, &set, 0, nullptr);
    float lighting[12] = {0, 0, 0, 1, 1, .91f, .68f, .025f, 32, 1, 0, 0};
    uint32_t on = 1;
    memcpy(lighting + 10, &on, 4);
    vkCmdPushConstants(cmd, d.pipelineLayout,
                       VK_SHADER_STAGE_VERTEX_BIT |
                           VK_SHADER_STAGE_FRAGMENT_BIT,
                       0, 48, lighting);
    for (int i = 0; i < 4; i++) {
      if (mask & (1u << i)) {
        vkCmdBindPipeline(cmd, VK_PIPELINE_BIND_POINT_GRAPHICS, pipes[i]);
        if (i == 2) {
          VkDeviceSize off = 0;
          vkCmdBindVertexBuffers(cmd, 0, 1, &vertices.buffer, &off);
        }
        vkCmdDraw(cmd,
                  i == 0   ? 3
                  : i == 1 ? 6
                  : i == 2 ? sphereCount
                           : 24,
                  1, 0, 0);
      }
      vkCmdWriteTimestamp(cmd, VK_PIPELINE_STAGE_BOTTOM_OF_PIPE_BIT, queries,
                          i + 1);
    }
    vkCmdEndRenderPass(cmd);
    vkCmdWriteTimestamp(cmd, VK_PIPELINE_STAGE_BOTTOM_OF_PIPE_BIT, queries, 5);
    if (copy) {
      VkBufferImageCopy c{};
      c.imageSubresource = {VK_IMAGE_ASPECT_COLOR_BIT, 0, 0, 1};
      c.imageExtent = {w, h, 1};
      vkCmdCopyImageToBuffer(cmd, image, VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL,
                             readback.buffer, 1, &c);
      VkMemoryBarrier mb{VK_STRUCTURE_TYPE_MEMORY_BARRIER};
      mb.srcAccessMask = VK_ACCESS_TRANSFER_WRITE_BIT;
      mb.dstAccessMask = VK_ACCESS_HOST_READ_BIT;
      vkCmdPipelineBarrier(cmd, VK_PIPELINE_STAGE_TRANSFER_BIT,
                           VK_PIPELINE_STAGE_HOST_BIT, 0, 1, &mb, 0, nullptr, 0,
                           nullptr);
    }
    Vk(vkEndCommandBuffer(cmd));
    VkSubmitInfo si{VK_STRUCTURE_TYPE_SUBMIT_INFO};
    si.commandBufferCount = 1;
    si.pCommandBuffers = &cmd;
    auto start = std::chrono::steady_clock::now();
    Vk(vkQueueSubmit(d.queue, 1, &si, {}));
    auto end = std::chrono::steady_clock::now();
    Vk(vkQueueWaitIdle(d.queue));
    uint64_t t[6];
    Vk(vkGetQueryPoolResults(d.device, queries, 0, 6, sizeof(t), t, 8,
                             VK_QUERY_RESULT_64_BIT));
    std::array<double, 6> out{};
    out[0] = (t[1] - t[0]) * period / 1e6;
    out[1] = (t[2] - t[1]) * period / 1e6;
    out[2] = (t[3] - t[2]) * period / 1e6;
    out[3] = (t[4] - t[3]) * period / 1e6;
    out[4] = (t[5] - t[0]) * period / 1e6;
    out[5] = std::chrono::duration<double, std::milli>(end - start).count();
    return out;
  }
  std::vector<float> Pixels() {
    auto p = (float *)readback.mapped;
    return {p, p + size_t(w) * h * 4};
  }
  void Save(std::string path) {
    auto p = (float *)readback.mapped;
    std::ofstream f(path, std::ios::binary);
    f << "P6\n" << w << " " << h << "\n255\n";
    for (size_t i = 0; i < size_t(w) * h; i++)
      for (int k = 0; k < 3; k++) {
        unsigned char b =
            (unsigned char)(255 * pow(std::max(0.f, p[i * 4 + k]) /
                                          (1 + std::max(0.f, p[i * 4 + k])),
                                      1 / 2.2));
        f.write((char *)&b, 1);
      }
  }
  ~Renderer() {
    vkDeviceWaitIdle(d.device);
    for (auto p : pipes)
      vkDestroyPipeline(d.device, p, nullptr);
    vkDestroyQueryPool(d.device, queries, nullptr);
    vkDestroyFramebuffer(d.device, fb, nullptr);
    vkDestroyRenderPass(d.device, pass, nullptr);
    vkDestroyImageView(d.device, view, nullptr);
    vkDestroyImage(d.device, image, nullptr);
    vkFreeMemory(d.device, memory, nullptr);
  }
};
// Explicit diagnostic mode can load a historical vertex module without changing
// deployed production files. Default invocation below always enforces the gate.
static void Witness(Renderer &r, const std::string &prefix, bool timing) {
  auto hash = [](const std::vector<float> &pixels) {
    uint64_t h = 14695981039346656037ull;
    for (float f : pixels) {
      uint32_t bits;
      std::memcpy(&bits, &f, 4);
      for (int b = 0; b < 4; b++) {
        h ^= (bits >> (b * 8)) & 255;
        h *= 1099511628211ull;
      }
    }
    return h;
  };
  for (int pose = 0; pose < 6; pose++) {
    double yaw = pose == 0   ? 0
                 : pose == 1 ? .2
                 : pose == 2 ? -.3
                 : pose == 3 ? 1.4
                 : pose == 4 ? 3.141592653589793
                             : 3.3;
    double pitch = pose == 1 ? .1 : pose == 2 ? -.2 : 0;
    r.Pose(yaw, pitch);
    r.Draw(1, true);
    auto background = r.Pixels();
    r.Draw(15, true);
    auto full = r.Pixels();
    r.Save(prefix + "-" + std::to_string(pose) + ".ppm");
    r.Draw(13, true);
    auto noGlow = r.Pixels();
    size_t changed = 0, others = 0;
    double x = 0, y = 0, weight = 0;
    for (size_t i = 0; i < full.size() / 4; i++) {
      double diff = 0;
      for (int c = 0; c < 3; c++)
        diff += std::abs(full[i * 4 + c] - noGlow[i * 4 + c]);
      if (diff > 1e-7) {
        changed++;
        weight += diff;
        x += (i % r.w + .5) * diff;
        y += (i / r.w + .5) * diff;
      }
      for (int c = 0; c < 3; c++)
        if (background[i * 4 + c] != noGlow[i * 4 + c]) {
          others++;
          break;
        }
    }
    float *m = (float *)r.frame.mapped + 8, *b = (float *)r.body.mapped;
    double clip[4]{};
    for (int k = 0; k < 4; k++)
      clip[k] = double(m[8 + k]) * (double(b[2]) + b[46]) + m[12 + k];
    std::cout << "WITNESS pose=" << pose << " yaw=" << yaw << " pitch=" << pitch
              << " clipW=" << clip[3]
              << " signedPixel=" << (clip[0] / clip[3] + 1) * r.w / 2 << ","
              << (clip[1] / clip[3] + 1) * r.h / 2 << " absolutePixel="
              << (clip[0] / std::abs(clip[3]) + 1) * r.w / 2 << ","
              << (clip[1] / std::abs(clip[3]) + 1) * r.h / 2
              << " centroid=" << (weight ? x / weight : -1) << ","
              << (weight ? y / weight : -1) << " glowPixels=" << changed
              << " otherPixels=" << others << " fullHDR=" << hash(full)
              << " backgroundHDR=" << hash(background) << '\n';
  }
  if (timing)
    for (int pose : {0, 4}) {
      r.Pose(pose ? 3.141592653589793 : 0, 0);
      for (int i = 0; i < 16; i++)
        r.Draw(15, false);
      std::array<std::vector<double>, 6> samples;
      for (int i = 0; i < 128; i++) {
        auto v = r.Draw(15, false);
        for (int k = 0; k < 6; k++)
          samples[k].push_back(v[k]);
      }
      for (int k = 0; k < 6; k++) {
        auto &v = samples[k];
        std::sort(v.begin(), v.end());
        std::cout << "COST pose=" << pose << " phase=" << k
                  << " samples=128 median=" << v[63] << " p95=" << v[121]
                  << " p99=" << v[126] << " max=" << v[127] << " ms\n";
      }
    }
}
int main(int argc, char **argv) {
  try {
    if ((argc == 5 || argc == 6) && std::string(argv[2]) == "--witness") {
      const bool timing = argc == 6;
      Require(!timing || std::string(argv[5]) == "--timing",
              "unknown witness option");
      Renderer r(argv[3], timing ? 3440 : 860, timing ? 1440 : 360);
      r.Setup(argv[1]);
      Witness(r, argv[4], timing);
      Require(Device::validationErrors == 0,
              "witness Vulkan validation errors");
      return 0;
    }
    Require(argc == 2, "expected production stellar_glow.vert.spv path");
    const auto directory =
        std::filesystem::path(argv[1]).parent_path().string();
    Renderer r(directory, 860, 360);
    r.Setup(argv[1]);
    unsigned cases = 0;
    // Source distances exercise Sun approaches, Earth-scale observation and
    // Solar overview. Rotations cover rear hemisphere, front hemisphere and
    // both off-frustum directions.
    for (double distance : {5.e9, 1.496e11, 4.e12})
      for (int pose = 0; pose < 8; pose++) {
        const double pi = 3.141592653589793;
        double yaw = pose == 0   ? 0
                     : pose == 1 ? .2
                     : pose == 2 ? -.3
                     : pose == 3 ? 1.4
                     : pose == 4 ? pi
                     : pose == 5 ? 3.3
                     : pose == 6 ? pi - 1.4
                                 : pi + .25;
        double pitch = pose == 1 ? .1 : pose == 2 ? -.2 : pose == 7 ? .2 : 0;
        r.Pose(yaw, pitch, distance);
        r.Draw(1, true);
        auto background = r.Pixels();
        r.Draw(15, true);
        auto full = r.Pixels();
        r.Draw(13, true);
        auto withoutGlow = r.Pixels();
        r.Draw(2, true);
        auto glow = r.Pixels();
        float *m = (float *)r.frame.mapped + 8, *b = (float *)r.body.mapped;
        double clip[4]{};
        for (int k = 0; k < 4; k++)
          clip[k] = double(m[8 + k]) * (double(b[2]) + b[46]) + m[12 + k];
        double weight = 0, x = 0, y = 0;
        size_t pixels = 0;
        for (size_t i = 0; i < glow.size() / 4; i++) {
          const double v = glow[i * 4] + glow[i * 4 + 1] + glow[i * 4 + 2];
          if (v > 0) {
            weight += v;
            x += (i % r.w + .5) * v;
            y += (i / r.w + .5) * v;
            pixels++;
          }
        }
        if (clip[3] <= 0) {
          Require(pixels == 0, "rear-facing stellar glow leaked into sky");
          Require(full == background && withoutGlow == background,
                  "rear Sun/marker/background isolation mismatch");
        } else if (pose == 6) {
          Require(pixels == 0,
                  "fully off-frustum halo must rasterize no pixels");
        } else {
          Require(pixels > 0 && full != withoutGlow,
                  "front stellar halo unexpectedly removed");
          const double px = (clip[0] / clip[3] + 1) * r.w / 2,
                       py = (clip[1] / clip[3] + 1) * r.h / 2;
          // Subpixel sampling asymmetry of this smooth finite quad is bounded
          // by one pixel.
          Require(std::abs(x / weight - px) < 1 &&
                      std::abs(y / weight - py) < 1,
                  "halo centroid detached from signed stellar projection");
        }
        std::cout << "STELLAR_PROJECTION distance=" << distance
                  << " pose=" << pose << " clipW=" << clip[3]
                  << " pixels=" << pixels << " PASS\n";
        cases++;
      }
    r.Pose(3.141592653589793, 0);
    uint32_t disabled = 1;
    std::memcpy((float *)r.body.mapped + 11, &disabled, 4);
    r.Draw(2, true);
    auto disabledPixels = r.Pixels();
    for (auto f : disabledPixels)
      Require(f == 0, "non-stellar instance generated halo");
    r.Pose(0, 0, 0);
    r.Draw(2, true);
    for (auto f : r.Pixels())
      Require(f == 0, "zero projective depth generated halo");
    Require(Device::validationErrors == 0,
            "stellar projection Vulkan validation errors");
    std::cout << "Stellar projection PASS: " << cases
              << " production raster cases, disabled instance, zero depth; "
                 "strict validation\n";
    return 0;
  } catch (const std::exception &e) {
    std::cerr << e.what() << '\n';
    return 1;
  }
}
