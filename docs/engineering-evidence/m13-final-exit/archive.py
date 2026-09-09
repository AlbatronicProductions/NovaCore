"""Current-ticket inventory and lossless journal consolidation; no deletion."""
import gzip, hashlib, importlib.util, json, pathlib, subprocess, sys
HERE=pathlib.Path(__file__).resolve().parent;ROOT=HERE.parents[2]
SCRATCH=ROOT/'build/m13-final-exit'
spec=importlib.util.spec_from_file_location('allocation_helpers',HERE.parent/'m13-final-whole-frame-causality/archive.py')
u=importlib.util.module_from_spec(spec);spec.loader.exec_module(u)

def inventory():
    assert SCRATCH.resolve()==pathlib.Path('E:/NovaCore/build/m13-final-exit')
    assert SCRATCH.is_dir() and not SCRATCH.lstat().st_file_attributes&0x400
    files=[]
    for p in sorted(SCRATCH.rglob('*')):
        assert not p.lstat().st_file_attributes&0x400
        if p.is_file():files.append(u.record(p,SCRATCH))
    names=b''.join(str(SCRATCH/f['relative']).encode()+b'\0' for f in files)
    ignored=subprocess.run(['git','check-ignore','-z','--stdin'],input=names,cwd=ROOT,capture_output=True,check=True)
    assert len(ignored.stdout.rstrip(b'\0').split(b'\0'))==len(files)
    linked=[f for f in files if f['links']>1]
    assert len(linked)==1 and linked[0]['relative']=='host/earth-data/earth_elevation_8192x4096.r16'
    asset=ROOT/'assets/earth/runtime/earth_elevation_8192x4096.r16'
    assert (SCRATCH/linked[0]['relative']).samefile(asset)
    result=dict(roots=[dict(root=str(SCRATCH),files=files,allIgnored=True)],count=len(files),
        logicalBytesByName=sum(f['bytes'] for f in files),
        exclusiveLogicalBytes=sum(f['bytes'] for f in files if f['links']==1),
        exclusiveAllocatedBytes=sum(f['allocated'] for f in files if f['links']==1),
        protectedHardlink=dict(path=str(asset),sha256=u.sha(asset),bytes=asset.stat().st_size),
        reason='Resolved private current-ticket hosts, route probe and one reusable parity slot. No normal runtime, permanent test or asset consumer. Preserve production hardlink target and all earlier evidence.')
    (HERE/'disposable-manifest.json').write_text(json.dumps(result,indent=2)+'\n')
    print({k:result[k] for k in ['count','logicalBytesByName','exclusiveLogicalBytes','exclusiveAllocatedBytes']})

def compress():
    rows=[]
    for p in sorted(HERE.glob('*.json')):
        if p.stat().st_size<=100000:continue
        data=p.read_bytes();json.loads(data);dest=p.with_suffix('.json.gz');assert not dest.exists()
        compressed=gzip.compress(data,compresslevel=9,mtime=0);dest.write_bytes(compressed)
        assert gzip.decompress(dest.read_bytes())==data
        rows.append(dict(source=p.name,destination=dest.name,sourceBytes=len(data),compressedBytes=len(compressed),sourceSha256=hashlib.sha256(data).hexdigest(),compressedSha256=u.sha(dest)))
    (HERE/'compressed-journals.json').write_text(json.dumps(dict(lossless=True,files=rows),indent=2)+'\n')
    print('Verified compressed journals:',len(rows))

if __name__=='__main__':{'inventory':inventory,'compress':compress}[sys.argv[1]]()
