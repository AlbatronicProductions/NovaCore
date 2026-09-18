"""Resolve historical source seals without restoring over the candidate."""
from pathlib import Path
import hashlib,json,zipfile,subprocess
root=Path(__file__).resolve().parents[3]; out=Path(__file__).resolve().parent
hist=root/'docs/engineering-evidence/srv01-surface-to-flight-gauntlet'
def sha(b):return hashlib.sha256(b).hexdigest().lower()
lookup={}
for p in hist.rglob('*'):
    if p.is_file() and p.suffix!='.zip':lookup[sha(p.read_bytes())]=str(p.relative_to(root)).replace('\\','/')
for zpath in [*hist.rglob('*.zip'),*out.glob('historical-*-inputs.zip')]:
    with zipfile.ZipFile(zpath) as z:
        for name in z.namelist():
            if not name.endswith('/'):lookup[sha(z.read(name))]=str(zpath.relative_to(root)).replace('\\','/')+'!'+name
for d in ['src','tests','samples','native','tools']:
    paths=subprocess.check_output(['git','ls-files','--cached','--others','--exclude-standard',d],cwd=root,text=True).splitlines()
    for name in paths:
        p=root/name
        if p.is_file():lookup[sha(p.read_bytes())]=name
def walk(v):
    if isinstance(v,dict):
        p=v.get('path','');h=v.get('sha256','')
        if isinstance(p,str) and p.startswith(('src/','tests/','samples/','native/','tools/')) and isinstance(h,str) and len(h)==64:yield p,h.lower()
        for x in v.values():yield from walk(x)
    elif isinstance(v,list):
        for x in v:yield from walk(x)
result=[]
for name in ['stage3/allocation-closure/identity.json','stage4/identity.json','stage5/identity.json','stage5/stock-florida-restart/candidate-seals.json','stage5/florida-presentation-closure/predecessor-seals.json']:
    j=json.loads((hist/name).read_text(encoding='utf-8-sig'));items=[]
    for p,h in sorted(set(walk(j))):
        source=lookup.get(h)
        if source is None:
            g=subprocess.run(['git','show','ab14e3e0f8b53bf5e8bf580a99aea3f2b6fd21f5:'+p],cwd=root,capture_output=True)
            if g.returncode==0:
                for label,b in [('raw',g.stdout),('CRLF',g.stdout.replace(b'\r\n',b'\n').replace(b'\n',b'\r\n'))]:
                    if sha(b)==h:source='baseline:'+p+' ('+label+')'
        items.append(dict(path=p,sha256=h,retainedSource=source))
    result.append(dict(checkpoint=name,checked=len(items),unresolved=sum(x['retainedSource'] is None for x in items),files=items))
(out/'historical-source-resolution.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
for x in result:
    print(x['checkpoint'],x['checked'],'unresolved',x['unresolved'])
    for y in x['files']:
        if not y['retainedSource']:print('MISSING',y['path'],y['sha256'])
