# Explicit bounded diagnostic. Temporary source is restored byte-for-byte in finally.
# Retained to reproduce the unresolved compound-contact policy witness; no production dependency.
param([switch]$Run)
$ErrorActionPreference='Stop'
if (!$Run) {throw 'Use -Run only after Project Control authorizes a fresh diagnostic reproduction.'}
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
Set-Location -LiteralPath $root
if ((git rev-parse HEAD) -ne '5537d08e4ab051a7f31bc638b5f717ef3ce4f3e0') {throw 'Wrong baseline; review reproduction first'}
$identity=Get-Content (Join-Path $PSScriptRoot 'current-draft-identity.json') -Raw | ConvertFrom-Json
foreach($file in $identity.files) {if ((Get-FileHash -LiteralPath $file.path).Hash -ne $file.sha256) {throw ('Source mismatch: '+$file.path)}}
$runtime=@'
// TEMPORARY bounded attribution, removed before permanent qualification.
using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using NovaCore.Core;

namespace NovaCore.Simulation.Spacecraft.Contact.Staging;

internal sealed class LocalContactProbe(BepuPhysics.Simulation simulation, BodyHandle body)
{
    internal readonly record struct Contact(int Child, int Feature, float Depth, Double3 Normal,
        Double3 OffsetFromA, Double3 BodyPositionAtCallback, DoubleQuaternion BodyOrientationAtCallback, bool DynamicA, bool Convex);
    internal readonly Contact[] Contacts = new Contact[64];
    internal int Count;
    internal bool Overflow;
    internal void Record<T>(int child, CollidablePair pair, ref T manifold) where T : unmanaged, IContactManifold<T>
    {
        var bodyState = simulation.Bodies.GetBodyReference(body);
        for (var i = 0; i < manifold.Count; i++)
        {
            if (Count == Contacts.Length) { Overflow = true; return; }
            var n = manifold.GetNormal(i); var p = manifold.GetOffset(i); var position = bodyState.Pose.Position;
            var q = bodyState.Pose.Orientation;
            Contacts[Count++] = new(child, manifold.GetFeatureId(i), manifold.GetDepth(i), new(n.X,n.Y,n.Z), new(p.X,p.Y,p.Z),
                new(position.X,position.Y,position.Z), new(q.X,q.Y,q.Z,q.W), pair.A.Mobility == CollidableMobility.Dynamic, typeof(T)==typeof(ConvexContactManifold));
        }
    }
}

internal sealed partial class LocalContactWorld
{
    internal LocalContactProbe EnableProbeForTest() => metrics.Probe = new(simulation, body);
    internal readonly record struct RawProbe(Double3 Position, DoubleQuaternion Orientation, Double3 Velocity,
        Double3 AngularVelocity, int Body, int Static, float SpeculativeMargin, int Constraints, long Generation, long Frontier,
        Double3 SlabPosition, Double3 SlabDimensions);
    internal RawProbe ReadProbeForTest()
    {
        var s = simulation.Bodies.GetBodyReference(body);var q=s.Pose.Orientation;
        var slab=simulation.Statics.GetStaticReference(plane); ref var box=ref simulation.Shapes.GetShape<Box>(surfaceShape.Index);
        return new(FromFloat(s.Pose.Position), new(q.X,q.Y,q.Z,q.W), FromFloat(s.Velocity.Linear), FromFloat(s.Velocity.Angular),
            body.Value, plane.Value, s.Collidable.SpeculativeMargin, s.Constraints.Count, Generation, frontier,
            FromFloat(slab.Pose.Position),new(box.Width,box.Height,box.Length));
    }
}
'@
$test=@'
// TEMPORARY attribution runner, not an acceptance test or measurement-policy change.
using System.Text.Json;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Contact.Staging;
using NovaCore.Simulation.Transactions;

internal static partial class EngineeringContactArticleTests
{
    internal static void Probe()
    {
        using var f=new Fixture(tilted:true);
        var observer=f.World.EnableProbeForTest();
        var rows=new List<object>();
        double peak=0,geometryDifference=0; int peakStep=0,peakChild=-1;
        for(var step=0;step<=40;step++)
        {
            observer.Count=0;
            var before=f.World.ReadProbeForTest();
            var ticks=step==0?0:step%3==1?16666:16667;
            if(step>0)
            {
                Check(f.Credit(ticks).Status==ContactHostCreditStatus.Accepted,"probe host credit");
                Check(f.Service().Published==1,"probe exactly one private step/publication");
            }
            Check(!observer.Overflow,"bounded observer capacity");
            var raw=f.World.ReadProbeForTest();
            Check(f.World.Read(f.Engine,f.Configuration,f.Receipt,out var endpoint)==LocalContactStatus.Success,"probe endpoint");
            var cornerMinimum=Geometry(endpoint.Motion.PositionRoot,endpoint.Motion.BodyToRoot).MinY;
            var children=new object[3]; var supportMinimum=double.MaxValue; var deepest=-1;
            for(var i=0;i<3;i++)
            {
                var actual=f.World.ReadArticleChildForTest(i);
                var centre=raw.Position+raw.Orientation.Rotate(actual.CentreAssembly);
                var q=raw.Orientation*actual.Orientation;var h=actual.Dimensions*.5;
                // Independent matrix-row/support-function calculation, without Rotate or corner enumeration.
                var r10=2*(q.X*q.Y+q.Z*q.W); var r11=1-2*(q.X*q.X+q.Z*q.Z); var r12=2*(q.Y*q.Z-q.X*q.W);
                var radius=Math.Abs(r10)*h.X+Math.Abs(r11)*h.Y+Math.Abs(r12)*h.Z;
                var c=actual.CentreAssembly;
                var distance=raw.Position.Y+r10*c.X+r11*c.Y+r12*c.Z-radius-(raw.SlabPosition.Y+raw.SlabDimensions.Y*.5);
                if(distance<supportMinimum){supportMinimum=distance;deepest=i;}
                children[i]=new{index=i,dimensions=V(actual.Dimensions),offset=V(actual.CentreAssembly),world=V(centre),q=Q(q),distance,
                    contact=(endpoint.ArticleContactChildMask&(1<<i))!=0};
            }
            geometryDifference=Math.Max(geometryDifference,Math.Abs(cornerMinimum-supportMinimum));
            if(-supportMinimum>peak){peak=-supportMinimum;peakStep=step;peakChild=deepest;}
            var contacts=observer.Contacts.Take(observer.Count).Select(c=>new{child=c.Child,feature=c.Feature,depth=c.Depth,normal=V(c.Normal),
                offsetA=V(c.OffsetFromA),callbackBody=V(c.BodyPositionAtCallback),callbackQ=Q(c.BodyOrientationAtCallback),dynamicA=c.DynamicA,convex=c.Convex}).ToArray();
            rows.Add(new{step,ticks,dt=(float)(ticks/1_000_000d),before=new{p=V(before.Position),q=Q(before.Orientation),v=V(before.Velocity),w=V(before.AngularVelocity),margin=before.SpeculativeMargin},
                after=new{p=V(raw.Position),q=Q(raw.Orientation),v=V(raw.Velocity),w=V(raw.AngularVelocity),margin=raw.SpeculativeMargin,raw.Constraints,raw.Body,raw.Static,raw.Generation,raw.Frontier},
                canonical=new{p=V(endpoint.Motion.PositionRoot),q=Q(endpoint.Motion.BodyToRoot),v=V(endpoint.Motion.VelocityRoot),w=V(endpoint.Motion.AngularVelocityBody),tick=f.Clock.CurrentTime.Ticks,
                    revision=f.Engine.State.Revision.Value,debt=f.Clock.PendingSimulationDebt.Ticks,history=f.Engine.ProcessedPersistentContactCount},
                slab=new{p=V(raw.SlabPosition),size=V(raw.SlabDimensions)},cornerMinimum,supportMinimum,deepest,children,contacts});
        }
        Console.WriteLine("ARTICLE_TRANSIENT "+JsonSerializer.Serialize(new{peak,peakStep,peakChild,geometryDifference,rows}));
    }
    private static double[] V(Double3 v)=>[v.X,v.Y,v.Z];
    private static double[] Q(DoubleQuaternion q)=>[q.X,q.Y,q.Z,q.W];
}
'@
$callbacks='src/NovaCore.Simulation/Spacecraft/Contact/Staging/LocalContactCallbacks.cs'
$tests='tests/NovaCore.Simulation.Tests/EngineeringContactArticleTests.cs'
$program='tests/NovaCore.Simulation.Tests/Program.cs'
$newRuntime='src/NovaCore.Simulation/Spacecraft/Contact/Staging/LocalContactWorld.Probe.cs'
$newTest='tests/NovaCore.Simulation.Tests/EngineeringContactArticleProbe.cs'
foreach($path in @($newRuntime,$newTest)) {if (Test-Path -LiteralPath $path) {throw ('Existing probe must be reviewed: '+$path)}}
$output=Join-Path $root 'build/engineering-article-tilt-reproduction.txt'
if (Test-Path -LiteralPath $output) {throw 'Previous reproduction exists; review before another run'}
$saved=@{}
foreach($path in @($callbacks,$tests,$program)) {$saved[$path]=[IO.File]::ReadAllBytes((Join-Path $root $path))}
$utf8=[Text.UTF8Encoding]::new($false)
try {
    [IO.File]::WriteAllText((Join-Path $root $newRuntime),$runtime,$utf8)
    [IO.File]::WriteAllText((Join-Path $root $newTest),$test,$utf8)
    $text=[IO.File]::ReadAllText((Join-Path $root $callbacks))
    $text=$text.Replace('internal int ArticleChildMask;','internal int ArticleChildMask;'+[Environment]::NewLine+'    internal LocalContactProbe? Probe;')
    $text=$text.Replace('material = new PairMaterialProperties(.5f, 2f, new SpringSettings(30, 1));','material = new PairMaterialProperties(.5f, 2f, new SpringSettings(30, 1));'+[Environment]::NewLine+'        metrics.Probe?.Record(-1, pair, ref manifold);')
    $text=$text.Replace('var child = pair.A.Mobility == CollidableMobility.Dynamic ? childA : childB;','var child = pair.A.Mobility == CollidableMobility.Dynamic ? childA : childB;'+[Environment]::NewLine+'        metrics.Probe?.Record(child, pair, ref manifold);')
    [IO.File]::WriteAllText((Join-Path $root $callbacks),$text,$utf8)
    $text=[IO.File]::ReadAllText((Join-Path $root $tests)).Replace('internal static class EngineeringContactArticleTests','internal static partial class EngineeringContactArticleTests')
    [IO.File]::WriteAllText((Join-Path $root $tests),$text,$utf8)
    $text=[IO.File]::ReadAllText((Join-Path $root $program))
    $needle='if (args.Contains("--engineering-article-cheap", StringComparer.Ordinal))'
    $text=$text.Replace($needle,'if (args.Contains("--engineering-article-probe", StringComparer.Ordinal)) { EngineeringContactArticleTests.Probe(); return; }'+[Environment]::NewLine+$needle)
    [IO.File]::WriteAllText((Join-Path $root $program),$text,$utf8)
    dotnet build tests/NovaCore.Simulation.Tests/NovaCore.Simulation.Tests.csproj -c Debug --nologo
    if ($LASTEXITCODE -ne 0) {throw 'Diagnostic build failed'}
    dotnet tests/NovaCore.Simulation.Tests/bin/Debug/net10.0/NovaCore.Simulation.Tests.dll --engineering-article-probe | Set-Content -LiteralPath $output -Encoding utf8
    if ($LASTEXITCODE -ne 0) {throw 'Diagnostic run failed'}
}
finally {
    foreach($path in $saved.Keys) {[IO.File]::WriteAllBytes((Join-Path $root $path),$saved[$path])}
    foreach($path in @($newRuntime,$newTest)) {
        $absolute=[IO.Path]::GetFullPath((Join-Path $root $path))
        if (!$absolute.StartsWith($root+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) {throw 'Unsafe cleanup target'}
        if (Test-Path -LiteralPath $absolute) {Remove-Item -LiteralPath $absolute -Force}
    }
    foreach($file in $identity.files) {if ((Get-FileHash -LiteralPath $file.path).Hash -ne $file.sha256) {throw ('Restoration mismatch: '+$file.path)}}
    dotnet build tests/NovaCore.Simulation.Tests/NovaCore.Simulation.Tests.csproj -c Debug --nologo
    if ($LASTEXITCODE -ne 0) {throw 'Restored-source build failed'}
}
Write-Output ('Bounded diagnostic retained at '+$output)
