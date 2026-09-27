#pragma once
#include "CausalRecorder.h"
#include <type_traits>

namespace nc::causal {
// Protocol 2: one first-eligible capture interval per App lifetime. Fixed records
// go through the existing non-waiting ring; no strings, heap storage or locks.
enum class RecordingOp:uint64_t {
  FinalDraw=1, EndRenderPass, SceneLabelEnd, CaptureClockStart, CaptureClockFrequency,
  HeaderBegin, HeaderClock, AuthorityScalars, BulkPhysical, BulkIndices, BulkVisibility,
  BulkCompacted, BulkCounters, BulkIndirect, InputCamera, InputGpu, InputPresentation,
  InputPupils, InputPreparation, InputPublishedPupil, ResidencyScalars, InputResidency,
  CatalogScan, InputCatalog, InputDemand, ReserveClock, ReserveSlot, SlotHeader,
  CaptureLabelBegin, ResetQueries, TimestampBefore, BarrierBefore, CopyPhysical,
  CopyIndices, CopyVisibility, CopyCompacted, CopyCounters, CopyIndirect, BarrierAfter,
  TimestampAfter, ScheduledMarker, CaptureLabelEnd, AuthorityMarker, DurableMarker,
  CaptureClockEnd, CostMarker, FrameTimestamp, EndCommandBuffer, IncomingPupilRead, AuthorityResources
};
// Scalar ids are one-based; the header key is retained separately so lexical
// name lookup cannot silently turn a viewport dimension into an array index.
enum class AuthorityScalar:uint64_t { Frame=1, RecordQpc, Swap, Generation, IncomingGeneration,
  TopologyHash, TopologyFamily, PreparedPupil, CullPupil, RasterPupil, Vertices, Triangles,
  Draws, Dispatches, Groups, Width, Height, PhysicalGeneration, TerrainVersion,
  IncomingVertices, IncomingTriangles, IncomingTopologyHash, Flags };
class RecordingCallTrace {
  static_assert(std::atomic<uint64_t>::is_always_lock_free&&std::atomic<uint32_t>::is_always_lock_free);
  Recorder* recorder{};
  bool claimed{};
  uint64_t ordinal{},lastReturned{},lastReturnedOrdinal{},events{};
  std::array<uint64_t,32> context{};
  void Emit(uint64_t edge,uint64_t op,uint64_t a=0,uint64_t b=0,uint64_t c=0,uint64_t d=0){
    if(!recorder)return;
    // An instrumented-path expansion cannot silently turn into per-frame traffic.
    // Overflow is a diagnostic error, without changing the production call path.
    if(++events>160){recorder->Emit(RecordingCall,Info,-7002,{1,5});recorder=nullptr;return;}
    std::array<uint64_t,42> words{2,edge,op,ordinal,lastReturned,lastReturnedOrdinal,a,b,c,d};
    std::copy(context.begin(),context.end(),words.begin()+10);
    recorder->EmitWords(RecordingCall,Info,0,words);
  }
public:
  bool Claimed() const{return claimed;}
  bool Active() const{return recorder!=nullptr;}
  void Start(Recorder& sink,std::initializer_list<uint64_t> identity){
    if(claimed||!sink.Active())return;
    claimed=true;recorder=&sink;
    size_t i=0;for(auto value:identity)if(i<context.size())context[i++]=value;
    Emit(0,0);
  }
  void Slots(uint64_t state0,uint64_t identity0,uint64_t state1,uint64_t identity1){
    if(!recorder)return;
    context[18]=state0;context[19]=identity0;context[20]=state1;context[21]=identity1;
    Emit(4,0,state0,identity0,state1,identity1);
  }
  void Enter(RecordingOp op,uint64_t a=0,uint64_t b=0,uint64_t c=0,uint64_t d=0){
    if(recorder){++ordinal;Emit(1,uint64_t(op),a,b,c,d);}
  }
  void Returned(RecordingOp op){
    if(recorder){lastReturned=uint64_t(op);lastReturnedOrdinal=ordinal;Emit(2,uint64_t(op));}
  }
  template<class F> void Scalar(AuthorityScalar field,size_t headerIndex,F&& body){
    // One-shot fixed records only; no additional read, allocation, lock or wait
    // in the captured expression. The return marker follows the actual store.
    Emit(5,uint64_t(RecordingOp::AuthorityScalars),uint64_t(field),headerIndex);
    body();
    Emit(6,uint64_t(RecordingOp::AuthorityScalars),uint64_t(field),headerIndex);
  }
  template<class F> auto Call(RecordingOp op,F&& body,uint64_t a=0,uint64_t b=0,uint64_t c=0,uint64_t d=0){
    Enter(op,a,b,c,d);
    // Deliberately not an RAII "return" marker: an exception/non-return must not
    // manufacture successful completion of the operation being investigated.
    if constexpr(std::is_void_v<std::invoke_result_t<F>>){body();Returned(op);}
    else {auto value=body();Returned(op);return value;}
  }
  void Finish(){if(recorder){Emit(3,0);recorder=nullptr;}}
};
}
