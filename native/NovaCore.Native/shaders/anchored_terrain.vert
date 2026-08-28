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
layout(location=0) out vec3 normal;
layout(location=1) out vec3 viewDirection;
layout(location=2) out vec4 color;
layout(location=3) flat out vec3 lightDirection;
layout(location=4) noperspective out vec3 barycentric;
layout(push_constant) uniform StellarLighting { vec4 sourceCenterExposure; vec4 sourceColorAmbient; vec4 radianceGlowEnabled; } lighting;
vec3 RotateQuaternion(vec3 point,vec4 quaternion){return point+2.0*cross(quaternion.xyz,cross(quaternion.xyz,point)+quaternion.w*point);}
void main(){Presentation p=presentations.values[0];dvec3 body=dvec3(bodyHigh.xyz)+dvec3(bodyLow.xyz);dvec3 camera=dvec3(inputData.cameraHighRadiusHigh.xyz)+dvec3(inputData.cameraLowRadiusLow.xyz);vec3 relative=RotateQuaternion(vec3(body-camera),p.bodyOrientation);normal=normalize(RotateQuaternion(bodyNormal.xyz,p.bodyOrientation));viewDirection=-relative;lightDirection=normalize(lighting.sourceCenterExposure.xyz-p.centerRadius.xyz);color=bodyColor;uint corner=uint(gl_VertexIndex)%3u;barycentric=corner==0u?vec3(1,0,0):corner==1u?vec3(0,1,0):vec3(0,0,1);gl_Position=frameData.camera.viewProjection*vec4(relative,1.0);}
