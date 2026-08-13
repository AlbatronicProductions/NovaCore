#version 460
#extension GL_GOOGLE_include_directive : require
#include "planet_material.glsl"
#include "earth_ocean_material.glsl"
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
struct Environment { vec4 centerRadius; uvec4 identity; vec4 atmosphere; vec4 scattering; vec4 clouds; vec4 cloudShape; vec4 ocean; vec4 oceanColorExposure; };
layout(std430,set=0,binding=2) readonly buffer PlanetaryInput { vec4 cameraHighRadiusHigh; vec4 cameraLowRadiusLow; vec4 thresholds; uvec4 controls; vec4 viewForwardHalfAngle; vec4 textureDemand; } planetaryInput;
layout(std430,set=0,binding=11) readonly buffer Environments { Environment value; } environmentData;
layout(std430,set=0,binding=12) readonly buffer EyeballDebugInput { vec4 cameraHighRadiusHigh; vec4 cameraLowRadiusLow; vec4 surface; uvec4 identity; vec4 tangentAnchorAngle; vec4 mapping; uvec4 topology; uvec4 reserved; } eyeDebug;
layout(push_constant) uniform StellarLighting { vec4 sourceCenterExposure; vec4 sourceColorAmbient; vec4 radianceGlowEnabled; } lighting;
layout(location=0) out vec4 outColor;

void main()
{
  Environment environment=environmentData.value;
  vec3 up=normalize(bodyDirection);
  vec3 surfaceNormal=normalize(normal);
  float viewDistance=length(viewDirection);
  float macroFade=clamp(1.0-smoothstep(220000.0,900000.0,viewDistance),0.0,1.0);
  float localScale=max(localDetail.x,1.0);
  float localMicroScale=max(localDetail.y,1.0);
  float localFadeStart=max(localDetail.z,0.0);
  float localFadeEnd=max(localDetail.w,localFadeStart+1.0);
  float localBlend=clamp(1.0-smoothstep(localFadeStart,localFadeEnd,viewDistance),0.0,1.0);
  float localContribution=localBlend*macroFade;

  vec3 albedo=PlanetAlbedo(material.x,up,color.rgb,response.w);
  float roughness=response.x;
  float specular=response.y;
  bool earthData=material.x==10u;
  vec4 earthAlbedoLand=vec4(albedo,1);
  float earthElevation=terrainHeight,earthCloud=0.0,earthBlend=1.0;
  uint earthLevel=0u;
  if(earthData)
  {
    uint desired=uint(planetaryInput.textureDemand.w);
    EarthSurfaceSample(up,desired,planetaryInput.controls.w,earthAlbedoLand,earthElevation,earthCloud,earthBlend,earthLevel);
    albedo=earthAlbedoLand.rgb;
  }

  bool ocean=(environment.identity.z&4u)!=0u&&(earthData?earthAlbedoLand.a<.5:terrainHeight>=0.0&&terrainHeight<environment.ocean.x);
  vec3 bodyPosition=PlanetBodyPosition(up,environment.centerRadius.w,terrainHeight);

  if(ocean)
  {
    EarthOceanMaterial oceanBase=EarthOceanBaseMaterial(albedo,environment.oceanColorExposure.rgb,environment.ocean.y,specular);
    float oceanDetailWeight=EarthOceanDetailWeight(viewDistance);
    float waveFade=1.0-smoothstep(1200.0,45000.0,viewDistance);
    vec3 reference=abs(up.y)<.9?vec3(0,1,0):vec3(1,0,0);
    vec3 east=normalize(cross(reference,up));
    vec3 north=normalize(cross(up,east));
    float waveEast=PlanetTriplanarNoise(PlanetWrappedBodyCoordinate(bodyPosition,up,7.0),up)-.5;
    float waveNorth=PlanetTriplanarNoise(PlanetWrappedBodyCoordinate(bodyPosition,up,13.0)+vec3(23.0),up)-.5;
    surfaceNormal=normalize(up+(east*waveEast+north*waveNorth)*environment.ocean.w*waveFade*oceanDetailWeight);
    float fresnel=pow(1.0-max(dot(surfaceNormal,normalize(viewDirection)),0.0),5.0);
    float broad=PlanetTriplanarNoise(bodyPosition/1800.0,up);
    vec3 detailedAlbedo=mix(environment.oceanColorExposure.rgb,vec3(.035,.16,.34),.12+.55*fresnel);
    detailedAlbedo*=.90+.16*broad;
    albedo=mix(oceanBase.albedo,detailedAlbedo,oceanDetailWeight);
    roughness=mix(oceanBase.roughness,mix(environment.ocean.y,.42,1.0-waveFade),oceanDetailWeight);
    specular=mix(oceanBase.specular,max(specular,.48),oceanDetailWeight);
  }
  else if(material.x==3u&&terrainHeight>=environment.ocean.x)
  {
    float elevation=clamp((terrainHeight-environment.ocean.x)/4200.0,0.0,1.0);
    float slope=1.0-clamp(dot(surfaceNormal,up),0.0,1.0);
    float moderate=smoothstep(.035,.16,slope);
    float steep=smoothstep(.14,.36,slope);
    float regional=PlanetFilteredTriplanarNoise(bodyPosition/120000.0,up,.04,.9);
    float landscape=PlanetFilteredTriplanarNoise(bodyPosition/9000.0,surfaceNormal,.05,.85);

    float local=PlanetTriplanarNoise(PlanetWrappedBodyCoordinate(bodyPosition,up,localScale),surfaceNormal);
    vec2 micro=ProjectWorldVectorToBc5Tangent(surfaceNormal,PlanetTangentDetailCoordinate(PlanetWrappedBodyCoordinate(bodyPosition,up,localMicroScale),up,surfaceNormal));
    vec3 localNormal=ComposeMicroNormal(surfaceNormal,micro,localContribution,0.14);

    float aridity=clamp(.12+.76*regional+.18*elevation,0.0,1.0);
    vec3 sediment=mix(vec3(.045,.075,.025),vec3(.20,.13,.055),aridity);
    sediment=mix(sediment,vec3(.18,.16,.09),0.22*landscape);
    vec3 substrate=mix(vec3(.12,.11,.09),vec3(.28,.26,.22),landscape);
    vec3 cliff=mix(vec3(.13,.14,.13),vec3(.30,.29,.27),local);
    albedo=mix(sediment,substrate,moderate*.62);
    albedo=mix(albedo,cliff,steep);
    vec3 localized=mix(albedo,albedo*(0.94+0.20*(local-.5)),localContribution);
    albedo=mix(albedo,localized,localContribution);
    roughness=clamp(mix(.90,.66,steep)+(.24*(local-.5))*localContribution,.46,.98);
    roughness=mix(roughness,clamp(roughness+0.12*(local-.5),0.46,0.98),localContribution*0.55);
    surfaceNormal=localNormal;
    specular=mix(.025,.085,steep);
  }
  else if(earthData)
  {
    float slope=1.0-clamp(dot(surfaceNormal,up),0.0,1.0);
    float local=PlanetTriplanarNoise(PlanetWrappedBodyCoordinate(bodyPosition,up,localScale),surfaceNormal);
    float surface=PlanetTriplanarNoise(PlanetWrappedBodyCoordinate(bodyPosition,up,localMicroScale)+vec3(31.0,17.0,7.0),surfaceNormal);
    vec2 micro=ProjectWorldVectorToBc5Tangent(surfaceNormal,PlanetTangentDetailCoordinate(PlanetWrappedBodyCoordinate(bodyPosition,up,localMicroScale),up,surfaceNormal));

    albedo*=.88+.20*local+.08*surface;
    albedo=mix(albedo,albedo*(0.94+0.12*(local-.5)),localContribution);
    albedo=mix(albedo,albedo*.62+vec3(.10,.09,.075),smoothstep(.12,.36,slope)*.28);
    surfaceNormal=ComposeMicroNormal(surfaceNormal,micro,localContribution,0.24);
    roughness=clamp(.74+.16*(surface-.5)+0.12*(local-.5)*localContribution,.48,.94);
    specular=0.035;
  }

  if((environment.identity.z&2u)!=0u)
  {
    float mask,shadowMask;
    if(earthData)
    {
      mask=smoothstep(.16,.74,earthCloud);
      vec3 shadowDirection=normalize(up+normalize(lightDirection)*.012);
      vec4 shadowAlbedo;
      float shadowElevation,shadowCloud,shadowBlend;
      uint shadowLevel;
      EarthSurfaceSample(shadowDirection,earthLevel,planetaryInput.controls.w,shadowAlbedo,shadowElevation,shadowCloud,shadowBlend,shadowLevel);
      shadowMask=smoothstep(.16,.74,shadowCloud);
    }
    else
    {
      float clouds=PlanetFbm(up*environment.cloudShape.x)+.35*PlanetFbm(up*environment.cloudShape.y);
      mask=smoothstep(environment.clouds.z,1.05,clouds);
      vec3 shadowDirection=normalize(up+normalize(lightDirection)*.012);
      float shadowClouds=PlanetFbm(shadowDirection*environment.cloudShape.x)+.35*PlanetFbm(shadowDirection*environment.cloudShape.y);
      shadowMask=smoothstep(environment.clouds.z,1.05,shadowClouds);
    }

    float surfaceCloudWeight=smoothstep(200000.0,1000000.0,viewDistance);
    albedo*=1.0-shadowMask*environment.cloudShape.z*surfaceCloudWeight;
    albedo=mix(albedo,vec3(.70,.76,.84),mask*.30*surfaceCloudWeight);
  }

  uint debugMode=eyeDebug.reserved.x;
  if(earthData&&debugMode!=0u)
  {
    uint requested=planetaryInput.thresholds.z>1000000.0?1u:planetaryInput.thresholds.z>100000.0?2u:planetaryInput.thresholds.z>10000.0?3u:4u;
    vec2 uv=EarthUv(up);
    uint page=EarthResidentPage(uv,requested,earthLevel);
    uint slot=earthPageTable.values[page].slots.x-1u;
    vec3 debugColor=vec3(0);

    if(debugMode==1u) debugColor=vec3(float(earthLevel)/4.0,1.0-float(earthLevel)/4.0,.25+.15*float(earthLevel));
    else if(debugMode==2u) debugColor=fract(vec3(slot*.1031,slot*.11369,slot*.13787));
    else if(debugMode==3u) debugColor=earthLevel==requested?vec3(.05,.9,.12):vec3(.9,.08,.05);
    else if(debugMode==4u) debugColor=vec3(float(requested-earthLevel)/4.0);
    else if(debugMode==5u) debugColor=vec3(float(requested)/4.0,float(earthLevel)/4.0,earthLevel==requested?1.0:0.0);
    else if(debugMode==6u) debugColor=vec3(earthBlend,1.0-earthBlend,.15);
    else if(debugMode==7u) debugColor=earthAlbedoLand.rgb;
    else if(debugMode==8u) debugColor=vec3(clamp((earthElevation+11000.0)/20000.0,0.0,1.0));
    else if(debugMode==9u) debugColor=earthAlbedoLand.a<.5?vec3(.02,.2,1.0):vec3(.18,.85,.08);
    else if(debugMode==10u) debugColor=vec3(earthCloud);
    else debugColor=vec3(.08,.32,.95)*clamp(1.0-abs(dot(up,normalize(viewDirection))),0.0,1.0);

    outColor=vec4(debugColor,1);
    return;
  }

  vec3 lit=PlanetLighting(albedo,surfaceNormal,lightDirection,viewDirection,roughness,specular,response.z,lighting.sourceColorAmbient.w);
  float aerial=0.0;
  if((environment.identity.z&1u)!=0u)
  {
    float cameraAltitude=max(length(environment.centerRadius.xyz)-environment.centerRadius.w,0.0);
    float fragmentAltitude=max(terrainHeight,environment.ocean.x);
    float density=.5*(exp(-cameraAltitude/max(environment.atmosphere.y,1.0))+exp(-fragmentAltitude/max(environment.atmosphere.y,1.0)));
    float atmospherePresence=1.0-smoothstep(environment.atmosphere.x*.85,environment.atmosphere.x*1.30,cameraAltitude);
    aerial=(1.0-exp(-viewDistance*density/145000.0))*atmospherePresence;
  }

  float sunAmount=.18+.82*max(dot(up,normalize(lightDirection)),0.0);
  vec3 haze=mix(vec3(.12,.25,.48),vec3(.36,.49,.66),clamp(sunAmount,0.0,1.0));
  lit=mix(lit,haze*sunAmount,clamp(aerial,0.0,earthData ? .38 : .64));
  outColor=vec4(lit,color.a);
}
