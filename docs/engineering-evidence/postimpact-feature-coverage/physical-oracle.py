"""Diagnostic SAME-model physical oracle; never a production contact certificate.

Requires mpmath 1.3.0. All exported binary64 values are restored exactly through
float.as_integer_ratio(), rather than interpreted as exact printed decimals.
Earth follows the seed's central-force Kepler ODE via universal variables;
spacecraft rotation follows the torque-free Euler/quaternion ODE via independent
high-precision Taylor coefficients. Compare precision and degree before relying
on numerical signs. Sampling and converged arithmetic are NOT interval proofs.
"""
import argparse
import json
import hashlib
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[3] / '.codex' / 'coverage-causal' / 'pythondeps'))
import mpmath as mp


def exact(x):
    if isinstance(x, int):
        return mp.mpf(x)
    n, d = float(x).as_integer_ratio()
    return mp.mpf(n) / d


def vec(v):
    if isinstance(v, dict):
        return tuple(exact(v[k]) for k in ('X', 'Y', 'Z'))
    return tuple(map(exact, v))


def add(a, b): return tuple(x+y for x, y in zip(a, b))
def sub(a, b): return tuple(x-y for x, y in zip(a, b))
def scale(a, b): return tuple(x*b for x in a)
def dot(a, b): return sum(x*y for x, y in zip(a, b))
def cross(a, b): return (a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2], a[0]*b[1]-a[1]*b[0])
def norm(a): return mp.sqrt(dot(a, a))


def rotate(q, v):
    u = q[:3]
    twice = scale(cross(u, v), 2)
    return add(add(v, scale(twice, q[3])), cross(u, twice))


def qproduct(q, w):
    return (*add(scale(w, q[3]), cross(q[:3], w)), -dot(q[:3], w))


def horner(coefficients, t):
    v = coefficients[-1]
    for c in reversed(coefficients[:-1]):
        v = add(scale(v, t), c)
    return v


class Physics:
    """Input mapping names are listed in prepare-model.json reproduction instructions."""
    def __init__(self, p, degree):
        self.x0, self.v0, self.force = map(vec, (p['position'], p['velocity'], p['force']))
        self.mass = exact(p['mass'])
        self.acc = scale(self.force, 1/self.mass)
        self.epoch = exact(p['epochTicks'])/1000000
        self.target = exact(p['targetTicks'])/1000000
        self.start = exact(p['startTicks'])/1000000
        self.er0, self.ev0 = map(vec, (p['earthPosition'], p['earthVelocity']))
        self.mu = exact(p['mu'])
        self.eradius = norm(self.er0)
        self.sqrtmu = mp.sqrt(self.mu)
        self.kepler_alpha = 2/self.eradius-dot(self.ev0, self.ev0)/self.mu
        self.rv = dot(self.er0, self.ev0)/self.sqrtmu
        self.orientation = {k:exact(v) for k, v in p['earthOrientation'].items()}
        self.lever, self.up, self.east, self.north = map(vec, (p['lever'], p['up'], p['east'], p['north']))
        self.radius, self.plane = exact(p['radius']), exact(p['planeAltitude'])
        self.innerE, self.innerN = exact(p['innerEast']), exact(p['innerNorth'])
        self.plus, self.spin, self.inertia = map(vec, (p['plusVelocity'], p['plusSpin'], p['inertia']))
        q = tuple(exact(p['quaternion'][k]) for k in ('X','Y','Z','W'))
        self.q0 = scale(q, 1/norm(q))
        self.alpha = mp.findroot(lambda t:self.pre(t)[0], (exact(p['rootLower']), exact(p['rootUpper'])))
        self.xalpha = self.com_pre(self.alpha)[0]
        self.wc, self.qc = self.rotation_coefficients(degree)

    def earth(self, t):
        if t == 0: return self.er0, self.ev0, scale(self.er0, -self.mu/self.eradius**3)
        def stumpff(z):
            # Entire-series form is well conditioned over this <= 3600 second source domain.
            c, s = mp.mpf('.5'), mp.mpf(1)/6
            ct, st = c, s
            for k in range(1, 30):
                ct *= -z/((2*k+1)*(2*k+2)); st *= -z/((2*k+2)*(2*k+3))
                c += ct; s += st
            return c, s
        def equation(chi):
            z = self.kepler_alpha*chi**2
            c, s = stumpff(z)
            return self.rv*chi**2*c+(1-self.kepler_alpha*self.eradius)*chi**3*s+self.eradius*chi-self.sqrtmu*t
        chi = mp.findroot(equation, self.sqrtmu*t/self.eradius)
        c, s = stumpff(self.kepler_alpha*chi**2)
        f = 1-chi**2*c/self.eradius
        g = t-chi**3*s/self.sqrtmu
        r = add(scale(self.er0, f), scale(self.ev0, g))
        rn = norm(r)
        fd = self.sqrtmu/(rn*self.eradius)*(self.kepler_alpha*chi**3*s-chi)
        gd = 1-chi**2*c/rn
        v = add(scale(self.er0, fd), scale(self.ev0, gd))
        return r, v, scale(r, -self.mu/rn**3)

    def com_pre(self, t):
        d = t-self.epoch
        return add(add(self.x0,scale(self.v0,d)),scale(self.acc,d*d/2)), add(self.v0,scale(self.acc,d))

    def body_jet(self, t, p, v, a):
        o = self.orientation
        day, century = o['SecondsPerDay'], o['SecondsPerDay']*o['DaysPerCentury']
        rad = mp.pi/180
        angles = ((-(o['Ra0']+o['RaT']*t/century+90)*rad, -o['RaT']*rad/century, False),
                  (-(90-o['Dec0']-o['DecT']*t/century)*rad, o['DecT']*rad/century, True),
                  (-(o['W0']+o['Wd']*t/day)*rad, -o['Wd']*rad/day, False))
        for angle, rate, xaxis in angles:
            c, s = mp.cos(angle), mp.sin(angle)
            def axial(z):
                return (z[0],c*z[1]-s*z[2],s*z[1]+c*z[2]) if xaxis else (c*z[0]-s*z[1],s*z[0]+c*z[1],z[2])
            axis = (rate,0,0) if xaxis else (0,0,rate)
            q, rv = axial(p), axial(v)
            a = add(add(axial(a),scale(cross(axis,rv),2)),cross(axis,cross(axis,q)))
            v = add(rv,cross(axis,q)); p = q
        def basis(z): return (z[0],z[2],-z[1])
        return basis(p), basis(v), basis(a)

    def gap_jet(self,t,p,v,a):
        er,ev,ea = self.earth(t)
        body = self.body_jet(t,sub(p,er),sub(v,ev),sub(a,ea))
        return (dot(self.up,body[0])-self.radius-self.plane,dot(self.up,body[1]),dot(self.up,body[2])),body

    def pre(self,t):
        p,v = self.com_pre(t)
        return self.gap_jet(t,add(p,rotate(self.q0,self.lever)),v,self.acc)[0]

    def rotation_coefficients(self,degree):
        wc,qc=[self.spin],[self.q0]
        c=tuple((self.inertia[(i+1)%3]-self.inertia[(i+2)%3])/self.inertia[i] for i in range(3))
        for n in range(degree):
            wn=tuple(c[i]*sum(wc[k][(i+1)%3]*wc[n-k][(i+2)%3] for k in range(n+1))/(n+1) for i in range(3))
            qn=(mp.mpf(0),)*4
            for k in range(n+1): qn=add(qn,qproduct(qc[k],wc[n-k]))
            wc.append(wn);qc.append(scale(qn,mp.mpf(1)/(2*(n+1))))
        return wc,qc

    def post(self,s):
        q,w = horner(self.qc,s),horner(self.wc,s)
        dw=tuple((self.inertia[(i+1)%3]-self.inertia[(i+2)%3])*w[(i+1)%3]*w[(i+2)%3]/self.inertia[i] for i in range(3))
        p=add(add(self.xalpha,scale(self.plus,s)),scale(self.acc,s*s/2))
        v=add(self.plus,scale(self.acc,s))
        lever=rotate(q,self.lever)
        rotational_v=rotate(q,cross(w,self.lever))
        angular_a=rotate(q,cross(dw,self.lever))
        centripetal=rotate(q,cross(w,cross(w,self.lever)))
        jet,body=self.gap_jet(self.alpha+s,add(p,lever),add(v,rotational_v),add(self.acc,add(angular_a,centripetal)))
        return jet,body,q,w

    def departure_components(self):
        zero=(mp.mpf(0),)*3
        er,ev,ea=self.earth(self.alpha)
        lever=rotate(self.q0,self.lever)
        rv=rotate(self.q0,cross(self.spin,self.lever))
        dw=tuple((self.inertia[(i+1)%3]-self.inertia[(i+2)%3])*self.spin[(i+1)%3]*self.spin[(i+2)%3]/self.inertia[i] for i in range(3))
        ra=rotate(self.q0,cross(dw,self.lever))
        rc=rotate(self.q0,cross(self.spin,cross(self.spin,self.lever)))
        def projection(z): return dot(self.up,self.body_jet(self.alpha,z,zero,zero)[0])
        p=sub(add(self.xalpha,lever),er)
        v=sub(add(self.plus,rv),ev)
        rotating=self.body_jet(self.alpha,p,v,zero)
        return dict(firstCOM=projection(self.plus),firstFeatureRotation=projection(rv),firstEarthTranslation=projection(scale(ev,-1)),
                    firstEarthOrientation=dot(self.up,self.body_jet(self.alpha,p,zero,zero)[1]),
                    secondCOM=projection(self.acc),secondAngular=projection(ra),secondCentripetal=projection(rc),
                    secondEarthTranslation=projection(scale(ea,-1)),secondEarthOrientation=dot(self.up,rotating[2]))


def strings(value):
    if isinstance(value,dict):return {k:strings(v) for k,v in value.items()}
    if isinstance(value,(list,tuple)):return [strings(v) for v in value]
    if isinstance(value,mp.mpf):return mp.nstr(value,55)
    return value


def exported_model(p):
    s,a,r=p['FrozenSourceTranslation'],p['FrozenSourceRotation'],p['region']
    return dict(name='force' if p['force'] else 'coast',position=s['PositionRoot'],velocity=s['VelocityRoot'],force=s['ConstantForceRoot'],
        mass=p['Properties']['MassKilograms'],epochTicks=s['Epoch']['Ticks'],startTicks=p['SourceStart']['Ticks'],targetTicks=p['SourceEnd']['Ticks'],
        earthPosition=p['seed']['Position'],earthVelocity=p['seed']['Velocity'],mu=p['GravitationalParameter'],earthOrientation=p['orientation'],
        lever=p['feature'],up=r['Up'],east=r['East'],north=r['North'],radius=r['RadiusMetres'],planeAltitude=r['PlaneAltitudeMetres'],
        innerEast=r['InnerEastMetres'],innerNorth=r['InnerNorthMetres'],plusVelocity=p['InitialLinearVelocityRoot'],plusSpin=p['InitialAngularVelocityBody'],
        inertia=a['PrincipalInertia'],quaternion=a['OrientationLocalToParent'],rootLower=p['PoseRootEnclosure']['Lower'],rootUpper=p['PoseRootEnclosure']['Upper'])


def analyze(p,precision,degree):
    mp.mp.dps=precision
    m=Physics(p,degree)
    duration=m.target-m.alpha
    initial=m.post(0)[0]
    samples=[]
    for sigma in [mp.mpf(0),mp.mpf(1)/64,mp.mpf(1)/32]+[mp.mpf(k)/32 for k in range(2,33)]:
        s=sigma*duration; jet,body,q,w=m.post(s)
        projection=dot(m.up,body[0])
        samples.append(dict(sigma=sigma,elapsed=s,gap=jet[0],first=jet[1],second=jet[2],quaternionNormDefect=dot(q,q)-1,
                            gradingEast=m.radius*dot(m.east,body[0])/projection,gradingNorth=m.radius*dot(m.north,body[0])/projection))
    tiny=[]
    for k in (4,6,8,10,12,14,16,18):
        s=mp.mpf(10)**-k
        tiny.append(dict(elapsed=s,jet=m.post(s)[0]))
    beta=None
    # Independent diagnostic sign-change location; neither this estimate nor sampling grants authority.
    if initial[1]>0 and initial[2]<0:
        estimate=-2*initial[1]/initial[2]
        if 0<estimate<duration:
            root=mp.findroot(lambda s:m.post(s)[0][0],(estimate*mp.mpf('.8'),estimate*mp.mpf('1.2')))
            beta=dict(elapsed=root,sigma=root/duration,time=m.alpha+root,jet=m.post(root)[0],
                      before=m.post(root/2)[0],after=m.post(root*mp.mpf('1.5'))[0])
    return strings(dict(name=p['name'],precision=precision,degree=degree,alpha=m.alpha,alphaInExportedBracket=exact(p['rootLower'])<=m.alpha<=exact(p['rootUpper']),
                        duration=duration,preRootJet=m.pre(m.alpha),initialPostJet=initial,departureComponents=m.departure_components(),
                        samples=samples,tinyTimeSamples=tiny,nextRootEstimate=beta))


if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('model');parser.add_argument('output')
    args=parser.parse_args()
    paths=[Path(args.model)/f'{name}-input.json' for name in ('coast','force')]
    models=[exported_model(json.loads(p.read_text(encoding='utf-8-sig'))) for p in paths]
    results=[]
    for precision,degree in ((60,40),(90,64)):
        for p in models:results.append(analyze(p,precision,degree))
    Path(args.output).write_text(json.dumps(dict(disclaimer='High precision diagnostic reference, not interval certification',
        inputSha256={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in paths},results=results),indent=2)+'\n')
