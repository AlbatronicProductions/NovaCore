"""Lead-run current M13.5 cached-key parity; no build, deployment or deletion.

python -B parity.py florida NEW_LABEL
python -B parity.py inland ANOTHER_NEW_LABEL

Requires capture.py private-host integration. A/B use one identical private
binary/shader set and toggle ONLY NOVACORE_EXIT_CACHED_KEYS. One baseline repeat
is allowed on mismatch; an A/A mismatch NEVER waives an A/B failure. All inputs,
full64-byte prepared values, oriented index multiplicity and D32/HDR/image bytes
are checked at frame175. One raw slot is reused; capture timing is excluded.
"""
import argparse
import hashlib
import json
import pathlib
import sys
import time

sys.dont_write_bytecode = True
import numpy as np
import capture

WIDTH, HEIGHT, FRAME = 3440, 1440, 175
NAMES = ('pixels.bin', 'prepared.bin', 'selected.bin')
FIELDS = ('generation', 'incomingGeneration', 'currentTopology',
          'incomingTopology', 'cameraBits', 'gpuBits', 'presentationBits',
          'lightingBits', 'publishedBits', 'incomingBits', 'cursor', 'active',
          'completeDependencies')


def sha(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b''):
            h.update(chunk)
    return h.hexdigest()


def fields(line, marker):
    return dict(part.strip().split('=', 1)
                for part in line.split(marker, 1)[1].split(';') if '=' in part)


def columnar_rows(value):
    return [{**value.get('constants', {}),
             **{key: column[i] for key, column in value.get('columns', {}).items()}}
            for i in range(value['count'])]


def run_capture(pose,label,cached):
    capture.guard_raw_slot(create=True)
    paths={name:(capture.RAW/name).resolve() for name in NAMES}
    assert all(path.parent==capture.RAW.resolve() for path in paths.values())
    before_mtimes={name:path.stat().st_mtime_ns if path.exists() else None for name,path in paths.items()}
    started_ns=time.time_ns()
    result=capture.fixed(pose,label,cached=cached)
    assert result['exitCode'] == 0 and not result['errors']
    records = [fields(line, 'Pixel parity:') for line in result['lines']
               if 'Pixel parity:' in line]
    assert len(records) == 1, ('capture marker count', records)
    record = records[0]
    assert (int(record['frame']), int(record['width']), int(record['height'])) == (FRAME, WIDTH, HEIGHT)
    vertices = int(record['vertices'])
    assert vertices > 0
    file_info = {}
    for name, path in paths.items():
        stat = path.stat()
        assert stat.st_mtime_ns != before_mtimes[name], ('stale capture file', name)
        # Timestamp check supplements the marker and exact file-length checks.
        # Allow one second for filesystem timestamp granularity.
        assert stat.st_mtime_ns >= started_ns - 1_000_000_000, ('capture predates launch', name)
        file_info[name] = {'bytes': stat.st_size, 'sha256': sha(path)}
    assert file_info['pixels.bin']['bytes'] == WIDTH * HEIGHT * 16
    assert file_info['prepared.bin']['bytes'] == vertices * 64
    selected_bytes = paths['selected.bin'].read_bytes()
    assert len(selected_bytes) > 0 and len(selected_bytes) % 12 == 0
    triangles = np.frombuffer(selected_bytes, dtype='<u4').reshape(-1, 3)
    assert int(triangles.max()) < vertices, 'selected index exceeds prepared vertex count'
    order = np.lexsort((triangles[:, 2], triangles[:, 1], triangles[:, 0]))
    oriented_hash = hashlib.sha256(triangles[order].tobytes()).hexdigest()
    # Keep winding and multiplicity; never sort the three indices within a triangle.
    correlation = [fields(line, 'Composition frame:') for line in result['lines']
                   if 'Composition frame:' in line]
    correlation = [row for row in correlation if int(row['frame']) == FRAME]
    assert len(correlation) == 1, ('frame input identity missing/ambiguous', len(correlation))
    inputs = {key: correlation[0][key] for key in FIELDS}
    assert inputs['generation'] == record['generation']
    directional = [row for row in columnar_rows(result['directionalRows'])
                   if row['submittedFrame'] == FRAME and row['timingFrame'] == FRAME]
    assert len(directional) == 1
    geometry = {key: directional[0][key] for key in
                ('tcsPatches', 'refinedVertices', 'compactedTriangles',
                 'maximumOuterTesFactor', 'maximumInnerTesFactor')}
    report = {'label': label, 'cachedKeys': cached, 'capture': record,
              'inputs': inputs, 'geometry': geometry, 'files': file_info,
              'selectedOrientedMultisetSha256': oriented_hash,
              'selectedTriangles': len(triangles),
              'runtime': {key: result[key] for key in
                          ('nativeHash', 'managedHash', 'shaderHashes', 'args', 'cwd', 'env')},
              'runJournal': label + '.json',
              'runJournalSha256': sha(capture.HERE / (label + '.json')),
              'keyMemory': [line for line in result['lines'] if 'Poll memory type:' in line]}
    return report, paths['pixels.bin'].read_bytes()


def compare(a, ab, b, bb):
    differences = {}
    for key in ('nativeHash', 'managedHash', 'shaderHashes', 'args', 'cwd'):
        if a['runtime'][key] != b['runtime'][key]:
            differences['runtime.' + key] = [a['runtime'][key], b['runtime'][key]]
    # Only the cached-key preference differs; every other environment value must match.
    def comparable_env(r):
        return {k: v for k, v in r['runtime']['env'].items()
                if k not in ('NOVACORE_EXIT_CACHED_KEYS',)}
    if comparable_env(a) != comparable_env(b):
        differences['environment'] = [comparable_env(a), comparable_env(b)]
    for category in ('inputs', 'capture', 'geometry'):
        for key in a[category].keys() | b[category].keys():
            if a[category].get(key) != b[category].get(key):
                differences[category + '.' + key] = [a[category].get(key), b[category].get(key)]
    n = WIDTH * HEIGHT
    attachments = {}
    for name, begin, end, dtype, channels in (
            ('depth', 0, n * 4, '<f4', 1),
            ('hdr', n * 4, n * 12, '<f2', 4),
            ('image', n * 12, n * 16, 'u1', 4)):
        xbytes, ybytes = memoryview(ab)[begin:end], memoryview(bb)[begin:end]
        x = np.frombuffer(xbytes, dtype=dtype).reshape(n, channels)
        y = np.frombuffer(ybytes, dtype=dtype).reshape(n, channels)
        xb = np.frombuffer(xbytes, dtype='u1').reshape(n, -1)
        yb = np.frombuffer(ybytes, dtype='u1').reshape(n, -1)
        changed, finite, max_delta, examples = 0, True, 0.0, []
        for first in range(0, n, 65536):
            last = min(n, first + 65536)
            mask = np.any(xb[first:last] != yb[first:last], axis=1)
            changed += int(mask.sum())
            finite = finite and bool(np.isfinite(x[first:last]).all() and np.isfinite(y[first:last]).all())
            delta = np.abs(x[first:last].astype(np.float64) - y[first:last].astype(np.float64))
            valid = delta[np.isfinite(delta)]
            if valid.size:
                max_delta = max(max_delta, float(valid.max()))
            for at in np.flatnonzero(mask)[:max(0, 16 - len(examples))]:
                pixel = first + int(at)
                examples.append({'x': pixel % WIDTH, 'y': pixel // WIDTH,
                                 'beforeBytes': xb[pixel].tolist(), 'afterBytes': yb[pixel].tolist()})
        attachments[name] = {'equal': changed == 0, 'differentPixels': changed,
                             'finite': finite, 'maxFiniteAbsoluteDifference': max_delta,
                             'beforeSha256': hashlib.sha256(xbytes).hexdigest(),
                             'afterSha256': hashlib.sha256(ybytes).hexdigest(), 'examples': examples}
    prepared = a['files']['prepared.bin'] == b['files']['prepared.bin']
    selected = (a['selectedTriangles'] == b['selectedTriangles'] and
                a['selectedOrientedMultisetSha256'] == b['selectedOrientedMultisetSha256'])
    passed = not differences and prepared and selected and all(
        value['equal'] and value['finite'] for value in attachments.values())
    return {'pass': passed, 'inputDifferences': differences,
            'preparedFull64ByteExact': prepared, 'selectedOrientedMultisetExact': selected,
            'selectedBufferOrderExact': a['files']['selected.bin'] == b['files']['selected.bin'],
            'attachments': attachments}


def run(pose, label):
    assert label and all(c.isalnum() or c in '-_' for c in label)
    assert not any((capture.HERE/(label+'-parity'+ext)).exists() for ext in ('.json','.json.gz'))
    output = capture.HERE / (label + '-parity.json')
    assert not output.exists(), 'refusing to overwrite retained evidence'
    names = [label + '-capture-' + suffix for suffix in ('base', 'cached', 'aa')]
    assert all(not (capture.HERE/(name+ext)).exists() for name in names for ext in ('.json','.json.gz'))
    a, ap = run_capture(pose, names[0], False)
    b, bp = run_capture(pose, names[1], True)
    ab = compare(a, ap, b, bp)
    report = {'schema': 1, 'pose': pose, 'frame': FRAME, 'width': WIDTH, 'height': HEIGHT,
              'baseline': a, 'cachedKeys': b, 'ab': ab, 'pass': ab['pass'],
              'baselineRepeatRun': False, 'rawCaptureSlot': str(capture.RAW.resolve()),
              'limits': ['Capture-enabled timings are excluded from performance evidence.',
                         'One fixed frame does not qualify transitions or resource lifetime negatives.',
                         'Prepared SHA256 covers all 64 bytes per current vertex, not incoming unpublished buffers.',
                         'Oriented triangle multiset ignores only compaction order, not winding or multiplicity.',
                         'D32 plus full HDR/image bytes do not record each individual post-TES vertex.',
                         'A/A variation is evidence to investigate, never an automatic A/B mismatch waiver.',
                         'Raw slot remains for lead cleanup; no bulk archive retained by this helper.']}
    if not ab['pass']:
        aa, aap = run_capture(pose, names[2], False)
        report.update(baselineRepeatRun=True, baselineRepeat=aa,
                      aa=compare(a, ap, aa, aap), repeatBaselineVsCached=compare(aa, aap, b, bp))
        report['pass'] = False
    output.write_text(json.dumps(report, indent=2, allow_nan=False) + '\n', encoding='utf-8')
    print(json.dumps({'report': str(output), 'pass': report['pass'],
                      'baselineRepeatRun': report['baselineRepeatRun']}), flush=True)
    return report


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('pose', choices=('florida', 'inland'))
    parser.add_argument('label')
    options = parser.parse_args()
    result=run(options.pose, options.label)
    raise SystemExit(0 if result['pass'] else 1)
