#include "PresentationResult.h"
#include <cstdio>
#include <cstdlib>
#include <initializer_list>

int main() {
  unsigned cases = 0;
  auto expect = [&](VkResult result, bool resized, nc::PresentationResult expected) {
    ++cases;
    if (nc::ClassifyPresentation(result, resized) != expected) {
      std::fprintf(stderr, "presentation result %d, resized=%d misclassified\n", int(result), resized);
      std::exit(1);
    }
  };
  expect(VK_SUCCESS, false, nc::PresentationResult::Complete);
  expect(VK_SUCCESS, true, nc::PresentationResult::Recreate);
  for (bool resized : {false, true}) {
    expect(VK_SUBOPTIMAL_KHR, resized, nc::PresentationResult::Recreate);
    expect(VK_ERROR_OUT_OF_DATE_KHR, resized, nc::PresentationResult::Recreate);
    for (VkResult failure : {VK_ERROR_DEVICE_LOST, VK_ERROR_SURFACE_LOST_KHR,
         VK_ERROR_OUT_OF_HOST_MEMORY, VK_ERROR_OUT_OF_DEVICE_MEMORY,
         VK_ERROR_UNKNOWN, VK_TIMEOUT})
      expect(failure, resized, nc::PresentationResult::Failed);
  }
  std::printf("PASS %u presentation outcomes; no Vulkan device created\n", cases);
}
