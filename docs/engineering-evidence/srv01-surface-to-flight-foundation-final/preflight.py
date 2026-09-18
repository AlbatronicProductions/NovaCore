"""Read-only candidate capture. Writes only this final evidence package."""
from pathlib import Path
import subprocess, hashlib, json, datetime
root=Path(__file__).resolve().parents[3]
out=Path(__file__).resolve().parent
def git(*args):
    return subprocess.check_output(['git',*args],cwd=root,text=True).strip()
def save(name,value):
    (out/name).write_text(json.dumps(value,indent=2)+'\n',encoding='utf-8')
def seal(p):
    b=(root/p).read_bytes()
    return dict(path=p,bytes=len(b),sha256=hashlib.sha256(b).hexdigest())
expected='ab14e3e0f8b53bf5e8bf580a99aea3f2b6fd21f5'
refs={r:git('rev-parse',r) for r in ['HEAD','main','origin/main']}
refs['remote_main']=git('ls-remote','origin','refs/heads/main').split()[0]
assert all(v==expected for v in refs.values()),refs
assert not git('diff','--cached','--name-only'),'Index is not empty'
checks={}
for name,args in [('worktree',['diff','--check']),('index',['diff','--cached','--check'])]:
    result=subprocess.run(['git',*args],cwd=root,text=True,capture_output=True)
    checks[name]=dict(exit=result.returncode,stdout=result.stdout,stderr=result.stderr)
    assert result.returncode==0,checks[name]
paths=git('ls-files','--cached','--others','--exclude-standard').splitlines()
source=[p for p in paths if p.startswith(('src/','tests/','samples/','native/','tools/','assets/','external/')) and (root/p).is_file()]
save('entry-source-seals.json',[seal(p) for p in sorted(set(source))])
save('preflight.json',dict(utc=datetime.datetime.now(datetime.timezone.utc).isoformat(),refs=refs,branch=git('branch','--show-current'),status=git('status','--short'),checks=checks,tags=git('for-each-ref','--format=%(refname) %(objectname) %(*objectname)','refs/tags').splitlines()))
print('Preflight PASS; source/input seals:',len(source))
