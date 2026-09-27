#pragma once
#include <windows.h>
#include <cstdint>
#include <stdexcept>

namespace nc::startup {
enum Slot { UiReady=6, Loading=7, NativeBegin=8, NativeReady=9, Submission=10,
  Completion=11, Steady=12, Shutdown=13, Cleanup=14, Exit=15, Denied=19 };
struct Cancelled {};
class Channel {
  HANDLE mapping=nullptr; volatile LONG64* words=nullptr;
  static LONG64 Now(){LARGE_INTEGER t;QueryPerformanceCounter(&t);return t.QuadPart;}
  LONG64 Read(int i)const{return InterlockedCompareExchange64(words+i,0,0);}
  void Invalid(const char* message){if(words){UnmapViewOfFile(const_cast<LONG64*>(words));words=nullptr;}throw std::runtime_error(message);}
public:
  ~Channel(){if(words)UnmapViewOfFile(const_cast<LONG64*>(words));if(mapping)CloseHandle(mapping);}
  void Open(){
    wchar_t name[256]{};auto n=GetEnvironmentVariableW(L"NOVACORE_STARTUP_MAPPING",name,256);if(!n)return;
    if(n>=256)throw std::runtime_error("Startup mapping name overflow");
    mapping=OpenFileMappingW(FILE_MAP_ALL_ACCESS,FALSE,name);
    if(mapping)words=static_cast<volatile LONG64*>(MapViewOfFile(mapping,FILE_MAP_ALL_ACCESS,0,0,256));
    LARGE_INTEGER f;QueryPerformanceFrequency(&f);
    if(!words||Read(0)!=0x315452415453434eLL||Read(1)!=1||Read(2)!=256||Read(24)!=f.QuadPart)
      Invalid("Startup mapping contract invalid");
    auto owner=InterlockedCompareExchange64(words+3,GetCurrentProcessId(),0);
    if(owner&&owner!=GetCurrentProcessId())Invalid("Startup mapping owner mismatch");
  }
  void Mark(Slot slot){if(words)InterlockedCompareExchange64(words+slot,Now(),0);}
  // Optional one-way diagnostic admission protocol. Lifecycle timestamps,
  // heartbeat, stop and all their deadlines retain their original ownership.
  uint64_t CaptureDisableRequest()const{
    if(!words||!Read(25))return 0;
    if(Read(25)!=2026092301LL)throw std::runtime_error("capture control contract invalid");
    return uint64_t(Read(20));
  }
  void AcknowledgeCaptureDisable(uint64_t request){if(words){InterlockedExchange64(words+23,Now());InterlockedExchange64(words+22,LONG64(request));}}
  void CheckAllowed(){if(words&&(Read(5)||Read(Shutdown)||Now()-Read(4)>Read(24))){Mark(Denied);throw Cancelled{};}}
  void BeginNative(){CheckAllowed();if(words&&(!Read(UiReady)||!Read(Loading)))throw std::runtime_error("Missing managed startup handoff");Mark(NativeBegin);}
  void Submitted(uint64_t frame){if(words&&frame&&!Read(Submission)){InterlockedExchange64(words+16,frame);Mark(Submission);}}
  void Completed(uint64_t frame){if(!words||!frame)return;
    if(!Read(Completion)){InterlockedExchange64(words+17,frame);Mark(Completion);}
    else if(!Read(Steady)&&frame>uint64_t(Read(17))){InterlockedExchange64(words+18,frame);Mark(Steady);}
  }
};
}
