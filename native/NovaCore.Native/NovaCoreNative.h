#pragma once
#include <stdint.h>

#ifdef _WIN32
#define NC_API __declspec(dllexport)
#else
#define NC_API
#endif

extern "C" {
struct NcEncodedPosition { float high[4]; float low[4]; };
struct NcRenderObject { NcEncodedPosition position; uint32_t mesh; uint32_t padding[3]; };
struct NcFrameSubmission { NcEncodedPosition camera; NcRenderObject* objects; uint32_t objectCount; uint32_t padding; };
struct NcInputState { float deltaSeconds; uint32_t moveLeft; uint32_t moveRight; uint32_t moveForward; uint32_t moveBackward; };
enum NcHostEventType : uint32_t { NC_DIAGNOSTIC = 1, NC_UPDATE_FRAME = 2 };
enum NcLogCategory : uint32_t { NC_LOG_ALWAYS = 0, NC_LOG_NONE = 0, NC_LOG_STARTUP = 1 << 0, NC_LOG_VULKAN = 1 << 1, NC_LOG_PRECISION = 1 << 2, NC_LOG_INPUT = 1 << 3, NC_LOG_RENDERER = 1 << 4, NC_LOG_VALIDATION = 1 << 5 };
struct NcHostEvent { NcHostEventType type; uint32_t logCategory; const char* utf8Message; NcInputState input; NcFrameSubmission* submission; };
typedef void(__cdecl* NcHostCallback)(NcHostEvent* hostEvent, void* userData);
enum NcResult : int32_t { NC_SUCCESS = 0, NC_FAILURE = 1, NC_INVALID_ARGUMENT = 2 };
NC_API NcResult __cdecl nc_run_renderer(NcFrameSubmission* submission, NcHostCallback callback, void* userData);
}
