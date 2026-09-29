#include "PreparedSurfaceRaster.h"
#include <limits>
#include <stdexcept>
#include <iostream>
int main(){
  auto test=[](bool pass){if(!pass)throw std::runtime_error("prepared raster eligibility");};
  auto eligible=[](uint32_t family,uint32_t generation,uint32_t mode,uint32_t diagnostic,uint32_t flags,float target){return DirectPreparedSurfaceEligible(family,generation,mode,diagnostic,flags,target);};
  test(eligible(1,4,2,0,0,1.5f));
  test(!eligible(0,4,2,0,0,1.5f));test(!eligible(1,3,2,0,0,1.5f));test(!eligible(1,4,0,0,0,1.5f));
  for(uint32_t bit=1;bit<=32768;bit<<=1)test(!eligible(1,4,2,bit,0,1.5f));
  for(uint32_t bit=1024;bit<=32768;bit<<=1)test(!eligible(1,4,2,0,bit,1.5f));
  for(float value:{-1.f,-1001.f,-2000.f,std::numeric_limits<float>::infinity(),std::numeric_limits<float>::quiet_NaN()})test(!eligible(1,4,2,0,0,value));
  // Logging alone is harmless; the uploaded negative input is the factor-probe authority.
  test(eligible(1,4,2,0,2,1.5f));test(!eligible(1,4,2,0,2,-1.5f));
  std::cout<<"PASS prepared raster eligibility; GPU execution=0\n";
}
