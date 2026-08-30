#ifndef NOVACORE_ANCHORED_PHYSICAL_SURFACE_GLSL
#define NOVACORE_ANCHORED_PHYSICAL_SURFACE_GLSL

layout(std430,set=0,binding=33) readonly buffer AnchoredElevationOracle
{
  uint anchoredOracleWords[];
};

uint AnchoredOracleU16(uint index)
{
  uint word=anchoredOracleWords[index>>1u];
  return (index&1u)==0u?word&0xffffu:word>>16u;
}

double AnchoredOracleElevation(dvec3 direction)
{
  const uint width=8192u,height=4096u;
  direction=normalize(direction);
  // GLSL exposes the transcendental overloads used here at FP32.  Their
  // angular error is far below one source texel; retain FP64 for address
  // interpolation and all physical reconstruction after this lookup address.
  vec3 lookupDirection=normalize(vec3(direction));
  // Canonical BodyFixedGeography is right-handed: +longitude advances toward
  // -Z, so this must remain atan(-Z,+X), matching the CPU oracle transport.
  double longitude=double(atan(-lookupDirection.z,lookupDirection.x));
  double u=longitude/6.283185307179586476925286766559+.5;
  u-=floor(u);
  double v=double(acos(clamp(lookupDirection.y,-1.0,1.0)))/3.1415926535897932384626433832795;
  double px=u*double(width)-.5,py=v*double(height)-.5;
  int ix=int(floor(px)),iy=clamp(int(floor(py)),0,int(height)-1);
  uint x0=uint((ix%int(width)+int(width))%int(width));
  uint x1=(x0+1u)%width,y0=uint(iy),y1=min(y0+1u,height-1u);
  double tx=px-floor(px),ty=py-floor(py);
  double a=-11000.0+double(AnchoredOracleU16(y0*width+x0))*(20000.0/65535.0);
  double b=-11000.0+double(AnchoredOracleU16(y0*width+x1))*(20000.0/65535.0);
  double c=-11000.0+double(AnchoredOracleU16(y1*width+x0))*(20000.0/65535.0);
  double d=-11000.0+double(AnchoredOracleU16(y1*width+x1))*(20000.0/65535.0);
  return mix(mix(a,b,tx),mix(c,d,tx),ty);
}

// Shared anchored physical-surface evaluation.  The production vertex stage
// evaluates this at the reusable base vertices; tessellation then refines the
// already-physical surface instead of repeating the complete height and normal
// oracle for every generated TES invocation.
double AnchoredPhysicalHeight(dvec3 direction)
{
  vec3 unitDirection=normalize(vec3(direction));
  // SurfaceAnchor, camera clearance, the independent GPU height query, and
  // mesh-preparation validation all use this checked 8192x4096 FP64 authority.
  // terrain-v5 remains the complete visual/material fallback, but its bounded
  // L2 payload must not replace physical geometry authority at low altitude.
  double base=max(0.0,AnchoredOracleElevation(direction)+double(LocalTerrainElevationResidual(unitDirection)));
  return max(0.0,base+TerrainModifierHeightD(normalize(direction)));
}

dvec3 AnchoredPhysicalPoint(dvec3 direction,double radius)
{
  direction=normalize(direction);
  return direction*(radius+AnchoredPhysicalHeight(direction));
}

vec3 AnchoredPhysicalNormal(dvec3 direction,double radius)
{
  direction=normalize(direction);dvec3 east=PhysicalEastD(direction),north=normalize(cross(direction,east));
  // This is the canonical terrain-v5 source differential, not a camera- or
  // tessellation-dependent derivative.
  double angle=40.0/radius;
  dvec3 left=AnchoredPhysicalPoint(direction-east*angle,radius);
  dvec3 right=AnchoredPhysicalPoint(direction+east*angle,radius);
  dvec3 down=AnchoredPhysicalPoint(direction-north*angle,radius);
  dvec3 up=AnchoredPhysicalPoint(direction+north*angle,radius);
  vec3 candidate=normalize(vec3(cross(right-left,up-down))),radial=vec3(direction);
  if(any(isnan(candidate))||any(isinf(candidate)))return radial;
  return dot(candidate,radial)<0.0?-candidate:candidate;
}

#endif
