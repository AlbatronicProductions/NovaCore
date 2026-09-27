#pragma once
#include "FrozenCapture.h"
namespace nc::frozen {
constexpr size_t PackMetadataBytes=64;
constexpr uint32_t PackMagic=0x344b5046;
using PackParameters=std::array<uint32_t,12>;
inline PackParameters PackingParameters(const Header& h){
  if(!h[Vertices]||h[Vertices]>SlotBytes/64||!h[Triangles]||h[Triangles]>SlotBytes/12||h[PayloadBytes]>SlotBytes)throw std::runtime_error("frozen pack capacity");
  PackParameters p{uint32_t(h[Vertices]),uint32_t(h[Triangles])};
  for(size_t i=0;i<5;++i){auto& s=h.sections[std::array<size_t,5>{0,2,3,4,5}[i]];
    if(s.offset<HeaderBytes||s.offset%4||s.bytes>SlotBytes||s.offset-HeaderBytes>SlotBytes-s.bytes)throw std::runtime_error("frozen pack section bounds");p[2+i]=uint32_t((s.offset-HeaderBytes)/4);}
  p[7]=uint32_t(h[Frame]);p[8]=uint32_t(h[Frame]>>32);p[9]=uint32_t(h[Identity]);p[10]=uint32_t(h[Identity]>>32);return p;
}
inline bool CompletePacking(Header& h,const uint8_t* bytes,const uint32_t* m){
  if(!m||m[1])return false;
  uint64_t count{};try{count=CompactedPrefixBytes(h,bytes)/4;}catch(const std::runtime_error&){return false;}
  const uint64_t actual=48*h[Vertices]+16*std::min<uint64_t>(h[Vertices],4)+4*h[Triangles]+count*4+188+PackMetadataBytes;
  if(!m||m[0]!=PackMagic||m[1]||m[2]!=h[Vertices]||m[3]!=h[Triangles]||m[4]!=count||
    (uint64_t(m[5])|(uint64_t(m[6])<<32))!=h[Frame]||(uint64_t(m[7])|(uint64_t(m[8])<<32))!=h[Identity]||
    m[9]!=actual||m[10]!=64*h[Vertices]+4*h[Triangles]+count*4+188||m[11]!=(h[Vertices]-std::min<uint64_t>(h[Vertices],4))*4||m[12]||m[13]||m[14]||m[15])return false;
  for(size_t i=6;i<h[Sections];++i)if(h.sections[i].offset+h.sections[i].bytes>HeaderBytes-PackMetadataBytes)return false;
  std::memcpy(reinterpret_cast<uint8_t*>(&h)+HeaderBytes-PackMetadataBytes,m,PackMetadataBytes);
  h[GpuCopiedBytes]=actual;h[HeaderFlags]|=4;return true;
}
}
