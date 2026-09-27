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
layout(location = 3) in vec3 inMaterial;
layout(location = 0) out vec3 color;
layout(location = 1) out vec3 normal;
layout(location = 2) out vec3 cameraRelativePosition;
layout(location = 3) flat out uint mesh;
layout(location = 4) out vec3 material;
layout(location = 5) flat out uint editorStyle;

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
  editorStyle=0u;
  // Presentation-only editor tint. All ordinary and exhaust padding semantics
  // remain unchanged; no mesh/material duplication or physical data is needed.
  if ((object.padding0 & 0xffffff00u) == 0x4e434500u) {
    uint mode = object.padding0 & 255u;
    editorStyle=mode;
    if (mode == 1u) color = mix(inColor, vec3(0.12,1.0,0.36),0.78);
    if (mode == 2u) color = mix(inColor, vec3(1.0,0.10,0.12),0.82);
    if (mode == 3u) color = mix(inColor, vec3(0.1,0.65,1.0),0.48);
    if (mode == 4u) color = vec3(1.0,0.75,0.10);
    if (mode == 5u) color = vec3(1.0,0.15,0.1);
    if (mode == 7u) color = vec3(0.2,1.0,0.55);
    if (mode == 6u) color = mix(inColor,vec3(0.20,0.70,1.0),0.7);
  }
  normal = normalize(Rotate(object.rotation, object.mesh>=1024u?inNormal/object.scale.xyz:inNormal));
  cameraRelativePosition = presented;
  mesh = object.mesh;
  material = inMaterial;
}
