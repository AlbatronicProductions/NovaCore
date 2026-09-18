"""Independent authored-data aggregation and static contact algebra. No engine execution."""
from pathlib import Path
from fractions import Fraction as F
import json, math, hashlib

ROOT=Path('E:/NovaCore'); OUT=Path(__file__).parent
DATA=ROOT/'src/NovaCore.Simulation/Spacecraft/Assemblies/Data'
stock_path=DATA/'SRV01-FourHorn.json'; development_path=DATA/'SRV01-Development-Propulsion-v1.json'
stock=json.loads(stock_path.read_text(),parse_float=F,parse_int=F)
development=json.loads(development_path.read_text(),parse_float=F,parse_int=F)
def vector(x): return [x[k] for k in 'xyz']
def matrix(x): return [[x[k] for k in row] for row in ('abc','def','ghi')]
def dot(x,y): return sum(a*b for a,b in zip(x,y))
def plus(x,y): return [a+b for a,b in zip(x,y)]
def transpose(a): return list(map(list,zip(*a)))
def mv(a,v): return [dot(row,v) for row in a]
def mm(a,b): return [[dot(row,col) for col in zip(*b)] for row in a]
def scale(a,s): return [[v*s for v in row] for row in a]
def madd(a,b): return [plus(x,y) for x,y in zip(a,b)]
def parallel(v): return [[(dot(v,v) if i==j else 0)-v[i]*v[j] for j in range(3)] for i in range(3)]
def inverse(a):
    rows=[list(row)+[F(i==j) for j in range(3)] for i,row in enumerate(a)]
    for col in range(3):
        pivot=next(i for i in range(col,3) if rows[i][col])
        rows[col],rows[pivot]=rows[pivot],rows[col]
        divisor=rows[col][col]; rows[col]=[x/divisor for x in rows[col]]
        for i in range(3):
            if i!=col:
                factor=rows[i][col]; rows[i]=[x-factor*y for x,y in zip(rows[i],rows[col])]
    return [row[3:] for row in rows]
def cross(a,b): return [a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]]
def floats(x):
    if isinstance(x,F): return float(x)
    if isinstance(x,list): return [floats(v) for v in x]
    if isinstance(x,dict): return {k:floats(v) for k,v in x.items()}
    return x

definitions={x['id']:x for x in stock['definitions']}
dry=F(0); first=[F(0)]*3; origin=[[F(0)]*3 for _ in range(3)]; parts=[]
for instance in stock['design']['instances']:
    d=definitions[instance['definition']['id']]; mass=d['dryMassKg']
    rotation=matrix(instance['pose']['rotation'])
    com=plus(vector(instance['pose']['position']),mv(rotation,vector(d['localCom'])))
    inertia=mm(mm(rotation,matrix(d['localInertia'])),transpose(rotation))
    dry+=mass; first=plus(first,[mass*x for x in com])
    origin=madd(origin,madd(inertia,scale(parallel(com),mass)))
    parts.append(dict(id=instance['id'],dryMass=mass,com=com,inertiaAtOwnCom=inertia))
    for store in d['stores']:
        assert plus(vector(instance['pose']['position']),mv(rotation,vector(store['datum'])))==[0,0,0]

controls=json.loads((OUT/'controls.json').read_text()); failed=json.loads((OUT/'failed-witness.json').read_text())
observations={'stock':controls['stockInput'],'development':failed['input']}
# Geometry equivalence excludes definition digests, which legitimately change with capacities/engine.
for a,b in zip(observations['stock']['children'],observations['development']['children'],strict=True):
    assert {k:v for k,v in a.items() if k!='DefinitionDigest'}=={k:v for k,v in b.items() if k!='DefinitionDigest'}
rows=[]; matrices={}
for name,config in [('stock',stock['design']),('development',development)]:
    fuel=config['initialFuelKg']; ox=config['initialOxidizerKg']; mass=dry+fuel+ox
    com=[x/mass for x in first]; inertia=madd(origin,scale(parallel(com),-mass))
    expected=observations[name]['mass']
    assert float(mass)==expected['Mass']
    assert max(abs(float(com[i])-expected['Com'][k]) for i,k in enumerate('XYZ'))<1e-12
    assert max(abs(float(inertia[i][j])-expected['Inertia']['ABCDEFGHI'[3*i+j]]) for i in range(3) for j in range(3))<1e-9
    # Exact ideal upright map, not the production quaternion; support plane normal is local +Y.
    rotation=[[0,-1,0],[1,0,0],[0,0,1]]
    local_inertia=mm(mm(rotation,inertia),transpose(rotation)); inv=inverse(local_inertia)
    a=F(26,100); n=[0,1,0]
    offsets=[[x,-F(17,10)-com[0],z] for x,z in [(-a,-a),(a,a),(-a,a),(a,-a)]]
    jac=[cross(r,n) for r in offsets]
    K=[[1/mass+dot(x,mv(inv,y)) for y in jac] for x in jac]
    diagonal=K[0][0]; omega=2*math.pi*30; weight=mass*F(981,100)
    compression=float(weight*diagonal)/(4*omega*omega)
    moments=[inertia[i][i] for i in range(3)]
    rows.append(dict(profile=name,fuelKg=fuel,oxidizerKg=ox,massKg=mass,com=com,
        fullInertiaMaterial=inertia,fullInertiaLocal=local_inertia,localNormal=n,contactOffsets=offsets,
        rotationalInverseMass=diagonal-1/mass,inverseEffectiveMass=diagonal,effectiveMass=1/diagonal,
        rotationalToTranslationalRatio=mass*diagonal-1,radiiOfGyration=[math.sqrt(float(x/mass)) for x in moments],
        weightN=weight,symmetricRowForceN=weight/4,frequencyHz=30,dampingRatio=1,
        rowStiffnessNPerM=omega*omega/float(diagonal),predictedStaticCompressionM=compression,
        idealNormalMatrix=K,nonzeroEigenvalues=[4/mass,4*a*a/inertia[1][1],4*a*a/inertia[2][2]],
        redundantNormalModeEigenvalue=0))
    matrices[name]=inertia
result=dict(method='Exact rational aggregation from authored JSON; independent matrix algebra. Production diagnostic properties checked afterward, not used to construct the oracle.',
    inputs=[dict(path=str(p.relative_to(ROOT)),sha256=hashlib.sha256(p.read_bytes()).hexdigest().upper()) for p in [stock_path,development_path]],
    dryMass=dry,firstMoment=first,inertiaAboutMaterialOrigin=origin,parts=parts,profiles=rows,
    geometryMatched=True,massRatio=rows[1]['massKg']/rows[0]['massKg'],
    principalInertiaRatios=[matrices['development'][i][i]/matrices['stock'][i][i] for i in range(3)],
    derivation='At static symmetry lambda=m*g*h/4. Zero row update gives depth*p*CFM/Kii=lambda*softness. softness/CFM=q and q/p=1/(h*omega^2), hence depth=m*g*Kii/(4*omega^2).',
    limits='Ideal stationary symmetric four-contact model at g=9.81. Not a dynamic trajectory oracle or proof of the entire residual motion. A redundant normal row is expected, not by itself a conditioning defect. No candidate tensor or parameter selected.')
(OUT/'mass-oracle.json').write_text(json.dumps(floats(result),indent=2)+'\n')
print(json.dumps(floats(dict(massRatio=result['massRatio'],principalInertiaRatios=result['principalInertiaRatios'],profiles=[{k:r[k] for k in ('profile','massKg','effectiveMass','predictedStaticCompressionM','radiiOfGyration')} for r in rows])),indent=2))
