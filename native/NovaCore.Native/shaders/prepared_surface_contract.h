#ifndef NOVACORE_PREPARED_SURFACE_CONTRACT_H
#define NOVACORE_PREPARED_SURFACE_CONTRACT_H
// Shared by preparation, TES and native pipeline selection. In this contract the
// published vertices contain the complete render relief; TES adds no geometry.
// Authored displacement must change this contract and its raster eligibility
// together. It must never silently enter the direct prepared-surface pipeline.
#ifndef NOVACORE_PREPARED_RENDER_TERRAIN
#define NOVACORE_PREPARED_RENDER_TERRAIN 1
#endif
#endif
