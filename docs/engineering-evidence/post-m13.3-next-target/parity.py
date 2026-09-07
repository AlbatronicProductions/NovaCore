"""Reuse one bounded capture file, compare exact attachment bytes in memory."""
import argparse,hashlib,json,sys
sys.dont_write_bytecode=True
import numpy as np
from run import run,OUT,HERE
def compare(pose,label,mixed=False,aa=False):
    a=run(pose,label+'-before',capture=True,mixed=mixed)
    before=(OUT/'pixels.bin').read_bytes()
    b=run(pose,label+'-after',zero=not aa,capture=True,mixed=mixed)
    after=(OUT/'pixels.bin').read_bytes()
    n=3440*1440
    result={'pose':pose,'mixed':mixed,'aa':aa,'frame':175,'before':a['capture'],'after':b['capture'],'attachments':{}}
    for name,begin,end,dtype,channels in [('depth',0,n*4,'<f4',1),('hdr',n*4,n*12,'<f2',4),('image',n*12,n*16,'u1',4)]:
        x=np.frombuffer(before[begin:end],dtype=dtype).reshape(n,channels);y=np.frombuffer(after[begin:end],dtype=dtype).reshape(n,channels)
        xb=np.frombuffer(before[begin:end],dtype=np.uint8).reshape(n,-1);yb=np.frombuffer(after[begin:end],dtype=np.uint8).reshape(n,-1)
        delta=np.abs(x.astype(np.float64)-y.astype(np.float64))
        result['attachments'][name]={'equal':before[begin:end]==after[begin:end],'differentPixels':int(np.any(xb!=yb,axis=1).sum()),'maxAbsolute':float(delta.max()),'finite':bool(np.isfinite(x).all() and np.isfinite(y).all()),'beforeSha256':hashlib.sha256(before[begin:end]).hexdigest(),'afterSha256':hashlib.sha256(after[begin:end]).hexdigest()}
        indexes=np.flatnonzero(np.any(xb!=yb,axis=1))[:16]
        result['attachments'][name]['examples']=[{'x':int(i%3440),'y':int(i//3440),'before':x[i].astype(float).tolist(),'after':y[i].astype(float).tolist()} for i in indexes]
    result['preparedExact']=a['capture']['files']['prepared.bin']['sha256']==b['capture']['files']['prepared.bin']['sha256']
    result['selectedExact']=a['capture']['files']['selected.bin']['sha256']==b['capture']['files']['selected.bin']['sha256']
    result['selectedOrientedMultisetExact']=a['capture']['selectedOrientedMultisetSha256']==b['capture']['selectedOrientedMultisetSha256']
    result['pass']=result['preparedExact'] and result['selectedOrientedMultisetExact'] and all(v['equal'] and v['finite'] for v in result['attachments'].values())
    (HERE/(label+'-parity.json')).write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps(result),flush=True)
    return result
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('pose');p.add_argument('label');p.add_argument('--mixed',action='store_true');p.add_argument('--aa',action='store_true');compare(**vars(p.parse_args()))
