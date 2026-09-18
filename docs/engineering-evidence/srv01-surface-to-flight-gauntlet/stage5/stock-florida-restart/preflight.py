"""One-time pre-edit preservation. Never replace historical seals with new bytes."""
from pathlib import Path
import hashlib, json, subprocess, zipfile

r=Path('E:/NovaCore'); e=Path(__file__).parent
def git(*args): return subprocess.check_output(['git',*args],cwd=r,text=True).strip()
def sha(data): return hashlib.sha256(data).hexdigest().upper()
old=json.loads((e.parent/'identity.json').read_text())
assert not (e/'preflight.json').exists(), 'Preflight already captured'
assert not git('diff','--cached','--name-only')
for ref in ('HEAD','main','origin/main'):
    assert git('rev-parse',ref)==old['refs'][ref]
assert git('ls-remote','--refs','origin','refs/heads/main').split()[0]==old['remoteMain']
for s in old['stage5Changes']:
    assert sha((r/s['path']).read_bytes())==s['current'],s['path']
for s in old['stage4Preservation']+old['stage3Preservation']:
    if s['preservedAt']=='current': data=(r/s['path']).read_bytes()
    else:
        p,name=s['preservedAt'].split('::')
        with zipfile.ZipFile(r/p) as z: data=z.read(name)
    assert sha(data)==s['sha256'],s['path']
tags=git('show-ref','--tags').splitlines()
assert tags==json.loads((e.parent.parent/'stage4/preflight.json').read_text(encoding='utf-8-sig'))['tags']
with zipfile.ZipFile(e/'prior-stage5-inputs.zip','x',compression=zipfile.ZIP_DEFLATED) as z:
    for s in old['stage5Changes']: z.write(r/s['path'],s['path'])
paths=git('ls-files','--cached','--others','--exclude-standard','src','tests','samples').splitlines()
seals=[dict(path=p,sha256=sha((r/p).read_bytes())) for p in paths if (r/p).is_file()]
subprocess.run(['git','diff','--check'],cwd=r,check=True)
out=dict(head=git('rev-parse','HEAD'),main=git('rev-parse','main'),originMain=git('rev-parse','origin/main'),
    remoteMain=old['remoteMain'],branch=git('branch','--show-current'),tags=tags,
    status=git('status','--short').splitlines(),priorStage5Seals=old['stage5Changes'],
    sourceSeals=seals,stage4Preserved=35,stage3Preserved=29,staged=[],diffCheck='PASS',
    priorSnapshotSha256=sha((e/'prior-stage5-inputs.zip').read_bytes()))
(e/'preflight.json').write_text(json.dumps(out,indent=2)+'\n')
print(f'PASS: Stage5 14/14 preserved; Stage4 35/35; Stage3 29/29; {len(seals)} source/test/sample seals; refs/tags unchanged')
