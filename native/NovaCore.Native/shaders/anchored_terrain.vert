#version 460
#extension GL_GOOGLE_include_directive : require
#extension GL_ARB_gpu_shader_fp64 : require
#include "production_cube_surface.glsl"
#include "local_terrain.glsl"
#include "physical_surface.glsl"
#include "planetary_natural_terrain_surface.glsl"

struct EncodedPosition { vec4 high; vec4 low; };
struct GpuCameraData { EncodedPosition position; mat4 viewProjection; };
struct Presentation { vec4 centerRadius; vec4 colorDistant; vec4 blendMetricState; uvec4 identity; vec4 surface; uvec4 hooks; vec4 ringGeometry; vec4 ringOrientation; vec4 ringColor; vec4 bodyOrientation; vec4 localDetail; vec4 centerLow; };
layout(std430,set=0,binding=0) readonly buffer Frame { GpuCameraData camera; } frameData;
layout(std430,set=0,binding=2) readonly buffer Input { vec4 cameraHighRadiusHigh; vec4 cameraLowRadiusLow; vec4 thresholds; uvec4 controls; vec4 viewForwardHalfAngle; vec4 textureDemand; } inputData;
layout(std430,set=0,binding=6) readonly buffer Presentations { Presentation values[]; } presentations;
layout(set=0,binding=25) uniform sampler2DArray productionElevation;
layout(std430,set=0,binding=27) readonly buffer ProductionLayers { uint values[]; } productionLayers;
layout(std430,set=0,binding=32) readonly buffer AnchoredFrame { uvec4 control; uvec4 entries[]; } anchoredFrame;
layout(std430,set=0,binding=35) readonly buffer NaturalGlobalPrepared { uvec4 naturalControl; dvec4 naturalGlobalValues[]; } naturalGlobal;
layout(std430,set=0,binding=36) readonly buffer NaturalAnchoredPrepared { dvec4 naturalAnchoredValues[]; } naturalAnchored;
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
layout(location=18) out float geographicHeight;
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

vec3 NaturalPreparedBaseNormal(dvec3 direction,double radius,dvec3 preparedGradient)
{
  dvec3 east=PhysicalEastD(direction),north=normalize(cross(direction,east));
  double angle=40.0/radius;
  dvec3 leftDirection=normalize(direction-east*angle),rightDirection=normalize(direction+east*angle);
  dvec3 downDirection=normalize(direction-north*angle),upDirection=normalize(direction+north*angle);
  dvec3 left=leftDirection*(radius+CanonicalGeographicHeight(leftDirection));
  dvec3 right=rightDirection*(radius+CanonicalGeographicHeight(rightDirection));
  dvec3 down=downDirection*(radius+CanonicalGeographicHeight(downDirection));
  dvec3 up=upDirection*(radius+CanonicalGeographicHeight(upDirection));
  vec3 geographicNormal=normalize(vec3(cross(right-left,up-down))),radial=vec3(direction);
  if(dot(geographicNormal,radial)<0.0)geographicNormal=-geographicNormal;
  double radialComponent=max(double(dot(geographicNormal,radial)),1e-9);
  double eastSlope=-double(dot(geographicNormal,vec3(east)))/radialComponent+dot(preparedGradient,east);
  double northSlope=-double(dot(geographicNormal,vec3(north)))/radialComponent+dot(preparedGradient,north);
  return normalize(radial-vec3(east)*float(eastSlope)-vec3(north)*float(northSlope));
}

void main()
{
  Presentation p=presentations.values[0];
  uint descriptor=AnchoredPatchOffset+uint(gl_InstanceIndex)*5u;
  uvec4 address=anchoredFrame.entries[descriptor+1u];
  uvec4 cache=anchoredFrame.entries[descriptor+2u];
  dvec3 direction=normalize(ProductionProjectD(address,dvec2(baseUv)));
  double radius=double(inputData.cameraHighRadiusHigh.w)+double(inputData.cameraLowRadiusLow.w);
  double geographic=AnchoredGeographicHeight(direction);
  // The reusable topology samples the canonical full base field.  Frequency
  // context remains presentation metadata for bounded filtering/diagnostics;
  // it is not allowed to define another geometric surface.
  bool natural=naturalGlobal.naturalControl.x==NOVACORE_PHYSICAL_GENERATION_M12D;
  dvec4 prepared=natural?naturalAnchored.naturalAnchoredValues[cache.x*25u+uint(gl_VertexIndex)]:dvec4(0.0);
  double height=natural?max(0.0,geographic+prepared.x):CanonicalBasePhysicalHeight(direction);
  dvec3 body=direction*(radius+height);
  dvec3 origin,east,north,up;DecodeBillboardFrame(origin,east,north,up);
  vec3 relative=BillboardRelative(body,origin,east,north,up,p.bodyOrientation);

  color=vec4(1.0);normal=natural?NaturalPreparedBaseNormal(direction,radius,prepared.yzw):AnchoredBasePhysicalNormal(direction,radius);
  lightDirection=normalize(RotateQuaternion(lighting.sourceCenterExposure.xyz-p.centerRadius.xyz,
    vec4(-p.bodyOrientation.xyz,p.bodyOrientation.w)));
  material=uvec2(p.identity.w,p.identity.z);response=p.surface;
  viewDirection=-RotateQuaternion(relative,vec4(-p.bodyOrientation.xyz,p.bodyOrientation.w));
  bodyDirection=vec3(direction);terrainHeight=float(height);
  bodyCameraHigh=inputData.cameraHighRadiusHigh.xyz;bodyCameraLow=inputData.cameraLowRadiusLow.xyz;
  localDetail=p.localDetail;productionLayer=0x40000000u;productionUv=baseUv;
  productionAddress=address;productionTransition=vec2(1.0,0.0);topologyCoordinate=baseUv;
  conservativeTrianglePosition=relative;
  geographicHeight=float(geographic);
  gl_Position=frameData.camera.viewProjection*vec4(relative,1.0);
}
