#define main RecordingProbeMain
#include "RecordingCallProbe.cpp"
#undef main
int main(int argc,char** argv){try{
 Need(argc==2,"output");std::filesystem::path out=argv[1];std::filesystem::create_directories(out);
 using namespace nc::frozen;
 for(auto busy:{Free,Reserved,InFlight,Ready,Writing}){
  App a;std::array<std::vector<uint8_t>,2> storage;Initialize(a,out,storage);
  // Prevent worker consuming the adversarial Ready fixture during this check.
  a.frozen.Stop();a.frozen.slots[0].state=busy;a.frozen.SuspendAdmissions();
  Check(a.frozen.Drained()==(busy==Free),"drain ignored capture owner");
  Check(a.frozen.Reserve(GetTickCount64()+10000)==nullptr,"disabled reserve admitted work");a.frozen.slots[0].state=Free;
 }
 for(auto state:{Empty,Allocated,Recorded,nc::frozen::Submitted,Proven}){
  Writer w;w.topologies[0].state=state;w.SuspendAdmissions();Check(w.Drained()==(state!=Recorded&&state!=nc::frozen::Submitted),"drain ignored GPU topology work");
  w.topologies[0].pins=1;Check(!w.Drained(),"drain ignored writer topology pin");w.topologies[0].pins=0;
 }
 {App a;std::array<std::vector<uint8_t>,2> storage;Initialize(a,out,storage);a.anchoredPipelineStatisticsFrameSubmitted=true;apiCount=0;RecordFrozenCapture(a,{});
  auto& s=a.frozen.slots[1];auto* draw=reinterpret_cast<uint32_t*>(s.mapped+s.header.sections[5].offset-HeaderBytes);draw[0]=3;draw[1]=1;
  a.frozen.Submitted(168,231);a.frozen.Presented(168,0);a.startup.request=900;
  allocationCalls=0;countAllocations=true;Check(!FrozenAdmissionsEnabled(a),"request not accepted");PollFrozenDrain(a);countAllocations=false;
  Check(allocationCalls==0&&!a.frozenDrainReported&&s.state==InFlight&&a.startup.ack==900,"disable stole in-flight ownership");
  unsigned oldCopies=copies;apiCount=0;auto creates=topologyCreates;RecordFrozenTopologies(a,{});AcquireFrozenTopology(a,a.productionBillboardTopologyResources[0]);RecordFrozenCapture(a,{});
  Check(copies==oldCopies&&topologyCreates==creates&&a.frozen.LastAdmittedIdentity()==1,"new heavy work after disable");
  a.causal.completed=168;SeedPackedMetadata(a);CompleteFrozenCapture(a);
  auto begin=GetTickCount64();while(!a.frozen.Drained()&&GetTickCount64()-begin<2000)Sleep(1);PollFrozenDrain(a);
  Check(a.frozenDrainReported&&a.frozen.lastDurableIdentity==1&&s.state==Free&&a.frozen.Active(),"pending capture not drained without joining writer");
  Check(!FrozenAdmissionsEnabled(a),"request re-enabled");a.startup.request=901;bool rejected=false;try{FrozenAdmissionsEnabled(a);}catch(...){rejected=true;}Check(rejected,"changed request accepted");a.frozen.Stop();
 }
 {Writer w;w.failure=1;w.SuspendAdmissions();Check(!w.Drained(),"failure hidden by drain");}
 std::cout<<"PASS control checks="<<checks<<"; no GPU execution; no new admission/ownership wait\n";return 0;
}catch(const std::exception& e){std::cerr<<"FAIL "<<e.what()<<'\n';return 1;}}
