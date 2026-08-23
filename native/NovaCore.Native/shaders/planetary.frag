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
layout(location=10) flat in vec4 localDetail;
layout(push_constant) uniform StellarLighting { vec4 sourceCenterExposure; vec4 sourceColorAmbient; vec4 radianceGlowEnabled; } lighting;
layout(location=0) out vec4 outColor;

void main()
{
  vec3 up=normalize(bodyDirection),surfaceNormal=normalize(normal);
  vec3 albedo=PlanetAlbedo(material.x,up,color.rgb,response.w);
  float roughness=response.x,specular=response.y;
  vec3 lit=PlanetLighting(albedo,surfaceNormal,lightDirection,viewDirection,roughness,specular,response.z,lighting.sourceColorAmbient.w);
  outColor=vec4(lit,color.a);
}
