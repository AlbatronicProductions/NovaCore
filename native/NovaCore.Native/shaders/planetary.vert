#version 460
struct EncodedPosition { vec4 high; vec4 low; };
struct GpuCameraData { EncodedPosition position; mat4 viewProjection; };
struct PlanetaryPatch { uvec4 address; vec4 centerRadius; vec4 color; };
layout(std430,set=0,binding=0) readonly buffer Frame { GpuCameraData camera; } frameData;
layout(std430,set=0,binding=1) readonly buffer Patches { PlanetaryPatch patches[]; } patchData;
layout(location=0) in vec2 inUv;
layout(location=0) out vec3 color;
vec3 Cube(uint face,float a,float b) {
  if(face==0) return vec3(1,b,-a); if(face==1) return vec3(-1,b,a);
  if(face==2) return vec3(a,1,-b); if(face==3) return vec3(a,-1,b);
  if(face==4) return vec3(a,b,1); return vec3(-a,b,-1);
}
void main(){PlanetaryPatch p=patchData.patches[gl_InstanceIndex];float cells=exp2(float(p.address.y));vec2 uv=(vec2(p.address.zw)+inUv)/cells;float a=2*uv.x-1;float b=2*uv.y-1;vec3 local=normalize(Cube(p.address.x,a,b))*p.centerRadius.w;gl_Position=frameData.camera.viewProjection*vec4(p.centerRadius.xyz+local,1);color=p.color.rgb;}
