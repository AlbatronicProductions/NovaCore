#version 460
struct EncodedPosition { vec4 high; vec4 low; };
struct GpuCameraData { EncodedPosition position; mat4 viewProjection; };
layout(std430, set = 0, binding = 0) readonly buffer GpuFrameData { GpuCameraData camera; } frameData;
layout(location = 0) in vec3 inPositionHigh;
layout(location = 1) in vec3 inPositionLow;
void main() { gl_Position = frameData.camera.viewProjection * vec4(inPositionHigh, 1.0) +
  frameData.camera.viewProjection * vec4(inPositionLow, 0.0); }
