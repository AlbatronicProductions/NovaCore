// Scalar arithmetic shared with the CPU-only native regression. Keep this free
// of descriptors/invocations so the production expression itself is exercised.
float terrainEdgeFactor(float ax,float ay,float aw,float bx,float by,float bw,
  float midDistance,float edgeLength,float alignment,float width,float height,float verticalTan)
{
  float fade=1-clamp(midDistance/50,0.0f,1.0f);
  // The distance policy owns this result. Do not evaluate an unused projective
  // singularity and subsequently multiply infinity by zero.
  if(fade==0.0f||edgeLength==0.0f)return 1.0f;
  // At the eye plane projected demand is unbounded. This is the same limiting
  // result as the existing [1,64] policy, not a new distance/quality ceiling.
  if(aw==0.0f||bw==0.0f)return 64.0f;
  float dx=(ax/aw-bx/bw)*width,dy=(ay/aw-by/bw)*height;
  float pixels=.5f*sqrt(dx*dx+dy*dy);
  float skew=(alignment-.8f)/.2f;
  if(skew>0.0f){
    float midW=abs((aw+bw)*.5f);
    if(midW==0.0f)return 64.0f;
    float compensation=.5f*1.41421356237f*height*(.6f*edgeLength)/(midW*verticalTan);
    // A dot of normalized vectors can round above one. Preserve its geometric
    // domain, and avoid 0*infinity in mix at its exact endpoint.
    if(skew>=1.0f)pixels=compensation;
    else pixels=mix(pixels,compensation,skew);
  }
  // Overflow of projected demand has the same saturated limit. Do not pass a
  // NaN to SPIR-V FClamp, whose NaN result is implementation-dependent.
  if(isnan(pixels)||isinf(pixels))return 64.0f;
  return clamp(pixels/3*fade,1.0f,64.0f);
}
