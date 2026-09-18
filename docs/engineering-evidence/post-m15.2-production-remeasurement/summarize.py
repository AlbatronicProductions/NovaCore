"""Consolidate audit output without retaining bulk frame traces."""
from pathlib import Path
import json, math, re, hashlib

repo=Path(__file__).resolve().parents[3]
out=repo/'build/post-m15.2-production-remeasurement'
evidence=Path(__file__).resolve().parent
def distribution(values):
    a=sorted(values)
    if not a:return None
    return dict(samples=len(a),median=a[math.ceil(.5*len(a))-1],p95=a[math.ceil(.95*len(a))-1],p99=a[math.ceil(.99*len(a))-1],maximum=a[-1],mean=sum(a)/len(a))
def capture(path):
    native=[]; managed=[]
    text=path.read_text(encoding='utf-8-sig')
    for line in text.splitlines():
        if line.startswith('AUDIT_NATIVE,'):native.append(list(map(float,line.split(',')[1:])))
        if line.startswith('AUDIT_MANAGED,'):managed.append(list(map(float,line.split(',')[1:])))
    assert len(native)==len(managed)>0,(path,len(native),len(managed))
    contact='contact' in path.name
    rows=[]
    for n,m in zip(native,managed):
        assert n[0]==m[0]
        f=int(m[7])
        phase=('ready-box' if f==0 else 'powered-held-box' if f==1 else 'exhausted-held-box' if f==2 else 'dry-box' if f<1200 else 'held-complete-box') if contact else ('ready' if m[11]==0 else 'held-complete' if m[11]==2 else 'main' if f<=16 else 'main-gimbal' if f<=32 else 'rcs')
        # InspectGpuTimings runs before this frame's draw: the next row owns this GPU result.
        gpu=native[int(n[0])+1][10] if int(n[0])+1<len(native) else None
        if gpu is not None and gpu<=0:gpu=None # startup recreation has no usable timestamp result
        rows.append(dict(frame=int(n[0]),phase=phase,frontier=f,ticks=int(m[8]),totalMs=n[1],updateInclusiveMs=n[2],fenceMs=n[3],inspectionMs=n[4],callbackMs=n[5],uploadMs=n[6],recordMs=n[7],submitMs=n[8],presentMs=n[9],gpuMs=gpu,managedServiceMs=m[1],managedPresentationMs=m[2],allocatedCounterBytes=int(m[3]),gen0=int(m[4]),gen1=int(m[5]),gen2=int(m[6]),main=int(m[9]),jets=int(m[10]),objects=int(m[13])))
    phases={}
    for phase in dict.fromkeys(r['phase'] for r in rows):
        a=[r for r in rows if r['phase']==phase]
        phases[phase]={k:distribution([r[k] for r in a if r[k] is not None]) for k in ['totalMs','updateInclusiveMs','fenceMs','callbackMs','recordMs','submitMs','presentMs','gpuMs','managedServiceMs','managedPresentationMs']}
        phases[phase]['tails']=sorted(a,key=lambda r:r['totalMs'],reverse=True)[:4]
        phases[phase]['collections']={k:sum(r[k] for r in a) for k in ['gen0','gen1','gen2']}
        phases[phase]['thresholdCounts']={str(t):sum(r['totalMs']>t for r in a) for t in [6.67,6.94,11.11,16.67]}
    return dict(file=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),allFrames=distribution([r['totalMs'] for r in rows]),phases=phases,thresholdCounts={str(t):sum(r['totalMs']>t for r in rows) for t in [6.67,6.94,11.11,16.67]},tails=sorted(rows,key=lambda r:r['totalMs'],reverse=True)[:12],transitionFrames=[r for i,r in enumerate(rows) if i==0 or r['phase']!=rows[i-1]['phase']],terminal=[line for line in text.splitlines() if 'STOCK_ASSEMBLY_END' in line or 'POWERED_CONTACT_LIVE_END' in line])

baseline=[]
for path in sorted(out.glob('baseline-*.log')):
    lines=path.read_text(encoding='utf-8-sig').splitlines()
    baseline.append(dict(file=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),results=[l for l in lines if any(k in l for k in ['Frame pacing:','CPU timings:','GPU timing averages:','Fence wait pacing:','STOCK_ASSEMBLY_END','STOCK_ASSEMBLY_FRAME','SRV01_LIVE_SERVICE','SRV01_COLD','Physical device:','Swapchain'])]))
captures=[capture(p) for p in sorted(out.glob('diagnostic-*.log')) if re.fullmatch(r'diagnostic-(\d+|camera|contact)\.log',p.name)]
operations=[]
for line in (out/'operations.log').read_text(encoding='utf-8-sig').splitlines():
    if line.startswith('ASSEMBLY_COST '):
        obj=json.loads(line[len('ASSEMBLY_COST '):])
        for k in ['rawMilliseconds','rawAllocationCounterDeltas','rawCollections']:obj.pop(k,None)
        operations.append(obj)
checks={name:(out/name).read_text(encoding='utf-8-sig').splitlines() for name in ['srv01-correctness.log','presentation.log','storage.log']}
result=dict(baseline=baseline,captures=captures,operations=operations,allocation=(out/'allocation.log').read_text(encoding='utf-8-sig').splitlines(),checks=checks,method='Three fresh untouched production 480-frame runs. Diagnostic copies: three 540-frame stock runs incl 60 READY frames; one camera orbit; one 4000-frame separate powered-contact box. Nearest-rank frame percentiles. Operation campaign retains its middle-pair median. No subtraction of percentile populations. GPU aligned to next capture row; zero startup and unavailable final GPU omitted. Phase labels are destination observations: a bounded callback can cross a phase boundary. Contact held bins include idle inspection, not continuously powered work. All cold/transition tails retained.')
(evidence/'measurements.json').write_text(json.dumps(result,indent=2)+'\n')
print('BASELINE')
for b in baseline:
    print(b['file']); print('\n'.join(l for l in b['results'] if 'Frame pacing' in l))
print('CAPTURES')
for c in captures:
    print(c['file'],c['allFrames'],c['thresholdCounts'])
    for t in c['tails'][:4]:print(t)
print('OPERATIONS')
for op in operations:print(op['population'],op['boundary'],*[op[k] for k in ['medianMs','p95Ms','p99Ms','maxMs']],op['allocationCounterDeltaDuringSamples'])
