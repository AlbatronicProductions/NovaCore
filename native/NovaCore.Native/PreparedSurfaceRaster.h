#pragma once
#include "NovaCoreNative.h"
#include "shaders/prepared_surface_contract.h"
#include <cmath>

inline bool DirectPreparedSurfaceEligible(uint32_t topologyFamily,uint32_t physicalGeneration,uint32_t surfaceMode,uint32_t surfaceDiagnostic,
                                         uint32_t rasterFlags,float targetTexelPixels) {
  // Raster overrides and negative factor probes explicitly exercise the TES
  // diagnostic owner. Recording ordinary evidence does not change rendering.
  return NOVACORE_PREPARED_RENDER_TERRAIN &&
    topologyFamily==NC_PLANETARY_PRODUCTION_TOPOLOGY_NESTED_SCALE_MESH_NCSM1 &&
    physicalGeneration==4 && surfaceMode==NC_PLANETARY_SURFACE_PRODUCTION_CUBE &&
    surfaceDiagnostic==0 && (rasterFlags&0xfc00u)==0 &&
    std::isfinite(targetTexelPixels) && targetTexelPixels>=0;
}
