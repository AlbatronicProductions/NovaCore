#include <algorithm>
#include <cmath>
#include <cstdint>
#include <fstream>
#include <iostream>
#include <limits>
#include <stdexcept>

static float clamp(float x,float lo,float hi){return std::clamp(x,lo,hi);}
static float mix(float a,float b,float t){return a*(1-t)+b*t;}
using std::sqrt;using std::abs;using std::isnan;using std::isinf;
#include "shaders/production_tessellation_factor.glsl"

static void Check(bool b,const char* text){if(!b)throw std::runtime_error(text);}
static float Old(const float* v){
  const float dx=(v[0]/v[2]-v[3]/v[5])*v[9],dy=(v[1]/v[2]-v[4]/v[5])*v[10];
  float pixels=.5f*sqrt(dx*dx+dy*dy),skew=(v[8]-.8f)/.2f;
  if(skew>0){const float mw=abs((v[2]+v[5])*.5f);float comp=.5f*1.41421356237f*v[10]*(.6f*v[7])/(mw*v[11]);pixels=mix(pixels,comp,skew);}
  return clamp(pixels/3*(1-clamp(v[6]/50,0,1)),1,64);
}
static float Current(const float* v){return terrainEdgeFactor(v[0],v[1],v[2],v[3],v[4],v[5],v[6],v[7],v[8],v[9],v[10],v[11]);}
int main(int argc,char** argv){try{
  float p[]{1,1,0,2,2,10,60,10,.5f,3440,1440,.57735026f};
  Check(!std::isfinite(Old(p))&&Current(p)==1,"zero-fade eye-plane regression");
  p[6]=10; p[5]=0; p[8]=.85f;
  Check(!std::isfinite(Old(p))&&Current(p)==64,"active eye-plane regression");
  uint64_t samples=0,finiteUnchanged=0;uint32_t rng=713u;
  auto random=[&](){rng=rng*1664525u+1013904223u;return float(rng>>8)/16777216.f;};
  for(int i=0;i<100000;i++){
    float a[]{random()*100-50,random()*100-50,random()*200+.01f,random()*100-50,random()*100-50,random()*200+.01f,random()*200,random()*60+.001f,random(),3440,1440,.57735026f};
    float old=Old(a),now=Current(a);Check(std::isfinite(now)&&now>=1&&now<=64,"bounded finite ordinary output");
    Check(old==now,"ordinary finite law changed");finiteUnchanged++;
  }
  // The positive refinement limit must stay finite through signed zero,
  // subnormal depths, opposite signs, and midpoint-depth cancellation.
  for(float w:{0.f,-0.f,std::numeric_limits<float>::denorm_min(),-std::numeric_limits<float>::denorm_min(),1e-20f,-1e-20f,1e-7f,-1e-7f,1.f,-1.f})
  for(float wb:{0.f,-0.f,1e-20f,-1e-20f,1e-7f,-1e-7f,1.f,-1.f})
  for(float d:{0.f,10.f,std::nextafter(50.f,0.f),50.f,std::nextafter(50.f,100.f),1000000.f})
  for(float alignment:{0.f,.8f,.9f,1.f,std::nextafter(1.f,2.f)}){
    float a[]{1,2,w,3,4,wb,d,10,alignment,3440,1440,.57735026f};float f=Current(a);
    Check(std::isfinite(f)&&f>=1&&f<=64,"bounded singular neighborhood output");if(d>=50)Check(f==1,"inactive range preserved");samples++;
  }
  uint64_t replayed=0;double maxDifference=0;
  if(argc==2){std::ifstream input(argv[1]);Check(bool(input),"replay input missing");float v[13];
    while(input>>v[0]){for(int i=1;i<13;i++)Check(bool(input>>v[i]),"truncated replay row");float result=Current(v);Check(std::isfinite(result)&&result>=1&&result<=64,"replay non-finite factor");
      const double difference=abs(double(result)-v[12]);maxDifference=std::max(maxDifference,difference);Check(difference<=2e-5,"shared shader scalar arithmetic differs from offline model");replayed++;}
    Check(input.eof(),"invalid replay row");Check(replayed>0,"empty replay");}
  std::cout<<"{\"ordinaryUnchanged\":"<<finiteUnchanged<<",\"singularNeighborhood\":"<<samples<<",\"replayedEdges\":"<<replayed<<",\"maxDifference\":"<<maxDifference<<",\"gpuExposure\":false}\n";return 0;
}catch(const std::exception& e){std::cerr<<e.what()<<'\n';return 1;}}
