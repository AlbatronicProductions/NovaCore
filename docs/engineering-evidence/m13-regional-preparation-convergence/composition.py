"""Private A/B/C/D controls; prior evidence remains read-only."""
import os,sys,pathlib,json,subprocess,inspect
HERE=pathlib.Path(__file__).resolve().parent;ROOT=HERE.parents[2]
PRIOR=HERE.parent/'m13-regional-preparation-blocker'
sys.path.insert(0,str(PRIOR));import gauntlet as g
from prototype import source as copy_source
assess=g.assess
OUT=ROOT/'build/m13-regional-preparation-convergence';HOST=OUT/'host'
g.HERE=assess.HERE=HERE;g.OUT=assess.OUT=OUT;g.HOST=assess.HOST=HOST
SHADERS=ROOT/'native/NovaCore.Native/shaders';NAME='production_spherical_billboard_incoming_prepare.comp'
base_instrument=g.instrument
def instrument(s):
    s=base_instrument(s)
    s=s.replace('std::array<uint32_t,12> reused{},total{};uint64_t mismatch=0;',
      'std::array<uint32_t,12> reused{},total{};uint64_t mismatch=0;uint64_t negatives[4]{};')
    s=s.replace('mismatch+=marker[1];',
      'mismatch+=marker[1];for(uint32_t bit=0;bit<4;bit++)if(marker[2]&(1u<<bit)){++negatives[bit];if(marker[0])throw std::runtime_error("changed dependency accepted for physical reuse");}')
    s=s.replace('    if(mismatch)throw std::runtime_error("GPU exact direction reuse physical words differ");',
      '''    {char negative[512];std::snprintf(negative,sizeof negative,"Composition negative oracle: generation=%llu; regionalContributionChanged=%llu; supportContributionChanged=%llu; sourceIdentityRejected=%llu; basisChanged=%llu; invalidAccepted=0",(unsigned long long)a.productionBillboardIncomingGeneration,(unsigned long long)negatives[0],(unsigned long long)negatives[1],(unsigned long long)negatives[2],(unsigned long long)negatives[3]);a.Log(NC_LOG_ALWAYS,negative);}
    if(mismatch)throw std::runtime_error("GPU exact direction reuse physical words differ");''')
    anchor='void CreateProductionBillboard(App &a){'
    assert s.count(anchor)==1;s=s.replace(anchor,(HERE/'correlation.inl').read_text()+'\n'+anchor)
    anchor='  UpdateRegionalPhysical(a);'
    assert s.count(anchor)==1;s=s.replace(anchor,anchor+'\n  CompositionCorrelation(a);')
    anchor='  NcHostEvent e{NC_UPDATE_FRAME, NC_LOG_NONE, nullptr, in, a.submission};'
    s=s.replace(anchor,'  if(std::getenv("NOVACORE_COMPOSITION_CONTROL")){in={};in.viewportWidthPixels=a.extent.width;in.viewportHeightPixels=a.extent.height;}\n'+anchor)
    return s
assess.instrument=instrument
# Keep all scalar/identity rows. Each raw constant block is bounded (<1 KiB/frame).
base_execute=assess.execute
def execute(label,args,env,native_mode='profile',folder=HOST):
    return base_execute(label,args,env,native_mode,folder)
code=inspect.getsource(__import__('importlib').import_module('assess').__dict__.get('execute',base_execute)) if False else None
# The previous module defines execute through exec. Re-read its original function
# and apply the same retained markers plus this ticket's correlation rows.
original=inspect.getsource(pathlib) if False else (HERE.parent/'m13-exit-assessment/assess.py').read_text()
start=original.index('def execute(');end=original.index('\ndef fixed(',start)
src=original[start:end].replace('    prep={k:',"    markers+=['Prep GPU oracle','Prep heap','Prep placement','Prep publication','NCSM1 physical slice','NCSM1 staged pupil publication','Composition ']\n    prep={k:")
exec(src,assess.__dict__)

def build(label='private-host'):
    OUT.mkdir(parents=True,exist_ok=True)
    before=assess.verify_deployment()
    names=['native/NovaCore.Native/NovaCoreNative.cpp','samples/NovaCore.Triangle/EarthRouteValidation.cs','samples/NovaCore.Triangle/Program.cs']
    original={n:(ROOT/n).read_bytes() for n in names}
    native=ROOT/'build/native-ninja-release/NovaCore.Native.dll';saved=native.read_bytes()
    try:
        path=ROOT/names[0];path.write_text(instrument(path.read_text()))
        path=ROOT/names[1];s=path.read_text();s=s.replace('    private int _frame;', '''    private int _frame;
    private static readonly bool Composition=Environment.GetEnvironmentVariable("NOVACORE_COMPOSITION_CONTROL") is not null;
    private bool _compositionStarted;
    internal bool CompositionComplete=>Composition&&_frame>=1000;
    [System.Runtime.InteropServices.DllImport("NovaCore.Native",EntryPoint="ncCompositionReady")]
    private static extern int CompositionReady();
''');s=s.replace('        _frame++;','''        if(Composition&&!_compositionStarted){if(CompositionReady()==0)return;_compositionStarted=true;Console.WriteLine("Composition route: logicalFrame=0; ready=true; clock=pausedJ2000; externalInput=disabled");}
        _frame++;
        if(Composition)Console.WriteLine($"Composition route frame: logicalFrame={_frame}");''');path.write_text(s)
        path=ROOT/names[2];s=path.read_text();anchor='solarScene.ApplyPresentationInput(s.Camera,solarInput,out var rateChanged,out var pauseChanged);'
        assert s.count(anchor)==1;s=s.replace(anchor,'if(Environment.GetEnvironmentVariable("NOVACORE_COMPOSITION_CONTROL") is not null&&!solarScene.IsPaused)solarInput.PauseToggle=1u;'+anchor)
        anchor='    UpdatePlanetaryPatches(s);LogEarthLodDiagnostics(s,earthScene);'
        assert s.count(anchor)==1;s=s.replace(anchor,anchor+'\n    if(s.RouteValidation?.CompositionComplete==true)PostQuitMessage(0);')
        path.write_text(s)
        (HERE/(label+'.patch')).write_bytes(subprocess.check_output(['git','diff','--',*names],cwd=ROOT))
        subprocess.run(['pwsh','-NoProfile','-Command',"& 'C:/Program Files/Microsoft Visual Studio/18/Community/Common7/Tools/Launch-VsDevShell.ps1' -Arch amd64 -HostArch amd64 -SkipAutomaticLocation\ncmake --build build/native-ninja-release --target NovaCore.Native --parallel 4\nexit $LASTEXITCODE"],cwd=ROOT,check=True)
        oracle=HOST/'earth-data/earth_elevation_8192x4096.r16';oracle.parent.mkdir(parents=True,exist_ok=True)
        if not oracle.exists():os.link(ROOT/'assets/earth/runtime'/oracle.name,oracle)
        subprocess.run(['dotnet','build','samples/NovaCore.Triangle/NovaCore.Triangle.csproj','-c','Release','--no-restore','-o',str(HOST),'-v','quiet'],cwd=ROOT,check=True)
    finally:
        for n,b in original.items():(ROOT/n).write_bytes(b)
        native.write_bytes(saved)
    after=assess.verify_deployment();assert before['deployment']==after['deployment'] and before['assets']==after['assets']
    assert not subprocess.check_output(['git','diff','--name-only'],cwd=ROOT,text=True).strip()
    assess.write(label,dict(native=assess.sha(HOST/'NovaCore.Native.dll'),managed=assess.sha(HOST/'NovaCore.Triangle.dll'),productionSourceRestored=True,deploymentUnchanged=True))

def run(mode):
    assert mode in ['A','B','C','D','D-oracle','D-invalid-source']
    oracle=mode in ['D-oracle','D-invalid-source']
    env=assess.environment();env.update(NOVACORE_COMPOSITION_CONTROL='1',NOVACORE_PERFORMANCE_FRAME_LOG='1',NOVACORE_EARTH_ROUTE_VALIDATION='regional')
    if mode in ['C','D','D-oracle','D-invalid-source']:env['NOVACORE_PREP_DEVICE_LOCAL']='1'
    shader=HOST/'shaders'/(NAME+'.spv');saved=shader.read_bytes()
    try:
        if mode in ['B','D','D-oracle','D-invalid-source']:
            s=copy_source(oracle)
            # Explicitly reject changed basis in addition to exact direction.
            anchor='sameBits(radius,published.transition.y) && regionalCatalog.entries[0].storage.w==0u)'
            s=s.replace(anchor,'''sameBits(radius,published.transition.y) &&
     sameBits(frame.east.x,published.east.x)&&sameBits(frame.east.y,published.east.y)&&sameBits(frame.east.z,published.east.z)&&
     sameBits(frame.north.x,published.north.x)&&sameBits(frame.north.y,published.north.y)&&sameBits(frame.north.z,published.north.z)&&
     sameBits(frame.up.x,published.up.x)&&sameBits(frame.up.y,published.up.y)&&sameBits(frame.up.z,published.up.z)&&
     regionalCatalog.entries[0].storage.w==0u)''')
            if oracle:
                anchor='  // Test-only catalog queries use the same function as real geographic demand.'
                extra='''
  uint negative=0u;
  if(pupilFrames.current.metadata.x!=0u && vertex<pupilFrames.current.metadata.z){
    dvec3 previous=canonicalDirection(publishedLattice.values[vertex].xyz,pupilFrames.current);
    if(!sameBits(RegionalPhysicalResidual(previous),RegionalPhysicalResidual(direction)))negative|=1u;
    FacilitySample priorSupport=FacilitySupport(previous),nextSupport=FacilitySupport(direction);
    if(!sameBits(priorSupport.weight,nextSupport.weight)||!sameBits(priorSupport.plane,nextSupport.plane)||
      !sameBits(priorSupport.gradient.x,nextSupport.gradient.x)||!sameBits(priorSupport.gradient.y,nextSupport.gradient.y)||
      !sameBits(priorSupport.gradient.z,nextSupport.gradient.z))negative|=2u;
    if(any(notEqual(pupilFrames.current.east.xyz,frame.east.xyz))||any(notEqual(pupilFrames.current.north.xyz,frame.north.xyz))||any(notEqual(pupilFrames.current.up.xyz,frame.up.xyz)))negative|=8u;
  }
  physical.values[vertex].reserved.z=uintBitsToFloat(negative);
'''
                if mode=='D-invalid-source':
                    s=s.replace('PupilFrame published=pupilFrames.current;','PupilFrame published=pupilFrames.current;published.metadata.x=0u;')
                    extra=extra.replace('uint negative=0u;','uint negative=4u;')
                s=s.replace(anchor,extra+anchor)
            path=OUT/(mode+'.comp');path.write_text(s)
            subprocess.run([str(pathlib.Path(os.environ['VULKAN_SDK'])/'Bin/glslc.exe'),'-I',str(SHADERS),str(path),'-o',str(shader)],check=True)
        if oracle:env['NOVACORE_PREP_GPU_ORACLE']='1'
        args=['--scene=sol','--focus=earth','--surface-site=florida-launch','--physical-surface=m12d-natural-candidate','--solar-epoch=j2000','--benchmark-frames=3000','--log=startup,validation,vulkan']
        d=assess.execute(mode,args,env,'profile',HOST)
        assert any('logicalFrame=1000' in x for x in d['lines']),'Controlled route did not finish'
        return d
    finally:shader.write_bytes(saved)

if __name__=='__main__':
    mode=sys.argv[1]
    if mode=='baseline':
        d=assess.verify_deployment();old=json.loads((PRIOR/'baseline.json').read_text());assert d['deployment']==old['deployment'] and d['assets']==old['assets'];d['permanentBudgetBytes']=16*1024*1024;d['architectureAuthority']='Project Control authorizes exact reuse plus device-local physical working storage';assess.write('baseline',d)
    elif mode.startswith('build'):build('private-host'+mode[5:])
    else:run(mode)
