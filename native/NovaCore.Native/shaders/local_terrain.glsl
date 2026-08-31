#ifndef NOVACORE_LOCAL_TERRAIN_GLSL
#define NOVACORE_LOCAL_TERRAIN_GLSL

// NCCUBE2 local terrain is addressed by stable body-fixed relaxed-cube
// geography.  The metadata table is fixed-size and the BC arrays are
// renderer-lifetime resources; residency changes only the sector-to-layer map.
layout(set=0,binding=28) uniform sampler2DArray localTerrainAlbedo;
layout(set=0,binding=29) uniform sampler2DArray localTerrainResidual;
layout(set=0,binding=30) uniform sampler2DArray localTerrainNormal;
layout(set=0,binding=34) uniform sampler2DArray localTerrainControl;
layout(std430,set=0,binding=31) readonly buffer LocalTerrainLookup { uint words[]; } localTerrain;

const uint LocalTerrainLookupCapacity=2048u;
const uint LocalTerrainLookupEntryWords=12u;
const float LocalTerrainStoredExtent=264.0;
const float LocalTerrainBoundaryWidth=8.0/256.0;

uint LocalTerrainHash(uint face,uint level,uint x,uint y,uint detailFrequency,uint payloadVersion)
{
  uint value=face*73856093u^level*19349663u^x*83492791u^y*2654435761u^
    detailFrequency*2246822519u^payloadVersion*3266489917u;
  value^=value>>16u;
  return value&(LocalTerrainLookupCapacity-1u);
}

bool LookupLocalTerrain(
  uint face,
  uint level,
  uvec2 sector,
  out uint layer,
  out vec2 residualRange)
{
  layer=0u;residualRange=vec2(0);
  uint detailFrequency=localTerrain.words[2],payloadVersion=localTerrain.words[3];
  uint slot=LocalTerrainHash(face,level,sector.x,sector.y,detailFrequency,payloadVersion);
  for(uint probe=0u;probe<LocalTerrainLookupCapacity;probe++)
  {
    uint offset=16u+slot*LocalTerrainLookupEntryWords;
    uint residentLayer=localTerrain.words[offset+6u];
    if(residentLayer==0u)return false;
    if(localTerrain.words[offset]==face&&localTerrain.words[offset+1u]==level&&
       localTerrain.words[offset+2u]==sector.x&&localTerrain.words[offset+3u]==sector.y&&
       localTerrain.words[offset+4u]==detailFrequency&&localTerrain.words[offset+5u]==payloadVersion)
    {layer=residentLayer;residualRange=vec2(uintBitsToFloat(localTerrain.words[offset+8u]),uintBitsToFloat(localTerrain.words[offset+9u]));return true;}
    slot=(slot+1u)&(LocalTerrainLookupCapacity-1u);
  }
  return false;
}

float LocalTerrainCoverage(uint face,uint level,uvec2 sector,vec2 localUv)
{
  uint cells=1u<<level,unusedLayer;vec2 unusedRange;float weight=1.0;
  bool neighbor=sector.x>0u&&LookupLocalTerrain(face,level,uvec2(sector.x-1u,sector.y),unusedLayer,unusedRange);
  if(!neighbor)weight=min(weight,smoothstep(0.0,LocalTerrainBoundaryWidth,localUv.x));
  neighbor=sector.x+1u<cells&&LookupLocalTerrain(face,level,uvec2(sector.x+1u,sector.y),unusedLayer,unusedRange);
  if(!neighbor)weight=min(weight,smoothstep(0.0,LocalTerrainBoundaryWidth,1.0-localUv.x));
  neighbor=sector.y>0u&&LookupLocalTerrain(face,level,uvec2(sector.x,sector.y-1u),unusedLayer,unusedRange);
  if(!neighbor)weight=min(weight,smoothstep(0.0,LocalTerrainBoundaryWidth,localUv.y));
  neighbor=sector.y+1u<cells&&LookupLocalTerrain(face,level,uvec2(sector.x,sector.y+1u),unusedLayer,unusedRange);
  if(!neighbor)weight=min(weight,smoothstep(0.0,LocalTerrainBoundaryWidth,1.0-localUv.y));
  return weight;
}

bool ResolveLocalTerrain(
  vec3 unitDirection,
  out uint face,
  out uint level,
  out uint layer,
  out vec2 faceUv,
  out vec2 localUv,
  out vec2 residualRange)
{
  layer=0u;level=0u;face=0u;faceUv=vec2(0);localUv=vec2(0);residualRange=vec2(0);
  if(localTerrain.words[0]==0u)return false;
  ProductionDirectionAddress(unitDirection,face,faceUv);
  uint maximumLevel=localTerrain.words[1],minimumLevel=localTerrain.words[8];
  for(int signedLevel=int(maximumLevel);signedLevel>=int(minimumLevel);signedLevel--)
  {
    level=uint(signedLevel);uint cells=1u<<level;
    uvec2 sector=min(uvec2(floor(faceUv*float(cells))),uvec2(cells-1u));
    if(LookupLocalTerrain(face,level,sector,layer,residualRange)){localUv=clamp(faceUv*float(cells)-vec2(sector),vec2(0),vec2(1));return true;}
  }
  return false;
}

vec2 LocalTerrainStoredUv(vec2 localUv){return (vec2(4.0)+localUv*256.0)/LocalTerrainStoredExtent;}

float LocalTerrainElevationResidual(vec3 unitDirection)
{
  uint face,level,layer;vec2 faceUv,localUv,residualRange;
  if(!ResolveLocalTerrain(unitDirection,face,level,layer,faceUv,localUv,residualRange))return 0.0;
  float encoded=textureLod(localTerrainResidual,vec3(LocalTerrainStoredUv(localUv),float(layer-1u)),0.0).r;
  uvec2 sector=min(uvec2(floor(faceUv*float(1u<<level))),uvec2((1u<<level)-1u));
  return mix(residualRange.x,residualRange.y,encoded)*LocalTerrainCoverage(face,level,sector,localUv);
}

#ifdef NOVACORE_LOCAL_TERRAIN_FRAGMENT
struct LocalTerrainMaterialSample
{
  bool resident;
  vec3 albedo;
  vec2 normalXY;
  float controlClass;
  float weight;
  uint level;
};

LocalTerrainMaterialSample SampleLocalTerrainMaterial(vec3 unitDirection)
{
  LocalTerrainMaterialSample result;result.resident=false;result.albedo=vec3(0);result.normalXY=vec2(0);result.controlClass=0.0;result.weight=0.0;result.level=0u;
  uint face,level,layer;vec2 faceUv,localUv,residualRange;
  if(!ResolveLocalTerrain(unitDirection,face,level,layer,faceUv,localUv,residualRange))return result;
  float cells=float(1u<<level);
  vec2 stored=LocalTerrainStoredUv(localUv);
  // Gradients derive from the continuous face coordinate, then narrow into
  // the sector payload.  Four-texel gutters make filtering continuous at a
  // resident neighbor and clamp safely to base terrain when it is absent.
  vec2 gradientX=dFdx(faceUv)*cells*(256.0/LocalTerrainStoredExtent);
  vec2 gradientY=dFdy(faceUv)*cells*(256.0/LocalTerrainStoredExtent);
  result.albedo=textureGrad(localTerrainAlbedo,vec3(stored,float(layer-1u)),gradientX,gradientY).rgb;
  result.normalXY=textureGrad(localTerrainNormal,vec3(stored,float(layer-1u)),gradientX,gradientY).rg*2.0-1.0;
  // Control identity is categorical.  It must not create fractional biome
  // identities through linear filtering at a texel or record boundary.
  ivec2 controlTexel=clamp(ivec2(floor(stored*LocalTerrainStoredExtent)),ivec2(0),ivec2(263));
  result.controlClass=round(texelFetch(localTerrainControl,ivec3(controlTexel,int(layer-1u)),0).r*255.0);
  uvec2 sector=min(uvec2(floor(faceUv*cells)),uvec2(uint(cells)-1u));
  result.weight=LocalTerrainCoverage(face,level,sector,localUv);
  result.level=level;
  result.resident=true;
  return result;
}

vec3 ApplyLocalTerrainNormal(vec3 unitDirection,vec2 normalXY)
{
  uint face;vec2 faceUv;ProductionDirectionAddress(unitDirection,face,faceUv);
  const float epsilon=1e-4;
  vec3 du=normalize(ProductionSpherifyF(ProductionCubeF(face,2.0*(faceUv.x+epsilon)-1.0,2.0*faceUv.y-1.0)))-unitDirection;
  vec3 dv=normalize(ProductionSpherifyF(ProductionCubeF(face,2.0*faceUv.x-1.0,2.0*(faceUv.y+epsilon)-1.0)))-unitDirection;
  du=normalize(du);dv=normalize(dv-dot(dv,du)*du);
  float nz=sqrt(max(0.0,1.0-dot(normalXY,normalXY)));
  return normalize(du*normalXY.x+dv*normalXY.y+unitDirection*nz);
}
#endif

#endif
