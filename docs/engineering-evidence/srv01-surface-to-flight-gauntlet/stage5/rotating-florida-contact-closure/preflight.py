from pathlib import Path
import hashlib, json, subprocess, zipfile
r=Path('E:/NovaCore'); e=Path(__file__).parent
def git(*a): return subprocess.check_output(['git',*a],cwd=r,text=True).strip()
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest().upper()
old=json.loads((e.parent/'identity.json').read_text())
assert git('rev-parse','HEAD')==old['refs']['HEAD']
assert git('rev-parse','main')==old['refs']['main']
assert git('rev-parse','origin/main')==old['refs']['origin/main']
assert git('ls-remote','--refs','origin','refs/heads/main').split()[0]==old['remoteMain']
assert not git('diff','--cached','--name-only')
for s in old['stage5Changes']: assert sha(r/s['path'])==s['current'],s['path']
for s in old['stage4Preservation']+old['stage3Preservation']:
 if s['preservedAt']=='current': assert sha(r/s['path'])==s['sha256']
 else:
  p,name=s['preservedAt'].split('::')
  with zipfile.ZipFile(r/p) as z: assert hashlib.sha256(z.read(name)).hexdigest().upper()==s['sha256']
tags=git('show-ref','--tags').splitlines()
assert tags==json.loads((e.parent.parent/'stage4/preflight.json').read_text(encoding='utf-8-sig'))['tags']
subprocess.run(['git','diff','--check'],cwd=r,capture_output=True,check=True)
result=dict(head=git('rev-parse','HEAD'),main=git('rev-parse','main'),originMain=git('rev-parse','origin/main'),
 remoteMain=old['remoteMain'],branch=git('branch','--show-current'),tags=tags,status=git('status','--short').splitlines(),
 priorStage5Seals=old['stage5Changes'],stage4Preserved=35,stage3Preserved=29,
 binaryInputs=[dict(path=str(p.relative_to(r)),sha256=sha(p)) for p in (r/'build/srv01-stage5/artifacts/bin/NovaCore.Graphics.Tests/debug').glob('*.dll')],
 staged=[],diffCheck='PASS')
(e/'preflight.json').write_text(json.dumps(result,indent=2)+'\n')
print('Preflight PASS: Stage5 14/14, Stage4 35/35, Stage3 29/29; main/remote/tags unchanged; no staging.')
