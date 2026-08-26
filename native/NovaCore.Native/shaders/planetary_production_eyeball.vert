#version 460
#extension GL_GOOGLE_include_directive : require
#extension GL_ARB_gpu_shader_fp64 : require
#include "production_cube_surface.glsl"
#include "local_terrain.glsl"
struct EncodedPosition { vec4 high; vec4 low; };
struct Camera { EncodedPosition position; mat4 viewProjection; };
struct Presentation { vec4 centerRadius; vec4 colorDistant; vec4 blendMetricState; uvec4 identity; vec4 surface; uvec4 hooks; vec4 ringGeometry; vec4 ringOrientation; vec4 ringColor; vec4 bodyOrientation; vec4 localDetail; };
layout(std430,set=0,binding=0) readonly buffer Frame { Camera camera; } frameData;
layout(std430,set=0,binding=2) readonly buffer Input { vec4 cameraHighRadiusHigh; vec4 cameraLowRadiusLow; vec4 thresholds; uvec4 controls; vec4 viewForwardHalfAngle; vec4 textureDemand; } inputData;
layout(std430,set=0,binding=6) readonly buffer Presentations { Presentation values[]; } presentations;
layout(std430,set=0,binding=12) readonly buffer EyeballInput { vec4 cameraHighRadiusHigh; vec4 cameraLowRadiusLow; vec4 surface; uvec4 identity; vec4 tangentAnchorAngle; vec4 mapping; uvec4 topology; uvec4 reserved; } eye;
layout(std430,set=0,binding=27) readonly buffer ProductionLayers { uint values[]; } productionLayers;
layout(set=0,binding=24) uniform sampler2DArray productionAlbedo;
layout(set=0,binding=25) uniform sampler2DArray productionElevation;
layout(set=0,binding=26) uniform sampler2DArray productionLand;
layout(push_constant) uniform StellarLighting { vec4 sourceCenterExposure; vec4 sourceColorAmbient; vec4 radianceGlowEnabled; } lighting;
layout(location=0) in vec2 inParameter;
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
layout(location=14) flat out vec2 productionTransition;

vec3 RotateQuaternion(vec3 point,vec4 quaternion){return point+2.0*cross(quaternion.xyz,cross(quaternion.xyz,point)+quaternion.w*point);}
dvec3 TangentReference(dvec3 direction){return abs(direction.y)<.95?dvec3(0,1,0):dvec3(1,0,0);}
double RadiusD(){return double(eye.cameraHighRadiusHigh.w)+double(eye.cameraLowRadiusLow.w);}
dvec3 CameraD(){return dvec3(eye.cameraHighRadiusHigh.xyz)+dvec3(eye.cameraLowRadiusLow.xyz);}
uint ResolveLayer(vec3 direction,out vec2 localUv,out uvec4 address)
{
  uint face;vec2 uv;ProductionDirectionAddress(direction,face,uv);
  uint maximumLevel=min(inputData.controls.x,3u);
  for(int signedLevel=int(maximumLevel);signedLevel>=0;signedLevel--)
  {
    uint level=uint(signedLevel),cells=1u<<level;
    uvec2 patchCoordinate=min(uvec2(floor(uv*float(cells))),uvec2(cells-1u));
    uint ordinal=ProductionPatchOrdinal(face,level,patchCoordinate.x,patchCoordinate.y),layer=productionLayers.values[ordinal];
    if(layer!=0u){localUv=clamp(uv*float(cells)-vec2(patchCoordinate),vec2(0),vec2(1));address=uvec4(face,level,patchCoordinate);return layer;}
  }
  localUv=uv;address=uvec4(face,0u,0u,0u);return 0u;
}
float HeightAt(vec3 direction,out uint layer,out vec2 localUv)
{
  uvec4 address;layer=ResolveLayer(direction,localUv,address);if(layer==0u)return 0.0;
  vec2 stored=(vec2(4)+localUv*256.0)/264.0;
  return texture(productionElevation,vec3(stored,float(layer-1u))).r*20000.0-11000.0+LocalTerrainElevationResidual(direction);
}
float HeightAt(vec3 direction){uint layer;vec2 uv;return HeightAt(direction,layer,uv);}
void main()
{
  dvec3 anchor=normalize(dvec3(eye.tangentAnchorAngle.xyz));dvec3 east=normalize(cross(TangentReference(anchor),anchor)),north=normalize(cross(anchor,east));
  float angleF=eye.tangentAnchorAngle.w*pow(inParameter.x,eye.mapping.x);
  float azimuthF=6.2831853071795864769*inParameter.y;
  double angle=double(angleF);dvec3 tangent=east*double(cos(azimuthF))+north*double(sin(azimuthF)),direction=normalize(anchor*double(cos(angleF))+tangent*double(sin(angleF)));
  uint layer;vec2 localUv;uvec4 resolvedAddress;layer=ResolveLayer(vec3(direction),localUv,resolvedAddress);float rawHeight=layer==0u?0.0:texture(productionElevation,vec3((vec2(4)+localUv*256.0)/264.0,float(layer-1u))).r*20000.0-11000.0+LocalTerrainElevationResidual(vec3(direction));double visibleHeight=max(double(rawHeight),double(eye.surface.z));double radius=RadiusD();
  double normalAngle=max(double(eye.mapping.z)/radius,double(eye.tangentAnchorAngle.w)/double(eye.topology.z)*.18);
  dvec3 reference=TangentReference(direction),localEast=normalize(cross(reference,direction)),localNorth=normalize(cross(direction,localEast));
  dvec3 ld=normalize(direction-localEast*normalAngle),rd=normalize(direction+localEast*normalAngle),dd=normalize(direction-localNorth*normalAngle),ud=normalize(direction+localNorth*normalAngle);
  dvec3 left=ld*(radius+max(double(HeightAt(vec3(ld))),double(eye.surface.z))),right=rd*(radius+max(double(HeightAt(vec3(rd))),double(eye.surface.z)));
  dvec3 down=dd*(radius+max(double(HeightAt(vec3(dd))),double(eye.surface.z))),up=ud*(radius+max(double(HeightAt(vec3(ud))),double(eye.surface.z)));
  dvec3 surfaceNormal=normalize(cross(right-left,up-down));if(dot(surfaceNormal,direction)<0.0)surfaceNormal=-surfaceNormal;if(rawHeight<eye.surface.z)surfaceNormal=direction;
  // A sub-metre partial-transition lift makes the refinement overlay robust
  // against the coarse base triangle's interpolated depth and vanishes at
  // full ownership.
  double transitionLift=.5*double(1.0-eye.surface.w);
  dvec3 relative=direction*(radius+visibleHeight+transitionLift)-CameraD();
  Presentation p=presentations.values[0];vec3 localPosition=vec3(relative),position=RotateQuaternion(localPosition,p.bodyOrientation);
  gl_Position=frameData.camera.viewProjection*vec4(position,1);color=vec4(p.colorDistant.rgb,eye.surface.w);normal=vec3(surfaceNormal);bodyDirection=vec3(direction);terrainHeight=rawHeight;
  lightDirection=normalize(RotateQuaternion(lighting.sourceCenterExposure.xyz-p.centerRadius.xyz,vec4(-p.bodyOrientation.xyz,p.bodyOrientation.w)));
  material=uvec2(p.identity.w,p.identity.z);response=p.surface;viewDirection=-localPosition;localDetail=p.localDetail;bodyCameraHigh=eye.cameraHighRadiusHigh.xyz;bodyCameraLow=eye.cameraLowRadiusLow.xyz;
  productionLayer=layer|0x80000000u;productionUv=localUv;productionAddress=resolvedAddress;productionTransition=vec2(1.0,0.0);
}
