#version 460
struct EncodedPosition { vec4 high; vec4 low; };
struct RenderObject { EncodedPosition position; uint mesh; uint padding0; uint padding1; uint padding2; };
layout(std430, set = 0, binding = 0) readonly buffer RenderData { EncodedPosition camera; RenderObject objects[]; } renderData;
layout(location = 0) out vec3 color;
void main() {
  vec2 positions[3] = vec2[](vec2(0.0, -0.5), vec2(0.5, 0.5), vec2(-0.5, 0.5));
  vec3 colors[3] = vec3[](vec3(1,0,0), vec3(0,1,0), vec3(0,0,1));
  vec3 relativePosition = (renderData.objects[gl_InstanceIndex].position.high.xyz - renderData.camera.high.xyz) + (renderData.objects[gl_InstanceIndex].position.low.xyz - renderData.camera.low.xyz);
  gl_Position = vec4(positions[gl_VertexIndex] + relativePosition.xy, 0.0, 1.0);
  color = colors[gl_VertexIndex];
}
