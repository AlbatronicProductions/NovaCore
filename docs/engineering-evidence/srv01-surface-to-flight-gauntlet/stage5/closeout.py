"""One-time read-only engineering closeout; writes concise evidence only.

No build, test, simulation, cleanup, staging or Git mutation. Run only against the
stopped draft and still-present disposable outputs; do not reseal later work.
"""
from pathlib import Path
from datetime import datetime, timezone
import hashlib
import json
import math
import subprocess
import zipfile

e = Path(__file__).resolve().parent
r = e.parents[3]
expected = 'ab14e3e0f8b53bf5e8bf580a99aea3f2b6fd21f5'

def read(p):
    return p.read_text(encoding='utf-8-sig')

def sha_bytes(b):
    return hashlib.sha256(b).hexdigest().upper()

def sha(p):
    return sha_bytes(p.read_bytes())

def write(name, value):
    (e / name).write_text(json.dumps(value, indent=2) + '\n', encoding='utf-8')

def git(*args):
    return subprocess.check_output(['git', *args], cwd=r, text=True).strip()

pre = json.loads(read(e / 'preflight.json'))
pre4 = json.loads(read(e.parent / 'stage4/preflight.json'))
qualified4 = json.loads(read(e.parent / 'stage4/identity.json'))
assert pre['qualifiedStage4'] == qualified4
refs = {n: git('rev-parse', n) for n in ['HEAD', 'main', 'origin/main']}
assert set(refs.values()) == {expected}
branch = git('branch', '--show-current')
assert branch == 'codex/srv01-supported-contact-admission'
assert not git('diff', '--cached', '--name-only')
remote = git('ls-remote', '--refs', 'origin', 'refs/heads/main').split()[0]
assert remote == expected
tags = git('show-ref', '--tags').splitlines()
assert tags == pre4['tags'] and len(tags) == 67
remote_tags = {line.split()[1]: line.split()[0] for line in git('ls-remote', '--refs', 'origin', 'refs/tags/*').splitlines()}
assert remote_tags == {line.split()[1]: line.split()[0] for line in tags}

archives = [e / 'pre-stage5-overlap.zip', e.parent / 'stage4/pre-stage4-overlap.zip']

def preservation(seals):
    result = []
    for s in seals:
        place = None
        if sha(r / s['path']) == s['sha256']:
            place = 'current'
        else:
            for p in archives:
                with zipfile.ZipFile(p) as z:
                    if s['path'] in z.namelist() and sha_bytes(z.read(s['path'])) == s['sha256']:
                        place = str(p.relative_to(r)).replace('\\', '/') + '::' + s['path']
                        break
        assert place, s['path']
        result.append(dict(path=s['path'], sha256=s['sha256'], preservedAt=place))
    return result

stage4 = preservation(qualified4['sourceSeals'])
stage3 = preservation(qualified4['stage3Preservation'])
new = [
    'src/NovaCore.Simulation/Spacecraft/Assemblies/AssemblyFloridaSite.cs',
    'src/NovaCore.Simulation/Spacecraft/Contact/Staging/LocalContactWorld.Site.cs',
    'tests/NovaCore.Graphics.Tests/AssemblyFloridaSiteTests.cs',
]
changes = []
with zipfile.ZipFile(archives[0]) as z:
    assert sorted(z.namelist()) == sorted(pre['potentialOverlaps'])
    for p in pre['potentialOverlaps']:
        before = sha_bytes(z.read(p))
        after = sha(r / p)
        assert before != after, p
        changes.append(dict(path=p, beforeStage5=before, current=after, status='unqualified draft'))
for p in new:
    changes.append(dict(path=p, current=sha(r / p), status='new unqualified draft'))

ksa_root = Path('E:/Kitten Space Agency')
ksa_expected = {
    'KSA.dll': 'A03E98153C6F353AF6F280CD3BDC1C71C718008E2049F508DC6FBE54457A0AA8',
    'BepuPhysics.dll': '77185E513BD530DEB322E41DD4ACDA9AD8E32E626CFA19492FB7481F25ED1FA7',
    'BepuUtilities.dll': 'E0A1528DED4EF8EFCB8002B99704CB8614B7DB840D2CB39B5E45EDA52D63BC68',
}
for p, value in ksa_expected.items():
    assert sha(ksa_root / p) == value

for args in [('diff', '--check'), ('diff', '--cached', '--check')]:
    subprocess.run(['git', *args], cwd=r, check=True, capture_output=True)
write('identity.json', dict(
    capturedUtc=datetime.now(timezone.utc).isoformat(), refs=refs, remoteMain=remote, branch=branch,
    historicalTagsUnchanged=True, historicalTagCount=len(tags), remoteTagsMatch=True,
    stagedPaths=[], diffCheck='PASS', cachedDiffCheck='PASS',
    gitStatus=git('status', '--short').splitlines(),
    stage4Preservation=stage4, stage3Preservation=stage3, stage5Changes=changes,
    archives=[dict(path=str(p.relative_to(r)).replace('\\', '/'), sha256=sha(p), bytes=p.stat().st_size) for p in archives],
    ksaRoot=str(ksa_root), ksaHashes=ksa_expected,
    stage5Qualification='UNQUALIFIED - material settled support failure',
    correctedTestExecuted=False,
    sourceCaveat='Stage5 production draft bytes unchanged after the executed contact run. Current test has corrected independent settled predicates and has not been rebuilt or rerun.',
))

raw = r / 'build/srv01-stage5/cheap-debug-contact.txt'
first = r / 'build/srv01-stage5/cheap-debug.txt'
velocity = [.13680320220602227, -.0018795684388216859, -.011012865703827182]
angular = [-.0002068077434545408, .006575534975447907, -.08108537076623575]
speed = math.sqrt(sum(x*x for x in velocity))
tol = .000061
write('results.json', dict(
    judgment='REVISE - STOP FOR PROJECT CONTROL',
    run=dict(configuration='Debug', intervals=1200, tick=20000000, publications=1200,
        peakPenetrationMetres=.019043728598313825, finalPenetrationMetres=.014560651361285926,
        finalVelocityMetresPerSecond=velocity, finalSpeedMetresPerSecond=speed,
        finalAngularRadiansPerSecond=angular, stateRevision=1200, historyCount=1200,
        timelineRevision=0, debtTicks=0, unchangedStoresAndMassEachInterval=True,
        retainedWorldGenerationAndBody=True),
    inheritedBars=dict(transientPenetrationMetres=.020, featureToleranceMetres=tol,
        settledHeightMetres=2*tol, settledDriftMetres=tol, settledLinearSpeedMetresPerSecond=tol/.016667,
        settledAngularSurfaceSpeedMetresPerSecond=tol/.016667, requiredSupportedIntervals=600),
    actualProperlySupportedCount=None,
    unmeasured=['proper final600 support count', 'full settled-window maximum drift', 'full settled-window maximum speed/angular rate'],
    withdrawn=['FLORIDA_SITE_CHEAP PASS', 'support=600/600'],
    withdrawalReason='Preliminary test accepted -20mm <= minimum height <= +0.122mm, not the inherited absolute settled-height limit. Final endpoint already disproves height and speed requirements.',
    currentTest='Corrected independent matrix corner oracle and inherited height/drift/speed predicates; NOT rebuilt or rerun.',
    cause='UNATTRIBUTED',
    notReached=['Release qualification', 'complete refusal matrix', 'host-partition determinism', 'allocation', 'storage', 'performance', 'full regression', 'presentation/manual acceptance', 'Stage6'],
    frameWitness=dict(positionRoundTripMetres=4.0783538020105096e-10,
        velocityRoundTripMetresPerSecond=3.0468225842716704e-14,
        centeredDerivativeErrorMetresPerSecond=7.995158878049328e-7,
        inertialAccelerationBalanceBelowMetresPerSecondSquared=2e-13),
    originalOutput=dict(path=str(raw.relative_to(r)), sha256=sha(raw), lines=read(raw).splitlines()),
    mechanicalFirstAttempt=dict(reason='Wrong free-flight service route on contact owner; corrected only the test call. No native step.',
        sha256=sha(first), lines=read(first).splitlines()),
    builds=[dict(path=p.name, sha256=sha(p), lines=read(p).splitlines()) for p in sorted((r/'build/srv01-stage5').glob('*build*.txt'))],
))

inventory = []
for item in json.loads(read(e/'disposable-inventory.json')):
    p = Path(item['Path'])
    assert p.is_relative_to(r/'build') and p.resolve().is_relative_to((r/'build').resolve())
    files = [f for f in p.rglob('*') if f.is_file()]
    inventory.append(dict(Path=str(p), Files=len(files), Bytes=sum(f.stat().st_size for f in files)))
write('disposable-inventory.json', inventory)
print(json.dumps(dict(stage4Seals=len(stage4), stage3Seals=len(stage3), stage5ChangedPaths=len(changes),
    branch=branch, refs=refs, historicalTags=len(tags), staged=0, diffCheck='PASS',
    finalSpeed=speed, disposableFiles=sum(x['Files'] for x in inventory), disposableBytes=sum(x['Bytes'] for x in inventory)), indent=2))
