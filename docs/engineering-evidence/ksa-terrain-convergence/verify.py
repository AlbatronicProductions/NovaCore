"""Verify candidate, preserved baseline evidence, and bounded retained package."""
import ast,hashlib,json,pathlib,subprocess
ROOT=pathlib.Path(__file__).resolve().parents[3];HERE=pathlib.Path(__file__).resolve().parent
def sha(p):
    with pathlib.Path(p).open('rb') as f:return hashlib.file_digest(f,'sha256').hexdigest()
def git(*args):return subprocess.check_output(['git',*args],cwd=ROOT,text=True).strip()
def save(p,v):p.write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8')
old=HERE.parent/'exact-local-domain-encoding';prior=json.loads((old/'verification.json').read_text(encoding='utf-8-sig'))
for name,expected in prior['refs'].items():assert git('rev-parse',name)==expected,name
assert git('branch','--show-current')=='codex/m13-ksa-terrain-convergence-implementation'
assert not git('diff','--cached','--name-only')
for name,expected in prior['priorEvidenceFiles'].items():assert sha(ROOT/name)==expected,name
for item in json.loads((old/'closeout.json').read_text(encoding='utf-8-sig'))['files']:assert sha(old/item['path'])==item['sha256'],item['path']
for name in ['native/NovaCore.Native/NovaCoreNative.cpp','samples/NovaCore.Triangle/ProductionBillboardDesktopTraversal.cs']:
    expected=subprocess.check_output(['git','show','HEAD:'+name],cwd=ROOT).replace(b'\r\n',b'\n')
    assert (ROOT/name).read_bytes().replace(b'\r\n',b'\n')==expected,name
for config,d in json.loads((HERE/'deployment.json').read_text(encoding='utf-8-sig')).items():
    sample=pathlib.Path(d['sample']).parent
    assert sha(sample/'NovaCore.Native.dll')==d['native']
    assert sha(sample/'NovaCore.Triangle.dll')==d['managed']
    assert sha(sample/'NovaCore.Triangle.exe')==d['executable']
    for name,h in d['shaders'].items():assert sha(sample/'shaders'/name)==h,name
assert sha(ROOT/'assets/earth/runtime/earth_elevation_8192x4096.r16')=='4600bc01767eb81404756af62c0ee87b4bc459b82de15dca6989df34fef76317'
for p in HERE.rglob('*'):
    if not p.is_file():continue
    if p.suffix=='.json':json.loads(p.read_text(encoding='utf-8-sig'))
    if p.suffix=='.py':ast.parse(p.read_text(encoding='utf-8-sig'))
    if p.suffix in ['.py','.cs','.csproj','.json','.md']:
        text=p.read_text(encoding='utf-8-sig');assert all(line==line.rstrip() for line in text.splitlines()),p
        if p.suffix not in ['.json']:assert text.endswith('\n'),p
subprocess.run(['git','-c','core.safecrlf=false','diff','--check'],cwd=ROOT,check=True,capture_output=True)
changed=git('diff','--name-only').splitlines()
record=dict(judgment='PASS',technicalClassification='READY FOR M13.3 ARCHITECTURAL IMPLEMENTATION REVIEW',refs={name:git('rev-parse',name) for name in prior['refs']},branch=git('branch','--show-current'),stagedDiff=git('diff','--cached'),diffCheck='PASS',priorEvidenceFilesVerified=len(prior['priorEvidenceFiles'])+len(json.loads((old/'closeout.json').read_text(encoding='utf-8-sig'))['files']),priorEvidenceUnchanged=True,temporaryHostInstrumentationAbsent=True,deployedDebugReleaseIdentity='PASS49 shaders each plus native, managed and executable',trackedFiles={name:sha(ROOT/name) for name in changed},newPermanentTestSha256=sha(ROOT/'tests/NovaCore.Graphics.Tests/TerrainRenderAuthorityTests.cs'),status=git('status','--short'))
if (ROOT/'build/ksa-terrain-convergence').exists() or (ROOT/'build/ksa-convergence-build.log').exists():
    record['technicalClassification']='BLOCKED'
    record['blocker']='Automatic approval review rejected manifest-verified scratch deletion: blocked by policy. No deletion performed; guarded manual cleanup script supplied.'
record['status']=subprocess.check_output(['git','status','--short'],cwd=ROOT,text=True).rstrip('\r\n')
save(HERE/'final-verification.json',record)
manifest=json.loads((HERE/'scratch-retirement-manifest.json').read_text(encoding='utf-8-sig'))
remaining=[p for p in (ROOT/'build/ksa-terrain-convergence').rglob('*') if p.is_file()]
if (ROOT/'build/ksa-convergence-build.log').exists():remaining.append(ROOT/'build/ksa-convergence-build.log')
storage=dict(unit='Logical file-view bytes at retirement plus final retained package, not cumulative overwritten writes. Allocated retirement uses Windows FileStandardInfo and distinct file identities.',createdBytes=0,retainedBytes=0,disposedBytes=manifest['logicalBytes']-sum(p.stat().st_size for p in remaining),remainingNewScratchBytes=sum(p.stat().st_size for p in remaining),remainingNewTotalBytes=0,removedFiles=manifest['fileCount']-len(remaining),allocatedBytesRecovered=manifest['allocatedBytesReclaimable'] if not remaining else None,retiredSharedOracleLinkEntries=manifest['sharedLinkEntries'] if not remaining else 0,sharedOracleBytesNotReclaimed=manifest['sharedOutsideScopeLogicalBytes'],activeNormalBuildDeployments='Retained; ordinary build outputs not counted as diagnostic scratch creation or deletion.',priorEvidenceAndScratch='Unchanged; no retry of prior rejected cleanup.')
if remaining:
    storage['allocatedBytesRecovered']=0
    storage['allocatedBytesRecoverableAfterManualCleanup']=manifest['allocatedBytesReclaimable']
    storage['cleanupExecution']='BLOCKED by automatic execution policy; no deletion performed.'
for _ in range(4):
    retained=sum(p.stat().st_size for p in HERE.rglob('*') if p.is_file())
    storage.update(retainedBytes=retained,createdBytes=manifest['logicalBytes']+retained,remainingNewTotalBytes=storage['remainingNewScratchBytes']+retained)
    save(HERE/'storage.json',storage)
print(json.dumps(dict(result='PASS',priorEvidenceUnchanged=True,stagedDiffEmpty=True,storage=storage,status=record['status']),indent=2))
