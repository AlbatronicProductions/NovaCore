#version 460
layout(location=0) in vec4 positionHeight;
layout(location=1) in vec4 vertexNormal;
layout(location=2) in vec4 vertexDirection;
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
struct Camera { vec4 positionHigh; vec4 positionLow; mat4 viewProjection; };
struct Presentation { vec4 centerRadius; vec4 colorDistant; vec4 blendMetricState; uvec4 identity; vec4 surface; uvec4 hooks; vec4 ringGeometry; vec4 ringOrientation; vec4 ringColor; vec4 bodyOrientation; };
layout(std430,set=0,binding=0) readonly buffer Frame { Camera camera; } frameData;
layout(std430,set=0,binding=6) readonly buffer Presentations { Presentation values[]; } presentations;
layout(std430,set=0,binding=12) readonly buffer EyeballInput { vec4 cameraHighRadiusHigh; vec4 cameraLowRadiusLow; vec4 surface; uvec4 identity; vec4 viewForwardMargin; vec4 mapping; uvec4 topology; uvec4 reserved; } eye;
layout(push_constant) uniform StellarLighting { vec4 sourceCenterExposure; vec4 sourceColorAmbient; vec4 radianceGlowEnabled; } lighting;
vec3 RotateQuaternion(vec3 point,vec4 quaternion){return point+2.0*cross(quaternion.xyz,cross(quaternion.xyz,point)+quaternion.w*point);}
void main(){Presentation p=presentations.values[0];vec3 localPosition=positionHeight.xyz;vec3 position=RotateQuaternion(localPosition,p.bodyOrientation);gl_Position=frameData.camera.viewProjection*vec4(position,1);color=vec4(p.colorDistant.rgb,eye.surface.w);normal=vertexNormal.xyz;bodyDirection=vertexDirection.xyz;terrainHeight=positionHeight.w;lightDirection=normalize(RotateQuaternion(lighting.sourceCenterExposure.xyz-p.centerRadius.xyz,vec4(-p.bodyOrientation.xyz,p.bodyOrientation.w)));material=uvec2(p.identity.w,p.identity.z);response=p.surface;viewDirection=-localPosition;bodyCameraHigh=eye.cameraHighRadiusHigh.xyz;bodyCameraLow=eye.cameraLowRadiusLow.xyz;}
