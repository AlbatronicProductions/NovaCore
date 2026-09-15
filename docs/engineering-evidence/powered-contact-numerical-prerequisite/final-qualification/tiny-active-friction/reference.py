"""Cold independent active-friction reference; never imports or executes the candidate.

Run: python reference.py <repo> <new-output-directory>
Only stdlib Decimal/Fraction are required. Exact binary inputs, 1100-digit arithmetic.
"""
from decimal import Decimal as D, localcontext
from pathlib import Path
import hashlib, json, math, struct, sys

PRECISION = 1100
RESIDUAL_BAR = D('1e-900')
Z = D(0)

def exact(x):
    n, d = float(x).as_integer_ratio()
    return D(n) / D(d)

def dot(a,b): return sum((x*y for x,y in zip(a,b)),Z)
def norm(a): return dot(a,a).sqrt()
def cross(a,b): return [a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]]
def vec(x): return [float(x[k]) for k in ('X','Y','Z')]

def eliminate(a,b):
    # Full independent pivoted elimination, not the candidate's sequential iterations.
    a=[list(row)+[v] for row,v in zip(a,b)]
    for i in range(len(b)):
        p=max(range(i,len(b)),key=lambda r:abs(a[r][i]))
        assert a[p][i] != 0
        a[i],a[p]=a[p],a[i]
        q=a[i][i]; a[i]=[v/q for v in a[i]]
        for r in range(len(b)):
            if r!=i:
                q=a[r][i]; a[r]=[x-q*y for x,y in zip(a[r],a[i])]
    return [row[-1] for row in a]

def model(data):
    g=data['geometry']
    n,t0,t1=(vec(g[k]) for k in ('Normal','Tangent0','Tangent1'))
    r=[vec(g['Lever'+str(i)]) for i in range(4)]
    # The supplied input boundary is the current FP64-prepared J rows/radii. Reproduce
    # that cold boundary, then import each resulting bit pattern exactly. Do not
    # normalize the FP32-derived frame or silently discard its small normal tilt.
    center=[.25*sum(p[j] for p in r) for j in range(3)]
    lf=[n]*4+[t0,t1,[0.,0.,0.]]
    af=[cross(p,n) for p in r]+[cross(center,t0),cross(center,t1),n]
    radii=[math.sqrt(sum((p[j]-center[j])**2 for j in range(3))) for p in r]
    L=[[exact(v) for v in a] for a in lf]; A=[[exact(v) for v in a] for a in af]
    K=[[D('.125')*dot(L[i],L[j])+D('.5')*dot(A[i],A[j]) for j in range(7)] for i in range(7)]
    source=[exact(v) for k in ('Linear','Angular') for v in vec(data['source'][k])]
    return dict(L=L,A=A,K=K,source=source,r=[exact(v) for v in radii],
                depth=[exact(g['Depth'+str(i)]) for i in range(4)],omega=exact(g['Omega']),
                guard=exact(struct.unpack('<f',struct.pack('<f',1e-16))[0]),
                inputs=dict(linear=lf,angular=af,radii=radii))

def solve(m,force):
    h=D(2)**-1075; omega=m['omega']; k=omega*h*(omega*h+2)
    K=m['K']; J=[dot(m['L'][i],m['source'][:3])+dot(m['A'][i],m['source'][3:]) for i in range(7)]
    accel=[Z,exact(-9.81)+D(force)/8,Z]; ja=[dot(a,accel) for a in m['L']]
    # z=lambda/h avoids an ill-scaled O(2^1075) normal softness coefficient.
    rhs=[m['depth'][i]*omega**2-omega*(omega*h+2)*J[i]-k*ja[i] for i in range(4)]
    def tangent_raw(zn,zw):
        f=[J[i]+h*(ja[i]+dot(K[i][:4],zn)+K[i][6]*zw) for i in (4,5)]
        return eliminate([row[4:6] for row in K[4:6]],[-v for v in f])
    raw=tangent_raw([Z]*4,Z); direction=[v/norm(raw) for v in raw]
    # Determine this branch's own twist sign from its source, not the other branch.
    initial_twist=-J[6]/K[6][6]; sign=D(1) if initial_twist>0 else D(-1)
    for iteration in range(1,7):
        matrix=[[((K[i][i] if i==j else Z)+k*(K[i][j]+
                    (K[i][4]*direction[0]+K[i][5]*direction[1])*D('.125')+
                    K[i][6]*sign*D('.125')*m['r'][j])) for j in range(4)] for i in range(4)]
        zn=eliminate(matrix,rhs); capz=D('.125')*sum(zn); twistcapz=D('.125')*dot(m['r'],zn)
        zw=sign*twistcapz; raw=tangent_raw(zn,zw); nxt=[v/norm(raw) for v in raw]
        direction_error=norm([x-y for x,y in zip(nxt,direction)])
        direction=nxt
        if direction_error < D('1e-1000'): break
    else: raise ArithmeticError('Bounded reference direction solve did not converge')
    z=zn+[capz*v for v in direction]+[zw]; impulses=[h*v for v in z]
    # Endpoint and per-piece increments are kept distinct before projection.
    dv=[h*accel[j]+D('.125')*sum((m['L'][i][j]*impulses[i] for i in range(7)),Z) for j in range(3)]
    dw=[D('.5')*sum((m['A'][i][j]*impulses[i] for i in range(7)),Z) for j in range(3)]
    endpoint=[x+y for x,y in zip(m['source'],dv+dw)]
    result=dict(force=force,z=z,impulses=impulses,increment=dv+dw,endpoint=endpoint,
                iterations=iteration,directionError=direction_error)
    result['check']=check(m,result)
    assert result['check']['pass'], result['check']
    return result

def check(m,s):
    # A second formulation checks the original unscaled equations and final velocity.
    # It does not reuse the reduced normal matrix, rhs, or direction iteration.
    with localcontext() as c:
        c.prec=1200
        h=D(2)**-1075; w=m['omega']; alpha=1/(w*h*(w*h+2)); lam=s['impulses']
        accel=[Z,exact(-9.81)+D(s['force'])/8,Z]
        v=[m['source'][j]+h*accel[j]+D('.125')*sum((m['L'][i][j]*lam[i] for i in range(7)),Z) for j in range(3)]
        av=[m['source'][j+3]+D('.5')*sum((m['A'][i][j]*lam[i] for i in range(7)),Z) for j in range(3)]
        slip=[dot(m['L'][i],v)+dot(m['A'][i],av) for i in range(7)]
        bias=[min(m['depth'][i]/h,m['depth'][i]/(h+2/w),D(2)) for i in range(4)]
        nr=[slip[i]+alpha*m['K'][i][i]*lam[i]-bias[i] for i in range(4)]
        e=eliminate([row[4:6] for row in m['K'][4:6]],slip[4:6])
        raw=[lam[i+4]-e[i] for i in range(2)]; length=norm(raw)
        cap=D('.125')*sum(lam[:4]); twcap=D('.125')*dot(m['r'],lam[:4])
        factor=min(D(1),cap/max(m['guard'],length))
        tr=[lam[i+4]-raw[i]*factor for i in range(2)]
        wr=lam[6]-slip[6]/m['K'][6][6]; projected=max(-twcap,min(twcap,wr))
        boundary=abs(norm(lam[4:6])/cap-1)
        normalized=max(max(abs(x)/max(D(1),abs(b)) for x,b in zip(nr,bias)),
                       norm(tr)/cap,abs(lam[6]-projected)/twcap,boundary)
        # Conditional tangent block KKT (scalar Ktt here); normal-dependent cap
        # is fixed within its block. Never claim a global cone-QP stationarity law.
        tangent_cross=abs(lam[4]*slip[5]-lam[5]*slip[4])/(cap*norm(slip[4:6]))
        kkt=dot(lam[4:6],slip[4:6])<0 and lam[6]*slip[6]<0
        ktt_scalar=m['K'][4][4]==m['K'][5][5] and m['K'][4][5]==0
        normal_positive=all(x>0 for x in lam[:4])
        branch=length>m['guard'] and length>cap and wr < -twcap
        dimensions='N,T: kg*m/s; twist: kg*m^2/s; velocity: m/s; angular: 1/s'
        return dict(pass_=None, **{'pass':normal_positive and branch and normalized<=RESIDUAL_BAR and tangent_cross<=RESIDUAL_BAR and kkt and ktt_scalar},
                    normalActive=[x>0 for x in lam[:4]],tangent='SATURATED / GUARD INACTIVE',twist='NEGATIVE SATURATED',
                    normalResidual=nr,normalizedResidual=normalized,tangentBoundaryRelative=boundary,
                    tangentConditionalKktCross=tangent_cross,conditionalKktOpposingSlip=kkt,kttScalar=ktt_scalar,
                    tangentRaw=raw,tangentRawLength=length,tangentGuard=m['guard'],tangentCap=cap,
                    twistRaw=wr,twistCap=twcap,slip=slip,dimensions=dimensions,
                    finite=all(x.is_finite() for x in lam+v+av),positiveDepthBiasBranch=all(bias[i]==m['depth'][i]/(h+2/w) for i in range(4)))

def scale53(x):
    if not x: return dict(mantissa=0.,exponent=0)
    a=abs(x); e=int(math.floor(float(a.log10())/math.log10(2)))
    while a<D(2)**e: e-=1
    while a>=D(2)**(e+1): e+=1
    mant=float(x/(D(2)**e))
    if abs(mant)==2: mant/=2; e+=1
    return dict(mantissa=mant,exponent=e)

def scaled_value(x): return exact(x['mantissa'])*D(2)**x['exponent']
def float32(x): return struct.unpack('<f',struct.pack('<f',float(x)))[0]
def projection(x):
    s=scale53(x)
    return dict(highPrecision=format(x,'.100E'),double=float(x),doubleHex=float(x).hex(),float=float32(x),scaled53=s)

def response(b,p):
    out={}
    for field in ('endpoint','increment','impulses'):
        rows=[]
        for bv,pv in zip(b[field],p[field]):
            r=projection(pv-bv)
            r.update(differenceOfRoundedDouble=float(pv)-float(bv),
                     differenceOfRoundedFloat=float32(pv)-float32(bv),
                     differenceOfRoundedScaled53=projection(scaled_value(scale53(pv))-scaled_value(scale53(bv))))
            rows.append(r)
        out[field]=rows
    return out

def clean(x):
    if isinstance(x,D): return str(x)
    if isinstance(x,dict): return {k:clean(v) for k,v in x.items()}
    if isinstance(x,(list,tuple)): return [clean(v) for v in x]
    return x

def main():
    repo=Path(sys.argv[1]); out=Path(sys.argv[2]); out.mkdir(parents=True,exist_ok=False)
    path=repo/'docs/engineering-evidence/powered-contact-numerical-prerequisite/final-qualification/results/tiny-event.json'
    data=json.loads(path.read_text())
    with localcontext() as c:
        c.prec=PRECISION
        m=model(data); b=solve(m,0); p=solve(m,16)
        for name,result in [('baseline-reference.json',b),('powered-reference.json',p),('separated-response.json',response(b,p)),
                            ('reference-inputs.json',dict(sourceFile=str(path.relative_to(repo)),sourceSha256=hashlib.sha256(path.read_bytes()).hexdigest().upper(),
                             precision=PRECISION,checkPrecision=1200,residualBar=RESIDUAL_BAR,h='2^-1075',prepared=m['inputs']))]:
            (out/name).write_text(json.dumps(clean(result),indent=2)+'\n',encoding='utf-8')
        print(json.dumps({'baselineCheck':clean(b['check']),'poweredCheck':clean(p['check']),
                          'response':response(b,p)},default=str,indent=2))

if __name__=='__main__': main()
