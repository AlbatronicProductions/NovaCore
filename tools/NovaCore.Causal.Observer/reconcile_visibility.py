"""Read-only schema-2 population audit. Never launches an application or GPU.

Joins counters to their own completed frame; refuses to call aggregate counts
an exact frozen-pupil replay when that session did not record the required data.
"""
import argparse
import collections
import hashlib
import json
import pathlib
import re
import struct


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def real(word):
    return struct.unpack('<d', struct.pack('<Q', word))[0]


def scalar(word):
    return struct.unpack('<f', struct.pack('<I', word & 0xffffffff))[0]


def audit(source, output, model):
    if output.exists() or source == output or source in output.parents:
        raise ValueError('Use a fresh output directory outside the preserved session.')
    blob = (source / 'breadcrumbs.bin').read_bytes()
    header = struct.unpack_from('<16Q', blob)
    if (header[:3] != (0x314c41535541434e, 2, 512)
            or not 0 < header[15] <= 131072 or len(blob) != 128 + header[15]*512):
        raise ValueError('Unexpected journal schema or size')
    records, bad, empty = [], 0, 0
    for offset in range(128, len(blob), 512):
        entry = blob[offset:offset+512]
        words = struct.unpack('<64Q', entry)
        if not words[0]:
            empty += 1
            continue
        check = 14695981039346656037
        for value in entry[:488] + entry[496:504]:
            check = ((check ^ value)*1099511628211) & 0xffffffffffffffff
        if words[0] != words[63] or check != words[61]:
            bad += 1
            continue
        records.append(words)
    records.sort(key=lambda w: w[0])
    gaps = sum(b[0] != a[0]+1 for a, b in zip(records, records[1:]))
    if bad or gaps or not records:
        raise ValueError(f'Unusable journal: bad={bad}, gaps={gaps}, records={len(records)}')
    main, culls, tess, stats, submitted, presented, kinds = {}, {}, {}, {}, set(), {}, collections.Counter()
    def add(target, key, value):
        target.setdefault(key, []).append(value)
    for w in records:
        d, frame, completed = w[9:61], w[2], w[4]
        if w[5:8] == (10, 1, 0):
            submitted.add(w[3])
        if w[5] == 11 and w[6] == 1:
            presented[frame] = w[7]
        if w[5:7] != (14, 2):
            continue
        kinds[d[0]] += 1
        if d[0] == 1:
            add(main, completed, dict(serial=w[0], frame=frame, submitted=w[3], completed=completed,
                viewport=list(d[1:3]), camera=[real(x) for x in d[4:7]], altitude=real(d[7]),
                forward=[real(x) for x in d[8:11]], level=d[13], triangles=d[14], visible=d[15],
                vertices=d[17], publication=d[18], incomingPublication=d[19],
                physicalBuffer=d[20], vertexCapacity=d[23], triangleCapacity=d[24],
                authoritative=bool(d[36])))
        elif d[0] == 3:
            add(culls, completed, dict(serial=w[0], level=d[1], visible=d[2], total=d[3],
                screenRejected=d[4], horizonRejected=d[5], invalid=d[6], overflow=d[7]))
        elif d[0] == 5:
            add(tess, completed, dict(serial=w[0], indices=d[1], outer=scalar(d[2]), inner=scalar(d[3])))
        elif d[0] == 6:
            add(stats, d[1], dict(serial=w[0], queryFrame=d[1], clipping=d[2], fragments=d[3],
                tcs=d[4], tes=d[5]))
    rows, failures = [], []
    for frame in sorted(main.keys() & culls.keys() & tess.keys() & stats.keys()):
        m, c, t, s = main[frame][0], culls[frame][0], tess[frame][0], stats[frame][0]
        # Duplicated snapshots bracket host publication. They may legitimately
        # differ in general; never silently pick one when they do.
        for group, key in ((main, 'main'), (culls, 'cull'), (tess, 'tess')):
            signatures = {json.dumps({k:v for k,v in x.items() if k != 'serial'}, sort_keys=True) for x in group[frame]}
            if len(signatures) != 1:
                failures.append(dict(frame=frame, reason=key+' changed across publication inspection'))
        # The rolling window may begin after this frame's submit/present. Those
        # absent events are unknown, not failed operations or invented successes.
        submission = True if frame in submitted else None
        presentation = presented[frame]==0 if frame in presented else None
        checks = dict(inputAccounted=c['total']==c['visible']+c['screenRejected']+c['horizonRejected']+c['invalid'],
            snapshotMatchesCull=m['visible']==c['visible'] and m['triangles']==c['total'],
            compacted=3*c['visible']==t['indices'], queryMatches=s['tcs']==c['visible'],
            factorOneWork=t['outer']==1 and s['tes']==3*s['tcs'], valid=c['invalid']==0 and c['overflow']==0,
            submitted=submission, presented=presentation)
        if any(value is False for value in checks.values()):
            failures.append(dict(frame=frame, checks=checks))
        rows.append(dict(frame=frame, cameraState=m, cull=c, tessellation=t, gpuStatistics=s, checks=checks))
    if not rows:
        raise ValueError('No joined completed frames')
    legacy = json.loads(model.read_text())
    measurements = [x for x in legacy['results'] if x['Pose']=='recorded-live-horizon']
    pose = next(x for x in legacy['poses'] if x['Name']=='recorded-live-horizon')
    old_camera = [pose['Camera'][x] for x in ('X','Y','Z')]
    camera_matches = [r for r in rows if r['cameraState']['camera']==old_camera]
    log = (source / 'application-session' / 'segment-0.log').read_text(encoding='utf-8-sig')
    slices = re.findall(r'physical slice: incoming=(\d+); generation=18; pupil=(\d+); first=(\d+); count=(\d+); total=(\d+); complete=(\d+)', log)
    publications = re.findall(r'staged pupil publication: generation=18; pupil=(\d+)', log)
    demands = re.findall(r'regional demand: incoming=0; pupil=(\d+); generation=18; required=(\d+); anchoredPatches=\d+; unresolved=(\d+)', log)
    proof_inputs = [dict(path=str(p), bytes=p.stat().st_size, sha256=sha(p)) for p in sorted(source.rglob('*')) if p.is_file()]
    peak = max(rows, key=lambda r:r['cull']['visible'])
    missing = ['published pupil basis and offsets', 'prepared/cull/raster pupil identity in rolling journal',
        'exact mapped camera projection and body orientation', 'exact mapped culling constants',
        'prepared physical vertices and normals', 'compacted triangle membership']
    summary = dict(status='REVISE: historical frozen replay inputs unavailable', liveExposurePerformed=False,
        journal=dict(records=len(records), bad=bad, empty=empty, gaps=gaps, firstSerial=records[0][0],
            lastSerial=records[-1][0], kinds=dict(kinds), retainedSeconds=(records[-1][1]-records[0][1])/header[5]),
        joinedFrames=len(rows), firstFrame=rows[0]['frame'], lastFrame=rows[-1]['frame'], failures=failures,
        coverage=dict(lastSubmitted=max(w[3] for w in records), lastCompleted=max(w[4] for w in records),
            framesWithoutRetainedSubmit=[r['frame'] for r in rows if r['checks']['submitted'] is None],
            framesWithoutRetainedPresent=[r['frame'] for r in rows if r['checks']['presented'] is None],
            completedWithoutJoinedCounters=sorted({w[4] for w in records}-{r['frame'] for r in rows})),
        range=dict(visible=[min(r['cull']['visible'] for r in rows),peak['cull']['visible']],
            publications=sorted({r['cameraState']['publication'] for r in rows}),
            physicalBuffers=sorted({r['cameraState']['physicalBuffer'] for r in rows})),
        peak=peak, last=rows[-1],
        legacyModel=dict(path=str(model), sha256=sha(model), measurements=measurements, pose=pose,
            cameraMatchingCompletedFrames=[r['frame'] for r in camera_matches],
            differences=[dict(frame=r['frame'], height=m['Height'],
                visibleDelta=r['cull']['visible']-m['RetainedUpperBound'],
                horizonRejectedDelta=r['cull']['horizonRejected']-m['HorizonRejected'],
                screenRejectedDelta=r['cull']['screenRejected']-m['ScreenRejected']) for r in camera_matches for m in measurements]),
        lifecycle=dict(incomingPreparationSlices=[list(map(int,x)) for x in slices if x[0]=='1'],
            currentPreparationSliceCount=sum(x[0]=='0' for x in slices), stagedPublications=publications,
            completedDependencyChecks=len(demands), firstDemand=list(map(int,demands[0])), lastDemand=list(map(int,demands[-1])),
            allDemandMasksEmptyAndResolved=all(x[1:] == ('0','0') for x in demands)),
        exactReplay=dict(available=False, missing=missing,
            reason='Publication ordinal and physical buffer handle do not encode pupil contents; reconstructed pupil is a different input.'),
        sources=proof_inputs)
    output.mkdir(parents=True)
    (output/'audit.json').write_text(json.dumps(summary,indent=2)+'\n')
    with (output/'completed-frames.jsonl').open('w') as result:
        for row in rows:
            result.write(json.dumps(row,separators=(',',':'))+'\n')
    print(json.dumps({k:summary[k] for k in ('status','joinedFrames','firstFrame','lastFrame','failures','range','lifecycle')}))
    return 3  # fail closed: aggregate consistency cannot satisfy the frozen replay gate


if __name__ == '__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('source',type=pathlib.Path)
    parser.add_argument('output',type=pathlib.Path)
    parser.add_argument('model',type=pathlib.Path)
    args=parser.parse_args()
    raise SystemExit(audit(args.source.resolve(),args.output.resolve(),args.model.resolve()))
