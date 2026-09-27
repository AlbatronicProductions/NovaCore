#pragma once
#include <windows.h>
#include <array>
#include <cstdint>

namespace nc::minimum {
inline uint64_t ownedAllocationCalls{},ownedAllocationBytes{},ownedRetainedBytes{};
struct MeasurementSample {uint64_t loop{},ticks{},cycles{},events{},allocationCalls{},allocationBytes{};};
struct MeasurementHeader {uint64_t size{},version{},count{},frequency{},retainedBytes{},ownedCalls{},ownedBytes{},overflow{};};
static_assert(sizeof(MeasurementSample)==48&&sizeof(MeasurementHeader)==64);
struct Measurement {
  static constexpr size_t Capacity=32768;
  std::array<MeasurementSample,Capacity> samples{};
  MeasurementSample current{};
  uint64_t count{},startLoop{},frequency{},allocCalls{},allocBytes{};
  uint32_t depth{};bool active{},frame{},stop{},overflow{};
  void Start(uint64_t nextLoop){count=0;startLoop=nextLoop;active=true;stop=false;overflow=false;LARGE_INTEGER f{};if(!QueryPerformanceFrequency(&f)||f.QuadPart<=0)overflow=true;frequency=f.QuadPart;}
  void Begin(uint64_t loop){if(!active||loop<startLoop)return;current={};current.loop=loop;allocCalls=ownedAllocationCalls;allocBytes=ownedAllocationBytes;frame=true;}
  void End(){if(!frame)return;frame=false;current.allocationCalls=ownedAllocationCalls-allocCalls;current.allocationBytes=ownedAllocationBytes-allocBytes;if(count<samples.size())samples[count++]=current;else overflow=true;if(stop)active=false;}
};
// Measures only explicit producer bookkeeping blocks. Never spans a Vulkan call,
// host callback, filesystem operation or a renderer scope's lifetime.
struct MeasurementScope {
  Measurement* m{};LARGE_INTEGER start{};ULONG64 cycles{};
  explicit MeasurementScope(Measurement& value){if(!value.frame)return;m=&value;if(m->depth++==0){if(!QueryPerformanceCounter(&start)||!QueryThreadCycleTime(GetCurrentThread(),&cycles))m->overflow=true;}}
  ~MeasurementScope(){if(!m||--m->depth!=0)return;ULONG64 endCycles{};LARGE_INTEGER end{};if(!QueryThreadCycleTime(GetCurrentThread(),&endCycles)||!QueryPerformanceCounter(&end)||end.QuadPart<start.QuadPart||endCycles<cycles){m->overflow=true;return;}m->current.ticks+=uint64_t(end.QuadPart-start.QuadPart);m->current.cycles+=endCycles-cycles;}
};
}
