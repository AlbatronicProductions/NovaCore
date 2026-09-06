"""Bounded frame-aligned performance evidence; no bulk GPU capture required."""
import hashlib
import json
import math
import re
from pathlib import Path


def fields(line):
    result = dict(re.findall(r'(\w+)=([^;]+)', line))
    for key, value in result.items():
        try:
            result[key] = float(value) if any(c in value for c in '.eE') else int(value)
        except ValueError:
            pass
    return result


def stats(values):
    values = sorted(values)
    if not values:
        return None
    return dict(n=len(values), median=values[math.ceil(.5*len(values))-1],
                p95=values[math.ceil(.95*len(values))-1], minimum=values[0], maximum=values[-1])


def analyze(text, name, count=100):
    lines = text.splitlines()
    frames = [fields(x) for x in lines if 'P2S5F directional visibility:' in x][-count:]
    if len(frames) != count or any(x['submittedFrame'] != x['timingFrame'] for x in frames):
        raise ValueError(f'{name}: missing or mismatched frame queries')
    cpu = {v['frame']: v for x in lines if 'Performance CPU frame:' in x for v in [fields(x)]}
    submit = {v['sample']: v for x in lines if 'Performance CPU submission:' in x for v in [fields(x)]}
    metrics = {key: stats([x[key] for x in frames]) for key in frames[0]
               if isinstance(frames[0][key], (float, int))}
    geometry = {v['frame']:v for x in lines if 'Performance geometry frame:' in x for v in [fields(x)]}
    metrics['clippingInputPrimitives'] = stats([geometry[x['submittedFrame']]['clippingInputPrimitives'] for x in frames if x['submittedFrame'] in geometry])
    # Fence wait in update N+1 waits for the work submitted in frame N.
    for key in ('fenceWait', 'update'):
        metrics[key] = stats([cpu[x['submittedFrame']+1][key] for x in frames if x['submittedFrame']+1 in cpu])
    for key in ('record', 'submit', 'present'):
        metrics[key] = stats([submit[x['submittedFrame']][key] for x in frames if x['submittedFrame'] in submit])
    preparation = {}
    for kind in ('demand', 'incomingPhysicalPreparation'):
        values = [fields(x)['ms'] for x in lines if f'NCSM1 regional GPU work: kind={kind};' in x]
        preparation[kind] = dict(sumMs=sum(values), slices=stats(values))
    useful = [x for x in lines if any(k in x for k in (
        'directional visibility PASS', 'regional ready:', 'regional demand:', 'regional physical totals:',
        'spherical billboard publication:', 'pupil identity:', 'frame timing: publication', 'Florida seating:',
        'CPU timings:', 'VUID-', 'validation error', 'directional pose:'))]
    return dict(name=name, logSha256=hashlib.sha256(text.encode()).hexdigest(),
                rawUtf8Bytes=len(text.encode()), samples=len(frames), metrics=metrics,
                firstFrame=frames[0], lastFrame=frames[-1], preparation=preparation,
                provenance=useful, preparedGeometry=[fields(x) for x in lines if 'Performance prepared geometry:' in x])


if __name__ == '__main__':
    import sys
    output = Path(sys.argv[1])
    result = [analyze(Path(p).read_text(encoding='utf-8'), Path(p).stem) for p in sys.argv[2:]]
    output.write_text(json.dumps(result, indent=2)+'\n', encoding='utf-8')
