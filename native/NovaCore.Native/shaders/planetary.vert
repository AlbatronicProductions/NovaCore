#version 460
struct EncodedPosition { vec4 high; vec4 low; };
struct GpuCameraData { EncodedPosition position; mat4 viewProjection; };
struct PlanetaryPatch { uvec4 address; vec4 centerRadius; vec4 color; uvec4 transitions; };
struct Presentation { vec4 centerRadius; vec4 colorDistant; vec4 blendMetricState; uvec4 identity; vec4 surface; uvec4 hooks; vec4 ringGeometry; vec4 ringOrientation; vec4 ringColor; };
layout(std430,set=0,binding=0) readonly buffer Frame { GpuCameraData camera; } frameData;
layout(std430,set=0,binding=1) readonly buffer Patches { PlanetaryPatch patches[]; } patchData;
layout(std430,set=0,binding=6) readonly buffer Presentations { Presentation values[]; } presentations;
layout(push_constant) uniform StellarLighting { vec4 sourceCenterExposure; vec4 sourceColorAmbient; vec4 radianceGlowEnabled; } lighting;
layout(location=0) in vec2 inUv;
layout(location=0) out vec4 color;
layout(location=1) out vec3 normal;
layout(location=2) flat out vec3 lightDirection;
layout(location=3) flat out uvec2 material;
layout(location=4) flat out vec4 response;
layout(location=5) out vec3 viewDirection;
vec3 Cube(uint face,float a,float b) {
  if(face==0) return vec3(1,b,-a); if(face==1) return vec3(-1,b,a);
  if(face==2) return vec3(a,1,-b); if(face==3) return vec3(a,-1,b);
  if(face==4) return vec3(a,b,1); return vec3(-a,b,-1);
}
void main(){
  PlanetaryPatch p=patchData.patches[gl_InstanceIndex];
  Presentation presentation=presentations.values[0];
  vec2 stitched=inUv;uint mask=p.transitions.x;const float grid=16.0;
  if(((mask&1u)!=0u&&inUv.x==0.0)||((mask&2u)!=0u&&inUv.x==1.0))stitched.y=floor(inUv.y*grid*.5)*2.0/grid;
  if(((mask&4u)!=0u&&inUv.y==0.0)||((mask&8u)!=0u&&inUv.y==1.0))stitched.x=floor(inUv.x*grid*.5)*2.0/grid;
  float cells=exp2(float(p.address.y));vec2 uv=(vec2(p.address.zw)+stitched)/cells;float a=2*uv.x-1;float b=2*uv.y-1;normal=normalize(Cube(p.address.x,a,b));vec3 local=normal*p.centerRadius.w;vec3 position=p.centerRadius.xyz+local;gl_Position=frameData.camera.viewProjection*vec4(position,1);color=p.color;lightDirection=normalize(lighting.sourceCenterExposure.xyz-p.centerRadius.xyz);material=uvec2(presentation.identity.w,presentation.identity.z);response=presentation.surface;viewDirection=-position;
}
