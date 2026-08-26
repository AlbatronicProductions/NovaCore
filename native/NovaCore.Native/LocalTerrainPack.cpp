#include "LocalTerrainPack.h"
#include <algorithm>
#include <chrono>
#include <cmath>
#include <cstring>
#include <fstream>
#include <windows.h>
#include <bcrypt.h>

namespace nc::localterrain { namespace {
uint32_t Read32(const uint8_t*p){uint32_t v;std::memcpy(&v,p,4);return v;}uint64_t Read64(const uint8_t*p){uint64_t v;std::memcpy(&v,p,8);return v;}
float ReadFloat(const uint8_t*p){float v;std::memcpy(&v,p,4);return v;}
bool Decode(Codec codec,const std::vector<uint8_t>&source,std::vector<uint8_t>&target,uint32_t expected){
  target.clear();target.resize(expected);if(codec==Codec::Raw){if(source.size()!=expected)return false;std::memcpy(target.data(),source.data(),expected);return true;}
  size_t input=0,output=0;while(input<source.size()){const uint8_t control=source[input++];if((control&0x80u)==0){const size_t count=size_t(control)+1;if(input+count>source.size()||output+count>target.size())return false;std::memcpy(target.data()+output,source.data()+input,count);input+=count;output+=count;}else{const size_t count=size_t(control&0x7fu)+3;if(input>=source.size()||output+count>target.size())return false;std::memset(target.data()+output,source[input++],count);output+=count;}}return output==target.size();
}
bool Digest(const SectorId&id,const Payload&p,std::array<uint8_t,32>&result){
  std::array<uint8_t,28>identity{};std::memcpy(identity.data(),&id.bodyId,8);std::memcpy(identity.data()+8,&id.terrainVersion,4);identity[12]=uint8_t(id.face);identity[13]=uint8_t(id.level);identity[14]=uint8_t(id.detailFrequency);identity[15]=uint8_t(id.payloadVersion);std::memcpy(identity.data()+16,&id.x,4);std::memcpy(identity.data()+20,&id.y,4);
  BCRYPT_ALG_HANDLE algorithm{};BCRYPT_HASH_HANDLE hash{};DWORD objectBytes{},resultBytes{};std::vector<uint8_t>object;
  if(BCryptOpenAlgorithmProvider(&algorithm,BCRYPT_SHA256_ALGORITHM,nullptr,0)<0)return false;auto close=[&]{if(hash)BCryptDestroyHash(hash);if(algorithm)BCryptCloseAlgorithmProvider(algorithm,0);};
  if(BCryptGetProperty(algorithm,BCRYPT_OBJECT_LENGTH,reinterpret_cast<PUCHAR>(&objectBytes),sizeof objectBytes,&resultBytes,0)<0){close();return false;}object.resize(objectBytes);if(BCryptCreateHash(algorithm,&hash,object.data(),objectBytes,nullptr,0,0)<0){close();return false;}
  auto add=[&](const uint8_t*data,size_t bytes){return BCryptHashData(hash,const_cast<PUCHAR>(data),static_cast<ULONG>(bytes),0)>=0;};const bool valid=add(identity.data(),24)&&add(p.albedoBc7.data(),p.albedoBc7.size())&&add(p.elevationBc4.data(),p.elevationBc4.size())&&add(p.normalBc5.data(),p.normalBc5.size())&&BCryptFinishHash(hash,result.data(),static_cast<ULONG>(result.size()),0)>=0;close();return valid;
}
}

bool Pack::Open(const std::string&path,std::string&error){
  path_.clear();records_.clear();std::ifstream input(path,std::ios::binary);std::array<uint8_t,HeaderBytes>header{};if(!input.read(reinterpret_cast<char*>(header.data()),header.size())){error="local NCCUBE2 header missing or truncated";return false;}
  if(std::memcmp(header.data(),"NCCUBE2\0",8)!=0||Read32(header.data()+8)!=2||Read32(header.data()+12)!=HeaderBytes||Read32(header.data()+16)!=RecordHeaderBytes){error="local NCCUBE2 header contract mismatch";return false;}
  interior_=Read32(header.data()+20);gutter_=Read32(header.data()+24);extent_=Read32(header.data()+28);const uint32_t count=Read32(header.data()+32);bodyId_=Read64(header.data()+40);terrainVersion_=Read32(header.data()+48);minimumLevel_=header[52];maximumLevel_=header[53];residualMinimum_=ReadFloat(header.data()+56);residualMaximum_=ReadFloat(header.data()+60);
  if(!count||!bodyId_||!terrainVersion_||interior_+2*gutter_!=extent_||minimumLevel_<3||maximumLevel_<minimumLevel_||maximumLevel_>20||!std::isfinite(residualMinimum_)||!std::isfinite(residualMaximum_)||residualMaximum_<=residualMinimum_){error="local NCCUBE2 hierarchy contract mismatch";return false;}
  records_.reserve(count);
  for(uint32_t index=0;index<count;index++){std::array<uint8_t,RecordHeaderBytes>bytes{};if(!input.read(reinterpret_cast<char*>(bytes.data()),bytes.size())){error="local NCCUBE2 record header truncated";return false;}Record record{};record.id={Read64(bytes.data()),Read32(bytes.data()+8),bytes[12],bytes[13],Read32(bytes.data()+16),Read32(bytes.data()+20),bytes[14],bytes[15]};record.payloadOffset=Read64(bytes.data()+24);for(uint32_t channel=0;channel<3;channel++){record.storedBytes[channel]=Read32(bytes.data()+32+channel*4);record.gpuBytes[channel]=Read32(bytes.data()+44+channel*4);record.codecs[channel]=Codec(bytes[56+channel]);}std::memcpy(record.digest.data(),bytes.data()+64,32);
    const uint32_t size=1u<<record.id.level;const bool inconsistentPayload=!records_.empty()&&(record.id.detailFrequency!=records_.front().id.detailFrequency||record.id.payloadVersion!=records_.front().id.payloadVersion);if(record.id.bodyId!=bodyId_||record.id.terrainVersion!=terrainVersion_||record.id.face>=6||record.id.level<minimumLevel_||record.id.level>maximumLevel_||record.id.x>=size||record.id.y>=size||!record.id.detailFrequency||(record.id.payloadVersion!=1u&&record.id.payloadVersion!=PayloadVersion)||inconsistentPayload||record.payloadOffset!=uint64_t(input.tellg())||record.gpuBytes!=std::array<uint32_t,3>{Bc7Bytes,Bc4Bytes,Bc5Bytes}||std::any_of(record.storedBytes.begin(),record.storedBytes.end(),[](uint32_t value){return value==0;})||std::any_of(record.codecs.begin(),record.codecs.end(),[](Codec value){return value!=Codec::Raw&&value!=Codec::PackBits;})){error="local NCCUBE2 record identity/layout invalid";return false;}
    if(std::find_if(records_.begin(),records_.end(),[&](const Record&other){return other.id==record.id;})!=records_.end()){error="local NCCUBE2 sector identity duplicated";return false;}records_.push_back(record);input.seekg(static_cast<std::streamoff>(record.storedBytes[0]+record.storedBytes[1]+record.storedBytes[2]),std::ios::cur);if(!input){error="local NCCUBE2 record payload truncated";return false;}}
  input.peek();if(!input.eof()){error="local NCCUBE2 trailing bytes";return false;}path_=path;return true;
}
const Record*Pack::Find(const SectorId&id)const{auto found=std::lower_bound(records_.begin(),records_.end(),id,[](const Record&record,const SectorId&value){return record.id<value;});return found!=records_.end()&&found->id==id?&*found:nullptr;}
bool Pack::Read(const SectorId&id,Payload&payload,std::string&error)const{
  const auto*record=Find(id);if(!record){error="local terrain sector is outside sparse package";return false;}const auto start=std::chrono::steady_clock::now();std::ifstream input(path_,std::ios::binary);input.seekg(record->payloadOffset);std::array<std::vector<uint8_t>,3>stored;for(uint32_t channel=0;channel<3;channel++){stored[channel].resize(record->storedBytes[channel]);if(!input.read(reinterpret_cast<char*>(stored[channel].data()),stored[channel].size())){error="local terrain payload read failed";return false;}}
  payload={};payload.id=id;payload.storedBytes=record->storedBytes[0]+record->storedBytes[1]+record->storedBytes[2];payload.transcodedBytes=record->gpuBytes[0]+record->gpuBytes[1]+record->gpuBytes[2];if(!Decode(record->codecs[0],stored[0],payload.albedoBc7,record->gpuBytes[0])||!Decode(record->codecs[1],stored[1],payload.elevationBc4,record->gpuBytes[1])||!Decode(record->codecs[2],stored[2],payload.normalBc5,record->gpuBytes[2])){error="local terrain supercompression decode failed";return false;}std::array<uint8_t,32>digest{};payload.digestValid=Digest(id,payload,digest)&&digest==record->digest;payload.transcodeMilliseconds=std::chrono::duration<double,std::milli>(std::chrono::steady_clock::now()-start).count();if(!payload.digestValid){error="local terrain payload digest mismatch";return false;}return true;
}
} // namespace nc::localterrain
