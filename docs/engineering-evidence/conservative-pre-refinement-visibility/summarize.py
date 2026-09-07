"""Compact all completed proof checkpoints, aligned timing series, and capture outcomes."""
import sys
sys.dont_write_bytecode=True
import json,gzip,math,re,hashlib
from pathlib import Path
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[2];OUT=ROOT/'build/conservative-pre-refinement-visibility'
sys.path.insert(0,str(HERE.parent/'near-surface-performance'))
from analyze import fields

def stats(values):
    a=sorted(values)
    return dict(n=len(a),median=a[math.ceil(len(a)*.5)-1],p95=a[math.ceil(len(a)*.95)-1],p99=a[math.ceil(len(a)*.99)-1],minimum=a[0],maximum=a[-1]) if a else None

def summarize():
    summary={};traces={};details={}
    for p in sorted(OUT.glob('*.log')):
        lines=p.read_text(encoding='utf-8',errors='replace').splitlines();name=p.stem
        if not any('PASS:' in x for x in lines):continue
        rows=[fields(x) for x in lines if 'P2S5F directional visibility:' in x]
        physical=[fields(x) for x in lines if 'Dual physical:' in x]
        dual=[fields(x) for x in lines if 'Dual parity:' in x]
        oracle=[fields(x) for x in lines if 'Visibility oracle:' in x]
        cpu=[fields(x) for x in lines if 'Performance CPU frame:' in x]
        gpu=[fields(x) for x in lines if 'Performance GPU frame:' in x]
        submit=[fields(x) for x in lines if 'Performance CPU submission:' in x]
        phases=[x for x in lines if any(t in x for t in ('PASS:','phase:','warp phase','finest snap:'))]
        d=dict(logSha256=hashlib.sha256(p.read_bytes()).hexdigest(),rawLogBytes=p.stat().st_size,passRecords=phases)
        result=OUT/(name+'.json')
        if result.exists():
            details[name]=json.loads(result.read_text());d.update({k:details[name].get(k) for k in ('pose','probe','captureMode','nativeSha256','managedSha256','shaderVariants','captureAnalysis')})
        dynamic=any(t in name for t in ('traversal','return','motion','warp'))
        if len(rows)>=100 and not dynamic and not name.startswith(('dual-','physical-')):
            r=rows[-100:];assert all(x['submittedFrame']==x['timingFrame'] for x in r)
            d['steady']={k:stats([x[k] for x in r]) for k in ('gpuTotalMs','gpuDetailedDrawMs','gpuCullCompactMs','tcsPatches','refinedVertices','clippingOutputPrimitives','fragmentInvocations')}
            ids={x['submittedFrame'] for x in r}
            d['steady'].update({k:stats([x[k] for x in cpu if x['frame']-1 in ids]) for k in ('update','fenceWait')})
            d['steady'].update({k:stats([x[k] for x in submit if x['sample'] in ids]) for k in ('record','submit','present')})
            geometry=[fields(x) for x in lines if 'Performance geometry frame:' in x]
            d['steady']['generatedPrimitives']=stats([x['clippingInputPrimitives'] for x in geometry if x['frame'] in ids])
            d['steady']['delta8_33Ms']={k:d['steady']['gpuTotalMs'][k]-8.33 for k in ('median','p95','p99')}
            d['steady']['delta11_11Ms']={k:d['steady']['gpuTotalMs'][k]-11.11 for k in ('median','p95','p99')}
        if physical:
            d['dualPhysical']=dict(frames=len(physical),generations=sorted({x['generation'] for x in physical}),totals={k:sum(x[k] for x in physical) for k in ('baselineRecords','comparedRecords','mismatch','missing','baselineConflict','tableOverflow','sampleOverflow')},first=physical[0],last=physical[-1])
        if dual:
            d['dualParity']=dict(frames=len(dual),generations=sorted({x['generation'] for x in dual}),totals={k:sum(x[k] for x in dual) for k in ('depth','hdr','image')},first=dual[0],last=dual[-1])
        if oracle:
            d['oracle']=dict(frames=len(oracle),firstFrame=oracle[0]['frame'],lastFrame=oracle[-1]['frame'],generations=sorted({x['generation'] for x in oracle}),totals={k:sum(x[k] for x in oracle) for k in ('tes','proposed','trueRejects','falseRejects','falseRetains','containmentViolations','rejectTes')},last=oracle[-1])
            assert d['oracle']['totals']['falseRejects']==0 and d['oracle']['totals']['containmentViolations']==0
        if dynamic:
            incoming={x['frame'] for x in cpu if x.get('incoming')==1}
            d['transitionTiming']={label:{k:stats([x[k] for x in gpu if (x['frame'] in incoming)==flag]) for k in ('total','terrain')} for label,flag in [('incoming',True),('notIncoming',False)]}
        if name.startswith(('dual-','physical-')):
            traces[name]=dict(dualParity=dual,physical=physical,phases=phases)
        elif name.startswith('final-bound-'):
            traces[name]=dict(oracle=oracle,gpu=[{k:x[k] for k in ('frame','total','terrain')} for x in gpu],incoming=[{k:x[k] for k in ('frame','generation','incoming') if k in x} for x in cpu],phases=phases)
        elif name.startswith(('paired-','repeat-','timing-','baseline-','recovery-control-')) or name=='warp-control':
            compact=[{k:x[k] for k in ('submittedFrame','timingFrame','gpuTotalMs','gpuDetailedDrawMs','gpuCullCompactMs','refinedVertices','tcsPatches','clippingOutputPrimitives','fragmentInvocations')} for x in rows[-100:]]
            traces[name]=dict(gpu=gpu,cpu=cpu,submission=submit,directional=compact,phases=phases)
        if name in details:
            for key in ('visibilityOracle','dualParity','dualPhysical'):details[name].pop(key,None)
        summary[name]=d
    for name,data in [('measurements',details),('frame-traces',traces)]:
        with gzip.GzipFile(filename=str(HERE/(name+'.json.gz')),mode='wb',mtime=0) as f:f.write(json.dumps(data,separators=(',',':')).encode())
    (HERE/'results.json').write_text(json.dumps(summary,indent=2)+'\n')
    return summary
if __name__=='__main__':
    s=summarize()
    for k,v in s.items():
        if 'steady' in v:print(k, v['steady']['gpuTotalMs'])
