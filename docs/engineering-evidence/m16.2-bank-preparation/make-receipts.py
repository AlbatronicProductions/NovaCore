"""Read-only Git/source/package audit plus local evidence receipt generation; never stages."""
from pathlib import Path
import collections, datetime, hashlib, json, re, subprocess

ROOT = Path(__file__).resolve().parents[3]
HERE = Path(__file__).resolve().parent
RUN = ROOT / 'build/m16.2-bank-preparation'
def git(*args):
    return subprocess.check_output(['git', '-C', str(ROOT), *args]).decode('utf-8').strip()
def digest(p):
    with p.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()
def record(p):
    return {'path':p.relative_to(ROOT).as_posix(), 'bytes':p.stat().st_size, 'sha256':digest(p)}
def read(p): return json.loads(p.read_text(encoding='utf-8-sig'))
def write(name, value): (HERE/name).write_text(json.dumps(value, indent=2, ensure_ascii=False)+'\n', encoding='utf-8')

assert git('rev-parse', 'HEAD') == git('rev-parse', 'm16.1^{commit}') == '40314c0f72396ea5f4ff39121f0b6821f1f96146'
assert not git('diff', '--cached', '--name-only'), 'Index must remain empty'
freeze = read(RUN/'input-freeze.json')
drift = [e['path'] for e in freeze['files'] if not (ROOT/e['path']).is_file() or digest(ROOT/e['path']) != e['sha256']]
assert not drift, 'Production input drift: '+repr(drift)
deleted = git('diff', '--name-only', '--diff-filter=D').splitlines()
assert sorted(deleted) == ['native/NovaCore.Native/shaders/solar_speed_hud.frag','native/NovaCore.Native/shaders/solar_speed_hud.vert']

commands = []
for line in (RUN/'commands.jsonl').read_text(encoding='utf-8-sig').splitlines():
    run = json.loads(line)
    log = Path(run['log'])
    text = log.read_text(encoding='utf-8-sig', errors='replace')
    run['logSha256'] = digest(log)
    lines = text.splitlines()
    run['failureDetails'] = [lines[max(0,i-8):min(len(lines),i+8)] for i,line in enumerate(lines) if line.startswith('FAIL')]
    if run['name']=='graphics-Debug':
        run['failureClassification']={'assertionFailures':['Earth horizon submission boundary','M12D-P2S5C production spherical billboard runtime','Opaque distant-detailed handoff'],
            'agentInterrupted':['M12D-P2S5F nested production scale-mesh topology'],
            'documentedArtifactModeReplacement':'topology-fixture-Debug PASS; original raw nonzero count retained'}
    run['summaryLines'] = [s for s in text.splitlines() if re.search(r'(^Graphics |^Native GPU |^PASS.*(checks|tests)|^Passed|\b\d+/\d+|^FAIL|Warning\(s\)|Error\(s\))',s)][-25:]
    run['reportedResultLines'] = [s for s in lines if re.search(r'(^NovaCore .*tests passed|^SURFACE_.*_PASS|^PASS$|^PASS .*checks|^PASS .*tests|^Graphics |^Native GPU )',s)]
    run['invocationFailure'] = bool(run['exitCode'] and any(x in text for x in ['cannot find the file specified','Pass an evidence output directory','output directory, recorder executable required']))
    run['evidenceClass'] = 'AUTOMATED NATIVE INPUT + AUTHORITATIVE TEST-PROBE' if run['name'].startswith(('graphics','modular-editor')) else 'AUTHORITATIVE TEST-PROBE / BUILD'
    commands.append(run)
write('qualification-summary.json', {'recordedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),
    'commands':commands,'invocationErrorsAreNotCandidateFailures':True,
    'fullGraphicsCountsComeFromRunnerSummary':True,'noPhysicalInputClaim':True,
    'notRun':[{'gate':'Full Graphics Release', 'reason':'Sequential full-suite phase stops at mandatory Debug failure; requires correction and both configurations before banking'},
        {'gate':'Non-96-DPI layout/hit/render coordinates','reason':'No actual scaled-DPI native run performed; 96-DPI resizing is insufficient'}]})

inherited = ['tests/NovaCore.Graphics.Tests/TerrainRenderAuthorityTests.cs',
    'tests/NovaCore.Graphics.Tests/EarthHorizonSubmissionTests.cs',
    'tests/NovaCore.Graphics.Tests/GraphicsTestHarness.cs',
    'native/NovaCore.Native/shaders/prepared_surface_contract.h',
    'native/NovaCore.Native/shaders/production_spherical_billboard_physical.glsl',
    'native/NovaCore.Native/shaders/production_spherical_billboard.vert']
assert not git('diff','m16.1','--',*inherited)
program_path='tests/NovaCore.Graphics.Tests/Program.cs'
current_program=(ROOT/program_path).read_text()
parent_program=git('show','m16.1:'+program_path)
def handoff_block(text):
    return text[text.index('static void OpaqueDistantDetailedHandoffTest'):text.index('static void PlanetaryPresentationPipelineTest')].replace('\r\n','\n').strip()
assert handoff_block(current_program)==handoff_block(parent_program)
write('inherited-test-contracts.json', {'parent':'m16.1','diffFromParentEmpty':True,
    'files':[record(ROOT/p) for p in inherited],
    'opaqueHandoffTestBlockUnchangedFromParent':True,
    'opaqueHandoffTestBlockSha256':hashlib.sha256(handoff_block(current_program).encode()).hexdigest(),
    'opaqueHandoffCause':'First-match indirect terrain draw finds banked background depth prepass instead of later color branch; preserve explicit prepass depth reset and scene ordering when correcting assertion',
    'gateDisposition':'Still FAIL; inherited origin is not a waiver',
    'constructionCount':'667 current versus 675 prior: eleven versus fifteen daylight-search hours, two checks per attempted hour; all five routes and timed ignition/coast loops retained'})

native=[]
for launch in sorted(RUN.glob('native-*/launch.json')):
    folder=launch.parent
    result=read(folder/'result.json') if (folder/'result.json').exists() else None
    log=(folder/'stdout.log').read_text(encoding='utf-8-sig',errors='replace')
    native.append({'launch':read(launch),'result':result,'log':record(folder/'stdout.log'),
        'stderr':record(folder/'stderr.log'),
        'memorySamples':[s for s in log.splitlines() if s.startswith('GPU_MEMORY_UI')][:7],
        'runtimeFingerprints':[s for s in log.splitlines() if s.startswith('Runtime ')],
        'overlap':'Functional run overlapped the full Debug suite CPU-only scale-mesh regeneration and CPU retention; not a performance benchmark',
        'keyEvents':[s for s in log.splitlines() if any(k in s for k in ['GPU:','PLAYER_TARGET generation','PLAYER_GPU_BIND','LOADING','GPU_MEMORY_CLOSED','Session ended','Diagnostic session:','recorder:'])][:100]})
write('native-witnesses.json',{'currentRuns':native,
    'priorAcceptedReports':['docs/engineering-evidence/player-entry-convergence/README.md','docs/engineering-evidence/player-entry-convergence/editor-restoration.md','docs/engineering-evidence/player-entry-convergence/minimum-speed-pause.md'],
    'physicalUserInput':'NOT RUN in bank preparation; scoped user acceptance supplied by Project Control',
    'non96DpiLayoutGate':'NOT RUN - required', 'physicalMonitorDpiTransition':'NOT RUN - unclaimed',
    'heldHardwareAndFocusLoss':'NOT RUN - unclaimed'})

for name in ['package.json','cleanup-reference.json','hardware.json','topology-mode-switch.json']:
    write(name,read(RUN/name))
for name in ['remote-preflight.txt','readme-render.json']:
    (HERE/name).write_bytes((RUN/name).read_bytes())
for name, asset in [('terrain-global-status.json','earth-surface-v5'),('terrain-regional-status.json','earth-florida-m12')]:
    status_output=(RUN/name).read_text(encoding='utf-8-sig')
    assert 'Status: Valid' in status_output
    write(name,{'command':['dotnet','run','--project','tools/NovaCore.AssetTool','-c','Release','--no-build','--','status',asset],
        'reportedStatus':'Valid','capturedOutput':status_output})

special={ (HERE/'candidate-identity.json').relative_to(ROOT).as_posix(), (HERE/'bank-inventory.json').relative_to(ROOT).as_posix(), (HERE/'stage-paths.txt').relative_to(ROOT).as_posix() }
changed=set(git('diff','--name-only').splitlines())
untracked=set(git('ls-files','--others','--exclude-standard').splitlines())
intended=sorted(changed|untracked|special)
def category(path):
    if path.startswith('docs/engineering-evidence/'): return 'evidence'
    if path.startswith('docs/images/') and not path.endswith('.md'): return 'assets'
    if path.startswith('docs/') or path=='README.md': return 'docs'
    if path.startswith('tests/') or 'Qualification.cs' in path or path.endswith('PlayerGpuMemoryTests.cpp'): return 'tests'
    return 'production'
allowed=['README.md','docs/','native/NovaCore.Native/','samples/NovaCore.Triangle/','src/NovaCore.Interop/','tests/NovaCore.Graphics.Tests/','tests/NovaCore.Player.Tests/','tools/NovaCore.App/','tools/NovaCore.ConstructionEditor/']
unexpected=[p for p in intended if not any(p==prefix or p.startswith(prefix) for prefix in allowed)]
assert not unexpected, 'Unclassified changes: '+repr(unexpected)
status = subprocess.check_output(['git', '-C', str(ROOT), 'status', '--porcelain=v1', '--untracked-files=all']).decode('utf-8').splitlines()
no_blob_change=[s[3:] for s in status if s[3:] not in intended]
entries=[]
for path in intended:
    p=ROOT/path
    action='delete' if path in deleted else 'add' if path in untracked or path in special else 'modify'
    item={'path':path,'action':action,'category':category(path)}
    if p.is_file() and path not in special: item.update({'bytes':p.stat().st_size,'sha256':digest(p)})
    elif path in special: item['hashScope']='Self-referential receipt/pathlist excluded from internal digest; verify separately'
    entries.append(item)
(HERE/'stage-paths.txt').write_text(''.join(p+'\n' for p in intended),encoding='utf-8')
write('bank-inventory.json',{'judgment':'REVISE - not authorization to stage or bank','head':git('rev-parse','HEAD'),
    'intended':entries,'categoryCounts':dict(collections.Counter(e['category'] for e in entries)),
    'staged':[],'unrelatedChanges':[],
    'modifiedStatusWithoutBlobChange':no_blob_change,
    'exclusions':['build/','**/bin/','**/obj/','.novacore/cache/','recorder runtime journals/capsules/dumps','E:/Videos/2026-09-28 21-45-06.mp4','E:/Kitten Space Agency','user player saves','external local tool/project attachments'],
    'exclusionNote':'Status-only paths have no git blob diff; preserved without edit or staging. No identified unrelated content changes.'})
payload=''.join(e['path']+'\0'+e.get('sha256',e['action'])+'\n' for e in entries if e['path'] not in special)
write('candidate-identity.json',{'schema':'novacore.m16.2-preparation/1','recordedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),
    'judgment':'REVISE','branch':git('branch','--show-current'),'head':git('rev-parse','HEAD'),
    'parentTag':'m16.1','parentTagObject':git('rev-parse','m16.1'),'parentCommit':git('rev-parse','m16.1^{commit}'),
    'tagProposed':'m16.2','banked':False,'indexEmpty':True,'dirtyStatus':status,
    'sourceFreeze':freeze,'sourceDrift':drift,'deletedSources':deleted,
    'candidateContentsSha256':hashlib.sha256(payload.encode()).hexdigest(),
    'candidateDigestAlgorithm':'UTF8 path NUL raw file SHA256 (or delete) LF, path sorted; three self-referential receipt/pathlist files excluded',
    'package':read(RUN/'package.json'),'requiredNotRun':['non-96-DPI layout/hit/render-coordinate gate'],
    'independentReview':{'agent':'/root/independent_verifier','verdict':'REVISE','scope':'Complete AD4 production/test diff plus matching 1079-input freeze; read-only, no app launches','findings':['Runtime package shader closure failure','Required non-96-DPI coverage missing','Exact pause fixture retention: resolved in evidence','Explicit Player/native-memory targets and native/window integration classification: recorded']},
    'landingPage':'README.md','productionBehaviorChangedInPreparation':False})
print(json.dumps({'paths':len(entries),'categories':dict(collections.Counter(e['category'] for e in entries)),
    'noBlobChange':no_blob_change,'sourceDrift':drift,'currentNativeRuns':len(native)},indent=2))
