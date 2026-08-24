#ifndef NOVACORE_PRODUCTION_CUBE_FILTER_GLSL
#define NOVACORE_PRODUCTION_CUBE_FILTER_GLSL

// Patch-local array coordinates are discontinuous at patch and cube-face
// boundaries even though the represented spherical surface is continuous.
// Implicit derivatives therefore cannot be used for anisotropic filtering:
// a 1 -> 0 UV wrap or array-layer change can be interpreted as a screen-sized
// texture footprint.  Derive the ordinary footprint from the continuous
// face address and bound discontinuities against the physical direction
// derivative.  The stored payload includes four gutter texels on each edge.
void ProductionPayloadGradients(
  vec2 continuousFaceUv,
  vec3 unitDirection,
  float levelCells,
  out vec2 gradientX,
  out vec2 gradientY)
{
  const float interiorToStored=256.0/264.0;
  gradientX=dFdx(continuousFaceUv)*levelCells*interiorToStored;
  gradientY=dFdy(continuousFaceUv)*levelCells*interiorToStored;

  // A relaxed cube-sphere face spans four direction units around its
  // circumference-scale footprint.  This is deliberately a generous bound:
  // normal anisotropic footprints pass unchanged, while derivative spikes
  // caused by a face/address discontinuity collapse to an isotropic sample.
  float maximumX=max(length(dFdx(unitDirection))*levelCells*4.0*interiorToStored,1e-7);
  float maximumY=max(length(dFdy(unitDirection))*levelCells*4.0*interiorToStored,1e-7);
  float lengthX=length(gradientX),lengthY=length(gradientY);
  gradientX=lengthX>maximumX*8.0?vec2(0.0):(lengthX>maximumX?gradientX*(maximumX/lengthX):gradientX);
  gradientY=lengthY>maximumY*8.0?vec2(0.0):(lengthY>maximumY?gradientY*(maximumY/lengthY):gradientY);
}

#endif
