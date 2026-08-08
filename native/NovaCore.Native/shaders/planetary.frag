#version 460
#extension GL_GOOGLE_include_directive : require
#include "planet_material.glsl"
layout(location=0) in vec4 color;
layout(location=1) in vec3 normal;
layout(location=2) flat in vec3 lightDirection;
layout(location=3) flat in uvec2 material;
layout(location=4) flat in vec4 response;
layout(location=5) in vec3 viewDirection;
layout(location=6) in vec3 bodyDirection;
layout(location=7) in float terrainHeight;
struct Environment { vec4 centerRadius; uvec4 identity; vec4 atmosphere; vec4 scattering; vec4 clouds; vec4 cloudShape; vec4 ocean; vec4 oceanColorExposure; };
layout(std430,set=0,binding=11) readonly buffer Environments { Environment value; } environmentData;
layout(push_constant) uniform StellarLighting { vec4 sourceCenterExposure; vec4 sourceColorAmbient; vec4 radianceGlowEnabled; } lighting;
layout(location=0) out vec4 outColor;
void main(){vec3 surfaceNormal=normalize(normal);vec3 albedo=PlanetAlbedo(material.x,bodyDirection,color.rgb,response.w);bool ocean=(environmentData.value.identity.z&4u)!=0u&&terrainHeight>=0.0&&terrainHeight<environmentData.value.ocean.x;if(ocean){float wave=(PlanetNoise(bodyDirection*environmentData.value.ocean.z)-.5)*environmentData.value.ocean.w;vec3 tangent=normalize(cross(abs(bodyDirection.y)<.9?vec3(0,1,0):vec3(1,0,0),bodyDirection));surfaceNormal=normalize(surfaceNormal+tangent*wave);float fresnel=pow(1.0-max(dot(surfaceNormal,normalize(viewDirection)),0.0),5.0);float broad=PlanetNoise(bodyDirection*environmentData.value.ocean.z*.075);albedo=mix(environmentData.value.oceanColorExposure.rgb,vec3(.035,.16,.34),.16+.42*fresnel);albedo*=.88+.20*broad;}if((environmentData.value.identity.z&2u)!=0u){float clouds=PlanetFbm(bodyDirection*environmentData.value.cloudShape.x)+.35*PlanetFbm(bodyDirection*environmentData.value.cloudShape.y);float mask=smoothstep(environmentData.value.clouds.z,1.05,clouds);vec3 shadowDirection=normalize(bodyDirection+normalize(lightDirection)*.012);float shadowClouds=PlanetFbm(shadowDirection*environmentData.value.cloudShape.x)+.35*PlanetFbm(shadowDirection*environmentData.value.cloudShape.y);float shadowMask=smoothstep(environmentData.value.clouds.z,1.05,shadowClouds);albedo*=1.0-shadowMask*environmentData.value.cloudShape.z;albedo=mix(albedo,vec3(.70,.76,.84),mask*.34);}vec3 lit=PlanetLighting(albedo,surfaceNormal,lightDirection,viewDirection,ocean?environmentData.value.ocean.y:response.x,ocean?max(response.y,.45):response.y,response.z,lighting.sourceColorAmbient.w);outColor=vec4(lit,color.a);}
