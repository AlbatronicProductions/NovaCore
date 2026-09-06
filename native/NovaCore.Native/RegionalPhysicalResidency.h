#pragma once
#include "LocalTerrainPack.h"
#include <algorithm>
#include <array>
#include <condition_variable>
#include <deque>
#include <mutex>
#include <stdexcept>
#include <thread>

namespace nc::regionalphysical {
// A bounded physical cache, independent of presentation material arrays.
// The immutable catalog is authoritative even before I/O.
constexpr uint32_t MaximumRecords=896, MaskWords=(MaximumRecords+31)/32;
constexpr uint32_t ReadyCapacity=8, UploadBudget=8;
constexpr uint64_t MaximumPayloadBytes=uint64_t(MaximumRecords)*localterrain::R16Bytes;
struct alignas(16) Entry {
  uint32_t face{},level{},x{},y{};
  uint32_t offset{},resident{},neighbors{},reserved{};
  float minimum{},maximum{};uint32_t padding[2]{};
};
static_assert(sizeof(Entry)==48);
struct alignas(16) Catalog {
  uint32_t count{},minimumLevel{},maximumLevel{},faces{};
  uint32_t bodyLow{},bodyHigh{},terrainVersion{},physicalGeneration{4};
  // Dataset identity is the ordered vector of immutable record SHA-256s and
  // sector identities on the host, never a filename or allocation address.
  std::array<Entry,MaximumRecords> entries{};
};
struct Completion {uint32_t record{};localterrain::Payload payload;std::string error;};
class Residency final {
 public:
  explicit Residency(const localterrain::Pack& pack):pack_(pack),identity_(pack.Records()) {
    if(!pack.IsProductionLayout()||identity_.size()>MaximumRecords)
      throw std::runtime_error("regional physical catalog exceeds supported immutable physical contract");
    catalog.count=pack.RecordCount();catalog.minimumLevel=pack.MinimumLevel();catalog.maximumLevel=pack.MaximumLevel();
    catalog.bodyLow=uint32_t(pack.BodyId());catalog.bodyHigh=uint32_t(pack.BodyId()>>32);catalog.terrainVersion=pack.TerrainVersion();
    for(uint32_t i=0;i<catalog.count;++i){const auto& r=identity_[i];auto& e=catalog.entries[i];
      e.face=r.id.face;e.level=r.id.level;e.x=r.id.x;e.y=r.id.y;e.offset=i*(localterrain::R16Bytes/4);
      e.minimum=r.residualMinimum;e.maximum=r.residualMaximum;catalog.faces|=1u<<e.face;
      auto id=r.id;const uint32_t cells=1u<<id.level;
      if(id.x>0){--id.x;if(pack.Contains(id))e.neighbors|=1u;++id.x;}
      if(id.x+1<cells){++id.x;if(pack.Contains(id))e.neighbors|=2u;--id.x;}
      if(id.y>0){--id.y;if(pack.Contains(id))e.neighbors|=4u;++id.y;}
      if(id.y+1<cells){++id.y;if(pack.Contains(id))e.neighbors|=8u;--id.y;}
    }
    for(uint32_t i=0;i<catalog.count;++i){auto child=identity_[i].id;++child.level;child.x*=2;child.y*=2;
      bool covered=child.level<=catalog.maximumLevel;
      if(covered)for(uint32_t y=0;y<2;++y)for(uint32_t x=0;x<2;++x){auto c=child;c.x+=x;c.y+=y;covered=covered&&Covered(c);}
      contributes_[i]=!covered;if(!covered)++contributingRecords;
    }
    worker_=std::thread([this]{Work();});
  }
  ~Residency(){ {std::lock_guard lock(mutex_);stop_=true;}wake_.notify_all();if(worker_.joinable())worker_.join(); }
  Residency(const Residency&)=delete;Residency& operator=(const Residency&)=delete;
  bool Matches(const localterrain::Pack& pack,uint32_t physicalGeneration)const {
    if(physicalGeneration!=catalog.physicalGeneration||pack.BodyId()!=(uint64_t(catalog.bodyHigh)<<32|catalog.bodyLow)||pack.TerrainVersion()!=catalog.terrainVersion||pack.RecordCount()!=identity_.size())return false;
    const auto& records=pack.Records();for(size_t i=0;i<identity_.size();++i)
      if(records[i].id!=identity_[i].id||records[i].digest!=identity_[i].digest||records[i].residualMinimum!=identity_[i].residualMinimum||records[i].residualMaximum!=identity_[i].residualMaximum)return false;
    return true;
  }
  void Require(const std::array<uint32_t,MaskWords>& mask){
    std::lock_guard lock(mutex_);
    for(uint32_t i=0;i<catalog.count;++i)if(mask[i/32]&(1u<<(i%32))){
      if(catalog.entries[i].resident){++hits;continue;}
      if(!requested_[i]){requested_[i]=true;queue_.push_back(i);++requests;++misses;}
    }
    queueHighWater=std::max(queueHighWater,uint32_t(queue_.size()));wake_.notify_all();
  }
  bool Complete(const std::array<uint32_t,MaskWords>& mask)const {
    for(uint32_t i=0;i<catalog.count;++i)if((mask[i/32]&(1u<<(i%32)))&&!catalog.entries[i].resident)return false;
    return true;
  }
  bool AllContributingResident()const {for(uint32_t i=0;i<catalog.count;++i)if(contributes_[i]&&!catalog.entries[i].resident)return false;return true;}
  bool Pop(Completion& result){std::lock_guard lock(mutex_);if(ready_.empty())return false;result=std::move(ready_.front());ready_.pop_front();wake_.notify_all();return true;}
  void Published(const Completion& value){catalog.entries[value.record].resident=1;++loaded;readBytes+=value.payload.storedBytes;uploadBytes+=value.payload.elevationBc4.size();transcodeMs+=value.payload.transcodeMilliseconds;}
  Catalog catalog{};
  uint64_t requests{},hits{},misses{},loaded{},readBytes{},uploadBytes{};uint32_t queueHighWater{},contributingRecords{};double transcodeMs{};
 private:
  bool Covered(localterrain::SectorId id)const {if(pack_.Contains(id))return true;if(id.level>=catalog.maximumLevel)return false;
    ++id.level;id.x*=2;id.y*=2;for(uint32_t y=0;y<2;++y)for(uint32_t x=0;x<2;++x){auto c=id;c.x+=x;c.y+=y;if(!Covered(c))return false;}return true;}
  void Work(){for(;;){uint32_t record;
    {std::unique_lock lock(mutex_);wake_.wait(lock,[&]{return stop_||(!queue_.empty()&&ready_.size()<ReadyCapacity);});if(stop_)return;record=queue_.front();queue_.pop_front();}
    Completion result;result.record=record;
    // The existing record digest covers all channels. Read/verify that atomic
    // record, but retain/upload only residuals; material publication is unrelated.
    if(!pack_.Read(identity_[record].id,result.payload,result.error)||!result.payload.digestValid||result.payload.elevationBc4.size()!=localterrain::R16Bytes)
      result.error="regional physical record failed integrity/size validation: "+result.error;
    result.payload.albedoBc7.clear();result.payload.normalBc5.clear();result.payload.controlR8.clear();
    {std::lock_guard lock(mutex_);if(stop_)return;ready_.push_back(std::move(result));}
  }}
  const localterrain::Pack& pack_;const std::vector<localterrain::Record> identity_;
  std::array<bool,MaximumRecords> requested_{},contributes_{};std::mutex mutex_;std::condition_variable wake_;
  std::deque<uint32_t> queue_;std::deque<Completion> ready_;bool stop_{};std::thread worker_;
};
} // namespace nc::regionalphysical
