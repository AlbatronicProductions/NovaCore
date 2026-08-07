#version 460
layout(input_attachment_index=0,set=0,binding=7) uniform subpassInput sceneColor;
layout(push_constant) uniform StellarLighting { vec4 sourceCenterExposure; vec4 sourceColorAmbient; vec4 radianceGlowEnabled; } lighting;
layout(location=0) in vec2 uv;
layout(location=0) out vec4 outColor;
vec3 Aces(vec3 x){const float a=2.51,b=.03,c=2.43,d=.59,e=.14;return clamp((x*(a*x+b))/(x*(c*x+d)+e),0.0,1.0);}
vec3 LinearToSrgb(vec3 x){return mix(12.92*x,1.055*pow(x,vec3(1.0/2.4))-.055,step(vec3(.0031308),x));}
void main(){float exposure=lighting.sourceCenterExposure.w>0.0?lighting.sourceCenterExposure.w:1.0;outColor=vec4(LinearToSrgb(Aces(max(subpassLoad(sceneColor).rgb,vec3(0))*exposure)),1);}
