#ifndef NOVACORE_REGIONAL_PHYSICAL_GLSL
#define NOVACORE_REGIONAL_PHYSICAL_GLSL
// The complete immutable catalog chooses the canonical record independently
// of residency. A missing chosen record is never replaced with zero/parent data.
struct RegionalEntry{uvec4 address;uvec4 storage;vec4 range;};
layout(set=0,binding=53,std430) readonly buffer RegionalCatalog{
  uvec4 info;uvec4 identity;RegionalEntry entries[];
} regionalCatalog;
layout(set=0,binding=54,std430) readonly buffer RegionalResiduals{uint words[];} regionalResiduals;
bool regionalLess(uvec4 a,uvec4 b){for(uint i=0u;i<4u;i++){if(a[i]!=b[i])return a[i]<b[i];}return false;}
int RegionalRecord(dvec3 direction,out dvec2 localUv){
  localUv=dvec2(0);if(regionalCatalog.info.x==0u)return -1;
  dvec3 absolute=abs(direction);uint face;
  if(absolute.x>=absolute.y&&absolute.x>=absolute.z)face=direction.x>=0.0?0u:1u;
  else if(absolute.y>=absolute.z)face=direction.y>=0.0?2u:3u;
  else face=direction.z>=0.0?4u:5u;
  if((regionalCatalog.info.w&(1u<<face))==0u)return -1;
  dvec2 uv;ProductionDirectionAddressD(direction,face,uv);
  for(int level=int(regionalCatalog.info.z);level>=int(regionalCatalog.info.y);--level){
    uint cells=1u<<uint(level);uvec2 cell=min(uvec2(floor(uv*double(cells))),uvec2(cells-1u));
    uvec4 key=uvec4(face,uint(level),cell);uint lo=0u,hi=regionalCatalog.info.x;
    while(lo<hi){uint mid=(lo+hi)/2u;if(regionalLess(regionalCatalog.entries[mid].address,key))lo=mid+1u;else hi=mid;}
    if(lo<regionalCatalog.info.x&&all(equal(regionalCatalog.entries[lo].address,key))){localUv=clamp(uv*double(cells)-dvec2(cell),dvec2(0),dvec2(1));return int(lo);}
  }return -1;
}
double RegionalCoverage(RegionalEntry entry,dvec2 uv){
  double weight=1.0;double distances[4]=double[4](uv.x,1.0-uv.x,uv.y,1.0-uv.y);
  for(uint i=0u;i<4u;++i)if((entry.storage.z&(1u<<i))==0u){double t=clamp(distances[i]/(8.0/256.0),0.0,1.0);weight=min(weight,t*t*(3.0-2.0*t));}
  return weight;
}
double RegionalTexel(uint offset,uint index){uint word=regionalResiduals.words[offset+index/2u];return double((index&1u)==0u?word&65535u:word>>16u);}
double RegionalPhysicalResidual(dvec3 direction){
  dvec2 uv;int index=RegionalRecord(direction,uv);if(index<0)return 0.0;
  RegionalEntry entry=regionalCatalog.entries[index];
  // Unreachable under the host dependency gate. Keep an invalid diagnostic
  // sentinel here; readiness is enforced before dispatch, not inferred from height.
  if(entry.storage.y==0u)return packDouble2x32(uvec2(0u,0x7ff80000u));
  dvec2 pixel=3.5+uv*256.0;uvec2 p=uvec2(floor(pixel)),q=min(p+1u,uvec2(263));dvec2 f=fract(pixel);
  uint offset=entry.storage.x;
  double a=RegionalTexel(offset,p.y*264u+p.x),b=RegionalTexel(offset,p.y*264u+q.x);
  double c=RegionalTexel(offset,q.y*264u+p.x),d=RegionalTexel(offset,q.y*264u+q.x);
  double encoded=mix(mix(a,b,f.x),mix(c,d,f.x),f.y)/65535.0;
  // CPU per-record range subtraction is FP32 before its promotion to double.
  return (double(entry.range.x)+encoded*double(entry.range.y-entry.range.x))*RegionalCoverage(entry,uv);
}
#endif
