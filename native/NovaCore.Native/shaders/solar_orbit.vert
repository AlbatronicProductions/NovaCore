#version 460
struct EncodedPosition { vec4 high; vec4 low; };
struct GpuCameraData { EncodedPosition position; mat4 viewProjection; };
layout(std430,set=0,binding=0) readonly buffer Frame { GpuCameraData camera; } frameData;
struct Presentation { vec4 centerRadius; vec4 colorDistant; vec4 blendMetricState; uvec4 identity; vec4 surface; uvec4 hooks; vec4 ringGeometry; vec4 ringOrientation; vec4 ringColor; vec4 bodyOrientation; vec4 localDetail; };
layout(std430,set=0,binding=6) readonly buffer Presentations { Presentation values[]; } presentations;
layout(location=0) in vec3 inPosition;
layout(location=0) flat out uint pathIndex;
layout(location=1) flat out float opacity;
void main(){
  pathIndex=uint(gl_VertexIndex/256);opacity=0.0;
  uint ordinal=pathIndex+2u;
  for(uint index=0u;index<10u;index++){
    uint enabled=floatBitsToUint(presentations.values[index].blendMetricState.w);
    if((enabled&255u)==ordinal){opacity=float((enabled>>8u)&255u)/255.0;break;}
  }
  gl_Position=opacity>0.0?frameData.camera.viewProjection*vec4(inPosition,1.0):vec4(2.0,2.0,2.0,1.0);
}
