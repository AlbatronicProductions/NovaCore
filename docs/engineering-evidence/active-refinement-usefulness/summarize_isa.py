"""Static driver listing counts, not dynamic instructions or measured occupancy."""
import hashlib,json,re
from pathlib import Path

def summarize(path):
    data=path.read_bytes();text=data.decode()
    instructions=re.findall(r'^\s+([a-z][a-z0-9_]+)\s+[^\n]*;',text,re.M)
    return dict(bytes=len(data),sha256=hashlib.sha256(data).hexdigest(),
      instructions=len(instructions),scalar=sum(x.startswith('s_') for x in instructions),
      vector=sum(x.startswith('v_') for x in instructions),
      fp64=sum('f64' in x for x in instructions),
      memory=sum(x.startswith(('image_','buffer_','global_','flat_','s_load','s_buffer')) for x in instructions),
      lds=sum(x.startswith('ds_') for x in instructions),
      transcendental={op:sum(x.startswith('v_'+op+'_') for x in instructions) for op in ['rcp','rsq','sqrt','log','exp','sin','cos']},
      wave32='UC_VERSION_W32_BIT' in text,
      caveat='Static listing mnemonics across all paths; not weighted dynamic work, occupancy, or helper utilization.')

if __name__=='__main__':
    root=Path(__file__).resolve().parents[3]/'build/active-refinement-usefulness'
    result={p.name:summarize(p) for p in root.glob('*-stage-*.isa')}
    (root/'isa-summary.json').write_text(json.dumps(result,indent=2)+'\n')
