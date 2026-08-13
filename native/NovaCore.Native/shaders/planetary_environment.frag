#version 460
#extension GL_GOOGLE_include_directive : require
#include "earth_virtual_texture.glsl"
struct EncodedPosition { vec4 high; vec4 low; };
struct GpuCameraData { EncodedPosition position; mat4 viewProjection; };
struct Presentation { vec4 centerRadius; vec4 colorDistant; vec4 blendMetricState; uvec4 identity; vec4 surface; uvec4 hooks; vec4 ringGeometry; vec4 ringOrientation; vec4 ringColor; vec4 bodyOrientation; vec4 localDetail; };
struct Environment { vec4 centerRadius; uvec4 identity; vec4 atmosphere; vec4 scattering; vec4 clouds; vec4 cloudShape; vec4 ocean; vec4 oceanColorExposure; };
layout(std430,set=0,binding=0) readonly buffer Frame { GpuCameraData camera; } frameData;
layout(std430,set=0,binding=2) readonly buffer PlanetaryInput { vec4 cameraHighRadiusHigh; vec4 cameraLowRadiusLow; vec4 thresholds; uvec4 controls; vec4 viewForwardHalfAngle; vec4 textureDemand; } planetaryInput;
layout(std430,set=0,binding=6) readonly buffer Presentations { Presentation values[]; } presentations;
layout(std430,set=0,binding=11) readonly buffer Environments { Environment value; } environmentData;
layout(push_constant) uniform StellarLighting { vec4 sourceCenterExposure; vec4 sourceColorAmbient; vec4 radianceGlowEnabled; } lighting;
layout(location=0) in vec2 uv;
layout(location=0) out vec4 outColor;

float Hash(vec3 p){p=fract(p*.1031);p+=dot(p,p.yzx+33.33);return fract((p.x+p.y)*p.z);}
float Noise(vec3 p){vec3 i=floor(p),f=fract(p);f=f*f*(3.0-2.0*f);return mix(mix(mix(Hash(i),Hash(i+vec3(1,0,0)),f.x),mix(Hash(i+vec3(0,1,0)),Hash(i+vec3(1,1,0)),f.x),f.y),mix(mix(Hash(i+vec3(0,0,1)),Hash(i+vec3(1,0,1)),f.x),mix(Hash(i+vec3(0,1,1)),Hash(i+vec3(1)),f.x),f.y),f.z);}
float Fbm(vec3 p){float v=0.0,w=.55;for(int i=0;i<4;i++){v+=w*Noise(p);p=p*2.03+vec3(7.1,19.7,3.3);w*=.5;}return v;}
vec3 RotateQuaternion(vec3 point,vec4 quaternion){return point+2.0*cross(quaternion.xyz,cross(quaternion.xyz,point)+quaternion.w*point);}
bool SphereInterval(vec3 ro,vec3 rd,vec3 center,float radius,out float nearT,out float farT){vec3 oc=ro-center;float b=dot(oc,rd),c=dot(oc,oc)-radius*radius,h=b*b-c;if(h<0.0)return false;h=sqrt(h);nearT=-b-h;farT=-b+h;return farT>0.0;}
void main(){
  Environment e=environmentData.value;if(e.identity.z==0u){outColor=vec4(0);return;}
  vec2 ndc=uv*2.0-1.0;vec4 reconstructed=inverse(frameData.camera.viewProjection)*vec4(ndc,.5,1.0);vec3 rd=normalize(reconstructed.xyz/max(abs(reconstructed.w),1e-7));vec3 ro=vec3(0);vec3 center=e.centerRadius.xyz;float planet=e.centerRadius.w;
  float atmosphereRadius=planet+e.atmosphere.x,nearT,farT;if(!SphereInterval(ro,rd,center,atmosphereRadius,nearT,farT)){outColor=vec4(0);return;}nearT=max(nearT,0.0);
  float surfaceNear,surfaceFar;if(SphereInterval(ro,rd,center,planet+e.ocean.x,surfaceNear,surfaceFar)&&surfaceNear>0.0)farT=min(farT,surfaceNear);if(farT<=nearT){outColor=vec4(0);return;}
  vec3 sun=normalize(lighting.sourceCenterExposure.xyz-center);float mu=dot(rd,sun);float rayleighPhase=.059683*(1.0+mu*mu);float g=e.atmosphere.w;float miePhase=.119366*(1.0-g*g)/pow(max(1.0+g*g-2.0*g*mu,.01),1.5);
  float stepLength=(farT-nearT)/10.0;vec3 scatter=vec3(0);float optical=0.0;float cloudAlpha=0.0;vec3 cloudColor=vec3(0);
  for(int i=0;i<10;i++){float t=nearT+(float(i)+.5)*stepLength;vec3 p=ro+rd*t;float altitude=length(p-center)-planet;float rayleigh=exp(-max(altitude,0.0)/e.atmosphere.y);float mie=exp(-max(altitude,0.0)/e.atmosphere.z);vec3 local=e.scattering.rgb*rayleigh*rayleighPhase+vec3(e.scattering.w*mie*miePhase);scatter+=local*stepLength*.00012;optical+=(rayleigh*.00000012+mie*.0000004)*stepLength;
    if((e.identity.z&2u)!=0u&&altitude>=e.clouds.x&&altitude<=e.clouds.y){vec3 d=normalize(p-center);vec4 bodyOrientation=presentations.values[0].bodyOrientation;vec3 bodyDirection=RotateQuaternion(d,vec4(-bodyOrientation.xyz,bodyOrientation.w));float shape;if(e.identity.w==2u){uint desired=uint(planetaryInput.textureDemand.w);vec4 earthAlbedo;float earthElevation,earthCloud,earthBlend;uint earthLevel;EarthSurfaceSample(bodyDirection,desired,planetaryInput.controls.w,earthAlbedo,earthElevation,earthCloud,earthBlend,earthLevel);shape=earthCloud;}else shape=Fbm(bodyDirection*e.cloudShape.x)+.35*Fbm(bodyDirection*e.cloudShape.y);float density=smoothstep(e.identity.w==2u?.16:e.clouds.z,e.identity.w==2u?.78:1.08,shape)*e.clouds.w*stepLength/max(e.clouds.y-e.clouds.x,1.0);float transmittance=1.0-cloudAlpha;float sunTerm=.28+.72*max(dot(d,sun),0.0);cloudColor+=vec3(.62,.69,.80)*density*transmittance*sunTerm;cloudAlpha=clamp(cloudAlpha+density*transmittance,0.0,.70);}}
  float cameraAltitude=length(center)-planet;float inside=1.0-smoothstep(0.0,e.atmosphere.x,cameraAltitude);float localDay=max(dot(normalize(-center),sun),0.0);float atmosphereAlpha=clamp(1.0-exp(-optical),0.0,.88);vec3 atmosphereColor=scatter*e.oceanColorExposure.w+vec3(.035,.12,.38)*inside*atmosphereAlpha*(.35+.65*localDay);vec3 color=atmosphereColor+cloudColor;float alpha=clamp(atmosphereAlpha+cloudAlpha*(1.0-atmosphereAlpha),0.0,.97);outColor=vec4(color/max(alpha,.001),alpha);
}
