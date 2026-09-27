#include "NovaCoreNative.h"
#include "FrozenCapture.h"
#include "CausalRecorder.h"
#include "StartupLifecycle.h"
#include <vector>
#include <iostream>
#include <string>
using namespace nc::frozen;

void Fill(Slot& slot,uint64_t frame,bool large){
  auto id=slot.header[Identity];auto& h=slot.header;h.Begin();h[Identity]=id;h[Frame]=frame;h[Generation]=18+id;h[TopologyHash]=0xBCE444AFFB2D713Bull;
  h[TopologyFamily]=1;h[PreparedPupil]=h[CullPupil]=h[RasterPupil]=30+id;h[HeaderFlags]=3; // CPU synthetic marker
  h[Vertices]=large?712106:30;h[Triangles]=large?1424208:28;h[Width]=3440;h[Height]=1322;h[PhysicalGeneration]=4;h[TerrainVersion]=5;
  h.Bulk(Physical,h[Vertices]*64,64);h.Bulk(Indices,h[Triangles]*12,4);h.Bulk(Visibility,h[Triangles]*4,4);h.Bulk(Compacted,h[Triangles]*12,4);
  h.Bulk(Counters,168,4);h.Bulk(Indirect,20,4);
  NcCameraData camera{};camera.viewProjection.columns[0]=1;camera.viewProjection.columns[5]=1;camera.viewProjection.columns[10]=1;camera.viewProjection.columns[15]=1;
  NcPlanetaryGpuConstants gpu{};gpu.cameraBodyHighX=6378217;gpu.viewForwardZ=-1;gpu.viewportHeightPixels=1322;
  NcPlanetaryPresentation presentation{};NcProductionBillboardFrame pupils{};pupils.current.identity[0]=uint32_t(h[PreparedPupil]);
  pupils.current.metadata[0]=1;pupils.current.metadata[2]=uint32_t(h[Vertices]);std::array<uint8_t,512> preparation{};uint64_t residency[30]{};
  h.Input(Camera,&camera,sizeof(camera));h.Input(GpuInput,&gpu,sizeof(gpu));h.Input(Presentation,&presentation,sizeof(presentation));h.Input(Pupils,&pupils,sizeof(pupils));
  h.Input(Preparation,preparation.data(),preparation.size());h.Input(PublishedPupil,&pupils.current,sizeof(pupils.current));h.Input(Residency,residency,sizeof(residency));
  uint32_t catalog[32]{};std::array<uint8_t,576> demand{};h.Input(CatalogResidency,catalog,sizeof(catalog));h.Input(DemandInputs,demand.data(),demand.size());
  auto at=[&](int i){return slot.mapped+h.sections[i].offset-HeaderBytes;};
  auto* physical=reinterpret_cast<NcSphericalBillboardPhysicalVertex*>(at(0));
  for(uint32_t i=0;i<h[Vertices];++i){physical[i]={};physical[i].bodyFixed[0]=6378137+double(id);physical[i].bodyFixed[1]=double(i);physical[i].normal[0]=physical[i].normal[3]=1;}
  auto* indices=reinterpret_cast<uint32_t*>(at(1));auto* flags=reinterpret_cast<uint32_t*>(at(2));auto* compacted=reinterpret_cast<uint32_t*>(at(3));uint32_t visible=0;
  for(uint32_t i=0;i<h[Triangles];++i){for(uint32_t axis=0;axis<3;++axis)indices[3*i+axis]=(i+axis)%uint32_t(h[Vertices]);flags[i]=(i%2)==0;
    if(flags[i]){for(uint32_t axis=0;axis<3;++axis)compacted[3*visible+axis]=indices[3*i+axis];++visible;}}
  auto* counters=reinterpret_cast<uint32_t*>(at(4));std::fill(counters,counters+42,0);counters[0]=visible;counters[1]=uint32_t(h[Triangles])-visible;counters[4]=visible*3;counters[5]=uint32_t(h[Triangles]);counters[6]=uint32_t(h[Vertices]);float factor=1;std::memcpy(counters+23,&factor,4);
  auto* draw=reinterpret_cast<uint32_t*>(at(5));draw[0]=visible*3;draw[1]=1;draw[2]=draw[3]=draw[4]=0;
  h[Tcs]=visible;h[Tes]=visible*3;h[Clipping]=visible;
}
int main(int argc,char** argv){try{
  nc::startup::Channel startup;startup.Open();startup.Mark(nc::startup::UiReady);startup.Mark(nc::startup::Loading);startup.BeginNative();
  const std::string mode=argc>1?argv[1]:"mock-frozen-normal";bool large=mode=="mock-frozen-large";
  wchar_t output[32768];if(!GetEnvironmentVariableW(L"NOVACORE_FROZEN_CAPTURE",output,32768))return 2;
  nc::causal::Recorder recorder;recorder.Open();if(!recorder.Active())return 2;
  Writer writer;std::array<std::vector<uint8_t>,SlotCount> storage;
  for(size_t i=0;i<SlotCount;++i){storage[i].resize(large?SlotBytes:16384);writer.slots[i].mapped=storage[i].data();}
  writer.Start(mode=="mock-frozen-storage"?std::filesystem::path(output)/L"missing-directory":std::filesystem::path(output));
  startup.Mark(nc::startup::NativeReady);recorder.Emit(nc::causal::Setup,nc::causal::End);uint64_t sequence=0;uint64_t start=GetTickCount64();Header latestAuthority{};
  uint64_t disableRequest{},drainedAt{};bool adaptive=mode.starts_with("mock-frozen-adaptive");
  for(uint64_t frame=1;frame<=10000&&!recorder.StopRequested();++frame){
    recorder.frame=frame;
    if(adaptive&&mode!="mock-frozen-adaptive-missing-ack"){
      auto request=startup.CaptureDisableRequest();if(request&&!disableRequest){disableRequest=request;writer.SuspendAdmissions();recorder.Emit(nc::causal::Phase(30),nc::causal::Info,0,{1,request,writer.LastAdmittedIdentity(),writer.lastDurableIdentity.load(),writer.slots[0].state.load(),writer.slots[1].state.load()});startup.AcknowledgeCaptureDisable(request);}
      if(disableRequest&&!drainedAt&&writer.Drained()){drainedAt=GetTickCount64();recorder.Emit(nc::causal::Phase(30),nc::causal::Info,0,{2,disableRequest,writer.LastAdmittedIdentity(),writer.lastDurableIdentity.load(),0,0});}
      if(mode=="mock-frozen-adaptive-fault"&&drainedAt&&GetTickCount64()-drainedAt>1000)recorder.Emit(nc::causal::Fault,nc::causal::Info,-4);
    }
    auto* slot=writer.Reserve(GetTickCount64());
    if(slot){Fill(*slot,frame,large);LARGE_INTEGER now;QueryPerformanceCounter(&now);slot->header[RecordQpc]=now.QuadPart;
      slot->header[CaptureTransport]=2;slot->header[IndexReadbackFrame]=frame;
      slot->header[GpuCopiedBytes]=64*slot->header[Vertices]+28*slot->header[Triangles]+188;
      latestAuthority=slot->header;
      recorder.Emit(nc::causal::FrozenCapture,nc::causal::Info,0,{2,slot->header[Identity],frame,slot->header[PayloadBytes],slot->header[Generation],slot->header[PreparedPupil]});}
    auto authority=latestAuthority;authority[Frame]=frame;
    if(!slot){authority[Identity]=0;authority[HeaderFlags]=2;}
    recorder.Bytes(nc::causal::FrameAuthority,frame,&authority,sizeof(authority));
    recorder.Emit(nc::causal::Submit,nc::causal::Begin);writer.Submitted(frame,++sequence);recorder.submitted=frame;
    recorder.Emit(nc::causal::Submit,nc::causal::End,0,{1,2,sequence});writer.Presented(frame,0);
    startup.Submitted(frame);
    if(mode=="mock-frozen-repeated-cost"||adaptive){
      recorder.Emit(nc::causal::FenceApi,nc::causal::Begin,0,{1,91,1,UINT64_MAX});
      if(slot)Sleep(7);
      recorder.Emit(nc::causal::FenceApi,nc::causal::End);
    }
    if(mode=="mock-frozen-abrupt"&&slot&&slot->header[Identity]==2){writer.Stop();return 42;}
    recorder.completed=frame;recorder.Emit(nc::causal::Completed,nc::causal::Info);
    startup.Completed(frame);
    if(slot&&mode!="mock-frozen-backpressure"){
      LARGE_INTEGER began,ended,frequency;QueryPerformanceCounter(&began);QueryPerformanceFrequency(&frequency);
      auto& h=slot->header;h[Completed]=mode=="mock-frozen-stale"?frame+1:frame;h[CompletedSubmission]=sequence;LARGE_INTEGER now;QueryPerformanceCounter(&now);h[CompleteQpc]=now.QuadPart;
      if(mode=="mock-frozen-completion-cost")Sleep(2);
      recorder.Emit(nc::causal::FrozenCapture,nc::causal::Info,0,{8,h[Identity],frame,h[GpuCopiedBytes],0,h[IndexReadbackFrame],h[CompactedHandle],h[IndexHandle],size_t(slot-writer.slots.data())});
      recorder.Emit(nc::causal::FrozenCapture,nc::causal::Info,0,{3,h[Identity],frame,sequence,h[Generation],h[PreparedPupil],h[Tcs],h[Tes]});
      QueryPerformanceCounter(&ended);
      recorder.Emit(nc::causal::FrozenCapture,nc::causal::Info,0,{6,h[Identity],frame,h[PayloadBytes],nc::causal::Recorder::Bits(0.0),nc::causal::Recorder::Bits(double(ended.QuadPart-began.QuadPart)*1000/double(frequency.QuadPart))});
      slot->state.store(Ready,std::memory_order_release);
    }
    recorder.Emit(nc::causal::FrozenCapture,nc::causal::Info,writer.failure.load()?-7001:0,{5,writer.lastDurableIdentity.load(),writer.lastDurableFrame.load(),writer.lastWriteMilliseconds.load(),writer.failure.load()});
    if(writer.failure.load())break;
    if(GetTickCount64()-start>4200)break;
    Sleep(10);
  }
  startup.Mark(nc::startup::Shutdown);writer.Stop();recorder.Emit(nc::causal::FrozenCapture,nc::causal::Info,writer.failure.load()?-7001:0,{4,writer.lastDurableIdentity.load(),writer.lastDurableFrame.load(),writer.lastWriteMilliseconds.load(),writer.failure.load()});
  if(writer.failure.load())recorder.Text(nc::causal::FrozenCapture,-7001,writer.FailureText());
  recorder.Emit(nc::causal::Cleanup,nc::causal::End);
  startup.Mark(nc::startup::Cleanup);startup.Mark(nc::startup::Exit);
  std::cout<<"CPU frozen mock; durable="<<writer.lastDurableIdentity<<" writeMs="<<writer.lastWriteMilliseconds<<" error="<<writer.failure<<'\n';
  return writer.failure.load()?3:0;
}catch(const std::exception& e){std::cerr<<e.what()<<'\n';return 4;}}
