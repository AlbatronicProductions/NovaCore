#include "NovaCoreNative.h"
#include <algorithm>
#include <array>
#include <chrono>
#include <cmath>
#include <compare>
#include <cstddef>
#include <cstdio>
#include <cstring>
#include <fstream>
#include <optional>
#include <stdexcept>
#include <string>
#include <vector>
#include <vulkan/vulkan.h>
#include <windows.h>

namespace {
constexpr uint32_t Width = 960, Height = 540;
constexpr uint32_t GpuPatchCapacity = 6144, GpuNodeCount = 8190;
static_assert(sizeof(NcEncodedPosition) == 32);
static_assert(sizeof(NcCameraData) == 96);
static_assert(alignof(NcCameraData) == 16);
static_assert(offsetof(NcCameraData, position) == 0);
static_assert(offsetof(NcCameraData, viewProjection) == 32);
static_assert(sizeof(NcRenderTransform) == 32);
static_assert(sizeof(NcRenderObject) == 80);
static_assert(offsetof(NcRenderObject, position) == 0 &&
              offsetof(NcRenderObject, transform) == 32 &&
              offsetof(NcRenderObject, mesh) == 64);
static_assert(sizeof(NcDrawBatch) == 16);
static_assert(sizeof(NcPlanetaryPatch) == 64);
static_assert(sizeof(NcPlanetaryGpuConstants) == 48);
static_assert(alignof(NcPlanetaryGpuConstants) == 16);
static_assert(offsetof(NcPlanetaryGpuConstants, refinementThreshold) == 16 &&
              offsetof(NcPlanetaryGpuConstants, maximumLevel) == 32);
static_assert(sizeof(NcFrameSubmission) == 272);
static_assert(offsetof(NcFrameSubmission, planetaryGpu) == 208);
static_assert(offsetof(NcFrameSubmission, planetaryMode) == 256);
static_assert(sizeof(NcOrbitLineVertex) == 12);
static_assert(sizeof(NcInputState) == 64);
static_assert(offsetof(NcInputState, deltaSeconds) == 0);
static_assert(offsetof(NcInputState, moveLeft) == 4);
static_assert(offsetof(NcInputState, moveRight) == 8);
static_assert(offsetof(NcInputState, moveForward) == 12);
static_assert(offsetof(NcInputState, moveBackward) == 16);
static_assert(offsetof(NcInputState, moveDown) == 20);
static_assert(offsetof(NcInputState, moveUp) == 24);
static_assert(offsetof(NcInputState, reset) == 28);
static_assert(offsetof(NcInputState, lookActive) == 32);
static_assert(offsetof(NcInputState, mouseDeltaX) == 36);
static_assert(offsetof(NcInputState, mouseDeltaY) == 40);
static_assert(offsetof(NcInputState, mouseWheelDetents) == 44);
static_assert(offsetof(NcInputState, pauseToggle) == 48);
static_assert(offsetof(NcInputState, rateDecrease) == 52);
static_assert(offsetof(NcInputState, rateIncrease) == 56);
static_assert(offsetof(NcInputState, sasModeKey) == 60);
struct Vertex {
  float position[3];
  float color[3];
};
static_assert(sizeof(Vertex) == 24);
struct PatchVertex { float uv[2]; };
static_assert(sizeof(PatchVertex) == 8);
struct Mesh {
  VkBuffer vb{}, ib{};
  VkDeviceMemory vm{}, im{};
  uint32_t indices{};
};
struct GpuPlanetaryControl {
  VkDrawIndexedIndirectCommand draw{};
  uint32_t roots{}, candidates{}, refined{}, culled{}, active{}, balanced{},
      minimumLevel{}, maximumLevel{}, overflow{}, padding[2]{};
};
static_assert(sizeof(GpuPlanetaryControl) == 64);
struct PatchIdentity {
  uint32_t face{}, level{}, x{}, y{}, stitchMask{};
  auto operator<=>(const PatchIdentity &) const = default;
};
struct Queues {
  std::optional<uint32_t> graphics, present;
  bool Complete() const { return graphics && present; }
};
struct App {
  NcHostCallback cb{};
  void *cbData{};
  NcFrameSubmission *submission{};
  HWND window{};
  VkInstance instance{};
  VkDebugUtilsMessengerEXT debug{};
  VkSurfaceKHR surface{};
  VkPhysicalDevice physical{};
  VkDevice device{};
  VkQueue graphicsQueue{}, presentQueue{};
  VkSwapchainKHR swapchain{};
  VkFormat format{};
  VkExtent2D extent{};
  std::vector<VkImage> images;
  std::vector<VkImageView> views;
  VkRenderPass renderPass{};
  VkPipelineLayout pipelineLayout{};
  VkPipeline pipeline{};
  VkPipeline planetaryPipeline{};
  VkPipeline planetaryComputePipeline{};
  VkPipeline orbitPipeline{};
  VkPipeline previousOrbitPipeline{};
  VkPipeline bodyForwardPipeline{};
  VkPipeline targetDirectionPipeline{};
  std::vector<VkFramebuffer> framebuffers;
  VkCommandPool pool{};
  std::vector<VkCommandBuffer> commands;
  VkSemaphore imageAvailable{};
  std::vector<VkSemaphore> renderFinished;
  VkFence fence{};
  bool resized{};
  uint64_t frame{};
  VkBuffer submissionBuffer{};
  VkDeviceMemory submissionMemory{};
  void *mapped{};
  VkDeviceSize submissionSize{};
  VkDescriptorSetLayout descriptorLayout{};
  VkDescriptorPool descriptorPool{};
  VkDescriptorSet descriptor{};
  VkBuffer patchBuffer{};
  VkDeviceMemory patchMemory{};
  void *patchMapped{};
  VkDeviceSize patchSize{};
  VkBuffer gpuInputBuffer{}, gpuWorkBuffer{}, gpuNodeBuffer{}, gpuControlBuffer{};
  VkDeviceMemory gpuInputMemory{}, gpuWorkMemory{}, gpuNodeMemory{}, gpuControlMemory{};
  void *gpuInputMapped{}, *gpuWorkMapped{}, *gpuNodeMapped{}, *gpuControlMapped{};
  std::vector<NcPlanetaryPatch> validationCpuOracle;
  bool gpuFrameSubmitted{}, hasGpuTelemetry{}, hasParityResult{};
  GpuPlanetaryControl lastGpuTelemetry{};
  uint64_t lastCpuHash{}, lastGpuHash{};
  VkBuffer orbitBuffer{};
  VkDeviceMemory orbitMemory{};
  void *orbitMapped{};
  VkDeviceSize orbitSize{};
  VkBuffer previousOrbitBuffer{};
  VkDeviceMemory previousOrbitMemory{};
  void *previousOrbitMapped{};
  VkDeviceSize previousOrbitSize{};
  VkBuffer bodyForwardBuffer{};
  VkDeviceMemory bodyForwardMemory{};
  void *bodyForwardMapped{};
  VkDeviceSize bodyForwardSize{};
  VkBuffer targetDirectionBuffer{};
  VkDeviceMemory targetDirectionMemory{};
  void *targetDirectionMapped{};
  VkDeviceSize targetDirectionSize{};
  Mesh triangle{};
  Mesh planetaryPatch{};
  LONG rawMouseX{}, rawMouseY{};
  LONG wheelDeltaRaw{};
  bool lookActive{};
  bool pauseWasDown{}, rateDecreaseWasDown{}, rateIncreaseWasDown{};
  std::array<bool, 8> sasModeWasDown{};
  void Log(uint32_t cat, const char *msg) const {
    if (cb) {
      NcHostEvent e{NC_DIAGNOSTIC, cat, msg, {}, {}};
      cb(&e, cbData);
    }
  }
  void Check(VkResult r, const char *what) {
    if (r != VK_SUCCESS) {
      Log(NC_LOG_ALWAYS, what);
      throw std::runtime_error(what);
    }
  }
};
App *gApp{};
void ClearRawInput(App &a) {
  a.rawMouseX = 0;
  a.rawMouseY = 0;
  a.lookActive = false;
  a.wheelDeltaRaw = 0;
  a.pauseWasDown = false;
  a.rateDecreaseWasDown = false;
  a.rateIncreaseWasDown = false;
  a.sasModeWasDown.fill(false);
}
void ClearLookInput(App &a) {
  a.rawMouseX = 0;
  a.rawMouseY = 0;
  a.lookActive = false;
}
void RawInput(App &a, LPARAM l) {
  UINT size = 0;
  GetRawInputData((HRAWINPUT)l, RID_INPUT, nullptr, &size,
                  sizeof(RAWINPUTHEADER));
  std::vector<BYTE> data(size);
  if (GetRawInputData((HRAWINPUT)l, RID_INPUT, data.data(), &size,
                      sizeof(RAWINPUTHEADER)) != size)
    return;
  auto *raw = (RAWINPUT *)data.data();
  if (raw->header.dwType == RIM_TYPEMOUSE && a.lookActive) {
    a.rawMouseX += raw->data.mouse.lLastX;
    a.rawMouseY += raw->data.mouse.lLastY;
  }
}
LRESULT CALLBACK Proc(HWND h, UINT m, WPARAM w, LPARAM l) {
  if (m == WM_SIZE && gApp)
    gApp->resized = true;
  if (m == WM_INPUT && gApp)
    RawInput(*gApp, l);
  if (m == WM_MOUSEWHEEL && gApp) {
    const auto delta = GET_WHEEL_DELTA_WPARAM(w);
    gApp->wheelDeltaRaw += delta;
    char message[160];
    std::snprintf(message, sizeof(message),
                  "WM_MOUSEWHEEL wParam=0x%llX delta=%d accumulatedRaw=%ld",
                  static_cast<unsigned long long>(w), static_cast<int>(delta),
                  static_cast<long>(gApp->wheelDeltaRaw));
    gApp->Log(NC_LOG_CAMERA, message);
  }
  if ((m == WM_KILLFOCUS || m == WM_CAPTURECHANGED || m == WM_DESTROY) && gApp)
    ClearRawInput(*gApp);
  if (m == WM_CLOSE)
    DestroyWindow(h);
  if (m == WM_DESTROY)
    PostQuitMessage(0);
  return DefWindowProc(h, m, w, l);
}
VKAPI_ATTR VkBool32 VKAPI_CALL
Debug(VkDebugUtilsMessageSeverityFlagBitsEXT, VkDebugUtilsMessageTypeFlagsEXT,
      const VkDebugUtilsMessengerCallbackDataEXT *d, void *u) {
  static_cast<App *>(u)->Log(NC_LOG_VALIDATION, d->pMessage);
  return VK_FALSE;
}
Queues FindQueues(VkPhysicalDevice d, VkSurfaceKHR s) {
  Queues q;
  uint32_t n = 0;
  vkGetPhysicalDeviceQueueFamilyProperties(d, &n, nullptr);
  std::vector<VkQueueFamilyProperties> p(n);
  vkGetPhysicalDeviceQueueFamilyProperties(d, &n, p.data());
  for (uint32_t i = 0; i < n; i++) {
    if ((p[i].queueFlags & (VK_QUEUE_GRAPHICS_BIT|VK_QUEUE_COMPUTE_BIT)) == (VK_QUEUE_GRAPHICS_BIT|VK_QUEUE_COMPUTE_BIT))
      q.graphics = i;
    VkBool32 present{};
    vkGetPhysicalDeviceSurfaceSupportKHR(d, i, s, &present);
    if (present)
      q.present = i;
  }
  return q;
}
uint32_t Memory(App &a, uint32_t bits, VkMemoryPropertyFlags props) {
  VkPhysicalDeviceMemoryProperties m;
  vkGetPhysicalDeviceMemoryProperties(a.physical, &m);
  for (uint32_t i = 0; i < m.memoryTypeCount; i++)
    if ((bits & (1u << i)) && (m.memoryTypes[i].propertyFlags & props) == props)
      return i;
  throw std::runtime_error("no suitable GPU memory type");
}
void Buffer(App &a, VkDeviceSize size, VkBufferUsageFlags usage, VkBuffer &b,
            VkDeviceMemory &m, const void *data) {
  VkBufferCreateInfo ci{VK_STRUCTURE_TYPE_BUFFER_CREATE_INFO};
  ci.size = size;
  ci.usage = usage;
  ci.sharingMode = VK_SHARING_MODE_EXCLUSIVE;
  a.Check(vkCreateBuffer(a.device, &ci, nullptr, &b), "buffer create failed");
  VkMemoryRequirements r;
  vkGetBufferMemoryRequirements(a.device, b, &r);
  VkMemoryAllocateInfo ai{VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO};
  ai.allocationSize = r.size;
  ai.memoryTypeIndex = Memory(a, r.memoryTypeBits,
                              VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT |
                                  VK_MEMORY_PROPERTY_HOST_COHERENT_BIT);
  a.Check(vkAllocateMemory(a.device, &ai, nullptr, &m),
          "buffer allocation failed");
  a.Check(vkBindBufferMemory(a.device, b, m, 0), "buffer bind failed");
  void *p{};
  a.Check(vkMapMemory(a.device, m, 0, size, 0, &p), "buffer map failed");
  std::memcpy(p, data, (size_t)size);
  vkUnmapMemory(a.device, m);
}
void CreateMesh(App &a) {
  constexpr std::array<Vertex, 3> v{{{{0, -.04f, 0}, {1, 0, 0}},
                                     {{.04f, .04f, 0}, {0, 1, 0}},
                                     {{-.04f, .04f, 0}, {0, 0, 1}}}};
  constexpr std::array<uint32_t, 3> i{{0, 1, 2}};
  Buffer(a, sizeof(v), VK_BUFFER_USAGE_VERTEX_BUFFER_BIT, a.triangle.vb,
         a.triangle.vm, v.data());
  Buffer(a, sizeof(i), VK_BUFFER_USAGE_INDEX_BUFFER_BIT, a.triangle.ib,
         a.triangle.im, i.data());
  a.triangle.indices = 3;
  constexpr uint32_t cells=16, side=cells+1;
  std::array<PatchVertex,side*side> pv{};
  std::array<uint32_t,cells*cells*6> pi{};
  uint32_t vertex=0,index=0;
  for(uint32_t y=0;y<side;y++)for(uint32_t x=0;x<side;x++)pv[vertex++]={{float(x)/cells,float(y)/cells}};
  for(uint32_t y=0;y<cells;y++)for(uint32_t x=0;x<cells;x++){uint32_t q=y*side+x;pi[index++]=q;pi[index++]=q+side;pi[index++]=q+1;pi[index++]=q+1;pi[index++]=q+side;pi[index++]=q+side+1;}
  Buffer(a,sizeof(pv),VK_BUFFER_USAGE_VERTEX_BUFFER_BIT,a.planetaryPatch.vb,a.planetaryPatch.vm,pv.data());
  Buffer(a,sizeof(pi),VK_BUFFER_USAGE_INDEX_BUFFER_BIT,a.planetaryPatch.ib,a.planetaryPatch.im,pi.data());
  a.planetaryPatch.indices=(uint32_t)pi.size();
  a.Log(NC_LOG_VULKAN, "Created built-in triangle mesh (host-visible static "
                       "vertex/index buffers)");
}
void DestroyMesh(App &a) {
  if (a.triangle.vb)
    vkDestroyBuffer(a.device, a.triangle.vb, nullptr);
  if (a.triangle.vm)
    vkFreeMemory(a.device, a.triangle.vm, nullptr);
  if (a.triangle.ib)
    vkDestroyBuffer(a.device, a.triangle.ib, nullptr);
  if (a.triangle.im)
    vkFreeMemory(a.device, a.triangle.im, nullptr);
  a.triangle = {};
  if(a.planetaryPatch.vb)vkDestroyBuffer(a.device,a.planetaryPatch.vb,nullptr);
  if(a.planetaryPatch.vm)vkFreeMemory(a.device,a.planetaryPatch.vm,nullptr);
  if(a.planetaryPatch.ib)vkDestroyBuffer(a.device,a.planetaryPatch.ib,nullptr);
  if(a.planetaryPatch.im)vkFreeMemory(a.device,a.planetaryPatch.im,nullptr);
  a.planetaryPatch={};
}
Mesh *MeshFor(App &a, NcMeshHandle h) {
  return h.value == 1 ? &a.triangle : nullptr;
}
void Validate(App &a) {
  auto *s = a.submission;
  if ((s->objectCount && !s->objects) || (s->batchCount && !s->batches))
    throw std::runtime_error("invalid frame submission pointer");
  for (uint32_t i = 0; i < s->objectCount; i++) {
    auto &o = s->objects[i];
    if (!MeshFor(a, o.mesh))
      throw std::runtime_error("invalid mesh handle in render object");
    for (float x : o.transform.rotation)
      if (!std::isfinite(x))
        throw std::runtime_error("non-finite rotation");
    for (float x : o.transform.scale)
      if (!std::isfinite(x))
        throw std::runtime_error("non-finite scale");
  }
  for (uint32_t i = 0; i < s->batchCount; i++) {
    auto &b = s->batches[i];
    if (!MeshFor(a, b.mesh) || !b.objectCount ||
        b.firstObject > s->objectCount ||
        b.objectCount > s->objectCount - b.firstObject)
      throw std::runtime_error("invalid render batch");
  }
  if(nc_validate_planetary_patches(s->planetaryPatches,s->planetaryPatchCount)!=NC_SUCCESS)throw std::runtime_error("invalid planetary patch submission");
  if(s->planetaryGpuAlignmentPadding||s->planetaryPadding[0]||s->planetaryPadding[1]||s->planetaryPadding[2])throw std::runtime_error("invalid planetary frame padding");
  if(s->planetaryMode>NC_PLANETARY_CPU_GPU_VALIDATION)throw std::runtime_error("invalid planetary mode");
  if(s->planetaryMode!=NC_PLANETARY_CPU_REFERENCE){const auto &g=s->planetaryGpu;if(!std::isfinite(g.cameraBodyX)||!std::isfinite(g.cameraBodyY)||!std::isfinite(g.cameraBodyZ)||!std::isfinite(g.radius)||g.radius<=0||!std::isfinite(g.refinementThreshold)||g.refinementThreshold<=0||!std::isfinite(g.nearFieldAltitudeRadii)||g.nearFieldAltitudeRadii<=0||g.maximumLevel>5||!g.outputCapacity||g.outputCapacity>GpuPatchCapacity||g.padding0||g.padding1||g.padding2||g.padding3)throw std::runtime_error("invalid planetary GPU constants");if(s->planetaryMode==NC_PLANETARY_GPU_PRODUCTION&&s->planetaryPatchCount)throw std::runtime_error("GPU planetary mode received CPU leaves");}
}
void Window(App &a) {
  WNDCLASSW wc{.lpfnWndProc = Proc,
               .hInstance = GetModuleHandleW(nullptr),
               .lpszClassName = L"NovaCoreWindow"};
  RegisterClassW(&wc);
  a.window =
      CreateWindowExW(0, wc.lpszClassName, L"NovaCore - Generic Mesh Rendering",
                      WS_OVERLAPPEDWINDOW, CW_USEDEFAULT, CW_USEDEFAULT, Width,
                      Height, nullptr, nullptr, wc.hInstance, nullptr);
  if (!a.window)
    throw std::runtime_error("CreateWindowExW failed");
  RAWINPUTDEVICE device{1, 2, RIDEV_INPUTSINK, a.window};
  if (!RegisterRawInputDevices(&device, 1, sizeof(device)))
    throw std::runtime_error("RegisterRawInputDevices failed");
  ShowWindow(a.window, SW_SHOW);
}
void Instance(App &a) {
  const char *ext[]{VK_KHR_SURFACE_EXTENSION_NAME,
                    VK_KHR_WIN32_SURFACE_EXTENSION_NAME,
                    VK_EXT_DEBUG_UTILS_EXTENSION_NAME};
  const char *layer = "VK_LAYER_KHRONOS_validation";
  VkApplicationInfo ai{VK_STRUCTURE_TYPE_APPLICATION_INFO,
                       nullptr,
                       "NovaCore",
                       1,
                       "NovaCore",
                       1,
                       VK_API_VERSION_1_0};
  VkInstanceCreateInfo ci{VK_STRUCTURE_TYPE_INSTANCE_CREATE_INFO};
  ci.pApplicationInfo = &ai;
  ci.enabledExtensionCount = 3;
  ci.ppEnabledExtensionNames = ext;
#ifdef NC_DEBUG_BUILD
  ci.enabledLayerCount = 1;
  ci.ppEnabledLayerNames = &layer;
#endif
  a.Check(vkCreateInstance(&ci, nullptr, &a.instance),
          "vkCreateInstance failed");
}
void SetupDebug(App &a) {
  auto fn = (PFN_vkCreateDebugUtilsMessengerEXT)vkGetInstanceProcAddr(
      a.instance, "vkCreateDebugUtilsMessengerEXT");
  if (!fn)
    return;
  VkDebugUtilsMessengerCreateInfoEXT ci{
      VK_STRUCTURE_TYPE_DEBUG_UTILS_MESSENGER_CREATE_INFO_EXT};
  ci.messageSeverity = VK_DEBUG_UTILS_MESSAGE_SEVERITY_WARNING_BIT_EXT |
                       VK_DEBUG_UTILS_MESSAGE_SEVERITY_ERROR_BIT_EXT;
  ci.messageType = VK_DEBUG_UTILS_MESSAGE_TYPE_GENERAL_BIT_EXT |
                   VK_DEBUG_UTILS_MESSAGE_TYPE_VALIDATION_BIT_EXT |
                   VK_DEBUG_UTILS_MESSAGE_TYPE_PERFORMANCE_BIT_EXT;
  ci.pfnUserCallback = Debug;
  ci.pUserData = &a;
  a.Check(fn(a.instance, &ci, nullptr, &a.debug), "debug messenger failed");
}
void Surface(App &a) {
  VkWin32SurfaceCreateInfoKHR ci{
      VK_STRUCTURE_TYPE_WIN32_SURFACE_CREATE_INFO_KHR};
  ci.hinstance = GetModuleHandleW(nullptr);
  ci.hwnd = a.window;
  a.Check(vkCreateWin32SurfaceKHR(a.instance, &ci, nullptr, &a.surface),
          "surface creation failed");
}
bool Suitable(VkPhysicalDevice d, VkSurfaceKHR s) {
  auto q = FindQueues(d, s);
  uint32_t n = 0;
  vkEnumerateDeviceExtensionProperties(d, nullptr, &n, nullptr);
  std::vector<VkExtensionProperties> x(n);
  vkEnumerateDeviceExtensionProperties(d, nullptr, &n, x.data());
  return q.Complete() && std::any_of(x.begin(), x.end(), [](auto &e) {
           return !std::strcmp(e.extensionName,
                               VK_KHR_SWAPCHAIN_EXTENSION_NAME);
         });
}
void Device(App &a) {
  uint32_t n = 0;
  vkEnumeratePhysicalDevices(a.instance, &n, nullptr);
  std::vector<VkPhysicalDevice> d(n);
  vkEnumeratePhysicalDevices(a.instance, &n, d.data());
  for (auto x : d)
    if (Suitable(x, a.surface)) {
      a.physical = x;
      break;
    }
  if (!a.physical)
    throw std::runtime_error("no suitable Vulkan GPU");
  VkPhysicalDeviceProperties p;
  vkGetPhysicalDeviceProperties(a.physical, &p);
  char text[256];
  std::snprintf(text, sizeof text, "GPU: %s | Vulkan %u.%u", p.deviceName,
                VK_VERSION_MAJOR(p.apiVersion), VK_VERSION_MINOR(p.apiVersion));
  a.Log(NC_LOG_STARTUP, text);
  auto q = FindQueues(a.physical, a.surface);
  float pri = 1;
  std::array<uint32_t, 2> ids{*q.graphics, *q.present};
  std::vector<VkDeviceQueueCreateInfo> qs;
  for (uint32_t id : ids)
    if (std::none_of(qs.begin(), qs.end(),
                     [id](auto &v) { return v.queueFamilyIndex == id; }))
      qs.push_back({VK_STRUCTURE_TYPE_DEVICE_QUEUE_CREATE_INFO, nullptr, 0, id,
                    1, &pri});
  const char *ex = VK_KHR_SWAPCHAIN_EXTENSION_NAME;
  VkDeviceCreateInfo ci{VK_STRUCTURE_TYPE_DEVICE_CREATE_INFO};
  ci.queueCreateInfoCount = (uint32_t)qs.size();
  ci.pQueueCreateInfos = qs.data();
  ci.enabledExtensionCount = 1;
  ci.ppEnabledExtensionNames = &ex;
  a.Check(vkCreateDevice(a.physical, &ci, nullptr, &a.device),
          "logical device failed");
  vkGetDeviceQueue(a.device, *q.graphics, 0, &a.graphicsQueue);
  vkGetDeviceQueue(a.device, *q.present, 0, &a.presentQueue);
  a.Log(NC_LOG_VALIDATION, "Enabled layer: VK_LAYER_KHRONOS_validation");
}
VkSurfaceFormatKHR Format(App &a) {
  uint32_t n;
  a.Check(
      vkGetPhysicalDeviceSurfaceFormatsKHR(a.physical, a.surface, &n, nullptr),
      "surface format count failed");
  std::vector<VkSurfaceFormatKHR> v(n);
  a.Check(
      vkGetPhysicalDeviceSurfaceFormatsKHR(a.physical, a.surface, &n, v.data()),
      "surface format query failed");
  return v[0];
}
void DestroySwap(App &a) {
  for (auto x : a.framebuffers)
    vkDestroyFramebuffer(a.device, x, nullptr);
  a.framebuffers.clear();
  if (a.pipeline)
    vkDestroyPipeline(a.device, a.pipeline, nullptr);
  if (a.planetaryPipeline)
    vkDestroyPipeline(a.device,a.planetaryPipeline,nullptr);
  if (a.planetaryComputePipeline)
    vkDestroyPipeline(a.device,a.planetaryComputePipeline,nullptr);
  if (a.orbitPipeline)
    vkDestroyPipeline(a.device, a.orbitPipeline, nullptr);
  if (a.previousOrbitPipeline)
    vkDestroyPipeline(a.device, a.previousOrbitPipeline, nullptr);
  if (a.bodyForwardPipeline)
    vkDestroyPipeline(a.device, a.bodyForwardPipeline, nullptr);
  if (a.targetDirectionPipeline)
    vkDestroyPipeline(a.device, a.targetDirectionPipeline, nullptr);
  a.pipeline = {};
  a.planetaryPipeline = {};
  a.planetaryComputePipeline = {};
  a.orbitPipeline = {};
  a.previousOrbitPipeline = {};
  a.bodyForwardPipeline = {};
  a.targetDirectionPipeline = {};
  if (a.pipelineLayout)
    vkDestroyPipelineLayout(a.device, a.pipelineLayout, nullptr);
  if (a.descriptorLayout)
    vkDestroyDescriptorSetLayout(a.device, a.descriptorLayout, nullptr);
  if (a.renderPass)
    vkDestroyRenderPass(a.device, a.renderPass, nullptr);
  for (auto x : a.views)
    vkDestroyImageView(a.device, x, nullptr);
  a.views.clear();
  if (a.swapchain)
    vkDestroySwapchainKHR(a.device, a.swapchain, nullptr);
  a.swapchain = {};
}
std::vector<char> Read(const char *n) {
  HMODULE m{};
  GetModuleHandleExA(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS |
                         GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
                     reinterpret_cast<LPCSTR>(&Read), &m);
  char p[MAX_PATH]{};
  GetModuleFileNameA(m, p, MAX_PATH);
  std::string f(p);
  f.resize(f.find_last_of("\\/") + 1);
  std::ifstream in(f + n, std::ios::binary | std::ios::ate);
  if (!in)
    throw std::runtime_error("shader file missing");
  auto z = in.tellg();
  std::vector<char> b((size_t)z);
  in.seekg(0);
  in.read(b.data(), z);
  return b;
}
VkShaderModule Shader(App &a, const char *n) {
  auto b = Read(n);
  VkShaderModuleCreateInfo ci{VK_STRUCTURE_TYPE_SHADER_MODULE_CREATE_INFO};
  ci.codeSize = b.size();
  ci.pCode = (const uint32_t *)b.data();
  VkShaderModule m;
  a.Check(vkCreateShaderModule(a.device, &ci, nullptr, &m),
          "shader module failed");
  return m;
}
void Swap(App &a) {
  VkSurfaceCapabilitiesKHR c;
  a.Check(vkGetPhysicalDeviceSurfaceCapabilitiesKHR(a.physical, a.surface, &c),
          "surface caps failed");
  auto sf = Format(a);
  a.extent = c.currentExtent.width == UINT32_MAX ? VkExtent2D{Width, Height}
                                                 : c.currentExtent;
  a.extent.width = std::clamp(a.extent.width, c.minImageExtent.width,
                              c.maxImageExtent.width);
  a.extent.height = std::clamp(a.extent.height, c.minImageExtent.height,
                               c.maxImageExtent.height);
  uint32_t count = std::min(c.minImageCount + 1,
                            c.maxImageCount ? c.maxImageCount : UINT32_MAX);
  auto q = FindQueues(a.physical, a.surface);
  uint32_t ids[]{*q.graphics, *q.present};
  VkSwapchainCreateInfoKHR ci{VK_STRUCTURE_TYPE_SWAPCHAIN_CREATE_INFO_KHR};
  ci.surface = a.surface;
  ci.minImageCount = count;
  ci.imageFormat = sf.format;
  ci.imageColorSpace = sf.colorSpace;
  ci.imageExtent = a.extent;
  ci.imageArrayLayers = 1;
  ci.imageUsage = VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT;
  ci.imageSharingMode = *q.graphics == *q.present ? VK_SHARING_MODE_EXCLUSIVE
                                                  : VK_SHARING_MODE_CONCURRENT;
  ci.queueFamilyIndexCount = *q.graphics == *q.present ? 0 : 2;
  ci.pQueueFamilyIndices = ids;
  ci.preTransform = c.currentTransform;
  ci.compositeAlpha = VK_COMPOSITE_ALPHA_OPAQUE_BIT_KHR;
  ci.presentMode = VK_PRESENT_MODE_FIFO_KHR;
  ci.clipped = VK_TRUE;
  a.Check(vkCreateSwapchainKHR(a.device, &ci, nullptr, &a.swapchain),
          "swapchain failed");
  a.format = sf.format;
  uint32_t n;
  a.Check(vkGetSwapchainImagesKHR(a.device, a.swapchain, &n, nullptr),
          "swap images failed");
  a.images.resize(n);
  a.Check(vkGetSwapchainImagesKHR(a.device, a.swapchain, &n, a.images.data()),
          "swap image query failed");
  a.views.resize(n);
  for (uint32_t i = 0; i < n; i++) {
    VkImageViewCreateInfo vi{VK_STRUCTURE_TYPE_IMAGE_VIEW_CREATE_INFO};
    vi.image = a.images[i];
    vi.viewType = VK_IMAGE_VIEW_TYPE_2D;
    vi.format = a.format;
    vi.subresourceRange.aspectMask = VK_IMAGE_ASPECT_COLOR_BIT;
    vi.subresourceRange.levelCount = 1;
    vi.subresourceRange.layerCount = 1;
    a.Check(vkCreateImageView(a.device, &vi, nullptr, &a.views[i]),
            "view failed");
  }
  VkAttachmentDescription ad{};
  ad.format = a.format;
  ad.samples = VK_SAMPLE_COUNT_1_BIT;
  ad.loadOp = VK_ATTACHMENT_LOAD_OP_CLEAR;
  ad.storeOp = VK_ATTACHMENT_STORE_OP_STORE;
  ad.finalLayout = VK_IMAGE_LAYOUT_PRESENT_SRC_KHR;
  VkAttachmentReference ar{0, VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL};
  VkSubpassDescription sub{};
  sub.pipelineBindPoint = VK_PIPELINE_BIND_POINT_GRAPHICS;
  sub.colorAttachmentCount = 1;
  sub.pColorAttachments = &ar;
  VkRenderPassCreateInfo rp{VK_STRUCTURE_TYPE_RENDER_PASS_CREATE_INFO};
  rp.attachmentCount = 1;
  rp.pAttachments = &ad;
  rp.subpassCount = 1;
  rp.pSubpasses = &sub;
  a.Check(vkCreateRenderPass(a.device, &rp, nullptr, &a.renderPass),
          "render pass failed");
  VkDescriptorSetLayoutBinding binds[6]{};
  for(uint32_t binding=0;binding<6;binding++){binds[binding].binding=binding;binds[binding].descriptorType=VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;binds[binding].descriptorCount=1;binds[binding].stageFlags=binding==0?VK_SHADER_STAGE_VERTEX_BIT:(binding==1?VK_SHADER_STAGE_VERTEX_BIT|VK_SHADER_STAGE_COMPUTE_BIT:VK_SHADER_STAGE_COMPUTE_BIT);}
  VkDescriptorSetLayoutCreateInfo dl{
      VK_STRUCTURE_TYPE_DESCRIPTOR_SET_LAYOUT_CREATE_INFO};
  dl.bindingCount = 6;
  dl.pBindings = binds;
  a.Check(
      vkCreateDescriptorSetLayout(a.device, &dl, nullptr, &a.descriptorLayout),
      "descriptor layout failed");
  VkPipelineLayoutCreateInfo pl{VK_STRUCTURE_TYPE_PIPELINE_LAYOUT_CREATE_INFO};
  pl.setLayoutCount = 1;
  pl.pSetLayouts = &a.descriptorLayout;
  a.Check(vkCreatePipelineLayout(a.device, &pl, nullptr, &a.pipelineLayout),
          "pipeline layout failed");
  VkShaderModule vs{}, fs{};
  try { vs = Shader(a, "shaders/triangle.vert.spv"); fs = Shader(a, "shaders/triangle.frag.spv"); }
  catch (...) { if (vs) vkDestroyShaderModule(a.device, vs, nullptr); throw; }
  VkPipelineShaderStageCreateInfo stages[2]{
      {VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO, nullptr, 0,
       VK_SHADER_STAGE_VERTEX_BIT, vs, "main"},
      {VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO, nullptr, 0,
       VK_SHADER_STAGE_FRAGMENT_BIT, fs, "main"}};
  VkVertexInputBindingDescription vb{0, sizeof(Vertex),
                                     VK_VERTEX_INPUT_RATE_VERTEX};
  VkVertexInputAttributeDescription va[2]{
      {0, 0, VK_FORMAT_R32G32B32_SFLOAT, offsetof(Vertex, position)},
      {1, 0, VK_FORMAT_R32G32B32_SFLOAT, offsetof(Vertex, color)}};
  VkPipelineVertexInputStateCreateInfo vi{
      VK_STRUCTURE_TYPE_PIPELINE_VERTEX_INPUT_STATE_CREATE_INFO};
  vi.vertexBindingDescriptionCount = 1;
  vi.pVertexBindingDescriptions = &vb;
  vi.vertexAttributeDescriptionCount = 2;
  vi.pVertexAttributeDescriptions = va;
  VkPipelineInputAssemblyStateCreateInfo ia{
      VK_STRUCTURE_TYPE_PIPELINE_INPUT_ASSEMBLY_STATE_CREATE_INFO};
  ia.topology = VK_PRIMITIVE_TOPOLOGY_TRIANGLE_LIST;
  VkViewport vp{0, 0, (float)a.extent.width, (float)a.extent.height, 0, 1};
  VkRect2D sc{{0, 0}, a.extent};
  VkPipelineViewportStateCreateInfo vps{
      VK_STRUCTURE_TYPE_PIPELINE_VIEWPORT_STATE_CREATE_INFO};
  vps.viewportCount = 1;
  vps.pViewports = &vp;
  vps.scissorCount = 1;
  vps.pScissors = &sc;
  VkPipelineRasterizationStateCreateInfo rs{
      VK_STRUCTURE_TYPE_PIPELINE_RASTERIZATION_STATE_CREATE_INFO};
  rs.polygonMode = VK_POLYGON_MODE_FILL;
  rs.cullMode = VK_CULL_MODE_NONE;
  rs.frontFace = VK_FRONT_FACE_CLOCKWISE;
  rs.lineWidth = 1;
  VkPipelineMultisampleStateCreateInfo ms{
      VK_STRUCTURE_TYPE_PIPELINE_MULTISAMPLE_STATE_CREATE_INFO};
  ms.rasterizationSamples = VK_SAMPLE_COUNT_1_BIT;
  VkPipelineColorBlendAttachmentState ca{};
  ca.colorWriteMask = 0xf;
  VkPipelineColorBlendStateCreateInfo cb{
      VK_STRUCTURE_TYPE_PIPELINE_COLOR_BLEND_STATE_CREATE_INFO};
  cb.attachmentCount = 1;
  cb.pAttachments = &ca;
  VkGraphicsPipelineCreateInfo gp{
      VK_STRUCTURE_TYPE_GRAPHICS_PIPELINE_CREATE_INFO};
  gp.stageCount = 2;
  gp.pStages = stages;
  gp.pVertexInputState = &vi;
  gp.pInputAssemblyState = &ia;
  gp.pViewportState = &vps;
  gp.pRasterizationState = &rs;
  gp.pMultisampleState = &ms;
  gp.pColorBlendState = &cb;
  gp.layout = a.pipelineLayout;
  gp.renderPass = a.renderPass;
  VkPipeline trianglePipeline{};
  VkResult triangleResult = vkCreateGraphicsPipelines(a.device, {}, 1, &gp, nullptr, &trianglePipeline);
  vkDestroyShaderModule(a.device, vs, nullptr);
  vkDestroyShaderModule(a.device, fs, nullptr);
  if (triangleResult != VK_SUCCESS && trianglePipeline) vkDestroyPipeline(a.device, trianglePipeline, nullptr);
  a.Check(triangleResult, "pipeline failed");
  a.pipeline = trianglePipeline;
  VkShaderModule planetaryVs{},planetaryFs{};
  try{planetaryVs=Shader(a,"shaders/planetary.vert.spv");planetaryFs=Shader(a,"shaders/planetary.frag.spv");}catch(...){if(planetaryVs)vkDestroyShaderModule(a.device,planetaryVs,nullptr);throw;}
  VkPipelineShaderStageCreateInfo planetaryStages[2]{{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_VERTEX_BIT,planetaryVs,"main"},{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_FRAGMENT_BIT,planetaryFs,"main"}};
  VkVertexInputBindingDescription planetaryBinding{0,sizeof(PatchVertex),VK_VERTEX_INPUT_RATE_VERTEX};
  VkVertexInputAttributeDescription planetaryAttribute{0,0,VK_FORMAT_R32G32_SFLOAT,0};
  VkPipelineVertexInputStateCreateInfo planetaryInput{VK_STRUCTURE_TYPE_PIPELINE_VERTEX_INPUT_STATE_CREATE_INFO};planetaryInput.vertexBindingDescriptionCount=1;planetaryInput.pVertexBindingDescriptions=&planetaryBinding;planetaryInput.vertexAttributeDescriptionCount=1;planetaryInput.pVertexAttributeDescriptions=&planetaryAttribute;
  VkPipelineRasterizationStateCreateInfo planetaryRaster=rs;planetaryRaster.cullMode=VK_CULL_MODE_BACK_BIT;planetaryRaster.frontFace=VK_FRONT_FACE_CLOCKWISE;
  VkGraphicsPipelineCreateInfo planetaryCreate=gp;planetaryCreate.pStages=planetaryStages;planetaryCreate.pVertexInputState=&planetaryInput;planetaryCreate.pRasterizationState=&planetaryRaster;
  VkPipeline planetaryPipeline{};VkResult planetaryResult=vkCreateGraphicsPipelines(a.device,{},1,&planetaryCreate,nullptr,&planetaryPipeline);vkDestroyShaderModule(a.device,planetaryVs,nullptr);vkDestroyShaderModule(a.device,planetaryFs,nullptr);if(planetaryResult!=VK_SUCCESS&&planetaryPipeline)vkDestroyPipeline(a.device,planetaryPipeline,nullptr);a.Check(planetaryResult,"planetary pipeline failed");a.planetaryPipeline=planetaryPipeline;
  VkShaderModule planetaryCompute=Shader(a,"shaders/planetary_select.comp.spv");VkPipelineShaderStageCreateInfo planetaryComputeStage{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_COMPUTE_BIT,planetaryCompute,"main"};VkComputePipelineCreateInfo planetaryComputeCreate{VK_STRUCTURE_TYPE_COMPUTE_PIPELINE_CREATE_INFO};planetaryComputeCreate.stage=planetaryComputeStage;planetaryComputeCreate.layout=a.pipelineLayout;VkResult planetaryComputeResult=vkCreateComputePipelines(a.device,{},1,&planetaryComputeCreate,nullptr,&a.planetaryComputePipeline);vkDestroyShaderModule(a.device,planetaryCompute,nullptr);a.Check(planetaryComputeResult,"planetary compute pipeline failed");
  VkShaderModule orbitVs{}, orbitFs{};
  try { orbitVs = Shader(a, "shaders/orbit.vert.spv"); orbitFs = Shader(a, "shaders/orbit.frag.spv"); }
  catch (...) { if (orbitVs) vkDestroyShaderModule(a.device, orbitVs, nullptr); throw; }
  VkPipelineShaderStageCreateInfo orbitStages[2]{{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO, nullptr, 0, VK_SHADER_STAGE_VERTEX_BIT, orbitVs, "main"}, {VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO, nullptr, 0, VK_SHADER_STAGE_FRAGMENT_BIT, orbitFs, "main"}};
  VkVertexInputBindingDescription orbitBinding{0, sizeof(NcOrbitLineVertex), VK_VERTEX_INPUT_RATE_VERTEX};
  VkVertexInputAttributeDescription orbitAttribute{0, 0, VK_FORMAT_R32G32B32_SFLOAT, 0};
  VkPipelineVertexInputStateCreateInfo orbitInput{VK_STRUCTURE_TYPE_PIPELINE_VERTEX_INPUT_STATE_CREATE_INFO}; orbitInput.vertexBindingDescriptionCount = 1; orbitInput.pVertexBindingDescriptions = &orbitBinding; orbitInput.vertexAttributeDescriptionCount = 1; orbitInput.pVertexAttributeDescriptions = &orbitAttribute;
  VkPipelineInputAssemblyStateCreateInfo orbitAssembly{VK_STRUCTURE_TYPE_PIPELINE_INPUT_ASSEMBLY_STATE_CREATE_INFO}; orbitAssembly.topology = VK_PRIMITIVE_TOPOLOGY_LINE_STRIP;
  VkGraphicsPipelineCreateInfo orbitPipeline = gp; orbitPipeline.pStages = orbitStages; orbitPipeline.pVertexInputState = &orbitInput; orbitPipeline.pInputAssemblyState = &orbitAssembly;
  VkPipeline activeOrbitPipeline{};
  VkResult activeOrbitResult = vkCreateGraphicsPipelines(a.device, {}, 1, &orbitPipeline, nullptr, &activeOrbitPipeline);
  vkDestroyShaderModule(a.device, orbitVs, nullptr);
  vkDestroyShaderModule(a.device, orbitFs, nullptr);
  if (activeOrbitResult != VK_SUCCESS && activeOrbitPipeline) vkDestroyPipeline(a.device, activeOrbitPipeline, nullptr);
  a.Check(activeOrbitResult, "orbit pipeline failed");
  a.orbitPipeline = activeOrbitPipeline;
  VkShaderModule previousOrbitVs{}, previousOrbitFs{};
  try { previousOrbitVs = Shader(a, "shaders/orbit.vert.spv"); previousOrbitFs = Shader(a, "shaders/orbit_previous.frag.spv"); }
  catch (...) { if (previousOrbitVs) vkDestroyShaderModule(a.device, previousOrbitVs, nullptr); throw; }
  orbitStages[0].module = previousOrbitVs;
  orbitStages[1].module = previousOrbitFs;
  VkPipeline dimOrbitPipeline{};
  VkResult previousOrbitResult = vkCreateGraphicsPipelines(a.device, {}, 1, &orbitPipeline, nullptr, &dimOrbitPipeline);
  vkDestroyShaderModule(a.device, previousOrbitVs, nullptr);
  vkDestroyShaderModule(a.device, previousOrbitFs, nullptr);
  if (previousOrbitResult != VK_SUCCESS && dimOrbitPipeline) vkDestroyPipeline(a.device, dimOrbitPipeline, nullptr);
  a.Check(previousOrbitResult, "previous orbit pipeline failed");
  a.previousOrbitPipeline = dimOrbitPipeline;
  VkShaderModule bodyForwardVs{}, bodyForwardFs{};
  try { bodyForwardVs = Shader(a, "shaders/orbit.vert.spv"); bodyForwardFs = Shader(a, "shaders/body_forward.frag.spv"); }
  catch (...) { if (bodyForwardVs) vkDestroyShaderModule(a.device, bodyForwardVs, nullptr); throw; }
  orbitStages[0].module = bodyForwardVs;
  orbitStages[1].module = bodyForwardFs;
  VkPipeline bodyForwardPipeline{};
  VkResult bodyForwardResult = vkCreateGraphicsPipelines(a.device, {}, 1, &orbitPipeline, nullptr, &bodyForwardPipeline);
  vkDestroyShaderModule(a.device, bodyForwardVs, nullptr);
  vkDestroyShaderModule(a.device, bodyForwardFs, nullptr);
  if (bodyForwardResult != VK_SUCCESS && bodyForwardPipeline) vkDestroyPipeline(a.device, bodyForwardPipeline, nullptr);
  a.Check(bodyForwardResult, "body-forward pipeline failed");
  a.bodyForwardPipeline = bodyForwardPipeline;
  VkShaderModule targetDirectionVs{}, targetDirectionFs{};
  try { targetDirectionVs = Shader(a, "shaders/orbit.vert.spv"); targetDirectionFs = Shader(a, "shaders/sas_target.frag.spv"); }
  catch (...) { if (targetDirectionVs) vkDestroyShaderModule(a.device, targetDirectionVs, nullptr); throw; }
  orbitStages[0].module = targetDirectionVs;
  orbitStages[1].module = targetDirectionFs;
  VkPipeline targetDirectionPipeline{};
  VkResult targetDirectionResult = vkCreateGraphicsPipelines(a.device, {}, 1, &orbitPipeline, nullptr, &targetDirectionPipeline);
  vkDestroyShaderModule(a.device, targetDirectionVs, nullptr);
  vkDestroyShaderModule(a.device, targetDirectionFs, nullptr);
  if (targetDirectionResult != VK_SUCCESS && targetDirectionPipeline) vkDestroyPipeline(a.device, targetDirectionPipeline, nullptr);
  a.Check(targetDirectionResult, "SAS target pipeline failed");
  a.targetDirectionPipeline = targetDirectionPipeline;
  a.framebuffers.resize(a.views.size());
  for (size_t i = 0; i < a.views.size(); i++) {
    VkImageView x = a.views[i];
    VkFramebufferCreateInfo fb{VK_STRUCTURE_TYPE_FRAMEBUFFER_CREATE_INFO};
    fb.renderPass = a.renderPass;
    fb.attachmentCount = 1;
    fb.pAttachments = &x;
    fb.width = a.extent.width;
    fb.height = a.extent.height;
    fb.layers = 1;
    a.Check(vkCreateFramebuffer(a.device, &fb, nullptr, &a.framebuffers[i]),
            "framebuffer failed");
  }
}
void DestroyPatchBuffer(App &a) {
  if(a.patchMapped)vkUnmapMemory(a.device,a.patchMemory);
  if(a.patchBuffer)vkDestroyBuffer(a.device,a.patchBuffer,nullptr);
  if(a.patchMemory)vkFreeMemory(a.device,a.patchMemory,nullptr);
  a.patchMapped=nullptr;a.patchBuffer={};a.patchMemory={};a.patchSize=0;
}
void CreatePatchBuffer(App &a,VkDeviceSize size) {
  a.patchSize=size;
  VkBufferCreateInfo pci{VK_STRUCTURE_TYPE_BUFFER_CREATE_INFO};pci.size=a.patchSize;pci.usage=VK_BUFFER_USAGE_STORAGE_BUFFER_BIT;pci.sharingMode=VK_SHARING_MODE_EXCLUSIVE;
  a.Check(vkCreateBuffer(a.device,&pci,nullptr,&a.patchBuffer),"patch buffer failed");VkMemoryRequirements pr;vkGetBufferMemoryRequirements(a.device,a.patchBuffer,&pr);VkMemoryAllocateInfo pai{VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO};pai.allocationSize=pr.size;pai.memoryTypeIndex=Memory(a,pr.memoryTypeBits,VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT|VK_MEMORY_PROPERTY_HOST_COHERENT_BIT);a.Check(vkAllocateMemory(a.device,&pai,nullptr,&a.patchMemory),"patch memory failed");a.Check(vkBindBufferMemory(a.device,a.patchBuffer,a.patchMemory,0),"patch bind failed");a.Check(vkMapMemory(a.device,a.patchMemory,0,a.patchSize,0,&a.patchMapped),"patch map failed");
}
void CreateHostBuffer(App &a,VkDeviceSize size,VkBufferUsageFlags usage,VkBuffer &buffer,VkDeviceMemory &memory,void *&mapped,const char *failure) {
  VkBufferCreateInfo ci{VK_STRUCTURE_TYPE_BUFFER_CREATE_INFO};ci.size=size;ci.usage=usage;ci.sharingMode=VK_SHARING_MODE_EXCLUSIVE;a.Check(vkCreateBuffer(a.device,&ci,nullptr,&buffer),failure);VkMemoryRequirements requirements;vkGetBufferMemoryRequirements(a.device,buffer,&requirements);VkMemoryAllocateInfo allocation{VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO};allocation.allocationSize=requirements.size;allocation.memoryTypeIndex=Memory(a,requirements.memoryTypeBits,VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT|VK_MEMORY_PROPERTY_HOST_COHERENT_BIT);a.Check(vkAllocateMemory(a.device,&allocation,nullptr,&memory),failure);a.Check(vkBindBufferMemory(a.device,buffer,memory,0),failure);a.Check(vkMapMemory(a.device,memory,0,size,0,&mapped),failure);std::memset(mapped,0,(size_t)size);
}
void DestroyHostBuffer(App &a,VkBuffer &buffer,VkDeviceMemory &memory,void *&mapped) {
  if(mapped)vkUnmapMemory(a.device,memory);if(buffer)vkDestroyBuffer(a.device,buffer,nullptr);if(memory)vkFreeMemory(a.device,memory,nullptr);mapped=nullptr;buffer={};memory={};
}
void DestroySubmission(App &a) {
  if (a.mapped)
    vkUnmapMemory(a.device, a.submissionMemory);
  if (a.descriptorPool)
    vkDestroyDescriptorPool(a.device, a.descriptorPool, nullptr);
  if (a.submissionBuffer)
    vkDestroyBuffer(a.device, a.submissionBuffer, nullptr);
  if (a.submissionMemory)
    vkFreeMemory(a.device, a.submissionMemory, nullptr);
  a.mapped = nullptr;
  a.descriptorPool = {};
  a.submissionBuffer = {};
  a.submissionMemory = {};
  DestroyPatchBuffer(a);
  DestroyHostBuffer(a,a.gpuInputBuffer,a.gpuInputMemory,a.gpuInputMapped);
  DestroyHostBuffer(a,a.gpuWorkBuffer,a.gpuWorkMemory,a.gpuWorkMapped);
  DestroyHostBuffer(a,a.gpuNodeBuffer,a.gpuNodeMemory,a.gpuNodeMapped);
  DestroyHostBuffer(a,a.gpuControlBuffer,a.gpuControlMemory,a.gpuControlMapped);
  a.validationCpuOracle.clear();a.gpuFrameSubmitted=false;a.hasGpuTelemetry=false;a.hasParityResult=false;
  if (a.orbitMapped) vkUnmapMemory(a.device, a.orbitMemory);
  if (a.orbitBuffer) vkDestroyBuffer(a.device, a.orbitBuffer, nullptr);
  if (a.orbitMemory) vkFreeMemory(a.device, a.orbitMemory, nullptr);
  a.orbitMapped = nullptr; a.orbitBuffer = {}; a.orbitMemory = {}; a.orbitSize = 0;
  if (a.previousOrbitMapped) vkUnmapMemory(a.device, a.previousOrbitMemory);
  if (a.previousOrbitBuffer) vkDestroyBuffer(a.device, a.previousOrbitBuffer, nullptr);
  if (a.previousOrbitMemory) vkFreeMemory(a.device, a.previousOrbitMemory, nullptr);
  a.previousOrbitMapped = nullptr; a.previousOrbitBuffer = {}; a.previousOrbitMemory = {}; a.previousOrbitSize = 0;
  if (a.bodyForwardMapped) vkUnmapMemory(a.device, a.bodyForwardMemory);
  if (a.bodyForwardBuffer) vkDestroyBuffer(a.device, a.bodyForwardBuffer, nullptr);
  if (a.bodyForwardMemory) vkFreeMemory(a.device, a.bodyForwardMemory, nullptr);
  a.bodyForwardMapped = nullptr; a.bodyForwardBuffer = {}; a.bodyForwardMemory = {}; a.bodyForwardSize = 0;
  if (a.targetDirectionMapped) vkUnmapMemory(a.device, a.targetDirectionMemory);
  if (a.targetDirectionBuffer) vkDestroyBuffer(a.device, a.targetDirectionBuffer, nullptr);
  if (a.targetDirectionMemory) vkFreeMemory(a.device, a.targetDirectionMemory, nullptr);
  a.targetDirectionMapped = nullptr; a.targetDirectionBuffer = {}; a.targetDirectionMemory = {}; a.targetDirectionSize = 0;
}
void Submission(App &a) {
  a.submissionSize = sizeof(NcCameraData) +
                     sizeof(NcRenderObject) * a.submission->objectCount;
  Buffer(a, a.submissionSize, VK_BUFFER_USAGE_STORAGE_BUFFER_BIT,
         a.submissionBuffer, a.submissionMemory,
         nullptr); /* Buffer copied null; remap after replacing zero-initialized
                      source is not permitted */
}
void CreateSubmission(App &a) {
  a.submissionSize = sizeof(NcCameraData) +
                     sizeof(NcRenderObject) * a.submission->objectCount;
  VkBufferCreateInfo ci{VK_STRUCTURE_TYPE_BUFFER_CREATE_INFO};
  ci.size = a.submissionSize;
  ci.usage = VK_BUFFER_USAGE_STORAGE_BUFFER_BIT;
  ci.sharingMode = VK_SHARING_MODE_EXCLUSIVE;
  a.Check(vkCreateBuffer(a.device, &ci, nullptr, &a.submissionBuffer),
          "submission buffer failed");
  VkMemoryRequirements r;
  vkGetBufferMemoryRequirements(a.device, a.submissionBuffer, &r);
  VkMemoryAllocateInfo ai{VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO};
  ai.allocationSize = r.size;
  ai.memoryTypeIndex = Memory(a, r.memoryTypeBits,
                              VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT |
                                  VK_MEMORY_PROPERTY_HOST_COHERENT_BIT);
  a.Check(vkAllocateMemory(a.device, &ai, nullptr, &a.submissionMemory),
          "submission memory failed");
  a.Check(
      vkBindBufferMemory(a.device, a.submissionBuffer, a.submissionMemory, 0),
      "submission bind failed");
  a.Check(vkMapMemory(a.device, a.submissionMemory, 0, a.submissionSize, 0,
                      &a.mapped),
          "submission map failed");
  CreatePatchBuffer(a,sizeof(NcPlanetaryPatch)*GpuPatchCapacity);
  CreateHostBuffer(a,sizeof(NcPlanetaryGpuConstants),VK_BUFFER_USAGE_STORAGE_BUFFER_BIT,a.gpuInputBuffer,a.gpuInputMemory,a.gpuInputMapped,"planetary GPU input buffer failed");
  CreateHostBuffer(a,sizeof(uint32_t)*4*GpuPatchCapacity*2,VK_BUFFER_USAGE_STORAGE_BUFFER_BIT,a.gpuWorkBuffer,a.gpuWorkMemory,a.gpuWorkMapped,"planetary GPU work buffer failed");
  CreateHostBuffer(a,sizeof(uint32_t)*GpuNodeCount,VK_BUFFER_USAGE_STORAGE_BUFFER_BIT,a.gpuNodeBuffer,a.gpuNodeMemory,a.gpuNodeMapped,"planetary GPU node buffer failed");
  CreateHostBuffer(a,sizeof(GpuPlanetaryControl),VK_BUFFER_USAGE_STORAGE_BUFFER_BIT|VK_BUFFER_USAGE_INDIRECT_BUFFER_BIT,a.gpuControlBuffer,a.gpuControlMemory,a.gpuControlMapped,"planetary GPU control buffer failed");
  VkDescriptorPoolSize ps{VK_DESCRIPTOR_TYPE_STORAGE_BUFFER, 6};
  VkDescriptorPoolCreateInfo pi{VK_STRUCTURE_TYPE_DESCRIPTOR_POOL_CREATE_INFO};
  pi.maxSets = 1;
  pi.poolSizeCount = 1;
  pi.pPoolSizes = &ps;
  a.Check(vkCreateDescriptorPool(a.device, &pi, nullptr, &a.descriptorPool),
          "descriptor pool failed");
  VkDescriptorSetAllocateInfo si{
      VK_STRUCTURE_TYPE_DESCRIPTOR_SET_ALLOCATE_INFO};
  si.descriptorPool = a.descriptorPool;
  si.descriptorSetCount = 1;
  si.pSetLayouts = &a.descriptorLayout;
  a.Check(vkAllocateDescriptorSets(a.device, &si, &a.descriptor),
          "descriptor set failed");
  VkDescriptorBufferInfo infos[6]{{a.submissionBuffer,0,a.submissionSize},{a.patchBuffer,0,a.patchSize},{a.gpuInputBuffer,0,sizeof(NcPlanetaryGpuConstants)},{a.gpuWorkBuffer,0,sizeof(uint32_t)*4*GpuPatchCapacity*2},{a.gpuNodeBuffer,0,sizeof(uint32_t)*GpuNodeCount},{a.gpuControlBuffer,0,sizeof(GpuPlanetaryControl)}};
  VkWriteDescriptorSet writes[6]{};for(uint32_t binding=0;binding<6;binding++){writes[binding].sType=VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET;writes[binding].dstSet=a.descriptor;writes[binding].dstBinding=binding;writes[binding].descriptorCount=1;writes[binding].descriptorType=VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;writes[binding].pBufferInfo=&infos[binding];}
  vkUpdateDescriptorSets(a.device,6,writes,0,nullptr);
}
void EnsurePatchCapacity(App &a,uint32_t count) {
  const auto required=sizeof(NcPlanetaryPatch)*std::max<uint32_t>(1,count);
  if(required<=a.patchSize)return;
  vkDeviceWaitIdle(a.device);
  DestroyPatchBuffer(a);
  CreatePatchBuffer(a,required);
  VkDescriptorBufferInfo info{a.patchBuffer,0,a.patchSize};
  VkWriteDescriptorSet write{VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET};write.dstSet=a.descriptor;write.dstBinding=1;write.descriptorCount=1;write.descriptorType=VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;write.pBufferInfo=&info;
  vkUpdateDescriptorSets(a.device,1,&write,0,nullptr);
}
void Upload(App &a) {
  std::memcpy(a.mapped, &a.submission->camera, sizeof(NcCameraData));
  std::memcpy((char *)a.mapped + sizeof(NcCameraData),
              a.submission->objects,
              sizeof(NcRenderObject) * a.submission->objectCount);
  std::memcpy(a.gpuInputMapped,&a.submission->planetaryGpu,sizeof(NcPlanetaryGpuConstants));
  if(a.submission->planetaryMode==NC_PLANETARY_CPU_REFERENCE&&a.submission->planetaryPatchCount)std::memcpy(a.patchMapped,a.submission->planetaryPatches,sizeof(NcPlanetaryPatch)*a.submission->planetaryPatchCount);
  if (a.submission->orbitVertexCount) {
    auto needed = sizeof(NcOrbitLineVertex) * a.submission->orbitVertexCount;
    if (needed != a.orbitSize) {
      if (a.orbitMapped) vkUnmapMemory(a.device, a.orbitMemory);
      if (a.orbitBuffer) vkDestroyBuffer(a.device, a.orbitBuffer, nullptr);
      if (a.orbitMemory) vkFreeMemory(a.device, a.orbitMemory, nullptr);
      a.orbitMapped = nullptr; a.orbitBuffer = {}; a.orbitMemory = {}; a.orbitSize = needed;
      VkBufferCreateInfo ci{VK_STRUCTURE_TYPE_BUFFER_CREATE_INFO}; ci.size = needed; ci.usage = VK_BUFFER_USAGE_VERTEX_BUFFER_BIT; ci.sharingMode = VK_SHARING_MODE_EXCLUSIVE;
      a.Check(vkCreateBuffer(a.device, &ci, nullptr, &a.orbitBuffer), "orbit buffer failed"); VkMemoryRequirements r; vkGetBufferMemoryRequirements(a.device, a.orbitBuffer, &r);
      VkMemoryAllocateInfo ai{VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO}; ai.allocationSize = r.size; ai.memoryTypeIndex = Memory(a, r.memoryTypeBits, VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT | VK_MEMORY_PROPERTY_HOST_COHERENT_BIT);
      a.Check(vkAllocateMemory(a.device, &ai, nullptr, &a.orbitMemory), "orbit memory failed"); a.Check(vkBindBufferMemory(a.device, a.orbitBuffer, a.orbitMemory, 0), "orbit bind failed"); a.Check(vkMapMemory(a.device, a.orbitMemory, 0, needed, 0, &a.orbitMapped), "orbit map failed");
    }
    std::memcpy(a.orbitMapped, a.submission->orbitVertices, needed);
  }
  if (a.submission->previousOrbitVertexCount) {
    auto needed = sizeof(NcOrbitLineVertex) * a.submission->previousOrbitVertexCount;
    if (needed != a.previousOrbitSize) {
      if (a.previousOrbitMapped) vkUnmapMemory(a.device, a.previousOrbitMemory); if (a.previousOrbitBuffer) vkDestroyBuffer(a.device, a.previousOrbitBuffer, nullptr); if (a.previousOrbitMemory) vkFreeMemory(a.device, a.previousOrbitMemory, nullptr);
      a.previousOrbitMapped = nullptr; a.previousOrbitBuffer = {}; a.previousOrbitMemory = {}; a.previousOrbitSize = needed;
      VkBufferCreateInfo ci{VK_STRUCTURE_TYPE_BUFFER_CREATE_INFO}; ci.size = needed; ci.usage = VK_BUFFER_USAGE_VERTEX_BUFFER_BIT; ci.sharingMode = VK_SHARING_MODE_EXCLUSIVE; a.Check(vkCreateBuffer(a.device, &ci, nullptr, &a.previousOrbitBuffer), "previous orbit buffer failed"); VkMemoryRequirements r; vkGetBufferMemoryRequirements(a.device, a.previousOrbitBuffer, &r);
      VkMemoryAllocateInfo ai{VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO}; ai.allocationSize = r.size; ai.memoryTypeIndex = Memory(a, r.memoryTypeBits, VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT | VK_MEMORY_PROPERTY_HOST_COHERENT_BIT); a.Check(vkAllocateMemory(a.device, &ai, nullptr, &a.previousOrbitMemory), "previous orbit memory failed"); a.Check(vkBindBufferMemory(a.device, a.previousOrbitBuffer, a.previousOrbitMemory, 0), "previous orbit bind failed"); a.Check(vkMapMemory(a.device, a.previousOrbitMemory, 0, needed, 0, &a.previousOrbitMapped), "previous orbit map failed");
    }
    std::memcpy(a.previousOrbitMapped, a.submission->previousOrbitVertices, needed);
  }
  if (a.submission->bodyForwardVertexCount) {
    auto needed = sizeof(NcOrbitLineVertex) * a.submission->bodyForwardVertexCount;
    if (needed != a.bodyForwardSize) {
      if (a.bodyForwardMapped) vkUnmapMemory(a.device, a.bodyForwardMemory); if (a.bodyForwardBuffer) vkDestroyBuffer(a.device, a.bodyForwardBuffer, nullptr); if (a.bodyForwardMemory) vkFreeMemory(a.device, a.bodyForwardMemory, nullptr);
      a.bodyForwardMapped = nullptr; a.bodyForwardBuffer = {}; a.bodyForwardMemory = {}; a.bodyForwardSize = needed;
      VkBufferCreateInfo ci{VK_STRUCTURE_TYPE_BUFFER_CREATE_INFO}; ci.size = needed; ci.usage = VK_BUFFER_USAGE_VERTEX_BUFFER_BIT; ci.sharingMode = VK_SHARING_MODE_EXCLUSIVE; a.Check(vkCreateBuffer(a.device, &ci, nullptr, &a.bodyForwardBuffer), "body-forward buffer failed"); VkMemoryRequirements r; vkGetBufferMemoryRequirements(a.device, a.bodyForwardBuffer, &r);
      VkMemoryAllocateInfo ai{VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO}; ai.allocationSize = r.size; ai.memoryTypeIndex = Memory(a, r.memoryTypeBits, VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT | VK_MEMORY_PROPERTY_HOST_COHERENT_BIT); a.Check(vkAllocateMemory(a.device, &ai, nullptr, &a.bodyForwardMemory), "body-forward memory failed"); a.Check(vkBindBufferMemory(a.device, a.bodyForwardBuffer, a.bodyForwardMemory, 0), "body-forward bind failed"); a.Check(vkMapMemory(a.device, a.bodyForwardMemory, 0, needed, 0, &a.bodyForwardMapped), "body-forward map failed");
    }
    std::memcpy(a.bodyForwardMapped, a.submission->bodyForwardVertices, needed);
  }
  if (a.submission->targetDirectionVertexCount) {
    auto needed = sizeof(NcOrbitLineVertex) * a.submission->targetDirectionVertexCount;
    if (needed != a.targetDirectionSize) {
      if (a.targetDirectionMapped) vkUnmapMemory(a.device, a.targetDirectionMemory); if (a.targetDirectionBuffer) vkDestroyBuffer(a.device, a.targetDirectionBuffer, nullptr); if (a.targetDirectionMemory) vkFreeMemory(a.device, a.targetDirectionMemory, nullptr);
      a.targetDirectionMapped = nullptr; a.targetDirectionBuffer = {}; a.targetDirectionMemory = {}; a.targetDirectionSize = needed;
      VkBufferCreateInfo ci{VK_STRUCTURE_TYPE_BUFFER_CREATE_INFO}; ci.size = needed; ci.usage = VK_BUFFER_USAGE_VERTEX_BUFFER_BIT; ci.sharingMode = VK_SHARING_MODE_EXCLUSIVE; a.Check(vkCreateBuffer(a.device, &ci, nullptr, &a.targetDirectionBuffer), "SAS target buffer failed"); VkMemoryRequirements r; vkGetBufferMemoryRequirements(a.device, a.targetDirectionBuffer, &r);
      VkMemoryAllocateInfo ai{VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO}; ai.allocationSize = r.size; ai.memoryTypeIndex = Memory(a, r.memoryTypeBits, VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT | VK_MEMORY_PROPERTY_HOST_COHERENT_BIT); a.Check(vkAllocateMemory(a.device, &ai, nullptr, &a.targetDirectionMemory), "SAS target memory failed"); a.Check(vkBindBufferMemory(a.device, a.targetDirectionBuffer, a.targetDirectionMemory, 0), "SAS target bind failed"); a.Check(vkMapMemory(a.device, a.targetDirectionMemory, 0, needed, 0, &a.targetDirectionMapped), "SAS target map failed");
    }
    std::memcpy(a.targetDirectionMapped, a.submission->targetDirectionVertices, needed);
  }
}
void Commands(App &a) {
  auto q = FindQueues(a.physical, a.surface);
  VkCommandPoolCreateInfo ci{VK_STRUCTURE_TYPE_COMMAND_POOL_CREATE_INFO};
  ci.flags = VK_COMMAND_POOL_CREATE_RESET_COMMAND_BUFFER_BIT;
  ci.queueFamilyIndex = *q.graphics;
  a.Check(vkCreateCommandPool(a.device, &ci, nullptr, &a.pool),
          "command pool failed");
  a.commands.resize(a.framebuffers.size());
  VkCommandBufferAllocateInfo ai{
      VK_STRUCTURE_TYPE_COMMAND_BUFFER_ALLOCATE_INFO};
  ai.commandPool = a.pool;
  ai.level = VK_COMMAND_BUFFER_LEVEL_PRIMARY;
  ai.commandBufferCount = (uint32_t)a.commands.size();
  a.Check(vkAllocateCommandBuffers(a.device, &ai, a.commands.data()),
          "command allocation failed");
  VkSemaphoreCreateInfo si{VK_STRUCTURE_TYPE_SEMAPHORE_CREATE_INFO};
  a.Check(vkCreateSemaphore(a.device, &si, nullptr, &a.imageAvailable),
          "acquire semaphore failed");
  a.renderFinished.resize(a.framebuffers.size());
  for (auto &s : a.renderFinished)
    a.Check(vkCreateSemaphore(a.device, &si, nullptr, &s),
            "present semaphore failed");
  VkFenceCreateInfo fi{VK_STRUCTURE_TYPE_FENCE_CREATE_INFO};
  fi.flags = VK_FENCE_CREATE_SIGNALED_BIT;
  a.Check(vkCreateFence(a.device, &fi, nullptr, &a.fence), "fence failed");
}
void Record(App &a, uint32_t image) {
  auto c = a.commands[image];
  vkResetCommandBuffer(c, 0);
  VkCommandBufferBeginInfo bi{VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO};
  a.Check(vkBeginCommandBuffer(c, &bi), "command begin failed");
  const bool gpuPlanetary=a.submission->planetaryMode!=NC_PLANETARY_CPU_REFERENCE;
  if(gpuPlanetary){VkMemoryBarrier hostBarrier{VK_STRUCTURE_TYPE_MEMORY_BARRIER};hostBarrier.srcAccessMask=VK_ACCESS_HOST_WRITE_BIT;hostBarrier.dstAccessMask=VK_ACCESS_SHADER_READ_BIT;vkCmdPipelineBarrier(c,VK_PIPELINE_STAGE_HOST_BIT,VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,0,1,&hostBarrier,0,nullptr,0,nullptr);vkCmdBindPipeline(c,VK_PIPELINE_BIND_POINT_COMPUTE,a.planetaryComputePipeline);vkCmdBindDescriptorSets(c,VK_PIPELINE_BIND_POINT_COMPUTE,a.pipelineLayout,0,1,&a.descriptor,0,nullptr);vkCmdDispatch(c,1,1,1);VkMemoryBarrier computeBarrier{VK_STRUCTURE_TYPE_MEMORY_BARRIER};computeBarrier.srcAccessMask=VK_ACCESS_SHADER_WRITE_BIT;computeBarrier.dstAccessMask=VK_ACCESS_INDIRECT_COMMAND_READ_BIT|VK_ACCESS_SHADER_READ_BIT;VkPipelineStageFlags consumers=VK_PIPELINE_STAGE_DRAW_INDIRECT_BIT|VK_PIPELINE_STAGE_VERTEX_SHADER_BIT;if(a.submission->planetaryMode==NC_PLANETARY_CPU_GPU_VALIDATION){computeBarrier.dstAccessMask|=VK_ACCESS_HOST_READ_BIT;consumers|=VK_PIPELINE_STAGE_HOST_BIT;}vkCmdPipelineBarrier(c,VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,consumers,0,1,&computeBarrier,0,nullptr,0,nullptr);}
  VkClearValue clear{{{.02f, .02f, .04f, 1}}};
  VkRenderPassBeginInfo rp{VK_STRUCTURE_TYPE_RENDER_PASS_BEGIN_INFO};
  rp.renderPass = a.renderPass;
  rp.framebuffer = a.framebuffers[image];
  rp.renderArea = {{0, 0}, a.extent};
  rp.clearValueCount = 1;
  rp.pClearValues = &clear;
  vkCmdBeginRenderPass(c, &rp, VK_SUBPASS_CONTENTS_INLINE);
  vkCmdBindPipeline(c, VK_PIPELINE_BIND_POINT_GRAPHICS, a.pipeline);
  vkCmdBindDescriptorSets(c, VK_PIPELINE_BIND_POINT_GRAPHICS, a.pipelineLayout,
                          0, 1, &a.descriptor, 0, nullptr);
  for (uint32_t i = 0; i < a.submission->batchCount; i++) {
    auto &b = a.submission->batches[i];
    auto *m = MeshFor(a, b.mesh);
    VkDeviceSize offset = 0;
    vkCmdBindVertexBuffers(c, 0, 1, &m->vb, &offset);
    vkCmdBindIndexBuffer(c, m->ib, 0, VK_INDEX_TYPE_UINT32);
    vkCmdDrawIndexed(c, m->indices, b.objectCount, 0, 0, b.firstObject);
  }
  if(a.submission->planetaryPatchCount||gpuPlanetary){VkDeviceSize offset=0;vkCmdBindPipeline(c,VK_PIPELINE_BIND_POINT_GRAPHICS,a.planetaryPipeline);vkCmdBindVertexBuffers(c,0,1,&a.planetaryPatch.vb,&offset);vkCmdBindIndexBuffer(c,a.planetaryPatch.ib,0,VK_INDEX_TYPE_UINT32);if(gpuPlanetary)vkCmdDrawIndexedIndirect(c,a.gpuControlBuffer,0,1,sizeof(VkDrawIndexedIndirectCommand));else vkCmdDrawIndexed(c,a.planetaryPatch.indices,a.submission->planetaryPatchCount,0,0,0);}
  if (a.submission->orbitVertexCount >= 2 && a.orbitBuffer) { VkDeviceSize offset = 0; vkCmdBindPipeline(c, VK_PIPELINE_BIND_POINT_GRAPHICS, a.orbitPipeline); vkCmdBindVertexBuffers(c, 0, 1, &a.orbitBuffer, &offset); vkCmdDraw(c, a.submission->orbitVertexCount, 1, 0, 0); }
  if (a.submission->previousOrbitVertexCount >= 2 && a.previousOrbitBuffer) { VkDeviceSize offset = 0; vkCmdBindPipeline(c, VK_PIPELINE_BIND_POINT_GRAPHICS, a.previousOrbitPipeline); vkCmdBindVertexBuffers(c, 0, 1, &a.previousOrbitBuffer, &offset); vkCmdDraw(c, a.submission->previousOrbitVertexCount, 1, 0, 0); }
  if (a.submission->bodyForwardVertexCount == 2 && a.bodyForwardBuffer) { VkDeviceSize offset = 0; vkCmdBindPipeline(c, VK_PIPELINE_BIND_POINT_GRAPHICS, a.bodyForwardPipeline); vkCmdBindVertexBuffers(c, 0, 1, &a.bodyForwardBuffer, &offset); vkCmdDraw(c, 2, 1, 0, 0); }
  if (a.submission->targetDirectionVertexCount == 2 && a.targetDirectionBuffer) { VkDeviceSize offset = 0; vkCmdBindPipeline(c, VK_PIPELINE_BIND_POINT_GRAPHICS, a.targetDirectionPipeline); vkCmdBindVertexBuffers(c, 0, 1, &a.targetDirectionBuffer, &offset); vkCmdDraw(c, 2, 1, 0, 0); }
  vkCmdEndRenderPass(c);
  a.Check(vkEndCommandBuffer(c), "command end failed");
}
void Recreate(App &a) {
  int w = 0, h = 0;
  while (!w || !h) {
    RECT r;
    GetClientRect(a.window, &r);
    w = r.right;
    h = r.bottom;
    MSG m;
    while (PeekMessage(&m, nullptr, 0, 0, PM_REMOVE)) {
      TranslateMessage(&m);
      DispatchMessage(&m);
    }
  }
  vkDeviceWaitIdle(a.device);
  for (auto s : a.renderFinished)
    vkDestroySemaphore(a.device, s, nullptr);
  a.renderFinished.clear();
  DestroySubmission(a);
  if (!a.commands.empty())
    vkFreeCommandBuffers(a.device, a.pool, (uint32_t)a.commands.size(),
                         a.commands.data());
  a.commands.clear();
  DestroySwap(a);
  Swap(a);
  CreateSubmission(a);
  VkCommandBufferAllocateInfo ai{
      VK_STRUCTURE_TYPE_COMMAND_BUFFER_ALLOCATE_INFO};
  ai.commandPool = a.pool;
  ai.level = VK_COMMAND_BUFFER_LEVEL_PRIMARY;
  ai.commandBufferCount = (uint32_t)a.framebuffers.size();
  a.commands.resize(a.framebuffers.size());
  a.Check(vkAllocateCommandBuffers(a.device, &ai, a.commands.data()),
          "resize command allocation failed");
  VkSemaphoreCreateInfo si{VK_STRUCTURE_TYPE_SEMAPHORE_CREATE_INFO};
  a.renderFinished.resize(a.framebuffers.size());
  for (auto &s : a.renderFinished)
    a.Check(vkCreateSemaphore(a.device, &si, nullptr, &s),
            "resize semaphore failed");
  a.resized = false;
  a.Log(NC_LOG_ALWAYS, "Swapchain recreated after resize");
}
std::vector<PatchIdentity> CanonicalPatches(const NcPlanetaryPatch *patches,uint32_t count) {
  std::vector<PatchIdentity> result;result.reserve(count);for(uint32_t index=0;index<count;index++){const auto &patch=patches[index];result.push_back({patch.face,patch.level,patch.x,patch.y,patch.stitchMask});}std::sort(result.begin(),result.end());return result;
}
uint64_t PatchHash(const std::vector<PatchIdentity> &patches) {
  uint64_t hash=14695981039346656037ull;auto mix=[&](uint32_t value){hash=(hash^value)*1099511628211ull;};for(const auto &patch:patches){mix(patch.face);mix(patch.level);mix(patch.x);mix(patch.y);mix(patch.stitchMask);}return hash;
}
void InspectGpuPlanetary(App &a) {
  if(!a.gpuFrameSubmitted||!a.gpuControlMapped)return;GpuPlanetaryControl telemetry;std::memcpy(&telemetry,a.gpuControlMapped,sizeof(telemetry));
  if(!a.hasGpuTelemetry||std::memcmp(&telemetry,&a.lastGpuTelemetry,sizeof(telemetry))!=0){char message[256];std::snprintf(message,sizeof message,"GPU planetary: roots=%u; candidates=%u; refined=%u; culled=%u; active=%u; balanced=%u; min=%u; max=%u; overflow=%u; indirectInstances=%u",telemetry.roots,telemetry.candidates,telemetry.refined,telemetry.culled,telemetry.active,telemetry.balanced,telemetry.minimumLevel,telemetry.maximumLevel,telemetry.overflow,telemetry.draw.instanceCount);a.Log(NC_LOG_ALWAYS,message);a.lastGpuTelemetry=telemetry;a.hasGpuTelemetry=true;}
  if(a.submission->planetaryMode!=NC_PLANETARY_CPU_GPU_VALIDATION||a.validationCpuOracle.empty())return;
  const auto gpuCount=std::min<uint32_t>(telemetry.active,GpuPatchCapacity);auto cpu=CanonicalPatches(a.validationCpuOracle.data(),(uint32_t)a.validationCpuOracle.size());auto gpu=CanonicalPatches(static_cast<const NcPlanetaryPatch *>(a.patchMapped),gpuCount);const auto cpuHash=PatchHash(cpu),gpuHash=PatchHash(gpu);uint32_t cpuMinimum=0,cpuMaximum=0;if(!cpu.empty()){cpuMinimum=cpuMaximum=cpu.front().level;for(const auto &patch:cpu){cpuMinimum=std::min(cpuMinimum,patch.level);cpuMaximum=std::max(cpuMaximum,patch.level);}}const bool match=telemetry.roots==6&&telemetry.overflow==0&&telemetry.draw.instanceCount==telemetry.active&&telemetry.minimumLevel==cpuMinimum&&telemetry.maximumLevel==cpuMaximum&&cpu==gpu;
  if(!a.hasParityResult||!match||cpuHash!=a.lastCpuHash||gpuHash!=a.lastGpuHash){char message[256];std::snprintf(message,sizeof message,"CPU/GPU planetary parity: match=%s; cpu=%zu; gpu=%zu; cpuHash=0x%016llX; gpuHash=0x%016llX",match?"true":"false",cpu.size(),gpu.size(),(unsigned long long)cpuHash,(unsigned long long)gpuHash);a.Log(match?NC_LOG_ALWAYS:NC_LOG_VALIDATION,message);a.lastCpuHash=cpuHash;a.lastGpuHash=gpuHash;a.hasParityResult=true;}
}
void Draw(App &a) {
  uint32_t image{};
  VkResult ar = vkAcquireNextImageKHR(a.device, a.swapchain, UINT64_MAX,
                                      a.imageAvailable, {}, &image);
  if (ar == VK_ERROR_OUT_OF_DATE_KHR) {
    a.Log(NC_LOG_ALWAYS, "Acquire out of date; recreating swapchain");
    Recreate(a);
    return;
  }
  if (ar != VK_SUCCESS && ar != VK_SUBOPTIMAL_KHR)
    throw std::runtime_error("acquire image failed");
  bool recreate = ar == VK_SUBOPTIMAL_KHR || a.resized;
  vkResetFences(a.device, 1, &a.fence);
  Record(a, image);
  if(a.submission->planetaryMode==NC_PLANETARY_CPU_GPU_VALIDATION)a.validationCpuOracle.assign(a.submission->planetaryPatches,a.submission->planetaryPatches+a.submission->planetaryPatchCount);else a.validationCpuOracle.clear();a.gpuFrameSubmitted=a.submission->planetaryMode!=NC_PLANETARY_CPU_REFERENCE;
  VkPipelineStageFlags stage = VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT;
  VkSubmitInfo si{VK_STRUCTURE_TYPE_SUBMIT_INFO};
  si.waitSemaphoreCount = 1;
  si.pWaitSemaphores = &a.imageAvailable;
  si.pWaitDstStageMask = &stage;
  si.commandBufferCount = 1;
  si.pCommandBuffers = &a.commands[image];
  si.signalSemaphoreCount = 1;
  si.pSignalSemaphores = &a.renderFinished[image];
  a.Check(vkQueueSubmit(a.graphicsQueue, 1, &si, a.fence), "submit failed");
  VkPresentInfoKHR pi{VK_STRUCTURE_TYPE_PRESENT_INFO_KHR};
  pi.waitSemaphoreCount = 1;
  pi.pWaitSemaphores = &a.renderFinished[image];
  pi.swapchainCount = 1;
  pi.pSwapchains = &a.swapchain;
  pi.pImageIndices = &image;
  VkResult pr = vkQueuePresentKHR(a.presentQueue, &pi);
  if (pr == VK_ERROR_OUT_OF_DATE_KHR || pr == VK_SUBOPTIMAL_KHR || recreate) {
    Recreate(a);
    return;
  }
  a.Check(pr, "present failed");
}
void Destroy(App &a) {
  if (a.device)
    vkDeviceWaitIdle(a.device);
  if (a.device) {
    if (a.fence)
      vkDestroyFence(a.device, a.fence, nullptr);
    for (auto s : a.renderFinished)
      vkDestroySemaphore(a.device, s, nullptr);
    if (a.imageAvailable)
      vkDestroySemaphore(a.device, a.imageAvailable, nullptr);
    DestroyMesh(a);
    DestroySubmission(a);
    if (a.pool)
      vkDestroyCommandPool(a.device, a.pool, nullptr);
    DestroySwap(a);
    vkDestroyDevice(a.device, nullptr);
  }
  if (a.surface)
    vkDestroySurfaceKHR(a.instance, a.surface, nullptr);
  if (a.debug) {
    auto fn = (PFN_vkDestroyDebugUtilsMessengerEXT)vkGetInstanceProcAddr(
        a.instance, "vkDestroyDebugUtilsMessengerEXT");
    if (fn)
      fn(a.instance, a.debug, nullptr);
  }
  if (a.instance)
    vkDestroyInstance(a.instance, nullptr);
  if (a.window)
    DestroyWindow(a.window);
  gApp = nullptr;
}
void Update(App &a, float dt) {
  a.Check(vkWaitForFences(a.device, 1, &a.fence, VK_TRUE, UINT64_MAX), "frame fence wait failed");
  InspectGpuPlanetary(a);
  bool active = (GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0 || (GetAsyncKeyState(VK_RBUTTON) & 0x8000) != 0;
  if (active && !a.lookActive) {
    a.lookActive = true;
    a.rawMouseX = 0;
    a.rawMouseY = 0;
  }
  if (!active)
    ClearLookInput(a);
  auto x = (float)a.rawMouseX, y = (float)a.rawMouseY;
  // Preserve high-resolution-wheel remainder until it forms a whole Win32 detent.
  auto wheel = static_cast<int32_t>(a.wheelDeltaRaw / WHEEL_DELTA);
  a.wheelDeltaRaw %= WHEEL_DELTA;
  a.rawMouseX = 0;
  a.rawMouseY = 0;
  auto rising = [](int key, bool &wasDown) {
    const bool down = (GetAsyncKeyState(key) & 0x8000) != 0;
    const bool result = down && !wasDown;
    wasDown = down;
    return result;
  };
  uint32_t sasModeKey = 0;
  for (int key = 0; key < 8; ++key) if (rising('0' + key, a.sasModeWasDown[key])) { sasModeKey = static_cast<uint32_t>(key + 1); break; }
  NcInputState in{dt,
                  (GetAsyncKeyState('A') & 0x8000) != 0,
                  (GetAsyncKeyState('D') & 0x8000) != 0,
                  (GetAsyncKeyState('W') & 0x8000) != 0,
                  (GetAsyncKeyState('S') & 0x8000) != 0,
                  (GetAsyncKeyState('Q') & 0x8000) != 0,
                  (GetAsyncKeyState('E') & 0x8000) != 0,
                  (GetAsyncKeyState('R') & 0x8000) != 0,
                  a.lookActive,
                  x,
                  y,
                  wheel,
                  rising(VK_SPACE, a.pauseWasDown),
                  rising(VK_OEM_COMMA, a.rateDecreaseWasDown),
                  rising(VK_OEM_PERIOD, a.rateIncreaseWasDown), sasModeKey};
  NcHostEvent e{NC_UPDATE_FRAME, NC_LOG_NONE, nullptr, in, a.submission};
  a.cb(&e, a.cbData);
  if(a.submission->planetaryMode==NC_PLANETARY_CPU_REFERENCE)EnsurePatchCapacity(a,a.submission->planetaryPatchCount);
  Validate(a);
  Upload(a);
}
} // namespace
extern "C" NC_API NcResult __cdecl nc_validate_planetary_patches(const NcPlanetaryPatch *patches, uint32_t count) {
  if (count && !patches) return NC_INVALID_ARGUMENT;
  for (uint32_t i=0;i<count;i++) { const auto &p=patches[i]; if (p.face>=6 || !std::isfinite(p.radius) || p.radius<=0 || !std::isfinite(p.centerX) || !std::isfinite(p.centerY) || !std::isfinite(p.centerZ) || p.level>=31 || p.x >= (1u<<p.level) || p.y >= (1u<<p.level) || p.stitchMask>15 || p.reserved0 || p.reserved1 || p.reserved2) return NC_INVALID_ARGUMENT; }
  return NC_SUCCESS;
}
extern "C" NC_API NcResult __cdecl nc_get_abi_layout(NcAbiLayout *o) {
  if (!o)
    return NC_INVALID_ARGUMENT;
  *o = {(uint32_t)sizeof(NcEncodedPosition),
        (uint32_t)sizeof(NcCameraData),
        (uint32_t)offsetof(NcCameraData, position),
        (uint32_t)offsetof(NcCameraData, viewProjection),
        (uint32_t)sizeof(NcRenderTransform),
        (uint32_t)sizeof(NcRenderObject),
        (uint32_t)offsetof(NcRenderObject, position),
        (uint32_t)offsetof(NcRenderObject, transform),
        (uint32_t)offsetof(NcRenderObject, mesh),
        (uint32_t)sizeof(NcDrawBatch),
        (uint32_t)sizeof(NcOrbitLineVertex),
        (uint32_t)sizeof(NcFrameSubmission),
        (uint32_t)offsetof(NcFrameSubmission, objects),
        (uint32_t)offsetof(NcFrameSubmission, batches),
        (uint32_t)offsetof(NcFrameSubmission, orbitVertices),
        (uint32_t)offsetof(NcFrameSubmission, orbitVertexCount),
        (uint32_t)sizeof(NcInputState),
        (uint32_t)offsetof(NcInputState, deltaSeconds),
        (uint32_t)offsetof(NcInputState, moveLeft),
        (uint32_t)offsetof(NcInputState, moveRight),
        (uint32_t)offsetof(NcInputState, moveForward),
        (uint32_t)offsetof(NcInputState, moveBackward),
        (uint32_t)offsetof(NcInputState, moveDown),
        (uint32_t)offsetof(NcInputState, moveUp),
        (uint32_t)offsetof(NcInputState, reset),
        (uint32_t)offsetof(NcInputState, lookActive),
        (uint32_t)offsetof(NcInputState, mouseDeltaX),
        (uint32_t)offsetof(NcInputState, mouseDeltaY),
        (uint32_t)offsetof(NcInputState, mouseWheelDetents),
        (uint32_t)offsetof(NcInputState, pauseToggle),
        (uint32_t)offsetof(NcInputState, rateDecrease),
        (uint32_t)offsetof(NcInputState, rateIncrease),
        (uint32_t)offsetof(NcInputState, sasModeKey),
        (uint32_t)offsetof(NcFrameSubmission, planetaryGpu),
        (uint32_t)offsetof(NcFrameSubmission, planetaryMode)};
  return NC_SUCCESS;
}
extern "C" NC_API NcResult __cdecl
nc_run_renderer(NcFrameSubmission *s, NcHostCallback cb, void *data) {
  if (!cb || !s || !s->objects || !s->objectCount || !s->batches ||
      !s->batchCount)
    return NC_INVALID_ARGUMENT;
  App a;
  a.cb = cb;
  a.cbData = data;
  a.submission = s;
  try {
    gApp = &a;
    Window(a);
    Instance(a);
    SetupDebug(a);
    Surface(a);
    Device(a);
    CreateMesh(a);
    Validate(a);
    Swap(a);
    CreateSubmission(a);
    Commands(a);
    a.Log(NC_LOG_STARTUP, "Native host callback is active");
    auto start = std::chrono::steady_clock::now(), last = start;
    uint64_t frames = 0;
    bool run = true;
    while (run) {
      MSG m;
      while (PeekMessage(&m, nullptr, 0, 0, PM_REMOVE)) {
        if (m.message == WM_QUIT)
          run = false;
        TranslateMessage(&m);
        DispatchMessage(&m);
      }
      if (run) {
        auto now = std::chrono::steady_clock::now();
        Update(a, std::chrono::duration<float>(now - last).count());
        last = now;
        Draw(a);
        frames++;
      }
    }
    vkDeviceWaitIdle(a.device);
    char text[128];
    auto ms = std::chrono::duration<double, std::milli>(
                  std::chrono::steady_clock::now() - start)
                  .count() /
              std::max<uint64_t>(1, frames);
    std::snprintf(text, sizeof text,
                  "Average frame time: %.3f ms (%llu frames)", ms,
                  (unsigned long long)frames);
    a.Log(NC_LOG_ALWAYS, text);
    Destroy(a);
    return NC_SUCCESS;
  } catch (const std::exception &e) {
    a.Log(NC_LOG_ALWAYS, e.what());
    Destroy(a);
    return NC_FAILURE;
  }
}
