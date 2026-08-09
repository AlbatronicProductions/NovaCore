#version 460

layout(location=0) in vec2 glyphUv;
layout(location=1) flat in int characterCode;
layout(location=2) flat in uint elementKind;
layout(location=3) flat in float opacity;
layout(location=0) out vec4 outColor;

uvec2 GlyphBits(int value){
  int c=value;if(c>=97&&c<=122)c-=32;
  if(c==48)return uvec2(0xa33ae62eu,0x3u);if(c==49)return uvec2(0x884210c4u,0x3u);if(c==50)return uvec2(0xc444422eu,0x7u);if(c==51)return uvec2(0xe107420fu,0x3u);if(c==52)return uvec2(0x11f4a988u,0x2u);
  if(c==53)return uvec2(0xe107843fu,0x3u);if(c==54)return uvec2(0xa317842eu,0x3u);if(c==55)return uvec2(0x8422221fu,0x0u);if(c==56)return uvec2(0xa317462eu,0x3u);if(c==57)return uvec2(0xa10f462eu,0x3u);
  if(c==65)return uvec2(0x631fc62eu,0x4u);if(c==68)return uvec2(0xe318c62fu,0x3u);if(c==69)return uvec2(0xc217843fu,0x7u);if(c==73)return uvec2(0xc842109fu,0x7u);if(c==76)return uvec2(0xc2108421u,0x7u);
  if(c==77)return uvec2(0x631ad771u,0x4u);if(c==78)return uvec2(0x631cd671u,0x4u);if(c==79)return uvec2(0xa318c62eu,0x3u);if(c==80)return uvec2(0x4217c62fu,0x0u);if(c==82)return uvec2(0x5257c62fu,0x4u);
  if(c==83)return uvec2(0xe107043eu,0x3u);if(c==84)return uvec2(0x842109fu,0x1u);if(c==85)return uvec2(0xa318c631u,0x3u);if(c==87)return uvec2(0x775ac631u,0x4u);if(c==88)return uvec2(0x62a22a31u,0x4u);
  if(c==46)return uvec2(0u,1u);if(c==44)return uvec2(0x88000000u,0u);if(c==40)return uvec2(0x04108444u,1u);if(c==41)return uvec2(0x11084104u,1u);return uvec2(0u);
}
float GlyphCoverage(int code,vec2 point){
  if(code==32)return 0.0;vec2 grid=vec2(5,7);ivec2 cell=ivec2(clamp(floor(point*grid),vec2(0),grid-1));int bit=cell.y*5+cell.x;uvec2 bits=GlyphBits(code);bool filled=bit<32?((bits.x>>bit)&1u)!=0u:((bits.y>>(bit-32))&1u)!=0u;if(!filled)return 0.0;vec2 q=abs(fract(point*grid)-.5)-vec2(.43);float distance=length(max(q,0.0))+min(max(q.x,q.y),0.0)-.045;float antialias=max(fwidth(distance),.012);return 1.0-smoothstep(-antialias,antialias,distance);
}
float RoundedPanel(vec2 uv){vec2 q=abs(uv-.5)-vec2(.47,.39);return length(max(q,0.0))+min(max(q.x,q.y),0.0)-.06;}
void main(){
  if(elementKind==0u){float edge=RoundedPanel(glyphUv);float coverage=1.0-smoothstep(-.008,.008,edge);float border=1.0-smoothstep(-.020,-.006,abs(edge));vec3 panel=mix(vec3(.018,.023,.032),vec3(.11,.14,.19),border*.22);outColor=vec4(panel,.78*coverage*opacity);return;}
  float coverage=GlyphCoverage(characterCode,glyphUv);if(coverage<=0.0)discard;vec3 ink=elementKind==1u?vec3(.61,.67,.76):vec3(.91,.94,.98);outColor=vec4(ink,coverage*opacity*(elementKind==1u?.88:.98));
}
