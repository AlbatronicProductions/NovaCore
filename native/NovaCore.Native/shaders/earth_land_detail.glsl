struct EarthLandProceduralMaterial
{
  vec3 albedoMultiplier;
  float roughnessOffset;
  vec2 encodedMicroNormal;
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

EarthLandProceduralMaterial EarthLandProceduralSample(vec2 enuMetres,float localScale,float microScale)
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
  float value=1.0+.16*(low-.5)+.12*(ridge-.5)+.05*(fine-.5);
  vec3 chroma=vec3(.035,-.005,-.025)*(low-.5)+vec3(.018,.010,-.012)*(ridge-.5);
  EarthLandProceduralMaterial material;
  material.albedoMultiplier=clamp(vec3(value)+chroma,vec3(.82),vec3(1.18));
  material.roughnessOffset=clamp(.10*(fine-.5)+.06*(ridge-.5),-.08,.08);
  material.encodedMicroNormal=clamp(vec2(.5)+vec2(fineX-.5,fineY-.5)*.64,vec2(.08),vec2(.92));
  return material;
}
