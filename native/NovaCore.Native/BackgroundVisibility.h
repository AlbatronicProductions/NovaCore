#pragma once
#include <cstdint>

// Performance advice only: both branches draw the complete unchanged scene.
// A dense far mesh can cost more to draw twice than the hidden sky saves.
// The completed indirect count is read only after its existing frame fence;
// a camera change can make this advice stale but cannot change pixel ownership.
inline bool BackgroundDepthPrepassWorthwhile(uint32_t indices,uint32_t width,uint32_t height){
  return indices!=0 && uint64_t(indices)*64 <= uint64_t(width)*height*3;
}
