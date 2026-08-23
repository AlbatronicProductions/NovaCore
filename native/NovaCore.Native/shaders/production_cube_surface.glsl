#ifndef NOVACORE_PRODUCTION_CUBE_SURFACE_GLSL
#define NOVACORE_PRODUCTION_CUBE_SURFACE_GLSL

// GLSL mirror of RelaxedCubeSphereProjection.AlgorithmVersion 1.  Extended
// coordinates cross a canonical face edge before the symmetric spherification,
// so shared edge and triple-corner vertices are bit-identical in direction.
dvec3 ProductionCubeD(uint face,double a,double b)
{
  if(face==0u)return dvec3(1,b,-a);
  if(face==1u)return dvec3(-1,b,a);
  if(face==2u)return dvec3(a,1,-b);
  if(face==3u)return dvec3(a,-1,b);
  if(face==4u)return dvec3(a,b,1);
  return dvec3(-a,b,-1);
}
dvec3 ProductionSpherifyD(dvec3 cube)
{
  cube/=max(max(abs(cube.x),abs(cube.y)),abs(cube.z));
  dvec3 q=cube*cube;
  return dvec3(
    cube.x*sqrt(max(0.0,1.0-.5*(q.y+q.z)+q.y*q.z/3.0)),
    cube.y*sqrt(max(0.0,1.0-.5*(q.z+q.x)+q.z*q.x/3.0)),
    cube.z*sqrt(max(0.0,1.0-.5*(q.x+q.y)+q.x*q.y/3.0)));
}
dvec3 ProductionProjectD(uvec4 address,dvec2 local)
{
  double cells=double(1u<<address.y);
  dvec2 uv=(dvec2(address.zw)+local)/cells;
  return ProductionSpherifyD(ProductionCubeD(address.x,2.0*uv.x-1.0,2.0*uv.y-1.0));
}
dvec3 ProductionProjectGridD(uvec4 address,uvec2 grid)
{
  uint denominator=16u<<address.y;
  uvec2 numerator=address.zw*16u+grid;
  dvec2 uv=dvec2(numerator)/double(denominator);
  return ProductionSpherifyD(ProductionCubeD(address.x,2.0*uv.x-1.0,2.0*uv.y-1.0));
}
vec3 ProductionSpherifyF(vec3 cube)
{
  cube/=max(max(abs(cube.x),abs(cube.y)),abs(cube.z));
  vec3 q=cube*cube;
  return vec3(
    cube.x*sqrt(max(0.0,1.0-.5*(q.y+q.z)+q.y*q.z/3.0)),
    cube.y*sqrt(max(0.0,1.0-.5*(q.z+q.x)+q.z*q.x/3.0)),
    cube.z*sqrt(max(0.0,1.0-.5*(q.x+q.y)+q.x*q.y/3.0)));
}
vec3 ProductionCubeF(uint face,float a,float b)
{
  if(face==0u)return vec3(1,b,-a);
  if(face==1u)return vec3(-1,b,a);
  if(face==2u)return vec3(a,1,-b);
  if(face==3u)return vec3(a,-1,b);
  if(face==4u)return vec3(a,b,1);
  return vec3(-a,b,-1);
}
vec2 ProductionFaceCoordinates(uint face,vec3 direction)
{
  if(face==0u)return vec2(-direction.z,direction.y)/direction.x;
  if(face==1u)return vec2(direction.z,direction.y)/-direction.x;
  if(face==2u)return vec2(direction.x,-direction.z)/direction.y;
  if(face==3u)return vec2(direction.x,direction.z)/-direction.y;
  if(face==4u)return vec2(direction.x,direction.y)/direction.z;
  return vec2(-direction.x,direction.y)/-direction.z;
}
void ProductionDirectionAddress(vec3 unitDirection,out uint face,out vec2 uv)
{
  vec3 direction=normalize(unitDirection),absolute=abs(direction);
  if(absolute.x>=absolute.y&&absolute.x>=absolute.z)face=direction.x>=0.0?0u:1u;
  else if(absolute.y>=absolute.z)face=direction.y>=0.0?2u:3u;
  else face=direction.z>=0.0?4u:5u;
  vec2 target=ProductionFaceCoordinates(face,direction),coordinates=clamp(target,vec2(-1),vec2(1));
  const float epsilon=1e-3;
  for(uint iteration=0u;iteration<4u;iteration++)
  {
    vec2 value=ProductionFaceCoordinates(face,normalize(ProductionSpherifyF(ProductionCubeF(face,coordinates.x,coordinates.y))));
    vec2 dx=(ProductionFaceCoordinates(face,normalize(ProductionSpherifyF(ProductionCubeF(face,coordinates.x+epsilon,coordinates.y))))-value)/epsilon;
    vec2 dy=(ProductionFaceCoordinates(face,normalize(ProductionSpherifyF(ProductionCubeF(face,coordinates.x,coordinates.y+epsilon))))-value)/epsilon;
    float determinant=dx.x*dy.y-dx.y*dy.x;
    if(abs(determinant)<1e-8)break;
    vec2 error=value-target;
    coordinates-=vec2((error.x*dy.y-error.y*dy.x)/determinant,(dx.x*error.y-dx.y*error.x)/determinant);
    coordinates=clamp(coordinates,vec2(-1),vec2(1));
  }
  uv=coordinates*.5+.5;
}
uint ProductionMorton(uint x,uint y)
{
  uint value=0u;for(uint bit=0u;bit<12u;bit++){value|=((x>>bit)&1u)<<(2u*bit);value|=((y>>bit)&1u)<<(2u*bit+1u);}return value;
}
uint ProductionPatchOrdinal(uint face,uint level,uint x,uint y)
{
  uint preceding=0u;for(uint previous=0u;previous<level;previous++)preceding+=6u<<(2u*previous);
  uint cells=1u<<level;return preceding+face*cells*cells+ProductionMorton(x,y);
}
vec3 ProductionProjectF(uvec4 address,vec2 local)
{
  float cells=float(1u<<address.y);
  vec2 uv=(vec2(address.zw)+local)/cells;
  return ProductionSpherifyF(vec3(ProductionCubeD(address.x,double(2.0*uv.x-1.0),double(2.0*uv.y-1.0))));
}
#endif
