#version 460
#extension GL_GOOGLE_include_directive : require
#extension GL_ARB_gpu_shader_fp64 : require
#include "production_cube_surface.glsl"
#include "local_terrain.glsl"
#include "physical_surface.glsl"

struct EncodedPosition { vec4 high; vec4 low; };
struct GpuCameraData { EncodedPosition position; mat4 viewProjection; };
struct Presentation { vec4 centerRadius; vec4 colorDistant; vec4 blendMetricState; uvec4 identity; vec4 surface; uvec4 hooks; vec4 ringGeometry; vec4 ringOrientation; vec4 ringColor; vec4 bodyOrientation; vec4 localDetail; vec4 centerLow; };
layout(std430,set=0,binding=0) readonly buffer Frame { GpuCameraData camera; } frameData;
layout(std430,set=0,binding=2) readonly buffer Input { vec4 cameraHighRadiusHigh; vec4 cameraLowRadiusLow; vec4 thresholds; uvec4 controls; vec4 viewForwardHalfAngle; vec4 textureDemand; } inputData;
layout(std430,set=0,binding=6) readonly buffer Presentations { Presentation values[]; } presentations;
layout(set=0,binding=25) uniform sampler2DArray productionElevation;
layout(std430,set=0,binding=27) readonly buffer ProductionLayers { uint values[]; } productionLayers;
layout(std430,set=0,binding=32) readonly buffer AnchoredFrame { uvec4 control; uvec4 entries[]; } anchoredFrame;
#include "anchored_physical_surface.glsl"
layout(location=0) in vec2 baseUv;

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
layout(location=17) out vec3 conservativeTrianglePosition;
layout(push_constant) uniform StellarLighting { vec4 sourceCenterExposure; vec4 sourceColorAmbient; vec4 radianceGlowEnabled; } lighting;

const uint AnchoredFrameOffset=16384u;
const uint AnchoredPatchOffset=16393u;

vec3 RotateQuaternion(vec3 point,vec4 quaternion)
{
  return point+2.0*cross(quaternion.xyz,cross(quaternion.xyz,point)+quaternion.w*point);
}

void DecodeBillboardFrame(out dvec3 origin,out dvec3 east,out dvec3 north,out dvec3 up)
{
  origin=dvec3(uintBitsToFloat(anchoredFrame.entries[AnchoredFrameOffset+0u].xyz))+
    dvec3(uintBitsToFloat(anchoredFrame.entries[AnchoredFrameOffset+1u].xyz));
  east=dvec3(uintBitsToFloat(anchoredFrame.entries[AnchoredFrameOffset+2u].xyz))+
    dvec3(uintBitsToFloat(anchoredFrame.entries[AnchoredFrameOffset+3u].xyz));
  north=dvec3(uintBitsToFloat(anchoredFrame.entries[AnchoredFrameOffset+4u].xyz))+
    dvec3(uintBitsToFloat(anchoredFrame.entries[AnchoredFrameOffset+5u].xyz));
  up=dvec3(uintBitsToFloat(anchoredFrame.entries[AnchoredFrameOffset+6u].xyz))+
    dvec3(uintBitsToFloat(anchoredFrame.entries[AnchoredFrameOffset+7u].xyz));
}

vec3 BillboardRelative(dvec3 body,dvec3 origin,dvec3 east,dvec3 north,dvec3 up,vec4 orientation)
{
  dvec3 camera=dvec3(inputData.cameraHighRadiusHigh.xyz)+dvec3(inputData.cameraLowRadiusLow.xyz);
  dvec3 bodyOffset=body-origin,cameraOffset=camera-origin;
  dvec3 tangent=dvec3(dot(bodyOffset,east)-dot(cameraOffset,east),
    dot(bodyOffset,north)-dot(cameraOffset,north),dot(bodyOffset,up)-dot(cameraOffset,up));
  return RotateQuaternion(vec3(east*tangent.x+north*tangent.y+up*tangent.z),orientation);
}

void main()
{
  Presentation p=presentations.values[0];
  uint descriptor=AnchoredPatchOffset+uint(gl_InstanceIndex)*5u;
  uvec4 address=anchoredFrame.entries[descriptor+1u];
  dvec3 direction=normalize(ProductionProjectD(address,dvec2(baseUv)));
  double radius=double(inputData.cameraHighRadiusHigh.w)+double(inputData.cameraLowRadiusLow.w);
  double height=AnchoredPhysicalHeight(direction);
  dvec3 body=direction*(radius+height);
  dvec3 origin,east,north,up;DecodeBillboardFrame(origin,east,north,up);
  vec3 relative=BillboardRelative(body,origin,east,north,up,p.bodyOrientation);

  color=vec4(1.0);normal=AnchoredPhysicalNormal(direction,radius);
  lightDirection=normalize(RotateQuaternion(lighting.sourceCenterExposure.xyz-p.centerRadius.xyz,
    vec4(-p.bodyOrientation.xyz,p.bodyOrientation.w)));
  material=uvec2(p.identity.w,p.identity.z);response=p.surface;
  viewDirection=-RotateQuaternion(relative,vec4(-p.bodyOrientation.xyz,p.bodyOrientation.w));
  bodyDirection=vec3(direction);terrainHeight=float(height);
  bodyCameraHigh=inputData.cameraHighRadiusHigh.xyz;bodyCameraLow=inputData.cameraLowRadiusLow.xyz;
  localDetail=p.localDetail;productionLayer=0x40000000u;productionUv=baseUv;
  productionAddress=address;productionTransition=vec2(1.0,0.0);topologyCoordinate=baseUv;
  conservativeTrianglePosition=relative;
  gl_Position=frameData.camera.viewProjection*vec4(relative,1.0);
}
