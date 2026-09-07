"""Build a private diagnostic host; restore the two temporary host edits exactly."""
import argparse,hashlib,json,os,pathlib,subprocess
ROOT=pathlib.Path(__file__).resolve().parents[3]
HERE=pathlib.Path(__file__).resolve().parent
OUT=ROOT/'build/ksa-terrain-convergence'
def command(args):subprocess.run(args,cwd=ROOT,check=True)
if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--baseline',action='store_true');baseline=parser.parse_args().baseline
    OUT.mkdir(parents=True,exist_ok=True)
    original={p:(ROOT/p).read_bytes() for p in ['native/NovaCore.Native/NovaCoreNative.cpp','samples/NovaCore.Triangle/ProductionBillboardDesktopTraversal.cs','samples/NovaCore.Triangle/Program.cs']}
    for p,v in original.items():
        expected=subprocess.check_output(['git','show','HEAD:'+p],cwd=ROOT).replace(b'\r\n',b'\n')
        if p.endswith('/Program.cs'):
            expected=expected.replace(b'        var candidateAltitude=Math.Max(10d,Math.Sqrt(cameraBody.LengthSquared)-PlanetarySphericalBillboardNaturalTerrainProof.EarthRadiusMetres);',b'        // Render sampling follows physical ground clearance, already resolved by the scene.\n        var candidateAltitude=Math.Max(10d,gpu.SurfaceAltitudeMetres);').replace(b'var altitude=Math.Max(10d,Math.Sqrt(cameraBody.LengthSquared)-PlanetarySphericalBillboardNaturalTerrainProof.EarthRadiusMetres);',b'var altitude=Math.Max(10d,gpu.SurfaceAltitudeMetres);')
        assert expected==v.replace(b'\r\n',b'\n')
    patch=HERE.parent/'post-m13.2-next-target/instrumentation.patch'
    command(['git','apply','--check',str(patch)])
    try:
        command(['git','apply',str(patch)])
        if baseline:
            p='samples/NovaCore.Triangle/Program.cs'
            (ROOT/p).write_bytes(subprocess.check_output(['git','show','HEAD:'+p],cwd=ROOT))
        p=ROOT/'native/NovaCore.Native/NovaCoreNative.cpp'
        text=p.read_text(encoding='utf-8').replace('16777216ull*128','65536ull*128').replace('samples>16777216','samples>65536')
        p.write_text(text,encoding='utf-8')
        p=ROOT/'samples/NovaCore.Triangle/ProductionBillboardDesktopTraversal.cs'
        text=p.read_text(encoding='utf-8')
        anchor='        var performanceAltitude = Environment.GetEnvironmentVariable("NOVACORE_PERFORMANCE_ALTITUDE_METRES");'
        assert anchor in text
        text=text.replace(anchor,'''        var geography = Environment.GetEnvironmentVariable("NOVACORE_PERFORMANCE_GEOGRAPHY");
        if (geography is not null)
        {
            var degrees = geography.Split(',').Select(x => double.Parse(x, CultureInfo.InvariantCulture)).ToArray();
            if (degrees.Length != 2 || !double.IsFinite(degrees[0]) || !double.IsFinite(degrees[1]) ||
                Math.Abs(degrees[0]) > 90 || Math.Abs(degrees[1]) > 180)
                throw new InvalidOperationException("Invalid diagnostic latitude/longitude.");
            _direction = NovaCore.Core.Surface.BodyFixedGeography.DirectionFromLatitudeLongitude(degrees[0]*Math.PI/180d,degrees[1]*Math.PI/180d);
        }
'''+anchor)
        p.write_text(text,encoding='utf-8')
        p=ROOT/'samples/NovaCore.Triangle/Program.cs';text=p.read_text(encoding='utf-8')
        anchor='SolarSystemScene.TryCreateAt(root,SimulationInstant.Zero,out sol,out var solError)'
        assert anchor in text
        text=text.replace(anchor,'SolarSystemScene.TryCreateAt(root,SimulationInstant.FromSecondsRounded(double.Parse(Environment.GetEnvironmentVariable("NOVACORE_PERFORMANCE_EPOCH_SECONDS") ?? "0",System.Globalization.CultureInfo.InvariantCulture)),out sol,out var solError)')
        p.write_text(text,encoding='utf-8')
        (OUT/('host-baseline-reproduced.patch' if baseline else 'host-reproduced.patch')).write_bytes(subprocess.check_output(['git','diff','--',*original.keys()],cwd=ROOT))
        command(['pwsh','-NoProfile','-Command',"& 'C:/Program Files/Microsoft Visual Studio/18/Community/Common7/Tools/Launch-VsDevShell.ps1' -Arch amd64 -HostArch amd64 -SkipAutomaticLocation\ncmake --build build/native-ninja-release\nexit $LASTEXITCODE"])
        destination=OUT/('host-baseline' if baseline else 'host')
        oracle=destination/'earth-data/earth_elevation_8192x4096.r16';oracle.parent.mkdir(parents=True,exist_ok=True)
        if not oracle.exists():os.link(ROOT/'assets/earth/runtime'/oracle.name,oracle)
        command(['dotnet','build','samples/NovaCore.Triangle/NovaCore.Triangle.csproj','-c','Release','--no-restore','-o',str(destination)])
    finally:
        for p,v in original.items():(ROOT/p).write_bytes(v)
    print('Private diagnostic host built; source-only host instrumentation restored.')
