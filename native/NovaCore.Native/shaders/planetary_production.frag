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
layout(location=15) in vec2 topologyCoordinate;
layout(std430,set=0,binding=2) readonly buffer Input { vec4 cameraHighRadiusHigh; vec4 cameraLowRadiusLow; vec4 thresholds; uvec4 controls; vec4 viewForwardHalfAngle; vec4 textureDemand; } inputData;
layout(std430,set=0,binding=12) readonly buffer EyeballInput { vec4 cameraHighRadiusHigh; vec4 cameraLowRadiusLow; vec4 surface; uvec4 identity; vec4 tangentAnchorAngle; vec4 mapping; uvec4 topology; uvec4 reserved; uvec4 anchoredAddress; } eye;
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

vec3 ProductionPhysicalNormal(vec3 unitDirection,float elevationMetres,float radiusMetres)
{
  // Reconstruct the displaced-surface differential in fragment space.  The
  // radial base is analytic, so a constant-height ocean remains perfectly
  // smooth instead of exposing the coarse triangle/patch interpolation in a
  // tight specular highlight.  Height derivatives come from the same
  // transactionally blended, gutter-filtered payload used for color.
  vec3 radial=normalize(unitDirection);
  vec3 sphereMetres=radial*radiusMetres;
  vec3 sigmaX=dFdx(sphereMetres),sigmaY=dFdy(sphereMetres);
  vec3 r1=cross(sigmaY,radial),r2=cross(radial,sigmaX);
  float determinant=dot(sigmaX,r1);
  if(abs(determinant)<1e-12||isnan(determinant)||isinf(determinant))return radial;
  vec3 gradient=sign(determinant)*(dFdx(elevationMetres)*r1+dFdy(elevationMetres)*r2);
  vec3 candidate=normalize(abs(determinant)*radial-gradient);
  if(any(isnan(candidate))||any(isinf(candidate)))return radial;
  return dot(candidate,radial)<0.0?-candidate:candidate;
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

bool ProductionEyeballOwnsVisibleDirection(vec3 unitDirection,float representedHeight)
{
  if(eye.identity.w==0u||eye.surface.w<.999999)return false;
  vec3 direction=normalize(unitDirection),anchor=normalize(eye.tangentAnchorAngle.xyz);
  vec3 reference=abs(anchor.y)<.95?vec3(0,1,0):vec3(1,0,0);
  vec3 east=normalize(cross(reference,anchor)),north=normalize(cross(anchor,east));
  float turns=atan(dot(direction,north),dot(direction,east))/6.2831853071795864769;
  turns=turns-floor(turns);
  uint segments=max(eye.topology.z,3u);
  uint segment=min(uint(floor(turns*float(segments))),segments-1u);
  float azimuth0=6.2831853071795864769*float(segment)/float(segments);
  float azimuth1=6.2831853071795864769*float((segment+1u)%segments)/float(segments);
  float capAngle=eye.tangentAnchorAngle.w;
  vec3 edge0=normalize(anchor*cos(capAngle)+(east*cos(azimuth0)+north*sin(azimuth0))*sin(capAngle));
  vec3 edge1=normalize(anchor*cos(capAngle)+(east*cos(azimuth1)+north*sin(azimuth1))*sin(capAngle));
  vec3 edgePlane=cross(edge0,edge1);
  // The outer Eye ring is a spherical polygon, not the analytic small circle
  // implied by maximumAngleRadians.  Classifying against the exact outer
  // topology makes every fully promoted pixel exclusive: the Eye owns the
  // inside (including its edge), and the cube remains authoritative outside.
  bool insidePolygon=dot(edgePlane,direction)*dot(edgePlane,anchor)>=0.0;
  return insidePolygon&&ProductionEyeballVisibleFromCamera(direction,representedHeight);
}

bool ProductionAnchoredOwnsDirection(vec3 unitDirection)
{
  if((eye.anchoredAddress.w&0x80000000u)==0u)return false;
  uvec4 address=uvec4(eye.anchoredAddress.xyz,eye.anchoredAddress.w&0x7fffffffu);
  address=uvec4(address.x,address.y,address.z,address.w);
  vec3 direction=normalize(unitDirection),center=ProductionProjectF(address,vec2(.5));
  for(uint side=0u;side<4u;side++)for(uint segment=0u;segment<4u;segment++)
  {
    float a=float(segment)*.25,b=float(segment+1u)*.25;
    vec2 local0=side==0u?vec2(a,0):side==1u?vec2(1,a):side==2u?vec2(1-a,1):vec2(0,1-a);
    vec2 local1=side==0u?vec2(b,0):side==1u?vec2(1,b):side==2u?vec2(1-b,1):vec2(0,1-b);
    vec3 edge0=ProductionProjectF(address,local0),edge1=ProductionProjectF(address,local1),plane=cross(edge0,edge1);
    if(dot(plane,direction)*dot(plane,center)<0.0)return false;
  }
  return true;
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
  // The persistent cube-sphere remains the opaque coverage/depth parent until
  // native promotion and CPU visible-footprint proof are both complete.  Only
  // then does the Eye exclusively own front-visible pixels inside its analytic
  // cap, avoiding both transient holes and depth competition between the two
  // distinct triangulations.
  vec3 unitDirection=normalize(bodyDirection);
  if(ProductionAnchoredOwnsDirection(unitDirection))discard;
  if(eyeball&&!ProductionEyeballVisibleFromCamera(unitDirection,terrainHeight))discard;
  if(!eyeball&&ProductionEyeballOwnsVisibleDirection(unitDirection,terrainHeight))discard;
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
  float bodyRadius=inputData.cameraHighRadiusHigh.w+inputData.cameraLowRadiusLow.w;
  if(!eyeball)
  {
    vec3 physical=ProductionPhysicalNormal(unitDirection,visible.elevation,bodyRadius);
    // The terrain payload contains bathymetry, not an implemented water
    // displacement surface.  Keep current ocean shading on the analytic sea
    // level normal and blend continuously through the land mask so quantized
    // sub-sea elevation cannot become high-frequency specular noise.
    surfaceNormal=normalize(mix(unitDirection,physical,smoothstep(.45,.55,visible.land)));
  }
  LocalTerrainMaterialSample localSample=SampleLocalTerrainMaterial(unitDirection);
  if(eyeball&&localSample.resident){sampledAlbedo=mix(sampledAlbedo,localSample.albedo,localSample.weight);surfaceNormal=normalize(mix(surfaceNormal,ApplyLocalTerrainNormal(unitDirection,localSample.normalXY),localSample.weight));}
  ProductionEarthMaterial earth=ProductionEarthSurfaceMaterial(
    sampledAlbedo,
    visible.land,
    sampledHeight,
    response);
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
  uint diagnostic=floatBitsToUint(lighting.radianceGlowEnabled.w)>>16;
  if((diagnostic&32u)!=0u){outColor=vec4(eyeball?vec3(0.05,0.9,0.2):vec3(0.9,0.08,0.05),1.0);return;}
  if((diagnostic&16u)!=0u){outColor=vec4(surfaceNormal*.5+.5,1.0);return;}
  if((diagnostic&8u)!=0u){outColor=vec4(earth.albedo,1.0);return;}
  if((diagnostic&64u)!=0u)
  {
    float boundary;
    if(eyeball)
    {
      float ringDistance=abs(fract(topologyCoordinate.x*float(max(eye.topology.y,1u)))-.5);
      float spokeDistance=abs(fract(topologyCoordinate.y*float(max(eye.topology.z,1u)))-.5);
      boundary=1.0-smoothstep(.46,.495,max(ringDistance,spokeDistance));
    }
    else
    {
      vec2 edgeDistance=min(topologyCoordinate,1.0-topologyCoordinate);
      float patchDistance=min(edgeDistance.x,edgeDistance.y);
      float pixelWidth=max(fwidth(topologyCoordinate.x),fwidth(topologyCoordinate.y));
      boundary=1.0-smoothstep(0.0,pixelWidth*1.5,patchDistance);
      uint cells=1u<<productionAddress.y;
      bool faceEdge=(productionAddress.z==0u&&topologyCoordinate.x<pixelWidth*1.5)||(productionAddress.z+1u==cells&&1.0-topologyCoordinate.x<pixelWidth*1.5)||(productionAddress.w==0u&&topologyCoordinate.y<pixelWidth*1.5)||(productionAddress.w+1u==cells&&1.0-topologyCoordinate.y<pixelWidth*1.5);
      if(faceEdge){outColor=vec4(0.0,1.0,1.0,1.0);return;}
    }
    outColor=vec4(mix(earth.albedo,eyeball?vec3(0.1,1.0,0.2):vec3(1.0,.15,.05),boundary),1.0);return;
  }
  if((diagnostic&128u)!=0u){float depth=clamp(-log2(max(gl_FragCoord.z,1e-20))/64.0,0.0,1.0);outColor=vec4(vec3(depth),1.0);return;}
  if((diagnostic&256u)!=0u){vec3 faces[6]=vec3[6](vec3(1,.1,.1),vec3(.1,1,.1),vec3(.1,.3,1),vec3(1,1,.1),vec3(1,.1,1),vec3(.1,1,1));float parity=float((resolvedAddress.z^resolvedAddress.w)&1u)*.22;outColor=vec4(faces[resolvedAddress.x]*(.62+.1*float(resolvedAddress.y)+parity),1.0);return;}
  float ambient=max(lighting.sourceColorAmbient.w,.025);
  if((diagnostic&512u)!=0u){float diffuse=max(dot(normalize(surfaceNormal),normalize(lightDirection)),0.0);outColor=vec4(earth.albedo*mix(ambient,1.0,diffuse),1.0);return;}
  vec3 lit=PlanetLighting(earth.albedo,surfaceNormal,lightDirection,viewDirection,earth.roughness,earth.specular,response.z*terrainMaterial.ambientOcclusion,ambient);
  outColor=vec4(lit,eyeball?eye.surface.w:1.0);
}
