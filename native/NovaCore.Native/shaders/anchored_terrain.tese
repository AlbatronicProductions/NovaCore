#version 460
#extension GL_GOOGLE_include_directive : require
#extension GL_ARB_gpu_shader_fp64 : require
#include "production_cube_surface.glsl"

struct EncodedPosition { vec4 high; vec4 low; };
struct GpuCameraData { EncodedPosition position; mat4 viewProjection; };
struct Presentation { vec4 centerRadius; vec4 colorDistant; vec4 blendMetricState; uvec4 identity; vec4 surface; uvec4 hooks; vec4 ringGeometry; vec4 ringOrientation; vec4 ringColor; vec4 bodyOrientation; vec4 localDetail; vec4 centerLow; };
layout(std430,set=0,binding=0) readonly buffer Frame { GpuCameraData camera; } frameData;
layout(std430,set=0,binding=2) readonly buffer Input { vec4 cameraHighRadiusHigh; vec4 cameraLowRadiusLow; vec4 thresholds; uvec4 controls; vec4 viewForwardHalfAngle; vec4 textureDemand; } inputData;
layout(std430,set=0,binding=6) readonly buffer Presentations { Presentation values[]; } presentations;
layout(std430,set=0,binding=32) readonly buffer AnchoredFrame { uvec4 control; uvec4 entries[]; } anchoredFrame;

// The reusable base templates are outward in canonical model space.  Vulkan's
// tessellator emits the barycentric subdivision with the opposite screen-space
// parity for this projection when the domain is declared ccw; request cw so
// the evaluated physical surface remains CCW after the accepted view/projection
// transform and matches the production VK_FRONT_FACE_COUNTER_CLOCKWISE contract.
layout(triangles,equal_spacing,cw) in;
layout(location=0) in vec4 inColor[]; layout(location=0) out vec4 color;
layout(location=1) in vec3 inNormal[]; layout(location=1) out vec3 normal;
layout(location=2) flat in vec3 inLightDirection[]; layout(location=2) flat out vec3 lightDirection;
layout(location=3) flat in uvec2 inMaterial[]; layout(location=3) flat out uvec2 material;
layout(location=4) flat in vec4 inResponse[]; layout(location=4) flat out vec4 response;
layout(location=5) in vec3 inViewDirection[]; layout(location=5) out vec3 viewDirection;
layout(location=6) in vec3 inBodyDirection[]; layout(location=6) out vec3 bodyDirection;
layout(location=7) in float inTerrainHeight[]; layout(location=7) out float terrainHeight;
layout(location=8) flat in vec3 inBodyCameraHigh[]; layout(location=8) flat out vec3 bodyCameraHigh;
layout(location=9) flat in vec3 inBodyCameraLow[]; layout(location=9) flat out vec3 bodyCameraLow;
layout(location=10) flat in vec4 inLocalDetail[]; layout(location=10) flat out vec4 localDetail;
layout(location=11) flat in uint inProductionLayer[]; layout(location=11) flat out uint productionLayer;
layout(location=12) in vec2 inProductionUv[]; layout(location=12) out vec2 productionUv;
layout(location=13) flat in uvec4 inProductionAddress[]; layout(location=13) flat out uvec4 productionAddress;
layout(location=14) in vec2 inProductionTransition[]; layout(location=14) out vec2 productionTransition;
layout(location=15) in vec2 inTopologyCoordinate[]; layout(location=15) out vec2 topologyCoordinate;

vec3 RotateQuaternion(vec3 point,vec4 quaternion){return point+2.0*cross(quaternion.xyz,cross(quaternion.xyz,point)+quaternion.w*point);}
vec3 BillboardRelative(dvec3 body)
{
  const uint frameOffset=16384u;
  dvec3 origin=dvec3(uintBitsToFloat(anchoredFrame.entries[frameOffset+0u].xyz))+dvec3(uintBitsToFloat(anchoredFrame.entries[frameOffset+1u].xyz));
  dvec3 east=dvec3(uintBitsToFloat(anchoredFrame.entries[frameOffset+2u].xyz))+dvec3(uintBitsToFloat(anchoredFrame.entries[frameOffset+3u].xyz));
  dvec3 north=dvec3(uintBitsToFloat(anchoredFrame.entries[frameOffset+4u].xyz))+dvec3(uintBitsToFloat(anchoredFrame.entries[frameOffset+5u].xyz));
  dvec3 up=dvec3(uintBitsToFloat(anchoredFrame.entries[frameOffset+6u].xyz))+dvec3(uintBitsToFloat(anchoredFrame.entries[frameOffset+7u].xyz));
  dvec3 camera=dvec3(inputData.cameraHighRadiusHigh.xyz)+dvec3(inputData.cameraLowRadiusLow.xyz);
  dvec3 bodyOffset=body-origin,cameraOffset=camera-origin;
  dvec3 tangent=dvec3(dot(bodyOffset,east)-dot(cameraOffset,east),dot(bodyOffset,north)-dot(cameraOffset,north),dot(bodyOffset,up)-dot(cameraOffset,up));
  return vec3(east*tangent.x+north*tangent.y+up*tangent.z);
}

void main()
{
  vec3 barycentric=gl_TessCoord;
  vec2 uv=inProductionUv[0]*barycentric.x+inProductionUv[1]*barycentric.y+inProductionUv[2]*barycentric.z;
  uvec4 address=inProductionAddress[0];dvec3 direction=normalize(ProductionProjectD(address,dvec2(uv)));
  double radius=double(inputData.cameraHighRadiusHigh.w)+double(inputData.cameraLowRadiusLow.w);
  float height=inTerrainHeight[0]*barycentric.x+inTerrainHeight[1]*barycentric.y+inTerrainHeight[2]*barycentric.z;
  dvec3 body=direction*(radius+double(height));
  vec3 relativeBody=BillboardRelative(body);Presentation p=presentations.values[0];
  vec3 relative=RotateQuaternion(relativeBody,p.bodyOrientation);
  color=inColor[0];normal=normalize(inNormal[0]*barycentric.x+inNormal[1]*barycentric.y+inNormal[2]*barycentric.z);lightDirection=inLightDirection[0];
  material=inMaterial[0];response=inResponse[0];viewDirection=-relativeBody;bodyDirection=vec3(direction);
  terrainHeight=height;bodyCameraHigh=inBodyCameraHigh[0];bodyCameraLow=inBodyCameraLow[0];
  localDetail=inLocalDetail[0];productionLayer=inProductionLayer[0];productionUv=uv;
  productionAddress=address;productionTransition=inProductionTransition[0];topologyCoordinate=uv;
  gl_Position=frameData.camera.viewProjection*vec4(relative,1.0);
}
