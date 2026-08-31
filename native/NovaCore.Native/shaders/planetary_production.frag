#version 460
#extension GL_GOOGLE_include_directive : require
#extension GL_ARB_gpu_shader_fp64 : require
// Published anchored geometry is payload-complete. Run depth/stencil before
// this shared material shader so terrain-v5 fill pixels already owned by the
// anchored raster never execute the expensive fallback material path.
layout(early_fragment_tests) in;
#define NOVACORE_LOCAL_TERRAIN_FRAGMENT
#include "planet_material.glsl"
#include "production_cube_surface.glsl"
#include "local_terrain.glsl"
#include "physical_surface.glsl"
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
layout(set=0,binding=24) uniform sampler2DArray productionAlbedo;
layout(set=0,binding=25) uniform sampler2DArray productionElevation;
layout(set=0,binding=26) uniform sampler2DArray productionLand;
layout(std430,set=0,binding=27) readonly buffer ProductionLayers { uint values[]; } productionLayers;
layout(std430,set=0,binding=32) readonly buffer AnchoredCoverage { uvec4 control; uvec4 entries[]; } anchoredCoverage;
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
  // Elevation is physical authority, not a presentation-frequency channel.
  // The production array has one mip; explicit LOD0 preserves its bilinear
  // body-fixed field without anisotropic, screen-footprint-dependent taps.
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

bool ProductionAnchoredOwnsDirection(vec3 unitDirection)
{
  if(anchoredCoverage.control.x==0u||anchoredCoverage.control.w==0u)return false;
  uint face;vec2 uv;ProductionDirectionAddress(normalize(unitDirection),face,uv);
  for(int signedLevel=int(anchoredCoverage.control.y);signedLevel>=0;signedLevel--)
  {
    uint level=uint(signedLevel),cells=1u<<level;
    uvec2 coordinate=min(uvec2(floor(uv*float(cells))),uvec2(cells-1u));
    uint key=0x80000000u|face|(level<<3u),slot=(key*0x9e3779b9u^coordinate.x*0x85ebca6bu^coordinate.y*0xc2b2ae35u);
    slot^=slot>>16u;slot&=anchoredCoverage.control.w-1u;
    for(uint probe=0u;probe<anchoredCoverage.control.w;probe++,slot=(slot+1u)&(anchoredCoverage.control.w-1u))
    {
      uvec4 entry=anchoredCoverage.entries[slot];
      if(entry.x==0u)break;
      if(entry.x==key&&entry.y==coordinate.x&&entry.z==coordinate.y&&entry.w==anchoredCoverage.control.z)return true;
    }
  }
  return false;
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

float ProductionFixedElevation(vec3 direction)
{
  vec2 localUv;uvec4 address;
  uint layer=ResolveProductionFragmentLayer(normalize(direction),localUv,address);
  if(layer==0u)return 0.0;
  vec2 storedUv=(vec2(4.0)+clamp(localUv,0.0,1.0)*256.0)/264.0;
  double base=max(0.0,double(textureLod(productionElevation,vec3(storedUv,float(layer-1u)),0.0).r*20000.0-11000.0+
    LocalTerrainElevationResidual(direction)));
  return float(max(0.0,base+TerrainModifierHeightD(dvec3(direction),base)));
}

vec3 ProductionFixedPhysicalNormal(vec3 unitDirection,float radiusMetres)
{
  // Resolve the authoritative global payload level at this body-fixed point
  // and differentiate at its physical source spacing. The resulting normal
  // is invariant under camera distance, projected pixel footprint and mesh
  // tier; local NCCUBE2 normal refinement is still applied afterwards.
  dvec3 radial=normalize(dvec3(unitDirection));
  vec2 centerUv;uvec4 centerAddress;
  uint centerLayer=ResolveProductionFragmentLayer(vec3(radial),centerUv,centerAddress);
  if(centerLayer==0u)return vec3(radial);
  double sourceTexels=256.0*double(1u<<centerAddress.y);
  double sampleRadiusMetres=double(radiusMetres)*1.57079632679489661923/sourceTexels;
  // Use the canonical body-fixed geographic frame.  The former arbitrary
  // reference-axis switch at abs(Y)=0.95 rotated this finite-difference
  // stencil by roughly ninety degrees along a body-fixed latitude ring.  A
  // coarse but continuous elevation field then produced a visible lighting
  // facet at that ring.  Longitude is singular only at the mathematical pole;
  // keep that unavoidable fallback there instead of introducing a mid-latitude
  // frame discontinuity.
  double horizontalSquared=radial.x*radial.x+radial.z*radial.z;
  dvec3 east=horizontalSquared>1e-24
    ?dvec3(radial.z,0.0,-radial.x)/sqrt(horizontalSquared)
    :dvec3(1.0,0.0,0.0);
  dvec3 north=normalize(cross(radial,east));
  double angle=sampleRadiusMetres/double(radiusMetres);
  dvec3 leftDirection=normalize(radial-east*angle),rightDirection=normalize(radial+east*angle);
  dvec3 downDirection=normalize(radial-north*angle),upDirection=normalize(radial+north*angle);
  dvec3 left=leftDirection*(double(radiusMetres)+double(ProductionFixedElevation(vec3(leftDirection))));
  dvec3 right=rightDirection*(double(radiusMetres)+double(ProductionFixedElevation(vec3(rightDirection))));
  dvec3 down=downDirection*(double(radiusMetres)+double(ProductionFixedElevation(vec3(downDirection))));
  dvec3 up=upDirection*(double(radiusMetres)+double(ProductionFixedElevation(vec3(upDirection))));
  vec3 candidate=normalize(vec3(cross(right-left,up-down)));
  if(any(isnan(candidate))||any(isinf(candidate)))return vec3(radial);
  return dot(candidate,vec3(radial))<0.0?-candidate:candidate;
}

void main()
{
  bool anchored=(productionLayer&0x40000000u)!=0u;
  // Published patch identities remain the transactional geographic authority,
  // but pixel ownership is resolved by the rasterizer.  The anchored pass
  // writes stencil for the samples it actually covers and the complete
  // terrain-v5 parent subsequently fills stencil zero.  Discarding the parent
  // from an independently reconstructed direction created grazing-angle teeth
  // wherever that analytic boundary differed from the piecewise-linear mesh.
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
  vec3 samplingDirection=ProductionRaySphereDirection(unitDirection,0.0,bodyRadius);
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
  float baseHeight=max(0.0,visible.elevation+LocalTerrainElevationResidual(samplingDirection));
  PhysicalModifierEvaluationD modifierEvaluation=EvaluateTerrainModifiersD(dvec3(samplingDirection),double(baseHeight));
  visible.elevation=max(0.0,baseHeight+float(modifierEvaluation.tiledHeight+modifierEvaluation.erosionHeight+modifierEvaluation.mesoHeight+modifierEvaluation.nearHeight));
  float sampledHeight=visible.elevation;
  vec3 sampledAlbedo=visible.albedo;
  LocalTerrainMaterialSample localSample=SampleLocalTerrainMaterial(samplingDirection);
  if(diagnostic==32770u)
  {
    float value=clamp((baseHeight+100.0)/200.0,0.0,1.0);
    outColor=vec4(vec3(value),1.0);return;
  }
  if(diagnostic==32784u)
  {
    float residual=LocalTerrainElevationResidual(samplingDirection);
    float value=clamp(.5+residual/128.0,0.0,1.0);
    outColor=vec4(value,.25,1.0-value,1.0);return;
  }
  if(diagnostic==32800u||diagnostic==33040u)
  {
    float control=localSample.resident?localSample.controlClass:0.0;
    vec3 color=control==0.0?vec3(0,.12,.3):
      control==1.0?vec3(.1,.65,.8):control==2.0?vec3(.85,.72,.35):
      control==3.0?vec3(.2,.5,.3):control==4.0?vec3(.2,.72,.16):
      control==5.0?vec3(.55,.72,.24):control==6.0?vec3(.08,.36,.08):
      control==7.0?vec3(.55,.55,.58):vec3(1.0,.55,.08);
    if(diagnostic==33040u)color=mix(color,vec3(localSample.controlClass/8.0),.35);
    outColor=vec4(color,1.0);return;
  }
  if(diagnostic==33296u)
  {
    float levelValue=localSample.resident?float(localSample.level-8u)/3.0:0.0;
    outColor=localSample.resident?vec4(levelValue,localSample.weight,1.0-levelValue,1.0):vec4(.5,0,.5,1);return;
  }
  if(diagnostic==32832u)
  {
    outColor=vec4(1.0-localSample.weight,localSample.weight,0.0,1.0);return;
  }
  if(diagnostic==32896u)
  {
    float boundary=localSample.resident?1.0-localSample.weight:1.0;
    outColor=vec4(boundary,localSample.weight,0.0,1.0);return;
  }
  if((diagnostic&32768u)!=0u)
  {
    uint mode=(diagnostic>>8u)&7u;
    if(mode==0u){float value=clamp((globalHeight+1000.0)/5000.0,0.0,1.0);outColor=vec4(vec3(value),1.0);return;}
    if(mode==1u){float value=clamp(.5+float(modifierEvaluation.tiledHeight+modifierEvaluation.erosionHeight+modifierEvaluation.mesoHeight)/96.0,0.0,1.0);outColor=vec4(value,.2,1.0-value,1.0);return;}
    if(mode==2u){float value=clamp((visible.elevation+1000.0)/5000.0,0.0,1.0);outColor=vec4(vec3(value),1.0);return;}
    if(mode==3u){outColor=vec4(ProductionFixedPhysicalNormal(samplingDirection,bodyRadius)*.5+.5,1.0);return;}
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
  // Geometry producers may provide a useful construction normal, but it is
  // never lighting authority. Reconstruct the same physical differential
  // after the rendered elevation transaction for global and anchored
  // ownership alike so mesh-tier changes cannot expose interpolated facets.
  float landWeight=smoothstep(.45,.55,visible.land);
  float representedHeight=mix(0.0,visible.elevation,landWeight);
  // Resolve one final-pixel body-fixed surface point for every geometry owner.
  // The former material coordinate used each owner's interpolated mesh point:
  // coarse global chord positions and fine dynamic positions therefore sampled
  // different procedural fields at the same canonical pixel.  The represented
  // radial shell is already the payload/height authority, so retain its FP64
  // ray intersection and derive both material identity and smooth derivatives
  // from that shared point instead of from renderer topology.
  dvec3 bodyMetres=ProductionRaySpherePosition(samplingDirection,representedHeight,bodyRadiusMetres);
  vec3 analyticSphere=normalize(vec3(bodyMetres));
  // Published hierarchy vertices already carry the canonical CPU physical
  // normal for their exact body-fixed identity. Interpolate that authority
  // across the 16x16 patch instead of reconstructing four FP64 height probes
  // per fragment. The global fallback retains its topology-independent fixed
  // reconstruction because its L0-L2 mesh is intentionally coarse.
  vec3 physical=anchored?normalize(normal):ProductionFixedPhysicalNormal(analyticSphere,bodyRadius);
  if((diagnostic&8192u)!=0u){outColor=vec4(normalize(mix(unitDirection,physical,smoothstep(.45,.55,visible.land)))*.5+.5,1.0);return;}
  // The terrain payload contains bathymetry, not an implemented water
  // displacement surface. Keep current ocean shading on the analytic sea
  // level normal and blend continuously through the land mask so quantized
  // sub-sea elevation cannot become high-frequency specular noise.
  surfaceNormal=normalize(mix(analyticSphere,physical,landWeight));
  if(localSample.resident)sampledAlbedo=mix(sampledAlbedo,localSample.albedo,localSample.weight);
  // The stored regional BC5 field is a payload/diagnostic channel.  Lighting
  // authority is the normal generated from the final composed displaced
  // surface above; applying BC5 here would count the regional slope twice.
  ProductionEarthMaterial earth=ProductionEarthSurfaceMaterial(
    sampledAlbedo,
    visible.land,
    sampledHeight,
    response);
  float surfaceAltitude=max(length(bodyCameraHigh+bodyCameraLow)-bodyRadius,0.0);
  dvec3 cameraBodyMetres=dvec3(bodyCameraHigh)+dvec3(bodyCameraLow);
  vec3 differentialMetres=vec3(bodyMetres-cameraBodyMetres);
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
    localSample.resident?localSample.controlClass:-1.0,
    modifierEvaluation.biomes);
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
  outColor=vec4(lit,1.0);
}
