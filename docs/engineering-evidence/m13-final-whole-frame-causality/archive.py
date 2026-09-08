"""Inventory/compress this ticket only. Deletion is a separate guarded PowerShell step."""
import ctypes,gzip,hashlib,json,pathlib,subprocess,sys
from ctypes import wintypes
HERE=pathlib.Path(__file__).resolve().parent;ROOT=HERE.parents[2]
SCRATCH=ROOT/'build/m13-final-whole-frame-causality'
FAILED=ROOT/'build/regional-live-tests/c035332a73f94fed8db68c247b03628a'
def sha(p):return hashlib.file_digest(p.open('rb'),'sha256').hexdigest()
class Standard(ctypes.Structure):
    _fields_=[('allocation',ctypes.c_longlong),('end',ctypes.c_longlong),('links',wintypes.DWORD),('pending',ctypes.c_byte),('directory',ctypes.c_byte)]
k=ctypes.WinDLL('kernel32',use_last_error=True)
k.CreateFileW.argtypes=[wintypes.LPCWSTR,wintypes.DWORD,wintypes.DWORD,ctypes.c_void_p,wintypes.DWORD,wintypes.DWORD,wintypes.HANDLE];k.CreateFileW.restype=wintypes.HANDLE
k.GetFileInformationByHandleEx.argtypes=[wintypes.HANDLE,ctypes.c_int,ctypes.c_void_p,wintypes.DWORD];k.GetFileInformationByHandleEx.restype=wintypes.BOOL
k.CloseHandle.argtypes=[wintypes.HANDLE]
def allocated(p):
    handle=k.CreateFileW(str(p),0x80,7,None,3,0,None)
    assert handle!=ctypes.c_void_p(-1).value
    try:
        info=Standard();assert k.GetFileInformationByHandleEx(handle,1,ctypes.byref(info),ctypes.sizeof(info));return info.allocation
    finally:k.CloseHandle(handle)
def record(p,base):
    st=p.lstat();assert not st.st_file_attributes&0x400
    assert p.resolve().is_relative_to(base.resolve())
    return dict(relative=p.relative_to(base).as_posix(),bytes=st.st_size,allocated=allocated(p),links=st.st_nlink,sha256=sha(p))
def inventory():
    assert SCRATCH.resolve()==pathlib.Path('E:/NovaCore/build/m13-final-whole-frame-causality')
    assert FAILED.resolve()==pathlib.Path('E:/NovaCore/build/regional-live-tests/c035332a73f94fed8db68c247b03628a')
    roots=[]
    for base in [SCRATCH,FAILED]:
        assert base.is_dir() and not base.lstat().st_file_attributes&0x400
        files=[]
        for p in sorted(base.rglob('*')):
            assert not p.lstat().st_file_attributes&0x400
            if p.is_file():files.append(record(p,base))
        names=b''.join(str(base/x['relative']).encode()+b'\0' for x in files)
        result=subprocess.run(['git','check-ignore','-z','--stdin'],input=names,cwd=ROOT,capture_output=True,check=True)
        assert len(result.stdout.rstrip(b'\0').split(b'\0'))==len(files)
        roots.append(dict(root=str(base),files=files,allIgnored=True))
    files=[f for r in roots for f in r['files']]
    linked=[f for f in files if f['links']>1]
    assert len(linked)==1 and linked[0]['relative']=='host/earth-data/earth_elevation_8192x4096.r16'
    asset=ROOT/'assets/earth/runtime/earth_elevation_8192x4096.r16'
    assert (SCRATCH/linked[0]['relative']).samefile(asset)
    result=dict(roots=roots,count=len(files),logicalBytesByName=sum(f['bytes'] for f in files),exclusiveLogicalBytes=sum(f['bytes'] for f in files if f['links']==1),exclusiveAllocatedBytes=sum(f['allocated'] for f in files if f['links']==1),protectedHardlink=dict(path=str(asset),sha256=sha(asset),bytes=asset.stat().st_size),reason='Resolved private timing/capture host, route probe, raw readbacks and classified failed-test output. Reports/hashes/source and successful unchanged regressions retained. No permanent runtime/test consumer.')
    (HERE/'disposable-manifest.json').write_text(json.dumps(result,indent=2)+'\n')
    print({x:y for x,y in result.items() if x not in ['roots','reason']})
def compress():
    rows=[]
    for p in sorted(HERE.glob('*.json')):
        if p.stat().st_size<=100000:continue
        data=p.read_bytes();json.loads(data);dest=p.with_suffix('.json.gz');assert not dest.exists()
        compressed=gzip.compress(data,compresslevel=9,mtime=0);dest.write_bytes(compressed)
        assert gzip.decompress(dest.read_bytes())==data
        rows.append(dict(source=p.name,destination=dest.name,sourceBytes=len(data),compressedBytes=len(compressed),sourceSha256=hashlib.sha256(data).hexdigest(),compressedSha256=sha(dest)))
    (HERE/'compressed-journals.json').write_text(json.dumps(dict(lossless=True,files=rows),indent=2)+'\n')
    print('Lossless journal streams prepared:',len(rows))
if __name__=='__main__':
    {'inventory':inventory,'compress':compress}[sys.argv[1]]()
