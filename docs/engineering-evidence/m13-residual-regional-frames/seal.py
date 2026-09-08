"""Verify compact evidence and account only this ticket's retained/disposed files."""
import contextlib,gzip,hashlib,io,json,pathlib,subprocess
HERE=pathlib.Path(__file__).resolve().parent
ROOT=HERE.parents[2]
sha=lambda b:hashlib.sha256(b).hexdigest()
def write(name,data):
    (HERE/name).write_text(json.dumps(data,indent=2,ensure_ascii=True)+'\n',encoding='utf-8')

assert not (ROOT/'build/m13-residual-regional-frames').exists()
assert not subprocess.check_output(['git','diff','--name-only'],cwd=ROOT).strip()
assert not subprocess.check_output(['git','diff','--cached','--name-only'],cwd=ROOT).strip()
check=subprocess.run(['git','diff','--check'],cwd=ROOT,capture_output=True,text=True)
assert check.returncode==0
status=subprocess.check_output(['git','status','--short'],cwd=ROOT,text=True)
expected=''.join('?? docs/engineering-evidence/'+name+'/\n' for name in [
    'm13-exit-assessment','m13-regional-preparation-blocker',
    'm13-regional-preparation-convergence','m13-residual-regional-frames'])
assert status==expected,status
revisions={ref:subprocess.check_output(['git','rev-parse',ref],cwd=ROOT,text=True).strip()
    for ref in ['HEAD','main','origin/main','m13.4-zero-contribution-terrain-material-noise^{}',
                'm13.3-prepared-physical-terrain^{}']}
assert len({revisions[x] for x in ['HEAD','main','origin/main','m13.4-zero-contribution-terrain-material-noise^{}']})==1
assert revisions['HEAD']=='047ae479b33831eae1c0dfa3f37c657a7f70148f'
assert revisions['m13.3-prepared-physical-terrain^{}']=='180eaf150ba5db6364e17dd48336690778f058f9'

journals=json.loads((HERE/'journal-manifest.json').read_bytes())
for row in journals['files']:
    p=HERE/row['compressed']; packed=p.read_bytes(); raw=gzip.decompress(packed)
    assert sha(packed)==row['compressedSha256'] and sha(raw)==row['originalSha256']
    assert len(raw)==row['originalBytes'];json.loads(raw)
    assert not (HERE/row['original']).exists()
parsed=[]
for p in HERE.iterdir():
    if p.suffix in ['.py','.md','.inl','.comp','.cpp','.hpp','.ps1','.patch','.json']:
        text=p.read_text(encoding='utf-8')
        # Unified patch context lines intentionally contain a leading space
        # even when the original source line is blank; preserve patch bytes.
        if p.suffix!='.patch':
            assert not any(line.rstrip()!=line for line in text.splitlines()),p
        if p.suffix=='.json':json.loads(text);parsed.append(p.name)
        if p.suffix=='.py':compile(text,str(p),'exec')
# Exercise the retained numeric reproduction after compression. This reads
# evidence only and writes no captures, build output or Python bytecode.
text=(HERE/'frame-pacing-review.md').read_text(encoding='utf-8')
snippet=text.split('```python\n',1)[1].split('```',1)[0]
with contextlib.redirect_stdout(io.StringIO()) as output:
    exec(compile(snippet,'retained-frame-pacing-reproduction','exec'),{})
reproductionLines=len(output.getvalue().splitlines())
prior=HERE.parent/'m13-regional-preparation-convergence'
previous=json.loads((prior/'retained-manifest.json').read_bytes())
for row in previous['files']:
    b=(prior/row['path']).read_bytes()
    assert len(b)==row['bytes'] and sha(b)==row['sha256']
smoke=json.loads((HERE/'closeout.json').read_bytes())
assert smoke['postCleanupSmoke']=='PASS' and smoke['frames']==240
assert smoke['all49ShadersPerConfigurationUnchanged'] and smoke['assetsUnchanged']
write('verification.json',dict(result='PASS',jsonFilesParsed=parsed,
    pythonSyntax='PASS',utf8AndWhitespace='PASS',losslessJournals=len(journals['files']),
    retainedNumericReproduction='PASS',reproductionLines=reproductionLines,
    priorManifestHashesVerified=len(previous['files']),revisions=revisions,
    sourceDiffEmpty=True,stagedDiffEmpty=True,scratchAbsent=True,
    postCleanupNormalFloridaSmoke='PASS',gitDiffCheck=dict(exitCode=check.returncode,
    stdout=check.stdout,stderr=check.stderr),gitStatusShort=status))

excluded={'retained-manifest.json','storage.json'}
files=[]
for p in sorted(HERE.iterdir()):
    assert p.is_file(),p
    if p.name not in excluded:
        b=p.read_bytes();files.append(dict(path=p.name,bytes=len(b),sha256=sha(b)))
write('retained-manifest.json',dict(budgetBytes=8*1024*1024,files=files,
    exclusions=sorted(excluded),reason='Manifest/accounting self-reference only',
    retainedPurpose='Causal six-frame evidence, exact identities, validation-ownership control, explicit unqualified lifetime contract, independent reviews and reproduction. No production dependency.'))

disposed=json.loads((HERE/'disposable-manifest.json').read_bytes())
accidental=json.loads((HERE/'accidental-bytecode-cleanup.json').read_bytes())
# The separately recorded bytecode was removed before scratch inventory.
accidentalBytes=18270
assert str(accidentalBytes) in json.dumps(accidental)
exclusiveDisposed=disposed['exclusiveLogicalBytes']+journals['originalBytes']+accidentalBytes
logicalDisposed=disposed['logicalBytes']+journals['originalBytes']+accidentalBytes
for attempt in range(8):
    retained=sum(p.stat().st_size for p in HERE.iterdir())
    data=dict(units='bytes',accounting='Final distinct retained files plus hash-inventoried disposed file instances; not cumulative write traffic or allocated NTFS bytes',
        createdExclusiveAccountedBytes=retained+exclusiveDisposed,
        createdLogicalAccountedBytes=retained+logicalDisposed,
        permanentRetainedBytes=retained,disposedExclusiveBytes=exclusiveDisposed,
        disposedLogicalBytes=logicalDisposed,disposedPrivateHostFiles=len(disposed['files']),
        disposedPrivateHostExclusiveBytes=disposed['exclusiveLogicalBytes'],
        disposedPrivateHostLogicalBytes=disposed['logicalBytes'],
        removedProtectedAssetHardLinkAliasBytes=disposed['logicalBytes']-disposed['exclusiveLogicalBytes'],
        protectedAssetRemoved=False,uncompressedJournalDuplicatesRemovedBytes=journals['originalBytes'],
        compressedJournalBytes=journals['compressedBytes'],accidentalBytecodeRemovedBytes=accidentalBytes,
        remainingScratchBytes=0,remainingScratchFiles=0,retainedBudgetBytes=8*1024*1024,
        allocatedDiskSavings='not measured',allPriorEvidencePreserved=True)
    write('storage.json',data)
    if sum(p.stat().st_size for p in HERE.iterdir())==retained:break
else:raise AssertionError('Storage self-size did not converge')
assert retained<8*1024*1024
print(json.dumps(data,indent=2));print(status,end='')
