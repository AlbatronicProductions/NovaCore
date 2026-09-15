"""Arithmetic-only recovery after the payoff process refused before flushing results.
No candidate solver, world, or installation call. Reference is used only AFTER D construction.
"""
import json, math, struct, sys
from pathlib import Path

def v(x): return [x[k] for k in ('X','Y','Z')]
def dot(a,b): return sum(x*y for x,y in zip(a,b))
def add(a,b): return [x+y for x,y in zip(a,b)]
def sub(a,b): return [x-y for x,y in zip(a,b)]
def mul(s,a): return [s*x for x in a]
def norm(a): return math.sqrt(dot(a,a))
def cross(a,b): return [a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]]
def f32(x): return struct.unpack('<f',struct.pack('<f',x))[0]
def residual(a,b,x): return [dot(row,x)-rhs for row,rhs in zip(a,b)]
def modes(x):
    return [sum(x[:4])/4,(-x[0]+x[1]-x[2]+x[3])/4,(-x[0]+x[1]+x[2]-x[3])/4,(x[0]+x[1]-x[2]-x[3])/4,*x[4:]]

def geometry(basis,mass):
    n,t1,t2=v(basis['normal']),v(basis['tangent1']),v(basis['tangent2'])
    r=[v(x) for x in basis['offsets']]; c=mul(.25,[sum(x[k] for x in r) for k in range(3)])
    linear=[n,n,n,n,t1,t2,[0.,0.,0.]]
    angular=[*[cross(x,n) for x in r],cross(c,t1),cross(c,t2),n]
    k=[[dot(x,y)/mass+.5*dot(angular[i],angular[j]) for j,y in enumerate(linear)] for i,x in enumerate(linear)]
    return linear,angular,k,[norm(sub(x,c)) for x in r]

def equation(linear,angular,k,depth,free,omega,h):
    alpha=1/(omega*h*(omega*h+2)); a=[row[:] for row in k]
    rhs=[-dot(x,free[0])-dot(angular[i],free[1]) for i,x in enumerate(linear)]
    for i in range(4):
        a[i][i]+=alpha*k[i][i]
        rhs[i]+=min(depth[i]/h,depth[i]/(h+2/omega),2)
    return a,rhs

def error_step(a,e,reference,radii):
    x=e[:]; branch=True
    for i in range(4):
        x[i]=-sum(a[i][j]*x[j] for j in range(7) if j!=i)/a[i][i]
        branch &= x[i]+reference[i]>0
    b4=-sum(a[4][j]*x[j] for j in range(7) if j not in (4,5))
    b5=-sum(a[5][j]*x[j] for j in range(7) if j not in (4,5))
    det=a[4][4]*a[5][5]-a[4][5]*a[5][4]
    x[4]=(a[5][5]*b4-a[4][5]*b5)/det
    x[5]=(a[4][4]*b5-a[5][4]*b4)/det
    candidate=add(x,reference)
    branch &= norm(candidate[4:6]) < .125*sum(candidate[:4])
    x[6]=-sum(a[6][j]*x[j] for j in range(6))/a[6][6]
    branch &= abs(x[6]+reference[6]) < .125*sum(candidate[i]*radii[i] for i in range(4))
    return x,bool(branch)

def mapped(linear,angular,error,mass):
    p=[sum(linear[i][k]*error[i] for i in range(7)) for k in range(3)]
    l=[sum(angular[i][k]*error[i] for i in range(7)) for k in range(3)]
    return mul(1/mass,p),mul(.5,l)

def audit(name,a,rhs,initial,reference,linear,angular,radii,mass,measured):
    # Derive solely from current A/rhs/initial; do not reference target solution here.
    r=residual(a,rhs,initial); numerator=sum(r[:4]); denominator=sum(sum(row[:4]) for row in a[:4])
    delta=-numerator/denominator
    diagnostic=[x+(delta if i<4 else 0) for i,x in enumerate(initial)]
    cap=.125*sum(diagnostic[:4]); t=norm(diagnostic[4:6]); twistcap=.125*sum(diagnostic[i]*radii[i] for i in range(4))
    feasible=min(diagnostic[:4])>0 and t<cap and abs(diagnostic[6])<twistcap
    out=dict(name=name,kind='ARITHMETIC RECONSTRUCTION ONLY',delta=delta,numerator=numerator,denominator=denominator,
             diagnostic=diagnostic,tangent=t,tangentCap=cap,twist=abs(diagnostic[6]),twistCap=twistcap,
             feasible=feasible,refusal=None if feasible else 'CurrentFrictionCap',initialModes=modes(sub(initial,reference)))
    # Independently check this equation map against the already saved candidate baseline first.
    e=sub(initial,reference); branch=True
    for _ in range(8): e,ok=error_step(a,e,reference,radii); branch &= ok
    proof=max(abs(x-y) for x,y in zip(e,sub(measured,reference)))
    assert proof<=1e-12 and branch,(name,'baseline reconstruction',proof,branch)
    out['baselineMapError']=proof
    if feasible:
        e=sub(diagnostic,reference); stage=True
        for _ in range(8): e,ok=error_step(a,e,reference,radii); stage &= ok
        lv,av=mapped(linear,angular,e,mass)
        errors=[abs(sum(e[:4])),norm(lv),norm(av)]
        out.update(predictedSignedError=e,predictedLinearErrorVector=lv,predictedAngularErrorVector=av,
                   predictedErrors=errors,unclampedStages=stage,predictedPass=stage and all(x<=1e-4 for x in errors),
                   commonResidualAfter=sum(residual(a,rhs,diagnostic)[:4]))
    return out

if __name__=='__main__':
    convergence=json.loads(Path(sys.argv[1]).read_text())
    synthetic=json.loads(Path(sys.argv[2]).read_text())
    item=convergence['coast']; inp=item['inputs']; omega=inp['omega']; h=inp['h']; mass=inp['Mass']
    linear,angular,k,radii=geometry(inp,mass)
    coast=audit('original-coast',item['a'],item['rhs'],inp['guess'],item['reference']['Impulses'],linear,angular,radii,mass,item['result']['Impulses'])
    results=[]
    for s in synthetic['cases']:
        if s['transport']['Status']!='Ready': continue
        linear,angular,k,radii=geometry(s['newBasis'],8)
        free=(mul(-9.81*h,linear[0]),[0.,0.,0.])
        a,rhs=equation(linear,angular,k,[f32(.0004)]*4,free,omega,h)
        results.append(audit(s['Name'],a,rhs,s['transport']['Cache'],s['reference']['Impulses'],linear,angular,radii,8,s['eightSweep']['solved']['Impulses']))
    Path(sys.argv[3]).write_text(json.dumps(dict(coast=coast,synthetics=results,candidateSolverCalls=0,
        note='Independent homogeneous error propagation, not recovered measurements from the aborted payoff process.'),indent=2)+'\n')
    print(json.dumps(dict(coast=coast['predictedErrors'],synthetics=[(x['name'],x['feasible'],x.get('predictedErrors')) for x in results])))
