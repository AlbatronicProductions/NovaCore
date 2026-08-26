#version 460

struct EncodedPosition { vec4 high; vec4 low; };
struct GpuCameraData { EncodedPosition position; mat4 viewProjection; };
struct GpuRenderObject {
  EncodedPosition position;
  vec4 rotation;
  vec4 scale;
  uint mesh;
  uint padding0;
  uint padding1;
  uint padding2;
};

layout(std430, set = 0, binding = 0) readonly buffer GpuFrameData {
  GpuCameraData camera;
  GpuRenderObject objects[];
} frameData;

layout(location = 0) in vec3 inPosition;
layout(location = 1) in vec3 inColor;
layout(location = 2) in vec3 inNormal;
layout(location = 0) out vec3 color;
layout(location = 1) out vec3 normal;
layout(location = 2) out vec3 cameraRelativePosition;
layout(location = 3) flat out uint mesh;

// Right-handed Hamilton rotation, XYZW quaternion: q * v * conjugate(q).
vec3 Rotate(vec4 q, vec3 v) {
  return v + 2.0 * cross(q.xyz, cross(q.xyz, v) + q.w * v);
}

void main() {
  GpuRenderObject object = frameData.objects[gl_InstanceIndex];
  // Object transport is already camera relative; FP64 root subtraction occurred on the CPU.
  vec3 relativePosition = object.position.high.xyz + object.position.low.xyz;
  vec3 local = Rotate(object.rotation, inPosition * object.scale.xyz);
  vec3 presented = local + relativePosition;
  gl_Position = frameData.camera.viewProjection * vec4(presented, 1.0);
  color = inColor;
  normal = normalize(Rotate(object.rotation, inNormal));
  cameraRelativePosition = presented;
  mesh = object.mesh;
}
