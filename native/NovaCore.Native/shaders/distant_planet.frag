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
void main(){if(stellar!=0u)discard;vec3 surfaceNormal=normalize(normal);vec3 albedo=PlanetAlbedo(material.x,surfaceNormal,color.rgb,response.w);bool ocean=(environmentData.value.identity.z&4u)!=0u&&material.x==3u&&float(PlanetTerrainHeightD(dvec3(surfaceNormal),24u,double(environmentData.value.cloudShape.w)))<environmentData.value.ocean.x;if(ocean){albedo=environmentData.value.oceanColorExposure.rgb;}if((environmentData.value.identity.z&2u)!=0u&&material.x==3u){float clouds=PlanetFbm(surfaceNormal*environmentData.value.cloudShape.x)+.35*PlanetFbm(surfaceNormal*environmentData.value.cloudShape.y);float mask=smoothstep(environmentData.value.clouds.z,1.05,clouds);vec3 shadowDirection=normalize(surfaceNormal+normalize(lightDirection)*.012);float shadowClouds=PlanetFbm(shadowDirection*environmentData.value.cloudShape.x)+.35*PlanetFbm(shadowDirection*environmentData.value.cloudShape.y);albedo*=1.0-smoothstep(environmentData.value.clouds.z,1.05,shadowClouds)*environmentData.value.cloudShape.z;albedo=mix(albedo,vec3(.70,.76,.84),mask*.34);}outColor=vec4(PlanetLighting(albedo,surfaceNormal,lightDirection,viewDirection,ocean?environmentData.value.ocean.y:response.x,ocean?max(response.y,.45):response.y,response.z,lighting.sourceColorAmbient.w),color.a);}
