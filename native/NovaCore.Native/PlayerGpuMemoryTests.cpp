// Deterministic driver seam: exercise the production owner without a GPU/device.
#include <windows.h>
#include <vulkan/vulkan.h>
#include <algorithm>
#include <array>
#include <chrono>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <fstream>
#include <memory>
#include <mutex>
#include <stdexcept>
#include <string>
#include <vector>
#include "StartupLifecycle.h"
#include "PlayerGpuMemory.h"
static int instances,destroyed,surfaces,surfaceDestroyed,queries;
static bool supported=true,failQuery=false,emptyHeaps=false,missingEntry=false;
static ULONGLONG testTick=1000;
static ULONGLONG TestTick(){return testTick;}
static VkResult Instance(const VkInstanceCreateInfo*,const VkAllocationCallbacks*,VkInstance* p){*p=(VkInstance)1;++instances;return VK_SUCCESS;}
static void DestroyInstance(VkInstance,const VkAllocationCallbacks*){++destroyed;}
static VkResult Surface(VkInstance,const VkWin32SurfaceCreateInfoKHR*,const VkAllocationCallbacks*,VkSurfaceKHR* p){*p=(VkSurfaceKHR)2;++surfaces;return VK_SUCCESS;}
static void DestroySurface(VkInstance,VkSurfaceKHR,const VkAllocationCallbacks*){++surfaceDestroyed;}
static VkResult Devices(VkInstance,uint32_t* n,VkPhysicalDevice* p){*n=1;if(p)*p=(VkPhysicalDevice)3;return VK_SUCCESS;}
static bool Suitable(VkPhysicalDevice,VkSurfaceKHR){return true;}
static void Properties(VkPhysicalDevice,VkPhysicalDeviceProperties2* p){
  auto* id=(VkPhysicalDeviceIDProperties*)p->pNext;id->deviceUUID[0]=42;id->deviceLUIDValid=VK_TRUE;
  std::strcpy(p->properties.deviceName,"test driver");
}
static VkResult Extensions(VkPhysicalDevice,const char*,uint32_t* n,VkExtensionProperties* p){*n=supported?1:0;if(p&&supported)std::strcpy(p->extensionName,VK_EXT_MEMORY_BUDGET_EXTENSION_NAME);return VK_SUCCESS;}
static void Memory(VkPhysicalDevice,VkPhysicalDeviceMemoryProperties2* p){
  ++queries;if(failQuery)throw std::runtime_error("injected query failure");
  auto& m=p->memoryProperties;m.memoryHeapCount=emptyHeaps?0:3;
  m.memoryHeaps[0]={1024,VK_MEMORY_HEAP_DEVICE_LOCAL_BIT};m.memoryHeaps[1]={2048,0};m.memoryHeaps[2]={4096,VK_MEMORY_HEAP_DEVICE_LOCAL_BIT};
  m.memoryTypeCount=4;m.memoryTypes[0]={1,0};m.memoryTypes[1]={3,0};m.memoryTypes[2]={2,1};m.memoryTypes[3]={1,2};
  auto* b=(VkPhysicalDeviceMemoryBudgetPropertiesEXT*)p->pNext;
  b->heapBudget[0]=900;b->heapBudget[1]=1500;b->heapBudget[2]=3000;
  b->heapUsage[0]=100;b->heapUsage[1]=200;b->heapUsage[2]=300;
}
static PFN_vkVoidFunction Entry(VkInstance,const char*){return missingEntry?nullptr:(PFN_vkVoidFunction)&Memory;}
#define vkCreateInstance Instance
#define vkDestroyInstance DestroyInstance
#define vkCreateWin32SurfaceKHR Surface
#define vkDestroySurfaceKHR DestroySurface
#define vkEnumeratePhysicalDevices Devices
#define vkGetPhysicalDeviceProperties2 Properties
#define vkEnumerateDeviceExtensionProperties Extensions
#define vkGetInstanceProcAddr Entry
#define GetTickCount64 TestTick
#include "PlayerGpuMemory.inl"
#undef GetTickCount64
static int checks;
static void Check(bool v,const char* what){++checks;if(!v)throw std::runtime_error(what);}
int main(){
  try{
    SetEnvironmentVariableW(L"NOVACORE_STARTUP_MAPPING",nullptr);SetEnvironmentVariableW(L"NOVACORE_GPU_MEMORY_SAMPLES",nullptr);
    {PlayerGpuMemory owner;owner.Start((HWND)1);auto s=owner.Read();
      Check(s.status==1,"ready");Check(s.heapCount==3&&s.localHeapMask==5,"include each local heap once");
      Check(s.usageBytes==400&&s.budgetBytes==3900&&s.capacityBytes==5120,"raw aggregation independent of memory types");
      Check(s.heapUsage[1]==200&&s.heapFlags[1]==0,"excluded nonlocal retained in receipt");
      Check(s.uuid[0]==42&&owner.Matches((VkPhysicalDevice)3),"identity binding");
      Check(s.qpc&&s.frequency&&s.utcFileTime,"timestamped sample");
      auto q=queries;owner.Poll();owner.Read();Check(queries==q,"one cadence and cache-only read");
      failQuery=true;testTick+=1000;owner.Poll();auto failed=owner.Read();
      Check(failed.status==3&&failed.sequence>s.sequence&&failed.usageBytes==0&&failed.budgetBytes==0&&failed.capacityBytes==0,"failure clears previously live aggregate");
      Check(failed.heapUsage[0]==0&&failed.heapUsage[2]==0&&failed.heapBudget[0]==0&&failed.heapBudget[2]==0,"failure clears stale heap estimates");
      failQuery=false;testTick+=1000;owner.Poll();Check(owner.Read().status==1&&owner.Read().usageBytes==400,"bounded retry recovers fresh sample");
    }
    Check(instances==destroyed&&surfaces==surfaceDestroyed,"normal cleanup exactly once");
    supported=false;{PlayerGpuMemory owner;owner.Start((HWND)1);Check(owner.Read().status==2,"unsupported explicit");}supported=true;
    failQuery=true;{PlayerGpuMemory owner;owner.Start((HWND)1);auto s=owner.Read();Check(s.status==3&&s.usageBytes==0&&s.budgetBytes==0,"failed query cannot publish valid values");}failQuery=false;
    missingEntry=true;{PlayerGpuMemory owner;owner.Start((HWND)1);Check(owner.Read().status==3,"missing function explicit");}missingEntry=false;
    emptyHeaps=true;{PlayerGpuMemory owner;owner.Start((HWND)1);Check(owner.Read().status==2,"no local heaps unsupported");}emptyHeaps=false;
    Check(instances==destroyed&&surfaces==surfaceDestroyed,"all failure paths clean up");
    std::printf("PASS GPU memory owner: %d checks; mock driver; zero GPU/device creation\n",checks);return 0;
  }catch(const std::exception& e){std::fprintf(stderr,"FAIL %s\n",e.what());return 1;}
}
