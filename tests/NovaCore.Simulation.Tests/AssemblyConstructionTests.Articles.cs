using System.Collections.Immutable;
using System.Text.Json;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;

internal static partial class AssemblyConstructionTests
{
    private static CompiledConstructionDesign CapsuleDesign()
    {
        var stock=StockDevelopment();var part=stock.Catalog.Data.Definitions.Single(d=>d.Id=="nc.capsule.return-3500");
        var instance=new PartInstanceData("probe",0,stock.Catalog.Reference(part),new(Double3.Zero,Matrix3.Identity));
        return CompiledConstructionDesign.Compile(stock.Catalog,new(CompiledConstructionDesign.Schema,"proof.independent-capsule",1,
            stock.Catalog.DependencyDigest([instance]),"probe","probe",[instance],[],[],
            [new("probe",part.Stores.Select(s=>new ConstructionStoreInitial(s.Id,s.CapacityKg,true)).ToImmutableArray(),[])],[],[]));
    }
    internal static void ConstructionArticle()
    {
        checks=0;var design=StockDevelopment();using var session=ConstructionApplicationSession.Create(design);
        var b=session.Binding;var snapshot=Observe(session);
        Check(b.Parts.Length==9&&b.Parts.Distinct().Count()==9&&b.Subparts.Length==8&&b.Subparts.Distinct().Count()==8,"DLV distinct placed and parent-local functional identities");
        Check(b.DryRegionCount==150&&b.StoreRegionCount==36&&b.Actuators.Length==38,"accepted constituent/resource/actuator expansion");
        Check(Math.Abs(b.FullLoadedReference.Mass-247283.61451443692)<1e-6&&snapshot.ReferenceMass==b.FullLoadedReference,"current accepted full-load mass");
        var engines=design.Parts.Where(p=>p.Definition.Id=="nc.booster-engine.liquid-a").ToArray();
        Check(engines.Length==3&&engines.All(p=>ReferenceEquals(p.Definition,engines[0].Definition)),"three instances one immutable definition");
        Check(b.Power.Modules.Length==0&&b.Power.Power.Count==9&&b.Power.Data.Count==1,"absent DLV electrical hardware remains absent; command data distinct");
        Check(design.Data.ControlPart=="Capsule"&&b.Parts.All(p=>b.Power.CanCommand(design.Index(p.DesignInstance))),"explicit capsule control reachability");
        IndependentArticleMass(design,b.FullLoadedReference);
        foreach(var p in design.Parts)foreach(var c in p.Definition.Construction!.Consumers)
        {
            var instance=b.Actuators.Single(a=>a.Part.DesignInstance==p.Instance.Id&&a.LocalId==c.Id);
            Check((instance.ForcePoint-p.Instance.Pose.Point(c.ForcePoint)).LengthSquared<1e-20&&
                (instance.Axis-p.Instance.Pose.Rotation.Apply(c.Axis)).LengthSquared<1e-20,"actuator placement from owned part frame");
        }
        foreach(var c in design.Connections.Where(c=>c.Detachable))
        {
            var members=design.Partition(c.Id);Check(members.Contains(c.Child)&&!members.Contains(c.Parent),"detachable membership only, no successor body");
            Check((c.Services&(ConstructionService.Propellant|ConstructionService.Electricity))==0&&c.Services.HasFlag(ConstructionService.Data),"accepted release planes preserve explicit service isolation");
        }
        var saved=session.Save();using var reload=ConstructionApplicationSession.Restore(design.Catalog,saved);
        Check(saved.SequenceEqual(reload.Save())&&reload.Binding.Identity!=b.Identity,"DLV common replay with fresh identity");
        var demand=Enumerable.Repeat(true,b.Fuel.Consumers.Length).ToImmutableArray();
        Check(session.Engine.AdvanceConstructionServices(session.Authority,0,new(1,demand,[]))==NovaCore.Simulation.Spacecraft.Assemblies.ConstructionServiceStatus.UnqualifiedHardware&&session.Save().SequenceEqual(saved),"unqualified DLV hardware cannot consume or publish a partial successor");
        Check(!session.Engine.State.Spacecraft.TryGetAttitude(b.Spacecraft.Id,out _)&&!session.Engine.State.Spacecraft.TryGetAppliedEndpoint(b.Spacecraft.Id,out _),"DLV static identity not physical/contact admission");
        Console.WriteLine($"Construction Stage 7 PASS: {checks} checks; 150 dry + 36 resource regions; 38 actuators; mass {b.FullLoadedReference.Mass:R}; COM {b.FullLoadedReference.Com}; static only");
    }
    private static void IndependentArticleMass(CompiledConstructionDesign design,AssemblyMass expected)
    {
        // Independent scalar origin-tensor accumulation, then translate once to aggregate COM.
        double mass=0;var first=new double[3];var origin=new double[3,3];
        foreach(var part in design.Parts)
        {
            var pose=part.Instance.Pose;var r=pose.Rotation;double[,] rotation={{r.A,r.B,r.C},{r.D,r.E,r.F},{r.G,r.H,r.I}};
            var regions=part.Definition.Construction!.MassRegions.Concat(part.Definition.Stores.Select(s=>new MassRegionData(s.Id,s.CapacityKg,s.Datum,
                part.Definition.Construction.StoreGeometry.Single(g=>g.Store==s.Id).InertiaPerKg*s.CapacityKg)));
            foreach(var region in regions)
            {
                double[] local=[region.Com.X,region.Com.Y,region.Com.Z],p=[pose.Position.X,pose.Position.Y,pose.Position.Z];
                for(var i=0;i<3;i++)for(var j=0;j<3;j++)p[i]+=rotation[i,j]*local[j];
                var t=region.InertiaAtCom;double[,] tensor={{t.A,t.B,t.C},{t.D,t.E,t.F},{t.G,t.H,t.I}};
                mass+=region.MassKg;for(var i=0;i<3;i++)first[i]+=region.MassKg*p[i];
                var square=p.Sum(x=>x*x);
                for(var i=0;i<3;i++)for(var j=0;j<3;j++)
                {
                    double rotated=0;for(var u=0;u<3;u++)for(var v=0;v<3;v++)rotated+=rotation[i,u]*tensor[u,v]*rotation[j,v];
                    origin[i,j]+=rotated+region.MassKg*((i==j?square:0)-p[i]*p[j]);
                }
            }
        }
        var center=first.Select(v=>v/mass).ToArray();var e=expected.Inertia;double[,] target={{e.A,e.B,e.C},{e.D,e.E,e.F},{e.G,e.H,e.I}};
        Check(Math.Abs(mass-expected.Mass)<1e-6&&Math.Abs(center[0]-expected.Com.X)<1e-8&&Math.Abs(center[1]-expected.Com.Y)<1e-8&&Math.Abs(center[2]-expected.Com.Z)<1e-8,"independent scalar mass/first moment");
        var c2=center.Sum(x=>x*x);for(var i=0;i<3;i++)for(var j=0;j<3;j++)
        {var value=origin[i,j]-mass*((i==j?c2:0)-center[i]*center[j]);Check(Math.Abs(value-target[i,j])<=1e-7+Math.Abs(target[i,j])*1e-10,"independent origin-to-COM tensor");}
    }
    internal static void ConstructionReuse()
    {
        checks=0;var capsule=CapsuleDesign();using var a=ConstructionApplicationSession.Create(capsule);
        using var b=ConstructionApplicationSession.Create(CompiledConstructionDesign.Load(capsule.Catalog,capsule.Save()));
        Check(a.Binding.Parts.Length==1&&a.Binding.Subparts.Length==4&&a.Binding.Fuel.Stores.Length==10&&a.Binding.Actuators.Length==24,"non-DLV capsule same construction path");
        Check(Math.Abs(a.Binding.Initial.ReferenceMass!.Value.Mass-3890.35)<1e-7,"capsule full store envelope independent of launcher");
        Check(a.Save().SequenceEqual(b.Save())&&a.Binding.Identity!=b.Binding.Identity,"stock/player non-DLV convergence");
        using var editor=new ConstructionEditorSession(capsule.Catalog);editor.Load(0,capsule.Save());var save=a.Save();editor.Clear(editor.Revision);
        Check(a.Save().SequenceEqual(save),"non-DLV runtime survives independent editor clear");
        using var reloaded=ConstructionApplicationSession.Restore(capsule.Catalog,save);Check(save.SequenceEqual(reloaded.Save()),"non-DLV runtime replay");
        var stock=StockDevelopment();var instances=stock.Data.Instances.Where(p=>p.Definition.Id.StartsWith("nc.booster-",StringComparison.Ordinal)).Select((p,i)=>p with {Order=i}).ToImmutableArray();
        var names=instances.Select(p=>p.Id).ToHashSet(StringComparer.Ordinal);var transformed=new AssemblyPose(new(17,-9,3),Matrix3.Mate);
        instances=instances.Select(p=>p with {Pose=transformed.Then(p.Pose)}).ToImmutableArray();
        var repeated=CompiledConstructionDesign.Compile(stock.Catalog,stock.Data with {Id="proof.repeated-engine-platform",ControlPart=null,Instances=instances,
            DependencyDigest=stock.Catalog.DependencyDigest(instances),Connections=stock.Data.Connections.Where(c=>names.Contains(c.Parent)&&names.Contains(c.Child)).ToImmutableArray(),
            Configuration=stock.Data.Configuration.Where(c=>names.Contains(c.Part)).ToImmutableArray()});
        using var platform=ConstructionApplicationSession.Create(repeated);
        Check(platform.Binding.Parts.Length==4&&platform.Binding.Actuators.Length==3&&platform.Binding.Subparts.Length==3&&platform.Binding.Fuel.Stores.Length==17,"non-DLV repeated engine platform owns instances locally");
        Check(platform.Binding.Design.ControlIndex==-1&&Math.Abs(platform.Binding.Initial.ReferenceMass!.Value.Mass-197564.76565693234)<1e-6,"no invented control hardware or vehicle-specific mass");
        IndependentArticleMass(repeated,platform.Binding.FullLoadedReference);
        var platformBytes=platform.Save();using var platformReload=ConstructionApplicationSession.Restore(stock.Catalog,platformBytes);
        Check(platformBytes.SequenceEqual(platformReload.Save()),"transformed repeated platform shared replay");
        Console.WriteLine($"Construction Stage 8 reuse PASS: {checks} checks; capsule-only design {capsule.Digest}; no launcher assumptions");
    }
    internal static void MeasureConstructionRuntime()
    {
        var dlv=StockDevelopment();var capsule=CapsuleDesign();var fixture=RuntimeFixture();
        using var initial=ConstructionApplicationSession.Create(fixture,capacity:4096);var save=initial.Save();
        var rows=new List<Measurement>{Measure("runtime-DLV-cold",()=>{using var s=ConstructionApplicationSession.Create(dlv);},64),
            Measure("runtime-nonDLV-cold",()=>{using var s=ConstructionApplicationSession.Create(capsule);},64),
            Measure("runtime-save",()=>initial.Save()),Measure("runtime-restore-empty-history",()=>{using var s=ConstructionApplicationSession.Restore(fixture.Catalog,save);})};
        using var populated=ConstructionApplicationSession.Create(fixture,capacity:256);
        for(var i=0;i<128;i++)if(populated.Engine.AdvanceConstructionServices(populated.Authority,i,ServiceStep(false,1))!=ConstructionServiceStatus.Advanced)throw new InvalidOperationException();
        var populatedSave=populated.Save();
        rows.Add(Measure("runtime-replay-128-commands",()=>{using var s=ConstructionApplicationSession.Restore(fixture.Catalog,populatedSave);},32,4));
        var active=Enumerable.Range(0,140).Select(_=>ConstructionApplicationSession.Create(fixture,capacity:1)).ToArray();var activeIndex=0;var activeCommand=ServiceStep();
        try{rows.Add(Measure("runtime-active-fuel-joint-interval-precreated",()=>{var s=active[activeIndex++];if(s.Engine.AdvanceConstructionServices(s.Authority,0,activeCommand)!=ConstructionServiceStatus.Advanced)throw new InvalidOperationException();}));}
        finally{foreach(var s in active)s.Dispose();}
        for(var window=0;window<3;window++)
        {
            using var s=ConstructionApplicationSession.Create(fixture,capacity:4096);var sequence=0;var command=ServiceStep(false,1);
            rows.Add(Measure("runtime-fuel-off-powered-load-window-"+window,()=>{if(s.Engine.AdvanceConstructionServices(s.Authority,sequence++,command)!=ConstructionServiceStatus.Advanced)throw new InvalidOperationException();},512));
        }
        rows.Add(Measure("runtime-observation",()=>{if(initial.Engine.ObserveConstructionServices(initial.Authority,out _)!=ConstructionServiceStatus.Ready)throw new InvalidOperationException();},1024));
        Console.WriteLine(JsonSerializer.Serialize(new {scope="Static runtime construction and canonical service accounting; no flight integration",DesignBytes=fixture.Save().Length,RuntimeSaveBytes=save.Length,PopulatedRuntimeSaveBytes=populatedSave.Length,HistoryCapacity=4096,Results=rows},new JsonSerializerOptions{WriteIndented=true}));
    }
}
