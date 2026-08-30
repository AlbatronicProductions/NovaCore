#version 460
struct EncodedPosition { vec4 high; vec4 low; };
struct GpuCameraData { EncodedPosition position; mat4 viewProjection; };
layout(std430,set=0,binding=0) readonly buffer Frame { GpuCameraData camera; } frameData;
struct Presentation { vec4 centerRadius; vec4 colorDistant; vec4 blendMetricState; uvec4 identity; vec4 surface; uvec4 hooks; vec4 ringGeometry; vec4 ringOrientation; vec4 ringColor; vec4 bodyOrientation; vec4 localDetail; vec4 centerLow; };
layout(std430,set=0,binding=6) readonly buffer Presentations { Presentation values[]; } presentations;
layout(location=0) out vec2 glyphUv;
layout(location=1) flat out int characterCode;
layout(location=2) flat out vec4 labelColor;

const vec2 Corners[6]=vec2[](vec2(0,0),vec2(1,0),vec2(1,1),vec2(0,0),vec2(1,1),vec2(0,1));
int LabelLength(int id){return id==1?3:id==2?7:id==3?5:id==4?5:id==5?4:id==6?4:id==7?7:id==8?6:id==9?6:7;}
int LabelCharacter(int id,int index){
  if(id==1){int a[3]=int[](83,117,110);return a[index];}if(id==2){int a[7]=int[](77,101,114,99,117,114,121);return a[index];}
  if(id==3){int a[5]=int[](86,101,110,117,115);return a[index];}if(id==4){int a[5]=int[](69,97,114,116,104);return a[index];}
  if(id==5){int a[4]=int[](77,111,111,110);return a[index];}if(id==6){int a[4]=int[](77,97,114,115);return a[index];}
  if(id==7){int a[7]=int[](74,117,112,105,116,101,114);return a[index];}if(id==8){int a[6]=int[](83,97,116,117,114,110);return a[index];}
  if(id==9){int a[6]=int[](85,114,97,110,117,115);return a[index];}int a[7]=int[](78,101,112,116,117,110,101);return a[index];
}

void main(){
  int character=int(gl_VertexIndex/6);int corner=int(gl_VertexIndex%6);glyphUv=Corners[corner];characterCode=0;labelColor=vec4(0);
  Presentation p=presentations.values[gl_InstanceIndex];uint enabled=floatBitsToUint(p.blendMetricState.w);vec4 clip=frameData.camera.viewProjection*vec4(p.centerRadius.xyz,1.0)+frameData.camera.viewProjection*vec4(p.centerLow.xyz,0.0);int id=int(enabled&255u);
  if((enabled&0x40000000u)==0u||clip.w<=0.0||character>=LabelLength(id)){gl_Position=vec4(2,2,2,1);return;}
  bool focused=(enabled&0x80000000u)!=0u;vec2 anchor=clip.xy/clip.w;float yBase=focused?.013:.009;
  vec2 offset=vec2(.009+float(character)*.0103,yBase)+Corners[corner]*vec2(.009,.021);
  gl_Position=vec4((anchor+offset)*clip.w,clip.z,clip.w);characterCode=LabelCharacter(id,character);
  labelColor=vec4(focused?vec3(.98,.91,.64):vec3(.88,.91,.96),focused?.98:.88);
}
