#version 460
struct EncodedPosition { vec4 high; vec4 low; };
struct GpuCameraData { EncodedPosition position; mat4 viewProjection; };
layout(std430,set=0,binding=0) readonly buffer Frame { GpuCameraData camera; } frameData;
struct Presentation { vec4 centerRadius; vec4 colorDistant; vec4 blendMetricState; uvec4 identity; vec4 surface; uvec4 hooks; vec4 ringGeometry; vec4 ringOrientation; vec4 ringColor; vec4 bodyOrientation; vec4 localDetail; vec4 centerLow; };
layout(std430,set=0,binding=6) readonly buffer Presentations { Presentation values[]; } presentations;
layout(location=0) in vec3 inPosition;
layout(location=0) out vec3 normal;
layout(location=1) out vec3 viewDirection;
layout(location=2) flat out uint stellar;
void main(){
  Presentation p=presentations.values[gl_InstanceIndex];
  stellar=floatBitsToUint(p.blendMetricState.w)&0x20000000u;
  vec3 local=inPosition*p.centerRadius.w;
  vec3 position=p.centerRadius.xyz+(p.centerLow.xyz+local);
  gl_Position=stellar!=0u?frameData.camera.viewProjection*vec4(p.centerRadius.xyz,1.0)+
    frameData.camera.viewProjection*vec4(p.centerLow.xyz+local,0.0):vec4(2,2,2,1);
  normal=normalize(inPosition);
  viewDirection=normalize(-position);
}
