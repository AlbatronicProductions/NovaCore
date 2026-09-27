// No Vulkan loader or renderer. Executes the extracted production recording tail
// and capture functions against ordinary CPU memory and local API substitutes.
#include <new>
#include <cstdlib>
static thread_local bool countAllocations=false;
static thread_local size_t allocationCalls=0;
void* operator new(size_t n){if(countAllocations)++allocationCalls;if(auto p=std::malloc(n?n:1))return p;throw std::bad_alloc();}
void* operator new[](size_t n){return ::operator new(n);}
void operator delete(void* p)noexcept{std::free(p);}
void operator delete[](void* p)noexcept{std::free(p);}
void operator delete(void* p,size_t)noexcept{std::free(p);}
void operator delete[](void* p,size_t)noexcept{std::free(p);}
void* operator new(size_t n,std::align_val_t align){if(countAllocations)++allocationCalls;if(auto p=_aligned_malloc(n?n:1,size_t(align)))return p;throw std::bad_alloc();}
void operator delete(void* p,std::align_val_t)noexcept{_aligned_free(p);}
void operator delete(void* p,size_t,std::align_val_t)noexcept{_aligned_free(p);}
#define NC_RECORDING_CALL_PROBE
#include "FrozenOwnershipProbe.cpp"
#define vkCmdDraw CausalDraw
#include "FrozenBaseline.generated.inl"
#undef vkCmdDraw
#include <fstream>

struct Mapping {
  HANDLE handle{};uint64_t* words{};
  Mapping(){wchar_t name[128];swprintf_s(name,L"Local\\NovaCore.RecordingProbe.%lu",GetCurrentProcessId());
    handle=CreateFileMappingW(INVALID_HANDLE_VALUE,nullptr,PAGE_READWRITE,0,DWORD(nc::causal::HeaderBytes+nc::causal::Capacity*nc::causal::RecordBytes),name);
    words=static_cast<uint64_t*>(MapViewOfFile(handle,FILE_MAP_ALL_ACCESS,0,0,0));Need(words!=nullptr,"mapping");
    SetEnvironmentVariableW(L"NOVACORE_CAUSAL_MAPPING",name);}
  void Reset(){std::memset(words,0,nc::causal::HeaderBytes+nc::causal::Capacity*nc::causal::RecordBytes);
    words[0]=nc::causal::Magic;words[1]=2;words[2]=512;words[3]=nc::causal::Capacity;words[10]=1;
    LARGE_INTEGER now,f;QueryPerformanceCounter(&now);QueryPerformanceFrequency(&f);words[5]=f.QuadPart;words[14]=now.QuadPart;}
  const nc::causal::Entry* Entries()const{return reinterpret_cast<const nc::causal::Entry*>(reinterpret_cast<const uint8_t*>(words)+128);}
  size_t TraceCount()const{size_t n=0;for(size_t i=0;i<words[6];i++)n+=Entries()[i].phase==nc::causal::RecordingCall;return n;}
  void Save(const std::filesystem::path& path){words[7]=words[8]=words[6];std::ofstream file(path,std::ios::binary);file.write(reinterpret_cast<const char*>(words),128+words[6]*512);Need(bool(file),"journal output");}
  ~Mapping(){SetEnvironmentVariableW(L"NOVACORE_CAUSAL_MAPPING",nullptr);if(words)UnmapViewOfFile(words);if(handle)CloseHandle(handle);}
};
struct Result {Header header{};std::array<ApiCall,64> calls{};size_t count{},markers{},allocations{};double ms{};};
static void RetainedMetadata(App& a){
  // The payload allocation is exactly catalog.count * R16Bytes in production.
  // Five resident records were logged; their exact bit positions were not saved.
  a.catalog.count=uint32_t(a.regionalPayloadBytes/nc::localterrain::R16Bytes);
  a.catalog.physicalGeneration=4;a.catalog.terrainVersion=5;
  for(size_t i=0;i<a.catalog.entries.size();i++)a.catalog.entries[i].resident=i<5;
  a.productionBillboardPhysicalBuffer=(VkBuffer)565148976677378ull;a.productionBillboardIndexBuffer=(VkBuffer)562949953421824ull;
  a.productionBillboardVisibilityBuffer=(VkBuffer)567347999932932ull;a.productionBillboardCompactedBuffer=(VkBuffer)569547023188486ull;
  a.productionBillboardCounterBuffer=(VkBuffer)573945069699594ull;a.productionBillboardIndirectBuffer=(VkBuffer)571746046444040ull;
  a.frozenBuffers={(VkBuffer)385928581349727ull,(VkBuffer)388127604605281ull};a.fence=(VkFence)380431023210842ull;
  a.beginLabel=BeginLabel;a.endLabel=EndLabel;
  a.productionBillboardTopologyResources[0].index=a.productionBillboardIndexBuffer;a.frozen.topologies[0].authority[nc::frozen::TSource]=uint64_t(a.productionBillboardIndexBuffer);
}
static Result Run(Mapping& mapping,const std::filesystem::path& output,bool original,bool enabled,bool save,bool eligible=true,bool capture=true){
  mapping.Reset();App a;std::array<std::vector<uint8_t>,2> storage;Initialize(a,output,storage);RetainedMetadata(a);
  gApp=&a;a.causal.draws=12; // Production CausalDraw increments the final draw to 13.
  if(enabled)a.causal.Open();a.anchoredPipelineStatisticsFrameSubmitted=eligible;if(!capture)a.frozen.Stop();apiCount=0;
  a.causal.Emit(Phase::Record,Kind::Begin);
  allocationCalls=0;countAllocations=true;auto start=std::chrono::steady_clock::now();
  if(original)baseline::ProbeRecordTail(a,(VkCommandBuffer)2603718174112ull);else ProbeRecordTail(a,(VkCommandBuffer)2603718174112ull);
  auto end=std::chrono::steady_clock::now();countAllocations=false;
  a.causal.Emit(Phase::Record,Kind::End);
  Result result{a.frozen.slots[1].header,apiCalls,apiCount,mapping.TraceCount(),allocationCalls,std::chrono::duration<double,std::milli>(end-start).count()};
  result.header[nc::frozen::RecordQpc]=0;
  CheckState(a,nc::frozen::Free,eligible&&capture?nc::frozen::Reserved:nc::frozen::Free);Check(a.frozen.failure==0,"recording changed ownership failure");
  Check(allocationCalls==0,"allocation in recording path");
  if(!original&&enabled&&eligible&&capture){
    Check(result.markers==149,"first capture marker count");
    Check(result.header[nc::frozen::Width]==960&&result.header[nc::frozen::Height]==582,"production-scope viewport header keys");
    const auto* records=mapping.Entries();
    for(size_t i=0;i<mapping.words[6];i++)Check(records[i].commit==records[i].serial&&records[i].data[52]==nc::causal::Recorder::Checksum(records[i]),"marker checksum/commit");
    Check(mapping.words[11]==0&&mapping.words[12]==0,"recorder loss");
    if(save)mapping.Save(output/"recorded-commands.bin");
    // Later frames may still emit the pre-existing authority events, but no new
    // detailed markers. The first reservation remains unsubmitted throughout.
    a.causal.frame=169;apiCount=0;ProbeRecordTail(a,(VkCommandBuffer)2603718174112ull);
    Check(mapping.TraceCount()==result.markers,"trace repeated on later frame");
  }else Check(result.markers==0,"ineligible/inactive trace emitted markers");
  a.frozen.Stop();gApp=nullptr;return result;
}
static void Equivalent(const Result& a,const Result& b){Check(a.count==b.count&&a.calls==b.calls,"instrumentation changed API sequence/arguments");Check(std::memcmp(&a.header,&b.header,sizeof(Header))==0,"instrumentation changed capture authority");}
int main(int argc,char** argv){try{
  Need(argc==2,"output directory");std::filesystem::path output=argv[1];std::filesystem::create_directories(output);Mapping mapping;
  auto old=Run(mapping,output,true,true,false);auto marked=Run(mapping,output,false,true,true);Equivalent(old,marked);
  auto oldOff=Run(mapping,output,true,false,false);auto markedOff=Run(mapping,output,false,false,false);Equivalent(oldOff,markedOff);Check(markedOff.markers==0,"disabled recorder emitted markers");
  Equivalent(Run(mapping,output,true,true,false,false),Run(mapping,output,false,true,false,false));
  Equivalent(Run(mapping,output,true,true,false,true,false),Run(mapping,output,false,true,false,true,false));
  // The extracted implementation retains the production namespace's Width and
  // Height constants. Vary actual extents to detect shadowed keys or stale data.
  {App a;std::array<std::vector<uint8_t>,2> storage;Initialize(a,output,storage);RetainedMetadata(a);
    for(auto extent:{VkExtent2D{960,582},VkExtent2D{960,540},VkExtent2D{1,1},VkExtent2D{3440,1440}}){
      a.extent=extent;auto header=FrozenFrameAuthority(a);
      Check(header[nc::frozen::Width]==extent.width&&header[nc::frozen::Height]==extent.height,"viewport keys shadowed in production scope");
      Check(header[nc::frozen::PhysicalGeneration]==4&&header[nc::frozen::TerrainVersion]==5,"adjacent authority fields corrupted");
      Check(header[nc::frozen::Generation]==1&&header[nc::frozen::TopologyHash]==0x118D7D350CB66136ull,"generation authority changed");
    }a.frozen.Stop();}
  // An exception is not a successfully returned call. This stays in the CPU
  // fixture and never inserts an exception or recovery branch into production.
  {mapping.Reset();Recorder sink;sink.Open();nc::causal::RecordingCallTrace trace;trace.Start(sink,{});
    bool caught=false;try{trace.Call(nc::causal::RecordingOp::InputCamera,[]{throw 7;});}catch(int){caught=true;}
    Check(caught&&mapping.TraceCount()==2,"exception manufactured a return marker");}
  {mapping.Reset();Recorder sink;sink.Open();nc::causal::RecordingCallTrace trace;trace.Start(sink,{});
    trace.Enter(nc::causal::RecordingOp::AuthorityScalars);bool caught=false;
    try{trace.Scalar(nc::causal::AuthorityScalar::Frame,nc::frozen::Frame,[]{throw 7;});}catch(int){caught=true;}
    Check(caught&&mapping.TraceCount()==3,"scalar exception manufactured a return marker");}
  {mapping.Reset();Recorder sink;sink.Open();nc::causal::RecordingCallTrace trace;trace.Start(sink,{});
    unsigned calls=0;for(unsigned i=0;i<200;i++)trace.Call(nc::causal::RecordingOp::FinalDraw,[&]{++calls;});
    Check(mapping.TraceCount()==161&&calls==200,"marker cap changed production calls or exceeded bound");
    Check(mapping.Entries()[mapping.words[6]-1].result==-7002,"marker overflow did not fail closed");}
  // The post-fence transfer of the exact GPU-produced prefix is also a zero-
  // allocation renderer operation. Worker allocations are on its own thread.
  {App a;std::array<std::vector<uint8_t>,2> storage;Initialize(a,output,storage);a.anchoredPipelineStatisticsFrameSubmitted=true;apiCount=0;
    RecordFrozenCapture(a,{});auto& slot=a.frozen.slots[1];auto* draw=reinterpret_cast<uint32_t*>(slot.mapped+slot.header.sections[5].offset-nc::frozen::HeaderBytes);
    draw[0]=46104;draw[1]=1;a.frozen.Submitted(168,231);a.frozen.Presented(168,0);a.causal.completed=168;
    allocationCalls=0;countAllocations=true;SeedPackedMetadata(a);CompleteFrozenCapture(a);countAllocations=false;
    Check(allocationCalls==0,"allocation in post-fence exact-prefix transfer");a.frozen.Stop();
    Check(a.frozen.failure==0&&slot.header[nc::frozen::HostCompactedBytes]==0,"post-fence GPU-only publication");}
  // Repeat paired runs, measuring just the CPU recording path, not setup/worker
  // shutdown. These are CPU/mock costs, never GPU or live readback measurements.
  std::ofstream timings(output/"cpu-cost.csv");timings<<"sample,baseline_ms,instrumented_ms,delta_ms\n";
  for(unsigned i=0;i<65;i++){auto before=Run(mapping,output,true,true,false);auto after=Run(mapping,output,false,true,false);Equivalent(before,after);timings<<i<<','<<before.ms<<','<<after.ms<<','<<after.ms-before.ms<<'\n';}
  std::cout<<"PASS checks="<<checks<<"; corrected baseline/marked API and authority parity; marker count=149; allocation calls=0; GPU calls=0; 65 paired CPU-only samples\n";return 0;
}catch(const std::exception& error){countAllocations=false;std::cerr<<"FAIL "<<error.what()<<'\n';return 1;}}
