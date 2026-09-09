"""Lead-run implemented-default check against this ticket's sealed A/B proof.

python -B candidate_parity.py florida candidate-florida
python -B candidate_parity.py inland candidate-inland

Requires a freshly built private-candidate-capture-host.json with the same
restoration/capture fields as private-capture-host.json. No build or production
deployment is performed here. NOVACORE_EXIT_CACHED_KEYS must be absent: this
qualifies the implemented default, not the opt-in diagnostic control.
"""
import argparse
import gzip
import hashlib
import inspect
import json
from pathlib import Path
import sys

sys.dont_write_bytecode = True
import numpy as np
import parity as p


def read_reference(pose, filename=None):
    name = filename or ('keys-' + pose + '-parity.json')
    assert Path(name).name == name and name.endswith(('.json', '.json.gz'))
    path = p.capture.HERE / name
    if not path.exists() and name.endswith('.json'):
        path = p.capture.HERE / (name + '.gz')
    assert path.is_file() and path.resolve().parent == p.capture.HERE.resolve()
    raw = path.read_bytes()
    report = json.loads(gzip.decompress(raw) if path.suffix == '.gz' else raw)
    assert report['pose'] == pose and report['frame'] == p.FRAME
    assert (report['width'], report['height']) == (p.WIDTH, p.HEIGHT)
    assert report['pass'] and report['ab']['pass'], 'reference A/B must have passed'
    assert report['ab']['preparedFull64ByteExact'] and report['ab']['selectedOrientedMultisetExact']
    assert all(r['equal'] and r['finite'] for r in report['ab']['attachments'].values())
    # Cross-check the attachment digests sealed by the same-host A/B report.
    assert all(r['beforeSha256'] == r['afterSha256'] for r in report['ab']['attachments'].values())
    return path, raw, report


def select_candidate_manifest():
    """Reuse current capture.fixed, changing only its private manifest selection."""
    source = inspect.getsource(p.capture.fixed)
    assert source.count("HERE/'private-capture-host.json'") == 1
    source = source.replace('def fixed(', 'def _candidate_fixed(', 1)
    source = source.replace("HERE/'private-capture-host.json'", "HERE/'private-candidate-capture-host.json'", 1)
    exec(source, p.capture.__dict__)
    p.capture.fixed = p.capture._candidate_fixed


def compare_reference(reference, actual, pixels):
    baseline = reference['baseline']
    differences = {}
    for category in ('inputs', 'capture', 'geometry'):
        for key in baseline[category].keys() | actual[category].keys():
            if baseline[category].get(key) != actual[category].get(key):
                differences[category + '.' + key] = [baseline[category].get(key), actual[category].get(key)]
    # A new native hash is expected. Managed fixed-pose driver and deployed
    # shaders must be identical; all other execution inputs also remain fixed.
    for key in ('managedHash', 'shaderHashes', 'args', 'cwd', 'env'):
        if baseline['runtime'][key] != actual['runtime'][key]:
            differences['runtime.' + key] = [baseline['runtime'][key], actual['runtime'][key]]
    assert 'NOVACORE_EXIT_CACHED_KEYS' not in actual['runtime']['env']
    assert len(pixels) == p.WIDTH * p.HEIGHT * 16
    n = p.WIDTH * p.HEIGHT
    attachments = {}
    for name, begin, end, dtype in (
            ('depth', 0, n * 4, '<f4'),
            ('hdr', n * 4, n * 12, '<f2'),
            ('image', n * 12, n * 16, 'u1')):
        data = memoryview(pixels)[begin:end]
        digest = hashlib.sha256(data).hexdigest()
        expected = reference['ab']['attachments'][name]['beforeSha256']
        attachments[name] = {'equalToPreChange': digest == expected,
                             'referenceSha256': expected, 'candidateSha256': digest,
                             'finite': bool(np.isfinite(np.frombuffer(data, dtype=dtype)).all())}
    prepared = actual['files']['prepared.bin'] == baseline['files']['prepared.bin']
    selected = (actual['selectedTriangles'] == baseline['selectedTriangles'] and
                actual['selectedOrientedMultisetSha256'] == baseline['selectedOrientedMultisetSha256'])
    passed = not differences and prepared and selected and all(
        value['equalToPreChange'] and value['finite'] for value in attachments.values())
    return {'pass': passed, 'inputDifferences': differences,
            'preparedFull64ByteExact': prepared, 'selectedOrientedMultisetExact': selected,
            'selectedBufferOrderExact': actual['files']['selected.bin'] == baseline['files']['selected.bin'],
            'attachments': attachments,
            'nativeHashChange': {'preChange': baseline['runtime']['nativeHash'],
                                 'implementedDefault': actual['runtime']['nativeHash']}}


def run(pose, label, reference_name=None):
    assert pose in ('florida', 'inland')
    assert label and all(c.isalnum() or c in '-_' for c in label)
    output = p.capture.HERE / (label + '-parity.json')
    assert all(not (p.capture.HERE / (label + suffix)).exists() for suffix in
               ('-parity.json', '-parity.json.gz', '-capture.json', '-capture.json.gz'))
    reference_path, reference_bytes, reference = read_reference(pose, reference_name)
    select_candidate_manifest()
    actual, pixels = p.run_capture(pose, label + '-capture', False)
    comparison = compare_reference(reference, actual, pixels)
    manifest = p.capture.HERE / 'private-candidate-capture-host.json'
    report = {'schema': 1, 'pose': pose, 'frame': p.FRAME,
              'width': p.WIDTH, 'height': p.HEIGHT, 'pass': comparison['pass'],
              'actual': actual, 'comparison': comparison,
              'preChangeReport': reference_path.name,
              'preChangeReportSha256': hashlib.sha256(reference_bytes).hexdigest(),
              'candidateManifest': manifest.name, 'candidateManifestSha256': p.sha(manifest),
              'implementationDefault': True, 'performanceTimingsExcluded': True,
              'rawCaptureSlot': str(p.capture.RAW.resolve()),
              'limits': ['Comparison uses complete pre-change SHA256 digests from this ticket; prior raw buffers are not required.',
                         'This fixed-frame proof does not qualify dynamic transitions or resource lifetime negatives.',
                         'No individual post-TES vertex stream is recorded; prepared values, selected triangles and full D32/HDR/image bytes are checked.',
                         'A mismatch fails; no variation allowance or attachment tolerance is applied.']}
    output.write_text(json.dumps(report, indent=2, allow_nan=False) + '\n', encoding='utf-8')
    print(json.dumps({'report': str(output), 'pass': report['pass']}), flush=True)
    return report


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('pose', choices=('florida', 'inland'))
    parser.add_argument('label')
    parser.add_argument('--reference', help='Existing current-ticket parity filename; default keys-POSE-parity.json[.gz].')
    options = parser.parse_args()
    result = run(options.pose, options.label, options.reference)
    raise SystemExit(0 if result['pass'] else 1)
