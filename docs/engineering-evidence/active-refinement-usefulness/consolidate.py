"""Consolidate bounded evidence and a disposable-file manifest; never deletes."""
import hashlib,json,re,shutil
from pathlib import Path
from summarize_isa import summarize

HERE=Path(__file__).resolve().parent
OUT=HERE.parents[2]/'build/active-refinement-usefulness'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()

def main():
    measurements={}
    invalid={'capture-B-normal','capture-B-fragment-diagnostics','capture-B-fragment-ncsm1'}
    for p in sorted(OUT.glob('*.json')):
        if p.name=='isa-summary.json':continue
        data=json.loads(p.read_text())
        if p.stem in invalid:
            old=data.pop('captureAnalysis',{})
            data['excludedAnalysis']='Superseded: std430 dvec4 array requires byte-32 offset; old parser used byte 16. No old sample statistics accepted.'
            if 'versusBaseline' in old:
                data['attachmentParityOnly']={k:v for k,v in old['versusBaseline'].items() if k in ['depth','hdr','image']}
        if p.stem=='inspect-B-triangles':data['excludedAnalysis']='Empty primitive buffer from overlapping diagnostic reset/header writes; superseded by geometry-B-triangles.'
        measurements[p.stem]=data
    (HERE/'measurements.json').write_text(json.dumps(measurements,indent=2)+'\n')
    isa={p.name:summarize(p) for p in sorted(OUT.glob('*-stage-*.isa'))}
    (HERE/'isa-summary.json').write_text(json.dumps(isa,indent=2)+'\n')
    # Three representative actual driver listings, bounded and sufficient to inspect
    # the physical arithmetic and proposed ordinary fragment compiler boundary.
    for name in ['fixed-B-stage-4.isa','fixed-B-stage-16.isa','probe-B-fragment-ncsm1-stage-16.isa']:
        shutil.copy2(OUT/name,HERE/name)
    files={p.name:dict(bytes=p.stat().st_size,sha256=sha(p)) for p in sorted(OUT.iterdir()) if p.is_file()}
    assert not any(p.is_dir() for p in OUT.iterdir()),'A live or unresolved isolated runtime remains'
    (HERE/'disposable-manifest.json').write_text(json.dumps(files,indent=2)+'\n')
    raw=sum(x['bytes'] for d in measurements.values() for x in d.get('rawFiles',{}).values())
    # The earlier empty-record run predates pre-analysis manifest persistence.
    # Its serialized sizes follow the retained host layout and logged sample count;
    # retain this as reconstructed, rather than claiming a contemporaneous hash.
    failed=OUT/'capture-B-triangles-v2.log'
    reconstructed=0
    if failed.exists():
        count=int(re.search(r'TES parity capture:.*?samples=(\d+)',failed.read_text()).group(1))
        reconstructed=32+count*128+3440*1440*16+712106*64+16
    (HERE/'storage-work.json').write_text(json.dumps(dict(
      manifestedRawBytes=raw,reconstructedUnmanifestedRawBytes=reconstructed,
      reconstruction='Empty primitive reset attempt: logged TES count and deterministic serialized layout; no raw hash retained. Original overflow attempt wrote no raw files.',
      buildDiagnosticFileBytes=sum(x['bytes'] for x in files.values()),
      rawFilesRemaining=0,
      scope='Logical diagnostic bytes only; excludes normal build outputs and transient isolated deployment copies. No whole-workspace or allocated-size claim.'),indent=2)+'\n')

if __name__=='__main__':main()
