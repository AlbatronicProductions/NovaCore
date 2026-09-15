"""Saved-input identity and algebraic prediction only; never invokes powered Prepare/solve."""
from pathlib import Path
import json,struct
root=Path(__file__).parent;old=root.parent/'friction-boundary-continuation/results'
def load(name):return json.loads((old/name).read_text(),parse_int=float)
b=load('baseline-historical.json');p=load('powered-historical.json')
def bits(x):
    if isinstance(x,float):return struct.pack('>d',x).hex()
    if isinstance(x,list):return [bits(v) for v in x]
    if isinstance(x,dict):return {k:bits(v) for k,v in x.items()}
    return x
checks={k:bits(b['seed'][k])==bits(p['seed'][k]) for k in ['Identity','Generation','Piece','ProducingGeometry','Cache','Endpoint']}
checks['currentGeometry']=bits(b['current'])==bits(p['current'])
assert all(checks.values())
out=dict(status='PREDICTION ONLY',bitwiseInputChecks=checks,baselineOldLoad=b['seed']['Load'],poweredOldLoad=p['seed']['Load'],
    exactDuration='Each prior installed exact2^-1075; same following exact16666/1000000 minus2^-1075',
    equation='Same current coast geometry, source, mass, duration and load imply identical K/b/reference.',
    preparation='The different old load adds only a common normal shift s*q. D(x+s*q)=D(x) in exact arithmetic.',
    prediction='Same tangent mismatch/coupling and approximately same finite-sweep errors; represented common-shift/D cancellation can differ by rounding. No claim of measured powered equality.',
    noDPrediction='Not equal by this argument: without D the common old-load shift survives.',poweredSolverCalls=0)
with (root/'powered-prediction.json').open('x') as f:json.dump(out,f,indent=2)
print(json.dumps(out,indent=2))
