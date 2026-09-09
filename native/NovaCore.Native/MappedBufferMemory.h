#pragma once
#include <vulkan/vulkan.h>

namespace nc {
enum class MappedBufferUse { Host, TerrainGpuWorkingSet, TerrainRequestKeys };
constexpr uint32_t NoMappedMemoryType=~0u;
struct MappedMemoryTypes { uint32_t preferred=NoMappedMemoryType,fallback=NoMappedMemoryType; };

// Mapping/coherence are requirements. GPU locality and CPU read caching are
// role-specific preferences, never new device requirements.
inline MappedMemoryTypes SelectMappedMemoryTypes(const VkPhysicalDeviceMemoryProperties& properties,
    uint32_t compatibleBits,MappedBufferUse use){
  MappedMemoryTypes result;
  constexpr auto required=VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT|VK_MEMORY_PROPERTY_HOST_COHERENT_BIT;
  for(uint32_t i=0;i<properties.memoryTypeCount;i++){
    const auto flags=properties.memoryTypes[i].propertyFlags;
    if(!(compatibleBits&(1u<<i))||(flags&required)!=required)continue;
    if(result.fallback==NoMappedMemoryType)result.fallback=i;
    const bool preferred=(use==MappedBufferUse::TerrainGpuWorkingSet&&(flags&VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT))||
        (use==MappedBufferUse::TerrainRequestKeys&&(flags&VK_MEMORY_PROPERTY_HOST_CACHED_BIT));
    if(preferred&&result.preferred==NoMappedMemoryType)result.preferred=i;
  }
  return result;
}
inline bool MappedMemoryCapacityFailure(VkResult result){
  return result==VK_ERROR_OUT_OF_DEVICE_MEMORY||result==VK_ERROR_OUT_OF_HOST_MEMORY||result==VK_ERROR_MEMORY_MAP_FAILED;
}
// Attempt must release any tentative allocation/mapping on failure. A different
// compatible host type may recover a limited BAR/local heap; device/validation
// errors remain fatal and the same UMA type is never retried twice.
template<class Attempt> VkResult AllocateMappedMemory(MappedMemoryTypes types,Attempt&& attempt){
  if(types.preferred!=NoMappedMemoryType){
    const VkResult result=attempt(types.preferred);
    if(result==VK_SUCCESS||!MappedMemoryCapacityFailure(result)||types.fallback==types.preferred||types.fallback==NoMappedMemoryType)return result;
  }
  return types.fallback==NoMappedMemoryType?VK_ERROR_FEATURE_NOT_PRESENT:attempt(types.fallback);
}
}
