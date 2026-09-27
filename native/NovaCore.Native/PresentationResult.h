#pragma once
#include <vulkan/vulkan.h>

namespace nc {
enum class PresentationResult { Complete, Recreate, Failed };

// Window changes must never hide a device failure returned by presentation.
constexpr PresentationResult ClassifyPresentation(VkResult result, bool resized) {
  if (result != VK_SUCCESS && result != VK_SUBOPTIMAL_KHR &&
      result != VK_ERROR_OUT_OF_DATE_KHR)
    return PresentationResult::Failed;
  return resized || result != VK_SUCCESS ? PresentationResult::Recreate
                                        : PresentationResult::Complete;
}
}
