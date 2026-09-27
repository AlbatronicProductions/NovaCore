#include "CausalRecorder.h"
#include "RecordingCallTrace.h"
#include "StartupLifecycle.h"
#include <cstdio>
#include <string>
static int GuardTests(){
  int passed=0;
  for(int test=0;test<6;test++){
    wchar_t name[128];swprintf_s(name,L"Local\\NovaCore.Startup.Guard.%lu.%d",GetCurrentProcessId(),test);
    HANDLE map=CreateFileMappingW(INVALID_HANDLE_VALUE,nullptr,PAGE_READWRITE,0,256,name);
    auto* w=static_cast<LONG64*>(MapViewOfFile(map,FILE_MAP_ALL_ACCESS,0,0,256));if(!w)return 2;
    LARGE_INTEGER f,t;QueryPerformanceFrequency(&f);QueryPerformanceCounter(&t);
    w[0]=0x315452415453434eLL;w[1]=1;w[2]=256;w[24]=f.QuadPart;w[4]=t.QuadPart;w[6]=w[7]=t.QuadPart;
    if(test==0)w[5]=1;if(test==1)w[4]-=2*f.QuadPart;if(test==2)w[13]=t.QuadPart;if(test==3)w[7]=0;if(test==5)w[3]=GetCurrentProcessId()+1;
    SetEnvironmentVariableW(L"NOVACORE_STARTUP_MAPPING",name);int simulatedGpuCalls=0;bool rejected=false;
    try{nc::startup::Channel startup;startup.Open();startup.BeginNative();++simulatedGpuCalls;
      startup.Submitted(0);startup.Completed(0);if(w[10]||w[11])return 3;
      startup.Mark(nc::startup::NativeReady);startup.Submitted(1);startup.Completed(1);startup.Completed(1);if(w[12])return 4;
      startup.Completed(2);if(!w[10]||!w[11]||!w[12]||w[16]!=1||w[17]!=1||w[18]!=2)return 5;
    }catch(const nc::startup::Cancelled&){rejected=true;}catch(const std::runtime_error&){rejected=true;}
    const bool expected=test==4?(!rejected&&simulatedGpuCalls==1):(rejected&&simulatedGpuCalls==0);
    if(expected)++passed;UnmapViewOfFile(w);CloseHandle(map);
  }
  SetEnvironmentVariableW(L"NOVACORE_STARTUP_MAPPING",nullptr);
  std::printf("Native startup admission: %d/6 passed; Vulkan calls=0; denied cases simulated GPU calls=0\n",passed);return passed==6?0:1;
}
int main(int argc,char**argv){
  if(argc>1&&std::string(argv[1])=="--test-startup-guard")return GuardTests();
  nc::startup::Channel startup;startup.Open();
  const std::string mode=argc>1?argv[1]:"mock-normal";
  auto finish=[&](){startup.Mark(nc::startup::Shutdown);startup.Mark(nc::startup::Cleanup);startup.Mark(nc::startup::Exit);return 0;};
  startup.Mark(nc::startup::UiReady);
  if(mode=="mock-shutdown-ui")return finish();
  startup.Mark(nc::startup::Loading);
  if(mode=="mock-shutdown-loading")return finish();
  if(mode=="mock-late-native"){
    // The external observer revokes the lease before this simulated GPU boundary.
    Sleep(250);startup.Mark(nc::startup::Shutdown);
    try{startup.BeginNative();return 7;}catch(const nc::startup::Cancelled&){std::puts("native admission cancelled; simulated GPU calls=0");return finish();}
  }
  startup.BeginNative();
  if(mode=="mock-shutdown-native-init")return finish();
  nc::causal::Recorder recorder;recorder.Open();if(!recorder.Active())return 2;
  startup.Mark(nc::startup::NativeReady);
  recorder.Emit(nc::causal::Setup,nc::causal::End);
  if(mode=="mock-shutdown-await-submit")return finish();
  if(mode=="mock-no-first-frame"){Sleep(10000);return 4;}
  if(mode=="mock-stall"){recorder.Emit(nc::causal::Fence,nc::causal::Begin);Sleep(10000);return 4;}
  for(uint64_t frame=1;frame<=100000&&!recorder.StopRequested();frame++){
    recorder.frame=frame;recorder.Emit(nc::causal::Submit,nc::causal::Begin);
    recorder.submitted=frame;recorder.Emit(nc::causal::Submit,nc::causal::End);
    startup.Submitted(frame);
    if(mode=="mock-shutdown-await-completion")return finish();
    if(mode=="mock-first-frame-stall"){Sleep(10000);return 4;}
    if(mode=="mock-fatal"&&frame==20){recorder.Emit(nc::causal::Present,nc::causal::End,-4);break;}
    recorder.completed=frame;recorder.Emit(nc::causal::Completed,nc::causal::Info);
    startup.Completed(frame);
    if(mode=="mock-scalar-stall"&&frame==2){
      recorder.Emit(nc::causal::Record,nc::causal::Begin);
      nc::causal::RecordingCallTrace trace;trace.Start(recorder,{1234,1,0,77});
      trace.Call(nc::causal::RecordingOp::HeaderClock,[]{});
      trace.Enter(nc::causal::RecordingOp::AuthorityScalars);
      constexpr size_t keys[]={4,7,10,11,12,13,14,15,16,17,18,19,20,21,22};
      for(size_t i=0;i<std::size(keys);i++)trace.Scalar(nc::causal::AuthorityScalar(i+1),keys[i],[]{});
      trace.Scalar(nc::causal::AuthorityScalar::Width,23,[]{Sleep(10000);});return 4;
    }
    if(mode=="mock-recording-stall"&&frame==2){
      recorder.frame=3;recorder.Emit(nc::causal::Record,nc::causal::Begin);
      nc::causal::RecordingCallTrace trace;trace.Start(recorder,{1234,1,0,77});
      trace.Call(nc::causal::RecordingOp::FinalDraw,[]{});
      trace.Call(nc::causal::RecordingOp::EndRenderPass,[]{});
      trace.Call(nc::causal::RecordingOp::InputCamera,[]{Sleep(10000);}); // CPU-only missing return.
      return 9;
    }
    if(mode=="mock-shutdown-first-completion"||(mode=="mock-shutdown-steady"&&frame==2))return finish();
    if(mode=="mock-workload")for(int operation=0;operation<120;operation++)recorder.Emit(nc::causal::Dispatch,nc::causal::Info,0,{uint64_t(operation),1024,1,1});
    if(mode!="mock-flood")Sleep(mode=="mock-workload"?8:10);
    if((mode=="mock-normal"||mode=="mock-cleanup-linger")&&frame==50)break;
  }
  startup.Mark(nc::startup::Shutdown);recorder.Emit(nc::causal::Cleanup,nc::causal::End);startup.Mark(nc::startup::Cleanup);
  if(mode=="mock-cleanup-linger")Sleep(1500); // Managed/UI shutdown after the renderer has fully stopped.
  std::puts("Mock producer complete; no GPU.");startup.Mark(nc::startup::Exit);return 0;
}
