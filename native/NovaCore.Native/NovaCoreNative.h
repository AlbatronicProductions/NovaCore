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
struct alignas(16) NcPlanetaryGpuConstants {
  float cameraBodyHighX, cameraBodyHighY, cameraBodyHighZ, radiusHigh;
  float cameraBodyLowX, cameraBodyLowY, cameraBodyLowZ, radiusLow;
  float refinementThreshold, nearFieldAltitudeRadii, surfaceAltitudeMetres, maximumTerrainHeightMetres;
  uint32_t maximumLevel, outputCapacity, terrainVersion, terrainFrame;
  float viewForwardX, viewForwardY, viewForwardZ, viewHalfAngleRadians;
  float viewportHeightPixels, verticalTanHalfFov, targetTexelPixels, requestedAlbedoLevel;
};
struct alignas(16) NcPlanetaryEyeball {
  float cameraBodyHighX, cameraBodyHighY, cameraBodyHighZ, radiusHigh;
  float cameraBodyLowX, cameraBodyLowY, cameraBodyLowZ, radiusLow;
  float surfaceAltitudeMetres, maximumTerrainHeightMetres, oceanSeaLevelMetres, blendAlpha;
  uint32_t bodyIdLow, bodyIdHigh, terrainVersion, enabled;
  float tangentAnchorX, tangentAnchorY, tangentAnchorZ, maximumAngleRadians;
  float radialWarpExponent, detailFrequency, normalStepMetres, regionalAlpha;
  uint32_t vertexCount, indexCount, radialRingCount, azimuthSegmentCount;
  uint32_t reserved0, reserved1, reserved2, reserved3;
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
};
struct alignas(16) NcSolarLighting { float sourceCenterX, sourceCenterY, sourceCenterZ, exposure; float photosphereR, photosphereG, photosphereB, ambientFloor; float sourceRadiance, glowStrength; uint32_t enabled, speedHud; };
struct NcOrbitLineVertex { float position[3]; };
struct NcFrameSubmission { NcCameraData camera; NcRenderObject* objects; uint32_t objectCount; NcDrawBatch* batches; uint32_t batchCount; NcOrbitLineVertex* orbitVertices; uint32_t orbitVertexCount; NcOrbitLineVertex* previousOrbitVertices; uint32_t previousOrbitVertexCount; NcOrbitLineVertex* bodyForwardVertices; uint32_t bodyForwardVertexCount; NcOrbitLineVertex* targetDirectionVertices; uint32_t targetDirectionVertexCount; NcPlanetaryPatch* planetaryPatches; uint32_t planetaryPatchCount; uint32_t planetaryGpuAlignmentPadding; NcPlanetaryGpuConstants planetaryGpu; NcPlanetaryMode planetaryMode; NcPlanetarySurfaceMode planetarySurfaceMode; uint32_t planetaryPadding[2]; NcPlanetaryPresentation planetaryPresentation; NcPlanetaryPresentation* distantBodies; uint32_t distantBodyCount, distantBodyPadding; NcSolarLighting solarLighting; NcPlanetaryEyeball planetaryEyeball; };
struct NcAbiLayout { uint32_t encodedPositionSize, cameraDataSize, cameraPositionOffset, cameraViewProjectionOffset, renderTransformSize, renderObjectSize, renderObjectPositionOffset, renderObjectTransformOffset, renderObjectMeshOffset; uint32_t drawBatchSize, orbitLineVertexSize, frameSubmissionSize, frameObjectsOffset, frameBatchesOffset, frameOrbitVerticesOffset, frameOrbitVertexCountOffset; uint32_t inputStateSize, inputDeltaSecondsOffset, inputMoveLeftOffset, inputMoveRightOffset, inputMoveForwardOffset, inputMoveBackwardOffset, inputMoveDownOffset, inputMoveUpOffset, inputResetOffset, inputLookActiveOffset, inputMouseDeltaXOffset, inputMouseDeltaYOffset, inputMouseWheelDetentsOffset, inputPauseToggleOffset, inputRateDecreaseOffset, inputRateIncreaseOffset, inputSasModeKeyOffset; uint32_t framePlanetaryGpuOffset, framePlanetaryModeOffset, framePlanetaryPresentationOffset, inputPresentationFocusOffset, frameSolarLightingOffset, framePlanetaryEyeballOffset; };
// mouseWheelDetents is signed Win32 WHEEL_DELTA-normalized detents, consumed once per callback.
struct NcInputState { float deltaSeconds; uint32_t moveLeft, moveRight, moveForward, moveBackward, moveDown, moveUp, reset, lookActive; float mouseDeltaX, mouseDeltaY; int32_t mouseWheelDetents; uint32_t pauseToggle, rateDecrease, rateIncrease, sasModeKey; NcPresentationFocus presentationFocus; };
enum NcHostEventType : uint32_t { NC_DIAGNOSTIC = 1, NC_UPDATE_FRAME = 2 };
enum NcLogCategory : uint32_t { NC_LOG_ALWAYS = 0, NC_LOG_NONE = 0, NC_LOG_STARTUP = 1 << 0, NC_LOG_VULKAN = 1 << 1, NC_LOG_PRECISION = 1 << 2, NC_LOG_INPUT = 1 << 3, NC_LOG_RENDERER = 1 << 4, NC_LOG_VALIDATION = 1 << 5, NC_LOG_CAMERA = 1 << 6 };
struct NcHostEvent { NcHostEventType type; uint32_t logCategory; const char* utf8Message; NcInputState input; NcFrameSubmission* submission; };
struct NcRuntimeAssets { uint32_t size, version; const char* productionTerrainPathUtf8; };
typedef void(__cdecl* NcHostCallback)(NcHostEvent* hostEvent, void* userData);
enum NcResult : int32_t { NC_SUCCESS = 0, NC_FAILURE = 1, NC_INVALID_ARGUMENT = 2 };
NC_API NcResult __cdecl nc_run_renderer(NcFrameSubmission* submission, NcHostCallback callback, void* userData);
NC_API NcResult __cdecl nc_run_renderer_with_assets(NcFrameSubmission* submission, NcHostCallback callback, void* userData, const NcRuntimeAssets* assets);
NC_API NcResult __cdecl nc_validate_planetary_patches(const NcPlanetaryPatch* patches, uint32_t count);
NC_API NcResult __cdecl nc_validate_terrain_asset(const char* pathUtf8, uint64_t bodyId, uint32_t terrainVersion, uint32_t expectedRecordCount);
NC_API NcResult __cdecl nc_get_abi_layout(NcAbiLayout* layout);
}
