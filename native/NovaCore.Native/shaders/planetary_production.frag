#version 460
#extension GL_GOOGLE_include_directive : require
#extension GL_ARB_gpu_shader_fp64 : require
#define NOVACORE_LOCAL_TERRAIN_FRAGMENT
#include "planet_material.glsl"
#include "production_cube_surface.glsl"
#include "local_terrain.glsl"
#include "production_cube_filter.glsl"
#include "production_earth_material.glsl"
#include "production_terrain_material.glsl"
layout(location=0) in vec4 color;
layout(location=1) in vec3 normal;
layout(location=2) flat in vec3 lightDirection;
layout(location=3) flat in uvec2 material;
layout(location=4) flat in vec4 response;
layout(location=5) in vec3 viewDirection;
layout(location=6) in vec3 bodyDirection;
layout(location=7) in float terrainHeight;
layout(location=8) flat in vec3 bodyCameraHigh;
layout(location=9) flat in vec3 bodyCameraLow;
layout(location=10) flat in vec4 localDetail;
layout(location=11) flat in uint productionLayer;
layout(location=12) in vec2 productionUv;
layout(std430,set=0,binding=2) readonly buffer Input { vec4 cameraHighRadiusHigh; vec4 cameraLowRadiusLow; vec4 thresholds; uvec4 controls; vec4 viewForwardHalfAngle; vec4 textureDemand; } inputData;
layout(std430,set=0,binding=12) readonly buffer EyeballInput { vec4 cameraHighRadiusHigh; vec4 cameraLowRadiusLow; vec4 surface; uvec4 identity; vec4 tangentAnchorAngle; vec4 mapping; uvec4 topology; uvec4 reserved; } eye;
layout(set=0,binding=24) uniform sampler2DArray productionAlbedo;
layout(set=0,binding=25) uniform sampler2DArray productionElevation;
layout(set=0,binding=26) uniform sampler2DArray productionLand;
layout(std430,set=0,binding=27) readonly buffer ProductionLayers { uint values[]; } productionLayers;
layout(push_constant) uniform StellarLighting { vec4 sourceCenterExposure; vec4 sourceColorAmbient; vec4 radianceGlowEnabled; } lighting;
layout(location=0) out vec4 outColor;

uint ResolveProductionFragmentLayer(
  vec3 direction,
  out vec2 localUv,
  out vec2 continuousFaceUv,
  out float levelCells)
{
  uint face;vec2 uv;ProductionDirectionAddress(direction,face,uv);
  continuousFaceUv=uv;
  uint maximumLevel=min(inputData.controls.x,3u);
  for(int signedLevel=int(maximumLevel);signedLevel>=0;signedLevel--)
  {
    uint level=uint(signedLevel),cells=1u<<level;
    uvec2 patchCoordinate=min(uvec2(floor(uv*float(cells))),uvec2(cells-1u));
    uint layer=productionLayers.values[ProductionPatchOrdinal(face,level,patchCoordinate.x,patchCoordinate.y)];
    if(layer!=0u)
    {
      levelCells=float(cells);
      localUv=clamp(uv*levelCells-vec2(patchCoordinate),vec2(0),vec2(1));
      return layer;
    }
  }
  localUv=uv;levelCells=1.0;return 0u;
}

bool ProductionEyeballOwnsDirection(vec3 unitDirection)
{
  vec3 anchor=normalize(eye.tangentAnchorAngle.xyz);
  vec3 reference=abs(anchor.y)<.95?vec3(0,1,0):vec3(1,0,0);
  vec3 east=normalize(cross(reference,anchor)),north=normalize(cross(anchor,east));
  vec3 direction=normalize(unitDirection);
  float axial=dot(direction,anchor);
  if(axial<=0.0)return false;
  vec2 gnomonic=vec2(dot(direction,east),dot(direction,north))/axial;
  float segmentCount=float(max(eye.topology.w,3u));
  float sector=6.2831853071795864769/segmentCount;
  float localAngle=mod(atan(gnomonic.y,gnomonic.x)+.5*sector,sector)-.5*sector;
  float polygonRadius=tan(eye.tangentAnchorAngle.w)*cos(.5*sector)/cos(localAngle);
  // The Eyeball outer ring is a regular polygon in gnomonic space.  Matching
  // that actual raster boundary (rather than an analytic circular cap) gives
  // the two meshes one coherent color/depth owner without exposing sawteeth.
  return length(gnomonic)<polygonRadius*(1.0-2e-6);
}

void main()
{
  bool eyeball=(productionLayer&0x80000000u)!=0u;
  if(!eyeball&&eye.identity.w!=0u&&eye.surface.w>=.99999&&ProductionEyeballOwnsDirection(bodyDirection))discard;
  vec3 unitDirection=normalize(bodyDirection);
  vec2 resolvedUv,continuousFaceUv;float levelCells;
  // Resolve both global and Eyeball fragments from the same physical sphere
  // direction.  A flat vertex layer is valid for ownership, but not for
  // filtered payload addressing across hierarchical or cube-face boundaries.
  uint resolvedLayer=ResolveProductionFragmentLayer(unitDirection,resolvedUv,continuousFaceUv,levelCells);
  if(resolvedLayer==0u)discard;
  vec3 surfaceNormal=normalize(normal);
  vec2 storedUv=(vec2(4.0)+clamp(resolvedUv,0.0,1.0)*256.0)/264.0;
  vec2 gradientX,gradientY;
  ProductionPayloadGradients(continuousFaceUv,unitDirection,levelCells,gradientX,gradientY);
  float layer=float(resolvedLayer-1u);
  float sampledHeight=textureGrad(productionElevation,vec3(storedUv,layer),gradientX,gradientY).r*20000.0-11000.0;
  vec3 sampledAlbedo=textureGrad(productionAlbedo,vec3(storedUv,layer),gradientX,gradientY).rgb;
  LocalTerrainMaterialSample localSample=SampleLocalTerrainMaterial(unitDirection);
  if(localSample.resident){sampledAlbedo=localSample.albedo;surfaceNormal=ApplyLocalTerrainNormal(unitDirection,localSample.normalXY);}
  ProductionEarthMaterial earth=ProductionEarthSurfaceMaterial(
    sampledAlbedo,
    textureGrad(productionLand,vec3(storedUv,layer),gradientX,gradientY).r,
    sampledHeight,
    response);
  float bodyRadius=inputData.cameraHighRadiusHigh.w+inputData.cameraLowRadiusLow.w;
  float surfaceAltitude=max(length(bodyCameraHigh+bodyCameraLow)-bodyRadius,0.0);
  dvec3 bodyMetres=dvec3(bodyCameraHigh)+dvec3(bodyCameraLow)-dvec3(viewDirection);
  vec3 differentialMetres=-viewDirection;
  ProductionTerrainMaterial terrainMaterial=SynthesizeProductionTerrainMaterial(
    earth.albedo,
    textureGrad(productionLand,vec3(storedUv,layer),gradientX,gradientY).r,
    sampledHeight,
    unitDirection,
    surfaceNormal,
    bodyMetres,
    differentialMetres,
    surfaceAltitude);
  earth.albedo=terrainMaterial.albedo;
  earth.roughness=mix(earth.roughness,terrainMaterial.roughness,terrainMaterial.detailWeight);
  earth.specular=mix(earth.specular,.035,terrainMaterial.detailWeight*(1.0-terrainMaterial.metallic));
  surfaceNormal=terrainMaterial.normal;
  float ambient=max(lighting.sourceColorAmbient.w,.025);
  vec3 lit=PlanetLighting(earth.albedo,surfaceNormal,lightDirection,viewDirection,earth.roughness,earth.specular,response.z*terrainMaterial.ambientOcclusion,ambient);
  outColor=vec4(lit,eyeball?eye.surface.w:1.0);
}
