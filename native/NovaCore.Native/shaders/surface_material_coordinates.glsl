#ifndef NOVACORE_SURFACE_MATERIAL_COORDINATES
#define NOVACORE_SURFACE_MATERIAL_COORDINATES
// The physical surface supplies the receiver. Preserve its camera-relative
// differential and restore body-fixed identity in FP64, without another ray hit.
dvec3 SurfaceMaterialBodyPosition(vec3 cameraHigh,vec3 cameraLow,vec3 relativeBody)
{
  return dvec3(cameraHigh)+dvec3(cameraLow)+dvec3(relativeBody);
}
#endif
