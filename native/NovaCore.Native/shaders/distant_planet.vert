#version 460
struct EncodedPosition { vec4 high; vec4 low; };
struct GpuCameraData { EncodedPosition position; mat4 viewProjection; };
layout(std430,set=0,binding=0) readonly buffer Frame { GpuCameraData camera; } frameData;
struct Presentation { vec4 centerRadius; vec4 colorDistant; vec4 blendMetricState; uvec4 identity; vec4 surface; uvec4 hooks; vec4 ringGeometry; vec4 ringOrientation; vec4 ringColor; };
layout(std430,set=0,binding=6) readonly buffer Presentations { Presentation values[]; } presentations;
layout(push_constant) uniform StellarLighting { vec4 sourceCenterExposure; vec4 sourceColorAmbient; vec4 radianceGlowEnabled; } lighting;
layout(location=0) in vec3 inPosition;
layout(location=0) out vec4 color;
layout(location=1) out vec3 normal;
layout(location=2) flat out vec3 lightDirection;
layout(location=3) flat out uint stellar;
layout(location=4) flat out uvec2 material;
layout(location=5) flat out vec4 response;
layout(location=6) out vec3 viewDirection;
void main(){
  Presentation presentation=presentations.values[gl_InstanceIndex];
  vec3 position=presentation.centerRadius.xyz+inPosition*presentation.centerRadius.w;
  gl_Position=frameData.camera.viewProjection*vec4(position,1);
  color=vec4(presentation.colorDistant.rgb,presentation.colorDistant.a);
  normal=normalize(inPosition);
  lightDirection=normalize(lighting.sourceCenterExposure.xyz-presentation.centerRadius.xyz);
  stellar=floatBitsToUint(presentation.blendMetricState.w)&0x20000000u;
  material=uvec2(presentation.identity.w,presentation.identity.z);
  response=presentation.surface;
  viewDirection=-position;
}
