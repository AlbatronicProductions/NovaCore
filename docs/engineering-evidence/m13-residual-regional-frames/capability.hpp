#pragma once
// Private qualification contract. Not a production ABI or Vulkan resource owner.
// Populate ActualState from live native ownership/descriptor/fence records, NOT
// by copying ReuseToken. Digests identify immutable inputs, not their filenames.
#include <array>
#include <cstdint>
#include <initializer_list>

namespace nc::residualproof {
using Digest = std::array<std::uint64_t, 4>;
struct DomainIdentity {
  std::uint64_t body{}, physicalGeneration{}, radiusBits{};
  Digest oracle{}, regional{}, support{}, preparation{};
  bool operator==(const DomainIdentity&) const = default;
};
struct BufferIdentity {
  // Allocation serials never repeat within rendererEpoch; contentEpoch advances
  // before any reassignment/overwrite, even when the VkBuffer handle survives.
  std::uint64_t allocation{}, contentEpoch{}, latticeAllocation{};
  std::uint64_t generation{}, topology{};
  std::uint32_t topologyFamily{}, vertices{};
  // Exact bytes of the native 160-byte pupil frame (20 eight-byte words).
  // This includes snap/transition fields; no epsilon or hash substitutes for it.
  std::array<std::uint64_t, 20> frame{};
  DomainIdentity domain{};
  bool operator==(const BufferIdentity&) const = default;
};
struct MapIdentity {
  std::uint64_t allocation{}, mappingEpoch{}, contentVersion{}, mappingHash{};
  std::uint64_t sourceTopology{}, destinationTopology{};
  std::uint32_t entries{}, sourceVertices{}, destinationVertices{};
  bool operator==(const MapIdentity&) const = default;
};
struct ReuseToken {
  std::uint32_t version{1};
  std::uint64_t rendererEpoch{}, bindingEpoch{}, transaction{};
  BufferIdentity source{}, destination{};
  MapIdentity map{};
  bool operator==(const ReuseToken&) const = default;
};
struct ActualState {
  ReuseToken expected{};
  bool active{}, cancelled{}, currentOwnerRetained{};
  bool destinationAlive{}, destinationBound{}, destinationWritable{};
  bool dependenciesComplete{}, domainValid{};
  bool sourceAlive{}, sourceBound{}, sourceIsCurrent{}, sourceComplete{};
  bool sourceReadOnly{}, mapAlive{}, mapBound{}, mapAddressValid{}, mapReadOnly{};
  std::uint64_t completedSerial{}, sourceLastWriteSerial{};
  std::uint64_t destinationLastUseSerial{}, mapLastUseSerial{};
  std::uint32_t sourceCapacity{}, destinationCapacity{}, mapEntryCapacity{}, cursor{}, sliceCount{};
};
enum class ReuseDecision { AllowReuse, Recompute, CancelRetainCurrent };

inline bool ValidIdentity(const BufferIdentity& value) {
  return value.allocation && value.contentEpoch && value.latticeAllocation &&
    value.generation && value.topology && value.vertices;
}
inline bool ValidDestination(const ActualState& a) {
  const auto& e = a.expected;
  return e.version == 1 && e.rendererEpoch && e.bindingEpoch && e.transaction &&
    a.active && !a.cancelled && a.currentOwnerRetained && a.destinationAlive &&
    a.destinationBound && a.domainValid && a.dependenciesComplete &&
    ValidIdentity(e.destination) &&
    a.destinationCapacity >= e.destination.vertices &&
    (!a.sourceAlive || e.source.allocation != e.destination.allocation);
}
inline bool SameBasis(const BufferIdentity& source, const BufferIdentity& destination) {
  // east.xyz, north.xyz, up.xyz; radius and remaining physical inputs are domain.
  for (const auto i : {0u, 1u, 2u, 4u, 5u, 6u, 8u, 9u, 10u})
    if (source.frame[i] != destination.frame[i]) return false;
  return true;
}
inline ReuseDecision DecideReuse(const ActualState& a, const ReuseToken& offered) {
  const auto& e = a.expected;
  if (!ValidDestination(a) || !a.destinationWritable || !a.sliceCount ||
      a.cursor >= e.destination.vertices ||
      std::uint64_t(a.cursor) + a.sliceCount > e.destination.vertices ||
      a.destinationLastUseSerial > a.completedSerial)
    return ReuseDecision::CancelRetainCurrent;
  // Invalid optional source/map metadata is harmless only because the actual
  // destination above remains valid. Caller dispatches unchanged complete H.
  if (!(offered == e) || !ValidIdentity(e.source) || !a.sourceAlive ||
      !a.sourceBound || !a.sourceIsCurrent || !a.sourceComplete ||
      !a.sourceReadOnly || a.sourceLastWriteSerial > a.completedSerial ||
      a.sourceCapacity < e.source.vertices ||
      !a.mapAlive || !a.mapBound || !a.mapAddressValid || !a.mapReadOnly ||
      a.mapLastUseSerial > a.completedSerial || !e.map.allocation ||
      !e.map.mappingEpoch || !e.map.contentVersion || !e.map.mappingHash ||
      e.map.sourceTopology != e.source.topology ||
      e.map.destinationTopology != e.destination.topology ||
      e.map.sourceVertices != e.source.vertices ||
      e.map.destinationVertices != e.destination.vertices ||
      e.map.entries != e.destination.vertices ||
      a.mapEntryCapacity < e.map.entries ||
      !(e.source.domain == e.destination.domain) || !SameBasis(e.source, e.destination))
    return ReuseDecision::Recompute;
  // This permits evaluating a guarded map entry, not blindly copying a vertex.
  // GPU must bounds-check the entry and prove exact canonical direction/radius
  // before copying all 64 bytes; otherwise it evaluates complete destination H.
  return ReuseDecision::AllowReuse;
}

struct PublicationEvidence {
  std::uint64_t rendererEpoch{}, bindingEpoch{}, transaction{}, finalSubmitSerial{};
  BufferIdentity destination{};
  std::uint32_t preparedVertices{}, validatedVertices{}, invalidPositions{}, invalidNormals{};
  bool finalSliceRecorded{}, finalFenceComplete{}, topologyValidated{};
};
inline bool CanPublish(const ActualState& a, const PublicationEvidence& proof) {
  const auto& e = a.expected;
  // Physical publication qualification only. Existing draw/compaction readiness
  // remains an ADDITIONAL gate until a separately proven contract replaces it.
  return ValidDestination(a) && proof.rendererEpoch == e.rendererEpoch &&
    proof.bindingEpoch == e.bindingEpoch && proof.transaction == e.transaction &&
    proof.destination == e.destination && proof.finalSubmitSerial &&
    proof.finalSubmitSerial <= a.completedSerial &&
    a.destinationLastUseSerial == proof.finalSubmitSerial &&
    proof.finalSliceRecorded && proof.finalFenceComplete && proof.topologyValidated &&
    a.cursor == e.destination.vertices &&
    proof.preparedVertices == e.destination.vertices &&
    proof.validatedVertices == e.destination.vertices &&
    proof.invalidPositions == 0 && proof.invalidNormals == 0;
}
} // namespace nc::residualproof
