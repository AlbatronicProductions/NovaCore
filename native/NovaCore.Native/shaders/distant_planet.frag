#version 460
#extension GL_ARB_gpu_shader_fp64 : require
#extension GL_GOOGLE_include_directive : require
#include "planet_material.glsl"
#include "production_cube_surface.glsl"
layout(location=0) in vec4 color;
layout(location=1) in vec3 normal;
layout(location=2) flat in vec3 lightDirection;
layout(location=3) flat in uint stellar;
layout(location=4) flat in uvec2 material;
layout(location=5) flat in vec4 response;
layout(location=6) in vec3 viewDirection;
layout(location=7) flat in float surfaceAltitudeMetres;
layout(set=0,binding=24) uniform sampler2DArray productionAlbedo;
layout(std430,set=0,binding=27) readonly buffer ProductionLayers { uint values[]; } productionLayers;
layout(push_constant) uniform StellarLighting { vec4 sourceCenterExposure; vec4 sourceColorAmbient; vec4 radianceGlowEnabled; } lighting;
layout(location=0) out vec4 outColor;
void main(){if(stellar!=0u)discard;vec3 surfaceNormal=normalize(normal);vec3 albedo=PlanetAlbedo(material.x,surfaceNormal,color.rgb,response.w);float roughness=response.x,specular=response.y;bool earthData=material.x==10u;if(earthData){uint face;vec2 faceUv;ProductionDirectionAddress(surfaceNormal,face,faceUv);uint layer=productionLayers.values[ProductionPatchOrdinal(face,0u,0u,0u)];if(layer!=0u){vec2 storedUv=(vec2(4.0)+clamp(faceUv,0.0,1.0)*256.0)/264.0;albedo=texture(productionAlbedo,vec3(storedUv,float(layer-1u))).rgb;}}outColor=vec4(PlanetLighting(albedo,surfaceNormal,lightDirection,viewDirection,roughness,specular,response.z,lighting.sourceColorAmbient.w),color.a);}
