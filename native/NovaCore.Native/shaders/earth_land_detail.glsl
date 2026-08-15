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
};

layout(set=0,binding=21) uniform sampler2DArray earthMaterialNormals;

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

EarthLandMaterialSelection EarthSelectLandMaterials(EarthLandMaterialWeights weights,float slope,vec2 enuMetres,float heightContribution)
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
  float boundedHeightContribution=clamp(heightContribution,0.0,1.0);
  for(uint candidate=0u;candidate<3u;candidate++)
  {
    uint index=indices[candidate];
    heights[candidate]=boundedHeightContribution>0.0?EarthMaterialMicroHeight(index,enuMetres):0.0;
    scores[candidate]=EarthMaterialWeight(weights,index)+(index==2u?rockSlopeBoost:0.0)+EarthMaterialHeightStrength(index)*heights[candidate]*boundedHeightContribution;
  }
  float maximum=max(scores.x,max(scores.y,scores.z));
  vec3 contributions=smoothstep(vec3(maximum-.22),vec3(maximum+.015),scores);
  contributions/=max(dot(contributions,vec3(1)),1e-5);

  EarthLandMaterialSelection selection;
  selection.indices=indices;
  selection.contributions=contributions;
  selection.microHeights=heights;
  return selection;
}

vec3 EarthLandFamilyMicroNormal(vec2 enuMetres,uint index)
{
  return DecodeBc5Normal(texture(earthMaterialNormals,vec3(enuMetres/EarthMaterialNormalScale(index),float(index))).rg);
}

vec3 EarthLandMicroNormal(vec2 enuMetres,EarthLandMaterialSelection selection)
{
  vec3 blended=vec3(0);
  for(uint candidate=0u;candidate<3u;candidate++)
    blended+=EarthLandFamilyMicroNormal(enuMetres,selection.indices[candidate])*selection.contributions[candidate];
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

vec3 EarthLandFamilyAlbedo(uint index,vec3 generic,float microHeight)
{
  if(index==0u)return mix(generic,generic*vec3(1.18,1.04,.72)+vec3(.022,.010,0),.55)*(1.0+.10*microHeight);
  if(index==1u)return mix(generic,generic*vec3(.78,1.04,.78),.42)*(1.0+.07*microHeight);
  if(index==2u)
  {
    float rockLuma=dot(generic,vec3(.2126,.7152,.0722));
    return mix(generic,vec3(rockLuma)*vec3(.90,.95,1.00),.52)*(1.0+.13*microHeight);
  }
  if(index==3u)return mix(generic,vec3(.82,.87,.94),.68)*(1.0+.04*microHeight);
  return mix(generic,clamp(generic*vec3(1.01,1.00,.98),vec3(0),vec3(1)),.12)*(1.0+.035*microHeight);
}

float EarthLandFamilyRoughness(uint index,float fine,float ridge,float microHeight)
{
  float base=index==0u?.76:index==1u?.86:index==2u?.94:index==3u?.60:.82;
  return clamp(base+.06*(fine-.5)+.05*(ridge-.5)+.06*microHeight,.50,.97);
}

float EarthLandFamilyNormalStrength(uint index)
{
  return index==0u?.13:index==1u?.18:index==2u?.30:index==3u?.09:.15;
}

float EarthLandFamilySpecular(uint index)
{
  return index==0u?.025:index==1u?.030:index==2u?.035:index==3u?.12:.035;
}

EarthLandProceduralMaterial EarthLandProceduralSample(vec2 enuMetres,float localScale,float microScale,vec3 macroAlbedo,EarthLandMaterialSelection selection)
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
  vec3 generic=macroAlbedo*clamp(vec3(value)+chroma,vec3(.82),vec3(1.18));

  vec3 selectedAlbedo=vec3(0);
  float selectedRoughness=0.0;
  float selectedStrength=0.0;
  float selectedSpecular=0.0;
  for(uint candidate=0u;candidate<3u;candidate++)
  {
    uint index=selection.indices[candidate];
    float contribution=selection.contributions[candidate];
    float microHeight=selection.microHeights[candidate];
    selectedAlbedo+=EarthLandFamilyAlbedo(index,generic,microHeight)*contribution;
    selectedRoughness+=EarthLandFamilyRoughness(index,fine,ridge,microHeight)*contribution;
    selectedStrength+=EarthLandFamilyNormalStrength(index)*contribution;
    selectedSpecular+=EarthLandFamilySpecular(index)*contribution;
  }

  EarthLandProceduralMaterial material;
  material.albedo=clamp(selectedAlbedo,vec3(0),vec3(1));
  material.roughness=clamp(selectedRoughness,.50,.97);
  material.encodedMicroNormal=clamp(vec2(.5)+vec2(fineX-.5,fineY-.5)*.64,vec2(.08),vec2(.92));
  material.microNormalStrength=clamp(selectedStrength,.08,.30);
  material.specular=clamp(selectedSpecular,.02,.12);
  return material;
}
