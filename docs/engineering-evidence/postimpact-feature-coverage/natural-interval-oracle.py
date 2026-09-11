"""Diagnostic high-precision NATURAL interval replay at the fixed failed Coast leaf.

Same alpha enclosure, sigma domain, source polynomial, Earth Taylor model,
Euler/quaternion degree 11, and source rigorous remainder upper bounds. This
isolates ordinary floating outward rounding from dependency widening. No model
parameters or production bounds are changed, and no coverage receipt is issued.
"""
import importlib.util
import json
import sys
from pathlib import Path

spec=importlib.util.spec_from_file_location('physical',Path(__file__).with_name('physical-oracle.py'))
physical=importlib.util.module_from_spec(spec);spec.loader.exec_module(physical)
mp=physical.mp
add,sub,scale,dot,cross,rotate=physical.add,physical.sub,physical.scale,physical.dot,physical.cross,physical.rotate

def run(raw,leaf,dps,taylor_trig=True):
    mp.mp.dps=dps;mp.iv.dps=dps
    iv=mp.iv
    def e(x):
        n,d=float(x).as_integer_ratio()
        return iv.mpf(n)/d
    def v(x):return tuple(e(x[k]) for k in ('X','Y','Z'))
    def b(x):return iv.mpf([e(x['Lower']).a,e(x['Upper']).b])
    def n(x):return iv.sqrt(dot(x,x))
    def bounds(x):return [mp.mpf(y) for y in x._mpi_]
    def trig(angle,cosine):
        if not taylor_trig:return iv.cos(angle) if cosine else iv.sin(angle)
        lo,hi=bounds(angle)
        turns=int(mp.nint((lo+hi)/(4*mp.pi)))
        x=angle-2*turns*iv.pi
        term=iv.mpf(1) if cosine else x;total=term;square=x**2
        for k in range(1,25):
            order=2*k-1 if cosine else 2*k
            term=-term*square/(order*(order+1));total+=term
        magnitude=max(abs(z) for z in bounds(x))
        order=49 if cosine else 50
        remainder=iv.mpf(magnitude)**order
        for k in range(2,order+1):remainder/=k
        return total+iv.mpf([-remainder.b,remainder.b])
    def summary(x):
        lo,hi=bounds(x)
        return dict(lower=mp.nstr(lo,45),upper=mp.nstr(hi,45),width=mp.nstr(hi-lo,45))
    s,a,r=raw['FrozenSourceTranslation'],raw['FrozenSourceRotation'],raw['region']
    alpha=b(raw['PoseRootEnclosure']);sigma=b(leaf['sigma'])
    target=e(raw['SourceEnd']['Ticks'])/1000000
    elapsed=sigma*(target-alpha);time=alpha+elapsed
    dt=time-e(s['Epoch']['Ticks'])/1000000
    acc=scale(v(s['ConstantForceRoot']),1/e(raw['Properties']['MassKilograms']))
    pos,vel,plus=v(s['PositionRoot']),v(s['VelocityRoot']),v(raw['InitialLinearVelocityRoot'])
    er,ev=v(raw['seed']['Position']),v(raw['seed']['Velocity'])
    mu=e(raw['GravitationalParameter']);radius=n(er)
    ea=scale(er,-mu/radius**3)
    ej=scale(sub(ev,scale(er,3*dot(er,ev)/radius**2)),-mu/radius**3)
    relative=sub(sub(pos,er),scale(vel,e(s['Epoch']['Ticks'])/1000000))
    relative=add(relative,scale(sub(vel,ev),time))
    relative=add(relative,sub(scale(acc,dt**2/2),scale(ea,time**2/2)))
    relative=sub(relative,scale(ej,time**3/6))
    relative=add(relative,scale(sub(plus,add(vel,scale(acc,dt))),elapsed))
    relative=add(relative,scale(acc,elapsed**2))
    # Retain the ORIGINAL binary64 rigorous upper remainder; high precision does
    # not replace the physical Earth remainder with zero.
    earth_error=e(leaf['components']['earthRemainder'])
    relative=tuple(z+iv.mpf([-earth_error.b,earth_error.b]) for z in relative)
    q=tuple(e(a['OrientationLocalToParent'][k]) for k in ('X','Y','Z','W'))
    q=scale(q,1/n(q));w=v(raw['InitialAngularVelocityBody']);inertia=v(a['PrincipalInertia'])
    wc,qc=[w],[q]
    c=tuple((inertia[(i+1)%3]-inertia[(i+2)%3])/inertia[i] for i in range(3))
    for order in range(11):
        wn=tuple(c[i]*sum(wc[k][(i+1)%3]*wc[order-k][(i+2)%3] for k in range(order+1))/(order+1) for i in range(3))
        qn=(iv.mpf(0),)*4
        for k in range(order+1):qn=add(qn,physical.qproduct(qc[k],wc[order-k]))
        wc.append(wn);qc.append(scale(qn,iv.mpf(1)/(2*(order+1))))
    q=physical.horner(qc,elapsed)
    qr=e(leaf['trajectory']['Rotation']['AttitudeRemainder'])
    q=tuple(z+iv.mpf([-qr.b,qr.b]) for z in q)
    relative=add(relative,rotate(q,v(raw['feature'])))
    o={k:e(value) for k,value in raw['orientation'].items()}
    day,century=o['SecondsPerDay'],o['SecondsPerDay']*o['DaysPerCentury']
    rad=iv.pi/180
    angles=((-(o['Ra0']+o['RaT']*time/century+90)*rad,False),
            (-(90-o['Dec0']-o['DecT']*time/century)*rad,True),
            (-(o['W0']+o['Wd']*time/day)*rad,False))
    body=relative
    for angle,xaxis in angles:
        cs,sn=trig(angle,True),trig(angle,False);x,y,z=body
        body=(x,cs*y-sn*z,sn*y+cs*z) if xaxis else (cs*x-sn*y,sn*x+cs*y,z)
    body=(body[0],body[2],-body[1])
    gap=dot(v(r['Up']),body)-e(r['RadiusMetres'])-e(r['PlaneAltitudeMetres'])
    return dict(precision=dps,trig='SAME_24_TERM_NATURAL_INTERVAL_TAYLOR' if taylor_trig else 'DIRECT_HIGH_PRECISION_INTERVAL_TRIG',alpha=summary(alpha),sigma=summary(sigma),elapsed=summary(elapsed),time=summary(time),
                relative=[summary(z) for z in relative],body=[summary(z) for z in body],gap=summary(gap),
                sourceBinary64Gap=leaf['gap']['Value'],physicalEarthRemainderPreserved=leaf['components']['earthRemainder'],
                physicalRotationRemainderPreserved=leaf['trajectory']['Rotation']['AttitudeRemainder'])

if __name__=='__main__':
    directory=Path(sys.argv[1]);raw=json.loads((directory/'coast-input.json').read_text(encoding='utf-8-sig'))
    trace=json.loads((directory/'coast-intervals.json').read_text(encoding='utf-8-sig'))
    result=[run(raw,trace['selected'][-1],dps,taylor) for taylor in (True,False) for dps in (60,90)]
    Path(sys.argv[2]).write_text(json.dumps(result,indent=2)+'\n')
