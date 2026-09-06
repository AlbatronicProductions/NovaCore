#pragma once
#include "AuthoredFacilityGeometry.h"
#include <algorithm>
#include <cmath>
#include <cstring>
#include <stdexcept>

// Stable body-fixed authored instance. No camera, Sun, pupil or physical-preparation identity.
struct NcFacilityCasterDefinition {
  uint64_t bodyId, facilityId, objectId;
  uint32_t version, geometrySet;
  double origin[3], east[3], north[3], up[3];
  double foundationScale[3], maximumRayDistance;
};
static_assert(sizeof(NcFacilityCasterDefinition)==160);
namespace nc::facility {
struct alignas(16) GpuCaster { float minimum[4],maximum[4]; };
struct alignas(32) GpuVisibility {
  uint32_t control[4]{}; // count, definition version, body identity low/high
  float boundsMin[4]{},boundsMax[4]{},padding[4]{};
  double cameraLocal[4]{},east[4]{},north[4]{},up[4]{};
  GpuCaster boxes[MaximumCasters]{};
  float receiverRelativeMin[4]{},receiverRelativeMax[4]{};
};
static_assert(offsetof(GpuVisibility,cameraLocal)==64);
static_assert(offsetof(GpuVisibility,boxes)==192);
static_assert(sizeof(GpuVisibility)==480);
inline void LocalizeReceiverBounds(GpuVisibility&g){
  // Conservative finite reach, for every Sun direction. A cheap body-relative
  // reject precedes all per-fragment Sun/frame math; the tighter swept local
  // bound remains Sun-aware. Directed rounding protects FP32 coarse rejection.
  for(int k=0;k<3;k++){
    double center=-(g.east[k]*g.cameraLocal[0]+g.north[k]*g.cameraLocal[1]+g.up[k]*g.cameraLocal[2]);
    double radius=g.boundsMax[3];
    double guard=8*1.1920928955078125e-7*(std::abs(center)+radius);
    g.receiverRelativeMin[k]=std::nextafter(float(center-radius-guard),-INFINITY);
    g.receiverRelativeMax[k]=std::nextafter(float(center+radius+guard),INFINITY);
  }
}
inline double Dot(const double*a,const double*b){return a[0]*b[0]+a[1]*b[1]+a[2]*b[2];}
inline void Validate(const NcFacilityCasterDefinition& d){
  if(!d.bodyId||!d.facilityId||!d.objectId||d.version!=GeometryVersion||d.geometrySet!=3||
     !std::isfinite(d.maximumRayDistance)||d.maximumRayDistance<=0||d.maximumRayDistance>2048)
    throw std::runtime_error("invalid authored facility caster identity/range");
  for(auto x:d.origin)if(!std::isfinite(x))throw std::runtime_error("invalid facility origin");
  for(auto x:d.foundationScale)if(!std::isfinite(x)||x<=0||x>2048)throw std::runtime_error("invalid bounded facility scale");
  const double*basis[]{d.east,d.north,d.up};
  for(int i=0;i<3;i++)for(int j=0;j<3;j++)if(!std::isfinite(Dot(basis[i],basis[j]))||std::abs(Dot(basis[i],basis[j])-(i==j?1.:0.))>1e-12)
    throw std::runtime_error("facility caster frame must be orthonormal FP64");
  double cross[]{d.east[1]*d.north[2]-d.east[2]*d.north[1],d.east[2]*d.north[0]-d.east[0]*d.north[2],d.east[0]*d.north[1]-d.east[1]*d.north[0]};
  if(std::abs(Dot(cross,d.up)-1)>1e-12)throw std::runtime_error("facility caster frame must be right handed");
}
inline GpuVisibility Prepare(const NcFacilityCasterDefinition& d){
  Validate(d);GpuVisibility g{};g.control[0]=5;g.control[1]=d.version;g.control[2]=uint32_t(d.bodyId);g.control[3]=uint32_t(d.bodyId>>32);
  for(int k=0;k<3;k++){g.east[k]=d.east[k];g.north[k]=d.north[k];g.up[k]=d.up[k];g.boundsMin[k]=INFINITY;g.boundsMax[k]=-INFINITY;}
  g.boundsMin[3]=float(d.maximumRayDistance);
  for(uint32_t i=0;i<5;i++)for(int k=0;k<3;k++){
    g.boxes[i].minimum[k]=i<4?LaunchPadBoxes[i].minimum[k]:float(FoundationUnit.minimum[k]*d.foundationScale[k]);
    g.boxes[i].maximum[k]=i<4?LaunchPadBoxes[i].maximum[k]:float(FoundationUnit.maximum[k]*d.foundationScale[k]);
    g.boundsMin[k]=std::min(g.boundsMin[k],g.boxes[i].minimum[k]);g.boundsMax[k]=std::max(g.boundsMax[k],g.boxes[i].maximum[k]);
  }
  double cornerSquared=0;for(int k=0;k<3;k++){double extent=std::max(std::abs(g.boundsMin[k]),std::abs(g.boundsMax[k]));cornerSquared+=extent*extent;}
  g.boundsMax[3]=float(d.maximumRayDistance+std::sqrt(cornerSquared));LocalizeReceiverBounds(g);return g;
}
// Independent FP64 reference. A touching tangent of zero length is clear; a ray
// entering an opaque volume is blocked. No geometry inflation or depth bias.
inline bool RayBox(const double*p,const double*l,const GpuCaster&b,double maximum){
  double enter=0,leave=maximum;
  for(int k=0;k<3;k++){
    if(l[k]==0){if(p[k]<b.minimum[k]||p[k]>b.maximum[k])return false;continue;}
    double a=(double(b.minimum[k])-p[k])/l[k],z=(double(b.maximum[k])-p[k])/l[k];
    enter=std::max(enter,std::min(a,z));leave=std::min(leave,std::max(a,z));
  }return leave>enter;
}
inline bool Blocked(const GpuVisibility&g,const double*p,const double*l){
  for(uint32_t i=0;i<g.control[0];i++)if(RayBox(p,l,g.boxes[i],g.boundsMin[3]))return true;
  return false;
}
}
