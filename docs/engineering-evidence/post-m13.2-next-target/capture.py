"""Bounded final-triangle parent oracle and exact TES reuse census.

The zero-support test is deliberately a counterexample test, NOT a safe culler.
Whole-parent final clipping is a hindsight ceiling, not a pre-TES predicate.
"""
import sys,re,json
sys.dont_write_bytecode=True
from pathlib import Path
import numpy as np
from run import run,OUT
from capture_analysis import SAMPLE,TRI,keys,clip_polygon,Parity

def parents(runtime,result):
    s=np.memmap(runtime/'tes.bin',dtype=SAMPLE,offset=32,mode='r')
    t=np.memmap(runtime/'triangles.bin',dtype=TRI,offset=16,mode='r')
    n=len(t);parent=s['identity'][t['samples'][:,0],0].copy()
    size=int(parent.max())+1
    clipped=np.zeros(n,dtype=bool);planes=np.zeros(6,dtype='i8');exclusive=np.zeros(7,dtype='i8')
    area=np.zeros(n);partial=0
    for start in range(0,n,100000):
        v=t['clip'][start:start+100000].astype('f8');x,y,z,w=[v[:,:,i] for i in range(4)]
        ds=np.stack([w+x,w-x,w+y,w-y,z,w-z],axis=2)
        mask=np.all(ds<0,axis=1);out=mask.any(axis=1);inside=np.all(ds>=0,axis=(1,2))
        planes+=mask.sum(axis=0);first=np.argmax(mask,axis=1)
        exclusive[:6]+=np.bincount(first[out],minlength=6)
        for i in np.flatnonzero(~out&~inside):
            area[start+i],corners=clip_polygon(v[i]);out[i]=corners<3;partial+=1
            if out[i]:exclusive[6]+=1
        ix=np.flatnonzero(inside)
        q=v[ix,:,:2]/v[ix,:,3,None];ab=q[:,1]-q[:,0];ac=q[:,2]-q[:,0]
        area[start+ix]=(ab[:,0]*ac[:,1]-ab[:,1]*ac[:,0])*.5
        clipped[start:start+len(v)]=out
    generated=np.bincount(parent,minlength=size)
    surviving=np.bincount(parent[~clipped],minlength=size)
    entered=t['counters'][:,0]>0
    frag=np.bincount(parent[entered],minlength=size)
    whole=(generated>0)&(surviving==0);mixed=(surviving>0)&(surviving<generated)
    zero=(generated>0)&(frag==0)
    positive=int(np.count_nonzero(area[entered]>0));negative=int(np.count_nonzero(area[entered]<0))
    back=area>0 if negative>positive else area<0
    # Recover pre-TES prepared corners with same-frame compacted indices.
    prep=np.memmap(runtime/'prepared.bin',dtype=np.dtype([('body','<f8',4),('normal','<f4',4),('reserved','<f4',4)]),mode='r')
    indices=np.fromfile(runtime/'selected.bin',dtype='<u4').reshape(-1,3)
    rows=(OUT/(result['name']+'.log')).read_text().splitlines()
    row=next(r for r in rows if 'P2S5F directional visibility:' in r and 'submittedFrame=175;' in r)
    def vector(name):return np.array([float(v) for v in re.search(name+r'=\(([^)]+)\)',row)[1].split(',')])
    cam=vector('cameraBody');quat=vector('bodyOrientation');m=vector('viewProjection').reshape(4,4).T
    p=(prep['body'][indices,:3]-cam).astype('f4');q=quat.astype('f4')
    p=p+np.float32(2)*np.cross(q[:3],np.cross(q[:3],p)+q[3]*p)
    c=np.concatenate([p,np.ones((*p.shape[:2],1),dtype='f4')],axis=2)@m.T
    x,y,z,w=[c[:,:,i] for i in range(4)];ds=np.stack([w+x,w-x,w+y,w-y,z,w-z],axis=2)
    proposed=np.any(np.all(ds<0,axis=1),axis=1)
    assert len(indices)==size
    counterexample=proposed&(surviving>0)
    report=dict(generatedTriangles=n,parents=size,fullyClipped=int(clipped.sum()),clipSurviving=int((~clipped).sum()),
        planeNames=['left','right','bottom','top','near','far','cross-plane'],planeOverlappingCounts=planes.tolist(),planeExclusiveCounts=exclusive.tolist(),partialClipInputs=partial,
        whollyClippedParents=int(whole.sum()),partiallyVisibleParents=int(mixed.sum()),
        clippedTrianglesInWhollyClippedParents=int(generated[whole].sum()),clippedTrianglesInPartiallyVisibleParents=int(clipped[mixed[parent]].sum()),
        parentsWithNoEarlyDepthFragments=int(zero.sum()),trianglesInNoFragmentParents=int(generated[zero].sum()),
        fragmentProducingTriangles=int(entered.sum()),backFacingClipSurvivors=int(np.count_nonzero(back&~clipped)),
        frontWinding=dict(positive=positive,negative=negative),clippedWithFragments=int(np.count_nonzero(clipped&entered)),
        zeroEnvelopeCounterexample=dict(proposedRejects=int(proposed.sum()),trueClipRejects=int(np.count_nonzero(proposed&whole)),falseClipRejects=int(counterexample.sum()),falseFragmentRejects=int(np.count_nonzero(proposed&(frag>0))),falseRetainedWholeParents=int(np.count_nonzero(~proposed&whole)),generatedTrianglesInProposedRejects=int(generated[proposed].sum())),
        safeExistingBound=dict(additionalProposedRejects=0,trueRejects=0,falseRejects=0,reason='Existing conservative predicate already ran before TCS; no stronger proven bound is assumed.'),
        hindsightCeiling=dict(patches=int(whole.sum()),generatedTriangles=int(generated[whole].sum()),reason='Requires all final triangles; not an earlier algorithm and not a millisecond saving.'),
        rawBytes=sum(v['bytes'] for v in result['rawFiles'].values()))
    print('Parent oracle',json.dumps(report),flush=True)
    return report

def census(runtime,result):
    s=np.memmap(runtime/'tes.bin',dtype=SAMPLE,offset=32,mode='r');d=keys(s['direction'][:,:3]);patch=s['identity'][:,0]
    unique,inv,count=np.unique(d,return_inverse=True,return_counts=True)
    lo=np.full(len(unique),np.iinfo('u4').max,dtype='u4');hi=np.zeros(len(unique),dtype='u4')
    np.minimum.at(lo,inv,patch);np.maximum.at(hi,inv,patch)
    pair=keys(np.column_stack([s['direction'][:,:3].copy().view('u4'),patch]))
    npairs=len(np.unique(pair));cross=lo!=hi;edge=s['identity'][:,1]>0
    near=s['direction'][:,3]>0
    values,freq=np.unique(count,return_counts=True)
    return dict(executions=len(s),uniqueExactDirections=len(unique),duplicateExecutions=len(s)-len(unique),withinPatchRepeatedExecutions=len(s)-npairs,acrossPatchRepeatedIdentities=npairs-len(unique),crossPatchDirectionGroups=int(cross.sum()),sharedEdgeExecutions=int(edge.sum()),nearExecutions=int(near.sum()),outsideNearExecutions=int((~near).sum()),uniqueNearDirections=len(np.unique(d[near])),frequencyGroups={str(int(v)):int(f) for v,f in zip(values,freq)},identity='Exact FP64 direction bits; no proximity merging. Counts are capture-shader executions, not assumed identical to uninstrumented driver accounting.')

if __name__=='__main__':
    mode=sys.argv[1]
    if mode=='parents':
        for label,pose in [('D','B'),('E','C'),('C','D'),('B','A'),('A','E')]:run(pose,'normal','parent-'+label,capture='gs',callback=parents)
    elif mode=='tes':
        for label,pose in [('D','B'),('E','C')]:run(pose,'normal','census-'+label,capture='tes',callback=census)
        for label,pose in [('A','E'),('B','A'),('C','D'),('D','B'),('E','C')]:
            parity=Parity()
            for probe in ['normal','defer-base']:run(pose,probe,'parity-'+label+'-'+probe,capture='tes',callback=parity)
