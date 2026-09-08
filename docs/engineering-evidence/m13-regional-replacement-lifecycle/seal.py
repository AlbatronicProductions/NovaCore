"""Read-only evidence checks and bounded package accounting; no GPU/build work."""
import contextlib,gzip,hashlib,io,json,pathlib,subprocess
HERE=pathlib.Path(__file__).resolve().parent
ROOT=HERE.parents[2]
BUDGET=12*1024*1024
sha=lambda b:hashlib.sha256(b).hexdigest()
def write(name,data):
    (HERE/name).write_text(json.dumps(data,indent=2,ensure_ascii=True)+'\n',encoding='utf-8')

assert not (ROOT/'build/m13-regional-replacement-lifecycle').exists()
assert not subprocess.check_output(['git','diff','--name-only'],cwd=ROOT).strip()
assert not subprocess.check_output(['git','diff','--cached','--name-only'],cwd=ROOT).strip()
check=subprocess.run(['git','diff','--check'],cwd=ROOT,capture_output=True,text=True)
assert check.returncode==0
status=subprocess.check_output(['git','status','--short'],cwd=ROOT,text=True)
expected=''.join('?? docs/engineering-evidence/'+name+'/\n' for name in sorted([
    'm13-exit-assessment','m13-regional-preparation-blocker',
    'm13-regional-preparation-convergence','m13-residual-regional-frames',
    'm13-regional-replacement-lifecycle']))
assert status==expected,status
branch=subprocess.check_output(['git','branch','--show-current'],cwd=ROOT,text=True).strip()
assert branch=='codex/m13-regional-replacement-lifecycle'
revisions={ref:subprocess.check_output(['git','rev-parse',ref],cwd=ROOT,text=True).strip()
    for ref in ['HEAD','main','origin/main','m13.4-zero-contribution-terrain-material-noise^{}',
                'm13.3-prepared-physical-terrain^{}']}
assert len({revisions[x] for x in ['HEAD','main','origin/main','m13.4-zero-contribution-terrain-material-noise^{}']})==1
assert revisions['HEAD']=='047ae479b33831eae1c0dfa3f37c657a7f70148f'
assert revisions['m13.3-prepared-physical-terrain^{}']=='180eaf150ba5db6364e17dd48336690778f058f9'

journals=json.loads((HERE/'journal-manifest.json').read_bytes())
for row in journals['files']:
    packed=(HERE/row['compressed']).read_bytes();raw=gzip.decompress(packed)
    assert sha(packed)==row['compressedSha256'] and sha(raw)==row['originalSha256']
    assert len(raw)==row['originalBytes'];json.loads(raw)
    assert not (HERE/row['original']).exists()
parsed=[]
generated={'verification.json','retained-manifest.json','storage.json'}
for p in sorted(HERE.iterdir()):
    assert p.is_file(),p
    if p.suffix in ['.py','.md','.inl','.ps1','.patch','.json']:
        text=p.read_text(encoding='utf-8')
        # Blank unified-patch context lines legitimately retain their prefix.
        if p.suffix!='.patch':
            assert not any(line.rstrip()!=line for line in text.splitlines()),p
        if p.suffix=='.json':
            json.loads(text)
            if p.name not in generated:parsed.append(p.name)
        if p.suffix=='.py':compile(text,str(p),'exec')

text=(HERE/'verify-b.md').read_text(encoding='utf-8')
snippet=text.split('```python\n',1)[1].split('```',1)[0]
scope={}
with contextlib.redirect_stdout(io.StringIO()) as output:
    exec(compile(snippet,'retained-verify-b-reproduction','exec'),scope)
pc=scope['PAIR']['pairedCorrected']
metrics=scope['pairmetrics'](pc['selected'])
assert len(pc['selected'])==64
assert metrics['reference']['gpu']['total']['above8_33']==26
assert metrics['work']['gpu']['total']['above8_33']==39
assert abs(metrics['reference']['gpu']['total']['p95']-9.02848)<1e-8
assert abs(metrics['work']['gpu']['total']['p95']-10.62408)<1e-8
assert all(x['reference']['hardware'][k]==x['work']['hardware'][k]
           for x in pc['selected'] for k in ['tcsPatches','tesInvocations'])
assert all(x['reference']['hardware'][k]!=x['work']['hardware'][k]
           for x in pc['selected'] for k in ['clippingOutput','fragments'])
cross=scope['cross'](scope['MODES']['oldA'],scope['MODES']['bank'])
assert not cross['differentPhysicalIdentityFrames']
assert not any(cross['hardwareDifferentFrames'].get(k,[]) for k in ['tcsPatches','tesInvocations'])
priorChecks={}
for name in ['m13-regional-preparation-convergence','m13-residual-regional-frames']:
    prior=HERE.parent/name
    previous=json.loads((prior/'retained-manifest.json').read_bytes())
    for row in previous['files']:
        b=(prior/row['path']).read_bytes()
        assert len(b)==row['bytes'] and sha(b)==row['sha256'],(name,row['path'])
    priorChecks[name]=len(previous['files'])
smoke=json.loads((HERE/'closeout.json').read_bytes())
assert smoke['postCleanupSmoke']=='PASS' and smoke['frames']==240
assert smoke['all49ShadersPerConfigurationUnchanged'] and smoke['assetsUnchanged']
write('verification.json',dict(result='PASS',jsonFilesParsed=parsed,
    pythonSyntax='PASS',utf8AndWhitespace='PASS',losslessJournals=len(journals['files']),
    retainedNumericReproduction='PASS',reproductionLines=len(output.getvalue().splitlines()),
    pairedTargetCount=64,pairedInputsAndTcsTesExact=True,
    pairedClippingAndFragmentCountersDifferInAll64=True,rasterParityClaimed=False,
    priorSealedManifestHashesVerified=priorChecks,revisions=revisions,branch=branch,
    sourceDiffEmpty=True,stagedDiffEmpty=True,scratchAbsent=True,
    postCleanupNormalFloridaSmoke='PASS',gitDiffCheck=dict(exitCode=check.returncode,
    stdout=check.stdout,stderr=check.stderr),gitStatusShort=status))

excluded={'retained-manifest.json','storage.json'}
files=[]
for p in sorted(HERE.iterdir()):
    assert p.is_file(),p
    if p.name not in excluded:
        b=p.read_bytes();files.append(dict(path=p.name,bytes=len(b),sha256=sha(b)))
write('retained-manifest.json',dict(budgetBytes=BUDGET,files=files,
    exclusions=sorted(excluded),reason='Manifest/accounting self-reference only',
    retainedPurpose='Regional event timing, production lifetime model, paired causal control with explicit limits, KSA frequency comparison, independent reviews and reproducible Project Control judgment. No production dependency.'))

disposed=json.loads((HERE/'disposable-manifest.json').read_bytes())
exclusiveDisposed=disposed['exclusiveLogicalBytes']+journals['originalBytes']
logicalDisposed=disposed['logicalBytes']+journals['originalBytes']
assert len(disposed['files'])==77
assert exclusiveDisposed==28222029 and logicalDisposed==95330893
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
        compressedJournalBytes=journals['compressedBytes'],accidentalBytecodeRemovedBytes=0,
        remainingScratchBytes=0,remainingScratchFiles=0,retainedBudgetBytes=BUDGET,
        allocatedDiskSavings='not measured',allPriorSealedEvidencePreserved=True)
    write('storage.json',data)
    if sum(p.stat().st_size for p in HERE.iterdir())==retained:break
else:raise AssertionError('Storage self-size did not converge')
assert retained<BUDGET
print(json.dumps(data,indent=2));print(status,end='')
