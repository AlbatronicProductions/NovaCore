"""Compact the already completed qualification. No launch, build or raw mutation."""
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
BUILD = ROOT / 'build/performance-150fps'
OUT = ROOT / 'docs/engineering-evidence/performance-150fps'


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def identity(path):
    return {'bytes': path.stat().st_size,
            'sha256': hashlib.sha256(path.read_bytes()).hexdigest()}


seal = read(BUILD / 'stage-a/final-seal.json')
assert all(seal['frozenComparison'].values()) and not seal['protectedFailures']
analysis = read(BUILD / 'stage-a/analysis.json')['native']
journal = read(BUILD / 'stage-a/journal-summary.json')
assert journal['complete'] and journal['fullSpanChecksumsValid']
retention = read(BUILD / 'stage-a/runtime-retention.json')
assert not retention['Deleted'] and not retention['Failures']
result = {
    'judgment': 'REVISE - 150 FPS not qualified; FROZEN UNBANKED',
    'stageARecurringFootprintOwner': 'PASS',
    'recorderStorageMaintenance': 'PASS',
    'recorderAcceptanceScope': 'Temporary qualification infrastructure while blackout remains unresolved; not accepted permanent public-player architecture. Public release requires explicit Project Control KEEP / DEV-ONLY / RETIRE source classification. Runtime and bulk forensic evidence remain local-only.',
    'stageB': 'Final authorized payoff gate NOT MET; no production correction or new native verification',
    'finalGpuAttempt': {'productionCorrections': 0, 'nativeVerifications': 0,
                       'performanceGap': 'OPEN', 'payoff': 'NOT MET'},
    'blackout': 'UNRESOLVED - logged health is not visual acceptance',
    'playerAcceptance': 'HOLD',
    'identity': {k: seal[k] for k in ('head', 'headUnchanged', 'refsUnchanged',
        'sourceSha256', 'packageSha256', 'protectedCount', 'protectedFailures', 'frozenComparison')},
    'package': {'files': len(seal['packageFiles']),
                'bytes': sum(v['bytes'] for v in seal['packageFiles'].values())},
    'nativeProcess': {k: v for k, v in read(BUILD / 'stage-a/process.json').items() if k != 'samples'},
    'os': read(BUILD / 'stage-a/os-after.json'),
    'display': {'saved': [3440, 1440], 'actualViewport': [3440, 1322]},
    'offlineComparison': read(BUILD / 'offline-comparison.json'),
    'flightGroups': {k: analysis['flightGroups'][k] for k in ('all', 'grounded',
        'restingNoActuation', 'groundedRcs', 'postHandoffNoNativeSlices')},
    'producer': analysis['producer'],
    'observerAllocatedBytes': analysis['observerAllocatedBytes'],
    'instrumentationAllocatedBytes': analysis['instrumentationAllocatedBytes'],
    'journal': journal,
    'retention': retention,
    'review': {'cpuCorrectness': 'PASS', 'cpuNativeAnalysis': 'Stage A scoped PASS; overall REVISE',
               'retentionRedTeam': 'PASS', 'ksaGpu': 'No new exact replacement established'},
    'notes': analysis['notes'] + [
        '377 flight GPU owners complete; whole-window startup/final sample absence disclosed in raw analysis.',
        'No real session was reclassified, unpinned, compacted or retired.',
        'Native one-exposure marker is consumed. Reproduction commands do not authorize another launch.',
        'CPU transition and unresolved spikes remain counted; no steady-state 150 FPS PASS.',
        'Runtime report totals are the maintenance snapshot before its regeneratable status report rewrite.']
}
OUT.mkdir(exist_ok=True)
(OUT / 'results.json').write_text(json.dumps(result, indent=2) + '\n')

paths = [
    'entry.json', 'offline-final-driver.log', 'offline-comparison.json',
    'stage-a-final-replay.json', 'maintenance-debug-driver.log',
    'maintenance/retention-Debug.log', 'maintenance/recorder-Debug.log', 'maintenance/ui-Debug.txt',
    'retention-final-Release.log', 'recorder-final-Release.log', 'storage-ui-Release.txt',
    'package-before-native.json', 'stage-a/source-freeze.json', 'stage-a/post-native-seal.json', 'stage-a/final-seal.json',
    'stage-a/native-launch.json', 'stage-a/offline-admission.json', 'stage-a/prelaunch-check.json',
    'stage-a/native.json', 'stage-a/native.json.input.ncflight.json', 'stage-a/native-timing.csv',
    'stage-a/driver.log', 'stage-a/driver-error.log', 'stage-a/process.json', 'stage-a/os-after.json',
    'stage-a/analysis.json', 'stage-a/journal-summary.json', 'stage-a/runtime-retention.json']
manifest = {str((BUILD / name).relative_to(ROOT)).replace('\\', '/'): identity(BUILD / name) for name in paths}
for path in sorted(OUT.iterdir()):
    if path.is_file() and path.name != 'evidence-manifest.json':
        manifest[path.relative_to(ROOT).as_posix()] = identity(path)
for path in sorted((ROOT / 'tools/physics').glob('*performance-150*')):
    if path.is_file():
        manifest[path.relative_to(ROOT).as_posix()] = identity(path)
for name in ('tools/physics/analyze-final-gpu-payoff.py',
             'tools/physics/qualify-recorder-maintenance.ps1',
             'README.md', 'docs/NOVACORE_CURRENT_STATE.md', 'docs/KNOWN_LIMITATIONS.md',
             'docs/CHANGELOG.md', 'docs/build-windows.md'):
    manifest[name] = identity(ROOT / name)
(OUT / 'evidence-manifest.json').write_text(json.dumps(manifest, indent=2) + '\n')
total = sum(p.stat().st_size for p in OUT.iterdir() if p.is_file())
assert total <= 250 * 1024, 'Compact permanent evidence budget exceeded'
print(f'PERFORMANCE_EVIDENCE_SEALED files={len(manifest)} packageBytes={total}; raw writes=0; GPU=0')
