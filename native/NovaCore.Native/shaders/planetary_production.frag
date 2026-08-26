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
layout(location=13) flat in uvec4 productionAddress;
layout(location=14) in vec2 productionTransition;
layout(std430,set=0,binding=2) readonly buffer Input { vec4 cameraHighRadiusHigh; vec4 cameraLowRadiusLow; vec4 thresholds; uvec4 controls; vec4 viewForwardHalfAngle; vec4 textureDemand; } inputData;
layout(std430,set=0,binding=12) readonly buffer EyeballInput { vec4 cameraHighRadiusHigh; vec4 cameraLowRadiusLow; vec4 surface; uvec4 identity; vec4 tangentAnchorAngle; vec4 mapping; uvec4 topology; uvec4 reserved; } eye;
layout(set=0,binding=24) uniform sampler2DArray productionAlbedo;
layout(set=0,binding=25) uniform sampler2DArray productionElevation;
layout(set=0,binding=26) uniform sampler2DArray productionLand;
layout(std430,set=0,binding=27) readonly buffer ProductionLayers { uint values[]; } productionLayers;
layout(push_constant) uniform StellarLighting { vec4 sourceCenterExposure; vec4 sourceColorAmbient; vec4 radianceGlowEnabled; } lighting;
layout(location=0) out vec4 outColor;

struct ProductionSurfaceSample
{
  vec3 albedo;
  float elevation;
  float land;
};

ProductionSurfaceSample SampleProductionSurface(
  uvec4 address,
  uint layer,
  vec2 localUv,
  vec3 unitDirection)
{
  ProductionSurfaceSample result;
  float cells=float(1u<<address.y);
  vec2 continuousFaceUv=(vec2(address.zw)+localUv)/cells;
  vec2 storedUv=(vec2(4.0)+clamp(localUv,0.0,1.0)*256.0)/264.0;
  vec2 gradientX,gradientY;
  ProductionPayloadGradients(continuousFaceUv,unitDirection,cells,gradientX,gradientY);
  float payloadLayer=float(layer-1u);
  result.albedo=textureGrad(productionAlbedo,vec3(storedUv,payloadLayer),gradientX,gradientY).rgb;
  result.elevation=textureGrad(productionElevation,vec3(storedUv,payloadLayer),gradientX,gradientY).r*20000.0-11000.0;
  result.land=textureGrad(productionLand,vec3(storedUv,payloadLayer),gradientX,gradientY).r;
  return result;
}

float MixedLodEdgeWeight(vec2 localUv,uint stitchMask)
{
  const float width=2.0/16.0;
  float weight=1.0;
  if((stitchMask&1u)!=0u)weight=min(weight,smoothstep(0.0,width,localUv.x));
  if((stitchMask&2u)!=0u)weight=min(weight,smoothstep(0.0,width,1.0-localUv.x));
  if((stitchMask&4u)!=0u)weight=min(weight,smoothstep(0.0,width,localUv.y));
  if((stitchMask&8u)!=0u)weight=min(weight,smoothstep(0.0,width,1.0-localUv.y));
  return weight;
}

bool ProductionEyeballVisibleFromCamera(vec3 unitDirection,float representedHeight)
{
  dvec3 direction=normalize(dvec3(unitDirection));
  dvec3 cameraBody=dvec3(bodyCameraHigh)+dvec3(bodyCameraLow);
  double bodyRadius=double(inputData.cameraHighRadiusHigh.w)+double(inputData.cameraLowRadiusLow.w);
  dvec3 surfacePoint=direction*(bodyRadius+double(max(representedHeight,eye.surface.z)));
  // The persistent Eye intentionally contains a much larger body-fixed cap
  // than is visible at orbital altitude.  Rasterization is two-sided, so a
  // geometric visibility test is required before those far-side triangles
  // can contribute color or depth.  The cube-sphere remains the opaque parent
  // for every rejected fragment.
  return dot(cameraBody-surfacePoint,direction)>0.0;
}

uint ResolveProductionFragmentLayer(vec3 unitDirection,out vec2 localUv,out uvec4 address)
{
  // A radial Eye triangle is not confined to one cube payload chart.  Its
  // vertices can cross an LOD page edge or even a cube-face edge, so a flat
  // provoking-vertex address paired with interpolated UVs is not a coherent
  // surface coordinate.  Resolve the chart from the interpolated body-fixed
  // direction at the fragment; global cube patches retain their transactional
  // per-instance address below.
  uint face;vec2 uv;ProductionDirectionAddress(unitDirection,face,uv);
  uint maximumLevel=min(inputData.controls.x,3u);
  for(int signedLevel=int(maximumLevel);signedLevel>=0;signedLevel--)
  {
    uint level=uint(signedLevel),cells=1u<<level;
    uvec2 patchCoordinate=min(uvec2(floor(uv*float(cells))),uvec2(cells-1u));
    uint layer=productionLayers.values[ProductionPatchOrdinal(face,level,patchCoordinate.x,patchCoordinate.y)];
    if(layer!=0u)
    {
      localUv=clamp(uv*float(cells)-vec2(patchCoordinate),vec2(0),vec2(1));
      address=uvec4(face,level,patchCoordinate);
      return layer;
    }
  }
  localUv=uv;address=uvec4(face,0u,0u,0u);return 0u;
}

void main()
{
  bool eyeball=(productionLayer&0x80000000u)!=0u;
  // The persistent cube-sphere remains the opaque coverage/depth parent while
  // the local Eyeball refines it.  Discarding the parent over the Eyeball's
  // analytic cap is not a valid ownership transaction: portions of the large
  // radial mesh can be beyond the geometric horizon (or clipped) and therefore
  // cannot prove pixel coverage for the region being discarded.  Keeping the
  // parent lets the depth test select only genuinely represented Eyeball
  // fragments and prevents transient globe openings during approach.
  vec3 unitDirection=normalize(bodyDirection);
  if(eyeball&&!ProductionEyeballVisibleFromCamera(unitDirection,terrainHeight))discard;
  uint resolvedLayer=productionLayer&0x7fffffffu;
  vec2 resolvedUv=productionUv;
  uvec4 resolvedAddress=productionAddress;
  if(eyeball)resolvedLayer=ResolveProductionFragmentLayer(unitDirection,resolvedUv,resolvedAddress);
  if(resolvedLayer==0u)discard;
  vec3 surfaceNormal=normalize(normal);
  ProductionSurfaceSample visible=SampleProductionSurface(resolvedAddress,resolvedLayer,resolvedUv,unitDirection);
  if(!eyeball&&productionAddress.y>0u)
  {
    float surfaceWeight=productionTransition.x*MixedLodEdgeWeight(productionUv,uint(round(productionTransition.y)));
    if(surfaceWeight<.999999)
    {
      uvec4 parentAddress=uvec4(productionAddress.x,productionAddress.y-1u,productionAddress.z>>1u,productionAddress.w>>1u);
      uint parentLayer=productionLayers.values[ProductionPatchOrdinal(parentAddress.x,parentAddress.y,parentAddress.z,parentAddress.w)];
      if(parentLayer==0u)discard;
      float parentCells=float(1u<<parentAddress.y),childCells=float(1u<<productionAddress.y);
      vec2 continuousFaceUv=(vec2(productionAddress.zw)+productionUv)/childCells;
      vec2 parentUv=clamp(continuousFaceUv*parentCells-vec2(parentAddress.zw),vec2(0),vec2(1));
      ProductionSurfaceSample parent=SampleProductionSurface(parentAddress,parentLayer,parentUv,unitDirection);
      visible.albedo=mix(parent.albedo,visible.albedo,surfaceWeight);
      visible.elevation=mix(parent.elevation,visible.elevation,surfaceWeight);
      visible.land=mix(parent.land,visible.land,surfaceWeight);
    }
  }
  float sampledHeight=visible.elevation;
  vec3 sampledAlbedo=visible.albedo;
  LocalTerrainMaterialSample localSample=SampleLocalTerrainMaterial(unitDirection);
  if(eyeball&&localSample.resident){sampledAlbedo=mix(sampledAlbedo,localSample.albedo,localSample.weight);surfaceNormal=normalize(mix(surfaceNormal,ApplyLocalTerrainNormal(unitDirection,localSample.normalXY),localSample.weight));}
  ProductionEarthMaterial earth=ProductionEarthSurfaceMaterial(
    sampledAlbedo,
    visible.land,
    sampledHeight,
    response);
  float bodyRadius=inputData.cameraHighRadiusHigh.w+inputData.cameraLowRadiusLow.w;
  float surfaceAltitude=max(length(bodyCameraHigh+bodyCameraLow)-bodyRadius,0.0);
  dvec3 bodyMetres=dvec3(bodyCameraHigh)+dvec3(bodyCameraLow)-dvec3(viewDirection);
  vec3 differentialMetres=-viewDirection;
  ProductionTerrainMaterial terrainMaterial=SynthesizeProductionTerrainMaterial(
    earth.albedo,
    visible.land,
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
