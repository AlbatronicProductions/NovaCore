#version 460
#extension GL_GOOGLE_include_directive : require
#extension GL_ARB_gpu_shader_fp64 : require
#include "production_cube_surface.glsl"
#include "local_terrain.glsl"
#include "physical_surface.glsl"
#include "planetary_physical_authority.glsl"
#include "planetary_natural_terrain_surface.glsl"
#include "production_spherical_billboard_physical.glsl"

struct EncodedPosition{vec4 high;vec4 low;};
struct GpuCameraData{EncodedPosition position;mat4 viewProjection;};
struct Presentation{vec4 centerRadius;vec4 colorDistant;vec4 blendMetricState;uvec4 identity;vec4 surface;uvec4 hooks;vec4 ringGeometry;vec4 ringOrientation;vec4 ringColor;vec4 bodyOrientation;vec4 localDetail; vec4 centerLow;};
layout(set=0,binding=0,std430)readonly buffer Frame{GpuCameraData camera;}frameData;
layout(set=0,binding=2,std430)readonly buffer Input{vec4 cameraHighRadiusHigh;vec4 cameraLowRadiusLow;vec4 thresholds;uvec4 controls;vec4 viewForwardHalfAngle;vec4 textureDemand;}inputData;
layout(set=0,binding=6,std430)readonly buffer Presentations{Presentation values[];}presentations;

layout(triangles,fractional_odd_spacing,cw) in;
layout(location=0) out vec4 color;
layout(location=1) in vec3 i1[];layout(location=1) out vec3 normal;
layout(location=2) flat in vec3 i2[];layout(location=2) flat out vec3 lightDirection;
layout(location=3) flat out uvec2 material;
layout(location=4) flat out vec4 response;
layout(location=5) in vec3 i5[];layout(location=5) out vec3 viewDirection;
layout(location=6) in vec3 i6[];layout(location=6) out vec3 bodyDirection;
layout(location=7) in float i7[];layout(location=7) out float terrainHeight;
layout(location=8) flat out vec3 bodyCameraHigh;
layout(location=9) flat out vec3 bodyCameraLow;
layout(location=10) flat out vec4 localDetail;
layout(location=11) flat out uint productionLayer;
layout(location=12) out vec2 productionUv;
layout(location=13) flat out uvec4 productionAddress;
layout(location=14) out vec2 productionTransition;
layout(location=15) out vec2 topologyCoordinate;

vec3 RotateQuaternion(vec3 point,vec4 quaternion){return point+2.0*cross(quaternion.xyz,cross(quaternion.xyz,point)+quaternion.w*point);}

void main()
{
  vec3 barycentric=gl_TessCoord;
  dvec3 camera=dvec3(inputData.cameraHighRadiusHigh.xyz)+dvec3(inputData.cameraLowRadiusLow.xyz);
  vec3 interpolatedView=i5[0]*barycentric.x+i5[1]*barycentric.y+i5[2]*barycentric.z;
  // The prepared vertices are already the displaced physical base. Interpolate
  // that small camera-relative quantity and recover its body-fixed anchor for
  // height/material addressing. Normalizing interpolated corner directions and
  // rebuilding a planet-radius point changes the coarse spherical triangle and
  // lets presentation geography slide when its pupil/topology changes.
  dvec3 preparedBody=camera-dvec3(interpolatedView);
  double preparedRadiusSquared=dot(preparedBody,preparedBody);
  dvec3 direction=preparedRadiusSquared>1e-18?
    preparedBody*inversesqrt(preparedRadiusSquared):normalize(dvec3(i6[0]));
  double baseHeight=double(i7[0]*barycentric.x+i7[1]*barycentric.y+i7[2]*barycentric.z);
  vec3 baseNormal=normalize(i1[0]*barycentric.x+i1[1]*barycentric.y+i1[2]*barycentric.z);
  double localWeight=preparedRadiusSquared>1e-18?
    1.0-smoothstep(40.0,50.0,double(length(interpolatedView))):0.0;
  // Match the production scale-mesh responsibility: the prepared base is the
  // complete terrain outside the bounded refinement footprint.  Do not run
  // the expensive near-field authority and discard it by multiplying by zero.
  // KSA's TES takes the same early-out boundary before detailed displacement;
  // NovaCore keeps evaluating the common outputs below because its fragment ABI
  // still consumes the anchored body direction and prepared base normal.
  double nearHeight=0.0;
  dvec3 nearGradient=dvec3(0.0);
  if(localWeight>0.0){
    NaturalTerrainFieldSampleD nearValue=EvaluateNaturalCandidateNearD(direction);
    nearHeight=nearValue.height;
    nearGradient=nearValue.bodyGradient;
  }
  double height=max(0.0,baseHeight+nearHeight*localWeight);
  dvec3 east=PhysicalEastD(direction),north=normalize(cross(direction,east));
  double radial=max(dot(dvec3(baseNormal),direction),1e-9);
  double eastSlope=-dot(dvec3(baseNormal),east)/radial+dot(nearGradient,east)*localWeight;
  double northSlope=-dot(dvec3(baseNormal),north)/radial+dot(nearGradient,north)*localWeight;
  vec3 surfaceNormal=normalize(vec3(direction-east*eastSlope-north*northSlope));
  double localDisplacement=height-baseHeight;
  vec3 relativeBody=-interpolatedView+vec3(direction*localDisplacement);Presentation p=presentations.values[0];
  vec3 localRelative=preparedRadiusSquared>1e-18?
    RotateQuaternion(vec3(direction*localDisplacement),p.bodyOrientation):vec3(0.0);
  uint face;dvec2 faceUv;ProductionDirectionAddressD(direction,face,faceUv);uint level=min(uint(max(inputData.textureDemand.w,0.0)),2u),cells=1u<<level;uvec2 cell=min(uvec2(faceUv*double(cells)),uvec2(cells-1u));vec2 local=vec2(faceUv*double(cells)-dvec2(cell));
  vec4 baseClip=gl_in[0].gl_Position*barycentric.x+
    gl_in[1].gl_Position*barycentric.y+gl_in[2].gl_Position*barycentric.z;
  gl_Position=baseClip+frameData.camera.viewProjection*vec4(localRelative,0.0);
  // Same frame/body values previously repeated at every VS/TCS control point.
  // The fragment interface and all physical/interpolated outputs are unchanged.
  color=vec4(1);normal=surfaceNormal;lightDirection=i2[0];material=uvec2(p.identity.w,p.identity.z);response=p.surface;viewDirection=-relativeBody;bodyDirection=vec3(direction);terrainHeight=float(height);bodyCameraHigh=inputData.cameraHighRadiusHigh.xyz;bodyCameraLow=inputData.cameraLowRadiusLow.xyz;localDetail=p.localDetail;productionLayer=0x40000000u;productionUv=local;productionAddress=uvec4(face,level,cell);productionTransition=vec2(1,0);topologyCoordinate=local;
}
