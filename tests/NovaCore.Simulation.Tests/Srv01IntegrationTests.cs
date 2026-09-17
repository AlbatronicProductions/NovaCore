using System.Collections.Immutable;
using System.Numerics;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Transactions;

// Cold authoring is explicit and versioned. Runtime loading never repairs hashes,
// infers physical properties from a mesh, or migrates an old four-jet save.
internal static class Srv01IntegrationTests
{
    internal const string StockId="novacore.stock.SRV01.FourHorn";
    internal static CompiledAssemblyDesign AuthoredDesign()
    {
        var old=AssemblyStockCatalog.LoadDefault().Resolve("novacore.stock.SRV01.G0B").Data;
        var oldRcs=old.Definitions.Single(d=>d.Role==AssemblyRole.RcsJet);
        PropulsionData Jet(string id,Double3 point,Double3 axis)=>oldRcs.Propulsion! with {Id=id,Point=point,Axis=axis};
        var block=oldRcs with {Id="novacore.part.RcsBlock_A",Role=AssemblyRole.RcsBlock,Propulsion=null,
            JetActuators=[Jet("bottom",new(0,-.193,.184),-Double3.UnitZ),Jet("left",new(-.184,-.193,0),Double3.UnitX),
                Jet("right",new(.184,-.193,0),-Double3.UnitX),Jet("top",new(0,-.193,-.184),Double3.UnitZ)]};
        var q=Math.Sqrt(.5);var signs=new[]{(1,-1),(-1,-1),(-1,1),(1,1)};
        var instanceOrder=new[]{3,5,4,6};
        var instances=old.Design.Instances.Select(p=>
        {
            if(p.Definition.Id!=oldRcs.Id)return p;
            var n=int.Parse(p.Id.AsSpan(p.Id.Length-2),System.Globalization.CultureInfo.InvariantCulture)-1;
            var a=signs[n].Item1*q;var b=signs[n].Item2*q;
            return p with {Order=instanceOrder[n],Definition=new(block.Id,block.Revision,"authoring"),
                Pose=new(new(0,a,-b),new(0,0,-1,b,a,0,a,-b,0))};
        }).ToImmutableArray();
        var tank=old.Definitions.Single(d=>d.Role==AssemblyRole.Tank);
        tank=tank with {Revision=2,Attachments=tank.Attachments.Select(e=>
        {
            if(!e.Id.StartsWith("RCS_",StringComparison.Ordinal))return e;
            var pose=instances.Single(p=>p.Id==e.Id.ToLowerInvariant()).Pose.Then(block.Attachments.Single().Frame);
            return e with {Frame=pose with {Rotation=pose.Rotation*Matrix3.Mate}};
        }).ToImmutableArray()};
        var defs=old.Definitions.Select(d=>d.Role==AssemblyRole.RcsJet?block:d.Role==AssemblyRole.Tank?tank:d)
            .OrderBy(d=>d.Id,StringComparer.Ordinal).ToImmutableArray();
        instances=instances.Select(p=>p with {Definition=new(p.Definition.Id,defs.Single(d=>d.Id==p.Definition.Id).Revision,
            AssemblyJson.Digest(defs.Single(d=>d.Id==p.Definition.Id)))}).OrderBy(p=>p.Order).ToImmutableArray();
        var feeds=ImmutableArray.CreateBuilder<FeedEdgeData>();
        foreach(var instance in instances)
        {
            var def=defs.Single(d=>d.Id==instance.Definition.Id);
            foreach(var actuator in def.Propulsion is {} p?new[]{p}:def.JetActuators?.ToArray()??[])
                foreach(var species in new[]{"FUEL","OXIDIZER"})feeds.Add(new("tank_01",species,instance.Id,species,actuator.Id));
        }
        var rows=old.Design.Pairs.Select(p=>p with {FirstActuator=p.Axis==0?(p.Sign==1?"left":"right"):"bottom",SecondActuator=p.Axis==0?(p.Sign==1?"left":"right"):"bottom"}).ToImmutableArray();
        var data=new CatalogDesignData("novacore.assembly/2",defs,old.Design with {Id=StockId,Revision=1,Instances=instances,Feeds=feeds.ToImmutable(),Pairs=rows});
        return CompiledAssemblyDesign.Compile(AssemblyJson.Write(data));
    }
    internal static void Author(string path)
    {
        var d=AuthoredDesign();File.WriteAllBytes(path,d.Save());
        Console.WriteLine($"SRV01_AUTHORED digest={d.Digest} jets={d.Jets.Length} feeds={d.Data.Design.Feeds.Length} mass={d.DryMass:R} first={d.FirstMoment}");
    }
    private static void Check(bool value,string why){if(!value)throw new InvalidOperationException("SRV01 sixteen-jet contract: "+why);}
    private static CatalogDesignData Reseal(CatalogDesignData data)
    {
        var defs=data.Definitions.Select(d=>d with {JetActuators=d.JetActuators?.OrderBy(j=>j.Id,StringComparer.Ordinal).ToImmutableArray()}).ToImmutableArray();
        return data with {Definitions=defs,Design=data.Design with {Instances=data.Design.Instances.Select(p=>p with {Definition=p.Definition with {Digest=AssemblyJson.Digest(defs.Single(d=>d.Id==p.Definition.Id))}}).ToImmutableArray()}};
    }
    private static void Reject(CatalogDesignData data,string why)
    {
        try{CompiledAssemblyDesign.Compile(AssemblyJson.Write(data));}catch(Exception e)when(e is InvalidDataException or InvalidOperationException){return;}
        throw new InvalidOperationException("SRV01 accepted invalid "+why);
    }
    internal static void Run()
    {
        var d=AssemblyStockCatalog.LoadDefault().Resolve(StockId);Check(d.Digest==AuthoredDesign().Digest,"reproducible explicit physical authoring");
        Check(d.Jets.Length==16&&d.Parts.Length==7&&d.Data.Design.Feeds.Length==34,"four blocks/sixteen consumers plus main");
        Check(d.FirstMoment==new Double3(734.4,0,0)&&d.DryMass==630,"independent first moment/mass");
        Check(Math.Abs(d.OriginInertia.A-190.47255)<1e-11&&Math.Abs(d.OriginInertia.E-1496.626691666667)<1e-10&&d.OriginInertia.E==d.OriginInertia.I,"independent full origin tensor");
        foreach(var mass in new[]{630d,705d,730d})
        {
            var actual=d.ObserveMass(mass);Check(actual.Com==new Double3(734.4/mass,0,0),"moving COM");
            Check(Math.Abs(actual.Inertia.E-(1496.626691666667-734.4*734.4/mass))<1e-10&&actual.Inertia.E==actual.Inertia.I,"parallel-axis tensor");
            foreach(var jet in d.Jets)
            {
                var w=AssemblyActuation.Jet(jet);var point=jet.Instance.Pose.Point(jet.Propulsion.Point);
                Check(Math.Abs(w.Force.LengthSquared-22.5*22.5)<1e-10&&w.MomentAtOrigin==Double3.Cross(point,w.Force),"each identified nozzle force/point");
                var current=w.MomentAtOrigin-Double3.Cross(actual.Com,w.Force);
                Check((current-Double3.Cross(point-actual.Com,w.Force)).LengthSquared<1e-24,"individual current-COM moment");
            }
        }
        Check(d.Jets.Select(j=>(j.Instance.Id,j.Propulsion.Id)).Distinct().Count()==16,"tuple identities");
        foreach(var pair in d.Data.Design.Pairs)
        {
            var a=AssemblyActuation.Jet(d.Jet(pair.First,pair.FirstActuator));var b=AssemblyActuation.Jet(d.Jet(pair.Second,pair.SecondActuator));
            var force=a.Force+b.Force;var torque=a.MomentAtOrigin+b.MomentAtOrigin;
            var expected=pair.Axis==0?new Double3(pair.Sign*36.315,0,0):pair.Axis==1?new Double3(0,pair.Sign*25.678582758789474,0):new Double3(0,0,pair.Sign*25.678582758789474);
            Check((torque-expected).LengthSquared<1e-24&&force==(pair.Axis==0?Double3.Zero:new Double3(45,0,0)),"honest fixed-row force/torque");
        }
        var block=d.Data.Definitions.Single(x=>x.Role==AssemblyRole.RcsBlock);
        Reject(Reseal(d.Data with {Definitions=d.Data.Definitions.Replace(block,block with {JetActuators=block.JetActuators!.Value.RemoveAt(0)})}),"missing nozzle");
        var huge=block with {JetActuators=block.JetActuators!.Value.Select(j=>j.Id=="left"?j with {Point=new(0,0,1e20)}:j).ToImmutableArray()};
        Reject(Reseal(d.Data with {Definitions=d.Data.Definitions.Replace(block,huge)}),"unbounded individual cancellation");
        Reject(d.Data with {Design=d.Data.Design with {Feeds=d.Data.Design.Feeds.SetItem(1,d.Data.Design.Feeds[0])}},"duplicate feed without duplicate inventory");
        Reject(d.Data with {Design=d.Data.Design with {Feeds=d.Data.Design.Feeds.SetItem(0,d.Data.Design.Feeds[0] with {Actuator="unowned"})}},"foreign actuator feed");
        Reject(d.Data with {Design=d.Data.Design with {Pairs=d.Data.Design.Pairs.SetItem(0,d.Data.Design.Pairs[0] with {FirstActuator=null})}},"legacy part-only command on block");
        var reversed=Reseal(d.Data with {Definitions=d.Data.Definitions.Replace(block,block with {JetActuators=block.JetActuators!.Value.Reverse().ToImmutableArray()}),Design=d.Data.Design with {Instances=d.Data.Design.Instances.Reverse().ToImmutableArray(),Feeds=d.Data.Design.Feeds.Reverse().ToImmutableArray()}});
        Check(CompiledAssemblyDesign.Compile(AssemblyJson.Write(reversed)).Digest==d.Digest,"enumeration-independent identity");
        // A valid explicitly authored alternative row exercises bit 15 without
        // giving arbitrary masks or hidden force authority to the runtime caller.
        var plus=d.Data.Design.Pairs.Single(p=>p.Name=="+PITCH");
        var alternate=CompiledAssemblyDesign.Compile(AssemblyJson.Write(d.Data with {Design=d.Data.Design with {Pairs=d.Data.Design.Pairs.Replace(plus,plus with {First="rcs_04",FirstActuator="top",Second="rcs_01",SecondActuator="bottom"})}}));
        var highJet=AssemblyActuation.Jet(alternate.Jet("rcs_04","top"));var lowJet=AssemblyActuation.Jet(alternate.Jet("rcs_01","bottom"));
        Check(highJet.Force+lowJet.Force==Double3.Zero&&((highJet.MomentAtOrigin+lowJet.MomentAtOrigin)-new Double3(0,25.678582758789474,0)).LengthSquared<1e-24,"bit15 independently authored pure pitch couple");
        var launch=new AssemblyLaunch(alternate,new(new(202),new(1),new(2),"bit15"),"bit15",new(default,default,DoubleQuaternion.Identity,default),default,[new(false,"+PITCH",0,0,15625)]);
        Check((launch.Plan[0].Jets&0x8000)!=0&&BitOperations.PopCount(launch.Plan[0].Jets)==2,"bit15 remains separate");
        var s=AssemblyApplicationSession.Create(launch);Check(s.Engine.AdmitAssemblyHostTime(s.Authority,1,new(15625)).Status==AssemblyFlightStatus.AcceptedCredit,"bit15 credit");
        Check(s.Engine.ServiceAssemblyFlightDebt(s.Authority).Status==AssemblyFlightStatus.Completed,"bit15 publish");
        Check(s.Engine.ObserveAssemblyFlight(s.Authority,out var observation)==AssemblyFlightStatus.Ready&&(observation.State.Actual.Jets&0x8000)!=0,"bit15 copied observation");
        Check(observation.State.Stores.Fuel==AssemblyResources.Mass(30d-3d/32768)&&observation.State.Stores.Oxidizer==AssemblyResources.Mass(45d-9d/65536),"bit15 exact two-nozzle debit");
        var catalog=new AssemblyStockCatalog([(alternate.Save(),alternate.Digest)]);var restored=AssemblyApplicationSession.Restore(catalog,s.Save());
        Check(restored.Engine.ObserveAssemblyFlight(restored.Authority,out var reloaded)==AssemblyFlightStatus.Ready&&reloaded==observation,"bit15 save/reload exact");
        void RejectSave(AssemblyStockCatalog registry,AssemblySaveData value,string why)
        {
            try{AssemblyApplicationSession.Restore(registry,AssemblyJson.Write(value));}
            catch(InvalidDataException){return;}
            throw new InvalidOperationException("SRV01 accepted invalid save: "+why);
        }
        var dto=AssemblyJson.Read<AssemblySaveData>(s.Save());
        RejectSave(catalog,dto with {Current=dto.Current with {Actual=dto.Current.Actual with {Jets=(ushort)(dto.Current.Actual.Jets^0x8000)}}},"tampered high jet bit");
        var old=AssemblyStockCatalog.LoadDefault().Resolve("novacore.stock.SRV01.G0B");
        var oldSession=AssemblyApplicationSession.Create(new(old,launch.Spacecraft,"old-profile",launch.Initial.Motion,default,[new(false,null,0,0,15625)]));
        var oldSave=AssemblyJson.Read<AssemblySaveData>(oldSession.Save());
        RejectSave(AssemblyStockCatalog.LoadDefault(),oldSave with {DesignId=StockId,DesignDigest=d.Digest},"historical four-jet save relabelled sixteen-jet");
        AssemblyProductionTests.ValidateFourHorn(d);
        Console.WriteLine("SRV01_INTEGRATION PASS sixteen endpoints/identity/shared-feed/mass/COM/tensor/command-wrench/bit15/refusals/replay/24-independent-trajectories");
    }
}
