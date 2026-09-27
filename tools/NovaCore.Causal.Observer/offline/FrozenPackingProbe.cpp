// Executes the exact shader integer functions, using checked ordinary memory.
// No Vulkan or driver dependency. This qualifies encoding, not hardware cost.
#include "FrozenPacking.h"
#include <vector>
#include <iostream>
#include <random>
using uint=uint32_t;using std::min;using namespace nc::frozen;
struct Words {std::vector<uint> words;};
static Words physical,visibility,compacted,counters,indirect,packed,metadata;
static struct {PackParameters values;} p;
static uint atomicOr(uint& target,uint value){auto old=target;target|=value;return old;}
static uint atomicCompSwap(uint& target,uint compare,uint value){auto old=target;if(target==compare)target=value;return old;}
#include "FrozenPackShader.generated.inl"
#include "shaders/frozen_capture_pack_shared.glsl"
static unsigned checks{};
static void Check(bool ok,const char* why){++checks;if(!ok)throw std::runtime_error(why);}
static Header Setup(uint v,uint t,uint count){Header h;h.Begin();h[Frame]=0x123456789ull;h[Identity]=0xabcdef123ull;h[Vertices]=v;h[Triangles]=t;h[CaptureTransport]=4;
 h.Bulk(Physical,v*64ull,64);h.Bulk(Indices,t*12ull,4);h.Bulk(Visibility,t*4ull,4);h.Bulk(Compacted,t*12ull,4);h.Bulk(Counters,168,4);h.Bulk(Indirect,20,4);
 physical.words.resize(v*16);visibility.words.resize(t);compacted.words.resize(t*3);counters.words.resize(42);indirect.words={count,1,0,0,0};metadata.words.assign(16,0);
 std::mt19937 random(v+t+count);for(auto& x:physical.words)x=random();for(uint i=4;i<v;++i)for(uint k=12;k<16;++k)physical.words[i*16+k]=0;
 for(auto& x:visibility.words)x=random();for(auto& x:compacted.words)x=random();for(auto& x:counters.words)x=random();
 packed.words.assign(size_t((h[PayloadBytes]+3)/4+32),0xcdcdcdcd);p.values=PackingParameters(h);return h;}
static void Run(Header& h,bool reverse=false){constexpr uint lanes=65536;for(uint j=0;j<lanes;++j){uint lane=reverse?lanes-1-j:j;FrozenPackLane(lane,lanes,p.values[0],p.values[1],p.values[2],p.values[3],p.values[4],p.values[5],p.values[6]);}
 Check(std::all_of(packed.words.end()-32,packed.words.end(),[](uint x){return x==0xcdcdcdcd;}),"destination canary overwritten");}
int main(){try{
 for(uint v:{1u,2u,3u,4u,5u,127u,256u,65537u,712106u})for(uint mode=0;mode<3;++mode){uint t=v*2,count=mode==0?0:mode==1?t*3/2/3*3:t*3;auto h=Setup(v,t,count);Run(h,mode==1);
   Check(CompletePacking(h,reinterpret_cast<uint8_t*>(packed.words.data()),metadata.words.data()),"valid packing rejected");
   auto metadataSaved=metadata.words;auto valid=h;
   for(size_t k=0;k<16;++k){metadata.words=metadataSaved;metadata.words[k]^=1;auto changed=valid;Check(!CompletePacking(changed,reinterpret_cast<uint8_t*>(packed.words.data()),metadata.words.data()),"corrupt packing metadata accepted");}metadata.words=metadataSaved;
   ExpandPhysicalPacking(h,reinterpret_cast<uint8_t*>(packed.words.data()));
   Check(std::memcmp(packed.words.data()+p.values[2],physical.words.data(),v*64ull)==0,"physical bits not lossless");
   Check(std::memcmp(packed.words.data()+p.values[3],visibility.words.data(),t*4ull)==0,"visibility flags normalized");
   Check(std::memcmp(packed.words.data()+p.values[4],compacted.words.data(),count*4ull)==0,"actual compacted order changed");
   Check(std::all_of(packed.words.begin()+p.values[4]+count,packed.words.begin()+p.values[4]+t*3,[](uint x){return x==0xcdcdcdcd;}),"unwritten prefix tail copied");
   Check(std::memcmp(packed.words.data()+p.values[5],counters.words.data(),168)==0,"counters changed");
 }
 for(uint bad:{1u,2u,25u,0xffffffffu}){auto h=Setup(5,8,bad);Run(h);Check(metadata.words[1]==1&&!CompletePacking(h,reinterpret_cast<uint8_t*>(packed.words.data()),metadata.words.data()),"bad count accepted");Check(packed.words[p.values[4]]==0xcdcdcdcd,"invalid prefix read");}
 for(uint word=1;word<5;++word){auto h=Setup(5,8,3);indirect.words[word]^=1;Run(h);Check(metadata.words[1]==1,"bad indirect contract accepted");}
 {auto h=Setup(9,16,24);physical.words[6*16+14]=0x80000000;Run(h,true);Check(metadata.words[1]==2&&metadata.words[12]==7&&metadata.words[13]==2&&metadata.words[14]==0x80000000&&!CompletePacking(h,reinterpret_cast<uint8_t*>(packed.words.data()),metadata.words.data()),"nonzero omitted word lost");}
 std::cout<<"PASS packing checks="<<checks<<"; exact shared shader source; GPU calls=0\n";return 0;
}catch(const std::exception& e){std::cerr<<"FAIL "<<e.what()<<'\n';return 1;}}
