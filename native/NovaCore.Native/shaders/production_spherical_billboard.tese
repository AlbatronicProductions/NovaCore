#version 460
#extension GL_GOOGLE_include_directive : require
#extension GL_ARB_gpu_shader_fp64 : require
#include "production_cube_surface.glsl"
#include "local_terrain.glsl"
#include "physical_surface.glsl"
#include "planetary_physical_authority.glsl"
#include "planetary_natural_terrain_surface.glsl"

struct EncodedPosition{vec4 high;vec4 low;};
struct GpuCameraData{EncodedPosition position;mat4 viewProjection;};
struct Presentation{vec4 centerRadius;vec4 colorDistant;vec4 blendMetricState;uvec4 identity;vec4 surface;uvec4 hooks;vec4 ringGeometry;vec4 ringOrientation;vec4 ringColor;vec4 bodyOrientation;vec4 localDetail; vec4 centerLow;};
layout(set=0,binding=0,std430)readonly buffer Frame{GpuCameraData camera;}frameData;
layout(set=0,binding=2,std430)readonly buffer Input{vec4 cameraHighRadiusHigh;vec4 cameraLowRadiusLow;vec4 thresholds;uvec4 controls;vec4 viewForwardHalfAngle;vec4 textureDemand;}inputData;
layout(set=0,binding=6,std430)readonly buffer Presentations{Presentation values[];}presentations;

layout(triangles,equal_spacing,cw) in;
layout(location=0) in vec4 i0[];layout(location=0) out vec4 color;
layout(location=1) in vec3 i1[];layout(location=1) out vec3 normal;
layout(location=2) flat in vec3 i2[];layout(location=2) flat out vec3 lightDirection;
layout(location=3) flat in uvec2 i3[];layout(location=3) flat out uvec2 material;
layout(location=4) flat in vec4 i4[];layout(location=4) flat out vec4 response;
layout(location=5) in vec3 i5[];layout(location=5) out vec3 viewDirection;
layout(location=6) in vec3 i6[];layout(location=6) out vec3 bodyDirection;
layout(location=7) in float i7[];layout(location=7) out float terrainHeight;
layout(location=8) flat in vec3 i8[];layout(location=8) flat out vec3 bodyCameraHigh;
layout(location=9) flat in vec3 i9[];layout(location=9) flat out vec3 bodyCameraLow;
layout(location=10) flat in vec4 i10[];layout(location=10) flat out vec4 localDetail;
layout(location=11) flat in uint i11[];layout(location=11) flat out uint productionLayer;
layout(location=12) in vec2 i12[];layout(location=12) out vec2 productionUv;
layout(location=13) flat in uvec4 i13[];layout(location=13) flat out uvec4 productionAddress;
layout(location=14) in vec2 i14[];layout(location=14) out vec2 productionTransition;
layout(location=15) in vec2 i15[];layout(location=15) out vec2 topologyCoordinate;
layout(location=18) in float i18[];

vec3 RotateQuaternion(vec3 point,vec4 quaternion){return point+2.0*cross(quaternion.xyz,cross(quaternion.xyz,point)+quaternion.w*point);}

double CandidateBaseHeightD(dvec3 direction)
{
  double geographic=CanonicalGeographicHeight(direction);
  NaturalTerrainCompositionSampleD value=EvaluateNaturalCandidateD(direction);
  return max(0.0,geographic+value.macro.height+value.meso.height);
}

double CandidatePhysicalHeightD(dvec3 direction)
{
  NaturalTerrainCompositionSampleD value=EvaluateNaturalCandidateD(direction);
  return max(0.0,CandidateBaseHeightD(direction)+value.nearField.height);
}

vec3 CandidatePhysicalNormalD(dvec3 direction,double radius)
{
  direction=normalize(direction);
  dvec3 east=PhysicalEastD(direction),north=normalize(cross(direction,east));
  double angle=NOVACORE_NORMAL_SAMPLE_RADIUS/radius;
  dvec3 leftDirection=normalize(direction-east*angle),rightDirection=normalize(direction+east*angle);
  dvec3 downDirection=normalize(direction-north*angle),upDirection=normalize(direction+north*angle);
  double leftHeight=CandidateBaseHeightD(leftDirection),rightHeight=CandidateBaseHeightD(rightDirection);
  double downHeight=CandidateBaseHeightD(downDirection),upHeight=CandidateBaseHeightD(upDirection);
  dvec3 left=leftDirection*(radius+leftHeight),right=rightDirection*(radius+rightHeight);
  dvec3 down=downDirection*(radius+downHeight),up=upDirection*(radius+upHeight);
  dvec3 baseNormal=normalize(cross(right-left,up-down));if(dot(baseNormal,direction)<0.0)baseNormal=-baseNormal;
  NaturalTerrainFieldSampleD nearValue=EvaluateNaturalCandidateNearD(direction);
  double radial=max(dot(baseNormal,direction),1e-9);
  double eastSlope=-dot(baseNormal,east)/radial+dot(nearValue.bodyGradient,east);
  double northSlope=-dot(baseNormal,north)/radial+dot(nearValue.bodyGradient,north);
  return normalize(vec3(direction-east*eastSlope-north*northSlope));
}

void main()
{
  vec3 barycentric=gl_TessCoord;
  dvec3 direction=normalize(dvec3(i6[0])*double(barycentric.x)+dvec3(i6[1])*double(barycentric.y)+dvec3(i6[2])*double(barycentric.z));
  double radius=double(inputData.cameraHighRadiusHigh.w)+double(inputData.cameraLowRadiusLow.w);
  bool refined=max(gl_TessLevelInner[0],max(gl_TessLevelOuter[0],max(gl_TessLevelOuter[1],gl_TessLevelOuter[2])))>1.0;
  double height=refined?CandidatePhysicalHeightD(direction):double(i7[0]*barycentric.x+i7[1]*barycentric.y+i7[2]*barycentric.z);
  vec3 surfaceNormal=refined?CandidatePhysicalNormalD(direction,radius):normalize(i1[0]*barycentric.x+i1[1]*barycentric.y+i1[2]*barycentric.z);
  dvec3 camera=dvec3(inputData.cameraHighRadiusHigh.xyz)+dvec3(inputData.cameraLowRadiusLow.xyz);
  dvec3 body=direction*(radius+height);vec3 relativeBody=vec3(body-camera);Presentation p=presentations.values[0];
  vec3 relative=RotateQuaternion(relativeBody,p.bodyOrientation);
  uint face;dvec2 faceUv;ProductionDirectionAddressD(direction,face,faceUv);uint level=min(uint(max(inputData.textureDemand.w,0.0)),2u),cells=1u<<level;uvec2 cell=min(uvec2(faceUv*double(cells)),uvec2(cells-1u));vec2 local=vec2(faceUv*double(cells)-dvec2(cell));
  gl_Position=frameData.camera.viewProjection*vec4(relative,1.0);
  color=i0[0];normal=surfaceNormal;lightDirection=i2[0];material=i3[0];response=i4[0];viewDirection=-relativeBody;bodyDirection=vec3(direction);terrainHeight=float(height);bodyCameraHigh=i8[0];bodyCameraLow=i9[0];localDetail=i10[0];productionLayer=i11[0];productionUv=local;productionAddress=uvec4(face,level,cell);productionTransition=i14[0];topologyCoordinate=local;
}
