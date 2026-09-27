"""Read-only analysis of the bounded post-contact fixture/native observations."""
import argparse, csv, json, math, pathlib

CEILING = 1000 / 150

def stats(values, budget=True, frame_ids=None):
    raw=list(values)
    ids=list(frame_ids) if frame_ids is not None else list(range(len(raw)))
    pairs=[(i,float(v)) for i,v in zip(ids,raw) if v is not None and math.isfinite(float(v))]
    a = [v for _,v in pairs]
    if not a: return {"count": 0}
    ordered = sorted(a)
    q = lambda p: ordered[max(0, math.ceil(len(a)*p)-1)]
    result=dict(count=len(a),median=q(.5),p95=q(.95),p99=q(.99),maximum=max(a))
    if not budget:return result
    run = longest = 0;previous=None
    for i,v in pairs:
        if previous is None or i!=previous+1:run=0
        run = run+1 if v > CEILING else 0
        longest = max(longest, run)
        previous=i
    return result|dict(over6667=sum(v>CEILING for v in a),longestOver6667=longest,
                       headroomMedian=CEILING-q(.5),headroomP95=CEILING-q(.95),headroomP99=CEILING-q(.99))

def work(rows):
    measured = [r for r in rows if r.get('Work') is not None]
    if not measured: return {}
    w = [r['Work'] for r in measured]
    counters = ('Intervals','Slices','Creates','Disposes','Tiles','Meshes')
    return {'sums': {k:sum(v[k] for v in w) for k in w[0]},
            'distributions':{k:stats((v[k] for v in w),frame_ids=(r.get('DisplayFrame',i) for i,r in enumerate(measured))) for k in w[0] if k not in counters},
            'debt':{'admitted':sum(r['Admitted'] for r in measured),
                    'retired':sum(v['Intervals'] for v in w)*15625,
                    'firstBefore':measured[0]['DebtBefore'], 'lastAfter':measured[-1]['DebtAfter'],
                    'maximumBefore':max(r['DebtBefore'] for r in measured),
                    'maximumBeforeService':max(r['DebtBefore']+r['Admitted'] for r in measured),
                    'maximumAfter':max(r['DebtAfter'] for r in measured),
                    'reconciliationErrors':sum(r['DebtBefore']+r['Admitted']-r['Work']['Intervals']*15625 != r['DebtAfter'] for r in measured),
                    'crossRowContinuityErrors':sum(b['DebtBefore']!=a['DebtAfter'] for a,b in zip(measured,measured[1:]) if 'DisplayFrame' not in a or b['DisplayFrame']==a['DisplayFrame']+1),
                    'maximumIntervalsPerCallback':max(v['Intervals'] for v in w)}}

def fixture(path):
    rows = json.loads(path.read_text(encoding='utf-8-sig'))
    groups = {'all':rows,'contact':[r for r in rows if r['After']['TerrainContacts']>0],
              'separatedContactAuthority':[r for r in rows if r['After']['Consumer']!=0 and r['After']['TerrainContacts']==0],
              'freeFlight':[r for r in rows if r['After']['Consumer']==0]}
    return {'path':str(path),'first':rows[0]['Before'],'last':rows[-1]['After'],
            'groups':{k:{'serviceMs':stats(r['ServiceMs'] for r in v),'work':work(v)} for k,v in groups.items()}}

def native(root):
    report = json.loads((root/'native.json').read_text(encoding='utf-8-sig'))
    rows = report['rows']
    frames = [{k:float(v) for k,v in r.items()} for r in csv.DictReader((root/'native-timing.csv').open())]
    byloop = {int(f['loop']):f for f in frames}
    byterrain = {int(f['terrain']):f for f in frames if f['submitted']}
    physical = {r['DisplayFrame']:r for r in rows}
    assert len(byloop)==len(frames) and len(physical)==len(rows)
    assert all(r['DisplayFrame'] in byloop for r in rows), 'missing native/managed frame join'
    join_error=max(abs(r['FrameMs']-byloop[r['DisplayFrame']]['cadenceMs']) for r in rows)
    assert all(abs(r['FrameMs']-byloop[r['DisplayFrame']]['cadenceMs'])<=max(.001,abs(r['FrameMs'])*2e-7) for r in rows), 'native/managed cadence disproves frame join'
    # Cadence at N closes the start(N-1) -> start(N) interval.
    for f in frames:
        following = byloop.get(int(f['loop'])+1)
        f['followingCadenceMs'] = following['cadenceMs'] if following else None
    samples = {}
    for f in frames:
        if f['gpuSample']>0: samples.setdefault(int(f['gpuSample']), f)
    gpu = []
    for f in samples.values():
        owner = byterrain.get(int(f['gpuFrame']))
        assert owner is not None, 'GPU sample has no submitted frame owner'
        gpu.append((owner, f))
    def group(selected):
        ids = {int(f['loop']) for f in selected}
        ps = [physical[n] for n in sorted(ids) if n in physical]
        gs = [(owner,g) for owner,g in gpu if int(owner['loop']) in ids]
        return {'frames':len(selected),'cpu':{k:stats((f[k] for f in selected),frame_ids=(int(f['loop']) for f in selected)) for k in
                    ('frameMs','followingCadenceMs','updateMs','fenceMs','inspectionMs','callbackMs','uploadMs','recordMs','submitMs','presentMs')},
                'gpu':{k:stats((g[k] for _,g in gs),frame_ids=(int(owner['loop']) for owner,_ in gs)) for k in frames[0] if k.startswith('gpu') and k not in ('gpuFrame','gpuSample')},
                'gpuCoverage':{'submittedOwners':sum(bool(f['submitted']) for f in selected),'sampleOwners':len(gs),
                    'missingOwnerIds':sorted({int(f['loop']) for f in selected if f['submitted']}-{int(o['loop']) for o,_ in gs}),
                    'duplicateOwnerSamples':len(gs)-len({int(o['loop']) for o,_ in gs})},
                'serviceMs':stats((r['ServiceMs'] for r in ps if r['Work'] is not None),frame_ids=(r['DisplayFrame'] for r in ps if r['Work'] is not None)),
                'observerMs':stats((r['ObserverMs'] for r in ps),frame_ids=(r['DisplayFrame'] for r in ps)),
                'instrumentationMs':stats((r['InstrumentationMs'] for r in ps if r['Work'] is not None),frame_ids=(r['DisplayFrame'] for r in ps if r['Work'] is not None)),
                'serviceAllocatedBytes':stats((r['ServiceAllocated'] for r in ps if r['Work'] is not None),budget=False),
                'work':work(ps)}
    flight = [byloop[n] for n in physical]
    predicates = {
        'all':lambda r:True,
        'grounded':lambda r:r['Physical']['TerrainContacts']>0,
        'groundedNoActuation':lambda r:r['Physical']['TerrainContacts']>0 and not r['Physical']['Main'] and r['Physical']['Jets']==0,
        'restingNoActuation':lambda r:r['Physical']['TerrainContacts']>0 and not r['Physical']['Main'] and r['Physical']['Jets']==0 and r['Physical']['Velocity']['LengthSquared']<.000004 and r['Physical']['Angular']['LengthSquared']<.000004,
        'groundedRcs':lambda r:r['Physical']['TerrainContacts']>0 and r['Physical']['Jets']>0,
        'separatedContactAuthority':lambda r:r['Physical']['TerrainContacts']==0 and r['Physical']['Consumer']!=0,
        'freeFlight':lambda r:r['Physical']['Consumer']==0,
        'postHandoffFreeFlight':lambda r:r['Physical']['Consumer']==0 and r['Physical']['World']==0 and r['Physical']['Sequence']>0,
        'postHandoffNoNativeSlices':lambda r:r['Physical']['Consumer']==0 and r['Physical']['World']==0 and r['Physical']['Sequence']>0 and r['Work'] is not None and r['Work']['Slices']==0,
        'contactWithoutWorldOrTileCreation':lambda r:r['Physical']['TerrainContacts']>0 and r['Work'] is not None and r['Work']['Creates']==0 and r['Work']['Tiles']==0,
    }
    worst = sorted(flight,key=lambda f:f['frameMs'],reverse=True)[:12]
    return {'ceilingMs':CEILING,'percentiles':'nearest rank; chronological runs; no outliers removed',
            'route':{k:report[k] for k in ('judgment','error','TerrainSeen','GroundedRcs','Powered','source','vessel')},
            'first':rows[0]['Physical'],'last':rows[-1]['Physical'],
            'nativeAll':group(frames),
            'flightGroups':{k:group([byloop[r['DisplayFrame']] for r in rows if pred(r)]) for k,pred in predicates.items()},
            'worstFlightFrames':[{'native':f,'physical':physical[int(f['loop'])]} for f in worst],
            'gpuDistinctSamples':len(gpu),
            'maximumCadenceJoinErrorMs':join_error,
            'instrumentationAllocatedBytes':sum(r['InstrumentationAllocated'] for r in rows),
            'observerAllocatedBytes':sum(r['ObserverAllocated'] for r in rows),
            'producer':{k:report[k] for k in ('header','producerMicroseconds','timedRecorderAllocations','timedRecorderBytes')},
            'join':'managed DisplayFrame=native loop; GPU deduplicated by sample and joined by terrain identity; cadence shifted one frame',
            'notes':['CPU/GPU intervals overlap and must not be summed.',
                     'CPU consecutive counts break at missing frame IDs; sparse GPU samples are not continuous coverage.',
                     'Physical groups classify post-service endpoints; transition callbacks can contain both owners. Zero-interval callbacks are included and are not solver samples.',
                     'First restored Work=null is not a measured zero.',
                     'FrameMs is Update+Draw; cadence includes next pump/observer interval.',
                     'InstrumentationMs excludes stage timestamp overhead which remains inside serviceMs.']}

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--root',type=pathlib.Path,default=pathlib.Path('build/post-contact-performance'))
    p.add_argument('--output',type=pathlib.Path,required=True);p.add_argument('--offline-only',action='store_true');a=p.parse_args()
    result={'fixtures':{path.stem:fixture(path) for path in a.root.glob('*replay*.json')}}
    for name in ('baseline','corrected'):
        path=a.root/(name+'.json')
        if path.exists():result['fixtures'][name]=fixture(path)
    if not a.offline_only:result['native']=native(a.root)
    def clean(value):
        if isinstance(value,float) and not math.isfinite(value):return None
        if isinstance(value,dict):return {k:clean(v) for k,v in value.items()}
        if isinstance(value,list):return [clean(v) for v in value]
        return value
    a.output.write_text(json.dumps(clean(result),indent=2,allow_nan=False)+'\n')
    print('POST_CONTACT_ANALYSIS',a.output)
