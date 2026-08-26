#pragma once

#include "NovaCoreNative.h"

NcResult InitializePlanetaryMeshPreparation(
  const NcPlanetaryMeshPreparationAssets* assets,
  NcPlanetaryMeshPreparationMetrics* metrics);
NcResult PreparePlanetaryMesh(
  const NcPlanetaryHeightQuery* vertices,
  const uint32_t* indices,
  const uint32_t* adjacencyWords,
  const NcPlanetaryMeshPreparationDispatch* dispatch,
  NcPlanetaryDisplacedVertex* displaced,
  NcPlanetaryPhysicalNormal* normals,
  NcPlanetaryMeshPreparationMetrics* metrics);
NcResult ShutdownPlanetaryMeshPreparation();
