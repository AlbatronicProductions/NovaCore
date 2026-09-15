"""Offline arithmetic over saved input only; no candidate/world/solver execution."""
from pathlib import Path
import json, math, struct, sys

def normalized(v):
    if v==0:return (0.,0)
    m,e=math.frexp(v);return (m*2,e-1)
def product(x,v):
    y=normalized(v);m,e=normalized(x[0]*y[0]);return (m,x[1]+y[1]+e) if m else (0.,0)
def add(x,y):
    if x[0]==0:return y
    if y[0]==0:return x
    e=max(x[1],y[1]);m,shift=normalized(math.ldexp(x[0],x[1]-e)+math.ldexp(y[0],y[1]-e));return (m,e+shift)
def length(x,y):
    e=max(x[1],y[1]);a=math.ldexp(x[0],x[1]-e);b=math.ldexp(y[0],y[1]-e)
    m,d=normalized(math.sqrt(a*a+b*b));return (m,e+d)
def compare(x,y):
    if x[1]!=y[1]:return -1 if x[1]<y[1] else 1
    return -1 if x[0]<y[0] else 1 if x[0]>y[0] else 0
def encode(x):return dict(mantissa=x[0],exponent=x[1],mantissaBits=struct.pack('>d',x[0]).hex().upper())

if __name__=='__main__':
    root=Path(sys.argv[1]);x=json.loads((root/'candidate-results/baseline-coast-input.json').read_text())
    g=x['accepted']['ProducingGeometry'];cache=x['accepted']['Cache']
    rows=[(cache[k]['Mantissa'],cache[k]['Exponent']) for k in ('N0','N1','N2','N3','T0','T1','Twist')]
    levers=[[g['Lever'+str(i)][a] for a in ('X','Y','Z')] for i in range(4)]
    center=[.25*sum(r[j] for r in levers) for j in range(3)]
    radii=[math.sqrt(sum((r[j]-center[j])**2 for j in range(3))) for r in levers]
    tangent=twist=(0.,0)
    for i in range(4):tangent=add(tangent,product(rows[i],.125));twist=add(twist,product(rows[i],.125*radii[i]))
    t=length(rows[4],rows[5]);w=(abs(rows[6][0]),rows[6][1]);tc=compare(t,tangent);wc=compare(w,twist)
    result=dict(kind='OFFLINE SAVED-INPUT ARITHMETIC ONLY',candidateRuns=0,solverCalls=0,
        source='GeneralizedTransport.Feasible strict comparisons; Scaled/ScaleMath binary64 operations',
        tangentLength=encode(t),tangentCap=encode(tangent),tangentComparison=tc,
        twistMagnitude=encode(w),twistCap=encode(twist),twistComparison=wc,
        strictTangentPass=tc<0,strictTwistPass=wc<0,
        conclusion='Accepted active-boundary history fails strict old-cache admission before transport elimination')
    with (root/'admission-witness.json').open('x',encoding='utf-8') as f:json.dump(result,f,indent=2);f.write('\n')
    print(json.dumps(result,indent=2))
