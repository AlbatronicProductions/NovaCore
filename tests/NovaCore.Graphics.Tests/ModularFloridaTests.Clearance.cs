using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Graphics;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Transactions;

internal static partial class ModularFloridaTests
{
    private static void FlightClearance(AssemblyDefinitionCatalog catalog,(IPhysicalSurfacePointQuery Query,FloridaSlabSupport Slab) terrain)
    {
        foreach(var longer in new[]{false,true})
        {
            var craft=CraftCompiler.Compile(catalog,Craft(catalog,longer).Data,Assets);
            using var s=ConstructionApplicationSession.CreateSupported(craft,terrain.Query,terrain.Slab);
            var p=s.Binding.Physical!;var cold=s.Binding.Initial.Physical!;
            Need(!p.Clearance.Clears(cold.Motion,craft.InitialMass,s.Clock.CurrentTime,15625,out _),"supported hull cannot be declared clear");
            Need(!p.Clearance.Clears(cold.Motion with {PositionO=new(100,-10,0)},craft.InitialMass,s.Clock.CurrentTime,15625,out _),"below-terrain outside grading refuses");
            Need(!p.Clearance.Clears(cold.Motion with {PositionO=new(100,1000,0),VelocityO=new(0,-1e8,0)},craft.InitialMass,s.Clock.CurrentTime,15625,out _),"endpoint height cannot conceal crossing trajectory");
            Need(!p.Site.GradedTerrainBallClear(new(100,0,0),2)&&!p.Site.GradedTerrainBallClear(new(0,-50,0),2),"finite-slab private world cannot ignore terrain outside proof");
            foreach(var y in new[]{.5,1000d})foreach(var axis in new[]{new Double3(1,0,0),new Double3(1,2,3)})
            {
                var motion=new AssemblyMotion(new(y>1?150:0,y,0),new(.3,5,-.1),(cold.Motion.BodyToWorld*DoubleQuaternion.FromAxisAngle(axis,.01)).Normalized(),new(.2,-.1,.15));
                Need(p.Clearance.Clears(motion,craft.InitialMass,s.Clock.CurrentTime,15625,out var lower)&&lower>0,"whole swept hull bound for controlled ascent");
                var row=p.Control.Resolve(new(true,new(1,-1,1)));
                for(var i=0;i<=8;i++)
                {
                    var ticks=15625L*i/8;var services=p.Services.Advance(s.Binding.Initial.Fuel,s.Binding.Initial.Power,ticks,row.Consumers.AsSpan());
                    var moved=AssemblyDynamics.Evaluate(p.Control,motion,services,default,row,site:p.Site,epoch:s.Clock.CurrentTime).Motion;
                    foreach(var hull in craft.Collision)foreach(var v in hull.Vertices)
                    {
                        var local=moved.PositionO+moved.BodyToWorld.Rotate(v);var point=p.Site.OriginBodyFixed+p.Site.LocalToBodyFixed.Rotate(local);var radius=Norm(point);
                        var h=PlanetaryPhysicalSurface.EvaluateFinalHeightNoGradient(PlanetaryTerrainDefinition.EarthProductionCubeV5,point/radius,PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate);
                        Need(radius>terrain.Query.Authority.ReferenceRadiusMetres+h,"independent sampled physical hull clearance inside certified interval");
                    }
                }
            }
            // Fresh unloaded contact has no compressed resting-contact history.
            var command=p.Control.Resolve(new(true));var evolution=p.Services.Advance(s.Binding.Initial.Fuel,s.Binding.Initial.Power,15625,command.Consumers.AsSpan());
            var oracle=AssemblyDynamics.Evaluate(p.Control,cold.Motion,evolution,default,command,site:p.Site,epoch:s.Clock.CurrentTime);
            s.Engine.AdmitAssemblyControl(s.Control!,s.Control!.Identity,1,new(true));s.Engine.AdmitConstructionHostTime(s.Authority,1,new(15625));
            Need(s.Engine.ServiceConstructionDebt(s.Authority,out _)==ConstructionServiceStatus.Published,"fresh native ignition");var actual=Observe(s).Physical!.Motion;
            Console.WriteLine($"FRESH_CONTACT_ORACLE long={longer} position={Norm(actual.PositionO-oracle.Motion.PositionO):R} velocity={Norm(actual.VelocityO-oracle.Motion.VelocityO):R}");
            Need(Norm(actual.PositionO-oracle.Motion.PositionO)<p.Contact.ContactTolerance&&Norm(actual.VelocityO-oracle.Motion.VelocityO)<1e-4,"uncompressed native force integration agrees with RK oracle");
        }
    }
}
