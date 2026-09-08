"""Verify retained evidence and account exact final file-inventory storage."""
import pathlib,json,gzip,hashlib,subprocess
HERE=pathlib.Path(__file__).resolve().parent;ROOT=HERE.parents[2]
sha=lambda b:hashlib.sha256(b).hexdigest()
assert not (ROOT/'build/m13-regional-preparation-convergence').exists()
for record in json.loads((HERE/'journal-manifest.json').read_bytes())['files']:
    packed=(HERE/record['compressed']).read_bytes()
    assert sha(packed)==record['compressedSha256']
    raw=gzip.decompress(packed);assert sha(raw)==record['originalSha256'];json.loads(raw)
    assert not (HERE/record['original']).exists()
for p in HERE.iterdir():
    assert p.is_file(),str(p)
    assert not p.lstat().st_file_attributes&0x400,str(p)
    if p.suffix=='.json':json.loads(p.read_bytes())
    elif p.suffix in ['.md','.py','.ps1','.inl','.patch']:
        text=p.read_bytes().decode('utf-8')
        if p.suffix=='.py':compile(text,str(p),'exec')
        # A patch's context may faithfully retain the old source's whitespace.
        if p.suffix!='.patch':assert not any(line.rstrip()!=line for line in text.splitlines()),str(p)
check=subprocess.run(['git','diff','--check'],cwd=ROOT,capture_output=True,text=True);assert check.returncode==0
assert not subprocess.check_output(['git','diff','--name-only'],cwd=ROOT,text=True).strip()
assert not subprocess.check_output(['git','diff','--cached','--name-only'],cwd=ROOT,text=True).strip()
files=[]
for p in sorted(HERE.iterdir()):
    if p.name in ['retained-manifest.json','storage.json']:continue
    raw=p.read_bytes();files.append(dict(path=p.name,bytes=len(raw),sha256=sha(raw)))
manifest=dict(budgetBytes=16*1024*1024,files=files,
    retainedPurpose='Bounded exact-input/performance and physical-oracle evidence for Project Control review of the rejected final M13 blocker candidate; no production consumers.',
    exclusions='Manifest and storage ledger excluded from their own hash list.',
    gitDiffCheck=dict(exitCode=check.returncode,stdout=check.stdout,stderr=check.stderr),
    gitStatusShort=subprocess.check_output(['git','status','--short'],cwd=ROOT,text=True))
(HERE/'retained-manifest.json').write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf-8')
disposable=json.loads((HERE/'disposable-manifest.json').read_bytes())
journals=json.loads((HERE/'journal-manifest.json').read_bytes())
base=sum(p.stat().st_size for p in HERE.iterdir() if p.name!='storage.json')
storage=dict(accounting='Exclusive logical file inventory, not allocated bytes, cumulative write I/O, or host RAM output.',
    scratchFilesRemoved=len(disposable['files']),scratchLogicalBytesRemoved=disposable['logicalBytes'],
    protectedElevationAliasBytes=disposable['logicalBytes']-disposable['exclusiveLogicalBytes'],
    scratchExclusiveBytesRemoved=disposable['exclusiveLogicalBytes'],
    uncompressedJournalDuplicatesRemoved=journals['originalBytes'],compressedJournalBytesRetained=journals['compressedBytes'],
    disposedBytes=disposable['exclusiveLogicalBytes']+journals['originalBytes'],remainingTemporaryBytes=0,
    retainedBytes=base,createdAccountedBytes=0,budgetBytes=16*1024*1024,retainedFileCount=len(list(HERE.iterdir()))+int(not (HERE/'storage.json').exists()),
    postCleanupSmoke='PASS; 240 normal deployed Florida frames',scratchAbsent=True)
for _ in range(20):
    storage['createdAccountedBytes']=storage['disposedBytes']+storage['retainedBytes']
    encoded=(json.dumps(storage,indent=2)+'\n').encode('utf-8');size=base+len(encoded)
    if size==storage['retainedBytes']:break
    storage['retainedBytes']=size
else:raise RuntimeError('Storage ledger did not converge')
(HERE/'storage.json').write_bytes(encoded)
assert sum(p.stat().st_size for p in HERE.iterdir())==storage['retainedBytes']<storage['budgetBytes']
print(json.dumps(storage,indent=2))
