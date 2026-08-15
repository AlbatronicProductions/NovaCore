struct EarthLandProceduralMaterial
{
  vec3 albedo;
  float roughness;
  vec2 encodedMicroNormal;
  float microNormalStrength;
  float specular;
};

struct EarthLandMaterialWeights
{
  vec4 families;
  float fallback;
};

struct EarthLandMaterialSelection
{
  uvec3 indices;
  vec3 contributions;
  vec3 microHeights;
  vec3 pbrAlbedo0;
  vec3 pbrAlbedo1;
  vec3 pbrAlbedo2;
  vec3 pbrRoughness;
};

layout(set=0,binding=21) uniform sampler2DArray earthMaterialNormals;
layout(set=0,binding=22) uniform sampler2DArray earthMaterialAlbedo;
layout(set=0,binding=23) uniform sampler2DArray earthMaterialSurface;

struct EarthLandPbrSample
{
  vec3 albedo;
  float roughness;
  float microHeight;
};

struct EarthMesoMaterialDomain
{
  vec4 familySignals;
  vec2 primaryWarpMetres;
  vec2 secondaryWarpMetres;
  vec3 colorModulation;
  float roughnessOffset;
  float sampleBlend;
};

struct EarthMesoTextureCoordinates
{
  vec2 primary;
  vec2 secondary;
  vec2 primaryRotation;
  vec2 secondaryRotation;
  float blend;
};

vec2 EarthFixedEnuMetres(vec3 bodyDirection,vec3 anchorDirection,float surfaceRadius)
{
  vec3 anchor=normalize(anchorDirection);
  vec3 eastCandidate=cross(vec3(0,1,0),anchor);
  vec3 east=dot(eastCandidate,eastCandidate)>1e-12?normalize(eastCandidate):normalize(cross(vec3(0,0,1),anchor));
  vec3 north=normalize(cross(anchor,east));
  vec3 direction=normalize(bodyDirection);
  float cosine=clamp(dot(anchor,direction),-1.0,1.0);
  float angle=acos(cosine);
  vec3 tangent=direction-anchor*cosine;
  float tangentLength=length(tangent);
  vec2 azimuth=tangentLength>1e-7?vec2(dot(tangent,east),dot(tangent,north))/tangentLength:vec2(0);
  return azimuth*(angle*surfaceRadius);
}

float EarthFilteredNoise2(vec2 coordinate)
{
  float footprint=max(length(dFdx(coordinate)),length(dFdy(coordinate)));
  return mix(PlanetNoise2(coordinate),.5,smoothstep(.35,1.1,footprint));
}

vec2 EarthRotateMeso(vec2 value,vec2 cosineSine)
{
  return vec2(cosineSine.x*value.x-cosineSine.y*value.y,cosineSine.y*value.x+cosineSine.x*value.y);
}

vec2 EarthInverseRotateMeso(vec2 value,vec2 cosineSine)
{
  return vec2(cosineSine.x*value.x+cosineSine.y*value.y,-cosineSine.y*value.x+cosineSine.x*value.y);
}

float EarthMaterialNormalScale(uint index);

EarthMesoMaterialDomain EarthMesoDomain(vec2 enuMetres)
{
  float geologyA=EarthFilteredNoise2(enuMetres/1280.0+vec2(17.0,61.0));
  float geologyB=EarthFilteredNoise2(enuMetres/1280.0+vec2(113.0,29.0));
  float patchA=EarthFilteredNoise2(enuMetres/384.0+vec2(47.0,101.0));
  float patchB=EarthFilteredNoise2(enuMetres/384.0+vec2(149.0,73.0));
  float localA=EarthFilteredNoise2(enuMetres/96.0+vec2(83.0,191.0));
  float localB=EarthFilteredNoise2(enuMetres/96.0+vec2(211.0,37.0));

  EarthMesoMaterialDomain domain;
  domain.familySignals=vec4(geologyA,geologyB,patchA,patchB);
  domain.primaryWarpMetres=vec2(patchA-.5,geologyB-.5)*18.0+vec2(localA-.5,localB-.5)*5.0;
  domain.secondaryWarpMetres=vec2(geologyA-.5,patchB-.5)*-21.0+vec2(localB-.5,localA-.5)*6.0;
  domain.colorModulation=clamp(vec3((geologyA-.5)*.055+(localB-.5)*.018,
                                    (patchB-.5)*.045+(geologyB-.5)*.012,
                                    (geologyB-.5)*.050+(localA-.5)*.014),vec3(-.045),vec3(.045));
  domain.roughnessOffset=clamp((patchA-.5)*.075+(localB-.5)*.025,-.045,.045);
  domain.sampleBlend=mix(.28,.72,smoothstep(.18,.82,localA*.61+localB*.39));
  return domain;
}

EarthMesoTextureCoordinates EarthMesoCoordinates(vec2 enuMetres,uint index,EarthMesoMaterialDomain domain)
{
  float family=float(index);
  float primaryAngle=.37+family*.91;
  float secondaryAngle=-1.11+family*.73;
  vec2 primaryRotation=vec2(cos(primaryAngle),sin(primaryAngle));
  vec2 secondaryRotation=vec2(cos(secondaryAngle),sin(secondaryAngle));
  float period=EarthMaterialNormalScale(index);
  vec2 primaryPhase=vec2(13.17+family*19.31,41.73+family*7.91);
  vec2 secondaryPhase=vec2(71.29+family*11.17,23.53+family*17.47);

  EarthMesoTextureCoordinates coordinates;
  coordinates.primary=EarthRotateMeso(enuMetres+domain.primaryWarpMetres,primaryRotation)/(period*.97)+primaryPhase;
  coordinates.secondary=EarthRotateMeso(enuMetres+domain.secondaryWarpMetres,secondaryRotation)/(period*1.13)+secondaryPhase;
  coordinates.primaryRotation=primaryRotation;
  coordinates.secondaryRotation=secondaryRotation;
  coordinates.blend=domain.sampleBlend;
  return coordinates;
}

EarthLandMaterialWeights EarthApplyMesoFamilyBias(EarthLandMaterialWeights weights,EarthMesoMaterialDomain domain)
{
  vec4 signals=domain.familySignals;
  vec4 multipliers=clamp(vec4(.84+.30*(signals.x*.62+signals.z*.38),
                              .84+.30*(signals.y*.55+signals.w*.45),
                              .86+.27*(signals.z*.58+signals.y*.42),
                              .88+.23*(signals.w*.61+signals.x*.39)),vec4(.82),vec4(1.18));
  float fallbackMultiplier=clamp(.88+.24*(signals.x*.23+signals.y*.31+signals.z*.19+signals.w*.27),.84,1.16);
  vec4 families=weights.families*multipliers;
  float fallback=weights.fallback*fallbackMultiplier;
  float total=max(dot(families,vec4(1))+fallback,1e-5);
  EarthLandMaterialWeights biased;
  biased.families=clamp(families/total,vec4(0),vec4(1));
  biased.fallback=clamp(fallback/total,0.0,1.0);
  return biased;
}

float EarthLocalDetailFade(float viewDistance,float fadeStart,float fadeEnd)
{
  return clamp(1.0-smoothstep(max(fadeStart,0.0),max(fadeEnd,fadeStart+1.0),viewDistance),0.0,1.0);
}

float EarthMaterialMicroNormalFade(float viewDistance)
{
  return clamp(1.0-smoothstep(1000.0,3000.0,viewDistance),0.0,1.0);
}

float EarthMaterialWeight(EarthLandMaterialWeights weights,uint index)
{
  if(index==0u)return weights.families.x;
  if(index==1u)return weights.families.y;
  if(index==2u)return weights.families.z;
  if(index==3u)return weights.families.w;
  return weights.fallback;
}

float EarthMaterialNormalScale(uint index)
{
  if(index==0u)return 3.5;
  if(index==1u)return 3.0;
  if(index==2u)return 2.5;
  if(index==3u)return 4.5;
  return 4.0;
}

EarthLandPbrSample EarthLandFamilyPbr(vec2 enuMetres,uint index,EarthMesoMaterialDomain domain)
{
  EarthMesoTextureCoordinates coordinates=EarthMesoCoordinates(enuMetres,index,domain);
  vec3 primary=vec3(coordinates.primary,float(index));
  vec3 secondary=vec3(coordinates.secondary,float(index));
  vec2 primaryDx=dFdx(coordinates.primary);
  vec2 primaryDy=dFdy(coordinates.primary);
  vec2 secondaryDx=dFdx(coordinates.secondary);
  vec2 secondaryDy=dFdy(coordinates.secondary);
  vec2 primarySurface=textureGrad(earthMaterialSurface,primary,primaryDx,primaryDy).rg;
  vec2 secondarySurface=textureGrad(earthMaterialSurface,secondary,secondaryDx,secondaryDy).rg;
  vec2 surface=mix(primarySurface,secondarySurface,coordinates.blend);
  EarthLandPbrSample sampleValue;
  vec3 primaryAlbedo=textureGrad(earthMaterialAlbedo,primary,primaryDx,primaryDy).rgb;
  vec3 secondaryAlbedo=textureGrad(earthMaterialAlbedo,secondary,secondaryDx,secondaryDy).rgb;
  sampleValue.albedo=mix(primaryAlbedo,secondaryAlbedo,coordinates.blend);
  sampleValue.roughness=clamp(surface.r,.30,.98);
  sampleValue.microHeight=clamp(surface.g-.5,-.5,.5);
  return sampleValue;
}

float EarthMaterialHeightStrength(uint index)
{
  if(index==0u)return .26;
  if(index==1u)return .22;
  if(index==2u)return .34;
  if(index==3u)return .31;
  return .12;
}

float EarthMaterialMicroHeight(uint index,vec2 enuMetres)
{
  if(index==0u)
  {
    float dune=PlanetNoise2(enuMetres/42.0+vec2(17.0,61.0));
    float grain=PlanetNoise2(enuMetres/11.0+vec2(83.0,29.0));
    return clamp((dune*.68+grain*.32)-.5,-.5,.5);
  }
  if(index==1u)
  {
    float soil=PlanetNoise2(enuMetres/68.0+vec2(31.0,107.0));
    float clump=PlanetNoise2(enuMetres/23.0+vec2(97.0,13.0));
    return clamp((soil*.72+clump*.28)-.5,-.5,.5);
  }
  if(index==2u)
  {
    float ridge=1.0-abs(2.0*PlanetNoise2(enuMetres/36.0+vec2(53.0,7.0))-1.0);
    float fracture=PlanetNoise2(enuMetres/13.0+vec2(113.0,47.0));
    return clamp((ridge*.78+fracture*.22)-.5,-.5,.5);
  }
  if(index==3u)
  {
    float drift=PlanetNoise2(enuMetres/180.0+vec2(11.0,89.0));
    float broad=PlanetNoise2(enuMetres/520.0+vec2(71.0,37.0));
    return clamp((drift*.62+broad*.38)-.5,-.5,.5);
  }
  float neutral=PlanetNoise2(enuMetres/92.0+vec2(43.0,73.0));
  return clamp(neutral-.5,-.5,.5);
}

EarthLandMaterialSelection EarthSelectLandMaterials(EarthLandMaterialWeights weights,float slope,vec2 enuMetres,float heightContribution,EarthMesoMaterialDomain domain)
{
  uvec3 indices=uvec3(0u);
  vec3 strongest=vec3(-1.0);
  float rockSlopeBoost=.30*smoothstep(.04,.32,clamp(slope,0.0,1.0));
  for(uint index=0u;index<5u;index++)
  {
    float score=EarthMaterialWeight(weights,index)+(index==2u?rockSlopeBoost:0.0);
    if(score>strongest.x||(score==strongest.x&&index<indices.x))
    {
      strongest.z=strongest.y;indices.z=indices.y;
      strongest.y=strongest.x;indices.y=indices.x;
      strongest.x=score;indices.x=index;
    }
    else if(score>strongest.y||(score==strongest.y&&index<indices.y))
    {
      strongest.z=strongest.y;indices.z=indices.y;
      strongest.y=score;indices.y=index;
    }
    else if(score>strongest.z||(score==strongest.z&&index<indices.z))
    {
      strongest.z=score;indices.z=index;
    }
  }

  vec3 heights;
  vec3 scores;
  vec3 sampledAlbedo[3];
  vec3 sampledRoughness;
  float boundedHeightContribution=clamp(heightContribution,0.0,1.0);
  for(uint candidate=0u;candidate<3u;candidate++)
  {
    uint index=indices[candidate];
    EarthLandPbrSample pbr=EarthLandFamilyPbr(enuMetres,index,domain);
    sampledAlbedo[candidate]=pbr.albedo;
    sampledRoughness[candidate]=pbr.roughness;
    heights[candidate]=boundedHeightContribution>0.0?pbr.microHeight:0.0;
    scores[candidate]=EarthMaterialWeight(weights,index)+(index==2u?rockSlopeBoost:0.0)+EarthMaterialHeightStrength(index)*heights[candidate]*boundedHeightContribution;
  }
  float maximum=max(scores.x,max(scores.y,scores.z));
  vec3 contributions=smoothstep(vec3(maximum-.22),vec3(maximum+.015),scores);
  contributions/=max(dot(contributions,vec3(1)),1e-5);

  EarthLandMaterialSelection selection;
  selection.indices=indices;
  selection.contributions=contributions;
  selection.microHeights=heights;
  selection.pbrAlbedo0=sampledAlbedo[0];
  selection.pbrAlbedo1=sampledAlbedo[1];
  selection.pbrAlbedo2=sampledAlbedo[2];
  selection.pbrRoughness=sampledRoughness;
  return selection;
}

vec3 EarthSelectionPbrAlbedo(EarthLandMaterialSelection selection,uint candidate)
{
  return candidate==0u?selection.pbrAlbedo0:candidate==1u?selection.pbrAlbedo1:selection.pbrAlbedo2;
}

vec3 EarthLandFamilyMicroNormal(vec2 enuMetres,uint index,EarthMesoMaterialDomain domain)
{
  EarthMesoTextureCoordinates coordinates=EarthMesoCoordinates(enuMetres,index,domain);
  vec3 primaryCoordinate=vec3(coordinates.primary,float(index));
  vec3 secondaryCoordinate=vec3(coordinates.secondary,float(index));
  vec3 primaryNormal=DecodeBc5Normal(textureGrad(earthMaterialNormals,primaryCoordinate,dFdx(coordinates.primary),dFdy(coordinates.primary)).rg);
  vec3 secondaryNormal=DecodeBc5Normal(textureGrad(earthMaterialNormals,secondaryCoordinate,dFdx(coordinates.secondary),dFdy(coordinates.secondary)).rg);
  primaryNormal.xy=EarthInverseRotateMeso(primaryNormal.xy,coordinates.primaryRotation);
  secondaryNormal.xy=EarthInverseRotateMeso(secondaryNormal.xy,coordinates.secondaryRotation);
  return normalize(mix(primaryNormal,secondaryNormal,coordinates.blend));
}

vec3 EarthLandMicroNormal(vec2 enuMetres,EarthLandMaterialSelection selection,EarthMesoMaterialDomain domain)
{
  vec3 blended=vec3(0);
  for(uint candidate=0u;candidate<3u;candidate++)
    blended+=EarthLandFamilyMicroNormal(enuMetres,selection.indices[candidate],domain)*selection.contributions[candidate];
  return normalize(blended);
}

EarthLandMaterialWeights EarthLandClassify(vec3 macroAlbedo,float elevationMetres,float slope,float latitude,vec2 enuMetres)
{
  vec3 macro=clamp(macroAlbedo,vec3(0),vec3(1));
  float maximum=max(max(macro.r,macro.g),macro.b);
  float minimum=min(min(macro.r,macro.g),macro.b);
  float saturation=(maximum-minimum)/max(maximum,1e-4);
  float brightness=dot(macro,vec3(.2126,.7152,.0722));
  float greenness=macro.g-.5*(macro.r+macro.b);
  float warmth=macro.r-macro.b;
  float steep=smoothstep(.045,.30,clamp(slope,0.0,1.0));
  float highland=smoothstep(900.0,4800.0,elevationMetres);
  float polar=smoothstep(.56,.90,abs(latitude));
  float neutral=1.0-smoothstep(.18,.55,saturation);
  float cool=smoothstep(-.08,.08,macro.b-macro.r);
  float climate=PlanetNoise2(enuMetres/240000.0+vec2(43.0,97.0));

  float snowIce=smoothstep(.34,.72,brightness)*neutral*cool*smoothstep(.10,.62,max(polar,highland));
  float rock=max(steep,highland*.72)*mix(.55,1.0,neutral)*(1.0-snowIce);
  float arid=smoothstep(.015,.18,warmth)*(1.0-smoothstep(.00,.095,greenness))*(1.0-.85*polar)*(1.0-.75*snowIce)*mix(.82,1.18,climate);
  float temperate=smoothstep(-.015,.085,greenness)*(1.0-.68*steep)*(1.0-.72*highland)*(1.0-snowIce);
  float fallback=.14+.28*(1.0-max(max(arid,temperate),max(rock,snowIce)));
  float total=max(arid+temperate+rock+snowIce+fallback,1e-5);

  EarthLandMaterialWeights weights;
  weights.families=clamp(vec4(arid,temperate,rock,snowIce)/total,vec4(0),vec4(1));
  weights.fallback=clamp(fallback/total,0.0,1.0);
  return weights;
}

vec3 EarthLandFamilyReferenceAlbedo(uint index)
{
  if(index==0u)return vec3(.273,.125,.033);
  if(index==1u)return vec3(.043,.027,.010);
  if(index==2u)return vec3(.155,.147,.133);
  if(index==3u)return vec3(.674,.768,.890);
  return vec3(.196,.188,.163);
}

vec3 EarthLandFamilyAlbedo(uint index,vec3 generic,float microHeight,vec3 pbrAlbedo)
{
  vec3 family;
  if(index==0u)family=mix(generic,generic*vec3(1.18,1.04,.72)+vec3(.022,.010,0),.55)*(1.0+.10*microHeight);
  else if(index==1u)family=mix(generic,generic*vec3(.78,1.04,.78),.42)*(1.0+.07*microHeight);
  else if(index==2u)
  {
    float rockLuma=dot(generic,vec3(.2126,.7152,.0722));
    family=mix(generic,vec3(rockLuma)*vec3(.90,.95,1.00),.52)*(1.0+.13*microHeight);
  }
  else if(index==3u)family=mix(generic,vec3(.82,.87,.94),.68)*(1.0+.04*microHeight);
  else family=mix(generic,clamp(generic*vec3(1.01,1.00,.98),vec3(0),vec3(1)),.12)*(1.0+.035*microHeight);
  vec3 localRatio=clamp(pbrAlbedo/max(EarthLandFamilyReferenceAlbedo(index),vec3(.008)),vec3(.68),vec3(1.32));
  return clamp(family*mix(vec3(1),localRatio,.72),vec3(0),vec3(1));
}

float EarthLandFamilyRoughness(uint index,float fine,float ridge,float microHeight,float pbrRoughness)
{
  float base=index==0u?.76:index==1u?.86:index==2u?.94:index==3u?.60:.82;
  float procedural=clamp(base+.06*(fine-.5)+.05*(ridge-.5)+.06*microHeight,.42,.97);
  return clamp(mix(procedural,pbrRoughness,.82),.30,.98);
}

float EarthLandFamilyNormalStrength(uint index)
{
  return index==0u?.13:index==1u?.18:index==2u?.30:index==3u?.09:.15;
}

float EarthLandFamilySpecular(uint index)
{
  return index==0u?.025:index==1u?.030:index==2u?.035:index==3u?.12:.035;
}

EarthLandProceduralMaterial EarthLandProceduralSample(vec2 enuMetres,float localScale,float microScale,vec3 macroAlbedo,EarthLandMaterialSelection selection,EarthMesoMaterialDomain domain)
{
  float lowScale=max(localScale*64.0,1.0);
  float mediumScale=max(localScale*6.0,1.0);
  float highScale=max(microScale*8.0,1.0);
  float low=EarthFilteredNoise2(enuMetres/lowScale+vec2(19.0,47.0));
  float medium=EarthFilteredNoise2(enuMetres/mediumScale+vec2(71.0,13.0));
  float ridge=1.0-abs(2.0*medium-1.0);
  float fineX=EarthFilteredNoise2(enuMetres/highScale+vec2(37.0,83.0));
  float fineY=EarthFilteredNoise2(enuMetres/highScale+vec2(109.0,29.0));
  float fine=.5*(fineX+fineY);
  float value=1.0+.15*(low-.5)+.11*(ridge-.5)+.045*(fine-.5);
  vec3 chroma=vec3(.035,-.005,-.025)*(low-.5)+vec3(.018,.010,-.012)*(ridge-.5);
  vec3 generic=macroAlbedo*clamp(vec3(value)+chroma+domain.colorModulation,vec3(.82),vec3(1.18));

  vec3 selectedAlbedo=vec3(0);
  float selectedRoughness=0.0;
  float selectedStrength=0.0;
  float selectedSpecular=0.0;
  for(uint candidate=0u;candidate<3u;candidate++)
  {
    uint index=selection.indices[candidate];
    float contribution=selection.contributions[candidate];
    float microHeight=selection.microHeights[candidate];
    selectedAlbedo+=EarthLandFamilyAlbedo(index,generic,microHeight,EarthSelectionPbrAlbedo(selection,candidate))*contribution;
    selectedRoughness+=EarthLandFamilyRoughness(index,fine,ridge,microHeight,selection.pbrRoughness[candidate])*contribution;
    selectedStrength+=EarthLandFamilyNormalStrength(index)*contribution;
    selectedSpecular+=EarthLandFamilySpecular(index)*contribution;
  }

  EarthLandProceduralMaterial material;
  material.albedo=clamp(selectedAlbedo,vec3(0),vec3(1));
  material.roughness=clamp(selectedRoughness+domain.roughnessOffset,.50,.97);
  material.encodedMicroNormal=clamp(vec2(.5)+vec2(fineX-.5,fineY-.5)*.64,vec2(.08),vec2(.92));
  material.microNormalStrength=clamp(selectedStrength,.08,.30);
  material.specular=clamp(selectedSpecular,.02,.12);
  return material;
}
