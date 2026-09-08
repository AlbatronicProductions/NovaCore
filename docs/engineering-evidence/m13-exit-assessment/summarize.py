"""Derive compact exit tables, retaining the original columnar observations."""
import json
from assess import HERE,stats
from records import read,unpack

def runs(values,threshold=11.11):
    result=[];active=[]
    for frame,value in values:
        if value>threshold:
            if active and frame!=active[-1][0]+1:result.append(active);active=[]
            active.append((frame,value))
        elif active:result.append(active);active=[]
    if active:result.append(active)
    return [{'first':r[0][0],'last':r[-1][0],'count':len(r),'average':sum(v for f,v in r)/len(r),'max':max(v for f,v in r)}for r in result]

def aligned(data):
    # GPU timestamps are inspected before current host update/submission. Match
    # sequence and values; never reinterpret a stale geometry ID as a new frame.
    g=data['gpuRows'];j=0;result=[]
    for h in data['hostRows']:
        if j<len(g) and h['gpuTotal']==g[j]['total'] and h['gpuFrame']==g[j]['frame']:
            result.append((h,g[j]));j+=1
    assert j==len(g),(data['label'],j,len(g))
    return result

summary={'fixed':{},'dynamic':{},'alignment':'GPU readback completes at host N+1; a positively identified terrain frame requires gpuFrame == geometryFrame == host frame - 1. Bootstrap ID0 and focus-away stale ID aliases remain in full-stream timings, never in aligned Earth statistics.'}
for pose in ['orbital','factor1','florida','grazing','active-refinement','active','inland']:
    d=read(HERE/('fixed-'+pose+'.json'));c=read(HERE/('cpu-'+pose+'.json'))
    summary['fixed'][pose]={'gpu':d['metrics'],'cpu':{k:v for k,v in c['metrics'].items()if k.startswith('host_')},'preparation':{k:stats([r['ms']for r in v])for k,v in d['preparation'].items()},'pose':next(x for x in d['lines']if 'directional pose:'in x)}
for path in HERE.glob('dynamic-*.json'):
    d=read(path);pairs=aligned(d);earth=[(h,g)for h,g in pairs if h['gpuFrame']>0 and h['gpuFrame']==h['geometryFrame']==h['frame']-1]
    frames={h['frame']:h for h in d['hostRows']}
    out={'frames':len(d['hostRows']),'gpuFullStream':stats([g['total']for h,g in pairs]),'gpuAlignedEarth':stats([g['total']for h,g in earth]),'cpuFullStream':stats([h['total']for h in d['hostRows']]),'cpuAfterFirstWindowFrame':stats([h['total']for h in d['hostRows']if h['frame']>1]),'cpuScopesAlignedEarth':{k:stats([h[k]for h,g in earth])for k in ['update','fenceWait','hostCallback','validationUpload','record','submit','present']},'overBudgetBands':runs([(g['frame'],g['total'])for h,g in earth]),'outliers40ms':d['outliers40ms'],'preparation':{k:stats([r['ms']for r in v])for k,v in d['preparation'].items()},'gpuWorstAlignedEarth':sorted([{'gpu':g,'submissionHost':frames[g['frame']],'completionHost':h}for h,g in earth],key=lambda r:r['gpu']['total'])[-4:]}
    out['observedRefinementTransitions']=sum((a[0]['tesInvocations']>4*a[0]['tcsPatches'])!=(b[0]['tesInvocations']>4*b[0]['tcsPatches'])for a,b in zip(earth,earth[1:])if a[0]['tcsPatches']and b[0]['tcsPatches'])
    out['post30MajorHostStalls']=[h for h in d['outliers40ms']if h['frame']>30]
    summary['dynamic'][d['label']]=out
(HERE/'summary.json').write_text(json.dumps(summary,separators=(',',':'))+'\n')

def f(x):return f'{x:.3f}'
print('FIXED TABLE')
for name,d in summary['fixed'].items():
 m=d['gpu'];c=d['cpu'];v=m['gpuTotalMs'];print('| '+' | '.join([name,f(m['gpuDetailedDrawMs']['median']),f(v['median']),f(v['p95']),f(v['p99']),f(c['host_total']['median'])+'/'+f(c['host_update']['median']),f(v['median']-8.33),f(v['median']-11.11)])+' |')
print('CPU WORKLOAD TABLE')
for name,d in summary['fixed'].items():
 m=d['gpu'];c=d['cpu'];print('| '+' | '.join([name,*[f(c['host_'+k]['median'])for k in ['record','submit','fenceWait']],*[str(m[k]['median'])for k in ['tcsPatches','refinedVertices','fragmentInvocations','clippingOutputPrimitives']],f(m['gpuCullCompactMs']['median'])])+' |')
print('DYNAMIC TABLE')
for n,d in summary['dynamic'].items():
 g=d['gpuAlignedEarth'];allg=d['gpuFullStream'];c=d['cpuFullStream'];print('| '+' | '.join([n,str(d['frames']),str(g['n']),'/'.join(f(g[k])for k in ['median','p95','p99']),'/'.join(f(allg[k])for k in ['median','p95','p99']),'/'.join(f(c[k])for k in ['median','p95','p99']),str(d['observedRefinementTransitions'])])+' |')
print('regional bands', [x for x in summary['dynamic']['dynamic-regional']['overBudgetBands']if x['count']>=5])
