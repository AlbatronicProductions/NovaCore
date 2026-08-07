#version 460
struct EncodedPosition { vec4 high; vec4 low; };
struct GpuCameraData { EncodedPosition position; mat4 viewProjection; };
layout(std430,set=0,binding=0) readonly buffer Frame { GpuCameraData camera; } frameData;
layout(push_constant) uniform StellarLighting { vec4 sourceCenterExposure; vec4 sourceColorAmbient; vec4 radianceGlowEnabled; } lighting;
layout(location=0) in vec2 uv;
layout(location=0) out vec4 outColor;

float Hash12(vec2 p){vec3 p3=fract(vec3(p.xyx)*vec3(.1031,.1030,.0973));p3+=dot(p3,p3.yzx+33.33);return fract((p3.x+p3.y)*p3.z);}
float Hash13(vec3 p){p=fract(p*.1031);p+=dot(p,p.yzx+31.32);return fract((p.x+p.y)*p.z);}
vec2 Octahedral(vec3 n){n/=abs(n.x)+abs(n.y)+abs(n.z);vec2 p=n.xy;if(n.z<0.0)p=(1.0-abs(p.yx))*sign(p);return p*.5+.5;}
float Noise(vec3 p){vec3 i=floor(p),f=fract(p);f=f*f*(3.0-2.0*f);float n000=Hash13(i),n100=Hash13(i+vec3(1,0,0)),n010=Hash13(i+vec3(0,1,0)),n110=Hash13(i+vec3(1,1,0));float n001=Hash13(i+vec3(0,0,1)),n101=Hash13(i+vec3(1,0,1)),n011=Hash13(i+vec3(0,1,1)),n111=Hash13(i+vec3(1));return mix(mix(mix(n000,n100,f.x),mix(n010,n110,f.x),f.y),mix(mix(n001,n101,f.x),mix(n011,n111,f.x),f.y),f.z);}
vec3 StarLayer(vec2 sky,float scale,float threshold,float size){vec2 cell=floor(sky*scale),local=fract(sky*scale)-.5;float seed=Hash12(cell);float present=smoothstep(threshold,1.0,seed);vec2 offset=vec2(Hash12(cell+17.1),Hash12(cell+43.7))-.5;float radial=length(local-offset*.72);float core=pow(max(0.0,1.0-radial/size),7.0)*present;float brightness=.35+2.4*pow(seed,10.0);float temperature=Hash12(cell+91.3);vec3 tint=mix(vec3(.62,.76,1.0),vec3(1.0,.72,.48),smoothstep(.25,.85,temperature));return tint*core*brightness;}
void main(){
  if(floatBitsToUint(lighting.radianceGlowEnabled.z)==0u){outColor=vec4(.02,.02,.04,1);return;}
  vec2 ndc=uv*2.0-1.0;
  vec4 reconstructed=inverse(frameData.camera.viewProjection)*vec4(ndc,.5,1.0);
  vec3 direction=normalize(reconstructed.xyz/max(abs(reconstructed.w),1e-7));
  vec3 galacticPole=normalize(vec3(.31,.83,.46));
  float latitude=abs(dot(direction,galacticPole));
  float band=exp(-latitude*latitude/0.018);
  float dust=Noise(direction*7.0)+.55*Noise(direction*19.0)+.25*Noise(direction*53.0);
  float darkLane=smoothstep(.35,.8,Noise(direction*13.0+vec3(9.2,1.7,4.1)));
  vec3 milky=vec3(.045,.052,.075)*band*(.32+1.25*dust)*(1.0-.46*darkLane);
  vec2 sky=Octahedral(direction);
  vec3 stars=StarLayer(sky,180.0,.955,.13)+StarLayer(sky+vec2(.173,.417),360.0,.978,.17)+StarLayer(sky+vec2(.631,.083),720.0,.990,.22);
  float haze=.0012+.0015*Noise(direction*4.0);
  outColor=vec4(vec3(haze)+milky+stars,1);
}
