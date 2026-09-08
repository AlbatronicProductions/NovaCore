// Private CPU negative controls for the predicates consumed by instrumentation.
// This does not prove Vulkan lifetime, GPU copy equality, or production readiness.
#include "capability.hpp"
#include <cstdio>
#include <functional>
#include <utility>
#include <vector>
using namespace nc::residualproof;

ActualState Baseline() {
  ActualState a{};
  auto& e = a.expected;
  e.rendererEpoch = 1; e.bindingEpoch = 2; e.transaction = 3;
  e.source = {10, 1, 30, 0x100000011ull, 100, 1, 16};
  e.destination = {11, 2, 31, 0x100000012ull, 101, 1, 24};
  e.source.domain = {6, 4, 0x41584daf33333333ull, {1,2,3,4}, {5,6,7,8},
    {9,10,11,12}, {13,14,15,16}};
  e.destination.domain = e.source.domain;
  for (unsigned i = 0; i < 20; ++i) e.source.frame[i] = e.destination.frame[i] = 1000 + i;
  // Different source/destination pupil and snap identities are permitted;
  // per-vertex GPU direction guards determine whether addresses really match.
  e.destination.frame[3]++; e.destination.frame[16]++;
  e.map = {50, 1, 2, 222, 100, 101, 24, 16, 24};
  a.active = a.currentOwnerRetained = a.destinationAlive = a.destinationBound = true;
  a.destinationWritable = a.dependenciesComplete = a.domainValid = true;
  a.sourceAlive = a.sourceBound = a.sourceIsCurrent = a.sourceComplete = true;
  a.sourceReadOnly = a.mapAlive = a.mapBound = a.mapAddressValid = a.mapReadOnly = true;
  a.completedSerial = 100; a.sourceLastWriteSerial = 80;
  a.destinationLastUseSerial = 99; a.mapLastUseSerial = 99;
  a.sourceCapacity = 16; a.destinationCapacity = a.mapEntryCapacity = 24;
  a.cursor = 8; a.sliceCount = 8;
  return a;
}
PublicationEvidence Completed(ActualState& a) {
  a.cursor = a.expected.destination.vertices;
  a.destinationLastUseSerial = 100;
  a.destinationWritable = false;
  return {a.expected.rendererEpoch, a.expected.bindingEpoch, a.expected.transaction,
    100, a.expected.destination, 24, 24, 0, 0, true, true, true};
}
int main() {
  unsigned checks = 0, failures = 0;
  auto require = [&](bool result, const char* name) {
    ++checks;
    if (!result) { ++failures; std::fprintf(stderr, "FAIL: %s\n", name); }
  };
  require(DecideReuse(Baseline(), Baseline().expected) == ReuseDecision::AllowReuse,
    "valid independent source/destination generations");
  using TokenMutation = std::pair<const char*, std::function<void(ReuseToken&)>>;
  const std::vector<TokenMutation> staleTokens{
    {"version", [](auto& t){++t.version;}},
    {"renderer epoch", [](auto& t){++t.rendererEpoch;}},
    {"binding epoch", [](auto& t){++t.bindingEpoch;}},
    {"transaction", [](auto& t){++t.transaction;}},
    {"source allocation", [](auto& t){++t.source.allocation;}},
    {"source overwritten", [](auto& t){++t.source.contentEpoch;}},
    {"source lattice", [](auto& t){++t.source.latticeAllocation;}},
    {"source high32 generation", [](auto& t){t.source.generation += 1ull<<32;}},
    {"source topology", [](auto& t){++t.source.topology;}},
    {"source family", [](auto& t){++t.source.topologyFamily;}},
    {"source count", [](auto& t){++t.source.vertices;}},
    {"source frame", [](auto& t){++t.source.frame[16];}},
    {"destination allocation", [](auto& t){++t.destination.allocation;}},
    {"destination overwritten", [](auto& t){++t.destination.contentEpoch;}},
    {"destination lattice", [](auto& t){++t.destination.latticeAllocation;}},
    {"destination high32 generation", [](auto& t){t.destination.generation += 1ull<<32;}},
    {"destination topology", [](auto& t){++t.destination.topology;}},
    {"destination count", [](auto& t){++t.destination.vertices;}},
    {"destination frame", [](auto& t){++t.destination.frame[16];}},
    {"basis signed zero bit", [](auto& t){t.source.frame[0] ^= 1ull<<63;}},
    {"source regional", [](auto& t){++t.source.domain.regional[0];}},
    {"source support", [](auto& t){++t.source.domain.support[0];}},
    {"destination regional", [](auto& t){++t.destination.domain.regional[0];}},
    {"destination support", [](auto& t){++t.destination.domain.support[0];}},
    {"map allocation", [](auto& t){++t.map.allocation;}},
    {"map remapped", [](auto& t){++t.map.mappingEpoch;}},
    {"map rewritten", [](auto& t){++t.map.contentVersion;}},
    {"map hash", [](auto& t){++t.map.mappingHash;}},
    {"map source", [](auto& t){++t.map.sourceTopology;}},
    {"map destination", [](auto& t){++t.map.destinationTopology;}},
    {"map truncated", [](auto& t){--t.map.entries;}},
    {"map source count", [](auto& t){++t.map.sourceVertices;}},
    {"map destination count", [](auto& t){++t.map.destinationVertices;}}
  };
  for (const auto& [name, mutate] : staleTokens) {
    auto a = Baseline(); auto offered = a.expected; mutate(offered);
    require(DecideReuse(a, offered) == ReuseDecision::Recompute, name);
  }
  using ActualMutation = std::pair<const char*, std::function<void(ActualState&)>>;
  const std::vector<ActualMutation> noReuse{
    {"source dead", [](auto& a){a.sourceAlive=false;}},
    {"source unbound", [](auto& a){a.sourceBound=false;}},
    {"source no longer current", [](auto& a){a.sourceIsCurrent=false;}},
    {"source incomplete", [](auto& a){a.sourceComplete=false;}},
    {"source writable", [](auto& a){a.sourceReadOnly=false;}},
    {"short source", [](auto& a){a.sourceCapacity=15;}},
    {"source fence withheld", [](auto& a){a.sourceLastWriteSerial=101;}},
    {"map dead", [](auto& a){a.mapAlive=false;}},
    {"map unbound", [](auto& a){a.mapBound=false;}},
    {"mapped address invalid", [](auto& a){a.mapAddressValid=false;}},
    {"map writable", [](auto& a){a.mapReadOnly=false;}},
    {"short actual map", [](auto& a){a.mapEntryCapacity=23;}},
    {"map busy", [](auto& a){a.mapLastUseSerial=101;}},
    {"actual basis mismatch", [](auto& a){++a.expected.destination.frame[0];}},
    {"actual radius mismatch", [](auto& a){++a.expected.destination.domain.radiusBits;}},
    {"actual physical generation mismatch", [](auto& a){++a.expected.destination.domain.physicalGeneration;}},
    {"actual oracle mismatch", [](auto& a){++a.expected.destination.domain.oracle[0];}},
    {"actual regional change", [](auto& a){++a.expected.destination.domain.regional[0];}},
    {"actual support change", [](auto& a){++a.expected.destination.domain.support[0];}},
    {"actual preparation change", [](auto& a){++a.expected.destination.domain.preparation[0];}},
    {"actual map pair invalid", [](auto& a){++a.expected.map.destinationTopology;}},
    {"actual map extent invalid", [](auto& a){--a.expected.map.entries;}}
  };
  for (const auto& [name, mutate] : noReuse) {
    auto a=Baseline(); mutate(a);
    require(DecideReuse(a, a.expected) == ReuseDecision::Recompute, name);
  }
  const std::vector<ActualMutation> cancel{
    {"cancelled transaction", [](auto& a){a.cancelled=true;}},
    {"inactive transaction", [](auto& a){a.active=false;}},
    {"owner not retained", [](auto& a){a.currentOwnerRetained=false;}},
    {"destination destroyed", [](auto& a){a.destinationAlive=false;}},
    {"destination binding missing", [](auto& a){a.destinationBound=false;}},
    {"destination not writable", [](auto& a){a.destinationWritable=false;}},
    {"dependencies missing", [](auto& a){a.dependenciesComplete=false;}},
    {"invalid actual domain", [](auto& a){a.domainValid=false;}},
    {"aliased destination", [](auto& a){a.expected.destination.allocation=a.expected.source.allocation;}},
    {"destination fence withheld", [](auto& a){a.destinationLastUseSerial=101;}},
    {"short destination", [](auto& a){a.destinationCapacity=23;}},
    {"empty slice", [](auto& a){a.sliceCount=0;}},
    {"slice beyond destination", [](auto& a){a.cursor=24;}},
    {"slice overflow", [](auto& a){a.sliceCount=0xffffffffu;}}
  };
  for (const auto& [name, mutate] : cancel) {
    auto a=Baseline(); mutate(a);
    require(DecideReuse(a, a.expected) == ReuseDecision::CancelRetainCurrent, name);
  }
  using ProofMutation = std::pair<const char*, std::function<void(PublicationEvidence&)>>;
  const std::vector<ProofMutation> invalidProof{
    {"publication renderer", [](auto& p){++p.rendererEpoch;}},
    {"publication binding", [](auto& p){++p.bindingEpoch;}},
    {"publication transaction", [](auto& p){++p.transaction;}},
    {"publication allocation", [](auto& p){++p.destination.allocation;}},
    {"publication high32 generation", [](auto& p){p.destination.generation += 1ull<<32;}},
    {"publication final serial", [](auto& p){++p.finalSubmitSerial;}},
    {"publication old serial", [](auto& p){--p.finalSubmitSerial;}},
    {"publication missing final slice", [](auto& p){p.finalSliceRecorded=false;}},
    {"publication withheld fence", [](auto& p){p.finalFenceComplete=false;}},
    {"publication incomplete preparation", [](auto& p){--p.preparedVertices;}},
    {"publication incomplete validation", [](auto& p){--p.validatedVertices;}},
    {"publication invalid position", [](auto& p){++p.invalidPositions;}},
    {"publication invalid normal", [](auto& p){++p.invalidNormals;}},
    {"publication unchecked topology", [](auto& p){p.topologyValidated=false;}}
  };
  for (const auto& [name, mutate] : invalidProof) {
    auto a=Baseline(); auto p=Completed(a); mutate(p);
    require(!CanPublish(a,p), name);
  }
  {
    auto a=Baseline(); auto p=Completed(a);
    require(CanPublish(a,p), "complete fenced physical destination");
    --a.completedSerial; require(!CanPublish(a,p), "final submit not completed");
    ++a.completedSerial; a.cancelled=true; require(!CanPublish(a,p), "cancel after final slice");
    a.cancelled=false; --a.cursor; require(!CanPublish(a,p), "incomplete actual cursor");
  }
  {
    auto a=Baseline(); auto old=a.expected;
    ++a.expected.bindingEpoch;
    require(DecideReuse(a,old)==ReuseDecision::Recompute, "resize rejects old binding token");
    require(DecideReuse(a,a.expected)==ReuseDecision::AllowReuse, "resize independently rebound token");
    a.expected.destination.contentEpoch++; a.expected.transaction++;
    require(DecideReuse(a,old)==ReuseDecision::Recompute, "spare reassignment rejects old transaction");
  }
  std::printf("capability qualification: checks=%u failures=%u\n", checks, failures);
  return failures ? 1 : 0;
}
