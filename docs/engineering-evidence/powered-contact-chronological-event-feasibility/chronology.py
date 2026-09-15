"""Standalone evidence only. Python -B; no third-party modules or backend steps."""
from fractions import Fraction as Q
from decimal import Decimal as D, localcontext
import hashlib
import json
import math
from pathlib import Path

ROOT = Path(__file__).resolve().parent
H, DRY, G, I = Q(1, 60), Q(8), Q(981, 100), Q(2)
A, B, DEPTH = Q(1), Q(1, 2), Q(1, 2)
RADIUS = Q.from_float(math.sqrt(1.25))
NTERMS = 16


def dec(x):
    return D(x.numerator) / D(x.denominator) if isinstance(x, Q) else D(x)


def fmt(x):
    return format(dec(x), '.24E')


def case(name, force, dx, dy, e, fraction, slide, twist, tiny=False):
    force, dx, dy, e = map(Q, (force, dx, dy, e))
    q = Q(2) if tiny else Q.from_float(float(force) / 5120.0)
    fuel = (Q(1, 2**1074) if fraction else Q(0)) if tiny else Q.from_float(float(q) * float(H * fraction))
    return dict(name=name, force=force, fx=force*dx, fy=force*dy, e=e,
                q=q, fuel=fuel, h=fuel/q, m0=DRY+fuel,
                k=Q(1, 8) if slide else Q(0), c=RADIUS/8 if twist else Q(0),
                v0=Q(1) if slide else Q(0), w0=Q(3, 25) if twist else Q(0))


CASES = [
    case('ordinary-centered', 32, 0, 1, 0, Q(1,2), False, False),
    case('friction', 40, Q(3,5), Q(4,5), 0, Q(1,2), True, False),
    case('off-com', 80, Q(3,5), Q(4,5), Q(1,10), Q(1,2), True, True),
    case('tiny-positive', 16, Q(3,5), Q(4,5), Q(1,10), 1, True, True, True),
    case('tiny-zero', 16, Q(3,5), Q(4,5), Q(1,10), 0, True, True, True),
    case('low-thrust', 8, 0, 1, 0, Q(1,2), False, False),
    case('near-unloading', 80, Q(7,25), Q(24,25), 0, Q(1,16), True, False),
    case('tipping-relevant', 80, Q(3,5), Q(4,5), Q(1,10), Q(1,2), True, True),
    case('late-exhaustion', 8, Q(3,5), Q(4,5), 0, Q(99,100), True, False),
]


# Reference-only polynomial machinery. Candidate never calls these routines.
def integ(p, constant=Q(0)):
    return [constant] + [v/Q(i+1) for i,v in enumerate(p)]


def val(p, t):
    total = Q(0)
    for c in reversed(p):
        total = total*t+c
    return total


def product(p, r):
    out = [Q(0)]*(len(p)+len(r)-1)
    for i,x in enumerate(p):
        for j,y in enumerate(r):
            out[i+j] += x*y
    return out


def ref(s):
    h, q, m, k, c = (s[x] for x in ('h','q','m0','k','c'))
    f = s['fx'] + k*s['fy']
    acceleration = [f/m*(q/m)**n for n in range(NTERMS+1)]
    acceleration[0] -= k*G
    vp = integ(acceleration, s['v0'])
    np = [G*m-s['fy'], -G*q]
    wp = integ([(s['e']*s['fx']-c*np[0])/I, -c*np[1]/I], s['w0'])
    hc = H-h
    vh, wh = val(vp,h), val(wp,h)
    vc, wc = [vh,-k*G], [wh,-c*G*DRY/I]
    jn = val(integ(np),h)+G*DRY*hc
    x = val(integ(vp),h)+val(integ(vc),hc)
    theta = val(integ(wp),h)+val(integ(wc),hc)
    wt = -k*(val(integ(product(np,vp)),h)+G*DRY*val(integ(vc),hc))
    ww = -c*(val(integ(product(np,wp)),h)+G*DRY*val(integ(wc),hc))
    delta=q*h/m
    tail = abs(f)*h/m*delta**(NTERMS+1)/(Q(NTERMS+2)*(1-delta))
    bounds=dict(vx=tail,x=H*tail,work_t=k*G*m*H*tail)
    out = dict(vx=val(vc,hc), x=x, wy=val(wc,hc), theta=theta, jn=jn,
               jt=-k*jn,jtw=-c*jn,work_t=wt,work_twist=ww,vy=Q(0),y=Q(0),
               wx=Q(0),mass=DRY,fuel=Q(0),support_mx=s['e']*s['fy']*h,
               support_mz=DEPTH*k*jn)
    # Convex powered v(t) and affine powered angular acceleration: endpoints
    # plus their only possible stationary points suffice for full admission.
    vt=[Q(0),h]; wt_points=[Q(0),h]
    if k and h:
        t=(m-f/(k*G))/q
        if 0<t<h: vt.append(t)
    if c and h:
        t=(c*np[0]-s['e']*s['fx'])/(c*G*q)
        if 0<t<h: wt_points.append(t)
    vmin=min([val(vp,t) for t in vt]+[val(vc,hc)])
    wmin=min([val(wp,t) for t in wt_points]+[val(wc,hc)])
    reactions=[]
    for powered,mass in [(True,m),(True,DRY),(False,DRY)] if h else [(False,DRY)]:
        n=G*mass-(s['fy'] if powered else 0)
        mx=s['e']*s['fy'] if powered else Q(0)
        mz=DEPTH*k*n
        reactions += [n/4-sz*mx/(4*B)+sx*mz/(4*A) for sx in (-1,1) for sz in (-1,1)]
    assert 0<=h<H and m>=DRY and min(reactions)>0, 'rank/support admission failed'
    assert not k or vmin>tail, 'sliding regime failed'
    assert not c or wmin>0, 'twist regime failed'
    admission = dict(rank=3,condition_min=Q(4),condition_max=m/2,
                     min_pad_normal=min(reactions),min_vx=vmin,min_wy=wmin,
                     reference_tail_max=max(bounds.values()),h=h)
    return out, bounds, admission


def candidate(s):
    # Full-H temporal moments: no intermediate body endpoint or solver call.
    h,q,m,k,c,fx,fy,e,v0,w0=map(dec,(s[x] for x in ('h','q','m0','k','c','fx','fy','e','v0','w0')))
    Hh,g,mf,ii=map(dec,(H,G,DRY,I)); hc=Hh-h
    L=(m/mf).ln(); f=fx+k*fy
    jn=g*(m*h-q*h*h/2+mf*hc)-fy*h
    vx=v0-k*g*Hh+f/q*L
    x=v0*Hh-k*g*Hh*Hh/2+f/q*((Hh-m/q)*L+h)
    mass_moment=m*(Hh*h-h*h/2)-q*(Hh*h*h/2-h*h*h/3)+mf*hc*hc/2
    wy=w0+(e*fx*h-c*jn)/ii
    theta=w0*Hh+((e*fx+c*fy)*(Hh*h-h*h/2)-c*g*mass_moment)/ii
    ln0=h-mf/q*L
    lnm=(m*m-mf*mf-2*mf*mf*L)/(4*q)
    jnp=g*(m*h-q*h*h/2)-fy*h
    vh=v0-k*g*h+f/q*L
    npv=v0*jnp-k*g*(g*(m*h*h/2-q*h*h*h/3)-fy*h*h/2)+f/q*(g*lnm-fy*ln0)
    work_t=-k*(npv+g*mf*(vh*hc-k*g*hc*hc/2))
    aa=g*m-fy; bb=g*q
    c1=(e*fx-c*aa)/ii; c2=c*bb/(2*ii)
    npw=aa*w0*h+(aa*c1-bb*w0)*h*h/2+(aa*c2-bb*c1)*h*h*h/3-bb*c2*h**4/4
    wh=w0+c1*h+c2*h*h
    work_twist=-c*(npw+g*mf*(wh*hc-c*g*mf*hc*hc/(2*ii)))
    return dict(vx=vx,x=x,wy=wy,theta=theta,jn=jn,jt=-k*jn,jtw=-c*jn,
                work_t=work_t,work_twist=work_twist,vy=D(0),y=D(0),wx=D(0),
                mass=mf,fuel=D(0),support_mx=e*fy*h,support_mz=dec(DEPTH)*k*jn)


def control(s, kind):
    m,k,c=s['m0'],s['k'],s['c']
    duty = Q(1) if kind=='ksa_like' and s['fuel'] else Q(0)
    if kind=='average': duty=s['h']/H
    fx,fy=s['fx']*duty,s['fy']*duty
    n=G*m-fy; tau=s['e']*fx
    ax=(fx-k*n)/m; aw=(tau-c*n)/I
    out=dict(vx=s['v0']+ax*H,x=s['v0']*H+ax*H*H/2,
             wy=s['w0']+aw*H,theta=s['w0']*H+aw*H*H/2,
             jn=n*H,jt=-k*n*H,jtw=-c*n*H,
             work_t=-k*n*(s['v0']*H+ax*H*H/2),
             work_twist=-c*n*(s['w0']*H+aw*H*H/2),
             vy=Q(0),y=Q(0),wx=Q(0),mass=DRY,fuel=Q(0),
             support_mx=s['e']*fy*H,support_mz=DEPTH*k*n*H)
    minimum=n/4-abs(s['e']*fy)/(4*B)-DEPTH*k*n/(4*A)
    assert minimum>0 and (not k or out['vx']>0) and (not c or out['wy']>0), 'control regime invalid'
    if kind=='post_impulse':
        out['vx']+=s['fx']*s['h']/DRY
        out['vy']+=s['fy']*s['h']/DRY
        out['wy']+=s['e']*s['fx']*s['h']/I
        out['wx']-=s['e']*s['fy']*s['h']/I
    return out


def thresholds():
    radius=dec(Q(3,2)).sqrt()
    ang=D('.001')/(radius*dec(H))
    return dict(vx=D('.06'),vy=D('.06'),wx=ang,wy=ang,x=D('.001'),y=D('.001'),
                theta=D('.001')/radius,jn=D('.48'),jt=D('.48'),jtw=2*ang,
                support_mx=2*ang,support_mz=2*ang,work_t=D('.07848'),work_twist=D('.07848'))


def comparison(other, reference):
    differences={k:abs(dec(other[k])-dec(v)) for k,v in reference.items()}
    exceeded=[k for k,bar in thresholds().items() if differences[k]>=bar]
    result='MATERIAL PHYSICAL DIFFERENCE' if exceeded else ('NEGLIGIBLE' if max(differences.values())<=D('1e-12') else 'MEASURABLE BUT NON-MATERIAL')
    # Nonzero normal/roll endpoint violates a rigid supported regime; the
    # 1e-12 guard prevents an infinitesimal projection from winning materiality.
    if abs(dec(other['vy']))>D('1e-12') or abs(dec(other['wx']))>D('1e-12'):
        result='REGIME-CHANGING DIFFERENCE'
    return dict(classification=result,exceeded=exceeded,absolute_differences={k:fmt(v) for k,v in differences.items()})


def execute():
    results=[]
    for s in CASES:
        reference,bounds,admission=ref(s)  # Admission precedes Candidate C.
        assert max(bounds.values())<Q(1,10**45), 'reference tail too large'
        got=candidate(s)
        errors={k:abs(v-dec(reference[k])) for k,v in got.items()}
        assert max(errors.values())<=D('1e-40'), ('candidate mismatch',s['name'],{k:fmt(v) for k,v in errors.items()})
        assert s['q']*s['h']==s['fuel'], 'inexact debit'
        assert (s['fuel']*2**1074).denominator==1, 'resource is not exact binary64 resource units'
        controls={}
        for kind in ('ksa_like','average','post_impulse'):
            value=control(s,kind)
            controls[kind]=dict(endpoint={k:fmt(v) for k,v in value.items()},**comparison(value,reference))
        row=dict(name=s['name'],input={k:str(v) for k,v in s.items()},
                 admission={k:fmt(v) if isinstance(v,Q) else v for k,v in admission.items()},
                 ledger=dict(positive_event=s['h']>0,h_exact=str(s['h']),fuel_exact=str(s['fuel']),
                             fuel_units=str(s['fuel']*2**1074),debit_count=1 if s['h'] else 0,
                             event_order='start < empty < target' if s['h'] else 'empty at start < target',
                             final_fuel='0',actual_engine_at_target='OFF',
                             ksa_like_unsupplied_demand=fmt(max(Q(0),s['q']*H-s['fuel']) if s['fuel'] else Q(0))),
                 candidate={k:fmt(v) for k,v in got.items()},reference={k:fmt(v) for k,v in reference.items()},
                 candidate_max_absolute_error=fmt(max(errors.values())),
                 candidate_binary64={k:float(v).hex() for k,v in got.items()},
                 candidate_event_backend_calls=0,candidate_total_backend_calls=0,
                 controls=controls,
                 frozen_wrench_velocity_error_bound=fmt(dec(s['force']*H)*abs(dec(reference['theta']))/dec(DRY)))
        results.append(row)
        print(s['name'], 'PASS', 'max-error',fmt(max(errors.values())),
              'min-pad',fmt(admission['min_pad_normal']),
              'KSA',controls['ksa_like']['classification'],
              'average',controls['average']['classification'])
    groups={'ordinary-event.json':results[:1], 'friction-work.json':results[1:2],
            'off-com.json':results[2:3], 'tiny-event.json':results[3:5],
            'realistic-payoff.json':results[5:]}
    hashes={p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest().upper() for p in
            ('model-contract.md','rank-conditioning.md','chronology.py')}
    for filename,rows in groups.items():
        data=dict(preregistered_inputs_sha256=hashes,decimal_precision=700,reference_series_degree=NTERMS,
                  materiality_bars={k:fmt(v) for k,v in thresholds().items()},results=rows)
        (ROOT/filename).write_text(json.dumps(data,indent=2)+'\n',encoding='utf-8')


if __name__=='__main__':
    with localcontext() as ctx:
        ctx.prec=700
        execute()
