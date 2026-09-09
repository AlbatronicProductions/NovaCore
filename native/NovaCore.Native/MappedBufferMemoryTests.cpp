#include "MappedBufferMemory.h"
#include <iostream>
#include <stdexcept>
#include <vector>

int main(){try{
  unsigned checks=0;
  const auto require=[&](bool value){++checks;if(!value)throw std::runtime_error("mapped terrain memory contract failed");};
  constexpr auto host=VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT|VK_MEMORY_PROPERTY_HOST_COHERENT_BIT;
  VkPhysicalDeviceMemoryProperties p{};p.memoryTypeCount=4;
  p.memoryTypes[0].propertyFlags=VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT;
  p.memoryTypes[1].propertyFlags=host|VK_MEMORY_PROPERTY_HOST_CACHED_BIT;
  p.memoryTypes[2].propertyFlags=host|VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT;
  p.memoryTypes[3].propertyFlags=VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT|VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT;
  auto t=nc::SelectMappedMemoryTypes(p,15,nc::MappedBufferUse::TerrainGpuWorkingSet);
  require(t.preferred==2&&t.fallback==1);
  auto ordinary=nc::SelectMappedMemoryTypes(p,15,nc::MappedBufferUse::Host);
  require(ordinary.preferred==nc::NoMappedMemoryType&&ordinary.fallback==1);
  auto excluded=nc::SelectMappedMemoryTypes(p,11,nc::MappedBufferUse::TerrainGpuWorkingSet);
  require(excluded.preferred==nc::NoMappedMemoryType&&excluded.fallback==1);
  auto absent=nc::SelectMappedMemoryTypes(p,9,nc::MappedBufferUse::TerrainGpuWorkingSet);
  require(absent.preferred==nc::NoMappedMemoryType&&absent.fallback==nc::NoMappedMemoryType);
  std::vector<uint32_t> attempts;
  auto run=[&](nc::MappedMemoryTypes types,VkResult preferred,VkResult fallback){
    attempts.clear();return nc::AllocateMappedMemory(types,[&](uint32_t type){attempts.push_back(type);return type==types.preferred?preferred:fallback;});
  };
  require(run(t,VK_SUCCESS,VK_SUCCESS)==VK_SUCCESS&&attempts==std::vector<uint32_t>{2});
  for(auto failure:{VK_ERROR_OUT_OF_DEVICE_MEMORY,VK_ERROR_OUT_OF_HOST_MEMORY,VK_ERROR_MEMORY_MAP_FAILED})
    require(run(t,failure,VK_SUCCESS)==VK_SUCCESS&&attempts==std::vector<uint32_t>({2,1}));
  require(run(t,VK_ERROR_DEVICE_LOST,VK_SUCCESS)==VK_ERROR_DEVICE_LOST&&attempts==std::vector<uint32_t>{2});
  require(run(t,VK_ERROR_OUT_OF_DEVICE_MEMORY,VK_ERROR_OUT_OF_HOST_MEMORY)==VK_ERROR_OUT_OF_HOST_MEMORY&&attempts==std::vector<uint32_t>({2,1}));
  require(run(excluded,VK_SUCCESS,VK_SUCCESS)==VK_SUCCESS&&attempts==std::vector<uint32_t>{1});
  require(run(absent,VK_SUCCESS,VK_SUCCESS)==VK_ERROR_FEATURE_NOT_PRESENT&&attempts.empty());
  auto uma=nc::SelectMappedMemoryTypes(p,4,nc::MappedBufferUse::TerrainGpuWorkingSet);
  require(uma.preferred==2&&uma.fallback==2);
  require(run(uma,VK_ERROR_MEMORY_MAP_FAILED,VK_SUCCESS)==VK_ERROR_MEMORY_MAP_FAILED&&attempts==std::vector<uint32_t>{2});
  // A CPU-read key table prefers caching without weakening coherence or the
  // existing compatible-host fallback, and without changing GPU role selection.
  p.memoryTypeCount=5;
  p.memoryTypes[0].propertyFlags=VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT|VK_MEMORY_PROPERTY_HOST_CACHED_BIT;
  p.memoryTypes[1].propertyFlags=host;
  p.memoryTypes[3].propertyFlags=VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT|VK_MEMORY_PROPERTY_HOST_CACHED_BIT;
  p.memoryTypes[4].propertyFlags=host|VK_MEMORY_PROPERTY_HOST_CACHED_BIT;
  auto keys=nc::SelectMappedMemoryTypes(p,31,nc::MappedBufferUse::TerrainRequestKeys);
  require(keys.preferred==4&&keys.fallback==1);
  auto keyExcluded=nc::SelectMappedMemoryTypes(p,15,nc::MappedBufferUse::TerrainRequestKeys);
  require(keyExcluded.preferred==nc::NoMappedMemoryType&&keyExcluded.fallback==1);
  auto keyAbsent=nc::SelectMappedMemoryTypes(p,9,nc::MappedBufferUse::TerrainRequestKeys);
  require(keyAbsent.preferred==nc::NoMappedMemoryType&&keyAbsent.fallback==nc::NoMappedMemoryType);
  auto keyUma=nc::SelectMappedMemoryTypes(p,16,nc::MappedBufferUse::TerrainRequestKeys);
  require(keyUma.preferred==4&&keyUma.fallback==4);
  auto sameHost=nc::SelectMappedMemoryTypes(p,31,nc::MappedBufferUse::Host);
  require(sameHost.preferred==nc::NoMappedMemoryType&&sameHost.fallback==1);
  auto sameGpu=nc::SelectMappedMemoryTypes(p,31,nc::MappedBufferUse::TerrainGpuWorkingSet);
  require(sameGpu.preferred==2&&sameGpu.fallback==1);
  require(run(keys,VK_SUCCESS,VK_SUCCESS)==VK_SUCCESS&&attempts==std::vector<uint32_t>{4});
  for(auto failure:{VK_ERROR_OUT_OF_DEVICE_MEMORY,VK_ERROR_OUT_OF_HOST_MEMORY,VK_ERROR_MEMORY_MAP_FAILED})
    require(run(keys,failure,VK_SUCCESS)==VK_SUCCESS&&attempts==std::vector<uint32_t>({4,1}));
  for(auto failure:{VK_ERROR_DEVICE_LOST,VK_ERROR_UNKNOWN})
    require(run(keys,failure,VK_SUCCESS)==failure&&attempts==std::vector<uint32_t>{4});
  require(run(keys,VK_ERROR_MEMORY_MAP_FAILED,VK_ERROR_OUT_OF_HOST_MEMORY)==VK_ERROR_OUT_OF_HOST_MEMORY&&attempts==std::vector<uint32_t>({4,1}));
  require(run(keyExcluded,VK_SUCCESS,VK_SUCCESS)==VK_SUCCESS&&attempts==std::vector<uint32_t>{1});
  require(run(keyAbsent,VK_SUCCESS,VK_SUCCESS)==VK_ERROR_FEATURE_NOT_PRESENT&&attempts.empty());
  require(run(keyUma,VK_ERROR_MEMORY_MAP_FAILED,VK_SUCCESS)==VK_ERROR_MEMORY_MAP_FAILED&&attempts==std::vector<uint32_t>{4});
  std::cout<<"PASS mapped terrain memory policy: "<<checks<<" checks\n";return 0;
}catch(const std::exception& error){std::cerr<<"FAIL "<<error.what()<<'\n';return 1;}}
