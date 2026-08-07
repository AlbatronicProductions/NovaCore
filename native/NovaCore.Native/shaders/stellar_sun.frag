#version 460
layout(push_constant) uniform StellarLighting { vec4 sourceCenterExposure; vec4 sourceColorAmbient; vec4 radianceGlowEnabled; } lighting;
layout(location=0) in vec3 normal;
layout(location=1) in vec3 viewDirection;
layout(location=2) flat in uint stellar;
layout(location=0) out vec4 outColor;
float Surface(vec3 n){float a=sin(n.x*37.0+sin(n.y*19.0))*sin(n.z*31.0+n.y*13.0);float b=sin(dot(n,vec3(31.0,47.0,59.0))+sin(n.z*23.0));float c=sin(dot(n,vec3(-71.0,29.0,43.0)));return .5*a+.3*b+.2*c;}
void main(){if(stellar==0u)discard;float limb=clamp(dot(normalize(normal),normalize(viewDirection)),0.0,1.0);float variation=.96+.04*Surface(normalize(normal));float radiance=lighting.radianceGlowEnabled.x*(.45+.55*sqrt(limb))*variation;outColor=vec4(lighting.sourceColorAmbient.rgb*radiance,1);}
