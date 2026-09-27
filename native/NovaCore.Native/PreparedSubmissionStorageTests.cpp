#include "PreparedSubmissionStorage.h"
#include <array>
#include <iostream>
#include <vector>

int main() {
    static_assert(sizeof(NcCameraData) == 96 && sizeof(NcRenderObject) == 80);
    constexpr uint32_t capacity = 54;
    constexpr size_t bytes = 96 + capacity * 80;
    std::array<unsigned char, bytes + 32> guarded{};
    guarded.fill(0xDA);
    std::array<NcRenderObject, capacity + 1> objects{};
    for (uint32_t i = 0; i < objects.size(); ++i) objects[i].mesh.value = i + 1000;
    NcFrameSubmission source{}; source.objects = objects.data();
    for (const uint32_t count : {37u, 38u, 39u, 37u, 54u}) {
        source.objectCount = count;
        nc::CopyPreparedSubmission(guarded.data(), bytes, capacity, source);
        if (std::memcmp(guarded.data() + 96, objects.data(), count * 80)) return 1;
        for (size_t i = bytes; i < guarded.size(); ++i) if (guarded[i] != 0xDA) return 2;
    }
    const auto before = guarded;
    source.objectCount = 55;
    try { nc::CopyPreparedSubmission(guarded.data(), bytes, capacity, source); return 3; }
    catch (const std::runtime_error&) {}
    if (before != guarded) return 4;
    // Resize/recreation reuses prepared capacity, independent of idle active length.
    source.objectCount = 37;
    std::array<unsigned char, bytes> recreated{};
    nc::CopyPreparedSubmission(recreated.data(), nc::SubmissionBytes(capacity), capacity, source);
    source.objectCount = 39;
    nc::CopyPreparedSubmission(recreated.data(), nc::SubmissionBytes(capacity), capacity, source);
    if (std::memcmp(recreated.data() + 96, objects.data(), 39 * 80)) return 5;
    // Cross the retired 4096-object limit, the current editor bound, and a
    // larger catalog envelope. Test exact byte/device boundaries independently.
    for(const uint32_t n : {4095u,4096u,4101u,18585u,34080u}) {
        const uint32_t size=96+n*80;
        if(!nc::PreparedCapacityFits(n,n,size)||nc::PreparedCapacityFits(n,n,size-1)||
           nc::PreparedCapacityFits(n,n+1,size)||nc::PreparedCapacityFits(0,0,size)||
           nc::PreparedCapacityFits(UINT32_MAX,1,UINT32_MAX))return 6;
        std::vector<NcRenderObject> input(n+1);input[n-1].mesh.value=0xABCDEF;
        std::vector<unsigned char> output(size+32,0xDA);
        source.objects=input.data();source.objectCount=n;
        nc::CopyPreparedSubmission(output.data(),size,n,source);
        if(std::memcmp(output.data()+96,input.data(),n*80))return 7;
        for(size_t i=size;i<output.size();++i)if(output[i]!=0xDA)return 8;
        const auto intact=output;source.objectCount=n+1;
        try{nc::CopyPreparedSubmission(output.data(),size,n,source);return 9;}catch(const std::runtime_error&){}
        if(output!=intact)return 10;
    }
    std::cout << "EDITOR_SUBMISSION PASS active=4101 prepared=18585 larger=34080 deviceBoundary=exact canary=unchanged refusal=atomic\n";
    std::cout << "PREPARED_SUBMISSION PASS counts=37,38,39,37,54 bytes=4416 overflow=refused canary=unchanged recreate=retained\n";
}
