"""Independent SRV01 FourHorn trajectory reference reproduction.

Adaptation of the retained critic's direct coupled 6x6 Newton/Euler oracle.
No production evaluator or old endpoint values are imported. Authored JSON is
physical input; old reference cases supply only the unchanged 24-case schedule.
Decimal 60-digit integration, exact Fraction event/depletion boundaries, and
independent 4x/8x temporal refinement. All generated writes go to the explicitly supplied output directory.
"""
from pathlib import Path
from decimal import Decimal as D, getcontext
from fractions import Fraction as Q
import argparse, hashlib, json, math, time, sys

getcontext().prec = 60
HERE = Path(__file__).resolve().parent
REPO = HERE.parents[2]
DESIGN = REPO / 'src/NovaCore.Simulation/Spacecraft/Assemblies/Data/SRV01-FourHorn.json'
DESIGN_SHA = 'ac23c15ae52adc5e8e836bd954d9084b10a2699cc01f0a6906a992724ea67fcf'
CASES = REPO / 'tests/NovaCore.Simulation.Tests/Data/Assembly-Reference-Endpoints.json'
CASES_SHA = '2fc90b50e227462e87c35148d8c3fc862091f521f4b45ea54d0f07ba60160431'
OLD_ORACLE = 'retained critic check_recovery.py (provenance only; no runtime dependency)'
OLD_SHA = 'd2933344a9b7a776588ec350ae4efba3a219fadda259c8d55174413242849d59'
Z, O = D(0), D(1)

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def decimal(x): return D.from_float(x) if isinstance(x, float) else D(x)
def rational_decimal(x): return D(x.numerator) / D(x.denominator)
def add(a,b): return [x+y for x,y in zip(a,b)]
def sub(a,b): return [x-y for x,y in zip(a,b)]
def scale(a,k): return [x*k for x in a]
def dot(a,b): return sum((x*y for x,y in zip(a,b)), Z)
def norm(a): return dot(a,a).sqrt()
def cross(a,b): return [a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]]
def mv(a,b): return [dot(row,b) for row in a]
def transpose(a): return list(map(list,zip(*a)))
def mm(a,b): return [[dot(r,c) for c in transpose(b)] for r in a]
def matrix(d): return [[decimal(d[k]) for k in row] for row in ('abc','def','ghi')]
def vector(d): return [decimal(d[k]) for k in 'xyz']
def qmul(p,q):
    w,x,y,z=p; a,b,c,d=q
    return [w*a-x*b-y*c-z*d,w*b+x*a+y*d-z*c,w*c-x*d+y*a+z*b,w*d+x*c-y*b+z*a]
def rotate(q,v): return qmul(qmul(q,[Z]+v),[q[0],-q[1],-q[2],-q[3]])[1:]
def sin_cos(x):
    s=term=x; n=1
    while abs(term)>D('1e-70'):
        term *= -x*x / D((2*n)*(2*n+1)); s+=term; n+=1
    c=term=O; n=1
    while abs(term)>D('1e-70'):
        term *= -x*x / D((2*n-1)*(2*n)); c+=term; n+=1
    return s,c

class Oracle:
    def __init__(self, data):
        self.data=data; definitions={x['id']:x for x in data['definitions']}
        self.parts={}; self.jets={}; self.dry=Z; self.first=[Z]*3
        self.io=[[Z]*3 for _ in range(3)]
        for p in sorted(data['design']['instances'],key=lambda x:x['order']):
            d=definitions[p['definition']['id']]; R=matrix(p['pose']['rotation']); pos=vector(p['pose']['position'])
            c=add(pos,mv(R,vector(d['localCom']))); mass=decimal(d['dryMassKg'])
            I=mm(mm(R,matrix(d['localInertia'])),transpose(R))
            self.dry+=mass; self.first=add(self.first,scale(c,mass)); cc=dot(c,c)
            for i in range(3):
                for j in range(3): self.io[i][j]+=I[i][j]+mass*((cc if i==j else Z)-c[i]*c[j])
            self.parts[p['id']] = (d,R,pos)
            for jet in d.get('jetActuators',[]):
                point=add(pos,mv(R,vector(jet['point']))); force=scale(mv(R,vector(jet['axis'])),decimal(jet['fullThrustN']))
                self.jets[(p['id'],jet['id'])]=(point,force,Q(jet['extentRateKgS']))
            if d['role']=='MainEngine': self.main=(d,R,pos)
        self.pairs={p['name']:p for p in data['design']['pairs']}
        self.pair_parts={tuple(sorted([int(p['first'][-2:]),int(p['second'][-2:])])):name for name,p in self.pairs.items()}
        assert len(self.jets)==16 and len(self.pairs)==6 and len(self.pair_parts)==6
        assert self.first[1]==0 and self.first[2]==0
        # Decimal arithmetic can leave last-place cancellation residue. Keep
        # every term in the full 6x6 solve; never truncate a tensor component.
        assert all(abs(self.io[i][j])<D('1e-45') for i in range(3) for j in range(3) if i!=j)
        s=self.first; self.sk=[[Z,-s[2],s[1]],[s[2],Z,-s[0]],[-s[1],s[0],Z]]

    def wrench(self, case):
        f=[Z]*3; t=[Z]*3; rate=Q(0)
        if case['main']:
            d,R,pos=self.main; e=d['propulsion']; g=d['gimbal']
            gy,gz=map(decimal,case['angles']); sy,cy=sin_cos(gy); sz,cz=sin_cos(gz)
            rotation=[[cz*cy,-sz,cz*sy],[sz*cy,cz,sz*sy],[-sy,Z,cy]]
            force=scale(mv(R,mv(rotation,vector(e['axis']))),decimal(e['fullThrustN']))
            pivot=add(pos,mv(R,vector(g['pivot'])))
            # Coaxial nozzle offset contributes identically zero moment.
            f=add(f,force); t=add(t,cross(pivot,force)); rate+=Q(e['extentRateKgS'])
        selected=[]; pair_name=None
        if case['pair']:
            pair_name=self.pair_parts[tuple(sorted(case['pair']))]; p=self.pairs[pair_name]
            selected=[(p['first'],p['firstActuator']),(p['second'],p['secondActuator'])]
            for key in selected:
                point,force,k=self.jets[key];f=add(f,force);t=add(t,cross(point,force));rate+=k
        return f,t,rate,pair_name,selected

    def solve6(self, mass, force, torque, omega):
        linear=sub(force,cross(omega,cross(omega,self.first)))
        angular=sub(torque,cross(omega,mv(self.io,omega)))
        rows=[[mass if i==j else Z for j in range(3)]+[-x for x in self.sk[i]]+[linear[i]] for i in range(3)]
        rows += [list(self.sk[i])+list(self.io[i])+[angular[i]] for i in range(3)]
        for col in range(6):
            pivot=max(range(col,6),key=lambda i:abs(rows[i][col])); rows[col],rows[pivot]=rows[pivot],rows[col]
            factor=rows[col][col];assert factor!=0
            for j in range(col,7): rows[col][j]/=factor
            for i in range(6):
                if i==col or rows[i][col]==0: continue
                factor=rows[i][col]
                for j in range(col,7): rows[i][j]-=factor*rows[col][j]
        return [row[6] for row in rows]

    def integrate(self, case, refinement):
        force,torque,k,_,_=self.wrench(case); total_rate=5*k; load=Q(case['load']); duration=Q(case['duration'])
        exhaustion=load/total_rate if total_rate else None
        intervals=duration*64*refinement; assert intervals.denominator==1
        events={Q(i,64*refinement) for i in range(int(intervals)+1)}
        if exhaustion is not None and 0<exhaustion<duration: events.add(exhaustion)
        events=sorted(events)
        initial_v=[decimal(.15),decimal(-.2),Z] if case['moving'] else [Z]*3
        initial_w=[decimal(.03),decimal(-.04),Z] if case['moving'] else [Z]*3
        y=[Z]*3+initial_v+[O,Z,Z,Z]+initial_w
        def derivative(time,state,powered):
            mass=self.dry+rational_decimal(max(Q(0),load-total_rate*time))
            a=self.solve6(mass,force if powered else [Z]*3,torque if powered else [Z]*3,state[10:])
            return state[3:6]+rotate(state[6:10],a[:3])+scale(qmul(state[6:10],[Z]+state[10:]),D('.5'))+a[3:]
        for start,end in zip(events,events[1:]):
            delta=end-start;h=rational_decimal(delta);powered=bool(total_rate and start<exhaustion)
            a=derivative(start,y,powered);b=derivative(start+delta/2,add(y,scale(a,h/2)),powered)
            c=derivative(start+delta/2,add(y,scale(b,h/2)),powered);d=derivative(end,add(y,scale(c,h)),powered)
            y=[v+h*(aa+2*bb+2*cc+dd)/6 for v,aa,bb,cc,dd in zip(y,a,b,c,d)]
            y[6:10]=scale(y[6:10],O/norm(y[6:10]))
        if y[6]<0:y[6:10]=scale(y[6:10],-O)
        return y, {'steps':len(events)-1,'powered_seconds':str(min(duration,exhaustion) if total_rate else Q(0)),
                   'remaining_total_resource_kg':str(max(Q(0),load-total_rate*duration))}

def errors(a,b):
    q=min(norm(sub(a[6:10],b[6:10])),norm(add(a[6:10],b[6:10])))
    return {'position_m':float(norm(sub(a[:3],b[:3]))),'velocity_m_s':float(norm(sub(a[3:6],b[3:6]))),
            'orientation_rad':4*math.asin(min(1,float(q/2))),'omega_rad_s':float(norm(sub(a[10:],b[10:])))}

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--suffix',default='');parser.add_argument('--output-directory',type=Path,required=True);args=parser.parse_args();args.output_directory.mkdir(parents=True,exist_ok=True)
    assert sha(DESIGN)==DESIGN_SHA and sha(CASES)==CASES_SHA
    oracle=Oracle(json.loads(DESIGN.read_text())); cases=[r['case'] for r in json.loads(CASES.read_text())['trajectory_rows']]
    assert len(cases)==24 and len({x['name'] for x in cases})==24
    rows=[]; began=time.perf_counter()
    for case in cases:
        coarse,_=oracle.integrate(case,4); fine,stats=oracle.integrate(case,8)
        err=errors(coarse,fine)
        assert err['position_m']<1e-9 and err['velocity_m_s']<1e-9 and err['orientation_rad']<1e-11 and err['omega_rad_s']<1e-11,(case['name'],err)
        force,torque,rate,pair,selected=oracle.wrench(case)
        # Existing test fixture stores quaternion as XYZW; oracle uses WXYZ.
        endpoint=fine[:6]+fine[7:10]+[fine[6]]+fine[10:]
        rows.append({'case':case,'pair_name':pair,'selected_nozzles':[list(x) for x in selected],
                     'endpoint':[float(x) for x in endpoint],'endpoint_decimal60':[str(x) for x in endpoint],
                     'reference_refinement_difference':err,'stats':stats,
                     'force_N':[str(x) for x in force],'moment_origin_Nm':[str(x) for x in torque],
                     'exact_extent_rate_kg_s':str(rate)})
        print(case['name']+': '+json.dumps(err),flush=True)
    # Analytic references exercise the direct coupled solver without relying
    # on either numerical refinement or a second implementation of its ODE.
    straight={'name':'analytic straight main at rest','main':1,'pair':[],
              'angles':[0,0],'duration':'2','load':'75','moving':False}
    actual,_=oracle.integrate(straight,8)
    total=D(2); initial=oracle.dry+D(75); mu=D(25)/128; final=initial-mu*total
    log=(initial/final).ln(); speed=D(3072)*log
    distance=D(3072)*(total-final/mu*log)
    expected=[distance,Z,Z,speed,Z,Z,O,Z,Z,Z,Z,Z,Z]
    straight_error=errors(actual,expected)
    roll_case={'name':'analytic roll at rest','main':0,'pair':[1,3],
               'angles':[0,0],'duration':'2','load':'75','moving':False}
    actual,_=oracle.integrate(roll_case,8)
    force,torque,_,_,_=oracle.wrench(roll_case)
    assert force==[Z]*3 and torque[1:]==[Z]*2
    acceleration=torque[0]/oracle.io[0][0]
    sn,cs=sin_cos(acceleration*total*total/4)
    expected=[Z]*6+[cs,sn,Z,Z,acceleration*total,Z,Z]
    roll_error=errors(actual,expected)
    for err in (straight_error,roll_error):
        assert err['position_m']<1e-11 and err['velocity_m_s']<1e-11 and err['orientation_rad']<1e-11 and err['omega_rad_s']<1e-11
    result={'provenance':'Independent Decimal60 direct coupled 6x6 point-store dynamics; no C# evaluator or previous endpoint values imported.',
            'schema':'novacore.assembly-reference/2','design_sha256':DESIGN_SHA,'source_sha256':sha(Path(__file__)),
            'retained_direct_oracle':str(OLD_ORACLE),'retained_direct_oracle_sha256':OLD_SHA,
            'retained_case_source':str(CASES),'retained_case_source_sha256':CASES_SHA,
            'design_bytes_interpretation':'Every authored JSON floating value is lifted from its exact binary64 value; aggregation and dynamics use Decimal60.',
            'method':'Direct six-equation Gaussian solve; RK4 at 256 and 512 steps/second, exact rational exhaustion event split; XYZW fixture quaternion.',
            'dry_mass_kg':str(oracle.dry),'first_moment_kg_m':[str(x) for x in oracle.first],
            'inertia_about_origin_kg_m2':[[str(x) for x in row] for row in oracle.io],
            'trajectory_rows':rows,'maximum_reference_refinement_difference':{k:max(r['reference_refinement_difference'][k] for r in rows) for k in rows[0]['reference_refinement_difference']},
            'analytic_checks':{'straight_main_rocket_equation':straight_error,'constant_axial_roll_acceleration':roll_error},
            'limitation':'Finite independent endpoint convergence evidence, not a formal global truncation-error certificate or production execution result.'}
    path=args.output_directory/('Assembly-FourHorn-Reference-Endpoints'+args.suffix+'.json')
    path.write_text(json.dumps(result,indent=2)+'\n',encoding='utf8')
    print(json.dumps({'output':str(path),'sha256':sha(path),'cases':len(rows),'maxima':result['maximum_reference_refinement_difference'],'elapsed_seconds':time.perf_counter()-began}),flush=True)

if __name__=='__main__':main()
