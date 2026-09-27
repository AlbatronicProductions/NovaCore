using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text.Json;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;

internal static partial class ModularCraftTests
{
    internal static AssemblyDefinitionCatalog StarterCatalog()=>AssemblyDefinitionCatalog.Load(File.ReadAllBytes("assets/vehicles/modular-starter/catalog.json"));
    // Test assembly only. Gameplay must construct the player's document, never substitute this fixture.
    private static CompiledConstructionDesign StarterCraft(AssemblyDefinitionCatalog catalog,bool longer)
    {
        var parts=ImmutableArray.CreateBuilder<PartInstanceData>();var edges=ImmutableArray.CreateBuilder<StructuralEdgeData>();
        PartDefinitionData Def(string id)=>catalog.Data.Definitions.Single(d=>d.Id==id);
        parts.Add(new("core",0,catalog.Reference(Def("nc.core.command-2")),new(Double3.Zero,Matrix3.Identity)));
        void Attach(string id,string definition,int parent,string target,string child)
        {
            var p=parts[parent];var pd=catalog.Resolve(p.Definition);var d=Def(definition);
            var frame=CompiledConstructionDesign.Snap(p.Pose,pd.Attachments.Single(a=>a.Id==target),d.Attachments.Single(a=>a.Id==child));
            parts.Add(new(id,parts.Count,catalog.Reference(d),frame));
            var services=pd.Construction!.Interfaces.Single(a=>a.Interface==target).Services&d.Construction!.Interfaces.Single(a=>a.Interface==child).Services;
            edges.Add(new(p.Id,target,id,child,new("joint-"+id,false,services,0)));
        }
        Attach("tank",longer?"nc.tank.long-2":"nc.tank.short-2",0,"aft","fore");
        Attach("adapter","nc.mount.single-2to1",1,"aft","fore");Attach("engine","nc.engine.main-1",2,"engine","fore");
        var tank=catalog.Resolve(parts[1].Definition);var group=tank.Standard!.SocketGroups.Single();
        var set=group.Placements.Single(s=>s.Anchor=="radial-0"&&s.Count==8);
        foreach(var socket in set.Sockets)Attach("block-"+socket,"nc.rcs.block-r1",1,socket,"mount");
        var basis=parts[4];var members=parts.Skip(4).Select(p=>new ConstructionSymmetryMember(p.Id,
            p==basis?new(Double3.Zero,Matrix3.Identity):PartStandard.Inverse(basis.Pose).Then(p.Pose))).ToImmutableArray();
        var instances=parts.ToImmutable();
        var config=instances.Select(p=>{var d=catalog.Resolve(p.Definition);return new ConstructionConfiguration(p.Id,
            d.Stores.Select(s=>new ConstructionStoreInitial(s.Id,s.CapacityKg,true)).ToImmutableArray(),
            d.Construction!.Electrical.Select(e=>new ConstructionElectricalInitial(e.Id,e.CapacityJ,true)).ToImmutableArray());}).ToImmutableArray();
        return CompiledConstructionDesign.Compile(catalog,new(CompiledConstructionDesign.CraftSchema,"starter-test",1,catalog.DependencyDigest(instances),"core","core",instances,edges.ToImmutable(),[],config,
            [new("attitude",basis.Id,members,new("tank",group.Id,set.Anchor,8,set.Sockets,group.Axis,basis.Definition))],[],new("Test assembly only",1)));
    }
    internal static void StarterDefinitionsGate()
    {
        checks=0;var catalog=StarterCatalog();var bytes=catalog.Save();
        Check(catalog.Data.Definitions.Length==6,"exactly six production definitions");
        Check(catalog.Data.Definitions.Count(d=>d.Standard!.RootEligible)==1,"one command root");
        Check(catalog.Save().SequenceEqual(AssemblyDefinitionCatalog.Load(bytes).Save()),"six definitions strict canonical roundtrip");
        Check(AssemblyDefinitionCatalog.Compile(catalog.Data with {Definitions=catalog.Data.Definitions.Reverse().ToImmutableArray(),Resources=catalog.Data.Resources.Reverse().ToImmutableArray()}).Digest==catalog.Digest,"catalog enumeration independent");
        var library=new ConstructionAssetLibrary();
        foreach(var d in catalog.Data.Definitions)
        {
            var c=d.Construction!;var asset=library.Resolve("assets/vehicles/modular-starter",c.Asset);
            Check(!asset.Bytes.IsDefaultOrEmpty,"strict GLB container and named nodes");
            var kind=c.Asset.RelativePath[..^4];var physical=File.ReadAllBytes($"assets/vehicles/modular-starter/{kind}.physics.json");
            Check(Convert.ToHexStringLower(SHA256.HashData(physical))==c.PhysicalSource.Sha256,"physical source content seal");
            using var source=JsonDocument.Parse(physical);var regions=source.RootElement.GetProperty("primitives");
            Check(regions.GetArrayLength()==c.MassRegions.Length,"all dry regions sourced");
            foreach(var r in regions.EnumerateArray())
            {
                var region=c.MassRegions.Single(x=>x.Id==r.GetProperty("id").GetString());var mass=r.GetProperty("massKg").GetDouble();
                var q=r.GetProperty("dimensions").EnumerateArray().Select(x=>x.GetDouble()).ToArray();
                var expected=r.GetProperty("kind").GetString()=="box"?
                    new Double3(mass*(q[1]*q[1]+q[2]*q[2])/12,mass*(q[0]*q[0]+q[2]*q[2])/12,mass*(q[0]*q[0]+q[1]*q[1])/12):
                    new Double3(mass*(q[0]*q[0]+q[1]*q[1])/2,mass*((q[0]*q[0]+q[1]*q[1])/4+q[2]*q[2]/12),mass*((q[0]*q[0]+q[1]*q[1])/4+q[2]*q[2]/12));
                var rot=r.GetProperty("rotation").EnumerateArray().Select(x=>x.GetDouble()).ToArray();var rotation=new Matrix3(rot[0],rot[1],rot[2],rot[3],rot[4],rot[5],rot[6],rot[7],rot[8]);
                Check(region.MassKg==mass&&(region.InertiaAtCom-rotation*Matrix3.Diagonal(expected)*rotation.Transpose()).Maximum<1e-12,"independent primitive inertia");
            }
            foreach(var law in d.Standard!.StoreLaws)
            {
                var s=d.Stores.Single(x=>x.Id==law.Store);var volume=Math.PI*(law.OuterRadiusM-law.InnerRadiusM)*(law.OuterRadiusM+law.InnerRadiusM)*law.LengthM;
                Check(Math.Abs(volume*law.DensityKgM3-s.CapacityKg)<1e-10,"physical inventory from actual tank geometry");
            }
            Reject(()=>AssemblyDefinitionCatalog.Compile(catalog.Data with {Definitions=catalog.Data.Definitions.Select(p=>p.Id==d.Id?p with {DryMassKg=p.DryMassKg+1}:p).ToImmutableArray()}),"mass tamper refused");
            Reject(()=>library.Resolve("assets/vehicles/modular-starter",c.Asset with {Sha256=new string('0',64)}),"asset tamper refused");
        }
        foreach(var longer in new[]{false,true})
        {
            var craft=StarterCraft(catalog,longer);var length=longer?3d:1.5;
            Check(craft.Parts.Length==12&&craft.Connections.Length==11&&craft.Data.Symmetry.Single().Members.Length==8,"exact twelve-instance topology");
            Check(craft.Parts[1].Instance.Pose.Position==new Double3(-length,0,0)&&craft.Parts[2].Instance.Pose.Position==new Double3(-length,0,0),"authored stack placement");
            var dry=craft.Parts.Sum(p=>p.Definition.DryMassKg);var fuel=craft.Parts.Sum(p=>p.Definition.Stores.Sum(s=>s.CapacityKg));var wet=dry+fuel;
            Check(dry==(longer?564:504)&&wet==(longer?2164:1304),"independent dry/wet budget");
            var consumers=craft.Parts.SelectMany(p=>p.Definition.Construction!.Consumers.Select(c=>(Part:p,Consumer:c))).ToArray();
            Check(consumers.Length==33&&consumers.Count(x=>x.Consumer.Model=="nc.actuator.attitude/1")==32,"explicit main and 32 attitude model identities");
            var main=consumers.Single(x=>x.Consumer.Model=="nc.actuator.main/1");
            Check(main.Consumer.ThrustN==30720&&main.Consumer.TotalFlowKgS==10&&main.Consumer.Mixture.Select(m=>m.Weight).SequenceEqual(new uint[]{2,3}),"main thrust/flow/mixture budget");
            Check(main.Consumer.ThrustN/(wet*9.81)>1.4,"liftoff margin");
            Check(craft.Parts.Sum(p=>p.Definition.Construction!.Electrical.Where(e=>e.Role==ElectricalRole.Load).Sum(e=>e.Watts))==56,"finite command controller load");
            Check(craft.Parts.Sum(p=>p.Definition.Construction!.Electrical.Sum(e=>e.CapacityJ))==90000,"one finite battery");
            var support=craft.Parts.SelectMany(p=>p.Definition.Standard!.Support.Select(f=>(Foot:f,Point:p.Instance.Pose.Point(f.Frame.Position)))).ToArray();
            Check(support.Length==4&&support.All(s=>Math.Abs(s.Point.X+length+1.8)<1e-14&&wet*9.81/2<s.Foot.MaximumLoadN),"four adapter-owned feet and two-foot load margin");
            foreach(var fill in new[]{0d,.5,1d})
            {
                var mass=AssemblyConstructionFacts.Aggregate(craft.Parts.SelectMany(p=>new[]{new MassRegionData(p.Instance.Id,p.Definition.DryMassKg,p.Com,p.InertiaAtOwnCom)}
                    .Concat(p.Definition.Stores.Where(s=>fill>0).Select(s=>new MassRegionData(p.Instance.Id+"."+s.Id,s.CapacityKg*fill,p.Instance.Pose.Point(s.Datum),p.Instance.Pose.Rotation*p.Definition.Construction!.StoreGeometry.Single(g=>g.Store==s.Id).InertiaPerKg*p.Instance.Pose.Rotation.Transpose()*(s.CapacityKg*fill))))));
                var adapterCom=mass.Com.X+length;
                var oracle=longer?new[]{1.0939716312056738,1.3321114369501466,1.394177449168207}:new[]{.4146825396825397,.5630530973451328,.620398773006135};
                Check(Math.Abs(adapterCom-(oracle[(int)(fill*2)]+2/mass.Mass))<1e-12,"planned COM plus derived 2 kg-m brace first-moment correction");
                Check(mass.Inertia.PhysicalInertia&&Math.Abs(mass.Com.Y)<1e-14&&Math.Abs(mass.Com.Z)<1e-14,"full physical tensor and signed symmetry");
                var margin=.95-(adapterCom+1.8)*Math.Tan(2*Math.PI/180)-.06;
                Check(margin>(longer?.778:.805),"revised two-degree support margin including 60 mm allowance");
            }
            var jets=consumers.Where(x=>x.Consumer.Model=="nc.actuator.attitude/1").ToArray();
            var rollForce=Double3.Zero;var rollTorque=Double3.Zero;
            foreach(var (p,c) in jets.Where(x=>x.Consumer.Id=="tangent-positive"))
            {var f=p.Instance.Pose.Rotation.Apply(c.Axis)*c.ThrustN;var point=p.Instance.Pose.Point(c.ForcePoint);rollForce+=f;rollTorque+=Double3.Cross(point,f);}
            Check(rollForce.LengthSquared<1e-24&&Math.Abs(rollTorque.X-90)<1e-12&&Math.Abs(rollTorque.Y)+Math.Abs(rollTorque.Z)<1e-12,"eight-jet roll wrench cancels translation");
            foreach(var (_,c) in jets)
                Check(Double3.Dot(c.ForcePoint-new Double3(0,.15,0),c.Axis)<0,"each exhaust exits opposite force without traversing own block");
            Check(catalog.Save().SequenceEqual(bytes),"qualification source immutable");
        }
        Console.WriteLine($"Modular starter definitions PASS: {checks} checks; six definitions; geometric feasibility only, no runtime/flight PASS; catalog {catalog.Digest}");
    }
}
