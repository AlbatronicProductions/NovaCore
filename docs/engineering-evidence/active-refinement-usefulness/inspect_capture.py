"""Validate binary layout before accepting geometric statistics."""
import numpy as np
from capture_analysis import SAMPLE,TRI

def inspect(runtime,result):
    count=int(np.fromfile(runtime/'tes.bin',dtype='<u4',count=1)[0])
    s=np.fromfile(runtime/'tes.bin',dtype=SAMPLE,offset=32,count=count)
    n=int(np.fromfile(runtime/'triangles.bin',dtype='<u4',count=1)[0])
    t=np.fromfile(runtime/'triangles.bin',dtype=TRI,offset=16,count=n)
    report=dict(samples=count,triangles=n,readSamples=len(s),readTriangles=len(t),
      firstSamples=str(s[:3]),firstTriangles=str(t[:3]),
      directionFinite=bool(np.isfinite(s['direction']).all()),
      directionNormError=float(np.max(np.abs(np.linalg.norm(s['direction'][:,:3],axis=1)-1))),
      weightMinimum=float(s['direction'][:,3].min()),weightMaximum=float(s['direction'][:,3].max()),
      indexMinimum=int(t['samples'][:,:3].min()),indexMaximum=int(t['samples'][:,:3].max()),
      invalidIndices=int(np.count_nonzero(t['samples'][:,:3]>=count)),
      clipFinite=bool(np.isfinite(t['clip']).all()),
      counterMaximum=int(t['counters'].max()),counterSum=t['counters'].sum(axis=0,dtype='u8').tolist())
    print(report,flush=True)
    return report
