"""Read-only analysis of the fixed capture population. No retries or new captures."""
import json, math, pathlib, re, sys, hashlib
root = pathlib.Path(__file__).resolve().parent
scratch = root.parents[4] / 'build/srv01-display-gap-population'
order = ['A1','B1','C1','B2','C2','A2','C3','A3','B3']
phase = {0:'ready',1:'active',2:'held',3:'failed'}
def stats(values):
    if not values: return None
    s=sorted(values)
    return dict(samples=len(s),median=s[max(0,math.ceil(.5*len(s))-1)],p95=s[math.ceil(.95*len(s))-1],p99=s[math.ceil(.99*len(s))-1],max=s[-1])
def legacy(line):
    return {k:float(v) for k,v in re.findall(r'(samples|median_ms|p95_ms|p99_ms|max_ms)=([0-9.Ee+-]+)',line)}
captures=[]; pools={p:dict(display=[],native=[],active=[],held=[]) for p in 'ABC'}
for name in order:
    path=scratch/f'{name}.process.json'
    if not path.exists():
        captures.append(dict(id=name,result='NOT EXECUTED'));continue
    record=json.loads(path.read_text(encoding='utf-8-sig'))
    record['result']='INVALID'; captures.append(record)
    logpath=scratch/f'{name}.stdout.txt'
    text=logpath.read_text(encoding='utf-8-sig',errors='replace') if logpath.exists() else ''
    record['stdoutSha256']=hashlib.sha256(logpath.read_bytes()).hexdigest() if logpath.exists() else None
    lines=text.splitlines()
    managed=[x.removeprefix('POPULATION_REPORT ') for x in lines if x.startswith('POPULATION_REPORT ')]
    natives=[x.removeprefix('POPULATION_NATIVE ') for x in lines if x.startswith('POPULATION_NATIVE ')]
    if len(managed)!=1 or len(natives)!=1:
        record['invalidReason']='Missing or duplicate observer report';continue
    try: m=json.loads(managed[0]);n=json.loads(natives[0])
    except ValueError as e: record['invalidReason']=str(e);continue
    r=m.pop('rows',[]);sv=m.pop('services',[]);record['simulation']=m
    legacy_frames=[legacy(x) for x in lines if x.startswith('STOCK_ASSEMBLY_FRAME ')]
    legacy_services=[legacy(x) for x in lines if x.startswith('SRV01_LIVE_SERVICE ')]
    if len(r)!=4000 or len(sv)!=4000 or len(n)!=4000 or any(len(row)!=8 for row in r) or len(legacy_frames)!=1 or len(legacy_services)!=1:
        record['invalidReason']='Malformed dimensions or missing legacy reports; preserved without replacement';continue
    d=[(r[i][0]-r[i-1][0])*1000/m['frequency'] for i in range(1,len(r))]
    problems=[]
    extents=re.findall(r'EXHAUST_PRESENTATION_STORAGE .*?extent=(\d+x\d+)',text)
    record['recordedExtents']=extents
    record['resolutionProtocolDeviation']='Plan stated 1280x720; unchanged actual default 960x540; no replacement or workload change'
    if not extents or any(x!='960x540' for x in extents):problems.append('non-equivalent recorded render extent')
    if record.get('exitCode')!=0:problems.append('process did not exit successfully')
    if len(r)!=4000 or len(n)!=4000 or m['callbacks']!=4000:problems.append('not 4000 callbacks/native frames')
    expected=128 if name[0]=='C' else 1200
    if not m['completed'] or m['failed'] or m['frontier']!=expected:problems.append('episode failed/incomplete')
    if not m['identityPass'] or m['replayMatches']!=len(r):problems.append('identity or replay failed')
    if m['maxPublications']>4:problems.append('service budget exceeded')
    if m['credits']-m['time']!=m['debt']:problems.append('accounting conservation failed')
    if m['time']!=(2_000_000 if name[0]=='C' else 20_000_000):problems.append('wrong final epoch')
    if m['revision']!=expected or m['history']!=expected or m['timelineRevision']!=0:problems.append('revision/history invariant')
    if name[0]!='C' and (m['maxPenetration']>.02 or m['maxFinalSupport']>.000122):problems.append('sampled support bound')
    if name[0]=='A' and (m['fuel']!=28.4375 or m['oxide']!=42.65625 or m['mass']!=701.09375):problems.append('powered resource/mass successor')
    if name[0]=='B' and (m['fuel']!=30 or m['oxide']!=45 or m['mass']!=705):problems.append('unpowered resources/mass')
    if any(x<=0 for x in d):problems.append('non-monotone samples')
    record['result']='VALID' if not problems else 'INVALID';record['invalidReasons']=problems
    record['display']=stats(d);record['native']=stats(n);record['service']=stats([s for i,s in enumerate(sv) if r[i][1]==1])
    record['legacyDisplay']=legacy_frames[0]
    record['legacyService']=legacy_services[0]
    record['nativeMaximumIndex']=max(range(len(n)),key=n.__getitem__)
    record['nativeMaximumPhase']=phase[r[record['nativeMaximumIndex']][1]]
    cold=re.findall(r'SRV01_COLD_PREPARATION ms=([0-9.Ee+-]+)',text)
    record['coldPreparationMs']=float(cold[0]) if cold else None
    def event(i):
        gap=d[i-1];adj=n[i-1]+n[i]
        # The whole adjacent native frames bound, not measure, their contributions.
        outside=max(0,gap-adj)
        kind='UNKNOWN'
        if outside>gap*.5:kind='BETWEEN-CALLBACK / OUTER-HOST GAP (majority lower bound)'
        elif sv[i-1]>gap*.8:kind='CALLBACK EXECUTION DOMINATED (previous servicing lower bound)'
        return dict(endingCallback=i,ms=gap,startPhase=phase[r[i-1][1]],endPhase=phase[r[i][1]],cold=i<8,
          frontierAtStart=r[i-1][3],frontierAtEnd=r[i][3],nativePreviousMs=n[i-1],nativeCurrentMs=n[i],outsideAdjacentNativeLowerBoundMs=outside,
          previousServiceMs=sv[i-1],reentryServiceMs=sv[i],category=kind,
          recovery=[dict(callback=j,frontierBefore=r[j][3],frontierAfter=r[j][4],debtBefore=r[j][5],credit=r[j][6],debtAfter=r[j][7],serviceMs=sv[j],nativeMs=n[j]) for j in range(max(0,i-1),min(len(r),i+5))])
    top=sorted(range(1,len(r)),key=lambda i:d[i-1],reverse=True)[:8]
    record['largestIntervals']=[event(i) for i in top]
    record['extraordinaryEvents']=[event(i) for i in range(1,len(r)) if d[i-1]>=250]
    record['coldDisplay']=stats([d[i-1] for i in range(1,min(len(r),8))])
    record['activeDisplay']=stats([d[i-1] for i in range(1,len(r)) if r[i-1][1]==1 and r[i][1]==1])
    record['heldDisplay']=stats([d[i-1] for i in range(1,len(r)) if r[i-1][1]==2 and r[i][1]==2])
    record['postColdDisplay']=stats(d[7:])
    record['maximumDebtAfterCallback']=max(x[7] for x in r)
    record['budgetExhaustedCallbacks']=sum(x[4]-x[3]==4 and x[7]>0 for x in r)
    record['maxServiceIndex']=max(range(len(sv)),key=sv.__getitem__)
    record['completionCallback']=next((i for i,x in enumerate(r) if x[2]==2),None)
    record['boundaryTimestamps']=dict(first=r[0][0],last=r[-1][0],seconds=(r[-1][0]-r[0][0])/m['frequency'])
    record['nativeEnvironment']=[x for x in lines if any(k in x for k in ['[native] GPU:','Vulkan validation','Swapchain recreated','CPU timings:','GPU timing averages:'])]
    if record['result']=='VALID':
        p=pools[name[0]];p['display']+=d;p['native']+=n
        p['active']+=[d[i-1] for i in range(1,len(r)) if r[i-1][1]==1 and r[i][1]==1]
        p['held']+=[d[i-1] for i in range(1,len(r)) if r[i-1][1]==2 and r[i][1]==2]
summary={k:{metric:stats(vals) for metric,vals in p.items()} for k,p in pools.items()}
(root/'captures.json').write_text(json.dumps(dict(order=order,screenMs=250,captures=captures,populations=summary),indent=2)+'\n',encoding='utf-8')
for c in captures:
    print(c['id'],c['result'],('display='+str(c['display'])+' native='+str(c['native'])+' extraordinary='+str(len(c['extraordinaryEvents']))) if 'display' in c else c.get('invalidReason',''))
    if c.get('invalidReasons'):print('INVALID:',c['invalidReasons'])
