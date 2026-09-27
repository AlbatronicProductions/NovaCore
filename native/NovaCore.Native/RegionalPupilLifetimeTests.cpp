#include "NovaCoreNative.h"
#include "RegionalPupilLifetime.h"
#include <algorithm>
#include <array>
#include <iostream>
#include <stdexcept>

using Frame=NcProductionBillboardPupilFrame;
using nc::regionalphysical::RegionalPreparationVertexBudget;
using nc::regionalphysical::RegionalDemandVertexBudget;
struct Job {
  Frame frame{};std::array<uint32_t,28> mask{};
  uint64_t generation{},topology{};uint32_t phase{},cursor{};bool logged{};
  std::chrono::steady_clock::time_point started{};
};
struct Demand {Frame frames[2]{};std::array<uint32_t,28> masks[2]{};};
void Require(bool value,const char* message){if(!value)throw std::runtime_error(message);}
Frame Pupil(uint32_t identity,uint32_t vertices=712106){
  Frame f{};f.identity[0]=identity;f.identity[2]=17;f.identity[3]=1;
  f.metadata[0]=1;f.metadata[2]=vertices;return f;
}
bool Resolve(Frame& target,Job& job,Demand& demand,uint32_t published,
    bool resident=false,bool complete=true,uint32_t slot=0,uint64_t generation=18,uint64_t topology=0xBCE444AFFB2D713Bull){
  return nc::regionalphysical::ResolveDependencyTarget(target,job,demand,slot,generation,topology,
    resident,published,[&](const auto&){return complete;});
}

// CPU events acknowledge demand slices and preparation fences explicitly. No
// device is created and these acknowledgements are not GPU timing evidence.
uint32_t MovingCamera(uint32_t vertices,uint32_t pupilInterval,bool allResident=false,uint32_t ioDelay=0){
  Job job{};Demand demand{};uint32_t published=30,preparing=0,cursor=0,publications=0;
  uint32_t demandSlices=0,preparationSlices=0,completeTick=0;
  bool fencePending=false;
  for(uint32_t tick=0;tick<2000;++tick){
    if(fencePending){published=preparing;preparing=0;cursor=0;fencePending=false;++publications;}
    if(job.phase==1){job.phase=job.cursor==vertices?2u:0u;if(job.phase==2)completeTick=tick;}
    auto target=Pupil(31+tick/pupilInterval,vertices);
    bool ready=Resolve(target,job,demand,published,allResident,job.phase==2&&tick>=completeTick+ioDelay);
    if(ready&&!preparing&&target.identity[0]!=published)preparing=target.identity[0];
    if(preparing){
      Require(allResident||job.frame.identity[0]==preparing,"dependency lease changed during physical preparation");
      if(ready){cursor+=std::min(RegionalPreparationVertexBudget,vertices-cursor);++preparationSlices;fencePending=cursor==vertices;}
    }
    if(!ready&&job.phase==0&&job.frame.metadata[0]){
      Require(job.cursor<vertices,"demand range escaped topology");
      job.cursor+=std::min(RegionalDemandVertexBudget,vertices-job.cursor);job.phase=1;++demandSlices;
    }
  }
  std::cout<<"vertices="<<vertices<<" pupilInterval="<<pupilInterval<<" allResident="<<allResident
    <<" ioDelay="<<ioDelay<<" publications="<<publications<<" finalPupil="<<published
    <<" demandSlices="<<demandSlices<<" preparationSlices="<<preparationSlices<<'\n';
  Require(publications>0,"moving pupil starves completed dependency-to-preparation handoff");
  Require(published>30,"physical owner never advanced");
  return publications;
}
int main(){try{
  // Finest topology: 22 demand slices plus 11 preparation slices. The live
  // source logged repeatedly completed empty masks with no pupil publication.
  MovingCamera(712106,1);
  for(auto vertices:{32768u,32769u,696410u,712106u})for(auto interval:{1u,3u,22u,33u,100u})MovingCamera(vertices,interval);
  MovingCamera(712106,1,false,7);MovingCamera(712106,1,true);
  Job job{};Demand demand{};auto target=Pupil(31);
  Require(!Resolve(target,job,demand,30),"fresh request skipped dependency scan");
  job.phase=2;job.cursor=712106;target=Pupil(32);
  Require(!Resolve(target,job,demand,30,false,false)&&target.identity[0]==31,"unresolved mask lost its pupil");
  target=Pupil(32);Require(Resolve(target,job,demand,30)&&target.identity[0]==31,"completed pupil was replaced before handoff");
  target=Pupil(32);Require(!Resolve(target,job,demand,31)&&target.identity[0]==32&&job.cursor==0,"published pupil did not release dependency lease");
  job.phase=2;target=Pupil(33);
  Require(!Resolve(target,job,demand,31,false,true,0,19)&&job.generation==19&&target.identity[0]==33,"generation change retained stale dependencies");
  job.phase=2;target=Pupil(34);
  Require(!Resolve(target,job,demand,31,false,true,0,19,123)&&job.topology==123,"topology change retained stale dependencies");
  job={};target=Pupil(40);Resolve(target,job,demand,30,false,true,1);
  job.phase=2;target=Pupil(41);
  Require(Resolve(target,job,demand,30,false,true,1)&&target.identity[0]==40,"incoming completed footprint lost its frozen target");
  std::cout<<"Regional pupil dependency handoff: PASS; CPU-only; no Vulkan entry point\n";
  return 0;
}catch(const std::exception& e){std::cerr<<e.what()<<'\n';return 1;}}
