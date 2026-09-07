"""Minimal private native measurements. No geometry scans or capture resources."""
def instrument(source):
    s=source.replace('\r\n','\n')
    def rep(a,b):
        nonlocal s
        assert s.count(a)==1,(s.count(a),a[:90]);s=s.replace(a,b,1)
    rep('  uint64_t cpuTimingSamples{};\n','''  uint64_t cpuTimingSamples{};
  const bool performanceFrameLog=std::getenv("NOVACORE_PERFORMANCE_FRAME_LOG")!=nullptr;
  const bool performanceClipping=std::getenv("NOVACORE_PERFORMANCE_CLIPPING")!=nullptr;
  const bool performanceCountersOff=std::getenv("NOVACORE_PERFORMANCE_COUNTERS_OFF")!=nullptr;
  const char* performanceShaderInfo=std::getenv("NOVACORE_SHADER_INFO");
  bool performanceShaderDumped=false;
  std::array<double,8> performanceCpu{};
  double performanceAcquireMs{},performanceRecreateMs{},performanceShaderInfoMs{};
  uint32_t performanceRecreateCount{};
  VkResult performanceAcquireResult=VK_SUCCESS,performancePresentResult=VK_SUCCESS;
  std::array<uint64_t,5> performancePipeline{};
  uint64_t performancePipelineFrame{},performancePipelineGeneration{};
''')
    rep('  const char *ex = VK_KHR_SWAPCHAIN_EXTENSION_NAME;','''  const char* performanceExtensions[]{VK_KHR_SWAPCHAIN_EXTENSION_NAME,VK_AMD_SHADER_INFO_EXTENSION_NAME};
  if(a.performanceShaderInfo){uint32_t count=0;a.Check(vkEnumerateDeviceExtensionProperties(a.physical,nullptr,&count,nullptr),"extension enumeration failed");std::vector<VkExtensionProperties> available(count);a.Check(vkEnumerateDeviceExtensionProperties(a.physical,nullptr,&count,available.data()),"extension enumeration failed");if(std::none_of(available.begin(),available.end(),[](const auto& v){return std::strcmp(v.extensionName,VK_AMD_SHADER_INFO_EXTENSION_NAME)==0;}))throw std::runtime_error("requested AMD shader info unavailable");}''')
    rep('  ci.enabledExtensionCount = 1;\n  ci.ppEnabledExtensionNames = &ex;','  ci.enabledExtensionCount=a.performanceShaderInfo?2u:1u;\n  ci.ppEnabledExtensionNames=performanceExtensions;')
    rep('  a.Check(vkCreateQueryPool(a.device,&pipelineStatistics,nullptr,&a.anchoredPipelineStatistics),','  if(a.performanceClipping)pipelineStatistics.pipelineStatistics|=VK_QUERY_PIPELINE_STATISTIC_CLIPPING_INVOCATIONS_BIT;\n  a.Check(vkCreateQueryPool(a.device,&pipelineStatistics,nullptr,&a.anchoredPipelineStatistics),')
    rep('''  std::array<uint64_t,4> values{};
  const auto result=vkGetQueryPoolResults(a.device,a.anchoredPipelineStatistics,0,1,
    sizeof values,values.data(),sizeof values,VK_QUERY_RESULT_64_BIT);
  if(result!=VK_SUCCESS)return;''','''  std::array<uint64_t,5> extended{};
  const size_t count=a.performanceClipping?5u:4u;
  const auto result=vkGetQueryPoolResults(a.device,a.anchoredPipelineStatistics,0,1,count*sizeof(uint64_t),extended.data(),count*sizeof(uint64_t),VK_QUERY_RESULT_64_BIT);
  if(result!=VK_SUCCESS)return;
  std::array<uint64_t,4> values{};std::copy_n(extended.begin()+(a.performanceClipping?1:0),4,values.begin());
  a.performancePipeline={a.performanceClipping?extended[0]:0,values[0],values[1],values[2],values[3]};
  a.performancePipelineFrame=a.anchoredPipelineStatisticsTerrainFrame;a.performancePipelineGeneration=a.anchoredPipelineStatisticsGeneration;''')
    rep('      vkCmdBindPipeline(c,VK_PIPELINE_BIND_POINT_GRAPHICS,rasterPipeline);','''      if(a.performanceShaderInfo&&!a.performanceShaderDumped){
        const auto begin=std::chrono::steady_clock::now();
        auto query=reinterpret_cast<PFN_vkGetShaderInfoAMD>(vkGetDeviceProcAddr(a.device,"vkGetShaderInfoAMD"));
        if(!query)throw std::runtime_error("shader info function unavailable");
        for(auto stage:{VK_SHADER_STAGE_VERTEX_BIT,VK_SHADER_STAGE_TESSELLATION_CONTROL_BIT,VK_SHADER_STAGE_TESSELLATION_EVALUATION_BIT,VK_SHADER_STAGE_FRAGMENT_BIT}){
          VkShaderStatisticsInfoAMD info{};size_t bytes=sizeof info;
          a.Check(query(a.device,rasterPipeline,stage,VK_SHADER_INFO_TYPE_STATISTICS_AMD,&bytes,&info),"shader statistics failed");
          char row[640];std::snprintf(row,sizeof row,"AMD shader statistics: oppositeFace=%u; stage=%u; vgpr=%u; sgpr=%u; ldsBytes=%zu; scratchBytes=%zu",rasterPipeline==a.productionBillboardOppositeFacePipeline?1u:0u,unsigned(stage),info.resourceUsage.numUsedVgprs,info.resourceUsage.numUsedSgprs,info.resourceUsage.ldsUsageSizeInBytes,info.resourceUsage.scratchMemUsageInBytes);a.Log(NC_LOG_ALWAYS,row);
          bytes=0;a.Check(query(a.device,rasterPipeline,stage,VK_SHADER_INFO_TYPE_DISASSEMBLY_AMD,&bytes,nullptr),"shader ISA size failed");std::vector<char> isa(bytes);a.Check(query(a.device,rasterPipeline,stage,VK_SHADER_INFO_TYPE_DISASSEMBLY_AMD,&bytes,isa.data()),"shader ISA failed");
          std::ofstream out(std::string(a.performanceShaderInfo)+"/stage-"+std::to_string(unsigned(stage))+".isa",std::ios::binary);out.write(isa.data(),bytes);if(!out)throw std::runtime_error("shader ISA write failed");
        }
        a.performanceShaderDumped=true;a.performanceShaderInfoMs=std::chrono::duration<double,std::milli>(std::chrono::steady_clock::now()-begin).count();
      }
      vkCmdBindPipeline(c,VK_PIPELINE_BIND_POINT_GRAPHICS,rasterPipeline);''')
    # Counter-off leaves the explicit factor-one control intact; only telemetry
    # sentinels (-1005/-1006 or ordinary negative target) become positive.
    rep('const bool productionSurface=a.submission->planetarySurfaceMode==NC_PLANETARY_SURFACE_PRODUCTION_CUBE;',
        'if(a.performanceCountersOff&&(a.submission->productionBillboardFlags&32u)==0u)gpuInput.targetTexelPixels=std::abs(a.submission->planetaryGpu.targetTexelPixels);const bool productionSurface=a.submission->planetarySurfaceMode==NC_PLANETARY_SURFACE_PRODUCTION_CUBE;')
    rep('void Recreate(App &a) {','void Recreate(App &a) {\n  const auto performanceBegin=std::chrono::steady_clock::now();')
    rep('  a.Log(NC_LOG_ALWAYS, "Swapchain recreated after resize");','  a.Log(NC_LOG_ALWAYS, "Swapchain recreated after resize");\n  a.performanceRecreateMs+=std::chrono::duration<double,std::milli>(std::chrono::steady_clock::now()-performanceBegin).count();++a.performanceRecreateCount;')
    rep('''  VkResult ar = vkAcquireNextImageKHR(a.device, a.swapchain, UINT64_MAX,
                                      a.imageAvailable, {}, &image);''','''  const auto acquireBegin=std::chrono::steady_clock::now();
  VkResult ar = vkAcquireNextImageKHR(a.device, a.swapchain, UINT64_MAX,
                                      a.imageAvailable, {}, &image);
  a.performanceAcquireMs=std::chrono::duration<double,std::milli>(std::chrono::steady_clock::now()-acquireBegin).count();a.performanceAcquireResult=ar;''')
    rep('  if(a.earthSubmissionTraceRemaining&&','  a.performancePresentResult=pr;\n  if(a.earthSubmissionTraceRemaining&&')
    rep('a.cpuRecordMs+=recordMs;a.cpuSubmitMs+=submitMs;a.cpuPresentMs+=presentMs;','a.performanceCpu[5]=recordMs;a.performanceCpu[6]=submitMs;a.performanceCpu[7]=presentMs;a.cpuRecordMs+=recordMs;a.cpuSubmitMs+=submitMs;a.cpuPresentMs+=presentMs;')
    rep('  if((a.submission->productionBillboardFlags&2u)!=0u){a.c3CpuMs[0].push_back(updateMs);','''  a.performanceCpu[0]=updateMs;a.performanceCpu[1]=fenceWaitMs;a.performanceCpu[2]=std::chrono::duration<double,std::milli>(inspectionEnd-fenceEnd).count();a.performanceCpu[3]=std::chrono::duration<double,std::milli>(callbackEnd-inspectionEnd).count();a.performanceCpu[4]=std::chrono::duration<double,std::milli>(updateEnd-callbackEnd).count();
  if((a.submission->productionBillboardFlags&2u)!=0u){a.c3CpuMs[0].push_back(updateMs);''')
    rep('        auto frameBegin=std::chrono::steady_clock::now();auto now = frameBegin;','        a.performanceCpu.fill(0);a.performanceAcquireMs=0;a.performanceRecreateMs=0;a.performanceShaderInfoMs=0;a.performanceRecreateCount=0;a.performanceAcquireResult=VK_SUCCESS;a.performancePresentResult=VK_SUCCESS;\n        auto frameBegin=std::chrono::steady_clock::now();auto now = frameBegin;')
    rep('        frames++;','''        if(a.performanceFrameLog){
          const auto& pc=a.performanceCpu;const double scoped=pc[0]+a.performanceAcquireMs+pc[5]+pc[6]+pc[7]+a.performanceRecreateMs;
          char message[2048];const int n=std::snprintf(message,sizeof message,
          "Performance host frame: frame=%llu; generation=%llu; pupil=%u; flags=%u; update=%.6f; fenceWait=%.6f; inspection=%.6f; hostCallback=%.6f; validationUpload=%.6f; acquire=%.6f; record=%.6f; submit=%.6f; present=%.6f; recreate=%.6f; recreateCount=%u; acquireResult=%d; presentResult=%d; shaderInfo=%.6f; total=%.6f; unattributed=%.6f; gpuFrame=%llu; geometryFrame=%llu; gpuGeneration=%llu; gpuTotal=%.6f; gpuCandidate=%.6f; gpuCullAndPreparation=%.6f; clippingInput=%llu; clippingOutput=%llu; fragments=%llu; tcsPatches=%llu; tesInvocations=%llu",
          (unsigned long long)a.frame,(unsigned long long)a.productionBillboardGeneration,a.productionBillboardRasterFrameIdentity,a.submission->productionBillboardFlags,pc[0],pc[1],pc[2],pc[3],pc[4],a.performanceAcquireMs,pc[5],pc[6],pc[7],a.performanceRecreateMs,a.performanceRecreateCount,int(a.performanceAcquireResult),int(a.performancePresentResult),a.performanceShaderInfoMs,frameMs,frameMs-scoped,(unsigned long long)a.lastGpuTimingFrame,(unsigned long long)a.performancePipelineFrame,(unsigned long long)a.performancePipelineGeneration,a.lastGpuTimingMs[0],a.lastGpuTimingMs[9],a.lastGpuTimingMs[7],(unsigned long long)a.performancePipeline[0],(unsigned long long)a.performancePipeline[1],(unsigned long long)a.performancePipeline[2],(unsigned long long)a.performancePipeline[3],(unsigned long long)a.performancePipeline[4]);
          if(n<0||size_t(n)>=sizeof message)throw std::runtime_error("performance row truncated");a.Log(NC_LOG_ALWAYS,message);
        }
        frames++;''')
    rep('  if((a.submission->productionBillboardFlags&2u)!=0u)\n    for(uint32_t i=0;i<App::TimestampCount;i++)a.c3GpuMs[i].push_back(values[i]);','''  if(a.performanceFrameLog){char row[640];std::snprintf(row,sizeof row,"Performance GPU frame: frame=%llu; total=%.6f; detailed=%.6f; background=%.6f; preSurface=%.6f; scene=%.6f; toneMap=%.6f; cullAndPreparation=%.6f; materialsOverlays=%.6f; candidate=%.6f; globalFill=%.6f",(unsigned long long)a.lastGpuTimingFrame,values[0],values[2],values[3],values[4],values[5],values[6],values[7],values[8],values[9],values[10]);a.Log(NC_LOG_ALWAYS,row);}
  if((a.submission->productionBillboardFlags&2u)!=0u)
    for(uint32_t i=0;i<App::TimestampCount;i++)a.c3GpuMs[i].push_back(values[i]);''')
    return s
