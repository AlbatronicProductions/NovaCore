#include "PresentationGpuTestDevice.h"
#include <cmath>
#include <cfloat>
#include <iostream>
struct Context{float high[4]{},low[4]{};};
struct Sample{float relative[4]{},unused[4]{};};
int main(int argc,char**argv){try{
 Require(argc==2,"expected surface material compute shader");
 // Fixed physical receivers on three differently oriented bodies, observed
 // from many local camera positions. No ray/shell height enters this contract.
 const double directions[3][3]{{.1433224599406355,.4788205718227514,.8661348234979923},{1,0,0},{0,0,-1}};
 double maximumError=0;unsigned checked=0;
 for(const auto& direction:directions)for(double radius:{1737400.,6371008.8,69911000.})for(double distance:{1.,50.,500.,10000.}){
  double center[3];for(int k=0;k<3;k++)center[k]=direction[k]*(radius+15.134892258793116);
  Context context;double camera[3];for(int k=0;k<3;k++){camera[k]=center[k]+distance*(k==0?.71:k==1?-.19:.37);context.high[k]=float(camera[k]);context.low[k]=float(camera[k]-double(context.high[k]));}
  std::vector<Sample> samples;std::vector<std::array<double,3>> physical;
  for(int i=-2;i<=2;i++){
   Sample sample;std::array<double,3> p;
   for(int k=0;k<3;k++){p[k]=center[k]+i*(k==0?.013:k==1?.029:-.017);sample.relative[k]=float(p[k]-camera[k]);}
   samples.push_back(sample);physical.push_back(p);
  }
  Device device;auto result=device.Run(context,samples,argv[1]);
  for(size_t i=0;i<samples.size();i++)for(int k=0;k<3;k++){
   const double got=double(result[2*i][k])+double(result[2*i+1][k]);
   const double error=std::abs(got-physical[i][k]);maximumError=std::max(maximumError,error);
   // Bound input/output rounding using the spacing of their represented
   // FP32 values and FP64 body addition. This is a test bound, not a bias.
   auto ulp=[](float v){return double(std::nextafter(v,INFINITY))-double(v);};
   const double bound=ulp(samples[i].relative[k])+ulp(context.low[k])+ulp(result[2*i+1][k])+8*DBL_EPSILON*std::abs(physical[i][k]);
   Require(std::isfinite(got)&&error<=bound,"GPU receiver detached from fixed physical point beyond transport precision");
   checked++;
  }
 }
 Require(Device::validationErrors==0,"Vulkan presentation validation errors");
 std::cout<<"Surface material coordinates PASS: "<<checked<<" GPU coordinate components; 3 orientations, 3 body radii, 4 camera distances; maxPhysicalErrorMetres="<<maximumError<<"; FP64 body identity / FP32 local differential; no shell input\n";
 return 0;
}catch(const std::exception&e){std::cerr<<e.what()<<'\n';return 1;}}
