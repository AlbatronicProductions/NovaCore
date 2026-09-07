"""Consolidate only this investigation's owned scratch files; never deletes."""
import sys,json,hashlib,re,collections
sys.dont_write_bytecode=True
from pathlib import Path
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[2];OUT=ROOT/'build/post-m13.2-next-target'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def write(name,data):(HERE/name).write_text(json.dumps(data,indent=2)+'\n')
if __name__=='__main__':
    runs={p.stem:json.loads(p.read_text()) for p in OUT.glob('*.json') if p.name!='final-validation.json'}
    for label,r in runs.items():
        if not isinstance(r,dict) or 'rawFiles' not in r:continue
        p=OUT/(label+'.log')
        r['frame175StageCounts']=[line for line in p.read_text().splitlines() if 'GPU anchored refinement:' in line and 'submittedFrame=175;' in line]
    write('results.json',runs)
    compiler={};mapping={}
    for p in OUT.glob('*.isa'):
        h=sha(p);mapping[p.name]=h
        if h in compiler:continue
        s=p.read_text();ops=[line.strip().split()[0] for line in s.splitlines() if line.startswith('\t')]
        compiler[h]=dict(bytes=p.stat().st_size,instructions=len(ops),fp64=sum('f64' in o for o in ops),scalar=sum(o.startswith('s_') for o in ops),vector=sum(o.startswith('v_') for o in ops),memory=sum(any(w in o for w in ['load','store','sample','atomic']) for o in ops),exports=sum(o=='exp' for o in ops),wave32='UC_VERSION_W32_BIT' in s,opcodeCounts=dict(collections.Counter(ops)))
    write('compiler.json',dict(variants=mapping,isa=compiler,limitation='Static ISA categories overlap and do not measure dynamic occupancy, cache behavior or elapsed TES duration.'))
    if (OUT/'final-validation.json').exists():write('final-validation.json',json.loads((OUT/'final-validation.json').read_text()))
    parity={}
    for pose in 'ABCDE':
        a=runs['parity-'+pose+'-normal'];b=runs['parity-'+pose+'-defer-base']
        parity[pose]=dict(preparedExact=a['rawFiles']['prepared.bin']==b['rawFiles']['prepared.bin'],parity=b['captureAnalysis']['versusBaseline'],gsCombinedAttachmentsExact=runs['parent-'+pose]['rawFiles']['pixels.bin']['sha256']==a['rawFiles']['pixels.bin']['sha256'])
    files=list(OUT.rglob('*'));assert not any(p.is_symlink() for p in files);files=[p for p in files if p.is_file()]
    manifest=[dict(path=str(p.resolve()),bytes=p.stat().st_size,sha256=sha(p)) for p in files]
    write('scratch-retirement-manifest.json',manifest)
    raw=sum(v['bytes'] for r in runs.values() if isinstance(r,dict) for v in r.get('rawFiles',{}).values())
    write('closeout.json',dict(parity=parity,completedCaptureRawBytes=raw,scratchBytesBeforeRetirement=sum(x['bytes'] for x in manifest),scratchFilesBeforeRetirement=len(manifest),rawRetention='All completed per-run raw data retired by bounded TemporaryDirectory after hash/analysis; no raw capture retained.',diagnosticCreatedAccounting='Completed serialized captures plus final scratch file sizes are measured. Transient isolated executable/shader copies and compiler intermediate writes are excluded; report a lower bound, not an invented exact cumulative total.',disposableRemaining='PENDING final bounded retirement',finalClassification='PENDING final validation'))
    print('Retained package bytes',sum(p.stat().st_size for p in HERE.rglob('*') if p.is_file()),'known raw generated/retired',raw,'scratch',sum(x['bytes'] for x in manifest),flush=True)
