"""Read-only accounting before scoped diagnostic scratch retirement."""
import sys
sys.dont_write_bytecode=True
import ctypes,hashlib,json
from pathlib import Path
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[2];OUT=ROOT/'build/conservative-pre-refinement-visibility'
assert OUT.resolve()==Path('E:/NovaCore/build/conservative-pre-refinement-visibility')
k=ctypes.WinDLL('kernel32',use_last_error=True)
k.CreateFileW.argtypes=[ctypes.c_wchar_p,ctypes.c_uint32,ctypes.c_uint32,ctypes.c_void_p,ctypes.c_uint32,ctypes.c_uint32,ctypes.c_void_p];k.CreateFileW.restype=ctypes.c_void_p
k.GetFileInformationByHandleEx.argtypes=[ctypes.c_void_p,ctypes.c_int,ctypes.c_void_p,ctypes.c_uint32];k.GetFileInformationByHandleEx.restype=ctypes.c_int
k.CloseHandle.argtypes=[ctypes.c_void_p]
class Standard(ctypes.Structure):
 _fields_=[('allocation',ctypes.c_int64),('eof',ctypes.c_int64),('links',ctypes.c_uint32),('deleting',ctypes.c_ubyte),('directory',ctypes.c_ubyte)]
def allocated(p):
 handle=k.CreateFileW(str(p),0x80,7,None,3,0,None)
 if handle==ctypes.c_void_p(-1).value:raise ctypes.WinError(ctypes.get_last_error())
 try:
  value=Standard()
  if not k.GetFileInformationByHandleEx(handle,1,ctypes.byref(value),ctypes.sizeof(value)):raise ctypes.WinError(ctypes.get_last_error())
  return value.allocation
 finally:k.CloseHandle(handle)

def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()

if __name__=='__main__':
 files=[]
 for p in sorted(OUT.rglob('*')):
  assert not p.is_symlink()
  if p.is_file():
   st=p.stat();files.append(dict(path=p.relative_to(OUT).as_posix(),bytes=st.st_size,allocated=allocated(p),links=st.st_nlink,sha256=sha(p)))
 raw={}
 for p in OUT.glob('*.json'):
  d=json.loads(p.read_text())
  if isinstance(d,dict) and 'rawFiles' in d:raw[p.stem]=d['rawFiles']
 cache=ROOT/'docs/engineering-evidence/near-surface-performance/__pycache__/analyze.cpython-311.pyc'
 bytecode=dict(path=str(cache),bytes=cache.stat().st_size,allocated=allocated(cache),sha256=sha(cache)) if cache.exists() else None
 manifest=dict(root=str(OUT.resolve()),files=files,bytecode=bytecode,completedRawCaptures=raw)
 (HERE/'scratch-retirement-manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
 storage=dict(scratchLogicalBytes=sum(x['bytes'] for x in files),scratchAllocatedBytes=sum(x['allocated'] for x in files),scratchFiles=len(files),sharedHardLinkLogicalBytes=sum(x['bytes'] for x in files if x['links']>1),uniquelyLinkedScratchAllocatedBytes=sum(x['allocated'] for x in files if x['links']==1),completedRawCaptureLogicalBytes=sum(x['bytes'] for r in raw.values() for x in r.values()),generatedBytecode=bytecode,retentionBudgetBytes=3000000,rawArchiveRetention='None; each completed private raw capture was disposed by its runner. A bounded interrupted-runtime overlay is retained for the unresolved watchdog cause.',createdAccounting='Known lower bound only: raw capture payloads + inventoried scratch unique logical payload + retained files. Repeated private runtime copies, overwritten files, build-cache churn and GPU memory allocations are not represented as cumulative disk writes.',scratchRemoved=False,productionDeploymentRemoved=False)
 (HERE/'storage.json').write_text(json.dumps(storage,indent=2)+'\n')
 print(json.dumps(storage,indent=2))
