"""Generate CPU Vulkan fakes from installed public API declarations. Never load a driver."""
import argparse,pathlib,re
p=argparse.ArgumentParser();p.add_argument('--header',type=pathlib.Path,required=True);p.add_argument('--output',type=pathlib.Path,required=True);a=p.parse_args()
root=pathlib.Path(__file__).resolve().parents[2];wrappers=(root/'native/NovaCore.Native/OrdinaryNative.inl').read_text();api=a.header.read_text()
names='AllocateMemory FreeMemory CreateBuffer DestroyBuffer CreateImage DestroyImage BindBufferMemory BindImageMemory MapMemory UnmapMemory GetDeviceQueue DestroyCommandPool AllocateCommandBuffers FreeCommandBuffers BeginCommandBuffer EndCommandBuffer CreateFence DestroyFence CreateSwapchainKHR DestroySwapchainKHR AcquireNextImageKHR QueueSubmit QueuePresentKHR WaitForFences DeviceWaitIdle QueueWaitIdle DestroyDevice'.split()
text=['#include <vulkan/vulkan.h>','template<class T>T FakeHandle(uint64_t h){return (T)(uintptr_t)h;}','uint64_t rawCalls=0,rawHandle=1000;bool rawAcquireOutOfDate=false;VkResult rawSubmitResult=VK_SUCCESS;char rawMapped[64]{};']
create={'AllocateMemory':('VkDeviceMemory','pMemory'),'CreateBuffer':('VkBuffer','pBuffer'),'CreateImage':('VkImage','pImage'),'CreateFence':('VkFence','pFence'),'CreateSwapchainKHR':('VkSwapchainKHR','pSwapchain')}
for name in names:
    match=re.search(r'VKAPI_ATTR\s+(VkResult|void)\s+VKAPI_CALL\s+vk'+name+r'\s*\((.*?)\);',api,re.S);assert match,name
    body='if(nc::minimum::recorder&&nc::minimum::recorder->measurement.depth!=0)throw std::runtime_error("raw Vulkan call inside producer timing scope");++rawCalls;'
    if name in create:
        t,v=create[name];body+=f'*{v}=FakeHandle<{t}>(++rawHandle);'
    if name=='MapMemory':body+='*ppData=rawMapped;'
    if name=='GetDeviceQueue':body+='*pQueue=FakeHandle<VkQueue>(0x90);'
    if name=='AllocateCommandBuffers':body+='for(uint32_t i=0;i<pAllocateInfo->commandBufferCount;i++)pCommandBuffers[i]=FakeHandle<VkCommandBuffer>(++rawHandle);'
    if name=='AcquireNextImageKHR':body+='if(rawAcquireOutOfDate)return VK_ERROR_OUT_OF_DATE_KHR;*pImageIndex=0;'
    if match[1]=='VkResult':body+='return rawSubmitResult;' if name=='QueueSubmit' else 'return VK_SUCCESS;'
    text.append(f'VKAPI_ATTR {match[1]} VKAPI_CALL vk{name}({match[2]})'+'{'+body+'}')
text+=['struct Mesh{VkBuffer vb{},ib{};};','struct App {']
meshes=set('triangle floridaLaunchPad floridaLaunchFoundation contactQualificationBody contactQualificationSupport floridaSupportSlab planetaryPatch distantPlanetary stellarSun planetaryRing'.split())
fields=set(re.findall(r'(?:a\.|gApp->)(\w+)',wrappers))
for f in sorted(fields):
    if f in meshes:text.append(f'Mesh {f}{{}};')
    elif f.endswith('Buffer'):text.append(f'VkBuffer {f}{{}};')
    elif f in ('graphicsQueue','presentQueue'):text.append(f'VkQueue {f}{{}};')
    elif f=='swapchain':text.append('VkSwapchainKHR swapchain{};')
    elif f in ('sceneColor','sceneDepth'):text.append(f'VkImage {f}{{}};')
    elif f=='productionImages':text.append('std::array<VkImage,3> productionImages{};')
    elif f=='exhaustTargets':text.append('VkImage exhaustTargets[2]{};')
    elif f=='regionalPreparation':text.append('struct Job{uint64_t generation{};};Job regionalPreparation[2]{};')
    else:text.append(f'uint64_t {f}{{}};')
text+=['};','App* gApp{};','#include "OrdinaryNative.inl"']
text+=['VkDeviceMemory seedMemory{};','void SeedMinimumFixture(App& a){',
 'auto buffer=[&](VkBuffer& b){VkBufferCreateInfo bi{VK_STRUCTURE_TYPE_BUFFER_CREATE_INFO};bi.size=8192;bi.usage=VK_BUFFER_USAGE_STORAGE_BUFFER_BIT;vkCreateBuffer({},&bi,nullptr,&b);VkMemoryAllocateInfo mi{VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO};mi.allocationSize=8192;vkAllocateMemory({},&mi,nullptr,&seedMemory);vkBindBufferMemory({},b,seedMemory,0);void* data;vkMapMemory({},seedMemory,0,8192,0,&data);};',
 'auto image=[&](VkImage& im){VkImageCreateInfo ci{VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO};ci.extent={8,8,1};vkCreateImage({},&ci,nullptr,&im);VkMemoryAllocateInfo mi{VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO};mi.allocationSize=8192;vkAllocateMemory({},&mi,nullptr,&seedMemory);vkBindImageMemory({},im,seedMemory,0);};']
for f in sorted(fields):
    if f.endswith('Buffer'):text.append(f'buffer(a.{f});')
    if f in meshes:text.append(f'buffer(a.{f}.vb);buffer(a.{f}.ib);')
text+=['image(a.sceneColor);image(a.sceneDepth);for(auto& i:a.productionImages)image(i);for(auto& i:a.exhaustTargets)image(i);','}']
a.output.write_text('\n'.join(text)+'\n')
