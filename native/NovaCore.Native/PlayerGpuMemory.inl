// Included after Suitable(): configuration and renderer use the same admission
// policy. This owner creates no logical device, queues, allocations or GPU work.
// The application renderer borrows this instance, so the query and allocation
// physical-device handles have exactly the same driver ownership domain.
class PlayerGpuMemory {
  std::mutex mutex;
  uint64_t nextSample=0;
  DWORD ownerThread=GetCurrentThreadId();
  NcPlayerGpuMemorySnapshot published{};
  uint64_t sequence=0;
  nc::startup::Channel admission;
  PFN_vkGetPhysicalDeviceMemoryProperties2 query=nullptr;
  std::ofstream trace;
  static uint64_t Clock(){LARGE_INTEGER t;QueryPerformanceCounter(&t);return t.QuadPart;}
  void Publish(NcPlayerGpuMemorySnapshot s){
    s.size=sizeof(s);s.version=1;s.sequence=++sequence;s.qpc=Clock();
    LARGE_INTEGER f;QueryPerformanceFrequency(&f);s.frequency=f.QuadPart;
    FILETIME ft;GetSystemTimePreciseAsFileTime(&ft);s.utcFileTime=(uint64_t(ft.dwHighDateTime)<<32)|ft.dwLowDateTime;
    {std::lock_guard lock(mutex);published=s;}
    // Optional qualification evidence, never a render ring.
    if(trace){
      trace<<s.sequence<<','<<s.qpc<<','<<s.frequency<<','<<s.utcFileTime<<','<<s.status<<','<<s.queryNanoseconds<<','<<s.usageBytes<<','<<s.budgetBytes<<','<<s.capacityBytes<<','<<s.localHeapMask<<','<<s.vendorId<<','<<s.deviceId<<',';
      static constexpr char hex[]="0123456789ABCDEF";
      for(auto b:s.uuid)trace<<hex[b>>4]<<hex[b&15];trace<<',';
      for(auto b:s.luid)trace<<hex[b>>4]<<hex[b&15];trace<<','<<s.luidValid<<','<<s.heapCount;
      for(uint32_t i=0;i<16;i++)trace<<','<<s.heapFlags[i]<<','<<s.heapCapacity[i]<<','<<s.heapBudget[i]<<','<<s.heapUsage[i];
      trace<<'\n';trace.flush();
    }
  }
  void Failure(uint32_t status,const char* reason){
    auto s=Read();s.status=status;s.queryNanoseconds=0;s.usageBytes=s.budgetBytes=s.capacityBytes=0;
    std::fill(std::begin(s.heapUsage),std::end(s.heapUsage),0);std::fill(std::begin(s.heapBudget),std::end(s.heapBudget),0);
    std::snprintf(s.detail,sizeof(s.detail),"%s",reason);Publish(s);
  }
  void Sample(){
    admission.CheckAllowed();auto s=Read();
    VkPhysicalDeviceMemoryBudgetPropertiesEXT budget{VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_MEMORY_BUDGET_PROPERTIES_EXT};
    VkPhysicalDeviceMemoryProperties2 properties{VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_MEMORY_PROPERTIES_2};properties.pNext=&budget;
    const auto begin=Clock();query(physical,&properties);const auto end=Clock();
    s.queryNanoseconds=uint64_t(double(end-begin)*1e9/double(s.frequency));
    s.heapCount=properties.memoryProperties.memoryHeapCount;s.localHeapMask=0;
    s.usageBytes=s.budgetBytes=s.capacityBytes=0;
    for(uint32_t i=0;i<s.heapCount;i++){
      const auto& heap=properties.memoryProperties.memoryHeaps[i];
      s.heapUsage[i]=budget.heapUsage[i];s.heapBudget[i]=budget.heapBudget[i];s.heapCapacity[i]=heap.size;s.heapFlags[i]=heap.flags;
      if(heap.flags&VK_MEMORY_HEAP_DEVICE_LOCAL_BIT){s.localHeapMask|=1u<<i;s.usageBytes+=budget.heapUsage[i];s.budgetBytes+=budget.heapBudget[i];s.capacityBytes+=heap.size;}
    }
    if(!s.localHeapMask){Failure(2,"Selected adapter has no device-local heap");return;}
    s.status=1;std::snprintf(s.detail,sizeof(s.detail),"VK_EXT_memory_budget; process device-local estimates");Publish(s);
  }
public:
  VkInstance instance=nullptr;
  VkPhysicalDevice physical=nullptr;
  std::array<uint8_t,16> uuid{};
  bool selected=false,supported=false;
  bool OnOwnerThread()const{return GetCurrentThreadId()==ownerThread;}
  NcPlayerGpuMemorySnapshot Read(){std::lock_guard lock(mutex);return published;}
  void Start(HWND parent){
    if(const char* path=std::getenv("NOVACORE_GPU_MEMORY_SAMPLES")){
      trace.open(path);if(trace){trace<<"sequence,qpc,frequency,utcFileTime,status,queryNs,usageBytes,budgetBytes,capacityBytes,localHeapMask,vendor,device,uuid,luid,luidValid,heapCount";
        for(uint32_t i=0;i<16;i++)trace<<",h"<<i<<"Flags,h"<<i<<"Capacity,h"<<i<<"Budget,h"<<i<<"Usage";trace<<'\n';}
    }
    NcPlayerGpuMemorySnapshot initial{};initial.status=5;Publish(initial);
    VkSurfaceKHR surface=nullptr;
    try{
      admission.Open();admission.CheckAllowed();
      VkApplicationInfo app{VK_STRUCTURE_TYPE_APPLICATION_INFO};app.pApplicationName="NovaCore memory telemetry";app.apiVersion=VK_API_VERSION_1_1;
      const char* extensions[]{VK_KHR_SURFACE_EXTENSION_NAME,VK_KHR_WIN32_SURFACE_EXTENSION_NAME,VK_EXT_DEBUG_UTILS_EXTENSION_NAME};
      VkInstanceCreateInfo create{VK_STRUCTURE_TYPE_INSTANCE_CREATE_INFO};create.pApplicationInfo=&app;create.enabledExtensionCount=3;create.ppEnabledExtensionNames=extensions;
#ifdef NC_DEBUG_BUILD
      const char* layer="VK_LAYER_KHRONOS_validation";create.enabledLayerCount=1;create.ppEnabledLayerNames=&layer;
#endif
      if(vkCreateInstance(&create,nullptr,&instance)!=VK_SUCCESS)throw std::runtime_error("Vulkan 1.1 telemetry discovery failed");
      VkWin32SurfaceCreateInfoKHR surfaceInfo{VK_STRUCTURE_TYPE_WIN32_SURFACE_CREATE_INFO_KHR};surfaceInfo.hinstance=GetModuleHandleW(nullptr);surfaceInfo.hwnd=parent;
      if(vkCreateWin32SurfaceKHR(instance,&surfaceInfo,nullptr,&surface)!=VK_SUCCESS)throw std::runtime_error("Adapter presentation discovery failed");
      uint32_t count=0;if(vkEnumeratePhysicalDevices(instance,&count,nullptr)!=VK_SUCCESS)throw std::runtime_error("Adapter enumeration failed");
      std::vector<VkPhysicalDevice> devices(count);if(vkEnumeratePhysicalDevices(instance,&count,devices.data())!=VK_SUCCESS)throw std::runtime_error("Adapter enumeration changed");
      for(auto d:devices)if(Suitable(d,surface)){physical=d;break;}
      vkDestroySurfaceKHR(instance,surface,nullptr);surface=nullptr;
      if(!physical)throw std::runtime_error("No suitable Vulkan rendering adapter");
      VkPhysicalDeviceIDProperties id{VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_ID_PROPERTIES};
      VkPhysicalDeviceProperties2 properties{VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_PROPERTIES_2};properties.pNext=&id;vkGetPhysicalDeviceProperties2(physical,&properties);
      std::copy(std::begin(id.deviceUUID),std::end(id.deviceUUID),uuid.begin());selected=true;
      auto s=Read();std::memcpy(s.uuid,id.deviceUUID,16);std::memcpy(s.luid,id.deviceLUID,8);s.luidValid=id.deviceLUIDValid;
      s.vendorId=properties.properties.vendorID;s.deviceId=properties.properties.deviceID;s.driverVersion=properties.properties.driverVersion;
      std::snprintf(s.adapterName,sizeof(s.adapterName),"%s",properties.properties.deviceName);Publish(s);
      if(vkEnumerateDeviceExtensionProperties(physical,nullptr,&count,nullptr)!=VK_SUCCESS)throw std::runtime_error("Budget capability enumeration failed");
      std::vector<VkExtensionProperties> deviceExtensions(count);if(vkEnumerateDeviceExtensionProperties(physical,nullptr,&count,deviceExtensions.data())!=VK_SUCCESS)throw std::runtime_error("Budget capability enumeration changed");
      supported=std::any_of(deviceExtensions.begin(),deviceExtensions.end(),[](const auto& e){return std::strcmp(e.extensionName,VK_EXT_MEMORY_BUDGET_EXTENSION_NAME)==0;});
      if(!supported){Failure(2,"VK_EXT_memory_budget unsupported on selected adapter");return;}
      query=(PFN_vkGetPhysicalDeviceMemoryProperties2)vkGetInstanceProcAddr(instance,"vkGetPhysicalDeviceMemoryProperties2");
      if(!query)throw std::runtime_error("Vulkan memory-properties entry point missing");
      Poll();
    }catch(const nc::startup::Cancelled&){Failure(6,"Telemetry refused: recorder admission unavailable");}
    catch(const std::exception& e){Failure(3,e.what());}
    catch(...){Failure(3,"Memory telemetry initialization failed");}
    if(surface)vkDestroySurfaceKHR(instance,surface,nullptr);
  }
  bool Matches(VkPhysicalDevice candidate)const{
    VkPhysicalDeviceIDProperties id{VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_ID_PROPERTIES};
    VkPhysicalDeviceProperties2 properties{VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_PROPERTIES_2};properties.pNext=&id;vkGetPhysicalDeviceProperties2(candidate,&properties);
    return std::equal(uuid.begin(),uuid.end(),id.deviceUUID);
  }
  void Poll(){
    // WinForms timer and actual loading checkpoints run on the renderer's owned
    // UI thread. No paint query, concurrent driver call, or debug-callback writer.
    if(GetCurrentThreadId()!=ownerThread)throw std::runtime_error("Memory telemetry thread ownership mismatch");
    if(!query||GetTickCount64()<nextSample||Read().status==6)return;
    nextSample=GetTickCount64()+1000;
    try{Sample();}
    catch(const nc::startup::Cancelled&){Failure(6,"Telemetry stopped: recorder admission revoked");}
    catch(const std::exception& e){Failure(3,e.what());}
    catch(...){Failure(3,"Memory query failed");}
  }
  ~PlayerGpuMemory(){
    if(instance)vkDestroyInstance(instance,nullptr);
  }
};
std::unique_ptr<PlayerGpuMemory> playerGpuMemory;
bool PlayerGpuTelemetryActive(){return playerGpuMemory&&playerGpuMemory->selected;}
VkInstance PlayerGpuInstance(){
  if(!PlayerGpuTelemetryActive())return nullptr;
  if(!playerGpuMemory->OnOwnerThread())throw std::runtime_error("GPU instance borrow thread mismatch");
  return playerGpuMemory->instance;
}
bool ReadPlayerGpuMemory(NcPlayerGpuMemorySnapshot& s){if(!playerGpuMemory)return false;s=playerGpuMemory->Read();return true;}
