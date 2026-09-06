"""Verify final, uninstrumented candidate deployment and preserved inputs."""
import hashlib,json,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
HERE=Path(__file__).resolve().parent

def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def verify():
    baseline=json.loads((HERE/'baseline.json').read_text())
    for relative,digest in baseline['retainedEvidence'].items():
        assert sha(ROOT/relative)==digest,relative
    record={'configurations':{},'incomingEvidenceExact':True}
    for config,native in [('Debug','native-ninja'),('Release','native-ninja-release')]:
        source=ROOT/'build'/native
        buildShaders={p.name:sha(p) for p in (source/'shaders').glob('*.spv')}
        expected={name:buildShaders[name] for name in baseline['shaders']}
        assert len(expected)==49
        assert all(digest==expected[name] for name,digest in baseline['shaders'].items() if name!='planetary_production.frag.spv')
        paths=[ROOT/f'samples/NovaCore.Triangle/bin/{config}/net10.0',ROOT/f'tests/NovaCore.Graphics.Tests/bin/{config}/net10.0']
        for path in paths:
            assert sha(path/'NovaCore.Native.dll')==sha(source/'NovaCore.Native.dll')
        assert {p.name:sha(p) for p in (paths[0]/'shaders').glob('*.spv')}==expected
        # Graphics query tests load their configuration-specific build shaders
        # directly; the Triangle deployment contains only the 49 runtime shaders.
        record['configurations'][config]=dict(nativeSource=str(source/'NovaCore.Native.dll'),nativeSha256=sha(source/'NovaCore.Native.dll'),shaderHashes=expected,graphicsTestShaderRoot=str(source/'shaders'),allBuildShaderHashes=buildShaders,verifiedNativeDeploymentPaths=list(map(str,paths)),runtimeExeSha256=sha(paths[0]/'NovaCore.Triangle.exe'),runtimeManagedSha256=sha(paths[0]/'NovaCore.Triangle.dll'),elevationSha256=sha(paths[0]/'earth-data/earth_elevation_8192x4096.r16'))
    assert record['configurations']['Debug']['shaderHashes']==record['configurations']['Release']['shaderHashes']
    release=ROOT/'tools/NovaCore.Launcher/bin/Release/net10.0-windows'
    record['launcher']={p.name:sha(p) for p in [release/'NovaCore.Launcher.exe',release/'NovaCore.Launcher.dll']}
    record['productionSource']={p:sha(ROOT/p) for p in ['native/NovaCore.Native/NovaCoreNative.cpp','native/NovaCore.Native/shaders/planetary_production.frag','tests/NovaCore.Graphics.Tests/PlanetaryBillboardSurfaceWorkloadTests.cs']}
    record['unchangedPhysicalSource']=subprocess.check_output(['git','diff','--exit-code','HEAD','--','native/NovaCore.Native/shaders/production_spherical_billboard.tese','native/NovaCore.Native/shaders/production_spherical_billboard.tesc','samples/NovaCore.Triangle/ProductionBillboardDesktopTraversal.cs'],cwd=ROOT).decode()==''
    for ref in ['HEAD','origin/main','m13.1-ncsm1-tes-hotpath^{}']:
        value=subprocess.check_output(['git','rev-parse',ref],cwd=ROOT,text=True).strip();assert value==baseline['head'];record[ref]=value
    assert not subprocess.check_output(['git','diff','--cached','--name-only'],cwd=ROOT)
    record['gitStatus']=subprocess.check_output(['git','status','--short'],cwd=ROOT,text=True)
    (HERE/'final-identity.json').write_text(json.dumps(record,indent=2)+'\n',encoding='utf-8')
    print('Final native, 49 shaders per runtime deployment, test build shaders, unchanged physical sources, incoming evidence and Git references PASS')
if __name__=='__main__':verify()
