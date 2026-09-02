#ifndef NOVACORE_PLANETARY_PHYSICAL_AUTHORITY_GLSL
#define NOVACORE_PLANETARY_PHYSICAL_AUTHORITY_GLSL

// One body-fixed geographic-height source for every production geometry
// density.  The complete global mesh and the transactionally published
// anchored hierarchy may sample this field at different densities, but neither
// may substitute its presentation payload for physical surface authority.
layout(std430,set=0,binding=33) readonly buffer CanonicalElevationOracle
{
  uint canonicalElevationOracleWords[];
};

uint CanonicalElevationOracleU16(uint index)
{
  uint word=canonicalElevationOracleWords[index>>1u];
  return (index&1u)==0u?word&0xffffu:word>>16u;
}

double CanonicalElevationOracleMetres(dvec3 direction)
{
  const uint width=8192u,height=4096u;
  direction=normalize(direction);
  // Addressing is evaluated in the canonical right-handed body-fixed frame:
  // positive longitude advances toward -Z, matching the CPU oracle transport.
  vec3 lookupDirection=normalize(vec3(direction));
  double longitude=double(atan(-lookupDirection.z,lookupDirection.x));
  double u=longitude/6.283185307179586476925286766559+.5;
  u-=floor(u);
  double v=double(acos(clamp(lookupDirection.y,-1.0,1.0)))/3.1415926535897932384626433832795;
  double px=u*double(width)-.5,py=v*double(height)-.5;
  int ix=int(floor(px)),iy=clamp(int(floor(py)),0,int(height)-1);
  uint x0=uint((ix%int(width)+int(width))%int(width));
  uint x1=(x0+1u)%width,y0=uint(iy),y1=min(y0+1u,height-1u);
  double tx=px-floor(px),ty=py-floor(py);
  double a=-11000.0+double(CanonicalElevationOracleU16(y0*width+x0))*(20000.0/65535.0);
  double b=-11000.0+double(CanonicalElevationOracleU16(y0*width+x1))*(20000.0/65535.0);
  double c=-11000.0+double(CanonicalElevationOracleU16(y1*width+x0))*(20000.0/65535.0);
  double d=-11000.0+double(CanonicalElevationOracleU16(y1*width+x1))*(20000.0/65535.0);
  return mix(mix(a,b,tx),mix(c,d,tx),ty);
}

double CanonicalGeographicHeight(dvec3 direction)
{
  vec3 unitDirection=normalize(vec3(direction));
  // Regional NCCUBE2 data is a residual against the signed global oracle.
  // Clamp only after recomposition so CPU queries, clearance, complete global
  // geometry, and refined geometry resolve the same physical geography.
  return max(0.0,CanonicalElevationOracleMetres(direction)+
    double(LocalTerrainElevationResidual(unitDirection)));
}

double CanonicalBasePhysicalHeight(dvec3 direction)
{
  double geographic=CanonicalGeographicHeight(direction);
  return max(0.0,geographic+TerrainBaseModifierHeightD(normalize(direction),geographic));
}

double CanonicalBasePhysicalHeight(dvec3 direction,PhysicalFrequencyContextD frequency)
{
  double geographic=CanonicalGeographicHeight(direction);
  return max(0.0,geographic+TerrainBaseModifierHeightD(normalize(direction),geographic,frequency));
}

double CanonicalPhysicalHeight(dvec3 direction)
{
  double geographic=CanonicalGeographicHeight(direction);
  PhysicalModifierEvaluationD modifiers=EvaluateTerrainModifiersD(normalize(direction),geographic);
  double base=max(0.0,geographic+modifiers.tiledHeight+modifiers.erosionHeight+modifiers.mesoHeight);
  return max(0.0,base+modifiers.nearHeight);
}

dvec3 CanonicalBasePhysicalPoint(dvec3 direction,double radius)
{
  direction=normalize(direction);
  return direction*(radius+CanonicalBasePhysicalHeight(direction));
}

vec3 CanonicalBasePhysicalNormal(dvec3 direction,double radius)
{
  direction=normalize(direction);
  dvec3 east=PhysicalEastD(direction),north=normalize(cross(direction,east));
  double angle=40.0/radius;
  dvec3 left=CanonicalBasePhysicalPoint(direction-east*angle,radius);
  dvec3 right=CanonicalBasePhysicalPoint(direction+east*angle,radius);
  dvec3 down=CanonicalBasePhysicalPoint(direction-north*angle,radius);
  dvec3 up=CanonicalBasePhysicalPoint(direction+north*angle,radius);
  vec3 candidate=normalize(vec3(cross(right-left,up-down))),radial=vec3(direction);
  if(any(isnan(candidate))||any(isinf(candidate)))return radial;
  return dot(candidate,radial)<0.0?-candidate:candidate;
}

#endif
