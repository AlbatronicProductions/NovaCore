"""Explicit diagnostic batches. Apply instrumentation.patch and build first.

Runner letters retain the earlier campaign mapping: A orbital, B factor 1,
C Florida, D active, E grazing. No batch modifies deployed shaders.
"""
import argparse
import hashlib
import json
import sys
sys.dont_write_bytecode = True
import run

def main(batch):
    if batch == 'fixed':
        for label, pose in zip('ABCDE','BCDEA'):
            run.run(pose,'normal','fixed-'+label,geometry=True)
    elif batch == 'cadence':
        for motion in ['stationary','subthreshold','one']:
            run.run('B','normal','florida-'+motion+'-v2',motion=motion,level=17,delta=True,florida=True)
        for motion,speed in [('slow',10),('normal',50),('fast',250)]:
            run.run('B','normal',f'florida-{motion}-{speed}mps',motion=motion,level=17,delta=motion!='fast',florida=True,speed=speed)
        run.run('B','normal','florida-one-components',motion='one',level=17,delta=True,florida=True,components=True)
        run.run('B','normal','cadence-transition',motion='transition',level=17,delta=True)
    elif batch == 'decomposition':
        for pose in ['B','D']:
            for probe in ['no-near','no-normal','no-fragment','cheap-fragment','minimal-tcs']:
                run.run(pose,probe,f'probe-{pose}-{probe}',geometry=True)
    elif batch == 'copy-timing':
        for i in range(2):
            for probe in ['normal','copy-predicate']:
                run.run('B',probe,f'copy-timing-{i}-{probe}',motion='normal',level=17,florida=True,speed=50)
    elif batch == 'copy-parity':
        base=run.run('B','normal','copy-baseline-capture',motion='one',level=17,delta=True,florida=True,capture=True)
        probe=run.run('B','copy-predicate','copy-probe-capture',motion='one',level=17,delta=True,florida=True,capture=True)
        n=3440*1440
        result={}
        for key,start,end in [('depth',0,n*4),('hdr',n*4,n*12),('image',n*12,n*16),('prepared',0,n*0)]:
            a,b=(base['_raw']['prepared'],probe['_raw']['prepared']) if key=='prepared' else (base['_raw']['pixels'][start:end],probe['_raw']['pixels'][start:end])
            assert len(a)==len(b)
            result[key]=dict(bytes=len(a),exact=a==b,sha256Baseline=hashlib.sha256(a).hexdigest(),sha256Probe=hashlib.sha256(b).hexdigest(),unequalBytes=sum(x!=y for x,y in zip(a,b)))
        result.update(baselineIdentity=base['capture']['identity'],probeIdentity=probe['capture']['identity'],rawBytesCreated=base['capture']['bytes']+probe['capture']['bytes'])
        (run.OUTPUT/'copy-parity.json').write_text(json.dumps(result,indent=2)+'\n')
        print({k:v['exact'] for k,v in result.items() if isinstance(v,dict)})

if __name__ == '__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('batch',choices=['fixed','cadence','decomposition','copy-timing','copy-parity'])
    main(parser.parse_args().batch)
