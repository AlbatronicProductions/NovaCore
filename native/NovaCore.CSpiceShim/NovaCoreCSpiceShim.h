#pragma once
#include <stdint.h>
#ifdef _WIN32
#define NCSP_API extern "C" __declspec(dllexport)
#define NCSP_CALL __cdecl
#else
#define NCSP_API extern "C"
#define NCSP_CALL
#endif
struct NcspState { double positionKm[3]; double velocityKmPerSecond[3]; };
struct NcspMatrix3 { double value[9]; };
NCSP_API int NCSP_CALL NcspGetToolkitVersion(char* destination, int capacity);
NCSP_API int NCSP_CALL NcspLoadKernel(const char* utf8Path);
NCSP_API int NCSP_CALL NcspClearKernels();
NCSP_API int NCSP_CALL NcspQueryGeometricState(int target, double et, NcspState* state);
NCSP_API int NCSP_CALL NcspQueryFrameTransform(const char* fromFrame, const char* toFrame, double et, NcspMatrix3* transform);
NCSP_API int NCSP_CALL NcspHasFailure();
NCSP_API int NCSP_CALL NcspGetError(int longMessage, char* destination, int capacity);
NCSP_API void NCSP_CALL NcspResetError();
