#version 460
struct EncodedPosition{vec4 high;vec4 low;};struct GpuCameraData{EncodedPosition position;mat4 viewProjection;};layout(set=0,binding=0,std430)readonly buffer Frame{GpuCameraData camera;}frameData;layout(set=0,binding=2,std430)readonly buffer Input{vec4 a;vec4 b;vec4 c;uvec4 controls;vec4 d;vec4 textureDemand;}inputData;
layout(vertices=3) out;
// The user output payload is 13 scalars per control point. Body-wide constants
// and TES-recomputed addresses must not grow this per-patch transport again.
layout(set=0,binding=40,std430) buffer TessellationFactors { uint values[]; } tessellationFactors;
layout(set=0,binding=43,std430) buffer Counters { uint values[]; } counters;
layout(location=1) in vec3 i1[];layout(location=1) out vec3 o1[];layout(location=2) flat in vec3 i2[];layout(location=2) flat out vec3 o2[];layout(location=5) in vec3 i5[];layout(location=5) out vec3 o5[];layout(location=6) in vec3 i6[];layout(location=6) out vec3 o6[];layout(location=7) in float i7[];layout(location=7) out float o7[];layout(location=17) in vec3 ip[];
bool forceTesOne(){return inputData.textureDemand.z < -1000.5 && inputData.textureDemand.z > -1001.5;}
vec2 screenSize(){
  float h=inputData.textureDemand.x;
  // Row lengths recover the projection scales from the combined matrix without
  // depending on camera orientation.  Their ratio is the viewport aspect.
  float px=length(vec3(frameData.camera.viewProjection[0][0],frameData.camera.viewProjection[1][0],frameData.camera.viewProjection[2][0]));
  float py=length(vec3(frameData.camera.viewProjection[0][1],frameData.camera.viewProjection[1][1],frameData.camera.viewProjection[2][1]));
  return vec2(h*py/px,h);
}
float screenDistance(vec4 a,vec4 b){return .5*length((a.xy/a.w-b.xy/b.w)*screenSize());}
float edgeFactor(uint a,uint b){
  if(forceTesOne())return 1;
  vec3 p0=ip[a],p1=ip[b],mid=(p0+p1)*.5,edge=p1-p0;
  float midDistance=length(mid),edgeLength=length(edge);
  float pixels=screenDistance(gl_in[a].gl_Position,gl_in[b].gl_Position);
  float alignment=abs(dot(mid/midDistance,edge/edgeLength));
  float skew=(alignment-.8)/.2;
  if(skew>0){
    // KSA constructs a camera-space comparison edge with equal X/Y components.
    // For a standard perspective matrix this is the exact equivalent expressed
    // from NovaCore's combined VP plus its authoritative vertical FOV.
    float midW=abs((gl_in[a].gl_Position.w+gl_in[b].gl_Position.w)*.5);
    float verticalTan=inputData.textureDemand.y;
    float compensation=.5*1.41421356237*screenSize().y*(.6*edgeLength)/(midW*verticalTan);
    pixels=mix(pixels,compensation,skew);
  }
  float fade=1-clamp(midDistance/50,0,1);
  return clamp(pixels/3*fade,1,64);
}
void main(){
  uint i=gl_InvocationID;
  gl_out[gl_InvocationID].gl_Position=gl_in[gl_InvocationID].gl_Position;
  o1[gl_InvocationID]=i1[gl_InvocationID];
  o2[gl_InvocationID]=i2[gl_InvocationID];
  o5[gl_InvocationID]=i5[gl_InvocationID];
  o6[gl_InvocationID]=i6[gl_InvocationID];o7[gl_InvocationID]=i7[gl_InvocationID];
  barrier();
  if(i==0u){
    float a=edgeFactor(1,2),b=edgeFactor(2,0),c=edgeFactor(0,1),inner=(a+b+c)*.3333;
    gl_TessLevelOuter[0]=a;gl_TessLevelOuter[1]=b;gl_TessLevelOuter[2]=c;gl_TessLevelInner[0]=inner;
    atomicMax(counters.values[23],floatBitsToUint(max(a,max(b,c))));
    uint previousInner=atomicMax(counters.values[24],floatBitsToUint(inner));
    if(inputData.textureDemand.z<0){
      tessellationFactors.values[gl_PrimitiveID]=floatBitsToUint(inner);
      if(floatBitsToUint(inner)>previousInner)atomicExchange(counters.values[40],gl_PrimitiveID);
      uint bucket=inner<=1?25u:inner<=2?26u:inner<=4?27u:inner<=8?28u:inner<=16?29u:inner<=32?30u:31u;
      atomicAdd(counters.values[bucket],1u);
      atomicMax(counters.values[32],floatBitsToUint(max(screenDistance(gl_in[1].gl_Position,gl_in[2].gl_Position),max(screenDistance(gl_in[2].gl_Position,gl_in[0].gl_Position),screenDistance(gl_in[0].gl_Position,gl_in[1].gl_Position)))));
      float minimumW=min(gl_in[0].gl_Position.w,min(gl_in[1].gl_Position.w,gl_in[2].gl_Position.w));
      if(minimumW<=0)atomicAdd(counters.values[33],1u);else if(minimumW<1)atomicAdd(counters.values[34],1u);
    }
  }
}
