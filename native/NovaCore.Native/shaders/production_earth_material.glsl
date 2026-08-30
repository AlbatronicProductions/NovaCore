#ifndef NOVACORE_PRODUCTION_EARTH_MATERIAL_GLSL
#define NOVACORE_PRODUCTION_EARTH_MATERIAL_GLSL

// One terrain-v5 material authority shared by the distant bootstrap sphere,
// shallow global cube-sphere and dynamic anchored hierarchy. Representation
// ownership may change; these physical-surface inputs may not.
struct ProductionEarthMaterial
{
  vec3 albedo;
  float roughness;
  float specular;
};

ProductionEarthMaterial ProductionEarthSurfaceMaterial(
  vec3 payloadAlbedo,
  float landMask,
  float terrainHeight,
  vec4 response)
{
  bool land=landMask>=.5;
  vec3 albedo=payloadAlbedo;
  if(!land)albedo=mix(albedo,vec3(.012,.065,.18),.22);
  else albedo*=mix(.82,1.08,smoothstep(-200.0,4200.0,terrainHeight));
  return ProductionEarthMaterial(albedo,response.x,response.y);
}

#endif
