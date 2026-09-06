#version 460
#extension GL_GOOGLE_include_directive : require
#extension GL_ARB_gpu_shader_fp64 : require
// Depth tests precede material evaluation for the sole published physical owner.
layout(early_fragment_tests) in;
#include "planet_material.glsl"
#include "facility_light_occlusion.glsl"
#include "production_cube_surface.glsl"
#include "physical_surface.glsl"
#include "production_cube_filter.glsl"
#include "production_earth_material.glsl"
#include "production_terrain_material.glsl"
#include "surface_material_coordinates.glsl"
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
  // The terrain-v5 elevation channel remains presentation/classification data.
  // Physical geometry and physical normals are prepared upstream from the
  // canonical oracle. Explicit LOD0 keeps this legacy material input stable.
  result.elevation=textureLod(productionElevation,vec3(storedUv,payloadLayer),0.0).r*20000.0-11000.0;
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

dvec3 ProductionRaySpherePosition(vec3 fallbackDirection,float representedHeight,double radiusMetres)
{
  // Perspective-correct interpolation gives the fragment view ray even when
  // the rasterized owner is a coarse planar triangle. Intersect that ray with
  // the represented radial shell to obtain a topology-independent analytic
  // sphere direction for the diagnostic comparison.
  dvec3 camera=dvec3(bodyCameraHigh)+dvec3(bodyCameraLow);
  dvec3 ray=normalize(-dvec3(viewDirection));
  double shell=radiusMetres+double(representedHeight);
  double b=dot(camera,ray),c=dot(camera,camera)-shell*shell;
  double discriminant=b*b-c;
  if(discriminant<0.0||isnan(discriminant)||isinf(discriminant))
    return dvec3(normalize(fallbackDirection))*shell;
  double root=sqrt(discriminant),distance=-b-root;
  if(distance<=0.0)distance=-b+root;
  dvec3 point=camera+ray*distance;
  if(distance<=0.0||any(isnan(point))||any(isinf(point)))
    return dvec3(normalize(fallbackDirection))*shell;
  return point;
}

vec3 ProductionRaySphereDirection(vec3 fallbackDirection,float representedHeight,double radiusMetres)
{
  return normalize(vec3(ProductionRaySpherePosition(fallbackDirection,representedHeight,radiusMetres)));
}

uint ResolveProductionFragmentLayerAtOrBelow(vec3 unitDirection,uint requestedLevel,out vec2 localUv,out uvec4 address)
{
  // A rendered triangle is not physical addressing authority. Anchored
  // triangles can cross payload charts, while even a global relaxed-
  // cube triangle only linearly interpolates its patch-local coordinates.
  // Resolve the chart from the analytic body-fixed fragment direction. The
  // requested-level bound below still preserves the global patch's current
  // parent/child transaction rather than independently outrunning geometry.
  uint face;vec2 uv;ProductionDirectionAddress(unitDirection,face,uv);
  uint maximumLevel=min(min(requestedLevel,inputData.controls.x),3u);
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

uint ResolveProductionFragmentLayer(vec3 unitDirection,out vec2 localUv,out uvec4 address)
{
  return ResolveProductionFragmentLayerAtOrBelow(unitDirection,inputData.controls.x,localUv,address);
}

void main()
{
  bool anchored=(productionLayer&0x40000000u)!=0u;
  // NCSM1 and startup terrain-v5 are mutually exclusive publication owners.
  vec3 unitDirection=normalize(bodyDirection);
  uint diagnostic=floatBitsToUint(lighting.radianceGlowEnabled.w)>>16;
  // Ownership visualization is purely topological. Keep it ahead of physical
  // payload and material evaluation so this diagnostic also isolates geometry
  // submission/raster cost at production resolution.
  if((diagnostic&32u)!=0u)
  {
    uint hash=productionAddress.x*0x9e3779b9u^productionAddress.y*0x85ebca6bu^
      productionAddress.z*0xc2b2ae35u^productionAddress.w*0x27d4eb2fu;
    hash^=hash>>16u;
    float identity=.55+.45*float(hash&255u)/255.0;
    float lod=.35+.65*clamp(float(productionAddress.y)/20.0,0.0,1.0);
    outColor=vec4(anchored?vec3(.04,.20+.25*lod,.68*identity+.30):vec3(.68*identity+.30,.04,.04+.08*lod),1.0);
    return;
  }
  double bodyRadiusMetres=double(inputData.cameraHighRadiusHigh.w)+double(inputData.cameraLowRadiusLow.w);
  float bodyRadius=float(bodyRadiusMetres);
  // The global mesh's patch-local UV is a topology coordinate, not physical
  // surface authority.  Perspective interpolation across a planar relaxed-
  // cube triangle does not equal the body-fixed direction hit by the fragment
  // view ray, and its derivative changes semantics at every patch/cube edge.
  // Resolve every owner's payload address from the same analytic body-fixed
  // direction while retaining the global patch's active level.  This removes
  // a one-pixel material/filter discontinuity without changing geometry,
  // promotion, residency, or ownership.
  // Address every payload from one representation-independent camera ray.
  // Using the owner's interpolated terrainHeight here made the global chord
  // and dynamic mesh intersect different radial shells before either had
  // resolved the authoritative payload. Sea level is only an addressing
  // shell; the final physical point is reconstructed below from the resolved
  // height transaction.
  // The production spherical billboard is already one body-fixed physical
  // owner. Its prepared/TES direction is therefore the material-coordinate
  // authority too. Re-intersecting the fragment ray with a sea-level sphere
  // can select a different chart at grazing angles and turn valid geometry
  // into a payload miss. Retain the analytic addressing shell only for the
  // legacy non-anchored material path.
  vec3 samplingDirection=anchored
    ?unitDirection
    :ProductionRaySphereDirection(unitDirection,0.0,bodyRadius);
  vec2 resolvedUv;uvec4 resolvedAddress;
  uint resolvedLayer=anchored
    ?ResolveProductionFragmentLayer(samplingDirection,resolvedUv,resolvedAddress)
    :ResolveProductionFragmentLayerAtOrBelow(samplingDirection,productionAddress.y,resolvedUv,resolvedAddress);
  if(resolvedLayer==0u)discard;
  vec3 surfaceNormal=normalize(normal);
  ProductionSurfaceSample visible=SampleProductionSurface(resolvedAddress,resolvedLayer,resolvedUv,samplingDirection);
  if((diagnostic&8u)!=0u){outColor=vec4(visible.albedo,1.0);return;}
  if(!anchored&&resolvedAddress.y>0u)
  {
    float surfaceWeight=productionTransition.x*MixedLodEdgeWeight(productionUv,uint(round(productionTransition.y)));
    if(surfaceWeight<.999999)
    {
      uvec4 parentAddress=uvec4(resolvedAddress.x,resolvedAddress.y-1u,resolvedAddress.z>>1u,resolvedAddress.w>>1u);
      uint parentLayer=productionLayers.values[ProductionPatchOrdinal(parentAddress.x,parentAddress.y,parentAddress.z,parentAddress.w)];
      if(parentLayer==0u)discard;
      float parentCells=float(1u<<parentAddress.y),childCells=float(1u<<resolvedAddress.y);
      vec2 continuousFaceUv=(vec2(resolvedAddress.zw)+resolvedUv)/childCells;
      vec2 parentUv=clamp(continuousFaceUv*parentCells-vec2(parentAddress.zw),vec2(0),vec2(1));
      ProductionSurfaceSample parent=SampleProductionSurface(parentAddress,parentLayer,parentUv,samplingDirection);
      visible.albedo=mix(parent.albedo,visible.albedo,surfaceWeight);
      visible.elevation=mix(parent.elevation,visible.elevation,surfaceWeight);
      visible.land=mix(parent.land,visible.land,surfaceWeight);
    }
  }
  float globalHeight=max(0.0,visible.elevation);
  float baseHeight=max(0.0,visible.elevation);
  // Geometry/TES has already produced physical height and position. Fragment
  // presentation must not reconstruct modifiers; it consumes the geographic
  // base height only for its independent FP32 material classification.
  ProductionTerrainWeights presentationWeights=EvaluatePresentationBiomeWeightsF(samplingDirection,baseHeight,visible.land);
  float sampledHeight=max(0.0,terrainHeight);
  vec3 sampledAlbedo=visible.albedo;
  if((diagnostic&32768u)!=0u)
  {
    // Physical diagnostics retain the complete FP64 evaluator, outside the
    // production material path.
    PhysicalModifierEvaluationD modifierEvaluation=EvaluateTerrainModifiersD(dvec3(samplingDirection),double(baseHeight));
    uint mode=(diagnostic>>8u)&7u;
    if(mode==0u){float value=clamp((globalHeight+1000.0)/5000.0,0.0,1.0);outColor=vec4(vec3(value),1.0);return;}
    if(mode==1u){float value=clamp(.5+float(modifierEvaluation.tiledHeight+modifierEvaluation.erosionHeight+modifierEvaluation.mesoHeight)/96.0,0.0,1.0);outColor=vec4(value,.2,1.0-value,1.0);return;}
    if(mode==2u){float value=clamp((visible.elevation+1000.0)/5000.0,0.0,1.0);outColor=vec4(vec3(value),1.0);return;}
    if(mode==3u){outColor=vec4(normalize(normal)*.5+.5,1.0);return;}
    if(mode==4u)
    {
      const vec3 colors[10]=vec3[10](vec3(0,.14,.35),vec3(.82,.68,.34),vec3(.15,.34,.25),vec3(.18,.48,.10),vec3(.48,.42,.20),vec3(.72,.45,.16),vec3(.34,.32,.30),vec3(.48,.48,.46),vec3(.82,.88,.94),vec3(.54,.56,.58));
      outColor=vec4(colors[modifierEvaluation.biomes.ids.x],1);return;
    }
    if(mode==5u){outColor=vec4(vec3(modifierEvaluation.biomes.weights.xyz),1);return;}
    if(mode==6u){float id=float(modifierEvaluation.dominantId)/9.0;outColor=vec4(id,1.0-id,.2,1);return;}
    float nearValue=clamp(.5+float(modifierEvaluation.nearHeight)/(2.0*float(NOVACORE_NEAR_AMPLITUDE)),0.0,1.0);
    outColor=vec4(nearValue,.2,1.0-nearValue,1);return;
  }
  // Complete physical terrain owns its material receiver. The legacy radial
  // shell exists only for the bootstrap/global compatibility representation;
  // re-intersecting it for NCSM1 slides material identity with the camera and
  // evaluates filtering/normal derivatives on a surface that was never drawn.
  float landWeight=smoothstep(.45,.55,visible.land);
  float representedHeight=mix(0.0,visible.elevation,landWeight);
  dvec3 bodyMetres=anchored
    ?SurfaceMaterialBodyPosition(bodyCameraHigh,bodyCameraLow,-viewDirection)
    :ProductionRaySpherePosition(samplingDirection,representedHeight,bodyRadiusMetres);
  vec3 analyticSphere=normalize(vec3(bodyMetres));
  vec3 physical=normalize(normal);
  if((diagnostic&8192u)!=0u){outColor=vec4(normalize(mix(unitDirection,physical,smoothstep(.45,.55,visible.land)))*.5+.5,1.0);return;}
  // The terrain payload contains bathymetry, not an implemented water
  // displacement surface. Keep current ocean shading on the analytic sea
  // level normal and blend continuously through the land mask so quantized
  // sub-sea elevation cannot become high-frequency specular noise.
  surfaceNormal=normalize(mix(analyticSphere,physical,landWeight));
  ProductionEarthMaterial earth=ProductionEarthSurfaceMaterial(
    sampledAlbedo,
    visible.land,
    sampledHeight,
    response);
  float surfaceAltitude=max(length(bodyCameraHigh+bodyCameraLow)-bodyRadius,0.0);
  dvec3 cameraBodyMetres=dvec3(bodyCameraHigh)+dvec3(bodyCameraLow);
  vec3 differentialMetres=anchored?-viewDirection:vec3(bodyMetres-cameraBodyMetres);
  if((diagnostic&130u)==130u)
  {
    float footprint=TerrainWorldFootprintMetres(differentialMetres);
    float encoded=clamp(log2(max(footprint,1e-5))/24.0+.5,0.0,1.0);
    outColor=vec4(encoded,1.0-abs(encoded-.5)*2.0,1.0-encoded,1.0);
    return;
  }
  ProductionTerrainMaterial terrainMaterial=SynthesizeProductionTerrainMaterial(
    earth.albedo,
    visible.land,
    sampledHeight,
    analyticSphere,
    surfaceNormal,
    bodyMetres,
    differentialMetres,
    surfaceAltitude,
    -1.0,
    presentationWeights);
  earth.albedo=terrainMaterial.albedo;
  earth.roughness=mix(earth.roughness,terrainMaterial.roughness,terrainMaterial.detailWeight);
  earth.specular=mix(earth.specular,.035,terrainMaterial.detailWeight*(1.0-terrainMaterial.metallic));
  surfaceNormal=terrainMaterial.normal;
  if((diagnostic&2048u)!=0u)surfaceNormal=unitDirection;
  if((diagnostic&4096u)!=0u)surfaceNormal=analyticSphere;
  if((diagnostic&16384u)!=0u){vec3 screenDerivativePhysical=ProductionPhysicalNormal(analyticSphere,visible.elevation,bodyRadius);surfaceNormal=normalize(mix(analyticSphere,screenDerivativePhysical,smoothstep(.45,.55,visible.land)));}
  if((diagnostic&16u)!=0u){outColor=vec4(surfaceNormal*.5+.5,1.0);return;}
  if((diagnostic&8u)!=0u){outColor=vec4(earth.albedo,1.0);return;}
  if((diagnostic&64u)!=0u)
  {
    float boundary;
    vec2 edgeDistance=min(topologyCoordinate,1.0-topologyCoordinate);
    float patchDistance=min(edgeDistance.x,edgeDistance.y);
    float pixelWidth=max(fwidth(topologyCoordinate.x),fwidth(topologyCoordinate.y));
    boundary=1.0-smoothstep(0.0,pixelWidth*1.5,patchDistance);
    uint cells=1u<<productionAddress.y;
    bool faceEdge=(productionAddress.z==0u&&topologyCoordinate.x<pixelWidth*1.5)||(productionAddress.z+1u==cells&&1.0-topologyCoordinate.x<pixelWidth*1.5)||(productionAddress.w==0u&&topologyCoordinate.y<pixelWidth*1.5)||(productionAddress.w+1u==cells&&1.0-topologyCoordinate.y<pixelWidth*1.5);
    if(faceEdge){outColor=vec4(0.0,1.0,1.0,1.0);return;}
    outColor=vec4(mix(earth.albedo,vec3(1.0,.15,.05),boundary),1.0);return;
  }
  if((diagnostic&128u)!=0u){float depth=clamp(-log2(max(gl_FragCoord.z,1e-20))/64.0,0.0,1.0);outColor=vec4(vec3(depth),1.0);return;}
  if((diagnostic&256u)!=0u){uvec4 address=resolvedAddress;uint hash=address.x*0x9e3779b9u^address.y*0x85ebca6bu^address.z*0xc2b2ae35u^address.w*0x27d4eb2fu;hash^=hash>>16u;vec3 identityColor=vec3(float((hash>>0u)&255u),float((hash>>8u)&255u),float((hash>>16u)&255u))/255.0;outColor=vec4(.2+.8*identityColor,1.0);return;}
  float ambient=max(lighting.sourceColorAmbient.w,.025);
  if((diagnostic&512u)!=0u){float diffuse=max(dot(normalize(surfaceNormal),normalize(lightDirection)),0.0);outColor=vec4(earth.albedo*mix(ambient,1.0,diffuse),1.0);return;}
  if((diagnostic&1024u)!=0u)earth.specular=0.0;
  vec3 lit=PlanetLighting(earth.albedo,surfaceNormal,lightDirection,viewDirection,earth.roughness,earth.specular,response.z*terrainMaterial.ambientOcclusion,ambient);
  uint facilityTests;bool facilityEntered;
  float facilitySunVisibility=AuthoredFacilitySunVisibility(-viewDirection,lightDirection,facilityTests,facilityEntered);
  lit=FacilityVisibleLighting(lit,earth.albedo,ambient,response.z*terrainMaterial.ambientOcclusion,facilitySunVisibility);
  outColor=vec4(lit,1.0);
}
