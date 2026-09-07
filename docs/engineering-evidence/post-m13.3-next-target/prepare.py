"""Build one private measurement host; restore production source/deployment bytes.

Budget: 10 MiB permanent records; no raw frame/GPU buffers in discovery.
Run from this repository with Python and the installed VS/Vulkan SDK.
"""
import hashlib,json,os,pathlib,shutil,subprocess,sys
sys.dont_write_bytecode=True
ROOT=pathlib.Path(__file__).resolve().parents[3]
HERE=pathlib.Path(__file__).resolve().parent
OUT=ROOT/'build/post-m13.3-next-target'
HOST=OUT/'host'
BASE='180eaf150ba5db6364e17dd48336690778f058f9'
CANDIDATE_MATERIAL='521435c525b1538f730fda69fb89b62ec10846c7cd870f966a2ee9076a93b598'
CANDIDATE_FRAGMENT='8c59838abb3d7ee6418695ea40f044ca1f04f5c3c9216296d5cc19bba6110d6b'
def sha(p):
    with p.open('rb') as f:return hashlib.file_digest(f,'sha256').hexdigest()
def run(args):subprocess.run(args,cwd=ROOT,check=True)
def git(*args):return subprocess.check_output(['git',*args],cwd=ROOT).decode().strip()
def verify():
    reference=json.loads((HERE.parent/'ksa-terrain-convergence/deployment.json').read_text())
    data={'head':git('rev-parse','HEAD'),'originMain':git('rev-parse','origin/main'),
          'branch':git('branch','--show-current'),'status':git('status','--short'),
          'staged':git('diff','--cached','--stat'),'deployment':{}}
    for tag in ['m13.1-ncsm1-tes-hotpath','m13.2-ordinary-terrain-shading','m13.3-prepared-physical-terrain']:
        data[tag]={'object':git('rev-parse',tag),'commit':git('rev-parse',tag+'^{}')}
    for config,expected in reference.items():
        folder=ROOT/f'samples/NovaCore.Triangle/bin/{config}/net10.0'
        actual={key:sha(folder/file) for key,file in [('native','NovaCore.Native.dll'),('managed','NovaCore.Triangle.dll'),('executable','NovaCore.Triangle.exe')]}
        actual['shaders']={p.name:sha(p) for p in (folder/'shaders').glob('*.spv')}
        build=ROOT/'build'/('native-ninja' if config=='Debug' else 'native-ninja-release')
        assert actual['native']==sha(build/'NovaCore.Native.dll'),(config,'native deployment')
        assert set(actual['shaders'])==set(expected['shaders'])
        for name,value in actual['shaders'].items():
            allowed={expected['shaders'][name]}
            if name=='planetary_production.frag.spv':allowed.add(CANDIDATE_FRAGMENT)
            assert value in allowed,(config,name)
            assert value==sha(build/'shaders'/name),(config,name,'build deployment')
        data['deployment'][config]=actual
    assets=[ROOT/'assets/earth/runtime/earth_elevation_8192x4096.r16',
        ROOT/'.novacore/cache/terrain/v1/sha256/38/38ec671f475896f2c0a674e952f4121f117b18b1446bd363e3596bada4bf47ae.nccube',
        ROOT/'.novacore/cache/terrain/v1/sha256/c4/c45c6d94e004e1a2927dc65d405a347b1800c619b22b2eb6b3543f3c445d3afe.nccube']
    data['assets']={str(p.relative_to(ROOT)):{'bytes':p.stat().st_size,'sha256':sha(p)} for p in assets}
    data['preparedDefault']=1
    assert '#define NOVACORE_PREPARED_RENDER_TERRAIN 1' in (ROOT/'native/NovaCore.Native/shaders/production_spherical_billboard_physical.glsl').read_text()
    return data
def verify_source():
    # Reproduction is pinned to this campaign's inputs, not a moving HEAD.
    names=git('ls-tree','-r','--name-only',BASE,'native/NovaCore.Native/shaders','native/NovaCore.Native','samples/NovaCore.Triangle','src/NovaCore.Graphics').splitlines()
    for name in names:
        if not name.endswith(('.glsl','.frag','.vert','.tesc','.tese','.comp','.cpp','.h','.inl','.cs','.csproj','.txt')):continue
        current=(ROOT/name).read_bytes().replace(b'\r\n',b'\n')
        original=subprocess.check_output(['git','show',BASE+':'+name],cwd=ROOT).replace(b'\r\n',b'\n')
        if name.endswith('/production_terrain_material.glsl') and hashlib.sha256(current).hexdigest()==CANDIDATE_MATERIAL:continue
        assert current==original,('reproduction input changed',name)
def build():
    OUT.mkdir(parents=True,exist_ok=True)
    verify_source()
    baseline=verify()
    if not (HERE/'baseline-identity.json').exists():(HERE/'baseline-identity.json').write_text(json.dumps(baseline,indent=2)+'\n')
    paths=['native/NovaCore.Native/NovaCoreNative.cpp','samples/NovaCore.Triangle/ProductionBillboardDesktopTraversal.cs']
    original={p:(ROOT/p).read_bytes() for p in paths}
    for p,v in original.items():assert subprocess.check_output(['git','show','HEAD:'+p],cwd=ROOT).replace(b'\r\n',b'\n')==v.replace(b'\r\n',b'\n')
    binary=ROOT/'build/native-ninja-release/NovaCore.Native.dll'
    saved=binary.read_bytes()
    try:
        # Reuse only the previously verified fixed-pose managed patch, never raw capture instrumentation.
        old=(HERE.parent/'post-m13.2-next-target/instrumentation.patch').read_text()
        managed=old[old.index('diff --git a/samples/NovaCore.Triangle/ProductionBillboardDesktopTraversal.cs'):]
        subprocess.run(['git','apply','-'],cwd=ROOT,input=managed.encode(),check=True)
        p=ROOT/paths[1];text=p.read_text(encoding='utf-8')
        anchor='        var performanceAltitude = Environment.GetEnvironmentVariable("NOVACORE_PERFORMANCE_ALTITUDE_METRES");'
        text=text.replace(anchor,'''        var geography=Environment.GetEnvironmentVariable("NOVACORE_PERFORMANCE_GEOGRAPHY");
        if(geography is not null){var degrees=geography.Split(',').Select(x=>double.Parse(x,CultureInfo.InvariantCulture)).ToArray();if(degrees.Length!=2||!double.IsFinite(degrees[0])||!double.IsFinite(degrees[1])||Math.Abs(degrees[0])>90||Math.Abs(degrees[1])>180)throw new InvalidOperationException("invalid diagnostic geography");_direction=NovaCore.Core.Surface.BodyFixedGeography.DirectionFromLatitudeLongitude(degrees[0]*Math.PI/180,degrees[1]*Math.PI/180);}
'''+anchor)
        p.write_text(text,encoding='utf-8')
        from instrument import instrument
        from capture import instrument_capture
        p=ROOT/paths[0];p.write_text(instrument_capture(instrument(p.read_text(encoding='utf-8'))),encoding='utf-8')
        (HERE/'host-instrumentation.patch').write_bytes(subprocess.check_output(['git','diff','--',*paths],cwd=ROOT))
        run(['pwsh','-NoProfile','-Command',"& 'C:/Program Files/Microsoft Visual Studio/18/Community/Common7/Tools/Launch-VsDevShell.ps1' -Arch amd64 -HostArch amd64 -SkipAutomaticLocation\ncmake --build build/native-ninja-release --target NovaCore.Native --parallel 4\nexit $LASTEXITCODE"])
        oracle=HOST/'earth-data/earth_elevation_8192x4096.r16';oracle.parent.mkdir(parents=True,exist_ok=True)
        if not oracle.exists():os.link(ROOT/'assets/earth/runtime'/oracle.name,oracle)
        run(['dotnet','build','samples/NovaCore.Triangle/NovaCore.Triangle.csproj','-c','Release','--no-restore','-o',str(HOST),'-v','quiet'])
    finally:
        for p,v in original.items():(ROOT/p).write_bytes(v)
        binary.write_bytes(saved)
    after=verify();assert baseline['deployment']==after['deployment'];assert baseline['assets']==after['assets']
    (HERE/'private-host.json').write_text(json.dumps({'native':sha(HOST/'NovaCore.Native.dll'),'managed':sha(HOST/'NovaCore.Triangle.dll'),'productionDeploymentUnchanged':True,'sourceRestored':True},indent=2)+'\n')
    print('Private host built. Production source, native build DLL and deployed Debug/Release identities preserved.')
if __name__=='__main__':build()
