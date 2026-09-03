#pragma once
#include <stdint.h>

#ifdef _WIN32
#define NC_API __declspec(dllexport)
#else
#define NC_API
#endif

extern "C" {
struct NcEncodedPosition { float high[4]; float low[4]; };
struct NcFloat4x4 { float columns[16]; };
struct alignas(16) NcCameraData { NcEncodedPosition position; NcFloat4x4 viewProjection; };
struct NcMeshHandle { uint32_t value; };
struct NcRenderTransform { float rotation[4]; float scale[4]; };
// std430-compatible: 80 bytes, 16-byte alignment; position=0, transform=32, mesh=64.
struct alignas(16) NcRenderObject { NcEncodedPosition position; NcRenderTransform transform; NcMeshHandle mesh; uint32_t padding[3]; };
struct NcDrawBatch { NcMeshHandle mesh; uint32_t firstObject; uint32_t objectCount; uint32_t padding; };
struct NcPlanetaryPatch { uint32_t face, level, x, y; float centerX, centerY, centerZ, radius; float colorR, colorG, colorB, colorA; uint32_t stitchMask, reserved0, reserved1, reserved2; };
struct NcAnchoredSurfacePatch {
  uint32_t bodyIdLow, bodyIdHigh, terrainVersion, physicalSurfaceGeneration;
  uint32_t face, level, x, y;
  uint32_t cacheSlot, cacheGeneration, stitchMask, flags;
  uint32_t materialLevel, materialX, materialY, materialGeneration;
  float boundsX, boundsY, boundsZ, boundsRadius;
};
struct alignas(16) NcAnchoredSurfacePresentation {
  NcEncodedPosition origin;
  NcEncodedPosition east;
  NcEncodedPosition north;
  NcEncodedPosition up;
  uint32_t bodyIdLow, bodyIdHigh, snapIdentity, presentationGeneration;
};
struct alignas(16) NcPlanetaryGpuConstants {
  float cameraBodyHighX, cameraBodyHighY, cameraBodyHighZ, radiusHigh;
  float cameraBodyLowX, cameraBodyLowY, cameraBodyLowZ, radiusLow;
  float refinementThreshold, nearFieldAltitudeRadii, surfaceAltitudeMetres, maximumTerrainHeightMetres;
  uint32_t maximumLevel, outputCapacity, terrainVersion, terrainFrame;
  float viewForwardX, viewForwardY, viewForwardZ, viewHalfAngleRadians;
  float viewportHeightPixels, verticalTanHalfFov, targetTexelPixels, requestedAlbedoLevel;
};
enum NcPlanetaryMode : uint32_t { NC_PLANETARY_CPU_REFERENCE = 0, NC_PLANETARY_GPU_PRODUCTION = 1, NC_PLANETARY_CPU_GPU_VALIDATION = 2 };
enum NcPlanetarySurfaceMode : uint32_t { NC_PLANETARY_SURFACE_BOUNDED = 0, NC_PLANETARY_SURFACE_PRODUCTION_CUBE = 2 };
enum NcPlanetaryRenderRegime : uint32_t { NC_PLANETARY_DISTANT_ONLY = 0, NC_PLANETARY_TRANSITION = 1, NC_PLANETARY_DETAILED_ONLY = 2 };
enum NcPresentationFocus : uint32_t { NC_PRESENTATION_FOCUS_NONE = 0, NC_PRESENTATION_FOCUS_SUN = 1, NC_PRESENTATION_FOCUS_MERCURY = 2, NC_PRESENTATION_FOCUS_VENUS = 3, NC_PRESENTATION_FOCUS_EARTH = 4, NC_PRESENTATION_FOCUS_MOON = 5, NC_PRESENTATION_FOCUS_MARS = 6, NC_PRESENTATION_FOCUS_JUPITER = 7, NC_PRESENTATION_FOCUS_SATURN = 8, NC_PRESENTATION_FOCUS_URANUS = 9, NC_PRESENTATION_FOCUS_NEPTUNE = 10 };
struct alignas(16) NcPlanetaryPresentation {
  float centerX, centerY, centerZ, radius;
  float colorR, colorG, colorB, distantAlpha;
  float detailedAlpha, distanceRadii; NcPlanetaryRenderRegime regime; uint32_t enabled;
  uint32_t bodyIdLow, bodyIdHigh, materialKind, albedoSource;
  float roughness, specular, emissive, presentationRotationRadians;
  uint32_t projectionKind, ringAssociation, atmosphereHook, cloudHook;
  float ringInnerRadiusRatio, ringOuterRadiusRatio, ringOpacity, ringBandFrequency;
  float ringOrientationX, ringOrientationY, ringOrientationZ, ringOrientationW;
  float ringColorR, ringColorG, ringColorB, ringColorA;
  float bodyOrientationX, bodyOrientationY, bodyOrientationZ, bodyOrientationW;
  float localDetailScaleMeters, localDetailMicroScaleMeters, localDetailFadeStartMetres, localDetailFadeEndMetres;
  float centerLowX, centerLowY, centerLowZ, centerLowPadding;
};
struct alignas(16) NcSolarLighting { float sourceCenterX, sourceCenterY, sourceCenterZ, exposure; float photosphereR, photosphereG, photosphereB, ambientFloor; float sourceRadiance, glowStrength; uint32_t enabled, speedHud; };
struct NcOrbitLineVertex { float positionHigh[3]; float positionLow[3]; };
struct alignas(16) NcProductionBillboardLatticeVertex { int32_t cube[3]; uint32_t metadata; };
struct NcProductionSphericalBillboardSubmission {
  uint32_t size, version, enabled, level;
  uint32_t vertexCount, indexCount, latticeScale, physicalGeneration;
  uint32_t terrainDataGeneration, pupilGeneration, reserved0, reserved1;
  uint64_t topologyHash, publicationGeneration;
  const NcProductionBillboardLatticeVertex* latticeVertices;
  const uint32_t* indices;
  const struct NcSphericalBillboardPhysicalVertex* physicalVertices;
};
struct NcFrameSubmission { NcCameraData camera; NcRenderObject* objects; uint32_t objectCount; NcDrawBatch* batches; uint32_t batchCount; NcOrbitLineVertex* orbitVertices; uint32_t orbitVertexCount; NcOrbitLineVertex* previousOrbitVertices; uint32_t previousOrbitVertexCount; NcOrbitLineVertex* bodyForwardVertices; uint32_t bodyForwardVertexCount; NcOrbitLineVertex* targetDirectionVertices; uint32_t targetDirectionVertexCount; NcPlanetaryPatch* planetaryPatches; uint32_t planetaryPatchCount; uint32_t planetaryGpuAlignmentPadding; NcPlanetaryGpuConstants planetaryGpu; NcPlanetaryMode planetaryMode; NcPlanetarySurfaceMode planetarySurfaceMode; uint32_t physicalSurfaceGeneration, planetaryPadding; NcPlanetaryPresentation planetaryPresentation; NcPlanetaryPresentation* distantBodies; uint32_t distantBodyCount, distantBodyPadding; NcSolarLighting solarLighting; NcAnchoredSurfacePatch* anchoredSurfacePatches; uint32_t anchoredSurfacePatchCount, anchoredSurfaceCacheSlotCount, anchoredSurfaceActiveGeneration, anchoredSurfaceFlags, anchoredSurfaceGpuReadyGeneration, anchoredSurfacePadding[5]; NcAnchoredSurfacePresentation anchoredSurfacePresentation; NcProductionSphericalBillboardSubmission* productionBillboard; uint32_t productionBillboardFlags, productionBillboardPadding; };
struct NcAbiLayout { uint32_t encodedPositionSize, cameraDataSize, cameraPositionOffset, cameraViewProjectionOffset, renderTransformSize, renderObjectSize, renderObjectPositionOffset, renderObjectTransformOffset, renderObjectMeshOffset; uint32_t drawBatchSize, orbitLineVertexSize, frameSubmissionSize, frameObjectsOffset, frameBatchesOffset, frameOrbitVerticesOffset, frameOrbitVertexCountOffset; uint32_t inputStateSize, inputDeltaSecondsOffset, inputMoveLeftOffset, inputMoveRightOffset, inputMoveForwardOffset, inputMoveBackwardOffset, inputMoveDownOffset, inputMoveUpOffset, inputResetOffset, inputLookActiveOffset, inputMouseDeltaXOffset, inputMouseDeltaYOffset, inputMouseWheelDetentsOffset, inputPauseToggleOffset, inputRateDecreaseOffset, inputRateIncreaseOffset, inputSasModeKeyOffset, inputFastModifierOffset, inputSlowModifierOffset; uint32_t framePlanetaryGpuOffset, framePlanetaryModeOffset, framePlanetaryPresentationOffset, inputPresentationFocusOffset, frameSolarLightingOffset, inputViewportWidthOffset, inputViewportHeightOffset; };
// mouseWheelDetents is signed Win32 WHEEL_DELTA-normalized detents, consumed once per callback.
struct NcInputState { float deltaSeconds; uint32_t moveLeft, moveRight, moveForward, moveBackward, moveDown, moveUp, reset, lookActive; float mouseDeltaX, mouseDeltaY; int32_t mouseWheelDetents; uint32_t pauseToggle, rateDecrease, rateIncrease, sasModeKey, fastModifier, slowModifier; NcPresentationFocus presentationFocus; uint32_t viewportWidthPixels, viewportHeightPixels; };
enum NcHostEventType : uint32_t { NC_DIAGNOSTIC = 1, NC_UPDATE_FRAME = 2 };
enum NcLogCategory : uint32_t { NC_LOG_ALWAYS = 0, NC_LOG_NONE = 0, NC_LOG_STARTUP = 1 << 0, NC_LOG_VULKAN = 1 << 1, NC_LOG_PRECISION = 1 << 2, NC_LOG_INPUT = 1 << 3, NC_LOG_RENDERER = 1 << 4, NC_LOG_VALIDATION = 1 << 5, NC_LOG_CAMERA = 1 << 6 };
struct NcHostEvent { NcHostEventType type; uint32_t logCategory; const char* utf8Message; NcInputState input; NcFrameSubmission* submission; };
struct NcRuntimeAssets {
  uint32_t size, version;
  const char* productionTerrainPathUtf8;
  const char* localTerrainPathUtf8;
  const char* elevationOraclePathUtf8;
};
// Physical-height query ABI. These records are std430-compatible and are not
// part of the live frame submission.  Every byte is explicitly initialized by
// the managed caller or native result writer.
struct alignas(16) NcPlanetaryHeightQuery {
  float anchorHigh[4];
  float anchorLow[4];
  float localDelta[4];
  double oracleUv[2]; // CPU-oracle storage address derived from the same body-fixed point
  uint32_t identity[4]; // body low/high, terrain version, anchored tier
  uint32_t metadata[4]; // topology version, source policy, reserved, reserved
};
struct alignas(16) NcPlanetaryHeightResult {
  float reconstructedHigh[4];
  float reconstructedLow[4];
  double faceUv[2];
  double oracleAndTerrainV5Height[2];
  double localAndPhysicalHeight[2];
  double baseAndModifierHeight[2];
  double modifierHeights[2];
  double finalGradient[2];
  float physicalNormalAndWeight[4];
  double reconstructedXY[2];
  double reconstructedZAndLength[2];
  uint32_t globalIdentity[4]; // face, level, x, y
  uint32_t localIdentity[4];  // available, level, x, y
  uint32_t source[4];         // valid, local available, terrain version, reserved
};
struct NcPlanetaryHeightQueryAssets {
  uint32_t size, version;
  const char* elevationOraclePathUtf8;
  const char* productionTerrainPathUtf8;
  const char* localTerrainPathUtf8;
  const char* computeShaderPathUtf8;
};
struct alignas(8) NcPlanetaryHeightQueryMetrics {
  uint32_t size, version, queryCount, dispatchGroups;
  uint32_t validationErrors, globalRecordCount, localRecordCount, reserved;
  double cpuMilliseconds, gpuMilliseconds;
};
// Persistent mesh-preparation ABI. Native owns every Vulkan
// handle; initialize/prepare/shutdown delimit one reusable validation session.
struct alignas(16) NcPlanetaryDisplacedVertex {
  float bodyHigh[4];              // xyz physical body-fixed position; w physical height
  float bodyLow[4];               // xyz low residual; w terrain-v5 reference height
  float cameraRelative[4];        // xyz final presentation position; w regional residual
  double faceUv[2];
  uint32_t globalIdentity[4];     // face, level, x, y
  uint32_t localIdentity[4];      // available, level, x, y
  uint32_t source[4];             // valid, local available, terrain version, topology version
};
struct alignas(16) NcPlanetaryPhysicalNormal { float x, y, z, validity; };
struct NcPlanetaryMeshPreparationAssets {
  uint32_t size, version;
  const char* elevationOraclePathUtf8;
  const char* productionTerrainPathUtf8;
  const char* localTerrainPathUtf8; // optional; null/empty selects oracle-only fallback
  const char* displacementShaderPathUtf8;
  const char* normalShaderPathUtf8;
  uint32_t maximumVertexCount, maximumIndexCount, maximumAdjacencyCount, reserved;
};
struct alignas(16) NcPlanetaryMeshPreparationDispatch {
  uint32_t size, version, vertexCount, indexCount;
  uint32_t adjacencyCount, topologyVersion, terrainVersion, sourcePolicy;
  float cameraHigh[4];
  float cameraLow[4];
  double bodyRadiusMetres;
  uint32_t reserved[2];
};
struct alignas(8) NcPlanetaryMeshPreparationMetrics {
  uint32_t size, version, vertexCount, triangleCount;
  uint32_t adjacencyCount, displacementGroups, normalGroups, validationErrors;
  uint32_t initializationCount, preparationCount, pipelineCreationCount, shaderModuleCreationCount;
  uint64_t persistentBufferBytes;
  double setupMilliseconds, displacementMilliseconds, normalMilliseconds, totalMilliseconds;
};
// P2S3-only isolated spherical-billboard Vulkan proof ABI. It deliberately
// carries topology identity, not production patch identity.
struct alignas(16) NcSphericalBillboardProofVertex { float direction[4]; };
struct NcSphericalBillboardProofAssets {
  uint32_t size, version;
  const char* resetShaderPathUtf8;
  const char* prepareShaderPathUtf8;
  const char* normalShaderPathUtf8;
  const char* cullShaderPathUtf8;
  const char* compactShaderPathUtf8;
  const char* vertexShaderPathUtf8;
  const char* fragmentShaderPathUtf8;
  uint32_t maximumVertexWorkItems, maximumTriangleWorkItems, frameResourceCount, renderExtent;
};
struct NcSphericalBillboardProofTopology {
  uint32_t size, version, formatVersion, generatorVersion;
  uint32_t level, vertexCount, indexCount, neighborOffsetCount;
  uint32_t neighborCount, reserved0, reserved1, reserved2;
  uint64_t topologyHash;
  const NcSphericalBillboardProofVertex* vertices;
  const uint32_t* indices;
  const uint32_t* neighborOffsets;
  const uint32_t* neighbors;
};
// P2S4 publication payload.  These are canonical physical results prepared by
// the shared GPU physical-surface path; topology consumes but does not own them.
struct alignas(16) NcSphericalBillboardPhysicalVertex {
  double bodyFixed[4]; // xyz final displaced point; w canonical height
  float normal[4];     // xyz canonical final-surface normal; w validity
  float reserved[4];   // std430 dvec4 struct-array stride padding
};
static_assert(sizeof(NcSphericalBillboardPhysicalVertex) == 64,
              "physical billboard vertex must match std430 array stride");
struct NcSphericalBillboardPhysicalSurface {
  uint32_t size, version, vertexCount, physicalGeneration;
  uint32_t terrainDataGeneration, reserved0, reserved1, reserved2;
  uint64_t expectedTopologyHash;
  const NcSphericalBillboardPhysicalVertex* vertices;
};
struct NcSphericalBillboardProofFrame {
  uint32_t size, version, frameIndex, renderEnabled;
  uint32_t workVertexCount, workTriangleCount, reserved0, reserved1;
  uint64_t expectedTopologyHash;
  double bodyRadiusMetres, cameraDistanceMetres;
  float verticalTanHalfFov, aspectRatio;
  uint32_t reserved2, reserved3;
};
struct alignas(8) NcSphericalBillboardProofMetrics {
  uint32_t size, version, activeLevel, readiness;
  uint32_t baseVertexCount, baseTriangleCount, workVertexCount, workTriangleCount;
  uint32_t preparedVertices, visibleTriangles, backfaceRejected, frustumRejected;
  uint32_t invalidRejected, overflowCount, indirectIndexCount, indirectDrawCount;
  uint32_t invalidCommands, validationErrors, frameSlot, frameWaitCount;
  uint32_t topologyUploadCount, frameOutputWriteCount, cullingDispatchCount, indirectSubmissionCount;
  uint32_t topologyReplacementCount, runtimeTopologyGenerationCount, pipelineCreationCount, shaderModuleCreationCount;
  uint64_t topologyHash, topologyBytesUploaded, activeTopologyBytes, pixelChecksum;
  uint64_t immutableVertexBytes, immutableIndexBytes, immutableAdjacencyBytes;
  uint64_t framePositionBytes, frameNormalBytes, frameVisibilityBytes, frameCompactedIndexBytes;
  uint64_t frameIndirectBytes, frameCounterBytes, temporaryScratchBytes, totalAllocatedBytes;
  double setupMilliseconds, topologyUploadMilliseconds, cpuFrameMilliseconds;
  double preparationMilliseconds, normalMilliseconds, cullingMilliseconds, compactionMilliseconds;
  double drawMilliseconds, gpuTotalMilliseconds;
  uint32_t physicalGeneration, terrainDataGeneration, preparedPhysicalSamples, physicalPreparationDispatchCount;
  uint32_t physicalReuseCount, staleGenerationRejections, nonFinitePhysicalOutputs, reservedPhysical;
  uint64_t immutablePhysicalBytes;
  double directionDecodeMaximumErrorRadians;
  uint32_t incomingLevel, incomingReadiness, publicationCount, deferredRetirementCount;
  uint64_t incomingTopologyHash, incomingTopologyBytes, selectedIncomingBytes;
  uint32_t zeroOwnerFrames, overlapOwnerFrames, staleGenerationDraws, reservedProduction;
};
typedef void(__cdecl* NcHostCallback)(NcHostEvent* hostEvent, void* userData);
enum NcResult : int32_t { NC_SUCCESS = 0, NC_FAILURE = 1, NC_INVALID_ARGUMENT = 2 };
NC_API NcResult __cdecl nc_run_renderer(NcFrameSubmission* submission, NcHostCallback callback, void* userData);
NC_API NcResult __cdecl nc_run_renderer_with_assets(NcFrameSubmission* submission, NcHostCallback callback, void* userData, const NcRuntimeAssets* assets);
NC_API NcResult __cdecl nc_validate_planetary_patches(const NcPlanetaryPatch* patches, uint32_t count);
NC_API NcResult __cdecl nc_validate_terrain_asset(const char* pathUtf8, uint64_t bodyId, uint32_t terrainVersion, uint32_t expectedRecordCount);
NC_API NcResult __cdecl nc_query_planetary_physical_heights(const NcPlanetaryHeightQuery* queries, uint32_t count, NcPlanetaryHeightResult* results, const NcPlanetaryHeightQueryAssets* assets, NcPlanetaryHeightQueryMetrics* metrics);
NC_API NcResult __cdecl nc_initialize_planetary_mesh_preparation(const NcPlanetaryMeshPreparationAssets* assets, NcPlanetaryMeshPreparationMetrics* metrics);
NC_API NcResult __cdecl nc_prepare_planetary_mesh(const NcPlanetaryHeightQuery* vertices, const uint32_t* indices, const uint32_t* adjacencyWords, const NcPlanetaryMeshPreparationDispatch* dispatch, NcPlanetaryDisplacedVertex* displaced, NcPlanetaryPhysicalNormal* normals, NcPlanetaryMeshPreparationMetrics* metrics);
NC_API NcResult __cdecl nc_shutdown_planetary_mesh_preparation(void);
NC_API NcResult __cdecl nc_initialize_spherical_billboard_gpu_proof(const NcSphericalBillboardProofAssets* assets, NcSphericalBillboardProofMetrics* metrics);
NC_API NcResult __cdecl nc_upload_spherical_billboard_gpu_proof_topology(const NcSphericalBillboardProofTopology* topology, NcSphericalBillboardProofMetrics* metrics);
NC_API NcResult __cdecl nc_publish_spherical_billboard_physical_surface(const NcSphericalBillboardPhysicalSurface* surface, NcSphericalBillboardProofMetrics* metrics);
NC_API NcResult __cdecl nc_run_spherical_billboard_gpu_proof_frame(const NcSphericalBillboardProofFrame* frame, NcSphericalBillboardProofMetrics* metrics);
NC_API NcResult __cdecl nc_shutdown_spherical_billboard_gpu_proof(void);
NC_API NcResult __cdecl nc_get_abi_layout(NcAbiLayout* layout);
}
