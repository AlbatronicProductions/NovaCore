"""Current-ticket one-frame parity hooks and lead-only runner; no build command.

Build integration for the lead:
  1. Wrap the CURRENT poll.instrument(s) with instrument_capture(s).
  2. Apply instrument_program(s) to the already instrumented Program.cs; retain
     the current fixed-pose managed driver and restore source/deployment afterward.
  3. Build/deploy that private host ONLY to build/m13-final-exit/host.
  4. Write private-capture-host.json here with native/managed SHA256,
     sourceRestored=true, productionDeploymentUnchanged=true, captureFrame=175,
     rawCaptureSlot=str(RAW). This module never builds or emits that manifest.
  5. Lead runs python -B parity.py florida LABEL (then inland with a new label).

Only NOVACORE_EXIT_CACHED_KEYS differs between paired processes. M13.5 GPU
working-data placement remains untouched. No shader/ISA or counter toggle.
Old evidence is read only for retained pure capture/correlation source hooks;
all execution, journals and the one raw slot resolve inside THIS ticket.
"""
from pathlib import Path
import inspect,json,sys
sys.dont_write_bytecode=True
HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[2]
OUT=ROOT/'build/m13-final-exit'
HOST=OUT/'host'
RAW=OUT/'parity-slot'
LIMIT=512*1024*1024
BANK='d4baab6940a57a46e478b98e36f5e45d1c4b558f'
NAMES=('pixels.bin','prepared.bin','selected.bin')
assess=None

def instrument_capture(s):
    def rep(a,b):
        nonlocal s
        assert s.count(a)==1,(s.count(a),a[:100]);s=s.replace(a,b,1)
    rep('  uint32_t surfaceDiagnostic{};','''  uint32_t surfaceDiagnostic{};
  const char* parityDirectory=std::getenv("NOVACORE_PIXEL_PARITY");
  VkBuffer parityPixels{};VkDeviceMemory parityPixelMemory{};void* parityPixelMapped=nullptr;
  bool parityRecorded=false,parityWritten=false;
  const uint64_t parityFrame=175;
  uint64_t parityRecordedFrame{},parityRecordedGeneration{};uint32_t parityRecordedPupil{};
''')
    rep('image.usage=VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT|VK_IMAGE_USAGE_INPUT_ATTACHMENT_BIT;','image.usage=VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT|VK_IMAGE_USAGE_INPUT_ATTACHMENT_BIT|(a.parityDirectory?VK_IMAGE_USAGE_TRANSFER_SRC_BIT:0);')
    rep('depth.usage=VK_IMAGE_USAGE_DEPTH_STENCIL_ATTACHMENT_BIT;','depth.usage=VK_IMAGE_USAGE_DEPTH_STENCIL_ATTACHMENT_BIT|(a.parityDirectory?VK_IMAGE_USAGE_TRANSFER_SRC_BIT:0);')
    rep('  ci.imageUsage = VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT;','  ci.imageUsage = VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT|(a.parityDirectory?VK_IMAGE_USAGE_TRANSFER_SRC_BIT:0);')
    rep('  VkAttachmentReference sceneColor{0,VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL};','  if(a.parityDirectory){attachments[0].storeOp=VK_ATTACHMENT_STORE_OP_STORE;attachments[2].storeOp=VK_ATTACHMENT_STORE_OP_STORE;}\n  VkAttachmentReference sceneColor{0,VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL};')
    rep('void DestroySubmission(App &a,bool destroyProductionBillboard=true) {','void DestroySubmission(App &a,bool destroyProductionBillboard=true) {\n  DestroyHostBuffer(a,a.parityPixels,a.parityPixelMemory,a.parityPixelMapped);')
    rep('  CreateRegionalPhysical(a);','  if(a.parityDirectory)CreateHostBuffer(a,VkDeviceSize(a.extent.width)*a.extent.height*16,VK_BUFFER_USAGE_TRANSFER_DST_BIT,a.parityPixels,a.parityPixelMemory,a.parityPixelMapped,"single parity frame");\n  CreateRegionalPhysical(a);')
    old=(Path(__file__).resolve().parent.parent/'post-m13.2-next-target/instrumentation.patch').read_text()
    begin=old.index('+  if(a.parityDirectory&&a.frame==a.parityFrame){')
    end=old.index('\n   a.Check(vkEndCommandBuffer',begin)
    code='\n'.join(line[1:] for line in old[begin:end].splitlines() if line.startswith('+'))+'\n'
    code=code.replace('a.parityRecorded=true;', 'a.parityRecordedFrame=a.frame;a.parityRecordedGeneration=a.productionBillboardGeneration;a.parityRecordedPupil=a.productionBillboardRasterFrameIdentity;a.parityRecorded=true;')
    code=code.replace('    const VkDeviceSize pixels=', '    if(!a.productionBillboardAuthoritative||a.productionBillboardVertexCount==0)throw std::runtime_error("parity requires published physical owner");\n    if(uint64_t(a.extent.width)*a.extent.height*16ull+uint64_t(a.productionBillboardVertexCount)*64ull+uint64_t(a.productionBillboardTriangleCount)*12ull>512ull*1024ull*1024ull)throw std::runtime_error("parity raw-slot budget exceeded");\n    const VkDeviceSize pixels=')
    rep('  a.Check(vkEndCommandBuffer(c), "command end failed");',code+'  a.Check(vkEndCommandBuffer(c), "command end failed");')
    rep('  InspectRegionalPhysical(a);','''  if(a.parityDirectory&&a.parityRecorded&&!a.parityWritten){
    if(a.frame!=a.parityRecordedFrame||a.productionBillboardGeneration!=a.parityRecordedGeneration||a.productionBillboardRasterFrameIdentity!=a.parityRecordedPupil)throw std::runtime_error("parity publication changed before readback");
    auto save=[&](const char*name,const void*data,size_t bytes){std::ofstream out(std::string(a.parityDirectory)+name,std::ios::binary);out.write(static_cast<const char*>(data),bytes);if(!out)throw std::runtime_error("pixel parity write failed");};
    save("/pixels.bin",a.parityPixelMapped,size_t(a.extent.width)*a.extent.height*16);
    save("/prepared.bin",a.productionBillboardPhysicalMapped,size_t(a.productionBillboardVertexCount)*sizeof(NcSphericalBillboardPhysicalVertex));
    auto* draw=static_cast<VkDrawIndexedIndirectCommand*>(a.productionBillboardIndirectMapped);if(draw->indexCount%3u||uint64_t(draw->indexCount)>uint64_t(a.productionBillboardTriangleCount)*3u)throw std::runtime_error("parity selected-index bounds");save("/selected.bin",a.productionBillboardCompactedMapped,size_t(draw->indexCount)*sizeof(uint32_t));
    char row[512];std::snprintf(row,sizeof row,"Pixel parity: frame=%llu; width=%u; height=%u; generation=%llu; pupil=%u; physicalGeneration=%u; vertices=%u",(unsigned long long)a.frame,a.extent.width,a.extent.height,(unsigned long long)a.productionBillboardGeneration,a.productionBillboardRasterFrameIdentity,a.submission->physicalSurfaceGeneration,a.productionBillboardVertexCount);a.Log(NC_LOG_ALWAYS,row);a.parityWritten=true;
  }
  InspectRegionalPhysical(a);''')
    if '#include <fstream>' not in s:s='#include <fstream>\n'+s
    correlation=(HERE.parent/'m13-regional-preparation-convergence/correlation.inl').read_text(encoding='utf-8')
    correlation=correlation.replace('if(!std::getenv("NOVACORE_COMPOSITION_CONTROL"))return;', 'if(!std::getenv("NOVACORE_COMPOSITION_CONTROL")||a.frame!=175)return;')
    rep('void CreateProductionBillboard(App &a){',correlation+'\nvoid CreateProductionBillboard(App &a){')
    rep('  UpdateRegionalPhysical(a);','  UpdateRegionalPhysical(a);\n  CompositionCorrelation(a);')
    anchor='  NcHostEvent e{NC_UPDATE_FRAME, NC_LOG_NONE, nullptr, in, a.submission};'
    rep(anchor,'  if(std::getenv("NOVACORE_COMPOSITION_CONTROL")){in={};in.viewportWidthPixels=a.extent.width;in.viewportHeightPixels=a.extent.height;}\n'+anchor)
    return s

def instrument_program(s):
    anchor='solarScene.ApplyPresentationInput(s.Camera,solarInput,out var rateChanged,out var pauseChanged);'
    assert s.count(anchor)==1
    assert 'NOVACORE_COMPOSITION_CONTROL' not in s
    return s.replace(anchor,'if(Environment.GetEnvironmentVariable("NOVACORE_COMPOSITION_CONTROL") is not null&&!solarScene.IsPaused)solarInput.PauseToggle=1u;'+anchor,1)


def configure_runner():
    global assess
    if assess is not None:return assess
    import poll
    assert Path(poll.__file__).resolve().parent==HERE
    a=poll.a
    assert a.HERE.resolve()==HERE and a.OUT.resolve()==OUT.resolve() and a.HOST.resolve()==HOST.resolve() and a.BANK==BANK
    # Keep the current poll journal parser, adding only exact capture identities.
    source=(poll.e.PRIOR/'assess.py').read_text(encoding='utf-8')
    source=source[source.index('def execute('):source.index('\ndef fixed(')]
    source=source.replace('    prep={k:', "    markers+=['Residual GPU:','Performance publication:','Performance request polling:','Poll memory type:','Composition frame:','Pixel parity:']\n    prep={k:")
    exec(source,a.__dict__)
    assess=a
    return a


def guard_raw_slot(create=False):
    assert OUT==ROOT/'build/m13-final-exit'
    for p in (OUT,RAW):
        if p.exists():assert not (p.lstat().st_file_attributes&0x400),('reparse path',p)
    assert RAW.resolve().parent==OUT.resolve()
    if create:RAW.mkdir(parents=True,exist_ok=True)
    files=list(RAW.iterdir()) if RAW.exists() else []
    for p in files:
        assert p.name in NAMES and p.is_file(),('unclassified raw-slot entry',p)
        st=p.lstat()
        assert not (st.st_file_attributes&0x400) and st.st_nlink==1,('linked raw file',p)
    assert sum(p.stat().st_size for p in files)<=LIMIT,'raw-slot budget exceeded'
    return RAW


def fixed(pose,label,cached=False):
    assert pose in ('florida','inland')
    assert label and all(c.isalnum() or c in '-_' for c in label)
    assert not any((HERE/(label+x)).exists() for x in ('.json','.json.gz')),'evidence already exists'
    a=configure_runner()
    manifest=json.loads((HERE/'private-capture-host.json').read_text(encoding='utf-8'))
    assert manifest['native']==a.sha(HOST/'NovaCore.Native.dll')
    assert manifest['managed']==a.sha(HOST/'NovaCore.Triangle.dll')
    assert manifest['sourceRestored'] and manifest['productionDeploymentUnchanged']
    assert manifest['captureFrame']==175 and Path(manifest['rawCaptureSlot']).resolve()==RAW.resolve()
    guard_raw_slot(create=True)
    env=a.environment()
    env.update(NOVACORE_COMPOSITION_CONTROL='1',NOVACORE_PIXEL_PARITY=str(RAW),
        NOVACORE_PERFORMANCE_FRAME_LOG='1',NOVACORE_P2S5F_DIRECTIONAL_ONLY='1',
        NOVACORE_P2S5G_FIXED_DIAGNOSTIC_TIME='1',NOVACORE_P2S5F_DIRECTIONAL_LEVEL='17',
        NOVACORE_P2S5F_DIRECTIONAL_YAW_RADIANS='1.5707963267948966',
        NOVACORE_P2S5F_DIRECTIONAL_PITCH_RADIANS='-0.035' if pose=='florida' else '-1.0')
    if pose=='florida':env['NOVACORE_PERFORMANCE_FLORIDA']='1'
    else:env.update(NOVACORE_PERFORMANCE_GEOGRAPHY='40,-105',NOVACORE_PERFORMANCE_ALTITUDE_METRES='50')
    if cached:env['NOVACORE_EXIT_CACHED_KEYS']='1'
    forbidden=('NOVACORE_WHOLE_BULK_LOCAL','NOVACORE_PREP_DEVICE_LOCAL','NOVACORE_PERFORMANCE_COUNTERS_OFF','NOVACORE_PERFORMANCE_CLIPPING','NOVACORE_SHADER_INFO')
    assert not any(k in env for k in forbidden)
    args=['--scene=sol','--focus=earth','--altitude=10.004','--solar-epoch=j2000',
        '--physical-surface=m12d-natural-candidate','--p2s5c3-traversal',
        '--benchmark-frames=1000','--log=startup,validation,vulkan']
    if pose=='florida':args.append('--surface-site=florida-launch')
    result=a.execute(label,args,env,'profile',HOST)
    guard_raw_slot()
    return result
