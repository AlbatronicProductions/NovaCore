#pragma once

#include <array>
#include <compare>
#include <cstdint>
#include <string>
#include <vector>

namespace nc::localterrain {

constexpr uint32_t HeaderBytes=256,RecordHeaderBytes=128,InteriorTexels=256,GutterTexels=4,StoredExtent=264;
constexpr uint32_t Bc7Bytes=(StoredExtent/4)*(StoredExtent/4)*16;
constexpr uint32_t Bc4Bytes=(StoredExtent/4)*(StoredExtent/4)*8;
constexpr uint32_t Bc5Bytes=(StoredExtent/4)*(StoredExtent/4)*16;
constexpr uint32_t PayloadVersion=1;

enum class Codec:uint8_t{Raw=0,PackBits=1};

struct SectorId{
  uint64_t bodyId{};uint32_t terrainVersion{};uint32_t face{},level{},x{},y{},detailFrequency{},payloadVersion{};
  auto operator<=>(const SectorId&)const=default;
};
struct Record{
  SectorId id{};uint64_t payloadOffset{};std::array<uint32_t,3> storedBytes{},gpuBytes{};std::array<Codec,3> codecs{};std::array<uint8_t,32> digest{};
};
struct Payload{
  SectorId id{};std::vector<uint8_t> albedoBc7,elevationBc4,normalBc5;uint64_t storedBytes{},transcodedBytes{};double transcodeMilliseconds{};bool digestValid{};
};

class Pack final{
 public:
  bool Open(const std::string& path,std::string& error);
  bool Read(const SectorId&id,Payload&payload,std::string&error)const;
  bool Contains(const SectorId&id)const{return Find(id)!=nullptr;}
  const std::vector<Record>&Records()const{return records_;}
  uint64_t BodyId()const{return bodyId_;}uint32_t TerrainVersion()const{return terrainVersion_;}
  uint32_t MinimumLevel()const{return minimumLevel_;}uint32_t MaximumLevel()const{return maximumLevel_;}
  uint32_t RecordCount()const{return static_cast<uint32_t>(records_.size());}
  float ResidualMinimum()const{return residualMinimum_;}float ResidualMaximum()const{return residualMaximum_;}
  bool IsProductionLayout()const{return bodyId_==6&&terrainVersion_==4&&interior_==InteriorTexels&&gutter_==GutterTexels&&extent_==StoredExtent;}
 private:
  const Record*Find(const SectorId&id)const;
  std::string path_;uint64_t bodyId_{};uint32_t terrainVersion_{},minimumLevel_{},maximumLevel_{},interior_{},gutter_{},extent_{};
  float residualMinimum_{},residualMaximum_{};std::vector<Record>records_;
};

} // namespace nc::localterrain
