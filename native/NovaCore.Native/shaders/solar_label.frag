#version 460
#extension GL_GOOGLE_include_directive : require
#include "solar_sans_sdf.glsl"
layout(location=0) in vec2 glyphUv;
layout(location=1) flat in int characterCode;
layout(location=2) flat in vec4 labelColor;
layout(location=0) out vec4 outColor;
void main(){
  float ink=SansGlyphCoverage(characterCode,glyphUv,.052);float halo=SansGlyphCoverage(characterCode,glyphUv,.145);
  if(halo<=0.0)discard;float alpha=max(ink*labelColor.a,halo*.72*labelColor.a);vec3 color=mix(vec3(.008,.012,.020),labelColor.rgb,ink);
  outColor=vec4(color,alpha);
}
