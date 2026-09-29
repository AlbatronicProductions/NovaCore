#include "RegionalPhysicalResidency.h"
#include <chrono>
#include <iostream>
#include <memory>

int main(int argc,char** argv){try{
  if(argc!=2)throw std::runtime_error("pass the production regional pack path");
  nc::localterrain::Pack pack;std::string error;if(!pack.Open(argv[1],error))throw std::runtime_error(error);
  auto require=[](bool value,const char* message){if(!value)throw std::runtime_error(message);};
  auto rejects=[&](auto action,const char* message){bool failed=false;try{action();}catch(const std::runtime_error&){failed=true;}require(failed,message);};
  const auto bytes=uint64_t(pack.RecordCount())*nc::localterrain::R16Bytes;
  // Declared before the cache below, and explicitly joined in the active-stop
  // check. The production owner must likewise outlive every worker write.
  std::vector<uint8_t> storage(size_t(bytes)+64,0xA5);
  nc::regionalphysical::Residency residency(pack);
  auto waitFor=[&](auto& cache,uint32_t count){
    const auto deadline=std::chrono::steady_clock::now()+std::chrono::seconds(30);
    while(cache.CompletedCount()<count){if(std::chrono::steady_clock::now()>deadline)throw std::runtime_error("async record timeout");std::this_thread::yield();}
  };
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
  require(residency.CompletedCount()==0,"worker must wait for an owned payload allocation");
  rejects([&]{residency.BindPayload(nullptr,bytes);},"null binding accepted");
  rejects([&]{residency.BindPayload(storage.data()+32,bytes-1);},"short binding accepted");
  rejects([&]{residency.BindPayload(storage.data()+32,bytes+1);},"oversized binding accepted");
  residency.BindPayload(storage.data()+32,bytes);
  rejects([&]{residency.BindPayload(storage.data()+32,bytes);},"worker target rebound");
  waitFor(residency,1);
  require(!residency.Complete(demand),"CPU load completion alone must not satisfy GPU publication");
  require(std::all_of(storage.begin()+32+nc::localterrain::R16Bytes,storage.end(),[](uint8_t b){return b==0xA5;}),"unrequested ranges were written");
  require(residency.PublishCompleted()==1,"first completion not published exactly once");
  require(residency.Complete(demand),"uploaded residual must satisfy dependency");
  require(residency.PublishCompleted()==0,"duplicate publication");
  residency.Require(demand);require(residency.requests==1&&residency.hits==1,"matching physical identity should reuse record");
  require(!residency.AllContributingResident(),"one loaded record cannot certify entire geographic dataset");
  require(residency.contributingRecords==670,"production contributing partition changed; remeasure capacity");
  std::array<uint32_t,nc::regionalphysical::MaskWords> all{};
  for(uint32_t i=0;i<pack.RecordCount();++i)all[i/32]|=1u<<(i%32);
  const auto started=std::chrono::steady_clock::now();
  residency.Require(all);residency.Require(all);
  // Deliberately never service publication while the producer completes the
  // entire pack. The previous eight-payload queue could not pass this check.
  waitFor(residency,pack.RecordCount());
  const double producerMs=std::chrono::duration<double,std::milli>(std::chrono::steady_clock::now()-started).count();
  require(!residency.Complete(all)&&!residency.AllContributingResident(),"unpublished completions bypassed readiness");
  require(residency.loaded==1&&residency.requests==pack.RecordCount(),"worker changed render-owned residency or duplicated demand");
  require(residency.PublishCompleted()==pack.RecordCount()-1,"bounded completion drain lost records");
  require(residency.Complete(all)&&residency.AllContributingResident(),"complete published cache not ready");
  uint64_t stored=0;
  for(uint32_t i=0;i<pack.RecordCount();++i){nc::localterrain::Payload expected;
    require(pack.Read(pack.Records()[i].id,expected,error)&&expected.digestValid,error.c_str());stored+=expected.storedBytes;
    require(std::memcmp(storage.data()+32+size_t(i)*nc::localterrain::R16Bytes,expected.elevationBc4.data(),nc::localterrain::R16Bytes)==0,"worker output differs from canonical physical record");
  }
  require(std::all_of(storage.begin(),storage.begin()+32,[](uint8_t b){return b==0xA5;})&&
    std::all_of(storage.end()-32,storage.end(),[](uint8_t b){return b==0xA5;}),"payload guard overwritten");
  require(residency.readBytes==stored&&residency.uploadBytes==bytes,"physical byte accounting changed");
  residency.Require(all);require(residency.PublishCompleted()==0&&residency.requests==pack.RecordCount(),"retained records were reacquired");
  // Corrupt catalog identity in a separate pack: no failed record may touch the
  // destination, become resident, or satisfy physical readiness.
  altered[0].id.terrainVersion--;altered[0].digest[0]^=1;
  {std::vector<uint8_t> failedStorage(size_t(bytes),0xA5);nc::regionalphysical::Residency failed(changed);
    failed.BindPayload(failedStorage.data(),bytes);failed.Require(demand);waitFor(failed,1);
    rejects([&]{failed.PublishCompleted();},"corrupt record published");
    require(!failed.Complete(demand)&&failed.loaded==0,"corrupt record satisfied readiness");
    require(std::all_of(failedStorage.begin(),failedStorage.end(),[](uint8_t b){return b==0xA5;}),"corrupt record wrote payload");
  }
  {std::vector<uint8_t> retiringStorage(size_t(bytes),0xA5);
    auto retiring=std::make_unique<nc::regionalphysical::Residency>(pack);
    retiring->BindPayload(retiringStorage.data(),bytes);retiring->Require(all);waitFor(*retiring,1);
    retiring.reset(); // joins before poisoning/freeing the borrowed storage
    std::fill(retiringStorage.begin(),retiringStorage.end(),0x5A);
    std::this_thread::sleep_for(std::chrono::milliseconds(10));
    require(std::all_of(retiringStorage.begin(),retiringStorage.end(),[](uint8_t b){return b==0x5A;}),"worker wrote after retirement");
  }
  std::cout<<"Regional physical identity/readiness/cache: PASS; exactRecords="<<pack.RecordCount()
    <<"; producerWithoutPublicationMs="<<producerMs<<"; payloadBytes="<<bytes<<"; workerCopyMs="<<residency.workerCopyMs
    <<"; fixedReceipts="<<nc::regionalphysical::MaximumRecords<<"; no anchored owner required\n";
  return 0;
}catch(const std::exception& e){std::cerr<<e.what()<<'\n';return 1;}}
