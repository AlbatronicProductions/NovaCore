#ifndef NOVACORE_PLANETARY_NATURAL_TERRAIN_SURFACE_GLSL
#define NOVACORE_PLANETARY_NATURAL_TERRAIN_SURFACE_GLSL

#include "planetary_natural_terrain_families.glsl"

const uint NOVACORE_PHYSICAL_GENERATION_3=3u;
const uint NOVACORE_PHYSICAL_GENERATION_M12D=4u;
const uint NOVACORE_NATURAL_CANDIDATE_SEED=0x4D12D2B1u;
const double NOVACORE_NATURAL_EARTH_REFERENCE_RADIUS=6371008.8;

NaturalTerrainFamilyIdentityD NaturalCandidateIdentityD()
{
  return NaturalTerrainFamilyIdentityD(uvec2(6u,0u),uvec2(2u,0u),
    NOVACORE_NATURAL_CANDIDATE_SEED);
}

NaturalTerrainCompositionSampleD EvaluateNaturalCandidateD(dvec3 direction)
{
  return EvaluateNaturalTerrainCompositionD(normalize(direction)*NOVACORE_NATURAL_EARTH_REFERENCE_RADIUS,
    NaturalCandidateIdentityD());
}

NaturalTerrainFieldSampleD EvaluateNaturalCandidatePreparedD(dvec3 direction)
{
  NaturalTerrainCompositionSampleD value=EvaluateNaturalCandidateD(direction);
  return NaturalTerrainFamilyAdd(value.macro,value.meso);
}

NaturalTerrainFieldSampleD EvaluateNaturalCandidateNearD(dvec3 direction)
{
  return EvaluateNaturalTerrainNearD(normalize(direction)*NOVACORE_NATURAL_EARTH_REFERENCE_RADIUS,
    NaturalCandidateIdentityD());
}

#endif
