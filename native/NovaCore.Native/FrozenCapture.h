#pragma once
#include <windows.h>
#include <bcrypt.h>
#include <array>
#include <atomic>
#include <algorithm>
#include <cstdint>
#include <cstring>
#include <filesystem>
#include <stdexcept>
#include <thread>

namespace nc::frozen {
constexpr uint64_t Magic=0x314e455a4f52464eull;
constexpr size_t HeaderBytes=4096, SlotBytes=88ull*1024*1024, SlotCount=2;
constexpr uint64_t CadenceMs=1000;
constexpr size_t TopologyCount=4;
static_assert(std::atomic<uint32_t>::is_always_lock_free);
enum Word:size_t { WMagic=0,Version,HeaderSize,Identity,Frame,Submission,Completed,RecordQpc,CompleteQpc,Present,
  Swap,Generation,IncomingGeneration,TopologyHash,TopologyFamily,PreparedPupil,CullPupil,RasterPupil,
  Vertices,Triangles,Draws,Dispatches,Groups,Width,Height,PhysicalGeneration,TerrainVersion,
  IncomingVertices,IncomingTriangles,IncomingTopologyHash,IncomingPreparedPupil,Flags,
  PhysicalHandle,IndexHandle,VisibilityHandle,CompactedHandle,CounterHandle,IndirectHandle,
  VertexCapacity,TriangleCapacity,Clipping,Fragments,Tcs,Tes,CompletedSubmission,Reserved45,
  ResidencyBytes,WorkingBytes,PayloadBytes,Sections,SurfaceMode,HeaderFlags,Reserved52,Reserved53,
  Reserved54,Reserved55,Digest0,Digest1,Digest2,Digest3,Reserved60,Reserved61,Reserved62,Commit };
constexpr Word CaptureTransport=Reserved45,GpuCopiedBytes=Reserved52,HostCompactedBytes=Reserved53,IndexReadbackFrame=Reserved54;
enum SectionType:uint64_t { Physical=1,Indices,Visibility,Compacted,Counters,Indirect,
  Camera=100,GpuInput,Presentation,Pupils,Preparation,PublishedPupil,Residency,CatalogResidency,DemandInputs,TopologyProvenance };
struct Section {uint64_t type,offset,bytes,stride;};
struct alignas(64) Header {
  std::array<uint64_t,64> words{};
  std::array<Section,16> sections{};
  std::array<uint8_t,3072> inputs{};
  uint64_t& operator[](size_t index){return words[index];}
  uint64_t operator[](size_t index)const{return words[index];}
  void Begin(){*this={};words[WMagic]=Magic;words[Version]=1;words[HeaderSize]=HeaderBytes;words[Present]=uint64_t(INT64_MIN);}
  void Bulk(uint64_t type,uint64_t bytes,uint64_t stride){
    auto& count=words[Sections];if(count>=sections.size())throw std::runtime_error("frozen section capacity");
    auto& size=words[PayloadBytes];size=(size+255)&~255ull;
    if(bytes>SlotBytes||size>SlotBytes-bytes)throw std::runtime_error("frozen geometry exceeds capture capacity; no quality clamp permitted");
    sections[count++]={type,HeaderBytes+size,bytes,stride};size+=bytes;
  }
  void Input(uint64_t type,const void* data,size_t bytes){
    size_t offset=1024;for(size_t i=0;i<words[Sections];++i)if(sections[i].offset<HeaderBytes)offset=std::max(offset,size_t(sections[i].offset+sections[i].bytes));
    offset=(offset+31)&~size_t(31);
    if(words[Sections]>=sections.size()||bytes>HeaderBytes-offset)throw std::runtime_error("frozen input capacity");
    sections[words[Sections]++]={type,offset,bytes,1};std::memcpy(reinterpret_cast<uint8_t*>(this)+offset,data,bytes);
  }
};
static_assert(sizeof(Header)==HeaderBytes&&offsetof(Header,sections)==512&&offsetof(Header,inputs)==1024);
enum State:uint32_t { Free,Reserved,InFlight,Ready,Writing };
enum TopologyState:uint32_t { Empty,Allocated,Recorded,Submitted,Proven };
enum TopologyWord:size_t { TIdentity,TSource,TSourceBirth,THash,TFamily,TVertices,TTriangles,TSection,TOffset,TBytes,TStride,
  TDestination,TDestinationBirth,TFrame,TCommand,TSubmission,TCompleted,TCompletedSubmission,TRecordSerial,TCompleteSerial,TGpuCost,TCpuCost,TWordCount };
struct TopologyWitness {
  std::array<uint64_t,TWordCount> authority{};
  uint8_t* mapped{};std::atomic<uint32_t> pins{};TopologyState state{Empty};
  bool Matches(const Header& h)const{return state==Proven&&authority[TSource]==h[IndexHandle]&&authority[TSourceBirth]==h[Reserved55]&&authority[THash]==h[TopologyHash]&&authority[TFamily]==h[TopologyFamily]&&authority[TVertices]==h[Vertices]&&authority[TTriangles]==h[Triangles]&&authority[TBytes]==h.sections[1].bytes;}
  bool Retirable(uint64_t current,uint64_t incoming)const{return !pins.load(std::memory_order_acquire)&&(state==Empty||(state==Proven&&authority[TSource]!=current&&authority[TSource]!=incoming));}
};
// The immutable index bytes stay in this independently owned readback slot.
// A changed owner, topology, count or layout requires a fresh GPU copy.
struct IndexWitness {
  uint64_t generation{},handle{},hash{},family{},vertices{},triangles{},offset{},bytes{},frame{};
  bool Matches(const Header& h)const{return frame&&generation==h[Generation]&&handle==h[IndexHandle]&&hash==h[TopologyHash]&&family==h[TopologyFamily]&&vertices==h[Vertices]&&triangles==h[Triangles]&&offset==h.sections[1].offset&&bytes==h.sections[1].bytes;}
  void Remember(const Header& h){generation=h[Generation];handle=h[IndexHandle];hash=h[TopologyHash];family=h[TopologyFamily];vertices=h[Vertices];triangles=h[Triangles];offset=h.sections[1].offset;bytes=h.sections[1].bytes;frame=h[IndexReadbackFrame];}
};
struct Slot {std::atomic<uint32_t> state{Free};Header header{};uint8_t* mapped{};IndexWitness indices{};TopologyWitness* topology{};};
inline void ExpandPhysicalPacking(const Header& h,uint8_t* mapped){
  if(h[CaptureTransport]!=4)return;
  if(!(h[HeaderFlags]&4)||h.sections[0].type!=Physical||h.sections[0].bytes!=64*h[Vertices]||!h[Vertices]||h[Vertices]>SlotBytes/64)throw std::runtime_error("unproven packed physical witness");
  auto* physical=mapped+h.sections[0].offset-HeaderBytes;
  std::array<uint8_t,64> exceptions{};const size_t n=size_t(std::min<uint64_t>(h[Vertices],4));
  // Preserve the raw first-four exceptions before backwards expansion overwrites them.
  std::memcpy(exceptions.data(),physical+48*h[Vertices],16*n);
  for(size_t i=size_t(h[Vertices]);i-->0;){std::memmove(physical+64*i,physical+48*i,48);
    if(i<n)std::memcpy(physical+64*i+48,exceptions.data()+16*i,16);
    else std::memset(physical+64*i+48,0,16); // Exact zeros positively attested by the GPU.
  }
}
inline uint64_t CompactedPrefixBytes(const Header& h,const uint8_t* mapped){
  const auto& s=h.sections[5];
  if(s.type!=Indirect||s.bytes!=20||s.offset<HeaderBytes||s.offset-HeaderBytes>SlotBytes-20)throw std::runtime_error("frozen indirect layout");
  const auto* draw=reinterpret_cast<const uint32_t*>(mapped+s.offset-HeaderBytes);
  if(h[Triangles]>UINT32_MAX/3||draw[0]>h[Triangles]*3||draw[0]%3||draw[1]!=1||draw[2]||draw[3]||draw[4])throw std::runtime_error("frozen indirect bounds");
  return uint64_t(draw[0])*4;
}

class Sha256 {
  BCRYPT_ALG_HANDLE algorithm{};BCRYPT_HASH_HANDLE hash{};
public:
  Sha256(){if(BCryptOpenAlgorithmProvider(&algorithm,BCRYPT_SHA256_ALGORITHM,nullptr,0)<0||BCryptCreateHash(algorithm,&hash,nullptr,0,nullptr,0,0)<0)throw std::runtime_error("frozen SHA256 init failed");}
  void Add(const void* data,size_t bytes){while(bytes){auto n=ULONG(std::min<size_t>(bytes,1024*1024));if(BCryptHashData(hash,(PUCHAR)data,n,0)<0)throw std::runtime_error("frozen SHA256 update failed");data=static_cast<const uint8_t*>(data)+n;bytes-=n;}}
  std::array<uint8_t,32> Finish(){std::array<uint8_t,32> value{};if(BCryptFinishHash(hash,value.data(),32,0)<0)throw std::runtime_error("frozen SHA256 finish failed");return value;}
  ~Sha256(){if(hash)BCryptDestroyHash(hash);if(algorithm)BCryptCloseAlgorithmProvider(algorithm,0);}
};
enum StorageOperation:uint64_t {Create=1,Write,Position,Flush,Close,Publish};
class StorageError:public std::runtime_error {
public:const StorageOperation operation;const DWORD nativeError;
  StorageError(StorageOperation op,DWORD error,const char* text):std::runtime_error(text),operation(op),nativeError(error){}
};
inline void FileWrite(HANDLE file,const void* data,size_t bytes){while(bytes){DWORD done=0,want=DWORD(std::min<size_t>(bytes,1024*1024));if(!WriteFile(file,data,want,&done,nullptr))throw StorageError(Write,GetLastError(),"frozen write failed");if(done!=want)throw StorageError(Write,ERROR_WRITE_FAULT,"frozen short write");data=static_cast<const uint8_t*>(data)+done;bytes-=done;}}
inline void Seek(HANDLE file,uint64_t offset){LARGE_INTEGER pos;pos.QuadPart=offset;if(!SetFilePointerEx(file,pos,nullptr,FILE_BEGIN))throw StorageError(Position,GetLastError(),"frozen seek failed");}
inline void PublishFile(const std::filesystem::path& temporary,const std::filesystem::path& final){
  // MoveFileEx replacement conflicts with an open alarm/audit pin even when it
  // shares DELETE. ReplaceFile preserves that reader's old file identity.
  // No retry loop, overwrite-in-place, or unsupported WRITE_THROUGH flag.
  if(ReplaceFileW(final.c_str(),temporary.c_str(),nullptr,0,nullptr,nullptr))return;
  const DWORD replaceError=GetLastError();
  if(replaceError!=ERROR_FILE_NOT_FOUND)throw StorageError(Publish,replaceError,"frozen replacement failed");
  if(GetFileAttributesW(final.c_str())!=INVALID_FILE_ATTRIBUTES)throw StorageError(Publish,replaceError,"frozen replacement source absent");
  const DWORD missingError=GetLastError();
  if(missingError!=ERROR_FILE_NOT_FOUND)throw StorageError(Publish,missingError,"frozen publication target unavailable");
  // First publication has no destination to replace. Never replace a target
  // that appeared unexpectedly after this single-writer ownership check.
  if(!MoveFileExW(temporary.c_str(),final.c_str(),MOVEFILE_WRITE_THROUGH))throw StorageError(Publish,GetLastError(),"frozen first publication failed");
}

// The renderer only changes slot ownership and writes the small input header.
// GPU transfers fill independent readback storage. The worker alone hashes,
// validates lengths and writes files; no worker reads live renderer resources.
class Writer {
  std::filesystem::path directory;std::thread worker;std::atomic<bool> stopping{};
  std::array<char,192> failureText{};std::atomic<bool> textReady{};
  uint64_t nextIdentity{},nextDue{};
  bool admissionsSuspended{};
  void Save(Slot& slot){
    Header h=slot.header;
    if(h[Frame]!=h[Completed]||!h[Submission]||h[Submission]!=h[CompletedSubmission]||h[PreparedPupil]!=h[CullPupil]||h[CullPupil]!=h[RasterPupil])throw std::runtime_error("frozen completion/ownership mismatch");
    const auto compactedBytes=CompactedPrefixBytes(h,slot.mapped);
    ExpandPhysicalPacking(h,slot.mapped);
    // Never persist the unwritten tail of the atomic compacted-index buffer.
    if((h[CaptureTransport]==1&&h[HostCompactedBytes]!=compactedBytes)||(h[CaptureTransport]>=2&&h[HostCompactedBytes]!=0)||h[CaptureTransport]>4)throw std::runtime_error("frozen transport/prefix invalid");
    if(h[CaptureTransport]>=3){
      if(!slot.topology||!slot.topology->pins.load()||!slot.topology->Matches(h)||h[Sections]!=16||h.sections[15].type!=TopologyProvenance||h.sections[15].bytes!=sizeof(slot.topology->authority)||
        std::memcmp(reinterpret_cast<const uint8_t*>(&h)+h.sections[15].offset,slot.topology->authority.data(),sizeof(slot.topology->authority)))throw std::runtime_error("frozen topology writer ownership");
    }
    auto data=[&](const Section& s){return h[CaptureTransport]>=3&&s.type==Indices?slot.topology->mapped:slot.mapped+s.offset-HeaderBytes;};
    for(size_t i=0;i<h[Sections];++i)if(h.sections[i].type==Compacted)h.sections[i].bytes=compactedBytes;
    h[Commit]=h[Identity];for(size_t i=Digest0;i<=Digest3;++i)h[i]=0;
    Sha256 sha;sha.Add(&h,sizeof(h));
    for(size_t i=0;i<h[Sections];++i){const auto& s=h.sections[i];if(s.offset>=HeaderBytes)sha.Add(data(s),size_t(s.bytes));}
    auto digest=sha.Finish();std::memcpy(&h.words[Digest0],digest.data(),digest.size());
    auto name=L"snapshot-"+std::to_wstring(h[Identity]%2);auto temporary=directory/(name+L".tmp"),final=directory/(name+L".bin");
    HANDLE file=CreateFileW(temporary.c_str(),GENERIC_WRITE,FILE_SHARE_READ,nullptr,CREATE_ALWAYS,FILE_ATTRIBUTE_NORMAL,nullptr);
    if(file==INVALID_HANDLE_VALUE)throw StorageError(Create,GetLastError(),"frozen create failed");
    try{
      Header uncommitted{};FileWrite(file,&uncommitted,sizeof(uncommitted));
      for(size_t i=0;i<h[Sections];++i){const auto& s=h.sections[i];if(s.offset>=HeaderBytes){Seek(file,s.offset);FileWrite(file,data(s),size_t(s.bytes));}}
      Seek(file,0);FileWrite(file,&h,sizeof(h));if(!FlushFileBuffers(file))throw StorageError(Flush,GetLastError(),"frozen flush failed");
    }catch(...){CloseHandle(file);throw;}
    if(!CloseHandle(file))throw StorageError(Close,GetLastError(),"frozen close failed");
    PublishFile(temporary,final);
  }
  void Run(){
    for(;;){Slot* oldest=nullptr;
      for(auto& slot:slots)if(slot.state.load(std::memory_order_acquire)==Ready&&(!oldest||slot.header[Identity]<oldest->header[Identity]))oldest=&slot;
      if(oldest){oldest->state.store(Writing);const auto began=GetTickCount64();
        LARGE_INTEGER qpcStart,qpcEnd,frequency;QueryPerformanceCounter(&qpcStart);QueryPerformanceFrequency(&frequency);
        FILETIME created{},exited{},kernelStart{},userStart{},kernelEnd{},userEnd{};
        GetThreadTimes(GetCurrentThread(),&created,&exited,&kernelStart,&userStart);
        try{Save(*oldest);}catch(const std::exception& error){
          failureIdentity=oldest->header[Identity];failureFrame=oldest->header[Frame];
          if(const auto* storage=dynamic_cast<const StorageError*>(&error)){failureOperation=storage->operation;failureNativeError=storage->nativeError;}
          if(!textReady.load()){std::strncpy(failureText.data(),error.what(),failureText.size()-1);textReady.store(true,std::memory_order_release);}failure.store(1);
        }
        QueryPerformanceCounter(&qpcEnd);GetThreadTimes(GetCurrentThread(),&created,&exited,&kernelEnd,&userEnd);
        auto time=[](FILETIME t){return (uint64_t(t.dwHighDateTime)<<32)|t.dwLowDateTime;};
        lastWriteMicroseconds.store(uint64_t(double(qpcEnd.QuadPart-qpcStart.QuadPart)*1e6/double(frequency.QuadPart)));
        lastWriteCpu100ns.store(time(kernelEnd)+time(userEnd)-time(kernelStart)-time(userStart));
        lastWriteMilliseconds.store(GetTickCount64()-began);
        if(!failure.load()){lastDurableFrame.store(oldest->header[Frame]);lastDurableIdentity.store(oldest->header[Identity]);}
        if(oldest->topology){oldest->topology->pins.fetch_sub(1,std::memory_order_release);oldest->topology=nullptr;}
        oldest->state.store(Free,std::memory_order_release);
        if(failure.load()==1)break; // Preserve one partial file; never retry failed storage.
      }
      if(stopping.load()&&!oldest)break;
      Sleep(1);
    }
  }
public:
  std::array<Slot,SlotCount> slots{};
  std::array<TopologyWitness,TopologyCount> topologies{};
  uint64_t nextTopologyIdentity{};
  TopologyWitness& PinTopology(Header& h){
    for(auto& t:topologies)if(t.Matches(h)){
      if(!t.authority[TSourceBirth]||!t.authority[TSubmission]||t.authority[TFrame]!=t.authority[TCompleted]||t.authority[TSubmission]!=t.authority[TCompletedSubmission]||t.authority[TFrame]>=h[Frame])throw std::runtime_error("frozen topology completion authority invalid");
      h.Input(TopologyProvenance,t.authority.data(),sizeof(t.authority));t.pins.fetch_add(1,std::memory_order_acquire);return t;
    }
    // Cadence is never skipped or deferred to hide unavailable evidence.
    throw std::runtime_error("frozen topology witness not ready for due capture");
  }
  std::atomic<uint64_t> failure{},lastDurableFrame{},lastDurableIdentity{},lastWriteMilliseconds{};
  std::atomic<uint64_t> failureIdentity{},failureFrame{},failureOperation{},failureNativeError{};
  std::atomic<uint64_t> lastWriteMicroseconds{},lastWriteCpu100ns{};
  void Start(const std::filesystem::path& output){directory=output;worker=std::thread([this]{Run();});}
  bool Active()const{return worker.joinable();}
  bool AdmissionsEnabled()const{return !admissionsSuspended;}
  uint64_t LastAdmittedIdentity()const{return nextIdentity;}
  void SuspendAdmissions(){admissionsSuspended=true;}
  bool Drained()const{
    if(failure.load()||lastDurableIdentity.load()!=nextIdentity)return false;
    for(const auto& s:slots)if(s.state.load(std::memory_order_acquire)!=Free)return false;
    for(const auto& t:topologies)if(t.pins.load(std::memory_order_acquire)||t.state==Recorded||t.state==nc::frozen::Submitted)return false;
    return true;
  }
  const char* FailureText()const{return textReady.load(std::memory_order_acquire)?failureText.data():"frozen slot/completion failure";}
  Slot* Reserve(uint64_t now){
    if(!Active()||admissionsSuspended||now<nextDue)return nullptr;
    const uint64_t id=nextIdentity+1;auto& slot=slots[id%SlotCount];uint32_t expected=Free;
    if(!slot.state.compare_exchange_strong(expected,Reserved,std::memory_order_acquire)){failure.store(2);return nullptr;}
    slot.header.Begin();slot.header[Identity]=++nextIdentity;nextDue=now+CadenceMs;return &slot;
  }
  void Submitted(uint64_t frame,uint64_t sequence){
    for(auto& t:topologies)if(t.state==Recorded&&t.authority[TFrame]==frame){t.authority[TSubmission]=sequence;t.state=nc::frozen::Submitted;}
    for(auto& s:slots)if(s.state.load()==Reserved&&s.header[Frame]==frame){s.header[Submission]=sequence;s.state.store(InFlight,std::memory_order_release);}}
  void Presented(uint64_t frame,int64_t result){for(auto& s:slots)if(s.state.load()==InFlight&&s.header[Frame]==frame)s.header[Present]=uint64_t(result);}
  void Stop(){if(worker.joinable()){stopping=true;worker.join();}}
  ~Writer(){Stop();}
};
}
