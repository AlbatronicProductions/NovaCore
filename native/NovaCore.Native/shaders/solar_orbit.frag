#version 460
layout(location=0) flat in uint pathIndex;
layout(location=1) flat in float opacity;
layout(location=0) out vec4 outColor;
void main(){
  const vec3 colors[9]=vec3[](vec3(.48,.48,.48),vec3(.88,.72,.42),vec3(.08,.32,.72),vec3(.62,.62,.62),vec3(.72,.25,.14),vec3(.72,.53,.32),vec3(.82,.68,.38),vec3(.38,.78,.86),vec3(.12,.32,.86));
  outColor=vec4(colors[min(pathIndex,8u)]*.72,opacity);
}
