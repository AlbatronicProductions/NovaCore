"""Read-only result extraction and independent scalar/matrix oracles; no physics mutation."""
from pathlib import Path
import json, math, hashlib
E=Path(__file__).parent; R=Path('E:/NovaCore'); B=R/'build/srv01-stage5-closure'
def read(mode):
    d={}
    for line in (B/(mode+'.txt')).read_text(encoding='utf-8-sig').splitlines():
        tag,_,s=line.partition(' ')
        if tag in ('INPUT','TERMS','WITNESS','RESULT','CONSTRAINT'): d.setdefault(tag,[]).append(json.loads(s))
    return d
def save(name,data): (E/name).write_text(json.dumps(data,indent=2)+'\n',encoding='utf-8')
data={m:read(m) for m in ('canonical','raw-full','zero-rotation','flat-local','stock-local','canonical-contacts')}
c=data['canonical']; f=c['INPUT'][0]; result=c['RESULT'][0]
for m in ('raw-full','canonical-contacts'):
    for key in ('nativePose','nativeVelocity','final','peak','compliant'):
        assert data[m]['RESULT'][0][key]==result[key],(m,key)
save('failed-witness.json',dict(input=f,terms=c['TERMS'],samples=c['WITNESS'],
    solverFacing=data['canonical-contacts']['CONSTRAINT'],result=result,
    exactNativeReplay=True,accounting=dict(publications=1200,ticks=20000000,stateRevision=1200,
        history=1200,timelineRevision=0,debt=0,engine='OFF',stores='UNCHANGED',mass='UNCHANGED'),
    note='Diagnostic reconstruction of unchanged production. Selected scratch array is not a convex-manifold contact index map. SolverFacing descriptions are authoritative.'))
save('controls.json',dict(results=[data[m]['RESULT'][0] for m in data if m!='canonical-contacts'],
    stockInput=data['stock-local']['INPUT'][0],
    changes={'raw-full':'Raw retained native stepping, same site inputs; omits canonical publication; exact endpoint match.',
    'zero-rotation':'Only Omega/Alpha zeroed; same pure central gravity and Florida slab.',
    'flat-local':'Site callback branch removed, constant (0,-9.81,0); development mass/geometry/8x1/contact material unchanged.',
    'stock-local':'Existing stock profile/local factory: 705 kg with coherent stock COM/inertia; diagnostic comparison, not a Florida qualification.'}))

def v(d): return [d[x] for x in ('X','Y','Z')]
def add(a,b): return [x+y for x,y in zip(a,b)]
def sub(a,b): return [x-y for x,y in zip(a,b)]
def scale(a,k): return [x*k for x in a]
def dot(a,b): return sum(x*y for x,y in zip(a,b))
def cross(a,b): return [a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]]
def norm(a): return math.sqrt(dot(a,a))
def transpose(a): return list(map(list,zip(*a)))
def mul(a,b): return [[dot(row,col) for col in zip(*b)] for row in a]
def mv(a,b): return [dot(row,b) for row in a]
def madd(*a): return [[sum(m[i][j] for m in a) for j in range(3)] for i in range(3)]
def mscale(a,k): return [scale(row,k) for row in a]
def qmat(q):
    x,y,z,w=[q[k] for k in ('X','Y','Z','W')]
    return [[1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w)],
            [2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w)],
            [2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y)]]
def rot(axis,t,rate):
    co,si=math.cos(t),math.sin(t); I=[[1.,0,0],[0,1.,0],[0,0,1.]]
    if axis=='z': A=[[co,-si,0],[si,co,0],[0,0,1]]; J=[[0,-rate,0],[rate,0,0],[0,0,0]]
    else: A=[[1,0,0],[0,co,-si],[0,si,co]]; J=[[0,0,0],[0,0,-rate],[0,rate,0]]
    return A,mul(J,A),mul(mul(J,J),A)
def product(a,b):
    return mul(a[0],b[0]),madd(mul(a[1],b[0]),mul(a[0],b[1])),madd(mul(a[2],b[0]),mscale(mul(a[1],b[1]),2),mul(a[0],b[2]))
rad=math.pi/180; century=86400*36525
up=[.1433224599406355,.4788205718227514,.8661348234979923]
east=[.9865841313746494,0,-.163253642286255]
north=[-.07816920235165153,.8779127861008367,-.47239677793606216]
L=transpose([east,up,scale(north,-1)])
origin=add(scale(up,6371008.8+15.134892258793116+1.7),scale(east,48))
oracle=[]
for seconds,frame,term in [(0,f['f0'],c['TERMS'][0]),(20,f['f20'],c['TERMS'][1])]:
    # IAU linear model constants are sourced; computation uses independent matrices and product derivatives.
    e=product(product(product(rot('z',(90-.641*seconds/century)*rad,-.641*rad/century),
        rot('x',.557*seconds/century*rad,.557*rad/century)),
        rot('z',(190.147+360.9856235*seconds/86400)*rad,360.9856235*rad/86400)),rot('x',math.pi/2,0))
    C,Cd,Cdd=[mul(x,L) for x in e]; Ct=transpose(C)
    skew=mul(Cd,Ct); skewd=madd(mul(Cdd,Ct),mul(Cd,transpose(Cd)))
    omega=[skew[2][1],skew[0][2],skew[1][0]]; alpha=[skewd[2][1],skewd[0][2],skewd[1][0]]
    omegaL=mv(Ct,omega); alphaL=mv(Ct,alpha)
    p,pd,pdd=[mv(x,origin) for x in e]
    motion=f['initial']; q=qmat(motion['BodyToWorld']); po=v(motion['PositionO']); vo=v(motion['VelocityO'])
    pi=add(p,mv(C,po)); vi=add(pd,add(mv(C,vo),mv(Cd,po))); qi=mul(C,q)
    wi=add(v(motion['AngularVelocityBody']),mv(transpose(q),omegaL))
    com=add(po,mv(q,v(f['mass']['Com']))); radius=add(origin,mv(L,com))
    gravity=mv(transpose(L),scale(radius,-f['Mu']/norm(radius)**3)); originAL=mv(Ct,pdd)
    centrifugal=scale(cross(omegaL,cross(omegaL,com)),-1); euler=scale(cross(alphaL,com),-1); coriolis=scale(cross(omegaL,vo),-2)
    acceleration=add(sub(gravity,originAL),add(centrifugal,add(euler,coriolis)))
    qf=qmat(frame['Orientation']); eq=qmat(term['earth']['BodyToWorld'])
    errors=dict(originPosition=norm(sub(p,v(frame['Position']))),originVelocity=norm(sub(pd,v(frame['Velocity']))),
      orientationMatrix=max(abs(C[i][j]-qf[i][j]) for i in range(3) for j in range(3)),omega=norm(sub(omegaL,v(frame['Omega']))),alpha=norm(sub(alphaL,v(frame['Alpha']))),
      inertialPosition=norm(sub(pi,v(term['earth']['PositionO']))),inertialVelocity=norm(sub(vi,v(term['earth']['VelocityO']))),
      inertialOrientationMatrix=max(abs(qi[i][j]-eq[i][j]) for i in range(3) for j in range(3)),
      inertialBodyOmega=norm(sub(wi,v(term['earth']['AngularVelocityBody']))),acceleration=norm(sub(acceleration,v(term['actual']))))
    assert errors['inertialPosition']<1e-8 and errors['inertialVelocity']<1e-10 and errors['acceleration']<1e-12,errors
    oracle.append(dict(seconds=seconds,originPosition=p,originVelocity=pd,originAcceleration=pdd,omegaRoot=omega,alphaRoot=alpha,
      omegaLocal=omegaL,alphaLocal=alphaL,gravity=gravity,originAccelerationLocal=originAL,centrifugalLocal=centrifugal,eulerLocal=euler,coriolisLocal=coriolis,
      totalAcceleration=acceleration,inertialPosition=pi,inertialVelocity=vi,inertialBodyOmega=wi,errors=errors))
save('site-oracle.json',dict(method='Independent 3x3 products and analytic derivatives, no production frame/quaternion helper; known model constants only.',
    epochs=oracle,latitudeDegrees=math.degrees(math.asin(up[1])),longitudeEastDegrees=math.degrees(math.atan2(-up[2],up[0])),
    siteEastOffsetMetres=48,planeAltitudeMetres=15.134892258793116,
    limitations='Two held-relative endpoint probes at 0/20 s. This does not qualify evolving contact or rotating Stage3 handoff.'))

spring=[]
for mode in ('stock-local','canonical'):
    fi=data[mode]['INPUT'][0]; mass=fi['mass']['Mass']; inertia=fi['mass']['Inertia']['E']; a=.26; omega=2*math.pi*30
    K=1/mass+2*a*a/inertia
    spring.append(dict(mode=mode,mass=mass,transverseInertia=inertia,inverseRowEffectiveMass=K,
        rotationalToTranslationalRowContribution=mass*2*a*a/inertia,
        symmetricEquilibriumDepth=mass*9.81*K/(4*omega*omega),
        supportHeaveToPitchRatio=inertia/(mass*a*a)))
save('contact-analysis.json',dict(derivation='At stationary four-point symmetry lambda=m*g*h/4. Zero row update gives depth*p*CFM/Kii=lambda*softness; softness/CFM=q and q/p=1/(h*omega^2). Thus depth=m*g*Kii/(4*omega^2), Kii=1/m+2a^2/I. See mass-oracle.py for independent authored-data reconstruction. Not a full dynamic trajectory prediction.',
    comparison=spring,source='Accepted BEPU Contact4OneBodyFunctions, PenetrationLimitOneBody and SpringSettingsWide read directly from repository DLL.',
    actualConstraint=data['canonical-contacts']['CONSTRAINT'][-1],
    normalImpulseSum=sum(data['canonical-contacts']['CONSTRAINT'][-1]['impulses'][2:6]),
    gravitationalImpulseAtFinalInterval=f['mass']['Mass']*abs(c['TERMS'][1]['actual']['Y'])*.016667,
    limitation='Does not prove FP32 error, an incorrect mass definition, or a sole cause of residual angular/linear motion. No iteration/spring tuning performed.'))
print(json.dumps(dict(oracleMaxPosition=max(x['errors']['inertialPosition'] for x in oracle),oracleMaxVelocity=max(x['errors']['inertialVelocity'] for x in oracle),spring=spring),indent=2))
