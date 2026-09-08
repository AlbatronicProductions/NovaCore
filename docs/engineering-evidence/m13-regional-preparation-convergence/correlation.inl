// Private diagnostic constants and readiness; no production dependencies.
extern "C" __declspec(dllexport) int ncCompositionReady(){
  if(!gApp)return 0;auto& a=*gApp;
  return a.regionalPhysical&&a.regionalPhysical->AllContributingResident()&&
    a.productionBillboardAuthoritative&&!a.productionBillboardIncomingEnabled&&
    !a.regionalPreparation[0].active&&!a.regionalPreparation[1].active;
}
std::string CompositionHex(const void* data,size_t bytes){
  static const char digits[]="0123456789abcdef";std::string s(bytes*2,'0');const auto* p=static_cast<const unsigned char*>(data);
  for(size_t i=0;i<bytes;i++){s[i*2]=digits[p[i]>>4];s[i*2+1]=digits[p[i]&15];}return s;
}
void CompositionCorrelation(App& a){
  if(!std::getenv("NOVACORE_COMPOSITION_CONTROL"))return;
  auto gpu=a.submission->planetaryGpu;gpu.terrainFrame=0;
  std::string line="Composition frame: frame="+std::to_string(a.frame)+
    "; generation="+std::to_string(a.productionBillboardGeneration)+
    "; incomingGeneration="+std::to_string(a.productionBillboardIncomingGeneration)+
    "; currentTopology="+std::to_string(a.productionBillboardTopologyHash)+
    "; incomingTopology="+std::to_string(a.productionBillboardIncomingTopologyHash)+
    "; cameraBits="+CompositionHex(&a.submission->camera,sizeof a.submission->camera)+
    "; gpuBits="+CompositionHex(&gpu,sizeof gpu)+
    "; presentationBits="+CompositionHex(&a.submission->planetaryPresentation,sizeof a.submission->planetaryPresentation)+
    "; lightingBits="+CompositionHex(&a.submission->solarLighting,sizeof a.submission->solarLighting)+
    "; publishedBits="+CompositionHex(&a.regionalPublishedPupil,sizeof a.regionalPublishedPupil);
  if(a.regionalPreparationMapped){const auto& c=*static_cast<const RegionalPreparationControl*>(a.regionalPreparationMapped);
    line+="; incomingBits="+CompositionHex(&c.incoming,sizeof c.incoming)+
      "; cursor="+std::to_string(a.regionalPreparation[1].cursor)+
      "; active="+std::to_string(a.regionalPreparation[1].active?1:0)+
      "; completeDependencies="+std::to_string(a.regionalReady[1]?1:0);
  }
  a.Log(NC_LOG_ALWAYS,line.c_str());
}
