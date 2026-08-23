#version 460
layout(location=0) in vec4 color;
layout(location=1) in vec3 normal;
layout(location=2) flat in vec3 lightDirection;
layout(location=3) flat in uvec2 material;
layout(location=4) flat in vec4 response;
layout(location=5) in vec3 viewDirection;
layout(location=6) in vec3 bodyDirection;
layout(location=7) in float terrainHeight;
layout(location=11) flat in uint productionLayer;
layout(location=12) in vec2 productionUv;
layout(std430,set=0,binding=12) readonly buffer EyeballInput { vec4 cameraHighRadiusHigh; vec4 cameraLowRadiusLow; vec4 surface; uvec4 identity; vec4 tangentAnchorAngle; vec4 mapping; uvec4 topology; uvec4 reserved; } eye;
layout(set=0,binding=24) uniform sampler2DArray productionAlbedo;
layout(set=0,binding=26) uniform sampler2DArray productionLand;
layout(push_constant) uniform StellarLighting { vec4 sourceCenterExposure; vec4 sourceColorAmbient; vec4 radianceGlowEnabled; } lighting;
layout(location=0) out vec4 outColor;

void main()
{
  bool eyeball=(productionLayer&0x80000000u)!=0u;uint resolvedLayer=productionLayer&0x7fffffffu;
  if(resolvedLayer==0u)discard;
  if(!eyeball&&eye.identity.w!=0u&&eye.surface.w>=.99999&&dot(normalize(bodyDirection),normalize(eye.tangentAnchorAngle.xyz))>=cos(eye.tangentAnchorAngle.w))discard;
  vec3 up=normalize(bodyDirection),surfaceNormal=normalize(normal);
  vec2 storedUv=(vec2(4.0)+clamp(productionUv,0.0,1.0)*256.0)/264.0;
  float layer=float(resolvedLayer-1u);
  vec3 albedo=texture(productionAlbedo,vec3(storedUv,layer)).rgb;
  bool land=texture(productionLand,vec3(storedUv,layer)).r>=.5;
  if(!land)albedo=mix(albedo,vec3(.012,.065,.18),.22);
  else albedo*=mix(.82,1.08,smoothstep(-200.0,4200.0,terrainHeight));
  float ndl=max(dot(surfaceNormal,normalize(lightDirection)),0.0);
  float ambient=max(lighting.sourceColorAmbient.w,.025);
  vec3 lit=albedo*(ambient+ndl);
  outColor=vec4(lit,eyeball?eye.surface.w:1.0);
}
