"""Read-only verification by default; --write-measurements refreshes the report table."""
import hashlib,json,pathlib,subprocess,sys
from run import HERE,ROOT,deployments,git,sha

data=json.loads((HERE/'results.json').read_text(encoding='utf-8'))
verification=json.loads((HERE/'verification.json').read_text(encoding='utf-8'))
digest=hashlib.sha256(json.dumps(data,sort_keys=True,separators=(',',':')).encode()).hexdigest()
assert verification['repeatCanonicalJsonSha256']==[digest]*3
assert sha(HERE/'EncodingProof.cs')==verification['sourceSha256']
assert deployments()==verification['deployments']
for ref,expected in verification['refs'].items():assert git('rev-parse',ref)==expected,ref
assert not git('diff','--name-only') and not git('diff','--cached','--name-only')
for p,expected in verification['priorEvidenceFiles'].items():assert sha(ROOT/p)==expected,p
for p,expected in verification['ksaSourceProvenance'].items():assert sha(p)==expected,p
for name in ['global-fp64-control','exact-two-diff-local']:
    s=data['results'][name]
    assert s['Collisions']==s['RangeRejects']==s['AnchorMismatches']==0
    assert all(m['Mismatch']==0 for m in s['Metrics'].values())
assert data['globalLoaded'] and data['regionalLoaded']
assert sha(data['localPath'])==data['localSha256']
subprocess.run(['git','diff','--check'],cwd=ROOT,check=True)
if '--write-measurements' in sys.argv:
    lines=['# CPU encoding measurements','',
        f"All values below are generated from `results.json`. Each encoding has {data['results']['global-fp64-control']['Samples']:,} anchor observations. Counts include repeated topology contexts; collisions mean distinct source point bits sharing the same anchor/payload. They are not counts of unique mathematical equivalence classes.",'',
        '| Encoding | Collisions | Collisions changing full or near H | Anchor disagreements | Exact H input | Exact full H | Exact final position |',
        '|---|---:|---:|---:|---:|---:|---:|']
    for n,s in data['results'].items():
        m=s['Metrics'];lines.append(f"| {n} | {s['Collisions']} | {s['CollisionsChangingHeight']} | {s['AnchorMismatches']} | {m['input']['Exact']} | {m['height']['Exact']} | {m['final']['Exact']} |")
    lines+=['','## Error magnitudes','',
        'Units: input/final/height/near in metres; angular in radians. Large full-H extrema occur at the polar geographic singularity described in the main report. Near field is evaluated as an authority sensitivity test even for broad-patch stress points; this is not a rendered displacement measurement.','',
        '| Encoding | Input maximum / RMS | Angular maximum / RMS | Full H maximum / RMS | Near H maximum / RMS | Final position maximum / RMS |',
        '|---|---:|---:|---:|---:|---:|']
    for n,s in data['results'].items():
        values=[f"{s['Metrics'][k]['Max']:.12g} / {s['Metrics'][k]['Rms']:.12g}" for k in ['input','angular','height','near','final']]
        lines.append('| '+n+' | '+' | '.join(values)+' |')
    lines+=['','## Near-camera subset','',
        '1,024 observations, split camera and FP32 view at approximately 16 m, including a transverse axis crossing.','',
        '| Encoding | Collisions | H-input mismatches | Full H mismatches | Final-position mismatches |',
        '|---|---:|---:|---:|---:|']
    for n,s in data['results'].items():
        q=s['Subsets']['reachable-near-axis-camera'];lines.append(f"| {n} | {q['Collisions']} | {q['InputMismatches']} | {q['HeightMismatches']} | {q['FinalMismatches']} |")
    lines+=['','## Repetition','',f'Canonical JSON SHA-256, identical in three processes: `{digest}`.','',
        '| Encoding | Encoded + reconstructed stream SHA-256 |','|---|---|']
    lines += [f"| {n} | `{s['StreamSha256']}` |" for n,s in data['results'].items()]
    (HERE/'measurements.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
size=sum(p.stat().st_size for p in HERE.rglob('*') if p.is_file())
assert size<=256*1024,(size,'permanent evidence budget exceeded')
for p in HERE.iterdir():
    if p.suffix in ('.md','.py','.cs','.csproj','.json'):
        content=p.read_text(encoding='utf-8')
        assert content.endswith('\n'),(p,'missing final newline')
        assert all(line==line.rstrip() for line in content.splitlines()),(p,'trailing whitespace')
if (HERE/'closeout.json').exists():
    closeout=json.loads((HERE/'closeout.json').read_text(encoding='utf-8'))
    for item in closeout['files']:assert sha(HERE/item['path'])==item['sha256'],item['path']
    assert size==closeout['storage']['retainedBytes']
print(json.dumps({'result':'PASS','canonicalResultSha256':digest,'permanentBytesAtVerification':size,'status':git('status','--short')}))
