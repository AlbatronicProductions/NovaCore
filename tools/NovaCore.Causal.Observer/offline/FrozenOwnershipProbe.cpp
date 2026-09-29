// CPU-only structural replay. Production capture functions are extracted verbatim
// by build_frozen_ownership_probe.py. Vulkan entry points below are local fakes;
// this binary is deliberately NOT linked against Vulkan or NovaCore.Native.
#include "NovaCoreNative.h"
#include "FrozenCapture.h"
#include "FrozenPacking.h"
#include "CausalRecorder.h"
#include "RecordingCallTrace.h"
#include "RegionalPhysicalResidency.h"
#include <vulkan/vulkan.h>
#include <chrono>
#include <iostream>
#include <vector>
#include "FrozenTypes.generated.inl"
using nc::causal::Phase;
using nc::causal::Kind;
using nc::frozen::Header;
constexpr uint32_t ProductionBillboardCounterCount=42;
static void Need(bool value,const char* why){if(!value)throw std::runtime_error(why);}
struct Recorder : nc::causal::Recorder {
  uint64_t events{},authorities{};
  Header authority{};
  Recorder(){frame=168;submitted=167;completed=167;swap=2;draws=13;dispatches=4;groups=1082;submissionSequence=230;}
  void Emit(Phase phase,Kind kind,int64_t result=0,std::initializer_list<uint64_t> values={}){++events;nc::causal::Recorder::Emit(phase,kind,result,values);}
  void Bytes(Phase phase,uint64_t identity,const void* data,size_t bytes){Need(bytes==sizeof(Header),"authority size");std::memcpy(&authority,data,bytes);++authorities;nc::causal::Recorder::Bytes(phase,identity,data,bytes);}
};
struct App { bool directPreparedSurfaceSubmitted{};
  struct StartupControl {uint64_t request{},ack{};uint64_t CaptureDisableRequest()const{return request;}void AcknowledgeCaptureDisable(uint64_t r){ack=r;}} startup;
  uint64_t frozenDisableRequest{};bool frozenDrainReported{};
  Recorder causal; nc::frozen::Writer frozen;
  nc::causal::RecordingCallTrace recordingTrace;
  VkFence fence=(VkFence)9;bool productionBillboardFencePending{},productionBillboardIncomingFencePending{},productionBillboardAuthoritative=true;
  VkBuffer submissionBuffer=(VkBuffer)10,gpuInputBuffer=(VkBuffer)11,planetaryPresentationBuffer=(VkBuffer)12,productionBillboardFrameBuffer=(VkBuffer)13;
  VkBuffer regionalPreparationBuffer=(VkBuffer)14,regionalCatalogBuffer=(VkBuffer)15,regionalDemandBuffer=(VkBuffer)16;
  VkDeviceMemory submissionMemory=(VkDeviceMemory)20,gpuInputMemory=(VkDeviceMemory)21,planetaryPresentationMemory=(VkDeviceMemory)22,productionBillboardFrameMemory=(VkDeviceMemory)23;
  VkDeviceMemory regionalPreparationMemory=(VkDeviceMemory)24,regionalCatalogMemory=(VkDeviceMemory)25,regionalDemandMemory=(VkDeviceMemory)26;
  PFN_vkCmdBeginDebugUtilsLabelEXT beginLabel{};PFN_vkCmdEndDebugUtilsLabelEXT endLabel{};
  VkQueryPool timestampQueries=(VkQueryPool)3;
  void Check(VkResult result,const char* error){Need(result==VK_SUCCESS,error);}
  NcFrameSubmission source{};NcFrameSubmission* submission=&source;
  uint64_t productionBillboardGeneration=1,productionBillboardIncomingGeneration{},productionBillboardTopologyHash=0x118D7D350CB66136ull,productionBillboardTopologyFamily=1;
  uint64_t productionBillboardPreparedFrameIdentity=1,productionBillboardCullFrameIdentity=1,productionBillboardRasterFrameIdentity=1;
  uint64_t productionBillboardVertexCount=13826,productionBillboardTriangleCount=27648,productionBillboardVertexCapacity=13826,productionBillboardTriangleCapacity=27648;
  uint64_t productionBillboardIncomingVertexCount{},productionBillboardIncomingTriangleCount{},productionBillboardIncomingTopologyHash{};
  uint64_t productionBillboardTopologyResidentBytes=552992,productionBillboardTopologyPeakResidentBytes=552992,productionBillboardWorkingBytes=1327420,productionBillboardPeakWorkingBytes=1327420;
  uint64_t productionBillboardWorkAllocations=1,productionBillboardWorkReuses{},productionBillboardSpareVertexCapacity{},productionBillboardSpareTriangleCapacity{};
  uint64_t regionalScratchCapacity{},regionalPayloadBytes=119737728;
  VkBuffer productionBillboardPhysicalBuffer=(VkBuffer)1,productionBillboardIndexBuffer=(VkBuffer)2,productionBillboardVisibilityBuffer=(VkBuffer)3,productionBillboardCompactedBuffer=(VkBuffer)4,productionBillboardCounterBuffer=(VkBuffer)5,productionBillboardIndirectBuffer=(VkBuffer)6;
  VkBuffer productionBillboardIncomingPhysicalBuffer{},productionBillboardSparePhysicalBuffer{},regionalScratchBuffer{};
  VkExtent2D extent{960,582}; std::array<uint64_t,3> frozenDeviceIdentity{};
  NcCameraData camera{};NcPlanetaryGpuConstants gpu{};NcPlanetaryPresentation presentation{};NcProductionBillboardFrame pupils{};
  RegionalPreparationControl preparation{};RegionalDemandBuffer demand{};nc::regionalphysical::Catalog catalog{};
  void* mapped=&camera;void* gpuInputMapped=&gpu;void* planetaryPresentationMapped=&presentation;void* productionBillboardFrameMapped=&pupils;
  void* regionalPreparationMapped=&preparation;void* regionalCatalogMapped=&catalog;void* regionalDemandMapped=&demand;
  std::vector<uint32_t> compacted=std::vector<uint32_t>(27648*3,17);
  void* productionBillboardCompactedMapped=compacted.data();
  NcProductionBillboardPupilFrame regionalPublishedPupil{};RegionalPreparationJob regionalPreparation[2]{};RegionalDependencyJob regionalJobs[2]{};bool regionalReady[2]{true,false};
  std::vector<ProductionBillboardTopologyResource> productionBillboardTopologyResources{ProductionBillboardTopologyResource{}};
  std::vector<uint8_t> topologyStorage=std::vector<uint8_t>(27648*12);
  uint64_t causalBufferBirth=1000;
  VkQueryPool frozenTopologyQueries=(VkQueryPool)4;
  std::array<VkBuffer,nc::frozen::TopologyCount> frozenTopologyBuffers{};
  std::array<VkDeviceMemory,nc::frozen::TopologyCount> frozenTopologyMemory{};
  VkBuffer productionBillboardIncomingIndexBuffer{};
  bool anchoredPipelineStatisticsFrameSubmitted{};uint64_t anchoredPipelineStatisticsTerrainFrame=168;
  std::array<VkBuffer,2> frozenBuffers{(VkBuffer)7,(VkBuffer)8};
  VkPipeline frozenPackPipeline=(VkPipeline)31;VkPipelineLayout frozenPackLayout=(VkPipelineLayout)32;
  std::array<VkDescriptorSet,2> frozenPackSets{(VkDescriptorSet)33,(VkDescriptorSet)34};
  std::array<VkBuffer,2> frozenPackMetadataBuffers{(VkBuffer)35,(VkBuffer)36};
  std::array<std::array<uint32_t,16>,2> packMetadata{};
  std::array<void*,2> frozenPackMetadataMapped{packMetadata[0].data(),packMetadata[1].data()};
  VkQueryPool anchoredPipelineStatistics=(VkQueryPool)1,frozenTimingQueries=(VkQueryPool)2;VkDevice device{};float timestampPeriodNanoseconds=1;
};
#include "FrozenLabel.generated.inl"
struct ApiCall {std::array<uint64_t,12> values{};bool operator==(const ApiCall&)const=default;};
static std::array<ApiCall,64> apiCalls{};static size_t apiCount{};
static void Api(std::initializer_list<uint64_t> values){Need(apiCount<apiCalls.size(),"API trace capacity");auto& call=apiCalls[apiCount++];call={};std::copy(values.begin(),values.end(),call.values.begin());}
static unsigned copies{},barriers{},queryCalls{};
static bool queryReady=true;
VKAPI_ATTR void VKAPI_CALL vkCmdFillBuffer(VkCommandBuffer c,VkBuffer b,VkDeviceSize o,VkDeviceSize n,uint32_t v){Api({10,uint64_t(c),uint64_t(b),o,n,v});}
VKAPI_ATTR void VKAPI_CALL vkUpdateDescriptorSets(VkDevice,uint32_t n,const VkWriteDescriptorSet* writes,uint32_t,const VkCopyDescriptorSet*){Need(n==7,"pack descriptors");for(uint32_t i=0;i<n;++i)Api({11,writes[i].dstBinding,uint64_t(writes[i].dstSet),uint64_t(writes[i].pBufferInfo->buffer),writes[i].pBufferInfo->offset,writes[i].pBufferInfo->range});}
VKAPI_ATTR void VKAPI_CALL vkCmdBindPipeline(VkCommandBuffer c,VkPipelineBindPoint b,VkPipeline p){Api({12,uint64_t(c),uint64_t(b),uint64_t(p)});}
VKAPI_ATTR void VKAPI_CALL vkCmdBindDescriptorSets(VkCommandBuffer c,VkPipelineBindPoint b,VkPipelineLayout l,uint32_t f,uint32_t n,const VkDescriptorSet* s,uint32_t,const uint32_t*){Api({13,uint64_t(c),uint64_t(b),uint64_t(l),f,n,uint64_t(*s)});}
VKAPI_ATTR void VKAPI_CALL vkCmdPushConstants(VkCommandBuffer c,VkPipelineLayout l,VkShaderStageFlags s,uint32_t o,uint32_t n,const void* data){Need(n==48,"pack parameters");auto* p=static_cast<const uint32_t*>(data);Api({14,uint64_t(c),uint64_t(l),s,o,n});Api({15,p[0],p[1],p[2],p[3],p[4],p[5],p[6],p[7],p[8],p[9],p[10]});}
VKAPI_ATTR void VKAPI_CALL vkCmdDispatch(VkCommandBuffer c,uint32_t x,uint32_t y,uint32_t z){Api({16,uint64_t(c),x,y,z});}
VKAPI_ATTR void VKAPI_CALL vkCmdResetQueryPool(VkCommandBuffer c,VkQueryPool q,uint32_t f,uint32_t n){Api({1,uint64_t(c),uint64_t(q),f,n});}
VKAPI_ATTR void VKAPI_CALL vkCmdWriteTimestamp(VkCommandBuffer c,VkPipelineStageFlagBits s,VkQueryPool q,uint32_t i){Api({2,uint64_t(c),uint64_t(s),uint64_t(q),i});}
VKAPI_ATTR void VKAPI_CALL vkCmdPipelineBarrier(VkCommandBuffer c,VkPipelineStageFlags src,VkPipelineStageFlags dst,VkDependencyFlags dep,uint32_t n,const VkMemoryBarrier* m,uint32_t bn,const VkBufferMemoryBarrier*,uint32_t im,const VkImageMemoryBarrier*){++barriers;Api({3,uint64_t(c),src,dst,dep,n,m?m->srcAccessMask:0,m?m->dstAccessMask:0,bn,im});}
VKAPI_ATTR void VKAPI_CALL vkCmdCopyBuffer(VkCommandBuffer c,VkBuffer src,VkBuffer dst,uint32_t n,const VkBufferCopy* regions){Need(n==1&&regions[0].size>0,"copy region");++copies;Api({4,uint64_t(c),uint64_t(src),uint64_t(dst),n,regions[0].srcOffset,regions[0].dstOffset,regions[0].size});}
VKAPI_ATTR void VKAPI_CALL vkCmdDraw(VkCommandBuffer c,uint32_t n,uint32_t i,uint32_t f,uint32_t fi){Api({5,uint64_t(c),n,i,f,fi});}
VKAPI_ATTR void VKAPI_CALL vkCmdEndRenderPass(VkCommandBuffer c){Api({6,uint64_t(c)});}
VKAPI_ATTR VkResult VKAPI_CALL vkEndCommandBuffer(VkCommandBuffer c){Api({7,uint64_t(c)});return VK_SUCCESS;}
static VKAPI_ATTR void VKAPI_CALL BeginLabel(VkCommandBuffer c,const VkDebugUtilsLabelEXT*){Api({8,uint64_t(c)});}
static VKAPI_ATTR void VKAPI_CALL EndLabel(VkCommandBuffer c){Api({9,uint64_t(c)});}
VKAPI_ATTR VkResult VKAPI_CALL vkGetQueryPoolResults(VkDevice,VkQueryPool pool,uint32_t,uint32_t,size_t bytes,void* data,VkDeviceSize,VkQueryResultFlags flags){
  ++queryCalls;Need(!(flags&VK_QUERY_RESULT_WAIT_BIT),"capture must never request query WAIT");if(!queryReady)return VK_NOT_READY;
  if(pool==(VkQueryPool)1){std::array<uint64_t,4> q{15368,100,15368,46104};Need(bytes==sizeof(q),"statistics bytes");std::memcpy(data,q.data(),bytes);}
  else {std::array<uint64_t,2> q{100,200};Need(bytes==sizeof(q),"timestamp bytes");std::memcpy(data,q.data(),bytes);}return VK_SUCCESS;
}
static unsigned topologyCreates{},topologyDestroys{};
namespace nc {enum class MappedBufferUse{TerrainRequestKeys};}
void CreateHostBuffer(App& a,VkDeviceSize bytes,VkBufferUsageFlags,VkBuffer& buffer,VkDeviceMemory& memory,void*& mapped,const char*,nc::MappedBufferUse){
  mapped=std::calloc(1,size_t(bytes));Need(mapped!=nullptr,"mock storage");buffer=VkBuffer(++a.causalBufferBirth);memory=VkDeviceMemory(a.causalBufferBirth+10000);++topologyCreates;
}
void DestroyHostBuffer(App&,VkBuffer& buffer,VkDeviceMemory& memory,void*& mapped){if(buffer){std::free(mapped);++topologyDestroys;}buffer={};memory={};mapped=nullptr;}
VKAPI_ATTR void VKAPI_CALL vkDestroyQueryPool(VkDevice,VkQueryPool,const VkAllocationCallbacks*){}
#include "FrozenControl.generated.inl"
#include "FrozenTopology.generated.inl"
#include "FrozenPacking.generated.inl"
#include "FrozenFunctions.generated.inl"
static App* gApp{};
#include "FrozenDraw.generated.inl"
#define vkCmdDraw CausalDraw
#include "FrozenTail.generated.inl"
#undef vkCmdDraw

static unsigned checks{};
static void Check(bool value,const char* message){Need(value,message);++checks;}
static void Initialize(App& a,const std::filesystem::path& output,std::array<std::vector<uint8_t>,2>& storage){
  a.source.physicalSurfaceGeneration=4;a.source.planetaryGpu.terrainVersion=5;a.source.planetarySurfaceMode=NC_PLANETARY_SURFACE_PRODUCTION_CUBE;a.source.productionBillboardFlags=1;
  a.pupils.current.identity[0]=1;a.regionalPublishedPupil=a.pupils.current;
  a.catalog.count=896;for(size_t i=0;i<a.catalog.entries.size();i++)a.catalog.entries[i].resident=i%179==0;
  for(size_t i=0;i<2;i++){storage[i].resize(2*1024*1024);a.frozen.slots[i].mapped=storage[i].data();}
  auto& resource=a.productionBillboardTopologyResources[0];resource.index=a.productionBillboardIndexBuffer;resource.indexBirth=10;resource.hash=a.productionBillboardTopologyHash;resource.family=uint32_t(a.productionBillboardTopologyFamily);resource.vertexCount=uint32_t(a.productionBillboardVertexCount);resource.indexCount=uint32_t(a.productionBillboardTriangleCount*3);
  auto& t=a.frozen.topologies[0];t.state=nc::frozen::Proven;t.mapped=a.topologyStorage.data();
  t.authority={1,uint64_t(resource.index),resource.indexBirth,resource.hash,resource.family,resource.vertexCount,resource.indexCount/3,2,0,resource.indexCount*4ull,4,999,11,100,12,163,100,163,20,30,0,0};
  a.frozen.Start(output);
}
static void CheckState(App& a,unsigned left,unsigned right){Check(a.frozen.slots[0].state==left&&a.frozen.slots[1].state==right,"unexpected slot ownership");}
// Ownership tests seed simulated completed GPU metadata. Byte transport itself
// is tested separately using the exact shared integer shader function.
static void SeedPackedMetadata(App& a){for(size_t i=0;i<2;++i){auto& h=a.frozen.slots[i].header;if(h[nc::frozen::CaptureTransport]!=4)continue;
  auto count=uint32_t(nc::frozen::CompactedPrefixBytes(h,a.frozen.slots[i].mapped)/4);auto& m=a.packMetadata[i];m={};
  m[0]=nc::frozen::PackMagic;m[2]=uint32_t(h[nc::frozen::Vertices]);m[3]=uint32_t(h[nc::frozen::Triangles]);m[4]=count;m[5]=uint32_t(h[nc::frozen::Frame]);m[6]=uint32_t(h[nc::frozen::Frame]>>32);m[7]=uint32_t(h[nc::frozen::Identity]);m[8]=uint32_t(h[nc::frozen::Identity]>>32);
  m[9]=48*m[2]+16*std::min(m[2],4u)+4*m[3]+count*4+252;m[10]=64*m[2]+4*m[3]+count*4+188;m[11]=(m[2]-std::min(m[2],4u))*4;
}}
#ifndef NC_RECORDING_CALL_PROBE
int main(int argc,char** argv){try{
  Need(argc==2,"output directory required");std::filesystem::path output=argv[1];std::filesystem::create_directories(output);
  using namespace nc::frozen;
  // Structurally faithful metadata with CPU-owned synthetic bytes, not a replay
  // of the missing live physical vertices or exact pupil transforms.
  {
    App a;std::array<std::vector<uint8_t>,2> storage;Initialize(a,output,storage);CheckState(a,Free,Free);
    RecordFrozenCapture(a,{});CheckState(a,Free,Free);Check(a.causal.authorities==0&&copies==0,"ineligible frame recorded capture");
    a.anchoredPipelineStatisticsFrameSubmitted=true;
    auto begin=std::chrono::steady_clock::now();RecordFrozenCapture(a,{});
    auto ms=std::chrono::duration<double,std::milli>(std::chrono::steady_clock::now()-begin).count();
    CheckState(a,Free,Reserved);auto& slot=a.frozen.slots[1];
    Check(slot.header[Identity]==1&&slot.header[Frame]==168&&slot.header[Generation]==1,"first capture identity");
    Check(copies==0&&barriers==2&&a.causal.authorities==1&&slot.header[Sections]==16,"actual capture recording path incomplete");
    Check(slot.header[PayloadBytes]<storage[1].size(),"synthetic storage bound");
    CompleteFrozenCapture(a);CheckState(a,Free,Reserved);Check(queryCalls==0&&a.frozen.failure==0,"unsubmitted reserved slot consumed GPU completion");
    std::cout<<"first capture recorded CPU-only: milliseconds="<<ms<<" payload="<<slot.header[PayloadBytes]<<" sections="<<slot.header[Sections]<<"; before submission=Free/Reserved; queryCalls=0\n";
    auto close=std::chrono::steady_clock::now();a.frozen.Stop();CheckState(a,Free,Reserved);
    Check(a.frozen.lastDurableIdentity==0,"writer published unsubmitted slot");
    std::cout<<"stop with unsubmitted reservation: milliseconds="<<std::chrono::duration<double,std::milli>(std::chrono::steady_clock::now()-close).count()<<"; durable=0\n";
  }
  // Writer-owned or pending slots are a fail-closed branch, never a wait/reuse loop.
  for(auto busy:{Reserved,InFlight,Writing}){
    App a;std::array<std::vector<uint8_t>,2> storage;Initialize(a,output,storage);
    // These are injected adversarial Reserve branches, not historical states.
    // None is Ready, so the worker cannot consume the injected fixture.
    a.frozen.slots[1].state=busy;
    auto begin=std::chrono::steady_clock::now();auto* slot=a.frozen.Reserve(GetTickCount64());
    Check(slot==nullptr&&a.frozen.failure==2,"busy target did not fail closed");
    Check(a.frozen.slots[1].state==busy,"busy target overwritten");
    std::cout<<"busy target state="<<unsigned(busy)<<" refused without waiting: milliseconds="<<std::chrono::duration<double,std::milli>(std::chrono::steady_clock::now()-begin).count()<<'\n';
    a.frozen.slots[1].state=Free;a.frozen.Stop();
  }
  // Wrap to a previously reserved identity while a synthetic writer owns it.
  {
    App a;std::array<std::vector<uint8_t>,2> storage;Initialize(a,output,storage);
    auto now=GetTickCount64();auto* first=a.frozen.Reserve(now);Check(first==&a.frozen.slots[1],"first reserve slot");
    first->state=Writing;auto* second=a.frozen.Reserve(now+1001);Check(second==&a.frozen.slots[0]&&second->header[Identity]==2,"second reserve slot");
    auto* third=a.frozen.Reserve(now+2002);Check(third==nullptr&&a.frozen.failure==2,"busy wraparound did not fail closed");
    Check(first->state==Writing&&first->header[Identity]==1&&second->state==Reserved&&second->header[Identity]==2,"reuse stole writer ownership");
    first->state=Free;second->state=Free;a.frozen.Stop();
  }
  // Match producer handoff and complete only after the simulated frame fence.
  {
    App a;std::array<std::vector<uint8_t>,2> storage;Initialize(a,output,storage);a.anchoredPipelineStatisticsFrameSubmitted=true;RecordFrozenCapture(a,{});
    auto& slot=a.frozen.slots[1];slot.header[HeaderFlags]|=2; // Persist unmistakable CPU synthetic provenance.
    a.frozen.Submitted(167,230);CheckState(a,Free,Reserved);
    a.frozen.Submitted(168,231);CheckState(a,Free,InFlight);a.frozen.Presented(168,0);
    auto* draw=reinterpret_cast<uint32_t*>(slot.mapped+slot.header.sections[5].offset-HeaderBytes);draw[0]=46104;draw[1]=1;
    a.causal.completed=168;SeedPackedMetadata(a);CompleteFrozenCapture(a);
    a.frozen.Stop();CheckState(a,Free,Free);
    Check(a.frozen.failure==0&&a.frozen.lastDurableIdentity==1&&a.frozen.lastDurableFrame==168,"completed synthetic capture not published");
    Check(slot.header[Submission]==231&&slot.header[CompletedSubmission]==231&&slot.header[Completed]==168,"completion identity mismatch");
  }
  // Missing query readiness fails immediately; no Vulkan wait is permitted.
  {
    App a;std::array<std::vector<uint8_t>,2> storage;Initialize(a,output,storage);a.anchoredPipelineStatisticsFrameSubmitted=true;RecordFrozenCapture(a,{});
    a.frozen.Submitted(168,231);a.causal.completed=168;queryReady=false;CompleteFrozenCapture(a);queryReady=true;
    CheckState(a,Free,InFlight);Check(a.frozen.failure==4,"unavailable query did not fail closed");a.frozen.Stop();
  }
  // Every immutable identity invalidates reuse; publication generation does not.
  for(unsigned changed=0;changed<11;++changed){
    App a;std::array<std::vector<uint8_t>,2> storage;Initialize(a,output,storage);a.anchoredPipelineStatisticsFrameSubmitted=true;
    auto h=FrozenFrameAuthority(a);auto& t=a.frozen.topologies[0];
    if(changed==1)++a.productionBillboardGeneration;
    if(changed>=2&&changed<=8)++t.authority[std::array<size_t,7>{TSource,TSourceBirth,THash,TFamily,TVertices,TTriangles,TBytes}[changed-2]];
    if(changed==9)t.state=nc::frozen::Submitted;if(changed==10)t.authority[TSubmission]=0;
    a.productionBillboardPreparedFrameIdentity=a.productionBillboardCullFrameIdentity=a.productionBillboardRasterFrameIdentity=7;
    copies=barriers=0;apiCount=0;bool rejected=false;try{RecordFrozenCapture(a,{});}catch(const std::runtime_error&){rejected=true;}
    Check(rejected==(changed>=2),"topology identity readiness was skipped or accepted");
    if(!rejected){auto& actual=a.frozen.slots[1].header;Check(copies==0&&barriers==2,"topology repeated GPU copy");
      uint64_t bytes=0;for(size_t i=0;i<apiCount;++i)if(apiCalls[i].values[0]==4)bytes+=apiCalls[i].values[7];
      Check(bytes==0&&actual[GpuCopiedBytes]==0,"actual byte count fabricated before GPU completion");
      Check(actual[IndexReadbackFrame]==100&&actual[Sections]==16&&t.pins==1,"topology GPU provenance/pin");}
    a.frozen.Stop();
  }
  for(uint32_t drawCount:{0u,3u,46104u,82944u}){
    App a;std::array<std::vector<uint8_t>,2> storage;Initialize(a,output,storage);a.anchoredPipelineStatisticsFrameSubmitted=true;
    apiCount=0;RecordFrozenCapture(a,{});auto& slot=a.frozen.slots[1];slot.header[HeaderFlags]|=2;
    auto* draw=reinterpret_cast<uint32_t*>(slot.mapped+slot.header.sections[5].offset-HeaderBytes);draw[0]=drawCount;draw[1]=1;
    auto* destination=slot.mapped+slot.header.sections[3].offset-HeaderBytes;
    std::memset(destination,0xa5,size_t(slot.header.sections[3].bytes));
    // Local stand-in for the recorded GPU copy. Then make the live mapping
    // unusable: completion must depend exclusively on its owned readback slot.
    std::memcpy(destination,a.compacted.data(),drawCount*4);
    a.productionBillboardCompactedMapped=reinterpret_cast<void*>(uintptr_t(1));
    a.frozen.Submitted(168,231);a.frozen.Presented(168,0);a.causal.completed=168;SeedPackedMetadata(a);CompleteFrozenCapture(a);a.frozen.Stop();
    Check(slot.header[CaptureTransport]==4&&slot.header[HostCompactedBytes]==0,"unexpected host copy");
    Check(std::memcmp(destination,a.compacted.data(),drawCount*4)==0,"GPU-produced compacted prefix changed");
    for(uint64_t i=uint64_t(drawCount)*4;i<slot.header.sections[3].bytes;i++)Check(destination[i]==0xa5,"unwritten compacted tail was copied");
    Check(a.frozen.topologies[0].Matches(slot.header)&&a.frozen.failure==0,"completed ownership witness absent");
  }
  for(unsigned broken=0;broken<5;++broken){
    App a;std::array<std::vector<uint8_t>,2> storage;Initialize(a,output,storage);a.anchoredPipelineStatisticsFrameSubmitted=true;
    apiCount=0;RecordFrozenCapture(a,{});auto& slot=a.frozen.slots[1];
    auto* draw=reinterpret_cast<uint32_t*>(slot.mapped+slot.header.sections[5].offset-HeaderBytes);draw[0]=3;draw[1]=1;
    a.frozen.Submitted(168,231);a.causal.completed=168;
    if(broken==0)a.productionBillboardCompactedBuffer=(VkBuffer)99;
    if(broken==1)a.productionBillboardIndexBuffer=(VkBuffer)99;
    if(broken==2)++a.productionBillboardTriangleCount;
    if(broken==3)a.productionBillboardTriangleCapacity=0;
    if(broken==4)++a.productionBillboardGeneration;
    SeedPackedMetadata(a);CompleteFrozenCapture(a);Check(a.frozen.failure!=0&&slot.state==InFlight&&!slot.indices.frame,"stale resource owner became writable/published");a.frozen.Stop();
  }
  for(unsigned broken=0;broken<7;++broken){
    App a;auto h=FrozenFrameAuthority(a);std::vector<uint8_t> bytes(size_t(h[PayloadBytes]),uint8_t(0));
    auto* draw=reinterpret_cast<uint32_t*>(bytes.data()+h.sections[5].offset-HeaderBytes);draw[0]=3;draw[1]=1;
    if(broken==0)h[Triangles]=UINT64_MAX;if(broken==1)draw[0]=UINT32_MAX;
    if(broken==2)draw[0]=2;if(broken==3)draw[1]=2;if(broken==4)draw[2]=1;
    if(broken==5)h.sections[5].offset=UINT64_MAX;if(broken==6)h.sections[5].bytes=16;
    bool rejected=false;try{CompactedPrefixBytes(h,bytes.data());}catch(const std::runtime_error&){rejected=true;}
    Check(rejected,"invalid compacted prefix accepted");
  }
  std::cout<<"PASS checks="<<checks<<"; GPU calls=0; renderer launches=0; ordinary CPU memory fixtures, not historical geometry\n";return 0;
}catch(const std::exception& e){std::cerr<<"FAIL: "<<e.what()<<'\n';return 1;}}
#endif
