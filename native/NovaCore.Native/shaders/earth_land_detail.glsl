struct EarthLandProceduralMaterial
{
  vec3 albedo;
  float roughness;
  vec2 encodedMicroNormal;
  float microNormalStrength;
};

struct EarthLandMaterialWeights
{
  vec4 families;
  float fallback;
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

float EarthLocalDetailFade(float viewDistance,float fadeStart,float fadeEnd)
{
  return clamp(1.0-smoothstep(max(fadeStart,0.0),max(fadeEnd,fadeStart+1.0),viewDistance),0.0,1.0);
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

EarthLandProceduralMaterial EarthLandProceduralSample(vec2 enuMetres,float localScale,float microScale,vec3 macroAlbedo,EarthLandMaterialWeights weights)
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
  float familyFrequency=dot(weights.families,vec4(.56,.86,1.24,.32))+weights.fallback*.70;
  float value=1.0+familyFrequency*(.16*(low-.5)+.12*(ridge-.5)+.05*(fine-.5));
  vec3 chroma=(vec3(.035,-.005,-.025)*(low-.5)+vec3(.018,.010,-.012)*(ridge-.5))*familyFrequency;
  vec3 generic=macroAlbedo*clamp(vec3(value)+chroma,vec3(.82),vec3(1.18));
  vec3 arid=mix(generic,generic*vec3(1.10,1.02,.84)+vec3(.014,.007,0),.32);
  vec3 temperate=mix(generic,generic*vec3(.86,1.07,.86),.30);
  float rockLuma=dot(generic,vec3(.2126,.7152,.0722));
  vec3 rock=mix(generic,vec3(rockLuma)*vec3(.94,.97,1.00),.34);
  vec3 snowIce=mix(generic,vec3(.72,.78,.86),.42);
  EarthLandProceduralMaterial material;
  material.albedo=clamp(arid*weights.families.x+temperate*weights.families.y+rock*weights.families.z+snowIce*weights.families.w+generic*weights.fallback,vec3(0),vec3(1));
  material.roughness=clamp(dot(weights.families,vec4(.78,.88,.93,.62))+weights.fallback*.82+.08*(fine-.5)+.05*(ridge-.5),.52,.96);
  material.encodedMicroNormal=clamp(vec2(.5)+vec2(fineX-.5,fineY-.5)*.64,vec2(.08),vec2(.92));
  material.microNormalStrength=clamp(dot(weights.families,vec4(.13,.18,.28,.10))+weights.fallback*.16,.08,.30);
  return material;
}
