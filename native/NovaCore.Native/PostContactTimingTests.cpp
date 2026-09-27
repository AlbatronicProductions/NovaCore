#include "PostContactTiming.h"
#include <iostream>
#include <chrono>
#include <sstream>
int main(int argc,char**argv){
  try{
    if(argc!=2)throw std::runtime_error("Expected fresh CSV path");
    _putenv_s("NOVACORE_POST_CONTACT_TIMINGS","");PostContactTiming disabled;
    disabled.Record(1,2,1,3,4,{});if(disabled.count||!disabled.rows.empty())throw std::runtime_error("Disabled observer allocated");
    _putenv_s("NOVACORE_POST_CONTACT_TIMINGS",argv[1]);PostContactTiming enabled;
    enabled.Submitted(42);enabled.Completed();enabled.Submitted(43);
    if(enabled.completedTerrain!=42)throw std::runtime_error("GPU completion joined to current submit");
    enabled.Completed();if(enabled.completedTerrain!=43)throw std::runtime_error("GPU identity not advanced");
    const auto*address=enabled.rows.data();const auto capacity=enabled.rows.capacity();
    std::array<double,11> gpu{};for(size_t j=0;j<gpu.size();j++)gpu[j]=double(j)+.125;
    auto start=std::chrono::steady_clock::now();
    for(size_t i=0;i<16384;i++){enabled.cpu[3]=double(i);enabled.Record(i+1,i+9,i+8,2.5,3.5,gpu);}
    const auto elapsed=std::chrono::duration<double,std::micro>(std::chrono::steady_clock::now()-start).count();
    if(enabled.rows.data()!=address||enabled.rows.capacity()!=capacity)throw std::runtime_error("Producer storage changed");
    bool refused=false;try{enabled.Record(0,0,0,0,0,{});}catch(const std::runtime_error&){refused=true;}
    if(!refused||enabled.count!=16384||enabled.rows.back().gpu!=gpu||enabled.rows.back().cpu[3]!=16383)throw std::runtime_error("Bounded copy identity");
    enabled.Write();std::ifstream file(argv[1]);std::string line;size_t lines=0;while(std::getline(file,line))lines++;
    if(lines!=16385)throw std::runtime_error("Incomplete export");
    enabled.Begin();for(double value:enabled.cpu)if(value==value)throw std::runtime_error("Early draw return retained stale timing");
    std::cout<<"POST_CONTACT_TIMING_PASS rows=16384 retainedBytes="<<enabled.rows.size()*sizeof(PostContactTiming::Row)<<" copyUsPerRow="<<elapsed/16384<<" noGpu=true\n";
    _putenv_s("NOVACORE_POST_CONTACT_TIMINGS","");return 0;
  }catch(const std::exception&e){std::cerr<<e.what()<<'\n';return 1;}
}
