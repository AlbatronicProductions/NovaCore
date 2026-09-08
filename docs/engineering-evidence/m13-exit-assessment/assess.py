"""Fresh banked M13.4 exit assessment; one private timing host, no raw captures."""
import hashlib,json,math,os,pathlib,subprocess,sys,time
sys.dont_write_bytecode=True
SOURCE=pathlib.Path(__file__).resolve().parent
ROOT=SOURCE.parents[2]
HERE=pathlib.Path(os.environ.get('NOVACORE_EXIT_EVIDENCE_DIR',str(SOURCE))).resolve()
HERE.mkdir(parents=True,exist_ok=True)
OUT=ROOT/'build/m13-exit-assessment';HOST=OUT/'host'
BANK='047ae479b33831eae1c0dfa3f37c657a7f70148f'
OLD=SOURCE.parent/'post-m13.3-next-target'
sys.path.insert(0,str(OLD))
from records import pack,read
from instrument import instrument
from prepare import verify as verify_deployment,sha
sys.path.insert(0,str(SOURCE.parent/'near-surface-performance'))
from analyze import fields
def stats(values):
    v=sorted(values)
    return None if not v else {'n':len(v),'median':v[math.ceil(.5*len(v))-1],'p95':v[math.ceil(.95*len(v))-1],'p99':v[math.ceil(.99*len(v))-1],'min':v[0],'max':v[-1]}
def write(label,data):
    path=HERE/(label+'.json');assert not path.exists(),label
    path.write_text(json.dumps(data,separators=(',',':'))+'\n',encoding='utf-8')
def baseline():
    data=verify_deployment()
    for ref in ['HEAD','main','origin/main','m13.4-zero-contribution-terrain-material-noise^{}']:
        actual=subprocess.check_output(['git','rev-parse',ref],cwd=ROOT,text=True).strip();assert actual==BANK
    assert not subprocess.check_output(['git','diff','--name-only'],cwd=ROOT,text=True).strip()
    data.update(m13_4=BANK,m13_4TagObject=subprocess.check_output(['git','rev-parse','m13.4-zero-contribution-terrain-material-noise'],cwd=ROOT,text=True).strip(),initialStatusBeforeEvidence='',shaderSourceHashes={p.name:sha(p) for p in (ROOT/'native/NovaCore.Native/shaders').iterdir() if p.is_file()},budgetPermanentBytes=5*1024*1024,rawCapturesAllowed=False)
    write('baseline',data)
def build():
    OUT.mkdir(parents=True,exist_ok=True)
    before=verify_deployment()
    names=['native/NovaCore.Native/NovaCoreNative.cpp','samples/NovaCore.Triangle/ProductionBillboardDesktopTraversal.cs']
    original={n:(ROOT/n).read_bytes() for n in names}
    for n,b in original.items():assert b.replace(b'\r\n',b'\n')==subprocess.check_output(['git','show',BANK+':'+n],cwd=ROOT).replace(b'\r\n',b'\n')
    native=ROOT/'build/native-ninja-release/NovaCore.Native.dll';saved=native.read_bytes()
    try:
        old=(SOURCE.parent/'post-m13.2-next-target/instrumentation.patch').read_text()
        patch=old[old.index('diff --git a/samples/NovaCore.Triangle/ProductionBillboardDesktopTraversal.cs'):]
        subprocess.run(['git','apply','-'],cwd=ROOT,input=patch.encode(),check=True)
        path=ROOT/names[1];text=path.read_text();anchor='        var performanceAltitude = Environment.GetEnvironmentVariable("NOVACORE_PERFORMANCE_ALTITUDE_METRES");'
        assert text.count(anchor)==1
        text=text.replace(anchor,'''        var geography=Environment.GetEnvironmentVariable("NOVACORE_PERFORMANCE_GEOGRAPHY");
        if(geography is not null){var degrees=geography.Split(',').Select(x=>double.Parse(x,CultureInfo.InvariantCulture)).ToArray();if(degrees.Length!=2||!double.IsFinite(degrees[0])||!double.IsFinite(degrees[1])||Math.Abs(degrees[0])>90||Math.Abs(degrees[1])>180)throw new InvalidOperationException("invalid diagnostic geography");_direction=NovaCore.Core.Surface.BodyFixedGeography.DirectionFromLatitudeLongitude(degrees[0]*Math.PI/180,degrees[1]*Math.PI/180);}
'''+anchor)
        path.write_text(text)
        path=ROOT/names[0];path.write_text(instrument(path.read_text()))
        (HERE/'timing-host.patch').write_bytes(subprocess.check_output(['git','diff','--',*names],cwd=ROOT))
        subprocess.run(['pwsh','-NoProfile','-Command',"& 'C:/Program Files/Microsoft Visual Studio/18/Community/Common7/Tools/Launch-VsDevShell.ps1' -Arch amd64 -HostArch amd64 -SkipAutomaticLocation\ncmake --build build/native-ninja-release --target NovaCore.Native --parallel 4\nexit $LASTEXITCODE"],cwd=ROOT,check=True)
        oracle=HOST/'earth-data/earth_elevation_8192x4096.r16';oracle.parent.mkdir(parents=True,exist_ok=True);os.link(ROOT/'assets/earth/runtime'/oracle.name,oracle)
        subprocess.run(['dotnet','build','samples/NovaCore.Triangle/NovaCore.Triangle.csproj','-c','Release','--no-restore','-o',str(HOST),'-v','quiet'],cwd=ROOT,check=True)
    finally:
        for n,b in original.items():(ROOT/n).write_bytes(b)
        native.write_bytes(saved)
    after=verify_deployment();assert after['deployment']==before['deployment'] and after['assets']==before['assets']
    assert not subprocess.check_output(['git','diff','--name-only'],cwd=ROOT,text=True).strip()
    write('private-host',{'native':sha(HOST/'NovaCore.Native.dll'),'managed':sha(HOST/'NovaCore.Triangle.dll'),'sourceRestored':True,'productionDeploymentUnchanged':True,'noCaptureInstrumentation':True})
def environment():
    directory=OUT/'layers';(directory/'implicit').mkdir(parents=True,exist_ok=True);(directory/'explicit').mkdir(exist_ok=True)
    manifest=pathlib.Path(os.environ['VULKAN_SDK'])/'Bin/VkLayer_khronos_validation.json';data=json.loads(manifest.read_text());data['layer']['library_path']=str((manifest.parent/data['layer']['library_path']).resolve());(directory/'explicit/validation.json').write_text(json.dumps(data))
    env={k:v for k,v in os.environ.items() if not k.startswith(('NOVACORE_','VK_LAYER','VK_ADD_LAYER','VK_IMPLICIT_LAYER','VK_ADD_IMPLICIT_LAYER','VK_LOADER','VK_VALIDATION','VK_INSTANCE_LAYERS'))}
    env.update(VK_LAYER_PATH=str(directory/'explicit'),VK_IMPLICIT_LAYER_PATH=str(directory/'implicit'),VK_LAYER_SETTINGS_PATH=str(directory),VK_INSTANCE_LAYERS='VK_LAYER_KHRONOS_validation',NOVACORE_WINDOW_CLIENT_WIDTH='3440',NOVACORE_WINDOW_CLIENT_HEIGHT='1440',NOVACORE_WINDOW_BORDERLESS='1')
    return env
def execute(label,args,env,native_mode='normal',folder=HOST):
    assert not (HERE/(label+'.json')).exists()
    native=folder/'NovaCore.Native.dll';saved=None
    if folder==HOST and native_mode=='normal':
        saved=native.read_bytes();native.write_bytes((ROOT/'samples/NovaCore.Triangle/bin/Release/net10.0/NovaCore.Native.dll').read_bytes())
    nativeHash=sha(native);start=time.monotonic()
    try:result=subprocess.run([str(folder/'NovaCore.Triangle.exe'),*args],cwd=ROOT,env=env,text=True,capture_output=True,timeout=360)
    finally:
        if saved is not None:native.write_bytes(saved)
    log=result.stdout+result.stderr
    errors=[x for x in log.splitlines() if any(t in x for t in ['VUID-','Validation Error','VK_ERROR','FAIL:'])]
    if result.returncode or errors:
        (OUT/(label+'-failure.txt')).write_text(log);raise RuntimeError((label,result.returncode,errors))
    directional=[fields(x) for x in log.splitlines() if 'P2S5F directional visibility:' in x]
    host=[fields(x) for x in log.splitlines() if 'Performance host frame:' in x]
    gpu=[fields(x) for x in log.splitlines() if 'Performance GPU frame:' in x]
    metrics={};warm=directional[-100:]
    if directional:
        assert len(warm)==100 and all(r['submittedFrame']==r['timingFrame'] for r in warm)
        for k in warm[0]:
            values=[r[k] for r in warm if isinstance(r[k],(float,int)) and math.isfinite(r[k])]
            if values:metrics[k]=stats(values)
        byFrame={r['frame']:r for r in host}
        for k in ['update','fenceWait','inspection','hostCallback','validationUpload','acquire','record','submit','present','recreate','total','unattributed']:
            shift=1 if k in ['update','fenceWait','inspection','hostCallback','validationUpload'] else 0
            metrics['host_'+k]=stats([byFrame[r['submittedFrame']+shift][k] for r in warm if r['submittedFrame']+shift in byFrame])
    markers=['GPU:','directional pose:','PASS:','timing:','Frame pacing:','Fence wait pacing:','CPU timings:','Average frame time:','regional ready:','regional demand:','regional physical totals:','spherical billboard publication:','P2S5C3 phase:','finest snap:','altitude checkpoint:','P2S5E anchored warp','seating stability:','Runtime fingerprint:']
    markers+=['Earth route validation:','Earth route scenario validation:','P2S5C3 traversal','Production spherical','owner','Rendered ','GPU timings:']
    prep={k:[fields(x) for x in log.splitlines() if f'NCSM1 regional GPU work: kind={k};' in x] for k in ['demand','currentPhysicalPreparation','incomingPhysicalPreparation']}
    data={'label':label,'bank':BANK,'nativeMode':native_mode,'nativeHash':nativeHash,'managedHash':sha(folder/'NovaCore.Triangle.dll'),'shaderHashes':{p.name:sha(p) for p in (folder/'shaders').glob('*.spv')},'args':args,'cwd':str(ROOT),'env':{k:v for k,v in env.items() if k.startswith(('NOVACORE_','VK_'))},'exitCode':result.returncode,'seconds':time.monotonic()-start,'errors':errors,'logBytes':len(log.encode()),'logSha256':hashlib.sha256(log.encode()).hexdigest(),'metrics':metrics,'directionalRows':pack(directional),'hostRows':pack(host),'gpuRows':pack(gpu),'preparation':prep,'outliers40ms':[r for r in host if r['total']>=40],'lines':[x for x in log.splitlines() if any(m in x for m in markers)]}
    write(label,data)
    print(label,metrics.get('gpuTotalMs'),metrics.get('host_total'),flush=True)
    return data
def fixed(pose,profile=False):
    env=environment();env.update(NOVACORE_P2S5F_DIRECTIONAL_ONLY='1',NOVACORE_P2S5G_FIXED_DIAGNOSTIC_TIME='1',NOVACORE_P2S5F_DIRECTIONAL_LEVEL='0' if pose=='orbital' else '17',NOVACORE_P2S5F_DIRECTIONAL_YAW_RADIANS='1.5707963267948966',NOVACORE_P2S5F_DIRECTIONAL_PITCH_RADIANS={'active':'-1.0','inland':'-1.0','grazing':'-0.001'}.get(pose,'-0.035'))
    if pose in ['active','inland']:env['NOVACORE_PERFORMANCE_ALTITUDE_METRES']='50'
    if pose=='grazing':env['NOVACORE_PERFORMANCE_ALTITUDE_METRES']='10.004'
    if pose=='active-refinement':
        env.update(NOVACORE_PERFORMANCE_ALTITUDE_METRES='10.004',NOVACORE_P2S5F_DIRECTIONAL_PITCH_RADIANS='-1.0')
    if pose=='inland':env['NOVACORE_PERFORMANCE_GEOGRAPHY']='40,-105'
    if pose=='florida':env['NOVACORE_PERFORMANCE_FLORIDA']='1'
    if profile:env['NOVACORE_PERFORMANCE_FRAME_LOG']='1'
    args=['--scene=sol','--focus=earth','--altitude=10.004','--solar-epoch=j2000','--physical-surface=m12d-natural-candidate','--p2s5c3-traversal','--benchmark-frames=1000','--log=startup,validation,vulkan']
    if pose=='florida':args.append('--surface-site=florida-launch')
    data=execute(('cpu-' if profile else 'fixed-')+pose,args,env,'profile' if profile else 'normal')
    if pose=='active-refinement':assert data['metrics']['maximumOuterTesFactor']['min']>1,'Not an active-refinement workload'
    return data
def dynamic(mode,profile=True):
    env=environment()
    if profile:env['NOVACORE_PERFORMANCE_FRAME_LOG']='1'
    args=['--scene=sol','--focus=earth','--altitude=10.004','--solar-epoch=j2000','--physical-surface=m12d-natural-candidate','--log=startup,validation,vulkan']
    if mode in ['regional','florida']:
        env['NOVACORE_EARTH_ROUTE_VALIDATION']=mode
        args+=['--surface-site=florida-launch','--benchmark-frames='+('1000' if mode=='regional' else '500')]
    else:
        args+=['--p2s5c3-traversal','--benchmark-frames=20000']
    if mode=='warp':env['NOVACORE_P2S5E_WARP_ONLY']='1'
    if mode in ['grid','inland-grid','active-grid']:
        env.update(NOVACORE_P2S5F_DIRECTIONAL_ONLY='1',NOVACORE_P2S5F_DIRECTIONAL_GRID='1',NOVACORE_P2S5F_DIRECTIONAL_LEVEL='17',NOVACORE_P2S5G_FIXED_DIAGNOSTIC_TIME='1',NOVACORE_P2S5F_DIRECTIONAL_YAW_RADIANS='1.5707963267948966',NOVACORE_PERFORMANCE_ALTITUDE_METRES='50')
        if mode=='inland-grid':env['NOVACORE_PERFORMANCE_GEOGRAPHY']='40,-105'
        if mode=='active-grid':env['NOVACORE_PERFORMANCE_ALTITUDE_METRES']='10.004'
    return execute(('dynamic-' if profile else 'control-')+mode,args,env,'profile' if profile else 'normal')

if __name__=='__main__':
    mode=sys.argv[1]
    if mode=='baseline':baseline()
    elif mode=='build':build()
    elif mode=='fixed':
        for pose in ['orbital','factor1','florida','grazing','active','inland']:fixed(pose)
    elif mode=='cpu':
        for pose in ['orbital','factor1','florida','grazing','active','inland']:fixed(pose,True)
    elif mode=='dynamic':
        for case in ['full','grid','inland-grid','regional','florida','warp']:dynamic(case)
    elif mode=='control':
        for case in ['full','inland-grid']:dynamic(case,False)
    elif mode=='active':
        fixed('active-refinement');fixed('active-refinement',True)
        dynamic('active-grid');dynamic('active-grid',False)
