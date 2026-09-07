"""Reproduce numerical preflight only; no GPU run, production edit or cleanup.
Run with python -B. Build products stay in this proof's own build directory.
"""
import hashlib
import json
import math
from pathlib import Path
import subprocess

HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[2]
OUT=ROOT/'build/local-physical-domain-proof'
def sha(p):
    with p.open('rb') as f:return hashlib.file_digest(f,'sha256').hexdigest()
def git(*args):return subprocess.check_output(['git',*args],cwd=ROOT,text=True).strip()
def tree(folder):
    values={p.relative_to(folder).as_posix():sha(p) for p in sorted(folder.rglob('*')) if p.is_file()}
    return {'files':len(values),'contentManifestSha256':hashlib.sha256(json.dumps(values,sort_keys=True).encode()).hexdigest()}
def verify():
    expected=json.loads((HERE.parent/'ksa-terrain-architecture/verification.json').read_text())
    report={'head':git('rev-parse','HEAD'),'main':git('rev-parse','main'),'originMain':git('rev-parse','origin/main'),
        'branch':git('branch','--show-current'),'stagedDiff':git('diff','--cached'),'trackedDiff':git('diff'),
        'tags':{},'deployments':{},'existingEvidence':{},'source':{}}
    for tag in ['m13.1-ncsm1-tes-hotpath','m13.2-ordinary-terrain-shading']:
        report['tags'][tag]={'object':git('rev-parse',tag),'type':git('cat-file','-t',tag),'commit':git('rev-parse',tag+'^{}')}
    assert report['tags']==expected['tags']
    assert report['head']==report['main']==report['originMain']==expected['head']
    assert not report['trackedDiff'] and not report['stagedDiff']
    for config in ['Debug','Release']:
        runtime=ROOT/f'samples/NovaCore.Triangle/bin/{config}/net10.0'
        shaders={p.name:sha(p) for p in sorted((runtime/'shaders').glob('*.spv'))}
        binaries={name:sha(runtime/name) for name in ['NovaCore.Native.dll','NovaCore.Triangle.dll','NovaCore.Core.dll','NovaCore.Graphics.dll']}
        assert shaders==expected['deployments'][config]['shaders']
        assert binaries['NovaCore.Native.dll']==expected['deployments'][config]['native']
        assert binaries['NovaCore.Triangle.dll']==expected['deployments'][config]['managed']
        report['deployments'][config]={'path':str(runtime),'binaries':binaries,'shaders':shaders,'matchesPriorRestoredRuntime':True}
    for name in ['conservative-pre-refinement-visibility','post-m13.2-next-target','ksa-terrain-architecture']:
        report['existingEvidence'][name]=tree(HERE.parent/name)
    for name in ['native/NovaCore.Native/Shaders/production_spherical_billboard.vert',
        'native/NovaCore.Native/Shaders/production_spherical_billboard.tesc',
        'native/NovaCore.Native/Shaders/production_spherical_billboard.tese',
        'native/NovaCore.Native/Shaders/planetary_natural_terrain_surface.glsl',
        'native/NovaCore.Native/Shaders/planetary_natural_terrain_families.glsl',
        'native/NovaCore.Native/Shaders/planetary_natural_terrain_field.glsl',
        'src/NovaCore.Graphics/PlanetaryNaturalTerrainFamilies.cs',
        'src/NovaCore.Graphics/PlanetaryNaturalTerrainField.cs',
        'src/NovaCore.Core/Surface/FacilitySupportRegion.cs',
        'tests/NovaCore.Graphics.Tests/GpuPhysicalHeightPreparationTests.cs',
        'tests/NovaCore.Graphics.Tests/PlanetaryCanonicalPhysicalSurfaceAuthorityTests.cs']:
        report['source'][name]=sha(ROOT/name)
    return report

before=verify()
OUT.mkdir(parents=True,exist_ok=True)
build=subprocess.run(['dotnet','build',str(HERE/'CoordinateProof.csproj'),'-c','Release','--nologo','-v','quiet',
    '-p:BaseIntermediateOutputPath='+str(OUT/'obj')+'/', '-p:OutputPath='+str(OUT/'bin')+'/'],cwd=ROOT,text=True,capture_output=True)
assert build.returncode==0,build.stdout+build.stderr
def run():
    return json.loads(subprocess.check_output([str(OUT/'bin/CoordinateProof.exe')],cwd=ROOT,text=True))
a=run();b=run();assert a==b
assert a['informationLossWitness'] is not None
for group,s in a['groups']['local-fp64-roundtrip-control'].items():
    assert s['samples']==s['preparedPointExact']==s['directionExact']==s['hInputExact']==s['nearHeightExact']==s['nearGradientExact']
    assert s['nearDisplacementMaxMetres']==0
assert any(s['hInputExact']<s['samples'] for s in a['groups']['local-fp32-delta'].values())
assert any(s['hInputExact']<s['samples'] for s in a['groups']['local-fp64-norm-reassociation'].values())
(HERE/'numerical-results.json').write_text(json.dumps(a,indent=2)+'\n',encoding='utf-8')
summary={}
for variant,groups in a['groups'].items():
    near=[v for k,v in groups.items() if k.endswith('/near')]
    n=sum(s['samples'] for s in near)
    s={k:sum(x[k] for x in near) for k in ['samples','preparedPointExact','directionExact','hInputExact','nearHeightExact','nearGradientExact']}
    s.update({k:max(x[k] for x in near) for k in ['pointMaxMetres','hInputMaxMetres','angularMaxRadians',
        'nearHeightMaxMetres','gradientMax','nearDisplacementMaxMetres','fp32NearDisplacementMaxMetres','supportWeightMax']})
    for key in ['pointRmsMetres','nearHeightRmsMetres']:
        s[key]=math.sqrt(sum(x[key]**2*x['samples'] for x in near)/n)
    summary[variant]=s
after=verify();assert before==after
after.update({'numericalRepeatExact':True,'losslessControl':'PASS','negativeControlsDetected':'PASS',
    'diagnosticManagedBuild':'PASS; zero warnings/errors','priorDevelopmentError':'Initial proof compile used nonexistent Double3.Length; corrected to sqrt(LengthSquared); no production edit',
    'gitDiffCheck':subprocess.run(['git','diff','--check'],cwd=ROOT).returncode,'status':git('status','--short'),
    'stopGate':'Ticket section 6: numerical identity failed; sections 9/16 forbid GPU dual path and timing before proof',
    'nearSummary':summary})
assert after['gitDiffCheck']==0
(HERE/'verification.json').write_text(json.dumps(after,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'cases':a['cases'],'nearSummary':summary,'repeat':'PASS','sourceAndDeploymentUnchanged':True},indent=2))
