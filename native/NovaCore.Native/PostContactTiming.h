#pragma once
#include <array>
#include <cstdint>
#include <vector>
#include <string>
#include <fstream>
#include <iomanip>
#include <cstdlib>
#include <stdexcept>
#include <limits>
#include <chrono>

// Opt-in, bounded qualification copy of timings already computed by the
// renderer, with opt-in CPU upload-phase clocks. No Vulkan calls, extra GPU
// timestamps, worker or frame-path file I/O.
struct PostContactTiming {
  struct Row { uint64_t loop{},terrain{},gpuFrame{}; double frame{},cadence{}; std::array<double,8> cpu{}; std::array<double,11> gpu{}; uint64_t gpuSample{},submit{}; bool submitted{}; std::array<double,5> upload{}; std::array<double,9> regional{}; std::array<double,6> allocation{}; };
  struct Lap {
    double* values{};std::chrono::steady_clock::time_point previous{};
    explicit Lap(double* output):values(output){if(values)previous=std::chrono::steady_clock::now();}
    void Part(size_t i){if(!values)return;const auto now=std::chrono::steady_clock::now();values[i]=std::chrono::duration<double,std::milli>(now-previous).count();previous=now;}
  };
  std::string path;
  std::vector<Row> rows;
  size_t count{};
  uint64_t submittedTerrain{},completedTerrain{};
  std::array<double,8> cpu{};
  std::array<double,5> upload{};
  std::array<double,9> regional{};
  std::array<double,6> allocation{};
  std::chrono::steady_clock::time_point uploadLap{};
  void BeginUpload(std::chrono::steady_clock::time_point start){if(!rows.empty())uploadLap=start;}
  void UploadPart(size_t part){if(rows.empty())return;const auto now=std::chrono::steady_clock::now();upload[part]=std::chrono::duration<double,std::milli>(now-uploadLap).count();uploadLap=now;}
  PostContactTiming(){if(const auto*p=std::getenv("NOVACORE_POST_CONTACT_TIMINGS")){path=p;rows.resize(16384);}}
  void Begin(){if(!rows.empty()){cpu.fill(std::numeric_limits<double>::quiet_NaN());regional.fill(0);allocation.fill(0);}}
  void Submitted(uint64_t frame){submittedTerrain=frame;}
  void Completed(){completedTerrain=submittedTerrain;}
  void Record(uint64_t loop,uint64_t terrain,uint64_t gpuFrame,double frame,double cadence,const std::array<double,11>&gpu,uint64_t gpuSample=0,uint64_t submit=0,bool submitted=false){
    if(rows.empty())return;
    if(count==rows.size())throw std::runtime_error("Post-contact qualification timing capacity exhausted");
    rows[count++]={loop,terrain,gpuFrame,frame,cadence,cpu,gpu,gpuSample,submit,submitted,upload,regional,allocation};
  }
  void Write()const{
    if(rows.empty())return;
    std::ofstream f(path);if(!f)throw std::runtime_error("Post-contact qualification timing output refused");
    f<<"loop,terrain,gpuFrame,frameMs,cadenceMs,updateMs,fenceMs,inspectionMs,callbackMs,uploadMs,recordMs,submitMs,presentMs,gpuTotal,gpuUnused,gpuDetailed,gpuBackground,gpuPreSurface,gpuScene,gpuToneMap,gpuCull,gpuMaterials,gpuCandidate,gpuGlobal,gpuSample,submitSerial,submitted,createBillboardMs,validateMs,uploadCopyMs,prepareUploadsMs,regionalUpdateMs,regionalIdentityMs,regionalDestroyMs,regionalPayloadMs,regionalDescriptorMs,regionalBindMs,regionalPublishMs,regionalCatalogMs,regionalResolveMs,regionalPrepareMs,payloadCreateMs,payloadRequirementsMs,payloadAllocateMs,payloadBindMemoryMs,payloadMapMs,payloadClearMs\n"<<std::setprecision(17);
    for(size_t i=0;i<count;i++){const auto&r=rows[i];f<<r.loop<<','<<r.terrain<<','<<r.gpuFrame<<','<<r.frame<<','<<r.cadence;for(auto v:r.cpu)f<<','<<v;for(auto v:r.gpu)f<<','<<v;f<<','<<r.gpuSample<<','<<r.submit<<','<<r.submitted;for(auto v:r.upload)f<<','<<v;for(auto v:r.regional)f<<','<<v;for(auto v:r.allocation)f<<','<<v;f<<'\n';}
    if(!f)throw std::runtime_error("Post-contact qualification timing output incomplete");
  }
};
