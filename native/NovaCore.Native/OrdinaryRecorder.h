#pragma once
#include <windows.h>
#include <array>
#include <cstdint>
#include <cstring>
#include <exception>
#include <initializer_list>
#include <memory>
#include "OrdinaryMeasurement.h"

// Independent minimum recorder: no Vulkan dependency, disk calls, waits, locks,
// or per-event heap allocation. Header/record ABI matches OrdinaryProtocol.cs.
namespace nc::minimum {
enum Phase:uint64_t {Session=1,Frame,Update,Draw,Acquire,Record,Submit,Fence,Completed,Present,Error,Resource,Context,Publication,Callback,DeviceIdle,QueueIdle,Recreate,Window,Shutdown,Heartbeat};
enum Edge:uint64_t {Info,Enter,Return,Exception};
enum ResourceKind:uint64_t {Memory=1,Buffer,Image,FenceKind,Command,Queue,Swapchain,Semaphore};
struct Event { uint64_t w[32]{}; };
static_assert(sizeof(Event)==256);
struct Identity {uint64_t handle{},kind{},birth{},recording{},owner{};uint8_t state{};};
class Recorder {
  HANDLE mapping{};uint8_t* base{};LONGLONG frequency{};
  std::array<Identity,16384> identities{};size_t live{};uint64_t births{};
  struct Pending {Event event{};uint64_t queue{};bool active{};};
  std::array<Pending,64> pending{};
  std::array<uint64_t,128> roles{};
  uint64_t operation{};
  size_t Hash(uint64_t kind,uint64_t handle)const{return((handle>>3)^(kind*11400714819323198485ull))&(identities.size()-1);}
  volatile LONG64* Header(size_t i)const{return reinterpret_cast<volatile LONG64*>(base)+i;}
  Identity* Find(uint64_t kind,uint64_t h,bool insert=false){
    if(!h)return nullptr;size_t slot=Hash(kind,h);Identity* vacant=nullptr;
    for(size_t i=0;i<identities.size();i++){
      auto& e=identities[(slot+i)&(identities.size()-1)];
      if(e.state==1&&e.kind==kind&&e.handle==h)return &e;
      if(e.state!=1&&!vacant)vacant=&e;
      if(e.state==0)break;
    }return insert?vacant:nullptr;
  }
  void Erase(Identity* p){
    // Backshift deletion keeps failed lookups bounded by the live cluster, not
    // the entire history of retired handles. Never accumulate tombstones.
    const size_t mask=identities.size()-1;size_t hole=size_t(p-identities.data());
    for(size_t scan=(hole+1)&mask;identities[scan].state==1;scan=(scan+1)&mask){
      const auto& e=identities[scan];size_t home=Hash(e.kind,e.handle);
      if(((scan-home)&mask)>=((scan-hole)&mask)){identities[hole]=e;hole=scan;}
    }identities[hole]={};
  }
public:
  Measurement measurement;
  static void* operator new(size_t size){void* p=::operator new(size);++ownedAllocationCalls;ownedAllocationBytes+=size;ownedRetainedBytes+=size;return p;}
  static void operator delete(void* p)noexcept{ownedRetainedBytes-=sizeof(Recorder);::operator delete(p);}
  uint64_t loop{},terrain{},current{},incoming{},publication{},submission{},completed{};
  bool Open(const wchar_t* name,uint64_t lo,uint64_t hi){
    mapping=OpenFileMappingW(FILE_MAP_ALL_ACCESS,FALSE,name);if(!mapping)return false;
    base=static_cast<uint8_t*>(MapViewOfFile(mapping,FILE_MAP_ALL_ACCESS,0,0,4096+8192*256));if(!base)return false;
    LARGE_INTEGER f;QueryPerformanceFrequency(&f);frequency=f.QuadPart;
    return Read(0)==0x314D554D494D434E&&Read(1)==1&&Read(2)==256&&Read(3)==8192&&uint64_t(Read(4))==lo&&uint64_t(Read(5))==hi&&Read(6)==GetCurrentProcessId()&&Read(15)==1;
  }
  ~Recorder(){if(base)UnmapViewOfFile(base);if(mapping)CloseHandle(mapping);}
  LONG64 Read(size_t i)const{return InterlockedCompareExchange64(Header(i),0,0);}
  void Fault(LONG64 bit){MeasurementScope cost(measurement);InterlockedOr64(Header(12),bit);}
  uint64_t Op(){MeasurementScope cost(measurement);return ++operation;}
  Event Make(Phase phase,Edge edge=Info,int64_t result=0,uint64_t op=0){MeasurementScope cost(measurement);Event e{};e.w[2]=loop;e.w[3]=terrain;e.w[4]=phase;e.w[5]=edge;e.w[6]=uint64_t(result);e.w[7]=op;e.w[8]=submission;e.w[9]=completed;e.w[10]=current;e.w[11]=incoming;e.w[12]=publication;e.w[28]=GetCurrentThreadId();return e;}
  bool Emit(Event e){MeasurementScope cost(measurement);
    if(Read(14)!=0)return false;
    if(InterlockedCompareExchange64(Header(18),1,0)!=0){Fault(2);InterlockedIncrement64(Header(16));return false;}
    if(Read(14)!=0){InterlockedExchange64(Header(18),0);return false;}
    LARGE_INTEGER t;QueryPerformanceCounter(&t);InterlockedExchange64(Header(17),t.QuadPart);
    if(Read(15)!=1||t.QuadPart-Read(11)>frequency*5)Fault(32);
    const auto p=Read(8);
    const auto consumed=Read(9),durable=Read(10);
    if(p<0||consumed<0||consumed>p||durable<0||durable>p){Fault(16);InterlockedIncrement64(Header(16));InterlockedExchange64(Header(18),0);return false;}
    if(p==INT64_MAX||p-consumed>=8192){Fault(1);InterlockedIncrement64(Header(16));InterlockedExchange64(Header(18),0);return false;}
    if(measurement.frame)++measurement.current.events;
    e.w[0]=p+1;e.w[1]=t.QuadPart;e.w[29]=uint64_t(Read(12)|Read(13));
    uint64_t hash=14695981039346656037ull;for(size_t i=0;i<240;i++){hash^=reinterpret_cast<uint8_t*>(&e)[i];hash*=1099511628211ull;}e.w[30]=hash;e.w[31]=p+1;
    auto* slot=reinterpret_cast<Event*>(base+4096)+(p%8192);InterlockedExchange64(reinterpret_cast<volatile LONG64*>(&slot->w[31]),0);
    std::memcpy(slot,&e,248);InterlockedExchange64(reinterpret_cast<volatile LONG64*>(&slot->w[31]),p+1);InterlockedExchange64(Header(8),p+1);InterlockedExchange64(Header(18),0);return true;
  }
  uint64_t Birth(uint64_t kind,uint64_t handle,std::initializer_list<uint64_t> data={}){MeasurementScope cost(measurement);
    if(!handle)return 0;if(Find(kind,handle)){Fault(64);return 0;}
    auto* identity=Find(kind,handle,true);if(!identity||live>=8192||births==UINT64_MAX){Fault(4);return 0;}
    *identity={handle,kind,++births,0,data.size()?*data.begin():0,1};live++;
    auto e=Make(Resource);e.w[13]=handle;e.w[14]=identity->birth;e.w[17]=1;e.w[18]=kind;size_t n=19;for(auto x:data){if(n<28)e.w[n++]=x;}Emit(e);return identity->birth;
  }
  uint64_t Existing(uint64_t kind,uint64_t handle){MeasurementScope cost(measurement);auto* p=Find(kind,handle);return p?p->birth:0;}
  void RetireDevice(){MeasurementScope cost(measurement);for(size_t i=0;i<identities.size();)if(identities[i].state==1){Retire(identities[i].kind,identities[i].handle);i=0;}else i++;}
  void RetireChildren(uint64_t owner){MeasurementScope cost(measurement);for(size_t i=0;i<identities.size();)if(identities[i].state==1&&identities[i].kind==Command&&identities[i].owner==owner){Retire(Command,identities[i].handle);i=0;}else i++;}
  uint64_t Id(uint64_t kind,uint64_t handle){MeasurementScope cost(measurement);if(!handle)return 0;auto* p=Find(kind,handle);if(!p){Fault(64);return 0;}return p->birth;}
  void Retire(uint64_t kind,uint64_t handle){MeasurementScope cost(measurement);if(!handle)return;auto* p=Find(kind,handle);if(!p){Fault(64);return;}auto e=Make(Resource);e.w[13]=handle;e.w[14]=p->birth;e.w[17]=2;e.w[18]=kind;Emit(e);Erase(p);live--;}
  void Association(uint64_t action,uint64_t kind,uint64_t handle,uint64_t memory,uint64_t offset=0,uint64_t size=0){MeasurementScope cost(measurement);auto e=Make(Resource);e.w[13]=handle;e.w[14]=Id(kind,handle);e.w[15]=memory;e.w[16]=Id(Memory,memory);e.w[17]=action;e.w[18]=kind;e.w[19]=offset;e.w[20]=size;Emit(e);}
  uint64_t Recording(uint64_t handle,bool next=false){MeasurementScope cost(measurement);auto* p=Find(Command,handle);if(!p){Fault(64);return 0;}if(next)++p->recording;return p->recording;}
  void Role(uint64_t slot,uint64_t kind,uint64_t handle){MeasurementScope cost(measurement);if(slot==0||slot>=roles.size()){Fault(4);return;}uint64_t id=Id(kind,handle);if(roles[slot]==id)return;roles[slot]=id;auto e=Make(Context);e.w[13]=handle;e.w[14]=id;e.w[17]=slot;e.w[18]=kind;Emit(e);}
  void Submitted(Event e,uint64_t queue){MeasurementScope cost(measurement);for(auto& p:pending)if(!p.active){p={e,queue,true};return;}Fault(4);}
  void Complete(uint64_t fence,uint64_t birth,uint64_t queue=0,bool all=false){MeasurementScope cost(measurement);for(auto& p:pending)if(p.active&&(all||(queue&&p.queue==queue)||(fence&&p.event.w[13]==fence&&p.event.w[14]==birth))){auto e=p.event;e.w[4]=Completed;e.w[5]=Info;e.w[6]=0;e.w[7]=0;completed=e.w[8];e.w[9]=completed;Emit(e);p.active=false;}}
};
inline std::unique_ptr<Recorder> recorder;
struct Scope {
  Phase phase;uint64_t op{};Event event{};int exceptions=std::uncaught_exceptions();
  Scope(Phase p,uint64_t action=0,uint64_t kind=0,uint64_t handle=0):phase(p){if(recorder){MeasurementScope cost(recorder->measurement);op=recorder->Op();event=recorder->Make(p,Enter,0,op);event.w[13]=handle;event.w[14]=handle?recorder->Id(kind,handle):0;event.w[17]=action;event.w[18]=kind;recorder->Emit(event);}}
  ~Scope(){if(recorder){MeasurementScope cost(recorder->measurement);auto end=recorder->Make(phase,std::uncaught_exceptions()>exceptions?Exception:Return,std::uncaught_exceptions()>exceptions?INT64_MIN:0,op);for(size_t i=13;i<28;i++)end.w[i]=event.w[i];recorder->Emit(end);}}
};
inline void ErrorResult(int64_t result){if(recorder){MeasurementScope cost(recorder->measurement);recorder->Emit(recorder->Make(Error,Info,result));}}
struct FrameMeasurement {Recorder* r;explicit FrameMeasurement(Recorder* value):r(value){if(r)r->measurement.Begin(r->loop);}~FrameMeasurement(){if(r){r->measurement.End();if(r->measurement.overflow)r->Fault(4);}}};
}
