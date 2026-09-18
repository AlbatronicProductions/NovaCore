"""Read-only cleanup/retention inventory. This script never deletes anything."""
from pathlib import Path
import json,hashlib,subprocess,os
ROOT=Path(__file__).resolve().parents[3];OUT=Path(__file__).resolve().parent
def save(n,v):(OUT/n).write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8')
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def git(*a):return subprocess.check_output(['git',*a],cwd=ROOT,text=True).strip()
roots=['srv01-display-gap-population','srv01-startup-first-present','srv01-stage2-manual','srv01-stage3','srv01-stage4','srv01-stage5','srv01-stage5-closure','srv01-stage5-stock','srv01-stage5-final-closure','srv01-stage5-florida-presentation','srv01-foundation-final']
rows=[]
for name in roots:
    p=ROOT/'build'/name;assert p.resolve().parent==ROOT/'build'
    files=[f for f in p.rglob('*') if f.is_file()] if p.exists() else []
    reparse=[str(x) for x in [p,*p.rglob('*')] if x.exists() and getattr(x.lstat(),'st_file_attributes',0)&0x400]
    tracked=git('ls-files','--',p.relative_to(ROOT).as_posix()).splitlines()
    assert not reparse and not tracked,(p,reparse,tracked)
    rows.append(dict(path=str(p),exists=p.exists(),files=len(files),bytes=sum(f.stat().st_size for f in files),reparsePoints=0,trackedFiles=0,classification='DISPOSE',reason='Rebuildable native/managed outputs, logs and diagnostic copies; concise results, exact source checkpoints and reproducers retained. All resolved historical source seals checked.'))
for rel in ['build/srv01-stage5-simplified-slab','samples/NovaCore.Triangle/bin/Release/net10.0','tools/NovaCore.Launcher/bin/Release/net10.0-windows']:
    p=ROOT/rel;files=[f for f in p.rglob('*') if f.is_file()] if p.exists() else []
    rows.append(dict(path=str(p),exists=p.exists(),files=len(files),bytes=sum(f.stat().st_size for f in files),classification='MANUAL-ACCEPTANCE BINARY NO LONGER NEEDED / ALREADY ABSENT' if not p.exists() else 'MANUAL-ACCEPTANCE BINARY NO LONGER NEEDED'))
dupes=json.loads((OUT/'duplicate-evidence-retirement.json').read_text())
for x in dupes:
    p=ROOT/x['dispose'];k=ROOT/x['keep'];assert p.read_bytes()==k.read_bytes()
    rows.append(dict(path=str(p),exists=p.exists(),files=1,bytes=p.stat().st_size,classification='DISPOSE',reason='Exact duplicate of '+x['keep'],automaticDeletion='Rejected before execution: blocked by policy. No retry.'))
removed=json.loads((OUT/'historical-stock-test-recovery.json').read_text())['files'][1]
save('cleanup-inventory.json',dict(roots=rows,remainingFiles=sum(x['files'] for x in rows),remainingBytes=sum(x['bytes'] for x in rows),removedThisTicket=[dict(path=str(OUT/'recovered-program.cs.txt'),files=1,bytes=removed['bytes'],reason='Intermediate text proved byte-identical to the retained compressed source entry, then removed; absent verified.',exists=(OUT/'recovered-program.cs.txt').exists())],automaticDisposition='STOPPED after duplicate-evidence removal was rejected by automatic approval review as blocked by policy. Old blocked roots not retried; no workaround.',unrelatedBuildRoots='OUT OF SCOPE; retained untouched.'))
scope=['docs/engineering-evidence/srv01-surface-to-flight-gauntlet','docs/engineering-evidence/srv01-supported-contact-admission-design','docs/engineering-evidence/post-m15.2-production-remeasurement']
retention=[];dispose={x['dispose']:x for x in dupes}
for d in scope:
    for p in sorted((ROOT/d).rglob('*')):
        if not p.is_file():continue
        rel=p.relative_to(ROOT).as_posix();reason='Unique historical contract/causal result, provenance or reproduction dependency; read through final consolidated index.'
        if p.suffix=='.zip':reason='Exact sealed predecessor source input required to reconstruct a qualified or decisive failed unbanked checkpoint; not build output.'
        elif p.suffix=='.json':reason='Unique measurements, source/input seals, authority or raw-output provenance for this historical checkpoint.'
        elif p.suffix in ['.py','.ps1','.h','.csproj'] or p.name.endswith(('.cs.txt','.cpp.txt')):reason='Bounded original reproduction/generation script or probe; evidence-only, not compiled/loaded by production.'
        elif p.suffix=='.png':reason='Decisive visual witness of rejected ground placement near old facility; retained as a small reference image.'
        if rel in dispose:reason='Exact duplicate; bytes retained at '+dispose[rel]['keep']
        retention.append(dict(path=rel,bytes=p.stat().st_size,sha256=sha(p),classification='DISPOSE / WITHHOLD FROM BANK' if rel in dispose else 'KEEP',reason=reason))
save('evidence-retention.json',dict(policy='Single current entry point; historical reports are not rewritten as current. Retain concise unique causal history and exact source seals; no bulk build trees/KSA source. Combined historical+final evidence budget 6 MiB. Exact duplicate inventories/refs withheld.',files=retention))
print('Cleanup inventory',sum(x['files'] for x in rows),'files',sum(x['bytes'] for x in rows),'bytes; evidence classified',len(retention))
