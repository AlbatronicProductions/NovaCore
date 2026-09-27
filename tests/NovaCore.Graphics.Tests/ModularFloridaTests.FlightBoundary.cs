using System.Collections.Immutable;
using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Graphics;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Transactions;

internal static partial class ModularFloridaTests
{
    private static void FlightBoundaries(AssemblyDefinitionCatalog catalog,(IPhysicalSurfacePointQuery Query,FloridaSlabSupport Slab) terrain)
    {
        var bounds=(IPhysicalSurfaceHeightBounds)terrain.Query;
        foreach(var direction in new[]{FloridaFacilitySupport.Region.Up,new Double3(0,-(1-4e-11),0),new Double3(1,0,0),new Double3(-1,0,1e-14),Double3.UnitY})
        foreach(var angle in new[]{0d,1e-8,1e-5,.01,.2,Math.PI})
        {
            var d=direction.Normalized();var height=bounds.HeightUpperBound(direction,angle);
            var axis=Math.Abs(d.Y)<.9?Double3.Cross(d,Double3.UnitY).Normalized():Double3.Cross(d,Double3.UnitX).Normalized();var second=Double3.Cross(d,axis);
            for(var i=0;i<32;i++)
            {
                var a=angle*(i%4)/3;var azimuth=i*Math.Tau/32;
                var sample=d*Math.Cos(a)+(axis*Math.Cos(azimuth)+second*Math.Sin(azimuth))*Math.Sin(a);
                var actual=PlanetaryPhysicalSurface.EvaluateFinalHeightNoGradient(PlanetaryTerrainDefinition.EarthProductionCubeV5,sample.Normalized(),PlanetaryPhysicalSurfaceGeneration.M12DNaturalTerrainCandidate);
                Need(actual<=height,"radial cap includes independently queried boundary/interior");
            }
        }
        Need(EarthElevationDataset.CapElevationUpperBound(new(0,-(1-4e-11),0),1e-8)>=2839.3224994278,"nonunit south-polar regression covers all longitudes");
        Need(EarthElevationDataset.CapElevationUpperBound(new(8.003599179233558e-09,-.9999999999999999,1.0244139796887386e-08),1e-8)>=2828.0308232242,"near-polar latitude retains horizontal components");
        var polarCenter=new Double3(8.003599179233558e-09,-.9999999999999999,1.0244139796887386e-08);
        var polarSample=new Double3(8.306423103925325e-09,-1,2.5172941752637373e-10);
        Need(Math.Atan2(Norm(Double3.Cross(polarCenter,polarSample)),Double3.Dot(polarCenter,polarSample))<1e-8&&
            EarthElevationDataset.CapElevationUpperBound(polarCenter,1e-8)>=EarthElevationDataset.SampleElevation(polarSample),"independent polar sample lies inside declared cap");
        using(var refused=ConstructionApplicationSession.CreateSupported(CraftCompiler.Compile(catalog,Craft(catalog,false).Data,Assets),terrain.Query,terrain.Slab))
        {
            var before=Observe(refused);refused.Engine.AdmitConstructionHostTime(refused.Authority,1,new(15625));var clock=refused.Engine.CaptureContinuationClock();var revision=refused.Engine.State.Revision;
            Need(refused.Engine.ServiceConstructionDebt(refused.Authority,out var published,refusePublicationForTest:true)==ConstructionServiceStatus.Invalidated&&published==0,"post-native preparation refusal poisons private continuation");
            Need(refused.Engine.State.Spacecraft.TryGetConstruction(refused.Binding.Spacecraft.Id,out _,out var after)&&ReferenceEquals(before,after)&&
                clock==refused.Engine.CaptureContinuationClock()&&revision==refused.Engine.State.Revision,"post-native refusal preserves exact canonical source and debt");
            Need(refused.Engine.ServiceConstructionDebt(refused.Authority,out _)==ConstructionServiceStatus.Invalidated&&
                refused.Engine.AdmitAssemblyControl(refused.Control!,refused.Control!.Identity,1,new(true)).Status==AssemblyControlStatus.StaleSource,"invalidated private solve cannot resume through input or retry");
        }
        foreach(var mode in new[]{"fuel","power","tiny-fuel"})
        {
            var data=Craft(catalog,false).Data;
            data=data with {Configuration=data.Configuration.Select(c=>c with {
                Stores=c.Stores.Select(x=>x with {QuantityKg=mode=="tiny-fuel"?1e-300:mode=="fuel"?.01:x.QuantityKg}).ToImmutableArray(),
                Electrical=c.Electrical.Select(x=>x with {ChargeJ=mode=="power"&&x.ChargeJ>0?.5:x.ChargeJ}).ToImmutableArray()}).ToImmutableArray()};
            var craft=CraftCompiler.Compile(catalog,data,Assets);
            using var s=ConstructionApplicationSession.CreateSupported(craft,terrain.Query,terrain.Slab);
            Need(s.Engine.AdmitAssemblyControl(s.Control!,s.Control!.Identity,1,new(true,new(1,1,1))).Status==AssemblyControlStatus.Admitted,"depletion command");
            Need(s.Engine.AdmitConstructionHostTime(s.Authority,1,new(15625))==ConstructionServiceStatus.AcceptedCredit,"depletion credit");
            var status=s.Engine.ServiceConstructionDebt(s.Authority,out var published);
            Console.WriteLine($"DEPLETION {mode} {status} {published}");
            Need(status==ConstructionServiceStatus.Published&&published==1,"exact fractional depletion publication "+mode);
            var state=Observe(s);Need(s.Engine.ObserveConstructionActuation(s.Authority,out var actual)==ConstructionServiceStatus.Ready&&!actual.Main&&actual.Jets.IsEmpty,"empty required resource/power ceases all actuation at endpoint");
            var expected=s.Binding.Physical!.Services.Advance(s.Binding.Initial.Fuel,s.Binding.Initial.Power,15625,s.Binding.Physical.Control.Resolve(new(true,new(1,1,1))).Consumers.AsSpan());
            Need(expected.Fuel.Save().SequenceEqual(state.Fuel.Save())&&expected.Power.Save().SequenceEqual(state.Power.Save()),"native motion uses the same exact service proposal");
        }
    }
}
