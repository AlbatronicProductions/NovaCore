"""Deterministic, read-only verification and compact summary of retained run results."""
from pathlib import Path
from fractions import Fraction
import hashlib, json, math

root=Path(__file__).resolve().parent
names=['centered-baseline','low-thrust','sliding-baseline','near-unloading',
       'yaw-baseline','off-com','near-end','mid-event','tiny-event','zero-event']
runs={name:json.loads((root/(name+'-8.json')).read_text()) for name in names}
repeats=[json.loads((root/f'off-com-repeat{n}.json').read_text()) for n in (1,2)]
rows=[]
for name,r in runs.items():
    source,refresh,solved,end=r['snapshots']
    q=source['orientation']
    tilt=2*math.atan2(math.sqrt(sum(x*x for x in q[:3])),abs(q[3]))
    checks=dict(
        source_tilt=tilt<=r['bars']['orientation_error'],
        source_gap=-source['penetration']<=r['bars']['penetration'],
        endpoint_gap=-end['penetration']<=r['bars']['penetration'],
        nonnegative_rows=r['observation']['minimumNormalImpulse']>=0,
        measured_bars=not r['failures'],
        unchanged_pre_solve_cache=source['impulses']==refresh['impulses'],
        unchanged_post_solve_cache=solved['impulses']==end['impulses'],
        same_retained_handles=all(s['handle']==source['handle'] and s['body']==source['body'] and s['slab']==source['slab'] for s in r['snapshots']),
        same_contact_features=all([c['feature'] for c in s['contacts']]==[c['feature'] for c in source['contacts']] for s in r['snapshots']),
        ordinary_backend_step=r['backendSteps']==r['substeps']==1 and r['backendDtBits']==1015580809,
        exact_duration_partition=Fraction(r['ledger']['hp'])+Fraction(r['ledger']['hc'])==Fraction(r['ledger']['H']),
        exact_debit=r['input']['baseline'] or Fraction(r['input']['q'])*Fraction(r['ledger']['hp'])==Fraction(r['ledger']['fuel']),
        no_publication=r['ledger']['canonicalPublications']==0)
    assert all(checks.values()), (name,checks)
    rows.append(dict(file=name+'-8.json',checks=checks,source_tilt=tilt,
                     source_gap=-source['penetration'],endpoint_gap=-end['penetration'],
                     min_row=r['observation']['minimumNormalImpulse']))
for r in repeats:
    assert r==runs['off-com'], 'Off-COM full retained result differs'
tiny,zero=runs['tiny-event'],runs['zero-event']
assert Fraction(tiny['ledger']['hp'])==Fraction(1,2**1075)>0
assert Fraction(tiny['ledger']['fuel'])==Fraction(1,2**1074)
assert Fraction(zero['ledger']['hp'])==0
assert tiny['snapshots']==zero['snapshots'], 'Represented tiny/zero body/cache differs'
assert tiny['prepared']['linearAcceleration']==zero['prepared']['linearAcceleration']
assert tiny['prepared']['angularAcceleration']==zero['prepared']['angularAcceleration']
assert tiny['ledger']!=zero['ledger'], 'Exact event authority collapsed'
out=dict(judgment='REVISE',observed_physical_gates='PASS: ten rows and two off-COM repeats',
         work='UNQUALIFIED: endpoint impulse/slip proxies do not certify chronological work',
         cost='NOT RUN: full correctness gate not satisfied',rows=rows,
         deterministic_repeat='Original plus two fresh processes: complete JSON values identical',
         tiny_zero='Distinct exact ledgers, identical native snapshots and projected accelerations',
         binaries=runs['off-com']['binaryIdentity'],
         maximum_errors={k:max(r['measured'][k] for r in runs.values()) for k in runs['off-com']['measured']},
         plan_hash=hashlib.sha256((root/'plan.md').read_bytes()).hexdigest().upper(),
         inputs_hash=hashlib.sha256((root/'inputs.json').read_bytes()).hexdigest().upper(),
         source_hash=hashlib.sha256((root/'Program.cs').read_bytes()).hexdigest().upper())
(root/'results-summary.json').write_text(json.dumps(out,separators=(',',':'))+'\n')
print(json.dumps({k:v for k,v in out.items() if k not in ('rows',)},indent=2))
