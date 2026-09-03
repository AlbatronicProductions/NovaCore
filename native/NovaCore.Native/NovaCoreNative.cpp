#include "NovaCoreNative.h"
#include "ProductionCubeSurface.h"
#include "LocalTerrainPack.h"
#include "PlanetaryHeightQuery.h"
#include "PlanetaryMeshPreparation.h"
#include "PlanetarySphericalBillboardGpuProof.h"
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
#include <memory>
#include <mutex>
#include <optional>
#include <numeric>
#include <stdexcept>
#include <string>
#include <thread>
#include <vector>
#include <vulkan/vulkan.h>
#include <windows.h>

namespace {
constexpr uint32_t Width = 960, Height = 540;
constexpr uint32_t PhysicalOracleWidth=8192,PhysicalOracleHeight=4096;
constexpr VkDeviceSize PhysicalOracleBytes=VkDeviceSize(PhysicalOracleWidth)*PhysicalOracleHeight*sizeof(uint16_t);
constexpr uint32_t GpuPatchCapacity = 8192, GpuActiveHashCapacity = 16384,
                   GpuPreviousHashCapacity = 16384,
                   GpuNodeEntryCapacity = GpuActiveHashCapacity + GpuPreviousHashCapacity,
                   TerrainCacheCapacity = 8192, TerrainGridVertexCount = 289;
constexpr uint32_t ProductionPayloadSlots=256,ProductionLookupCapacity=512,
                   ProductionUploadBudget=2,
                   ProductionMaximumPendingUploads=2,
                   ProductionAlbedoLayerBytes=nc::production::StoredExtent*nc::production::StoredExtent*4,
                   ProductionElevationLayerBytes=nc::production::ElevationBytes,
                   ProductionLandLayerBytes=nc::production::LandBytes,
                   ProductionStagingBytes=ProductionMaximumPendingUploads*(ProductionAlbedoLayerBytes+ProductionElevationLayerBytes+ProductionLandLayerBytes);
constexpr uint32_t LocalPayloadSlots=256,LocalLookupCapacity=2048,LocalLookupEntryWords=12,LocalUploadBudget=2,LocalMaximumPendingUploads=2,
                   LocalAlbedoLayerBytes=nc::localterrain::Bc7Bytes,LocalElevationLayerBytes=nc::localterrain::R16Bytes,
                   LocalNormalLayerBytes=nc::localterrain::Bc5Bytes,LocalControlLayerBytes=nc::localterrain::R8Bytes,
                   LocalStagingBytes=LocalMaximumPendingUploads*(LocalAlbedoLayerBytes+LocalElevationLayerBytes+LocalNormalLayerBytes+LocalControlLayerBytes);
constexpr uint32_t SurfaceDiagnosticDisableGlobal=1u<<0,SurfaceDiagnosticPayload=1u<<1,
                   SurfaceDiagnosticDisableAnchored=1u<<2,SurfaceDiagnosticUnlit=1u<<3,
                   SurfaceDiagnosticNormals=1u<<4,SurfaceDiagnosticOwners=1u<<5,
                   SurfaceDiagnosticBoundaries=1u<<6,SurfaceDiagnosticDepth=1u<<7,
                   SurfaceDiagnosticAddresses=1u<<8,SurfaceDiagnosticDiffuseOnly=1u<<9,
                   SurfaceDiagnosticSpecularDisabled=1u<<10,SurfaceDiagnosticRadial=1u<<11,
                   SurfaceDiagnosticConstantSphere=1u<<12,SurfaceDiagnosticPhysicalNormals=1u<<13,
                   SurfaceDiagnosticScreenDerivative=1u<<14;
constexpr uint32_t AnchoredSurfaceBaseGridResolution=4,AnchoredSurfaceBaseVerticesPerPatch=25,
                   AnchoredSurfaceBaseIndicesPerPatch=96,AnchoredSurfaceMaximumPatches=6144,
                   AnchoredSurfaceMaximumCacheSlots=16384,AnchoredSurfaceCoverageCapacity=16384,
                   AnchoredSurfacePresentationVectorCount=9,AnchoredSurfacePatchVectorCount=5,
                   AnchoredSurfacePatchVectorOffset=AnchoredSurfaceCoverageCapacity+AnchoredSurfacePresentationVectorCount,
                   NaturalGlobalPatchCount=126,NaturalGlobalVerticesPerPatch=289,NaturalAnchoredVerticesPerPatch=25,
                   AnchoredSurfaceFrameResourceCount=3;
constexpr uint32_t AnchoredSurfaceReady=1u<<0,AnchoredSurfaceAuthoritative=1u<<1,
                   AnchoredSurfaceGeometryComplete=1u<<2,AnchoredSurfacePhysicalComplete=1u<<3,
                   AnchoredSurfaceMaterialComplete=1u<<4,AnchoredSurfaceSynchronizationComplete=1u<<5,
                   AnchoredSurfaceLocalRequired=1u<<6,
                   AnchoredSurfaceRequired=AnchoredSurfaceReady|AnchoredSurfaceAuthoritative|
                     AnchoredSurfaceGeometryComplete|AnchoredSurfacePhysicalComplete|
                     AnchoredSurfaceMaterialComplete|AnchoredSurfaceSynchronizationComplete;
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
static_assert(sizeof(NcAnchoredSurfacePatch) == 80);
static_assert(sizeof(NcAnchoredSurfacePresentation) == 144);
static_assert(alignof(NcAnchoredSurfacePresentation) == 16);
static_assert(sizeof(NcPlanetaryGpuConstants) == 96);
static_assert(alignof(NcPlanetaryGpuConstants) == 16);
static_assert(offsetof(NcPlanetaryGpuConstants, cameraBodyLowX) == 16 &&
              offsetof(NcPlanetaryGpuConstants, refinementThreshold) == 32 &&
              offsetof(NcPlanetaryGpuConstants, maximumLevel) == 48 &&
              offsetof(NcPlanetaryGpuConstants, viewForwardX) == 64 &&
              offsetof(NcPlanetaryGpuConstants, viewportHeightPixels) == 80);
static_assert(sizeof(NcPlanetaryPresentation) == 192);
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
static_assert(offsetof(NcPlanetaryPresentation, localDetailScaleMeters) == 160);
static_assert(offsetof(NcPlanetaryPresentation, centerLowX) == 176);
static_assert(sizeof(NcSolarLighting) == 48);
static_assert(alignof(NcSolarLighting) == 16);
static_assert(offsetof(NcSolarLighting, photosphereR) == 16);
static_assert(offsetof(NcSolarLighting, sourceRadiance) == 32);
static_assert(offsetof(NcSolarLighting, speedHud) == 44);
static_assert(sizeof(NcFrameSubmission) == 784);
static_assert(offsetof(NcFrameSubmission, planetaryGpu) == 208);
static_assert(offsetof(NcFrameSubmission, planetaryMode) == 304);
static_assert(offsetof(NcFrameSubmission, planetaryPresentation) == 320);
static_assert(offsetof(NcFrameSubmission, distantBodies) == 512);
static_assert(offsetof(NcFrameSubmission, distantBodyCount) == 520);
static_assert(offsetof(NcFrameSubmission, distantBodyPadding) == 524);
static_assert(offsetof(NcFrameSubmission, solarLighting) == 528);
static_assert(offsetof(NcFrameSubmission, anchoredSurfacePatches) == 576);
static_assert(offsetof(NcFrameSubmission, anchoredSurfacePatchCount) == 584);
static_assert(offsetof(NcFrameSubmission, anchoredSurfacePresentation) == 624);
static_assert(offsetof(NcFrameSubmission, productionBillboard) == 768);
static_assert(sizeof(NcOrbitLineVertex) == 24);
static_assert(sizeof(NcRuntimeAssets) == 32);
static_assert(sizeof(NcInputState) == 84);
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
static_assert(offsetof(NcInputState, fastModifier) == 64);
static_assert(offsetof(NcInputState, slowModifier) == 68);
static_assert(offsetof(NcInputState, presentationFocus) == 72);
static_assert(offsetof(NcInputState, viewportWidthPixels) == 76);
static_assert(offsetof(NcInputState, viewportHeightPixels) == 80);
struct Vertex {
  float position[3];
  float color[3];
  float normal[3];
};
static_assert(sizeof(Vertex) == 36);
struct PatchVertex { float uv[2]; };
static_assert(sizeof(PatchVertex) == 8);
struct DistantVertex { float position[3]; };
static_assert(sizeof(DistantVertex) == 12);
struct RingVertex { float directionX, directionZ, radial; };
static_assert(sizeof(RingVertex) == 12);
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
  VkDispatchIndirectCommand terrainDispatch{};
  uint32_t productionDemandSignature[20]{};
};
static_assert(sizeof(GpuPlanetaryControl) == 204 && offsetof(GpuPlanetaryControl, terrainDispatch) == 112 && offsetof(GpuPlanetaryControl, productionDemandSignature) == 124);
struct ProductionRequest { nc::production::PatchId id{}; uint32_t terrainSlot{}; };
struct ProductionReady { ProductionRequest request{}; std::unique_ptr<nc::production::Payload> payload; uint32_t state{}; };
struct ProductionIoState {
  std::thread worker; std::mutex mutex; std::condition_variable wake; bool stop{};
  std::array<ProductionRequest,512> requests{}; uint32_t requestHead{},requestTail{},requestCount{};
  std::array<ProductionReady,8> ready{}; const nc::production::Pack *pack{}; uint64_t diskLoads{},queueDrops{},digestFailures{};
};
struct LocalRequest{nc::localterrain::SectorId id{};uint64_t demandEpoch{};bool visible{};std::chrono::steady_clock::time_point requestedAt{};};
struct LocalReady{LocalRequest request{};std::unique_ptr<nc::localterrain::Payload>payload;uint32_t state{};};
struct LocalIoState{
  std::thread worker;std::mutex mutex;std::condition_variable wake;bool stop{};
  std::array<LocalRequest,256>requests{};uint32_t requestHead{},requestTail{},requestCount{};
  std::array<LocalReady,8>ready{};const nc::localterrain::Pack*pack{};
  uint64_t diskLoads{},queueDrops{},digestFailures{},bytesRead{},bytesTranscoded{};double transcodeMilliseconds{};
};
struct Queues {
  std::optional<uint32_t> graphics, present;
  bool Complete() const { return graphics && present; }
};
struct App {
  NcHostCallback cb{};
  void *cbData{};
  NcFrameSubmission *submission{};
  std::string productionTerrainPath;
  std::string localTerrainPath;
  std::string elevationOraclePath;
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
  // Stencil is the exact per-sample publication boundary between the dynamic
  // hierarchy and its complete terrain-v5 parent.  A geographic hash lookup
  // cannot reproduce the rasterizer's piecewise-linear patch silhouette at a
  // grazing edge; the depth/stencil surface therefore carries actual child
  // raster ownership without reducing reversed-Z depth precision.
  static constexpr VkFormat DepthFormat = VK_FORMAT_D32_SFLOAT_S8_UINT;
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
  std::array<VkImage,3> productionImages{};
  std::array<VkDeviceMemory,3> productionImageMemory{};
  std::array<VkImageView,3> productionImageViews{};
  VkSampler productionSampler{}; VkBuffer productionStagingBuffer{}; VkDeviceMemory productionStagingMemory{}; void *productionStagingMapped{};
  VkBuffer physicalOracleBuffer{};VkDeviceMemory physicalOracleMemory{};void *physicalOracleMapped{};
  std::unique_ptr<nc::production::Pack> productionPack; std::unique_ptr<ProductionIoState> productionIo;
  std::array<uint32_t,ProductionPayloadSlots> productionLayerTerrainSlot{};
  std::array<nc::production::PatchId,ProductionPayloadSlots> productionLayerPatch{};
  std::array<uint64_t,ProductionPayloadSlots> productionLayerLastUse{};
  std::array<uint32_t,ProductionPayloadSlots> productionLayerGeneration{};
  std::array<std::vector<uint16_t>,ProductionPayloadSlots> productionElevationCpu{};
  std::array<uint32_t,ProductionMaximumPendingUploads> productionUploadLayers{},productionUploadTerrainSlots{},productionUploadGenerations{};
  std::array<ProductionRequest,ProductionMaximumPendingUploads> productionUploadRequests{};
  uint32_t productionPendingUploads{}; bool productionImagesInitialized{};
  uint64_t productionRequests{},productionUploads{},productionUploadBytes{},productionQueueDrops{},productionEvictions{};
  std::array<VkImage,4> localImages{};std::array<VkDeviceMemory,4>localImageMemory{};std::array<VkImageView,4>localImageViews{};
  VkSampler localSampler{};VkBuffer localStagingBuffer{};VkDeviceMemory localStagingMemory{};void*localStagingMapped{};
  VkBuffer localLookupBuffer{};VkDeviceMemory localLookupMemory{};void*localLookupMapped{};
  std::unique_ptr<nc::localterrain::Pack>localPack;std::unique_ptr<LocalIoState>localIo;
  std::array<nc::localterrain::SectorId,LocalPayloadSlots>localLayerSector{};std::array<uint64_t,LocalPayloadSlots>localLayerLastUse{};
  std::array<uint32_t,LocalPayloadSlots>localLayerGeneration{};std::array<uint8_t,LocalPayloadSlots>localLayerOccupied{},localLayerVisible{},localLayerInFlight{},localLayerPublished{};
  std::array<float,LocalPayloadSlots>localLayerResidualMinimum{},localLayerResidualMaximum{};
  std::array<uint32_t,LocalMaximumPendingUploads>localUploadLayers{},localUploadGenerations{};std::array<LocalRequest,LocalMaximumPendingUploads>localUploadRequests{};
  std::array<nc::localterrain::SectorId,LocalPayloadSlots>localVisibleTarget{};uint32_t localVisibleTargetCount{},localPendingUploads{},localAnchoredGeneration{UINT32_MAX};bool localImagesInitialized{};uint64_t localDemandEpoch{1},localRequests{},localHits{},localMisses{},localEvictions{},localCanceled{},localUploads{},localPromotions{},localUploadBytes{};
  uint64_t localLastPupilBits[3]{};bool localHasLastPupil{};double localTranscodeMilliseconds{},localUploadLatencyMilliseconds{};
  uint64_t surfaceContextBodyId{},surfaceTransitionEpoch{},surfaceContextInvalidations{},productionDemandHits{},productionDemandMisses{};
  uint32_t surfaceContextTerrainVersion{},surfaceContextPhysicalGeneration{},surfaceContextMode{},surfaceContextRegime{},surfaceContextRadiusHighBits{},surfaceContextRadiusLowBits{};
  bool surfaceContextValid{},productionSurfaceLogged{},productionRootsReadyLogged{},productionGeometryTraceLogged{};uint32_t earthTransitionTraceRemaining{},earthSubmissionTraceRemaining{};uint64_t recordSerial{},submitSerial{},presentSerial{};
  VkPipelineLayout pipelineLayout{};
  VkPipeline pipeline{};
  VkPipeline backgroundPipeline{};
  VkPipeline toneMapPipeline{};
  VkPipeline stellarSunPipeline{};
  VkPipeline stellarGlowPipeline{};
  VkPipeline planetaryPipeline{};
  VkPipeline productionPlanetaryPipeline{};
  VkPipeline productionPlanetaryFillPipeline{};
  VkPipeline anchoredTerrainPipeline{};
  VkPipeline productionBillboardPipeline{},productionBillboardResetPipeline{},productionBillboardCullPipeline{},productionBillboardCompactPipeline{};
  VkPipeline productionBillboardIncomingResetPipeline{},productionBillboardIncomingCullPipeline{},productionBillboardIncomingCompactPipeline{};
  VkPipeline naturalGlobalPreparePipeline{},naturalAnchoredPreparePipeline{};
  VkPipeline planetaryComputePipeline{};
  VkPipeline planetaryTerrainPipeline{};
  VkPipeline productionPlanetaryTerrainPipeline{};
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
  VkBuffer anchoredSurfaceVertexBuffer{},anchoredSurfaceIndexBuffer{};
  VkDeviceMemory anchoredSurfaceVertexMemory{},anchoredSurfaceIndexMemory{};
  void *anchoredSurfaceVertexMapped{},*anchoredSurfaceIndexMapped{};
  std::array<VkBuffer,AnchoredSurfaceFrameResourceCount> anchoredSurfaceCoverageBuffers{},anchoredSurfaceIndirectBuffers{};
  std::array<VkDeviceMemory,AnchoredSurfaceFrameResourceCount> anchoredSurfaceCoverageMemories{},anchoredSurfaceIndirectMemories{};
  std::array<void*,AnchoredSurfaceFrameResourceCount> anchoredSurfaceCoverageMapped{},anchoredSurfaceIndirectMapped{};
  std::array<uint32_t,AnchoredSurfaceFrameResourceCount> anchoredSurfaceResourceGenerations{};
  uint32_t anchoredSurfaceResourceIndex{};
  std::vector<uint32_t> anchoredSurfaceSlotGenerations;
  std::vector<NcAnchoredSurfacePatch> anchoredSurfaceActivePatches;
  uint32_t anchoredSurfaceActivePatchCount{},anchoredSurfaceActiveGeneration{},anchoredSurfacePublicationLogGeneration{};
  uint64_t anchoredSurfaceUploadBytes{},anchoredSurfaceUploads{},anchoredSurfaceCapacityRejects{};
  bool anchoredSurfaceResourcesReady{},anchoredSurfaceActive{},anchoredSurfacePublicationRequested{},anchoredGroundTruthEnabled{};
  VkBuffer naturalGlobalPreparedBuffer{},naturalAnchoredPreparedBuffer{};
  VkDeviceMemory naturalGlobalPreparedMemory{},naturalAnchoredPreparedMemory{};
  void *naturalGlobalPreparedMapped{},*naturalAnchoredPreparedMapped{};
  bool naturalGlobalPreparationPending{},naturalGlobalPrepared{},naturalAnchoredPreparationPending{};
  uint32_t naturalAnchoredPreparationGeneration{},naturalAnchoredSubmittedGeneration{},naturalAnchoredPreparedGeneration{};
  uint32_t naturalAnchoredPreparationPatchCount{};
  uint64_t naturalGlobalPreparationDispatches{},naturalAnchoredPreparationDispatches{};
  VkBuffer productionBillboardLatticeBuffer{},productionBillboardPhysicalBuffer{},productionBillboardIndexBuffer{},productionBillboardVisibilityBuffer{},productionBillboardCompactedBuffer{},productionBillboardIndirectBuffer{},productionBillboardCounterBuffer{};
  VkDeviceMemory productionBillboardLatticeMemory{},productionBillboardPhysicalMemory{},productionBillboardIndexMemory{},productionBillboardVisibilityMemory{},productionBillboardCompactedMemory{},productionBillboardIndirectMemory{},productionBillboardCounterMemory{};
  void *productionBillboardLatticeMapped{},*productionBillboardPhysicalMapped{},*productionBillboardIndexMapped{},*productionBillboardVisibilityMapped{},*productionBillboardCompactedMapped{},*productionBillboardIndirectMapped{},*productionBillboardCounterMapped{};
  bool productionBillboardEnabled{},productionBillboardWorkRecorded{},productionBillboardFencePending{},productionBillboardAuthoritative{};
  uint32_t productionBillboardVertexCount{},productionBillboardTriangleCount{};uint64_t productionBillboardTopologyHash{},productionBillboardGeneration{};
  VkBuffer productionBillboardIncomingLatticeBuffer{},productionBillboardIncomingPhysicalBuffer{},productionBillboardIncomingIndexBuffer{},productionBillboardIncomingVisibilityBuffer{},productionBillboardIncomingCompactedBuffer{},productionBillboardIncomingIndirectBuffer{},productionBillboardIncomingCounterBuffer{};
  VkDeviceMemory productionBillboardIncomingLatticeMemory{},productionBillboardIncomingPhysicalMemory{},productionBillboardIncomingIndexMemory{},productionBillboardIncomingVisibilityMemory{},productionBillboardIncomingCompactedMemory{},productionBillboardIncomingIndirectMemory{},productionBillboardIncomingCounterMemory{};
  void *productionBillboardIncomingLatticeMapped{},*productionBillboardIncomingPhysicalMapped{},*productionBillboardIncomingIndexMapped{},*productionBillboardIncomingVisibilityMapped{},*productionBillboardIncomingCompactedMapped{},*productionBillboardIncomingIndirectMapped{},*productionBillboardIncomingCounterMapped{};
  bool productionBillboardIncomingEnabled{},productionBillboardIncomingWorkRecorded{},productionBillboardIncomingFencePending{},productionBillboardIncomingOwnsTopology{};
  uint32_t productionBillboardIncomingVertexCount{},productionBillboardIncomingTriangleCount{};uint64_t productionBillboardIncomingTopologyHash{},productionBillboardIncomingGeneration{};
  uint64_t productionBillboardTopologyUploads{},productionBillboardPublications{},productionBillboardDeferredRetirements{};
  VkBuffer gpuInputBuffer{}, gpuWorkBuffer{}, gpuNodeBuffer{}, gpuControlBuffer{};
  VkDeviceMemory gpuInputMemory{}, gpuWorkMemory{}, gpuNodeMemory{}, gpuControlMemory{};
  void *gpuInputMapped{}, *gpuWorkMapped{}, *gpuNodeMapped{}, *gpuControlMapped{};
  VkBuffer terrainKeyBuffer{}, terrainSampleBuffer{}, terrainPatchSlotBuffer{};
  VkDeviceMemory terrainKeyMemory{}, terrainSampleMemory{}, terrainPatchSlotMemory{};
  void *terrainKeyMapped{}, *terrainSampleMapped{}, *terrainPatchSlotMapped{};
  VkBuffer planetaryPresentationBuffer{};
  VkDeviceMemory planetaryPresentationMemory{};
  void *planetaryPresentationMapped{};
  VkBuffer productionLayerLookupBuffer{}; VkDeviceMemory productionLayerLookupMemory{}; void *productionLayerLookupMapped{};
  bool gpuFrameSubmitted{}, hasGpuTelemetry{};
  GpuPlanetaryControl lastGpuTelemetry{};
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
  Mesh floridaLaunchPad{};
  Mesh planetaryPatch{};
  Mesh distantPlanetary{};
  Mesh stellarSun{};
  Mesh planetaryRing{};
  VkQueryPool timestampQueries{};
  VkQueryPool anchoredPipelineStatistics{};
  float timestampPeriodNanoseconds{};
  bool timestampFrameSubmitted{};
  static constexpr uint32_t TimestampCount=11;
  std::array<double,TimestampCount> timestampAccumulatedMs{};
  uint64_t timestampSampleCount{};
  uint64_t anchoredPipelineStatisticsSamples{},anchoredTessellationControlPatches{};
  uint64_t anchoredTessellationEvaluationInvocations{},anchoredClippingPrimitives{};
  bool anchoredPipelineStatisticsFrameSubmitted{};
  double cpuUpdateMs{},cpuFenceWaitMs{},cpuInspectionMs{},cpuHostCallbackMs{},cpuUploadMs{};
  double cpuRecordMs{},cpuSubmitMs{},cpuPresentMs{};
  uint64_t cpuTimingSamples{};
  // Opt-in benchmark telemetry. It records only the settled measurement window
  // selected by the managed canonical M12 harness; normal production telemetry
  // and scheduling are unchanged.
  bool canonicalBenchmark{};
  std::vector<double> canonicalCpuUpdateMs,canonicalCpuFenceMs,canonicalGpuTotalMs,canonicalGpuMaterialMs,
                      canonicalGpuAnchoredMs,canonicalGpuGlobalFillMs,canonicalGpuOverlayMs;
  std::array<double, 8192> frameTimesMs{};
  size_t frameTimeCount{}, frameTimeCursor{};
  std::array<double, 8192> fenceTimesMs{};
  size_t fenceTimeCount{}, fenceTimeCursor{};
  LONG rawMouseX{}, rawMouseY{};
  LONG wheelDeltaRaw{};
  bool lookActive{};
  bool pauseWasDown{}, rateDecreaseWasDown{}, rateIncreaseWasDown{};
  std::array<bool, 8> sasModeWasDown{};
  std::array<bool, 10> presentationFocusWasDown{};
  bool resetWasDown{};
  uint32_t surfaceDiagnostic{};
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
uint32_t SurfaceDiagnosticFromEnvironment() {
  const char *value=std::getenv("NOVACORE_SURFACE_DIAGNOSTIC");
  if(!value||!*value||std::strcmp(value,"normal")==0)return 0;
  if(std::strcmp(value,"cube")==0)return SurfaceDiagnosticDisableAnchored;
  if(std::strcmp(value,"eye")==0)return SurfaceDiagnosticDisableGlobal|SurfaceDiagnosticDisableAnchored;
  if(std::strcmp(value,"cube-eye")==0)return SurfaceDiagnosticDisableAnchored;
  if(std::strcmp(value,"anchored")==0)return SurfaceDiagnosticDisableGlobal;
  if(std::strcmp(value,"cube-anchored")==0)return 0;
  if(std::strcmp(value,"unlit")==0)return SurfaceDiagnosticUnlit;
  if(std::strcmp(value,"normals")==0)return SurfaceDiagnosticNormals;
  if(std::strcmp(value,"owners")==0)return SurfaceDiagnosticOwners;
  if(std::strcmp(value,"boundaries")==0)return SurfaceDiagnosticBoundaries;
  if(std::strcmp(value,"depth")==0)return SurfaceDiagnosticDepth;
  if(std::strcmp(value,"addresses")==0)return SurfaceDiagnosticAddresses;
  if(std::strcmp(value,"diffuse")==0)return SurfaceDiagnosticDiffuseOnly;
  if(std::strcmp(value,"specular-off")==0)return SurfaceDiagnosticSpecularDisabled;
  if(std::strcmp(value,"radial")==0)return SurfaceDiagnosticRadial;
  if(std::strcmp(value,"constant-sphere")==0)return SurfaceDiagnosticConstantSphere;
  if(std::strcmp(value,"physical-normals")==0)return SurfaceDiagnosticPhysicalNormals;
  if(std::strcmp(value,"screen-derivative")==0)return SurfaceDiagnosticScreenDerivative;
  if(std::strcmp(value,"payload")==0)return SurfaceDiagnosticPayload;
  if(std::strcmp(value,"global-height")==0)return 1u<<15;
  if(std::strcmp(value,"physical-modifier")==0||std::strcmp(value,"modifier")==0)return (1u<<15)|(1u<<8);
  if(std::strcmp(value,"final-height")==0)return (1u<<15)|(2u<<8);
  if(std::strcmp(value,"biome-id")==0)return (1u<<15)|(4u<<8);
  if(std::strcmp(value,"biome-blend")==0)return (1u<<15)|(5u<<8);
  if(std::strcmp(value,"modifier-family")==0)return (1u<<15)|(6u<<8);
  if(std::strcmp(value,"near-physical")==0)return (1u<<15)|(7u<<8);
  if(std::strcmp(value,"regional-height")==0)return (1u<<15)|SurfaceDiagnosticPayload;
  if(std::strcmp(value,"residual")==0)return (1u<<15)|SurfaceDiagnosticNormals;
  if(std::strcmp(value,"regional-control")==0)return (1u<<15)|SurfaceDiagnosticOwners;
  if(std::strcmp(value,"regional-residency")==0)return (1u<<15)|SurfaceDiagnosticBoundaries;
  if(std::strcmp(value,"regional-boundary")==0)return (1u<<15)|SurfaceDiagnosticDepth;
  if(std::strcmp(value,"material-id")==0)return (1u<<15)|SurfaceDiagnosticAddresses|SurfaceDiagnosticNormals;
  if(std::strcmp(value,"regional-mip")==0)return (1u<<15)|SurfaceDiagnosticDiffuseOnly|SurfaceDiagnosticNormals;
  if(std::strcmp(value,"anchored-unlit")==0)return SurfaceDiagnosticDisableGlobal|SurfaceDiagnosticUnlit;
  if(std::strcmp(value,"cube-unlit")==0)return SurfaceDiagnosticDisableAnchored|SurfaceDiagnosticUnlit;
  if(std::strcmp(value,"anchored-physical-normals")==0)return SurfaceDiagnosticDisableGlobal|SurfaceDiagnosticPhysicalNormals;
  if(std::strcmp(value,"cube-physical-normals")==0)return SurfaceDiagnosticDisableAnchored|SurfaceDiagnosticPhysicalNormals;
  if(std::strcmp(value,"anchored-addresses")==0)return SurfaceDiagnosticDisableGlobal|SurfaceDiagnosticAddresses;
  if(std::strcmp(value,"cube-addresses")==0)return SurfaceDiagnosticDisableAnchored|SurfaceDiagnosticAddresses;
  if(std::strcmp(value,"anchored-depth")==0)return SurfaceDiagnosticDisableGlobal|SurfaceDiagnosticDepth;
  if(std::strcmp(value,"cube-depth")==0)return SurfaceDiagnosticDisableAnchored|SurfaceDiagnosticDepth;
  if(std::strcmp(value,"anchored-payload")==0)return SurfaceDiagnosticDisableGlobal|SurfaceDiagnosticPayload;
  if(std::strcmp(value,"cube-payload")==0)return SurfaceDiagnosticDisableAnchored|SurfaceDiagnosticPayload;
  if(std::strcmp(value,"anchored-footprint")==0)return SurfaceDiagnosticDisableGlobal|SurfaceDiagnosticPayload|SurfaceDiagnosticDepth;
  if(std::strcmp(value,"cube-footprint")==0)return SurfaceDiagnosticDisableAnchored|SurfaceDiagnosticPayload|SurfaceDiagnosticDepth;
  throw std::runtime_error("NOVACORE_SURFACE_DIAGNOSTIC is invalid");
}
void SeedProductionTerrainCacheHighWater(App &a) {
  if(!a.gpuControlMapped||!a.productionPack)return;
  auto *control=static_cast<GpuPlanetaryControl*>(a.gpuControlMapped);
  control->padding[0]=std::min<uint32_t>(a.productionPack->RecordCount(),TerrainCacheCapacity);
  char message[160];std::snprintf(message,sizeof message,"Production cache extent seeded: records=%u; controlHighWater=%u; offset=%zu",a.productionPack->RecordCount(),control->padding[0],offsetof(GpuPlanetaryControl,padding));a.Log(NC_LOG_ALWAYS,message);
}
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
  constexpr std::array<Vertex, 3> v{{{{0, -.04f, 0}, {1, 0, 0}, {0,0,1}},
                                     {{.04f, .04f, 0}, {0, 1, 0}, {0,0,1}},
                                     {{-.04f, .04f, 0}, {0, 0, 1}, {0,0,1}}}};
  constexpr std::array<uint32_t, 3> i{{0, 1, 2}};
  Buffer(a, sizeof(v), VK_BUFFER_USAGE_VERTEX_BUFFER_BIT, a.triangle.vb,
         a.triangle.vm, v.data());
  Buffer(a, sizeof(i), VK_BUFFER_USAGE_INDEX_BUFFER_BIT, a.triangle.ib,
         a.triangle.im, i.data());
  a.triangle.indices = 3;
  std::vector<Vertex> padVertices;std::vector<uint32_t> padIndices;
  const auto box=[&](float minX,float minY,float minZ,float maxX,float maxY,float maxZ,std::array<float,3> color){
    const std::array<std::array<float,3>,8> p{{{{minX,minY,minZ}},{{maxX,minY,minZ}},{{maxX,maxY,minZ}},{{minX,maxY,minZ}},{{minX,minY,maxZ}},{{maxX,minY,maxZ}},{{maxX,maxY,maxZ}},{{minX,maxY,maxZ}}}};
    const std::array<std::array<uint32_t,4>,6> q{{{{0,3,2,1}},{{4,5,6,7}},{{0,1,5,4}},{{1,2,6,5}},{{2,3,7,6}},{{3,0,4,7}}}};
    const std::array<std::array<float,3>,6> n{{{{0,0,-1}},{{0,0,1}},{{0,-1,0}},{{1,0,0}},{{0,1,0}},{{-1,0,0}}}};
    for(uint32_t face=0;face<6;face++){const uint32_t base=(uint32_t)padVertices.size();for(uint32_t corner:q[face])padVertices.push_back({{p[corner][0],p[corner][1],p[corner][2]},{color[0],color[1],color[2]},{n[face][0],n[face][1],n[face][2]}});padIndices.insert(padIndices.end(),{base,base+1,base+2,base,base+2,base+3});}
  };
  // Local axes are canonical East (+X), North (+Y), Up (+Z).
  box(-32,-24,0,32,24,1.5f,{.34f,.37f,.40f});
  box(-7,-7,1.5f,7,7,8.5f,{.48f,.50f,.52f});
  box(28,-5,1.5f,50,5,3.0f,{.82f,.33f,.10f}); // unmistakable east extension
  box(-2,18,1.5f,2,38,2.6f,{.18f,.55f,.86f}); // north marker
  Buffer(a,sizeof(Vertex)*padVertices.size(),VK_BUFFER_USAGE_VERTEX_BUFFER_BIT,a.floridaLaunchPad.vb,a.floridaLaunchPad.vm,padVertices.data());
  Buffer(a,sizeof(uint32_t)*padIndices.size(),VK_BUFFER_USAGE_INDEX_BUFFER_BIT,a.floridaLaunchPad.ib,a.floridaLaunchPad.im,padIndices.data());
  a.floridaLaunchPad.indices=(uint32_t)padIndices.size();
  constexpr uint32_t cells=16, side=cells+1;
  std::array<PatchVertex,side*side> pv{};
  std::array<uint32_t,cells*cells*6> pi{};
  uint32_t vertex=0,index=0;
  for(uint32_t y=0;y<side;y++)for(uint32_t x=0;x<side;x++)pv[vertex++]={{float(x)/cells,float(y)/cells}};
  for(uint32_t y=0;y<cells;y++)for(uint32_t x=0;x<cells;x++){uint32_t q=y*side+x;pi[index++]=q;pi[index++]=q+1;pi[index++]=q+side;pi[index++]=q+1;pi[index++]=q+side+1;pi[index++]=q+side;}
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
  {char message[192];std::snprintf(message,sizeof message,
    "Created persistent generic meshes: triangleIndices=%u; floridaLaunchPadVertices=%zu; floridaLaunchPadIndices=%u",
    a.triangle.indices,padVertices.size(),a.floridaLaunchPad.indices);a.Log(NC_LOG_VULKAN,message);}
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
  if(a.floridaLaunchPad.vb)vkDestroyBuffer(a.device,a.floridaLaunchPad.vb,nullptr);
  if(a.floridaLaunchPad.vm)vkFreeMemory(a.device,a.floridaLaunchPad.vm,nullptr);
  if(a.floridaLaunchPad.ib)vkDestroyBuffer(a.device,a.floridaLaunchPad.ib,nullptr);
  if(a.floridaLaunchPad.im)vkFreeMemory(a.device,a.floridaLaunchPad.im,nullptr);
  a.floridaLaunchPad={};
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
}
Mesh *MeshFor(App &a, NcMeshHandle h) {
  return h.value == 1 ? &a.triangle : h.value == 3 ? &a.floridaLaunchPad : nullptr;
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
  const auto validMaterial=[](const NcPlanetaryPresentation &body){
    const bool stellarMaterial=body.bodyIdLow==2&&body.bodyIdHigh==0&&body.materialKind==0&&body.albedoSource==0;
    const bool finite=std::isfinite(body.roughness)&&std::isfinite(body.specular)&&std::isfinite(body.emissive)&&std::isfinite(body.presentationRotationRadians)&&std::isfinite(body.ringInnerRadiusRatio)&&std::isfinite(body.ringOuterRadiusRatio)&&std::isfinite(body.ringOpacity)&&std::isfinite(body.ringBandFrequency)&&std::isfinite(body.ringOrientationX)&&std::isfinite(body.ringOrientationY)&&std::isfinite(body.ringOrientationZ)&&std::isfinite(body.ringOrientationW)&&std::isfinite(body.ringColorR)&&std::isfinite(body.ringColorG)&&std::isfinite(body.ringColorB)&&std::isfinite(body.ringColorA)&&std::isfinite(body.bodyOrientationX)&&std::isfinite(body.bodyOrientationY)&&std::isfinite(body.bodyOrientationZ)&&std::isfinite(body.bodyOrientationW)&&std::isfinite(body.localDetailScaleMeters)&&std::isfinite(body.localDetailMicroScaleMeters)&&std::isfinite(body.localDetailFadeStartMetres)&&std::isfinite(body.localDetailFadeEndMetres);
    const float bodyQ=body.bodyOrientationX*body.bodyOrientationX+body.bodyOrientationY*body.bodyOrientationY+body.bodyOrientationZ*body.bodyOrientationZ+body.bodyOrientationW*body.bodyOrientationW;
    const bool localDetail=body.localDetailScaleMeters>0&&body.localDetailMicroScaleMeters>0&&body.localDetailFadeStartMetres>=0&&body.localDetailFadeEndMetres>body.localDetailFadeStartMetres;
    if(!finite||std::abs(bodyQ-1)>1e-4f||body.roughness<0||body.roughness>1||body.specular<0||body.specular>1||body.emissive<0||body.projectionKind>0||!localDetail)
      return false;
    if(!stellarMaterial&&((!body.bodyIdLow&&!body.bodyIdHigh)||body.materialKind<1||body.materialKind>4||body.albedoSource<1||body.albedoSource>10))
      return false;
    if(!body.ringAssociation)
      return body.ringInnerRadiusRatio==0&&body.ringOuterRadiusRatio==0&&body.ringOpacity==0&&body.ringBandFrequency==0;
    if(body.ringInnerRadiusRatio<=1||body.ringOuterRadiusRatio<=body.ringInnerRadiusRatio||body.ringOpacity<=0||body.ringOpacity>1||body.ringBandFrequency<=0)
      return false;
    const float q=body.ringOrientationX*body.ringOrientationX+body.ringOrientationY*body.ringOrientationY+body.ringOrientationZ*body.ringOrientationZ+body.ringOrientationW*body.ringOrientationW;
    return std::abs(q-1)<1e-4f;
  };
  if(s->planetaryGpuAlignmentPadding||(s->planetarySurfaceMode!=NC_PLANETARY_SURFACE_BOUNDED&&s->planetarySurfaceMode!=NC_PLANETARY_SURFACE_PRODUCTION_CUBE)||
     (s->physicalSurfaceGeneration!=3u&&s->physicalSurfaceGeneration!=4u)||s->planetaryPadding)
    throw std::runtime_error("invalid planetary surface generation, mode, or frame padding");
  if(s->planetaryMode>NC_PLANETARY_CPU_GPU_VALIDATION)throw std::runtime_error("invalid planetary mode");
  const auto &presentation=s->planetaryPresentation;const bool hasPresentation=presentation.enabled!=0;
  const auto bodyCenterMatches=[&presentation](double cameraX,double cameraY,double cameraZ){const double vx=-cameraX,vy=-cameraY,vz=-cameraZ,qx=presentation.bodyOrientationX,qy=presentation.bodyOrientationY,qz=presentation.bodyOrientationZ,qw=presentation.bodyOrientationW;const double cx=qy*vz-qz*vy,cy=qz*vx-qx*vz,cz=qx*vy-qy*vx;const double ux=cx+qw*vx,uy=cy+qw*vy,uz=cz+qw*vz;const double rx=vx+2*(qy*uz-qz*uy),ry=vy+2*(qz*ux-qx*uz),rz=vz+2*(qx*uy-qy*ux);const double scale=std::max({1.0,std::abs(rx),std::abs(ry),std::abs(rz)}),tolerance=std::max(32.0,scale*1e-6);return std::abs((double(presentation.centerX)+presentation.centerLowX)-rx)<=tolerance&&std::abs((double(presentation.centerY)+presentation.centerLowY)-ry)<=tolerance&&std::abs((double(presentation.centerZ)+presentation.centerLowZ)-rz)<=tolerance;};
  if(s->distantBodyPadding||s->distantBodyCount>10||(s->distantBodyCount&&!s->distantBodies))throw std::runtime_error("invalid distant body batch");
  for(uint32_t i=0;i<s->distantBodyCount;i++){const auto &body=s->distantBodies[i];if(!body.enabled||!std::isfinite(body.centerX)||!std::isfinite(body.centerY)||!std::isfinite(body.centerZ)||!std::isfinite(body.centerLowX)||!std::isfinite(body.centerLowY)||!std::isfinite(body.centerLowZ)||body.centerLowPadding!=0||!std::isfinite(body.radius)||body.radius<=0||!std::isfinite(body.colorR)||!std::isfinite(body.colorG)||!std::isfinite(body.colorB)||!std::isfinite(body.distantAlpha)||body.distantAlpha<0||body.distantAlpha>1||!validMaterial(body))throw std::runtime_error("invalid distant body record");}
  if(presentation.enabled>1)throw std::runtime_error("invalid planetary presentation enable");
  if(hasPresentation){const bool validWeights=std::abs(presentation.distantAlpha+presentation.detailedAlpha-1)<=1e-5f;if(presentation.regime>NC_PLANETARY_DETAILED_ONLY||!std::isfinite(presentation.centerX)||!std::isfinite(presentation.centerY)||!std::isfinite(presentation.centerZ)||!std::isfinite(presentation.centerLowX)||!std::isfinite(presentation.centerLowY)||!std::isfinite(presentation.centerLowZ)||presentation.centerLowPadding!=0||!std::isfinite(presentation.radius)||presentation.radius<=0||!std::isfinite(presentation.colorR)||!std::isfinite(presentation.colorG)||!std::isfinite(presentation.colorB)||!std::isfinite(presentation.distantAlpha)||!std::isfinite(presentation.detailedAlpha)||!std::isfinite(presentation.distanceRadii)||presentation.distanceRadii<1||presentation.distantAlpha<0||presentation.distantAlpha>1||presentation.detailedAlpha<0||presentation.detailedAlpha>1||!validWeights||!validMaterial(presentation))throw std::runtime_error("invalid planetary presentation");if(presentation.regime==NC_PLANETARY_DISTANT_ONLY&&(presentation.distantAlpha!=1||presentation.detailedAlpha!=0||s->planetaryPatchCount))throw std::runtime_error("invalid distant-only planetary submission");if(presentation.regime==NC_PLANETARY_DETAILED_ONLY&&(presentation.distantAlpha!=0||presentation.detailedAlpha!=1))throw std::runtime_error("invalid detailed-only planetary submission");}
  if(s->planetaryMode!=NC_PLANETARY_CPU_REFERENCE||hasPresentation){const auto &g=s->planetaryGpu;const double cameraX=static_cast<double>(g.cameraBodyHighX)+g.cameraBodyLowX;const double cameraY=static_cast<double>(g.cameraBodyHighY)+g.cameraBodyLowY;const double cameraZ=static_cast<double>(g.cameraBodyHighZ)+g.cameraBodyLowZ;const double cameraRadius=std::sqrt(cameraX*cameraX+cameraY*cameraY+cameraZ*cameraZ);const double physicalSurfaceRadius=cameraRadius-double(g.surfaceAltitudeMetres);const double radius=static_cast<double>(g.radiusHigh)+g.radiusLow;const bool finite=std::isfinite(g.cameraBodyHighX)&&std::isfinite(g.cameraBodyHighY)&&std::isfinite(g.cameraBodyHighZ)&&std::isfinite(g.radiusHigh)&&std::isfinite(g.cameraBodyLowX)&&std::isfinite(g.cameraBodyLowY)&&std::isfinite(g.cameraBodyLowZ)&&std::isfinite(g.radiusLow)&&std::isfinite(g.refinementThreshold)&&std::isfinite(g.nearFieldAltitudeRadii)&&std::isfinite(g.surfaceAltitudeMetres)&&std::isfinite(g.maximumTerrainHeightMetres)&&std::isfinite(g.viewForwardX)&&std::isfinite(g.viewForwardY)&&std::isfinite(g.viewForwardZ)&&std::isfinite(g.viewHalfAngleRadians)&&std::isfinite(g.viewportHeightPixels)&&std::isfinite(g.verticalTanHalfFov)&&std::isfinite(g.targetTexelPixels)&&std::isfinite(g.requestedAlbedoLevel)&&std::isfinite(cameraRadius)&&std::isfinite(physicalSurfaceRadius);const float viewLength=g.viewForwardX*g.viewForwardX+g.viewForwardY*g.viewForwardY+g.viewForwardZ*g.viewForwardZ;const bool production=s->planetarySurfaceMode==NC_PLANETARY_SURFACE_PRODUCTION_CUBE;constexpr float physicalMinimumClearance=10.0f,invariantTolerance=0.0001f;if(!finite||radius<=0||g.refinementThreshold<=0||g.nearFieldAltitudeRadii<=0||g.surfaceAltitudeMetres<0||g.maximumTerrainHeightMetres<0||g.maximumLevel>24||!g.outputCapacity||g.outputCapacity>GpuPatchCapacity||g.terrainFrame||std::abs(viewLength-1)>1e-4f||g.viewHalfAngleRadians<=0||g.viewHalfAngleRadians>=1.5707964f||g.viewportHeightPixels<=0||g.verticalTanHalfFov<=0||g.targetTexelPixels<=0)throw std::runtime_error("invalid planetary GPU constants");if(production&&g.surfaceAltitudeMetres<physicalMinimumClearance-invariantTolerance){char message[320];std::snprintf(message,sizeof message,"production camera clearance escape: r=%.17g; surface=%.17g; clearance=%.17g; required=%.17g",cameraRadius,physicalSurfaceRadius,double(g.surfaceAltitudeMetres),double(physicalMinimumClearance));a.Log(NC_LOG_ALWAYS,message);throw std::runtime_error("production camera clearance invariant failed at final GPU submission");}if((g.terrainVersion==0)!=(g.maximumTerrainHeightMetres==0))throw std::runtime_error("inconsistent planetary terrain constants");if(production&&(s->planetaryMode==NC_PLANETARY_CPU_REFERENCE||g.terrainVersion!=5))throw std::runtime_error("invalid production cube-sphere authority");if(hasPresentation&&(!bodyCenterMatches(cameraX,cameraY,cameraZ)||presentation.radius!=g.radiusHigh))throw std::runtime_error("inconsistent planetary presentation authority");if(s->planetaryMode==NC_PLANETARY_GPU_PRODUCTION&&s->planetaryPatchCount)throw std::runtime_error("GPU planetary mode received CPU leaves");}
  const bool dynamicPointers=s->anchoredSurfacePatches!=nullptr;
  if(std::any_of(std::begin(s->anchoredSurfacePadding),std::end(s->anchoredSurfacePadding),[](uint32_t value){return value!=0u;})||s->anchoredSurfaceFlags>3u||s->anchoredSurfaceCacheSlotCount>AnchoredSurfaceMaximumCacheSlots||
     (s->anchoredSurfacePatchCount&&!dynamicPointers)||(!s->anchoredSurfacePatchCount&&s->anchoredSurfaceFlags)||
     (s->anchoredSurfaceFlags&&!(s->anchoredSurfaceFlags&1u))||
     (s->anchoredSurfaceCacheSlotCount!=0u)!=(s->anchoredSurfacePatches!=nullptr))throw std::runtime_error("invalid dynamic anchored surface submission");
  if(s->anchoredSurfacePatchCount){const auto &f=s->anchoredSurfacePresentation;const float values[]{
    f.origin.high[0],f.origin.high[1],f.origin.high[2],f.origin.low[0],f.origin.low[1],f.origin.low[2],
    f.east.high[0],f.east.high[1],f.east.high[2],f.east.low[0],f.east.low[1],f.east.low[2],
    f.north.high[0],f.north.high[1],f.north.high[2],f.north.low[0],f.north.low[1],f.north.low[2],
    f.up.high[0],f.up.high[1],f.up.high[2],f.up.low[0],f.up.low[1],f.up.low[2]};
    for(float value:values)if(!std::isfinite(value))throw std::runtime_error("invalid anchored spherical-billboard frame");
    const double ex=double(f.east.high[0])+f.east.low[0],ey=double(f.east.high[1])+f.east.low[1],ez=double(f.east.high[2])+f.east.low[2];
    const double nx=double(f.north.high[0])+f.north.low[0],ny=double(f.north.high[1])+f.north.low[1],nz=double(f.north.high[2])+f.north.low[2];
    const double ux=double(f.up.high[0])+f.up.low[0],uy=double(f.up.high[1])+f.up.low[1],uz=double(f.up.high[2])+f.up.low[2];
    const auto length=[](double x,double y,double z){return x*x+y*y+z*z;};
    const uint64_t body=uint64_t(f.bodyIdLow)|(uint64_t(f.bodyIdHigh)<<32u);
    if(body!=6u||!f.presentationGeneration||((f.snapIdentity>>3u)&31u)>24u||
       std::abs(length(ex,ey,ez)-1)>1e-10||std::abs(length(nx,ny,nz)-1)>1e-10||std::abs(length(ux,uy,uz)-1)>1e-10||
       std::abs(ex*nx+ey*ny+ez*nz)>1e-10||std::abs(ex*ux+ey*uy+ez*uz)>1e-10||std::abs(nx*ux+ny*uy+nz*uz)>1e-10)
      throw std::runtime_error("invalid anchored spherical-billboard authority");}
  const auto &lighting=s->solarLighting;const uint32_t hudPreset=lighting.speedHud&255u,hudAlpha=(lighting.speedHud>>8)&255u;if(lighting.enabled>1||(lighting.speedHud&0xffff0000u)||(hudPreset==0)!=(hudAlpha==0)||hudPreset>15)throw std::runtime_error("invalid Solar lighting flags");if(lighting.enabled&&(!std::isfinite(lighting.sourceCenterX)||!std::isfinite(lighting.sourceCenterY)||!std::isfinite(lighting.sourceCenterZ)||!std::isfinite(lighting.exposure)||lighting.exposure<=0||!std::isfinite(lighting.photosphereR)||lighting.photosphereR<0||!std::isfinite(lighting.photosphereG)||lighting.photosphereG<0||!std::isfinite(lighting.photosphereB)||lighting.photosphereB<0||!std::isfinite(lighting.ambientFloor)||lighting.ambientFloor<0||lighting.ambientFloor>1||!std::isfinite(lighting.sourceRadiance)||lighting.sourceRadiance<=1||!std::isfinite(lighting.glowStrength)||lighting.glowStrength<0||lighting.glowStrength>4))throw std::runtime_error("invalid Solar lighting presentation");
}
void Window(App &a) {
  WNDCLASSW wc{.lpfnWndProc = Proc,
               .hInstance = GetModuleHandleW(nullptr),
               .lpszClassName = L"NovaCoreWindow"};
  RegisterClassW(&wc);
  auto requestedDimension=[](const char *name,int fallback){char buffer[32]{};const DWORD length=GetEnvironmentVariableA(name,buffer,DWORD(std::size(buffer)));if(!length||length>=std::size(buffer))return fallback;char *end=nullptr;const auto parsed=std::strtol(buffer,&end,10);return end!=buffer&&*end=='\0'&&parsed>=320&&parsed<=8192?int(parsed):fallback;};
  auto environmentEnabled=[](const char *name){char buffer[8]{};const DWORD length=GetEnvironmentVariableA(name,buffer,DWORD(std::size(buffer)));return length==1&&buffer[0]=='1';};
  const int clientWidth=requestedDimension("NOVACORE_WINDOW_CLIENT_WIDTH",Width);
  const int clientHeight=requestedDimension("NOVACORE_WINDOW_CLIENT_HEIGHT",Height);
  const bool borderless=environmentEnabled("NOVACORE_WINDOW_BORDERLESS");
  const DWORD windowStyle=borderless?WS_POPUP:WS_OVERLAPPEDWINDOW;
  RECT outer{0,0,clientWidth,clientHeight};AdjustWindowRect(&outer,windowStyle,FALSE);
  a.window =
      CreateWindowExW(0, wc.lpszClassName, L"NovaCore - Generic Mesh Rendering",
                      windowStyle, borderless?0:CW_USEDEFAULT, borderless?0:CW_USEDEFAULT, outer.right-outer.left,
                      outer.bottom-outer.top, nullptr, nullptr, wc.hInstance, nullptr);
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
  return q.Complete() && features.shaderFloat64 && features.samplerAnisotropy &&
         features.multiDrawIndirect && features.tessellationShader && features.pipelineStatisticsQuery && std::any_of(x.begin(), x.end(), [](auto &e) {
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
  VkPhysicalDeviceFeatures enabledFeatures{};enabledFeatures.shaderFloat64=VK_TRUE;enabledFeatures.samplerAnisotropy=VK_TRUE;enabledFeatures.multiDrawIndirect=VK_TRUE;enabledFeatures.tessellationShader=VK_TRUE;enabledFeatures.pipelineStatisticsQuery=VK_TRUE;ci.pEnabledFeatures=&enabledFeatures;
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
  if(a.stellarSunPipeline)vkDestroyPipeline(a.device,a.stellarSunPipeline,nullptr);
  if(a.stellarGlowPipeline)vkDestroyPipeline(a.device,a.stellarGlowPipeline,nullptr);
  if (a.planetaryPipeline)
    vkDestroyPipeline(a.device,a.planetaryPipeline,nullptr);
  if (a.productionPlanetaryPipeline)
    vkDestroyPipeline(a.device,a.productionPlanetaryPipeline,nullptr);
  if (a.productionPlanetaryFillPipeline)
    vkDestroyPipeline(a.device,a.productionPlanetaryFillPipeline,nullptr);
  if (a.anchoredTerrainPipeline)
    vkDestroyPipeline(a.device,a.anchoredTerrainPipeline,nullptr);
  if(a.productionBillboardPipeline)vkDestroyPipeline(a.device,a.productionBillboardPipeline,nullptr);
  if(a.productionBillboardResetPipeline)vkDestroyPipeline(a.device,a.productionBillboardResetPipeline,nullptr);
  if(a.productionBillboardCullPipeline)vkDestroyPipeline(a.device,a.productionBillboardCullPipeline,nullptr);
  if(a.productionBillboardCompactPipeline)vkDestroyPipeline(a.device,a.productionBillboardCompactPipeline,nullptr);
  if(a.productionBillboardIncomingResetPipeline)vkDestroyPipeline(a.device,a.productionBillboardIncomingResetPipeline,nullptr);
  if(a.productionBillboardIncomingCullPipeline)vkDestroyPipeline(a.device,a.productionBillboardIncomingCullPipeline,nullptr);
  if(a.productionBillboardIncomingCompactPipeline)vkDestroyPipeline(a.device,a.productionBillboardIncomingCompactPipeline,nullptr);
  if (a.planetaryComputePipeline)
    vkDestroyPipeline(a.device,a.planetaryComputePipeline,nullptr);
  if (a.planetaryTerrainPipeline)
    vkDestroyPipeline(a.device,a.planetaryTerrainPipeline,nullptr);
  if (a.productionPlanetaryTerrainPipeline)
    vkDestroyPipeline(a.device,a.productionPlanetaryTerrainPipeline,nullptr);
  if(a.naturalGlobalPreparePipeline)vkDestroyPipeline(a.device,a.naturalGlobalPreparePipeline,nullptr);
  if(a.naturalAnchoredPreparePipeline)vkDestroyPipeline(a.device,a.naturalAnchoredPreparePipeline,nullptr);
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
  a.stellarSunPipeline={};
  a.stellarGlowPipeline={};
  a.planetaryPipeline = {};
  a.productionPlanetaryPipeline = {};
  a.productionPlanetaryFillPipeline = {};
  a.anchoredTerrainPipeline = {};
  a.productionBillboardPipeline={};a.productionBillboardResetPipeline={};a.productionBillboardCullPipeline={};a.productionBillboardCompactPipeline={};a.productionBillboardIncomingResetPipeline={};a.productionBillboardIncomingCullPipeline={};a.productionBillboardIncomingCompactPipeline={};
  a.planetaryComputePipeline = {};
  a.planetaryTerrainPipeline = {};
  a.productionPlanetaryTerrainPipeline={};
  a.naturalGlobalPreparePipeline={};
  a.naturalAnchoredPreparePipeline={};
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
std::string ModuleDirectory();
void ProductionIoWorker(ProductionIoState *state){
  for(;;){ProductionRequest request{};{std::unique_lock lock(state->mutex);state->wake.wait(lock,[&]{return state->stop||state->requestCount;});if(state->stop)return;request=state->requests[state->requestHead];state->requestHead=(state->requestHead+1)%state->requests.size();state->requestCount--;}
    auto payload=std::make_unique<nc::production::Payload>();std::string error;const bool valid=state->pack->Read(request.id,*payload,error);std::unique_lock lock(state->mutex);state->wake.wait(lock,[&]{return state->stop||std::any_of(state->ready.begin(),state->ready.end(),[](const auto &value){return value.state==0;});});if(state->stop)return;auto ready=std::find_if(state->ready.begin(),state->ready.end(),[](const auto &value){return value.state==0;});ready->request=request;ready->payload=valid?std::move(payload):nullptr;ready->state=valid?2u:3u;if(valid)state->diskLoads++;else state->digestFailures++;state->wake.notify_all();}
}
void LogLoadedRuntimePaths(App &a) {
  HMODULE module{};
  char modulePath[MAX_PATH]{},workingDirectory[MAX_PATH]{},productionShader[MAX_PATH]{},distantShader[MAX_PATH]{};
  if(!GetModuleHandleExA(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS|GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,reinterpret_cast<LPCSTR>(&LogLoadedRuntimePaths),&module)||
     !GetModuleFileNameA(module,modulePath,MAX_PATH))std::strcpy(modulePath,"unavailable");
  if(!GetCurrentDirectoryA(MAX_PATH,workingDirectory))std::strcpy(workingDirectory,"unavailable");
  if(!GetFullPathNameA("shaders/planetary_production.frag.spv",MAX_PATH,productionShader,nullptr))std::strcpy(productionShader,"unavailable");
  if(!GetFullPathNameA("shaders/distant_planet.frag.spv",MAX_PATH,distantShader,nullptr))std::strcpy(distantShader,"unavailable");
  char message[MAX_PATH*4];std::snprintf(message,sizeof message,"Loaded runtime paths: module=%s; cwd=%s; productionFragment=%s; distantFragment=%s",modulePath,workingDirectory,productionShader,distantShader);a.Log(NC_LOG_ALWAYS,message);
}
void CreateProductionImage(App &a,VkFormat format,VkImage &image,VkDeviceMemory &memory,VkImageView &view){
  VkImageCreateInfo create{VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO};create.imageType=VK_IMAGE_TYPE_2D;create.format=format;create.extent={nc::production::StoredExtent,nc::production::StoredExtent,1};create.mipLevels=1;create.arrayLayers=ProductionPayloadSlots;create.samples=VK_SAMPLE_COUNT_1_BIT;create.tiling=VK_IMAGE_TILING_OPTIMAL;create.usage=VK_IMAGE_USAGE_TRANSFER_DST_BIT|VK_IMAGE_USAGE_SAMPLED_BIT;create.sharingMode=VK_SHARING_MODE_EXCLUSIVE;create.initialLayout=VK_IMAGE_LAYOUT_UNDEFINED;a.Check(vkCreateImage(a.device,&create,nullptr,&image),"production cube image failed");VkMemoryRequirements requirements{};vkGetImageMemoryRequirements(a.device,image,&requirements);VkMemoryAllocateInfo allocation{VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO};allocation.allocationSize=requirements.size;allocation.memoryTypeIndex=Memory(a,requirements.memoryTypeBits,VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT);a.Check(vkAllocateMemory(a.device,&allocation,nullptr,&memory),"production cube image memory failed");a.Check(vkBindImageMemory(a.device,image,memory,0),"production cube image bind failed");VkImageViewCreateInfo viewCreate{VK_STRUCTURE_TYPE_IMAGE_VIEW_CREATE_INFO};viewCreate.image=image;viewCreate.viewType=VK_IMAGE_VIEW_TYPE_2D_ARRAY;viewCreate.format=format;viewCreate.subresourceRange.aspectMask=VK_IMAGE_ASPECT_COLOR_BIT;viewCreate.subresourceRange.levelCount=1;viewCreate.subresourceRange.layerCount=ProductionPayloadSlots;a.Check(vkCreateImageView(a.device,&viewCreate,nullptr,&view),"production cube image view failed");
}
void CreateProductionCubeSurface(App &a){
  if(!a.productionTerrainPath.empty()){a.productionPack=std::make_unique<nc::production::Pack>();std::string error;if(!a.productionPack->Open(a.productionTerrainPath,error)||!a.productionPack->IsProductionLayout())throw std::runtime_error("Production cube pack unavailable: "+(error.empty()?std::string("production layout mismatch"):error));}
CreateProductionImage(a,VK_FORMAT_R8G8B8A8_SRGB,a.productionImages[0],a.productionImageMemory[0],a.productionImageViews[0]);CreateProductionImage(a,VK_FORMAT_R16_UNORM,a.productionImages[1],a.productionImageMemory[1],a.productionImageViews[1]);CreateProductionImage(a,VK_FORMAT_R8_UNORM,a.productionImages[2],a.productionImageMemory[2],a.productionImageViews[2]);VkPhysicalDeviceProperties properties{};vkGetPhysicalDeviceProperties(a.physical,&properties);VkSamplerCreateInfo sampler{VK_STRUCTURE_TYPE_SAMPLER_CREATE_INFO};sampler.magFilter=VK_FILTER_LINEAR;sampler.minFilter=VK_FILTER_LINEAR;sampler.mipmapMode=VK_SAMPLER_MIPMAP_MODE_NEAREST;sampler.addressModeU=VK_SAMPLER_ADDRESS_MODE_CLAMP_TO_EDGE;sampler.addressModeV=VK_SAMPLER_ADDRESS_MODE_CLAMP_TO_EDGE;sampler.addressModeW=VK_SAMPLER_ADDRESS_MODE_CLAMP_TO_EDGE;sampler.anisotropyEnable=VK_TRUE;sampler.maxAnisotropy=std::min(8.0f,properties.limits.maxSamplerAnisotropy);sampler.maxLod=0;a.Check(vkCreateSampler(a.device,&sampler,nullptr,&a.productionSampler),"production cube sampler failed");CreateHostBuffer(a,ProductionStagingBytes,VK_BUFFER_USAGE_TRANSFER_SRC_BIT,a.productionStagingBuffer,a.productionStagingMemory,a.productionStagingMapped,"production cube staging failed");CreateHostBuffer(a,sizeof(uint32_t)*ProductionLookupCapacity,VK_BUFFER_USAGE_STORAGE_BUFFER_BIT,a.productionLayerLookupBuffer,a.productionLayerLookupMemory,a.productionLayerLookupMapped,"production layer lookup buffer failed");a.productionLayerTerrainSlot.fill(UINT32_MAX);a.productionLayerLastUse.fill(0);a.productionLayerGeneration.fill(0);
  if(a.productionPack){a.productionIo=std::make_unique<ProductionIoState>();a.productionIo->pack=a.productionPack.get();a.productionIo->worker=std::thread(ProductionIoWorker,a.productionIo.get());char message[320];std::snprintf(message,sizeof message,"Production cube pack: terrain-v%u; maxLevel=%u; records=%u; raw transactional payloads; physicalSlots=%u; immutableGlobalRecords=%u; uploadBudget=%u",a.productionPack->TerrainVersion(),a.productionPack->MaximumLevel(),a.productionPack->RecordCount(),ProductionPayloadSlots,a.productionPack->RecordCount(),ProductionUploadBudget);a.Log(NC_LOG_ALWAYS,message);}
}
void DestroyProductionCubeSurface(App &a){
  if(a.productionIo){{std::lock_guard lock(a.productionIo->mutex);a.productionIo->stop=true;}a.productionIo->wake.notify_all();if(a.productionIo->worker.joinable())a.productionIo->worker.join();a.productionIo.reset();}a.productionLayerTerrainSlot.fill(UINT32_MAX);for(auto &value:a.productionElevationCpu)value.clear();a.productionPendingUploads=0;DestroyHostBuffer(a,a.productionLayerLookupBuffer,a.productionLayerLookupMemory,a.productionLayerLookupMapped);DestroyHostBuffer(a,a.productionStagingBuffer,a.productionStagingMemory,a.productionStagingMapped);if(a.productionSampler)vkDestroySampler(a.device,a.productionSampler,nullptr);for(uint32_t channel=0;channel<3;channel++){if(a.productionImageViews[channel])vkDestroyImageView(a.device,a.productionImageViews[channel],nullptr);if(a.productionImages[channel])vkDestroyImage(a.device,a.productionImages[channel],nullptr);if(a.productionImageMemory[channel])vkFreeMemory(a.device,a.productionImageMemory[channel],nullptr);}a.productionPack.reset();
}
float ProductionElevationMetres(uint16_t encoded){return -11000.0f+float(encoded)*(20000.0f/65535.0f);}
float ProductionSampleElevation(const std::vector<uint16_t> &elevation,double localX,double localY){
  const double pixelX=double(nc::production::GutterTexels)-0.5+std::clamp(localX,0.0,double(nc::production::InteriorTexels));
  const double pixelY=double(nc::production::GutterTexels)-0.5+std::clamp(localY,0.0,double(nc::production::InteriorTexels));
  const uint32_t x0=std::min(uint32_t(std::floor(pixelX)),nc::production::StoredExtent-1),y0=std::min(uint32_t(std::floor(pixelY)),nc::production::StoredExtent-1);
  const uint32_t x1=std::min(x0+1,nc::production::StoredExtent-1),y1=std::min(y0+1,nc::production::StoredExtent-1);
  const float tx=float(pixelX-double(x0)),ty=float(pixelY-double(y0));
  const float h00=ProductionElevationMetres(elevation[y0*nc::production::StoredExtent+x0]),h10=ProductionElevationMetres(elevation[y0*nc::production::StoredExtent+x1]);
  const float h01=ProductionElevationMetres(elevation[y1*nc::production::StoredExtent+x0]),h11=ProductionElevationMetres(elevation[y1*nc::production::StoredExtent+x1]);
  return std::lerp(std::lerp(h00,h10,tx),std::lerp(h01,h11,tx),ty);
}
bool ProductionKeyMatches(const uint32_t *key,const nc::production::PatchId &id){return key[0]==uint32_t(id.bodyId)&&key[1]==uint32_t(id.bodyId>>32)&&key[2]==id.terrainVersion&&key[3]!=0u&&key[4]==id.face&&key[5]==id.level&&key[6]==id.x&&key[7]==id.y;}
void CompleteProductionUploads(App &a){
  if(!a.productionPendingUploads)return;
  auto *words=static_cast<uint32_t*>(a.terrainKeyMapped);auto *lookup=static_cast<uint32_t*>(a.productionLayerLookupMapped);
  for(uint32_t index=0;index<a.productionPendingUploads;index++){
    const uint32_t terrainSlot=a.productionUploadTerrainSlots[index],layer=a.productionUploadLayers[index];const uint32_t owner=a.productionLayerTerrainSlot[layer];uint32_t *key=words+terrainSlot*12u;
    if(owner==terrainSlot&&a.productionLayerGeneration[layer]==a.productionUploadGenerations[index]&&ProductionKeyMatches(key,a.productionUploadRequests[index].id)){
      key[10]=layer+1u;const auto &id=a.productionUploadRequests[index].id;const uint64_t ordinal=nc::production::Pack::Ordinal(id.face,id.level,id.x,id.y);if(lookup&&ordinal<ProductionLookupCapacity)lookup[ordinal]=layer+1u;a.productionLayerLastUse[layer]=a.frame;
    }
  }
  a.productionPendingUploads=0;
  if(!a.productionRootsReadyLogged&&lookup){
    uint32_t rootMask=0u;for(uint32_t face=0;face<6;face++)if(lookup[nc::production::Pack::Ordinal(face,0,0,0)]!=0u)rootMask|=1u<<face;
    if(rootMask==0x3fu){a.Log(NC_LOG_ALWAYS,"Earth terrain-v5 material roots ready: mask=0x3F; renderer-lifetime pinned=true");a.productionRootsReadyLogged=true;}
  }
}
void QueueProductionRequests(App &a){
  if(!a.productionIo||!a.productionPack||!a.terrainKeyMapped)return;auto *words=static_cast<uint32_t*>(a.terrainKeyMapped);auto &io=*a.productionIo;std::lock_guard lock(io.mutex);
  for(uint32_t slot=0;slot<TerrainCacheCapacity;slot++){const uint32_t *key=words+slot*12u;if(key[3]==0u||key[10]==0u)continue;const uint32_t layer=key[10]-1u;if(layer<ProductionPayloadSlots&&a.productionLayerTerrainSlot[layer]==slot)a.productionLayerLastUse[layer]=a.frame;}
  for(uint32_t slot=0;slot<TerrainCacheCapacity;slot++){uint32_t *key=words+slot*12u;if(key[3]==0u||key[10]!=0u||key[11]!=0u)continue;nc::production::PatchId id{uint64_t(key[0])|uint64_t(key[1])<<32,key[2],key[4],key[5],key[6],key[7]};if(id.level==0u&&a.productionLayerLookupMapped&&a.terrainSampleMapped){const uint64_t ordinal=nc::production::Pack::Ordinal(id.face,0,0,0);const uint32_t resident=ordinal<ProductionLookupCapacity?static_cast<const uint32_t*>(a.productionLayerLookupMapped)[ordinal]:0u;const uint32_t layer=resident?resident-1u:UINT32_MAX;if(layer<ProductionPayloadSlots&&a.productionLayerPatch[layer]==id&&!a.productionElevationCpu[layer].empty()){auto *terrain=static_cast<float*>(a.terrainSampleMapped);for(uint32_t y=0;y<17;y++)for(uint32_t x=0;x<17;x++){const float elevation=ProductionSampleElevation(a.productionElevationCpu[layer],double(x)*16.0,double(y)*16.0);const uint32_t sample=(slot*TerrainGridVertexCount+y*17u+x)*2u;terrain[sample]=elevation;terrain[sample+1]=elevation;}key[10]=resident;key[11]=2u;a.productionLayerLastUse[layer]=a.frame;continue;}}if(!a.productionPack->Contains(id)){key[11]=3u;continue;}if(io.requestCount==io.requests.size()){io.queueDrops++;a.productionQueueDrops++;break;}io.requests[io.requestTail]={id,slot};io.requestTail=(io.requestTail+1)%io.requests.size();io.requestCount++;key[11]=1u;a.productionRequests++;}io.wake.notify_all();
}
void PrepareProductionUploads(App &a){
  CompleteProductionUploads(a);QueueProductionRequests(a);if(!a.productionIo||a.productionPendingUploads)return;auto &io=*a.productionIo;auto *words=static_cast<uint32_t*>(a.terrainKeyMapped);auto *terrain=static_cast<float*>(a.terrainSampleMapped);std::lock_guard lock(io.mutex);
  for(auto &ready:io.ready){if(a.productionPendingUploads>=ProductionUploadBudget)break;if(ready.state==3u){uint32_t *key=words+ready.request.terrainSlot*12u;if(ProductionKeyMatches(key,ready.request.id))key[11]=3u;ready={};continue;}if(ready.state!=2u||!ready.payload)continue;const uint32_t terrainSlot=ready.request.terrainSlot;uint32_t *key=words+terrainSlot*12u;if(!ProductionKeyMatches(key,ready.request.id)||key[10]!=0u){ready={};continue;}uint32_t layer=UINT32_MAX;for(uint32_t candidate=0;candidate<ProductionPayloadSlots;candidate++)if(a.productionLayerTerrainSlot[candidate]==UINT32_MAX){layer=candidate;break;}if(layer==UINT32_MAX){ready={};key[11]=3u;continue;}a.productionLayerGeneration[layer]++;a.productionLayerTerrainSlot[layer]=terrainSlot;a.productionLayerPatch[layer]=ready.request.id;a.productionLayerLastUse[layer]=a.frame;a.productionElevationCpu[layer]=ready.payload->elevation;
    const uint32_t batch=a.productionPendingUploads,base=batch*(ProductionAlbedoLayerBytes+ProductionElevationLayerBytes+ProductionLandLayerBytes);auto *destination=static_cast<uint8_t*>(a.productionStagingMapped)+base;for(uint32_t texel=0;texel<nc::production::StoredExtent*nc::production::StoredExtent;texel++){destination[texel*4]=ready.payload->albedoRgb[texel*3];destination[texel*4+1]=ready.payload->albedoRgb[texel*3+1];destination[texel*4+2]=ready.payload->albedoRgb[texel*3+2];destination[texel*4+3]=255;}std::memcpy(destination+ProductionAlbedoLayerBytes,ready.payload->elevation.data(),ProductionElevationLayerBytes);std::memcpy(destination+ProductionAlbedoLayerBytes+ProductionElevationLayerBytes,ready.payload->land.data(),ProductionLandLayerBytes);
    uint32_t parentLayer=layer;if(ready.request.id.level>0u){const nc::production::PatchId parent{ready.request.id.bodyId,ready.request.id.terrainVersion,ready.request.id.face,ready.request.id.level-1,ready.request.id.x>>1,ready.request.id.y>>1};for(uint32_t candidate=0;candidate<ProductionPayloadSlots;candidate++){const uint32_t owner=a.productionLayerTerrainSlot[candidate];if(owner==UINT32_MAX)continue;const uint32_t *ownerKey=words+owner*12u;if(ownerKey[0]==uint32_t(parent.bodyId)&&ownerKey[1]==uint32_t(parent.bodyId>>32)&&ownerKey[2]==parent.terrainVersion&&ownerKey[4]==parent.face&&ownerKey[5]==parent.level&&ownerKey[6]==parent.x&&ownerKey[7]==parent.y){parentLayer=candidate;break;}}}
    for(uint32_t y=0;y<17;y++)for(uint32_t x=0;x<17;x++){const double childX=double(x)*16.0,childY=double(y)*16.0;double parentX=childX,parentY=childY;if(ready.request.id.level>0u){parentX=double(ready.request.id.x&1u)*128.0+double(x)*8.0;parentY=double(ready.request.id.y&1u)*128.0+double(y)*8.0;}const uint32_t sample=(terrainSlot*TerrainGridVertexCount+y*17u+x)*2u;terrain[sample]=ProductionSampleElevation(a.productionElevationCpu[parentLayer],parentX,parentY);terrain[sample+1]=ProductionSampleElevation(ready.payload->elevation,childX,childY);}
    a.productionUploadLayers[batch]=layer;a.productionUploadTerrainSlots[batch]=terrainSlot;a.productionUploadGenerations[batch]=a.productionLayerGeneration[layer];a.productionUploadRequests[batch]=ready.request;a.productionPendingUploads++;a.productionUploads++;a.productionUploadBytes+=ProductionAlbedoLayerBytes+ProductionElevationLayerBytes+ProductionLandLayerBytes;key[11]=2u;ready={};
  }io.wake.notify_all();
}
void RecordProductionUploads(App &a,VkCommandBuffer command){
  if(!a.productionPendingUploads||!a.submission)return;VkImageMemoryBarrier before[3]{};for(uint32_t channel=0;channel<3;channel++){before[channel].sType=VK_STRUCTURE_TYPE_IMAGE_MEMORY_BARRIER;before[channel].srcAccessMask=a.productionImagesInitialized?VK_ACCESS_SHADER_READ_BIT:0;before[channel].dstAccessMask=VK_ACCESS_TRANSFER_WRITE_BIT;before[channel].oldLayout=a.productionImagesInitialized?VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL:VK_IMAGE_LAYOUT_UNDEFINED;before[channel].newLayout=VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL;before[channel].image=a.productionImages[channel];before[channel].subresourceRange.aspectMask=VK_IMAGE_ASPECT_COLOR_BIT;before[channel].subresourceRange.levelCount=1;before[channel].subresourceRange.layerCount=ProductionPayloadSlots;}vkCmdPipelineBarrier(command,a.productionImagesInitialized?(VK_PIPELINE_STAGE_FRAGMENT_SHADER_BIT|VK_PIPELINE_STAGE_VERTEX_SHADER_BIT):VK_PIPELINE_STAGE_TOP_OF_PIPE_BIT,VK_PIPELINE_STAGE_TRANSFER_BIT,0,0,nullptr,0,nullptr,3,before);
  for(uint32_t index=0;index<a.productionPendingUploads;index++){const VkDeviceSize base=index*(ProductionAlbedoLayerBytes+ProductionElevationLayerBytes+ProductionLandLayerBytes);const VkDeviceSize offsets[3]{base,base+ProductionAlbedoLayerBytes,base+ProductionAlbedoLayerBytes+ProductionElevationLayerBytes};for(uint32_t channel=0;channel<3;channel++){VkBufferImageCopy copy{};copy.bufferOffset=offsets[channel];copy.imageSubresource.aspectMask=VK_IMAGE_ASPECT_COLOR_BIT;copy.imageSubresource.baseArrayLayer=a.productionUploadLayers[index];copy.imageSubresource.layerCount=1;copy.imageExtent={nc::production::StoredExtent,nc::production::StoredExtent,1};vkCmdCopyBufferToImage(command,a.productionStagingBuffer,a.productionImages[channel],VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,1,&copy);}}
  VkImageMemoryBarrier after[3]{before[0],before[1],before[2]};for(auto &barrier:after){barrier.srcAccessMask=VK_ACCESS_TRANSFER_WRITE_BIT;barrier.dstAccessMask=VK_ACCESS_SHADER_READ_BIT;barrier.oldLayout=VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL;barrier.newLayout=VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL;}vkCmdPipelineBarrier(command,VK_PIPELINE_STAGE_TRANSFER_BIT,VK_PIPELINE_STAGE_FRAGMENT_SHADER_BIT|VK_PIPELINE_STAGE_VERTEX_SHADER_BIT,0,0,nullptr,0,nullptr,3,after);a.productionImagesInitialized=true;
}
bool ProductionRootPayloadsReady(const App &a);
bool ProductionHierarchyPayloadsReady(const App &a){
  const auto *lookup=static_cast<const uint32_t*>(a.productionLayerLookupMapped);
  if(!lookup||!a.productionPack)return false;
  for(uint32_t ordinal=0;ordinal<a.productionPack->RecordCount();ordinal++)if(lookup[ordinal]==0u)return false;
  return true;
}
void BootstrapProductionHierarchy(App &a){
  if(!a.productionPack)return;
  const auto bootstrapStart=std::chrono::steady_clock::now();
  uint32_t injectedBatchDelayMilliseconds=0u;
  if(const char *value=std::getenv("NOVACORE_PRODUCTION_BOOTSTRAP_DELAY_MS")){char *end{};const unsigned long parsed=std::strtoul(value,&end,10);if(!end||*end!='\0'||parsed>100u)throw std::runtime_error("NOVACORE_PRODUCTION_BOOTSTRAP_DELAY_MS must be 0..100");injectedBatchDelayMilliseconds=uint32_t(parsed);}
  if(a.productionPack->RecordCount()>ProductionPayloadSlots||a.productionPack->RecordCount()>ProductionLookupCapacity)throw std::runtime_error("production hierarchy exceeds immutable residency capacity");
  if(!a.terrainKeyMapped||!a.terrainSampleMapped||!a.productionLayerLookupMapped||!a.productionStagingMapped)throw std::runtime_error("production hierarchy bootstrap resources unavailable");
  VkCommandBufferAllocateInfo allocate{VK_STRUCTURE_TYPE_COMMAND_BUFFER_ALLOCATE_INFO};allocate.commandPool=a.pool;allocate.level=VK_COMMAND_BUFFER_LEVEL_PRIMARY;allocate.commandBufferCount=1;VkCommandBuffer command{};a.Check(vkAllocateCommandBuffers(a.device,&allocate,&command),"production root bootstrap command allocation failed");
  auto *words=static_cast<uint32_t*>(a.terrainKeyMapped);auto *terrain=static_cast<float*>(a.terrainSampleMapped);
  for(uint32_t firstOrdinal=0;firstOrdinal<a.productionPack->RecordCount();firstOrdinal+=ProductionMaximumPendingUploads){
    a.productionPendingUploads=0;
    for(uint32_t ordinal=firstOrdinal;ordinal<std::min<uint32_t>(a.productionPack->RecordCount(),firstOrdinal+ProductionMaximumPendingUploads);ordinal++){
      nc::production::PatchId id;if(!a.productionPack->TryGetId(ordinal,id))throw std::runtime_error("Production hierarchy bootstrap identity unavailable at ordinal "+std::to_string(ordinal));nc::production::Payload payload;std::string error;if(!a.productionPack->Read(id,payload,error)||!payload.digestValid)throw std::runtime_error("Production hierarchy bootstrap failed at ordinal "+std::to_string(ordinal)+": "+error);
      const uint32_t batch=a.productionPendingUploads,layer=ordinal,terrainSlot=ordinal;uint32_t *key=words+terrainSlot*12u;a.productionLayerGeneration[layer]++;a.productionLayerTerrainSlot[layer]=terrainSlot;a.productionLayerPatch[layer]=id;a.productionLayerLastUse[layer]=a.frame;a.productionElevationCpu[layer]=payload.elevation;
      const uint32_t base=batch*(ProductionAlbedoLayerBytes+ProductionElevationLayerBytes+ProductionLandLayerBytes);auto *destination=static_cast<uint8_t*>(a.productionStagingMapped)+base;for(uint32_t texel=0;texel<nc::production::StoredExtent*nc::production::StoredExtent;texel++){destination[texel*4]=payload.albedoRgb[texel*3];destination[texel*4+1]=payload.albedoRgb[texel*3+1];destination[texel*4+2]=payload.albedoRgb[texel*3+2];destination[texel*4+3]=255;}std::memcpy(destination+ProductionAlbedoLayerBytes,payload.elevation.data(),ProductionElevationLayerBytes);std::memcpy(destination+ProductionAlbedoLayerBytes+ProductionElevationLayerBytes,payload.land.data(),ProductionLandLayerBytes);
      uint32_t parentLayer=layer;
      if(id.level>0u){
        const nc::production::PatchId parent{id.bodyId,id.terrainVersion,id.face,id.level-1u,id.x>>1u,id.y>>1u};
        const uint64_t parentOrdinal=nc::production::Pack::Ordinal(parent.face,parent.level,parent.x,parent.y);
        if(parentOrdinal>=ordinal||parentOrdinal>=a.productionPack->RecordCount())throw std::runtime_error("Production hierarchy bootstrap parent ordering invalid");
        parentLayer=uint32_t(parentOrdinal);
      }
      for(uint32_t y=0;y<17;y++)for(uint32_t x=0;x<17;x++){
        const double childX=double(x)*16.0,childY=double(y)*16.0;
        double parentX=childX,parentY=childY;
        if(id.level>0u){parentX=double(id.x&1u)*128.0+double(x)*8.0;parentY=double(id.y&1u)*128.0+double(y)*8.0;}
        const uint32_t sample=(terrainSlot*TerrainGridVertexCount+y*17u+x)*2u;
        terrain[sample]=ProductionSampleElevation(a.productionElevationCpu[parentLayer],parentX,parentY);
        terrain[sample+1]=ProductionSampleElevation(payload.elevation,childX,childY);
      }
      a.productionUploadLayers[batch]=layer;a.productionUploadTerrainSlots[batch]=terrainSlot;a.productionUploadGenerations[batch]=a.productionLayerGeneration[layer];a.productionUploadRequests[batch]={id,terrainSlot};a.productionPendingUploads++;a.productionUploads++;a.productionUploadBytes+=ProductionAlbedoLayerBytes+ProductionElevationLayerBytes+ProductionLandLayerBytes;key[10]=0u;key[11]=2u;
    }
    if(injectedBatchDelayMilliseconds)std::this_thread::sleep_for(std::chrono::milliseconds(injectedBatchDelayMilliseconds));
    VkCommandBufferBeginInfo begin{VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO};begin.flags=VK_COMMAND_BUFFER_USAGE_ONE_TIME_SUBMIT_BIT;a.Check(vkBeginCommandBuffer(command,&begin),"production root bootstrap command begin failed");RecordProductionUploads(a,command);a.Check(vkEndCommandBuffer(command),"production root bootstrap command end failed");VkSubmitInfo submit{VK_STRUCTURE_TYPE_SUBMIT_INFO};submit.commandBufferCount=1;submit.pCommandBuffers=&command;a.Check(vkQueueSubmit(a.graphicsQueue,1,&submit,VK_NULL_HANDLE),"production root bootstrap submit failed");a.Check(vkQueueWaitIdle(a.graphicsQueue),"production root bootstrap wait failed");CompleteProductionUploads(a);a.Check(vkResetCommandBuffer(command,0),"production root bootstrap command reset failed");
  }
  vkFreeCommandBuffers(a.device,a.pool,1,&command);if(!ProductionRootPayloadsReady(a)||!ProductionHierarchyPayloadsReady(a))throw std::runtime_error("production hierarchy bootstrap did not publish the complete L0-L2 surface");uint32_t publishedTerrainKeys=0u;for(uint32_t ordinal=0;ordinal<a.productionPack->RecordCount();ordinal++){const uint32_t *key=words+ordinal*12u;publishedTerrainKeys+=key[3]!=0u&&key[10]!=0u;}if(publishedTerrainKeys!=a.productionPack->RecordCount())throw std::runtime_error("production hierarchy bootstrap did not publish terrain-cache bindings");SeedProductionTerrainCacheHighWater(a);const double bootstrapMilliseconds=std::chrono::duration<double,std::milli>(std::chrono::steady_clock::now()-bootstrapStart).count();char message[288];std::snprintf(message,sizeof message,"Earth terrain-v5 complete L0-L2 hierarchy synchronously resident before first submitted presentation frame: records=%u; terrainBindings=%u; injectedBatchDelayMs=%u; milliseconds=%.3f",a.productionPack->RecordCount(),publishedTerrainKeys,injectedBatchDelayMilliseconds,bootstrapMilliseconds);a.Log(NC_LOG_ALWAYS,message);
}
void LocalIoWorker(LocalIoState*state){
  for(;;){LocalRequest request{};{std::unique_lock lock(state->mutex);state->wake.wait(lock,[&]{return state->stop||state->requestCount;});if(state->stop)return;request=state->requests[state->requestHead];state->requestHead=(state->requestHead+1)%state->requests.size();state->requestCount--;}
    auto payload=std::make_unique<nc::localterrain::Payload>();std::string error;const bool valid=state->pack->Read(request.id,*payload,error);std::unique_lock lock(state->mutex);state->wake.wait(lock,[&]{return state->stop||std::any_of(state->ready.begin(),state->ready.end(),[](const auto&value){return value.state==0;});});if(state->stop)return;auto ready=std::find_if(state->ready.begin(),state->ready.end(),[](const auto&value){return value.state==0;});ready->request=request;ready->payload=valid?std::move(payload):nullptr;ready->state=valid?2u:3u;if(valid){state->diskLoads++;state->bytesRead+=ready->payload->storedBytes;state->bytesTranscoded+=ready->payload->transcodedBytes;state->transcodeMilliseconds+=ready->payload->transcodeMilliseconds;}else state->digestFailures++;state->wake.notify_all();}
}
void CreateLocalImage(App&a,VkFormat format,uint32_t bytes,VkImage&image,VkDeviceMemory&memory,VkImageView&view){
  VkFormatProperties properties{};vkGetPhysicalDeviceFormatProperties(a.physical,format,&properties);if((properties.optimalTilingFeatures&(VK_FORMAT_FEATURE_SAMPLED_IMAGE_BIT|VK_FORMAT_FEATURE_TRANSFER_DST_BIT))!=(VK_FORMAT_FEATURE_SAMPLED_IMAGE_BIT|VK_FORMAT_FEATURE_TRANSFER_DST_BIT))throw std::runtime_error("required BC local terrain format unsupported");
  VkImageCreateInfo create{VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO};create.imageType=VK_IMAGE_TYPE_2D;create.format=format;create.extent={nc::localterrain::StoredExtent,nc::localterrain::StoredExtent,1};create.mipLevels=1;create.arrayLayers=LocalPayloadSlots;create.samples=VK_SAMPLE_COUNT_1_BIT;create.tiling=VK_IMAGE_TILING_OPTIMAL;create.usage=VK_IMAGE_USAGE_TRANSFER_DST_BIT|VK_IMAGE_USAGE_SAMPLED_BIT;create.sharingMode=VK_SHARING_MODE_EXCLUSIVE;create.initialLayout=VK_IMAGE_LAYOUT_UNDEFINED;a.Check(vkCreateImage(a.device,&create,nullptr,&image),"local terrain BC image failed");VkMemoryRequirements requirements{};vkGetImageMemoryRequirements(a.device,image,&requirements);VkMemoryAllocateInfo allocation{VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO};allocation.allocationSize=requirements.size;allocation.memoryTypeIndex=Memory(a,requirements.memoryTypeBits,VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT);a.Check(vkAllocateMemory(a.device,&allocation,nullptr,&memory),"local terrain BC memory failed");a.Check(vkBindImageMemory(a.device,image,memory,0),"local terrain BC bind failed");VkImageViewCreateInfo viewCreate{VK_STRUCTURE_TYPE_IMAGE_VIEW_CREATE_INFO};viewCreate.image=image;viewCreate.viewType=VK_IMAGE_VIEW_TYPE_2D_ARRAY;viewCreate.format=format;viewCreate.subresourceRange.aspectMask=VK_IMAGE_ASPECT_COLOR_BIT;viewCreate.subresourceRange.levelCount=1;viewCreate.subresourceRange.layerCount=LocalPayloadSlots;a.Check(vkCreateImageView(a.device,&viewCreate,nullptr,&view),"local terrain BC view failed");(void)bytes;
}
void CreateLocalTerrain(App&a){
  if(!a.localTerrainPath.empty()){a.localPack=std::make_unique<nc::localterrain::Pack>();std::string error;if(!a.localPack->Open(a.localTerrainPath,error)||!a.localPack->IsProductionLayout())throw std::runtime_error("Local terrain pack unavailable: "+(error.empty()?std::string("NCCUBE2 production layout mismatch"):error));}
  CreateLocalImage(a,VK_FORMAT_BC7_SRGB_BLOCK,LocalAlbedoLayerBytes,a.localImages[0],a.localImageMemory[0],a.localImageViews[0]);CreateLocalImage(a,VK_FORMAT_R16_UNORM,LocalElevationLayerBytes,a.localImages[1],a.localImageMemory[1],a.localImageViews[1]);CreateLocalImage(a,VK_FORMAT_BC5_UNORM_BLOCK,LocalNormalLayerBytes,a.localImages[2],a.localImageMemory[2],a.localImageViews[2]);CreateLocalImage(a,VK_FORMAT_R8_UNORM,LocalControlLayerBytes,a.localImages[3],a.localImageMemory[3],a.localImageViews[3]);VkPhysicalDeviceProperties properties{};vkGetPhysicalDeviceProperties(a.physical,&properties);VkSamplerCreateInfo sampler{VK_STRUCTURE_TYPE_SAMPLER_CREATE_INFO};sampler.magFilter=VK_FILTER_LINEAR;sampler.minFilter=VK_FILTER_LINEAR;sampler.mipmapMode=VK_SAMPLER_MIPMAP_MODE_NEAREST;sampler.addressModeU=VK_SAMPLER_ADDRESS_MODE_CLAMP_TO_EDGE;sampler.addressModeV=VK_SAMPLER_ADDRESS_MODE_CLAMP_TO_EDGE;sampler.addressModeW=VK_SAMPLER_ADDRESS_MODE_CLAMP_TO_EDGE;sampler.anisotropyEnable=VK_TRUE;sampler.maxAnisotropy=std::min(8.0f,properties.limits.maxSamplerAnisotropy);sampler.maxLod=0;a.Check(vkCreateSampler(a.device,&sampler,nullptr,&a.localSampler),"local terrain sampler failed");CreateHostBuffer(a,LocalStagingBytes,VK_BUFFER_USAGE_TRANSFER_SRC_BIT,a.localStagingBuffer,a.localStagingMemory,a.localStagingMapped,"local terrain staging failed");
  if(a.localPack){a.localIo=std::make_unique<LocalIoState>();a.localIo->pack=a.localPack.get();a.localIo->worker=std::thread(LocalIoWorker,a.localIo.get());char message[384];const uint64_t vram=uint64_t(LocalPayloadSlots)*(LocalAlbedoLayerBytes+LocalElevationLayerBytes+LocalNormalLayerBytes+LocalControlLayerBytes);std::snprintf(message,sizeof message,"Local terrain NCCUBE2 v3: records=%u; levels=%u-%u; BC7/R16/BC5/R8; slots=%u; VRAM=%llu; uploadBudget=%u; async=true; deterministicLRU=true",a.localPack->RecordCount(),a.localPack->MinimumLevel(),a.localPack->MaximumLevel(),LocalPayloadSlots,(unsigned long long)vram,LocalUploadBudget);a.Log(NC_LOG_ALWAYS,message);}
}
void DestroyLocalTerrain(App&a){
  if(a.localIo){{std::lock_guard lock(a.localIo->mutex);a.localIo->stop=true;}a.localIo->wake.notify_all();if(a.localIo->worker.joinable())a.localIo->worker.join();a.localIo.reset();}DestroyHostBuffer(a,a.localStagingBuffer,a.localStagingMemory,a.localStagingMapped);if(a.localSampler)vkDestroySampler(a.device,a.localSampler,nullptr);for(uint32_t channel=0;channel<4;channel++){if(a.localImageViews[channel])vkDestroyImageView(a.device,a.localImageViews[channel],nullptr);if(a.localImages[channel])vkDestroyImage(a.device,a.localImages[channel],nullptr);if(a.localImageMemory[channel])vkFreeMemory(a.device,a.localImageMemory[channel],nullptr);}a.localPack.reset();
}
std::array<double,3>LocalDirection(uint32_t face,uint32_t level,uint32_t x,uint32_t y){
  const double size=double(1u<<level),u=(double(x)+.5)/size,v=(double(y)+.5)/size,a=2*u-1,b=2*v-1;double cx{},cy{},cz{};switch(face){case 0:cx=1;cy=b;cz=-a;break;case 1:cx=-1;cy=b;cz=a;break;case 2:cx=a;cy=1;cz=-b;break;case 3:cx=a;cy=-1;cz=b;break;case 4:cx=a;cy=b;cz=1;break;default:cx=-a;cy=b;cz=-1;break;}const double x2=cx*cx,y2=cy*cy,z2=cz*cz;std::array<double,3>d{cx*std::sqrt(std::max(0.0,1-.5*(y2+z2)+y2*z2/3)),cy*std::sqrt(std::max(0.0,1-.5*(z2+x2)+z2*x2/3)),cz*std::sqrt(std::max(0.0,1-.5*(x2+y2)+x2*y2/3))};const double length=std::sqrt(d[0]*d[0]+d[1]*d[1]+d[2]*d[2]);for(double&value:d)value/=length;return d;
}
uint32_t LocalHash(const nc::localterrain::SectorId&id){uint32_t h=id.face*73856093u^id.level*19349663u^id.x*83492791u^id.y*2654435761u^id.detailFrequency*2246822519u^id.payloadVersion*3266489917u;h^=h>>16;return h&(LocalLookupCapacity-1u);}
void RebuildLocalLookup(App&a){
  if(!a.localLookupMapped)return;auto*words=static_cast<uint32_t*>(a.localLookupMapped);std::memset(words+16,0,sizeof(uint32_t)*LocalLookupEntryWords*LocalLookupCapacity);if(!a.localPack){std::memset(words,0,sizeof(uint32_t)*16);return;}words[0]=1;words[1]=a.localPack->MaximumLevel();words[2]=a.localPack->DetailFrequency();words[3]=a.localPack->PayloadVersionValue();const float residualMinimum=a.localPack->ResidualMinimum(),residualMaximum=a.localPack->ResidualMaximum();std::memcpy(words+4,&residualMinimum,sizeof(float));std::memcpy(words+5,&residualMaximum,sizeof(float));words[6]=LocalPayloadSlots;words[7]=LocalLookupCapacity;words[8]=a.localPack->MinimumLevel();words[9]=LocalLookupEntryWords;
  for(uint32_t layer=0;layer<LocalPayloadSlots;layer++)if(a.localLayerOccupied[layer]&&!a.localLayerInFlight[layer]&&a.localLayerPublished[layer]){const auto&id=a.localLayerSector[layer];uint32_t slot=LocalHash(id);for(uint32_t probe=0;probe<LocalLookupCapacity;probe++){uint32_t*entry=words+16+slot*LocalLookupEntryWords;if(entry[6]==0){entry[0]=id.face;entry[1]=id.level;entry[2]=id.x;entry[3]=id.y;entry[4]=id.detailFrequency;entry[5]=id.payloadVersion;entry[6]=layer+1;entry[7]=a.localLayerGeneration[layer];std::memcpy(entry+8,&a.localLayerResidualMinimum[layer],sizeof(float));std::memcpy(entry+9,&a.localLayerResidualMaximum[layer],sizeof(float));break;}slot=(slot+1)&(LocalLookupCapacity-1u);}}
}
bool TryPromoteLocalVisibleTransaction(App&a){
  if(!a.localVisibleTargetCount)return false;std::array<uint32_t,LocalPayloadSlots>layers{};for(uint32_t target=0;target<a.localVisibleTargetCount;target++){layers[target]=UINT32_MAX;for(uint32_t layer=0;layer<LocalPayloadSlots;layer++)if(a.localLayerOccupied[layer]&&!a.localLayerInFlight[layer]&&a.localLayerSector[layer]==a.localVisibleTarget[target]){layers[target]=layer;break;}if(layers[target]==UINT32_MAX)return false;}bool changed=false;for(uint32_t layer=0;layer<LocalPayloadSlots;layer++)if(a.localLayerPublished[layer]){bool retained=false;for(uint32_t target=0;target<a.localVisibleTargetCount;target++)retained|=layers[target]==layer;if(!retained){a.localLayerPublished[layer]=0;changed=true;}}for(uint32_t target=0;target<a.localVisibleTargetCount;target++)if(!a.localLayerPublished[layers[target]]){a.localLayerPublished[layers[target]]=1;changed=true;}if(changed){a.localPromotions++;RebuildLocalLookup(a);}return changed;
}
bool LocalPending(const App&a,const nc::localterrain::SectorId&id){
  for(uint32_t layer=0;layer<LocalPayloadSlots;layer++)if(a.localLayerOccupied[layer]&&a.localLayerSector[layer]==id)return true;for(uint32_t index=0;index<a.localPendingUploads;index++)if(a.localUploadRequests[index].id==id)return true;if(!a.localIo)return false;const auto&io=*a.localIo;for(uint32_t index=0,slot=io.requestHead;index<io.requestCount;index++,slot=(slot+1)%io.requests.size())if(io.requests[slot].id==id)return true;for(const auto&ready:io.ready)if(ready.state&&ready.request.id==id)return true;return false;
}
bool LocalSectorIntersectsAnchoredPatch(const nc::localterrain::SectorId&id,const NcAnchoredSurfacePatch&patch){
  if(id.face!=patch.face)return false;
  if(patch.level<=id.level){const uint32_t shift=id.level-patch.level;return (id.x>>shift)==patch.x&&(id.y>>shift)==patch.y;}
  const uint32_t shift=patch.level-id.level;return (patch.x>>shift)==id.x&&(patch.y>>shift)==id.y;
}
bool LocalSectorRequiredForAnchoredPatch(const App&a,const nc::localterrain::SectorId&id,const NcAnchoredSurfacePatch&patch){
  if((patch.flags&AnchoredSurfaceLocalRequired)==0u||!a.localPack)return false;
  const uint32_t selectedLevel=std::clamp(patch.level,a.localPack->MinimumLevel(),a.localPack->MaximumLevel());
  return id.level==selectedLevel&&LocalSectorIntersectsAnchoredPatch(id,patch);
}
bool LocalSectorPublished(const App&a,const nc::localterrain::SectorId&id){
  for(uint32_t layer=0;layer<LocalPayloadSlots;layer++)if(a.localLayerOccupied[layer]&&!a.localLayerInFlight[layer]&&a.localLayerPublished[layer]&&a.localLayerSector[layer]==id)return true;
  return false;
}
bool AnchoredPatchLocalPayloadsReady(const App&a,const NcAnchoredSurfacePatch&patch){
  if((patch.flags&AnchoredSurfaceLocalRequired)==0u)return true;
  if(!a.localPack||!a.localImagesInitialized)return false;bool intersects=false;
  for(const auto&record:a.localPack->Records())if(LocalSectorRequiredForAnchoredPatch(a,record.id,patch)){intersects=true;if(!LocalSectorPublished(a,record.id))return false;}
  return intersects;
}
bool QueueAnchoredLocalRequests(App&a){
  if(!a.localPack||!a.localIo||!a.submission||!a.submission->anchoredSurfacePatchCount||!a.submission->anchoredSurfacePatches)return false;
  if(a.localAnchoredGeneration==a.submission->anchoredSurfaceActiveGeneration){TryPromoteLocalVisibleTransaction(a);return false;}
  a.localAnchoredGeneration=a.submission->anchoredSurfaceActiveGeneration;a.localDemandEpoch++;
  for(auto&flag:a.localLayerVisible)flag=0;a.localVisibleTargetCount=0;
  for(const auto&record:a.localPack->Records()){
    bool visible=false;for(uint32_t index=0;index<a.submission->anchoredSurfacePatchCount;index++){const auto&patch=a.submission->anchoredSurfacePatches[index];if(LocalSectorRequiredForAnchoredPatch(a,record.id,patch)){visible=true;break;}}
    if(visible&&a.localVisibleTargetCount<a.localVisibleTarget.size())a.localVisibleTarget[a.localVisibleTargetCount++]=record.id;
  }
  auto&io=*a.localIo;std::lock_guard lock(io.mutex);
  for(uint32_t index=0;index<a.localVisibleTargetCount;index++){const auto&id=a.localVisibleTarget[index];bool resident=false;for(uint32_t layer=0;layer<LocalPayloadSlots;layer++)if(a.localLayerOccupied[layer]&&a.localLayerSector[layer]==id){a.localLayerLastUse[layer]=a.frame;a.localLayerVisible[layer]=1;resident=true;break;}if(resident){a.localHits++;continue;}a.localMisses++;if(LocalPending(a,id))continue;if(io.requestCount==io.requests.size()){io.queueDrops++;break;}io.requests[io.requestTail]={id,a.localDemandEpoch,true,std::chrono::steady_clock::now()};io.requestTail=(io.requestTail+1)%io.requests.size();io.requestCount++;a.localRequests++;}
  TryPromoteLocalVisibleTransaction(a);io.wake.notify_all();return true;
}
void QueueLocalRequests(App&a){
  QueueAnchoredLocalRequests(a);
}
void CompleteLocalUploads(App&a){if(!a.localPendingUploads)return;for(uint32_t index=0;index<a.localPendingUploads;index++){const uint32_t layer=a.localUploadLayers[index];if(a.localLayerOccupied[layer]&&a.localLayerGeneration[layer]==a.localUploadGenerations[index]&&a.localLayerSector[layer]==a.localUploadRequests[index].id){a.localLayerInFlight[layer]=0;a.localLayerLastUse[layer]=a.frame;}}a.localPendingUploads=0;RebuildLocalLookup(a);}
void PrepareLocalUploads(App&a){
  CompleteLocalUploads(a);QueueLocalRequests(a);if(!a.localIo||a.localPendingUploads)return;auto&io=*a.localIo;std::lock_guard lock(io.mutex);for(auto&ready:io.ready){if(a.localPendingUploads>=LocalUploadBudget)break;if(ready.state==3){ready={};continue;}if(ready.state!=2||!ready.payload)continue;if(ready.request.demandEpoch!=a.localDemandEpoch&&!ready.request.visible){a.localCanceled++;ready={};continue;}uint32_t layer=UINT32_MAX;for(uint32_t candidate=0;candidate<LocalPayloadSlots;candidate++)if(!a.localLayerOccupied[candidate]){layer=candidate;break;}if(layer==UINT32_MAX){uint64_t oldest=UINT64_MAX;for(uint32_t candidate=0;candidate<LocalPayloadSlots;candidate++)if(!a.localLayerVisible[candidate]&&!a.localLayerInFlight[candidate]&&a.localLayerLastUse[candidate]<oldest){oldest=a.localLayerLastUse[candidate];layer=candidate;}}if(layer==UINT32_MAX)break;if(a.localLayerOccupied[layer])a.localEvictions++;a.localLayerGeneration[layer]++;a.localLayerSector[layer]=ready.request.id;a.localLayerResidualMinimum[layer]=ready.payload->residualMinimum;a.localLayerResidualMaximum[layer]=ready.payload->residualMaximum;a.localLayerOccupied[layer]=1;a.localLayerVisible[layer]=ready.request.visible;a.localLayerInFlight[layer]=1;a.localLayerPublished[layer]=0;a.localLayerLastUse[layer]=a.frame;const uint32_t batch=a.localPendingUploads,base=batch*(LocalAlbedoLayerBytes+LocalElevationLayerBytes+LocalNormalLayerBytes+LocalControlLayerBytes);auto*destination=static_cast<uint8_t*>(a.localStagingMapped)+base;std::memcpy(destination,ready.payload->albedoBc7.data(),LocalAlbedoLayerBytes);std::memcpy(destination+LocalAlbedoLayerBytes,ready.payload->elevationBc4.data(),LocalElevationLayerBytes);std::memcpy(destination+LocalAlbedoLayerBytes+LocalElevationLayerBytes,ready.payload->normalBc5.data(),LocalNormalLayerBytes);std::memcpy(destination+LocalAlbedoLayerBytes+LocalElevationLayerBytes+LocalNormalLayerBytes,ready.payload->controlR8.data(),LocalControlLayerBytes);a.localUploadLayers[batch]=layer;a.localUploadGenerations[batch]=a.localLayerGeneration[layer];a.localUploadRequests[batch]=ready.request;a.localPendingUploads++;a.localUploads++;a.localUploadBytes+=LocalAlbedoLayerBytes+LocalElevationLayerBytes+LocalNormalLayerBytes+LocalControlLayerBytes;a.localTranscodeMilliseconds+=ready.payload->transcodeMilliseconds;a.localUploadLatencyMilliseconds+=std::chrono::duration<double,std::milli>(std::chrono::steady_clock::now()-ready.request.requestedAt).count();ready={};RebuildLocalLookup(a);}io.wake.notify_all();
}
void RecordLocalUploads(App&a,VkCommandBuffer command){
  if(!a.localPendingUploads)return;VkImageMemoryBarrier before[4]{};for(uint32_t channel=0;channel<4;channel++){before[channel].sType=VK_STRUCTURE_TYPE_IMAGE_MEMORY_BARRIER;before[channel].srcAccessMask=a.localImagesInitialized?VK_ACCESS_SHADER_READ_BIT:0;before[channel].dstAccessMask=VK_ACCESS_TRANSFER_WRITE_BIT;before[channel].oldLayout=a.localImagesInitialized?VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL:VK_IMAGE_LAYOUT_UNDEFINED;before[channel].newLayout=VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL;before[channel].image=a.localImages[channel];before[channel].subresourceRange.aspectMask=VK_IMAGE_ASPECT_COLOR_BIT;before[channel].subresourceRange.levelCount=1;before[channel].subresourceRange.layerCount=LocalPayloadSlots;}vkCmdPipelineBarrier(command,a.localImagesInitialized?(VK_PIPELINE_STAGE_FRAGMENT_SHADER_BIT|VK_PIPELINE_STAGE_VERTEX_SHADER_BIT):VK_PIPELINE_STAGE_TOP_OF_PIPE_BIT,VK_PIPELINE_STAGE_TRANSFER_BIT,0,0,nullptr,0,nullptr,4,before);for(uint32_t index=0;index<a.localPendingUploads;index++){const VkDeviceSize base=index*(LocalAlbedoLayerBytes+LocalElevationLayerBytes+LocalNormalLayerBytes+LocalControlLayerBytes);const VkDeviceSize offsets[4]{base,base+LocalAlbedoLayerBytes,base+LocalAlbedoLayerBytes+LocalElevationLayerBytes,base+LocalAlbedoLayerBytes+LocalElevationLayerBytes+LocalNormalLayerBytes};for(uint32_t channel=0;channel<4;channel++){VkBufferImageCopy copy{};copy.bufferOffset=offsets[channel];copy.imageSubresource.aspectMask=VK_IMAGE_ASPECT_COLOR_BIT;copy.imageSubresource.baseArrayLayer=a.localUploadLayers[index];copy.imageSubresource.layerCount=1;copy.imageExtent={nc::localterrain::StoredExtent,nc::localterrain::StoredExtent,1};vkCmdCopyBufferToImage(command,a.localStagingBuffer,a.localImages[channel],VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,1,&copy);}}VkImageMemoryBarrier after[4]{before[0],before[1],before[2],before[3]};for(auto&barrier:after){barrier.srcAccessMask=VK_ACCESS_TRANSFER_WRITE_BIT;barrier.dstAccessMask=VK_ACCESS_SHADER_READ_BIT;barrier.oldLayout=VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL;barrier.newLayout=VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL;}vkCmdPipelineBarrier(command,VK_PIPELINE_STAGE_TRANSFER_BIT,VK_PIPELINE_STAGE_FRAGMENT_SHADER_BIT|VK_PIPELINE_STAGE_VERTEX_SHADER_BIT,0,0,nullptr,0,nullptr,4,after);a.localImagesInitialized=true;
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
std::string ModuleDirectory(){HMODULE m{};GetModuleHandleExA(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS|GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,reinterpret_cast<LPCSTR>(&ModuleDirectory),&m);char p[MAX_PATH]{};GetModuleFileNameA(m,p,MAX_PATH);std::string path(p);path.resize(path.find_last_of("\\/")+1);return path;}
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
  VkImageCreateInfo depth=image;depth.format=App::DepthFormat;depth.usage=VK_IMAGE_USAGE_DEPTH_STENCIL_ATTACHMENT_BIT;a.Check(vkCreateImage(a.device,&depth,nullptr,&a.sceneDepth),"scene-depth image failed");vkGetImageMemoryRequirements(a.device,a.sceneDepth,&requirements);allocation.allocationSize=requirements.size;allocation.memoryTypeIndex=Memory(a,requirements.memoryTypeBits,VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT);a.Check(vkAllocateMemory(a.device,&allocation,nullptr,&a.sceneDepthMemory),"scene-depth memory failed");a.Check(vkBindImageMemory(a.device,a.sceneDepth,a.sceneDepthMemory,0),"scene-depth bind failed");view.image=a.sceneDepth;view.format=App::DepthFormat;view.subresourceRange.aspectMask=VK_IMAGE_ASPECT_DEPTH_BIT|VK_IMAGE_ASPECT_STENCIL_BIT;a.Check(vkCreateImageView(a.device,&view,nullptr,&a.sceneDepthView),"scene-depth view failed");
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
  attachments[2].format=App::DepthFormat;attachments[2].samples=VK_SAMPLE_COUNT_1_BIT;attachments[2].loadOp=VK_ATTACHMENT_LOAD_OP_CLEAR;attachments[2].storeOp=VK_ATTACHMENT_STORE_OP_DONT_CARE;attachments[2].stencilLoadOp=VK_ATTACHMENT_LOAD_OP_CLEAR;attachments[2].stencilStoreOp=VK_ATTACHMENT_STORE_OP_DONT_CARE;attachments[2].initialLayout=VK_IMAGE_LAYOUT_UNDEFINED;attachments[2].finalLayout=VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL;
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
  VkDescriptorSetLayoutBinding binds[39]{};
  const VkShaderStageFlags terrainStages=VK_SHADER_STAGE_VERTEX_BIT|VK_SHADER_STAGE_TESSELLATION_CONTROL_BIT|VK_SHADER_STAGE_TESSELLATION_EVALUATION_BIT|VK_SHADER_STAGE_FRAGMENT_BIT;
  for(uint32_t binding=0;binding<7;binding++){binds[binding].binding=binding;binds[binding].descriptorType=VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;binds[binding].descriptorCount=1;binds[binding].stageFlags=binding==0?terrainStages:(binding==1?VK_SHADER_STAGE_VERTEX_BIT|VK_SHADER_STAGE_COMPUTE_BIT:(binding==2?terrainStages|VK_SHADER_STAGE_COMPUTE_BIT:(binding==6?terrainStages|VK_SHADER_STAGE_COMPUTE_BIT:VK_SHADER_STAGE_COMPUTE_BIT)));}
  binds[7].binding=7;binds[7].descriptorType=VK_DESCRIPTOR_TYPE_INPUT_ATTACHMENT;binds[7].descriptorCount=1;binds[7].stageFlags=VK_SHADER_STAGE_FRAGMENT_BIT;
  for(uint32_t binding=8;binding<11;binding++){binds[binding].binding=binding;binds[binding].descriptorType=VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;binds[binding].descriptorCount=1;binds[binding].stageFlags=binding==8?VK_SHADER_STAGE_COMPUTE_BIT:VK_SHADER_STAGE_COMPUTE_BIT|VK_SHADER_STAGE_VERTEX_BIT;}
  for(uint32_t index=0;index<3;index++){auto &binding=binds[11+index];binding.binding=24+index;binding.descriptorType=VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER;binding.descriptorCount=1;binding.stageFlags=terrainStages;}
  binds[14].binding=27;binds[14].descriptorType=VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;binds[14].descriptorCount=1;binds[14].stageFlags=terrainStages;
  for(uint32_t index=0;index<3;index++){auto &binding=binds[15+index];binding.binding=28+index;binding.descriptorType=VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER;binding.descriptorCount=1;binding.stageFlags=terrainStages;}
  binds[18].binding=31;binds[18].descriptorType=VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;binds[18].descriptorCount=1;binds[18].stageFlags=terrainStages;
  binds[19].binding=32;binds[19].descriptorType=VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;binds[19].descriptorCount=1;binds[19].stageFlags=terrainStages|VK_SHADER_STAGE_COMPUTE_BIT;
  binds[20].binding=33;binds[20].descriptorType=VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;binds[20].descriptorCount=1;binds[20].stageFlags=VK_SHADER_STAGE_VERTEX_BIT|VK_SHADER_STAGE_TESSELLATION_EVALUATION_BIT;
  binds[21].binding=34;binds[21].descriptorType=VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER;binds[21].descriptorCount=1;binds[21].stageFlags=terrainStages;
  binds[22].binding=35;binds[22].descriptorType=VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;binds[22].descriptorCount=1;binds[22].stageFlags=VK_SHADER_STAGE_VERTEX_BIT|VK_SHADER_STAGE_TESSELLATION_EVALUATION_BIT|VK_SHADER_STAGE_COMPUTE_BIT;
  binds[23].binding=36;binds[23].descriptorType=VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;binds[23].descriptorCount=1;binds[23].stageFlags=VK_SHADER_STAGE_VERTEX_BIT|VK_SHADER_STAGE_COMPUTE_BIT;
  binds[24].binding=37;binds[24].descriptorType=VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;binds[24].descriptorCount=1;binds[24].stageFlags=VK_SHADER_STAGE_COMPUTE_BIT;
  for(uint32_t index=0;index<7;index++){auto &binding=binds[25+index];binding.binding=38+index;binding.descriptorType=VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;binding.descriptorCount=1;binding.stageFlags=VK_SHADER_STAGE_COMPUTE_BIT|VK_SHADER_STAGE_VERTEX_BIT;}
  for(uint32_t index=0;index<7;index++){auto &binding=binds[32+index];binding.binding=45+index;binding.descriptorType=VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;binding.descriptorCount=1;binding.stageFlags=VK_SHADER_STAGE_COMPUTE_BIT;}
  VkDescriptorSetLayoutCreateInfo dl{
      VK_STRUCTURE_TYPE_DESCRIPTOR_SET_LAYOUT_CREATE_INFO};
  dl.bindingCount = 39;
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
  VkVertexInputAttributeDescription va[3]{
      {0, 0, VK_FORMAT_R32G32B32_SFLOAT, offsetof(Vertex, position)},
      {1, 0, VK_FORMAT_R32G32B32_SFLOAT, offsetof(Vertex, color)},
      {2, 0, VK_FORMAT_R32G32B32_SFLOAT, offsetof(Vertex, normal)}};
  VkPipelineVertexInputStateCreateInfo vi{
      VK_STRUCTURE_TYPE_PIPELINE_VERTEX_INPUT_STATE_CREATE_INFO};
  vi.vertexBindingDescriptionCount = 1;
  vi.pVertexBindingDescriptions = &vb;
  vi.vertexAttributeDescriptionCount = 3;
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
  // The production grid is authored with literal outward model-space winding.
  // CameraRenderSnapshotBuilder's Vulkan projection and the positive-height
  // viewport preserve that authored front-face identity as COUNTER_CLOCKWISE.
  // Treating it as clockwise only appeared to work while uncullable L0 roots
  // supplied the opposite hemisphere; once selection horizon-culled that
  // hemisphere at L1/L2, the complete visible surface was raster-culled.
  VkPipelineRasterizationStateCreateInfo planetaryRaster=rs;planetaryRaster.cullMode=VK_CULL_MODE_BACK_BIT;planetaryRaster.frontFace=VK_FRONT_FACE_COUNTER_CLOCKWISE;
  VkPipelineColorBlendAttachmentState planetaryBlendAttachment=ca;planetaryBlendAttachment.blendEnable=VK_TRUE;planetaryBlendAttachment.srcColorBlendFactor=VK_BLEND_FACTOR_SRC_ALPHA;planetaryBlendAttachment.dstColorBlendFactor=VK_BLEND_FACTOR_ONE_MINUS_SRC_ALPHA;planetaryBlendAttachment.colorBlendOp=VK_BLEND_OP_ADD;planetaryBlendAttachment.srcAlphaBlendFactor=VK_BLEND_FACTOR_ONE;planetaryBlendAttachment.dstAlphaBlendFactor=VK_BLEND_FACTOR_ONE_MINUS_SRC_ALPHA;planetaryBlendAttachment.alphaBlendOp=VK_BLEND_OP_ADD;VkPipelineColorBlendStateCreateInfo planetaryBlend=cb;planetaryBlend.pAttachments=&planetaryBlendAttachment;
  VkGraphicsPipelineCreateInfo planetaryCreate=gp;planetaryCreate.pStages=planetaryStages;planetaryCreate.pVertexInputState=&planetaryInput;planetaryCreate.pRasterizationState=&planetaryRaster;planetaryCreate.pColorBlendState=&planetaryBlend;
  VkPipeline planetaryPipeline{};VkResult planetaryResult=vkCreateGraphicsPipelines(a.device,{},1,&planetaryCreate,nullptr,&planetaryPipeline);vkDestroyShaderModule(a.device,planetaryVs,nullptr);vkDestroyShaderModule(a.device,planetaryFs,nullptr);if(planetaryResult!=VK_SUCCESS&&planetaryPipeline)vkDestroyPipeline(a.device,planetaryPipeline,nullptr);a.Check(planetaryResult,"planetary pipeline failed");a.planetaryPipeline=planetaryPipeline;
  {VkShaderModule productionVs=Shader(a,"shaders/planetary.vert.spv"),productionFs{};try{productionFs=Shader(a,"shaders/planetary_production.frag.spv");}catch(...){vkDestroyShaderModule(a.device,productionVs,nullptr);throw;}VkPipelineShaderStageCreateInfo productionStages[2]{{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_VERTEX_BIT,productionVs,"main"},{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_FRAGMENT_BIT,productionFs,"main"}};VkGraphicsPipelineCreateInfo productionCreate=planetaryCreate;productionCreate.pStages=productionStages;productionCreate.pColorBlendState=&cb;VkResult result=vkCreateGraphicsPipelines(a.device,{},1,&productionCreate,nullptr,&a.productionPlanetaryPipeline);if(result==VK_SUCCESS){VkPipelineDepthStencilStateCreateInfo fillDepth=depth;fillDepth.stencilTestEnable=VK_TRUE;fillDepth.front.compareOp=VK_COMPARE_OP_EQUAL;fillDepth.front.failOp=VK_STENCIL_OP_KEEP;fillDepth.front.passOp=VK_STENCIL_OP_KEEP;fillDepth.front.depthFailOp=VK_STENCIL_OP_KEEP;fillDepth.front.compareMask=0xffu;fillDepth.front.writeMask=0u;fillDepth.front.reference=0u;fillDepth.back=fillDepth.front;productionCreate.pDepthStencilState=&fillDepth;result=vkCreateGraphicsPipelines(a.device,{},1,&productionCreate,nullptr,&a.productionPlanetaryFillPipeline);}vkDestroyShaderModule(a.device,productionVs,nullptr);vkDestroyShaderModule(a.device,productionFs,nullptr);a.Check(result,"production cube-sphere pipeline failed");}
  {
    VkShaderModule anchoredVs=Shader(a,"shaders/anchored_terrain.vert.spv"),anchoredTcs{},anchoredTes{},anchoredFs{};
    try{anchoredTcs=Shader(a,"shaders/anchored_terrain.tesc.spv");anchoredTes=Shader(a,"shaders/anchored_terrain.tese.spv");anchoredFs=Shader(a,"shaders/planetary_production.frag.spv");}
    catch(...){vkDestroyShaderModule(a.device,anchoredVs,nullptr);if(anchoredTcs)vkDestroyShaderModule(a.device,anchoredTcs,nullptr);if(anchoredTes)vkDestroyShaderModule(a.device,anchoredTes,nullptr);throw;}
    VkPipelineShaderStageCreateInfo stages[4]{
      {VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_VERTEX_BIT,anchoredVs,"main"},
      {VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_TESSELLATION_CONTROL_BIT,anchoredTcs,"main"},
      {VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_TESSELLATION_EVALUATION_BIT,anchoredTes,"main"},
      {VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_FRAGMENT_BIT,anchoredFs,"main"}};
    VkVertexInputBindingDescription binding{0,sizeof(PatchVertex),VK_VERTEX_INPUT_RATE_VERTEX};
    VkVertexInputAttributeDescription attribute{0,0,VK_FORMAT_R32G32_SFLOAT,0};
    VkPipelineVertexInputStateCreateInfo input{VK_STRUCTURE_TYPE_PIPELINE_VERTEX_INPUT_STATE_CREATE_INFO};input.vertexBindingDescriptionCount=1;input.pVertexBindingDescriptions=&binding;input.vertexAttributeDescriptionCount=1;input.pVertexAttributeDescriptions=&attribute;
    VkPipelineInputAssemblyStateCreateInfo patchAssembly=ia;patchAssembly.topology=VK_PRIMITIVE_TOPOLOGY_PATCH_LIST;
    VkPipelineTessellationStateCreateInfo tessellation{VK_STRUCTURE_TYPE_PIPELINE_TESSELLATION_STATE_CREATE_INFO};tessellation.patchControlPoints=3;
    VkPipelineDepthStencilStateCreateInfo anchoredDepth=depth;anchoredDepth.depthCompareOp=VK_COMPARE_OP_GREATER_OR_EQUAL;anchoredDepth.stencilTestEnable=VK_TRUE;anchoredDepth.front.compareOp=VK_COMPARE_OP_ALWAYS;anchoredDepth.front.failOp=VK_STENCIL_OP_KEEP;anchoredDepth.front.passOp=VK_STENCIL_OP_REPLACE;anchoredDepth.front.depthFailOp=VK_STENCIL_OP_KEEP;anchoredDepth.front.compareMask=0xffu;anchoredDepth.front.writeMask=0xffu;anchoredDepth.front.reference=1u;anchoredDepth.back=anchoredDepth.front;
    VkGraphicsPipelineCreateInfo create=planetaryCreate;create.stageCount=4;create.pStages=stages;create.pVertexInputState=&input;create.pInputAssemblyState=&patchAssembly;create.pTessellationState=&tessellation;create.pDepthStencilState=&anchoredDepth;create.pColorBlendState=&cb;
    VkResult result=vkCreateGraphicsPipelines(a.device,{},1,&create,nullptr,&a.anchoredTerrainPipeline);
    vkDestroyShaderModule(a.device,anchoredVs,nullptr);vkDestroyShaderModule(a.device,anchoredTcs,nullptr);vkDestroyShaderModule(a.device,anchoredTes,nullptr);vkDestroyShaderModule(a.device,anchoredFs,nullptr);a.Check(result,"GPU-refined anchored surface pipeline failed");
  }
  {
    VkShaderModule candidateVs=Shader(a,"shaders/production_spherical_billboard.vert.spv"),candidateTcs{},candidateTes{},candidateFs{};
    try{candidateTcs=Shader(a,"shaders/production_spherical_billboard.tesc.spv");candidateTes=Shader(a,"shaders/production_spherical_billboard.tese.spv");candidateFs=Shader(a,"shaders/planetary_production.frag.spv");}
    catch(...){vkDestroyShaderModule(a.device,candidateVs,nullptr);if(candidateTcs)vkDestroyShaderModule(a.device,candidateTcs,nullptr);if(candidateTes)vkDestroyShaderModule(a.device,candidateTes,nullptr);throw;}
    VkPipelineShaderStageCreateInfo candidateStages[4]{{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_VERTEX_BIT,candidateVs,"main"},{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_TESSELLATION_CONTROL_BIT,candidateTcs,"main"},{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_TESSELLATION_EVALUATION_BIT,candidateTes,"main"},{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_FRAGMENT_BIT,candidateFs,"main"}};
    VkPipelineVertexInputStateCreateInfo candidateInput{VK_STRUCTURE_TYPE_PIPELINE_VERTEX_INPUT_STATE_CREATE_INFO};VkPipelineInputAssemblyStateCreateInfo candidateAssembly=ia;candidateAssembly.topology=VK_PRIMITIVE_TOPOLOGY_PATCH_LIST;VkPipelineTessellationStateCreateInfo candidateTessellation{VK_STRUCTURE_TYPE_PIPELINE_TESSELLATION_STATE_CREATE_INFO};candidateTessellation.patchControlPoints=3;VkGraphicsPipelineCreateInfo candidateCreate=planetaryCreate;candidateCreate.stageCount=4;candidateCreate.pStages=candidateStages;candidateCreate.pVertexInputState=&candidateInput;candidateCreate.pInputAssemblyState=&candidateAssembly;candidateCreate.pTessellationState=&candidateTessellation;candidateCreate.pDepthStencilState=&depth;candidateCreate.pColorBlendState=&cb;
    VkResult result=vkCreateGraphicsPipelines(a.device,{},1,&candidateCreate,nullptr,&a.productionBillboardPipeline);vkDestroyShaderModule(a.device,candidateVs,nullptr);vkDestroyShaderModule(a.device,candidateTcs,nullptr);vkDestroyShaderModule(a.device,candidateTes,nullptr);vkDestroyShaderModule(a.device,candidateFs,nullptr);a.Check(result,"production spherical billboard graphics pipeline failed");
  }
  VkShaderModule distantVs{},distantFs{};try{distantVs=Shader(a,"shaders/distant_planet.vert.spv");distantFs=Shader(a,"shaders/distant_planet.frag.spv");}catch(...){if(distantVs)vkDestroyShaderModule(a.device,distantVs,nullptr);throw;}VkPipelineShaderStageCreateInfo distantStages[2]{{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_VERTEX_BIT,distantVs,"main"},{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_FRAGMENT_BIT,distantFs,"main"}};VkVertexInputBindingDescription distantBinding{0,sizeof(DistantVertex),VK_VERTEX_INPUT_RATE_VERTEX};VkVertexInputAttributeDescription distantAttribute{0,0,VK_FORMAT_R32G32B32_SFLOAT,0};VkPipelineVertexInputStateCreateInfo distantInput{VK_STRUCTURE_TYPE_PIPELINE_VERTEX_INPUT_STATE_CREATE_INFO};distantInput.vertexBindingDescriptionCount=1;distantInput.pVertexBindingDescriptions=&distantBinding;distantInput.vertexAttributeDescriptionCount=1;distantInput.pVertexAttributeDescriptions=&distantAttribute;VkPipelineRasterizationStateCreateInfo distantRaster=rs;distantRaster.cullMode=VK_CULL_MODE_BACK_BIT;distantRaster.frontFace=VK_FRONT_FACE_COUNTER_CLOCKWISE;VkGraphicsPipelineCreateInfo distantCreate=gp;distantCreate.pStages=distantStages;distantCreate.pVertexInputState=&distantInput;distantCreate.pRasterizationState=&distantRaster;distantCreate.pColorBlendState=&planetaryBlend;VkResult distantResult=vkCreateGraphicsPipelines(a.device,{},1,&distantCreate,nullptr,&a.distantPlanetaryPipeline);VkPipelineDepthStencilStateCreateInfo handoffDepth=depth;handoffDepth.depthWriteEnable=VK_FALSE;distantCreate.pDepthStencilState=&handoffDepth;VkResult handoffResult=distantResult==VK_SUCCESS?vkCreateGraphicsPipelines(a.device,{},1,&distantCreate,nullptr,&a.distantPlanetaryHandoffPipeline):distantResult;vkDestroyShaderModule(a.device,distantVs,nullptr);vkDestroyShaderModule(a.device,distantFs,nullptr);a.Check(distantResult,"distant planetary pipeline failed");a.Check(handoffResult,"distant planetary handoff pipeline failed");
  VkShaderModule ringVs=Shader(a,"shaders/planetary_ring.vert.spv"),ringFarFs{},ringNearFs{};try{ringFarFs=Shader(a,"shaders/planetary_ring_far.frag.spv");ringNearFs=Shader(a,"shaders/planetary_ring_near.frag.spv");}catch(...){vkDestroyShaderModule(a.device,ringVs,nullptr);if(ringFarFs)vkDestroyShaderModule(a.device,ringFarFs,nullptr);throw;}VkPipelineShaderStageCreateInfo ringStages[2]{{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_VERTEX_BIT,ringVs,"main"},{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_FRAGMENT_BIT,ringFarFs,"main"}};VkPipelineRasterizationStateCreateInfo ringRaster=rs;ringRaster.cullMode=VK_CULL_MODE_NONE;VkGraphicsPipelineCreateInfo ringCreate=gp;ringCreate.pStages=ringStages;ringCreate.pVertexInputState=&distantInput;ringCreate.pRasterizationState=&ringRaster;ringCreate.pColorBlendState=&planetaryBlend;a.Check(vkCreateGraphicsPipelines(a.device,{},1,&ringCreate,nullptr,&a.planetaryRingFarPipeline),"far planetary ring pipeline failed");ringStages[1].module=ringNearFs;a.Check(vkCreateGraphicsPipelines(a.device,{},1,&ringCreate,nullptr,&a.planetaryRingNearPipeline),"near planetary ring pipeline failed");vkDestroyShaderModule(a.device,ringVs,nullptr);vkDestroyShaderModule(a.device,ringFarFs,nullptr);vkDestroyShaderModule(a.device,ringNearFs,nullptr);
  VkShaderModule stellarVs=Shader(a,"shaders/stellar_sun.vert.spv"),stellarFs{};try{stellarFs=Shader(a,"shaders/stellar_sun.frag.spv");}catch(...){vkDestroyShaderModule(a.device,stellarVs,nullptr);throw;}VkPipelineShaderStageCreateInfo stellarStages[2]{{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_VERTEX_BIT,stellarVs,"main"},{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_FRAGMENT_BIT,stellarFs,"main"}};VkGraphicsPipelineCreateInfo stellarCreate=distantCreate;stellarCreate.pStages=stellarStages;stellarCreate.pColorBlendState=&cb;VkResult stellarResult=vkCreateGraphicsPipelines(a.device,{},1,&stellarCreate,nullptr,&a.stellarSunPipeline);vkDestroyShaderModule(a.device,stellarVs,nullptr);vkDestroyShaderModule(a.device,stellarFs,nullptr);a.Check(stellarResult,"stellar Sun pipeline failed");
  VkShaderModule glowVs=Shader(a,"shaders/stellar_glow.vert.spv"),glowFs{};try{glowFs=Shader(a,"shaders/stellar_glow.frag.spv");}catch(...){vkDestroyShaderModule(a.device,glowVs,nullptr);throw;}VkPipelineShaderStageCreateInfo glowStages[2]{{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_VERTEX_BIT,glowVs,"main"},{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_FRAGMENT_BIT,glowFs,"main"}};VkGraphicsPipelineCreateInfo glowCreate=gp;glowCreate.pStages=glowStages;glowCreate.pVertexInputState=&fullscreenInput;glowCreate.pColorBlendState=&planetaryBlend;glowCreate.pDepthStencilState=&noDepth;VkResult glowResult=vkCreateGraphicsPipelines(a.device,{},1,&glowCreate,nullptr,&a.stellarGlowPipeline);vkDestroyShaderModule(a.device,glowVs,nullptr);vkDestroyShaderModule(a.device,glowFs,nullptr);a.Check(glowResult,"stellar glow pipeline failed");
  VkShaderModule planetaryCompute=Shader(a,"shaders/planetary_select.comp.spv");VkPipelineShaderStageCreateInfo planetaryComputeStage{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_COMPUTE_BIT,planetaryCompute,"main"};VkComputePipelineCreateInfo planetaryComputeCreate{VK_STRUCTURE_TYPE_COMPUTE_PIPELINE_CREATE_INFO};planetaryComputeCreate.stage=planetaryComputeStage;planetaryComputeCreate.layout=a.pipelineLayout;VkResult planetaryComputeResult=vkCreateComputePipelines(a.device,{},1,&planetaryComputeCreate,nullptr,&a.planetaryComputePipeline);vkDestroyShaderModule(a.device,planetaryCompute,nullptr);a.Check(planetaryComputeResult,"planetary compute pipeline failed");
  VkShaderModule terrainCompute=Shader(a,"shaders/planetary_terrain_generate.comp.spv");VkPipelineShaderStageCreateInfo terrainComputeStage{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_COMPUTE_BIT,terrainCompute,"main"};VkComputePipelineCreateInfo terrainComputeCreate{VK_STRUCTURE_TYPE_COMPUTE_PIPELINE_CREATE_INFO};terrainComputeCreate.stage=terrainComputeStage;terrainComputeCreate.layout=a.pipelineLayout;VkResult terrainComputeResult=vkCreateComputePipelines(a.device,{},1,&terrainComputeCreate,nullptr,&a.planetaryTerrainPipeline);vkDestroyShaderModule(a.device,terrainCompute,nullptr);a.Check(terrainComputeResult,"planetary terrain compute pipeline failed");
  VkShaderModule productionTerrain=Shader(a,"shaders/planetary_production_terrain.comp.spv");VkPipelineShaderStageCreateInfo productionTerrainStage{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_COMPUTE_BIT,productionTerrain,"main"};VkComputePipelineCreateInfo productionTerrainCreate{VK_STRUCTURE_TYPE_COMPUTE_PIPELINE_CREATE_INFO};productionTerrainCreate.stage=productionTerrainStage;productionTerrainCreate.layout=a.pipelineLayout;VkResult productionTerrainResult=vkCreateComputePipelines(a.device,{},1,&productionTerrainCreate,nullptr,&a.productionPlanetaryTerrainPipeline);vkDestroyShaderModule(a.device,productionTerrain,nullptr);a.Check(productionTerrainResult,"production cube-sphere terrain pipeline failed");
  {VkShaderModule globalPrepare=Shader(a,"shaders/planetary_natural_terrain_global_prepare.comp.spv");VkPipelineShaderStageCreateInfo stage{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_COMPUTE_BIT,globalPrepare,"main"};VkComputePipelineCreateInfo create{VK_STRUCTURE_TYPE_COMPUTE_PIPELINE_CREATE_INFO};create.stage=stage;create.layout=a.pipelineLayout;VkResult result=vkCreateComputePipelines(a.device,{},1,&create,nullptr,&a.naturalGlobalPreparePipeline);vkDestroyShaderModule(a.device,globalPrepare,nullptr);a.Check(result,"natural terrain global preparation pipeline failed");}
  {VkShaderModule anchoredPrepare=Shader(a,"shaders/planetary_natural_terrain_anchored_prepare.comp.spv");VkPipelineShaderStageCreateInfo stage{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_COMPUTE_BIT,anchoredPrepare,"main"};VkComputePipelineCreateInfo create{VK_STRUCTURE_TYPE_COMPUTE_PIPELINE_CREATE_INFO};create.stage=stage;create.layout=a.pipelineLayout;VkResult result=vkCreateComputePipelines(a.device,{},1,&create,nullptr,&a.naturalAnchoredPreparePipeline);vkDestroyShaderModule(a.device,anchoredPrepare,nullptr);a.Check(result,"natural terrain anchored preparation pipeline failed");}
  auto createCandidateCompute=[&](const char *path,VkPipeline &pipeline,const char *failure){VkShaderModule module=Shader(a,path);VkPipelineShaderStageCreateInfo stage{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO,nullptr,0,VK_SHADER_STAGE_COMPUTE_BIT,module,"main"};VkComputePipelineCreateInfo create{VK_STRUCTURE_TYPE_COMPUTE_PIPELINE_CREATE_INFO};create.stage=stage;create.layout=a.pipelineLayout;VkResult result=vkCreateComputePipelines(a.device,{},1,&create,nullptr,&pipeline);vkDestroyShaderModule(a.device,module,nullptr);a.Check(result,failure);};
  createCandidateCompute("shaders/production_spherical_billboard_reset.comp.spv",a.productionBillboardResetPipeline,"production billboard reset pipeline failed");createCandidateCompute("shaders/production_spherical_billboard_cull.comp.spv",a.productionBillboardCullPipeline,"production billboard cull pipeline failed");createCandidateCompute("shaders/production_spherical_billboard_compact.comp.spv",a.productionBillboardCompactPipeline,"production billboard compact pipeline failed");
  createCandidateCompute("shaders/production_spherical_billboard_incoming_reset.comp.spv",a.productionBillboardIncomingResetPipeline,"incoming production billboard reset pipeline failed");createCandidateCompute("shaders/production_spherical_billboard_incoming_cull.comp.spv",a.productionBillboardIncomingCullPipeline,"incoming production billboard cull pipeline failed");createCandidateCompute("shaders/production_spherical_billboard_incoming_compact.comp.spv",a.productionBillboardIncomingCompactPipeline,"incoming production billboard compact pipeline failed");
  VkShaderModule orbitVs{}, orbitFs{};
  try { orbitVs = Shader(a, "shaders/orbit.vert.spv"); orbitFs = Shader(a, "shaders/orbit.frag.spv"); }
  catch (...) { if (orbitVs) vkDestroyShaderModule(a.device, orbitVs, nullptr); throw; }
  VkPipelineShaderStageCreateInfo orbitStages[2]{{VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO, nullptr, 0, VK_SHADER_STAGE_VERTEX_BIT, orbitVs, "main"}, {VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO, nullptr, 0, VK_SHADER_STAGE_FRAGMENT_BIT, orbitFs, "main"}};
  VkVertexInputBindingDescription orbitBinding{0, sizeof(NcOrbitLineVertex), VK_VERTEX_INPUT_RATE_VERTEX};
  VkVertexInputAttributeDescription orbitAttributes[2]{{0,0,VK_FORMAT_R32G32B32_SFLOAT,offsetof(NcOrbitLineVertex,positionHigh)},
    {1,0,VK_FORMAT_R32G32B32_SFLOAT,offsetof(NcOrbitLineVertex,positionLow)}};
  VkPipelineVertexInputStateCreateInfo orbitInput{VK_STRUCTURE_TYPE_PIPELINE_VERTEX_INPUT_STATE_CREATE_INFO}; orbitInput.vertexBindingDescriptionCount = 1; orbitInput.pVertexBindingDescriptions = &orbitBinding; orbitInput.vertexAttributeDescriptionCount = 2; orbitInput.pVertexAttributeDescriptions = orbitAttributes;
  VkPipelineInputAssemblyStateCreateInfo orbitAssembly{VK_STRUCTURE_TYPE_PIPELINE_INPUT_ASSEMBLY_STATE_CREATE_INFO}; orbitAssembly.topology = VK_PRIMITIVE_TOPOLOGY_LINE_STRIP;
  // Scene-space orbit/debug lines must participate in the same reversed-Z
  // occlusion test as terrain. They are overlays only in color ownership, not
  // x-ray geometry: disabling depth here drew far-side segments across Earth
  // and made them indistinguishable from permanent surface cracks.
  VkPipelineDepthStencilStateCreateInfo orbitDepth=depth;orbitDepth.depthWriteEnable=VK_FALSE;orbitDepth.depthCompareOp=VK_COMPARE_OP_GREATER_OR_EQUAL;
  VkGraphicsPipelineCreateInfo orbitPipeline = gp; orbitPipeline.pStages = orbitStages; orbitPipeline.pVertexInputState = &orbitInput; orbitPipeline.pInputAssemblyState = &orbitAssembly; orbitPipeline.pDepthStencilState=&orbitDepth;
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
void DestroyDynamicAnchoredSurface(App &a){
  for(uint32_t index=0;index<AnchoredSurfaceFrameResourceCount;index++){
    DestroyHostBuffer(a,a.anchoredSurfaceIndirectBuffers[index],a.anchoredSurfaceIndirectMemories[index],a.anchoredSurfaceIndirectMapped[index]);
    DestroyHostBuffer(a,a.anchoredSurfaceCoverageBuffers[index],a.anchoredSurfaceCoverageMemories[index],a.anchoredSurfaceCoverageMapped[index]);
  }
  DestroyHostBuffer(a,a.anchoredSurfaceVertexBuffer,a.anchoredSurfaceVertexMemory,a.anchoredSurfaceVertexMapped);
  DestroyHostBuffer(a,a.anchoredSurfaceIndexBuffer,a.anchoredSurfaceIndexMemory,a.anchoredSurfaceIndexMapped);
  DestroyHostBuffer(a,a.naturalGlobalPreparedBuffer,a.naturalGlobalPreparedMemory,a.naturalGlobalPreparedMapped);
  DestroyHostBuffer(a,a.naturalAnchoredPreparedBuffer,a.naturalAnchoredPreparedMemory,a.naturalAnchoredPreparedMapped);
  a.anchoredSurfaceSlotGenerations.clear();a.anchoredSurfaceActivePatches.clear();a.anchoredSurfaceActivePatchCount=0;a.anchoredSurfaceActiveGeneration=0;a.anchoredSurfacePublicationLogGeneration=0;
  a.anchoredSurfaceResourceGenerations.fill(0u);a.anchoredSurfaceResourceIndex=0;
  a.anchoredSurfaceResourcesReady=false;a.anchoredSurfaceActive=false;a.anchoredSurfacePublicationRequested=false;
  a.naturalGlobalPreparationPending=false;a.naturalGlobalPrepared=false;a.naturalAnchoredPreparationPending=false;
  a.naturalAnchoredPreparationGeneration=0;a.naturalAnchoredSubmittedGeneration=0;a.naturalAnchoredPreparedGeneration=0;
  a.naturalAnchoredPreparationPatchCount=0;
}
uint32_t AnchoredRemapIndex(uint32_t x,uint32_t y,uint32_t stitchMask){
  if(x==0u&&(stitchMask&1u)&&((y&1u)!=0u))y--;
  if(x==AnchoredSurfaceBaseGridResolution&&(stitchMask&2u)&&((y&1u)!=0u))y--;
  if(y==0u&&(stitchMask&4u)&&((x&1u)!=0u))x--;
  if(y==AnchoredSurfaceBaseGridResolution&&(stitchMask&8u)&&((x&1u)!=0u))x--;
  return y*(AnchoredSurfaceBaseGridResolution+1u)+x;
}
void ValidateAnchoredStitchTemplates(const uint32_t *indices){
  const int64_t expectedDoubleArea=int64_t(AnchoredSurfaceBaseGridResolution)*AnchoredSurfaceBaseGridResolution*2;
  for(uint32_t mask=0;mask<16u;mask++){
    int64_t doubleArea=0;const uint32_t first=mask*AnchoredSurfaceBaseIndicesPerPatch;
    for(uint32_t triangle=0;triangle<AnchoredSurfaceBaseIndicesPerPatch;triangle+=3u){
      const uint32_t i0=indices[first+triangle],i1=indices[first+triangle+1u],i2=indices[first+triangle+2u];
      if(i0>=AnchoredSurfaceBaseVerticesPerPatch||i1>=AnchoredSurfaceBaseVerticesPerPatch||i2>=AnchoredSurfaceBaseVerticesPerPatch)
        throw std::runtime_error("dynamic anchored stitch template index is out of range");
      const int64_t x0=i0%(AnchoredSurfaceBaseGridResolution+1u),y0=i0/(AnchoredSurfaceBaseGridResolution+1u);
      const int64_t x1=i1%(AnchoredSurfaceBaseGridResolution+1u),y1=i1/(AnchoredSurfaceBaseGridResolution+1u);
      const int64_t x2=i2%(AnchoredSurfaceBaseGridResolution+1u),y2=i2/(AnchoredSurfaceBaseGridResolution+1u);
      const int64_t area=(x1-x0)*(y2-y0)-(y1-y0)*(x2-x0);
      if(area<0)throw std::runtime_error("dynamic anchored stitch template winding is inconsistent");
      doubleArea+=area;
      const uint32_t values[]{i0,i1,i2};
      for(uint32_t value:values){const uint32_t x=value%(AnchoredSurfaceBaseGridResolution+1u),y=value/(AnchoredSurfaceBaseGridResolution+1u);
        if(((mask&1u)&&x==0u&&(y&1u))||((mask&2u)&&x==AnchoredSurfaceBaseGridResolution&&(y&1u))||
           ((mask&4u)&&y==0u&&(x&1u))||((mask&8u)&&y==AnchoredSurfaceBaseGridResolution&&(x&1u)))
          throw std::runtime_error("dynamic anchored stitch template retained an unmatched fine-edge vertex");
      }
    }
    if(doubleArea!=expectedDoubleArea)throw std::runtime_error("dynamic anchored stitch template does not cover exactly one patch");
  }
}
void CreateDynamicAnchoredSurface(App &a){
  const uint32_t requestedSlots=a.submission->anchoredSurfaceCacheSlotCount,slots=std::max(1u,requestedSlots);
  if(requestedSlots&&(requestedSlots>AnchoredSurfaceMaximumCacheSlots||!a.submission->anchoredSurfacePatches))
    throw std::runtime_error("invalid anchored surface cache allocation");
  CreateHostBuffer(a,VkDeviceSize(AnchoredSurfaceBaseVerticesPerPatch)*sizeof(PatchVertex),VK_BUFFER_USAGE_VERTEX_BUFFER_BIT,
    a.anchoredSurfaceVertexBuffer,a.anchoredSurfaceVertexMemory,a.anchoredSurfaceVertexMapped,"dynamic anchored vertex pool failed");
  auto *vertices=static_cast<PatchVertex*>(a.anchoredSurfaceVertexMapped);
  for(uint32_t y=0;y<=AnchoredSurfaceBaseGridResolution;y++)for(uint32_t x=0;x<=AnchoredSurfaceBaseGridResolution;x++)
    vertices[y*(AnchoredSurfaceBaseGridResolution+1u)+x]={{float(x)/AnchoredSurfaceBaseGridResolution,float(y)/AnchoredSurfaceBaseGridResolution}};
  CreateHostBuffer(a,VkDeviceSize(16u)*AnchoredSurfaceBaseIndicesPerPatch*sizeof(uint32_t),VK_BUFFER_USAGE_INDEX_BUFFER_BIT,
    a.anchoredSurfaceIndexBuffer,a.anchoredSurfaceIndexMemory,a.anchoredSurfaceIndexMapped,"dynamic anchored stitch index pool failed");
  auto *indices=static_cast<uint32_t*>(a.anchoredSurfaceIndexMapped);uint32_t write=0;
  for(uint32_t mask=0;mask<16u;mask++)for(uint32_t y=0;y<AnchoredSurfaceBaseGridResolution;y++)for(uint32_t x=0;x<AnchoredSurfaceBaseGridResolution;x++){
    const uint32_t q0=AnchoredRemapIndex(x,y,mask),q1=AnchoredRemapIndex(x+1u,y,mask),q2=AnchoredRemapIndex(x,y+1u,mask),q3=AnchoredRemapIndex(x+1u,y+1u,mask);
    indices[write++]=q0;indices[write++]=q1;indices[write++]=q2;indices[write++]=q1;indices[write++]=q3;indices[write++]=q2;
  }
  ValidateAnchoredStitchTemplates(indices);
  // std430 aligns the dvec4 array following the uvec4 header to 32 bytes.
  CreateHostBuffer(a,32u+VkDeviceSize(NaturalGlobalPatchCount)*NaturalGlobalVerticesPerPatch*sizeof(double)*4u,
    VK_BUFFER_USAGE_STORAGE_BUFFER_BIT,a.naturalGlobalPreparedBuffer,a.naturalGlobalPreparedMemory,
    a.naturalGlobalPreparedMapped,"natural terrain global prepared buffer failed");
  CreateHostBuffer(a,VkDeviceSize(slots)*NaturalAnchoredVerticesPerPatch*sizeof(double)*4u,
    VK_BUFFER_USAGE_STORAGE_BUFFER_BIT,a.naturalAnchoredPreparedBuffer,a.naturalAnchoredPreparedMemory,
    a.naturalAnchoredPreparedMapped,"natural terrain anchored prepared buffer failed");
  for(uint32_t index=0;index<AnchoredSurfaceFrameResourceCount;index++){
    CreateHostBuffer(a,sizeof(uint32_t)*4u*(1u+AnchoredSurfacePatchVectorOffset+AnchoredSurfaceMaximumPatches*AnchoredSurfacePatchVectorCount),VK_BUFFER_USAGE_STORAGE_BUFFER_BIT,
      a.anchoredSurfaceCoverageBuffers[index],a.anchoredSurfaceCoverageMemories[index],a.anchoredSurfaceCoverageMapped[index],"dynamic anchored coverage buffer failed");
    CreateHostBuffer(a,VkDeviceSize(AnchoredSurfaceMaximumPatches)*sizeof(VkDrawIndexedIndirectCommand),VK_BUFFER_USAGE_INDIRECT_BUFFER_BIT,
      a.anchoredSurfaceIndirectBuffers[index],a.anchoredSurfaceIndirectMemories[index],a.anchoredSurfaceIndirectMapped[index],"dynamic anchored indirect buffer failed");
  }
  a.anchoredSurfaceSlotGenerations.assign(slots,0u);a.anchoredSurfaceResourcesReady=requestedSlots!=0u;
}
uint32_t AnchoredCoverageHash(uint32_t key,uint32_t x,uint32_t y){
  uint32_t value=key*0x9e3779b9u;value^=x*0x85ebca6bu;value^=y*0xc2b2ae35u;value^=value>>16u;return value&(AnchoredSurfaceCoverageCapacity-1u);
}
void AuditDynamicAnchoredGroundTruth(App&a,uint32_t count,uint32_t resourceIndex){
  if(!a.anchoredGroundTruthEnabled||!count)return;
  const auto *draws=static_cast<const VkDrawIndexedIndirectCommand*>(a.anchoredSurfaceIndirectMapped[resourceIndex]);
  const auto *coverage=static_cast<const uint32_t*>(a.anchoredSurfaceCoverageMapped[resourceIndex]);
  const auto &frame=a.submission->anchoredSurfacePresentation;
  uint64_t descriptorMismatches=0,drawMismatches=0,nonFiniteBounds=0;
  descriptorMismatches+=std::memcmp(a.anchoredSurfaceActivePatches.data(),a.submission->anchoredSurfacePatches,size_t(count)*sizeof(NcAnchoredSurfacePatch))!=0;
  descriptorMismatches+=std::memcmp(coverage+4u*(1u+AnchoredSurfaceCoverageCapacity),&frame,sizeof frame)!=0;
  descriptorMismatches+=std::memcmp(coverage+4u*(1u+AnchoredSurfacePatchVectorOffset),a.submission->anchoredSurfacePatches,size_t(count)*sizeof(NcAnchoredSurfacePatch))!=0;
  for(uint32_t draw=0;draw<count;draw++){
    const auto &patch=a.submission->anchoredSurfacePatches[draw];const auto &command=draws[draw];
    drawMismatches+=command.indexCount!=AnchoredSurfaceBaseIndicesPerPatch||command.instanceCount!=1u||
      command.firstIndex!=patch.stitchMask*AnchoredSurfaceBaseIndicesPerPatch||command.vertexOffset!=0||command.firstInstance!=draw;
    nonFiniteBounds+=!std::isfinite(patch.boundsX)||!std::isfinite(patch.boundsY)||!std::isfinite(patch.boundsZ)||!std::isfinite(patch.boundsRadius)||patch.boundsRadius<=0;
  }
  char message[512];std::snprintf(message,sizeof message,"GPU refinement ground truth: generation=%u; patches=%u; reusableBaseVertices=%u; baseTriangles=%u; descriptorMismatches=%llu; drawMismatches=%llu; nonFiniteBounds=%llu; CPUFinalRaster=false; refinementTargetPixels=16; maximumTessFactor=16",a.submission->anchoredSurfaceActiveGeneration,count,AnchoredSurfaceBaseVerticesPerPatch,AnchoredSurfaceBaseIndicesPerPatch/3u,(unsigned long long)descriptorMismatches,(unsigned long long)drawMismatches,(unsigned long long)nonFiniteBounds);a.Log(NC_LOG_ALWAYS,message);
  if(descriptorMismatches||drawMismatches||nonFiniteBounds)throw std::runtime_error("dynamic anchored GPU descriptor/indirect invariant failed");
}
void BindDynamicAnchoredResource(App&a,uint32_t resourceIndex){
  VkDescriptorBufferInfo info{a.anchoredSurfaceCoverageBuffers[resourceIndex],0,
    sizeof(uint32_t)*4u*(1u+AnchoredSurfacePatchVectorOffset+AnchoredSurfaceMaximumPatches*AnchoredSurfacePatchVectorCount)};
  VkWriteDescriptorSet write{VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET};write.dstSet=a.descriptor;write.dstBinding=32;
  write.descriptorCount=1;write.descriptorType=VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;write.pBufferInfo=&info;
  vkUpdateDescriptorSets(a.device,1,&write,0,nullptr);
}
void BindNaturalAnchoredPreparationResource(App&a,uint32_t resourceIndex){
  VkDescriptorBufferInfo info{a.anchoredSurfaceCoverageBuffers[resourceIndex],0,
    sizeof(uint32_t)*4u*(1u+AnchoredSurfacePatchVectorOffset+AnchoredSurfaceMaximumPatches*AnchoredSurfacePatchVectorCount)};
  VkWriteDescriptorSet write{VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET};write.dstSet=a.descriptor;write.dstBinding=37;
  write.descriptorCount=1;write.descriptorType=VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;write.pBufferInfo=&info;
  vkUpdateDescriptorSets(a.device,1,&write,0,nullptr);
}
void UpdateDynamicAnchoredSurface(App &a){
  auto *coverage=static_cast<uint32_t*>(a.anchoredSurfaceCoverageMapped[a.anchoredSurfaceResourceIndex]);
  const auto count=a.submission->anchoredSurfacePatchCount;
  if(coverage)std::memcpy(coverage+4u*(1u+AnchoredSurfaceCoverageCapacity),
    &a.submission->anchoredSurfacePresentation,sizeof(NcAnchoredSurfacePresentation));
  a.anchoredSurfacePublicationRequested=count!=0u&&(a.submission->anchoredSurfaceFlags&1u)!=0u;
  if(!count){a.anchoredSurfaceActive=false;a.anchoredSurfaceActivePatchCount=0;a.anchoredSurfaceActivePatches.clear();if(coverage)std::memset(coverage,0,sizeof(uint32_t)*4u*(1u+AnchoredSurfaceCoverageCapacity));return;}
  if(count>AnchoredSurfaceMaximumPatches){a.anchoredSurfaceCapacityRejects++;if((a.anchoredSurfaceCapacityRejects&(a.anchoredSurfaceCapacityRejects-1u))==0u){char message[256];std::snprintf(message,sizeof message,"GPU terrain capacity preserved previous owner: requested=%u; capacity=%u; rejects=%llu",count,AnchoredSurfaceMaximumPatches,(unsigned long long)a.anchoredSurfaceCapacityRejects);a.Log(NC_LOG_ALWAYS,message);}return;}
  if(!a.anchoredSurfaceResourcesReady||!a.submission->anchoredSurfacePatches||
     (a.submission->anchoredSurfaceFlags&1u)==0u)return;
  const bool managedAcknowledged=a.submission->anchoredSurfaceGpuReadyGeneration==a.submission->anchoredSurfaceActiveGeneration;
  const bool newPublication=a.anchoredSurfaceActiveGeneration!=a.submission->anchoredSurfaceActiveGeneration;
  // Camera orientation changes update the frame data used by GPU visibility,
  // not the immutable retained descriptor generation.  Rebuilding the complete
  // hash table and indirect payload for an unchanged generation made a pure
  // look direction upload hundreds of megabytes of identical descriptors.
  if(!newPublication&&a.anchoredSurfaceActive&&count==a.anchoredSurfaceActivePatchCount)return;
  bool complete=true,authoritative=true;uint32_t maximumLevel=0;
  for(uint32_t index=0;index<count;index++){
    const auto &patch=a.submission->anchoredSurfacePatches[index];const uint64_t body=uint64_t(patch.bodyIdLow)|(uint64_t(patch.bodyIdHigh)<<32u);
    const uint32_t cells=patch.level<31u?1u<<patch.level:0u;
    if(body!=6u||patch.terrainVersion!=5u||patch.physicalSurfaceGeneration!=a.submission->physicalSurfaceGeneration||patch.face>=6u||patch.level>24u||!cells||patch.x>=cells||patch.y>=cells||
       patch.cacheSlot>=a.anchoredSurfaceSlotGenerations.size()||patch.stitchMask>15u){complete=false;continue;}
    const bool cpuComplete=(patch.flags&(AnchoredSurfaceReady|AnchoredSurfaceGeometryComplete|AnchoredSurfacePhysicalComplete|AnchoredSurfaceMaterialComplete))==(AnchoredSurfaceReady|AnchoredSurfaceGeometryComplete|AnchoredSurfacePhysicalComplete|AnchoredSurfaceMaterialComplete);
    if(!cpuComplete||!AnchoredPatchLocalPayloadsReady(a,patch)){complete=false;continue;}
    authoritative&=(patch.flags&AnchoredSurfaceRequired)==AnchoredSurfaceRequired;
    a.anchoredSurfaceSlotGenerations[patch.cacheSlot]=patch.cacheGeneration;
    maximumLevel=std::max(maximumLevel,patch.level);
  }
  if(!complete)return;
  if(a.submission->physicalSurfaceGeneration==4u&&newPublication&&
     a.naturalAnchoredPreparedGeneration!=a.submission->anchoredSurfaceActiveGeneration){
    // Preparation was submitted in the previous frame. The frame fence was
    // completed before this update, so this is the first point at which the
    // GPU-complete generation may be acknowledged to managed publication.
    if(a.naturalAnchoredSubmittedGeneration==a.submission->anchoredSurfaceActiveGeneration){
      a.naturalAnchoredPreparedGeneration=a.naturalAnchoredSubmittedGeneration;
      a.naturalAnchoredSubmittedGeneration=0u;
      a.submission->anchoredSurfaceGpuReadyGeneration=a.naturalAnchoredPreparedGeneration;
      return;
    }
    if(!a.naturalAnchoredPreparationPending||a.naturalAnchoredPreparationGeneration!=a.submission->anchoredSurfaceActiveGeneration){
      const uint32_t preparationResource=(a.anchoredSurfaceResourceIndex+1u)%AnchoredSurfaceFrameResourceCount;
      auto *preparation=static_cast<uint32_t*>(a.anchoredSurfaceCoverageMapped[preparationResource]);
      std::memset(preparation,0,sizeof(uint32_t)*4u*(1u+AnchoredSurfaceCoverageCapacity));
      preparation[0]=count;preparation[2]=a.submission->anchoredSurfaceActiveGeneration;
      std::memcpy(preparation+4u*(1u+AnchoredSurfacePatchVectorOffset),a.submission->anchoredSurfacePatches,size_t(count)*sizeof(NcAnchoredSurfacePatch));
      BindNaturalAnchoredPreparationResource(a,preparationResource);
      a.naturalAnchoredPreparationGeneration=a.submission->anchoredSurfaceActiveGeneration;
      a.naturalAnchoredPreparationPatchCount=count;
      a.naturalAnchoredPreparationPending=true;
    }
    return;
  }
  a.submission->anchoredSurfaceGpuReadyGeneration=a.submission->anchoredSurfaceActiveGeneration;
  if(!authoritative||!managedAcknowledged)return;
  // The frame fence was completed before this update. Build the replacement
  // coverage table and indirect stream in a retired slot, then bind it as one
  // immutable generation. The currently bound complete slot remains untouched
  // until this point and a slot is never reused while GPU work can reference it.
  const uint32_t resourceIndex=(a.anchoredSurfaceResourceIndex+1u)%AnchoredSurfaceFrameResourceCount;
  coverage=static_cast<uint32_t*>(a.anchoredSurfaceCoverageMapped[resourceIndex]);
  std::memcpy(coverage+4u*(1u+AnchoredSurfaceCoverageCapacity),
    &a.submission->anchoredSurfacePresentation,sizeof(NcAnchoredSurfacePresentation));
  if(coverage)std::memset(coverage,0,sizeof(uint32_t)*4u*(1u+AnchoredSurfaceCoverageCapacity));
  for(uint32_t index=0;index<count;index++){
    const auto &patch=a.submission->anchoredSurfacePatches[index];const uint32_t key=0x80000000u|patch.face|(patch.level<<3u);uint32_t slot=AnchoredCoverageHash(key,patch.x,patch.y);
    for(uint32_t probe=0;probe<AnchoredSurfaceCoverageCapacity;probe++,slot=(slot+1u)&(AnchoredSurfaceCoverageCapacity-1u)){
      uint32_t *entry=coverage+4u*(1u+slot);if(entry[0]==0u){entry[0]=key;entry[1]=patch.x;entry[2]=patch.y;entry[3]=a.submission->anchoredSurfaceActiveGeneration;break;}
      if(probe+1u==AnchoredSurfaceCoverageCapacity){complete=false;break;}
    }
    if(!complete)break;
  }
  if(!complete){std::memset(coverage,0,sizeof(uint32_t)*4u*(1u+AnchoredSurfaceCoverageCapacity));return;}
  coverage[0]=count;coverage[1]=maximumLevel;coverage[2]=a.submission->anchoredSurfaceActiveGeneration;coverage[3]=AnchoredSurfaceCoverageCapacity;
  std::memcpy(coverage+4u*(1u+AnchoredSurfacePatchVectorOffset),a.submission->anchoredSurfacePatches,size_t(count)*sizeof(NcAnchoredSurfacePatch));
  const size_t descriptorBytes=size_t(count)*sizeof(NcAnchoredSurfacePatch);
  a.anchoredSurfaceUploadBytes+=descriptorBytes;a.anchoredSurfaceUploads++;
  a.anchoredSurfaceActivePatches.assign(a.submission->anchoredSurfacePatches,a.submission->anchoredSurfacePatches+count);
  auto *draws=static_cast<VkDrawIndexedIndirectCommand*>(a.anchoredSurfaceIndirectMapped[resourceIndex]);
  for(uint32_t index=0;index<count;index++){
    const auto &patch=a.anchoredSurfaceActivePatches[index];
    const uint32_t firstIndex=patch.stitchMask*AnchoredSurfaceBaseIndicesPerPatch;
    if(firstIndex+AnchoredSurfaceBaseIndicesPerPatch>16u*AnchoredSurfaceBaseIndicesPerPatch){complete=false;break;}
    draws[index]={AnchoredSurfaceBaseIndicesPerPatch,1u,firstIndex,0,index};
  }
  if(!complete)return;
  if(newPublication)AuditDynamicAnchoredGroundTruth(a,count,resourceIndex);
  a.anchoredSurfaceResourceGenerations[resourceIndex]=a.submission->anchoredSurfaceActiveGeneration;
  a.anchoredSurfaceResourceIndex=resourceIndex;BindDynamicAnchoredResource(a,resourceIndex);
  a.anchoredSurfaceActivePatchCount=count;a.anchoredSurfaceActiveGeneration=a.submission->anchoredSurfaceActiveGeneration;
  a.anchoredSurfaceActive=true;
}
void CreateTerrainResidency(App &a) {
  if(a.terrainKeyBuffer)return;
  CreateHostBuffer(a,sizeof(uint32_t)*4*3*TerrainCacheCapacity,VK_BUFFER_USAGE_STORAGE_BUFFER_BIT,a.terrainKeyBuffer,a.terrainKeyMemory,a.terrainKeyMapped,"terrain residency key buffer failed");
  CreateHostBuffer(a,sizeof(float)*2*TerrainGridVertexCount*TerrainCacheCapacity,VK_BUFFER_USAGE_STORAGE_BUFFER_BIT,a.terrainSampleBuffer,a.terrainSampleMemory,a.terrainSampleMapped,"terrain residency sample buffer failed");
  CreateHostBuffer(a,sizeof(uint32_t)*2*GpuPatchCapacity,VK_BUFFER_USAGE_STORAGE_BUFFER_BIT,a.terrainPatchSlotBuffer,a.terrainPatchSlotMemory,a.terrainPatchSlotMapped,"terrain patch-slot buffer failed");
  // The complete bounded terrain-v5 L0-L2 hierarchy is renderer-lifetime
  // residency. Seed every stable identity before selection so focus and camera
  // changes can only alter demand/ownership, never payload availability.
  if(a.productionPack){auto *words=static_cast<uint32_t*>(a.terrainKeyMapped);for(uint32_t ordinal=0;ordinal<a.productionPack->RecordCount();ordinal++){nc::production::PatchId id;if(!a.productionPack->TryGetId(ordinal,id))throw std::runtime_error("production hierarchy identity unavailable");uint32_t *key=words+ordinal*12u;key[0]=uint32_t(id.bodyId);key[1]=uint32_t(id.bodyId>>32);key[2]=id.terrainVersion;key[3]=id.terrainVersion;key[4]=id.face;key[5]=id.level;key[6]=id.x;key[7]=id.y;}}
  char message[192];std::snprintf(message,sizeof message,"Terrain residency: capacity=%u; samples=%u; persistentBytes=%zu",TerrainCacheCapacity,TerrainGridVertexCount,size_t(sizeof(uint32_t)*4*3*TerrainCacheCapacity+sizeof(float)*2*TerrainGridVertexCount*TerrainCacheCapacity+sizeof(uint32_t)*2*GpuPatchCapacity));a.Log(NC_LOG_RENDERER,message);
}
void DestroyTerrainResidency(App &a) {
  DestroyHostBuffer(a,a.terrainPatchSlotBuffer,a.terrainPatchSlotMemory,a.terrainPatchSlotMapped);
  DestroyHostBuffer(a,a.terrainSampleBuffer,a.terrainSampleMemory,a.terrainSampleMapped);
  DestroyHostBuffer(a,a.terrainKeyBuffer,a.terrainKeyMemory,a.terrainKeyMapped);
}
void UpdateProductionBillboardDescriptors(App &a,bool incoming){
  if(!a.descriptor)return;const uint32_t base=incoming?45u:38u;const uint32_t vertices=incoming?a.productionBillboardIncomingVertexCount:a.productionBillboardVertexCount;const uint32_t triangles=incoming?a.productionBillboardIncomingTriangleCount:a.productionBillboardTriangleCount;
  VkBuffer buffers[7]{incoming?a.productionBillboardIncomingPhysicalBuffer:a.productionBillboardPhysicalBuffer,incoming?a.productionBillboardIncomingIndexBuffer:a.productionBillboardIndexBuffer,incoming?a.productionBillboardIncomingVisibilityBuffer:a.productionBillboardVisibilityBuffer,incoming?a.productionBillboardIncomingCompactedBuffer:a.productionBillboardCompactedBuffer,incoming?a.productionBillboardIncomingIndirectBuffer:a.productionBillboardIndirectBuffer,incoming?a.productionBillboardIncomingCounterBuffer:a.productionBillboardCounterBuffer,incoming?a.productionBillboardIncomingLatticeBuffer:a.productionBillboardLatticeBuffer};
  VkDeviceSize sizes[7]{VkDeviceSize(vertices)*sizeof(NcSphericalBillboardPhysicalVertex),VkDeviceSize(triangles)*3u*sizeof(uint32_t),VkDeviceSize(triangles)*sizeof(uint32_t),VkDeviceSize(triangles)*3u*sizeof(uint32_t),sizeof(VkDrawIndexedIndirectCommand),sizeof(uint32_t)*8u,VkDeviceSize(vertices)*sizeof(NcProductionBillboardLatticeVertex)};
  VkDescriptorBufferInfo infos[7]{};VkWriteDescriptorSet writes[7]{};for(uint32_t i=0;i<7;i++){infos[i]={buffers[i],0,sizes[i]};writes[i].sType=VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET;writes[i].dstSet=a.descriptor;writes[i].dstBinding=base+i;writes[i].descriptorCount=1;writes[i].descriptorType=VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;writes[i].pBufferInfo=&infos[i];}vkUpdateDescriptorSets(a.device,7,writes,0,nullptr);
}
void DestroyProductionBillboardIncoming(App &a){
  if(a.productionBillboardIncomingOwnsTopology){DestroyHostBuffer(a,a.productionBillboardIncomingLatticeBuffer,a.productionBillboardIncomingLatticeMemory,a.productionBillboardIncomingLatticeMapped);DestroyHostBuffer(a,a.productionBillboardIncomingIndexBuffer,a.productionBillboardIncomingIndexMemory,a.productionBillboardIncomingIndexMapped);}else{a.productionBillboardIncomingLatticeBuffer={};a.productionBillboardIncomingLatticeMemory={};a.productionBillboardIncomingLatticeMapped=nullptr;a.productionBillboardIncomingIndexBuffer={};a.productionBillboardIncomingIndexMemory={};a.productionBillboardIncomingIndexMapped=nullptr;}
  DestroyHostBuffer(a,a.productionBillboardIncomingPhysicalBuffer,a.productionBillboardIncomingPhysicalMemory,a.productionBillboardIncomingPhysicalMapped);DestroyHostBuffer(a,a.productionBillboardIncomingVisibilityBuffer,a.productionBillboardIncomingVisibilityMemory,a.productionBillboardIncomingVisibilityMapped);DestroyHostBuffer(a,a.productionBillboardIncomingCompactedBuffer,a.productionBillboardIncomingCompactedMemory,a.productionBillboardIncomingCompactedMapped);DestroyHostBuffer(a,a.productionBillboardIncomingIndirectBuffer,a.productionBillboardIncomingIndirectMemory,a.productionBillboardIncomingIndirectMapped);DestroyHostBuffer(a,a.productionBillboardIncomingCounterBuffer,a.productionBillboardIncomingCounterMemory,a.productionBillboardIncomingCounterMapped);a.productionBillboardIncomingEnabled=false;a.productionBillboardIncomingWorkRecorded=false;a.productionBillboardIncomingFencePending=false;a.productionBillboardIncomingOwnsTopology=false;a.productionBillboardIncomingVertexCount=0;a.productionBillboardIncomingTriangleCount=0;a.productionBillboardIncomingTopologyHash=0;a.productionBillboardIncomingGeneration=0;
}
void DestroyProductionBillboard(App &a){
  DestroyProductionBillboardIncoming(a);DestroyHostBuffer(a,a.productionBillboardLatticeBuffer,a.productionBillboardLatticeMemory,a.productionBillboardLatticeMapped);DestroyHostBuffer(a,a.productionBillboardPhysicalBuffer,a.productionBillboardPhysicalMemory,a.productionBillboardPhysicalMapped);DestroyHostBuffer(a,a.productionBillboardIndexBuffer,a.productionBillboardIndexMemory,a.productionBillboardIndexMapped);DestroyHostBuffer(a,a.productionBillboardVisibilityBuffer,a.productionBillboardVisibilityMemory,a.productionBillboardVisibilityMapped);DestroyHostBuffer(a,a.productionBillboardCompactedBuffer,a.productionBillboardCompactedMemory,a.productionBillboardCompactedMapped);DestroyHostBuffer(a,a.productionBillboardIndirectBuffer,a.productionBillboardIndirectMemory,a.productionBillboardIndirectMapped);DestroyHostBuffer(a,a.productionBillboardCounterBuffer,a.productionBillboardCounterMemory,a.productionBillboardCounterMapped);a.productionBillboardEnabled=false;a.productionBillboardWorkRecorded=false;a.productionBillboardFencePending=false;a.productionBillboardAuthoritative=false;a.productionBillboardVertexCount=0;a.productionBillboardTriangleCount=0;a.productionBillboardTopologyHash=0;a.productionBillboardGeneration=0;
}
void CreateProductionBillboard(App &a){
  const auto *candidate=a.submission->productionBillboard;if(!candidate||!candidate->enabled||candidate->publicationGeneration==a.productionBillboardGeneration||candidate->publicationGeneration==a.productionBillboardIncomingGeneration)return;if(a.productionBillboardIncomingEnabled)return;
  if(candidate->size!=sizeof(NcProductionSphericalBillboardSubmission)||candidate->version!=1||!candidate->latticeVertices||!candidate->indices||!candidate->physicalVertices||candidate->vertexCount==0||candidate->indexCount==0||candidate->indexCount%3u||candidate->latticeScale==0||candidate->topologyHash==0||candidate->physicalGeneration!=a.submission->physicalSurfaceGeneration)throw std::runtime_error("invalid production spherical billboard submission");
  a.productionBillboardIncomingVertexCount=candidate->vertexCount;a.productionBillboardIncomingTriangleCount=candidate->indexCount/3u;a.productionBillboardIncomingTopologyHash=candidate->topologyHash;a.productionBillboardIncomingGeneration=candidate->publicationGeneration;
  const bool reuseTopology=a.productionBillboardAuthoritative&&candidate->topologyHash==a.productionBillboardTopologyHash&&candidate->vertexCount==a.productionBillboardVertexCount&&candidate->indexCount==a.productionBillboardTriangleCount*3u;
  if(reuseTopology){a.productionBillboardIncomingLatticeBuffer=a.productionBillboardLatticeBuffer;a.productionBillboardIncomingLatticeMemory=a.productionBillboardLatticeMemory;a.productionBillboardIncomingLatticeMapped=a.productionBillboardLatticeMapped;a.productionBillboardIncomingIndexBuffer=a.productionBillboardIndexBuffer;a.productionBillboardIncomingIndexMemory=a.productionBillboardIndexMemory;a.productionBillboardIncomingIndexMapped=a.productionBillboardIndexMapped;}else{CreateHostBuffer(a,VkDeviceSize(candidate->vertexCount)*sizeof(NcProductionBillboardLatticeVertex),VK_BUFFER_USAGE_STORAGE_BUFFER_BIT,a.productionBillboardIncomingLatticeBuffer,a.productionBillboardIncomingLatticeMemory,a.productionBillboardIncomingLatticeMapped,"incoming production billboard lattice buffer failed");CreateHostBuffer(a,VkDeviceSize(candidate->indexCount)*sizeof(uint32_t),VK_BUFFER_USAGE_STORAGE_BUFFER_BIT,a.productionBillboardIncomingIndexBuffer,a.productionBillboardIncomingIndexMemory,a.productionBillboardIncomingIndexMapped,"incoming production billboard index buffer failed");std::memcpy(a.productionBillboardIncomingLatticeMapped,candidate->latticeVertices,VkDeviceSize(candidate->vertexCount)*sizeof(NcProductionBillboardLatticeVertex));std::memcpy(a.productionBillboardIncomingIndexMapped,candidate->indices,VkDeviceSize(candidate->indexCount)*sizeof(uint32_t));a.productionBillboardIncomingOwnsTopology=true;a.productionBillboardTopologyUploads++;}
  CreateHostBuffer(a,VkDeviceSize(candidate->vertexCount)*sizeof(NcSphericalBillboardPhysicalVertex),VK_BUFFER_USAGE_STORAGE_BUFFER_BIT,a.productionBillboardIncomingPhysicalBuffer,a.productionBillboardIncomingPhysicalMemory,a.productionBillboardIncomingPhysicalMapped,"incoming production billboard physical buffer failed");CreateHostBuffer(a,VkDeviceSize(a.productionBillboardIncomingTriangleCount)*sizeof(uint32_t),VK_BUFFER_USAGE_STORAGE_BUFFER_BIT,a.productionBillboardIncomingVisibilityBuffer,a.productionBillboardIncomingVisibilityMemory,a.productionBillboardIncomingVisibilityMapped,"incoming production billboard visibility buffer failed");CreateHostBuffer(a,VkDeviceSize(candidate->indexCount)*sizeof(uint32_t),VK_BUFFER_USAGE_STORAGE_BUFFER_BIT|VK_BUFFER_USAGE_INDEX_BUFFER_BIT,a.productionBillboardIncomingCompactedBuffer,a.productionBillboardIncomingCompactedMemory,a.productionBillboardIncomingCompactedMapped,"incoming production billboard compacted buffer failed");CreateHostBuffer(a,sizeof(VkDrawIndexedIndirectCommand),VK_BUFFER_USAGE_STORAGE_BUFFER_BIT|VK_BUFFER_USAGE_INDIRECT_BUFFER_BIT,a.productionBillboardIncomingIndirectBuffer,a.productionBillboardIncomingIndirectMemory,a.productionBillboardIncomingIndirectMapped,"incoming production billboard indirect buffer failed");CreateHostBuffer(a,sizeof(uint32_t)*8u,VK_BUFFER_USAGE_STORAGE_BUFFER_BIT,a.productionBillboardIncomingCounterBuffer,a.productionBillboardIncomingCounterMemory,a.productionBillboardIncomingCounterMapped,"incoming production billboard counter buffer failed");
  std::memcpy(a.productionBillboardIncomingPhysicalMapped,candidate->physicalVertices,VkDeviceSize(candidate->vertexCount)*sizeof(NcSphericalBillboardPhysicalVertex));auto *counters=static_cast<uint32_t*>(a.productionBillboardIncomingCounterMapped);counters[5]=a.productionBillboardIncomingTriangleCount;counters[6]=a.productionBillboardIncomingVertexCount;counters[7]=static_cast<uint32_t>(a.productionBillboardIncomingGeneration);a.productionBillboardIncomingEnabled=true;a.productionBillboardEnabled=true;UpdateProductionBillboardDescriptors(a,true);
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
  a.descriptor = {};
  a.submissionBuffer = {};
  a.submissionMemory = {};
  DestroyPatchBuffer(a);
  DestroyHostBuffer(a,a.gpuInputBuffer,a.gpuInputMemory,a.gpuInputMapped);
  DestroyHostBuffer(a,a.gpuWorkBuffer,a.gpuWorkMemory,a.gpuWorkMapped);
  DestroyHostBuffer(a,a.gpuNodeBuffer,a.gpuNodeMemory,a.gpuNodeMapped);
  DestroyHostBuffer(a,a.gpuControlBuffer,a.gpuControlMemory,a.gpuControlMapped);
  DestroyHostBuffer(a,a.planetaryPresentationBuffer,a.planetaryPresentationMemory,a.planetaryPresentationMapped);
  DestroyProductionBillboard(a);
  DestroyDynamicAnchoredSurface(a);
  DestroyHostBuffer(a,a.physicalOracleBuffer,a.physicalOracleMemory,a.physicalOracleMapped);
  DestroyHostBuffer(a,a.localLookupBuffer,a.localLookupMemory,a.localLookupMapped);
  a.gpuFrameSubmitted=false;a.hasGpuTelemetry=false;a.timestampFrameSubmitted=false;
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
  CreateDynamicAnchoredSurface(a);
  if(a.elevationOraclePath.empty())throw std::runtime_error("production physical elevation oracle path is required");
  CreateHostBuffer(a,PhysicalOracleBytes,VK_BUFFER_USAGE_STORAGE_BUFFER_BIT,a.physicalOracleBuffer,a.physicalOracleMemory,a.physicalOracleMapped,"physical elevation oracle buffer failed");
  {std::ifstream input(a.elevationOraclePath,std::ios::binary|std::ios::ate);if(!input||VkDeviceSize(input.tellg())!=PhysicalOracleBytes)throw std::runtime_error("physical elevation oracle dimensions mismatch");input.seekg(0);if(!input.read(static_cast<char*>(a.physicalOracleMapped),static_cast<std::streamsize>(PhysicalOracleBytes)))throw std::runtime_error("physical elevation oracle read failed");}
  CreateHostBuffer(a,sizeof(uint32_t)*(16+LocalLookupEntryWords*LocalLookupCapacity),VK_BUFFER_USAGE_STORAGE_BUFFER_BIT,a.localLookupBuffer,a.localLookupMemory,a.localLookupMapped,"local terrain lookup buffer failed");
  RebuildLocalLookup(a);
  CreateTerrainResidency(a);
  CreateProductionBillboard(a);
  VkDescriptorPoolSize ps[3]{{VK_DESCRIPTOR_TYPE_STORAGE_BUFFER,32},{VK_DESCRIPTOR_TYPE_INPUT_ATTACHMENT,1},{VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER,7}};
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
  VkDescriptorBufferInfo infos[10]{{a.submissionBuffer,0,a.submissionSize},{a.patchBuffer,0,a.patchSize},{a.gpuInputBuffer,0,sizeof(NcPlanetaryGpuConstants)},{a.gpuWorkBuffer,0,sizeof(uint32_t)*4*GpuPatchCapacity*2},{a.gpuNodeBuffer,0,sizeof(uint32_t)*4*GpuNodeEntryCapacity},{a.gpuControlBuffer,0,sizeof(GpuPlanetaryControl)},{a.planetaryPresentationBuffer,0,sizeof(NcPlanetaryPresentation)*10},{a.terrainKeyBuffer,0,sizeof(uint32_t)*4*3*TerrainCacheCapacity},{a.terrainSampleBuffer,0,sizeof(float)*2*TerrainGridVertexCount*TerrainCacheCapacity},{a.terrainPatchSlotBuffer,0,sizeof(uint32_t)*2*GpuPatchCapacity}};
  const uint32_t storageBindings[10]{0,1,2,3,4,5,6,8,9,10};VkWriteDescriptorSet writes[10]{};for(uint32_t index=0;index<10;index++){writes[index].sType=VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET;writes[index].dstSet=a.descriptor;writes[index].dstBinding=storageBindings[index];writes[index].descriptorCount=1;writes[index].descriptorType=VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;writes[index].pBufferInfo=&infos[index];}
  vkUpdateDescriptorSets(a.device,10,writes,0,nullptr);
  if(a.productionBillboardAuthoritative)UpdateProductionBillboardDescriptors(a,false);if(a.productionBillboardIncomingEnabled)UpdateProductionBillboardDescriptors(a,true);
  VkDescriptorBufferInfo productionLookupInfo{a.productionLayerLookupBuffer,0,sizeof(uint32_t)*ProductionLookupCapacity};VkWriteDescriptorSet productionLookupWrite{VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET};productionLookupWrite.dstSet=a.descriptor;productionLookupWrite.dstBinding=27;productionLookupWrite.descriptorCount=1;productionLookupWrite.descriptorType=VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;productionLookupWrite.pBufferInfo=&productionLookupInfo;vkUpdateDescriptorSets(a.device,1,&productionLookupWrite,0,nullptr);
  VkDescriptorBufferInfo localLookupInfo{a.localLookupBuffer,0,sizeof(uint32_t)*(16+LocalLookupEntryWords*LocalLookupCapacity)};VkWriteDescriptorSet localLookupWrite{VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET};localLookupWrite.dstSet=a.descriptor;localLookupWrite.dstBinding=31;localLookupWrite.descriptorCount=1;localLookupWrite.descriptorType=VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;localLookupWrite.pBufferInfo=&localLookupInfo;vkUpdateDescriptorSets(a.device,1,&localLookupWrite,0,nullptr);
  VkDescriptorBufferInfo anchoredCoverageInfo{a.anchoredSurfaceCoverageBuffers[a.anchoredSurfaceResourceIndex],0,sizeof(uint32_t)*4u*(1u+AnchoredSurfacePatchVectorOffset+AnchoredSurfaceMaximumPatches*AnchoredSurfacePatchVectorCount)};VkWriteDescriptorSet anchoredCoverageWrite{VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET};anchoredCoverageWrite.dstSet=a.descriptor;anchoredCoverageWrite.dstBinding=32;anchoredCoverageWrite.descriptorCount=1;anchoredCoverageWrite.descriptorType=VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;anchoredCoverageWrite.pBufferInfo=&anchoredCoverageInfo;vkUpdateDescriptorSets(a.device,1,&anchoredCoverageWrite,0,nullptr);
  VkDescriptorBufferInfo physicalOracleInfo{a.physicalOracleBuffer,0,PhysicalOracleBytes};VkWriteDescriptorSet physicalOracleWrite{VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET};physicalOracleWrite.dstSet=a.descriptor;physicalOracleWrite.dstBinding=33;physicalOracleWrite.descriptorCount=1;physicalOracleWrite.descriptorType=VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;physicalOracleWrite.pBufferInfo=&physicalOracleInfo;vkUpdateDescriptorSets(a.device,1,&physicalOracleWrite,0,nullptr);
  VkDescriptorBufferInfo naturalGlobalInfo{a.naturalGlobalPreparedBuffer,0,32u+VkDeviceSize(NaturalGlobalPatchCount)*NaturalGlobalVerticesPerPatch*sizeof(double)*4u};VkWriteDescriptorSet naturalGlobalWrite{VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET};naturalGlobalWrite.dstSet=a.descriptor;naturalGlobalWrite.dstBinding=35;naturalGlobalWrite.descriptorCount=1;naturalGlobalWrite.descriptorType=VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;naturalGlobalWrite.pBufferInfo=&naturalGlobalInfo;vkUpdateDescriptorSets(a.device,1,&naturalGlobalWrite,0,nullptr);
  VkDescriptorBufferInfo naturalAnchoredInfo{a.naturalAnchoredPreparedBuffer,0,VkDeviceSize(std::max(1u,a.submission->anchoredSurfaceCacheSlotCount))*NaturalAnchoredVerticesPerPatch*sizeof(double)*4u};VkWriteDescriptorSet naturalAnchoredWrite{VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET};naturalAnchoredWrite.dstSet=a.descriptor;naturalAnchoredWrite.dstBinding=36;naturalAnchoredWrite.descriptorCount=1;naturalAnchoredWrite.descriptorType=VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;naturalAnchoredWrite.pBufferInfo=&naturalAnchoredInfo;vkUpdateDescriptorSets(a.device,1,&naturalAnchoredWrite,0,nullptr);
  VkDescriptorBufferInfo naturalAnchoredInputInfo{a.anchoredSurfaceCoverageBuffers[a.anchoredSurfaceResourceIndex],0,sizeof(uint32_t)*4u*(1u+AnchoredSurfacePatchVectorOffset+AnchoredSurfaceMaximumPatches*AnchoredSurfacePatchVectorCount)};VkWriteDescriptorSet naturalAnchoredInputWrite{VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET};naturalAnchoredInputWrite.dstSet=a.descriptor;naturalAnchoredInputWrite.dstBinding=37;naturalAnchoredInputWrite.descriptorCount=1;naturalAnchoredInputWrite.descriptorType=VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;naturalAnchoredInputWrite.pBufferInfo=&naturalAnchoredInputInfo;vkUpdateDescriptorSets(a.device,1,&naturalAnchoredInputWrite,0,nullptr);
  VkDescriptorImageInfo sceneInput{};sceneInput.imageView=a.sceneColorView;sceneInput.imageLayout=VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL;VkWriteDescriptorSet sceneWrite{VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET};sceneWrite.dstSet=a.descriptor;sceneWrite.dstBinding=7;sceneWrite.descriptorCount=1;sceneWrite.descriptorType=VK_DESCRIPTOR_TYPE_INPUT_ATTACHMENT;sceneWrite.pImageInfo=&sceneInput;vkUpdateDescriptorSets(a.device,1,&sceneWrite,0,nullptr);
  if(a.productionPack){VkDescriptorImageInfo productionInfos[3]{};VkWriteDescriptorSet productionWrites[3]{};for(uint32_t index=0;index<3;index++){productionInfos[index]={a.productionSampler,a.productionImageViews[index],VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL};productionWrites[index].sType=VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET;productionWrites[index].dstSet=a.descriptor;productionWrites[index].dstBinding=24+index;productionWrites[index].descriptorCount=1;productionWrites[index].descriptorType=VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER;productionWrites[index].pImageInfo=&productionInfos[index];}vkUpdateDescriptorSets(a.device,3,productionWrites,0,nullptr);}
  {VkDescriptorImageInfo localInfos[4]{};VkWriteDescriptorSet localWrites[4]{};for(uint32_t index=0;index<4;index++){localInfos[index]={a.localSampler,a.localImageViews[index],VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL};localWrites[index].sType=VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET;localWrites[index].dstSet=a.descriptor;localWrites[index].dstBinding=index<3?28+index:34;localWrites[index].descriptorCount=1;localWrites[index].descriptorType=VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER;localWrites[index].pImageInfo=&localInfos[index];}vkUpdateDescriptorSets(a.device,4,localWrites,0,nullptr);}
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
bool ProductionRootPayloadsReady(const App &a) {
  const auto *lookup=static_cast<const uint32_t*>(a.productionLayerLookupMapped);
  if(!lookup)return false;
  for(uint32_t face=0;face<6;face++)if(lookup[nc::production::Pack::Ordinal(face,0,0,0)]==0u)return false;
  return true;
}
void Upload(App &a) {
  std::memcpy(a.mapped, &a.submission->camera, sizeof(NcCameraData));
  std::memcpy((char *)a.mapped + sizeof(NcCameraData),
              a.submission->objects,
              sizeof(NcRenderObject) * a.submission->objectCount);
  auto gpuInput=a.submission->planetaryGpu;gpuInput.terrainFrame=static_cast<uint32_t>(++a.frame);const bool productionSurface=a.submission->planetarySurfaceMode==NC_PLANETARY_SURFACE_PRODUCTION_CUBE;if(productionSurface){if(!a.productionPack)throw std::runtime_error("production cube pack is required for mode 2");gpuInput.maximumLevel=std::min(gpuInput.maximumLevel,a.productionPack->MaximumLevel());}if(a.submission->planetaryMode==NC_PLANETARY_CPU_REFERENCE)gpuInput.terrainVersion=0;
  const auto &contextPresentation=a.submission->planetaryPresentation;
  const uint64_t contextBody=uint64_t(contextPresentation.bodyIdLow)|(uint64_t(contextPresentation.bodyIdHigh)<<32u);
  uint32_t radiusHighBits{},radiusLowBits{};
  std::memcpy(&radiusHighBits,&gpuInput.radiusHigh,4);std::memcpy(&radiusLowBits,&gpuInput.radiusLow,4);
  const uint32_t contextMode=static_cast<uint32_t>(a.submission->planetarySurfaceMode),contextRegime=static_cast<uint32_t>(contextPresentation.regime);
  const bool contextChanged=!a.surfaceContextValid||a.surfaceContextBodyId!=contextBody||a.surfaceContextTerrainVersion!=gpuInput.terrainVersion||a.surfaceContextPhysicalGeneration!=a.submission->physicalSurfaceGeneration||a.surfaceContextMode!=contextMode||a.surfaceContextRegime!=contextRegime||a.surfaceContextRadiusHighBits!=radiusHighBits||a.surfaceContextRadiusLowBits!=radiusLowBits;
  if(contextChanged){
    if(productionSurface&&!ProductionHierarchyPayloadsReady(a))throw std::runtime_error("production context selected before the complete immutable L0-L2 hierarchy was resident");
    a.surfaceContextValid=true;a.surfaceContextBodyId=contextBody;a.surfaceContextTerrainVersion=gpuInput.terrainVersion;a.surfaceContextPhysicalGeneration=a.submission->physicalSurfaceGeneration;a.surfaceContextMode=contextMode;a.surfaceContextRegime=contextRegime;a.surfaceContextRadiusHighBits=radiusHighBits;a.surfaceContextRadiusLowBits=radiusLowBits;a.surfaceTransitionEpoch++;a.surfaceContextInvalidations++;a.earthTransitionTraceRemaining=contextBody==nc::production::EarthBodyId?180u:0u;a.earthSubmissionTraceRemaining=a.earthTransitionTraceRemaining;a.productionGeometryTraceLogged=false;std::memset(a.gpuControlMapped,0,sizeof(GpuPlanetaryControl));if(productionSurface)SeedProductionTerrainCacheHighWater(a);a.hasGpuTelemetry=false;
    auto *naturalControl=static_cast<uint32_t*>(a.naturalGlobalPreparedMapped);if(a.submission->physicalSurfaceGeneration==4u){naturalControl[0]=0u;a.naturalGlobalPreparationPending=true;a.naturalGlobalPrepared=false;}else{naturalControl[0]=3u;a.naturalGlobalPreparationPending=false;a.naturalGlobalPrepared=false;a.naturalAnchoredPreparationPending=false;a.naturalAnchoredPreparationGeneration=0u;a.naturalAnchoredSubmittedGeneration=0u;a.naturalAnchoredPreparedGeneration=0u;}
    char transition[352];std::snprintf(transition,sizeof transition,"Planetary context transition: epoch=%llu; body=%llu; surfaceMode=%u; terrainVersion=%u; physicalGeneration=%u; regime=%u; productionEligible=%s; owner=%s",(unsigned long long)a.surfaceTransitionEpoch,(unsigned long long)contextBody,contextMode,gpuInput.terrainVersion,a.submission->physicalSurfaceGeneration,contextRegime,productionSurface?"true":"false",productionSurface?"terrain-v5":"bounded-sphere");a.Log(NC_LOG_ALWAYS,transition);
    if(contextBody==6u){
      uint32_t rootMask=0u;auto *lookup=static_cast<uint32_t*>(a.productionLayerLookupMapped);for(uint32_t face=0;face<6&&lookup;face++)if(lookup[nc::production::Pack::Ordinal(face,0,0,0)]!=0u)rootMask|=1u<<face;
      const float lx=a.submission->solarLighting.sourceCenterX-contextPresentation.centerX,ly=a.submission->solarLighting.sourceCenterY-contextPresentation.centerY,lz=a.submission->solarLighting.sourceCenterZ-contextPresentation.centerZ;
      char materialState[384];std::snprintf(materialState,sizeof materialState,"Earth material state: epoch=%llu; body=6; terrainVersion=%u; material=%u; response=(%.6f,%.6f,%.6f); rootPayloadMask=0x%02X; sharedAlbedoElevationLand=true; owner=terrain-v5; sunDirectionInput=(%.6g,%.6g,%.6g)",(unsigned long long)a.surfaceTransitionEpoch,gpuInput.terrainVersion,contextPresentation.albedoSource,contextPresentation.roughness,contextPresentation.specular,contextPresentation.emissive,rootMask,lx,ly,lz);a.Log(NC_LOG_ALWAYS,materialState);
    }
  }else if(productionSurface)a.productionDemandHits++;else a.productionDemandMisses++;
  if(productionSurface&&!a.productionSurfaceLogged){a.Log(NC_LOG_ALWAYS,"Production surface: terrain-v5 body-fixed relaxed cube-sphere; real NCCUBE payloads; dynamic hierarchy enabled");a.productionSurfaceLogged=true;}std::memcpy(a.gpuInputMapped,&gpuInput,sizeof(gpuInput));
  if(a.submission->distantBodyCount)std::memcpy(a.planetaryPresentationMapped,a.submission->distantBodies,sizeof(NcPlanetaryPresentation)*a.submission->distantBodyCount);else std::memcpy(a.planetaryPresentationMapped,&a.submission->planetaryPresentation,sizeof(NcPlanetaryPresentation));
  if(!a.productionBillboardAuthoritative)UpdateDynamicAnchoredSurface(a);
  if(a.anchoredSurfaceActive){
    if(a.anchoredSurfacePublicationLogGeneration!=a.anchoredSurfaceActiveGeneration){
      a.anchoredSurfacePublicationLogGeneration=a.anchoredSurfaceActiveGeneration;char message[384];
      std::snprintf(message,sizeof message,"Dynamic hierarchy publication: generation=%u; physicalGeneration=%u; patches=%u; coverageEntries=%u; indirectCommands=%u; naturalPrepared=%s; complete=true; invalidDraws=0; zeroOwner=0; ownershipOverlap=0; globalFill=true",a.anchoredSurfaceActiveGeneration,a.submission->physicalSurfaceGeneration,a.anchoredSurfaceActivePatchCount,a.anchoredSurfaceActivePatchCount,a.anchoredSurfaceActivePatchCount,a.submission->physicalSurfaceGeneration==4u&&a.naturalAnchoredPreparedGeneration==a.anchoredSurfaceActiveGeneration?"true":"false");
      a.Log(NC_LOG_ALWAYS,message);
    }
  }
  if(a.earthTransitionTraceRemaining){uint32_t rootMask=0u;const auto *lookup=static_cast<const uint32_t*>(a.productionLayerLookupMapped);for(uint32_t face=0;face<6&&lookup;face++)if(lookup[nc::production::Pack::Ordinal(face,0,0,0)]!=0u)rootMask|=1u<<face;const double cameraX=double(gpuInput.cameraBodyHighX)+gpuInput.cameraBodyLowX,cameraY=double(gpuInput.cameraBodyHighY)+gpuInput.cameraBodyLowY,cameraZ=double(gpuInput.cameraBodyHighZ)+gpuInput.cameraBodyLowZ,radius=double(gpuInput.radiusHigh)+gpuInput.radiusLow,distance=std::sqrt(cameraX*cameraX+cameraY*cameraY+cameraZ*cameraZ);const bool candidateOwner=a.productionBillboardAuthoritative,distantOwner=false,globalOwner=productionSurface&&!candidateOwner,dynamicOwner=!candidateOwner&&a.anchoredSurfaceActive;char trace[832];std::snprintf(trace,sizeof trace,"Earth focus frame: frame=%llu; epoch=%llu; focusedBody=%llu; cameraTargetBody=%llu; radius=%.9f; distance=%.9f; altitude=%.9f; surfaceMode=%u; terrainVersion=%u; regime=%u; roots=0x%02X; activePatches=%u; distantOwner=%u; globalOwner=%u; dynamicOwner=%u; candidateOwner=%u; material=%s; fingerprint=%u/%.6f/%.6f/%.6f; center=(%.9g,%.9g,%.9g); orientation=(%.9g,%.9g,%.9g,%.9g); presentationRadius=%.9f; draws=%u/%u/%u/%u",(unsigned long long)a.frame,(unsigned long long)a.surfaceTransitionEpoch,(unsigned long long)contextBody,(unsigned long long)contextBody,radius,distance,double(gpuInput.surfaceAltitudeMetres),contextMode,gpuInput.terrainVersion,contextRegime,rootMask,a.hasGpuTelemetry?a.lastGpuTelemetry.active:0u,distantOwner?1u:0u,globalOwner?1u:0u,dynamicOwner?1u:0u,candidateOwner?1u:0u,candidateOwner?"production-billboard":"terrain-v5-root",contextPresentation.albedoSource,contextPresentation.roughness,contextPresentation.specular,contextPresentation.emissive,contextPresentation.centerX,contextPresentation.centerY,contextPresentation.centerZ,contextPresentation.bodyOrientationX,contextPresentation.bodyOrientationY,contextPresentation.bodyOrientationZ,contextPresentation.bodyOrientationW,double(contextPresentation.radius),distantOwner?1u:0u,globalOwner?1u:0u,dynamicOwner?1u:0u,candidateOwner?1u:0u);a.Log(NC_LOG_ALWAYS,trace);a.earthTransitionTraceRemaining--;}
  // A production Earth has no generic distant fallback: its six immutable
  // terrain-v5 roots remain the visible owner at every planetary distance.
  // Clearing the selector here would leave the DistantOnly regime with no
  // geometry at all.  Unsupported bodies retain the bounded-sphere reset.
  if(!productionSurface&&a.submission->planetaryPresentation.enabled&&a.submission->planetaryPresentation.regime==NC_PLANETARY_DISTANT_ONLY)std::memset(a.gpuControlMapped,0,sizeof(GpuPlanetaryControl));
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
  VkQueryPoolCreateInfo pipelineStatistics{VK_STRUCTURE_TYPE_QUERY_POOL_CREATE_INFO};
  pipelineStatistics.queryType=VK_QUERY_TYPE_PIPELINE_STATISTICS;pipelineStatistics.queryCount=1;
  pipelineStatistics.pipelineStatistics=VK_QUERY_PIPELINE_STATISTIC_CLIPPING_PRIMITIVES_BIT|
    VK_QUERY_PIPELINE_STATISTIC_TESSELLATION_CONTROL_SHADER_PATCHES_BIT|
    VK_QUERY_PIPELINE_STATISTIC_TESSELLATION_EVALUATION_SHADER_INVOCATIONS_BIT;
  a.Check(vkCreateQueryPool(a.device,&pipelineStatistics,nullptr,&a.anchoredPipelineStatistics),
    "anchored pipeline statistics query pool failed");
}
void RecordProductionBillboardWork(App &a,VkCommandBuffer c,bool incoming){
  const bool enabled=incoming?a.productionBillboardIncomingEnabled:a.productionBillboardAuthoritative;if(!enabled)return;if(incoming&&a.productionBillboardIncomingWorkRecorded)return;const uint32_t triangles=incoming?a.productionBillboardIncomingTriangleCount:a.productionBillboardTriangleCount;
  vkCmdBindDescriptorSets(c,VK_PIPELINE_BIND_POINT_COMPUTE,a.pipelineLayout,0,1,&a.descriptor,0,nullptr);vkCmdBindPipeline(c,VK_PIPELINE_BIND_POINT_COMPUTE,incoming?a.productionBillboardIncomingResetPipeline:a.productionBillboardResetPipeline);vkCmdDispatch(c,1,1,1);VkMemoryBarrier resetBarrier{VK_STRUCTURE_TYPE_MEMORY_BARRIER};resetBarrier.srcAccessMask=VK_ACCESS_SHADER_WRITE_BIT;resetBarrier.dstAccessMask=VK_ACCESS_SHADER_READ_BIT|VK_ACCESS_SHADER_WRITE_BIT;vkCmdPipelineBarrier(c,VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,0,1,&resetBarrier,0,nullptr,0,nullptr);vkCmdBindPipeline(c,VK_PIPELINE_BIND_POINT_COMPUTE,incoming?a.productionBillboardIncomingCullPipeline:a.productionBillboardCullPipeline);vkCmdDispatch(c,(triangles+63u)/64u,1,1);VkMemoryBarrier cullBarrier{VK_STRUCTURE_TYPE_MEMORY_BARRIER};cullBarrier.srcAccessMask=VK_ACCESS_SHADER_WRITE_BIT;cullBarrier.dstAccessMask=VK_ACCESS_SHADER_READ_BIT|VK_ACCESS_SHADER_WRITE_BIT;vkCmdPipelineBarrier(c,VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,0,1,&cullBarrier,0,nullptr,0,nullptr);vkCmdBindPipeline(c,VK_PIPELINE_BIND_POINT_COMPUTE,incoming?a.productionBillboardIncomingCompactPipeline:a.productionBillboardCompactPipeline);vkCmdDispatch(c,(triangles+63u)/64u,1,1);VkMemoryBarrier compactBarrier{VK_STRUCTURE_TYPE_MEMORY_BARRIER};compactBarrier.srcAccessMask=VK_ACCESS_SHADER_WRITE_BIT;compactBarrier.dstAccessMask=VK_ACCESS_HOST_READ_BIT|VK_ACCESS_INDIRECT_COMMAND_READ_BIT|VK_ACCESS_INDEX_READ_BIT;vkCmdPipelineBarrier(c,VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,VK_PIPELINE_STAGE_HOST_BIT|VK_PIPELINE_STAGE_DRAW_INDIRECT_BIT|VK_PIPELINE_STAGE_VERTEX_INPUT_BIT,0,1,&compactBarrier,0,nullptr,0,nullptr);if(incoming)a.productionBillboardIncomingWorkRecorded=true;else a.productionBillboardWorkRecorded=true;
}
void Record(App &a, uint32_t image) {
  auto c = a.commands[image];
  vkResetCommandBuffer(c, 0);
  VkCommandBufferBeginInfo bi{VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO};
  a.Check(vkBeginCommandBuffer(c, &bi), "command begin failed");
  RecordProductionUploads(a,c);
  RecordLocalUploads(a,c);
  vkCmdResetQueryPool(c,a.timestampQueries,0,App::TimestampCount);vkCmdWriteTimestamp(c,VK_PIPELINE_STAGE_TOP_OF_PIPE_BIT,a.timestampQueries,0);
  vkCmdResetQueryPool(c,a.anchoredPipelineStatistics,0,1);
  const auto &presentation=a.submission->planetaryPresentation;const bool candidate=a.productionBillboardAuthoritative;const bool production=a.submission->planetarySurfaceMode==NC_PLANETARY_SURFACE_PRODUCTION_CUBE;const bool handoff=presentation.enabled!=0;const bool detailedPresentation=!handoff||presentation.regime!=NC_PLANETARY_DISTANT_ONLY;const bool distantPresentation=!candidate&&handoff&&!production&&presentation.regime!=NC_PLANETARY_DETAILED_ONLY&&presentation.distantAlpha>0;const bool diagnosticGlobal=(a.surfaceDiagnostic&SurfaceDiagnosticDisableGlobal)==0;const bool diagnosticAnchored=(a.surfaceDiagnostic&SurfaceDiagnosticDisableAnchored)==0;const bool regional=production||detailedPresentation;const bool gpuPlanetary=!candidate&&regional&&a.submission->planetaryMode!=NC_PLANETARY_CPU_REFERENCE;
  const bool productionBillboardCompute=a.productionBillboardAuthoritative||a.productionBillboardIncomingEnabled;
  if(distantPresentation||detailedPresentation||gpuPlanetary||productionBillboardCompute){VkMemoryBarrier hostBarrier{VK_STRUCTURE_TYPE_MEMORY_BARRIER};hostBarrier.srcAccessMask=VK_ACCESS_HOST_WRITE_BIT;hostBarrier.dstAccessMask=VK_ACCESS_SHADER_READ_BIT|((a.anchoredSurfaceActive||productionBillboardCompute)?(VK_ACCESS_INDIRECT_COMMAND_READ_BIT|VK_ACCESS_INDEX_READ_BIT):0);VkPipelineStageFlags readers=VK_PIPELINE_STAGE_VERTEX_SHADER_BIT|VK_PIPELINE_STAGE_FRAGMENT_SHADER_BIT|((gpuPlanetary||productionBillboardCompute)?VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT:0)|((a.anchoredSurfaceActive||productionBillboardCompute)?(VK_PIPELINE_STAGE_TESSELLATION_CONTROL_SHADER_BIT|VK_PIPELINE_STAGE_TESSELLATION_EVALUATION_SHADER_BIT|VK_PIPELINE_STAGE_DRAW_INDIRECT_BIT|VK_PIPELINE_STAGE_VERTEX_INPUT_BIT):0);vkCmdPipelineBarrier(c,VK_PIPELINE_STAGE_HOST_BIT,readers,0,1,&hostBarrier,0,nullptr,0,nullptr);}
  if(a.naturalGlobalPreparationPending||a.naturalAnchoredPreparationPending){vkCmdBindDescriptorSets(c,VK_PIPELINE_BIND_POINT_COMPUTE,a.pipelineLayout,0,1,&a.descriptor,0,nullptr);if(a.naturalGlobalPreparationPending){vkCmdBindPipeline(c,VK_PIPELINE_BIND_POINT_COMPUTE,a.naturalGlobalPreparePipeline);vkCmdDispatch(c,(NaturalGlobalPatchCount*NaturalGlobalVerticesPerPatch+63u)/64u,1,1);a.naturalGlobalPreparationPending=false;a.naturalGlobalPrepared=true;a.naturalGlobalPreparationDispatches++;}if(a.naturalAnchoredPreparationPending){vkCmdBindPipeline(c,VK_PIPELINE_BIND_POINT_COMPUTE,a.naturalAnchoredPreparePipeline);vkCmdDispatch(c,(a.naturalAnchoredPreparationPatchCount*NaturalAnchoredVerticesPerPatch+63u)/64u,1,1);a.naturalAnchoredPreparationPending=false;a.naturalAnchoredSubmittedGeneration=a.naturalAnchoredPreparationGeneration;a.naturalAnchoredPreparationDispatches++;}VkMemoryBarrier naturalBarrier{VK_STRUCTURE_TYPE_MEMORY_BARRIER};naturalBarrier.srcAccessMask=VK_ACCESS_SHADER_WRITE_BIT;naturalBarrier.dstAccessMask=VK_ACCESS_SHADER_READ_BIT;vkCmdPipelineBarrier(c,VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,VK_PIPELINE_STAGE_VERTEX_SHADER_BIT|VK_PIPELINE_STAGE_TESSELLATION_EVALUATION_SHADER_BIT,0,1,&naturalBarrier,0,nullptr,0,nullptr);}
  RecordProductionBillboardWork(a,c,false);RecordProductionBillboardWork(a,c,true);
  if(gpuPlanetary){vkCmdBindDescriptorSets(c,VK_PIPELINE_BIND_POINT_COMPUTE,a.pipelineLayout,0,1,&a.descriptor,0,nullptr);vkCmdBindPipeline(c,VK_PIPELINE_BIND_POINT_COMPUTE,a.planetaryComputePipeline);vkCmdDispatch(c,1,1,1);VkMemoryBarrier selectionBarrier{VK_STRUCTURE_TYPE_MEMORY_BARRIER};selectionBarrier.srcAccessMask=VK_ACCESS_SHADER_WRITE_BIT;selectionBarrier.dstAccessMask=VK_ACCESS_SHADER_READ_BIT|VK_ACCESS_SHADER_WRITE_BIT|VK_ACCESS_INDIRECT_COMMAND_READ_BIT;vkCmdPipelineBarrier(c,VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT|VK_PIPELINE_STAGE_DRAW_INDIRECT_BIT,0,1,&selectionBarrier,0,nullptr,0,nullptr);vkCmdBindPipeline(c,VK_PIPELINE_BIND_POINT_COMPUTE,production?a.productionPlanetaryTerrainPipeline:a.planetaryTerrainPipeline);vkCmdDispatchIndirect(c,a.gpuControlBuffer,offsetof(GpuPlanetaryControl,terrainDispatch));VkMemoryBarrier computeBarrier{VK_STRUCTURE_TYPE_MEMORY_BARRIER};computeBarrier.srcAccessMask=VK_ACCESS_SHADER_WRITE_BIT;computeBarrier.dstAccessMask=VK_ACCESS_INDIRECT_COMMAND_READ_BIT|VK_ACCESS_SHADER_READ_BIT;VkPipelineStageFlags consumers=VK_PIPELINE_STAGE_DRAW_INDIRECT_BIT|VK_PIPELINE_STAGE_VERTEX_SHADER_BIT;if(a.submission->planetaryMode==NC_PLANETARY_CPU_GPU_VALIDATION){computeBarrier.dstAccessMask|=VK_ACCESS_HOST_READ_BIT;consumers|=VK_PIPELINE_STAGE_HOST_BIT;}vkCmdPipelineBarrier(c,VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,consumers,0,1,&computeBarrier,0,nullptr,0,nullptr);}
  vkCmdWriteTimestamp(c,VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,a.timestampQueries,1);
  vkCmdWriteTimestamp(c,VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,a.timestampQueries,2);
  NcSolarLighting lighting=a.submission->solarLighting;if(!lighting.enabled){lighting.exposure=1;lighting.ambientFloor=.025f;lighting.photosphereR=1;lighting.photosphereG=.91f;lighting.photosphereB=.68f;lighting.sourceRadiance=32;}lighting.speedHud|=a.surfaceDiagnostic<<16;
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
  if(distantCount){VkDeviceSize offset=0;vkCmdBindDescriptorSets(c,VK_PIPELINE_BIND_POINT_GRAPHICS,a.pipelineLayout,0,1,&a.descriptor,0,nullptr);const uint32_t firstUnfocused=handoff?1u:0u;if(distantCount>firstUnfocused){vkCmdBindVertexBuffers(c,0,1,&a.distantPlanetary.vb,&offset);vkCmdBindIndexBuffer(c,a.distantPlanetary.ib,0,VK_INDEX_TYPE_UINT32);vkCmdBindPipeline(c,VK_PIPELINE_BIND_POINT_GRAPHICS,a.distantPlanetaryPipeline);vkCmdDrawIndexed(c,a.distantPlanetary.indices,distantCount-firstUnfocused,0,0,firstUnfocused);}if(handoff&&distantPresentation){vkCmdBindVertexBuffers(c,0,1,&a.distantPlanetary.vb,&offset);vkCmdBindIndexBuffer(c,a.distantPlanetary.ib,0,VK_INDEX_TYPE_UINT32);vkCmdBindPipeline(c,VK_PIPELINE_BIND_POINT_GRAPHICS,!detailedPresentation?a.distantPlanetaryPipeline:a.distantPlanetaryHandoffPipeline);vkCmdDrawIndexed(c,a.distantPlanetary.indices,1,0,0,0);}}
  // Actual child raster coverage is the pixel-ownership authority.  The
  // hierarchy writes stencil one first; terrain-v5 then fills only stencil
  // zero. This preserves a complete parent without analytic/raster boundary
  // disagreement, redundant visible overlap, depth bias, or skirts.
  vkCmdWriteTimestamp(c,VK_PIPELINE_STAGE_VERTEX_INPUT_BIT,a.timestampQueries,5);
  a.anchoredPipelineStatisticsFrameSubmitted=candidate||diagnosticAnchored&&a.anchoredSurfaceActive;
  if(a.anchoredPipelineStatisticsFrameSubmitted){vkCmdBeginQuery(c,a.anchoredPipelineStatistics,0,0);if(candidate){vkCmdBindPipeline(c,VK_PIPELINE_BIND_POINT_GRAPHICS,a.productionBillboardPipeline);vkCmdBindDescriptorSets(c,VK_PIPELINE_BIND_POINT_GRAPHICS,a.pipelineLayout,0,1,&a.descriptor,0,nullptr);vkCmdBindIndexBuffer(c,a.productionBillboardCompactedBuffer,0,VK_INDEX_TYPE_UINT32);vkCmdDrawIndexedIndirect(c,a.productionBillboardIndirectBuffer,0,1,sizeof(VkDrawIndexedIndirectCommand));}else{VkDeviceSize offset=0;vkCmdBindPipeline(c,VK_PIPELINE_BIND_POINT_GRAPHICS,a.anchoredTerrainPipeline);vkCmdBindVertexBuffers(c,0,1,&a.anchoredSurfaceVertexBuffer,&offset);vkCmdBindIndexBuffer(c,a.anchoredSurfaceIndexBuffer,0,VK_INDEX_TYPE_UINT32);vkCmdDrawIndexedIndirect(c,a.anchoredSurfaceIndirectBuffers[a.anchoredSurfaceResourceIndex],0,a.anchoredSurfaceActivePatchCount,sizeof(VkDrawIndexedIndirectCommand));}vkCmdEndQuery(c,a.anchoredPipelineStatistics,0);}
  vkCmdWriteTimestamp(c,VK_PIPELINE_STAGE_FRAGMENT_SHADER_BIT,a.timestampQueries,9);
  if(!candidate&&diagnosticGlobal&&regional&&(a.submission->planetaryPatchCount||gpuPlanetary)){VkDeviceSize offset=0;const bool exactRasterFill=production&&diagnosticAnchored&&a.anchoredSurfaceActive;vkCmdBindPipeline(c,VK_PIPELINE_BIND_POINT_GRAPHICS,production?(exactRasterFill?a.productionPlanetaryFillPipeline:a.productionPlanetaryPipeline):a.planetaryPipeline);vkCmdBindVertexBuffers(c,0,1,&a.planetaryPatch.vb,&offset);vkCmdBindIndexBuffer(c,a.planetaryPatch.ib,0,VK_INDEX_TYPE_UINT32);if(gpuPlanetary)vkCmdDrawIndexedIndirect(c,a.gpuControlBuffer,0,1,sizeof(VkDrawIndexedIndirectCommand));else vkCmdDrawIndexed(c,a.planetaryPatch.indices,a.submission->planetaryPatchCount,0,0,0);}
  vkCmdWriteTimestamp(c,VK_PIPELINE_STAGE_FRAGMENT_SHADER_BIT,a.timestampQueries,10);
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
void InspectGpuPlanetary(App &a) {
  if(!a.gpuFrameSubmitted||!a.gpuControlMapped)return;GpuPlanetaryControl telemetry;std::memcpy(&telemetry,a.gpuControlMapped,sizeof(telemetry));
  if(!a.hasGpuTelemetry||std::memcmp(&telemetry,&a.lastGpuTelemetry,sizeof(telemetry))!=0){char message[544];std::snprintf(message,sizeof message,"GPU planetary: roots=%u; candidates=%u; refined=%u; culled=%u; frustum=%u; horizon=%u; active=%u; splits=%u; merges=%u; balanced=%u; parentFallbacks=%u; pendingChildren=%u; min=%u; max=%u; overflow=%u; indirectInstances=%u; terrainHits=%u; terrainMisses=%u; terrainGenerated=%u; terrainEvictions=%u; terrainResident=%u/%u; residentBeforeDemand=%u",telemetry.roots,telemetry.candidates,telemetry.refined,telemetry.culled,telemetry.frustumCulled,telemetry.horizonCulled,telemetry.active,telemetry.splits,telemetry.merges,telemetry.balanced,telemetry.parentFallbacks,telemetry.pendingChildren,telemetry.minimumLevel,telemetry.maximumLevel,telemetry.overflow,telemetry.draw.instanceCount,telemetry.cacheHits,telemetry.cacheMisses,telemetry.cacheGenerated,telemetry.cacheEvictions,telemetry.cacheResident,telemetry.cacheCapacity,telemetry.padding[1]);a.Log(NC_LOG_ALWAYS,message);if(a.productionIo&&a.submission->planetarySurfaceMode==NC_PLANETARY_SURFACE_PRODUCTION_CUBE){std::lock_guard lock(a.productionIo->mutex);uint32_t ready=0,failed=0;for(const auto &value:a.productionIo->ready){ready+=value.state==2u;failed+=value.state==3u;}char residency[384];std::snprintf(residency,sizeof residency,"Production residency: requests=%llu; diskLoads=%llu; ready=%u; failed=%u; queued=%u; pendingUploads=%u; uploads=%llu; uploadBytes=%llu; queueDrops=%llu; digestFailures=%llu",(unsigned long long)a.productionRequests,(unsigned long long)a.productionIo->diskLoads,ready,failed,a.productionIo->requestCount,a.productionPendingUploads,(unsigned long long)a.productionUploads,(unsigned long long)a.productionUploadBytes,(unsigned long long)(a.productionQueueDrops+a.productionIo->queueDrops),(unsigned long long)a.productionIo->digestFailures);a.Log(NC_LOG_ALWAYS,residency);}a.lastGpuTelemetry=telemetry;a.hasGpuTelemetry=true;}
  if(!a.productionGeometryTraceLogged&&telemetry.active&&telemetry.cacheHits==telemetry.active&&a.submission->planetarySurfaceMode==NC_PLANETARY_SURFACE_PRODUCTION_CUBE&&a.patchMapped&&a.terrainPatchSlotMapped&&a.terrainSampleMapped){
    const auto *patches=static_cast<const NcPlanetaryPatch*>(a.patchMapped);const auto *slots=static_cast<const uint32_t*>(a.terrainPatchSlotMapped);const auto *terrain=static_cast<const float*>(a.terrainSampleMapped);const auto &gpu=a.submission->planetaryGpu;const auto &presentation=a.submission->planetaryPresentation;const auto &matrix=a.submission->camera.viewProjection.columns;
    const double radius=double(gpu.radiusHigh)+gpu.radiusLow,camera[3]={double(gpu.cameraBodyHighX)+gpu.cameraBodyLowX,double(gpu.cameraBodyHighY)+gpu.cameraBodyLowY,double(gpu.cameraBodyHighZ)+gpu.cameraBodyLowZ};
    auto cube=[](uint32_t face,double x,double y,std::array<double,3>&value){if(face==0)value={1,y,-x};else if(face==1)value={-1,y,x};else if(face==2)value={x,1,-y};else if(face==3)value={x,-1,y};else if(face==4)value={x,y,1};else value={-x,y,-1};};
    auto spherify=[](std::array<double,3> value){const double maximum=std::max({std::abs(value[0]),std::abs(value[1]),std::abs(value[2])});for(double &component:value)component/=maximum;const double x2=value[0]*value[0],y2=value[1]*value[1],z2=value[2]*value[2];return std::array<double,3>{value[0]*std::sqrt(std::max(0.0,1.0-.5*(y2+z2)+y2*z2/3.0)),value[1]*std::sqrt(std::max(0.0,1.0-.5*(z2+x2)+z2*x2/3.0)),value[2]*std::sqrt(std::max(0.0,1.0-.5*(x2+y2)+x2*y2/3.0))};};
    auto rotate=[&](std::array<double,3> point){const std::array<double,3> q{presentation.bodyOrientationX,presentation.bodyOrientationY,presentation.bodyOrientationZ};const double w=presentation.bodyOrientationW;auto cross=[](const std::array<double,3>&left,const std::array<double,3>&right){return std::array<double,3>{left[1]*right[2]-left[2]*right[1],left[2]*right[0]-left[0]*right[2],left[0]*right[1]-left[1]*right[0]};};const auto qxp=cross(q,point);const std::array<double,3> inner{qxp[0]+w*point[0],qxp[1]+w*point[1],qxp[2]+w*point[2]};const auto qxi=cross(q,inner);return std::array<double,3>{point[0]+2*qxi[0],point[1]+2*qxi[1],point[2]+2*qxi[2]};};
    const uint32_t count=std::min<uint32_t>(telemetry.active,6u);for(uint32_t index=0;index<count;index++){const auto &patch=patches[index];const uint32_t terrainSlot=slots[index*2],layer=slots[index*2+1];const double cells=double(1u<<patch.level),u=(double(patch.x)+.5)/cells,v=(double(patch.y)+.5)/cells;std::array<double,3> raw{},direction{};cube(patch.face,2*u-1,2*v-1,raw);direction=spherify(raw);const uint32_t sample=(terrainSlot*TerrainGridVertexCount+8u*17u+8u)*2u;const double height=terrain[sample];std::array<double,3> relative{direction[0]*(radius+height)-camera[0],direction[1]*(radius+height)-camera[1],direction[2]*(radius+height)-camera[2]};const auto position=rotate(relative);double clip[4]{};for(uint32_t row=0;row<4;row++)clip[row]=double(matrix[row])*position[0]+double(matrix[4+row])*position[1]+double(matrix[8+row])*position[2]+double(matrix[12+row]);char trace[512];std::snprintf(trace,sizeof trace,"Production shader input: patch=%u face=%u level=%u x=%u y=%u terrainSlot=%u layer=%u height=%.6f bodyDirection=(%.9f,%.9f,%.9f) clip=(%.9g,%.9g,%.9g,%.9g) ndc=(%.9g,%.9g,%.9g)",index,patch.face,patch.level,patch.x,patch.y,terrainSlot,layer,height,direction[0],direction[1],direction[2],clip[0],clip[1],clip[2],clip[3],clip[3]!=0?clip[0]/clip[3]:0,clip[3]!=0?clip[1]/clip[3]:0,clip[3]!=0?clip[2]/clip[3]:0);a.Log(NC_LOG_ALWAYS,trace);}
    a.productionGeometryTraceLogged=true;
  }
}
void InspectGpuTimings(App &a){
  if(!a.timestampFrameSubmitted||!a.timestampQueries)return;std::array<uint64_t,App::TimestampCount> ticks{};const auto result=vkGetQueryPoolResults(a.device,a.timestampQueries,0,App::TimestampCount,sizeof(ticks),ticks.data(),sizeof(uint64_t),VK_QUERY_RESULT_64_BIT);if(result!=VK_SUCCESS)return;
  std::array<double,App::TimestampCount> values{};const double scale=double(a.timestampPeriodNanoseconds)/1e6;const bool detailedOwner=a.anchoredSurfaceActive||a.productionBillboardAuthoritative;values[0]=(ticks[8]-ticks[0])*scale;values[1]=0;values[2]=detailedOwner?(ticks[6]-ticks[5])*scale:0;values[3]=(ticks[3]-ticks[2])*scale;values[4]=(ticks[4]-ticks[3])*scale;values[5]=(ticks[7]-ticks[0])*scale;values[6]=(ticks[8]-ticks[7])*scale;values[7]=(ticks[1]-ticks[0])*scale;values[8]=(ticks[7]-ticks[4])*scale;values[9]=detailedOwner?(ticks[9]-ticks[5])*scale:0;values[10]=(ticks[10]-ticks[9])*scale;for(uint32_t i=0;i<App::TimestampCount;i++)a.timestampAccumulatedMs[i]+=values[i];a.timestampSampleCount++;if(a.canonicalBenchmark&&a.frame>120u&&a.canonicalGpuTotalMs.size()<480u){a.canonicalGpuTotalMs.push_back(values[0]);a.canonicalGpuMaterialMs.push_back(values[8]);a.canonicalGpuAnchoredMs.push_back(values[9]);a.canonicalGpuGlobalFillMs.push_back(values[10]);a.canonicalGpuOverlayMs.push_back((ticks[7]-ticks[6])*scale);}
  if(a.timestampSampleCount==1||a.timestampSampleCount%120==0){char message[384];std::snprintf(message,sizeof message,"GPU timings: total=%.3f ms; anchoredCompute=%.3f; anchoredDraw=%.3f; background=%.3f; preSurface=%.3f; scene=%.3f; toneMap=%.3f; regionalCompute=%.3f; materialsOverlays=%.3f",values[0],values[1],values[2],values[3],values[4],values[5],values[6],values[7],values[8]);a.Log(NC_LOG_ALWAYS,message);if(a.localIo){std::lock_guard lock(a.localIo->mutex);uint32_t resident=0,visible=0,inFlight=0,ready=0,failed=0,published=0;for(uint32_t slot=0;slot<LocalPayloadSlots;slot++){resident+=a.localLayerOccupied[slot]&&!a.localLayerInFlight[slot];visible+=a.localLayerVisible[slot];inFlight+=a.localLayerInFlight[slot];published+=a.localLayerPublished[slot];}for(const auto&value:a.localIo->ready){ready+=value.state==2u;failed+=value.state==3u;}const uint64_t samples=a.localHits+a.localMisses;const double hitRate=samples?100.0*double(a.localHits)/double(samples):0.0;const uint64_t vram=uint64_t(LocalPayloadSlots)*(LocalAlbedoLayerBytes+LocalElevationLayerBytes+LocalNormalLayerBytes+LocalControlLayerBytes);const double uploadLatency=a.localUploads?a.localUploadLatencyMilliseconds/double(a.localUploads):0.0;char local[928];std::snprintf(local,sizeof local,"Regional terrain streaming: requested=%llu; hits=%llu; misses=%llu; hitRate=%.2f%%; resident=%u/%u; visible=%u/%u; published=%u; promotions=%llu; inFlight=%u; evictions=%llu; queued=%u; ready=%u; failed=%u; canceled=%llu; queueDrops=%llu; bytesRead=%llu; bytesSupercompressed=%llu; bytesTranscoded=%llu; bytesUploaded=%llu; transcodeMs=%.3f; uploadLatencyAvgMs=%.3f; uploads=%llu; uploadBudget=%u; selectedFrequency=%u; fallbackFrequency=%u; BC7VRAM=%llu; R16VRAM=%llu; BC5VRAM=%llu; R8VRAM=%llu; totalVRAM=%llu",(unsigned long long)a.localRequests,(unsigned long long)a.localHits,(unsigned long long)a.localMisses,hitRate,resident,LocalPayloadSlots,visible,a.localVisibleTargetCount,published,(unsigned long long)a.localPromotions,inFlight,(unsigned long long)a.localEvictions,a.localIo->requestCount,ready,failed,(unsigned long long)a.localCanceled,(unsigned long long)a.localIo->queueDrops,(unsigned long long)a.localIo->bytesRead,(unsigned long long)a.localIo->bytesRead,(unsigned long long)a.localIo->bytesTranscoded,(unsigned long long)a.localUploadBytes,a.localIo->transcodeMilliseconds,uploadLatency,(unsigned long long)a.localUploads,LocalUploadBudget,visible?1u:0u,0u,(unsigned long long)(uint64_t(LocalPayloadSlots)*LocalAlbedoLayerBytes),(unsigned long long)(uint64_t(LocalPayloadSlots)*LocalElevationLayerBytes),(unsigned long long)(uint64_t(LocalPayloadSlots)*LocalNormalLayerBytes),(unsigned long long)(uint64_t(LocalPayloadSlots)*LocalControlLayerBytes),(unsigned long long)vram);a.Log(NC_LOG_ALWAYS,local);}}
}
void InspectAnchoredPipelineStatistics(App &a){
  if(!a.anchoredPipelineStatisticsFrameSubmitted||!a.anchoredPipelineStatistics)return;
  // Results are returned in ascending VkQueryPipelineStatisticFlagBits order:
  // clipping primitives, TCS patches, then TES invocations.
  std::array<uint64_t,3> values{};
  const auto result=vkGetQueryPoolResults(a.device,a.anchoredPipelineStatistics,0,1,
    sizeof values,values.data(),sizeof values,VK_QUERY_RESULT_64_BIT);
  if(result!=VK_SUCCESS)return;
  a.anchoredClippingPrimitives+=values[0];
  a.anchoredTessellationControlPatches+=values[1];
  a.anchoredTessellationEvaluationInvocations+=values[2];
  a.anchoredPipelineStatisticsSamples++;
  if(a.anchoredPipelineStatisticsSamples==1||a.anchoredPipelineStatisticsSamples%120==0){
    char message[320];std::snprintf(message,sizeof message,
      "GPU anchored refinement: tcsPatches=%llu; refinedVertices=%llu; rasterPrimitives=%llu; CPUFinalRaster=false",
      (unsigned long long)values[1],(unsigned long long)values[2],(unsigned long long)values[0]);
    a.Log(NC_LOG_ALWAYS,message);
  }
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
  a.recordSerial++;
  const bool detailedPresentation = !a.submission->planetaryPresentation.enabled ||
      a.submission->planetaryPresentation.regime != NC_PLANETARY_DISTANT_ONLY;
  const bool regionalPresentation = detailedPresentation;
  const bool gpuFrameSubmitted = regionalPresentation &&
      a.submission->planetaryMode != NC_PLANETARY_CPU_REFERENCE;
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
  const auto submitStart=std::chrono::steady_clock::now();a.Check(vkQueueSubmit(a.graphicsQueue, 1, &si, a.fence), "submit failed");a.submitSerial++;if(a.productionBillboardIncomingEnabled&&a.productionBillboardIncomingWorkRecorded)a.productionBillboardIncomingFencePending=true;const auto submitEnd=std::chrono::steady_clock::now();
  VkPresentInfoKHR pi{VK_STRUCTURE_TYPE_PRESENT_INFO_KHR};
  pi.waitSemaphoreCount = 1;
  pi.pWaitSemaphores = &a.renderFinished[image];
  pi.swapchainCount = 1;
  pi.pSwapchains = &a.swapchain;
  pi.pImageIndices = &image;
  const auto presentStart=std::chrono::steady_clock::now();VkResult pr = vkQueuePresentKHR(a.presentQueue, &pi);a.presentSerial++;const auto presentEnd=std::chrono::steady_clock::now();
  if(a.earthSubmissionTraceRemaining&&a.submission->planetarySurfaceMode==NC_PLANETARY_SURFACE_PRODUCTION_CUBE){const bool candidate=a.productionBillboardAuthoritative;char trace[448];std::snprintf(trace,sizeof trace,"Earth Vulkan submission: terrainFrame=%llu; swapchainImage=%u; recordSerial=%llu; submitSerial=%llu; presentSerial=%llu; serializedFence=true; globalDraw=%u; dynamicHierarchyDraw=%u; candidateIndirectDraw=%u; visibleEarthOwners=1",(unsigned long long)a.frame,image,(unsigned long long)a.recordSerial,(unsigned long long)a.submitSerial,(unsigned long long)a.presentSerial,candidate?0u:1u,!candidate&&a.anchoredSurfaceActive?1u:0u,candidate?1u:0u);a.Log(NC_LOG_ALWAYS,trace);a.earthSubmissionTraceRemaining--;}
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
    if(a.anchoredPipelineStatistics)vkDestroyQueryPool(a.device,a.anchoredPipelineStatistics,nullptr);
    for (auto s : a.renderFinished)
      vkDestroySemaphore(a.device, s, nullptr);
    if (a.imageAvailable)
      vkDestroySemaphore(a.device, a.imageAvailable, nullptr);
    DestroyMesh(a);
    DestroySubmission(a);
    DestroyLocalTerrain(a);
    DestroyProductionCubeSurface(a);
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
void InspectProductionBillboardPublication(App &a){
  if(!a.productionBillboardIncomingFencePending)return;const auto *draw=static_cast<const VkDrawIndexedIndirectCommand*>(a.productionBillboardIncomingIndirectMapped);const auto *counters=static_cast<const uint32_t*>(a.productionBillboardIncomingCounterMapped);const bool topologyReady=a.productionBillboardIncomingLatticeMapped&&a.productionBillboardIncomingIndexMapped;const bool physicalReady=a.productionBillboardIncomingPhysicalMapped&&a.productionBillboardIncomingVertexCount==counters[6];const bool normalReady=physicalReady&&counters[3]==0;const bool cullReady=counters[0]+counters[1]+counters[3]==a.productionBillboardIncomingTriangleCount;const bool compactReady=draw&&draw->indexCount==counters[4]&&draw->indexCount%3u==0;const bool drawReady=draw&&draw->instanceCount==1u&&draw->indexCount>0&&draw->indexCount<=a.productionBillboardIncomingTriangleCount*3u;const bool valid=topologyReady&&physicalReady&&normalReady&&cullReady&&compactReady&&drawReady&&counters[2]==0;
  if(!valid)throw std::runtime_error("production billboard incoming generation failed complete readiness validation");const uint32_t visible=counters[0],compacted=draw->indexCount;const bool topologyReused=!a.productionBillboardIncomingOwnsTopology;
  DestroyHostBuffer(a,a.productionBillboardPhysicalBuffer,a.productionBillboardPhysicalMemory,a.productionBillboardPhysicalMapped);DestroyHostBuffer(a,a.productionBillboardVisibilityBuffer,a.productionBillboardVisibilityMemory,a.productionBillboardVisibilityMapped);DestroyHostBuffer(a,a.productionBillboardCompactedBuffer,a.productionBillboardCompactedMemory,a.productionBillboardCompactedMapped);DestroyHostBuffer(a,a.productionBillboardIndirectBuffer,a.productionBillboardIndirectMemory,a.productionBillboardIndirectMapped);DestroyHostBuffer(a,a.productionBillboardCounterBuffer,a.productionBillboardCounterMemory,a.productionBillboardCounterMapped);
  if(a.productionBillboardIncomingOwnsTopology){DestroyHostBuffer(a,a.productionBillboardLatticeBuffer,a.productionBillboardLatticeMemory,a.productionBillboardLatticeMapped);DestroyHostBuffer(a,a.productionBillboardIndexBuffer,a.productionBillboardIndexMemory,a.productionBillboardIndexMapped);a.productionBillboardLatticeBuffer=a.productionBillboardIncomingLatticeBuffer;a.productionBillboardLatticeMemory=a.productionBillboardIncomingLatticeMemory;a.productionBillboardLatticeMapped=a.productionBillboardIncomingLatticeMapped;a.productionBillboardIndexBuffer=a.productionBillboardIncomingIndexBuffer;a.productionBillboardIndexMemory=a.productionBillboardIncomingIndexMemory;a.productionBillboardIndexMapped=a.productionBillboardIncomingIndexMapped;}
  a.productionBillboardPhysicalBuffer=a.productionBillboardIncomingPhysicalBuffer;a.productionBillboardPhysicalMemory=a.productionBillboardIncomingPhysicalMemory;a.productionBillboardPhysicalMapped=a.productionBillboardIncomingPhysicalMapped;a.productionBillboardVisibilityBuffer=a.productionBillboardIncomingVisibilityBuffer;a.productionBillboardVisibilityMemory=a.productionBillboardIncomingVisibilityMemory;a.productionBillboardVisibilityMapped=a.productionBillboardIncomingVisibilityMapped;a.productionBillboardCompactedBuffer=a.productionBillboardIncomingCompactedBuffer;a.productionBillboardCompactedMemory=a.productionBillboardIncomingCompactedMemory;a.productionBillboardCompactedMapped=a.productionBillboardIncomingCompactedMapped;a.productionBillboardIndirectBuffer=a.productionBillboardIncomingIndirectBuffer;a.productionBillboardIndirectMemory=a.productionBillboardIncomingIndirectMemory;a.productionBillboardIndirectMapped=a.productionBillboardIncomingIndirectMapped;a.productionBillboardCounterBuffer=a.productionBillboardIncomingCounterBuffer;a.productionBillboardCounterMemory=a.productionBillboardIncomingCounterMemory;a.productionBillboardCounterMapped=a.productionBillboardIncomingCounterMapped;a.productionBillboardVertexCount=a.productionBillboardIncomingVertexCount;a.productionBillboardTriangleCount=a.productionBillboardIncomingTriangleCount;a.productionBillboardTopologyHash=a.productionBillboardIncomingTopologyHash;a.productionBillboardGeneration=a.productionBillboardIncomingGeneration;
  a.productionBillboardIncomingLatticeBuffer={};a.productionBillboardIncomingLatticeMemory={};a.productionBillboardIncomingLatticeMapped=nullptr;a.productionBillboardIncomingIndexBuffer={};a.productionBillboardIncomingIndexMemory={};a.productionBillboardIncomingIndexMapped=nullptr;a.productionBillboardIncomingPhysicalBuffer={};a.productionBillboardIncomingPhysicalMemory={};a.productionBillboardIncomingPhysicalMapped=nullptr;a.productionBillboardIncomingVisibilityBuffer={};a.productionBillboardIncomingVisibilityMemory={};a.productionBillboardIncomingVisibilityMapped=nullptr;a.productionBillboardIncomingCompactedBuffer={};a.productionBillboardIncomingCompactedMemory={};a.productionBillboardIncomingCompactedMapped=nullptr;a.productionBillboardIncomingIndirectBuffer={};a.productionBillboardIncomingIndirectMemory={};a.productionBillboardIncomingIndirectMapped=nullptr;a.productionBillboardIncomingCounterBuffer={};a.productionBillboardIncomingCounterMemory={};a.productionBillboardIncomingCounterMapped=nullptr;a.productionBillboardIncomingEnabled=false;a.productionBillboardIncomingWorkRecorded=false;a.productionBillboardIncomingFencePending=false;a.productionBillboardIncomingOwnsTopology=false;a.productionBillboardIncomingVertexCount=0;a.productionBillboardIncomingTriangleCount=0;a.productionBillboardIncomingTopologyHash=0;a.productionBillboardIncomingGeneration=0;
  a.productionBillboardFencePending=false;a.productionBillboardAuthoritative=true;a.productionBillboardWorkRecorded=false;a.anchoredSurfaceActive=false;a.anchoredSurfaceActivePatchCount=0;a.productionBillboardPublications++;a.productionBillboardDeferredRetirements+=a.productionBillboardPublications>1?1u:0u;a.submission->productionBillboardPadding=static_cast<uint32_t>(a.productionBillboardGeneration);UpdateProductionBillboardDescriptors(a,false);char message[768];std::snprintf(message,sizeof message,"Production spherical billboard publication: generation=%llu; hash=0x%016llX; topologyResident=true; topologyReused=%u; topologyUploads=%llu; physicalReady=true; normalsReady=true; cullReady=true; compactReady=true; tesDrawReady=true; indirectValid=true; fenceComplete=true; inputTriangles=%u; visibleTriangles=%u; compactedIndices=%u; indirectDraws=1; invalidDraws=0; zeroOwner=0; overlapOwner=0; staleGenerationDraws=0; atomicFrameBoundary=true; publications=%llu; deferredRetirements=%llu",(unsigned long long)a.productionBillboardGeneration,(unsigned long long)a.productionBillboardTopologyHash,topologyReused?1u:0u,(unsigned long long)a.productionBillboardTopologyUploads,a.productionBillboardTriangleCount,visible,compacted,(unsigned long long)a.productionBillboardPublications,(unsigned long long)a.productionBillboardDeferredRetirements);a.Log(NC_LOG_ALWAYS,message);
}
void Update(App &a, float dt) {
  const auto updateStart=std::chrono::steady_clock::now();
  a.Check(vkWaitForFences(a.device, 1, &a.fence, VK_TRUE, UINT64_MAX), "frame fence wait failed");
  const auto fenceEnd=std::chrono::steady_clock::now();
  InspectProductionBillboardPublication(a);
  InspectGpuTimings(a);
  InspectAnchoredPipelineStatistics(a);
  InspectGpuPlanetary(a);
  const auto inspectionEnd=std::chrono::steady_clock::now();
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
                  rising(VK_OEM_PERIOD, a.rateIncreaseWasDown), sasModeKey,
                  (GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0,
                  (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0,
                  static_cast<NcPresentationFocus>(presentationFocus),
                  a.extent.width,
                  a.extent.height};
  NcHostEvent e{NC_UPDATE_FRAME, NC_LOG_NONE, nullptr, in, a.submission};
  a.cb(&e, a.cbData);
  const auto callbackEnd=std::chrono::steady_clock::now();
  CreateProductionBillboard(a);
  if(a.submission->planetaryMode==NC_PLANETARY_CPU_REFERENCE)EnsurePatchCapacity(a,a.submission->planetaryPatchCount);
  Validate(a);
  Upload(a);
  PrepareProductionUploads(a);
  PrepareLocalUploads(a);
  const auto updateEnd=std::chrono::steady_clock::now();
  a.cpuUpdateMs+=std::chrono::duration<double,std::milli>(updateEnd-updateStart).count();
  const double fenceWaitMs=std::chrono::duration<double,std::milli>(fenceEnd-updateStart).count();
  a.cpuFenceWaitMs+=fenceWaitMs;
  a.fenceTimesMs[a.fenceTimeCursor]=fenceWaitMs;a.fenceTimeCursor=(a.fenceTimeCursor+1)%a.fenceTimesMs.size();a.fenceTimeCount=std::min(a.fenceTimeCount+1,a.fenceTimesMs.size());
  a.cpuInspectionMs+=std::chrono::duration<double,std::milli>(inspectionEnd-fenceEnd).count();
  a.cpuHostCallbackMs+=std::chrono::duration<double,std::milli>(callbackEnd-inspectionEnd).count();
  a.cpuUploadMs+=std::chrono::duration<double,std::milli>(updateEnd-callbackEnd).count();
  if(a.canonicalBenchmark&&a.frame>=120u&&a.canonicalCpuUpdateMs.size()<480u){a.canonicalCpuUpdateMs.push_back(std::chrono::duration<double,std::milli>(updateEnd-updateStart).count());a.canonicalCpuFenceMs.push_back(std::chrono::duration<double,std::milli>(fenceEnd-updateStart).count());}
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
        (uint32_t)offsetof(NcInputState, fastModifier),
        (uint32_t)offsetof(NcInputState, slowModifier),
        (uint32_t)offsetof(NcFrameSubmission, planetaryGpu),
        (uint32_t)offsetof(NcFrameSubmission, planetaryMode),
        (uint32_t)offsetof(NcFrameSubmission, planetaryPresentation),
        (uint32_t)offsetof(NcInputState, presentationFocus),
        (uint32_t)offsetof(NcFrameSubmission, solarLighting),
        (uint32_t)offsetof(NcInputState, viewportWidthPixels),
        (uint32_t)offsetof(NcInputState, viewportHeightPixels)};
  return NC_SUCCESS;
}
static NcResult RunRenderer(NcFrameSubmission *s, NcHostCallback cb, void *data, const NcRuntimeAssets *assets) {
  if (!cb || !s || !s->objects || !s->objectCount || !s->batches ||
      !s->batchCount || (assets && (assets->size != sizeof(NcRuntimeAssets) || assets->version != 3u || !assets->productionTerrainPathUtf8 || !assets->elevationOraclePathUtf8)))
    return NC_INVALID_ARGUMENT;
  App a;
  a.cb = cb;
  a.cbData = data;
  a.submission = s;
  a.canonicalBenchmark=std::getenv("NOVACORE_M12_CANONICAL_BENCHMARK")&&std::strcmp(std::getenv("NOVACORE_M12_CANONICAL_BENCHMARK"),"1")==0;
  if(assets){a.productionTerrainPath=assets->productionTerrainPathUtf8;if(assets->localTerrainPathUtf8)a.localTerrainPath=assets->localTerrainPathUtf8;a.elevationOraclePath=assets->elevationOraclePathUtf8;}
  try {
    gApp = &a;
    if(const char*groundTruth=std::getenv("NOVACORE_GPU_GROUND_TRUTH"))
      a.anchoredGroundTruthEnabled=std::strcmp(groundTruth,"1")==0;
    if(a.anchoredGroundTruthEnabled)a.Log(NC_LOG_ALWAYS,"GPU ground-truth instrumentation enabled");
    if(a.canonicalBenchmark)a.Log(NC_LOG_ALWAYS,"M12 canonical benchmark telemetry: warmup=120; measured=480; fixed pose required by host");
    a.surfaceDiagnostic=SurfaceDiagnosticFromEnvironment();
    if(a.surfaceDiagnostic){char message[128];std::snprintf(message,sizeof message,"Surface diagnostic isolation flags: 0x%02X",a.surfaceDiagnostic);a.Log(NC_LOG_ALWAYS,message);}
    LogLoadedRuntimePaths(a);
    Window(a);
    Instance(a);
    SetupDebug(a);
    Surface(a);
    Device(a);
    CreateMesh(a);
    CreateProductionCubeSurface(a);
    CreateLocalTerrain(a);
    Validate(a);
    Swap(a);
    CreateSubmission(a);
    Commands(a);
    BootstrapProductionHierarchy(a);
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
    char text[512];
    auto ms = std::chrono::duration<double, std::milli>(
                  std::chrono::steady_clock::now() - start)
                  .count() /
              std::max<uint64_t>(1, frames);
    std::snprintf(text, sizeof text,
                  "Average frame time: %.3f ms (%llu frames)", ms,
                  (unsigned long long)frames);
    a.Log(NC_LOG_ALWAYS, text);
    if(a.frameTimeCount){std::vector<double> sorted(a.frameTimesMs.begin(),a.frameTimesMs.begin()+a.frameTimeCount);std::sort(sorted.begin(),sorted.end());auto percentile=[&](double p){return sorted[std::min(sorted.size()-1,size_t(std::ceil(p*sorted.size()))-1)];};std::snprintf(text,sizeof text,"Frame pacing: p50=%.3f ms; p95=%.3f ms; p99=%.3f ms; max=%.3f ms; samples=%zu",percentile(.50),percentile(.95),percentile(.99),sorted.back(),sorted.size());a.Log(NC_LOG_ALWAYS,text);}
    if(a.fenceTimeCount){std::vector<double> sorted(a.fenceTimesMs.begin(),a.fenceTimesMs.begin()+a.fenceTimeCount);const double average=std::accumulate(sorted.begin(),sorted.end(),0.0)/double(sorted.size());std::sort(sorted.begin(),sorted.end());const double p95=sorted[std::min(sorted.size()-1,size_t(std::ceil(.95*sorted.size()))-1)];std::snprintf(text,sizeof text,"Fence wait pacing: average=%.3f ms; p95=%.3f ms; max=%.3f ms; samples=%zu",average,p95,sorted.back(),sorted.size());a.Log(NC_LOG_ALWAYS,text);}
    if(a.cpuTimingSamples){const double n=double(a.cpuTimingSamples);std::snprintf(text,sizeof text,"CPU timings: update=%.3f ms; fence=%.3f; inspection=%.3f; hostCallback=%.3f; validationUpload=%.3f; record=%.3f; submit=%.3f; present=%.3f",a.cpuUpdateMs/n,a.cpuFenceWaitMs/n,a.cpuInspectionMs/n,a.cpuHostCallbackMs/n,a.cpuUploadMs/n,a.cpuRecordMs/n,a.cpuSubmitMs/n,a.cpuPresentMs/n);a.Log(NC_LOG_ALWAYS,text);}
    if(a.canonicalBenchmark){auto report=[&](const char*name,std::vector<double> values){if(values.empty())return;const double average=std::accumulate(values.begin(),values.end(),0.0)/double(values.size());std::sort(values.begin(),values.end());const double p95=values[std::min(values.size()-1,size_t(std::ceil(.95*values.size()))-1)];std::snprintf(text,sizeof text,"M12 canonical timing: %s average=%.3f ms; p95=%.3f ms; samples=%zu",name,average,p95,values.size());a.Log(NC_LOG_ALWAYS,text);};report("cpuUpdate",a.canonicalCpuUpdateMs);report("fenceWait",a.canonicalCpuFenceMs);report("gpuTotal",a.canonicalGpuTotalMs);report("materialsOverlays",a.canonicalGpuMaterialMs);report("anchoredTerrain",a.canonicalGpuAnchoredMs);report("globalFallback",a.canonicalGpuGlobalFillMs);report("overlays",a.canonicalGpuOverlayMs);}
    const uint64_t naturalGlobalBytes=32ull+uint64_t(NaturalGlobalPatchCount)*NaturalGlobalVerticesPerPatch*sizeof(double)*4ull;
    const uint64_t naturalAnchoredBytes=uint64_t(std::max(1u,a.submission?a.submission->anchoredSurfaceCacheSlotCount:0u))*NaturalAnchoredVerticesPerPatch*sizeof(double)*4ull;
    std::snprintf(text,sizeof text,"GPU terrain descriptor totals: physicalGeneration=%u; publications=%llu; bytes=%llu; reusableBaseVertices=%u; reusableTopologyTemplates=16; tessellationTargetPixels=16; tessellationMaximum=16; tessellationRangeMetres=50; basePhysicalEvaluation=preparedVertex; nearPhysicalEvaluation=TES; naturalGlobalPrepareDispatches=%llu; naturalAnchoredPrepareDispatches=%llu; naturalPreparedBytes=%llu; capacityRejects=%llu",a.submission?a.submission->physicalSurfaceGeneration:0u,static_cast<unsigned long long>(a.anchoredSurfaceUploads),static_cast<unsigned long long>(a.anchoredSurfaceUploadBytes),AnchoredSurfaceBaseVerticesPerPatch,static_cast<unsigned long long>(a.naturalGlobalPreparationDispatches),static_cast<unsigned long long>(a.naturalAnchoredPreparationDispatches),static_cast<unsigned long long>(naturalGlobalBytes+naturalAnchoredBytes),static_cast<unsigned long long>(a.anchoredSurfaceCapacityRejects));a.Log(NC_LOG_ALWAYS,text);
    if(a.anchoredPipelineStatisticsSamples){const double n=double(a.anchoredPipelineStatisticsSamples);std::snprintf(text,sizeof text,"GPU anchored refinement averages: tcsPatches=%.1f; refinedVertices=%.1f; rasterPrimitives=%.1f; samples=%llu; CPUFinalRaster=false",double(a.anchoredTessellationControlPatches)/n,double(a.anchoredTessellationEvaluationInvocations)/n,double(a.anchoredClippingPrimitives)/n,(unsigned long long)a.anchoredPipelineStatisticsSamples);a.Log(NC_LOG_ALWAYS,text);}
    if(a.timestampSampleCount){const double n=double(a.timestampSampleCount);std::snprintf(text,sizeof text,"GPU timing averages: total=%.3f ms; anchoredCompute=%.3f; anchoredDraw=%.3f; background=%.3f; preSurface=%.3f; toneMap=%.3f",a.timestampAccumulatedMs[0]/n,a.timestampAccumulatedMs[1]/n,a.timestampAccumulatedMs[2]/n,a.timestampAccumulatedMs[3]/n,a.timestampAccumulatedMs[4]/n,a.timestampAccumulatedMs[6]/n);a.Log(NC_LOG_ALWAYS,text);}
    Destroy(a);
    return NC_SUCCESS;
  } catch (const std::exception &e) {
    a.Log(NC_LOG_ALWAYS, e.what());
    Destroy(a);
    return NC_FAILURE;
  }
}
extern "C" NC_API NcResult __cdecl nc_validate_terrain_asset(const char *path, uint64_t bodyId, uint32_t terrainVersion, uint32_t expectedRecordCount) {
  if(!path||!*path||!bodyId||!terrainVersion||!expectedRecordCount)return NC_INVALID_ARGUMENT;
  std::ifstream input(path,std::ios::binary);char magic[8]{};if(!input.read(magic,sizeof magic))return NC_FAILURE;std::string error;
  if(std::memcmp(magic,"NCCUBE2\0",8)==0){nc::localterrain::Pack pack;if(!pack.Open(path,error)||pack.BodyId()!=bodyId||pack.TerrainVersion()!=terrainVersion||pack.RecordCount()!=expectedRecordCount||pack.Records().empty())return NC_FAILURE;nc::localterrain::Payload payload;return pack.Read(pack.Records().front().id,payload,error)&&payload.digestValid?NC_SUCCESS:NC_FAILURE;}
  nc::production::Pack pack;if(!pack.Open(path,error)||pack.BodyId()!=bodyId||pack.TerrainVersion()!=terrainVersion||pack.RecordCount()!=expectedRecordCount)return NC_FAILURE;nc::production::Payload payload;const nc::production::PatchId root{bodyId,terrainVersion,0,0,0,0};return pack.Read(root,payload,error)&&payload.digestValid?NC_SUCCESS:NC_FAILURE;
}

extern "C" NC_API NcResult __cdecl nc_query_planetary_physical_heights(const NcPlanetaryHeightQuery* queries,uint32_t count,NcPlanetaryHeightResult* results,const NcPlanetaryHeightQueryAssets* assets,NcPlanetaryHeightQueryMetrics* metrics){
  return RunPlanetaryHeightQueries(queries,count,results,assets,metrics);
}
extern "C" NC_API NcResult __cdecl nc_initialize_planetary_mesh_preparation(const NcPlanetaryMeshPreparationAssets* assets,NcPlanetaryMeshPreparationMetrics* metrics){return InitializePlanetaryMeshPreparation(assets,metrics);}
extern "C" NC_API NcResult __cdecl nc_prepare_planetary_mesh(const NcPlanetaryHeightQuery* vertices,const uint32_t* indices,const uint32_t* adjacencyWords,const NcPlanetaryMeshPreparationDispatch* dispatch,NcPlanetaryDisplacedVertex* displaced,NcPlanetaryPhysicalNormal* normals,NcPlanetaryMeshPreparationMetrics* metrics){return PreparePlanetaryMesh(vertices,indices,adjacencyWords,dispatch,displaced,normals,metrics);}
extern "C" NC_API NcResult __cdecl nc_shutdown_planetary_mesh_preparation(){return ShutdownPlanetaryMeshPreparation();}
extern "C" NC_API NcResult __cdecl nc_initialize_spherical_billboard_gpu_proof(const NcSphericalBillboardProofAssets* assets,NcSphericalBillboardProofMetrics* metrics){return InitializeSphericalBillboardGpuProof(assets,metrics);}
extern "C" NC_API NcResult __cdecl nc_upload_spherical_billboard_gpu_proof_topology(const NcSphericalBillboardProofTopology* topology,NcSphericalBillboardProofMetrics* metrics){return UploadSphericalBillboardGpuProofTopology(topology,metrics);}
extern "C" NC_API NcResult __cdecl nc_publish_spherical_billboard_physical_surface(const NcSphericalBillboardPhysicalSurface* surface,NcSphericalBillboardProofMetrics* metrics){return PublishSphericalBillboardPhysicalSurface(surface,metrics);}
extern "C" NC_API NcResult __cdecl nc_run_spherical_billboard_gpu_proof_frame(const NcSphericalBillboardProofFrame* frame,NcSphericalBillboardProofMetrics* metrics){return RunSphericalBillboardGpuProofFrame(frame,metrics);}
extern "C" NC_API NcResult __cdecl nc_shutdown_spherical_billboard_gpu_proof(){return ShutdownSphericalBillboardGpuProof();}
extern "C" NC_API NcResult __cdecl nc_run_renderer(NcFrameSubmission *s, NcHostCallback cb, void *data) { return RunRenderer(s,cb,data,nullptr); }
extern "C" NC_API NcResult __cdecl nc_run_renderer_with_assets(NcFrameSubmission *s, NcHostCallback cb, void *data, const NcRuntimeAssets *assets) { return RunRenderer(s,cb,data,assets); }
