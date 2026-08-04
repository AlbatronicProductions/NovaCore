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
struct NcFrameSubmission { NcCameraData camera; NcRenderObject* objects; uint32_t objectCount; NcDrawBatch* batches; uint32_t batchCount; };
struct NcAbiLayout { uint32_t encodedPositionSize, cameraDataSize, cameraPositionOffset, cameraViewProjectionOffset, renderTransformSize, renderObjectSize, renderObjectPositionOffset, renderObjectTransformOffset, renderObjectMeshOffset; uint32_t drawBatchSize, frameSubmissionSize, frameObjectsOffset, frameBatchesOffset; uint32_t inputStateSize, inputDeltaSecondsOffset, inputMoveLeftOffset, inputMoveRightOffset, inputMoveForwardOffset, inputMoveBackwardOffset, inputMoveDownOffset, inputMoveUpOffset, inputResetOffset, inputLookActiveOffset, inputMouseDeltaXOffset, inputMouseDeltaYOffset, inputMouseWheelDetentsOffset; };
// mouseWheelDetents is signed Win32 WHEEL_DELTA-normalized detents, consumed once per callback.
struct NcInputState { float deltaSeconds; uint32_t moveLeft, moveRight, moveForward, moveBackward, moveDown, moveUp, reset, lookActive; float mouseDeltaX, mouseDeltaY; int32_t mouseWheelDetents; };
enum NcHostEventType : uint32_t { NC_DIAGNOSTIC = 1, NC_UPDATE_FRAME = 2 };
enum NcLogCategory : uint32_t { NC_LOG_ALWAYS = 0, NC_LOG_NONE = 0, NC_LOG_STARTUP = 1 << 0, NC_LOG_VULKAN = 1 << 1, NC_LOG_PRECISION = 1 << 2, NC_LOG_INPUT = 1 << 3, NC_LOG_RENDERER = 1 << 4, NC_LOG_VALIDATION = 1 << 5, NC_LOG_CAMERA = 1 << 6 };
struct NcHostEvent { NcHostEventType type; uint32_t logCategory; const char* utf8Message; NcInputState input; NcFrameSubmission* submission; };
typedef void(__cdecl* NcHostCallback)(NcHostEvent* hostEvent, void* userData);
enum NcResult : int32_t { NC_SUCCESS = 0, NC_FAILURE = 1, NC_INVALID_ARGUMENT = 2 };
NC_API NcResult __cdecl nc_run_renderer(NcFrameSubmission* submission, NcHostCallback callback, void* userData);
NC_API NcResult __cdecl nc_get_abi_layout(NcAbiLayout* layout);
}
