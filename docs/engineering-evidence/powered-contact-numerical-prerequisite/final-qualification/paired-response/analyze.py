"""Read retained outputs; exact-rational differences, independent binary53 rounding.
No solver execution. Create-only summary. python -B analyze.py <fresh-json-output>
"""
from pathlib import Path
from fractions import Fraction as F
from decimal import Decimal as D, localcontext
import json, math, struct, sys
here=Path(__file__).resolve().parent
def read(p): return json.loads(p.read_text(encoding='utf-8-sig'))
trace=read(here/'diagnostics/stage-delta-inputs.json')
refs=read(here.parent/'tiny-active-friction/coast-reference.json')
def value(x): return F(x['Mantissa'])*F(2)**x['Exponent']
def scaled(x): return (x['Mantissa'],x['Exponent'])
def frac(x): return F(x[0])*F(2)**x[1]
def norm(x):
    if x==0:return (0.,0)
    m,e=math.frexp(x);return(m*2,e-1)
def mul(a,b):
    if a[0]==0 or b[0]==0:return (0.,0)
    m,e=norm(a[0]*b[0]);return(m,a[1]+b[1]+e)
def add(a,b):
    if a[0]==0:return b
    if b[0]==0:return a
    e=max(a[1],b[1]);m,z=norm(math.ldexp(a[0],a[1]-e)+math.ldexp(b[0],b[1]-e));return (m,e+z) if m else (0.,0)
def scientific(f):
    with localcontext() as c:
        c.prec=20;return str(D(f.numerator)/D(f.denominator))
def exact(f):return {'fraction':str(f),'decimal':scientific(f)}
def vec(x):return [x[c] for c in ('X','Y','Z')]
def increment(eq,cache):
    h=(eq['durationSignificand'],eq['durationExponent']);body=eq['body'];load=eq['load'];g=vec(load['Gravity']);f=vec(load['Force']);t=vec(load['Torque'])
    diag=vec(body['Diagonal']);off=vec(body['OffDiagonal'])
    def apply(v):return [diag[0]*v[0]+off[0]*v[1]+off[1]*v[2],off[0]*v[0]+diag[1]*v[1]+off[2]*v[2],off[1]*v[0]+off[2]*v[1]+diag[2]*v[2]]
    acc=[g[i]+body['Mass']*f[i] for i in range(3)];kick=acc+apply(t)
    result=[mul(h,norm(x)) for x in kick]
    for i,x in enumerate(cache):
        lin=vec(eq['linear'][i]);ang=apply(vec(eq['angular'][i]));mass=mul(x,norm(body['Mass']))
        for j in range(3):result[j]=add(result[j],mul(mass,norm(lin[j])));result[j+3]=add(result[j+3],mul(x,norm(ang[j])))
    return result
def round53(f):
    if not f:return F(0)
    sign=-1 if f<0 else 1;f=abs(f);e=f.numerator.bit_length()-f.denominator.bit_length()
    if f<F(2)**e:e-=1
    q=f/(F(2)**(e-52));n,r=divmod(q.numerator,q.denominator)
    if 2*r>q.denominator or (2*r==q.denominator and n%2):n+=1
    return sign*F(n)*F(2)**(e-52)
names=['N0','N1','N2','N3','T0','T1','Twist','dVx','dVy','dVz','dWx','dWy','dWz']
reference=[];ulps=[]
for field in ('impulses','increment'):
    for i,(b,p) in enumerate(zip(refs['baseline']['installedSourceReference'][field],refs['powered']['installedSourceReference'][field])):
        b=F(b);p=F(p);br=round53(b);pr=round53(p);assert br==pr and b==p
        ub=F(refs['baseline']['unroundedSourceReference'][field][i]);up=F(refs['powered']['unroundedSourceReference'][field][i]);assert round53(ub)==round53(up)
        magnitude=max(abs(br),abs(pr));e=magnitude.numerator.bit_length()-magnitude.denominator.bit_length()
        if magnitude<F(2)**e:e-=1
        ulp=F(2)**(e-52);ulps.append(ulp)
        reference.append({'field':field,'component':i,'installedReferenceExactlyEqual':True,'roundedAbsoluteDifference':str(pr-br),
            'commonBinary53Hex':float(br).hex(),'unroundedSourceDifference':scientific(up-ub),'roundDifferenceNotDifferenceOfRounds':scientific(round53(up-ub)),
            'referenceSizedUlp':exact(ulp)})
stages=[]
b=trace['baseline'];p=trace['powered'];assert b['Equations']==p['Equations']
for bs,ps in zip(b['Stages'],p['Stages']):
    bc=list(map(scaled,bs['values']));pc=list(map(scaled,ps['values']));bi=increment(b['Equations'],bc);pi=increment(p['Equations'],pc)
    diff=[frac(y)-frac(x) for x,y in zip(bc+bi,pc+pi)]
    stages.append({'stage':bs['stage'],'deltas':{n:exact(x) for n,x in zip(names,diff)},
        'note':'increments before S_FINAL are diagnostic exports of the saved stage, not extra candidate operations'})
    if bs['stage']=='S_FINAL':
        assert [frac(x) for x in bi]==[value(x) for x in b['Summary']['increments']]
        assert [frac(x) for x in pi]==[value(x) for x in p['Summary']['increments']]
first={n:next((s['stage'] for s in stages if F(s['deltas'][n]['fraction'])),None) for n in names}
rows=read(here/'diagnostics/sweep-results.json')['runs'];sweeps=[]
for a,b in zip(rows[::2],rows[1::2]):
    diffs=[value(y)-value(x) for x,y in zip(a['cache']+a['increments'],b['cache']+b['increments'])]
    sweeps.append({'mode':a['name'].replace('baseline-',''),'pairedPasses':sum(abs(x)<=u for x,u in zip(diffs,ulps)),
        'nativeSame':a['nativeBits']==b['nativeBits'],'delta':{n:exact(x) for n,x in zip(names,diffs)},
        'errorInReferenceUlps':{n:str(abs(x)/u) for n,x,u in zip(names,diffs,ulps)},
        'baseline':{k:a[k] for k in ('normalError','linearError','angularError','residual')},
        'powered':{k:b[k] for k in ('normalError','linearError','angularError','residual')}})
ds=read(here/'diagnostics/downstream-sensitivity.json')['results'];a,b=ds
down={'bothReady':a['status']==b['status']=='Ready','equationsIdentical':a['equations']==b['equations'],
    'cacheEqual':a['cache']==b['cache'],'incrementsEqual':a['increments']==b['increments'],'endpointEqual':a['endpoint']==b['endpoint'],
    'nativeBitsEqual':a['nativeBits']==b['nativeBits'],'normalClamps':[a['proof']['NormalClamps'],b['proof']['NormalClamps']],
    'tangentClamps':[a['proof']['TangentClamps'],b['proof']['TangentClamps']],'twistClamps':[a['proof']['TwistClamps'],b['proof']['TwistClamps']],
    'scope':'one identical frozen-geometry piece, diagnostic owner promotion; not a native-world trajectory'}
def differences(a,b,path=''):
    if type(a)!=type(b):return [path]
    if isinstance(a,dict):return sum((differences(a[k],b[k],path+'/'+k) for k in a),[])
    if isinstance(a,list):return sum((differences(x,y,path+'/'+str(i)) for i,(x,y) in enumerate(zip(a,b))),[])
    return [] if a==b else [path]
old=here.parent/'tiny-active-friction/candidate-results'
before=[read(old/(s+'-coast-input.json')) for s in ('baseline','powered')]
result={'referenceRounding':reference,'inputDifferencePaths':differences(*before),'fixedCoastEquationsIdentical':True,
    'firstNonzeroDiagnosticDelta':first,'stages':stages,'sweeps':sweeps,'downstream':down,
    'minimumAchievablePairedDifference':0,'minimumWitness':'D32 coalesces all thirteen fields with identical fixed equations; deterministic continuation preserves equality. Not absolute convergence.',
    'optionalTwoTimesControl':'NOT RUN; zero-input control and identical equations already isolate historical load plus finite arithmetic.'}
out=Path(sys.argv[1])
with out.open('x',encoding='utf-8') as f:json.dump(result,f,indent=2);f.write('\n')
print('INPUT DIFFERENCES',result['inputDifferencePaths']);print('FIRST',first)
for r in sweeps:print(r['mode'],r['pairedPasses'],'/13',r['nativeSame'])
print('DOWNSTREAM',down)
