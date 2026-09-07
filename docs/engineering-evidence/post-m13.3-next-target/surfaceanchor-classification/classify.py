"""Bounded isolated M13.3 / candidate headless comparison; no GPU runs."""
import hashlib,json,os,pathlib,subprocess,sys,time
sys.dont_write_bytecode=True
HERE=pathlib.Path(__file__).resolve().parent
ROOT=HERE.parents[3]
OUT=ROOT/'build/m13.4-surfaceanchor-classification'
BASELINE=OUT/'baseline'
BASE='180eaf150ba5db6364e17dd48336690778f058f9'
def sha(path):
    with path.open('rb') as f:return hashlib.file_digest(f,'sha256').hexdigest()
def run(args,cwd=ROOT):return subprocess.run(list(map(str,args)),cwd=cwd,check=True)
def setup():
    assert subprocess.check_output(['git','rev-parse','HEAD'],cwd=BASELINE,text=True).strip()==BASE
    links=[]
    for folder in ['assets','.novacore/cache']:
        for source in (ROOT/folder).rglob('*'):
            if not source.is_file():continue
            relative=source.relative_to(ROOT);target=BASELINE/relative
            if target.exists():continue
            target.parent.mkdir(parents=True,exist_ok=True);os.link(source,target)
            links.append({'path':str(relative),'bytes':source.stat().st_size,'sha256':sha(source)})
    build=BASELINE/'build/native-ninja-release';build.mkdir(parents=True,exist_ok=True)
    target=build/'NovaCore.Native.dll'
    if not target.exists():os.link(ROOT/'build/native-ninja-release/NovaCore.Native.dll',target)
    shaders=build/'shaders';shaders.mkdir(exist_ok=True)
    for source in (ROOT/'build/native-ninja-release/shaders').glob('*.spv'):
        if source.name=='planetary_production.frag.spv':continue
        target=shaders/source.name
        if not target.exists():os.link(source,target)
    run([pathlib.Path(os.environ['VULKAN_SDK'])/'Bin/glslc.exe','-I',BASELINE/'native/NovaCore.Native/shaders',BASELINE/'native/NovaCore.Native/shaders/planetary_production.frag','-o',shaders/'planetary_production.frag.spv'])
    expected=json.loads((HERE.parent/'baseline-identity.json').read_text())['deployment']['Release']['shaders']['planetary_production.frag.spv']
    assert sha(shaders/'planetary_production.frag.spv')==expected
    run(['dotnet','build','tests/NovaCore.Graphics.Tests/NovaCore.Graphics.Tests.csproj','-c','Release','-v','quiet'],BASELINE)
    (HERE/'baseline-setup.json').write_text(json.dumps({'commit':BASE,'assetsHardlinked':links,'native':sha(build/'NovaCore.Native.dll'),'baselineFragment':sha(shaders/'planetary_production.frag.spv'),'normalCandidateFragment':sha(ROOT/'build/native-ninja-release/shaders/planetary_production.frag.spv')},indent=2)+'\n')
def environment():
    env={k:v for k,v in os.environ.items() if not k.startswith(('NOVACORE_','VK_LAYER','VK_ADD_LAYER','VK_IMPLICIT_LAYER','VK_ADD_IMPLICIT_LAYER','VK_LOADER','VK_VALIDATION','VK_INSTANCE_LAYERS'))}
    env.update(VK_LAYER_PATH=str(pathlib.Path(os.environ['VULKAN_SDK'])/'Bin'),VK_IMPLICIT_LAYER_PATH=str(OUT/'absent-implicit'),VK_LAYER_SETTINGS_PATH=str(OUT/'absent-settings'),VK_INSTANCE_LAYERS='VK_LAYER_KHRONOS_validation',NOVACORE_P2S5F_ARTIFACT_INPUT=str(ROOT/'assets/planetary-nested-scale-mesh'))
    return env
def execute(label,root,options=None,env_override=None):
    options=options or ['--category=headless']
    target=HERE/(label+'.json');assert not target.exists()
    exe=root/'tests/NovaCore.Graphics.Tests/bin/Release/net10.0/NovaCore.Graphics.Tests.exe'
    env=env_override or environment();started=time.monotonic()
    result=subprocess.run([str(exe),*options],cwd=root,env=env,capture_output=True,text=True,timeout=900)
    log=result.stdout+result.stderr
    (OUT/(label+'.txt')).write_text(log)
    record={'label':label,'root':str(root),'args':options,'exitCode':result.returncode,'seconds':time.monotonic()-started,'env':{k:v for k,v in env.items() if k.startswith(('NOVACORE_','VK_','DOTNET_','COMPlus_'))},'binaries':{p.name:sha(p) for p in exe.parent.glob('*.dll')},'executable':sha(exe),'logBytes':len(log.encode()),'logSha256':hashlib.sha256(log.encode()).hexdigest(),'lines':[x for x in log.splitlines() if any(t in x for t in ['PASS [','FAIL [','Graphics Release:','Exception:','SurfaceAnchor terrain:','Anchor predicate:','Program.cs:line'])]}
    target.write_text(json.dumps(record,indent=2)+'\n');print(label,result.returncode,record['lines'][-5:],flush=True)
    return record
if __name__=='__main__':
    if sys.argv[1]=='setup':setup()
    elif sys.argv[1]=='controls':
        execute('baseline-stock',BASELINE)
        execute('candidate-stock-repeat',ROOT)
