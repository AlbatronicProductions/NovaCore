using System.Reflection;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Spacecraft.Contact.Staging;

// Qualification-only read observer. No callback inside the solver, raw manifold
// capture, preparation, command, or canonical publication is introduced here.
internal readonly record struct SurfaceRetryObservation(long Epoch,long Sequence,int Consumer,
    Double3 Position,Double3 Velocity,Double3 Angular,double Mass,bool Main,int Jets,int Contacts,
    long World,long Frontier,int TerrainGeneration,int Tiles,int Triangles,int TerrainContacts,
    string Pair,int Feature0,int Feature1,int Feature2,int Feature3,double Depth,Double3 Impulse,bool RawCapture)
{
    private static object? Field(object o,string name)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(o);
    internal static SurfaceRetryObservation Read(ConstructionApplicationSession s)
    {
        if(s.Engine.ObserveConstructionServices(s.Authority,out var state)!=ConstructionServiceStatus.Ready||state?.Physical is null)throw new InvalidDataException("Retry observation unavailable.");
        s.Engine.ObserveConstructionActuation(s.Authority,out var act);s.Engine.ObserveConstructionContactPoints(s.Authority,out var count);
        var p=state.Physical;var world=s.Engine.ConstructionContactWorldForTest(s.Authority);
        var terrain=world is null?null:((LocalContactMetrics)Field(world,"metrics")!).Terrain;
        var mesh=world is null?null:(CraftTerrainColliders?)Field(world,"craftTerrain");
        var pair=terrain is null?default:(BepuPhysics.CollisionDetection.CollidablePair)Field(terrain,"pair")!;
        if(terrain?.ContactCount>0&&!terrain.Owns(pair))throw new InvalidDataException("Foreign terrain manifold.");
        var rows=terrain is null?null:(BepuPhysics.CollisionDetection.Contact[])Field(terrain,"rows")!;
        int n=terrain?.ContactCount??0;int Feature(int i)=>i<n?rows![i].FeatureId:-1;
        return new(state.Epoch.Ticks,state.Sequence,(int)p.Consumer,p.Motion.PositionO,p.Motion.VelocityO,p.Motion.AngularVelocityBody,state.ReferenceMass!.Value.Mass,act.Main,act.Jets.Count,count,
            world?.Generation??0,world is null?0:(long)Field(world,"frontier")!,mesh?.Generation??0,mesh?.TileCount??0,mesh?.TriangleCount??0,n,
            n==0?"":$"body={pair.A.BodyHandle.Value}/surface={pair.B.StaticHandle.Value}",Feature(0),Feature(1),Feature(2),Feature(3),terrain?.MaximumDepth??0,terrain?.LinearImpulse??default,
            terrain?.RawForTest is not null||world is not null&&Field(world,"craftDiagnosticSamples") is not null);
    }
}

internal enum SurfaceRetryAction { None,Roll,Release,Ignite,Complete }
internal sealed class SurfaceRetryRoute
{
    private readonly long groundedHold;
    internal SurfaceRetryRoute(long groundedHoldMicroseconds=500_000)
    {
        if(groundedHoldMicroseconds<500_000||groundedHoldMicroseconds>15_000_000)throw new ArgumentOutOfRangeException(nameof(groundedHoldMicroseconds));
        groundedHold=groundedHoldMicroseconds;
    }
    private long freeFlightSince;
    internal int Stage {get;private set;}
    internal long Start {get;private set;}=-1;
    private long steady=-1,phase,activeRcs=-1;
    private double groundedMass,rcsMass,groundedHeight;
    internal bool TerrainSeen {get;private set;}
    internal bool GroundedRcs {get;private set;}
    internal bool Powered {get;private set;}
    internal SurfaceRetryAction Observe(SurfaceRetryObservation v)
    {
        if(Start<0)Start=v.Epoch;
        if(v.RawCapture||v.Epoch<Start||v.Epoch-Start>25_000_000)throw new InvalidDataException("Surface retry diagnostic/epoch/deadline assertion.");
        TerrainSeen|=v.TerrainContacts>0&&v.TerrainGeneration>0&&v.Frontier>0;
        if(Stage==0){
            bool rest=v.TerrainContacts>0&&v.Velocity.LengthSquared<.000004&&v.Angular.LengthSquared<.000004;
            if(!rest)steady=-1;else if(steady<0)steady=v.Epoch;
            if(steady>=0&&v.Epoch-steady>=groundedHold){groundedMass=v.Mass;groundedHeight=v.Position.Y;phase=v.Epoch;Stage=1;return SurfaceRetryAction.Roll;}
        }else if(Stage==1){
            if(v.Jets>0&&activeRcs<0)activeRcs=v.Epoch;
            GroundedRcs|=v.TerrainContacts>0&&v.Jets>0&&v.Mass<groundedMass;
            if(activeRcs>=0&&v.Epoch-activeRcs>=62_500){if(!GroundedRcs)throw new InvalidDataException("No real grounded RCS/resource witness.");rcsMass=v.Mass;phase=v.Epoch;Stage=2;return SurfaceRetryAction.Release;}
        }else if(Stage==2&&v.Epoch-phase>=1_000_000){
            if(v.TerrainContacts==0||v.Jets!=0)throw new InvalidDataException("Ignition must originate from actual grounded released state.");
            phase=v.Epoch;Stage=3;return SurfaceRetryAction.Ignite;
        }else if(Stage==3){
            Powered|=v.Main&&v.Mass<rcsMass&&v.Position.Y>groundedHeight;
            if(v.Consumer==(int)AssemblyPhysicalConsumer.FreeFlight&&v.Contacts==0&&v.Velocity.Y>0&&Powered&&TerrainSeen&&GroundedRcs){if(freeFlightSince==0)freeFlightSince=v.Epoch;
                if(v.Epoch-freeFlightSince>=1_000_000){Stage=4;return SurfaceRetryAction.Complete;}}else freeFlightSince=0;
        }
        return SurfaceRetryAction.None;
    }
}

