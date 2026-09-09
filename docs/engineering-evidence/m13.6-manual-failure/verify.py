"""Seal this read-only investigation; never build, run GPU work, clean or modify prior evidence."""
import ctypes as c
from ctypes import wintypes as w
import datetime, json, pathlib, subprocess
import identity

HERE = pathlib.Path(__file__).resolve().parent
ROOT = HERE.parents[2]

class Standard(c.Structure):
    _fields_ = [('allocation', c.c_longlong), ('end', c.c_longlong),
                ('links', w.DWORD), ('pending', c.c_byte), ('directory', c.c_byte)]

api = c.WinDLL('kernel32', use_last_error=True)
api.CreateFileW.argtypes = [w.LPCWSTR, w.DWORD, w.DWORD, c.c_void_p, w.DWORD, w.DWORD, w.HANDLE]
api.CreateFileW.restype = w.HANDLE
api.GetFileInformationByHandleEx.argtypes = [w.HANDLE, c.c_int, c.c_void_p, w.DWORD]
api.GetFileInformationByHandleEx.restype = w.BOOL
api.CloseHandle.argtypes = [w.HANDLE]

def allocated(p):
    handle = api.CreateFileW(str(p), 0x80, 7, None, 3, 0, None)
    assert handle != c.c_void_p(-1).value
    try:
        info = Standard()
        assert api.GetFileInformationByHandleEx(handle, 1, c.byref(info), c.sizeof(info))
        return info.allocation
    finally:
        api.CloseHandle(handle)

def main():
    before = json.loads((HERE / 'identity.json').read_text())
    for ref, expected in before['refs'].items():
        assert identity.git('rev-parse', ref) == expected, ref
    assert identity.git('branch', '--show-current') == before['branch']
    assert not identity.git('diff', '--cached')
    assert set(identity.git('diff', '--name-only').splitlines()) == set(before['source'])
    for rel, digest in before['source'].items():
        assert identity.sha(ROOT / rel) == digest, rel
    import hashlib
    diff = subprocess.check_output(['git', 'diff', '--binary'], cwd=ROOT)
    assert hashlib.sha256(diff).hexdigest() == before['trackedDiffSha256']
    old = HERE.parent / 'm13-final-exit'
    assert {p.name for p in old.iterdir()} == set(before['previousPackage'])
    for name, row in before['previousPackage'].items():
        p = old / name
        assert p.stat().st_size == row['bytes'] and identity.sha(p) == row['sha256'], name
    for config, row in before['deployment'].items():
        folder = ROOT / f'samples/NovaCore.Triangle/bin/{config}/net10.0'
        for role, name in [('native', 'NovaCore.Native.dll'), ('managed', 'NovaCore.Triangle.dll'), ('executable', 'NovaCore.Triangle.exe')]:
            assert identity.sha(folder / name) == row[role]
        assert {p.name: identity.sha(p) for p in folder.rglob('*.spv')} == row['shaders']
    for rel, row in before['assets'].items():
        assert row['matchesPrior'] and identity.sha(ROOT / rel) == row['sha256']
    assert identity.sha(pathlib.Path(before['launcher']['path'])) == before['launcher']['sha256']
    assert not (ROOT / 'build/m13.6-manual-failure').exists()
    check = subprocess.run(['git', 'diff', '--check'], cwd=ROOT, capture_output=True, text=True)
    assert check.returncode == 0
    files = sorted(p for p in HERE.iterdir() if p.name != 'closeout.json')
    for p in files:
        assert p.is_file() and not p.lstat().st_file_attributes & 0x400
        raw = p.read_text(encoding='utf-8-sig')
        if p.suffix == '.json':
            json.loads(raw)
        if p.suffix in ['.md', '.py']:
            assert all(line == line.rstrip() for line in raw.splitlines()), p
    record = dict(
        capturedUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(),
        failureClassification='INSUFFICIENT EVIDENCE',
        confirmedManifestation='Windows LiveKernelEvent141 display-engine timeout; initiating cause unresolved',
        overRenderingClassification='INSUFFICIENT EVIDENCE',
        leadJudgment='ESCALATE TO PROJECT CONTROL',
        candidateStatus='M13.6 CANDIDATE — FOLLOW-UP REQUIRED',
        refs=before['refs'], branch=before['branch'],
        baselineSourceAndTrackedDiffPreserved=True, priorPackageFilesPreserved=len(before['previousPackage']),
        deploymentShadersAssetsAndLauncherPreserved=True, newProductionChanges=0,
        gpuRuns=0, builds=0, diagnosticRawBytes=0, dumpBytesCopied=0,
        protectedIndexEmpty=True, diffCheck=dict(exitCode=check.returncode, warnings=check.stderr),
        gitStatus=subprocess.check_output(['git', 'status', '--short'], cwd=ROOT, text=True),
        permanentBudgetBytes=4194304, disposedBytes=0, disposableRemainingBytes=0,
        storageMeaning='Created/retained denote final new-file footprint, not cumulative rewrite I/O. No disposable files or bulk payloads were created.',
        files={p.name: dict(bytes=p.stat().st_size, allocated=allocated(p), sha256=identity.sha(p)) for p in files})
    destination = HERE / 'closeout.json'
    for _ in range(8):
        destination.write_text(json.dumps(record, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')
        all_files = list(HERE.iterdir())
        size = sum(p.stat().st_size for p in all_files)
        disk = sum(allocated(p) for p in all_files)
        assert size <= record['permanentBudgetBytes']
        if record.get('createdLogicalBytes') == size and record.get('retainedAllocatedBytes') == disk:
            break
        record.update(fileCount=len(all_files), createdLogicalBytes=size, retainedLogicalBytes=size,
                      retainedAllocatedBytes=disk, remainingPermanentLogicalBytes=size)
    else:
        raise RuntimeError('Storage count did not stabilize')
    print(json.dumps({k:record[k] for k in ['failureClassification','overRenderingClassification','leadJudgment',
          'candidateStatus','fileCount','createdLogicalBytes','retainedAllocatedBytes','disposedBytes',
          'disposableRemainingBytes','gitStatus']}, indent=2))

if __name__ == '__main__':
    main()
