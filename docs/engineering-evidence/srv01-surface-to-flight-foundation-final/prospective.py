"""Review exact proposed bank in a disposable alternate index; real index untouched."""
from pathlib import Path
import os,json,subprocess,hashlib
ROOT=Path(__file__).resolve().parents[3];OUT=Path(__file__).resolve().parent
BUILD=ROOT/'build/srv01-foundation-final';BUILD.mkdir(exist_ok=True)
paths=json.loads((OUT/'bank-manifest.json').read_text())
assert paths==sorted(set(paths)) and all((ROOT/p).is_file() for p in paths)
real=subprocess.check_output(['git','diff','--cached','--name-only'],cwd=ROOT,text=True);assert not real.strip()
spec=BUILD/'prospective-paths.nul';spec.write_bytes(b'\0'.join(p.encode() for p in paths)+b'\0')
env=dict(os.environ,GIT_INDEX_FILE=str(BUILD/'prospective.index'))
def run(*args):
    p=subprocess.run(['git',*args],cwd=ROOT,env=env,text=True,capture_output=True)
    if p.returncode:raise RuntimeError(p.stdout+p.stderr)
    return p.stdout.strip()
run('read-tree','HEAD');run('add','--pathspec-from-file='+str(spec),'--pathspec-file-nul')
actual=run('diff','--cached','--name-only').splitlines();assert actual==paths,(set(actual)^set(paths))
check=subprocess.run(['git','diff','--cached','--check'],cwd=ROOT,env=env,text=True,capture_output=True)
result=dict(index='DISPOSABLE ALTERNATE INDEX ONLY',paths=len(paths),manifestMatch=True,whitespaceExit=check.returncode,whitespace=check.stdout+check.stderr,tree=run('write-tree'),manifestSha256=hashlib.sha256((OUT/'bank-manifest.json').read_bytes()).hexdigest())
(BUILD/'prospective.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print(json.dumps(result,indent=2))
assert not subprocess.check_output(['git','diff','--cached','--name-only'],cwd=ROOT,text=True).strip()
assert check.returncode==0,'Proposed bank has whitespace errors; real index untouched'
