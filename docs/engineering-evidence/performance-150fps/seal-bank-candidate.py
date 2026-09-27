"""Evidence-only freeze: no build, launch, Git writes or recorder mutation.

Run after refreshing bank-candidate/package.json and preservation.json with the
documented read-only verifiers. Rerun the compact evidence sealer last. Generated
receipt/manifest self-references are explicitly excluded from the full file seal.
"""
import hashlib
import json
import os
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[3]
OUT = Path(__file__).resolve().parent
BUILD = ROOT / 'build/performance-150fps/bank-candidate'
EXCLUDED = {
    'docs/engineering-evidence/performance-150fps/bank-candidate.json',
    'docs/engineering-evidence/performance-150fps/evidence-manifest.json',
}


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def identity(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            h.update(block)
    return {'bytes': path.stat().st_size, 'sha256': h.hexdigest()}


def git(*args):
    env = dict(os.environ, GIT_OPTIONAL_LOCKS='0')
    return subprocess.check_output(['git', *args], cwd=ROOT, env=env).decode('utf-8').rstrip('\r\n')


def write(path, value):
    path.write_text(json.dumps(value, indent=2) + '\n', encoding='utf-8')


entry = read(ROOT / 'build/performance-150fps/entry.json')
seal = read(BUILD / 'preservation.json')
package = read(BUILD / 'package.json')
assert package['judgment'] == 'PASS' and not package['errors']
assert seal['headUnchanged'] and seal['refsUnchanged']
assert not seal['protectedFailures'] and all(seal['frozenComparison'].values())
assert package['packageSha256'] == seal['packageSha256']

names = sorted(set(git('ls-files', '--cached', '--others', '--exclude-standard', '-z').split('\0')) - {''})
files = {n: identity(ROOT / n) for n in names if n not in EXCLUDED and (ROOT / n).is_file()}
assert all(files[n]['sha256'] == sha for n, sha in seal['runtimeFiles'].items())
assert all((ROOT / n).is_file() for n in entry['files']), 'Campaign entry file missing'
package_root = ROOT / 'tools/NovaCore.App/bin/Release/net10.0-windows'
actual_package = {p.relative_to(package_root).as_posix(): identity(p)
                  for p in sorted(package_root.rglob('*')) if p.is_file()}
assert actual_package == seal['packageFiles'], 'Package changed after verification'
write(BUILD / 'working-files.json', {'scope': 'All current nonignored Git-listed files, including untracked work',
      'authority': 'Preservation inventory, not a staging or publication allowlist',
      'excludedGeneratedSelfReferences': sorted(EXCLUDED), 'files': files})

index = Path(git('rev-parse', '--git-path', 'index'))
if not index.is_absolute():
    index = ROOT / index
index_id = identity(index)
assert index_id['sha256'] == entry['protected'][str(index.resolve())]
heads_tags = git('show-ref', '--heads', '--tags')
assert heads_tags == entry['refs'].strip()
assert git('rev-parse', 'HEAD') == entry['head'] == seal['head']
assert not git('diff', '--cached', '--name-only'), 'Unexpected staged delta'
refs = git('for-each-ref', '--format=%(refname) %(objectname)')
status = git('status', '--porcelain=v1', '-z', '--untracked-files=all').split('\0')
status = [s for s in status if s]
write(BUILD / 'git.json', {'head': seal['head'], 'branch': git('branch', '--show-current'),
      'index': index_id, 'headsTags': heads_tags.splitlines(), 'allRefs': refs.splitlines(),
      'statusPorcelainZRecords': status, 'stagedPaths': [],
      'note': 'Git optional locks disabled; no index/ref/history mutation'})

retention = read(ROOT / 'build/performance-150fps/stage-a/runtime-retention.json')
recorder_root = Path(os.environ['LOCALAPPDATA']) / 'NovaCore/MinimumRecorder'
raw_files = {p.relative_to(recorder_root).as_posix(): identity(p)
             for p in sorted(recorder_root.rglob('*')) if p.is_file()}
sessions = []
for decision in retention['Decisions']:
    session = decision['Session'].replace('-', '')
    actual_bytes = sum(v['bytes'] for n, v in raw_files.items() if n.startswith(session + '/'))
    assert actual_bytes == decision['Bytes'], f'Protected session changed: {session}'
    sessions.append({**decision, 'Directory': session, 'ActualBytes': actual_bytes})
assert not retention['Deleted'] and not retention['Failures']
raw_bytes = sum(s['ActualBytes'] for s in sessions)
assert raw_bytes == retention['After']['ExemptRawBytes']
known_sessions = {s['Directory'] for s in sessions}
assert all('/' not in n or n.split('/')[0] in known_sessions for n in raw_files), 'New storage requires classification'
write(BUILD / 'protected-recorder-files.json', {'root': str(recorder_root), 'files': raw_files})

selected = ('NovaCore.exe', 'NovaCore.dll', 'NovaCore.Native.dll',
            'NovaCore.Diagnostics.dll', 'NovaCore.Recorder.exe', 'NovaCore.Recorder.dll')
artifacts = ['package.json', 'preservation.json', 'working-files.json', 'git.json',
             'protected-recorder-files.json']
receipt = {
    'disposition': 'FROZEN UNBANKED - campaign closed; STOP FOR PROJECT CONTROL BANK DECISION',
    'accepted': ['unified application', 'construction/save-load', 'scalable RCS',
                 'scalable launch support', 'Florida authority', 'launch feedback',
                 'temporary qualification recorder', 'temporary qualification recorder retention/storage maintenance',
                 'Surface Recontact/grounded control/relaunch', 'Stage A CPU/contact performance',
                 'Debug/Release/package preservation'],
    'open': {'wholeFrame150Fps': 'REVISE / OPEN', 'dominantGpuOwner': 'detailed terrain/material draw',
             'blackoutHistoricalCause': 'UNRESOLVED', 'playerPublicPass': 'UNASSIGNED'},
    'instrumentation': {
        'minimumRecorder': 'TEMPORARY QUALIFICATION INFRASTRUCTURE; not accepted permanent public-player architecture',
        'engineeringBank': 'Required diagnostic source and permanent tests may remain versioned for M16.0 reproducibility; no bank performed',
        'developerOnly': ['heavy causal mapping', 'forensic capture', 'fault injection',
                          'qualification harnesses', 'temporary observers', 'analysis tooling'],
        'localOnlyNeverCommitOrPublicPackage': ['runtime MinimumRecorder data', 'raw journals',
             'dumps', 'blackout captures', 'bulk forensic evidence', 'temporary diagnostic outputs',
             'generated qualification evidence not explicitly selected for preservation'],
        'publicReleaseGate': 'Explicit Project Control KEEP / DEV-ONLY / RETIRE source classification required',
        'selectionScope': 'Concise final engineering reports/measurements/hashes/reproduction only; no blanket inventory publication'},
    'closureActions': {'productionChanges': 0, 'builds': 0, 'nativeLaunches': 0,
                      'rawRetirements': 0, 'commitsTagsPushesBanks': 0},
    'independentClosureReview': 'PASS - source/package/Git/forensic identities and report/receipt scope reconcile; no edits/builds/launches',
    'source': {'root': str(ROOT), 'runtimeFiles': len(seal['runtimeFiles']),
               'runtimeSha256': seal['sourceSha256'], 'workingFileCount': len(files),
               'selfReferenceExclusions': sorted(EXCLUDED)},
    'package': {k: package[k] for k in ('judgment', 'package', 'fileCount', 'bytes',
                                      'packageSha256', 'shaders', 'errors', 'scope')},
    'executablesAndOwners': {n: actual_package[n] for n in selected},
    'git': {'head': seal['head'], 'branch': git('branch', '--show-current'), 'index': index_id,
            'headsTagsCount': len(heads_tags.splitlines()),
            'headsTagsSha256': hashlib.sha256((heads_tags + '\n').encode()).hexdigest(),
            'allRefsCount': len(refs.splitlines()),
            'allRefsSha256': hashlib.sha256((refs + '\n').encode()).hexdigest(),
            'stagedPaths': [], 'statusRecordCount': len(status),
            'note': 'Current working tree contains preserved unbanked work; HEAD is not the candidate'},
    'preservation': {k: seal[k] for k in ('headUnchanged', 'refsUnchanged', 'protectedCount',
                                        'protectedFailures', 'frozenComparison')},
    'storage': {'root': str(recorder_root), 'totalBytes': sum(v['bytes'] for v in raw_files.values()),
                'protectedRawBytes': raw_bytes, 'ordinaryAbnormalRawBytes': 0,
                'cleanEligibleBytes': 0, 'activeBytes': 0, 'capsuleBytes': 0,
                'otherBytes': sum(v['bytes'] for v in raw_files.values()) - raw_bytes,
                'warningThresholdBytes': 536870912, 'ordinaryRawCeilingBytes': 340787200,
                'disposition': 'FULL KEEP / MANUAL REVIEW REQUIRED', 'sessions': sessions,
                'classificationAuthority': 'Existing qualified maintenance receipt; unchanged session inventories'},
    'receipts': {str((BUILD / n).relative_to(ROOT)).replace('\\', '/'): identity(BUILD / n)
                 for n in artifacts},
    'performanceAuthority': {'summary': 'results.json', 'native': 'native-results.md',
                             'finalGpuPayoff': 'final-gpu-payoff.json'},
}
write(OUT / 'bank-candidate.json', receipt)
print(json.dumps({'freeze': receipt['disposition'], 'workingFiles': len(files),
                  'headsTags': len(heads_tags.splitlines()), 'allRefs': len(refs.splitlines()),
                  'protectedIdentities': seal['protectedCount'], 'recorderBytes': receipt['storage']['totalBytes']}))
