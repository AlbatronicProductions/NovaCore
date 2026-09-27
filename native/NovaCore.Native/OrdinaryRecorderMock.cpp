// CPU-only exact production transport. No Vulkan loader or GPU calls.
#include "OrdinaryRecorder.h"
#include <objbase.h>
#include <cstdlib>
#include <new>
#include <algorithm>
#include <filesystem>
#include <fstream>
#include <string>
#include <vector>
#include <stdexcept>
#include <cstdio>
#include "OrdinaryMockVulkan.h"
bool countAllocations{};uint64_t producerAllocations{};
void* operator new(size_t n){if(countAllocations)producerAllocations++;if(auto* p=std::malloc(n))return p;throw std::bad_alloc();}
void operator delete(void* p)noexcept{std::free(p);}
void operator delete(void* p,size_t)noexcept{std::free(p);}
void Check(bool x,const char* text){if(!x)throw std::runtime_error(text);}
int wmain(int argc,wchar_t** argv){try{
  Check(argc==3,"output directory, observer executable required");std::filesystem::path root=argv[1];std::filesystem::create_directories(root);
  {
    namespace m=nc::minimum;
    auto test=std::make_unique<m::Measurement>();test->Start(3);test->Begin(2);Check(!test->frame,"measurement cannot start before selected frame");test->Begin(3);
    LARGE_INTEGER begin,end;QueryPerformanceCounter(&begin);
    {m::MeasurementScope outer(*test);{m::MeasurementScope inner(*test);Sleep(5);}Check(test->current.ticks==0&&test->depth==1,"nested producer scopes do not double count");}
    auto ticks=test->current.ticks;Check(ticks>0&&test->depth==0,"outer producer scope measures once");
    Sleep(80);Check(test->current.ticks==ticks,"unscoped API/wait time excluded");QueryPerformanceCounter(&end);test->stop=true;test->End();
    Check(test->count==1&&!test->active&&test->samples[0].ticks==ticks&&uint64_t(end.QuadPart-begin.QuadPart)>ticks,"bounded measurement stops at frame end");
    auto retained=m::ownedRetainedBytes,calls=m::ownedAllocationCalls,bytes=m::ownedAllocationBytes;
    {auto owner=std::make_unique<m::Recorder>();Check(m::ownedRetainedBytes==retained+sizeof(m::Recorder)&&m::ownedAllocationCalls==calls+1&&m::ownedAllocationBytes==bytes+sizeof(m::Recorder),"actual recorder owned allocation accounted");}
    Check(m::ownedRetainedBytes==retained,"recorder retained bytes released");
  }
  GUID id{};Check(CoCreateGuid(&id)==S_OK,"session uuid");wchar_t guid[40];StringFromGUID2(id,guid,40);std::wstring session=guid;session=session.substr(1,36);const auto directory=root/session;std::wstring name=L"Local\\MinimumMock-"+session;
  HANDLE mapping=CreateFileMappingW(INVALID_HANDLE_VALUE,nullptr,PAGE_READWRITE,0,4096+8192*256,name.c_str());Check(mapping!=nullptr,"mapping");auto* h=static_cast<LONG64*>(MapViewOfFile(mapping,FILE_MAP_ALL_ACCESS,0,0,0));Check(h!=nullptr,"view");
  memset(h,0,4096+8192*256);h[0]=0x314D554D494D434E;h[1]=1;h[2]=256;h[3]=8192;memcpy(h+4,&id,16);h[6]=GetCurrentProcessId();FILETIME created,exit,kernel,user;GetProcessTimes(GetCurrentProcess(),&created,&exit,&kernel,&user);h[7]=((uint64_t(created.dwHighDateTime)<<32)|created.dwLowDateTime)+504911232000000000ull;
  {
    auto probeName=name+L"-corruption";HANDLE pm=CreateFileMappingW(INVALID_HANDLE_VALUE,nullptr,PAGE_READWRITE,0,4096+8192*256,probeName.c_str());Check(pm!=nullptr,"probe mapping");auto* ph=static_cast<LONG64*>(MapViewOfFile(pm,FILE_MAP_ALL_ACCESS,0,0,0));Check(ph!=nullptr,"probe view");memcpy(ph,h,4096);ph[15]=1;LARGE_INTEGER now;QueryPerformanceCounter(&now);ph[11]=now.QuadPart;
    {auto probe=std::make_unique<nc::minimum::Recorder>();Check(probe->Open(probeName.c_str(),uint64_t(ph[4]),uint64_t(ph[5])),"probe ABI");ph[8]=-1;Check(!probe->Emit(probe->Make(nc::minimum::Frame))&&(ph[12]&16),"native corrupt sequence cannot index ring");ph[8]=0;ph[9]=1;Check(!probe->Emit(probe->Make(nc::minimum::Frame)),"native future consumed refused");ph[9]=0;ph[14]=1;Check(!probe->Emit(probe->Make(nc::minimum::Frame))&&ph[8]==0,"native sealed admission refuses late emission");}
    UnmapViewOfFile(ph);CloseHandle(pm);
  }
  std::wstring command=L"\""+std::wstring(argv[2])+L"\" observe \""+name+L"\" "+session+L" \""+directory.wstring()+L"\"";STARTUPINFOW si{sizeof(si)};PROCESS_INFORMATION pi{};Check(CreateProcessW(nullptr,command.data(),nullptr,nullptr,FALSE,CREATE_NO_WINDOW,nullptr,nullptr,&si,&pi),"observer process");
  for(int i=0;i<2000&&h[15]==0;i++)Sleep(10);Check(h[15]==1,"mandatory observer ready");
  namespace m=nc::minimum;m::recorder=std::make_unique<m::Recorder>();Check(m::recorder->Open(name.c_str(),uint64_t(h[4]),uint64_t(h[5])),"minimum native ABI session");auto& r=*m::recorder;
  auto memory=r.Birth(m::Memory,10,{4096});auto buffer=r.Birth(m::Buffer,20,{4096,16});r.Association(3,m::Buffer,20,10);r.Role(38,m::Buffer,20);
  r.Retire(m::Buffer,20);auto reused=r.Birth(m::Buffer,20,{8192,16});Check(reused!=buffer,"handle reuse birth");r.Association(3,m::Buffer,20,10);r.Role(38,m::Buffer,20);
  auto commandBirth=r.Birth(m::Command,30,{77,0});auto fence=r.Birth(m::FenceKind,40,{1});r.Complete(40,fence);Check(r.completed==0,"initial signalled fence is not completion");
  App app;gApp=&app;vkGetDeviceQueue({},0,0,&app.graphicsQueue);vkGetDeviceQueue({},0,0,&app.presentQueue);
  app.productionBillboardPhysicalBuffer=FakeHandle<VkBuffer>(20);app.regionalScratchBuffer=FakeHandle<VkBuffer>(21);r.Birth(m::Buffer,21,{8192,16});
  VkSwapchainCreateInfoKHR swap{VK_STRUCTURE_TYPE_SWAPCHAIN_CREATE_INFO_KHR};swap.imageExtent={3440,1440};vkCreateSwapchainKHR({},&swap,nullptr,&app.swapchain);
  auto commandHandle=FakeHandle<VkCommandBuffer>(30);auto fenceHandle=FakeHandle<VkFence>(40);
  SeedMinimumFixture(app);
  std::array<uint64_t,16> collisions{};for(size_t i=0;i<16;i++)collisions[i]=r.Birth(m::Buffer,100000+i*131072,{16,0});
  for(size_t i=0;i<16;i+=2)r.Retire(m::Buffer,100000+i*131072);
  for(size_t i=1;i<16;i+=2){Check(r.Id(m::Buffer,100000+i*131072)==collisions[i],"backshift preserves colliding identity lookup");r.Retire(m::Buffer,100000+i*131072);}
  std::vector<double> times;times.reserve(1800);LARGE_INTEGER frequency;QueryPerformanceFrequency(&frequency);FILETIME sc,se,sk,su;GetProcessTimes(GetCurrentProcess(),&sc,&se,&sk,&su);
  // Each frame simulates the production Update/Draw/acquire/record/submit/present envelope.
  r.measurement.Start(r.loop+1);countAllocations=true;for(int i=0;i<1800;i++){
    LARGE_INTEGER begin,end;QueryPerformanceCounter(&begin);r.loop++;r.terrain++;
    app.frame=r.terrain;uint64_t before=rawCalls;
    {m::FrameMeasurement measured(&r);m::Scope frame(m::Frame);{MinimumSync(app);m::Scope update(m::Update);vkWaitForFences({},1,&fenceHandle,VK_TRUE,UINT64_MAX);}
      {MinimumSync(app);m::Scope draw(m::Draw);uint32_t image{};rawAcquireOutOfDate=i==2;auto acquire=vkAcquireNextImageKHR({},app.swapchain,UINT64_MAX,{},{},&image);
       if(acquire!=VK_ERROR_OUT_OF_DATE_KHR){VkCommandBufferBeginInfo beginInfo{VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO};vkBeginCommandBuffer(commandHandle,&beginInfo);vkEndCommandBuffer(commandHandle);
        if(i%100==0){app.productionBillboardIncomingGeneration=1000+i;MinimumGeneration(app,3);std::swap(app.productionBillboardPhysicalBuffer,app.regionalScratchBuffer);MinimumPublication(app,2);}
        VkSubmitInfo submit{VK_STRUCTURE_TYPE_SUBMIT_INFO};submit.commandBufferCount=1;submit.pCommandBuffers=&commandHandle;vkQueueSubmit(app.graphicsQueue,1,&submit,i==0?VkFence{}:fenceHandle);if(i==0)vkQueueWaitIdle(app.graphicsQueue);
        VkPresentInfoKHR present{VK_STRUCTURE_TYPE_PRESENT_INFO_KHR};present.swapchainCount=1;present.pSwapchains=&app.swapchain;present.pImageIndices=&image;vkQueuePresentKHR(app.presentQueue,&present);
       }
      }
    }Check(rawCalls-before==uint64_t(i==2?2:i==0?7:6),"exact wrapper calls, acquire skip, no new GPU calls");QueryPerformanceCounter(&end);times.push_back(double(end.QuadPart-begin.QuadPart)*1e6/frequency.QuadPart);Sleep(8);
  }
  countAllocations=false;Check(r.measurement.count==1800&&!r.measurement.overflow,"all actual producer frames timed without overflow");for(size_t i=0;i<r.measurement.count;i++)Check(r.measurement.samples[i].ticks>0&&r.measurement.samples[i].events>0&&r.measurement.samples[i].allocationCalls==0&&r.measurement.samples[i].allocationBytes==0,"live accounting samples and owned zero allocation");Check(producerAllocations==0,"native producer zero allocations");r.Complete(0,0,0,true);Check(r.completed==1799,"bootstrap and frame completions; acquire skip");try{m::Scope scope(m::Callback);throw 1;}catch(int){}
  auto unmapSequence=h[8]+1;vkUnmapMemory({},seedMemory);auto* entry=reinterpret_cast<m::Event*>(reinterpret_cast<char*>(h)+4096)+(unmapSequence-1)%8192;Check(entry->w[0]==uint64_t(unmapSequence)&&entry->w[17]==5&&entry->w[5]==m::Enter,"actual unmap entry correctly attributed");
  rawSubmitResult=VK_ERROR_DEVICE_LOST;VkSubmitInfo failed{VK_STRUCTURE_TYPE_SUBMIT_INFO};failed.commandBufferCount=1;failed.pCommandBuffers=&commandHandle;
  Check(vkQueueSubmit(app.graphicsQueue,1,&failed,fenceHandle)==VK_ERROR_DEVICE_LOST,"failed submit result forwarded");m::ErrorResult(VK_ERROR_DEVICE_LOST);vkDeviceWaitIdle({});Check(r.completed==1799,"failed submission cannot manufacture completion");
  r.RetireChildren(77);r.Retire(m::FenceKind,40);r.Retire(m::Buffer,20);r.Retire(m::Memory,10);vkDestroyDevice({},nullptr);{m::Scope close(m::Shutdown);}
  auto bounded=std::make_unique<m::Measurement>();bounded->Start(1);for(size_t i=0;i<m::Measurement::Capacity+1;i++){bounded->Begin(i+1);bounded->End();}Check(bounded->overflow&&bounded->count==m::Measurement::Capacity,"measurement overflow explicit and bounded");
  const auto produced=h[8];Check(h[12]==0&&h[13]==0,"native producer faults");m::recorder.reset();InterlockedExchange64(h+14,1);Check(WaitForSingleObject(pi.hProcess,15000)==WAIT_OBJECT_0,"observer drain");DWORD code;GetExitCodeProcess(pi.hProcess,&code);Check(code==0,"observer success");Check(h[10]==produced,"native prefix durably drained");
  GetProcessTimes(GetCurrentProcess(),&created,&exit,&kernel,&user);auto ft=[](FILETIME f){return(uint64_t(f.dwHighDateTime)<<32)|f.dwLowDateTime;};std::sort(times.begin(),times.end());
  std::ofstream report(root/"native-mock.json");report<<"{\"judgment\":\"PASS\",\"frames\":1800,\"p50us\":"<<times[900]<<",\"p95us\":"<<times[1710]<<",\"p99us\":"<<times[1782]<<",\"maxus\":"<<times.back()<<",\"producerCpuMs\":"<<double(ft(kernel)+ft(user)-ft(sk)-ft(su))/10000<<",\"produced\":"<<produced<<",\"durable\":"<<h[10]<<"}\n";
  std::printf("PASS native transport no Vulkan; produced/durable=%lld; p99=%.3fus\n",produced,times[1782]);CloseHandle(pi.hThread);CloseHandle(pi.hProcess);UnmapViewOfFile(h);CloseHandle(mapping);return 0;
}catch(const std::exception& e){std::fprintf(stderr,"FAIL %s\n",e.what());return 1;}}
