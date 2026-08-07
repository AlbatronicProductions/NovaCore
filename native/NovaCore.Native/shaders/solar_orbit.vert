#version 460
struct EncodedPosition { vec4 high; vec4 low; };
struct GpuCameraData { EncodedPosition position; mat4 viewProjection; };
layout(std430,set=0,binding=0) readonly buffer Frame { GpuCameraData camera; } frameData;
layout(location=0) in vec3 inPosition;
layout(location=0) flat out uint pathIndex;
void main(){ gl_Position=frameData.camera.viewProjection*vec4(inPosition,1.0); pathIndex=uint(gl_VertexIndex/256); }
