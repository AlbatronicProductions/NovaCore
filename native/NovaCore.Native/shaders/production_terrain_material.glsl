#ifndef NOVACORE_PRODUCTION_TERRAIN_MATERIAL_GLSL
#define NOVACORE_PRODUCTION_TERRAIN_MATERIAL_GLSL

// Original NovaCore procedural ground library. Geographic imagery/elevation decides where the
// fragment is; these body-fixed functions add reusable metre-scale material frequency without
// becoming physical-height, cache, or celestial authority.
const float TerrainMaterialFullAltitude=1200.0;
const float TerrainMaterialZeroAltitude=18000.0;
const float TerrainMaterialMaximumVisualDisplacement=.45;
const float TerrainMaterialMaximumNormalAngleRadians=.1396263402; // 8 degrees

struct ProductionTerrainWeights { vec4 primary; vec3 secondary; };
struct ProductionTerrainMaterial
{
  vec3 albedo;
  vec3 normal;
  float roughness;
  float metallic;
  float ambientOcclusion;
  float visualDisplacement;
  float detailWeight;
};

float TerrainSaturate(float value){return clamp(value,0.0,1.0);}
float TerrainHash21(dvec2 point)
{
  dvec3 value=fract(dvec3(point.xyx)*dvec3(.1031,.1030,.0973));
  value+=dot(value,value.yzx+33.33);
  return float(fract((value.x+value.y)*value.z));
}
float TerrainNoise2(dvec2 point)
{
  dvec2 cell=floor(point);vec2 fraction=vec2(fract(point));fraction=fraction*fraction*(3.0-2.0*fraction);
  float a=TerrainHash21(cell),b=TerrainHash21(cell+dvec2(1,0)),c=TerrainHash21(cell+dvec2(0,1)),d=TerrainHash21(cell+dvec2(1));
  return mix(mix(a,b,fraction.x),mix(c,d,fraction.x),fraction.y);
}
float TerrainWorldFootprintMetres(vec3 differentialMetres)
{
  // Derivatives belong to the smooth body-fixed metre domain. Taking them after a
  // projection-axis decision makes the footprint jump when the two selected axes change.
  vec3 dx=dFdx(differentialMetres),dy=dFdy(differentialMetres);
  float footprint=max(length(dx),length(dy));
  return (isnan(footprint)||isinf(footprint))?1e9:clamp(footprint,1e-5,1e9);
}
float TerrainFrequencyAttenuation(float footprintMetres,float scaleMetres)
{
  // Fade continuously before a procedural period becomes smaller than two pixels.
  // The wide shoulder is deliberately conservative at grazing incidence.
  return 1.0-smoothstep(.22,.62,footprintMetres/max(scaleMetres,1e-5));
}
float TerrainNormalFrequencyAttenuation(float footprintMetres,float scaleMetres)
{
  // Shading normals require a wider safety shoulder than albedo/roughness.
  // A period is fully removed before fewer than roughly three pixels can
  // represent it, preventing direct sunlight from amplifying sub-pixel slope.
  return 1.0-smoothstep(.12,.38,footprintMetres/max(scaleMetres,1e-5));
}
vec3 TerrainBiplanarWeights(vec3 surfaceNormal)
{
  vec3 base=pow(max(abs(normalize(surfaceNormal)),vec3(1e-5)),vec3(3.0));
  base/=max(base.x+base.y+base.z,1e-6);
  vec3 selected=base;
  float smallest,second;
  if(base.x<=base.y&&base.x<=base.z){smallest=base.x;second=min(base.y,base.z);selected.x=0.0;}
  else if(base.y<=base.z){smallest=base.y;second=min(base.x,base.z);selected.y=0.0;}
  else{smallest=base.z;second=min(base.x,base.y);selected.z=0.0;}
  selected/=max(selected.x+selected.y+selected.z,1e-6);
  // Ordinary fragments retain two-axis biplanar cost. Only the narrow ambiguous
  // axis-change band uses all three weights so the selected pair cannot make a seam.
  float selectionConfidence=smoothstep(.012,.075,second-smallest);
  vec3 weights=mix(base,selected,selectionConfidence);
  return weights/max(weights.x+weights.y+weights.z,1e-6);
}
float TerrainBiplanarNoiseRaw(dvec3 bodyMetres,vec3 surfaceNormal,double scaleMetres,dvec3 offset)
{
  dvec3 point=(bodyMetres+offset)/scaleMetres;vec3 weights=TerrainBiplanarWeights(surfaceNormal);
  float value=0.0;
  if(weights.x>1e-4)value+=weights.x*TerrainNoise2(point.yz);
  if(weights.y>1e-4)value+=weights.y*TerrainNoise2(point.zx+dvec2(19.19,7.73));
  if(weights.z>1e-4)value+=weights.z*TerrainNoise2(point.xy+dvec2(41.17,3.11));
  return value;
}
float TerrainDetailWeight(float altitudeMetres)
{
  return 1.0-smoothstep(TerrainMaterialFullAltitude,TerrainMaterialZeroAltitude,max(altitudeMetres,0.0));
}

ProductionTerrainWeights ClassifyProductionTerrain(
  float landMask,float elevationMetres,float slope,float absoluteLatitude,float moisture,float temperature)
{
  float flatWeight=1.0-smoothstep(.32,.78,slope),cliff=smoothstep(.38,.82,slope);
  float beach=flatWeight*(1.0-smoothstep(30.0,360.0,abs(elevationMetres)))*(1.0-smoothstep(.84,.94,absoluteLatitude));
  float snowClimate=clamp(absoluteLatitude+clamp(elevationMetres/8000.0,0.0,1.0)*.42+(1.0-temperature)*.22,0.0,1.0);
  float snow=smoothstep(.72,.94,snowClimate)*(1.0-.42*cliff);
  float alpine=smoothstep(1100.0,3600.0,elevationMetres)*(1.0-snow)*(.38+.62*cliff);
  float desert=smoothstep(.42,.82,(1.0-moisture)*(.55+.45*temperature))*(1.0-snow)*(1.0-.35*alpine);
  float vegetation=flatWeight*moisture*temperature*(1.0-beach)*(1.0-snow)*(1.0-desert);
  float rock=cliff*(1.0-.5*snow)*(1.0-.35*alpine);
  float soil=flatWeight*(1.0-beach)*(1.0-snow)*(1.0-.6*vegetation)*(1.0-.55*desert);
  float land=smoothstep(.45,.55,landMask);
  vec4 primary=vec4(vegetation,soil,beach,rock)*land;
  vec3 secondary=vec3(alpine,desert,snow)*land;
  float total=dot(primary,vec4(1))+dot(secondary,vec3(1));
  if(total<=1e-7)return ProductionTerrainWeights(vec4(0,1,0,0),vec3(0));
  return ProductionTerrainWeights(primary/total,secondary/total);
}

void BlendProductionTerrainLibrary(
  ProductionTerrainWeights weights,out vec3 color,out float roughness,out float metallic,out float ao,out float displacement)
{
  const vec3 colors[7]=vec3[7](
    vec3(.105,.205,.070),vec3(.245,.165,.090),vec3(.54,.43,.245),vec3(.235,.225,.205),
    vec3(.355,.350,.335),vec3(.42,.245,.115),vec3(.72,.78,.82));
  const float roughnesses[7]=float[7](.88,.91,.82,.78,.74,.84,.62);
  const float occlusions[7]=float[7](.94,.91,.96,.82,.86,.90,.98);
  const float displacements[7]=float[7](.08,.12,.06,.45,.32,.22,.04);
  float values[7]=float[7](weights.primary.x,weights.primary.y,weights.primary.z,weights.primary.w,weights.secondary.x,weights.secondary.y,weights.secondary.z);
  color=vec3(0);roughness=0.0;metallic=0.0;ao=0.0;displacement=0.0;
  for(int index=0;index<7;index++){color+=colors[index]*values[index];roughness+=roughnesses[index]*values[index];ao+=occlusions[index]*values[index];displacement+=displacements[index]*values[index];}
  displacement=clamp(displacement,-TerrainMaterialMaximumVisualDisplacement,TerrainMaterialMaximumVisualDisplacement);
}

vec3 ApplyTerrainHeightNormal(vec3 geometricNormal,vec3 differentialMetres,float heightField)
{
  vec3 sigmaX=dFdx(differentialMetres),sigmaY=dFdy(differentialMetres);
  vec3 r1=cross(sigmaY,geometricNormal),r2=cross(geometricNormal,sigmaX);
  float determinant=dot(sigmaX,r1);
  vec3 gradient=sign(determinant)*(dFdx(heightField)*r1+dFdy(heightField)*r2);
  vec3 base=normalize(geometricNormal),candidate=normalize(abs(determinant)*base-gradient);
  float angle=acos(clamp(dot(base,candidate),-1.0,1.0));
  float amount=angle>1e-6?min(1.0,TerrainMaterialMaximumNormalAngleRadians/angle):1.0;
  return normalize(mix(base,candidate,amount));
}

ProductionTerrainMaterial SynthesizeProductionTerrainMaterial(
  vec3 geographicAlbedo,float landMask,float elevationMetres,vec3 bodyDirection,vec3 geometricNormal,
  dvec3 bodyMetres,vec3 differentialMetres,float altitudeMetres)
{
  ProductionTerrainMaterial result;
  result.detailWeight=TerrainDetailWeight(altitudeMetres);
  float slope=TerrainSaturate(1.0-dot(normalize(geometricNormal),normalize(bodyDirection)));
  float latitude=abs(bodyDirection.y);
  float moisture=TerrainSaturate(1.35*geographicAlbedo.g-.45*geographicAlbedo.r+.22);
  float temperature=TerrainSaturate(1.0-latitude*.78-max(elevationMetres,0.0)/9000.0);
  ProductionTerrainWeights weights=ClassifyProductionTerrain(landMask,elevationMetres,slope,latitude,moisture,temperature);
  vec3 materialColor;float materialRoughness,materialMetallic,materialAo,materialDisplacement;
  BlendProductionTerrainLibrary(weights,materialColor,materialRoughness,materialMetallic,materialAo,materialDisplacement);

  float footprintMetres=TerrainWorldFootprintMetres(differentialMetres);
  // Three decorrelated body-fixed bands supply broad, meso, and micro frequency. Each band is
  // independently band-limited from the same smooth pre-projection metre footprint. FP64
  // preserves metre identity at Earth radius; derivatives stay in the smooth camera-local
  // differential domain where translation cannot destroy their precision.
  float mesoRaw=TerrainBiplanarNoiseRaw(bodyMetres,geometricNormal,96.0,dvec3(137,271,419));
  float microRaw=TerrainBiplanarNoiseRaw(bodyMetres,geometricNormal,5.5,dvec3(613,89,347));
  float broadRaw=TerrainBiplanarNoiseRaw(bodyMetres,geometricNormal,410.0,dvec3(43,719,181));
  float meso=mix(.5,mesoRaw,TerrainFrequencyAttenuation(footprintMetres,96.0));
  float micro=mix(.5,microRaw,TerrainFrequencyAttenuation(footprintMetres,5.5));
  float broad=mix(.5,broadRaw,TerrainFrequencyAttenuation(footprintMetres,410.0));
  float normalMeso=mix(.5,mesoRaw,TerrainNormalFrequencyAttenuation(footprintMetres,96.0));
  float normalMicro=mix(.5,microRaw,TerrainNormalFrequencyAttenuation(footprintMetres,5.5));
  float variation=(meso-.5)*.18+(micro-.5)*.055+(broad-.5)*.12;
  vec3 synthesized=max(materialColor*(1.0+variation),vec3(0));
  float landDetail=result.detailWeight*smoothstep(.45,.55,landMask);
  result.albedo=mix(geographicAlbedo,mix(geographicAlbedo,synthesized,.62),landDetail);
  result.roughness=mix(.8,clamp(materialRoughness+(micro-.5)*.08,.04,1.0),landDetail);
  result.metallic=mix(0.0,materialMetallic,landDetail);
  result.ambientOcclusion=mix(1.0,clamp(materialAo-(meso-.5)*.08,.65,1.0),landDetail);
  result.visualDisplacement=materialDisplacement*((normalMeso-.5)*.72+(normalMicro-.5)*.28)*landDetail;
  result.normal=landDetail>0.0?ApplyTerrainHeightNormal(normalize(geometricNormal),differentialMetres,result.visualDisplacement):normalize(geometricNormal);
  return result;
}

#endif
