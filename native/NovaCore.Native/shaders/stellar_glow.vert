#version 460
struct EncodedPosition { vec4 high; vec4 low; };
struct GpuCameraData { EncodedPosition position; mat4 viewProjection; };
layout(std430,set=0,binding=0) readonly buffer Frame { GpuCameraData camera; } frameData;
struct Presentation { vec4 centerRadius; vec4 colorDistant; vec4 blendMetricState; uvec4 identity; vec4 surface; uvec4 hooks; vec4 ringGeometry; vec4 ringOrientation; vec4 ringColor; };
layout(std430,set=0,binding=6) readonly buffer Presentations { Presentation values[]; } presentations;
layout(location=0) out vec2 local;
layout(location=1) flat out uint stellar;
void main(){
  Presentation p=presentations.values[gl_InstanceIndex];
  stellar=floatBitsToUint(p.blendMetricState.w)&0x20000000u;
  vec2 corners[6]=vec2[6](vec2(-1,-1),vec2(1,-1),vec2(-1,1),vec2(-1,1),vec2(1,-1),vec2(1,1));
  local=corners[gl_VertexIndex];
  vec4 center=frameData.camera.viewProjection*vec4(p.centerRadius.xyz,1);
  float scaleX=length(vec3(frameData.camera.viewProjection[0][0],frameData.camera.viewProjection[1][0],frameData.camera.viewProjection[2][0]));
  float scaleY=length(vec3(frameData.camera.viewProjection[0][1],frameData.camera.viewProjection[1][1],frameData.camera.viewProjection[2][1]));
  float angular=p.centerRadius.w/max(length(p.centerRadius.xyz),p.centerRadius.w*1.001)*max(scaleX,scaleY);
  float extent=max(angular*2.0,.012);
  vec2 ndc=center.xy/max(abs(center.w),1e-6)+local*extent;
  gl_Position=stellar!=0u?vec4(ndc,0,1):vec4(2,2,2,1);
}
