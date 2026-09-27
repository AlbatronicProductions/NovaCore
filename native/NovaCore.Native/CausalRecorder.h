#pragma once
#include <windows.h>
#include <array>
#include <algorithm>
#include <atomic>
#include <cstddef>
#include <bit>
#include <cstdint>
#include <cstring>
#include <stdexcept>
#include <initializer_list>
#include <span>

namespace nc::causal {
constexpr uint64_t Magic=0x314c41535541434eull;
constexpr size_t HeaderBytes=128, RecordBytes=512, Capacity=16384;
enum Phase : uint64_t { Setup=1, Device, Swap, Recreate, Fence, Host, Upload, Record,
  Acquire, Submit, Present, Cleanup, Fault, Snapshot, Resource, Dispatch, Draw,
  Capabilities, Completed, MemoryBudget, Validation, DeviceIdle, QueueIdle, FenceApi, DiagnosticStop, FrameAuthority, FrozenCapture, RecordingCall };
enum Kind : uint64_t { Begin=0, End=1, Info=2 };
struct alignas(64) Entry { uint64_t serial,qpc,frame,submitted,completed,phase,kind;
  int64_t result; uint64_t swap; std::array<uint64_t,54> data; uint64_t commit; };
static_assert(sizeof(Entry)==RecordBytes && offsetof(Entry,commit)==504);

// One renderer-thread producer, one independent process consumer. The producer
// never opens files, flushes storage, allocates per event or waits for the reader.
class Recorder {
  HANDLE mapping{}; uint64_t* header{}; Entry* entries{};
  uint64_t serial{};
  volatile LONG emitting{};
public:
  std::atomic<uint64_t> frame{},submitted{},completed{},swap{};
  uint64_t draws{},dispatches{},groups{},submissionSequence{};
  static uint64_t Bits(double value){return std::bit_cast<uint64_t>(value);}
  bool Active() const{return header!=nullptr;}
  uint64_t LastSerial() const{return serial;}
  bool StopRequested(){
    if(!header)return false;
    if(InterlockedCompareExchange64((volatile LONG64*)&header[9],0,0)!=0){Emit(DiagnosticStop,Info,0,{1});return true;}
    LARGE_INTEGER now;QueryPerformanceCounter(&now);
    const auto heartbeat=InterlockedCompareExchange64((volatile LONG64*)&header[14],0,0);
    // A dead observer must not leave an unobserved live renderer running.
    if(heartbeat&&now.QuadPart-heartbeat>LONG64(header[5])){
      Emit(DiagnosticStop,Info,0,{2,uint64_t(heartbeat),uint64_t(now.QuadPart)});return true;
    }
    return false;
  }
  static uint64_t Checksum(const Entry& entry){
    const auto* bytes=reinterpret_cast<const unsigned char*>(&entry);
    uint64_t hash=14695981039346656037ull;
    for(size_t i=0;i<offsetof(Entry,commit);i++)if(i<488||i>=496){hash^=bytes[i];hash*=1099511628211ull;}
    return hash;
  }
  void Open() {
    wchar_t name[256];const DWORD n=GetEnvironmentVariableW(L"NOVACORE_CAUSAL_MAPPING",name,256);
    if(!n)return;
    if(n>=256)throw std::runtime_error("causal mapping name too long");
    mapping=OpenFileMappingW(FILE_MAP_ALL_ACCESS,FALSE,name);
    if(!mapping)throw std::runtime_error("causal observer mapping unavailable");
    auto* candidate=(uint64_t*)MapViewOfFile(mapping,FILE_MAP_ALL_ACCESS,0,0,HeaderBytes+Capacity*RecordBytes);
    if(!candidate||candidate[0]!=Magic||candidate[1]!=2||candidate[2]!=RecordBytes||candidate[3]!=Capacity||candidate[10]!=1){
      if(candidate)UnmapViewOfFile(candidate);throw std::runtime_error("causal observer not ready or incompatible");}
    if(candidate[4]&&candidate[4]!=GetCurrentProcessId()){UnmapViewOfFile(candidate);throw std::runtime_error("causal observer PID mismatch");}
    header=candidate;
    header[4]=GetCurrentProcessId();LARGE_INTEGER frequency;QueryPerformanceFrequency(&frequency);header[5]=frequency.QuadPart;
    entries=(Entry*)((char*)header+HeaderBytes);
    Emit(Setup,Begin);
  }
  void Emit(Phase phase,Kind kind,int64_t result=0,std::initializer_list<uint64_t> data={}) {
    EmitWords(phase,kind,result,std::span<const uint64_t>(data.begin(),data.size()));
  }
  void EmitWords(Phase phase,Kind kind,int64_t result,std::span<const uint64_t> data) {
    if(!header)return;
    if(InterlockedCompareExchange(&emitting,1,0)!=0){InterlockedIncrement64((volatile LONG64*)&header[12]);return;}
    Entry record{};record.serial=++serial;LARGE_INTEGER now;QueryPerformanceCounter(&now);
    record.qpc=now.QuadPart;record.frame=frame;record.submitted=submitted;record.completed=completed;
    record.phase=phase;record.kind=kind;record.result=result;record.swap=swap;
    size_t i=0;for(auto value:data){if(i<record.data.size())record.data[i++]=value;}
    record.data[53]=GetCurrentThreadId();
    record.data[52]=Checksum(record);
    auto &slot=entries[(serial-1)%Capacity];
    InterlockedExchange64((volatile LONG64*)&slot.commit,0);
    std::memcpy(&slot,&record,offsetof(Entry,commit));
    InterlockedExchange64((volatile LONG64*)&slot.commit,serial);
    InterlockedExchange64((volatile LONG64*)&header[6],serial);
    InterlockedExchange64((volatile LONG64*)&header[13],record.qpc);
    const auto read=InterlockedCompareExchange64((volatile LONG64*)&header[7],0,0);
    if(serial>uint64_t(read)+Capacity)InterlockedIncrement64((volatile LONG64*)&header[11]);
    InterlockedExchange(&emitting,0);
  }
  void Bytes(Phase phase,uint64_t identity,const void* data,size_t bytes){
    const auto* source=static_cast<const uint8_t*>(data);constexpr size_t fragment=376;
    const size_t chunks=(bytes+fragment-1)/fragment;
    for(size_t i=0;i<chunks;++i){std::array<uint64_t,52> words{};words[0]=identity;words[1]=i;words[2]=chunks;words[3]=bytes;
      const size_t count=std::min(fragment,bytes-i*fragment);words[4]=count;std::memcpy(words.data()+5,source+i*fragment,count);
      EmitWords(phase,Info,0,words);
    }
  }
  void Text(Phase phase,int64_t result,const char* text){
    // Text is fragmented through the same bounded ring; no independent I/O path.
    const size_t n=std::min<size_t>(text?std::strlen(text):0,400);
    std::array<uint64_t,51> words{};std::memcpy(words.data(),text,n);
    Emit(phase,Info,result,{n,words[0],words[1],words[2],words[3],words[4],words[5],words[6],words[7],words[8],words[9],words[10],words[11],words[12],words[13],words[14],words[15],words[16],words[17],words[18],words[19],words[20],words[21],words[22],words[23],words[24],words[25],words[26],words[27],words[28],words[29],words[30],words[31],words[32],words[33],words[34],words[35],words[36],words[37],words[38],words[39],words[40],words[41],words[42],words[43],words[44],words[45],words[46],words[47],words[48],words[49]});
  }
  ~Recorder(){if(header)UnmapViewOfFile(header);if(mapping)CloseHandle(mapping);}
  Recorder()=default;Recorder(const Recorder&)=delete;Recorder&operator=(const Recorder&)=delete;
};
}
