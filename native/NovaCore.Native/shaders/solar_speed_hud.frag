#version 460
#extension GL_GOOGLE_include_directive : require
#include "solar_sans_sdf.glsl"

layout(location=0) in vec2 glyphUv;
layout(location=1) flat in int characterCode;
layout(location=2) flat in uint elementKind;
layout(location=3) flat in float opacity;
layout(location=0) out vec4 outColor;

float RoundedPanel(vec2 uv){vec2 q=abs(uv-.5)-vec2(.47,.39);return length(max(q,0.0))+min(max(q.x,q.y),0.0)-.06;}
void main(){
  if(elementKind==0u){float edge=RoundedPanel(glyphUv);float coverage=1.0-smoothstep(-.008,.008,edge);float border=1.0-smoothstep(-.020,-.006,abs(edge));vec3 panel=mix(vec3(.018,.023,.032),vec3(.11,.14,.19),border*.22);outColor=vec4(panel,.78*coverage*opacity);return;}
  float coverage=SansGlyphCoverage(characterCode,glyphUv,elementKind==1u?.050:.055);float halo=SansGlyphCoverage(characterCode,glyphUv,.125);if(halo<=0.0)discard;vec3 ink=elementKind==1u?vec3(.66,.72,.81):vec3(.92,.95,.99);float alpha=max(coverage*(elementKind==1u?.88:.98),halo*.42)*opacity;outColor=vec4(mix(vec3(.012,.018,.028),ink,coverage),alpha);
}
