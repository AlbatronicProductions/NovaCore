#version 460
#extension GL_GOOGLE_include_directive : require
#extension GL_ARB_gpu_shader_fp64 : require
#include "production_cube_surface.glsl"
#include "local_terrain.glsl"
#include "physical_surface.glsl"
#include "planetary_physical_authority.glsl"
#include "planetary_natural_terrain_surface.glsl"
struct EncodedPosition { vec4 high; vec4 low; };
struct GpuCameraData { EncodedPosition position; mat4 viewProjection; };
struct PlanetaryPatch { uvec4 address; vec4 centerRadius; vec4 color; uvec4 transitions; };
struct Presentation { vec4 centerRadius; vec4 colorDistant; vec4 blendMetricState; uvec4 identity; vec4 surface; uvec4 hooks; vec4 ringGeometry; vec4 ringOrientation; vec4 ringColor; vec4 bodyOrientation; vec4 localDetail; vec4 centerLow; };
layout(std430,set=0,binding=0) readonly buffer Frame { GpuCameraData camera; } frameData;
layout(std430,set=0,binding=1) readonly buffer Patches { PlanetaryPatch patches[]; } patchData;
layout(std430,set=0,binding=2) readonly buffer Input { vec4 cameraHighRadiusHigh; vec4 cameraLowRadiusLow; vec4 thresholds; uvec4 controls; vec4 viewForwardHalfAngle; vec4 textureDemand; } inputData;
layout(std430,set=0,binding=6) readonly buffer Presentations { Presentation values[]; } presentations;
layout(std430,set=0,binding=9) readonly buffer TerrainSamples { vec2 heights[]; } terrainData;
layout(std430,set=0,binding=10) readonly buffer PatchTerrainSlots { uvec2 values[]; } patchTerrain;
layout(std430,set=0,binding=35) readonly buffer NaturalGlobalPrepared { uvec4 naturalControl; dvec4 naturalValues[]; } naturalGlobal;
layout(push_constant) uniform StellarLighting { vec4 sourceCenterExposure; vec4 sourceColorAmbient; vec4 radianceGlowEnabled; } lighting;
layout(location=0) in vec2 inUv;
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
const uint GRID_VERTICES=289u;
dvec3 CubeD(uint face,double a,double b){if(face==0u)return dvec3(1,b,-a);if(face==1u)return dvec3(-1,b,a);if(face==2u)return dvec3(a,1,-b);if(face==3u)return dvec3(a,-1,b);if(face==4u)return dvec3(a,b,1);return dvec3(-a,b,-1);}
dvec3 ProjectD(uvec4 address,dvec2 local){if(inputData.controls.z==NOVACORE_PRODUCTION_TERRAIN_VERSION)return ProductionProjectD(address,local);double cells=double(1u<<address.y);dvec2 uv=(dvec2(address.zw)+local)/cells;return normalize(CubeD(address.x,2.0*uv.x-1.0,2.0*uv.y-1.0));}
dvec3 ProjectGridD(uvec4 address,uvec2 grid){if(inputData.controls.z==NOVACORE_PRODUCTION_TERRAIN_VERSION)return ProductionProjectGridD(address,grid);uint denominator=16u<<address.y;uvec2 numerator=address.zw*16u+grid;dvec2 uv=dvec2(numerator)/double(denominator);return normalize(CubeD(address.x,2.0*uv.x-1.0,2.0*uv.y-1.0));}
double RadiusD(){return double(inputData.cameraHighRadiusHigh.w)+double(inputData.cameraLowRadiusLow.w);}
dvec3 CameraD(){return dvec3(inputData.cameraHighRadiusHigh.xyz)+dvec3(inputData.cameraLowRadiusLow.xyz);}
double HeightAt(uint slot,uint x,uint y,float morph,uvec4 address,uint transitionMask)
{
  if(inputData.controls.z==NOVACORE_PRODUCTION_TERRAIN_VERSION)
  {
    dvec3 direction=ProductionProjectGridD(address,uvec2(x,y));
    // LOD controls where H is sampled and how those samples are connected; it
    // never selects a different physical field.  Shared parent/child directions
    // therefore resolve bit-identical final heights independent of morph state.
    if(naturalGlobal.naturalControl.x==NOVACORE_PHYSICAL_GENERATION_M12D)
    {
      uint ordinal=ProductionPatchOrdinal(address.x,address.y,address.z,address.w);
      dvec4 prepared=naturalGlobal.naturalValues[ordinal*GRID_VERTICES+y*17u+x];
      double geographic=CanonicalGeographicHeight(direction);
      double base=max(0.0,geographic+prepared.x);
      return max(0.0,base+EvaluateNaturalCandidateNearD(direction).height);
    }
    return CanonicalPhysicalHeight(direction);
  }
  vec2 values=terrainData.heights[slot*GRID_VERTICES+y*17u+x];return double(mix(values.x,values.y,morph));
}
float EdgeMorph(uint packedAges,uint edge)
{
  uint age=(packedAges>>(edge*6u))&63u;
  return age>=30u?1.0:float(age)/30.0;
}
float ConstrainedMorph(uint stitchMask,uint finerNeighborMask,uint packedAges,uint x,uint y,float morph)
{
  if(((stitchMask&1u)!=0u&&x==0u)||((stitchMask&2u)!=0u&&x==16u)||
     ((stitchMask&4u)!=0u&&y==0u)||((stitchMask&8u)!=0u&&y==16u))return 0.0;
  if(((finerNeighborMask&1u)!=0u&&x==0u)||((finerNeighborMask&2u)!=0u&&x==16u)||
     ((finerNeighborMask&4u)!=0u&&y==0u)||((finerNeighborMask&8u)!=0u&&y==16u))return 1.0;
  if(x==0u)morph=min(morph,EdgeMorph(packedAges,0u));
  if(x==16u)morph=min(morph,EdgeMorph(packedAges,1u));
  if(y==0u)morph=min(morph,EdgeMorph(packedAges,2u));
  if(y==16u)morph=min(morph,EdgeMorph(packedAges,3u));
  return morph;
}
vec3 RotateQuaternion(vec3 point,vec4 quaternion){return point+2.0*cross(quaternion.xyz,cross(quaternion.xyz,point)+quaternion.w*point);}
void main(){
  PlanetaryPatch p=patchData.patches[gl_InstanceIndex];Presentation presentation=presentations.values[0];vec2 stitched=inUv;uint mask=p.transitions.x;const float grid=16.0;bool subdivisionDebug=p.color.a<0.0;
  if(((mask&1u)!=0u&&inUv.x==0.0)||((mask&2u)!=0u&&inUv.x==1.0))stitched.y=floor(inUv.y*grid*.5)*2.0/grid;
  if(((mask&4u)!=0u&&inUv.y==0.0)||((mask&8u)!=0u&&inUv.y==1.0))stitched.x=floor(inUv.x*grid*.5)*2.0/grid;
  uvec2 gridCoordinate=uvec2(round(stitched*16.0));dvec3 direction=ProjectGridD(p.address,gridCoordinate);dvec3 relativePosition;dvec3 surfaceNormal=direction;terrainHeight=-1.0;bool terrain=inputData.controls.z!=0u&&inputData.thresholds.w>0.0;
  float surfaceMorph=1.0;
  if(terrain){uint x=gridCoordinate.x,y=gridCoordinate.y;uint slot=patchTerrain.values[gl_InstanceIndex].x;float temporalMorph=p.transitions.y==0u?1.0:clamp(float(inputData.controls.w-p.transitions.y)/30.0,0.0,1.0);surfaceMorph=temporalMorph;float vertexMorph=ConstrainedMorph(mask,p.transitions.w,p.transitions.z,x,y,surfaceMorph);double radius=RadiusD();double rawHeight=HeightAt(slot,x,y,vertexMorph,p.address,mask);terrainHeight=float(rawHeight);double height=rawHeight;dvec3 absolutePosition=direction*(radius+height);relativePosition=absolutePosition-CameraD();uint xl=x==0u?0u:x-1u,xr=x==16u?16u:x+1u,yd=y==0u?0u:y-1u,yu=y==16u?16u:y+1u;bool coordinatedEdge=abs(vertexMorph-surfaceMorph)>1e-6;float leftMorph=coordinatedEdge?vertexMorph:ConstrainedMorph(mask,p.transitions.w,p.transitions.z,xl,y,surfaceMorph),rightMorph=coordinatedEdge?vertexMorph:ConstrainedMorph(mask,p.transitions.w,p.transitions.z,xr,y,surfaceMorph),downMorph=coordinatedEdge?vertexMorph:ConstrainedMorph(mask,p.transitions.w,p.transitions.z,x,yd,surfaceMorph),upMorph=coordinatedEdge?vertexMorph:ConstrainedMorph(mask,p.transitions.w,p.transitions.z,x,yu,surfaceMorph);dvec3 left=ProjectGridD(p.address,uvec2(xl,y))*(radius+HeightAt(slot,xl,y,leftMorph,p.address,mask));dvec3 right=ProjectGridD(p.address,uvec2(xr,y))*(radius+HeightAt(slot,xr,y,rightMorph,p.address,mask));dvec3 down=ProjectGridD(p.address,uvec2(x,yd))*(radius+HeightAt(slot,x,yd,downMorph,p.address,mask));dvec3 up=ProjectGridD(p.address,uvec2(x,yu))*(radius+HeightAt(slot,x,yu,upMorph,p.address,mask));surfaceNormal=normalize(cross(right-left,up-down));if(dot(surfaceNormal,direction)<0.0)surfaceNormal=-surfaceNormal;surfaceMorph=vertexMorph;}
  else{double cells=double(1u<<p.address.y);dvec2 uv=(dvec2(p.address.zw)+dvec2(stitched))/cells;direction=subdivisionDebug?ProductionProjectGridD(p.address,gridCoordinate):normalize(CubeD(p.address.x,2.0*uv.x-1.0,2.0*uv.y-1.0));surfaceNormal=direction;relativePosition=dvec3(p.centerRadius.xyz)+direction*double(p.centerRadius.w);}
	  vec3 localPosition=vec3(relativePosition);vec3 position=RotateQuaternion(localPosition,presentation.bodyOrientation);gl_Position=frameData.camera.viewProjection*vec4(position,1);color=vec4(p.color.rgb,subdivisionDebug?1.0:p.color.a);normal=vec3(surfaceNormal);bodyDirection=vec3(direction);lightDirection=normalize(RotateQuaternion(lighting.sourceCenterExposure.xyz-presentation.centerRadius.xyz,vec4(-presentation.bodyOrientation.xyz,presentation.bodyOrientation.w)));material=uvec2(presentation.identity.w,presentation.identity.z);response=presentation.surface;viewDirection=-localPosition;localDetail=presentation.localDetail;bodyCameraHigh=inputData.cameraHighRadiusHigh.xyz;bodyCameraLow=inputData.cameraLowRadiusLow.xyz;productionLayer=patchTerrain.values[gl_InstanceIndex].y;productionUv=stitched;productionAddress=p.address;productionTransition=vec2(surfaceMorph,float(mask));topologyCoordinate=stitched;
	}
