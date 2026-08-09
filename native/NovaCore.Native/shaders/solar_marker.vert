#version 460
struct EncodedPosition { vec4 high; vec4 low; };
struct GpuCameraData { EncodedPosition position; mat4 viewProjection; };
layout(std430,set=0,binding=0) readonly buffer Frame { GpuCameraData camera; } frameData;
struct Presentation { vec4 centerRadius; vec4 colorDistant; vec4 blendMetricState; uvec4 identity; vec4 surface; uvec4 hooks; vec4 ringGeometry; vec4 ringOrientation; vec4 ringColor; vec4 bodyOrientation; };
layout(std430,set=0,binding=6) readonly buffer Presentations { Presentation values[]; } presentations;
layout(location=0) out vec4 color;
void main(){
  Presentation p=presentations.values[gl_InstanceIndex];vec4 clip=frameData.camera.viewProjection*vec4(p.centerRadius.xyz,1.0);
  uint enabled=floatBitsToUint(p.blendMetricState.w);
  if(clip.w<=0.0||(enabled&0x10000000u)==0u){gl_Position=vec4(2.0,2.0,2.0,1.0);return;}
  const vec2 corners[6]=vec2[](vec2(0,0),vec2(1,0),vec2(1,1),vec2(0,0),vec2(1,1),vec2(0,1));
  int arm=gl_VertexIndex/6;int vertex=gl_VertexIndex%6;bool focused=(enabled&0x80000000u)!=0u;
  float extent=focused?.008:.0055;float gap=focused?.0033:.0024;float thickness=focused?.00085:.00060;
  vec2 origin;vec2 size;
  if(arm==0){origin=vec2(-extent,-thickness*.5);size=vec2(extent-gap,thickness);}
  else if(arm==1){origin=vec2(gap,-thickness*.5);size=vec2(extent-gap,thickness);}
  else if(arm==2){origin=vec2(-thickness*.5,-extent);size=vec2(thickness,extent-gap);}
  else{origin=vec2(-thickness*.5,gap);size=vec2(thickness,extent-gap);}
  vec2 markerLocal=origin+corners[vertex]*size;vec2 ndc=clip.xy/clip.w+markerLocal;
  gl_Position=vec4(ndc*clip.w,clip.z,clip.w);color=vec4(mix(p.colorDistant.rgb,vec3(1.0),focused?.38:.12),focused?.78:.48);
}
