#version 460
layout(push_constant) uniform StellarLighting { vec4 sourceCenterExposure; vec4 sourceColorAmbient; vec4 radianceGlowEnabled; } lighting;
layout(location=0) in vec2 local;
layout(location=1) flat in uint stellar;
layout(location=0) out vec4 outColor;
void main(){if(stellar==0u)discard;float r=length(local);if(r>=1.0)discard;float halo=pow(max(0.0,1.0-r),3.0)*lighting.radianceGlowEnabled.y;outColor=vec4(lighting.sourceColorAmbient.rgb*(3.2*halo),halo*.68);}
