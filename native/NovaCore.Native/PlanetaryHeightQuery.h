#pragma once
#include "NovaCoreNative.h"

NcResult RunPlanetaryHeightQueries(const NcPlanetaryHeightQuery* queries, uint32_t count,
  NcPlanetaryHeightResult* results, const NcPlanetaryHeightQueryAssets* assets,
  NcPlanetaryHeightQueryMetrics* metrics);
