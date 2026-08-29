#version 460
#extension GL_ARB_gpu_shader_fp64 : require
struct EncodedPosition { vec4 high; vec4 low; };
struct GpuCameraData { EncodedPosition position; mat4 viewProjection; };
struct Presentation { vec4 centerRadius; vec4 colorDistant; vec4 blendMetricState; uvec4 identity; vec4 surface; uvec4 hooks; vec4 ringGeometry; vec4 ringOrientation; vec4 ringColor; vec4 bodyOrientation; vec4 localDetail; };
layout(std430,set=0,binding=0) readonly buffer Frame { GpuCameraData camera; } frameData;
layout(std430,set=0,binding=2) readonly buffer Input { vec4 cameraHighRadiusHigh; vec4 cameraLowRadiusLow; vec4 thresholds; uvec4 controls; vec4 viewForwardHalfAngle; vec4 textureDemand; } inputData;
layout(std430,set=0,binding=6) readonly buffer Presentations { Presentation values[]; } presentations;
layout(location=0) in vec4 bodyHigh;
layout(location=1) in vec4 bodyLow;
layout(location=2) in vec4 bodyNormal;
layout(location=3) in vec4 bodyColor;
layout(location=0) out vec4 color;
layout(location=1) out vec3 normal;
layout(location=2) flat out vec3 lightDirection;
layout(location=3) flat out uvec2 material;
layout(location=4) flat out vec4 response;
layout(location=5) out vec3 viewDirection;
layout(location=6) out vec3 bodyDirection;
layout(location=7) out float terrainHeight;
layout(location=8) flat out vec3 bodyCameraHigh;
layout(location=9) flat out vec3 bodyCameraLow;
layout(location=10) flat out vec4 localDetail;
layout(location=11) flat out uint productionLayer;
layout(location=12) out vec2 productionUv;
layout(location=13) flat out uvec4 productionAddress;
layout(location=14) out vec2 productionTransition;
layout(location=15) out vec2 topologyCoordinate;
layout(push_constant) uniform StellarLighting { vec4 sourceCenterExposure; vec4 sourceColorAmbient; vec4 radianceGlowEnabled; } lighting;
vec3 RotateQuaternion(vec3 point,vec4 quaternion){return point+2.0*cross(quaternion.xyz,cross(quaternion.xyz,point)+quaternion.w*point);}
void main()
{
  Presentation p=presentations.values[0];
  dvec3 body=dvec3(bodyHigh.xyz)+dvec3(bodyLow.xyz);
  dvec3 camera=dvec3(inputData.cameraHighRadiusHigh.xyz)+dvec3(inputData.cameraLowRadiusLow.xyz);
  dvec3 relativeBody=body-camera;
  vec3 relative=RotateQuaternion(vec3(relativeBody),p.bodyOrientation);
  bodyDirection=normalize(vec3(body));
  normal=normalize(bodyNormal.xyz);
  viewDirection=-vec3(relativeBody);
  lightDirection=normalize(RotateQuaternion(lighting.sourceCenterExposure.xyz-p.centerRadius.xyz,
    vec4(-p.bodyOrientation.xyz,p.bodyOrientation.w)));
  color=vec4(1.0);
  material=uvec2(p.identity.w,p.identity.z);
  response=p.surface;
  terrainHeight=float(length(body)-(double(inputData.cameraHighRadiusHigh.w)+double(inputData.cameraLowRadiusLow.w)));
  bodyCameraHigh=inputData.cameraHighRadiusHigh.xyz;
  bodyCameraLow=inputData.cameraLowRadiusLow.xyz;
  localDetail=p.localDetail;
  productionLayer=0x40000000u;
  productionUv=vec2(0.0);
  productionAddress=uvec4(0u);
  productionTransition=vec2(1.0,0.0);
  topologyCoordinate=vec2(0.5);
  gl_Position=frameData.camera.viewProjection*vec4(relative,1.0);
}
