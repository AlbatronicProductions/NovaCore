#version 460
layout(location = 0) in vec3 color;
layout(location = 1) in vec3 normal;
layout(location = 2) in vec3 cameraRelativePosition;
layout(location = 3) flat in uint mesh;
layout(push_constant) uniform StellarLighting { vec4 sourceCenterExposure; vec4 sourceColorAmbient; vec4 radianceGlowEnabled; } lighting;
layout(location = 0) out vec4 outColor;
void main() {
  if(mesh!=3u){outColor=vec4(color,1.0);return;}
  vec3 lightDirection=normalize(lighting.sourceCenterExposure.xyz-cameraRelativePosition);
  float diffuse=max(dot(normalize(normal),lightDirection),0.0);
  float illumination=max(lighting.sourceColorAmbient.w,.035)+(1.0-max(lighting.sourceColorAmbient.w,.035))*diffuse;
  outColor=vec4(color*illumination,1.0);
}
