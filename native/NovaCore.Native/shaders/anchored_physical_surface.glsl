#ifndef NOVACORE_ANCHORED_PHYSICAL_SURFACE_GLSL
#define NOVACORE_ANCHORED_PHYSICAL_SURFACE_GLSL

#include "planetary_physical_authority.glsl"

// Shared anchored physical-surface evaluation.  The production vertex stage
// evaluates this at the reusable base vertices; tessellation then refines the
// already-physical surface instead of repeating the complete height and normal
// oracle for every generated TES invocation.
double AnchoredGeographicHeight(dvec3 direction)
{
  return CanonicalGeographicHeight(direction);
}

double AnchoredBasePhysicalHeight(dvec3 direction)
{
  return CanonicalBasePhysicalHeight(direction);
}

double AnchoredPhysicalHeight(dvec3 direction)
{
  return CanonicalPhysicalHeight(direction);
}

dvec3 AnchoredBasePhysicalPoint(dvec3 direction,double radius)
{
  return CanonicalBasePhysicalPoint(direction,radius);
}

vec3 AnchoredBasePhysicalNormal(dvec3 direction,double radius)
{
  return CanonicalBasePhysicalNormal(direction,radius);
}

vec3 AnchoredPhysicalNormal(dvec3 direction,double radius)
{
  direction=normalize(direction);vec3 radial=vec3(direction),base=AnchoredBasePhysicalNormal(direction,radius);
  dvec3 eastD=PhysicalEastD(direction),northD=normalize(cross(direction,eastD));
  NearPhysicalEvaluationD nearValue=EvaluateNearPhysicalD(direction,AnchoredGeographicHeight(direction));
  double radialComponent=max(double(dot(base,radial)),1e-9);
  double eastSlope=-double(dot(base,vec3(eastD)))/radialComponent+nearValue.eastGradient;
  double northSlope=-double(dot(base,vec3(northD)))/radialComponent+nearValue.northGradient;
  return normalize(radial-vec3(eastD)*float(eastSlope)-vec3(northD)*float(northSlope));
}

#endif
