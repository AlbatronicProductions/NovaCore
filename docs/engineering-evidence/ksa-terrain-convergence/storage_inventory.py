"""Read-only inventory of this ticket's exact scratch root, including hard links."""
import collections,ctypes,hashlib,json,pathlib
ROOT=pathlib.Path(__file__).resolve().parents[3];HERE=pathlib.Path(__file__).resolve().parent
OUT=ROOT/'build/ksa-terrain-convergence'
kernel=ctypes.WinDLL('kernel32',use_last_error=True)
class StandardInfo(ctypes.Structure):
    _fields_=[('AllocationSize',ctypes.c_int64),('EndOfFile',ctypes.c_int64),('NumberOfLinks',ctypes.c_uint32),('DeletePending',ctypes.c_ubyte),('Directory',ctypes.c_ubyte)]
kernel.CreateFileW.argtypes=[ctypes.c_wchar_p,ctypes.c_uint32,ctypes.c_uint32,ctypes.c_void_p,ctypes.c_uint32,ctypes.c_uint32,ctypes.c_void_p];kernel.CreateFileW.restype=ctypes.c_void_p
kernel.GetFileInformationByHandleEx.argtypes=[ctypes.c_void_p,ctypes.c_int,ctypes.c_void_p,ctypes.c_uint32];kernel.GetFileInformationByHandleEx.restype=ctypes.c_int
kernel.CloseHandle.argtypes=[ctypes.c_void_p]
def allocated(p):
    handle=kernel.CreateFileW(str(p),0x80,7,None,3,0x80,None)
    if handle==ctypes.c_void_p(-1).value:raise ctypes.WinError(ctypes.get_last_error())
    try:
        info=StandardInfo()
        if not kernel.GetFileInformationByHandleEx(handle,1,ctypes.byref(info),ctypes.sizeof(info)):raise ctypes.WinError(ctypes.get_last_error())
        return info.AllocationSize
    finally:kernel.CloseHandle(handle)
def sha(p):
    with p.open('rb') as f:return hashlib.file_digest(f,'sha256').hexdigest()
if __name__=='__main__':
    assert OUT.resolve()==pathlib.Path('E:/NovaCore/build/ksa-terrain-convergence')
    paths=list(OUT.rglob('*'));assert all(not (p.lstat().st_file_attributes & 0x400) for p in [OUT,*paths])
    files=[p for p in paths if p.is_file()]+[ROOT/'build/ksa-convergence-build.log']
    rows=[];groups=collections.defaultdict(list)
    for p in files:
        s=p.stat();row=dict(path=str(p.relative_to(ROOT)),bytes=s.st_size,allocatedBytes=allocated(p),links=s.st_nlink,fileIdentity=f'{s.st_dev}:{s.st_ino}',sha256=sha(p));rows.append(row);groups[row['fileIdentity']].append(row)
    external=[v for v in groups.values() if len(v)<v[0]['links']]
    own=[v for v in groups.values() if len(v)==v[0]['links']]
    result=dict(scope='Only new build/ksa-terrain-convergence and build/ksa-convergence-build.log; no production deployment/cache or prior evidence.',fileCount=len(rows),logicalBytes=sum(r['bytes'] for r in rows),allocatedBytesReclaimable=sum(v[0]['allocatedBytes'] for v in own),sharedOutsideScopeLogicalBytes=sum(v[0]['bytes'] for v in external),sharedLinkEntries=sum(len(v) for v in external),sharedOutsideScope=[dict(sha256=v[0]['sha256'],logicalBytes=v[0]['bytes'],links=v[0]['links'],insideEntries=len(v)) for v in external],files=rows)
    (HERE/'scratch-retirement-manifest.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps({k:v for k,v in result.items() if k!='files'},indent=2))
