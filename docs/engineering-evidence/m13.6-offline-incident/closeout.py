"""Verify preservation and bounded offline evidence. Never runs the export helper or GPU."""
import datetime, hashlib, json, pathlib, subprocess, sys
import inventory
HERE=pathlib.Path(__file__).resolve().parent
ROOT=HERE.parents[2]
sys.path.insert(0,str(HERE.parent/'m13.6-manual-failure'))
import verify as prior_helpers
h=inventory.helper

def main():
    baseline=json.loads((HERE/'baseline.json').read_text())
    assert {r:h.git('rev-parse',r) for r in baseline['refs']}==baseline['refs']
    assert h.git('branch','--show-current')==baseline['branch']
    assert not h.git('diff','--cached')
    assert set(h.git('diff','--name-only').splitlines())==set(baseline['source'])
    assert all(h.sha(ROOT/p)==v for p,v in baseline['source'].items())
    assert hashlib.sha256(subprocess.check_output(['git','diff','--binary'],cwd=ROOT)).hexdigest()==baseline['trackedDiffSha256']
    for name,rows in baseline['priorPackages'].items():
        folder=HERE.parent/name
        assert {p.name for p in folder.iterdir()}==set(rows)
        for rel,row in rows.items():
            p=folder/rel
            assert p.stat().st_size==row['bytes'] and h.sha(p)==row['sha256'],str(p)
    for config,rows in baseline['deployment'].items():
        folder=ROOT/f'samples/NovaCore.Triangle/bin/{config}/net10.0'
        for key,name in [('native','NovaCore.Native.dll'),('managed','NovaCore.Triangle.dll'),('executable','NovaCore.Triangle.exe')]:
            assert h.sha(folder/name)==rows[key]
        assert {p.name:h.sha(p) for p in folder.rglob('*.spv')}==rows['shaders']
    for rel,row in baseline['assets'].items():
        p=ROOT/rel
        assert p.stat().st_size==row['bytes'] and h.sha(p)==row['sha256']
    assert not (ROOT/'build/m13.6-offline-incident').exists()
    assert not (HERE/'elevated-export.json').exists()
    check=subprocess.run(['git','diff','--check'],cwd=ROOT,capture_output=True,text=True)
    assert check.returncode==0
    files=sorted(p for p in HERE.iterdir() if p.name!='closeout.json')
    for p in files:
        assert p.is_file() and not p.lstat().st_file_attributes&0x400
        raw=p.read_text(encoding='utf-8-sig')
        if p.suffix=='.json':json.loads(raw)
        assert all(line==line.rstrip() for line in raw.splitlines()),p
    record=dict(capturedUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(),
        classification='INSUFFICIENT EVIDENCE',confidence='LOW for causal attribution',pacing='INSUFFICIENT EVIDENCE',
        leadJudgment='ESCALATE TO PROJECT CONTROL',status='M13.6 CANDIDATE — FOLLOW-UP REQUIRED',
        sourceAndTrackedDiffUnchanged=True,refs=baseline['refs'],branch=baseline['branch'],
        priorPackagesPreserved={k:len(v) for k,v in baseline['priorPackages'].items()},
        deploymentShadersAssetsPreserved=True,stagedDiff='',
        elevatedExport=dict(attempts=1,outcome='Windows RunAs returned operation canceled by the user',helperStarted=False,bytesCopied=0),
        dumpAnalysisPerformed=False,newProductionEdits=0,gpuRuns=0,builds=0,traces=0,rawCaptureBytes=0,
        retestProtocol=dict(designOnly=True,implemented=False,executed=False,maximumProcessSeconds=180,prospectiveTemporaryBudget=67108864),
        budgetBytes=4194304,disposedBytes=0,disposableRemainingBytes=0,temporaryBytesCreated=0,
        storageMeaning='Created/retained are final new-file footprint, not cumulative rewrite I/O. Protected originals and prior packages remain in place.',
        files={p.name:dict(bytes=p.stat().st_size,allocated=prior_helpers.allocated(p),sha256=h.sha(p)) for p in files},
        diffCheck=dict(exitCode=check.returncode,warnings=check.stderr),
        gitStatus=subprocess.check_output(['git','status','--short'],cwd=ROOT,text=True))
    destination=HERE/'closeout.json'
    for _ in range(8):
        destination.write_text(json.dumps(record,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
        rows=list(HERE.iterdir())
        size=sum(p.stat().st_size for p in rows)
        allocated=sum(prior_helpers.allocated(p) for p in rows)
        assert size<=record['budgetBytes']
        if record.get('createdLogicalBytes')==size and record.get('retainedAllocatedBytes')==allocated:break
        record.update(fileCount=len(rows),createdLogicalBytes=size,retainedLogicalBytes=size,retainedAllocatedBytes=allocated,remainingPermanentLogicalBytes=size)
    else:raise RuntimeError('Storage did not stabilize')
    print(json.dumps({k:record[k] for k in ['classification','status','fileCount','createdLogicalBytes','retainedAllocatedBytes','disposedBytes','disposableRemainingBytes','gitStatus']},indent=2))

if __name__=='__main__':main()
