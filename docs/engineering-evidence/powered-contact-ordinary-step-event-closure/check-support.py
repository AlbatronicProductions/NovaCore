"""Read-only completion of frozen source/support gates from captured native values.

Added after centered-baseline-8; uses existing 1 mm and orientation bars.
No rerun, result rewrite, source-pose reset, error subtraction, or tolerance.
"""
import json, math, sys
from pathlib import Path

failed = False
for path in map(Path, sys.argv[1:]):
    r = json.loads(path.read_text())
    source, end = r['snapshots'][0], r['snapshots'][-1]
    q = source['orientation']
    values = dict(
        source_orientation_error=2*math.atan2(math.sqrt(sum(x*x for x in q[:3])), abs(q[3])),
        source_signed_corner_gap=-source['penetration'],
        endpoint_signed_corner_gap=-end['penetration'],
        minimum_normal_impulse=r['observation']['minimumNormalImpulse'])
    failures = []
    if values['source_orientation_error'] > r['bars']['orientation_error']:
        failures.append('source_orientation_error')
    for field in ('source_signed_corner_gap', 'endpoint_signed_corner_gap'):
        if values[field] > r['bars']['penetration']:
            failures.append(field)
    if values['minimum_normal_impulse'] < 0:
        failures.append('minimum_normal_impulse')
    if not all(math.isfinite(x) for x in values.values()):
        failures.append('nonfinite')
    print(json.dumps(dict(file=path.name, values=values, failures=failures)))
    failed |= bool(failures)
sys.exit(int(failed))
