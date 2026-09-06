#include "RegionalPhysicalResidency.h"
#include <chrono>
#include <iostream>

int main(int argc,char** argv){try{
  if(argc!=2)throw std::runtime_error("pass the production regional pack path");
  nc::localterrain::Pack pack;std::string error;if(!pack.Open(argv[1],error))throw std::runtime_error(error);
  nc::regionalphysical::Residency residency(pack);
  auto require=[](bool value,const char* message){if(!value)throw std::runtime_error(message);};
  require(residency.Matches(pack,4),"original physical identity must match");
  require(!residency.Matches(pack,3),"physical generation must invalidate reuse");
  auto changed=pack;
  // Fault injection into a separate in-memory catalog; source asset is read-only.
  auto& altered=const_cast<std::vector<nc::localterrain::Record>&>(changed.Records());
  altered[0].digest[0]^=1;
  require(!residency.Matches(changed,4),"same-body changed regional data must invalidate reuse");
  altered[0].digest[0]^=1;altered[0].id.terrainVersion++;
  require(!residency.Matches(changed,4),"terrain identity must invalidate reuse");
  std::array<uint32_t,nc::regionalphysical::MaskWords> demand{};
  require(residency.Complete(demand)&&residency.requests==0,"empty geographic dependency set must not request Florida");
  demand[0]=1;require(!residency.Complete(demand),"incoming readiness must reject unresolved physical data");
  residency.Require(demand);residency.Require(demand);
  require(residency.requests==1&&residency.misses==1,"deduplicate pending physical I/O");
  nc::regionalphysical::Completion completion;
  const auto timeout=std::chrono::steady_clock::now()+std::chrono::seconds(30);
  while(!residency.Pop(completion)){if(std::chrono::steady_clock::now()>timeout)throw std::runtime_error("async record timeout");std::this_thread::yield();}
  require(completion.error.empty(),completion.error.c_str());
  require(!residency.Complete(demand),"CPU load completion alone must not satisfy GPU publication");
  require(completion.payload.albedoBc7.empty()&&completion.payload.normalBc5.empty()&&completion.payload.controlR8.empty(),"material data retained by physical cache");
  residency.Published(completion);require(residency.Complete(demand),"uploaded residual must satisfy dependency");
  residency.Require(demand);require(residency.requests==1&&residency.hits==1,"matching physical identity should reuse record");
  require(!residency.AllContributingResident(),"one loaded record cannot certify entire geographic dataset");
  require(residency.contributingRecords==670,"production contributing partition changed; remeasure capacity");
  std::cout<<"Regional physical identity/readiness/cache: PASS; asynchronous source; no anchored owner required\n";
  return 0;
}catch(const std::exception& e){std::cerr<<e.what()<<'\n';return 1;}}
