#include "NovaCoreNative.h"
#include <algorithm>
#include <array>
#include <chrono>
#include <condition_variable>
#include <cmath>
#include <compare>
#include <cstddef>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <fstream>
#include <filesystem>
#include <memory>
#include <mutex>
#include <optional>
#include <stdexcept>
#include <string>
#include <thread>
#include <vector>
#include <vulkan/vulkan.h>
#include <windows.h>

namespace {
constexpr uint32_t Width = 960, Height = 540;
constexpr uint32_t GpuPatchCapacity = 8192, GpuActiveHashCapacity = 16384,
                   GpuPreviousHashCapacity = 16384,
                   GpuNodeEntryCapacity = GpuActiveHashCapacity + GpuPreviousHashCapacity,
                   TerrainCacheCapacity = 8192, TerrainGridVertexCount = 289,
                   EyeballRadialRings = 128, EyeballAzimuthSegments = 256,
                   EyeballVertexCount = 1 + EyeballRadialRings * EyeballAzimuthSegments,
                   EyeballIndexCount = EyeballAzimuthSegments * 3 +
                       (EyeballRadialRings - 1) * EyeballAzimuthSegments * 6;
constexpr uint32_t EarthTileSize=256,EarthTileGutter=2,EarthTileExtent=260,EarthMaximumLevel=4,
                   EarthTileCount=682,EarthPhysicalSlots=128,EarthUploadBudget=2,
                   EarthAlbedoTileBytes=EarthTileExtent*EarthTileExtent*4,
                   EarthElevationTileBytes=EarthTileExtent*EarthTileExtent*2,
                   EarthCloudTileBytes=EarthTileExtent*EarthTileExtent,
                   EarthTileRecordBytes=EarthAlbedoTileBytes+EarthElevationTileBytes+EarthCloudTileBytes;
constexpr uint32_t EarthStagingBytes=EarthTileRecordBytes*EarthUploadBudget;
static_assert(EarthAlbedoTileBytes==270400&&EarthElevationTileBytes==135200&&EarthCloudTileBytes==67600);
static_assert(EarthTileRecordBytes==473200&&EarthStagingBytes==946400);
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
static_assert(sizeof(NcPlanetaryGpuConstants) == 80);
static_assert(alignof(NcPlanetaryGpuConstants) == 16);
static_assert(offsetof(NcPlanetaryGpuConstants, cameraBodyLowX) == 16 &&
              offsetof(NcPlanetaryGpuConstants, refinementThreshold) == 32 &&
              offsetof(NcPlanetaryGpuConstants, maximumLevel) == 48 &&
              offsetof(NcPlanetaryGpuConstants, viewForwardX) == 64);
static_assert(sizeof(NcPlanetaryEyeball) == 128);
static_assert(alignof(NcPlanetaryEyeball) == 16);
static_assert(offsetof(NcPlanetaryEyeball, cameraBodyLowX) == 16);
static_assert(offsetof(NcPlanetaryEyeball, surfaceAltitudeMetres) == 32);
static_assert(offsetof(NcPlanetaryEyeball, bodyIdLow) == 48);
static_assert(offsetof(NcPlanetaryEyeball, viewForwardX) == 64);
static_assert(offsetof(NcPlanetaryEyeball, radialWarpExponent) == 80);
static_assert(offsetof(NcPlanetaryEyeball, vertexCount) == 96);
static_assert(sizeof(NcPlanetaryPresentation) == 160);
static_assert(alignof(NcPlanetaryPresentation) == 16);
static_assert(offsetof(NcPlanetaryPresentation, colorR) == 16);
static_assert(offsetof(NcPlanetaryPresentation, detailedAlpha) == 32);
static_assert(offsetof(NcPlanetaryPresentation, bodyIdLow) == 48);
static_assert(offsetof(NcPlanetaryPresentation, roughness) == 64);
static_assert(offsetof(NcPlanetaryPresentation, projectionKind) == 80);
static_assert(offsetof(NcPlanetaryPresentation, ringInnerRadiusRatio) == 96);
static_assert(offsetof(NcPlanetaryPresentation, ringOrientationX) == 112);
static_assert(offsetof(NcPlanetaryPresentation, ringColorR) == 128);
static_assert(offsetof(NcPlanetaryPresentation, bodyOrientationX) == 144);
static_assert(sizeof(NcSolarLighting) == 48);
static_assert(alignof(NcSolarLighting) == 16);
static_assert(offsetof(NcSolarLighting, photosphereR) == 16);
static_assert(offsetof(NcSolarLighting, sourceRadiance) == 32);
static_assert(offsetof(NcSolarLighting, speedHud) == 44);
static_assert(sizeof(NcPlanetaryEnvironment) == 128);
static_assert(alignof(NcPlanetaryEnvironment) == 16);
static_assert(offsetof(NcPlanetaryEnvironment, bodyIdLow) == 16);
static_assert(offsetof(NcPlanetaryEnvironment, atmosphereHeightMetres) == 32);
static_assert(offsetof(NcPlanetaryEnvironment, cloudBaseHeightMetres) == 64);
static_assert(offsetof(NcPlanetaryEnvironment, oceanSeaLevelMetres) == 96);
static_assert(sizeof(NcFrameSubmission) == 784);
static_assert(offsetof(NcFrameSubmission, planetaryGpu) == 208);
static_assert(offsetof(NcFrameSubmission, planetaryMode) == 288);
static_assert(offsetof(NcFrameSubmission, planetaryPresentation) == 304);
static_assert(offsetof(NcFrameSubmission, distantBodies) == 464);
static_assert(offsetof(NcFrameSubmission, distantBodyCount) == 472);
static_assert(offsetof(NcFrameSubmission, distantBodyPadding) == 476);
static_assert(offsetof(NcFrameSubmission, solarLighting) == 480);
static_assert(offsetof(NcFrameSubmission, planetaryEnvironment) == 528);
static_assert(offsetof(NcFrameSubmission, planetaryEyeball) == 656);
static_assert(sizeof(NcOrbitLineVertex) == 12);
static_assert(sizeof(NcInputState) == 68);
static_assert(sizeof(NcPresentationFocus) == 4);
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
static_assert(offsetof(NcInputState, presentationFocus) == 64);
struct Vertex {
  float position[3];
  float color[3];
};
static_assert(sizeof(Vertex) == 24);
struct PatchVertex { float uv[2]; };
static_assert(sizeof(PatchVertex) == 8);
struct DistantVertex { float position[3]; };
static_assert(sizeof(DistantVertex) == 12);
struct RingVertex { float directionX, directionZ, radial; };
static_assert(sizeof(RingVertex) == 12);
struct EyeballVertex { float positionHeight[4], normal[4], bodyDirection[4]; };
static_assert(sizeof(EyeballVertex) == 48);
struct Mesh {
  VkBuffer vb{}, ib{};
  VkDeviceMemory vm{}, im{};
  uint32_t indices{};
};
struct GpuPlanetaryControl {
  VkDrawIndexedIndirectCommand draw{};
  uint32_t roots{}, candidates{}, refined{}, culled{}, active{}, balanced{},
      minimumLevel{}, maximumLevel{}, overflow{}, cacheHits{}, cacheMisses{},
      cacheGenerated{}, cacheEvictions{}, cacheResident{}, cacheCapacity{},
      frustumCulled{}, horizonCulled{}, splits{}, merges{}, parentFallbacks{},
      pendingChildren{}, padding[2]{};
};
static_assert(sizeof(GpuPlanetaryControl) == 112);
struct PatchIdentity {
  uint32_t face{}, level{}, x{}, y{}, stitchMask{};
  auto operator<=>(const PatchIdentity &) const = default;
};
struct EarthPage { uint32_t slotPlusOne{},readyFrame{},parent{},flags{}; };
static_assert(sizeof(EarthPage)==16);
// state: 0 = free, 1 = owned by the I/O thread, 2 = complete and ready to stage.
struct EarthReadyTile { uint32_t tile{},state{};std::array<uint8_t,EarthTileRecordBytes> bytes{}; };
struct EarthIoState {
  std::thread worker;std::mutex mutex;std::condition_variable wake;bool stop{};
  std::array<uint32_t,256> requests{};uint32_t requestHead{},requestTail{},requestCount{};
  std::array<EarthReadyTile,4> ready{};std::string path;uint64_t diskLoads{},queueDrops{};
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
  static constexpr VkFormat SceneFormat = VK_FORMAT_R16G16B16A16_SFLOAT;
  static constexpr VkFormat DepthFormat = VK_FORMAT_D32_SFLOAT;
  VkExtent2D extent{};
  std::vector<VkImage> images;
  std::vector<VkImageView> views;
  VkRenderPass renderPass{};
  VkImage sceneColor{};
  VkDeviceMemory sceneColorMemory{};
  VkImageView sceneColorView{};
  VkImage sceneDepth{};
  VkDeviceMemory sceneDepthMemory{};
  VkImageView sceneDepthView{};
  VkImage earthAlbedo{},earthElevation{},earthCloud{};
  VkDeviceMemory earthAlbedoMemory{},earthElevationMemory{},earthCloudMemory{};
  VkImageView earthAlbedoView{},earthElevationView{},earthCloudView{};
  VkSampler earthSampler{};
  VkBuffer earthPageBuffer{},earthStagingBuffer{};VkDeviceMemory earthPageMemory{},earthStagingMemory{};
  void *earthPageMapped{},*earthStagingMapped{};
  std::array<EarthPage,EarthTileCount> earthPages{};
  std::array<uint32_t,EarthPhysicalSlots> earthSlotTile{};
  std::array<uint64_t,EarthPhysicalSlots> earthSlotLastUse{};
  std::array<uint8_t,EarthTileCount> earthRequested{};
  std::unique_ptr<EarthIoState> earthIo;bool earthAvailable{},earthImagesInitialized{};uint32_t earthPendingUploads{};
  std::array<uint32_t,EarthUploadBudget> earthUploadTiles{},earthUploadSlots{};
  uint64_t earthRequests{},earthDemandHits{},earthDemandMisses{},earthUploads{},earthFallbackFrames{},earthEvictions{};
  VkPipelineLayout pipelineLayout{};
  VkPipeline pipeline{};
  VkPipeline backgroundPipeline{};
  VkPipeline toneMapPipeline{};
  VkPipeline planetaryEnvironmentPipeline{};
  VkPipeline stellarSunPipeline{};
  VkPipeline stellarGlowPipeline{};
  VkPipeline planetaryPipeline{};
  VkPipeline planetaryComputePipeline{};
  VkPipeline planetaryTerrainPipeline{};
  VkPipeline planetaryEyeballComputePipeline{};
  VkPipeline planetaryEyeballPipeline{};
  VkPipeline distantPlanetaryPipeline{};
  VkPipeline distantPlanetaryHandoffPipeline{};
  VkPipeline planetaryRingFarPipeline{};
  VkPipeline planetaryRingNearPipeline{};
  VkPipeline solarOrbitPipeline{};
  VkPipeline solarMarkerPipeline{};
  VkPipeline solarLabelPipeline{};
  VkPipeline solarSpeedHudPipeline{};
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
  VkBuffer terrainKeyBuffer{}, terrainSampleBuffer{}, terrainPatchSlotBuffer{};
  VkDeviceMemory terrainKeyMemory{}, terrainSampleMemory{}, terrainPatchSlotMemory{};
  void *terrainKeyMapped{}, *terrainSampleMapped{}, *terrainPatchSlotMapped{};
  VkBuffer planetaryPresentationBuffer{};
  VkDeviceMemory planetaryPresentationMemory{};
  void *planetaryPresentationMapped{};
  VkBuffer planetaryEnvironmentBuffer{};
  VkDeviceMemory planetaryEnvironmentMemory{};
  void *planetaryEnvironmentMapped{};
  VkBuffer planetaryEyeballInputBuffer{}, planetaryEyeballIndirectBuffer{};
  VkDeviceMemory planetaryEyeballInputMemory{}, planetaryEyeballIndirectMemory{};
  void *planetaryEyeballInputMapped{}, *planetaryEyeballIndirectMapped{};
  void *planetaryEyeballVertexMapped{};
  bool hasEyeballValidation{};float lastEyeballValidationAltitude{};
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
  Mesh distantPlanetary{};
  Mesh stellarSun{};
  Mesh planetaryRing{};
  Mesh planetaryEyeball{};
  VkQueryPool timestampQueries{};
  float timestampPeriodNanoseconds{};
  bool timestampFrameSubmitted{};
  static constexpr uint32_t TimestampCount=9;
  std::array<double,TimestampCount> timestampAccumulatedMs{};
  uint64_t timestampSampleCount{};
  double cpuUpdateMs{},cpuRecordMs{},cpuSubmitMs{},cpuPresentMs{};
  uint64_t cpuTimingSamples{};
  std::array<double, 8192> frameTimesMs{};
  size_t frameTimeCount{}, frameTimeCursor{};
  LONG rawMouseX{}, rawMouseY{};
  LONG wheelDeltaRaw{};
  bool lookActive{};
  bool pauseWasDown{}, rateDecreaseWasDown{}, rateIncreaseWasDown{};
  std::array<bool, 8> sasModeWasDown{};
  std::array<bool, 10> presentationFocusWasDown{};
  bool resetWasDown{};
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
  a.presentationFocusWasDown.fill(false);
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
  constexpr uint32_t latitudeSegments=12,longitudeSegments=24;
  std::vector<DistantVertex> distantVertices;distantVertices.reserve(2+(latitudeSegments-1)*longitudeSegments);distantVertices.push_back({{0,1,0}});
  for(uint32_t latitude=1;latitude<latitudeSegments;latitude++){const auto phi=3.14159265358979323846*double(latitude)/latitudeSegments;const auto ring=std::sin(phi),height=std::cos(phi);for(uint32_t longitude=0;longitude<longitudeSegments;longitude++){const auto theta=2.0*3.14159265358979323846*double(longitude)/longitudeSegments;distantVertices.push_back({{float(ring*std::cos(theta)),float(height),float(ring*std::sin(theta))}});}}
  const uint32_t bottom=(uint32_t)distantVertices.size();distantVertices.push_back({{0,-1,0}});std::vector<uint32_t> distantIndices;distantIndices.reserve(6*longitudeSegments*(latitudeSegments-1));
  for(uint32_t longitude=0;longitude<longitudeSegments;longitude++){const auto next=(longitude+1)%longitudeSegments;distantIndices.insert(distantIndices.end(),{0,1+next,1+longitude});}
  for(uint32_t latitude=0;latitude<latitudeSegments-2;latitude++)for(uint32_t longitude=0;longitude<longitudeSegments;longitude++){const auto next=(longitude+1)%longitudeSegments;const auto upper=1+latitude*longitudeSegments+longitude,upperNext=1+latitude*longitudeSegments+next,lower=upper+longitudeSegments,lowerNext=upperNext+longitudeSegments;distantIndices.insert(distantIndices.end(),{upper,upperNext,lower,upperNext,lowerNext,lower});}
  const auto lastRing=1+(latitudeSegments-2)*longitudeSegments;for(uint32_t longitude=0;longitude<longitudeSegments;longitude++){const auto next=(longitude+1)%longitudeSegments;distantIndices.insert(distantIndices.end(),{lastRing+longitude,lastRing+next,bottom});}
  Buffer(a,sizeof(DistantVertex)*distantVertices.size(),VK_BUFFER_USAGE_VERTEX_BUFFER_BIT,a.distantPlanetary.vb,a.distantPlanetary.vm,distantVertices.data());Buffer(a,sizeof(uint32_t)*distantIndices.size(),VK_BUFFER_USAGE_INDEX_BUFFER_BIT,a.distantPlanetary.ib,a.distantPlanetary.im,distantIndices.data());a.distantPlanetary.indices=(uint32_t)distantIndices.size();
  constexpr uint32_t stellarLatitudeSegments=32,stellarLongitudeSegments=64;std::vector<DistantVertex> stellarVertices;stellarVertices.reserve(2+(stellarLatitudeSegments-1)*stellarLongitudeSegments);stellarVertices.push_back({{0,1,0}});for(uint32_t latitude=1;latitude<stellarLatitudeSegments;latitude++){const auto phi=3.14159265358979323846*double(latitude)/stellarLatitudeSegments;const auto ring=std::sin(phi),height=std::cos(phi);for(uint32_t longitude=0;longitude<stellarLongitudeSegments;longitude++){const auto theta=2.0*3.14159265358979323846*double(longitude)/stellarLongitudeSegments;stellarVertices.push_back({{float(ring*std::cos(theta)),float(height),float(ring*std::sin(theta))}});}}const uint32_t stellarBottom=(uint32_t)stellarVertices.size();stellarVertices.push_back({{0,-1,0}});std::vector<uint32_t> stellarIndices;stellarIndices.reserve(6*stellarLongitudeSegments*(stellarLatitudeSegments-1));for(uint32_t longitude=0;longitude<stellarLongitudeSegments;longitude++){const auto next=(longitude+1)%stellarLongitudeSegments;stellarIndices.insert(stellarIndices.end(),{0,1+next,1+longitude});}for(uint32_t latitude=0;latitude<stellarLatitudeSegments-2;latitude++)for(uint32_t longitude=0;longitude<stellarLongitudeSegments;longitude++){const auto next=(longitude+1)%stellarLongitudeSegments;const auto upper=1+latitude*stellarLongitudeSegments+longitude,upperNext=1+latitude*stellarLongitudeSegments+next,lower=upper+stellarLongitudeSegments,lowerNext=upperNext+stellarLongitudeSegments;stellarIndices.insert(stellarIndices.end(),{upper,upperNext,lower,upperNext,lowerNext,lower});}const auto stellarLastRing=1+(stellarLatitudeSegments-2)*stellarLongitudeSegments;for(uint32_t longitude=0;longitude<stellarLongitudeSegments;longitude++){const auto next=(longitude+1)%stellarLongitudeSegments;stellarIndices.insert(stellarIndices.end(),{stellarLastRing+longitude,stellarLastRing+next,stellarBottom});}Buffer(a,sizeof(DistantVertex)*stellarVertices.size(),VK_BUFFER_USAGE_VERTEX_BUFFER_BIT,a.stellarSun.vb,a.stellarSun.vm,stellarVertices.data());Buffer(a,sizeof(uint32_t)*stellarIndices.size(),VK_BUFFER_USAGE_INDEX_BUFFER_BIT,a.stellarSun.ib,a.stellarSun.im,stellarIndices.data());a.stellarSun.indices=(uint32_t)stellarIndices.size();
  constexpr uint32_t ringSegments=256;std::array<RingVertex,ringSegments*2> ringVertices{};std::array<uint32_t,ringSegments*6> ringIndices{};for(uint32_t segment=0;segment<ringSegments;segment++){const double angle=2.0*3.14159265358979323846*double(segment)/ringSegments;const float x=float(std::cos(angle)),z=float(std::sin(angle));ringVertices[segment*2]={x,z,0};ringVertices[segment*2+1]={x,z,1};const uint32_t next=(segment+1)%ringSegments,base=segment*6;ringIndices[base]=segment*2;ringIndices[base+1]=next*2;ringIndices[base+2]=segment*2+1;ringIndices[base+3]=segment*2+1;ringIndices[base+4]=next*2;ringIndices[base+5]=next*2+1;}Buffer(a,sizeof(ringVertices),VK_BUFFER_USAGE_VERTEX_BUFFER_BIT,a.planetaryRing.vb,a.planetaryRing.vm,ringVertices.data());Buffer(a,sizeof(ringIndices),VK_BUFFER_USAGE_INDEX_BUFFER_BIT,a.planetaryRing.ib,a.planetaryRing.im,ringIndices.data());a.planetaryRing.indices=(uint32_t)ringIndices.size();
  std::vector<EyeballVertex> eyeballVertices(EyeballVertexCount);std::vector<uint32_t> eyeballIndices;eyeballIndices.reserve(EyeballIndexCount);
  for(uint32_t segment=0;segment<EyeballAzimuthSegments;segment++){const uint32_t next=(segment+1)%EyeballAzimuthSegments;eyeballIndices.insert(eyeballIndices.end(),{0u,1u+segment,1u+next});}
  for(uint32_t ring=1;ring<EyeballRadialRings;ring++){const uint32_t inner=1u+(ring-1u)*EyeballAzimuthSegments,outer=1u+ring*EyeballAzimuthSegments;for(uint32_t segment=0;segment<EyeballAzimuthSegments;segment++){const uint32_t next=(segment+1)%EyeballAzimuthSegments;eyeballIndices.insert(eyeballIndices.end(),{inner+segment,outer+segment,inner+next,inner+next,outer+segment,outer+next});}}
  if(eyeballIndices.size()!=EyeballIndexCount)throw std::runtime_error("eyeball topology generation failed");
  Buffer(a,sizeof(EyeballVertex)*eyeballVertices.size(),VK_BUFFER_USAGE_VERTEX_BUFFER_BIT|VK_BUFFER_USAGE_STORAGE_BUFFER_BIT,a.planetaryEyeball.vb,a.planetaryEyeball.vm,eyeballVertices.data());a.Check(vkMapMemory(a.device,a.planetaryEyeball.vm,0,sizeof(EyeballVertex)*eyeballVertices.size(),0,&a.planetaryEyeballVertexMapped),"eyeball validation map failed");Buffer(a,sizeof(uint32_t)*eyeballIndices.size(),VK_BUFFER_USAGE_INDEX_BUFFER_BIT,a.planetaryEyeball.ib,a.planetaryEyeball.im,eyeballIndices.data());a.planetaryEyeball.indices=(uint32_t)eyeballIndices.size();
  {char message[192];std::snprintf(message,sizeof message,"Eyeball topology: vertices=%u; indices=%u; persistentBytes=%zu",EyeballVertexCount,EyeballIndexCount,sizeof(EyeballVertex)*size_t(EyeballVertexCount)+sizeof(uint32_t)*size_t(EyeballIndexCount));a.Log(NC_LOG_RENDERER,message);}
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
  if(a.distantPlanetary.vb)vkDestroyBuffer(a.device,a.distantPlanetary.vb,nullptr);
  if(a.distantPlanetary.vm)vkFreeMemory(a.device,a.distantPlanetary.vm,nullptr);
  if(a.distantPlanetary.ib)vkDestroyBuffer(a.device,a.distantPlanetary.ib,nullptr);
  if(a.distantPlanetary.im)vkFreeMemory(a.device,a.distantPlanetary.im,nullptr);
  a.distantPlanetary={};
  if(a.stellarSun.vb)vkDestroyBuffer(a.device,a.stellarSun.vb,nullptr);
  if(a.stellarSun.vm)vkFreeMemory(a.device,a.stellarSun.vm,nullptr);
  if(a.stellarSun.ib)vkDestroyBuffer(a.device,a.stellarSun.ib,nullptr);
  if(a.stellarSun.im)vkFreeMemory(a.device,a.stellarSun.im,nullptr);
  a.stellarSun={};
  if(a.planetaryRing.vb)vkDestroyBuffer(a.device,a.planetaryRing.vb,nullptr);
  if(a.planetaryRing.vm)vkFreeMemory(a.device,a.planetaryRing.vm,nullptr);
  if(a.planetaryRing.ib)vkDestroyBuffer(a.device,a.planetaryRing.ib,nullptr);
  if(a.planetaryRing.im)vkFreeMemory(a.device,a.planetaryRing.im,nullptr);
  a.planetaryRing={};
  if(a.planetaryEyeballVertexMapped)vkUnmapMemory(a.device,a.planetaryEyeball.vm);a.planetaryEyeballVertexMapped=nullptr;
  if(a.planetaryEyeball.vb)vkDestroyBuffer(a.device,a.planetaryEyeball.vb,nullptr);
  if(a.planetaryEyeball.vm)vkFreeMemory(a.device,a.planetaryEyeball.vm,nullptr);
  if(a.planetaryEyeball.ib)vkDestroyBuffer(a.device,a.planetaryEyeball.ib,nullptr);
  if(a.planetaryEyeball.im)vkFreeMemory(a.device,a.planetaryEyeball.im,nullptr);
  a.planetaryEyeball={};
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
  const auto validMaterial=[](const NcPlanetaryPresentation &body){const bool stellarMaterial=body.bodyIdLow==2&&body.bodyIdHigh==0&&body.materialKind==0&&body.albedoSource==0;const bool finite=std::isfinite(body.roughness)&&std::isfinite(body.specular)&&std::isfinite(body.emissive)&&std::isfinite(body.presentationRotationRadians)&&std::isfinite(body.ringInnerRadiusRatio)&&std::isfinite(body.ringOuterRadiusRatio)&&std::isfinite(body.ringOpacity)&&std::isfinite(body.ringBandFrequency)&&std::isfinite(body.ringOrientationX)&&std::isfinite(body.ringOrientationY)&&std::isfinite(body.ringOrientationZ)&&std::isfinite(body.ringOrientationW)&&std::isfinite(body.ringColorR)&&std::isfinite(body.ringColorG)&&std::isfinite(body.ringColorB)&&std::isfinite(body.ringColorA)&&std::isfinite(body.bodyOrientationX)&&std::isfinite(body.bodyOrientationY)&&std::isfinite(body.bodyOrientationZ)&&std::isfinite(body.bodyOrientationW);const float bodyQ=body.bodyOrientationX*body.bodyOrientationX+body.bodyOrientationY*body.bodyOrientationY+body.bodyOrientationZ*body.bodyOrientationZ+body.bodyOrientationW*body.bodyOrientationW;if(!finite||std::abs(bodyQ-1)>1e-4f||body.roughness<0||body.roughness>1||body.specular<0||body.specular>1||body.emissive<0||body.projectionKind>0)return false;if(!stellarMaterial&&((!body.bodyIdLow&&!body.bodyIdHigh)||body.materialKind<1||body.materialKind>4||body.albedoSource<1||body.albedoSource>10))return false;if(!body.ringAssociation)return body.ringInnerRadiusRatio==0&&body.ringOuterRadiusRatio==0&&body.ringOpacity==0&&body.ringBandFrequency==0;if(body.ringInnerRadiusRatio<=1||body.ringOuterRadiusRatio<=body.ringInnerRadiusRatio||body.ringOpacity<=0||body.ringOpacity>1||body.ringBandFrequency<=0)return false;const float q=body.ringOrientationX*body.ringOrientationX+body.ringOrientationY*body.ringOrientationY+body.ringOrientationZ*body.ringOrientationZ+body.ringOrientationW*body.ringOrientationW;return std::abs(q-1)<1e-4f;};
  if(s->planetaryGpuAlignmentPadding||s->planetaryPadding[0]||s->planetaryPadding[1]||s->planetaryPadding[2])throw std::runtime_error("invalid planetary frame padding");
  if(s->planetaryMode>NC_PLANETARY_CPU_GPU_VALIDATION)throw std::runtime_error("invalid planetary mode");
  const auto &presentation=s->planetaryPresentation;const bool hasPresentation=presentation.enabled!=0;
  const auto bodyCenterMatches=[&presentation](double cameraX,double cameraY,double cameraZ){const double vx=-cameraX,vy=-cameraY,vz=-cameraZ,qx=presentation.bodyOrientationX,qy=presentation.bodyOrientationY,qz=presentation.bodyOrientationZ,qw=presentation.bodyOrientationW;const double cx=qy*vz-qz*vy,cy=qz*vx-qx*vz,cz=qx*vy-qy*vx;const double ux=cx+qw*vx,uy=cy+qw*vy,uz=cz+qw*vz;const double rx=vx+2*(qy*uz-qz*uy),ry=vy+2*(qz*ux-qx*uz),rz=vz+2*(qx*uy-qy*ux);const double scale=std::max({1.0,std::abs(rx),std::abs(ry),std::abs(rz)}),tolerance=std::max(32.0,scale*1e-6);return std::abs(double(presentation.centerX)-rx)<=tolerance&&std::abs(double(presentation.centerY)-ry)<=tolerance&&std::abs(double(presentation.centerZ)-rz)<=tolerance;};
  if(s->distantBodyPadding||s->distantBodyCount>10||(s->distantBodyCount&&!s->distantBodies))throw std::runtime_error("invalid distant body batch");
  for(uint32_t i=0;i<s->distantBodyCount;i++){const auto &body=s->distantBodies[i];if(!body.enabled||!std::isfinite(body.centerX)||!std::isfinite(body.centerY)||!std::isfinite(body.centerZ)||!std::isfinite(body.radius)||body.radius<=0||!std::isfinite(body.colorR)||!std::isfinite(body.colorG)||!std::isfinite(body.colorB)||!std::isfinite(body.distantAlpha)||body.distantAlpha<0||body.distantAlpha>1||!validMaterial(body))throw std::runtime_error("invalid distant body record");}
  if(presentation.enabled>1)throw std::runtime_error("invalid planetary presentation enable");
  if(hasPresentation){if(presentation.regime>NC_PLANETARY_DETAILED_ONLY||!std::isfinite(presentation.centerX)||!std::isfinite(presentation.centerY)||!std::isfinite(presentation.centerZ)||!std::isfinite(presentation.radius)||presentation.radius<=0||!std::isfinite(presentation.colorR)||!std::isfinite(presentation.colorG)||!std::isfinite(presentation.colorB)||!std::isfinite(presentation.distantAlpha)||!std::isfinite(presentation.detailedAlpha)||!std::isfinite(presentation.distanceRadii)||presentation.distanceRadii<1||presentation.distantAlpha<0||presentation.distantAlpha>1||presentation.detailedAlpha<0||presentation.detailedAlpha>1||std::abs(presentation.distantAlpha+presentation.detailedAlpha-1)>1e-5f||!validMaterial(presentation))throw std::runtime_error("invalid planetary presentation");if(presentation.regime==NC_PLANETARY_DISTANT_ONLY&&(presentation.distantAlpha!=1||presentation.detailedAlpha!=0||s->planetaryPatchCount))throw std::runtime_error("invalid distant-only planetary submission");if(presentation.regime==NC_PLANETARY_DETAILED_ONLY&&(presentation.distantAlpha!=0||presentation.detailedAlpha!=1))throw std::runtime_error("invalid detailed-only planetary submission");}
  if(s->planetaryMode!=NC_PLANETARY_CPU_REFERENCE||hasPresentation){const auto &g=s->planetaryGpu;const double cameraX=static_cast<double>(g.cameraBodyHighX)+g.cameraBodyLowX;const double cameraY=static_cast<double>(g.cameraBodyHighY)+g.cameraBodyLowY;const double cameraZ=static_cast<double>(g.cameraBodyHighZ)+g.cameraBodyLowZ;const double radius=static_cast<double>(g.radiusHigh)+g.radiusLow;const bool finite=std::isfinite(g.cameraBodyHighX)&&std::isfinite(g.cameraBodyHighY)&&std::isfinite(g.cameraBodyHighZ)&&std::isfinite(g.radiusHigh)&&std::isfinite(g.cameraBodyLowX)&&std::isfinite(g.cameraBodyLowY)&&std::isfinite(g.cameraBodyLowZ)&&std::isfinite(g.radiusLow)&&std::isfinite(g.refinementThreshold)&&std::isfinite(g.nearFieldAltitudeRadii)&&std::isfinite(g.surfaceAltitudeMetres)&&std::isfinite(g.maximumTerrainHeightMetres)&&std::isfinite(g.viewForwardX)&&std::isfinite(g.viewForwardY)&&std::isfinite(g.viewForwardZ)&&std::isfinite(g.viewHalfAngleRadians);const float viewLength=g.viewForwardX*g.viewForwardX+g.viewForwardY*g.viewForwardY+g.viewForwardZ*g.viewForwardZ;if(!finite||radius<=0||g.refinementThreshold<=0||g.nearFieldAltitudeRadii<=0||g.surfaceAltitudeMetres<0||g.maximumTerrainHeightMetres<0||g.maximumLevel>24||!g.outputCapacity||g.outputCapacity>GpuPatchCapacity||g.terrainFrame||std::abs(viewLength-1)>1e-4f||g.viewHalfAngleRadians<=0||g.viewHalfAngleRadians>=1.5707964f)throw std::runtime_error("invalid planetary GPU constants");if((g.terrainVersion==0)!=(g.maximumTerrainHeightMetres==0))throw std::runtime_error("inconsistent planetary terrain constants");if(hasPresentation&&(!bodyCenterMatches(cameraX,cameraY,cameraZ)||presentation.radius!=g.radiusHigh))throw std::runtime_error("inconsistent planetary presentation authority");if(s->planetaryMode==NC_PLANETARY_GPU_PRODUCTION&&s->planetaryPatchCount)throw std::runtime_error("GPU planetary mode received CPU leaves");}
  const auto &lighting=s->solarLighting;const uint32_t hudPreset=lighting.speedHud&255u,hudAlpha=(lighting.speedHud>>8)&255u;if(lighting.enabled>1||(lighting.speedHud&0xffff0000u)||(hudPreset==0)!=(hudAlpha==0)||hudPreset>15)throw std::runtime_error("invalid Solar lighting flags");if(lighting.enabled&&(!std::isfinite(lighting.sourceCenterX)||!std::isfinite(lighting.sourceCenterY)||!std::isfinite(lighting.sourceCenterZ)||!std::isfinite(lighting.exposure)||lighting.exposure<=0||!std::isfinite(lighting.photosphereR)||lighting.photosphereR<0||!std::isfinite(lighting.photosphereG)||lighting.photosphereG<0||!std::isfinite(lighting.photosphereB)||lighting.photosphereB<0||!std::isfinite(lighting.ambientFloor)||lighting.ambientFloor<0||lighting.ambientFloor>1||!std::isfinite(lighting.sourceRadiance)||lighting.sourceRadiance<=1||!std::isfinite(lighting.glowStrength)||lighting.glowStrength<0||lighting.glowStrength>4))throw std::runtime_error("invalid Solar lighting presentation");
  const auto &environment=s->planetaryEnvironment;if(environment.enabledLayers){const bool finite=std::isfinite(environment.centerX)&&std::isfinite(environment.centerY)&&std::isfinite(environment.centerZ)&&std::isfinite(environment.radius)&&std::isfinite(environment.atmosphereHeightMetres)&&std::isfinite(environment.rayleighScaleHeightMetres)&&std::isfinite(environment.mieScaleHeightMetres)&&std::isfinite(environment.mieAnisotropy)&&std::isfinite(environment.rayleighR)&&std::isfinite(environment.rayleighG)&&std::isfinite(environment.rayleighB)&&std::isfinite(environment.mieScattering)&&std::isfinite(environment.cloudBaseHeightMetres)&&std::isfinite(environment.cloudTopHeightMetres)&&std::isfinite(environment.cloudCoverage)&&std::isfinite(environment.cloudDensity)&&std::isfinite(environment.cloudGlobalScale)&&std::isfinite(environment.cloudDetailScale)&&std::isfinite(environment.cloudShadowStrength)&&std::isfinite(environment.maximumTerrainHeightMetres)&&std::isfinite(environment.oceanSeaLevelMetres)&&std::isfinite(environment.oceanRoughness)&&std::isfinite(environment.oceanWaveScale)&&std::isfinite(environment.oceanWaveStrength)&&std::isfinite(environment.oceanColorR)&&std::isfinite(environment.oceanColorG)&&std::isfinite(environment.oceanColorB)&&std::isfinite(environment.exposureAdjustment);if(!finite||(environment.enabledLayers&~7u)||(!environment.bodyIdLow&&!environment.bodyIdHigh)||!environment.sourceVersion||environment.radius<=0||environment.atmosphereHeightMetres<=0||environment.rayleighScaleHeightMetres<=0||environment.rayleighScaleHeightMetres>=environment.atmosphereHeightMetres||environment.mieScaleHeightMetres<=0||environment.mieScaleHeightMetres>=environment.atmosphereHeightMetres||environment.mieAnisotropy<0||environment.mieAnisotropy>=1||environment.cloudBaseHeightMetres<0||environment.cloudTopHeightMetres<=environment.cloudBaseHeightMetres||environment.cloudTopHeightMetres>=environment.atmosphereHeightMetres||environment.cloudCoverage<=0||environment.cloudCoverage>=1||environment.cloudDensity<=0||environment.oceanSeaLevelMetres<0||environment.maximumTerrainHeightMetres<=environment.oceanSeaLevelMetres||environment.exposureAdjustment<=0)throw std::runtime_error("invalid planetary environment");if(!hasPresentation||environment.bodyIdLow!=presentation.bodyIdLow||environment.bodyIdHigh!=presentation.bodyIdHigh||environment.centerX!=presentation.centerX||environment.centerY!=presentation.centerY||environment.centerZ!=presentation.centerZ||environment.radius!=presentation.radius)throw std::runtime_error("inconsistent planetary environment authority");}
  const auto &eye=s->planetaryEyeball;if(eye.enabled>1)throw std::runtime_error("invalid eyeball enable");if(eye.enabled){const bool finite=std::isfinite(eye.cameraBodyHighX)&&std::isfinite(eye.cameraBodyHighY)&&std::isfinite(eye.cameraBodyHighZ)&&std::isfinite(eye.radiusHigh)&&std::isfinite(eye.cameraBodyLowX)&&std::isfinite(eye.cameraBodyLowY)&&std::isfinite(eye.cameraBodyLowZ)&&std::isfinite(eye.radiusLow)&&std::isfinite(eye.surfaceAltitudeMetres)&&std::isfinite(eye.maximumTerrainHeightMetres)&&std::isfinite(eye.oceanSeaLevelMetres)&&std::isfinite(eye.blendAlpha)&&std::isfinite(eye.viewForwardX)&&std::isfinite(eye.viewForwardY)&&std::isfinite(eye.viewForwardZ)&&std::isfinite(eye.horizonMarginRadians)&&std::isfinite(eye.radialWarpExponent)&&std::isfinite(eye.detailFrequency)&&std::isfinite(eye.normalStepMetres)&&std::isfinite(eye.regionalAlpha);const double cameraX=double(eye.cameraBodyHighX)+eye.cameraBodyLowX,cameraY=double(eye.cameraBodyHighY)+eye.cameraBodyLowY,cameraZ=double(eye.cameraBodyHighZ)+eye.cameraBodyLowZ;const float viewLength=eye.viewForwardX*eye.viewForwardX+eye.viewForwardY*eye.viewForwardY+eye.viewForwardZ*eye.viewForwardZ;if(!finite||!hasPresentation||(!eye.bodyIdLow&&!eye.bodyIdHigh)||!eye.terrainVersion||eye.radiusHigh<=0||eye.surfaceAltitudeMetres<0||eye.maximumTerrainHeightMetres<=eye.oceanSeaLevelMetres||eye.blendAlpha<=0||eye.blendAlpha>1||eye.regionalAlpha<0||eye.regionalAlpha>1||std::abs(eye.blendAlpha+eye.regionalAlpha-1)>1e-5f||std::abs(viewLength-1)>1e-4f||eye.horizonMarginRadians<=0||eye.radialWarpExponent<1||eye.detailFrequency<=0||eye.normalStepMetres<=0||eye.vertexCount!=EyeballVertexCount||eye.indexCount!=EyeballIndexCount||eye.radialRingCount!=EyeballRadialRings||eye.azimuthSegmentCount!=EyeballAzimuthSegments||eye.reserved0||eye.reserved1||eye.reserved2||eye.reserved3)throw std::runtime_error("invalid planetary eyeball");if(eye.bodyIdLow!=presentation.bodyIdLow||eye.bodyIdHigh!=presentation.bodyIdHigh||eye.radiusHigh!=presentation.radius||!bodyCenterMatches(cameraX,cameraY,cameraZ))throw std::runtime_error("inconsistent planetary eyeball authority");}
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
  VkPhysicalDeviceFeatures features{};vkGetPhysicalDeviceFeatures(d,&features);
  uint32_t n = 0;
  vkEnumerateDeviceExtensionProperties(d, nullptr, &n, nullptr);
  std::vector<VkExtensionProperties> x(n);
  vkEnumerateDeviceExtensionProperties(d, nullptr, &n, x.data());
  return q.Complete() && features.shaderFloat64 && features.samplerAnisotropy && std::any_of(x.begin(), x.end(), [](auto &e) {
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
  a.timestampPeriodNanoseconds=p.limits.timestampPeriod;
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
  VkPhysicalDeviceFeatures enabledFeatures{};enabledFeatures.shaderFloat64=VK_TRUE;enabledFeatures.samplerAnisotropy=VK_TRUE;ci.pEnabledFeatures=&enabledFeatures;
  a.Check(vkCreateDevice(a.physical, &ci, nullptr, &a.device),
          "logical device failed");
  vkGetDeviceQueue(a.device, *q.graphics, 0, &a.graphicsQueue);
  vkGetDeviceQueue(a.device, *q.present, 0, &a.presentQueue);
#ifdef NC_DEBUG_BUILD
  a.Log(NC_LOG_VALIDATION, "Enabled layer: VK_LAYER_KHRONOS_validation");
#else
  a.Log(NC_LOG_STARTUP, "Vulkan validation layer: disabled (Release)");
#endif
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
  if(a.backgroundPipeline)vkDestroyPipeline(a.device,a.backgroundPipeline,nullptr);
  if(a.toneMapPipeline)vkDestroyPipeline(a.device,a.toneMapPipeline,nullptr);
  if(a.planetaryEnvironmentPipeline)vkDestroyPipeline(a.device,a.planetaryEnvironmentPipeline,nullptr);
  if(a.stellarSunPipeline)vkDestroyPipeline(a.device,a.stellarSunPipeline,nullptr);
  if(a.stellarGlowPipeline)vkDestroyPipeline(a.device,a.stellarGlowPipeline,nullptr);
  if (a.planetaryPipeline)
    vkDestroyPipeline(a.device,a.planetaryPipeline,nullptr);
  if (a.planetaryComputePipeline)
    vkDestroyPipeline(a.device,a.planetaryComputePipeline,nullptr);
  if (a.planetaryTerrainPipeline)
    vkDestroyPipeline(a.device,a.planetaryTerrainPipeline,nullptr);
  if (a.planetaryEyeballComputePipeline)
    vkDestroyPipeline(a.device,a.planetaryEyeballComputePipeline,nullptr);
  if (a.planetaryEyeballPipeline)
    vkDestroyPipeline(a.device,a.planetaryEyeballPipeline,nullptr);
  if (a.distantPlanetaryPipeline)
    vkDestroyPipeline(a.device,a.distantPlanetaryPipeline,nullptr);
  if (a.distantPlanetaryHandoffPipeline)
    vkDestroyPipeline(a.device,a.distantPlanetaryHandoffPipeline,nullptr);
  if(a.planetaryRingFarPipeline)vkDestroyPipeline(a.device,a.planetaryRingFarPipeline,nullptr);
  if(a.planetaryRingNearPipeline)vkDestroyPipeline(a.device,a.planetaryRingNearPipeline,nullptr);
  if (a.solarOrbitPipeline)
    vkDestroyPipeline(a.device,a.solarOrbitPipeline,nullptr);
  if (a.solarMarkerPipeline)
    vkDestroyPipeline(a.device,a.solarMarkerPipeline,nullptr);
  if (a.solarLabelPipeline)
    vkDestroyPipeline(a.device,a.solarLabelPipeline,nullptr);
  if (a.solarSpeedHudPipeline)
    vkDestroyPipeline(a.device,a.solarSpeedHudPipeline,nullptr);
  if (a.orbitPipeline)
    vkDestroyPipeline(a.device, a.orbitPipeline, nullptr);
  if (a.previousOrbitPipeline)
    vkDestroyPipeline(a.device, a.previousOrbitPipeline, nullptr);
  if (a.bodyForwardPipeline)
    vkDestroyPipeline(a.device, a.bodyForwardPipeline, nullptr);
  if (a.targetDirectionPipeline)
    vkDestroyPipeline(a.device, a.targetDirectionPipeline, nullptr);
  a.pipeline = {};
  a.backgroundPipeline={};
  a.toneMapPipeline={};
  a.planetaryEnvironmentPipeline={};
  a.stellarSunPipeline={};
  a.stellarGlowPipeline={};
  a.planetaryPipeline = {};
  a.planetaryComputePipeline = {};
  a.planetaryTerrainPipeline = {};
  a.planetaryEyeballComputePipeline = {};
  a.planetaryEyeballPipeline = {};
  a.distantPlanetaryPipeline = {};
  a.distantPlanetaryHandoffPipeline = {};
  a.planetaryRingFarPipeline={};
  a.planetaryRingNearPipeline={};
  a.solarOrbitPipeline = {};
  a.solarMarkerPipeline = {};
  a.solarLabelPipeline = {};
  a.solarSpeedHudPipeline = {};
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
  if(a.sceneColorView)vkDestroyImageView(a.device,a.sceneColorView,nullptr);
  if(a.sceneColor)vkDestroyImage(a.device,a.sceneColor,nullptr);
  if(a.sceneColorMemory)vkFreeMemory(a.device,a.sceneColorMemory,nullptr);
  a.sceneColorView={};a.sceneColor={};a.sceneColorMemory={};
  if(a.sceneDepthView)vkDestroyImageView(a.device,a.sceneDepthView,nullptr);
  if(a.sceneDepth)vkDestroyImage(a.device,a.sceneDepth,nullptr);
  if(a.sceneDepthMemory)vkFreeMemory(a.device,a.sceneDepthMemory,nullptr);
  a.sceneDepthView={};a.sceneDepth={};a.sceneDepthMemory={};
  for (auto x : a.views)
    vkDestroyImageView(a.device, x, nullptr);
  a.views.clear();
  if (a.swapchain)
    vkDestroySwapchainKHR(a.device, a.swapchain, nullptr);
  a.swapchain = {};
}
void CreateHostBuffer(App &,VkDeviceSize,VkBufferUsageFlags,VkBuffer &,VkDeviceMemory &,void *&,const char *);
void DestroyHostBuffer(App &,VkBuffer &,VkDeviceMemory &,void *&);
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
std::string ModuleDirectory(){HMODULE m{};GetModuleHandleExA(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS|GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,reinterpret_cast<LPCSTR>(&ModuleDirectory),&m);char p[MAX_PATH]{};GetModuleFileNameA(m,p,MAX_PATH);std::string path(p);path.resize(path.find_last_of("\\/")+1);return path;}
uint32_t EarthLevelOffset(uint32_t level){uint32_t offset=0,count=2;for(uint32_t i=0;i<level;i++){offset+=count;count*=4;}return offset;}
uint32_t EarthTileIndex(uint32_t level,uint32_t x,uint32_t y){return EarthLevelOffset(level)+y*(1u<<(level+1u))+x;}
bool ReadEarthTile(const std::string &path,uint32_t tile,uint8_t *destination){std::ifstream input(path,std::ios::binary);if(!input)return false;input.seekg(128ull+uint64_t(tile)*EarthTileRecordBytes);input.read(reinterpret_cast<char*>(destination),EarthTileRecordBytes);return input.good()||input.gcount()==EarthTileRecordBytes;}
void GenerateEarthFallbackRoot(uint32_t root,uint8_t *destination){auto *albedo=destination;auto *elevation=reinterpret_cast<uint16_t*>(destination+EarthAlbedoTileBytes);auto *cloud=destination+EarthAlbedoTileBytes+EarthElevationTileBytes;for(uint32_t y=0;y<EarthTileExtent;y++)for(uint32_t x=0;x<EarthTileExtent;x++){const int globalX=(int(root*EarthTileSize)+int(x)-int(EarthTileGutter)+int(EarthTileSize*2))%int(EarthTileSize*2),globalY=std::clamp(int(y)-int(EarthTileGutter),0,int(EarthTileSize)-1);const double u=(globalX+.5)/(EarthTileSize*2.0),v=(globalY+.5)/EarthTileSize,longitude=(u-.5)*6.2831853071795864769,latitude=(.5-v)*3.14159265358979323846;const double continental=.48*std::sin(longitude*1.7+latitude*.8)+.31*std::sin(longitude*3.1-latitude*2.3)+.21*std::sin(longitude*7.3+latitude*5.1),land=continental>.12?1.0:0.0,height=land*std::clamp((continental-.12)*4200.0,0.0,5200.0);const size_t pixel=size_t(y)*EarthTileExtent+x;const double polar=std::pow(std::abs(latitude)/1.5707963267948966192,5.0);albedo[pixel*4+0]=uint8_t(std::clamp(land?(75.0+75.0*continental+125.0*polar):14.0,0.0,255.0));albedo[pixel*4+1]=uint8_t(std::clamp(land?(88.0+85.0*continental+135.0*polar):(52.0+25.0*std::cos(latitude)),0.0,255.0));albedo[pixel*4+2]=uint8_t(std::clamp(land?(45.0+55.0*continental+155.0*polar):118.0,0.0,255.0));albedo[pixel*4+3]=land?255:0;elevation[pixel]=uint16_t(std::clamp(std::lround((height+11000.0)/20000.0*65535.0),0l,65535l));cloud[pixel]=uint8_t(std::clamp(38.0+36.0*std::sin(longitude*4.0+latitude*7.0),0.0,255.0));}}
void EarthIoWorker(EarthIoState *io){for(;;){uint32_t tile{};uint32_t readyIndex{};{std::unique_lock lock(io->mutex);io->wake.wait(lock,[&]{if(io->stop)return true;if(!io->requestCount)return false;for(const auto &ready:io->ready)if(ready.state==0)return true;return false;});if(io->stop)return;for(readyIndex=0;readyIndex<io->ready.size();readyIndex++)if(io->ready[readyIndex].state==0)break;tile=io->requests[io->requestHead];io->requestHead=(io->requestHead+1)%io->requests.size();io->requestCount--;io->ready[readyIndex].state=1;io->ready[readyIndex].tile=tile;}const bool loaded=ReadEarthTile(io->path,tile,io->ready[readyIndex].bytes.data());{std::lock_guard lock(io->mutex);io->ready[readyIndex].state=loaded?2u:0u;if(loaded)io->diskLoads++;}io->wake.notify_all();}}
void CreateEarthImage(App &a,VkFormat format,VkImage &image,VkDeviceMemory &memory,VkImageView &view){VkImageCreateInfo create{VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO};create.imageType=VK_IMAGE_TYPE_2D;create.format=format;create.extent={EarthTileExtent,EarthTileExtent,1};create.mipLevels=1;create.arrayLayers=EarthPhysicalSlots;create.samples=VK_SAMPLE_COUNT_1_BIT;create.tiling=VK_IMAGE_TILING_OPTIMAL;create.usage=VK_IMAGE_USAGE_TRANSFER_DST_BIT|VK_IMAGE_USAGE_SAMPLED_BIT;create.sharingMode=VK_SHARING_MODE_EXCLUSIVE;create.initialLayout=VK_IMAGE_LAYOUT_UNDEFINED;a.Check(vkCreateImage(a.device,&create,nullptr,&image),"Earth physical-pool image failed");VkMemoryRequirements requirements{};vkGetImageMemoryRequirements(a.device,image,&requirements);VkMemoryAllocateInfo allocation{VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO};allocation.allocationSize=requirements.size;allocation.memoryTypeIndex=Memory(a,requirements.memoryTypeBits,VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT);a.Check(vkAllocateMemory(a.device,&allocation,nullptr,&memory),"Earth physical-pool memory failed");a.Check(vkBindImageMemory(a.device,image,memory,0),"Earth physical-pool bind failed");VkImageViewCreateInfo viewCreate{VK_STRUCTURE_TYPE_IMAGE_VIEW_CREATE_INFO};viewCreate.image=image;viewCreate.viewType=VK_IMAGE_VIEW_TYPE_2D_ARRAY;viewCreate.format=format;viewCreate.subresourceRange.aspectMask=VK_IMAGE_ASPECT_COLOR_BIT;viewCreate.subresourceRange.levelCount=1;viewCreate.subresourceRange.layerCount=EarthPhysicalSlots;a.Check(vkCreateImageView(a.device,&viewCreate,nullptr,&view),"Earth physical-pool view failed");}
void CreateEarthVirtualTexture(App &a){
  const std::string path=ModuleDirectory()+"earth-data\\earth_surface_v2.ncvtex";std::ifstream header(path,std::ios::binary);std::array<uint8_t,128> bytes{};bool sourceAvailable=bool(header&&header.read(reinterpret_cast<char*>(bytes.data()),bytes.size())&&std::memcmp(bytes.data(),"NCVTEAR1",8)==0);auto read32=[&](size_t offset){uint32_t value{};std::memcpy(&value,bytes.data()+offset,4);return value;};sourceAvailable=sourceAvailable&&read32(8)==2&&read32(12)==128&&read32(16)==EarthTileSize&&read32(20)==EarthTileGutter&&read32(24)==EarthMaximumLevel&&read32(28)==EarthTileCount&&read32(32)==EarthTileExtent&&read32(36)==EarthTileRecordBytes&&read32(40)==EarthAlbedoTileBytes&&read32(44)==EarthElevationTileBytes&&read32(48)==EarthCloudTileBytes;if(!sourceAvailable)a.Log(NC_LOG_ALWAYS,"Earth dataset missing or invalid: bounded procedural root fallback active");
  CreateEarthImage(a,VK_FORMAT_R8G8B8A8_SRGB,a.earthAlbedo,a.earthAlbedoMemory,a.earthAlbedoView);CreateEarthImage(a,VK_FORMAT_R16_UNORM,a.earthElevation,a.earthElevationMemory,a.earthElevationView);CreateEarthImage(a,VK_FORMAT_R8_UNORM,a.earthCloud,a.earthCloudMemory,a.earthCloudView);
  VkPhysicalDeviceProperties properties{};vkGetPhysicalDeviceProperties(a.physical,&properties);VkSamplerCreateInfo sampler{VK_STRUCTURE_TYPE_SAMPLER_CREATE_INFO};sampler.magFilter=VK_FILTER_LINEAR;sampler.minFilter=VK_FILTER_LINEAR;sampler.mipmapMode=VK_SAMPLER_MIPMAP_MODE_NEAREST;sampler.addressModeU=VK_SAMPLER_ADDRESS_MODE_CLAMP_TO_EDGE;sampler.addressModeV=VK_SAMPLER_ADDRESS_MODE_CLAMP_TO_EDGE;sampler.addressModeW=VK_SAMPLER_ADDRESS_MODE_CLAMP_TO_EDGE;sampler.anisotropyEnable=VK_TRUE;sampler.maxAnisotropy=std::min(8.0f,properties.limits.maxSamplerAnisotropy);sampler.maxLod=0;a.Check(vkCreateSampler(a.device,&sampler,nullptr,&a.earthSampler),"Earth tile sampler failed");
  CreateHostBuffer(a,sizeof(EarthPage)*EarthTileCount,VK_BUFFER_USAGE_STORAGE_BUFFER_BIT,a.earthPageBuffer,a.earthPageMemory,a.earthPageMapped,"Earth page table failed");CreateHostBuffer(a,EarthStagingBytes,VK_BUFFER_USAGE_TRANSFER_SRC_BIT,a.earthStagingBuffer,a.earthStagingMemory,a.earthStagingMapped,"Earth staging buffer failed");a.earthSlotTile.fill(UINT32_MAX);
  for(uint32_t level=0;level<=EarthMaximumLevel;level++)for(uint32_t y=0;y<(1u<<level);y++)for(uint32_t x=0;x<(1u<<(level+1u));x++){const uint32_t tile=EarthTileIndex(level,x,y);a.earthPages[tile].parent=level?EarthTileIndex(level-1,x/2,y/2):tile;}
  for(uint32_t root=0;root<2;root++){auto *rootBytes=reinterpret_cast<uint8_t*>(a.earthStagingMapped)+VkDeviceSize(root)*EarthTileRecordBytes;if(sourceAvailable){if(!ReadEarthTile(path,root,rootBytes))throw std::runtime_error("Earth root tile read failed");}else GenerateEarthFallbackRoot(root,rootBytes);a.earthSlotTile[root]=root;a.earthSlotLastUse[root]=1;a.earthPages[root].slotPlusOne=root+1;a.earthPages[root].readyFrame=1;a.earthPages[root].flags=sourceAvailable?1u:2u;a.earthUploadTiles[root]=root;a.earthUploadSlots[root]=root;a.earthRequested[root]=1;}a.earthPendingUploads=2;std::memcpy(a.earthPageMapped,a.earthPages.data(),sizeof(a.earthPages));
  if(sourceAvailable){a.earthIo=std::make_unique<EarthIoState>();a.earthIo->path=path;a.earthIo->worker=std::thread(EarthIoWorker,a.earthIo.get());}a.earthAvailable=true;char message[256];std::snprintf(message,sizeof message,"Earth VT: source=%s; tiles=%u; pool=%u; poolBytes=%u; uploadBudget=%u; identity=53c8cea5328e20e6",sourceAvailable?"checked-data":"procedural-root",EarthTileCount,EarthPhysicalSlots,EarthPhysicalSlots*EarthTileExtent*EarthTileExtent*7,EarthUploadBudget);a.Log(NC_LOG_RENDERER,message);
}
void DestroyEarthVirtualTexture(App &a){if(a.earthIo){{std::lock_guard lock(a.earthIo->mutex);a.earthIo->stop=true;}a.earthIo->wake.notify_all();if(a.earthIo->worker.joinable())a.earthIo->worker.join();a.earthIo.reset();}DestroyHostBuffer(a,a.earthStagingBuffer,a.earthStagingMemory,a.earthStagingMapped);DestroyHostBuffer(a,a.earthPageBuffer,a.earthPageMemory,a.earthPageMapped);if(a.earthSampler)vkDestroySampler(a.device,a.earthSampler,nullptr);if(a.earthAlbedoView)vkDestroyImageView(a.device,a.earthAlbedoView,nullptr);if(a.earthElevationView)vkDestroyImageView(a.device,a.earthElevationView,nullptr);if(a.earthCloudView)vkDestroyImageView(a.device,a.earthCloudView,nullptr);if(a.earthAlbedo)vkDestroyImage(a.device,a.earthAlbedo,nullptr);if(a.earthElevation)vkDestroyImage(a.device,a.earthElevation,nullptr);if(a.earthCloud)vkDestroyImage(a.device,a.earthCloud,nullptr);if(a.earthAlbedoMemory)vkFreeMemory(a.device,a.earthAlbedoMemory,nullptr);if(a.earthElevationMemory)vkFreeMemory(a.device,a.earthElevationMemory,nullptr);if(a.earthCloudMemory)vkFreeMemory(a.device,a.earthCloudMemory,nullptr);a.earthAvailable=false;}
void RequestEarthTile(App &a,uint32_t tile){if(!a.earthAvailable||tile>=EarthTileCount)return;if(a.earthPages[tile].slotPlusOne){a.earthSlotLastUse[a.earthPages[tile].slotPlusOne-1]=a.frame;return;}if(!a.earthIo||a.earthRequested[tile])return;a.earthRequested[tile]=1;auto &io=*a.earthIo;{std::lock_guard lock(io.mutex);if(io.requestCount==io.requests.size()){a.earthRequested[tile]=0;io.queueDrops++;return;}io.requests[io.requestTail]=tile;io.requestTail=(io.requestTail+1)%io.requests.size();io.requestCount++;a.earthRequests++;}io.wake.notify_one();}
bool EarthViewPupil(double cx,double cy,double cz,double fx,double fy,double fz,double surfaceRadius,double &nx,double &ny,double &nz){const double fl=std::sqrt(fx*fx+fy*fy+fz*fz);if(!(fl>0)||!(surfaceRadius>0))return false;fx/=fl;fy/=fl;fz/=fl;const double b=cx*fx+cy*fy+cz*fz,c=cx*cx+cy*cy+cz*cz-surfaceRadius*surfaceRadius,discriminant=b*b-c;if(discriminant<0)return false;const double root=std::sqrt(discriminant),nearDistance=-b-root,farDistance=-b+root,distance=nearDistance>=0?nearDistance:farDistance>=0?farDistance:-1;if(distance<0)return false;nx=cx+fx*distance;ny=cy+fy*distance;nz=cz+fz*distance;const double length=std::sqrt(nx*nx+ny*ny+nz*nz);if(!(length>0))return false;nx/=length;ny/=length;nz/=length;return true;}
void RequestEarthView(App &a){if(!a.earthAvailable||!a.submission)return;const auto &eye=a.submission->planetaryEyeball;const auto &gpu=a.submission->planetaryGpu;const auto &presentation=a.submission->planetaryPresentation;const bool eyeball=eye.enabled!=0;const bool earthFocused=eyeball?eye.bodyIdLow==6&&eye.bodyIdHigh==0:presentation.bodyIdLow==6&&presentation.bodyIdHigh==0;if(!earthFocused)return;const double cx=eyeball?double(eye.cameraBodyHighX)+eye.cameraBodyLowX:double(gpu.cameraBodyHighX)+gpu.cameraBodyLowX,cy=eyeball?double(eye.cameraBodyHighY)+eye.cameraBodyLowY:double(gpu.cameraBodyHighY)+gpu.cameraBodyLowY,cz=eyeball?double(eye.cameraBodyHighZ)+eye.cameraBodyLowZ:double(gpu.cameraBodyHighZ)+gpu.cameraBodyLowZ;const double length=std::sqrt(cx*cx+cy*cy+cz*cz);if(!(length>0))return;const double fx=eyeball?eye.viewForwardX:gpu.viewForwardX,fy=eyeball?eye.viewForwardY:gpu.viewForwardY,fz=eyeball?eye.viewForwardZ:gpu.viewForwardZ,altitude=eyeball?eye.surfaceAltitudeMetres:gpu.surfaceAltitudeMetres,surfaceRadius=std::max(1.0,length-altitude);double nx=cx/length,ny=cy/length,nz=cz/length;EarthViewPupil(cx,cy,cz,fx,fy,fz,surfaceRadius,nx,ny,nz);const double u=std::atan2(nz,nx)/(2.0*3.14159265358979323846)+.5,v=std::acos(std::clamp(ny,-1.0,1.0))/3.14159265358979323846;uint32_t level=altitude>1000000?1:altitude>100000?2:altitude>10000?3:4;bool desiredResident=true;for(uint32_t current=0;current<=level;current++){const int xt=1<<(current+1),yt=1<<current,cxTile=int(std::floor(u*xt)),cyTile=std::clamp(int(std::floor(v*yt)),0,yt-1);const int requestRadius=current==level?2:0;for(int dy=-requestRadius;dy<=requestRadius;dy++)for(int dx=-requestRadius;dx<=requestRadius;dx++){const int x=(cxTile+(dx%xt)+xt)%xt,y=std::clamp(cyTile+dy,0,yt-1);const uint32_t tile=EarthTileIndex(current,uint32_t(x),uint32_t(y));const bool resident=a.earthPages[tile].slotPlusOne!=0;if(current==level){if(resident)a.earthDemandHits++;else{a.earthDemandMisses++;desiredResident=false;}}RequestEarthTile(a,tile);}}if(!desiredResident)a.earthFallbackFrames++;}
void PrepareEarthUploads(App &a){if(!a.earthAvailable||a.earthPendingUploads)return;RequestEarthView(a);if(!a.earthIo)return;auto &io=*a.earthIo;std::lock_guard lock(io.mutex);for(auto &ready:io.ready){if(ready.state!=2||a.earthPendingUploads>=EarthUploadBudget)continue;uint32_t slot=UINT32_MAX;for(uint32_t candidate=2;candidate<EarthPhysicalSlots;candidate++)if(a.earthSlotTile[candidate]==UINT32_MAX){slot=candidate;break;}if(slot==UINT32_MAX){uint64_t oldest=UINT64_MAX;for(uint32_t candidate=2;candidate<EarthPhysicalSlots;candidate++)if(a.earthSlotLastUse[candidate]<oldest){oldest=a.earthSlotLastUse[candidate];slot=candidate;}const uint32_t evicted=a.earthSlotTile[slot];a.earthPages[evicted].slotPlusOne=0;a.earthPages[evicted].readyFrame=0;a.earthRequested[evicted]=0;a.earthEvictions++;}const uint32_t tile=ready.tile,index=a.earthPendingUploads++;std::memcpy(reinterpret_cast<uint8_t*>(a.earthStagingMapped)+VkDeviceSize(index)*EarthTileRecordBytes,ready.bytes.data(),EarthTileRecordBytes);a.earthSlotTile[slot]=tile;a.earthSlotLastUse[slot]=a.frame;a.earthPages[tile].slotPlusOne=slot+1;a.earthPages[tile].readyFrame=uint32_t(a.frame+1);a.earthPages[tile].flags=1;a.earthUploadTiles[index]=tile;a.earthUploadSlots[index]=slot;a.earthUploads++;ready.state=0;}std::memcpy(a.earthPageMapped,a.earthPages.data(),sizeof(a.earthPages));io.wake.notify_all();}
void RecordEarthUploads(App &a,VkCommandBuffer command){if(!a.earthAvailable||!a.earthPendingUploads)return;VkImageMemoryBarrier before[3]{};VkImage images[3]{a.earthAlbedo,a.earthElevation,a.earthCloud};for(uint32_t i=0;i<3;i++){before[i].sType=VK_STRUCTURE_TYPE_IMAGE_MEMORY_BARRIER;before[i].srcAccessMask=a.earthImagesInitialized?VK_ACCESS_SHADER_READ_BIT:0;before[i].dstAccessMask=VK_ACCESS_TRANSFER_WRITE_BIT;before[i].oldLayout=a.earthImagesInitialized?VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL:VK_IMAGE_LAYOUT_UNDEFINED;before[i].newLayout=VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL;before[i].image=images[i];before[i].subresourceRange.aspectMask=VK_IMAGE_ASPECT_COLOR_BIT;before[i].subresourceRange.levelCount=1;before[i].subresourceRange.layerCount=EarthPhysicalSlots;}vkCmdPipelineBarrier(command,a.earthImagesInitialized?(VK_PIPELINE_STAGE_FRAGMENT_SHADER_BIT|VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT):VK_PIPELINE_STAGE_TOP_OF_PIPE_BIT,VK_PIPELINE_STAGE_TRANSFER_BIT,0,0,nullptr,0,nullptr,3,before);
  for(uint32_t index=0;index<a.earthPendingUploads;index++){const VkDeviceSize base=VkDeviceSize(index)*EarthTileRecordBytes;VkBufferImageCopy copies[3]{};const VkDeviceSize offsets[3]{base,base+EarthAlbedoTileBytes,base+EarthAlbedoTileBytes+EarthElevationTileBytes};for(uint32_t layer=0;layer<3;layer++){copies[layer].bufferOffset=offsets[layer];copies[layer].imageSubresource.aspectMask=VK_IMAGE_ASPECT_COLOR_BIT;copies[layer].imageSubresource.layerCount=1;copies[layer].imageSubresource.baseArrayLayer=a.earthUploadSlots[index];copies[layer].imageExtent={EarthTileExtent,EarthTileExtent,1};vkCmdCopyBufferToImage(command,a.earthStagingBuffer,images[layer],VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,1,&copies[layer]);}}
  VkImageMemoryBarrier after[3]{};for(uint32_t i=0;i<3;i++){after[i]=before[i];after[i].srcAccessMask=VK_ACCESS_TRANSFER_WRITE_BIT;after[i].dstAccessMask=VK_ACCESS_SHADER_READ_BIT;after[i].oldLayout=VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL;after[i].newLayout=VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL;}vkCmdPipelineBarrier(command,VK_PIPELINE_STAGE_TRANSFER_BIT,VK_PIPELINE_STAGE_FRAGMENT_SHADER_BIT|VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,0,0,nullptr,0,nullptr,3,after);a.earthImagesInitialized=true;a.earthPendingUploads=0;}
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
void CreateSceneColor(App &a){
  VkImageCreateInfo image{VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO};image.imageType=VK_IMAGE_TYPE_2D;image.format=App::SceneFormat;image.extent={a.extent.width,a.extent.height,1};image.mipLevels=1;image.arrayLayers=1;image.samples=VK_SAMPLE_COUNT_1_BIT;image.tiling=VK_IMAGE_TILING_OPTIMAL;image.usage=VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT|VK_IMAGE_USAGE_INPUT_ATTACHMENT_BIT;image.sharingMode=VK_SHARING_MODE_EXCLUSIVE;image.initialLayout=VK_IMAGE_LAYOUT_UNDEFINED;
  a.Check(vkCreateImage(a.device,&image,nullptr,&a.sceneColor),"HDR scene-color image failed");VkMemoryRequirements requirements;vkGetImageMemoryRequirements(a.device,a.sceneColor,&requirements);VkMemoryAllocateInfo allocation{VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO};allocation.allocationSize=requirements.size;allocation.memoryTypeIndex=Memory(a,requirements.memoryTypeBits,VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT);a.Check(vkAllocateMemory(a.device,&allocation,nullptr,&a.sceneColorMemory),"HDR scene-color memory failed");a.Check(vkBindImageMemory(a.device,a.sceneColor,a.sceneColorMemory,0),"HDR scene-color bind failed");
  VkImageViewCreateInfo view{VK_STRUCTURE_TYPE_IMAGE_VIEW_CREATE_INFO};view.image=a.sceneColor;view.viewType=VK_IMAGE_VIEW_TYPE_2D;view.format=App::SceneFormat;view.subresourceRange.aspectMask=VK_IMAGE_ASPECT_COLOR_BIT;view.subresourceRange.levelCount=1;view.subresourceRange.layerCount=1;a.Check(vkCreateImageView(a.device,&view,nullptr,&a.sceneColorView),"HDR scene-color view failed");
  VkImageCreateInfo depth=image;depth.format=App::DepthFormat;depth.usage=VK_IMAGE_USAGE_DEPTH_STENCIL_ATTACHMENT_BIT;a.Check(vkCreateImage(a.device,&depth,nullptr,&a.sceneDepth),"scene-depth image failed");vkGetImageMemoryRequirements(a.device,a.sceneDepth,&requirements);allocation.allocationSize=requirements.size;allocation.memoryTypeIndex=Memory(a,requirements.memoryTypeBits,VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT);a.Check(vkAllocateMemory(a.device,&allocation,nullptr,&a.sceneDepthMemory),"scene-depth memory failed");a.Check(vkBindImageMemory(a.device,a.sceneDepth,a.sceneDepthMemory,0),"scene-depth bind failed");view.image=a.sceneDepth;view.format=App::DepthFormat;view.subresourceRange.aspectMask=VK_IMAGE_ASPECT_DEPTH_BIT;a.Check(vkCreateImageView(a.device,&view,nullptr,&a.sceneDepthView),"scene-depth view failed");
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
  {char message[192];std::snprintf(message,sizeof message,"Swapchain: format=%d colorSpace=%d extent=%ux%u; HDR scene format=%d",(int)a.format,(int)sf.colorSpace,a.extent.width,a.extent.height,(int)App::SceneFormat);a.Log(NC_LOG_VULKAN,message);}
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
  CreateSceneColor(a);
  VkAttachmentDescription attachments[3]{};
  attachments[0].format=App::SceneFormat;attachments[0].samples=VK_SAMPLE_COUNT_1_BIT;attachments[0].loadOp=VK_ATTACHMENT_LOAD_OP_CLEAR;attachments[0].storeOp=VK_ATTACHMENT_STORE_OP_DONT_CARE;attachments[0].initialLayout=VK_IMAGE_LAYOUT_UNDEFINED;attachments[0].finalLayout=VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL;
  attachments[1].format=a.format;attachments[1].samples=VK_SAMPLE_COUNT_1_BIT;attachments[1].loadOp=VK_ATTACHMENT_LOAD_OP_DONT_CARE;attachments[1].storeOp=VK_ATTACHMENT_STORE_OP_STORE;attachments[1].initialLayout=VK_IMAGE_LAYOUT_UNDEFINED;attachments[1].finalLayout=VK_IMAGE_LAYOUT_PRESENT_SRC_KHR;
  attachments[2].format=App::DepthFormat;attachments[2].samples=VK_SAMPLE_COUNT_1_BIT;attachments[2].loadOp=VK_ATTACHMENT_LOAD_OP_CLEAR;attachments[2].storeOp=VK_ATTACHMENT_STORE_OP_DONT_CARE;attachments[2].stencilLoadOp=VK_ATTACHMENT_LOAD_OP_DONT_CARE;attachments[2].stencilStoreOp=VK_ATTACHMENT_STORE_OP_DONT_CARE;attachments[2].initialLayout=VK_IMAGE_LAYOUT_UNDEFINED;attachments[2].finalLayout=VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL;
  VkAttachmentReference sceneColor{0,VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL};
  VkAttachmentReference sceneInput{0,VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL};
  VkAttachmentReference swapColor{1,VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL};
  VkAttachmentReference sceneDepth{2,VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL};
  VkSubpassDescription subpasses[2]{};subpasses[0].pipelineBindPoint=VK_PIPELINE_BIND_POINT_GRAPHICS;subpasses[0].colorAttachmentCount=1;subpasses[0].pColorAttachments=&sceneColor;subpasses[0].pDepthStencilAttachment=&sceneDepth;subpasses[1].pipelineBindPoint=VK_PIPELINE_BIND_POINT_GRAPHICS;subpasses[1].inputAttachmentCount=1;subpasses[1].pInputAttachments=&sceneInput;subpasses[1].colorAttachmentCount=1;subpasses[1].pColorAttachments=&swapColor;
  VkSubpassDependency dependencies[3]{};
  dependencies[0].srcSubpass=VK_SUBPASS_EXTERNAL;dependencies[0].dstSubpass=0;dependencies[0].srcStageMask=VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT|VK_PIPELINE_STAGE_LATE_FRAGMENT_TESTS_BIT;dependencies[0].dstStageMask=VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT|VK_PIPELINE_STAGE_EARLY_FRAGMENT_TESTS_BIT;dependencies[0].dstAccessMask=VK_ACCESS_COLOR_ATTACHMENT_WRITE_BIT|VK_ACCESS_DEPTH_STENCIL_ATTACHMENT_WRITE_BIT;
  dependencies[1].srcSubpass=0;dependencies[1].dstSubpass=1;dependencies[1].srcStageMask=VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT;dependencies[1].dstStageMask=VK_PIPELINE_STAGE_FRAGMENT_SHADER_BIT;dependencies[1].srcAccessMask=VK_ACCESS_COLOR_ATTACHMENT_WRITE_BIT;dependencies[1].dstAccessMask=VK_ACCESS_INPUT_ATTACHMENT_READ_BIT;dependencies[1].dependencyFlags=VK_DEPENDENCY_BY_REGION_BIT;
  dependencies[2].srcSubpass=1;dependencies[2].dstSubpass=VK_SUBPASS_EXTERNAL;dependencies[2].srcStageMask=VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT;dependencies[2].dstStageMask=VK_PIPELINE_STAGE_BOTTOM_OF_PIPE_BIT;dependencies[2].srcAccessMask=VK_ACCESS_COLOR_ATTACHMENT_WRITE_BIT;
  VkRenderPassCreateInfo rp{VK_STRUCTURE_TYPE_RENDER_PASS_CREATE_INFO};
  rp.attachmentCount=3;rp.pAttachments=attachments;rp.subpassCount=2;rp.pSubpasses=subpasses;rp.dependencyCount=3;rp.pDependencies=dependencies;
  a.Check(vkCreateRenderPass(a.device, &rp, nullptr, &a.renderPass),
          "render pass failed");
  VkDescriptorSetLayoutBinding binds[19]{};
  for(uint32_t binding=0;binding<7;binding++){binds[binding].binding=binding;binds[binding].descriptorType=VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;binds[binding].descriptorCount=1;binds[binding].stageFlags=binding==0?VK_SHADER_STAGE_VERTEX_BIT|VK_SHADER_STAGE_FRAGMENT_BIT:(binding==1?VK_SHADER_STAGE_VERTEX_BIT|VK_SHADER_STAGE_COMPUTE_BIT:(binding==2?VK_SHADER_STAGE_VERTEX_BIT|VK_SHADER_STAGE_FRAGMENT_BIT|VK_SHADER_STAGE_COMPUTE_BIT:(binding==6?VK_SHADER_STAGE_VERTEX_BIT|VK_SHADER_STAGE_FRAGMENT_BIT|VK_SHADER_STAGE_COMPUTE_BIT:VK_SHADER_STAGE_COMPUTE_BIT)));}
  binds[7].binding=7;binds[7].descriptorType=VK_DESCRIPTOR_TYPE_INPUT_ATTACHMENT;binds[7].descriptorCount=1;binds[7].stageFlags=VK_SHADER_STAGE_FRAGMENT_BIT;
  for(uint32_t binding=8;binding<11;binding++){binds[binding].binding=binding;binds[binding].descriptorType=VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;binds[binding].descriptorCount=1;binds[binding].stageFlags=binding==8?VK_SHADER_STAGE_COMPUTE_BIT:VK_SHADER_STAGE_COMPUTE_BIT|VK_SHADER_STAGE_VERTEX_BIT;}
  binds[11].binding=11;binds[11].descriptorType=VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;binds[11].descriptorCount=1;binds[11].stageFlags=VK_SHADER_STAGE_VERTEX_BIT|VK_SHADER_STAGE_FRAGMENT_BIT;
  binds[12].binding=12;binds[12].descriptorType=VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;binds[12].descriptorCount=1;binds[12].stageFlags=VK_SHADER_STAGE_COMPUTE_BIT|VK_SHADER_STAGE_VERTEX_BIT|VK_SHADER_STAGE_FRAGMENT_BIT;
  binds[13].binding=13;binds[13].descriptorType=VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;binds[13].descriptorCount=1;binds[13].stageFlags=VK_SHADER_STAGE_COMPUTE_BIT;
  binds[14].binding=14;binds[14].descriptorType=VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;binds[14].descriptorCount=1;binds[14].stageFlags=VK_SHADER_STAGE_COMPUTE_BIT;
  for(uint32_t binding=15;binding<18;binding++){binds[binding].binding=binding;binds[binding].descriptorType=VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER;binds[binding].descriptorCount=1;binds[binding].stageFlags=VK_SHADER_STAGE_FRAGMENT_BIT|VK_SHADER_STAGE_COMPUTE_BIT;}
  binds[18].binding=18;binds[18].descriptorType=VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;binds[18].descriptorCount=1;binds[18].stageFlags=VK_SHADER_STAGE_FRAGMENT_BIT|VK_SHADER_STAGE_COMPUTE_BIT;
  VkDescriptorSetLayoutCreateInfo dl{
      VK_STRUCTURE_TYPE_DESCRIPTOR_SET_LAYOUT_CREATE_INFO};
  dl.bindingCount = 19;
  dl.pBindings = binds;
  a.Check(
      vkCreateDescriptorSetLayout(a.device, &dl, nullptr, &a.descriptorLayout),
      "descriptor layout failed");
  VkPipelineLayoutCreateInfo pl{VK_STRUCTURE_TYPE_PIPELINE_LAYOUT_CREATE_INFO};
  pl.setLayoutCount = 1;
  pl.pSetLayouts = &a.descriptorLayout;
  VkPushConstantRange push{};push.stageFlags=VK_SHADER_STAGE_VERTEX_BIT|VK_SHADER_STAGE_FRAGMENT_BIT;push.size=sizeof(NcSolarLighting);pl.pushConstantRangeCount=1;pl.pPushConstantRanges=&push;
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
  VkPipelineDepthStencilStateCreateInfo depth{VK_STRUCTURE_TYPE_PIPELINE_DEPTH_STENCIL_STATE_CREATE_INFO};depth.depthTestEnable=VK_TRUE;depth.depthWriteEnable=VK_TRUE;depth.depthCompareOp=VK_COMPARE_OP_GREATER;depth.minDepthBounds=0;depth.maxDepthBounds=1;
  VkPipelineDepthStencilStateCreateInfo noDepth{VK_STRUCTURE_TYPE_PIPELINE_DEPTH_STENCIL_STATE_CREATE_INFO};noDepth.minDepthBounds=0;noDepth.maxDepthBounds=1;
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
  gp.pDepthStencilState = &depth;
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
  VkPipelineVertexInputStateCreateInfo fullscreenInput{VK_STRUCTURE_TYPE_PIPELINE_VERTEX_INPUT_STATE_CREATE_INFO};
  auto createFullscreenPipeline=[&](const char* fragment,uint32_t subpass,VkPipeline &destination,const char* failure){VkShaderModule fullscreenVs=Shader(a,"shaders/fullscreen.vert.spv"),fullscreenFs{};try{fullscreenFs=Shader(a,fragment);}catch(...){vkDestroyShaderModule(a.device,fullscreenVs,nullptr);throw;}VkPipelineShaderStageCreateInfo fullscreenStages[2]{{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_VERTEX_BIT,fullscreenVs,"main"},{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_FRAGMENT_BIT,fullscreenFs,"main"}};VkGraphicsPipelineCreateInfo create=gp;create.pStages=fullscreenStages;create.pVertexInputState=&fullscreenInput;create.pDepthStencilState=&noDepth;create.subpass=subpass;VkResult result=vkCreateGraphicsPipelines(a.device,{},1,&create,nullptr,&destination);vkDestroyShaderModule(a.device,fullscreenVs,nullptr);vkDestroyShaderModule(a.device,fullscreenFs,nullptr);a.Check(result,failure);};
  createFullscreenPipeline("shaders/space_background.frag.spv",0,a.backgroundPipeline,"space background pipeline failed");
  createFullscreenPipeline("shaders/tone_map.frag.spv",1,a.toneMapPipeline,"tone-map pipeline failed");
  VkShaderModule planetaryVs{},planetaryFs{};
  try{planetaryVs=Shader(a,"shaders/planetary.vert.spv");planetaryFs=Shader(a,"shaders/planetary.frag.spv");}catch(...){if(planetaryVs)vkDestroyShaderModule(a.device,planetaryVs,nullptr);throw;}
  VkPipelineShaderStageCreateInfo planetaryStages[2]{{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_VERTEX_BIT,planetaryVs,"main"},{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_FRAGMENT_BIT,planetaryFs,"main"}};
  VkVertexInputBindingDescription planetaryBinding{0,sizeof(PatchVertex),VK_VERTEX_INPUT_RATE_VERTEX};
  VkVertexInputAttributeDescription planetaryAttribute{0,0,VK_FORMAT_R32G32_SFLOAT,0};
  VkPipelineVertexInputStateCreateInfo planetaryInput{VK_STRUCTURE_TYPE_PIPELINE_VERTEX_INPUT_STATE_CREATE_INFO};planetaryInput.vertexBindingDescriptionCount=1;planetaryInput.pVertexBindingDescriptions=&planetaryBinding;planetaryInput.vertexAttributeDescriptionCount=1;planetaryInput.pVertexAttributeDescriptions=&planetaryAttribute;
  VkPipelineRasterizationStateCreateInfo planetaryRaster=rs;planetaryRaster.cullMode=VK_CULL_MODE_BACK_BIT;planetaryRaster.frontFace=VK_FRONT_FACE_CLOCKWISE;
  VkPipelineColorBlendAttachmentState planetaryBlendAttachment=ca;planetaryBlendAttachment.blendEnable=VK_TRUE;planetaryBlendAttachment.srcColorBlendFactor=VK_BLEND_FACTOR_SRC_ALPHA;planetaryBlendAttachment.dstColorBlendFactor=VK_BLEND_FACTOR_ONE_MINUS_SRC_ALPHA;planetaryBlendAttachment.colorBlendOp=VK_BLEND_OP_ADD;planetaryBlendAttachment.srcAlphaBlendFactor=VK_BLEND_FACTOR_ONE;planetaryBlendAttachment.dstAlphaBlendFactor=VK_BLEND_FACTOR_ONE_MINUS_SRC_ALPHA;planetaryBlendAttachment.alphaBlendOp=VK_BLEND_OP_ADD;VkPipelineColorBlendStateCreateInfo planetaryBlend=cb;planetaryBlend.pAttachments=&planetaryBlendAttachment;
  VkGraphicsPipelineCreateInfo planetaryCreate=gp;planetaryCreate.pStages=planetaryStages;planetaryCreate.pVertexInputState=&planetaryInput;planetaryCreate.pRasterizationState=&planetaryRaster;planetaryCreate.pColorBlendState=&planetaryBlend;
  VkPipeline planetaryPipeline{};VkResult planetaryResult=vkCreateGraphicsPipelines(a.device,{},1,&planetaryCreate,nullptr,&planetaryPipeline);vkDestroyShaderModule(a.device,planetaryVs,nullptr);vkDestroyShaderModule(a.device,planetaryFs,nullptr);if(planetaryResult!=VK_SUCCESS&&planetaryPipeline)vkDestroyPipeline(a.device,planetaryPipeline,nullptr);a.Check(planetaryResult,"planetary pipeline failed");a.planetaryPipeline=planetaryPipeline;
  VkShaderModule distantVs{},distantFs{};try{distantVs=Shader(a,"shaders/distant_planet.vert.spv");distantFs=Shader(a,"shaders/distant_planet.frag.spv");}catch(...){if(distantVs)vkDestroyShaderModule(a.device,distantVs,nullptr);throw;}VkPipelineShaderStageCreateInfo distantStages[2]{{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_VERTEX_BIT,distantVs,"main"},{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_FRAGMENT_BIT,distantFs,"main"}};VkVertexInputBindingDescription distantBinding{0,sizeof(DistantVertex),VK_VERTEX_INPUT_RATE_VERTEX};VkVertexInputAttributeDescription distantAttribute{0,0,VK_FORMAT_R32G32B32_SFLOAT,0};VkPipelineVertexInputStateCreateInfo distantInput{VK_STRUCTURE_TYPE_PIPELINE_VERTEX_INPUT_STATE_CREATE_INFO};distantInput.vertexBindingDescriptionCount=1;distantInput.pVertexBindingDescriptions=&distantBinding;distantInput.vertexAttributeDescriptionCount=1;distantInput.pVertexAttributeDescriptions=&distantAttribute;VkPipelineRasterizationStateCreateInfo distantRaster=rs;distantRaster.cullMode=VK_CULL_MODE_BACK_BIT;distantRaster.frontFace=VK_FRONT_FACE_COUNTER_CLOCKWISE;VkGraphicsPipelineCreateInfo distantCreate=gp;distantCreate.pStages=distantStages;distantCreate.pVertexInputState=&distantInput;distantCreate.pRasterizationState=&distantRaster;distantCreate.pColorBlendState=&planetaryBlend;VkResult distantResult=vkCreateGraphicsPipelines(a.device,{},1,&distantCreate,nullptr,&a.distantPlanetaryPipeline);VkPipelineDepthStencilStateCreateInfo handoffDepth=depth;handoffDepth.depthWriteEnable=VK_FALSE;distantCreate.pDepthStencilState=&handoffDepth;VkResult handoffResult=distantResult==VK_SUCCESS?vkCreateGraphicsPipelines(a.device,{},1,&distantCreate,nullptr,&a.distantPlanetaryHandoffPipeline):distantResult;vkDestroyShaderModule(a.device,distantVs,nullptr);vkDestroyShaderModule(a.device,distantFs,nullptr);a.Check(distantResult,"distant planetary pipeline failed");a.Check(handoffResult,"distant planetary handoff pipeline failed");
  {VkShaderModule environmentVs=Shader(a,"shaders/fullscreen.vert.spv"),environmentFs{};try{environmentFs=Shader(a,"shaders/planetary_environment.frag.spv");}catch(...){vkDestroyShaderModule(a.device,environmentVs,nullptr);throw;}VkPipelineShaderStageCreateInfo environmentStages[2]{{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_VERTEX_BIT,environmentVs,"main"},{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_FRAGMENT_BIT,environmentFs,"main"}};VkGraphicsPipelineCreateInfo environmentCreate=gp;environmentCreate.pStages=environmentStages;environmentCreate.pVertexInputState=&fullscreenInput;environmentCreate.pDepthStencilState=&noDepth;environmentCreate.pColorBlendState=&planetaryBlend;VkResult result=vkCreateGraphicsPipelines(a.device,{},1,&environmentCreate,nullptr,&a.planetaryEnvironmentPipeline);vkDestroyShaderModule(a.device,environmentVs,nullptr);vkDestroyShaderModule(a.device,environmentFs,nullptr);a.Check(result,"planetary environment pipeline failed");}
  VkShaderModule ringVs=Shader(a,"shaders/planetary_ring.vert.spv"),ringFarFs{},ringNearFs{};try{ringFarFs=Shader(a,"shaders/planetary_ring_far.frag.spv");ringNearFs=Shader(a,"shaders/planetary_ring_near.frag.spv");}catch(...){vkDestroyShaderModule(a.device,ringVs,nullptr);if(ringFarFs)vkDestroyShaderModule(a.device,ringFarFs,nullptr);throw;}VkPipelineShaderStageCreateInfo ringStages[2]{{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_VERTEX_BIT,ringVs,"main"},{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_FRAGMENT_BIT,ringFarFs,"main"}};VkPipelineRasterizationStateCreateInfo ringRaster=rs;ringRaster.cullMode=VK_CULL_MODE_NONE;VkGraphicsPipelineCreateInfo ringCreate=gp;ringCreate.pStages=ringStages;ringCreate.pVertexInputState=&distantInput;ringCreate.pRasterizationState=&ringRaster;ringCreate.pColorBlendState=&planetaryBlend;a.Check(vkCreateGraphicsPipelines(a.device,{},1,&ringCreate,nullptr,&a.planetaryRingFarPipeline),"far planetary ring pipeline failed");ringStages[1].module=ringNearFs;a.Check(vkCreateGraphicsPipelines(a.device,{},1,&ringCreate,nullptr,&a.planetaryRingNearPipeline),"near planetary ring pipeline failed");vkDestroyShaderModule(a.device,ringVs,nullptr);vkDestroyShaderModule(a.device,ringFarFs,nullptr);vkDestroyShaderModule(a.device,ringNearFs,nullptr);
  VkShaderModule stellarVs=Shader(a,"shaders/stellar_sun.vert.spv"),stellarFs{};try{stellarFs=Shader(a,"shaders/stellar_sun.frag.spv");}catch(...){vkDestroyShaderModule(a.device,stellarVs,nullptr);throw;}VkPipelineShaderStageCreateInfo stellarStages[2]{{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_VERTEX_BIT,stellarVs,"main"},{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_FRAGMENT_BIT,stellarFs,"main"}};VkGraphicsPipelineCreateInfo stellarCreate=distantCreate;stellarCreate.pStages=stellarStages;stellarCreate.pColorBlendState=&cb;VkResult stellarResult=vkCreateGraphicsPipelines(a.device,{},1,&stellarCreate,nullptr,&a.stellarSunPipeline);vkDestroyShaderModule(a.device,stellarVs,nullptr);vkDestroyShaderModule(a.device,stellarFs,nullptr);a.Check(stellarResult,"stellar Sun pipeline failed");
  VkShaderModule glowVs=Shader(a,"shaders/stellar_glow.vert.spv"),glowFs{};try{glowFs=Shader(a,"shaders/stellar_glow.frag.spv");}catch(...){vkDestroyShaderModule(a.device,glowVs,nullptr);throw;}VkPipelineShaderStageCreateInfo glowStages[2]{{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_VERTEX_BIT,glowVs,"main"},{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_FRAGMENT_BIT,glowFs,"main"}};VkGraphicsPipelineCreateInfo glowCreate=gp;glowCreate.pStages=glowStages;glowCreate.pVertexInputState=&fullscreenInput;glowCreate.pColorBlendState=&planetaryBlend;glowCreate.pDepthStencilState=&noDepth;VkResult glowResult=vkCreateGraphicsPipelines(a.device,{},1,&glowCreate,nullptr,&a.stellarGlowPipeline);vkDestroyShaderModule(a.device,glowVs,nullptr);vkDestroyShaderModule(a.device,glowFs,nullptr);a.Check(glowResult,"stellar glow pipeline failed");
  VkShaderModule planetaryCompute=Shader(a,"shaders/planetary_select.comp.spv");VkPipelineShaderStageCreateInfo planetaryComputeStage{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_COMPUTE_BIT,planetaryCompute,"main"};VkComputePipelineCreateInfo planetaryComputeCreate{VK_STRUCTURE_TYPE_COMPUTE_PIPELINE_CREATE_INFO};planetaryComputeCreate.stage=planetaryComputeStage;planetaryComputeCreate.layout=a.pipelineLayout;VkResult planetaryComputeResult=vkCreateComputePipelines(a.device,{},1,&planetaryComputeCreate,nullptr,&a.planetaryComputePipeline);vkDestroyShaderModule(a.device,planetaryCompute,nullptr);a.Check(planetaryComputeResult,"planetary compute pipeline failed");
  VkShaderModule terrainCompute=Shader(a,"shaders/planetary_terrain_generate.comp.spv");VkPipelineShaderStageCreateInfo terrainComputeStage{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_COMPUTE_BIT,terrainCompute,"main"};VkComputePipelineCreateInfo terrainComputeCreate{VK_STRUCTURE_TYPE_COMPUTE_PIPELINE_CREATE_INFO};terrainComputeCreate.stage=terrainComputeStage;terrainComputeCreate.layout=a.pipelineLayout;VkResult terrainComputeResult=vkCreateComputePipelines(a.device,{},1,&terrainComputeCreate,nullptr,&a.planetaryTerrainPipeline);vkDestroyShaderModule(a.device,terrainCompute,nullptr);a.Check(terrainComputeResult,"planetary terrain compute pipeline failed");
  VkShaderModule eyeballCompute=Shader(a,"shaders/planetary_eyeball_generate.comp.spv");VkPipelineShaderStageCreateInfo eyeballComputeStage{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_COMPUTE_BIT,eyeballCompute,"main"};VkComputePipelineCreateInfo eyeballComputeCreate{VK_STRUCTURE_TYPE_COMPUTE_PIPELINE_CREATE_INFO};eyeballComputeCreate.stage=eyeballComputeStage;eyeballComputeCreate.layout=a.pipelineLayout;VkResult eyeballComputeResult=vkCreateComputePipelines(a.device,{},1,&eyeballComputeCreate,nullptr,&a.planetaryEyeballComputePipeline);vkDestroyShaderModule(a.device,eyeballCompute,nullptr);a.Check(eyeballComputeResult,"planetary eyeball compute pipeline failed");
  VkShaderModule eyeballVs=Shader(a,"shaders/planetary_eyeball.vert.spv"),eyeballFs{};try{eyeballFs=Shader(a,"shaders/planetary.frag.spv");}catch(...){vkDestroyShaderModule(a.device,eyeballVs,nullptr);throw;}VkPipelineShaderStageCreateInfo eyeballStages[2]{{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_VERTEX_BIT,eyeballVs,"main"},{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_FRAGMENT_BIT,eyeballFs,"main"}};VkVertexInputBindingDescription eyeballBinding{0,sizeof(EyeballVertex),VK_VERTEX_INPUT_RATE_VERTEX};VkVertexInputAttributeDescription eyeballAttributes[3]{{0,0,VK_FORMAT_R32G32B32A32_SFLOAT,offsetof(EyeballVertex,positionHeight)},{1,0,VK_FORMAT_R32G32B32A32_SFLOAT,offsetof(EyeballVertex,normal)},{2,0,VK_FORMAT_R32G32B32A32_SFLOAT,offsetof(EyeballVertex,bodyDirection)}};VkPipelineVertexInputStateCreateInfo eyeballInput{VK_STRUCTURE_TYPE_PIPELINE_VERTEX_INPUT_STATE_CREATE_INFO};eyeballInput.vertexBindingDescriptionCount=1;eyeballInput.pVertexBindingDescriptions=&eyeballBinding;eyeballInput.vertexAttributeDescriptionCount=3;eyeballInput.pVertexAttributeDescriptions=eyeballAttributes;VkPipelineRasterizationStateCreateInfo eyeballRaster=rs;eyeballRaster.cullMode=VK_CULL_MODE_NONE;VkPipelineDepthStencilStateCreateInfo eyeballDepth=depth;eyeballDepth.depthCompareOp=VK_COMPARE_OP_GREATER_OR_EQUAL;VkGraphicsPipelineCreateInfo eyeballCreate=gp;eyeballCreate.pStages=eyeballStages;eyeballCreate.pVertexInputState=&eyeballInput;eyeballCreate.pRasterizationState=&eyeballRaster;eyeballCreate.pDepthStencilState=&eyeballDepth;eyeballCreate.pColorBlendState=&planetaryBlend;VkResult eyeballResult=vkCreateGraphicsPipelines(a.device,{},1,&eyeballCreate,nullptr,&a.planetaryEyeballPipeline);vkDestroyShaderModule(a.device,eyeballVs,nullptr);vkDestroyShaderModule(a.device,eyeballFs,nullptr);a.Check(eyeballResult,"planetary eyeball graphics pipeline failed");
  VkShaderModule orbitVs{}, orbitFs{};
  try { orbitVs = Shader(a, "shaders/orbit.vert.spv"); orbitFs = Shader(a, "shaders/orbit.frag.spv"); }
  catch (...) { if (orbitVs) vkDestroyShaderModule(a.device, orbitVs, nullptr); throw; }
  VkPipelineShaderStageCreateInfo orbitStages[2]{{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO, nullptr, 0, VK_SHADER_STAGE_VERTEX_BIT, orbitVs, "main"}, {VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO, nullptr, 0, VK_SHADER_STAGE_FRAGMENT_BIT, orbitFs, "main"}};
  VkVertexInputBindingDescription orbitBinding{0, sizeof(NcOrbitLineVertex), VK_VERTEX_INPUT_RATE_VERTEX};
  VkVertexInputAttributeDescription orbitAttribute{0, 0, VK_FORMAT_R32G32B32_SFLOAT, 0};
  VkPipelineVertexInputStateCreateInfo orbitInput{VK_STRUCTURE_TYPE_PIPELINE_VERTEX_INPUT_STATE_CREATE_INFO}; orbitInput.vertexBindingDescriptionCount = 1; orbitInput.pVertexBindingDescriptions = &orbitBinding; orbitInput.vertexAttributeDescriptionCount = 1; orbitInput.pVertexAttributeDescriptions = &orbitAttribute;
  VkPipelineInputAssemblyStateCreateInfo orbitAssembly{VK_STRUCTURE_TYPE_PIPELINE_INPUT_ASSEMBLY_STATE_CREATE_INFO}; orbitAssembly.topology = VK_PRIMITIVE_TOPOLOGY_LINE_STRIP;
  VkGraphicsPipelineCreateInfo orbitPipeline = gp; orbitPipeline.pStages = orbitStages; orbitPipeline.pVertexInputState = &orbitInput; orbitPipeline.pInputAssemblyState = &orbitAssembly; orbitPipeline.pDepthStencilState=&noDepth;
  VkPipeline activeOrbitPipeline{};
  VkResult activeOrbitResult = vkCreateGraphicsPipelines(a.device, {}, 1, &orbitPipeline, nullptr, &activeOrbitPipeline);
  vkDestroyShaderModule(a.device, orbitVs, nullptr);
  vkDestroyShaderModule(a.device, orbitFs, nullptr);
  if (activeOrbitResult != VK_SUCCESS && activeOrbitPipeline) vkDestroyPipeline(a.device, activeOrbitPipeline, nullptr);
  a.Check(activeOrbitResult, "orbit pipeline failed");
  a.orbitPipeline = activeOrbitPipeline;
  VkShaderModule solarOrbitVs{},solarOrbitFs{};try{solarOrbitVs=Shader(a,"shaders/solar_orbit.vert.spv");solarOrbitFs=Shader(a,"shaders/solar_orbit.frag.spv");}catch(...){if(solarOrbitVs)vkDestroyShaderModule(a.device,solarOrbitVs,nullptr);throw;}VkPipelineShaderStageCreateInfo solarOrbitStages[2]{{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_VERTEX_BIT,solarOrbitVs,"main"},{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_FRAGMENT_BIT,solarOrbitFs,"main"}};VkPipelineInputAssemblyStateCreateInfo solarOrbitAssembly=orbitAssembly;solarOrbitAssembly.topology=VK_PRIMITIVE_TOPOLOGY_LINE_LIST;VkGraphicsPipelineCreateInfo solarOrbitCreate=orbitPipeline;solarOrbitCreate.pStages=solarOrbitStages;solarOrbitCreate.pInputAssemblyState=&solarOrbitAssembly;solarOrbitCreate.pColorBlendState=&planetaryBlend;VkResult solarOrbitResult=vkCreateGraphicsPipelines(a.device,{},1,&solarOrbitCreate,nullptr,&a.solarOrbitPipeline);vkDestroyShaderModule(a.device,solarOrbitVs,nullptr);vkDestroyShaderModule(a.device,solarOrbitFs,nullptr);a.Check(solarOrbitResult,"solar orbit pipeline failed");
  VkPipelineVertexInputStateCreateInfo overlayInput{VK_STRUCTURE_TYPE_PIPELINE_VERTEX_INPUT_STATE_CREATE_INFO};VkPipelineInputAssemblyStateCreateInfo overlayAssembly{VK_STRUCTURE_TYPE_PIPELINE_INPUT_ASSEMBLY_STATE_CREATE_INFO};overlayAssembly.topology=VK_PRIMITIVE_TOPOLOGY_TRIANGLE_LIST;auto createOverlay=[&](const char *vertex,const char *fragment,VkPipeline &destination,const char *failure){VkShaderModule vs=Shader(a,vertex),fs{};try{fs=Shader(a,fragment);}catch(...){vkDestroyShaderModule(a.device,vs,nullptr);throw;}VkPipelineShaderStageCreateInfo stages[2]{{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_VERTEX_BIT,vs,"main"},{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_FRAGMENT_BIT,fs,"main"}};VkGraphicsPipelineCreateInfo create=gp;create.pStages=stages;create.pVertexInputState=&overlayInput;create.pInputAssemblyState=&overlayAssembly;create.pColorBlendState=&planetaryBlend;create.pDepthStencilState=&noDepth;VkResult result=vkCreateGraphicsPipelines(a.device,{},1,&create,nullptr,&destination);vkDestroyShaderModule(a.device,vs,nullptr);vkDestroyShaderModule(a.device,fs,nullptr);a.Check(result,failure);};createOverlay("shaders/solar_marker.vert.spv","shaders/solar_marker.frag.spv",a.solarMarkerPipeline,"solar marker pipeline failed");createOverlay("shaders/solar_label.vert.spv","shaders/solar_label.frag.spv",a.solarLabelPipeline,"solar label pipeline failed");createOverlay("shaders/solar_speed_hud.vert.spv","shaders/solar_speed_hud.frag.spv",a.solarSpeedHudPipeline,"solar speed HUD pipeline failed");
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
    VkImageView attachments[]{a.sceneColorView,a.views[i],a.sceneDepthView};
    VkFramebufferCreateInfo fb{VK_STRUCTURE_TYPE_FRAMEBUFFER_CREATE_INFO};
    fb.renderPass = a.renderPass;
    fb.attachmentCount = 3;
    fb.pAttachments = attachments;
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
void CreateTerrainResidency(App &a) {
  if(a.terrainKeyBuffer)return;
  CreateHostBuffer(a,sizeof(uint32_t)*4*3*TerrainCacheCapacity,VK_BUFFER_USAGE_STORAGE_BUFFER_BIT,a.terrainKeyBuffer,a.terrainKeyMemory,a.terrainKeyMapped,"terrain residency key buffer failed");
  CreateHostBuffer(a,sizeof(float)*2*TerrainGridVertexCount*TerrainCacheCapacity,VK_BUFFER_USAGE_STORAGE_BUFFER_BIT,a.terrainSampleBuffer,a.terrainSampleMemory,a.terrainSampleMapped,"terrain residency sample buffer failed");
  CreateHostBuffer(a,sizeof(uint32_t)*2*GpuPatchCapacity,VK_BUFFER_USAGE_STORAGE_BUFFER_BIT,a.terrainPatchSlotBuffer,a.terrainPatchSlotMemory,a.terrainPatchSlotMapped,"terrain patch-slot buffer failed");
  char message[192];std::snprintf(message,sizeof message,"Terrain residency: capacity=%u; samples=%u; persistentBytes=%zu",TerrainCacheCapacity,TerrainGridVertexCount,size_t(sizeof(uint32_t)*4*3*TerrainCacheCapacity+sizeof(float)*2*TerrainGridVertexCount*TerrainCacheCapacity+sizeof(uint32_t)*2*GpuPatchCapacity));a.Log(NC_LOG_RENDERER,message);
}
void DestroyTerrainResidency(App &a) {
  DestroyHostBuffer(a,a.terrainPatchSlotBuffer,a.terrainPatchSlotMemory,a.terrainPatchSlotMapped);
  DestroyHostBuffer(a,a.terrainSampleBuffer,a.terrainSampleMemory,a.terrainSampleMapped);
  DestroyHostBuffer(a,a.terrainKeyBuffer,a.terrainKeyMemory,a.terrainKeyMapped);
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
  DestroyHostBuffer(a,a.planetaryPresentationBuffer,a.planetaryPresentationMemory,a.planetaryPresentationMapped);
  DestroyHostBuffer(a,a.planetaryEnvironmentBuffer,a.planetaryEnvironmentMemory,a.planetaryEnvironmentMapped);
  DestroyHostBuffer(a,a.planetaryEyeballInputBuffer,a.planetaryEyeballInputMemory,a.planetaryEyeballInputMapped);
  DestroyHostBuffer(a,a.planetaryEyeballIndirectBuffer,a.planetaryEyeballIndirectMemory,a.planetaryEyeballIndirectMapped);
  a.validationCpuOracle.clear();a.gpuFrameSubmitted=false;a.hasGpuTelemetry=false;a.hasParityResult=false;a.timestampFrameSubmitted=false;a.hasEyeballValidation=false;
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
  CreateHostBuffer(a,sizeof(uint32_t)*4*GpuNodeEntryCapacity,VK_BUFFER_USAGE_STORAGE_BUFFER_BIT,a.gpuNodeBuffer,a.gpuNodeMemory,a.gpuNodeMapped,"planetary GPU sparse active-hash buffer failed");
  std::memset(a.gpuNodeMapped,0,sizeof(uint32_t)*4*GpuNodeEntryCapacity);
  CreateHostBuffer(a,sizeof(GpuPlanetaryControl),VK_BUFFER_USAGE_STORAGE_BUFFER_BIT|VK_BUFFER_USAGE_INDIRECT_BUFFER_BIT,a.gpuControlBuffer,a.gpuControlMemory,a.gpuControlMapped,"planetary GPU control buffer failed");
  CreateHostBuffer(a,sizeof(NcPlanetaryPresentation)*10,VK_BUFFER_USAGE_STORAGE_BUFFER_BIT,a.planetaryPresentationBuffer,a.planetaryPresentationMemory,a.planetaryPresentationMapped,"planetary presentation buffer failed");
  CreateHostBuffer(a,sizeof(NcPlanetaryEnvironment),VK_BUFFER_USAGE_STORAGE_BUFFER_BIT,a.planetaryEnvironmentBuffer,a.planetaryEnvironmentMemory,a.planetaryEnvironmentMapped,"planetary environment buffer failed");
  CreateHostBuffer(a,sizeof(NcPlanetaryEyeball),VK_BUFFER_USAGE_STORAGE_BUFFER_BIT,a.planetaryEyeballInputBuffer,a.planetaryEyeballInputMemory,a.planetaryEyeballInputMapped,"planetary eyeball input buffer failed");
  CreateHostBuffer(a,sizeof(VkDrawIndexedIndirectCommand),VK_BUFFER_USAGE_STORAGE_BUFFER_BIT|VK_BUFFER_USAGE_INDIRECT_BUFFER_BIT,a.planetaryEyeballIndirectBuffer,a.planetaryEyeballIndirectMemory,a.planetaryEyeballIndirectMapped,"planetary eyeball indirect buffer failed");
  CreateTerrainResidency(a);
  VkDescriptorPoolSize ps[3]{{VK_DESCRIPTOR_TYPE_STORAGE_BUFFER,15},{VK_DESCRIPTOR_TYPE_INPUT_ATTACHMENT,1},{VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER,3}};
  VkDescriptorPoolCreateInfo pi{VK_STRUCTURE_TYPE_DESCRIPTOR_POOL_CREATE_INFO};
  pi.maxSets = 1;
  pi.poolSizeCount = 3;
  pi.pPoolSizes = ps;
  a.Check(vkCreateDescriptorPool(a.device, &pi, nullptr, &a.descriptorPool),
          "descriptor pool failed");
  VkDescriptorSetAllocateInfo si{
      VK_STRUCTURE_TYPE_DESCRIPTOR_SET_ALLOCATE_INFO};
  si.descriptorPool = a.descriptorPool;
  si.descriptorSetCount = 1;
  si.pSetLayouts = &a.descriptorLayout;
  a.Check(vkAllocateDescriptorSets(a.device, &si, &a.descriptor),
          "descriptor set failed");
  VkDescriptorBufferInfo infos[14]{{a.submissionBuffer,0,a.submissionSize},{a.patchBuffer,0,a.patchSize},{a.gpuInputBuffer,0,sizeof(NcPlanetaryGpuConstants)},{a.gpuWorkBuffer,0,sizeof(uint32_t)*4*GpuPatchCapacity*2},{a.gpuNodeBuffer,0,sizeof(uint32_t)*4*GpuNodeEntryCapacity},{a.gpuControlBuffer,0,sizeof(GpuPlanetaryControl)},{a.planetaryPresentationBuffer,0,sizeof(NcPlanetaryPresentation)*10},{a.terrainKeyBuffer,0,sizeof(uint32_t)*4*3*TerrainCacheCapacity},{a.terrainSampleBuffer,0,sizeof(float)*2*TerrainGridVertexCount*TerrainCacheCapacity},{a.terrainPatchSlotBuffer,0,sizeof(uint32_t)*2*GpuPatchCapacity},{a.planetaryEnvironmentBuffer,0,sizeof(NcPlanetaryEnvironment)},{a.planetaryEyeballInputBuffer,0,sizeof(NcPlanetaryEyeball)},{a.planetaryEyeball.vb,0,sizeof(EyeballVertex)*EyeballVertexCount},{a.planetaryEyeballIndirectBuffer,0,sizeof(VkDrawIndexedIndirectCommand)}};
  const uint32_t storageBindings[14]{0,1,2,3,4,5,6,8,9,10,11,12,13,14};VkWriteDescriptorSet writes[14]{};for(uint32_t index=0;index<14;index++){writes[index].sType=VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET;writes[index].dstSet=a.descriptor;writes[index].dstBinding=storageBindings[index];writes[index].descriptorCount=1;writes[index].descriptorType=VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;writes[index].pBufferInfo=&infos[index];}
  vkUpdateDescriptorSets(a.device,14,writes,0,nullptr);
  VkDescriptorImageInfo sceneInput{};sceneInput.imageView=a.sceneColorView;sceneInput.imageLayout=VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL;VkWriteDescriptorSet sceneWrite{VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET};sceneWrite.dstSet=a.descriptor;sceneWrite.dstBinding=7;sceneWrite.descriptorCount=1;sceneWrite.descriptorType=VK_DESCRIPTOR_TYPE_INPUT_ATTACHMENT;sceneWrite.pImageInfo=&sceneInput;vkUpdateDescriptorSets(a.device,1,&sceneWrite,0,nullptr);
  if(a.earthAvailable){VkDescriptorBufferInfo pageInfo{a.earthPageBuffer,0,sizeof(EarthPage)*EarthTileCount};VkWriteDescriptorSet pageWrite{VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET};pageWrite.dstSet=a.descriptor;pageWrite.dstBinding=18;pageWrite.descriptorCount=1;pageWrite.descriptorType=VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;pageWrite.pBufferInfo=&pageInfo;VkDescriptorImageInfo earthImages[3]{{a.earthSampler,a.earthAlbedoView,VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL},{a.earthSampler,a.earthElevationView,VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL},{a.earthSampler,a.earthCloudView,VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL}};VkWriteDescriptorSet earthWrites[3]{};for(uint32_t index=0;index<3;index++){earthWrites[index].sType=VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET;earthWrites[index].dstSet=a.descriptor;earthWrites[index].dstBinding=15+index;earthWrites[index].descriptorCount=1;earthWrites[index].descriptorType=VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER;earthWrites[index].pImageInfo=&earthImages[index];}vkUpdateDescriptorSets(a.device,1,&pageWrite,0,nullptr);vkUpdateDescriptorSets(a.device,3,earthWrites,0,nullptr);}
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
  auto gpuInput=a.submission->planetaryGpu;gpuInput.terrainFrame=static_cast<uint32_t>(++a.frame);if(a.submission->planetaryMode==NC_PLANETARY_CPU_REFERENCE)gpuInput.terrainVersion=0;std::memcpy(a.gpuInputMapped,&gpuInput,sizeof(gpuInput));
  if(a.submission->distantBodyCount)std::memcpy(a.planetaryPresentationMapped,a.submission->distantBodies,sizeof(NcPlanetaryPresentation)*a.submission->distantBodyCount);else std::memcpy(a.planetaryPresentationMapped,&a.submission->planetaryPresentation,sizeof(NcPlanetaryPresentation));
  std::memcpy(a.planetaryEnvironmentMapped,&a.submission->planetaryEnvironment,sizeof(NcPlanetaryEnvironment));
  auto eyeballInput=a.submission->planetaryEyeball;char earthDebug[16]{};if(GetEnvironmentVariableA("NOVACORE_EARTH_DEBUG",earthDebug,sizeof earthDebug)){const auto mode=std::strtoul(earthDebug,nullptr,10);eyeballInput.reserved0=mode<=11?static_cast<uint32_t>(mode):0u;}std::memcpy(a.planetaryEyeballInputMapped,&eyeballInput,sizeof(eyeballInput));
  if(a.submission->planetaryPresentation.enabled&&a.submission->planetaryPresentation.regime==NC_PLANETARY_DISTANT_ONLY)std::memset(a.gpuControlMapped,0,sizeof(GpuPlanetaryControl));
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
  VkQueryPoolCreateInfo qi{VK_STRUCTURE_TYPE_QUERY_POOL_CREATE_INFO};qi.queryType=VK_QUERY_TYPE_TIMESTAMP;qi.queryCount=App::TimestampCount;a.Check(vkCreateQueryPool(a.device,&qi,nullptr,&a.timestampQueries),"timestamp query pool failed");
}
void Record(App &a, uint32_t image) {
  auto c = a.commands[image];
  vkResetCommandBuffer(c, 0);
  VkCommandBufferBeginInfo bi{VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO};
  a.Check(vkBeginCommandBuffer(c, &bi), "command begin failed");
  RecordEarthUploads(a,c);
  vkCmdResetQueryPool(c,a.timestampQueries,0,App::TimestampCount);vkCmdWriteTimestamp(c,VK_PIPELINE_STAGE_TOP_OF_PIPE_BIT,a.timestampQueries,0);
  const auto &presentation=a.submission->planetaryPresentation;const bool handoff=presentation.enabled!=0;const bool detailedPresentation=!handoff||presentation.regime!=NC_PLANETARY_DISTANT_ONLY;const bool distantPresentation=handoff&&presentation.regime!=NC_PLANETARY_DETAILED_ONLY&&presentation.distantAlpha>0;const bool eyeball=a.submission->planetaryEyeball.enabled!=0;const bool regional=detailedPresentation&&(!eyeball||a.submission->planetaryEyeball.regionalAlpha>0);const bool gpuPlanetary=regional&&a.submission->planetaryMode!=NC_PLANETARY_CPU_REFERENCE;
  if(distantPresentation||detailedPresentation||eyeball){VkMemoryBarrier hostBarrier{VK_STRUCTURE_TYPE_MEMORY_BARRIER};hostBarrier.srcAccessMask=VK_ACCESS_HOST_WRITE_BIT;hostBarrier.dstAccessMask=VK_ACCESS_SHADER_READ_BIT;VkPipelineStageFlags readers=VK_PIPELINE_STAGE_VERTEX_SHADER_BIT|((gpuPlanetary||eyeball)?VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT:0);vkCmdPipelineBarrier(c,VK_PIPELINE_STAGE_HOST_BIT,readers,0,1,&hostBarrier,0,nullptr,0,nullptr);}
  if(gpuPlanetary){vkCmdBindDescriptorSets(c,VK_PIPELINE_BIND_POINT_COMPUTE,a.pipelineLayout,0,1,&a.descriptor,0,nullptr);vkCmdBindPipeline(c,VK_PIPELINE_BIND_POINT_COMPUTE,a.planetaryComputePipeline);vkCmdDispatch(c,1,1,1);VkMemoryBarrier selectionBarrier{VK_STRUCTURE_TYPE_MEMORY_BARRIER};selectionBarrier.srcAccessMask=VK_ACCESS_SHADER_WRITE_BIT;selectionBarrier.dstAccessMask=VK_ACCESS_SHADER_READ_BIT|VK_ACCESS_SHADER_WRITE_BIT;vkCmdPipelineBarrier(c,VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,0,1,&selectionBarrier,0,nullptr,0,nullptr);vkCmdBindPipeline(c,VK_PIPELINE_BIND_POINT_COMPUTE,a.planetaryTerrainPipeline);vkCmdDispatch(c,GpuPatchCapacity,1,1);VkMemoryBarrier computeBarrier{VK_STRUCTURE_TYPE_MEMORY_BARRIER};computeBarrier.srcAccessMask=VK_ACCESS_SHADER_WRITE_BIT;computeBarrier.dstAccessMask=VK_ACCESS_INDIRECT_COMMAND_READ_BIT|VK_ACCESS_SHADER_READ_BIT;VkPipelineStageFlags consumers=VK_PIPELINE_STAGE_DRAW_INDIRECT_BIT|VK_PIPELINE_STAGE_VERTEX_SHADER_BIT;if(a.submission->planetaryMode==NC_PLANETARY_CPU_GPU_VALIDATION){computeBarrier.dstAccessMask|=VK_ACCESS_HOST_READ_BIT;consumers|=VK_PIPELINE_STAGE_HOST_BIT;}vkCmdPipelineBarrier(c,VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,consumers,0,1,&computeBarrier,0,nullptr,0,nullptr);}
  vkCmdWriteTimestamp(c,VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,a.timestampQueries,1);
  if(eyeball){vkCmdBindDescriptorSets(c,VK_PIPELINE_BIND_POINT_COMPUTE,a.pipelineLayout,0,1,&a.descriptor,0,nullptr);vkCmdBindPipeline(c,VK_PIPELINE_BIND_POINT_COMPUTE,a.planetaryEyeballComputePipeline);vkCmdDispatch(c,(EyeballVertexCount+255u)/256u,1,1);VkMemoryBarrier eyeBarrier{VK_STRUCTURE_TYPE_MEMORY_BARRIER};eyeBarrier.srcAccessMask=VK_ACCESS_SHADER_WRITE_BIT;eyeBarrier.dstAccessMask=VK_ACCESS_VERTEX_ATTRIBUTE_READ_BIT|VK_ACCESS_INDIRECT_COMMAND_READ_BIT|VK_ACCESS_HOST_READ_BIT;vkCmdPipelineBarrier(c,VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,VK_PIPELINE_STAGE_VERTEX_INPUT_BIT|VK_PIPELINE_STAGE_DRAW_INDIRECT_BIT|VK_PIPELINE_STAGE_HOST_BIT,0,1,&eyeBarrier,0,nullptr,0,nullptr);}
  vkCmdWriteTimestamp(c,VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,a.timestampQueries,2);
  NcSolarLighting lighting=a.submission->solarLighting;if(!lighting.enabled){lighting.exposure=1;lighting.ambientFloor=.025f;lighting.photosphereR=1;lighting.photosphereG=.91f;lighting.photosphereB=.68f;lighting.sourceRadiance=32;}
  vkCmdPushConstants(c,a.pipelineLayout,VK_SHADER_STAGE_VERTEX_BIT|VK_SHADER_STAGE_FRAGMENT_BIT,0,sizeof(lighting),&lighting);
  VkClearValue clears[3]{};clears[0].color={{0,0,0,1}};clears[1].color={{0,0,0,1}};clears[2].depthStencil={0,0};
  VkRenderPassBeginInfo rp{VK_STRUCTURE_TYPE_RENDER_PASS_BEGIN_INFO};
  rp.renderPass = a.renderPass;
  rp.framebuffer = a.framebuffers[image];
  rp.renderArea = {{0, 0}, a.extent};
  rp.clearValueCount = 3;
  rp.pClearValues = clears;
  vkCmdBeginRenderPass(c, &rp, VK_SUBPASS_CONTENTS_INLINE);
  vkCmdBindPipeline(c,VK_PIPELINE_BIND_POINT_GRAPHICS,a.backgroundPipeline);
  vkCmdBindDescriptorSets(c,VK_PIPELINE_BIND_POINT_GRAPHICS,a.pipelineLayout,0,1,&a.descriptor,0,nullptr);
  vkCmdDraw(c,3,1,0,0);
  vkCmdWriteTimestamp(c,VK_PIPELINE_STAGE_FRAGMENT_SHADER_BIT,a.timestampQueries,3);
  if(a.submission->planetaryEnvironment.enabledLayers){vkCmdBindPipeline(c,VK_PIPELINE_BIND_POINT_GRAPHICS,a.planetaryEnvironmentPipeline);vkCmdDraw(c,3,1,0,0);}
  vkCmdWriteTimestamp(c,VK_PIPELINE_STAGE_FRAGMENT_SHADER_BIT,a.timestampQueries,4);
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
  const uint32_t distantCount=a.submission->distantBodyCount?a.submission->distantBodyCount:(distantPresentation?1u:0u);const bool solarOverlay=a.submission->distantBodyCount==10&&a.submission->orbitVertexCount==2304;
  if(solarOverlay&&a.submission->orbitVertexCount>=2&&a.orbitBuffer){VkDeviceSize offset=0;vkCmdBindPipeline(c,VK_PIPELINE_BIND_POINT_GRAPHICS,a.solarOrbitPipeline);vkCmdBindVertexBuffers(c,0,1,&a.orbitBuffer,&offset);vkCmdDraw(c,a.submission->orbitVertexCount,1,0,0);}
  if(solarOverlay){vkCmdBindPipeline(c,VK_PIPELINE_BIND_POINT_GRAPHICS,a.stellarGlowPipeline);vkCmdBindDescriptorSets(c,VK_PIPELINE_BIND_POINT_GRAPHICS,a.pipelineLayout,0,1,&a.descriptor,0,nullptr);vkCmdDraw(c,6,distantCount,0,0);VkDeviceSize offset=0;vkCmdBindPipeline(c,VK_PIPELINE_BIND_POINT_GRAPHICS,a.planetaryRingFarPipeline);vkCmdBindVertexBuffers(c,0,1,&a.planetaryRing.vb,&offset);vkCmdBindIndexBuffer(c,a.planetaryRing.ib,0,VK_INDEX_TYPE_UINT32);vkCmdDrawIndexed(c,a.planetaryRing.indices,distantCount,0,0,0);}
  if(distantCount){VkDeviceSize offset=0;vkCmdBindDescriptorSets(c,VK_PIPELINE_BIND_POINT_GRAPHICS,a.pipelineLayout,0,1,&a.descriptor,0,nullptr);vkCmdBindVertexBuffers(c,0,1,&a.distantPlanetary.vb,&offset);vkCmdBindIndexBuffer(c,a.distantPlanetary.ib,0,VK_INDEX_TYPE_UINT32);const uint32_t firstUnfocused=handoff?1u:0u;if(distantCount>firstUnfocused){vkCmdBindPipeline(c,VK_PIPELINE_BIND_POINT_GRAPHICS,a.distantPlanetaryPipeline);vkCmdDrawIndexed(c,a.distantPlanetary.indices,distantCount-firstUnfocused,0,0,firstUnfocused);}if(handoff&&distantPresentation){vkCmdBindPipeline(c,VK_PIPELINE_BIND_POINT_GRAPHICS,detailedPresentation?a.distantPlanetaryHandoffPipeline:a.distantPlanetaryPipeline);vkCmdDrawIndexed(c,a.distantPlanetary.indices,1,0,0,0);}}
  if(regional&&(a.submission->planetaryPatchCount||gpuPlanetary)){VkDeviceSize offset=0;vkCmdBindPipeline(c,VK_PIPELINE_BIND_POINT_GRAPHICS,a.planetaryPipeline);vkCmdBindVertexBuffers(c,0,1,&a.planetaryPatch.vb,&offset);vkCmdBindIndexBuffer(c,a.planetaryPatch.ib,0,VK_INDEX_TYPE_UINT32);if(gpuPlanetary)vkCmdDrawIndexedIndirect(c,a.gpuControlBuffer,0,1,sizeof(VkDrawIndexedIndirectCommand));else vkCmdDrawIndexed(c,a.planetaryPatch.indices,a.submission->planetaryPatchCount,0,0,0);}
  vkCmdWriteTimestamp(c,VK_PIPELINE_STAGE_VERTEX_INPUT_BIT,a.timestampQueries,5);
  if(eyeball){VkDeviceSize offset=0;vkCmdBindPipeline(c,VK_PIPELINE_BIND_POINT_GRAPHICS,a.planetaryEyeballPipeline);vkCmdBindVertexBuffers(c,0,1,&a.planetaryEyeball.vb,&offset);vkCmdBindIndexBuffer(c,a.planetaryEyeball.ib,0,VK_INDEX_TYPE_UINT32);vkCmdDrawIndexedIndirect(c,a.planetaryEyeballIndirectBuffer,0,1,sizeof(VkDrawIndexedIndirectCommand));}
  vkCmdWriteTimestamp(c,VK_PIPELINE_STAGE_FRAGMENT_SHADER_BIT,a.timestampQueries,6);
  if(solarOverlay){VkDeviceSize offset=0;vkCmdBindPipeline(c,VK_PIPELINE_BIND_POINT_GRAPHICS,a.planetaryRingNearPipeline);vkCmdBindVertexBuffers(c,0,1,&a.planetaryRing.vb,&offset);vkCmdBindIndexBuffer(c,a.planetaryRing.ib,0,VK_INDEX_TYPE_UINT32);vkCmdDrawIndexed(c,a.planetaryRing.indices,distantCount,0,0,0);}
  if (!solarOverlay && a.submission->orbitVertexCount >= 2 && a.orbitBuffer) { VkDeviceSize offset = 0; vkCmdBindPipeline(c, VK_PIPELINE_BIND_POINT_GRAPHICS, a.orbitPipeline); vkCmdBindVertexBuffers(c, 0, 1, &a.orbitBuffer, &offset); vkCmdDraw(c, a.submission->orbitVertexCount, 1, 0, 0); }
  if (a.submission->previousOrbitVertexCount >= 2 && a.previousOrbitBuffer) { VkDeviceSize offset = 0; vkCmdBindPipeline(c, VK_PIPELINE_BIND_POINT_GRAPHICS, a.previousOrbitPipeline); vkCmdBindVertexBuffers(c, 0, 1, &a.previousOrbitBuffer, &offset); vkCmdDraw(c, a.submission->previousOrbitVertexCount, 1, 0, 0); }
  if (a.submission->bodyForwardVertexCount == 2 && a.bodyForwardBuffer) { VkDeviceSize offset = 0; vkCmdBindPipeline(c, VK_PIPELINE_BIND_POINT_GRAPHICS, a.bodyForwardPipeline); vkCmdBindVertexBuffers(c, 0, 1, &a.bodyForwardBuffer, &offset); vkCmdDraw(c, 2, 1, 0, 0); }
  if (a.submission->targetDirectionVertexCount == 2 && a.targetDirectionBuffer) { VkDeviceSize offset = 0; vkCmdBindPipeline(c, VK_PIPELINE_BIND_POINT_GRAPHICS, a.targetDirectionPipeline); vkCmdBindVertexBuffers(c, 0, 1, &a.targetDirectionBuffer, &offset); vkCmdDraw(c, 2, 1, 0, 0); }
  if(solarOverlay){VkDeviceSize offset=0;vkCmdBindPipeline(c,VK_PIPELINE_BIND_POINT_GRAPHICS,a.stellarSunPipeline);vkCmdBindVertexBuffers(c,0,1,&a.stellarSun.vb,&offset);vkCmdBindIndexBuffer(c,a.stellarSun.ib,0,VK_INDEX_TYPE_UINT32);vkCmdDrawIndexed(c,a.stellarSun.indices,distantCount,0,0,0);vkCmdBindDescriptorSets(c,VK_PIPELINE_BIND_POINT_GRAPHICS,a.pipelineLayout,0,1,&a.descriptor,0,nullptr);vkCmdBindPipeline(c,VK_PIPELINE_BIND_POINT_GRAPHICS,a.solarMarkerPipeline);vkCmdDraw(c,24,10,0,0);vkCmdBindPipeline(c,VK_PIPELINE_BIND_POINT_GRAPHICS,a.solarLabelPipeline);vkCmdDraw(c,42,10,0,0);vkCmdBindPipeline(c,VK_PIPELINE_BIND_POINT_GRAPHICS,a.solarSpeedHudPipeline);vkCmdDraw(c,210,1,0,0);}
  vkCmdWriteTimestamp(c,VK_PIPELINE_STAGE_BOTTOM_OF_PIPE_BIT,a.timestampQueries,7);
  vkCmdNextSubpass(c,VK_SUBPASS_CONTENTS_INLINE);
  vkCmdBindPipeline(c,VK_PIPELINE_BIND_POINT_GRAPHICS,a.toneMapPipeline);
  vkCmdBindDescriptorSets(c,VK_PIPELINE_BIND_POINT_GRAPHICS,a.pipelineLayout,0,1,&a.descriptor,0,nullptr);
  vkCmdDraw(c,3,1,0,0);
  vkCmdWriteTimestamp(c,VK_PIPELINE_STAGE_BOTTOM_OF_PIPE_BIT,a.timestampQueries,8);
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
  if(!a.hasGpuTelemetry||std::memcmp(&telemetry,&a.lastGpuTelemetry,sizeof(telemetry))!=0){char message[512];std::snprintf(message,sizeof message,"GPU planetary: roots=%u; candidates=%u; refined=%u; culled=%u; frustum=%u; horizon=%u; active=%u; splits=%u; merges=%u; balanced=%u; parentFallbacks=%u; pendingChildren=%u; min=%u; max=%u; overflow=%u; indirectInstances=%u; terrainHits=%u; terrainMisses=%u; terrainGenerated=%u; terrainEvictions=%u; terrainResident=%u/%u",telemetry.roots,telemetry.candidates,telemetry.refined,telemetry.culled,telemetry.frustumCulled,telemetry.horizonCulled,telemetry.active,telemetry.splits,telemetry.merges,telemetry.balanced,telemetry.parentFallbacks,telemetry.pendingChildren,telemetry.minimumLevel,telemetry.maximumLevel,telemetry.overflow,telemetry.draw.instanceCount,telemetry.cacheHits,telemetry.cacheMisses,telemetry.cacheGenerated,telemetry.cacheEvictions,telemetry.cacheResident,telemetry.cacheCapacity);a.Log(NC_LOG_ALWAYS,message);a.lastGpuTelemetry=telemetry;a.hasGpuTelemetry=true;}
  if(a.submission->planetaryMode!=NC_PLANETARY_CPU_GPU_VALIDATION||a.validationCpuOracle.empty())return;
  const auto gpuCount=std::min<uint32_t>(telemetry.active,GpuPatchCapacity);auto cpu=CanonicalPatches(a.validationCpuOracle.data(),(uint32_t)a.validationCpuOracle.size());auto gpu=CanonicalPatches(static_cast<const NcPlanetaryPatch *>(a.patchMapped),gpuCount);const auto cpuHash=PatchHash(cpu),gpuHash=PatchHash(gpu);uint32_t cpuMinimum=0,cpuMaximum=0;if(!cpu.empty()){cpuMinimum=cpuMaximum=cpu.front().level;for(const auto &patch:cpu){cpuMinimum=std::min(cpuMinimum,patch.level);cpuMaximum=std::max(cpuMaximum,patch.level);}}const bool match=telemetry.roots==6&&telemetry.overflow==0&&telemetry.draw.instanceCount==telemetry.active&&telemetry.minimumLevel==cpuMinimum&&telemetry.maximumLevel==cpuMaximum&&cpu==gpu;
  if(!a.hasParityResult||!match||cpuHash!=a.lastCpuHash||gpuHash!=a.lastGpuHash){char message[256];std::snprintf(message,sizeof message,"CPU/GPU planetary parity: match=%s; cpu=%zu; gpu=%zu; cpuHash=0x%016llX; gpuHash=0x%016llX",match?"true":"false",cpu.size(),gpu.size(),(unsigned long long)cpuHash,(unsigned long long)gpuHash);a.Log(match?NC_LOG_ALWAYS:NC_LOG_VALIDATION,message);a.lastCpuHash=cpuHash;a.lastGpuHash=gpuHash;a.hasParityResult=true;}
}
void InspectGpuTimings(App &a){
  if(!a.timestampFrameSubmitted||!a.timestampQueries)return;std::array<uint64_t,App::TimestampCount> ticks{};const auto result=vkGetQueryPoolResults(a.device,a.timestampQueries,0,App::TimestampCount,sizeof(ticks),ticks.data(),sizeof(uint64_t),VK_QUERY_RESULT_64_BIT);if(result!=VK_SUCCESS)return;
  std::array<double,App::TimestampCount> values{};const double scale=double(a.timestampPeriodNanoseconds)/1e6;const bool eyeball=a.submission&&a.submission->planetaryEyeball.enabled!=0;values[0]=(ticks[8]-ticks[0])*scale;values[1]=eyeball?(ticks[2]-ticks[1])*scale:0;values[2]=eyeball?(ticks[6]-ticks[5])*scale:0;values[3]=(ticks[3]-ticks[2])*scale;values[4]=(ticks[4]-ticks[3])*scale;values[5]=(ticks[7]-ticks[0])*scale;values[6]=(ticks[8]-ticks[7])*scale;values[7]=(ticks[1]-ticks[0])*scale;values[8]=(ticks[7]-ticks[4])*scale;for(uint32_t i=0;i<App::TimestampCount;i++)a.timestampAccumulatedMs[i]+=values[i];a.timestampSampleCount++;
  if(a.timestampSampleCount==1||a.timestampSampleCount%120==0){char message[384];std::snprintf(message,sizeof message,"GPU timings: total=%.3f ms; eyeballCompute=%.3f; eyeballDraw=%.3f; background=%.3f; environment=%.3f; scene=%.3f; toneMap=%.3f; regionalCompute=%.3f; materialsOverlays=%.3f",values[0],values[1],values[2],values[3],values[4],values[5],values[6],values[7],values[8]);a.Log(NC_LOG_ALWAYS,message);}
}
void InspectEyeball(App &a){
  if(!a.timestampFrameSubmitted)return;
  const auto &eye=a.submission->planetaryEyeball;if(!eye.enabled||!a.planetaryEyeballVertexMapped||!a.planetaryEyeballIndirectMapped)return;const auto *vertices=static_cast<const EyeballVertex*>(a.planetaryEyeballVertexMapped);VkDrawIndexedIndirectCommand draw{};std::memcpy(&draw,a.planetaryEyeballIndirectMapped,sizeof(draw));const double cx=double(eye.cameraBodyHighX)+eye.cameraBodyLowX,cy=double(eye.cameraBodyHighY)+eye.cameraBodyLowY,cz=double(eye.cameraBodyHighZ)+eye.cameraBodyLowZ,length=std::sqrt(cx*cx+cy*cy+cz*cz);const uint32_t samples[]{0u,1u,1u+64u*EyeballAzimuthSegments,1u+127u*EyeballAzimuthSegments,EyeballVertexCount-1u};
  double expectedX=cx/length,expectedY=cy/length,expectedZ=cz/length;const double surfaceRadius=std::max(1.0,length-eye.surfaceAltitudeMetres);EarthViewPupil(cx,cy,cz,eye.viewForwardX,eye.viewForwardY,eye.viewForwardZ,surfaceRadius,expectedX,expectedY,expectedZ);const double pupilDot=vertices[0].bodyDirection[0]*expectedX+vertices[0].bodyDirection[1]*expectedY+vertices[0].bodyDirection[2]*expectedZ;bool valid=draw.indexCount==EyeballIndexCount&&draw.instanceCount==1&&draw.firstIndex==0&&draw.vertexOffset==0&&draw.firstInstance==0&&pupilDot>.999999;uint64_t hash=14695981039346656037ull;for(auto index:samples){const auto &v=vertices[index];for(float value:v.positionHeight)valid=valid&&std::isfinite(value);for(float value:v.normal)valid=valid&&std::isfinite(value);for(float value:v.bodyDirection)valid=valid&&std::isfinite(value);valid=valid&&v.positionHeight[3]>=0&&v.positionHeight[3]<=eye.maximumTerrainHeightMetres;const double nl=double(v.normal[0])*v.normal[0]+double(v.normal[1])*v.normal[1]+double(v.normal[2])*v.normal[2];valid=valid&&nl>.9&&nl<1.1;for(float value:v.positionHeight){uint32_t bits;std::memcpy(&bits,&value,sizeof(bits));hash=(hash^bits)*1099511628211ull;}}
  if(!valid){char detail[256];const auto &v=vertices[0];const double nl=double(v.normal[0])*v.normal[0]+double(v.normal[1])*v.normal[1]+double(v.normal[2])*v.normal[2];std::snprintf(detail,sizeof detail,"eyeball GPU validation failed: draw=%u/%u pupilDot=%.9f height=%.3f normal2=%.6f",draw.indexCount,draw.instanceCount,pupilDot,v.positionHeight[3],nl);throw std::runtime_error(detail);}if(!a.hasEyeballValidation||eye.surfaceAltitudeMetres!=a.lastEyeballValidationAltitude){char message[256];std::snprintf(message,sizeof message,"Eyeball CPU/GPU authority: match=true; altitude=%.3f m; vertices=%u; indices=%u; indirectCommands=1; representativeHash=0x%016llX",eye.surfaceAltitudeMetres,eye.vertexCount,eye.indexCount,(unsigned long long)hash);a.Log(NC_LOG_ALWAYS,message);a.hasEyeballValidation=true;a.lastEyeballValidationAltitude=eye.surfaceAltitudeMetres;}
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
  const auto recordStart=std::chrono::steady_clock::now();Record(a, image);const auto recordEnd=std::chrono::steady_clock::now();
  const bool detailedPresentation = !a.submission->planetaryPresentation.enabled ||
      a.submission->planetaryPresentation.regime != NC_PLANETARY_DISTANT_ONLY;
  const bool regionalPresentation = detailedPresentation &&
      (!a.submission->planetaryEyeball.enabled || a.submission->planetaryEyeball.regionalAlpha > 0);
  const bool gpuFrameSubmitted = regionalPresentation &&
      a.submission->planetaryMode != NC_PLANETARY_CPU_REFERENCE;
  if (a.submission->planetaryMode == NC_PLANETARY_CPU_GPU_VALIDATION && gpuFrameSubmitted)
    a.validationCpuOracle.assign(a.submission->planetaryPatches,
                                 a.submission->planetaryPatches + a.submission->planetaryPatchCount);
  else
    a.validationCpuOracle.clear();
  a.gpuFrameSubmitted = gpuFrameSubmitted;
  VkPipelineStageFlags stage = VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT;
  VkSubmitInfo si{VK_STRUCTURE_TYPE_SUBMIT_INFO};
  si.waitSemaphoreCount = 1;
  si.pWaitSemaphores = &a.imageAvailable;
  si.pWaitDstStageMask = &stage;
  si.commandBufferCount = 1;
  si.pCommandBuffers = &a.commands[image];
  si.signalSemaphoreCount = 1;
  si.pSignalSemaphores = &a.renderFinished[image];
  const auto submitStart=std::chrono::steady_clock::now();a.Check(vkQueueSubmit(a.graphicsQueue, 1, &si, a.fence), "submit failed");const auto submitEnd=std::chrono::steady_clock::now();
  VkPresentInfoKHR pi{VK_STRUCTURE_TYPE_PRESENT_INFO_KHR};
  pi.waitSemaphoreCount = 1;
  pi.pWaitSemaphores = &a.renderFinished[image];
  pi.swapchainCount = 1;
  pi.pSwapchains = &a.swapchain;
  pi.pImageIndices = &image;
  const auto presentStart=std::chrono::steady_clock::now();VkResult pr = vkQueuePresentKHR(a.presentQueue, &pi);const auto presentEnd=std::chrono::steady_clock::now();
  a.cpuRecordMs+=std::chrono::duration<double,std::milli>(recordEnd-recordStart).count();a.cpuSubmitMs+=std::chrono::duration<double,std::milli>(submitEnd-submitStart).count();a.cpuPresentMs+=std::chrono::duration<double,std::milli>(presentEnd-presentStart).count();a.cpuTimingSamples++;a.timestampFrameSubmitted=true;
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
    if(a.timestampQueries)vkDestroyQueryPool(a.device,a.timestampQueries,nullptr);
    for (auto s : a.renderFinished)
      vkDestroySemaphore(a.device, s, nullptr);
    if (a.imageAvailable)
      vkDestroySemaphore(a.device, a.imageAvailable, nullptr);
    DestroyMesh(a);
    DestroySubmission(a);
    DestroyEarthVirtualTexture(a);
    DestroyTerrainResidency(a);
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
  const auto updateStart=std::chrono::steady_clock::now();
  a.Check(vkWaitForFences(a.device, 1, &a.fence, VK_TRUE, UINT64_MAX), "frame fence wait failed");
  InspectGpuTimings(a);
  InspectGpuPlanetary(a);
  InspectEyeball(a);
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
  uint32_t presentationFocus = 0;
  for (int index = 0; index < 10; ++index) { const int key = index == 9 ? '0' : '1' + index; if (rising(key, a.presentationFocusWasDown[index])) { presentationFocus = static_cast<uint32_t>(index + 1); break; } }
  NcInputState in{dt,
                  (GetAsyncKeyState('A') & 0x8000) != 0,
                  (GetAsyncKeyState('D') & 0x8000) != 0,
                  (GetAsyncKeyState('W') & 0x8000) != 0,
                  (GetAsyncKeyState('S') & 0x8000) != 0,
                  (GetAsyncKeyState('Q') & 0x8000) != 0,
                  (GetAsyncKeyState('E') & 0x8000) != 0,
                  rising('R', a.resetWasDown),
                  a.lookActive,
                  x,
                  y,
                  wheel,
                  rising(VK_SPACE, a.pauseWasDown),
                  rising(VK_OEM_COMMA, a.rateDecreaseWasDown),
                  rising(VK_OEM_PERIOD, a.rateIncreaseWasDown), sasModeKey, static_cast<NcPresentationFocus>(presentationFocus)};
  NcHostEvent e{NC_UPDATE_FRAME, NC_LOG_NONE, nullptr, in, a.submission};
  a.cb(&e, a.cbData);
  if(a.submission->planetaryMode==NC_PLANETARY_CPU_REFERENCE)EnsurePatchCapacity(a,a.submission->planetaryPatchCount);
  Validate(a);
  Upload(a);
  PrepareEarthUploads(a);
  a.cpuUpdateMs+=std::chrono::duration<double,std::milli>(std::chrono::steady_clock::now()-updateStart).count();
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
        (uint32_t)offsetof(NcFrameSubmission, planetaryMode),
        (uint32_t)offsetof(NcFrameSubmission, planetaryPresentation),
        (uint32_t)offsetof(NcInputState, presentationFocus),
        (uint32_t)offsetof(NcFrameSubmission, solarLighting),
        (uint32_t)offsetof(NcFrameSubmission, planetaryEnvironment),
        (uint32_t)offsetof(NcFrameSubmission, planetaryEyeball)};
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
    CreateEarthVirtualTexture(a);
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
        auto frameBegin=std::chrono::steady_clock::now();auto now = frameBegin;
        Update(a, std::chrono::duration<float>(now - last).count());
        last = now;
        Draw(a);
        const double frameMs=std::chrono::duration<double,std::milli>(std::chrono::steady_clock::now()-frameBegin).count();a.frameTimesMs[a.frameTimeCursor]=frameMs;a.frameTimeCursor=(a.frameTimeCursor+1)%a.frameTimesMs.size();a.frameTimeCount=std::min(a.frameTimeCount+1,a.frameTimesMs.size());
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
    if(a.frameTimeCount){std::vector<double> sorted(a.frameTimesMs.begin(),a.frameTimesMs.begin()+a.frameTimeCount);std::sort(sorted.begin(),sorted.end());auto percentile=[&](double p){return sorted[std::min(sorted.size()-1,size_t(std::ceil(p*sorted.size()))-1)];};std::snprintf(text,sizeof text,"Frame pacing: p95=%.3f ms; p99=%.3f ms; max=%.3f ms; samples=%zu",percentile(.95),percentile(.99),sorted.back(),sorted.size());a.Log(NC_LOG_ALWAYS,text);}
    if(a.cpuTimingSamples){const double n=double(a.cpuTimingSamples);std::snprintf(text,sizeof text,"CPU timings: update/fence/callback/upload=%.3f ms; record=%.3f; submit=%.3f; present=%.3f",a.cpuUpdateMs/n,a.cpuRecordMs/n,a.cpuSubmitMs/n,a.cpuPresentMs/n);a.Log(NC_LOG_ALWAYS,text);}
    if(a.timestampSampleCount){const double n=double(a.timestampSampleCount);std::snprintf(text,sizeof text,"GPU timing averages: total=%.3f ms; eyeballCompute=%.3f; eyeballDraw=%.3f; background=%.3f; environment=%.3f; toneMap=%.3f",a.timestampAccumulatedMs[0]/n,a.timestampAccumulatedMs[1]/n,a.timestampAccumulatedMs[2]/n,a.timestampAccumulatedMs[3]/n,a.timestampAccumulatedMs[4]/n,a.timestampAccumulatedMs[6]/n);a.Log(NC_LOG_ALWAYS,text);}
    if(a.earthAvailable){uint32_t occupancy=0;for(auto tile:a.earthSlotTile)if(tile!=UINT32_MAX)occupancy++;uint64_t diskLoads=0,queueDrops=0;if(a.earthIo){std::lock_guard lock(a.earthIo->mutex);diskLoads=a.earthIo->diskLoads;queueDrops=a.earthIo->queueDrops;}char earthText[320];std::snprintf(earthText,sizeof earthText,"Earth VT streaming: requests=%llu; demandHits=%llu; demandMisses=%llu; diskLoads=%llu; uploads=%llu; evictions=%llu; fallbackFrames=%llu; queueDrops=%llu; occupancy=%u/%u; poolBytes=%u; stagingBytes=%u",(unsigned long long)a.earthRequests,(unsigned long long)a.earthDemandHits,(unsigned long long)a.earthDemandMisses,(unsigned long long)diskLoads,(unsigned long long)a.earthUploads,(unsigned long long)a.earthEvictions,(unsigned long long)a.earthFallbackFrames,(unsigned long long)queueDrops,occupancy,EarthPhysicalSlots,EarthPhysicalSlots*EarthTileExtent*EarthTileExtent*7,EarthStagingBytes);a.Log(NC_LOG_ALWAYS,earthText);}
    Destroy(a);
    return NC_SUCCESS;
  } catch (const std::exception &e) {
    a.Log(NC_LOG_ALWAYS, e.what());
    Destroy(a);
    return NC_FAILURE;
  }
}
