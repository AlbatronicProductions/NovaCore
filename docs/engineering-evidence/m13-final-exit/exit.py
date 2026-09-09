"""Fresh M13.5 exit measurements; sole lead-run private timing host.

Permanent budget 6 MiB after journal compression. No raw captures/ISA dumps.
Production source/deployment restored after build; banked shaders throughout.
"""
import inspect,json,pathlib,subprocess,sys
sys.dont_write_bytecode=True
HERE=pathlib.Path(__file__).resolve().parent;ROOT=HERE.parents[2]
PRIOR=HERE.parent/'m13-exit-assessment'
sys.path.insert(0,str(PRIOR));import assess
OUT=ROOT/'build/m13-final-exit';HOST=OUT/'host'
BANK='d4baab6940a57a46e478b98e36f5e45d1c4b558f'
assess.HERE=HERE;assess.OUT=OUT;assess.HOST=HOST;assess.BANK=BANK
base=assess.instrument
source=(HERE.parent/'m13-residual-regional-frames/residual.py').read_text()
source=source[source.index('def instrument(s):'):source.index('    # A separate private causal control')]
source=source.replace('s=m.residual_mapped(s)','s=base(s)')+'    return s\n'
scope=dict(HERE=HERE,base=base);exec(source,scope)
def instrument(s):
    s=scope['instrument'](s)
    anchor='a.Log(NC_LOG_ALWAYS,message);\n}\nvoid Update(App &a, float dt)'
    assert s.count(anchor)==1
    s=s.replace(anchor,'''a.Log(NC_LOG_ALWAYS,message);
  if(a.performanceFrameLog){char marker[256];std::snprintf(marker,sizeof marker,"Performance publication: frame=%llu; generation=%llu; level=%u; pupil=%u; publications=%llu; physicalReady=true; finalFence=true",(unsigned long long)a.frame,(unsigned long long)a.productionBillboardGeneration,a.submission->productionBillboard?a.submission->productionBillboard->level:0u,a.productionBillboardPreparedFrameIdentity,(unsigned long long)a.productionBillboardPublications);a.Log(NC_LOG_ALWAYS,marker);}
}
void Update(App &a, float dt)''')
    return s
assess.instrument=instrument
source=inspect.getsource(assess.execute).replace('    prep={k:',"    markers+=['Residual GPU:','NCSM1 physical slice:','Performance publication:']\n    prep={k:")
exec(source,assess.__dict__)

def baseline():
    d=assess.verify_deployment()
    for ref in ['HEAD','main','origin/main','m13.5-local-gpu-terrain-working-data^{}']:
        assert subprocess.check_output(['git','rev-parse',ref],cwd=ROOT,text=True).strip()==BANK
    d['tags']={}
    for tag,commit in [('m13.5-local-gpu-terrain-working-data',BANK),('m13.4-zero-contribution-terrain-material-noise','047ae479b33831eae1c0dfa3f37c657a7f70148f'),('m13.3-prepared-physical-terrain','180eaf150ba5db6364e17dd48336690778f058f9')]:
        actual=subprocess.check_output(['git','rev-parse',tag+'^{}'],cwd=ROOT,text=True).strip();assert actual==commit
        assert subprocess.check_output(['git','cat-file','-t',tag],cwd=ROOT,text=True).strip()=='tag'
        d['tags'][tag]=dict(commit=actual,object=subprocess.check_output(['git','rev-parse',tag],cwd=ROOT,text=True).strip())
    old=json.loads((HERE.parent/'m13-final-whole-frame-causality/candidate-deployment-final.json').read_bytes())
    assert d['assets']==old['assets']
    assert all(d['deployment'][c]['shaders']==old['deployment'][c]['shaders'] for c in ['Debug','Release'])
    assert not subprocess.check_output(['git','diff','--name-only'],cwd=ROOT).strip()
    assert not subprocess.check_output(['git','diff','--cached','--name-only'],cwd=ROOT).strip()
    d.update(main=BANK,initialGitStatus='',budgetBytes=6*1024*1024,rawCaptureAllowed=False,shaderSourceHashes={p.name:assess.sha(p) for p in (ROOT/'native/NovaCore.Native/shaders').iterdir() if p.is_file()})
    assess.write('baseline',d)

def fixed(profile):
    for pose in ['orbital','factor1','florida','grazing','active-refinement','inland']:
        d=assess.fixed(pose,profile)
        assert d['metrics']['gpuTotalMs']['n']==100
        if pose=='active-refinement':assert d['metrics']['maximumOuterTesFactor']['min']==64
        if pose=='factor1':assert d['metrics']['maximumOuterTesFactor']['max']==1

def dynamic(mode,profile,label):
    env=assess.environment()
    if profile:env['NOVACORE_PERFORMANCE_FRAME_LOG']='1'
    args=['--scene=sol','--focus=earth','--altitude=10.004','--solar-epoch=j2000','--physical-surface=m12d-natural-candidate','--log=startup,validation,vulkan']
    if mode in ['regional','florida']:
        env['NOVACORE_EARTH_ROUTE_VALIDATION']=mode
        args+=['--surface-site=florida-launch','--benchmark-frames='+('1000' if mode=='regional' else '500')]
    else:args+=['--p2s5c3-traversal','--benchmark-frames=20000']
    if mode=='warp':env['NOVACORE_P2S5E_WARP_ONLY']='1'
    folder=HOST if profile else ROOT/'samples/NovaCore.Triangle/bin/Release/net10.0'
    managed=HOST/'NovaCore.Triangle.dll';saved=managed.read_bytes()
    try:
        # Dynamic profiles use the exact deployed managed route, not the private
        # fixed-pose driver. Only native timing/logging differs from control.
        if profile:managed.write_bytes((ROOT/'samples/NovaCore.Triangle/bin/Release/net10.0/NovaCore.Triangle.dll').read_bytes())
        return assess.execute(label,args,env,'profile' if profile else 'normal',folder)
    finally:managed.write_bytes(saved)

if __name__=='__main__':
    cmd=sys.argv[1]
    if cmd=='baseline':baseline()
    elif cmd in ['build','build-v2']:
        (HERE/'timing.inl').write_bytes((HERE.parent/'m13-final-whole-frame-causality/timing.inl').read_bytes())
        if cmd=='build-v2':
            text=inspect.getsource(assess.build).replace('os.link(ROOT/', 'oracle.exists() or os.link(ROOT/').replace('timing-host.patch','timing-host-v2.patch').replace("write('private-host',", "write('private-host-v2',")
            exec(text,assess.__dict__)
        assess.build()
    elif cmd=='fixed':fixed(False)
    elif cmd=='fixed-profile':fixed(True)
    elif cmd=='controls':
        for mode in ['full','regional','warp']:dynamic(mode,False,'control-'+mode)
    elif cmd=='profiles':
        for mode in ['full','regional','warp']:dynamic(mode,True,'dynamic-'+mode)
        dynamic('regional',True,'dynamic-regional-repeat')
    elif cmd=='grids':
        for mode in ['active-grid','inland-grid']:assess.dynamic(mode,True)
