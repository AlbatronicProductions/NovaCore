#version 460

struct EncodedPosition { vec4 high; vec4 low; };
struct GpuCameraData { EncodedPosition position; mat4 viewProjection; };
layout(std430,set=0,binding=0) readonly buffer Frame { GpuCameraData camera; } frameData;
layout(std430,set=0,binding=2) readonly buffer Input { vec4 cameraHighRadiusHigh; vec4 cameraLowRadiusLow; vec4 thresholds; uvec4 controls; vec4 viewForwardHalfAngle; vec4 textureDemand; } inputData;

layout(vertices=3) out;
layout(location=0) in vec4 inColor[]; layout(location=0) out vec4 outColor[];
layout(location=1) in vec3 inNormal[]; layout(location=1) out vec3 outNormal[];
layout(location=2) flat in vec3 inLightDirection[]; layout(location=2) flat out vec3 outLightDirection[];
layout(location=3) flat in uvec2 inMaterial[]; layout(location=3) flat out uvec2 outMaterial[];
layout(location=4) flat in vec4 inResponse[]; layout(location=4) flat out vec4 outResponse[];
layout(location=5) in vec3 inViewDirection[]; layout(location=5) out vec3 outViewDirection[];
layout(location=6) in vec3 inBodyDirection[]; layout(location=6) out vec3 outBodyDirection[];
layout(location=7) in float inTerrainHeight[]; layout(location=7) out float outTerrainHeight[];
layout(location=8) flat in vec3 inBodyCameraHigh[]; layout(location=8) flat out vec3 outBodyCameraHigh[];
layout(location=9) flat in vec3 inBodyCameraLow[]; layout(location=9) flat out vec3 outBodyCameraLow[];
layout(location=10) flat in vec4 inLocalDetail[]; layout(location=10) flat out vec4 outLocalDetail[];
layout(location=11) flat in uint inProductionLayer[]; layout(location=11) flat out uint outProductionLayer[];
layout(location=12) in vec2 inProductionUv[]; layout(location=12) out vec2 outProductionUv[];
layout(location=13) flat in uvec4 inProductionAddress[]; layout(location=13) flat out uvec4 outProductionAddress[];
layout(location=14) in vec2 inProductionTransition[]; layout(location=14) out vec2 outProductionTransition[];
layout(location=15) in vec2 inTopologyCoordinate[]; layout(location=15) out vec2 outTopologyCoordinate[];
layout(location=17) in vec3 inConservativeTrianglePosition[];

float ProjectedLengthPixels(vec4 a,vec4 b)
{
  if(a.w<=1e-6||b.w<=1e-6)return 1e30;
  vec2 delta=a.xy/a.w-b.xy/b.w;
  float height=max(inputData.textureDemand.x,1.0);
  float width=height*abs(frameData.camera.viewProjection[1][1]/frameData.camera.viewProjection[0][0]);
  return length(delta*vec2(width,height)*0.5);
}

float PerspectiveAwareLengthPixels(uint first,uint second)
{
  float endpointLength=ProjectedLengthPixels(gl_in[first].gl_Position,gl_in[second].gl_Position);
  vec3 firstPosition=inConservativeTrianglePosition[first];
  vec3 secondPosition=inConservativeTrianglePosition[second];
  vec3 midpoint=(firstPosition+secondPosition)*0.5;
  vec3 edge=secondPosition-firstPosition;
  float worldLength=length(edge),midpointDistance=length(midpoint);
  if(worldLength<=1e-6||midpointDistance<=1e-6)return endpointLength;
  float alignment=abs(dot(midpoint/midpointDistance,edge/worldLength));
  float skew=clamp((alignment-0.8)/0.2,0.0,1.0);
  float minimumDepth=min(abs(gl_in[first].gl_Position.w),abs(gl_in[second].gl_Position.w));
  float focalPixels=max(inputData.textureDemand.x,1.0)*abs(frameData.camera.viewProjection[1][1])*0.5;
  float compensated=minimumDepth<=1e-6?endpointLength:0.6*worldLength*focalPixels/minimumDepth;
  return mix(endpointLength,compensated,skew);
}

float QuantizedRefinement(float pixels,float midpointDistance)
{
  // Direct KSA installation evidence establishes a finite 50 metre terrain
  // tessellation range.  Retain NovaCore's independently selected 16 px / 16x
  // quality bound, but refine only the raster-proximate footprint.
  const float targetPixels=16.0,maximumFactor=16.0,rangeMetres=50.0;
  if(isnan(pixels)||isinf(pixels))return maximumFactor;
  float distanceScale=1.0-clamp(midpointDistance/rangeMetres,0.0,1.0);
  float required=max(1.0,pixels/targetPixels*distanceScale);
  return min(maximumFactor,exp2(ceil(log2(required))));
}

float EdgeRefinement(uint first,uint second)
{
  float midpointDistance=length((inConservativeTrianglePosition[first]+
    inConservativeTrianglePosition[second])*0.5);
  return QuantizedRefinement(PerspectiveAwareLengthPixels(first,second),midpointDistance);
}

vec4 ClipPlane(uint index)
{
  mat4 m=frameData.camera.viewProjection;
  vec4 row0=vec4(m[0][0],m[1][0],m[2][0],m[3][0]);
  vec4 row1=vec4(m[0][1],m[1][1],m[2][1],m[3][1]);
  vec4 row2=vec4(m[0][2],m[1][2],m[2][2],m[3][2]);
  vec4 row3=vec4(m[0][3],m[1][3],m[2][3],m[3][3]);
  if(index==0u)return row3+row0;if(index==1u)return row3-row0;
  if(index==2u)return row3+row1;if(index==3u)return row3-row1;
  if(index==4u)return row2;return row3-row2;
}

bool ConservativelyOutside()
{
  // The VS vertices are already evaluated on the canonical physical surface
  // and TES interpolates their heights.  Adding Earth's planet-wide height
  // envelope here made every small triangle a roughly 10 km sphere and
  // defeated final-visibility rejection for a retained viewer neighborhood.
  // Bound only the curved spherical interpolation between these displaced
  // endpoints.  The chord sagitta is conservative for the tiny production
  // patch triangles and refined children remain inside this envelope.
  vec3 center=(inConservativeTrianglePosition[0]+inConservativeTrianglePosition[1]+
    inConservativeTrianglePosition[2])/3.0;
  float edge01=length(inConservativeTrianglePosition[1]-inConservativeTrianglePosition[0]);
  float edge12=length(inConservativeTrianglePosition[2]-inConservativeTrianglePosition[1]);
  float edge20=length(inConservativeTrianglePosition[0]-inConservativeTrianglePosition[2]);
  float maximumEdge=max(edge01,max(edge12,edge20));
  float bodyRadius=max(inputData.cameraHighRadiusHigh.w+inputData.cameraLowRadiusLow.w,1.0);
  float curvatureGuard=maximumEdge*maximumEdge/(8.0*bodyRadius)+0.01;
  float radius=max(length(inConservativeTrianglePosition[0]-center),
    max(length(inConservativeTrianglePosition[1]-center),length(inConservativeTrianglePosition[2]-center)))+
    curvatureGuard;
  // Cull only against the four lateral frustum planes.  With reversed-Z and
  // an infinite far projection, extracting generic row2 / row3-row2 planes
  // here is not a conservative depth-volume test for camera-straddling
  // planetary triangles.  Near clipping remains raster authority and there
  // is deliberately no finite far plane.
  for(uint planeIndex=0u;planeIndex<4u;planeIndex++)
  {
    vec4 plane=ClipPlane(planeIndex);
    if(dot(plane,vec4(center,1.0))+radius*length(plane.xyz)<0.0)return true;
  }
  return false;
}

void main()
{
  uint i=gl_InvocationID;gl_out[gl_InvocationID].gl_Position=gl_in[gl_InvocationID].gl_Position;
  outColor[gl_InvocationID]=inColor[i];outNormal[gl_InvocationID]=inNormal[i];outLightDirection[gl_InvocationID]=inLightDirection[i];
  outMaterial[gl_InvocationID]=inMaterial[i];outResponse[gl_InvocationID]=inResponse[i];outViewDirection[gl_InvocationID]=inViewDirection[i];
  outBodyDirection[gl_InvocationID]=inBodyDirection[i];outTerrainHeight[gl_InvocationID]=inTerrainHeight[i];
  outBodyCameraHigh[gl_InvocationID]=inBodyCameraHigh[i];outBodyCameraLow[gl_InvocationID]=inBodyCameraLow[i];
  outLocalDetail[gl_InvocationID]=inLocalDetail[i];outProductionLayer[gl_InvocationID]=inProductionLayer[i];
  outProductionUv[gl_InvocationID]=inProductionUv[i];outProductionAddress[gl_InvocationID]=inProductionAddress[i];
  outProductionTransition[gl_InvocationID]=inProductionTransition[i];outTopologyCoordinate[gl_InvocationID]=inTopologyCoordinate[i];
  barrier();
  if(i==0u)
  {
    if(ConservativelyOutside())
      gl_TessLevelOuter[0]=gl_TessLevelOuter[1]=gl_TessLevelOuter[2]=gl_TessLevelInner[0]=0.0;
    else
    {
      float edge12=EdgeRefinement(1u,2u);
      float edge20=EdgeRefinement(2u,0u);
      float edge01=EdgeRefinement(0u,1u);
      gl_TessLevelOuter[0]=edge12;gl_TessLevelOuter[1]=edge20;gl_TessLevelOuter[2]=edge01;
      gl_TessLevelInner[0]=max(edge12,max(edge20,edge01));
    }
  }
}
