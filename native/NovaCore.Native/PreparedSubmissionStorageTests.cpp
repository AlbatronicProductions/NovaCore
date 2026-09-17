#include "PreparedSubmissionStorage.h"
#include <array>
#include <iostream>

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
    std::cout << "PREPARED_SUBMISSION PASS counts=37,38,39,37,54 bytes=4416 overflow=refused canary=unchanged recreate=retained\n";
}
