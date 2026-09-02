#version 450
layout(location=0) in vec3 inNormal;
layout(location=0) out vec4 outColor;
void main(){outColor=vec4(abs(normalize(inNormal))*0.75+0.25,1.0);}
