"""Read exactly the three declared captures; retain concise boundaries and tails."""
from pathlib import Path
import json, math, hashlib
r=Path('E:/NovaCore');e=Path(__file__).parent;b=r/'build/srv01-stage5-final-closure'
def records(text,prefix):return [json.loads(line[len(prefix):]) for line in text.splitlines() if line.startswith(prefix)]
def stats(rows,key):
    a=sorted(x[key] for x in rows)
    if not a:return None
    def p(q):return a[max(0,min(len(a)-1,math.ceil(q*len(a))-1))]
    return dict(samples=len(a),median=p(.5),p95=p(.95),p99=p(.99),maximum=p(1))
out=[]
for i in range(1,4):
    path=b/f'live-{i}.log';journal=json.loads((b/f'process-{i}.json').read_text(encoding='utf-8-sig'));assert journal['exitCode']==0
    text=path.read_text(encoding='utf-8-sig');rows=records(text,'FINAL_TIMING_ROW ');meta=records(text,'FINAL_TIMING_META ')[0]
    assert len(rows)==meta['count'] and [x['Index'] for x in rows]==list(range(len(rows)))
    assert all(x['SameWorld'] and x['SameSite'] and not x['Pending'] and not x['Invalidated'] for x in rows)
    assert meta['frontier']==1200 and meta['tick']==20_000_000 and meta['history']==1200 and meta['terminalPublished']==0
    service=max(rows,key=lambda x:x['ServiceMs']);display=max(rows,key=lambda x:x['DisplayMs'])
    selected={x['Index'] for x in rows if x['ServiceMs']>1 or x['DisplayMs']>6.67 or x['PoolAfter']!=x['PoolBefore'] or x['ConstraintsAfter']!=x['ConstraintsBefore']}
    selected.update(x['Index'] for x in sorted(rows,key=lambda x:x['ServiceMs'],reverse=True)[:12])
    selected.update(range(min(8,len(rows))))
    for center in [service['Index'],display['Index']]:selected.update(x for x in [center-1,center,center+1] if 0<=x<len(rows))
    result=dict(process=i,journal=journal,rawLogSha256=hashlib.sha256(path.read_bytes()).hexdigest().upper(),meta=meta,
        display=stats(rows,'DisplayMs'),service=stats(rows,'ServiceMs'),maxService=service,maxDisplay=display,
        nextDisplayAfterMaxService=rows[service['Index']+1] if service['Index']+1<len(rows) else None,
        maxIndicesSame=service['Index']==display['Index'],displayMaxIsFollowingService=display['Index']==service['Index']+1,
        afterFirstPublicationService=stats([x for x in rows if x['Before']>=1],'ServiceMs'),
        warmedService=stats([x for x in rows if x['Before']>=128],'ServiceMs'),
        warmedDisplay=stats([x for x in rows if x['Before']>=128],'DisplayMs'),
        serviceAbove1ms=sum(x['ServiceMs']>1 for x in rows),warmServiceAbove1ms=sum(x['ServiceMs']>1 and x['Before']>=128 for x in rows),
        displayAbove6_67ms=sum(x['DisplayMs']>6.67 for x in rows),
        warmedDisplayAbove6_67ms=sum(x['DisplayMs']>6.67 and x['Before']>=128 for x in rows),
        warmedDisplayTailContext=[dict(event=x,previousService=rows[x['Index']-1]['ServiceMs']) for x in rows if x['DisplayMs']>6.67 and x['Before']>=128 and x['Index']>0],
        gcDeltas=[sum(x[k] for x in rows) for k in ['G0','G1','G2']],
        summedCounterDelta=sum(x['Allocated'] for x in rows),
        selectedEvents=[rows[x] for x in sorted(selected)],
        aggregates=[line for line in text.splitlines() if line.startswith(('STOCK_ASSEMBLY_END','STOCK_ASSEMBLY_FRAME','SRV01_LIVE_SERVICE','SRV01_COLD_PREPARATION','[native] CPU timings','[native] GPU timing averages','[native] Frame pacing','[native] Fence wait'))])
    out.append(result)
    print(json.dumps({k:result[k] for k in ['process','display','service','maxService','maxDisplay','warmedService','serviceAbove1ms','warmServiceAbove1ms','gcDeltas']}))
assert len(set(x['meta']['trajectoryHash'] for x in out))==1
(e/'capture-results.json').write_text(json.dumps(dict(historicalCorrelation='UNKNOWN',executedProcesses=3,processes=out),indent=2)+'\n')
