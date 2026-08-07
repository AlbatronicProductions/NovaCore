#version 460
struct EncodedPosition { vec4 high; vec4 low; };
struct GpuCameraData { EncodedPosition position; mat4 viewProjection; };
layout(std430,set=0,binding=0) readonly buffer Frame { GpuCameraData camera; } frameData;
struct Presentation { vec4 centerRadius; vec4 colorDistant; float detailedAlpha; float distanceRadii; uint regime; uint enabled; };
layout(std430,set=0,binding=6) readonly buffer Presentations { Presentation values[]; } presentations;
layout(location=0) out vec4 color;
int labelLength(int id){return id==1?3:id==2?7:id==3?5:id==4?5:id==5?4:id==6?4:id==7?7:id==8?6:id==9?6:7;}
int characterAt(int id,int index){
  if(id==1){int a[3]=int[](83,85,78);return a[index];}if(id==2){int a[7]=int[](77,69,82,67,85,82,89);return a[index];}if(id==3){int a[5]=int[](86,69,78,85,83);return a[index];}if(id==4){int a[5]=int[](69,65,82,84,72);return a[index];}if(id==5){int a[4]=int[](77,79,79,78);return a[index];}if(id==6){int a[4]=int[](77,65,82,83);return a[index];}if(id==7){int a[7]=int[](74,85,80,73,84,69,82);return a[index];}if(id==8){int a[6]=int[](83,65,84,85,82,78);return a[index];}if(id==9){int a[6]=int[](85,82,65,78,85,83);return a[index];}int a[7]=int[](78,69,80,84,85,78,69);return a[index];
}
int glyphMask(int c,int r){
 if(c==65){int a[5]=int[](2,5,7,5,5);return a[r];}if(c==67){int a[5]=int[](7,4,4,4,7);return a[r];}if(c==69){int a[5]=int[](7,4,6,4,7);return a[r];}if(c==72){int a[5]=int[](5,5,7,5,5);return a[r];}if(c==73){int a[5]=int[](7,2,2,2,7);return a[r];}if(c==74){int a[5]=int[](1,1,1,5,2);return a[r];}if(c==77){int a[5]=int[](5,7,7,5,5);return a[r];}if(c==78){int a[5]=int[](5,7,7,7,5);return a[r];}if(c==79){int a[5]=int[](2,5,5,5,2);return a[r];}if(c==80){int a[5]=int[](6,5,6,4,4);return a[r];}if(c==82){int a[5]=int[](6,5,6,5,5);return a[r];}if(c==83){int a[5]=int[](3,4,2,1,6);return a[r];}if(c==84){int a[5]=int[](7,2,2,2,2);return a[r];}if(c==85){int a[5]=int[](5,5,5,5,7);return a[r];}if(c==86){int a[5]=int[](5,5,5,5,2);return a[r];}if(c==89){int a[5]=int[](5,5,2,2,2);return a[r];}return 0;
}
void main(){
 Presentation p=presentations.values[gl_InstanceIndex];vec4 clip=frameData.camera.viewProjection*vec4(p.centerRadius.xyz,1.0);int id=int(p.enabled&255u);int character=int(gl_VertexIndex/90);int local=int(gl_VertexIndex%90);int cell=local/6;int tri=local%6;int row=cell/3;int column=cell%3;
 if((p.enabled&0x40000000u)==0u||clip.w<=0.0||character>=labelLength(id)||((glyphMask(characterAt(id,character),row)>>(2-column))&1)==0){gl_Position=vec4(2.0,2.0,2.0,1.0);return;}
 const vec2 corners[6]=vec2[](vec2(0,0),vec2(1,0),vec2(1,1),vec2(0,0),vec2(1,1),vec2(0,1));vec2 anchor=clip.xy/clip.w;bool focused=(p.enabled&0x80000000u)!=0u;float xBase=.009;float yBase=focused?.012:.009;vec2 offset=vec2(xBase+float(character)*.009+float(column)*.0022,yBase+float(row)*.0028)+corners[tri]*vec2(.0022,.0028);gl_Position=vec4((anchor+offset)*clip.w,clip.z,clip.w);color=vec4(focused?vec3(.96,.90,.55):vec3(.72,.75,.80),1.0);
}
