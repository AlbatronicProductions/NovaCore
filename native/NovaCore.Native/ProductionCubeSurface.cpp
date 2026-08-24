#include "ProductionCubeSurface.h"

#include <algorithm>
#include <windows.h>
#include <bcrypt.h>
#include <cstring>
#include <fstream>
#include <limits>

namespace nc::production {
namespace {
uint32_t Read32(const uint8_t *value) { uint32_t result{}; std::memcpy(&result, value, sizeof result); return result; }
uint64_t Read64(const uint8_t *value) { uint64_t result{}; std::memcpy(&result, value, sizeof result); return result; }

bool Digest(const PatchId &id, const Payload &payload, std::array<uint8_t, 32> &result) {
  std::array<uint8_t, 24> identity{};
  std::memcpy(identity.data(), &id.bodyId, 8);
  std::memcpy(identity.data() + 8, &id.terrainVersion, 4);
  identity[12] = static_cast<uint8_t>(id.face);
  identity[13] = static_cast<uint8_t>(id.level);
  std::memcpy(identity.data() + 16, &id.x, 4);
  std::memcpy(identity.data() + 20, &id.y, 4);
  BCRYPT_ALG_HANDLE algorithm{}; BCRYPT_HASH_HANDLE hash{}; DWORD objectBytes{}, actual{};
  if (BCryptOpenAlgorithmProvider(&algorithm, BCRYPT_SHA256_ALGORITHM, nullptr, 0) < 0) return false;
  auto close = [&] { if (hash) BCryptDestroyHash(hash); BCryptCloseAlgorithmProvider(algorithm, 0); };
  if (BCryptGetProperty(algorithm, BCRYPT_OBJECT_LENGTH, reinterpret_cast<PUCHAR>(&objectBytes), sizeof objectBytes, &actual, 0) < 0) { close(); return false; }
  std::vector<uint8_t> object(objectBytes);
  if (BCryptCreateHash(algorithm, &hash, object.data(), objectBytes, nullptr, 0, 0) < 0) { close(); return false; }
  auto add = [&](const void *data, size_t bytes) { return bytes <= std::numeric_limits<ULONG>::max() && BCryptHashData(hash, reinterpret_cast<PUCHAR>(const_cast<void *>(data)), static_cast<ULONG>(bytes), 0) >= 0; };
  const bool valid = add(identity.data(), identity.size()) && add(payload.albedoRgb.data(), payload.albedoRgb.size()) &&
      add(payload.elevation.data(), payload.elevation.size() * sizeof(uint16_t)) && add(payload.land.data(), payload.land.size()) &&
      add(payload.cloud.data(), payload.cloud.size()) && BCryptFinishHash(hash, result.data(), static_cast<ULONG>(result.size()), 0) >= 0;
  close(); return valid;
}
}  // namespace

uint64_t Pack::Ordinal(uint32_t face, uint32_t level, uint32_t x, uint32_t y) {
  uint64_t preceding = 0; for (uint32_t previous = 0; previous < level; ++previous) preceding += 6ull << (2 * previous);
  uint64_t morton = 0; for (uint32_t bit = 0; bit < 24; ++bit) { morton |= uint64_t((x >> bit) & 1u) << (2 * bit); morton |= uint64_t((y >> bit) & 1u) << (2 * bit + 1); }
  return preceding + uint64_t(face) * (1ull << (2 * level)) + morton;
}

bool Pack::Open(const std::string &path, std::string &error) {
  path_.clear(); records_.clear(); bodyId_ = 0; maximumLevel_ = terrainVersion_ = interior_ = gutter_ = extent_ = 0;
  std::ifstream input(path, std::ios::binary); std::array<uint8_t, HeaderBytes> header{};
  if (!input.read(reinterpret_cast<char *>(header.data()), header.size())) { error = "production cube pack missing or truncated"; return false; }
  interior_ = Read32(header.data() + 16); gutter_ = Read32(header.data() + 20); extent_ = Read32(header.data() + 24);
  if (std::memcmp(header.data(), "NCCUBE1\0", 8) != 0 || Read32(header.data() + 8) != 1 || Read32(header.data() + 12) != HeaderBytes ||
      !interior_ || interior_ > 4096 || !gutter_ || gutter_ > 64 || extent_ != interior_ + 2 * gutter_) {
    error = "production cube pack header contract mismatch"; return false;
  }
  maximumLevel_ = Read32(header.data() + 28); const uint32_t recordCount = Read32(header.data() + 32); terrainVersion_ = Read32(header.data() + 36);
  uint64_t expected = 0; for (uint32_t level = 0; level <= maximumLevel_; ++level) expected += 6ull << (2 * level);
  if (maximumLevel_ > 8 || recordCount != expected || !terrainVersion_) { error = "production cube pack hierarchy contract mismatch"; return false; }
  const uint64_t texels = uint64_t(extent_) * extent_;
  if (texels > std::numeric_limits<uint32_t>::max() / 3u) { error = "production cube pack dimensions overflow"; return false; }
  const std::array<uint32_t,4> expectedBytes{static_cast<uint32_t>(texels*3u),static_cast<uint32_t>(texels*2u),static_cast<uint32_t>(texels),static_cast<uint32_t>(texels)};
  records_.resize(recordCount);
  std::vector<uint8_t> occupied(recordCount);
  for (uint32_t index = 0; index < recordCount; ++index) {
    const uint64_t offset = static_cast<uint64_t>(input.tellg()); std::array<uint8_t, RecordHeaderBytes> bytes{};
    if (!input.read(reinterpret_cast<char *>(bytes.data()), bytes.size())) { error = "production cube record header truncated"; return false; }
    Record record{}; record.id.bodyId = Read64(bytes.data()); record.id.terrainVersion = Read32(bytes.data() + 8); record.id.face = bytes[12]; record.id.level = bytes[13];
    record.id.x = Read32(bytes.data() + 16); record.id.y = Read32(bytes.data() + 20); record.ordinal = Read64(bytes.data() + 24); record.offset = offset;
    for (uint32_t channel = 0; channel < 4; ++channel) record.bytes[channel] = Read32(bytes.data() + 32 + channel * 4);
    std::memcpy(record.digest.data(), bytes.data() + 48, record.digest.size());
    if (index == 0) bodyId_ = record.id.bodyId;
    if (!bodyId_ || record.id.bodyId != bodyId_ || record.id.terrainVersion != terrainVersion_ || record.id.face >= 6 || record.id.level > maximumLevel_ ||
        record.id.x >= (1u << record.id.level) || record.id.y >= (1u << record.id.level) || record.ordinal != Ordinal(record.id.face, record.id.level, record.id.x, record.id.y) ||
        record.bytes != expectedBytes) { error = "production cube record identity/layout invalid"; return false; }
    if (record.ordinal >= records_.size() || occupied[record.ordinal]) { error = "production cube record ordinal duplicated or out of range"; return false; }
    records_[record.ordinal] = record; occupied[record.ordinal] = 1;
    input.seekg(static_cast<std::streamoff>(record.bytes[0]) + record.bytes[1] + record.bytes[2] + record.bytes[3], std::ios::cur);
    if (!input) { error = "production cube record payload truncated"; return false; }
  }
  if (std::find(occupied.begin(), occupied.end(), uint8_t{0}) != occupied.end()) { error = "production cube hierarchy is incomplete"; return false; }
  path_ = path; return true;
}

const Record *Pack::Find(const PatchId &id) const {
  if (id.bodyId != bodyId_ || id.terrainVersion != terrainVersion_ || id.face >= 6 || id.level > maximumLevel_ || id.x >= (1u << id.level) || id.y >= (1u << id.level)) return nullptr;
  const auto ordinal = Ordinal(id.face, id.level, id.x, id.y); return ordinal < records_.size() && records_[ordinal].id == id ? &records_[ordinal] : nullptr;
}
bool Pack::Contains(const PatchId &id) const { return Find(id) != nullptr; }

bool Pack::Read(const PatchId &id, Payload &payload, std::string &error) const {
  const auto *record = Find(id); if (!record) { error = "production cube patch is outside the shipped hierarchy"; return false; }
  std::ifstream input(path_, std::ios::binary); input.seekg(record->offset + RecordHeaderBytes);
  payload = {}; payload.id = id; payload.albedoRgb.resize(record->bytes[0]); payload.elevation.resize(record->bytes[1] / 2); payload.land.resize(record->bytes[2]); payload.cloud.resize(record->bytes[3]);
  if (!input.read(reinterpret_cast<char *>(payload.albedoRgb.data()), payload.albedoRgb.size()) ||
      !input.read(reinterpret_cast<char *>(payload.elevation.data()), record->bytes[1]) || !input.read(reinterpret_cast<char *>(payload.land.data()), payload.land.size()) ||
      !input.read(reinterpret_cast<char *>(payload.cloud.data()), payload.cloud.size())) {
    error = "production cube payload read failed"; return false;
  }
  std::array<uint8_t, 32> digest{};
  payload.digestValid = Digest(id, payload, digest) && digest == record->digest;
  if (!payload.digestValid) { error = "production cube payload digest mismatch"; return false; }
  return true;
}
}  // namespace nc::production
