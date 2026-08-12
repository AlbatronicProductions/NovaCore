struct EarthOceanMaterial
{
  vec3 albedo;
  float roughness;
  float specular;
};

EarthOceanMaterial EarthOceanBaseMaterial(
  vec3 sampledAlbedo,
  vec3 oceanColor,
  float oceanRoughness,
  float materialSpecular)
{
  EarthOceanMaterial material;
  material.albedo=mix(sampledAlbedo,oceanColor,.35);
  material.roughness=oceanRoughness;
  material.specular=max(materialSpecular,.45);
  return material;
}

float EarthOceanDetailWeight(float viewDistanceMetres)
{
  return 1.0-smoothstep(45000.0,900000.0,viewDistanceMetres);
}
