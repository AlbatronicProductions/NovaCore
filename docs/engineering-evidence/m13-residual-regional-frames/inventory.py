"""Classify exactly this ticket's disposable private host and compiler outputs."""
import hashlib,json,subprocess
from residual import ROOT,HERE,OUT,assess
baseline=json.loads((HERE/'baseline.json').read_text());current=assess.verify_deployment()
assert baseline['deployment']==current['deployment'] and baseline['assets']==current['assets']
assert OUT.resolve()==ROOT/'build/m13-residual-regional-frames'
files=[]
for path in sorted(OUT.rglob('*')):
    st=path.lstat();assert not st.st_file_attributes&0x400,str(path)
    assert path.resolve().is_relative_to(OUT.resolve())
    if path.is_file():files.append(dict(relative=path.relative_to(OUT).as_posix(),bytes=st.st_size,sha256=assess.sha(path),links=st.st_nlink))
ignored=subprocess.check_output(['git','check-ignore','--stdin'],cwd=ROOT,input='\n'.join(str(OUT/f['relative']) for f in files)+'\n',text=True).splitlines()
assert len(ignored)==len(files)
manifest=dict(root=str(OUT),files=files,logicalBytes=sum(f['bytes'] for f in files),exclusiveLogicalBytes=sum(f['bytes'] for f in files if f['links']==1),allGitIgnored=True,
 reason='Only private diagnostic host, copied shaders, generated probe shaders and loader manifest. All source/reproduction, compact numeric rows, oracle results and hashes retained. No production or permanent regression consumer. Elevation is a hard link; delete only private alias, preserving production asset.',protectedAssets=current['assets'])
(HERE/'disposable-manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
print('Disposable files',len(files),'logical bytes',manifest['logicalBytes'],'exclusive bytes',manifest['exclusiveLogicalBytes'])
