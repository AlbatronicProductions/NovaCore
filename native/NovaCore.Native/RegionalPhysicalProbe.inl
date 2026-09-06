// Opt-in live NCSM1 readback after the frame fence. Does not change geometry.
namespace RegionalPhysicalProbe {
using V=std::array<double,3>;
V add(V a,V b){return {a[0]+b[0],a[1]+b[1],a[2]+b[2]};}
V sub(V a,V b){return {a[0]-b[0],a[1]-b[1],a[2]-b[2]};}
V mul(V a,double b){return {a[0]*b,a[1]*b,a[2]*b};}
double dot(V a,V b){return a[0]*b[0]+a[1]*b[1]+a[2]*b[2];}
V cross(V a,V b){return {a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]};}
V unit(V a){return mul(a,1/std::sqrt(dot(a,a)));}
void vec(std::ofstream& f,V v){f<<'['<<v[0]<<','<<v[1]<<','<<v[2]<<']';}
void Capture(App& a){
  const char* dir=std::getenv("NOVACORE_REGIONAL_PHYSICAL_PROBE");if(!dir)return;
  if(!RegionalPhysicalEnabled(a)||!a.regionalPhysical->AllContributingResident())return;
  const auto frameNumber=a.anchoredPipelineStatisticsTerrainFrame;
  static uint64_t capturedGeneration=0;
  static bool adjacentCaptured=false;
  const bool adjacent=a.anchoredPipelineStatisticsLevel==16&&!adjacentCaptured;
  if(capturedGeneration==a.productionBillboardGeneration&&!adjacent&&frameNumber!=2&&frameNumber!=60&&frameNumber!=100&&frameNumber!=150&&frameNumber!=165&&frameNumber!=175&&frameNumber!=300&&frameNumber!=440&&frameNumber!=740)return;
  static uint64_t lastFrame=UINT64_MAX;if(lastFrame==frameNumber)return;
  if(!a.productionBillboardAuthoritative||a.productionBillboardGeneration!=a.anchoredPipelineStatisticsGeneration||
    a.productionBillboardPreparedFrameIdentity!=a.productionBillboardRasterFrameIdentity)return;
  capturedGeneration=a.productionBillboardGeneration;
  lastFrame=frameNumber;if(adjacent)adjacentCaptured=true;
  const auto* vertices=static_cast<const NcSphericalBillboardPhysicalVertex*>(a.productionBillboardPhysicalMapped);
  const auto* indices=static_cast<const uint32_t*>(a.productionBillboardCompactedMapped);
  const auto* source=static_cast<const uint32_t*>(a.productionBillboardIndexMapped);
  const auto* lattice=static_cast<const int32_t*>(a.productionBillboardLatticeMapped);
  const auto* draw=static_cast<const VkDrawIndexedIndirectCommand*>(a.productionBillboardIndirectMapped);
  const auto* pupil=static_cast<const NcProductionBillboardFrame*>(a.productionBillboardFrameMapped);
  const auto* counters=static_cast<const uint32_t*>(a.productionBillboardCounterMapped);
  const auto& gpu=a.submission->planetaryGpu;const auto& pr=a.submission->planetaryPresentation;
  const V site{.1433224599406355,.4788205718227514,.8661348234979923};
  const V east=unit(cross({0,1,0},site)),north=unit(cross(site,east));
  const double radius=double(gpu.radiusHigh)+double(gpu.radiusLow);
  const V camera{double(gpu.cameraBodyHighX)+gpu.cameraBodyLowX,double(gpu.cameraBodyHighY)+gpu.cameraBodyLowY,double(gpu.cameraBodyHighZ)+gpu.cameraBodyLowZ};
  const double offsets[17][2]{{0,0},{-32,-24},{32,-24},{-32,24},{32,24},{-32,0},{32,0},{0,-24},{0,24},{64,0},{96,0},{128,0},{160,0},{192,0},{224,0},{0,120},{0,216}};
  struct Result{bool found=false;uint32_t compact=0,source=UINT32_MAX;std::array<uint32_t,3> ids{};V bary{};double r=0;V direction{};};
  std::array<Result,17> results{};
  for(int q=0;q<17;q++)results[q].direction=q==0?site:unit(add(mul(site,radius),add(mul(east,offsets[q][0]),mul(north,offsets[q][1]))));
  for(uint32_t t=0;t<draw->indexCount/3;t++){
    const std::array<uint32_t,3> ids{indices[t*3],indices[t*3+1],indices[t*3+2]};
    // Reconstruct the live VS FP32 camera-relative transport before the rigid
    // body orientation/projection. Inner support has zero near displacement,
    // so factor-1 TES retains this triangle without a second terrain query.
    V p[3];for(int j=0;j<3;j++){const auto& v=vertices[ids[j]];
      p[j]=add(camera,{double(float(v.bodyFixed[0]-camera[0])),double(float(v.bodyFixed[1]-camera[1])),double(float(v.bodyFixed[2]-camera[2]))});}
    // Front hemisphere only; exact ray/triangle containment decides the sample.
    if(dot(p[0],site)<radius*.95)continue;
    auto e1=sub(p[1],p[0]),e2=sub(p[2],p[0]),n=cross(e1,e2);
    double d00=dot(e1,e1),d01=dot(e1,e2),d11=dot(e2,e2),det=d00*d11-d01*d01;
    if(det<=0)continue;
    for(auto& r:results){if(r.found)continue;double den=dot(n,r.direction);if(std::abs(den)<1e-20)continue;
      double hit=dot(n,p[0])/den;if(hit<radius*.9)continue;auto v=sub(mul(r.direction,hit),p[0]);
      double d20=dot(v,e1),d21=dot(v,e2),b=(d11*d20-d01*d21)/det,c=(d00*d21-d01*d20)/det;
      if(b>=-1e-7&&c>=-1e-7&&b+c<=1+1e-7){r.found=true;r.compact=t;r.ids=ids;r.bary={1-b-c,b,c};r.r=hit;}
    }
  }
  for(uint32_t t=0;t<a.productionBillboardTriangleCount;t++)for(auto& r:results)if(r.found&&r.source==UINT32_MAX&&source[t*3]==r.ids[0]&&source[t*3+1]==r.ids[1]&&source[t*3+2]==r.ids[2])r.source=t;
  std::ofstream f(std::string(dir)+"/frame-"+std::to_string(frameNumber)+".json");f.precision(17);
  float maxOuter=0,maxInner=0;std::memcpy(&maxOuter,counters+23,4);std::memcpy(&maxInner,counters+24,4);
  f<<"{\"frame\":"<<frameNumber<<",\"generation\":"<<a.productionBillboardGeneration<<",\"level\":"<<a.anchoredPipelineStatisticsLevel
   <<",\"topologyHash\":\""<<a.productionBillboardTopologyHash<<"\",\"pupilFrameIdentity\":"<<a.productionBillboardRasterFrameIdentity
   <<",\"radius\":"<<radius<<",\"physicalGeneration\":"<<a.submission->physicalSurfaceGeneration<<",\"surfaceMode\":"<<a.submission->planetarySurfaceMode
   <<",\"maxOuter\":"<<maxOuter<<",\"maxInner\":"<<maxInner<<",\"camera\":";vec(f,camera);
  f<<",\"orientation\":["<<pr.bodyOrientationX<<','<<pr.bodyOrientationY<<','<<pr.bodyOrientationZ<<','<<pr.bodyOrientationW<<"],\"matrix\":[";
  for(int j=0;j<16;j++){if(j)f<<',';f<<a.submission->camera.viewProjection.columns[j];}
  double components[6];for(int j=0;j<3;j++)std::memcpy(components+j*2,vertices[j].reserved,16);
  uint32_t residents=uint32_t(a.regionalPhysical->loaded);
  const auto*lookup=static_cast<const uint32_t*>(a.regionalCatalogMapped);
  f<<"],\"gpuExactAnchorComponents\":[";for(int j=0;j<6;j++){if(j)f<<',';f<<components[j];}
  f<<"],\"regionalPublishedLayers\":"<<residents<<",\"regionalRequests\":"<<a.regionalPhysical->requests<<",\"lookupEnabled\":"<<lookup[0];
  f<<",\"gpuOutsideCatalogRecords\":[";for(int j=0;j<4;j++){if(j)f<<',';f<<vertices[3].reserved[j];}f<<']';
  f<<",\"pupil\":{\"east\":[";for(int j=0;j<4;j++){if(j)f<<',';f<<pupil->current.east[j];}
  f<<"],\"north\":[";for(int j=0;j<4;j++){if(j)f<<',';f<<pupil->current.north[j];}
  f<<"],\"up\":[";for(int j=0;j<4;j++){if(j)f<<',';f<<pupil->current.up[j];}
  f<<"],\"transition\":[";for(int j=0;j<4;j++){if(j)f<<',';f<<pupil->current.transition[j];}
  f<<"],\"identity\":[";for(int j=0;j<4;j++){if(j)f<<',';f<<pupil->current.identity[j];}f<<"]},\"samples\":[";
  for(int q=0;q<17;q++){
    auto& r=results[q];if(q)f<<',';f<<"{\"east\":"<<offsets[q][0]<<",\"north\":"<<offsets[q][1]<<",\"direction\":";vec(f,r.direction);
    f<<",\"found\":"<<(r.found?"true":"false");if(r.found){
      f<<",\"height\":"<<r.r-radius<<",\"sourceTriangle\":"<<r.source<<",\"compactedTriangle\":"<<r.compact<<",\"barycentric\":";vec(f,r.bary);f<<",\"vertices\":[";
      for(int j=0;j<3;j++){if(j)f<<',';auto id=r.ids[j];auto& v=vertices[id];f<<"{\"index\":"<<id<<",\"lattice\":["<<lattice[id*4]<<','<<lattice[id*4+1]<<','<<lattice[id*4+2]<<','<<lattice[id*4+3]<<"],\"position\":";
        vec(f,{v.bodyFixed[0],v.bodyFixed[1],v.bodyFixed[2]});f<<",\"preparedHeight\":"<<v.bodyFixed[3]<<",\"normal\":";vec(f,{v.normal[0],v.normal[1],v.normal[2]});f<<'}';}f<<']';
    }f<<'}';
  }f<<"]}";if(!f)throw std::runtime_error("regional physical probe output failed");
}
}
