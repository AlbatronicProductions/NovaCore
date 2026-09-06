#include "FacilityLightOcclusion.h"
#include <vulkan/vulkan.h>
#include <cfloat>
#include <vector>
#include <fstream>
#include <iostream>
#include <string>

using namespace nc::facility;
#include "PresentationGpuTestDevice.h"
struct Sample{float p[4],light[4];};

int main(int argc,char**argv){try{
 Require(argc==2,"expected visibility compute SPIR-V path");
 NcFacilityCasterDefinition d{};d.bodyId=399;d.facilityId=1;d.objectId=2;d.version=1;d.geometrySet=3;d.east[0]=d.north[1]=d.up[2]=1;d.origin[0]=6371023.934892259;d.foundationScale[0]=64;d.foundationScale[1]=48;d.foundationScale[2]=6.835569277405739;d.maximumRayDistance=2048;
 auto g=Prepare(d);Require(g.control[0]==5,"bounded authored primitive count");
 for(int invalid=0;invalid<4;invalid++){
  auto bad=d;if(invalid==0)bad.version=2;if(invalid==1)bad.maximumRayDistance=2049;if(invalid==2)bad.foundationScale[2]=-1;if(invalid==3)bad.up[2]=-1;
  bool rejected=false;try{Prepare(bad);}catch(const std::runtime_error&){rejected=true;}Require(rejected,"invalid caster definition accepted");
 }
 struct Case{std::array<double,3> p,l;bool blocked;};
 std::vector<Case> cases{
  {{-39.916683,-4.931719,-6.83556875},{.70006437,-.23549202,.67413158},true},
  {{-32.018057,-.000751,-6.83556952},{.70006437,-.23549202,.67413158},true},
  {{60,0,-6.8},{.7,0,.714142842},false},
  {{-40,0,-6},{-1,0,0},false},{{-40,0,-6},{1,0,0},true},
  {{-40,25,-6},{1,0,0},false},{{-40,24,-6},{1,0,0},true},
  {{-32,0,-3},{-1,0,0},false},{{-32,0,-3},{1,0,0},true},
  {{-32.001,0,-3},{1,0,0},true},{{0,40,-1},{0,0,1},false},
  {{0,37,0},{0,0,1},true},{{0,39,0},{0,0,1},false},
  {{-2081,0,-3},{1,0,0},false},{{-2079,0,-3},{1,0,0},true},
  {{5000,0,0},{1,0,0},false},{{-100,0,-6},{1,0,.01},true}
 };
 // Distinct directions at the same point, long/parallel/grazing rays, near/far
 // camera translations and a non-axis-aligned orthonormal facility frame.
 size_t compared=0;for(int rotated=0;rotated<2;rotated++)for(double camera:{0.,600.,1000000.}){
  auto current=g;if(rotated){double c=std::cos(.7),s=std::sin(.7);current.east[0]=c;current.east[1]=s;current.north[0]=-s;current.north[1]=c;}
  current.cameraLocal[0]=camera;LocalizeReceiverBounds(current);std::vector<Sample> samples;
  for(auto test:cases){Sample sample{};double len=std::sqrt(Dot(test.l.data(),test.l.data()));for(int k=0;k<3;k++){
    sample.p[k]=float(current.east[k]*(test.p[0]-camera)+current.north[k]*test.p[1]+current.up[k]*test.p[2]);
    sample.light[k]=float((current.east[k]*test.l[0]+current.north[k]*test.l[1]+current.up[k]*test.l[2])/len);
  }samples.push_back(sample);}
  Device device;auto results=device.Run(current,samples,argv[1]);
  for(size_t i=0;i<cases.size();i++){
    // The oracle uses the exact represented fragment input. At a million metres
    // the upstream FP32 terrain varying cannot encode a sub-millimetre edge.
    double p[3],l[3];const double* basis[]{current.east,current.north,current.up};for(int k=0;k<3;k++){p[k]=current.cameraLocal[k];l[k]=0;for(int j=0;j<3;j++){p[k]+=basis[k][j]*samples[i].p[j];l[k]+=basis[k][j]*samples[i].light[j];}}
    bool expected=Blocked(current,p,l);bool gpu=results[i*2][0]==0;
    if(camera==0&&!rotated)Require(expected==cases[i].blocked,"analytical expected case failed");
    if(gpu!=expected){
      // Bound FP32 local transform/light rounding, not an intersection bias.
      // Only mathematically boundary-ambiguous samples may differ from FP64.
      double error=8*double(FLT_EPSILON)*(std::max({std::abs(p[0]),std::abs(p[1]),std::abs(p[2])})+d.maximumRayDistance);
      auto inner=current,outer=current;for(uint32_t b=0;b<5;b++)for(int k=0;k<3;k++){inner.boxes[b].minimum[k]+=float(error);inner.boxes[b].maximum[k]-=float(error);outer.boxes[b].minimum[k]-=float(error);outer.boxes[b].maximum[k]+=float(error);}
      Require(!Blocked(inner,p,l)&&Blocked(outer,p,l),"GPU/CPU disagreement outside derived FP32 boundary interval");
      std::cout<<"FP32 boundary interval: case="<<i<<" rotation="<<rotated<<" camera="<<camera<<" toleranceMetres="<<error<<'\n';
    }
    Require(results[i*2][1]<=5,"unbounded caster loop");
    if(i==15)Require(results[i*2][1]==0,"outside bound entered caster loop");
    for(int k=0;k<3;k++){float wanted=gpu?float((.2f+.1f*k)*.025f+(.2f+.1f*k)*.01f):(.7f+.1f*k);Require(std::abs(results[i*2+1][k]-wanted)<1e-7f,"lighting composition mismatch");}
    compared++;
  }
 }
 {auto inactive=g;inactive.control[0]=0;Device device;std::vector<Sample> samples{{{-40,0,-6,0},{1,0,0,0}}};auto result=device.Run(inactive,samples,argv[1]);Require(result[0][0]==1&&result[0][1]==0&&result[0][2]==0,"inactive caster must bypass all visibility work");}
 Require(Device::validationErrors==0,"Vulkan presentation validation errors");
 std::cout<<"Facility visibility PASS: "<<compared<<" GPU/CPU cases; parallel/grazing/boundary/multiple-caster/Sun/camera/orientation/range/lighting; inactive and invalid-definition gates\n";return 0;
}catch(const std::exception&e){std::cerr<<e.what()<<'\n';return 1;}}
