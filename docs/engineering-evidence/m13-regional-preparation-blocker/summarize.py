"""Compact, auditable statistics; no GPU execution or source mutations."""
import hashlib,json,statistics
from gauntlet import HERE,OUT,assess

def stats(v):
    s=assess.stats(v)
    if s:s.update(mean=statistics.mean(v),total=sum(v))
    return s

def summarize(label):
    d=assess.read(HERE/(label+'.json'))
    hosts=[r for r in d['hostRows'] if r['frame']>30]
    valid=[r for r in d['hostRows'] if r['gpuFrame']==r['geometryFrame']==r['frame']-1 and r['gpuFrame']>0]
    # Submission IDs are unique within these Earth-only transition windows.
    bands=[]
    for gen,lo in [(18,345),(19,357),(36,725),(37,737)]:
        gpu=[r for r in d['gpuRows'] if lo<=r['frame']<=lo+10]
        prep=[r for r in d['preparation']['incomingPhysicalPreparation'] if lo<=r['frame']<=lo+10]
        assert len(gpu)==len(prep)==11
        pubs=[assess.fields(x) for x in d['lines'] if 'Prep publication:' in x and f'generation={gen};' in x]
        bands.append(dict(generation=gen,firstGpuFrame=lo,lastGpuFrame=lo+10,publicationHostFrame=lo+11,
          gpu=stats([r['total'] for r in gpu]),prep=stats([r['ms'] for r in prep]),
          first8Prep=stats([r['ms'] for r in prep[:8]]),last3Prep=[r['ms'] for r in prep[-3:]],
          last3Gpu=[r['total'] for r in gpu[-3:]],publication=pubs,
          finalCullPreparationResidual=gpu[-1]['cullAndPreparation']-prep[-1]['ms']))
    selected=[r for r in d['gpuRows'] if any(b['firstGpuFrame']<=r['frame']<=b['lastGpuFrame'] for b in bands)]
    lines=d['lines']
    return dict(label=label,nativeHash=d['nativeHash'],managedHash=d['managedHash'],errors=d['errors'],
      frames=len(d['hostRows']),publications=sum('Production spherical billboard publication:' in x for x in lines),
      stagedPublications=sum('NCSM1 staged pupil publication:' in x for x in lines),
      validEarthGpu=stats([r['gpuTotal'] for r in valid]),bands=bands,
      combinedBands=stats([r['total'] for r in selected]),
      preparation={k:stats([r['ms'] for r in values]) for k,values in d['preparation'].items()},
      cpu={k:stats([r[k] for r in hosts]) for k in ['update','fenceWait','inspection','hostCallback','validationUpload','record','submit','present','total']},
      post30HostOver40ms=[r for r in hosts if r['total']>=40],
      residency=[x for x in lines if 'regional physical totals:' in x],
      heap=[x for x in lines if 'Prep heap:' in x],
      routeResult=[x for x in lines if 'PASS:' in x or 'Rendered ' in x or 'route validation:' in x])

if __name__=='__main__':
    labels=['profile','profile-repeat','same-index-copy','placement-only','same-index-height','same-index-normal']
    data={label:summarize(label) for label in labels}
    oracle=json.loads((HERE/'same-index-oracle.json').read_text())
    rows=[assess.fields(x) for x in oracle['lines'] if 'Prep GPU oracle:' in x]
    data['gpuOracle']={'publications':len(set(r['generation'] for r in rows)),
       'proposedCopies':sum(r['exactDirectionReuse'] for r in rows),
       'mismatches':sum(r['physicalWordMismatches'] for r in rows),
       'note':'Mismatch total is repeated per slice, zero remains exact.'}
    failure=OUT/'same-index-oracle-failure.txt'
    if failure.exists():
        content=failure.read_bytes();text=content.decode('utf-8')
        data['rejectedInitialProbe']={'sha256':hashlib.sha256(content).hexdigest(),'bytes':len(content),
          'errors':list(dict.fromkeys(x for x in text.splitlines() if 'Vulkan validation [error]' in x)),
          'classification':'Private prototype bootstrap descriptor initialization failure; fixed and rerun strictly. Not a production failure.'}
    (HERE/'summary.json').write_text(json.dumps(data,indent=2)+'\n')
    for label in labels:
        r=data[label];print(label,'GPU bands',[(b['generation'],round(b['gpu']['mean'],3),round(b['gpu']['max'],3)) for b in r['bands']], 'all44',r['combinedBands'])
