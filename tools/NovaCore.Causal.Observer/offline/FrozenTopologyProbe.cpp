// Exact production lifecycle functions, local Vulkan substitutes, no GPU loader.
#define main RecordingProbeMain
#include "RecordingCallProbe.cpp"
#undef main
static void LifecycleSetup(App& a,const std::filesystem::path& output,std::array<std::vector<uint8_t>,2>& storage){
  Initialize(a,output,storage);auto& t=a.frozen.topologies[0];t.state=nc::frozen::Empty;t.mapped=nullptr;t.authority={};a.productionBillboardIndexBuffer={};
}
int main(int argc,char** argv){try{
  Need(argc==2,"output directory");std::filesystem::path output=argv[1];std::filesystem::create_directories(output);using namespace nc::frozen;
  {
    App a;std::array<std::vector<uint8_t>,2> storage;LifecycleSetup(a,output,storage);auto r=a.productionBillboardTopologyResources[0];
    AcquireFrozenTopology(a,r);auto& t=a.frozen.topologies[0];Check(t.state==Allocated&&t.authority[TSourceBirth]==r.indexBirth&&t.authority[TBytes]==r.indexCount*4ull,"activation authority");
    const auto id=t.authority[TIdentity];AcquireFrozenTopology(a,r);Check(t.authority[TIdentity]==id&&a.frozen.nextTopologyIdentity==1,"resident reuse copied twice");
    apiCount=copies=barriers=queryCalls=0;allocationCalls=0;countAllocations=true;RecordFrozenTopologies(a,(VkCommandBuffer)72);countAllocations=false;
    Check(allocationCalls==0&&copies==1&&barriers==2&&t.state==Recorded,"topology recording allocations/operations");
    for(size_t i=0;i<apiCount;++i)if(apiCalls[i].values[0]==4)Check(apiCalls[i].values[2]==uint64_t(r.index)&&apiCalls[i].values[5]==0&&apiCalls[i].values[6]==0&&apiCalls[i].values[7]==r.indexCount*4ull,"actual immutable GPU source copy");
    CompleteFrozenTopologies(a);Check(queryCalls==0&&t.state==Recorded,"unsubmitted copy became ready");
    a.frozen.Submitted(167,230);Check(t.state==Recorded,"wrong frame submission");a.frozen.Submitted(168,231);Check(t.state==nc::frozen::Submitted,"submit ownership");
    bool rejected=false;try{CompleteFrozenTopologies(a);}catch(const std::runtime_error&){rejected=true;}Check(rejected&&t.state==nc::frozen::Submitted,"future GPU completion invented");
    a.causal.completed=168;allocationCalls=0;countAllocations=true;CompleteFrozenTopologies(a);countAllocations=false;
    Check(allocationCalls==0&&t.state==Proven&&t.authority[TCompleted]==168&&t.authority[TCompletedSubmission]==231&&queryCalls==1,"existing fence/no-wait completion");
    apiCount=copies=0;RecordFrozenTopologies(a,(VkCommandBuffer)73);Check(copies==0,"completed topology recopied");
    a.productionBillboardIndexBuffer=r.index;a.causal.frame=169;auto h=FrozenFrameAuthority(a);auto& pinned=a.frozen.PinTopology(h);
    Check(&pinned==&t&&t.pins==1&&h[Sections]==16,"ready provenance pin");
    Check(!t.Retirable(0,0),"writer pin evicted");t.pins.fetch_sub(1);Check(!t.Retirable(uint64_t(r.index),0)&&!t.Retirable(0,uint64_t(r.index)),"current/incoming owner evicted");
    auto bad=r;bad.indexBirth=0;const auto oldPointer=t.mapped;rejected=false;try{AcquireFrozenTopology(a,bad);}catch(const std::runtime_error&){rejected=true;}
    Check(rejected&&t.mapped==oldPointer&&t.state==Proven,"malformed acquire retired valid pointer");
    a.productionBillboardIndexBuffer={};a.frozen.Stop();DestroyFrozenTopologies(a);Check(t.state==Empty&&t.mapped==nullptr,"shutdown pointer retirement");
  }
  // Same native handle in a new lifetime cannot reuse the old GPU proof.
  {
    App a;std::array<std::vector<uint8_t>,2> storage;LifecycleSetup(a,output,storage);auto r=a.productionBillboardTopologyResources[0];
    AcquireFrozenTopology(a,r);auto& first=a.frozen.topologies[0];first.state=Proven;const auto id=first.authority[TIdentity];++r.indexBirth;AcquireFrozenTopology(a,r);
    Check(first.authority[TSourceBirth]==r.indexBirth&&first.authority[TIdentity]>id&&first.state==Allocated,"handle ABA reused old proof");a.frozen.Stop();DestroyFrozenTopologies(a);
  }
  for(auto state:{Allocated,Recorded,nc::frozen::Submitted}){
    App a;std::array<std::vector<uint8_t>,2> storage;Initialize(a,output,storage);a.anchoredPipelineStatisticsFrameSubmitted=true;a.frozen.topologies[0].state=state;
    bool rejected=false;try{RecordFrozenCapture(a,{});}catch(const std::runtime_error&){rejected=true;}
    Check(rejected&&a.frozen.slots[1].state==Reserved&&a.frozen.slots[1].header[Identity]==1,"unready due capture silently skipped");a.frozen.Stop();
  }
  {App a;std::array<std::vector<uint8_t>,2> storage;Initialize(a,output,storage);a.frozen.topologies[0].state=Allocated;a.frozen.topologies[1].state=Allocated;apiCount=0;
    bool rejected=false;try{RecordFrozenTopologies(a,(VkCommandBuffer)72);}catch(const std::runtime_error&){rejected=true;}
    Check(rejected&&apiCount==0,"multiple activations escaped before first GPU command");a.frozen.Stop();}
  // Four entries cover one current plus two independent writer pins and one
  // new incoming owner. No pending entry or writer-owned buffer is recyclable.
  {
    App a;std::array<std::vector<uint8_t>,2> storage;LifecycleSetup(a,output,storage);auto r=a.productionBillboardTopologyResources[0];
    for(size_t i=0;i<TopologyCount;i++){r.index=(VkBuffer)(100+i);r.indexBirth=200+i;AcquireFrozenTopology(a,r);auto& t=a.frozen.topologies[i];t.state=Proven;t.pins=1;}
    Check(a.frozen.nextTopologyIdentity==4,"cache fill count");r.index=(VkBuffer)300;r.indexBirth=400;
    bool rejected=false;try{AcquireFrozenTopology(a,r);}catch(const std::runtime_error&){rejected=true;}Check(rejected,"full pinned cache reused or waited");
    a.frozen.topologies[3].pins=0;AcquireFrozenTopology(a,r);Check(a.frozen.topologies[3].authority[TSource]==300&&a.frozen.topologies[0].authority[TSource]==100,"wrong cache retirement owner");
    for(auto& t:a.frozen.topologies)t.pins=0;a.frozen.Stop();DestroyFrozenTopologies(a);
  }
  // Query failure remains fail-closed, and cancellation retirement is permitted
  // only after the test's stand-in for GPU idle and writer shutdown.
  {
    App a;std::array<std::vector<uint8_t>,2> storage;LifecycleSetup(a,output,storage);AcquireFrozenTopology(a,a.productionBillboardTopologyResources[0]);apiCount=0;
    RecordFrozenTopologies(a,(VkCommandBuffer)72);a.frozen.Submitted(168,231);a.causal.completed=168;queryReady=false;
    bool rejected=false;try{CompleteFrozenTopologies(a);}catch(const std::runtime_error&){rejected=true;}queryReady=true;Check(rejected&&a.frozen.topologies[0].state==nc::frozen::Submitted,"unready GPU query accepted");a.frozen.Stop();DestroyFrozenTopologies(a);
  }
  Check(topologyCreates==topologyDestroys,"topology cache allocation/release mismatch");
  std::cout<<"PASS topology checks="<<checks<<" allocations="<<topologyCreates<<" releases="<<topologyDestroys<<"; record/completion allocations=0; GPU execution=false\n";return 0;
}catch(const std::exception& error){countAllocations=false;std::cerr<<"FAIL "<<error.what()<<'\n';return 1;}}
