#version 460
layout(location=0) in vec3 normal;
layout(location=1) in vec3 viewDirection;
layout(location=2) in vec4 color;
layout(location=3) flat in vec3 lightDirection;
layout(location=4) noperspective in vec3 barycentric;
layout(location=0) out vec4 outColor;
layout(push_constant) uniform StellarLighting { vec4 sourceCenterExposure; vec4 sourceColorAmbient; vec4 radianceGlowEnabled; } lighting;
void main(){vec3 n=normalize(normal),l=normalize(lightDirection),v=normalize(viewDirection);uint diagnostic=floatBitsToUint(lighting.radianceGlowEnabled.w)>>16;if((diagnostic&32u)!=0u){outColor=vec4(.1,.25,1,1);return;}if((diagnostic&16u)!=0u){outColor=vec4(n*.5+.5,1);return;}if((diagnostic&8u)!=0u){outColor=vec4(color.rgb,1);return;}if((diagnostic&64u)!=0u){float edge=min(barycentric.x,min(barycentric.y,barycentric.z));float line=1.0-smoothstep(0.0,fwidth(edge)*1.25,edge);outColor=vec4(mix(color.rgb,vec3(.1,.25,1),line),1);return;}if((diagnostic&128u)!=0u){float depth=clamp(-log2(max(gl_FragCoord.z,1e-20))/64.0,0.0,1.0);outColor=vec4(vec3(depth),1);return;}if((diagnostic&256u)!=0u){outColor=vec4(.1,.25,1,1);return;}float ambient=max(lighting.sourceColorAmbient.w,.035);float diffuse=max(dot(n,l),0.0);if((diagnostic&512u)!=0u){outColor=vec4(color.rgb*(ambient+(1.0-ambient)*diffuse),1);return;}vec3 h=normalize(l+v);float specular=pow(max(dot(n,h),0.0),24.0)*.08;outColor=vec4(color.rgb*(ambient+(1.0-ambient)*diffuse)+specular,1.0);}
