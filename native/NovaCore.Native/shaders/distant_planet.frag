#version 460
#extension GL_ARB_gpu_shader_fp64 : require
#extension GL_GOOGLE_include_directive : require
#include "planet_material.glsl"
#include "planet_terrain.glsl"
layout(location=0) in vec4 color;
layout(location=1) in vec3 normal;
layout(location=2) flat in vec3 lightDirection;
layout(location=3) flat in uint stellar;
layout(location=4) flat in uvec2 material;
layout(location=5) flat in vec4 response;
layout(location=6) in vec3 viewDirection;
struct Environment { vec4 centerRadius; uvec4 identity; vec4 atmosphere; vec4 scattering; vec4 clouds; vec4 cloudShape; vec4 ocean; vec4 oceanColorExposure; };
layout(std430,set=0,binding=11) readonly buffer Environments { Environment value; } environmentData;
layout(push_constant) uniform StellarLighting { vec4 sourceCenterExposure; vec4 sourceColorAmbient; vec4 radianceGlowEnabled; } lighting;
layout(location=0) out vec4 outColor;
void main(){if(stellar!=0u)discard;vec3 surfaceNormal=normalize(normal);vec3 albedo=PlanetAlbedo(material.x,surfaceNormal,color.rgb,response.w);bool earthData=material.x==10u;vec4 earthAlbedoLand=vec4(albedo,1);float earthElevation=0,earthCloud=0,earthBlend=1;uint earthLevel=0u;if(earthData){EarthSurfaceSample(surfaceNormal,0u,0u,earthAlbedoLand,earthElevation,earthCloud,earthBlend,earthLevel);albedo=earthAlbedoLand.rgb;}bool ocean=(environmentData.value.identity.z&4u)!=0u&&(earthData?earthAlbedoLand.a<.5:material.x==3u&&float(PlanetTerrainHeightD(dvec3(surfaceNormal),24u,double(environmentData.value.cloudShape.w)))<environmentData.value.ocean.x);if(ocean){albedo=mix(albedo,environmentData.value.oceanColorExposure.rgb,.35);}if((environmentData.value.identity.z&2u)!=0u&&(material.x==3u||earthData)){float mask,shadowMask;if(earthData){mask=smoothstep(.16,.74,earthCloud);vec4 shadowAlbedo;float shadowElevation,shadowCloud,shadowBlend;uint shadowLevel;EarthSurfaceSample(normalize(surfaceNormal+normalize(lightDirection)*.012),0u,0u,shadowAlbedo,shadowElevation,shadowCloud,shadowBlend,shadowLevel);shadowMask=smoothstep(.16,.74,shadowCloud);}else{float clouds=PlanetFbm(surfaceNormal*environmentData.value.cloudShape.x)+.35*PlanetFbm(surfaceNormal*environmentData.value.cloudShape.y);mask=smoothstep(environmentData.value.clouds.z,1.05,clouds);vec3 shadowDirection=normalize(surfaceNormal+normalize(lightDirection)*.012);float shadowClouds=PlanetFbm(shadowDirection*environmentData.value.cloudShape.x)+.35*PlanetFbm(shadowDirection*environmentData.value.cloudShape.y);shadowMask=smoothstep(environmentData.value.clouds.z,1.05,shadowClouds);}albedo*=1.0-shadowMask*environmentData.value.cloudShape.z;albedo=mix(albedo,vec3(.70,.76,.84),mask*.30);}outColor=vec4(PlanetLighting(albedo,surfaceNormal,lightDirection,viewDirection,ocean?environmentData.value.ocean.y:response.x,ocean?max(response.y,.45):response.y,response.z,lighting.sourceColorAmbient.w),color.a);}
