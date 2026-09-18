"""Consolidate existing closure evidence and verify identities. No test/app/deletion/staging."""
from pathlib import Path
import hashlib,json,re,subprocess,zipfile
r=Path('E:/NovaCore');e=Path(__file__).parent;b=r/'build/srv01-stage5-final-closure';old=e.parent/'stock-florida-restart'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def git(*args):return subprocess.check_output(['git',*args],cwd=r,text=True).strip()
def read(p):return p.read_text(encoding='utf-8-sig')
def save(name,x):(e/name).write_text(json.dumps(x,indent=2)+'\n')
seals=json.loads(read(old/'candidate-seals.json'));pre=json.loads(read(old/'preflight.json'));previous=json.loads(read(old/'final-verification.json'))
changed={x['path'] for x in seals['changed']}
for x in seals['changed']:assert sha(r/x['path'])==x['sha256'],x['path']
protected=[x for x in pre['sourceSeals'] if x['path'] not in changed]
for x in protected:assert sha(r/x['path'])==x['sha256'],x['path']
for x in previous['checkpointPreservation']+previous['priorSamplePreservation']:
    if x['preservedAt']=='current':digest=sha(r/x['path'])
    else:
        archive,name=x['preservedAt'].split('::')
        with zipfile.ZipFile(r/archive) as z:digest=hashlib.sha256(z.read(name)).hexdigest().upper()
    assert digest==x['sha256'],x['path']
for x in json.loads(read(old/'manual-build.json'))['files']:assert sha(r/x['path'])==x['sha256'],x['path']
capture=json.loads(read(e/'capture-preflight.json'))
for x in capture['inputs']:assert sha(r/x['path'])==x['sha256'],x['path']
assert len(list(b.glob('process-*.json')))==3
assert len(list(b.glob('live-*.log')))==3
results=json.loads(read(e/'capture-results.json'))
for x in results['processes']:
    assert sha(b/f"live-{x['process']}.log")==x['rawLogSha256']
    assert x['journal']['exitCode']==0 and x['maxService']['Index']==0 and x['maxDisplay']['Index']==1
    assert x['warmServiceAbove1ms']==0
assert 'combined=6274064' in read(b/'debug-storage.log')
assert 'combined=6274064' in read(r/'build/srv01-stage5-stock/release-presentation-storage.log')
assert 'LEGITIMATE EXCESS HOST CREDIT' in read(b/'terminal-credit.log')
runner=read(r/'tests/NovaCore.Simulation.Tests/Program.cs').split('var tests = new (string Name, Action Test)[]',1)[1].split('foreach (var (name, test)',1)[0]
groups=re.findall(r'^\s*\("([^"]+)",',runner,re.M);assert len(groups)==80
validation=[]
for c in ['debug','release']:
    lines=read(b/f'{c}-NovaCore.Simulation.Tests.log').splitlines();assert all('PASS '+name in lines for name in groups)
    assert 'NovaCore launcher tests passed: 18' in read(b/f'launcher-{c}.log')
    assert 'bytes=0 entry=PASS exit=PASS' in read(b/f'{c}-florida-presentation.log')
    assert 'bytes=152 entry=PASS exit=PASS' in read(b/f'{c}-florida-presentation.log')
for p in sorted(b.glob('*.log')):
    text=read(p)
    if p.name.startswith('live-'):continue
    validation.append(dict(path=str(p.relative_to(r)),sha256=sha(p),bytes=p.stat().st_size,
        witnesses=[x for x in text.splitlines() if x.startswith(('PASS ','FAIL ','TERMINAL_CREDIT ','FLORIDA_PRESENTATION','ORDINARY_','Build ','    0 Warning','    0 Error','NovaCore launcher tests passed'))]))
save('validation.json',dict(logs=validation,debugStorage=6274064,retainedReleaseStorage=6274064,simulationGroupsPerConfiguration=80,launcherGroupsPerConfiguration=18,
    permanentSourceTestEdits=0,manual='PENDING: no manual launch',tailClassification='A. BOUNDED COLD / FIRST-USE SERVICING',historicalCorrelation='UNKNOWN',optimization='NONE'))
refs={x:git('rev-parse',x) for x in ['HEAD','main','origin/main']};assert all(x==pre['head'] for x in refs.values())
remote=git('ls-remote','--refs','origin','refs/heads/main').split()[0];assert remote==pre['remoteMain']
tags=git('show-ref','--tags').splitlines();assert tags==pre['tags']
assert sorted(tuple(x.split()) for x in tags)==sorted(tuple(x.split()) for x in git('ls-remote','--refs','--tags','origin').splitlines())
assert not git('diff','--cached','--name-only')
assert not git('diff','--name-only','--','native','assets','external','tools')
for args in [('diff','--check'),('diff','--cached','--check')]:subprocess.run(['git',*args],cwd=r,check=True)
save('closure-verification.json',dict(engineeringQualified=True,judgment='PASS — STAGE 5 ENGINEERING QUALIFIED, MANUAL ACCEPTANCE PENDING',
    refs=refs,remoteMain=remote,branch=git('branch','--show-current'),historicalTags=len(tags),tagsUnchanged=True,staged=[],
    candidateSeals=7,protectedOtherSourceTestSampleFiles=len(protected),predecessorSealsPreserved=len(previous['checkpointPreservation']),manualBinarySeals='PASS',
    captures=3,replacements=0,productionOrPermanentTestEdits=0,diffCheck='PASS',cachedDiffCheck='PASS',manual='PENDING',stage6='CLOSED',banking='UNBANKED',
    status=git('status','--short').splitlines(),originalFirstLiveSha256=sha(old/'first-live-process.txt')))
prior=json.loads(read(old/'cleanup-inventory.json'))['paths'];inventory=[]
for x in prior+[dict(path=str(b),disposition='DISPOSE: closure witnesses, exact observer/reproducer and hashes retained; not the manual executable')]:
    p=Path(x['path']);files=[f for f in p.rglob('*') if f.is_file()]
    assert not any(f.is_symlink() for f in p.rglob('*'))
    assert not git('ls-files','--',str(p.relative_to(r)))
    disposition=x['disposition']
    if p==r/'build/srv01-stage5-stock':
        disposition='KEEP NOW: sealed uninstrumented final executable and logs required for pending Project Control manual acceptance'
    inventory.append(dict(path=str(p),files=len(files),bytes=sum(f.stat().st_size for f in files),disposition=disposition))
save('cleanup-inventory.json',dict(paths=inventory,removedFiles=0,removedBytes=0,totalFiles=sum(x['files'] for x in inventory),totalBytes=sum(x['bytes'] for x in inventory),
    newRoot=str(b),newFiles=inventory[-1]['files'],newBytes=inventory[-1]['bytes']))
dispose=[x for x in inventory if x['disposition'].startswith('DISPOSE')]
command="$reviewedPaths = @(\n"+'\n'.join("    '"+x['path']+"'" for x in dispose)+"\n)\nforeach ($reviewedPath in $reviewedPaths) {\n    if (Test-Path -LiteralPath $reviewedPath) {\n        Remove-Item -LiteralPath $reviewedPath -Recurse -Force -ErrorAction Stop\n    }\n}\n"
verification=',\n'.join("'"+x['path']+"'" for x in inventory)+" | ForEach-Object {\n    $files = @(if (Test-Path -LiteralPath $_) { Get-ChildItem -LiteralPath $_ -File -Recurse -Force })\n    [pscustomobject]@{ Path=$_; Exists=Test-Path -LiteralPath $_; Files=$files.Count; Bytes=($files | Measure-Object Length -Sum).Sum }\n}\n"
table='\n'.join(f"| `{x['path']}` | {x['files']:,} | {x['bytes']:,} | {x['disposition']} |" for x in inventory)
(e/'cleanup.md').write_text('# Closure cleanup inventory\n\nNo deletion attempted. Previously blocked deletions were not retried. All source/evidence/assets/KSA/legal work is excluded. Three KEEP roots remain dependencies for native rebuilding, the original diagnostic and the uninstrumented manual route.\n\n| Exact path | Files | Bytes | Disposition |\n|---|---:|---:|---|\n'+table+f"\n\nTotal remaining: {sum(x['files'] for x in inventory):,} files / {sum(x['bytes'] for x in inventory):,} bytes. Six released roots: {sum(x['files'] for x in dispose):,} files / {sum(x['bytes'] for x in dispose):,} bytes. Removed: 0 / 0.\n\nExact reviewed manual PowerShell command (literal allowlist, absent paths skipped):\n\n```powershell\n"+command+'```\n\nNon-destructive verification:\n\n```powershell\n'+verification+'```\n')
print('PASS closure verification; three captures only; 80/80 Simulation and18/18 Launcher each; source/refs/tags preserved')
print('Cleanup inventory:',[(x['path'].split('\\')[-1],x['files'],x['bytes']) for x in inventory])
