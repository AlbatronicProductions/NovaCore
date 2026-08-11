#version 460
struct EncodedPosition { vec4 high; vec4 low; };
struct GpuCameraData { EncodedPosition position; mat4 viewProjection; };
struct Presentation { vec4 centerRadius; vec4 colorDistant; vec4 blendMetricState; uvec4 identity; vec4 surface; uvec4 hooks; vec4 ringGeometry; vec4 ringOrientation; vec4 ringColor; vec4 bodyOrientation; vec4 localDetail; };
layout(std430,set=0,binding=0) readonly buffer Frame { GpuCameraData camera; } frameData;
layout(std430,set=0,binding=6) readonly buffer Presentations { Presentation values[]; } presentations;
layout(push_constant) uniform StellarLighting { vec4 sourceCenterExposure; vec4 sourceColorAmbient; vec4 radianceGlowEnabled; } lighting;
layout(location=0) in vec3 inRing;
layout(location=0) out vec4 color;
layout(location=1) out float radial;
layout(location=2) out float cameraSide;
layout(location=3) flat out vec3 lightDirection;
layout(location=4) flat out vec3 ringNormal;
layout(location=5) flat out float bandFrequency;
vec3 RotateQuaternion(vec3 point,vec4 quaternion){return point+2.0*cross(quaternion.xyz,cross(quaternion.xyz,point)+quaternion.w*point);}
void main(){
  Presentation p=presentations.values[gl_InstanceIndex];
  if(p.hooks.y==0u){gl_Position=vec4(2,2,2,1);color=vec4(0);radial=0;cameraSide=0;lightDirection=vec3(0,1,0);ringNormal=vec3(0,1,0);bandFrequency=1;return;}
  float radius=mix(p.ringGeometry.x,p.ringGeometry.y,inRing.z)*p.centerRadius.w;
  vec3 ringLocal=RotateQuaternion(vec3(inRing.x*radius,0,inRing.y*radius),p.ringOrientation);vec3 local=RotateQuaternion(ringLocal,p.bodyOrientation);
  vec3 position=p.centerRadius.xyz+local;
  gl_Position=frameData.camera.viewProjection*vec4(position,1);
  color=p.ringColor;radial=inRing.z;cameraSide=dot(local,-p.centerRadius.xyz);
  lightDirection=normalize(lighting.sourceCenterExposure.xyz-p.centerRadius.xyz);
  ringNormal=normalize(RotateQuaternion(RotateQuaternion(vec3(0,1,0),p.ringOrientation),p.bodyOrientation));bandFrequency=p.ringGeometry.w;
}
