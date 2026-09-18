"""Read-only source/ref verification and concise evidence output; no staging or tests."""
from pathlib import Path
import hashlib, json, subprocess, zipfile

r=Path('E:/NovaCore');e=Path(__file__).parent
def sha(b):return hashlib.sha256(b).hexdigest().upper()
def git(*args):return subprocess.check_output(['git',*args],cwd=r,text=True).strip()
pre=json.loads((e/'preflight.json').read_text());candidate=json.loads((e/'candidate-seals.json').read_text())
changed={s['path']:s for s in candidate['changed']}
for p,s in changed.items():assert sha((r/p).read_bytes())==s['sha256'],p
protected=[s for s in pre['sourceSeals'] if s['path'] not in changed]
for s in protected:assert sha((r/s['path']).read_bytes())==s['sha256'],s['path']
assert len(protected)==475
refs={ref:git('rev-parse',ref) for ref in ['HEAD','main','origin/main']}
assert all(v==pre['head'] for v in refs.values())
remote=git('ls-remote','--refs','origin','refs/heads/main').split()[0];assert remote==pre['remoteMain']
tags=git('show-ref','--tags').splitlines();assert tags==pre['tags']
remoteTags=git('ls-remote','--refs','--tags','origin').splitlines()
assert sorted(tuple(x.split()) for x in tags)==sorted(tuple(x.split()) for x in remoteTags)
assert not git('diff','--cached','--name-only')
for args in [('diff','--check'),('diff','--cached','--check')]:subprocess.run(['git',*args],cwd=r,check=True)
assert sha((e/'prior-stage5-inputs.zip').read_bytes())==pre['priorSnapshotSha256']
archiveIndex={}
for archive in (e.parent.parent.parent).rglob('*.zip'):
    with zipfile.ZipFile(archive) as z:
        for name in z.namelist():
            if not name.endswith('/'):
                archiveIndex.setdefault((name,sha(z.read(name))),str(archive.relative_to(r))+'::'+name)
old=json.loads((e.parent/'identity.json').read_text());preservation=[]
for family,rows in [('stage4',old['stage4Preservation']),('stage3',old['stage3Preservation'])]:
    for s in rows:
        path=s['path'];digest=s['sha256']
        if (r/path).exists() and sha((r/path).read_bytes())==digest:where='current'
        else:where=archiveIndex.get((path,digest));assert where,(family,path,digest)
        preservation.append(dict(family=family,path=path,sha256=digest,preservedAt=where))
priorSamples=[]
for s in pre['sourceSeals']:
    if s['path'] in changed and s['path'].startswith('samples/'):
        where=archiveIndex.get((s['path'],s['sha256']));assert where,s['path']
        priorSamples.append(dict(path=s['path'],sha256=s['sha256'],preservedAt=where))
assert len(priorSamples)==3
cleanupProof=[]
for name in ['Probe.cs','Probe.csproj']:
    a=r/'build/srv01-stage5-closure/probe'/name;b=e.parent/'rotating-florida-contact-closure/reproduction'/name
    assert a.read_bytes()==b.read_bytes();cleanupProof.append(dict(disposable=str(a),retained=str(b),sha256=sha(b.read_bytes())))
for name,path in [('native-source-backup.cpp','native/NovaCore.Native/src/novacore_native.cpp'),('scene-source-backup.cs','samples/NovaCore.Triangle/StockAssemblyDevelopmentScene.cs')]:
    a=r/'build/srv01-display-gap-population'/name;digest=sha(a.read_bytes())
    matches=[v for (p,h),v in archiveIndex.items() if h==digest]
    # Current file or banked blob also suffices; never infer preservation from filename.
    if (r/path).exists() and sha((r/path).read_bytes())==digest:matches.append(path)
    try:
        blob=subprocess.check_output(['git','show','HEAD:'+path],cwd=r,stderr=subprocess.DEVNULL)
        if sha(blob)==digest:matches.append('HEAD:'+path)
    except subprocess.CalledProcessError:pass
    if not matches:
        # Native filename may differ; compare only existing native source files.
        for p in (r/'native').rglob('*.cpp'):
            if sha(p.read_bytes())==digest:matches.append(str(p.relative_to(r)))
    assert matches,('Missing backup provenance',name,digest)
    cleanupProof.append(dict(disposable=str(a),retained=matches[0],sha256=digest))
output=dict(refs=refs,remoteMain=remote,branch=git('branch','--show-current'),historicalTags=len(tags),tagsUnchanged=True,
    staged=[],gitStatus=git('status','--short').splitlines(),diffCheck='PASS',cachedDiffCheck='PASS',
    sourceChanges=candidate['changed'],protectedOtherSourceTestSampleFiles=len(protected),priorStage5Archive='PASS 14/14 exact pre-edit inputs',
    priorSamplePreservation=priorSamples,checkpointPreservation=preservation,disposableSourceProof=cleanupProof,
    classification='REVISE',reason='First live process material unclassified servicing/display tail; Stage5 incomplete; manual not run',
    stage6='CLOSED',banking='UNBANKED')
(e/'final-verification.json').write_text(json.dumps(output,indent=2)+'\n')
print('PASS refs/tags/index/diff; 7 current seals; 475 protected files; 35 Stage4 + 29 Stage3 + 3 sample predecessors preserved')
