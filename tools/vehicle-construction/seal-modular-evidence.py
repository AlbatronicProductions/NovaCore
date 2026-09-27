"""Read-only entry preservation check and explicit campaign evidence seal.

Only the requested evidence JSON is written. No Git index/ref/history commands.
The expected changes are explicit; a newly authorized owner must be reviewed here.
"""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess

ROOT=Path(__file__).resolve().parents[2]
EVIDENCE=ROOT/'docs/engineering-evidence/modular-craft-first-playable'
ALLOWED={
    'src/NovaCore.Simulation/Spacecraft/Assemblies/'+name for name in (
        'AssemblyDesign.cs','AssemblyConstructionDefinitions.cs','AssemblyConstructionEditor.cs',
        'AssemblyConstructionGraph.cs','AssemblyConstructionFuel.cs','AssemblyConstructionPower.cs')}
ALLOWED.update(('tests/NovaCore.Simulation.Tests/Program.cs','tests/NovaCore.Graphics.Tests/Program.cs',
                'tests/NovaCore.Simulation.Tests/AssemblyConstructionTests.Measurements.cs'))
GATE5_ALLOWED={
    'native/NovaCore.Native/NovaCoreNative.cpp','native/NovaCore.Native/NovaCoreNative.h',
    'native/NovaCore.Native/shaders/triangle.vert','src/NovaCore.Interop/NativeRuntime.cs',
    'src/NovaCore.Graphics/ReusablePartVisuals.cs','tests/NovaCore.Graphics.Tests/NovaCore.Graphics.Tests.csproj',
    'tools/NovaCore.ConstructionEditor/Program.cs','tools/NovaCore.ConstructionEditor/NovaCore.ConstructionEditor.csproj'}


def digest(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def git(*args): return subprocess.check_output(['git',*args],cwd=ROOT,text=True).splitlines()


def main():
    parser=argparse.ArgumentParser();parser.add_argument('--gate',type=int,required=True);args=parser.parse_args()
    if args.gate>=5: ALLOWED.update(GATE5_ALLOWED)
    if args.gate>=8: ALLOWED.update('src/NovaCore.Simulation/Spacecraft/Assemblies/'+name for name in ('AssemblyDynamics.cs','AssemblyProfileAdmission.cs'))
    if args.gate>=9: ALLOWED.update((
        'src/NovaCore.Simulation/Spacecraft/Assemblies/AssemblyConstructionRuntime.cs',
        'src/NovaCore.Simulation/Spacecraft/Assemblies/AssemblyFloridaSite.cs',
        'src/NovaCore.Simulation/Spacecraft/SpacecraftStateStore.Construction.cs',
        'src/NovaCore.Simulation/Spacecraft/Translation/SpacecraftMotionEvaluator.cs',
        'src/NovaCore.Simulation/Spacecraft/ReferenceFrames/SpacecraftReferenceFrameEvaluator.cs',
        'src/NovaCore.Simulation/Transactions/SimulationTransactionEngine.Construction.cs',
        'src/NovaCore.Simulation/Transactions/SimulationTransactionEngine.AssemblyControl.cs',
        'src/NovaCore.Simulation/Spacecraft/Contact/Staging/LocalContactSource.cs',
        'src/NovaCore.Simulation/Spacecraft/Contact/Staging/LocalContactWorld.cs',
        'src/NovaCore.Graphics/SceneObjectFocusObservation.cs',
        'samples/NovaCore.Triangle/SolarSystemScene.cs','samples/NovaCore.Triangle/StockAssemblyDevelopmentScene.cs'))
    if args.gate>=10: ALLOWED.update((
        'src/NovaCore.Simulation/Spacecraft/Assemblies/AssemblyControl.cs',
        'src/NovaCore.Simulation/Spacecraft/Contact/Staging/LocalContactCallbacks.cs',
        'src/NovaCore.Simulation/Spacecraft/Contact/Staging/LocalContactWorld.Publication.cs',
        'src/NovaCore.Graphics/EarthElevationDataset.cs',
        'src/NovaCore.Graphics/PlanetaryLocalTerrain.cs',
        'src/NovaCore.Graphics/PlanetaryNaturalTerrainFamilies.cs',
        'src/NovaCore.Graphics/PlanetaryPhysicalSurfacePointQuery.cs',
        'samples/NovaCore.Triangle/PlayerFlightControlInput.cs'))
    if args.gate>=12: ALLOWED.update((
        'samples/NovaCore.Triangle/Program.cs','samples/NovaCore.Triangle/SampleOptions.cs'))
    entry=json.loads((EVIDENCE/'entry.json').read_text());changed=[];missing=[];unexpected=[]
    for row in entry['files']:
        p=ROOT/row['path']
        if not p.is_file(): missing.append(row['path']);continue
        current=digest(p)
        if current!=row['sha256']:
            changed.append(dict(path=row['path'],entrySha256=row['sha256'],sha256=current))
            if row['path'] not in ALLOWED:unexpected.append(row['path'])
    refs=git('show-ref'); public=lambda values: sorted(v for v in values if ' refs/codex/turn-diffs/' not in v)
    index=ROOT/git('rev-parse','--git-path','index')[0]
    head=git('rev-parse','HEAD')[0];worktrees=git('worktree','list','--porcelain')
    checks=dict(headPreserved=head==entry['head'],indexPreserved=digest(index)==entry['indexSha256'],
                publicRefsPreserved=public(refs)==public(entry['refs']),worktreesPreserved=worktrees==entry['worktrees'])
    paths=[p for p in ROOT.glob('src/NovaCore.Simulation/Spacecraft/Assemblies/*.cs') if p.name.startswith(('PartStandard','PartCompatibility','CraftDocument','CompiledCraft','AssemblyConstruction'))]
    paths+=list(ROOT.glob('tests/NovaCore.Simulation.Tests/ModularCraftTests*.cs'))
    paths += [ROOT/p for p in ALLOWED]
    paths+=list((ROOT/'assets/vehicles/modular-starter').glob('*'))
    paths+=list((ROOT/'tools/vehicle-construction').glob('*modular*'))
    paths+=[ROOT/'tests/NovaCore.Graphics.Tests/ModularGreyboxAssetTests.cs']
    if args.gate>=5:
        paths+=list(ROOT.glob('tools/NovaCore.ConstructionEditor/*.cs'))
        paths += [ROOT/'src/NovaCore.Graphics/PartVisualPicking.cs',ROOT/'tests/NovaCore.Graphics.Tests/ModularViewportTests.cs']
    if args.gate>=8:
        paths += [ROOT/'src/NovaCore.Simulation/Spacecraft/Assemblies/AssemblyDynamics.Craft.cs',ROOT/'src/NovaCore.Simulation/Spacecraft/Contact/Staging/LocalContactWorld.CraftPreparation.cs']
    if args.gate>=9:
        paths += [ROOT/'src/NovaCore.Simulation/Transactions/SimulationTransactionEngine.ConstructionPhysical.cs']
        paths += list(ROOT.glob('tests/NovaCore.Graphics.Tests/ModularFloridaTests*.cs'))
    if args.gate>=10:
        paths += [ROOT/'src/NovaCore.Core/Surface/IPhysicalSurfaceHeightBounds.cs',
                  ROOT/'src/NovaCore.Simulation/Spacecraft/Assemblies/CraftClearance.cs',
                  ROOT/'src/NovaCore.Simulation/Spacecraft/Contact/Staging/LocalContactWorld.Craft.cs',
                  ROOT/'src/NovaCore.Simulation/Spacecraft/Contact/Staging/LocalContactWorld.CraftDiagnostics.cs']
    if args.gate>=12:
        paths += [ROOT/'src/NovaCore.Simulation/Spacecraft/Assemblies/CraftLaunchAdmission.cs']
        paths += [ROOT/'samples/NovaCore.Triangle'/name for name in (
            'IApplicationVesselScene.cs','CraftFlightOptions.cs','ConstructionFlightScene.cs',
            'ConstructionFlightMeasurements.cs')]
    paths=sorted({p for p in paths if p.is_file()})
    result=dict(schema='novacore.modular-gate-seal/1',gate=args.gate,head=head,**checks,entryFiles=len(entry['files']),
                unchangedEntryFiles=len(entry['files'])-len(changed)-len(missing),changedEntryFiles=changed,missing=missing,unexpected=unexpected,
                refDelta=dict(removed=sorted(set(entry['refs'])-set(refs)),added=sorted(set(refs)-set(entry['refs']))),
                source=[dict(path=p.relative_to(ROOT).as_posix(),sha256=digest(p)) for p in paths])
    if missing or unexpected or not all(checks.values()):raise SystemExit(json.dumps(result,indent=2))
    out=EVIDENCE/f'gate{args.gate}-seal.json';out.write_text(json.dumps(result,indent=2)+'\n')
    print(f'Gate {args.gate} preservation PASS: {result["unchangedEntryFiles"]}/{len(entry["files"])} unchanged; {len(changed)} authorized changes; {len(paths)} source seals')


if __name__=='__main__':main()
