#version 460
struct EncodedPosition { vec4 high; vec4 low; };
struct GpuCameraData { EncodedPosition position; mat4 viewProjection; };
layout(std430,set=0,binding=0) readonly buffer Frame { GpuCameraData camera; } frameData;
layout(std430,set=0,binding=6) readonly buffer Presentation { vec4 centerRadius; vec4 colorDistant; vec4 blendMetricState; } presentation;
layout(location=0) in vec3 inPosition;
layout(location=0) out vec4 color;
void main(){
  vec3 position=presentation.centerRadius.xyz+inPosition*presentation.centerRadius.w;
  gl_Position=frameData.camera.viewProjection*vec4(position,1);
  color=vec4(presentation.colorDistant.rgb,presentation.colorDistant.a);
}
