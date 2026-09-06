"""Read-only cadence aggregation. GPU work is aligned by native submitted frame.
The window starts at scripted phase frame 80 and ends at the last submission.
Slice timing is not a whole-publication timing; both are reported separately.
"""
import sys,json,re,hashlib
from pathlib import Path
sys.dont_write_bytecode=True
sys.path.insert(0,str(Path(__file__).resolve().parent.parent/'near-surface-performance'))
from analyze import fields,stats

def cadence(path):
    lines=path.read_text(encoding='utf-8').splitlines();rows={};cameras={};pending=None
    for line in lines:
        if 'Preparation camera:' in line:pending=fields(line)
        if 'Performance CPU frame:' in line and pending is not None:
            v=fields(line);cameras[v['frame']]=pending;pending=None
    publications=[];lastCpu=0
    for line in lines:
        if 'Performance CPU frame:' in line:lastCpu=fields(line)['frame']
        if 'NCSM1 staged pupil publication:' in line:
            row=fields(line);row['frame']=lastCpu;publications.append(row)
    selected={f:c for f,c in cameras.items() if c['phaseFrame']>=80}
    lo=min(selected);hi=max(selected)
    identities=[fields(x) for x in lines if 'Preparation identity:' in x and 80<=fields(x)['phaseFrame']<=240]
    changedPupils=sum((a['pupil'],a['frameIdentity'])!=(b['pupil'],b['frameIdentity']) for a,b in zip(identities,identities[1:])) if identities else None

    kinds={'trigger':'Preparation trigger:','delta':'Preparation delta:','gpu':'Performance GPU frame:', 'sliceTime':'NCSM1 regional GPU work:', 'components':'Preparation components:'}
    for k,text in kinds.items():rows[k]=[fields(x) for x in lines if text in x and lo<=fields(x)['frame']<=hi]
    publications=[x for x in publications if lo<=x['frame']<=hi]
    times=[x['ms'] for x in rows['sliceTime'] if x['kind']=='currentPhysicalPreparation']
    gpu=[x['total'] for x in rows['gpu']]
    pubs=[]
    for trigger in rows['trigger']:
        done=next((d for d in publications if d['pupil']==trigger['pupil'] and d['generation']==trigger['generation']),None)
        if done:
            pubs.append(sum(x['ms'] for x in rows['sliceTime'] if x['kind']=='currentPhysicalPreparation' and trigger['frame']<=x['frame']<=done['frame']))
    changed={k:sum(int(t[k]!=0) for t in rows['trigger']) for k in ('latticeBasisChanged','eastOffsetChanged','northOffsetChanged','transitionChanged','metadataChanged')}
    d=rows['delta'];v=sum(x['vertices'] for x in d)
    result=dict(name=path.stem,logSha256=hashlib.sha256(path.read_bytes()).hexdigest(),firstFrame=lo,lastFrame=hi,frames=len(selected),gpuFrames=len(gpu),pupilIdentityChanges=changedPupils,identityRows=identities,triggers=len(rows['trigger']),completedPublications=len(publications),publicationRows=publications,currentSliceCount=len(times),currentSliceMs=stats(times),currentPublicationGpuMs=stats(pubs),currentGpuSumMs=sum(times),frameGpuSumMs=sum(gpu),currentGpuPercent=100*sum(times)/sum(gpu),triggerFieldCounts=changed,scriptedThresholdRadians=next(iter(selected.values()))['thresholdRadians'],scriptedAltitudeMetres=stats([c['altitude'] for c in selected.values()]),preparedVertices=v,sameIndexExact=sum(x['sameIndexExact'] for x in d),crossIndexPhysicalExact=sum(x['crossIndexPhysicalExact'] for x in d),crossIndexAdditionalExact=sum(x['crossIndexPhysicalExact']-x['sameIndexExact'] for x in d),deltaRows=d,componentRows=rows['components'],triggerRows=rows['trigger'],incomingSliceCount=sum(x['kind']=='incomingPhysicalPreparation' for x in rows['sliceTime']),firstCamera=next(iter(selected.values())),lastCamera=selected[hi])
    result['changedPercent']=None if not v else 100*(v-result['sameIndexExact'])/v
    result['additionalOverlapPercent']=None if not v else 100*result['crossIndexAdditionalExact']/v
    return result

if __name__=='__main__':
    out=Path('build/m13-next-target');results=[cadence(p) for p in sorted(out.glob('*.log')) if not p.stem.endswith('-failed') and p.stem.startswith(('near-','cadence-','florida-','copy-'))]
    (out/'cadence-summary.json').write_text(json.dumps(results,indent=2)+'\n')
    for r in results: print(r['name'],{k:r[k] for k in ('frames','triggers','completedPublications','currentGpuSumMs','currentGpuPercent','changedPercent','additionalOverlapPercent')})
