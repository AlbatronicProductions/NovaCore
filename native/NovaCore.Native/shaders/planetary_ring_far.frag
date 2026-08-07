#version 460
layout(location=0) in vec4 color;layout(location=1) in float radial;layout(location=2) in float cameraSide;layout(location=3) flat in vec3 lightDirection;layout(location=4) flat in vec3 ringNormal;layout(location=5) flat in float bandFrequency;
layout(push_constant) uniform StellarLighting { vec4 sourceCenterExposure; vec4 sourceColorAmbient; vec4 radianceGlowEnabled; } lighting;
layout(location=0) out vec4 outColor;
void main(){if(cameraSide>=0.0)discard;float edge=smoothstep(0.0,.035,radial)*smoothstep(0.0,.035,1.0-radial);float cassini=smoothstep(.012,.035,abs(radial-.57));float bands=.48+.52*(.5+.5*sin(radial*bandFrequency*6.2831853+sin(radial*31.0)*2.0));float alpha=color.a*edge*cassini*bands;float illumination=mix(lighting.sourceColorAmbient.w,1.0,abs(dot(normalize(ringNormal),normalize(lightDirection))));outColor=vec4(color.rgb*illumination,alpha);}
