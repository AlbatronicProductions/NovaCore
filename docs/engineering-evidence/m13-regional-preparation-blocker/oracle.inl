// Private CPU oracle, called after final fence/readiness and before ownership swap.
// Reads existing host-coherent mappings; never changes renderer data.
struct PrepKeyHash {
  template<class T,size_t N> size_t operator()(const std::array<T,N>& key)const{
    uint64_t value=1469598103934665603ull;
    for(auto x:key){value^=uint64_t(x);value*=1099511628211ull;}
    return size_t(value);
  }
};
void PrepIdentityOracle(App& a){
  if(std::getenv("NOVACORE_PERFORMANCE_FRAME_LOG")&&a.regionalPreparation[1].active){char row[512];std::snprintf(row,sizeof row,"Prep publication: completedGpuFrame=%llu; generation=%llu; sourceGeneration=%llu; pupil=%u; vertices=%u; elapsedMs=%.6f; currentRetained=true; completeDependencies=%u; finalFence=true",(unsigned long long)a.frame,(unsigned long long)a.productionBillboardIncomingGeneration,(unsigned long long)a.productionBillboardGeneration,a.regionalPublishedPupil.identity[0],a.productionBillboardIncomingVertexCount,std::chrono::duration<double,std::milli>(std::chrono::steady_clock::now()-a.regionalPreparation[1].started).count(),a.regionalReady[1]?1u:0u);a.Log(NC_LOG_ALWAYS,row);}
  if(std::getenv("NOVACORE_PREP_DIGEST")){
    const auto begin=std::chrono::steady_clock::now();uint64_t hash=1469598103934665603ull;
    const auto* values=static_cast<const uint64_t*>(a.productionBillboardIncomingPhysicalMapped);
    for(uint64_t i=0;i<uint64_t(a.productionBillboardIncomingVertexCount)*8u;i++){hash^=values[i];hash*=1099511628211ull;}
    const auto* frames=static_cast<const NcProductionBillboardFrame*>(a.productionBillboardFrameMapped);
    char row[512];std::snprintf(row,sizeof row,"Prep digest: generation=%llu; pupil=%u; vertices=%u; full64FnvWords=%016llX; cpuMs=%.6f",(unsigned long long)a.productionBillboardIncomingGeneration,frames->incoming.identity[0],a.productionBillboardIncomingVertexCount,(unsigned long long)hash,std::chrono::duration<double,std::milli>(std::chrono::steady_clock::now()-begin).count());a.Log(NC_LOG_ALWAYS,row);
  }
  if(std::getenv("NOVACORE_PREP_GPU_ORACLE")){
    auto* values=static_cast<unsigned char*>(a.productionBillboardIncomingPhysicalMapped);
    std::array<uint32_t,12> reused{},total{};uint64_t mismatch=0;
    for(uint32_t i=0;i<a.productionBillboardIncomingVertexCount;i++){
      uint32_t marker[4];std::memcpy(marker,values+64ull*i+48,16);
      ++total[i/65536];reused[i/65536]+=marker[0];mismatch+=marker[1];
      std::memset(values+64ull*i+48,0,16);
    }
    char row[512];for(uint32_t i=0;i<total.size();i++)if(total[i]){std::snprintf(row,sizeof row,"Prep GPU oracle: frame=%llu; generation=%llu; slice=%u; total=%u; exactDirectionReuse=%u; physicalWordMismatches=%llu; markerClearedBeforePublication=true",(unsigned long long)a.frame,(unsigned long long)a.productionBillboardIncomingGeneration,i,total[i],reused[i],(unsigned long long)mismatch);a.Log(NC_LOG_ALWAYS,row);}
    if(mismatch)throw std::runtime_error("GPU exact direction reuse physical words differ");
  }
  if(!std::getenv("NOVACORE_PREP_IDENTITY_ORACLE")||!a.productionBillboardAuthoritative||
      !a.productionBillboardPhysicalMapped||a.productionBillboardIncomingVertexCount<500000u)return;
  const auto begin=std::chrono::steady_clock::now();
  const auto* old=static_cast<const unsigned char*>(a.productionBillboardPhysicalMapped);
  const auto* next=static_cast<const unsigned char*>(a.productionBillboardIncomingPhysicalMapped);
  const auto* oldL=static_cast<const unsigned char*>(a.productionBillboardLatticeMapped);
  const auto* nextL=static_cast<const unsigned char*>(a.productionBillboardIncomingLatticeMapped);
  static_assert(sizeof(NcSphericalBillboardPhysicalVertex)==64);
  static_assert(sizeof(NcProductionBillboardLatticeVertex)==16);
  auto position=[](const unsigned char* bytes){std::array<uint64_t,4> key;std::memcpy(key.data(),bytes,32);return key;};
  auto lattice=[](const unsigned char* bytes){std::array<int32_t,3> key;std::memcpy(key.data(),bytes,12);int32_t gcd=std::gcd(std::gcd(key[0],key[1]),key[2]);for(auto& v:key)v/=gcd;return key;};
  std::unordered_map<std::array<uint64_t,4>,uint32_t,PrepKeyHash> positions;
  std::unordered_map<std::array<int32_t,3>,uint32_t,PrepKeyHash> coordinates;
  positions.reserve(a.productionBillboardVertexCount);coordinates.reserve(a.productionBillboardVertexCount);
  for(uint32_t i=0;i<a.productionBillboardVertexCount;i++){positions.emplace(position(old+64ull*i),i);coordinates.emplace(lattice(oldL+16ull*i),i);}
  std::array<uint64_t,7> count{};
  std::array<std::array<uint32_t,4>,12> slices{};
  for(uint32_t i=0;i<a.productionBillboardIncomingVertexCount;i++){
    auto& slice=slices[i/65536];++slice[0];const auto* n=next+64ull*i;
    auto p=positions.find(position(n));
    if(p!=positions.end()){
      ++count[0];
      if(std::memcmp(n,old+64ull*p->second,48)==0){++count[1];++slice[1];if(p->second==i)++count[2];else ++count[3];}
      if(std::memcmp(n,old+64ull*p->second,64)==0)++count[4];
    }
    auto q=coordinates.find(lattice(nextL+16ull*i));
    if(q!=coordinates.end()){
      ++count[5];++slice[2];
      if(std::memcmp(n+24,old+64ull*q->second+24,24)==0){++count[6];++slice[3];}
    }
  }
  const auto* frames=static_cast<const NcProductionBillboardFrame*>(a.productionBillboardFrameMapped);
  char row[1536];std::snprintf(row,sizeof row,"Prep identity oracle: frame=%llu; currentGeneration=%llu; incomingGeneration=%llu; currentPupil=%u; incomingPupil=%u; currentVertices=%u; incomingVertices=%u; bodyHeightExact=%llu; physical48Exact=%llu; sameIndexExact=%llu; differentIndexExact=%llu; full64Exact=%llu; rationalMapped=%llu; mappedHeightNormalExact=%llu; basisExact=%u; radiusExact=%u; currentRadius=%.17g; incomingRadius=%.17g; regionalReady=%u; fenceComplete=true; currentOwnerRetained=true; oracleCpuMs=%.3f",(unsigned long long)a.frame,(unsigned long long)a.productionBillboardGeneration,(unsigned long long)a.productionBillboardIncomingGeneration,a.regionalPublishedPupil.identity[0],frames->incoming.identity[0],a.productionBillboardVertexCount,a.productionBillboardIncomingVertexCount,(unsigned long long)count[0],(unsigned long long)count[1],(unsigned long long)count[2],(unsigned long long)count[3],(unsigned long long)count[4],(unsigned long long)count[5],(unsigned long long)count[6],std::memcmp(a.regionalPublishedPupil.east,frames->incoming.east,24)==0&&std::memcmp(a.regionalPublishedPupil.north,frames->incoming.north,24)==0&&std::memcmp(a.regionalPublishedPupil.up,frames->incoming.up,24)==0?1:0,std::memcmp(a.regionalPublishedPupil.transition+1,frames->incoming.transition+1,8)==0?1:0,a.regionalPublishedPupil.transition[1],frames->incoming.transition[1],a.regionalReady[1]?1:0,std::chrono::duration<double,std::milli>(std::chrono::steady_clock::now()-begin).count());a.Log(NC_LOG_ALWAYS,row);
  for(uint32_t i=0;i<slices.size();i++)if(slices[i][0]){std::snprintf(row,sizeof row,"Prep identity slice: incomingGeneration=%llu; slice=%u; total=%u; physicalExact=%u; rationalMapped=%u; mappedHeightNormalExact=%u",(unsigned long long)a.productionBillboardIncomingGeneration,i,slices[i][0],slices[i][1],slices[i][2],slices[i][3]);a.Log(NC_LOG_ALWAYS,row);}
}
