"""Final TES/primitive records; NumPy only, bounded summaries retained.

Projected areas use actual captured FP32 clip positions and Vulkan's homogeneous
clip volume. Counts from shader atomics exclude helper stores by Vulkan semantics.
They count early-depth survivors at entry, then non-discarded normal-path outputs.
"""
import hashlib
import numpy as np

SAMPLE=np.dtype([('body','<f8',4),('direction','<f8',4),('clip','<f4',4),('bary','<f4',4),('near','<f4',4),('identity','<u4',4)])
TRI=np.dtype([('clip','<f4',(3,4)),('samples','<u4',4),('counters','<u4',4)])
assert SAMPLE.itemsize==128 and TRI.itemsize==80

def digest(a):return hashlib.sha256(np.ascontiguousarray(a).tobytes()).hexdigest()
def quantiles(a):
    if not len(a):return None
    return dict(zip(['p10','p50','p90'],map(float,np.quantile(a,[.1,.5,.9],method='inverted_cdf'))))
def keys(a):return np.ascontiguousarray(a).view(np.dtype(('V',a.shape[1]*a.dtype.itemsize))).reshape(-1)

def clip_polygon(vertices):
    poly=list(vertices)
    planes=[lambda p:p[3]+p[0],lambda p:p[3]-p[0],lambda p:p[3]+p[1],lambda p:p[3]-p[1],lambda p:p[2],lambda p:p[3]-p[2]]
    for plane in planes:
        if not poly:return 0.0,0
        out=[];a=poly[-1];fa=plane(a)
        for b in poly:
            fb=plane(b)
            if (fa>=0)!=(fb>=0):out.append(a+(b-a)*(fa/(fa-fb)))
            if fb>=0:out.append(b)
            a=b;fa=fb
        poly=out
    if len(poly)<3:return 0.0,0
    p=np.asarray(poly);q=p[:,:2]/p[:,3,None]*np.array([1720.,720.])
    return float(np.sum(q[:,0]*np.roll(q[:,1],-1)-q[:,1]*np.roll(q[:,0],-1))*.5),len(poly)

def analyze(runtime,result):
    sampleCount=int(np.fromfile(runtime/'tes.bin',dtype='<u4',count=1)[0])
    samples=np.fromfile(runtime/'tes.bin',dtype=SAMPLE,offset=32,count=sampleCount)
    assert len(samples)==sampleCount
    assert np.isfinite(samples['direction']).all()
    assert np.all(np.abs(np.linalg.norm(samples['direction'][:,:3],axis=1)-1)<1e-10)
    assert np.all((samples['direction'][:,3]>=0)&(samples['direction'][:,3]<=1))
    report={'tesRecords':sampleCount}
    if sampleCount:
        dkeys=keys(samples['direction'][:,:3]);pkeys=keys(samples['body'][:,:3])
        unique,first,inv,counts=np.unique(dkeys,return_index=True,return_inverse=True,return_counts=True)
        patches=samples['identity'][:,0]
        lo=np.full(len(unique),np.iinfo('u4').max,dtype='u4');hi=np.zeros(len(unique),dtype='u4')
        np.minimum.at(lo,inv,patches);np.maximum.at(hi,inv,patches)
        near=samples['direction'][:,3]>0
        uniquePhysical=np.unique(pkeys)
        semantic=keys(np.column_stack([patches,samples['bary'][:,:3].view('<u4')]))
        records=np.unique(samples.view('V128'))
        report.update(uniqueDirections=len(unique),duplicateDirectionEvaluations=sampleCount-len(unique),uniquePhysicalPositions=len(uniquePhysical),duplicatePhysicalPositions=sampleCount-len(uniquePhysical),sharedPatchDirectionGroups=int(np.count_nonzero(lo!=hi)),sharedPatchDuplicateEvaluations=int(np.sum(counts[lo!=hi]-1)),edgeEvaluations=int(np.count_nonzero(samples['identity'][:,1])),uniquePatchBarycentric=len(np.unique(semantic)),nearEvaluations=int(np.count_nonzero(near)),outsideNearEvaluations=int(np.count_nonzero(~near)),uniqueNearDirections=len(np.unique(dkeys[near])),canonicalRecordSetSha256=digest(records),uniqueRecordCount=len(records))
        del uniquePhysical,records,semantic,pkeys,unique,first,inv,counts,lo,hi
        # Compaction order changes gl_PrimitiveID between runs. Exclude that
        # dispatch identity when comparing physical outputs at sampled locations.
        physical=keys(np.ascontiguousarray(samples.view('u1').reshape(-1,128)[:,:112]))
        report['physicalRecordSetSha256']=digest(np.unique(physical))
        del physical
    if (runtime/'triangles.bin').exists():
        n=int(np.fromfile(runtime/'triangles.bin',dtype='<u4',count=1)[0])
        tri=np.fromfile(runtime/'triangles.bin',dtype=TRI,offset=16,count=n)
        assert n and int(tri['samples'][:,:3].max())<sampleCount, str(dict(triangles=n,sampleCount=sampleCount,maxIndex=int(tri['samples'][:,:3].max()),firstRecords=str(tri[:3])))
        entered=tri['counters'][:,0]>0;output=tri['counters'][:,1]>0
        area=np.zeros(n,dtype='f8');corners=np.zeros(n,dtype='u1');outside=np.zeros(n,dtype=bool)
        partialCount=0
        for start in range(0,n,100000):
            v=tri['clip'][start:start+100000].astype('f8');x,y,z,w=[v[:,:,i] for i in range(4)]
            ds=np.stack([w+x,w-x,w+y,w-y,z,w-z],axis=2)
            out=np.any(np.all(ds<0,axis=1),axis=1);inside=np.all(ds>=0,axis=(1,2));outside[start:start+len(v)]=out
            idx=np.flatnonzero(inside)
            if len(idx):
                q=v[idx,:,:2]/v[idx,:,3,None]*np.array([1720.,720.]);ab=q[:,1]-q[:,0];ac=q[:,2]-q[:,0]
                area[start+idx]=(ab[:,0]*ac[:,1]-ab[:,1]*ac[:,0])*.5;corners[start+idx]=3
            for i in np.flatnonzero(~out&~inside):
                area[start+i],corners[start+i]=clip_polygon(v[i]);partialCount+=1
        survives=corners>=3
        # Determine the actual front-winding sign from primitives proven to enter
        # fragment processing; retain both signs instead of assuming conventions.
        signed=area[entered];positive=int(np.count_nonzero(signed>0));negative=int(np.count_nonzero(signed<0))
        front=area<0 if negative>positive else area>0
        def distribution(mask):
            a=np.abs(area[mask]);return dict(count=len(a),area=quantiles(a),below={str(t):dict(count=int(np.count_nonzero(a<t)),percent=float(100*np.mean(a<t)) if len(a) else 0) for t in [.25,.5,1.,2.]})
        ids=tri['samples'][:,:3]
        used=np.unique(ids[output]);allReferenced=np.unique(ids)
        report.update(generatedPrimitives=n,tesReferencedByGeneratedTriangles=len(allReferenced),homogeneousRejected=int(np.count_nonzero(~survives)),trivialHomogeneousRejected=int(np.count_nonzero(outside)),partialClipInput=partialCount,postClipPolygons=int(np.count_nonzero(survives)),postClipFanTriangles=int(np.sum(np.maximum(corners.astype('i4')-2,0))),frontFacingPolygons=int(np.count_nonzero(survives&front)),oppositeFacingPolygons=int(np.count_nonzero(survives&~front&(area!=0))),degeneratePolygons=int(np.count_nonzero(survives&(area==0))),earlyDepthSurvivingPrimitives=int(np.count_nonzero(entered)),colorOutputPrimitives=int(np.count_nonzero(output)),zeroFragmentPrimitives=int(np.count_nonzero(~entered)),earlyDepthSurvivingInvocations=int(np.sum(tri['counters'][:,0],dtype='u8')),colorOutputInvocations=int(np.sum(tri['counters'][:,1],dtype='u8')),observedFragmentWinding=dict(positive=positive,negative=negative),clippedButFragmentEntered=int(np.count_nonzero(~survives&entered)),oppositeFacingButFragmentEntered=int(np.count_nonzero(~front&entered)),clippedAreaDistribution=distribution(survives),frontAreaDistribution=distribution(survives&front),fragmentAreaDistribution=distribution(output),tesAdjacentToColorOutput=len(used),uniqueDirectionsAdjacentToColorOutput=len(np.unique(dkeys[used])),nearTesAdjacentToColorOutput=int(np.count_nonzero(near[used])),frontFacingZeroFragment=int(np.count_nonzero(front&survives&~entered)))
        if result['captureMode']=='ids':
            pixels=3440*1440;hdr=np.fromfile(runtime/'pixels.bin',dtype='<f2',offset=pixels*4,count=pixels*4).reshape(-1,4)
            valid=hdr[:,3]==16384
            winners=hdr[valid,0].astype('u4')+(hdr[valid,1].astype('u4')<<10)+(hdr[valid,2].astype('u4')<<20)-1
            assert len(winners) and int(winners.max())<n
            winning=np.unique(winners);vertices=np.unique(ids[winning])
            report['_visibleDirectionKeys']=np.unique(dkeys[vertices])
            report.update(uniqueDirectionsAdjacentToFinalPixels=len(report['_visibleDirectionKeys']),finalOwnerPixels=int(np.count_nonzero(valid)),finalVisiblePrimitives=len(winning),finalVisibleTesCornerRecords=len(vertices),nearFinalVisibleTesCornerRecords=int(np.count_nonzero(near[vertices])),finalVisibleAreaDistribution=distribution(np.isin(np.arange(n),winning)),tesPerOwnerPixel=sampleCount/len(winners),generatedPrimitivesPerOwnerPixel=n/len(winners),finalColorOverdraw=report['colorOutputInvocations']/len(winners))
    return report

class Parity:
    """One baseline attachment in memory; no raw archive retained on disk."""
    def __init__(self):self.base=None;self.baseDirections=None
    def __call__(self,runtime,result):
        report=analyze(runtime,result)
        data=(runtime/'pixels.bin').read_bytes()
        visible=report.pop('_visibleDirectionKeys',None)
        if self.base is None:
            self.base=(data,report)
            s=np.fromfile(runtime/'tes.bin',dtype=SAMPLE,offset=32)
            self.baseDirections=keys(s['direction'][:,:3]).copy()
        elif visible is not None:
            report['baselineTesExecutionsAdjacentToFinalPixels']=int(np.count_nonzero(np.isin(self.baseDirections,visible)))
            report['baselineTesPerOwnerPixel']=len(self.baseDirections)/report['finalOwnerPixels']
        if self.base[0] is not data:
            a,ar=self.base;n=3440*1440;parity={}
            for name,lo,hi in [('depth',0,n*4),('hdr',n*4,n*12),('image',n*12,n*16)]:
                x=np.frombuffer(a[lo:hi],dtype='u1');y=np.frombuffer(data[lo:hi],dtype='u1')
                parity[name]=dict(exact=bool(np.array_equal(x,y)),unequalBytes=int(np.count_nonzero(x!=y)))
                if name=='depth':
                    dx=x.view('<f4');dy=y.view('<f4');parity[name].update(changedPixels=int(np.count_nonzero(dx!=dy)),maxAbsolute=float(np.max(np.abs(dx.astype('f8')-dy.astype('f8')))))
            parity['physicalRecordSetExact']=report.get('physicalRecordSetSha256')==ar.get('physicalRecordSetSha256')
            parity['tesRecordSetExact']=report.get('canonicalRecordSetSha256')==ar.get('canonicalRecordSetSha256') if report['tesRecords'] and ar['tesRecords'] else None
            report['versusBaseline']=parity
        print('Capture analysis',report,flush=True)
        return report
