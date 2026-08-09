#version 460
struct EncodedPosition { vec4 high; vec4 low; };
struct GpuCameraData { EncodedPosition position; mat4 viewProjection; };
layout(std430,set=0,binding=0) readonly buffer Frame { GpuCameraData camera; } frameData;
struct Presentation { vec4 centerRadius; vec4 colorDistant; vec4 blendMetricState; uvec4 identity; vec4 surface; uvec4 hooks; vec4 ringGeometry; vec4 ringOrientation; vec4 ringColor; vec4 bodyOrientation; };
layout(std430,set=0,binding=6) readonly buffer Presentations { Presentation values[]; } presentations;
layout(push_constant) uniform StellarLighting { vec4 sourceCenterExposure; vec4 sourceColorAmbient; vec4 radianceGlowEnabled; } lighting;
layout(location=0) out vec4 color;

int labelLength(int id){return id==1?3:id==2?7:id==3?5:id==4?5:id==5?4:id==6?4:id==7?7:id==8?6:id==9?6:7;}
int characterAt(int id,int index){
  if(id==1){int a[3]=int[](83,85,78);return a[index];}if(id==2){int a[7]=int[](77,69,82,67,85,82,89);return a[index];}if(id==3){int a[5]=int[](86,69,78,85,83);return a[index];}if(id==4){int a[5]=int[](69,65,82,84,72);return a[index];}if(id==5){int a[4]=int[](77,79,79,78);return a[index];}if(id==6){int a[4]=int[](77,65,82,83);return a[index];}if(id==7){int a[7]=int[](74,85,80,73,84,69,82);return a[index];}if(id==8){int a[6]=int[](83,65,84,85,82,78);return a[index];}if(id==9){int a[6]=int[](85,82,65,78,85,83);return a[index];}int a[7]=int[](78,69,80,84,85,78,69);return a[index];
}

int glyphMask(int c,int r){
 if(c==32)return 0;if(c==40){int a[5]=int[](1,2,2,2,1);return a[r];}if(c==41){int a[5]=int[](4,2,2,2,4);return a[r];}if(c==44){int a[5]=int[](0,0,0,2,4);return a[r];}if(c==46){int a[5]=int[](0,0,0,0,2);return a[r];}if(c==58){int a[5]=int[](0,2,0,2,0);return a[r];}
 if(c==48){int a[5]=int[](7,5,5,5,7);return a[r];}if(c==49){int a[5]=int[](2,6,2,2,7);return a[r];}if(c==50){int a[5]=int[](6,1,7,4,7);return a[r];}if(c==51){int a[5]=int[](6,1,3,1,6);return a[r];}if(c==52){int a[5]=int[](5,5,7,1,1);return a[r];}if(c==53){int a[5]=int[](7,4,6,1,6);return a[r];}if(c==54){int a[5]=int[](3,4,7,5,7);return a[r];}if(c==55){int a[5]=int[](7,1,2,2,2);return a[r];}if(c==56){int a[5]=int[](7,5,7,5,7);return a[r];}if(c==57){int a[5]=int[](7,5,7,1,6);return a[r];}
 if(c==65){int a[5]=int[](2,5,7,5,5);return a[r];}if(c==67){int a[5]=int[](7,4,4,4,7);return a[r];}if(c==69){int a[5]=int[](7,4,6,4,7);return a[r];}if(c==72){int a[5]=int[](5,5,7,5,5);return a[r];}if(c==73){int a[5]=int[](7,2,2,2,7);return a[r];}if(c==74){int a[5]=int[](1,1,1,5,2);return a[r];}if(c==77){int a[5]=int[](5,7,7,5,5);return a[r];}if(c==78){int a[5]=int[](5,7,7,7,5);return a[r];}if(c==79){int a[5]=int[](2,5,5,5,2);return a[r];}if(c==80){int a[5]=int[](6,5,6,4,4);return a[r];}if(c==82){int a[5]=int[](6,5,6,5,5);return a[r];}if(c==83){int a[5]=int[](3,4,2,1,6);return a[r];}if(c==84){int a[5]=int[](7,2,2,2,2);return a[r];}if(c==85){int a[5]=int[](5,5,5,5,7);return a[r];}if(c==86){int a[5]=int[](5,5,5,5,2);return a[r];}if(c==89){int a[5]=int[](5,5,2,2,2);return a[r];}
 if(c==97){int a[5]=int[](0,2,1,3,3);return a[r];}if(c==100){int a[5]=int[](1,1,3,5,3);return a[r];}if(c==101){int a[5]=int[](0,2,7,4,3);return a[r];}if(c==105){int a[5]=int[](2,0,2,2,7);return a[r];}if(c==108){int a[5]=int[](4,4,4,4,7);return a[r];}if(c==109){int a[5]=int[](0,0,7,7,5);return a[r];}if(c==110){int a[5]=int[](0,0,6,5,5);return a[r];}if(c==111){int a[5]=int[](0,0,2,5,2);return a[r];}if(c==112){int a[5]=int[](0,6,5,6,4);return a[r];}if(c==116){int a[5]=int[](2,7,2,2,1);return a[r];}if(c==117){int a[5]=int[](0,0,5,5,7);return a[r];}if(c==119){int a[5]=int[](0,5,5,7,5);return a[r];}if(c==120){int a[5]=int[](0,5,2,2,5);return a[r];}return 0;
}

void main(){
  int character=int(gl_VertexIndex/90);int local=int(gl_VertexIndex%90);int cell=local/6;int tri=local%6;int row=cell/3;int column=cell%3;const vec2 corners[6]=vec2[](vec2(0,0),vec2(1,0),vec2(1,1),vec2(0,0),vec2(1,1),vec2(0,1));
  Presentation p=presentations.values[gl_InstanceIndex];uint enabled=floatBitsToUint(p.blendMetricState.w);vec4 clip=frameData.camera.viewProjection*vec4(p.centerRadius.xyz,1.0);int id=int(enabled&255u);
 if((enabled&0x40000000u)==0u||clip.w<=0.0||character>=labelLength(id)||((glyphMask(characterAt(id,character),row)>>(2-column))&1)==0){gl_Position=vec4(2,2,2,1);return;}
 vec2 anchor=clip.xy/clip.w;bool focused=(enabled&0x80000000u)!=0u;float xBase=.008;float yBase=focused?.0105:.008;vec2 offset=vec2(xBase+float(character)*.008+float(column)*.0019,yBase+float(row)*.0024)+corners[tri]*vec2(.0019,.0024);gl_Position=vec4((anchor+offset)*clip.w,clip.z,clip.w);color=vec4(focused?vec3(.96,.90,.55):vec3(.70,.74,.80),focused?.94:.76);
}
