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
float TerrainHash21Prepared(dvec3 value)
{
  value+=dot(value,value.yzx+33.33);
  return float(fract((value.x+value.y)*value.z));
}
vec4 TerrainHash21Corners(dvec2 cell)
{
  // Four value-noise corners contain only two distinct x coordinates and two
  // distinct y coordinates. Prepare those six hash-prefix components once;
  // each corner still executes the accepted scalar hash suffix unchanged.
  dvec2 x=cell.xx+dvec2(0,1),y=cell.yy+dvec2(0,1);
  dvec2 hashX=fract(x*.1031),hashY=fract(y*.1030),hashZ=fract(x*.0973);
  return vec4(
    TerrainHash21Prepared(dvec3(hashX.x,hashY.x,hashZ.x)),
    TerrainHash21Prepared(dvec3(hashX.y,hashY.x,hashZ.y)),
    TerrainHash21Prepared(dvec3(hashX.x,hashY.y,hashZ.x)),
    TerrainHash21Prepared(dvec3(hashX.y,hashY.y,hashZ.y)));
}
float TerrainNoise2(dvec2 point)
{
  dvec2 cell=floor(point);vec2 fraction=vec2(fract(point));fraction=fraction*fraction*(3.0-2.0*fraction);
  vec4 corners=TerrainHash21Corners(cell);
  return mix(mix(corners.x,corners.y,fraction.x),mix(corners.z,corners.w,fraction.x),fraction.y);
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
float TerrainBiplanarNoiseRaw(dvec3 bodyMetres,double scaleMetres,dvec3 offset,vec3 weights)
{
  dvec3 point=(bodyMetres+offset)/scaleMetres;
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

ProductionTerrainWeights ProductionWeightsFromBiome(BiomeBlendD biome,float landMask)
{
  float land=smoothstep(.45,.55,landMask);
  float grass=float(BiomeWeightD(biome,NOVACORE_BIOME_GRASS));
  float scrub=float(BiomeWeightD(biome,NOVACORE_BIOME_SCRUB));
  float wet=float(BiomeWeightD(biome,NOVACORE_BIOME_WETLAND));
  float developed=float(BiomeWeightD(biome,NOVACORE_BIOME_DEVELOPED));
  vec4 primary=vec4(grass+.28*scrub,wet+.55*scrub+.65*developed,
    float(BiomeWeightD(biome,NOVACORE_BIOME_BEACH)),float(BiomeWeightD(biome,NOVACORE_BIOME_ROCKY))+.35*developed)*land;
  vec3 secondary=vec3(float(BiomeWeightD(biome,NOVACORE_BIOME_ALPINE)),
    float(BiomeWeightD(biome,NOVACORE_BIOME_DESERT)),float(BiomeWeightD(biome,NOVACORE_BIOME_SNOW)))*land;
  float total=dot(primary,vec4(1))+dot(secondary,vec3(1));
  if(total<=1e-7)return ProductionTerrainWeights(vec4(0,1,0,0),vec3(0));
  return ProductionTerrainWeights(primary/total,secondary/total);
}

// Candidate D: presentation-only FP32 mirror of the canonical biome semantics.
// It intentionally emits the seven material-library weights directly: no FP64
// modifier evaluation, analytic slope, or physical-height reconstruction is
// required by this fragment path.
ProductionTerrainWeights EvaluatePresentationBiomeWeightsF(vec3 bodyFixedDirection,float geographicBaseHeight,float landMask)
{
  vec3 direction=normalize(bodyFixedDirection);
  vec3 point=direction*6371008.8;
  const vec3 climateA=vec3(.7427813527082074,.5570860145311556,-.3713906763541037);
  const vec3 climateB=vec3(-.4364357804719848,.2182178902359924,.8728715609439696);
  const vec3 climateC=vec3(.2672612419124244,-.8017837257372732,.5345224838248488);
  float latitude=abs(direction.y),temperature=TerrainSaturate(1.0-latitude*.82-max(geographicBaseHeight,0.0)/8500.0);
  float climateValueA=.5+.5*sin(dot(point,climateA)*(6.28318530718/1850000.0)+.37);
  float climateValueB=.5+.5*sin(dot(point,climateB)*(6.28318530718/620000.0)+2.11);
  float climateValueC=.5+.5*sin(dot(point,climateC)*(6.28318530718/210000.0)-1.43);
  float moisture=TerrainSaturate(.18+.46*climateValueA+.24*climateValueB+.12*climateValueC-.18*temperature);
  float aridity=TerrainSaturate((1.0-moisture)*(.55+.45*temperature));
  float coast=1.0-smoothstep(18.0,420.0,abs(geographicBaseHeight)),land=smoothstep(-2.0,8.0,geographicBaseHeight);
  float highland=smoothstep(420.0,2400.0,geographicBaseHeight),alpineGate=smoothstep(1400.0,3600.0,geographicBaseHeight);
  float cold=TerrainSaturate(latitude*.9+max(geographicBaseHeight,0.0)/7500.0+(1.0-temperature)*.25),snowGate=smoothstep(.72,.94,cold);
  float wet=smoothstep(.58,.86,moisture)*(1.0-smoothstep(130.0,900.0,geographicBaseHeight));
  float raw[10];
  raw[0]=1.0-land+land*coast*.18;raw[1]=land*coast*(1.0-.55*wet)*(1.0-snowGate);raw[2]=land*wet*(1.0-.6*highland);
  raw[3]=land*moisture*temperature*(1.0-coast)*(1.0-highland)*(1.0-snowGate);
  raw[4]=land*(1.0-abs(moisture-.38)*1.8)*temperature*(1.0-.7*highland)*(1.0-coast);
  raw[5]=land*smoothstep(.48,.82,aridity)*(1.0-highland)*(1.0-coast)*(1.0-snowGate);
  raw[6]=land*highland*(1.0-.55*snowGate);raw[7]=land*alpineGate*(1.0-snowGate);raw[8]=land*snowGate*(.35+.65*highland);
  raw[9]=land*(1.0-highland)*smoothstep(.78,.94,.5+.5*sin(dot(point,climateB)*(6.28318530718/145000.0)+.91))*.18;
  for(int index=0;index<10;index++)raw[index]=max(raw[index],0.0);
  bool selected[10];for(int index=0;index<10;index++)selected[index]=false;
  float weights[10];for(int index=0;index<10;index++)weights[index]=0.0;
  float total=0.0;
  for(int slot=0;slot<4;slot++){int best=0;float value=-1.0;for(int index=0;index<10;index++)if(!selected[index]&&raw[index]>value){best=index;value=raw[index];}selected[best]=true;weights[best]=value;total+=value;}
  if(!(total>1e-7)){weights[4]=1.0;total=1.0;}
  for(int index=0;index<10;index++)weights[index]/=total;
  float materialLand=smoothstep(.45,.55,landMask);
  vec4 primary=vec4(weights[3]+.28*weights[4],weights[2]+.55*weights[4]+.65*weights[9],weights[1],weights[6]+.35*weights[9])*materialLand;
  vec3 secondary=vec3(weights[7],weights[5],weights[8])*materialLand;
  float materialTotal=dot(primary,vec4(1.0))+dot(secondary,vec3(1.0));
  if(!(materialTotal>1e-7))return ProductionTerrainWeights(vec4(0,1,0,0),vec3(0));
  return ProductionTerrainWeights(primary/materialTotal,secondary/materialTotal);
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
  dvec3 bodyMetres,vec3 differentialMetres,float altitudeMetres,float regionalControlClass,ProductionTerrainWeights weights)
{
  ProductionTerrainMaterial result;
  result.detailWeight=TerrainDetailWeight(altitudeMetres);
  float slope=TerrainSaturate(1.0-dot(normalize(geometricNormal),normalize(bodyDirection)));
  float latitude=abs(bodyDirection.y);
  float moisture=TerrainSaturate(1.35*geographicAlbedo.g-.45*geographicAlbedo.r+.22);
  float temperature=TerrainSaturate(1.0-latitude*.78-max(elevationMetres,0.0)/9000.0);
  vec3 materialColor;float materialRoughness,materialMetallic,materialAo,materialDisplacement;
  BlendProductionTerrainLibrary(weights,materialColor,materialRoughness,materialMetallic,materialAo,materialDisplacement);

  float footprintMetres=TerrainWorldFootprintMetres(differentialMetres);
  // Three decorrelated body-fixed bands supply broad, meso, and micro frequency. Each band is
  // independently band-limited from the same smooth pre-projection metre footprint. FP64
  // preserves metre identity at Earth radius; derivatives stay in the smooth camera-local
  // differential domain where translation cannot destroy their precision.
  float mesoAttenuation=TerrainFrequencyAttenuation(footprintMetres,96.0);
  float microAttenuation=TerrainFrequencyAttenuation(footprintMetres,5.5);
  float broadAttenuation=TerrainFrequencyAttenuation(footprintMetres,410.0);
  float normalMesoAttenuation=TerrainNormalFrequencyAttenuation(footprintMetres,96.0);
  float normalMicroAttenuation=TerrainNormalFrequencyAttenuation(footprintMetres,5.5);
  // The body-fixed projection weights are common to all material frequencies.
  // Do not evaluate a decorrelated noise field once both its material and normal
  // contributions are fully band-limited at this pixel.
  vec3 biplanarWeights=TerrainBiplanarWeights(geometricNormal);
  float mesoRaw=(mesoAttenuation>0.0||normalMesoAttenuation>0.0)?TerrainBiplanarNoiseRaw(bodyMetres,96.0,dvec3(137,271,419),biplanarWeights):.5;
  float microRaw=(microAttenuation>0.0||normalMicroAttenuation>0.0)?TerrainBiplanarNoiseRaw(bodyMetres,5.5,dvec3(613,89,347),biplanarWeights):.5;
  float broadRaw=broadAttenuation>0.0?TerrainBiplanarNoiseRaw(bodyMetres,410.0,dvec3(43,719,181),biplanarWeights):.5;
  float meso=mix(.5,mesoRaw,mesoAttenuation);
  float micro=mix(.5,microRaw,microAttenuation);
  float broad=mix(.5,broadRaw,broadAttenuation);
  float normalMeso=mix(.5,mesoRaw,normalMesoAttenuation);
  float normalMicro=mix(.5,microRaw,normalMicroAttenuation);
  float variation=(meso-.5)*.18+(micro-.5)*.055+(broad-.5)*.12;
  // Terrain-v5/local payload albedo is geographic material authority. The
  // former close-range blend replaced 62% of that identity with seven constant
  // palette colors, producing the observed green-to-uniform-tan altitude
  // crossfade even though geometry and ownership remained valid. Procedural
  // synthesis contributes band-limited metre-scale variation and response;
  // it must not replace the macro geographic signal.
  vec3 detailedGeographic=max(geographicAlbedo*(1.0+variation),vec3(0));
  float landDetail=result.detailWeight*smoothstep(.45,.55,landMask);
  result.albedo=mix(geographicAlbedo,detailedGeographic,landDetail);
  result.roughness=mix(.8,clamp(materialRoughness+(micro-.5)*.08,.04,1.0),landDetail);
  result.metallic=mix(0.0,materialMetallic,landDetail);
  result.ambientOcclusion=mix(1.0,clamp(materialAo-(meso-.5)*.08,.65,1.0),landDetail);
  result.visualDisplacement=materialDisplacement*((normalMeso-.5)*.72+(normalMicro-.5)*.28)*landDetail;
  result.normal=landDetail<=0.0?normalize(geometricNormal):ApplyTerrainHeightNormal(normalize(geometricNormal),differentialMetres,result.visualDisplacement);
  // M12 regional control is categorical physical geography, not a replacement
  // color map. It selects restrained NovaCore material response while the
  // independently sourced macro albedo remains recognizable and continuous.
  if(regionalControlClass>=0.0&&landDetail>0.0)
  {
    int control=int(round(regionalControlClass));vec3 responseColor=vec3(1);float responseRoughness=result.roughness;
    if(control==1){responseColor=vec3(.93,1.03,1.02);responseRoughness=.78;}
    else if(control==2){responseColor=vec3(1.10,1.04,.84);responseRoughness=.86;}
    else if(control==3){responseColor=vec3(.78,.94,.80);responseRoughness=.94;}
    else if(control==4){responseColor=vec3(.86,1.08,.80);responseRoughness=.91;}
    else if(control==5){responseColor=vec3(.94,1.03,.83);responseRoughness=.90;}
    else if(control==6){responseColor=vec3(.72,.96,.70);responseRoughness=.93;}
    else if(control==7){responseColor=vec3(.94,.93,.91);responseRoughness=.72;}
    else if(control==8){responseColor=vec3(.96,.95,.91);responseRoughness=.68;}
    result.albedo*=mix(vec3(1),responseColor,.22*landDetail);
    result.roughness=mix(result.roughness,responseRoughness,.28*landDetail);
  }
  return result;
}

#endif
