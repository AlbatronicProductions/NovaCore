"""Read-only final checks plus a bounded closeout record; no builds or cleanup."""
import gzip,hashlib,importlib.util,json,pathlib,subprocess
HERE=pathlib.Path(__file__).resolve().parent;ROOT=HERE.parents[2]
spec=importlib.util.spec_from_file_location('allocation_helpers',HERE.parent/'m13-final-whole-frame-causality/archive.py')
u=importlib.util.module_from_spec(spec);spec.loader.exec_module(u)
def read(name):
    p=HERE/name
    if not p.exists():p=HERE/(name+'.gz')
    raw=p.read_bytes();return json.loads(gzip.decompress(raw) if p.suffix=='.gz' else raw)
def git(*args):return subprocess.check_output(['git',*args],cwd=ROOT,text=True).strip()
expected={
    'HEAD':'d4baab6940a57a46e478b98e36f5e45d1c4b558f',
    'main':'d4baab6940a57a46e478b98e36f5e45d1c4b558f',
    'origin/main':'d4baab6940a57a46e478b98e36f5e45d1c4b558f',
    'm13.5-local-gpu-terrain-working-data^{}':'d4baab6940a57a46e478b98e36f5e45d1c4b558f',
    'm13.4-zero-contribution-terrain-material-noise^{}':'047ae479b33831eae1c0dfa3f37c657a7f70148f',
    'm13.3-prepared-physical-terrain^{}':'180eaf150ba5db6364e17dd48336690778f058f9'}
assert {k:git('rev-parse',k) for k in expected}==expected
assert git('branch','--show-current')=='codex/m13-final-exit'
assert not git('diff','--cached','--name-only')
assert set(git('diff','--name-only').splitlines())=={'native/NovaCore.Native/MappedBufferMemory.h','native/NovaCore.Native/MappedBufferMemoryTests.cpp','native/NovaCore.Native/NovaCoreNative.cpp'}
diff=subprocess.run(['git','diff','--check'],cwd=ROOT,capture_output=True,text=True);assert diff.returncode==0
for name in ['m13-final-exit','m13-final-exit-empty-implicit']:assert not (ROOT/'build'/name).exists()
for row in read('compressed-journals.json')['files']:
    p=HERE/row['destination'];raw=p.read_bytes()
    assert hashlib.sha256(raw).hexdigest()==row['compressedSha256']
    assert hashlib.sha256(gzip.decompress(raw)).hexdigest()==row['sourceSha256']
    assert not (HERE/row['source']).exists()
for p in HERE.iterdir():
    assert p.is_file() and not p.lstat().st_file_attributes&0x400
    raw=p.read_bytes();data=gzip.decompress(raw) if p.suffix=='.gz' else raw
    text=data.decode('utf-8')
    if p.name.endswith(('.json','.json.gz')):json.loads(text)
    if p.suffix in ['.md','.py','.ps1','.inl']:assert all(line==line.rstrip() for line in text.splitlines()),p
validation=read('validation.json');assert all(r['exitCode']==0 and not r['validationErrors'] for r in validation)
assert validation[-1]['label']=='post-cleanup-normal-Florida'
for pose in ['florida','inland']:assert read('candidate-'+pose+'-parity.json')['pass']
baseline=read('baseline.json');deployment=read('candidate-deployment-final.json')
assert deployment['assets']==baseline['assets']
assert all(deployment['deployment'][c]['shaders']==baseline['deployment'][c]['shaders'] for c in ['Debug','Release'])
for config in ['Debug','Release']:
    native=ROOT/f'samples/NovaCore.Triangle/bin/{config}/net10.0/NovaCore.Native.dll'
    assert u.sha(native)==deployment['deployment'][config]['native']
assert u.sha(ROOT/'assets/earth/runtime/earth_elevation_8192x4096.r16')==read('disposable-manifest.json')['protectedHardlink']['sha256']
record=dict(leadJudgment='PASS',classification='M13.6 CANDIDATE — READY FOR PROJECT CONTROL ACCEPTANCE',
    recommendation='KEEP M13 OPEN FOR ONE PROVEN M13.6 RESPONSIBILITY',refs=expected,branch=git('branch','--show-current'),
    gitStatus=subprocess.check_output(['git','status','--short'],cwd=ROOT,text=True),staged='',diffCheck=dict(exitCode=diff.returncode,warnings=diff.stderr),
    scratchFilesRemoved=113,exclusiveScratchLogicalBytesRemoved=132633566,exclusiveScratchAllocatedBytesRemoved=132807760,
    looseJournalFilesRemoved=43,looseJournalBytesRemoved=28049753,compressedJournalBytes=4451055,
    totalExclusiveLogicalBytesRemoved=160683319,netLogicalRecoveryAfterCompression=156232264,cumulativeRawCaptureBytesWritten=759938376,
    permanentBudgetBytes=6291456,disposableRemainingBytes=0,postCleanupNormalFloridaFrames=500,
    finalDeployment=deployment['deployment'],sourceSha256={n:u.sha(ROOT/n) for n in git('diff','--name-only').splitlines()},
    note='Code/data equivalence across the final native relink and the distinct tested/final SHA256 values are in normal-rebuild-identity.json. No GPU closure or manual acceptance is claimed.')
path=HERE/'closeout.json'
for _ in range(6):
    path.write_text(json.dumps(record,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
    files=list(HERE.iterdir());size=sum(p.stat().st_size for p in files);allocated=sum(u.allocated(p) for p in files)
    assert size<=record['permanentBudgetBytes']
    if record.get('permanentLogicalBytes')==size and record.get('permanentAllocatedBytes')==allocated:break
    record.update(permanentFileCount=len(files),permanentLogicalBytes=size,permanentAllocatedBytes=allocated)
else:raise RuntimeError('storage accounting did not stabilize')
print({k:record[k] for k in ['leadJudgment','permanentFileCount','permanentLogicalBytes','permanentAllocatedBytes','disposableRemainingBytes']})
