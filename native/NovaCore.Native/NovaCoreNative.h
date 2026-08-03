#pragma once
#include <stdint.h>

#ifdef _WIN32
#define NC_API __declspec(dllexport)
#else
#define NC_API
#endif

extern "C" {
struct NcRelativePosition { double x; double y; double z; };
typedef void(__cdecl* NcDiagnosticCallback)(const char* utf8Message, void* userData);
enum NcResult : int32_t { NC_SUCCESS = 0, NC_FAILURE = 1, NC_INVALID_ARGUMENT = 2 };
NC_API NcResult __cdecl nc_run_triangle(NcRelativePosition position, NcDiagnosticCallback callback, void* userData);
}
