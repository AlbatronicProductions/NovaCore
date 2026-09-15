"""Exact discrete row-energy accounting. Reads retained native bits, never steps BEPU."""
import hashlib, json, runpy, struct, sys
from fractions import Fraction as Q
from pathlib import Path

HERE = Path(__file__).resolve().parent
PRIOR = HERE.parent
CHRONO = PRIOR.parent / 'powered-contact-chronological-event-feasibility' / 'chronology.py'
BAR = Q('0.07848')
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def bitq(b): return Q(struct.unpack('<f', struct.pack('<i', b))[0])
def energy(r):
    v = [bitq(b) for b in r[3:9]]
    inverse = [bitq(b) for b in r[9:16]]
    a,b,d,c,e,f = inverse[1:]
    det = a*(d*f-e*e)-b*(b*f-c*e)+c*(b*e-c*d)
    assert inverse[0]>0 and a>0 and a*d-b*b>0 and det>0, 'Nonpositive represented metric'
    # Invert the COMPLETE represented symmetric tensor exactly. Even spherical
    # source inertia can acquire tiny nonzero entries in its FP32 world transform.
    xx,xy,xz,yy,yz,zz = d*f-e*e,c*e-b*f,b*e-c*d,a*f-c*c,b*c-a*e,a*d-b*b
    x,y,z = v[3:]
    angular = (xx*x*x+yy*y*y+zz*z*z+2*(xy*x*y+xz*x*z+yz*y*z))/det
    return sum(x*x for x in v[:3])/(2*inverse[0]) + angular/2

def check(raw_path, trace_path):
    raw = json.loads(raw_path.read_text())
    old_path = PRIOR / (raw['name']+'-8.json')
    old = json.loads(old_path.read_text())
    # Every native outcome, original reference, cache, contact, bit and ledger;
    # only the copied assembly directory is different. Its hash is still pinned.
    old['binaryIdentity']['bepuPath'] = raw['binaryIdentity']['bepuPath']
    assert raw == old, 'Diagnostic native result differs from retained original'
    assert raw['binaryIdentity']['bepuSha256'] == '77185E513BD530DEB322E41DD4ACDA9AD8E32E626CFA19492FB7481F25ED1FA7'
    assert not raw['failures'] and raw['iterations'] == 8 and raw['substeps'] == 1
    rows = json.loads(trace_path.read_text())['rows']
    assert len(rows) == 108
    order = [(0,r) for r in range(6)] + [(p,r) for p in range(1,9) for r in [1,2,3,4,0,5]]
    sums = [Q(0) for _ in range(6)]
    warm = [Q(0) for _ in range(6)]
    impulses = [Q(0) for _ in range(7)]
    corrections = [Q(0) for _ in range(7)]
    variation = [Q(0) for _ in range(7)]
    positive = [0]*6
    for idx,(p,r) in enumerate(order):
        a,b = rows[2*idx:2*idx+2]
        assert a[:3] == [p,r,0] and b[:3] == [p,r,1]
        assert len(a)==len(b)==23 and a[9:16]==b[9:16]==rows[0][9:16]
        if idx: assert rows[2*idx-1][3:] == a[3:], 'Unobserved state change between rows'
        channels = [4,5] if r==0 else [6] if r==5 else [r-1]
        for j in range(7):
            if j not in channels: assert a[16+j]==b[16+j], 'Another impulse channel changed'
        if p==0: assert a[16:]==b[16:], 'Warm start changed cache'
        for j in channels:
            delta = bitq(b[16+j]) if p==0 else bitq(b[16+j])-bitq(a[16+j])
            impulses[j] += delta
            if p: corrections[j] += delta
            variation[j] += abs(delta)
        dk = energy(b)-energy(a)
        sums[r] += dk
        if p==0: warm[r] += dk
        if dk>0: positive[r] += 1
    assert impulses == [bitq(b) for b in rows[-1][16:]], 'Warm plus signed corrections did not telescope'
    assert sum(sums) == energy(rows[-1])-energy(rows[0]), 'Energy did not telescope'
    assert rows[-1][3:9] == raw['snapshots'][-1]['bits'][7:13], 'Trace differs from actual final velocity bits'
    assert rows[0][16:] == raw['snapshots'][1]['bits'][13:20], 'Observed warm cache differs from native source cache'

    assert sha(CHRONO) == 'FA73A09F15DBC6F74CE0FFC3516D3B3BB2F0867C513AA4034CB4A6616F4D42F1'
    ns = runpy.run_path(str(CHRONO))  # definitions only; no execute(), world or timing
    s = {k:Q(raw['input'][k]) for k in ('h','q','m0','k','c','fx','fy','e','v0','w0','fuel')}
    if raw['input']['baseline']:
        H,G,I=ns['H'],ns['G'],ns['I']
        x=s['v0']*H-s['k']*G*H*H/2
        angle=s['w0']*H-s['c']*G*s['m0']/I*H*H/2
        wt=-s['k']*G*s['m0']*x
        ww=-s['c']*G*s['m0']*angle
        tail=Q(0)
    else:
        reference,bounds,_=ns['ref'](s)
        wt,ww=reference['work_t'],reference['work_twist']
        tail=bounds['work_t']
        assert tail<Q(1,10**45)
    errors=[abs(sums[0]-wt)+tail,abs(sums[5]-ww)]
    assert all(x<BAR for x in errors), 'Frozen separate work bar failed'
    return dict(name=raw['name'],definition='ORDERED_DISCRETE_IMPULSE_WORK_APPROXIMATION',
        originalNativeValuesEqual=True,excludedIdentityField='binaryIdentity.bepuPath only',
        applications=54,observations=108,energyTelescopeExact=True,impulseTelescopeExact=True,
        workTangent=float(sums[0]),workTwist=float(sums[5]),normalEnergy=float(sum(sums[1:5])),
        totalContactEnergy=float(sum(sums)),warmTangent=float(warm[0]),warmTwist=float(warm[5]),
        referenceTangent=float(wt),referenceTwist=float(ww),tangentReferenceTail=float(tail),
        tangentErrorUpper=float(errors[0]),twistError=float(errors[1]),bar=float(BAR),
        workPass=True,positiveEnergyApplicationsByRow=positive,
        exactEnergyByRow=[str(x) for x in sums],exactImpulseTotal=[str(x) for x in impulses],
        signedCorrectiveImpulse=[float(x) for x in corrections],impulseTotalVariation=[float(x) for x in variation],
        sourceRawSha256=sha(raw_path),retainedOriginalSha256=sha(old_path),traceSha256=sha(trace_path))

if __name__=='__main__':
    result=check(Path(sys.argv[1]),Path(sys.argv[2]))
    print(json.dumps(result,indent=2))
    if len(sys.argv)>3: Path(sys.argv[3]).write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
