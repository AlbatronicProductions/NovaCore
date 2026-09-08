// Private address-lookup proof. The production design would transport the
// existing managed ParentVertexMap; this deliberately measures a synchronous
// diagnostic rational lookup separately and is not proposed production code.
void CompositionMapDescriptor(App& a){
  if(!a.descriptor||!a.compositionMapBuffer)return;
  VkDescriptorBufferInfo info{a.compositionMapBuffer,0,16ull+712106ull*4ull};
  VkWriteDescriptorSet write{VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET};
  write.dstSet=a.descriptor;write.dstBinding=59;write.descriptorCount=1;
  write.descriptorType=VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;write.pBufferInfo=&info;
  vkUpdateDescriptorSets(a.device,1,&write,0,nullptr);
}
void CompositionMakeMap(App& a,const NcProductionSphericalBillboardSubmission& candidate){
  if(!a.compositionMapMapped)return;
  auto* out=static_cast<uint32_t*>(a.compositionMapMapped);std::memset(out,0,16);
  if(!std::getenv("NOVACORE_COMPOSITION_MAPPED")||!a.productionBillboardAuthoritative||
    !a.productionBillboardLatticeMapped||candidate.vertexCount>712106u)return;
  const auto begin=std::chrono::steady_clock::now();
  // Physical shader independently validates the actual mapped source direction.
  // Rational coordinates establish only an address candidate, never authority.
  struct Hash{size_t operator()(const std::array<int32_t,3>& key)const{
    uint64_t hash=1469598103934665603ull;for(auto x:key){hash^=uint32_t(x);hash*=1099511628211ull;}return size_t(hash);}};
  auto key=[](const void* p){std::array<int32_t,3> k;std::memcpy(k.data(),p,12);
    int32_t divisor=std::gcd(std::gcd(k[0],k[1]),k[2]);if(!divisor)throw std::runtime_error("zero lattice map key");for(auto& x:k)x/=divisor;return k;};
  std::unordered_map<std::array<int32_t,3>,uint32_t,Hash> lookup;
  lookup.reserve(a.productionBillboardVertexCount);
  const auto* source=static_cast<const unsigned char*>(a.productionBillboardLatticeMapped);
  const auto* destination=reinterpret_cast<const unsigned char*>(candidate.latticeVertices);
  for(uint32_t i=0;i<a.productionBillboardVertexCount;i++)
    if(!lookup.emplace(key(source+16ull*i),i).second)throw std::runtime_error("duplicate source lattice key");
  uint32_t matches=0;
  for(uint32_t i=0;i<candidate.vertexCount;i++){
    const auto found=lookup.find(key(destination+16ull*i));
    out[4+i]=found==lookup.end()?UINT32_MAX:found->second;matches+=found!=lookup.end();
  }
  out[0]=candidate.vertexCount;out[1]=a.productionBillboardVertexCount;
  out[2]=uint32_t(a.productionBillboardGeneration);out[3]=uint32_t(candidate.publicationGeneration);
  CompositionMapDescriptor(a);
  char row[640];std::snprintf(row,sizeof row,"Composition map: generation=%llu; sourceGeneration=%llu; sourceHash=%llu; destinationHash=%llu; sourceVertices=%u; destinationVertices=%u; mapped=%u; uploadBytes=%llu; diagnosticSynchronousCpuMs=%.6f; productionOrchestrationQualified=false",
    (unsigned long long)candidate.publicationGeneration,(unsigned long long)a.productionBillboardGeneration,
    (unsigned long long)a.productionBillboardTopologyHash,(unsigned long long)candidate.topologyHash,
    a.productionBillboardVertexCount,candidate.vertexCount,matches,(unsigned long long)(16ull+4ull*candidate.vertexCount),
    std::chrono::duration<double,std::milli>(std::chrono::steady_clock::now()-begin).count());a.Log(NC_LOG_ALWAYS,row);
}
