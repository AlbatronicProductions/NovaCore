#pragma once

#include <array>
#include <compare>
#include <cstdint>
#include <string>
#include <vector>

namespace nc::production {

constexpr uint64_t EarthBodyId = 6;
constexpr uint32_t HeaderBytes = 256;
constexpr uint32_t RecordHeaderBytes = 96;
constexpr uint32_t InteriorTexels = 256;
constexpr uint32_t GutterTexels = 4;
constexpr uint32_t StoredExtent = InteriorTexels + 2 * GutterTexels;
constexpr uint32_t AlbedoBytes = StoredExtent * StoredExtent * 3;
constexpr uint32_t ElevationBytes = StoredExtent * StoredExtent * 2;
constexpr uint32_t LandBytes = StoredExtent * StoredExtent;
constexpr uint32_t CloudBytes = StoredExtent * StoredExtent;

struct PatchId {
  uint64_t bodyId{};
  uint32_t terrainVersion{};
  uint32_t face{}, level{}, x{}, y{};
  auto operator<=>(const PatchId &) const = default;
};

struct Record {
  PatchId id{};
  uint64_t ordinal{}, offset{};
  std::array<uint32_t, 4> bytes{};
  std::array<uint8_t, 32> digest{};
};

struct Payload {
  PatchId id{};
  std::vector<uint8_t> albedoRgb;
  std::vector<uint16_t> elevation;
  std::vector<uint8_t> land;
  std::vector<uint8_t> cloud;
  bool digestValid{};
};

class Pack final {
 public:
  bool Open(const std::string &path, std::string &error);
  bool Read(const PatchId &id, Payload &payload, std::string &error) const;
  bool Contains(const PatchId &id) const;
  uint32_t MaximumLevel() const { return maximumLevel_; }
  uint32_t RecordCount() const { return static_cast<uint32_t>(records_.size()); }
  uint32_t TerrainVersion() const { return terrainVersion_; }
  uint64_t BodyId() const { return bodyId_; }
  uint32_t Interior() const { return interior_; }
  uint32_t Gutter() const { return gutter_; }
  uint32_t Extent() const { return extent_; }
  bool IsProductionLayout() const { return bodyId_ == EarthBodyId && terrainVersion_ == 4 && interior_ == InteriorTexels && gutter_ == GutterTexels && extent_ == StoredExtent; }
  const std::string &Path() const { return path_; }

  static uint64_t Ordinal(uint32_t face, uint32_t level, uint32_t x, uint32_t y);

 private:
  const Record *Find(const PatchId &id) const;
  std::string path_;
  uint64_t bodyId_{};
  uint32_t maximumLevel_{}, terrainVersion_{}, interior_{}, gutter_{}, extent_{};
  std::vector<Record> records_;
};

}  // namespace nc::production
