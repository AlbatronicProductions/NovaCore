"""Bounded CPU proof. No GPU launch, production build/edit, or automatic deletion.
Run from any directory: python -B <this file>. Requires banked Release assemblies.
Permanent budget: 256 KiB. Scratch belongs only to build/exact-local-domain-encoding.
"""
import hashlib,json,pathlib,subprocess

HERE=pathlib.Path(__file__).resolve().parent
ROOT=HERE.parents[2]
SCRATCH=ROOT/'build/exact-local-domain-encoding'
def sha(p):
    with open(p,'rb') as f:return hashlib.file_digest(f,'sha256').hexdigest()
def git(*args):return subprocess.check_output(['git',*args],cwd=ROOT,text=True,encoding='utf-8').strip()
def save(name,data): (HERE/name).write_text(json.dumps(data,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
def deployments():
    expected=json.loads((HERE.parent/'local-physical-domain-proof/verification.json').read_text(encoding='utf-8'))['deployments']
    actual={}
    for config,old in expected.items():
        base=ROOT/'samples/NovaCore.Triangle/bin'/config/'net10.0'
        binaries={n:sha(base/n) for n in old['binaries']}
        shaders={p.name:sha(p) for p in sorted((base/'shaders').glob('*.spv'))}
        assert binaries==old['binaries'] and shaders==old['shaders'],f'{config}: deployment identity changed'
        actual[config]={'binaries':binaries,'shaders':shaders,'matchesPriorVerifiedDeployment':True}
    return actual

if __name__=='__main__':
    assert git('rev-parse','HEAD')=='4accf92fd080c16cc8656080aa69def3fa65db53'
    assert not git('diff','--name-only') and not git('diff','--cached','--name-only')
    before=deployments()
    SCRATCH.mkdir(parents=True,exist_ok=True)
    command=['dotnet','build',str(HERE/'EncodingProof.csproj'),'-c','Release','--nologo','-v','quiet',
        '-p:BaseIntermediateOutputPath='+str(SCRATCH/'obj')+'/', '-p:OutputPath='+str(SCRATCH/'bin')+'/']
    build=subprocess.run(command,cwd=ROOT,capture_output=True,text=True,encoding='utf-8')
    (SCRATCH/'build.log').write_text(build.stdout+build.stderr,encoding='utf-8')
    if build.returncode:print(build.stdout+build.stderr)
    build.check_returncode()
    runs=[]
    for i in range(3):
        output=subprocess.check_output(['dotnet',str(SCRATCH/'bin/EncodingProof.dll'),str(ROOT)],cwd=ROOT,text=True,encoding='utf-8')
        parsed=json.loads(output)
        encoded=json.dumps(parsed,sort_keys=True,separators=(',',':')).encode()
        runs.append(hashlib.sha256(encoded).hexdigest())
        if i==0:save('results.json',parsed)
    assert len(set(runs))==1,'nondeterministic CPU result'
    passing=parsed['results']['exact-two-diff-local']
    assert passing['Collisions']==passing['AnchorMismatches']==passing['RangeRejects']==0
    assert all(m['Mismatch']==0 for m in passing['Metrics'].values())
    assert all(parsed['results'][n]['CollisionsChangingHeight']>0 for n in ('single-fp32-control','split-fp32-local','fixed64-q2^-40','ksa-pattern'))
    after=deployments();assert before==after
    old=json.loads((HERE.parent/'local-physical-domain-proof/scratch-manifest.json').read_text(encoding='utf-8'))
    previous=[{'path':x['path'],'bytes':pathlib.Path(x['path']).stat().st_size,'sha256':sha(x['path'])} for x in old['files'] if pathlib.Path(x['path']).is_file()]
    prior_dirs=['conservative-pre-refinement-visibility','ksa-terrain-architecture','local-physical-domain-proof','post-m13.2-next-target']
    prior={str(p.relative_to(ROOT)):sha(p) for d in prior_dirs for p in sorted((HERE.parent/d).rglob('*')) if p.is_file()}
    paths=[
        'Content/Core/Shaders/Common/PrecisionFuncs.glsl',
        'Content/Core/Shaders/Planet/TerrainMesh/MeshDataCommon.glsl',
        'Content/Core/Shaders/Planet/TerrainMesh/PrepareModifiers.comp',
        'Content/Core/Shaders/Planet/TerrainMesh/FinalizeModifiers.comp',
        'Content/Core/Shaders/Planet/PlanetTessEvaluation.tese',
        'KSA.dll','Planet.Core.dll','Planet.Render.Core.dll']
    ksa={str(pathlib.Path('E:/Kitten Space Agency')/p):sha(pathlib.Path('E:/Kitten Space Agency')/p) for p in paths}
    for p in ['build/ksa-residency-reference/source/KSA.PlanetRenderer.cs','build/ksa-residency-reference/assembly-source/KSA/CubeMesh.cs']:
        ksa[str(ROOT/p)]=sha(ROOT/p)
    files=[{'path':str(p),'bytes':p.stat().st_size,'sha256':sha(p)} for p in sorted(SCRATCH.rglob('*')) if p.is_file()]
    save('scratch-manifest.json',{'root':str(SCRATCH),'files':files,'count':len(files),'bytes':sum(f['bytes'] for f in files)})
    refs={r:git('rev-parse',r) for r in ['HEAD','main','origin/main','m13.1-ncsm1-tes-hotpath','m13.1-ncsm1-tes-hotpath^{}','m13.2-ordinary-terrain-shading','m13.2-ordinary-terrain-shading^{}']}
    subprocess.run(['git','diff','--check'],cwd=ROOT,check=True)
    save('verification.json',{'refs':refs,'branch':git('branch','--show-current'),'status':git('status','--short'),
        'trackedDiff':git('diff','--stat'),'stagedDiff':git('diff','--cached','--stat'),'diffCheck':'PASS',
        'build':'Release PASS: zero warnings/errors','repeatCanonicalJsonSha256':runs,
        'binarySha256':sha(SCRATCH/'bin/EncodingProof.dll'),'sourceSha256':sha(HERE/'EncodingProof.cs'),
        'deployments':after,'ksaSourceProvenance':ksa,'priorEvidenceFiles':prior,
        'previousScratch':{'count':len(previous),'bytes':sum(p['bytes'] for p in previous),'files':previous},
        'gpuWork':'NOT RUN: CPU/cost gate did not authorize GPU prototype'})
    print(json.dumps({'repeatHashes':runs,'cases':parsed['cases'],'scratchFiles':len(files),'scratchBytes':sum(f['bytes'] for f in files)}))
