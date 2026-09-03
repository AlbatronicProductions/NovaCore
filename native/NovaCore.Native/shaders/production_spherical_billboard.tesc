#version 460
struct EncodedPosition{vec4 high;vec4 low;};struct GpuCameraData{EncodedPosition position;mat4 viewProjection;};layout(set=0,binding=0,std430)readonly buffer Frame{GpuCameraData camera;}frameData;layout(set=0,binding=2,std430)readonly buffer Input{vec4 a;vec4 b;vec4 c;uvec4 controls;vec4 d;vec4 textureDemand;}inputData;
layout(vertices=3) out;
layout(set=0,binding=40,std430) buffer TessellationFactors { uint values[]; } tessellationFactors;
layout(location=0) in vec4 i0[];layout(location=0) out vec4 o0[];layout(location=1) in vec3 i1[];layout(location=1) out vec3 o1[];layout(location=2) flat in vec3 i2[];layout(location=2) flat out vec3 o2[];layout(location=3) flat in uvec2 i3[];layout(location=3) flat out uvec2 o3[];layout(location=4) flat in vec4 i4[];layout(location=4) flat out vec4 o4[];layout(location=5) in vec3 i5[];layout(location=5) out vec3 o5[];layout(location=6) in vec3 i6[];layout(location=6) out vec3 o6[];layout(location=7) in float i7[];layout(location=7) out float o7[];layout(location=8) flat in vec3 i8[];layout(location=8) flat out vec3 o8[];layout(location=9) flat in vec3 i9[];layout(location=9) flat out vec3 o9[];layout(location=10) flat in vec4 i10[];layout(location=10) flat out vec4 o10[];layout(location=11) flat in uint i11[];layout(location=11) flat out uint o11[];layout(location=12) in vec2 i12[];layout(location=12) out vec2 o12[];layout(location=13) flat in uvec4 i13[];layout(location=13) flat out uvec4 o13[];layout(location=14) in vec2 i14[];layout(location=14) out vec2 o14[];layout(location=15) in vec2 i15[];layout(location=15) out vec2 o15[];layout(location=17) in vec3 ip[];layout(location=18) in float i18[];layout(location=18) out float o18[];
bool forceTesOne(){return inputData.textureDemand.z < -1000.5 && inputData.textureDemand.z > -1001.5;}
bool bypassScreenReject(){return inputData.textureDemand.z < -1001.5 && inputData.textureDemand.z > -1002.5;}
float edgeFactor(uint a,uint b){if(forceTesOne())return 1;vec4 ca=gl_in[a].gl_Position,cb=gl_in[b].gl_Position;if(ca.w<=1e-6||cb.w<=1e-6)return 1;float h=max(inputData.textureDemand.x,1.0),w=h*abs(frameData.camera.viewProjection[1][1]/frameData.camera.viewProjection[0][0]);float pixels=length((ca.xy/ca.w-cb.xy/cb.w)*vec2(w,h)*.5);float distance=length((ip[a]+ip[b])*.5);float fade=1.0-clamp(distance/50.0,0.0,1.0);float required=max(1.0,pixels*fade/5.0);return min(64.0,exp2(ceil(log2(required))));}
bool outsideConservativeViewport(){
  if(bypassScreenReject())return false;
  vec4 a=gl_in[0].gl_Position,b=gl_in[1].gl_Position,c=gl_in[2].gl_Position;
  if(a.w<=1e-6||b.w<=1e-6||c.w<=1e-6)return false;
  float h=max(inputData.textureDemand.x,1.0);
  float w=h*abs(frameData.camera.viewProjection[1][1]/frameData.camera.viewProjection[0][0]);
  // The 64-pixel guard exceeds the accepted 21.011-pixel worst residual edge
  // and retains boundary work while rejecting patches wholly outside the
  // rectangular framebuffer before any TES invocation is generated.
  float guardX=1.0+128.0/max(w,1.0),guardY=1.0+128.0/h;
  vec2 na=a.xy/a.w,nb=b.xy/b.w,nc=c.xy/c.w;
  return (na.x < -guardX&&nb.x < -guardX&&nc.x < -guardX)||
         (na.x >  guardX&&nb.x >  guardX&&nc.x >  guardX)||
         (na.y < -guardY&&nb.y < -guardY&&nc.y < -guardY)||
         (na.y >  guardY&&nb.y >  guardY&&nc.y >  guardY);
}
void main(){uint i=gl_InvocationID;gl_out[gl_InvocationID].gl_Position=gl_in[i].gl_Position;o0[gl_InvocationID]=i0[i];o1[gl_InvocationID]=i1[i];o2[gl_InvocationID]=i2[i];o3[gl_InvocationID]=i3[i];o4[gl_InvocationID]=i4[i];o5[gl_InvocationID]=i5[i];o6[gl_InvocationID]=i6[i];o7[gl_InvocationID]=i7[i];o8[gl_InvocationID]=i8[i];o9[gl_InvocationID]=i9[i];o10[gl_InvocationID]=i10[i];o11[gl_InvocationID]=i11[i];o12[gl_InvocationID]=i12[i];o13[gl_InvocationID]=i13[i];o14[gl_InvocationID]=i14[i];o15[gl_InvocationID]=i15[i];o18[gl_InvocationID]=i18[i];barrier();if(i==0u){if(outsideConservativeViewport()){gl_TessLevelOuter[0]=0;gl_TessLevelOuter[1]=0;gl_TessLevelOuter[2]=0;gl_TessLevelInner[0]=0;if(inputData.textureDemand.z<0.0)tessellationFactors.values[gl_PrimitiveID]=0u;return;}float a=edgeFactor(1,2),b=edgeFactor(2,0),c=edgeFactor(0,1),inner=max(a,max(b,c));gl_TessLevelOuter[0]=a;gl_TessLevelOuter[1]=b;gl_TessLevelOuter[2]=c;gl_TessLevelInner[0]=inner;if(inputData.textureDemand.z<0.0)tessellationFactors.values[gl_PrimitiveID]=uint(inner);}}
