#pragma once
#include "NovaCoreNative.h"
#include <cstring>
#include <stdexcept>

namespace nc {
inline uint64_t SubmissionBytes(uint32_t capacity) {
    return sizeof(NcCameraData) + uint64_t(sizeof(NcRenderObject)) * capacity;
}
// Preparation owns capacity; each frame supplies only its active length.
// Check before either copy so refusal cannot partially overwrite a frame.
inline void CopyPreparedSubmission(void* destination, uint64_t bytes,
                                   uint32_t capacity, const NcFrameSubmission& source) {
    if (!destination || source.objectCount > capacity || bytes != SubmissionBytes(capacity) ||
        (source.objectCount && !source.objects))
        throw std::runtime_error("render submission exceeds prepared capacity");
    std::memcpy(destination, &source.camera, sizeof(NcCameraData));
    if (source.objectCount)
        std::memcpy(static_cast<char*>(destination) + sizeof(NcCameraData), source.objects,
                    sizeof(NcRenderObject) * source.objectCount);
}
}
