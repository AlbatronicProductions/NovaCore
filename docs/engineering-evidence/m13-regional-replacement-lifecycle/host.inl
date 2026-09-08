// Private timestamp-only host scopes. Nested values are not additive parents.
static bool lifecycleReferenceTick=false;
extern "C" __declspec(dllexport) void ncLifecycleReference(int value){lifecycleReferenceTick=value!=0;}
double LifeNow(){return std::chrono::duration<double,std::milli>(std::chrono::steady_clock::now().time_since_epoch()).count();}
struct LifeScope{
  App& a;const char* name;uint64_t bytes;const char* label;double begin;
  LifeScope(App& app,const char* value,uint64_t count=0,const char* detail="none"):a(app),name(value),bytes(count),label(detail),begin(LifeNow()){}
  ~LifeScope(){char row[768];const double end=LifeNow();std::snprintf(row,sizeof row,"Lifecycle CPU: frame=%llu; name=%s; reference=%u; currentGeneration=%llu; incomingGeneration=%llu; beginMs=%.6f; endMs=%.6f; cpuMs=%.6f; bytes=%llu; label=%s",(unsigned long long)a.frame,name,lifecycleReferenceTick?1u:0u,(unsigned long long)a.productionBillboardGeneration,(unsigned long long)a.productionBillboardIncomingGeneration,begin,end,end-begin,(unsigned long long)bytes,label);a.Log(NC_LOG_ALWAYS,row);}
};
void LifeTransfers(App& a){
  char row[512];const uint64_t bytes=uint64_t(a.productionPendingUploads)*(ProductionAlbedoLayerBytes+ProductionElevationLayerBytes+ProductionLandLayerBytes);
  std::snprintf(row,sizeof row,"Lifecycle transfers: frame=%llu; reference=%u; pendingMaterialUploads=%u; copyBufferToImageCommands=%u; bytes=%llu; cumulativeMaterialUploadBytes=%llu; beforeHistoricalTotalScope=true",(unsigned long long)a.frame,lifecycleReferenceTick?1u:0u,a.productionPendingUploads,3u*a.productionPendingUploads,(unsigned long long)bytes,(unsigned long long)a.productionUploadBytes);a.Log(NC_LOG_ALWAYS,row);
}
