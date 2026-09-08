"""Bounded preparation gauntlet. Root owns builds; prior exit evidence is read-only."""
import json,os,pathlib,sys,subprocess,inspect
SOURCE=pathlib.Path(__file__).resolve().parent;ROOT=SOURCE.parents[2]
HERE=pathlib.Path(os.environ.get('NOVACORE_PREP_EVIDENCE_DIR',str(SOURCE))).resolve()
if HERE!=SOURCE:assert HERE.is_relative_to(ROOT/'build'),'Reproduction output must remain under build/'
HERE.mkdir(parents=True,exist_ok=True)
sys.path.insert(0,str(SOURCE.parent/'m13-exit-assessment'))
import assess
from records import unpack
assess.HERE=HERE;assess.OUT=ROOT/'build/m13-regional-preparation-blocker';assess.HOST=assess.OUT/'host'
OUT=assess.OUT;HOST=assess.HOST
execution=inspect.getsource(assess.execute).replace("    prep={k:","    markers+=['Prep identity','Prep compiler','Prep GPU oracle','Prep heap','Prep placement','Prep publication','Prep digest','NCSM1 physical slice','NCSM1 staged pupil publication']\n    prep={k:")
exec(execution,assess.__dict__)
old_instrument=assess.instrument
build_source=inspect.getsource(assess.build).replace('os.link(ROOT/', 'oracle.exists() or os.link(ROOT/').replace("write('private-host',", "write('private-host-v4',")
exec(build_source,assess.__dict__)
def instrument(source):
    s=old_instrument(source)
    s='#include <unordered_map>\n'+s
    anchor='void InspectProductionBillboardPublication(App &a){'
    assert s.count(anchor)==1
    s=s.replace(anchor,(SOURCE/'oracle.inl').read_text()+'\n'+anchor)
    anchor='  RetainCurrentProductionBillboardWorkAsSpare(a);'
    assert s.count(anchor)==1;s=s.replace(anchor,'  PrepIdentityOracle(a);\n'+anchor)
    anchor='allocation.memoryTypeIndex=Memory(a,requirements.memoryTypeBits,VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT|VK_MEMORY_PROPERTY_HOST_COHERENT_BIT);'
    assert s.count(anchor)==1
    s=s.replace(anchor,anchor+'''
    const bool physicalWorking=std::strcmp(failure,"incoming production billboard physical buffer failed")==0||std::strcmp(failure,"regional pupil staging allocation failed")==0;
    if(physicalWorking&&std::getenv("NOVACORE_PREP_DEVICE_LOCAL"))allocation.memoryTypeIndex=Memory(a,requirements.memoryTypeBits,VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT|VK_MEMORY_PROPERTY_HOST_COHERENT_BIT|VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT);
    if(std::getenv("NOVACORE_PREP_HEAPS")||physicalWorking){VkPhysicalDeviceMemoryProperties props{};vkGetPhysicalDeviceMemoryProperties(a.physical,&props);auto type=props.memoryTypes[allocation.memoryTypeIndex];auto heap=props.memoryHeaps[type.heapIndex];char row[512];std::snprintf(row,sizeof row,"Prep heap: label=%s; bytes=%llu; allocationBytes=%llu; type=%u; properties=%u; heap=%u; heapFlags=%u; heapBytes=%llu",failure,(unsigned long long)size,(unsigned long long)allocation.allocationSize,allocation.memoryTypeIndex,type.propertyFlags,type.heapIndex,heap.flags,(unsigned long long)heap.size);a.Log(NC_LOG_ALWAYS,row);if(physicalWorking)for(uint32_t i=0;i<props.memoryTypeCount;i++){const auto& t=props.memoryTypes[i];std::snprintf(row,sizeof row,"Prep placement memory type: type=%u; properties=%u; heap=%u; heapFlags=%u; heapBytes=%llu",i,t.propertyFlags,t.heapIndex,props.memoryHeaps[t.heapIndex].flags,(unsigned long long)props.memoryHeaps[t.heapIndex].size);a.Log(NC_LOG_ALWAYS,row);}}
''')
    anchor='vkUpdateDescriptorSets(a.device,7,writes,0,nullptr);'
    assert s.count(anchor)==1
    s=s.replace(anchor,anchor+'''if(incoming&&!a.productionBillboardAuthoritative){for(uint32_t i:{0u,6u}){writes[i].dstBinding=38u+i;vkUpdateDescriptorSets(a.device,1,&writes[i],0,nullptr);}}''')
    anchor='  createCandidateCompute("shaders/production_spherical_billboard_incoming_prepare.comp.spv",a.productionBillboardIncomingPreparePipeline,"incoming production billboard frame preparation pipeline failed");'
    assert s.count(anchor)==1
    s=s.replace(anchor,anchor+'''
  if(a.performanceShaderInfo){
    auto query=reinterpret_cast<PFN_vkGetShaderInfoAMD>(vkGetDeviceProcAddr(a.device,"vkGetShaderInfoAMD"));
    for(uint32_t which=0;which<2;which++){
      auto pipeline=which?a.productionBillboardIncomingPreparePipeline:a.productionBillboardPreparePipeline;
      VkShaderStatisticsInfoAMD info{};size_t bytes=sizeof info;a.Check(query(a.device,pipeline,VK_SHADER_STAGE_COMPUTE_BIT,VK_SHADER_INFO_TYPE_STATISTICS_AMD,&bytes,&info),"prep stats failed");
      char row[768];std::snprintf(row,sizeof row,"Prep compiler: incoming=%u; vgpr=%u; sgpr=%u; ldsBytes=%zu; scratchBytes=%zu; workgroupX=%u; workgroupY=%u; workgroupZ=%u; availableVgprs=%u; availableSgprs=%u; physicalVgprs=%u; physicalSgprs=%u",which,info.resourceUsage.numUsedVgprs,info.resourceUsage.numUsedSgprs,info.resourceUsage.ldsUsageSizeInBytes,info.resourceUsage.scratchMemUsageInBytes,info.computeWorkGroupSize[0],info.computeWorkGroupSize[1],info.computeWorkGroupSize[2],info.numAvailableVgprs,info.numAvailableSgprs,info.numPhysicalVgprs,info.numPhysicalSgprs);a.Log(NC_LOG_ALWAYS,row);
      bytes=0;a.Check(query(a.device,pipeline,VK_SHADER_STAGE_COMPUTE_BIT,VK_SHADER_INFO_TYPE_DISASSEMBLY_AMD,&bytes,nullptr),"prep ISA size failed");std::vector<char> isa(bytes);a.Check(query(a.device,pipeline,VK_SHADER_STAGE_COMPUTE_BIT,VK_SHADER_INFO_TYPE_DISASSEMBLY_AMD,&bytes,isa.data()),"prep ISA failed");std::ofstream file(std::string(a.performanceShaderInfo)+"/prep-"+std::to_string(which)+".isa",std::ios::binary);file.write(isa.data(),bytes);if(!file)throw std::runtime_error("prep ISA write failed");
    }
  }
''')
    return s
assess.instrument=instrument

def execute(label,env):
    # Preserve complete compact rows from existing timing host and new oracle lines.
    original=assess.write
    def write(name,data):
        original(name,data)
    assess.write=write
    return assess.execute(label,['--scene=sol','--focus=earth','--surface-site=florida-launch','--physical-surface=m12d-natural-candidate','--solar-epoch=j2000','--benchmark-frames=1000','--log=startup,validation,vulkan'],env,'profile',HOST)

if __name__=='__main__':
    mode=sys.argv[1]
    if mode=='baseline':
        data=assess.verify_deployment();old=json.loads((SOURCE.parent/'m13-exit-assessment/baseline.json').read_text())
        assert data['deployment']==old['deployment'] and data['assets']==old['assets']
        assert not subprocess.check_output(['git','diff','--name-only'],cwd=ROOT,text=True).strip()
        assert not subprocess.check_output(['git','diff','--cached','--name-only'],cwd=ROOT,text=True).strip()
        data['main']=subprocess.check_output(['git','rev-parse','main'],cwd=ROOT,text=True).strip();data['M13.4']=subprocess.check_output(['git','rev-parse','m13.4-zero-contribution-terrain-material-noise^{}'],cwd=ROOT,text=True).strip();data['permanentBudgetBytes']=10485760
        assess.write('baseline',data);print('Banked M13.4 baseline, both deployments, 49 shaders/configuration and three physical assets verified.')
    elif mode=='build':assess.build()
    elif mode in ['oracle','profile','profile-repeat','placement-only']:
        env=assess.environment();env['NOVACORE_EARTH_ROUTE_VALIDATION']='regional';env['NOVACORE_PERFORMANCE_FRAME_LOG']='1'
        if mode=='placement-only':env['NOVACORE_PREP_DEVICE_LOCAL']='1'
        if mode=='oracle':
            env['NOVACORE_PREP_IDENTITY_ORACLE']='1';(OUT/'compiler').mkdir(exist_ok=True);env['NOVACORE_SHADER_INFO']=str(OUT/'compiler')
        execute(mode,env)
