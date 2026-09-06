#pragma once
#include <array>
#include <cstdint>

namespace nc::facility {
// One authored source for raster meshes and analytical caster silhouettes.
// Primitive order, dimensions, colors and the existing mesh winding are unchanged.
struct Box { std::array<float,3> minimum, maximum, color; };
inline constexpr uint32_t GeometryVersion=1, MaximumCasters=8;
inline constexpr std::array<Box,4> LaunchPadBoxes{{
  {{-32,-24,0},{32,24,1.5f},{.34f,.37f,.40f}},
  {{-7,-7,1.5f},{7,7,8.5f},{.48f,.50f,.52f}},
  {{28,-5,1.5f},{50,5,3},{.82f,.33f,.10f}},
  {{-2,18,1.5f},{2,38,2.6f},{.18f,.55f,.86f}}
}};
inline constexpr Box FoundationUnit{{-.5f,-.5f,-1},{.5f,.5f,0},{.34f,.37f,.40f}};
}
