// Diagnostic-only adjacent timestamp boundaries. No overlapping summation.
void ResidualStamp(App& a,VkCommandBuffer c,bool incoming,uint32_t point){
  if(!a.residualQueries)return;
  vkCmdWriteTimestamp(c,VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,a.residualQueries,(incoming?8u:0u)+point);
  if(point==0)a.residualRecorded[incoming?1:0]=true;
}
void ResidualInspect(App& a){
  for(uint32_t slot=0;slot<2;slot++)if(a.residualRecorded[slot]){
    uint64_t ticks[7]{};a.Check(vkGetQueryPoolResults(a.device,a.residualQueries,slot*8,7,sizeof ticks,ticks,sizeof(uint64_t),VK_QUERY_RESULT_64_BIT),"residual timing readback failed");
    const double scale=double(a.timestampPeriodNanoseconds)/1e6;
    char row[768];std::snprintf(row,sizeof row,"Residual GPU: observedFrame=%llu; recordFrame=%llu; incoming=%u; resetMs=%.6f; resetBarrierMs=%.6f; cullMs=%.6f; cullBarrierMs=%.6f; compactMs=%.6f; compactBarrierMs=%.6f; blockMs=%.6f; fenceComplete=true",
      (unsigned long long)a.frame,(unsigned long long)a.residualRecordFrame,slot,
      (ticks[1]-ticks[0])*scale,(ticks[2]-ticks[1])*scale,(ticks[3]-ticks[2])*scale,
      (ticks[4]-ticks[3])*scale,(ticks[5]-ticks[4])*scale,(ticks[6]-ticks[5])*scale,(ticks[6]-ticks[0])*scale);
    a.Log(NC_LOG_ALWAYS,row);a.residualRecorded[slot]=false;
  }
}
