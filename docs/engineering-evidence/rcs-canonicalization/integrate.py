"""One-time guarded reconciliation of the accepted RCS source, not runtime setup.

Historical integration tool; normal reproduction builds the canonical source.
Never copies compiled binaries or changes Git metadata.
"""
import hashlib,json,pathlib,shutil,subprocess
root=pathlib.Path('E:/NovaCore')
authority=pathlib.Path('C:/Users/Tyler/.codex/worktrees/rcs-scalability/NovaCore')
source=authority/'docs/engineering-evidence/rcs-attitude-scalability'
out=root/'docs/engineering-evidence/rcs-canonicalization'
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
base=read(source/'frozen-entry.json');delta=read(source/'change-inventory.json');manifest=read(source/'source-manifest.json');package=read(source/'candidate-package.json')
assert len(delta)==31
for p,v in manifest['files'].items():assert sha(authority/p)==v['sha256'],p
for p,v in package['files'].items():assert sha(pathlib.Path(package['package'])/p)==v['sha256'],p
for p,v in base['files'].items():assert (root/p).is_file() and sha(root/p)==v['sha256'],('canonical changed',p)
for row in delta:
    p=root/row['path'];assert p.resolve().is_relative_to(root.resolve())
    assert (sha(p) if p.is_file() else None)==row['before'],row['path']
    assert sha(authority/row['path'])==row['after'],row['path']
entry=dict(head=subprocess.check_output(['git','rev-parse','HEAD'],cwd=root).decode().strip(),indexSha256=sha(root/'.git/index'),
    durableRefs=subprocess.check_output(['git','show-ref','--heads','--tags'],cwd=root).decode().strip(),
    branchPointFiles=len(base['files']),newerCanonicalChanges=[],overlaps=[],qualifiedSourceSha256=manifest['sourceSha256'],qualifiedPackageSha256=package['packageSha256'],
    canonicalFiles=base['files'],beforePackage=base['frozenPackage'],delta=delta)
out.mkdir(parents=True,exist_ok=True)
(out/'entry.json').write_text(json.dumps(entry,indent=2)+'\n',encoding='utf-8')
for row in delta:
    target=root/row['path'];target.parent.mkdir(parents=True,exist_ok=True)
    shutil.copyfile(authority/row['path'],target);assert sha(target)==row['after']
destination=root/'docs/engineering-evidence/rcs-attitude-scalability'
assert not destination.exists(),'Prior imported evidence must not be overwritten'
evidence={}
for p in source.rglob('*'):
    if p.is_file():
        target=destination/p.relative_to(source);target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(p,target)
        evidence[p.relative_to(source).as_posix()]=sha(p);assert sha(target)==sha(p)
result=dict(judgment='INTEGRATION_APPLIED_VALIDATION_PENDING',modified=28,added=3,deleted=0,semanticConflicts=[],qualifiedSourceSha256=manifest['sourceSha256'],copiedEvidence=evidence)
(out/'integration.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print(json.dumps({k:v for k,v in result.items() if k!='copiedEvidence'},indent=2))
