#version 460
struct EncodedPosition { vec4 high; vec4 low; };
struct GpuCameraData { EncodedPosition position; mat4 viewProjection; };
layout(std430,set=0,binding=0) readonly buffer Frame { GpuCameraData camera; } frameData;
struct Presentation { vec4 centerRadius; vec4 colorDistant; vec4 blendMetricState; uvec4 identity; vec4 surface; uvec4 hooks; vec4 ringGeometry; vec4 ringOrientation; vec4 ringColor; vec4 bodyOrientation; vec4 localDetail; };
layout(std430,set=0,binding=6) readonly buffer Presentations { Presentation values[]; } presentations;
layout(std430,set=0,binding=2) readonly buffer PlanetaryInput { vec4 cameraHighRadiusHigh; vec4 cameraLowRadiusLow; vec4 thresholds; uvec4 controls; vec4 viewForwardHalfAngle; vec4 textureDemand; } planetaryInput;
layout(push_constant) uniform StellarLighting { vec4 sourceCenterExposure; vec4 sourceColorAmbient; vec4 radianceGlowEnabled; } lighting;
layout(location=0) in vec3 inPosition;
layout(location=0) out vec4 color;
layout(location=1) out vec3 normal;
layout(location=2) flat out vec3 lightDirection;
layout(location=3) flat out uint stellar;
layout(location=4) flat out uvec2 material;
layout(location=5) flat out vec4 response;
layout(location=6) out vec3 viewDirection;
layout(location=7) flat out float surfaceAltitudeMetres;
vec3 RotateQuaternion(vec3 point,vec4 quaternion){return point+2.0*cross(quaternion.xyz,cross(quaternion.xyz,point)+quaternion.w*point);}
vec3 InverseRotateQuaternion(vec3 point,vec4 quaternion){return RotateQuaternion(point,vec4(-quaternion.xyz,quaternion.w));}
void main(){
  Presentation presentation=presentations.values[gl_InstanceIndex];
  vec3 bodyLocalPosition=inPosition*presentation.centerRadius.w;
  vec3 position=presentation.centerRadius.xyz+RotateQuaternion(bodyLocalPosition,presentation.bodyOrientation);
  gl_Position=frameData.camera.viewProjection*vec4(position,1);
  color=vec4(presentation.colorDistant.rgb,1.0);
  normal=normalize(inPosition);
  lightDirection=normalize(InverseRotateQuaternion(lighting.sourceCenterExposure.xyz-presentation.centerRadius.xyz,presentation.bodyOrientation));
  stellar=floatBitsToUint(presentation.blendMetricState.w)&0x20000000u;
  material=uvec2(presentation.identity.w,presentation.identity.z);
  response=presentation.surface;
  viewDirection=InverseRotateQuaternion(-position,presentation.bodyOrientation);
  surfaceAltitudeMetres=max((presentation.blendMetricState.y-1.0)*presentation.centerRadius.w,0.0);
}
