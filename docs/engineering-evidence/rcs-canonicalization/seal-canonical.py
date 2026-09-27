"""Seal canonical evidence; read source/package/Git, write this evidence only.

Requires completed canonical CPU/native/direct-player evidence. No build, Git
mutation, worktree write or deployment is performed. Run again to verify a seal.
"""
import datetime, hashlib, json, pathlib, re, shutil, subprocess

evidence = pathlib.Path(__file__).resolve().parent
root = evidence.parents[2]
build = root / 'build/rcs-canonicalization'
prior = root / 'docs/engineering-evidence/rcs-attitude-scalability'
read = lambda p: json.loads(p.read_text(encoding='utf-8-sig'))
sha = lambda p: hashlib.sha256(p.read_bytes()).hexdigest()
def write(name, value):
    (evidence/name).write_text(json.dumps(value, indent=2)+'\n', encoding='utf-8')
def git(*args):
    return subprocess.check_output(['git', '--no-optional-locks', *args], cwd=root).decode().strip()

entry = read(evidence/'entry.json')
manifest = read(prior/'source-manifest.json')
for name, row in manifest['files'].items():
    assert sha(root/name) == row['sha256'], ('qualified source changed', name)
names = {n for n in git('ls-files', '-co', '--exclude-standard', '-z').split('\0') if n and (root/n).is_file()}
runtime = {n for n in names if pathlib.PurePosixPath(n).parts[0] in {'src','native','samples','tools','tests','assets'} or n in {'NovaCore.sln','Directory.Build.props','Directory.Build.targets','NuGet.config','global.json'}}
assert runtime == set(manifest['files']), 'Runtime inventory changed'
package = read(build/'canonical-package.json')
assert package['judgment'] == 'PASS' and len(package['files']) == 127
assert len([n for n in package['files'] if n.endswith('.spv')]) == 66
assert pathlib.Path(package['package']).resolve() == (root/'tools/NovaCore.App/bin/Release/net10.0-windows').resolve()
for name, row in package['files'].items():
    assert sha(pathlib.Path(package['package'])/name) == row['sha256'], ('package changed', name)

allowed_docs = {'ENGINEERING_RULES.md','README.md','docs/CODEX_HANDOFF.md','docs/NOVACORE_CURRENT_STATE.md','docs/KNOWN_LIMITATIONS.md','docs/build-windows.md','docs/engineering-evidence/README.md'}
delta = {r['path']:r for r in entry['delta']}
changes = []
for name, row in entry['canonicalFiles'].items():
    after = sha(root/name) if (root/name).is_file() else None
    if after == row['sha256']:
        continue
    assert name in delta or name in allowed_docs, ('unrelated source changed', name)
    if name in delta:
        assert after == delta[name]['after'], name
    changes.append(dict(path=name, before=row['sha256'], after=after))
for name, row in delta.items():
    assert sha(root/name) == row['after'], name
preservation = dict(branchPointFiles=len(entry['canonicalFiles']), qualifiedDeltaFiles=len(delta),
    canonicalDocChanges=sorted(allowed_docs), unrelatedChanges=[], newerAcceptedCanonicalChanges=entry['newerCanonicalChanges'],
    headUnchanged=git('rev-parse','HEAD') == entry['head'],
    indexUnchanged=sha(root/'.git/index') == entry['indexSha256'],
    durableHeadsTagsUnchanged=git('show-ref','--heads','--tags') == entry['durableRefs'],
    note='Codex internal turn-diff refs are application-managed and are not release refs.')
assert all(preservation[k] for k in ['headUnchanged','indexUnchanged','durableHeadsTagsUnchanged'])
imported = read(evidence/'integration.json')['copiedEvidence']
assert all(sha(prior/n) == h for n,h in imported.items()), 'Historical evidence changed'
preservation['historicalEvidenceFilesUnchanged'] = len(imported)
preservation['judgment'] = 'PASS'

builds = read(build/'build-results.json')
assert len(builds) == 9 and all(r['exit'] == 0 for r in builds)
for row in builds:
    assert sha(build/(row['name']+'.log')) == row['logSha256']
cpu = read(build/'cpu-results.json')
assert len(cpu) == 66 and all(r['exit'] == 0 for r in cpu), 'Incomplete CPU regression inventory'
assert all(read(build/(c+'-cpu.json'))['checks'] == 1685 for c in ['debug','release'])
native = read(build/'native/results.json')
assert {r['route'] for r in native} == {'short','long','rcs64','rcs96'}
assert all(r['exit'] == 0 and r['result']['judgment'] == 'APPLICATION_INTEGRATION_PASS' for r in native)
assert all(r['packageSha256'] == package['packageSha256'] for r in native)
assert all(sum(w['rcsFrames'] for w in r['windows']) >= 768 for r in native if r['route'].startswith('rcs'))
ui = read(evidence/'direct-ui/result.json')
assert ui['judgment'] == 'PASS' and ui['packageSha256'] == package['packageSha256']
assert ui['normalPlayerNoQualificationFlags'] and ui['activeAttitudeObserved'] and ui['returnedToConstruction']

gpu=[]
for run in native:
    log=build/'native'/run['route']/'runtime.log'
    assert sha(log) == run['logSha256']
    samples=[]; health=[]; submitted=[]; whole=[]; cpu_periodic=[]; inputs=[]; step=-1
    for line in log.read_text(encoding='utf-8',errors='replace').splitlines():
        m=re.search(r'APPLICATION_QUALIFY step=(\d+)',line)
        if m: step=int(m[1])
        if 'GPU timings:' in line: samples.append(dict(step=step,**{k:float(v) for k,v in re.findall(r'(\w+)=([\d.]+)',line)}))
        if line.startswith('MODULAR_FRAME '): whole.append(line)
        if 'CPU timings:' in line: cpu_periodic.append(dict(step=step,**{k:float(v) for k,v in re.findall(r'(\w+)=([\d.]+)',line)}))
        if line.startswith('MODULAR_INPUT '): inputs.append(line)
        m=re.search(r'Renderer health: completedFrame=(\d+); extent=(\d+x\d+).*fenceMs=([\d.]+)',line)
        if m: health.append(dict(step=step,completedFrame=int(m[1]),extent=m[2],fenceMs=float(m[3])))
        m=re.search(r'GPU anchored refinement: submittedFrame=(\d+)',line)
        if m: submitted.append(int(m[1]))
    gpu.append(dict(route=run['route'],scope='Periodic asynchronous GPU queries; not frame-aligned CPU/GPU subtraction.',samples=samples,health=health,cpuPeriodic=cpu_periodic,initialFlight4096IncludingCold=whole,inputTrace=inputs,submittedCount=len(submitted),submittedFirst=submitted[0] if submitted else None,submittedLast=submitted[-1] if submitted else None,submittedMonotone=all(a<b for a,b in zip(submitted,submitted[1:])),completedMonotone=all(a['completedFrame']<b['completedFrame'] for a,b in zip(health,health[1:]))))

write('preservation.json',preservation)
write('canonical-changes.json',changes)
write('source-manifest.json',manifest)
write('candidate-package.json',package)
write('builds.json',builds)
write('cpu-results.json',cpu)
write('native-results.json',native)
write('native-gpu-samples.json',gpu)
for cfg in ['debug','release']:
    shutil.copyfile(build/(cfg+'-cpu.json'), evidence/(cfg+'-cpu.json'))
write('identity.json',dict(judgment='PASS_FROZEN_UNBANKED',sealedUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(),canonicalRepository=str(root),head=entry['head'],sourceSha256=manifest['sourceSha256'],sourceCount=len(manifest['files']),packageSha256=package['packageSha256'],candidateExecutable=str(pathlib.Path(package['package'])/'NovaCore.exe'),executableSha256=package['files']['NovaCore.exe']['sha256'],nativeSha256=package['files']['NovaCore.Native.dll']['sha256'],qualifiedWorktreeSourceSha256=entry['qualifiedSourceSha256'],qualifiedWorktreePackageSha256=entry['qualifiedPackageSha256'],banked=False,playerPass=False,milestone=None))
print('PASS: canonical source, package, qualification and preservation sealed.')
