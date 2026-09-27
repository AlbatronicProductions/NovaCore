using NovaCore.Core;
using System.Collections.Immutable;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Spacecraft.Contact.Staging;
using NovaCore.Simulation.Transactions;

internal static partial class ModularFloridaTests
{
    internal static void SurfaceRefinement()
    {
        checks=0;var terrain=Terrain();var catalog=AssemblyDefinitionCatalog.Load(File.ReadAllBytes(Path.Combine(Assets,"catalog.json")));
        var craft=CraftCompiler.Compile(catalog,Craft(catalog,true).Data,Assets);
        using var cold=ConstructionApplicationSession.CreateSupported(craft,terrain.Query,terrain.Slab);
        var basis=AssemblyJson.Read<ConstructionFlightCheckpoint>(cold.Save());
        var q=DoubleQuaternion.FromAxisAngle(Double3.UnitZ,.6)*AssemblyContactProfile.Upright;
        var y=cold.Binding.Physical!.Contact.SupportPlane-craft.Collision.SelectMany(h=>h.Vertices).Min(v=>q.Rotate(v).Y)+.01;
        var entry=basis with{Physical=new(new(new(0,y,0),new(0,-.2,0),q,default),default,AssemblyPhysicalConsumer.FreeFlight)};
        var entryBytes=AssemblyJson.Write(entry);
        int Refinement(ConstructionApplicationSession x)=>Field<LocalContactConfiguration>(x.Engine.ConstructionContactWorldForTest(x.Authority)!,"configuration").CraftRefinement;
        using(var s=ConstructionApplicationSession.RestoreFlight(catalog,entryBytes,Assets,terrain.Query,terrain.Slab))
        {
            var request=default(AssemblyControlRequest);var saw=false;
            for(var i=0;i<128;i++)
            {
                if(i==62){request=new(false,new(0,0,1));Need(s.Engine.AdmitAssemblyControl(s.Control!,s.Control!.Identity,1,request).Status==AssemblyControlStatus.Admitted,"refinement witness RCS command");}
                var before=Observe(s);var profile=s.Binding.Physical!;var row=profile.Control.Resolve(request);
                var once=profile.Services.Advance(before.Fuel,before.Power,15625,row.Consumers.AsSpan());
                var twice=profile.Services.Advance(once.Fuel,once.Power,15625,row.Consumers.AsSpan());
                var refinement=Refinement(s);var revision=s.Engine.State.Revision;var history=s.Engine.ObserveConstructionPhysicalHistory(s.Authority).Records.Length;
                Need(s.Engine.AdmitConstructionHostTime(s.Authority,i+1,new(15625))==ConstructionServiceStatus.AcceptedCredit,"refinement credit");
                var credited=s.Engine.CaptureContinuationClock();
                Need(s.Engine.ServiceConstructionDebt(s.Authority,out var n)==ConstructionServiceStatus.Published&&n==1,"refinement successor");
                var after=Observe(s);
                Need(after.Fuel.Save().SequenceEqual(once.Fuel.Save())&&after.Power.Save().SequenceEqual(once.Power.Save())&&after.ReferenceMass==craft.Mass.Evaluate(ConstructionNumerics.Quantities(once.Fuel)),"one independently reconstructed resource proposal");
                Need(after.Sequence==before.Sequence+1&&after.Epoch.Ticks==before.Epoch.Ticks+15625&&s.Engine.State.Revision.Value==revision.Value+1&&s.Engine.ObserveConstructionPhysicalHistory(s.Authority).Records.Length==history+1&&s.Engine.CaptureContinuationClock().Debt.Ticks==credited.Debt.Ticks-15625,"one canonical publication despite private trials");
                if(Refinement(s)>refinement)
                {
                    Need(!before.Fuel.Save().SequenceEqual(after.Fuel.Save()),"refinement interval consumed finite fuel");
                    Need(!after.Fuel.Save().SequenceEqual(twice.Fuel.Save())&&!after.Power.Save().SequenceEqual(twice.Power.Save()),"private retry does not double debit");
                    Console.WriteLine($"SURFACE_REFINEMENT step={i} before={refinement} after={Refinement(s)} onceOnly=True");saw=true;break;
                }
            }
            Need(saw,"positive natural refinement coverage");
        }
        using(var a=ConstructionApplicationSession.RestoreFlight(catalog,entryBytes,Assets,terrain.Query,terrain.Slab))
        using(var b=ConstructionApplicationSession.RestoreFlight(catalog,entryBytes,Assets,terrain.Query,terrain.Slab))
        {
            var refined=false;
            for(var pair=0;pair<64;pair++)
            {
                Need(a.Engine.AdmitConstructionHostTime(a.Authority,pair+1,new(31250))==ConstructionServiceStatus.AcceptedCredit&&a.Engine.ServiceConstructionDebt(a.Authority,out var count)==ConstructionServiceStatus.Published&&count==2,"paired host partition");
                for(var i=0;i<2;i++)Need(b.Engine.AdmitConstructionHostTime(b.Authority,2*pair+i+1,new(15625))==ConstructionServiceStatus.AcceptedCredit&&b.Engine.ServiceConstructionDebt(b.Authority,out var n)==ConstructionServiceStatus.Published&&n==1,"split host partition");
                var left=Observe(a);var right=Observe(b);
                Need(left.Physical==right.Physical&&left.ReferenceMass==right.ReferenceMass&&left.Fuel.Save().SequenceEqual(right.Fuel.Save())&&left.Power.Save().SequenceEqual(right.Power.Save())&&left.Epoch==right.Epoch&&left.Sequence==right.Sequence,"refined physics is independent of host partition");
                refined|=Refinement(a)>0&&Refinement(b)>0;
            }
            Need(refined&&a.Engine.CaptureContinuationClock().Debt.Ticks==0&&b.Engine.CaptureContinuationClock().Debt.Ticks==0,"refinement and debt coverage are nonvacuous");
        }
        var catalogBytes=catalog.Save();
        AssemblyDefinitionCatalog Thin(double halfThickness,DoubleQuaternion orientation)
        {
            var vertices=(from x in new[]{-.15,.15} from y in new[]{-.15,.15} from z in new[]{-halfThickness,halfThickness}
                select new Double3(.3,0,0)+orientation.Rotate(new(x,y,z))).ToImmutableArray();
            return AssemblyDefinitionCatalog.Compile(catalog.Data with {Definitions=catalog.Data.Definitions.Select(d=>d.Id=="nc.core.command-2"?
                d with {Standard=d.Standard! with {Collision=d.Standard!.Collision.Add(new("qualification-thin",vertices,"Thin convex region inside the existing core envelope."))}}:d).ToImmutableArray()});
        }
        foreach(var orientation in new[]{DoubleQuaternion.Identity,DoubleQuaternion.FromAxisAngle(Double3.UnitZ,.37)*DoubleQuaternion.FromAxisAngle(Double3.UnitY,.61)})
        {
            var thinCatalog=Thin(.0025,orientation);var thinCraft=CraftCompiler.Compile(thinCatalog,Craft(thinCatalog,false).Data,Assets);
            using var thin=ConstructionApplicationSession.CreateSupported(thinCraft,terrain.Query,terrain.Slab);
            var contact=thin.Binding.Physical!.Contact;var width=contact.MinimumNativeHalfWidth;
            Need(width>0&&width<=.0025-contact.ContactTolerance+contact.ContactTolerance/8,"native rotated thin hull uses face/inball width, never AABB width");
        }
        var tooThinCatalog=Thin(.001,DoubleQuaternion.Identity);
        using(var thin=ConstructionApplicationSession.CreateSupported(CraftCompiler.Compile(tooThinCatalog,Craft(tooThinCatalog,false).Data,Assets),terrain.Query,terrain.Slab))
        {
            var rejected=false;try{_=thin.Binding.Physical!.Contact.MinimumNativeHalfWidth;}catch(InvalidDataException){rejected=true;}
            Need(rejected,"native hull below numerical precision refuses");
        }
        var exhaustedCatalog=Thin(.002005,DoubleQuaternion.Identity);
        using(var thinCold=ConstructionApplicationSession.CreateSupported(CraftCompiler.Compile(exhaustedCatalog,Craft(exhaustedCatalog,false).Data,Assets),terrain.Query,terrain.Slab))
        {
            var saved=AssemblyJson.Read<ConstructionFlightCheckpoint>(thinCold.Save());
            saved=saved with {Physical=saved.Physical with {Consumer=AssemblyPhysicalConsumer.SurfaceContact,Motion=saved.Physical.Motion with {PositionO=new(0,.01,0)}}};
            using var refused=ConstructionApplicationSession.RestoreFlight(exhaustedCatalog,AssemblyJson.Write(saved),Assets,terrain.Query,terrain.Slab);
            var request=new AssemblyControlRequest(false,new(0,0,1));
            Need(refused.Engine.AdmitAssemblyControl(refused.Control!,refused.Control!.Identity,1,request).Status==AssemblyControlStatus.Admitted,"exhaustion RCS request");
            var original=Observe(refused);var revision=refused.Engine.State.Revision;var profile=refused.Binding.Physical!;
            var proposal=profile.Services.Advance(original.Fuel,original.Power,15625,profile.Control.Resolve(request).Consumers.AsSpan());
            Need(!proposal.Fuel.Save().SequenceEqual(original.Fuel.Save()),"failed trial has a real independently prepared resource debit");
            Need(refused.Engine.AdmitConstructionHostTime(refused.Authority,1,new(15625))==ConstructionServiceStatus.AcceptedCredit,"exhaustion credit");
            var clock=refused.Engine.CaptureContinuationClock();
            Need(refused.Engine.ServiceConstructionDebt(refused.Authority,out var published)==ConstructionServiceStatus.Invalidated&&published==0,"natural contact motion exhausts refinement without publication");
            var world=refused.Engine.ConstructionContactWorldForTest(refused.Authority)!;
            Need(Refinement(refused)==CraftSurfaceImpact.MaximumRefinement&&world.CraftMotionRefinementRequested,"real final refinement limit reached");
            Need(refused.Engine.State.Spacecraft.TryGetConstruction(refused.Binding.Spacecraft.Id,out _,out var retained)&&ReferenceEquals(retained,original)&&
                retained.Fuel.Save().SequenceEqual(original.Fuel.Save())&&retained.Power.Save().SequenceEqual(original.Power.Save())&&
                retained.ReferenceMass==original.ReferenceMass&&retained.Physical==original.Physical&&refused.Engine.State.Revision==revision&&refused.Engine.CaptureContinuationClock()==clock,
                "exhaustion preserves exact resources, mass, physical state, revision and credited debt");
            Need(refused.Engine.ServiceConstructionDebt(refused.Authority,out published)==ConstructionServiceStatus.Invalidated&&published==0,"poisoned trial cannot resume or double debit");
        }
        Need(catalog.Save().SequenceEqual(catalogBytes),"all adversarial fixtures preserve original source catalog");
        var q1=new DoubleQuaternion(.1,.2,.3,.4).Normalized();
        Need(q1.Normalized()!=q1,"normalization witness is not bit-idempotent");
        var rotated=basis with{Physical=basis.Physical with{Consumer=AssemblyPhysicalConsumer.FreeFlight,Motion=basis.Physical.Motion with{PositionO=new(0,10,0),BodyToWorld=q1,AngularVelocityBody=new(.1,.2,.3)}}};
        var encoded=AssemblyJson.Write(rotated);
        using(var s=ConstructionApplicationSession.RestoreFlight(catalog,encoded,Assets,terrain.Query,terrain.Slab))
        {
            Need(Observe(s).Physical==rotated.Physical&&s.Save().SequenceEqual(encoded),"physical registration preserves exact quaternion");
            var view=s.Engine.State.Spacecraft;var id=s.Binding.Spacecraft.Id;
            Need(!view.TryGetAttitude(id,out _)&&!view.TryGetRigidBody(id,out _)&&!view.TryGetTranslation(id,out _,out _),"legacy propagation authority stays inaccessible");
            var threw=false;try{_=view.GetAttitude(0);}catch(InvalidOperationException){threw=true;}
            Need(threw,"indexed legacy orientation is inaccessible");
            Need(view.TryGetConstruction(id,out var admitted,out var exact)&&ReferenceEquals(admitted,s.Binding)&&exact!.Physical!.Motion.BodyToWorld==q1,"typed endpoint is the sole orientation authority");
        }
        Console.WriteLine($"SURFACE_REFINEMENT_PASS checks={checks}");
    }
}
