"""Independent ordered equation reconstruction; consumes saved results, never runs candidate."""
from pathlib import Path
import json,math,sys
root=Path(__file__).parent;d=root/'curve-results-v2'
q=json.loads((d/'fixed-equations.json').read_text());trace=json.loads((d/'first-pass-trace.json').read_text())
K=q['K'];ref=q['reference'];initial=q['initial'];c=q['c'];h=q['h']
def vec(x):return [x[k] for k in ('X','Y','Z')]
def norm(v):return math.sqrt(sum(x*x for x in v))
def dot(a,b):return sum(x*y for x,y in zip(a,b))
L=[vec(x) for x in q['linearRows']];R=[vec(x) for x in q['angularRows']]
source=vec(q['source']['Linear'])+vec(q['source']['Angular'])
gravity=vec(q['load']['Gravity']);force=vec(q['load']['Force']);torque=vec(q['load']['Torque'])
free=[source[i]+h*(gravity[i]+.125*force[i]) for i in range(3)]+[source[i+3]+h*.5*torque[i] for i in range(3)]
fj=[dot(L[i],free[:3])+dot(R[i],free[3:]) for i in range(7)]
def update(x,stage,row=-1,linear=False):
    x=x.copy()
    if stage=='normal-row':
        i=row;x[i]=c*((0 if linear else q['bias'][i]-fj[i])-sum(K[i][j]*x[j] for j in range(7) if j!=i))/K[i][i]
    elif stage=='after-tangent':
        a=(0 if linear else fj[4])+sum(K[4][j]*x[j] for j in range(7) if j not in (4,5))
        b=(0 if linear else fj[5])+sum(K[5][j]*x[j] for j in range(7) if j not in (4,5))
        det=K[4][4]*K[5][5]-K[4][5]*K[5][4]
        x[4]=(-K[5][5]*a+K[4][5]*b)/det;x[5]=(K[5][4]*a-K[4][4]*b)/det
    elif stage=='after-sweep':x[6]=-((0 if linear else fj[6])+sum(K[6][j]*x[j] for j in range(6)))/K[6][6]
    return x
def sweep(x,linear=False):
    for i in range(4):x=update(x,'normal-row',i,linear)
    return update(update(x,'after-tangent',linear=linear),'after-sweep',linear=linear)
def physical(e):
    p=[sum(L[i][j]*e[i] for i in range(7)) for j in range(3)]
    a=[sum(R[i][j]*e[i] for i in range(7)) for j in range(3)]
    return dict(normalSum=sum(e[:4]),linearImpulse=p,angularImpulse=a,linearVelocity=[.125*x for x in p],angularVelocity=[.5*x for x in a],linearNorm=.125*norm(p),angularNorm=.5*norm(a))
assert all(row['nc']==row['tc']==row['wc']==0 for row in trace),'Affine interpretation invalid with active projections'
pred=initial.copy();maxerr=0;injections=[];before=None
for row in trace:
    pred=update(pred,row['stage'],row['row'])
    err=max(abs(a-b) for a,b in zip(pred,row['impulses']));maxerr=max(maxerr,err)
    if row['stage']=='before-tangent':before=row
    if row['stage']=='after-tangent':
        delta=[row['impulses'][i]-before['impulses'][i] for i in (4,5)]
        expected=[K[i][4]*delta[0]+K[i][5]*delta[1] for i in range(4)]
        observed=[row['residual'][i]-before['residual'][i] for i in range(4)]
        injections.append(dict(sweep=row['sweep'],tangentDelta=delta,beforeNormalResidual=before['residual'][:4],afterNormalResidual=row['residual'][:4],predictedResidualChange=expected,observedResidualChange=observed,maxDisagreement=max(abs(a-b) for a,b in zip(expected,observed))))
assert maxerr<=1e-12,'Independent ordered reconstruction failed'
e=[a-b for a,b in zip(initial,ref)];modes={}
for name,basis in [('normal-common',[1,1,1,1]),('normal-X',[-1,1,-1,1]),('normal-Z',[-1,1,1,-1]),('normal-checker',[1,1,-1,-1])]:
    coef=dot(e[:4],basis)/4;modes[name]=[coef*b for b in basis]+[0,0,0]
modes['tangent']=[0]*4+e[4:6]+[0];modes['twist']=[0]*6+[e[6]]
defect=[a-b for a,b in zip(sweep(ref),ref)]
contributions=[];sumchecks=[]
for count in (8,12):
    out={};summed=[0]*7
    for name,mode in modes.items():
        x=mode.copy()
        for _ in range(count):x=sweep(x,True)
        out[name]=dict(impulseError=x,physical=physical(x))
        summed=[a+b for a,b in zip(summed,x)]
    offset=[0]*7
    for _ in range(count):offset=[a+b for a,b in zip(sweep(offset,True),defect)]
    summed=[a+b for a,b in zip(summed,offset)]
    actual=next(row['impulses'] for row in trace if row['stage']=='after-sweep' and row['sweep']==count)
    observed=[a-b for a,b in zip(actual,ref)]
    error=max(abs(a-b) for a,b in zip(summed,observed))
    assert error<=1e-12,'Signed component reconstruction failed'
    contributions.append(dict(sweeps=count,modes=out,referenceFixedPointDefect=defect,propagatedDefect=offset,reconstructedError=summed,observedError=observed,maxDisagreement=error,reconstructedPhysical=physical(summed),observedPhysical=physical(observed)))
curve=json.loads((d/'convergence.json').read_text());dcheck=[]
for a,b in zip(curve['rows'],curve['noD']):
    x=[-q['delta']]*4+[0,0,0]
    for _ in range(a['Sweeps']):x=sweep(x,True)
    observed=[v-u for u,v in zip(a['Impulses'],b['Impulses'])]
    err=max(abs(u-v) for u,v in zip(x,observed));assert err<=1e-12,'D reconstruction failed'
    dcheck.append(dict(sweeps=a['Sweeps'],predictedNoDMinusD=x,observed=observed,maxDisagreement=err))
init=dict(error=e,modes={name:dict(error=x,physical=physical(x)) for name,x in modes.items()},physical=physical(e),
    tangentMagnitude=norm(initial[4:6]),referenceTangentMagnitude=norm(ref[4:6]),twistMagnitude=abs(initial[6]),referenceTwistMagnitude=abs(ref[6]),
    normalErrorNorm=norm(e[:4]),tangentRelativeError=norm(e[4:6])/norm(ref[4:6]),twistRelativeError=abs(e[6])/abs(ref[6]))
result=dict(status='PASS',arithmetic='Independent binary64 ordered equations, with explicit rounded-reference fixed-point defect; tolerance is reconstruction-only, never friction classification',
    reconstructionCeiling=1e-12,maxPerStageDisagreement=maxerr,initialMismatch=init,components=contributions,tangentNormalInjection=injections,DComparison=dcheck)
with (root/'causal-reconstruction.json').open('x') as f:json.dump(result,f,indent=2)
print(json.dumps(dict(status='PASS',maxPerStageDisagreement=maxerr,initial=init,components=contributions),indent=2))
