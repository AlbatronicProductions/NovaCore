#version 460

layout(push_constant) uniform StellarLighting {
  vec4 sourceCenterExposure;
  vec4 sourceColorAmbient;
  vec4 radianceGlowEnabled;
} lighting;

layout(location=0) out vec2 glyphUv;
layout(location=1) flat out int characterCode;
layout(location=2) flat out uint elementKind;
layout(location=3) flat out float opacity;

const int TitleCapacity=16;
const int ValueCapacity=18;
const vec2 Corners[6]=vec2[](vec2(0,0),vec2(1,0),vec2(1,1),vec2(0,0),vec2(1,1),vec2(0,1));

int TitleCharacter(int index){int text[16]=int[](83,105,109,117,108,97,116,105,111,110,32,83,112,101,101,100);return text[index];}
int ValueLength(int preset){if(preset==0)return 18;if(preset==1)return 13;if(preset<=3)return 2;if(preset<=5)return 3;if(preset<=7)return 4;if(preset<=9)return 6;if(preset<=11)return 7;if(preset==12)return 8;return 10;}
int ValueCharacter(int preset,int index){
  if(preset==0){int a[18]=int[](48,46,49,120,32,40,83,108,111,119,32,77,111,116,105,111,110,41);return a[index];}
  if(preset==1){int a[13]=int[](49,120,32,40,82,101,97,108,116,105,109,101,41);return a[index];}
  if(preset==2){int a[2]=int[](50,120);return a[index];}if(preset==3){int a[2]=int[](52,120);return a[index];}
  if(preset==4){int a[3]=int[](49,48,120);return a[index];}if(preset==5){int a[3]=int[](51,48,120);return a[index];}
  if(preset==6){int a[4]=int[](49,50,48,120);return a[index];}if(preset==7){int a[4]=int[](54,48,48,120);return a[index];}
  if(preset==8){int a[6]=int[](49,44,50,48,48,120);return a[index];}if(preset==9){int a[6]=int[](51,44,54,48,48,120);return a[index];}
  if(preset==10){int a[7]=int[](49,52,44,52,48,48,120);return a[index];}if(preset==11){int a[7]=int[](56,54,44,52,48,48,120);return a[index];}
  if(preset==12){int a[8]=int[](54,48,52,44,56,48,48,120);return a[index];}if(preset==13){int a[10]=int[](50,44,53,57,50,44,48,48,48,120);return a[index];}
  int a[10]=int[](55,44,55,55,54,44,48,48,48,120);return a[index];
}

void main(){
  uint packed=floatBitsToUint(lighting.radianceGlowEnabled.w);int preset=int(packed&255u)-1;opacity=float((packed>>8)&255u)/255.0;
  int quad=int(gl_VertexIndex/6);int corner=int(gl_VertexIndex%6);glyphUv=Corners[corner];characterCode=0;elementKind=0u;
  if(preset<0||preset>=15||opacity<=0.0){gl_Position=vec4(2,2,2,1);return;}
  if(quad==0){vec2 minimum=vec2(.655,-.945),size=vec2(.315,.145);gl_Position=vec4(minimum+glyphUv*size,0,1);return;}
  if(quad<=TitleCapacity){int index=quad-1;characterCode=TitleCharacter(index);elementKind=1u;float stride=.0116,width=.0102,height=.025;float start=.8125-float(TitleCapacity)*stride*.5;vec2 minimum=vec2(start+float(index)*stride,-.915);gl_Position=vec4(minimum+glyphUv*vec2(width,height),0,1);return;}
  int index=quad-1-TitleCapacity;int length=ValueLength(preset);if(index>=length){gl_Position=vec4(2,2,2,1);return;}characterCode=ValueCharacter(preset,index);elementKind=2u;float stride=.0152,width=.0135,height=.039;float start=.8125-float(length)*stride*.5;vec2 minimum=vec2(start+float(index)*stride,-.865);gl_Position=vec4(minimum+glyphUv*vec2(width,height),0,1);
}
