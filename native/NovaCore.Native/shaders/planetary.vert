#version 460
#extension GL_ARB_gpu_shader_fp64 : require
struct EncodedPosition { vec4 high; vec4 low; };
struct GpuCameraData { EncodedPosition position; mat4 viewProjection; };
struct PlanetaryPatch { uvec4 address; vec4 centerRadius; vec4 color; uvec4 transitions; };
struct Presentation { vec4 centerRadius; vec4 colorDistant; vec4 blendMetricState; uvec4 identity; vec4 surface; uvec4 hooks; vec4 ringGeometry; vec4 ringOrientation; vec4 ringColor; vec4 bodyOrientation; vec4 localDetail; };
struct Environment { vec4 centerRadius; uvec4 identity; vec4 atmosphere; vec4 scattering; vec4 clouds; vec4 cloudShape; vec4 ocean; vec4 oceanColorExposure; };
layout(std430,set=0,binding=0) readonly buffer Frame { GpuCameraData camera; } frameData;
layout(std430,set=0,binding=1) readonly buffer Patches { PlanetaryPatch patches[]; } patchData;
layout(std430,set=0,binding=2) readonly buffer Input { vec4 cameraHighRadiusHigh; vec4 cameraLowRadiusLow; vec4 thresholds; uvec4 controls; vec4 viewForwardHalfAngle; vec4 textureDemand; } inputData;
layout(std430,set=0,binding=6) readonly buffer Presentations { Presentation values[]; } presentations;
layout(std430,set=0,binding=9) readonly buffer TerrainSamples { vec2 heights[]; } terrainData;
layout(std430,set=0,binding=10) readonly buffer PatchTerrainSlots { uvec2 values[]; } patchTerrain;
layout(std430,set=0,binding=11) readonly buffer Environments { Environment value; } environmentData;
layout(std430,set=0,binding=12) readonly buffer EyeballInput { vec4 cameraHighRadiusHigh; vec4 cameraLowRadiusLow; vec4 surface; uvec4 identity; vec4 tangentAnchorAngle; vec4 mapping; uvec4 topology; uvec4 reserved; } eye;
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
const uint GRID_VERTICES=289u;
dvec3 CubeD(uint face,double a,double b){if(face==0u)return dvec3(1,b,-a);if(face==1u)return dvec3(-1,b,a);if(face==2u)return dvec3(a,1,-b);if(face==3u)return dvec3(a,-1,b);if(face==4u)return dvec3(a,b,1);return dvec3(-a,b,-1);}
dvec3 ProjectD(uvec4 address,dvec2 local){double cells=double(1u<<address.y);dvec2 uv=(dvec2(address.zw)+local)/cells;return normalize(CubeD(address.x,2.0*uv.x-1.0,2.0*uv.y-1.0));}
dvec3 ProjectGridD(uvec4 address,uvec2 grid){uint denominator=16u<<address.y;uvec2 numerator=address.zw*16u+grid;dvec2 uv=dvec2(numerator)/double(denominator);return normalize(CubeD(address.x,2.0*uv.x-1.0,2.0*uv.y-1.0));}
double RadiusD(){return double(inputData.cameraHighRadiusHigh.w)+double(inputData.cameraLowRadiusLow.w);}
dvec3 CameraD(){return dvec3(inputData.cameraHighRadiusHigh.xyz)+dvec3(inputData.cameraLowRadiusLow.xyz);}
double ProjectedSpan(uvec4 address){dvec3 center=ProjectD(address,dvec2(.5));double halfSpan=0.0;halfSpan=max(halfSpan,length(center-ProjectD(address,dvec2(0,0))));halfSpan=max(halfSpan,length(center-ProjectD(address,dvec2(1,0))));halfSpan=max(halfSpan,length(center-ProjectD(address,dvec2(0,1))));halfSpan=max(halfSpan,length(center-ProjectD(address,dvec2(1,1))));double radius=inputData.controls.z!=0u?length(CameraD())-double(inputData.thresholds.z):RadiusD();double distanceToPatch=max(length(CameraD()-center*radius),radius*1e-12);return 2.0*halfSpan*radius/distanceToPatch;}
double HeightAt(uint slot,uint x,uint y,float morph){vec2 values=terrainData.heights[slot*GRID_VERTICES+y*17u+x];return double(mix(values.x,values.y,morph));}
vec3 RotateQuaternion(vec3 point,vec4 quaternion){return point+2.0*cross(quaternion.xyz,cross(quaternion.xyz,point)+quaternion.w*point);}
void main(){
  PlanetaryPatch p=patchData.patches[gl_InstanceIndex];Presentation presentation=presentations.values[0];vec2 stitched=inUv;uint mask=p.transitions.x;const float grid=16.0;
  if(((mask&1u)!=0u&&inUv.x==0.0)||((mask&2u)!=0u&&inUv.x==1.0))stitched.y=floor(inUv.y*grid*.5)*2.0/grid;
  if(((mask&4u)!=0u&&inUv.y==0.0)||((mask&8u)!=0u&&inUv.y==1.0))stitched.x=floor(inUv.x*grid*.5)*2.0/grid;
  uvec2 gridCoordinate=uvec2(round(stitched*16.0));dvec3 direction=ProjectGridD(p.address,gridCoordinate);dvec3 relativePosition;dvec3 surfaceNormal=direction;terrainHeight=-1.0;bool terrain=inputData.controls.z!=0u&&inputData.thresholds.w>0.0;
  if(terrain){uint x=gridCoordinate.x,y=gridCoordinate.y;uint slot=patchTerrain.values[gl_InstanceIndex].x;float morph=1.0;double radius=RadiusD();double rawHeight=HeightAt(slot,x,y,morph);terrainHeight=float(rawHeight);bool ocean=(environmentData.value.identity.z&4u)!=0u&&environmentData.value.identity.xy==presentation.identity.xy;double height=ocean?max(rawHeight,double(environmentData.value.ocean.x)):rawHeight;dvec3 absolutePosition=direction*(radius+height);relativePosition=absolutePosition-CameraD();uint xl=x==0u?0u:x-1u,xr=x==16u?16u:x+1u,yd=y==0u?0u:y-1u,yu=y==16u?16u:y+1u;dvec3 left=ProjectGridD(p.address,uvec2(xl,y))*(radius+HeightAt(slot,xl,y,morph));dvec3 right=ProjectGridD(p.address,uvec2(xr,y))*(radius+HeightAt(slot,xr,y,morph));dvec3 down=ProjectGridD(p.address,uvec2(x,yd))*(radius+HeightAt(slot,x,yd,morph));dvec3 up=ProjectGridD(p.address,uvec2(x,yu))*(radius+HeightAt(slot,x,yu,morph));surfaceNormal=ocean&&rawHeight<double(environmentData.value.ocean.x)?direction:normalize(cross(right-left,up-down));if(dot(surfaceNormal,direction)<0.0)surfaceNormal=-surfaceNormal;}
  else{double cells=double(1u<<p.address.y);dvec2 uv=(dvec2(p.address.zw)+dvec2(stitched))/cells;direction=normalize(CubeD(p.address.x,2.0*uv.x-1.0,2.0*uv.y-1.0));relativePosition=dvec3(p.centerRadius.xyz)+direction*double(p.centerRadius.w);}
	  vec3 localPosition=vec3(relativePosition);vec3 position=RotateQuaternion(localPosition,presentation.bodyOrientation);gl_Position=frameData.camera.viewProjection*vec4(position,1);color=p.color;if(eye.identity.w!=0u)color.a=1.0;normal=vec3(surfaceNormal);bodyDirection=vec3(direction);lightDirection=normalize(RotateQuaternion(lighting.sourceCenterExposure.xyz-presentation.centerRadius.xyz,vec4(-presentation.bodyOrientation.xyz,presentation.bodyOrientation.w)));material=uvec2(presentation.identity.w,presentation.identity.z);response=presentation.surface;viewDirection=-localPosition;localDetail=presentation.localDetail;bodyCameraHigh=inputData.cameraHighRadiusHigh.xyz;bodyCameraLow=inputData.cameraLowRadiusLow.xyz;
	}
