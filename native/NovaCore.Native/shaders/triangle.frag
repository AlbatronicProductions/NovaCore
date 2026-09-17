#version 460
layout(location = 0) in vec3 color;
layout(location = 1) in vec3 normal;
layout(location = 2) in vec3 cameraRelativePosition;
layout(location = 3) flat in uint mesh;
layout(location = 4) in vec3 material;
layout(push_constant) uniform StellarLighting { vec4 sourceCenterExposure; vec4 sourceColorAmbient; vec4 radianceGlowEnabled; } lighting;
layout(location = 0) out vec4 outColor;
vec3 Brdf(vec3 n,vec3 v,vec3 l,vec3 radiance){
  vec3 h=normalize(v+l);float nv=max(dot(n,v),.0001),nl=max(dot(n,l),0.0),nh=max(dot(n,h),0.0),vh=max(dot(v,h),0.0);
  float a=max(material.y*material.y,.0025),a2=a*a,d=nh*nh*(a2-1.0)+1.0;
  float distribution=a2/(3.14159265359*d*d);
  float k=(material.y+1.0)*(material.y+1.0)/8.0;
  float visibility=nv/(nv*(1.0-k)+k)*nl/(nl*(1.0-k)+k);
  vec3 f0=mix(vec3(.04),color,material.x),f=f0+(1.0-f0)*pow(1.0-vh,5.0);
  return ((1.0-f)*(1.0-material.x)*color/3.14159265359+distribution*visibility*f/max(4.0*nv*nl,.0001))*radiance*nl;
}
void main() {
  if(mesh>=1024u){
    vec3 n=normalize(normal),v=normalize(-cameraRelativePosition);
    // Fixed presentation lighting in linear space; no simulation authority.
    vec3 lit=Brdf(n,v,normalize(vec3(-.4,.8,1.0)),vec3(3.4,3.3,3.1))+Brdf(n,v,normalize(vec3(.7,-.3,-.4)),vec3(1.1,1.3,1.6));
    lit+=color*(.16*(1.0-material.x)+.07*material.x)+color*material.z;
    outColor=vec4(lit,1.0);return;
  }
  if(mesh!=3u&&mesh!=4u){outColor=vec4(color,1.0);return;}
  vec3 lightDirection=normalize(lighting.sourceCenterExposure.xyz-cameraRelativePosition);
  float diffuse=max(dot(normalize(normal),lightDirection),0.0);
  float illumination=max(lighting.sourceColorAmbient.w,.035)+(1.0-max(lighting.sourceColorAmbient.w,.035))*diffuse;
  outColor=vec4(color*illumination,1.0);
}
