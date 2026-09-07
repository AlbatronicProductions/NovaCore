"""Bounded exact attachment and retained physical-record comparisons."""
import sys
sys.dont_write_bytecode=True
from pathlib import Path
import hashlib,json
import numpy as np
sys.path.insert(0,str(Path(__file__).resolve().parent.parent/'active-refinement-usefulness'))
from capture_analysis import SAMPLE,digest
class Compare:
    def __init__(self):self.base=None
    def __call__(self,runtime,result):
        data=(runtime/'pixels.bin').read_bytes();n=3440*1440
        a=np.fromfile(runtime/'tes.bin',dtype=SAMPLE,offset=32)
        physical=np.unique(np.ascontiguousarray(a.view('u1').reshape(-1,128)[:,:112]).view('V112').reshape(-1))
        selected=np.fromfile(runtime/'selected.bin',dtype='<u4').reshape(-1,3)
        selectedSet=digest(np.sort(np.ascontiguousarray(selected).view('V12').reshape(-1)))
        resultData=dict(selectedTriangleSetSha256=selectedSet,tesRecords=len(a),physicalUnique=len(physical),physicalSha256=digest(physical),prepared=result['rawFiles']['prepared.bin'],selected=result['rawFiles']['selected.bin'],pixelSha256=hashlib.sha256(data).hexdigest())
        if (runtime/'visibility.bin').exists():
            h=np.fromfile(runtime/'visibility.bin',dtype='<u4',count=4)
            v=np.fromfile(runtime/'visibility.bin',dtype='<u4',offset=16).reshape(-1,16)
            resultData['oracleCapture']=dict(header=h.tolist(),patches=len(v),proposed=int(np.count_nonzero(v[:,12])),falseRejectedVertices=int(v[:,15].astype('u8').sum()),containmentViolations=int(v[:,9].astype('u8').sum()))
        if self.base is None:self.base=(data,physical,resultData)
        else:
            old,records,report=self.base;parity={}
            for name,lo,hi in [('depth',0,n*4),('hdr',n*4,n*12),('image',n*12,n*16)]:
                x=np.frombuffer(old,dtype='u1',count=hi-lo,offset=lo);y=np.frombuffer(data,dtype='u1',count=hi-lo,offset=lo)
                parity[name]=dict(exact=bool(np.array_equal(x,y)),unequalBytes=int(np.count_nonzero(x!=y)))
            parity.update(preparedExact=resultData['prepared']==report['prepared'],selectedExact=resultData['selected']==report['selected'],selectedTriangleSetExact=selectedSet==report['selectedTriangleSetSha256'],retainedPhysicalExact=bool(np.isin(physical,records).all()),allPhysicalExact=bool(np.array_equal(physical,records)))
            resultData['versusBaseline']=parity
        print('Exact capture:',json.dumps(resultData),flush=True)
        return resultData
