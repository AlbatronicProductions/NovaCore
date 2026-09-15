"""Read accepted oracle/input source; emit compact frozen input and reference only."""
from pathlib import Path
from decimal import localcontext
import runpy, json, hashlib

ROOT=Path(__file__).resolve().parent
PRIOR=ROOT.parent/'powered-contact-chronological-event-feasibility'
ns=runpy.run_path(str(PRIOR/'chronology.py'))
Q=ns['Q']; H=ns['H']; G=ns['G']; I=ns['I']; fmt=ns['fmt']

with localcontext() as ctx:
    ctx.prec=700
    selected=[('centered-baseline',5,True),('low-thrust',5,False),
              ('sliding-baseline',6,True),('near-unloading',6,False),
              ('yaw-baseline',7,True),('off-com',7,False),
              ('near-end',8,False),('mid-event',1,False),
              ('tiny-event',3,False),('zero-event',4,False)]
    rows=[]
    for name,index,baseline in selected:
        s=ns['CASES'][index]
        if baseline:
            m,k,c=s['m0'],s['k'],s['c']; v,w=s['v0'],s['w0']
            x=v*H-k*G*H*H/2; theta=w*H-c*G*m/I*H*H/2
            oracle=dict(vx=v-k*G*H,vy=Q(0),wx=Q(0),wy=w-c*G*m/I*H,
                        x=x,y=Q(0),theta=theta,jn=G*m*H,jt=-k*G*m*H,
                        jtw=-c*G*m*H,work_t=-k*G*m*x,work_twist=-c*G*m*theta,
                        support_mx=Q(0),support_mz=Q(1,2)*k*G*m*H,mass=m,fuel=s['fuel'])
        else:
            oracle,bounds,admission=ns['ref'](s)
            assert max(bounds.values())<Q(1,10**45)
        # Preserve mass in no-engine baseline; command has no burn or event.
        inputs={k:str(v) for k,v in s.items()}
        if baseline:
            inputs.update(fx='0',fy='0',e='0',h='0')
        inputs.update(name=name,H=str(H),baseline=baseline)
        rows.append(dict(input=inputs,reference={k:fmt(v) for k,v in oracle.items()}))
    out=dict(plan_sha256=hashlib.sha256((ROOT/'plan.md').read_bytes()).hexdigest().upper(),
             prior_source_sha256=hashlib.sha256((PRIOR/'chronology.py').read_bytes()).hexdigest().upper(),
             cases=rows)
    (ROOT/'inputs.json').write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8')
    print('Prepared10frozen inputs, accepted oracle read only; no BEPU runs')
