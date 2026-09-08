"""Inventory only the private assessment host; verify the preserved deployment."""
import json,os,pathlib,subprocess
from assess import ROOT,HERE,OUT,sha,verify_deployment

before=json.loads((HERE/'baseline.json').read_text());after=verify_deployment()
assert before['deployment']==after['deployment'] and before['assets']==after['assets']
assert not subprocess.check_output(['git','diff','--name-only'],cwd=ROOT,text=True).strip()
assert not subprocess.check_output(['git','diff','--cached','--name-only'],cwd=ROOT,text=True).strip()
assert OUT.resolve()==ROOT/'build/m13-exit-assessment'
items=[]
for p in sorted(OUT.rglob('*')):
    st=p.lstat();assert not st.st_file_attributes&0x400,'Reparse point not disposable: '+str(p)
    if not p.is_file():continue
    assert p.resolve().is_relative_to(OUT.resolve())
    items.append({'relative':p.relative_to(OUT).as_posix(),'bytes':st.st_size,'sha256':sha(p),'links':st.st_nlink})
assert len(items)==75
paths='\n'.join(str(OUT/x['relative'])for x in items)+'\n'
ignored=subprocess.check_output(['git','check-ignore','--stdin'],cwd=ROOT,input=paths,text=True).splitlines()
assert len(ignored)==len(items)
manifest={'root':str(OUT),'reason':'Private assessment host, shaders copied from verified deployment, rebuildable timing binaries, process-local loader manifest. No production/test consumer. The 67,108,864-byte elevation entry is an additional hard link to the preserved production asset, not a second allocation. Restored production native/managed intermediates have been rebuilt separately. All records and recipe are retained before cleanup.','files':items,'logicalBytes':sum(x['bytes']for x in items),'exclusiveLogicalBytes':sum(x['bytes']for x in items if x['links']==1),'allGitIgnored':True,'protectedAssets':after['assets']}
(HERE/'disposable-manifest.json').write_text(json.dumps(manifest,separators=(',',':'))+'\n')
subprocess.run(['git','diff','--check'],cwd=ROOT,check=True)
print(json.dumps({k:v for k,v in manifest.items()if k not in ['files','protectedAssets','reason']}))
