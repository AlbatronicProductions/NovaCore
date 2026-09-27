using System.Collections.Immutable;
using System.Numerics;
using NovaCore.Core;
using static NovaCore.Simulation.Spacecraft.Assemblies.AssemblyConstructionFacts;

namespace NovaCore.Simulation.Spacecraft.Assemblies;

internal sealed record CraftDependency(DefinitionReference Definition,ConstructionAsset Asset,PhysicalSource Physics);
internal sealed record CraftRenderBinding(string Instance,ConstructionAsset Asset,AssemblyPose MaterialPose);
internal sealed record CraftCollision(string Part,string Shape,ImmutableArray<Double3> Vertices);
internal sealed record CraftSupport(string Part,PartSupportFoot Foot,AssemblyPose MaterialFrame);
internal sealed record CraftStoreMass(ConstructionStoreKey Key,Double3 Datum,Matrix3 InertiaPerKg,double CapacityKg);
internal sealed record CraftActuator(ConstructionConsumerKey Key,int Consumer,int Part,string Model,Double3 Point,Double3 Axis,double Thrust,
    GimbalData? Gimbal,Matrix3 PartRotation,Double3 PartOrigin,ImmutableArray<int> RequiredLoads,bool DataReachable);
internal readonly record struct CraftControlColumn(Double3 Force,Double3 MomentAtOrigin);
internal sealed record CraftDiagnostic(string Code,string? Part,string Field,string Message);

/// <summary>Prepared proportional-spatial coefficients in the root material frame.
/// Evaluation observes supplied quantities; it never spends or owns inventory.</summary>
internal sealed class CraftMassLaw
{
    internal AssemblyMass Dry {get;}
    internal ImmutableArray<CraftStoreMass> Stores {get;}
    internal double MaximumMass {get;}
    internal Double3 ComMinimum {get;}
    internal Double3 ComMaximum {get;}
    internal Matrix3 OriginInertiaMinimum {get;}
    internal Matrix3 OriginInertiaMaximum {get;}
    private readonly Matrix3 dryOrigin;
    private readonly ImmutableArray<Matrix3> originCoefficients;
    internal CraftMassLaw(AssemblyMass dry,ImmutableArray<CraftStoreMass> stores)
    {
        Dry=dry;Stores=stores;dryOrigin=dry.Inertia+Matrix3.Parallel(dry.Com)*dry.Mass;
        originCoefficients=stores.Select(s=>s.InertiaPerKg+Matrix3.Parallel(s.Datum)).ToImmutableArray();
        var maximumMass=dry.Mass;foreach(var s in stores)maximumMass=Math.BitIncrement(maximumMass+s.CapacityKg);MaximumMass=maximumMass;
        var min=dry.Com;var max=dry.Com;
        for(var i=0;i<stores.Length;i++){var s=stores[i];
            min=new(Math.Min(min.X,s.Datum.X),Math.Min(min.Y,s.Datum.Y),Math.Min(min.Z,s.Datum.Z));max=new(Math.Max(max.X,s.Datum.X),Math.Max(max.Y,s.Datum.Y),Math.Max(max.Z,s.Datum.Z));
        }
        // Exact positive-mass combinations lie in this convex hull; these are
        // mathematical law bounds, not an error budget for a later integrator.
        ComMinimum=new(Math.BitDecrement(min.X),Math.BitDecrement(min.Y),Math.BitDecrement(min.Z));
        ComMaximum=new(Math.BitIncrement(max.X),Math.BitIncrement(max.Y),Math.BitIncrement(max.Z));
        (OriginInertiaMinimum,OriginInertiaMaximum)=OriginBounds(dry,stores);
        Require(double.IsFinite(MaximumMass)&&min.IsFinite&&max.IsFinite&&OriginInertiaMinimum.Finite&&OriginInertiaMaximum.Finite,"Craft mass coefficient range exceeded.");
    }
    private static (Matrix3 Low,Matrix3 High) OriginBounds(AssemblyMass dry,ImmutableArray<CraftStoreMass> stores)
    {
        // Exact dyadic products include Parallel(), coefficient addition and mass
        // multiplication. A final nextafter alone cannot bound their rounding.
        const int exponent=3*PartStandardExact.QuantumExponent;
        var q=BigInteger.One<<PartStandardExact.QuantumExponent;
        static BigInteger[] Tensor(Matrix3 m)=>new[]{m.A,m.B,m.C,m.D,m.E,m.F,m.G,m.H,m.I}.Select(PartStandardExact.Encode).ToArray();
        static BigInteger[] Parallel(Double3 p){var x=PartStandardExact.Encode(p.X);var y=PartStandardExact.Encode(p.Y);var z=PartStandardExact.Encode(p.Z);return [y*y+z*z,-x*y,-x*z,-x*y,x*x+z*z,-y*z,-x*z,-y*z,x*x+y*y];}
        var central=Tensor(dry.Inertia);var parallel=Parallel(dry.Com);var mass=PartStandardExact.Encode(dry.Mass);
        var low=new BigInteger[9];var high=new BigInteger[9];
        for(var i=0;i<9;i++)low[i]=high[i]=central[i]*q*q+parallel[i]*mass;
        foreach(var store in stores){central=Tensor(store.InertiaPerKg);parallel=Parallel(store.Datum);mass=PartStandardExact.Encode(store.CapacityKg);
            for(var i=0;i<9;i++){var full=(central[i]*q+parallel[i])*mass;low[i]+=BigInteger.Min(0,full);high[i]+=BigInteger.Max(0,full);}}
        static double Round(BigInteger n,bool up){
            if(n.IsZero)return 0;var magnitude=BigInteger.Abs(n);var shift=Math.Max(0,checked((int)magnitude.GetBitLength())-53);
            var value=Math.ScaleB((double)(magnitude>>shift),shift-exponent)*n.Sign;Require(double.IsFinite(value),"Craft origin-inertia bound exceeds FP64 range.");
            var represented=PartStandardExact.Encode(value)<<(exponent-PartStandardExact.QuantumExponent);
            if(up&&represented<n)value=Math.BitIncrement(value);else if(!up&&represented>n)value=Math.BitDecrement(value);
            Require(double.IsFinite(value),"Craft origin-inertia bound exceeds FP64 range.");return value;
        }
        Matrix3 Build(BigInteger[] values,bool up){var v=values.Select(n=>Round(n,up)).ToArray();return new(v[0],v[1],v[2],v[3],v[4],v[5],v[6],v[7],v[8]);}
        return(Build(low,false),Build(high,true));
    }
    internal AssemblyMass Evaluate(ReadOnlySpan<double> quantities)=>Evaluate(quantities,true);
    internal AssemblyMass EvaluateDynamic(ReadOnlySpan<double> quantities)=>Evaluate(quantities,false);
    private AssemblyMass Evaluate(ReadOnlySpan<double> quantities,bool exactValidation)
    {
        Require(quantities.Length==Stores.Length,"Craft mass quantity dimensions.");var mass=Dry.Mass;var first=Dry.Com*mass;var tensor=dryOrigin;
        for(var i=0;i<Stores.Length;i++){
            var q=quantities[i];var s=Stores[i];Require(double.IsFinite(q)&&q>=0&&q<=s.CapacityKg,"Craft mass quantity range.");
            mass+=q;first+=s.Datum*q;tensor+=originCoefficients[i]*q;
        }
        var com=first/mass;var inertia=tensor-Matrix3.Parallel(com)*mass;inertia=Symmetric(inertia);
        Require(double.IsFinite(mass)&&mass>0&&com.IsFinite&&(exactValidation?PartStandardExact.PhysicalInertia(inertia):inertia.PhysicalInertia),"Craft mass numerical range is unqualified.");
        return new(mass,com,inertia);
    }
    internal static Matrix3 Symmetric(Matrix3 m)=>new(m.A,m.B,m.C,m.B,m.E,m.F,m.C,m.F,m.I);
}

/// <summary>Immutable compilation artifact, never a vessel identity or runtime owner.</summary>
internal sealed class CompiledCraft
{
    internal const string Schema="novacore.compiled-craft/1";
    internal string Digest {get;}
    internal CompiledConstructionDesign Design {get;}
    internal ImmutableArray<CraftDependency> Dependencies {get;}
    internal ImmutableArray<CraftRenderBinding> Render {get;}
    internal CompiledCraftPorts Ports {get;}
    internal ConstructionFuelNetwork Fuel {get;}
    internal ConstructionPowerNetwork Power {get;}
    internal CraftMassLaw Mass {get;}
    internal AssemblyMass InitialMass {get;}
    internal ImmutableArray<CraftCollision> Collision {get;}
    internal ImmutableArray<CraftCollision> Clearance {get;}
    internal ImmutableArray<CraftSupport> Support {get;}
    internal ImmutableArray<CraftActuator> Actuators {get;}
    internal ImmutableArray<CraftControlColumn> AllocationGeometry {get;}
    internal CompiledCraftAllocation Allocation {get;}
    internal ImmutableArray<CraftDiagnostic> Diagnostics {get;}
    internal ImmutableArray<CraftDiagnostic> AdmissionDiagnostics {get;}
    internal bool Function=>Diagnostics.IsEmpty;
    internal Double3 FocusCenter {get;}
    internal double FocusRadius {get;}
    internal string AdmissionProfile=>"novacore.modular-florida/1";
    internal bool PhysicalAdmissionQualified=>false; // Gate 8/9 qualification is separate.
    internal CompiledCraft(CompiledConstructionDesign design,ImmutableArray<CraftDependency> dependencies,ImmutableArray<CraftRenderBinding> render,
        CompiledCraftPorts ports,ConstructionFuelNetwork fuel,ConstructionPowerNetwork power,CraftMassLaw mass,AssemblyMass initial,
        ImmutableArray<CraftCollision> collision,ImmutableArray<CraftCollision> clearance,ImmutableArray<CraftSupport> support,
        ImmutableArray<CraftActuator> actuators,ImmutableArray<CraftDiagnostic> diagnostics)
    {
        Design=design;Dependencies=dependencies;Render=render;Ports=ports;Fuel=fuel;Power=power;Mass=mass;InitialMass=initial;Collision=collision;Clearance=clearance;Support=support;Actuators=actuators;
        Allocation=new(actuators,mass);
        Diagnostics=Allocation.Complete?diagnostics:diagnostics.Add(new("ATTITUDE_AUTHORITY",null,"actuators","Provide balanced authority for both directions of all three attitude axes."));
        AdmissionDiagnostics=support.IsEmpty?[new("SUPPORT_MISSING",null,"support","Add authored support hardware before Florida launch.")]:[];
        AllocationGeometry=actuators.Select(a=>new CraftControlColumn(a.Axis*a.Thrust,Double3.Cross(a.Point,a.Axis*a.Thrust))).ToImmutableArray();
        Require(AllocationGeometry.All(c=>c.Force.IsFinite&&c.MomentAtOrigin.IsFinite),"Craft actuator numerical range exceeded.");
        var vertices=collision.SelectMany(c=>c.Vertices).ToArray();
        var min=new Double3(vertices.Min(v=>v.X),vertices.Min(v=>v.Y),vertices.Min(v=>v.Z));var max=new Double3(vertices.Max(v=>v.X),vertices.Max(v=>v.Y),vertices.Max(v=>v.Z));
        FocusCenter=min+(max-min)*.5;FocusRadius=vertices.Max(v=>Math.Sqrt((v-FocusCenter).LengthSquared));
        Require(FocusCenter.IsFinite&&double.IsFinite(FocusRadius)&&FocusRadius>0,"Craft focus range exceeded.");
        static string Hex(System.Numerics.BigInteger value)=>value.ToString("x",System.Globalization.CultureInfo.InvariantCulture);
        Digest=AssemblyJson.Digest(new {Schema,Source=design.Digest,design.Data.DependencyDigest,Dependencies=dependencies,Profile=AdmissionProfile,
            Render=render,Ports=ports.Ports,Routes=ports.Routes,Mass=new{mass.Dry,mass.Stores,mass.MaximumMass,mass.ComMinimum,mass.ComMaximum,mass.OriginInertiaMinimum,mass.OriginInertiaMaximum,Initial=initial},
            Collision=collision,Clearance=clearance,Support=support,Actuators=actuators,AllocationGeometry,Allocation=new{Allocation.JetActuators,Allocation.AxisJets,Allocation.Complete,Allocation.ForceTolerance,Allocation.TorqueTolerance},Diagnostics,AdmissionDiagnostics,FocusCenter,FocusRadius,
            Fuel=new{Scale=Hex(fuel.Scale),Stores=fuel.Stores.Select(s=>new{s.Key,s.Resource,Capacity=Hex(s.Capacity),Initial=Hex(s.Initial),s.Enabled,s.Part}).ToArray(),
                Consumers=fuel.Consumers.Select(c=>new{c.Key,Rate=Hex(c.Rate),c.Rule,Terms=c.Terms.Select(t=>new{t.Resource,t.Weight,t.Stores,t.Levels,ShareNumerator=Hex(t.ShareNumerator)}).ToArray()}).ToArray()},
            Power=power.Modules.Select(m=>new{m.Key,m.Role,m.Part,m.Bus,Capacity=Hex(m.Capacity),Rate=Hex(m.Rate),m.Driver,Initial=Hex(m.Initial),m.Enabled}).ToArray()});
    }
}

internal static class CraftCompiler
{
    internal static CompiledCraft Compile(AssemblyDefinitionCatalog catalog,ConstructionDesignData source,string assetRoot)
    {
        Require(source is not null&&source.Schema==CompiledConstructionDesign.CraftSchema,"Craft compiler requires CraftDocument.");
        var design=CompiledConstructionDesign.Compile(catalog,source);new PartCompatibilityEvaluator(catalog).RequireFit(design);
        var library=new ConstructionAssetLibrary();
        var dependencies=design.Parts.Select(p=>p.Instance.Definition).Distinct().OrderBy(r=>r.Id,StringComparer.Ordinal).ThenBy(r=>r.Revision).Select(r=>{
            var c=catalog.Resolve(r).Construction!;_=library.Resolve(assetRoot,c.Asset);return new CraftDependency(r,c.Asset,c.PhysicalSource);
        }).ToImmutableArray();
        var root=design.Parts[design.RootIndex].Instance.Pose;var inverse=root.Rotation.Transpose();
        AssemblyPose Pose(CompiledPart p)=>new(inverse.Apply(p.Instance.Pose.Position-root.Position),inverse*p.Instance.Pose.Rotation);
        var render=design.Parts.Select(p=>new CraftRenderBinding(p.Instance.Id,p.Definition.Construction!.Asset,Pose(p))).ToImmutableArray();
        var ports=CompiledCraftPorts.Compile(design);var fuel=ConstructionFuelNetwork.Compile(design,ports:ports);var power=ConstructionPowerNetwork.Compile(fuel);
        var dry=Aggregate(design.Parts.SelectMany(p=>p.Definition.Construction!.MassRegions.Select(r=>{
            var pose=Pose(p);return new MassRegionData(p.Instance.Id+"/"+r.Id,r.MassKg,pose.Point(r.Com),CraftMassLaw.Symmetric(pose.Rotation*r.InertiaAtCom*pose.Rotation.Transpose()));
        })));
        var stores=fuel.Stores.Select(s=>{
            var part=design.Parts[s.Part];var pose=Pose(part);var store=part.Definition.Stores.Single(v=>v.Id==s.Key.Store);var law=part.Definition.Construction!.StoreGeometry.Single(v=>v.Store==store.Id);
            return new CraftStoreMass(s.Key,pose.Point(store.Datum),CraftMassLaw.Symmetric(pose.Rotation*law.InertiaPerKg*pose.Rotation.Transpose()),store.CapacityKg);
        }).ToImmutableArray();
        var mass=new CraftMassLaw(dry,stores);var initial=mass.Evaluate(stores.Select(s=>design.Data.Configuration.Single(c=>c.Part==s.Key.Part).Stores.Single(q=>q.Store==s.Key.Store).QuantityKg).ToArray());
        ImmutableArray<CraftCollision> Volumes(bool clearance)=>design.Parts.SelectMany(p=>(clearance?p.Definition.Standard!.Clearance:p.Definition.Standard!.Collision).Select(c=>new CraftCollision(p.Instance.Id,c.Id,c.Vertices.Select(Pose(p).Point).ToImmutableArray()))).ToImmutableArray();
        var support=design.Parts.SelectMany(p=>p.Definition.Standard!.Support.Select(s=>new CraftSupport(p.Instance.Id,s,Pose(p).Then(s.Frame)))).ToImmutableArray();
        var diagnostics=ImmutableArray.CreateBuilder<CraftDiagnostic>();
        void Problem(string code,string? part,string field,string remedy)=>diagnostics.Add(new(code,part,field,remedy));
        ImmutableArray<int> Loads(int part)=>design.Parts[part].Definition.Standard!.RequiredCommandLoads.Select(id=>power.Modules.FindIndex(m=>m.Part==part&&m.Key.Module==id)).ToImmutableArray();
        var actuators=fuel.Consumers.Select((consumer,index)=>{
            var part=design.Index(consumer.Key.Part);var p=design.Parts[part];var definition=p.Definition.Construction!.Consumers.Single(c=>c.Id==consumer.Key.Consumer);var pose=Pose(p);
            var loads=Loads(part);if(design.ControlIndex>=0)loads=loads.AddRange(Loads(design.ControlIndex)).Distinct().Order().ToImmutableArray();
            return new CraftActuator(consumer.Key,index,part,definition.Model,pose.Point(definition.ForcePoint),pose.Rotation.Apply(definition.Axis),definition.ThrustN,definition.Gimbal,pose.Rotation,pose.Position,loads,power.CanCommand(consumer.Key));
        }).ToImmutableArray();
        if(design.ControlIndex<0)Problem("CONTROL_MISSING",null,"controlPart","Choose a command core for control.");
        if(actuators.Count(a=>a.Model=="nc.actuator.main/1")!=1)Problem("MAIN_COUNT",null,"actuators","This launch profile requires one main engine.");
        if(!actuators.Any(a=>a.Model=="nc.actuator.attitude/1"))Problem("ATTITUDE_MISSING",null,"actuators","Provide authored attitude hardware with balanced axis authority.");
        foreach(var actuator in actuators){
            if(actuator.Model is not ("nc.actuator.main/1" or "nc.actuator.attitude/1"))Problem("ACTUATOR_MODEL",actuator.Key.Part,actuator.Key.Consumer,"This actuator model is not qualified for this launch profile.");
            if(!actuator.DataReachable)Problem("DATA_PATH",actuator.Key.Part,actuator.Key.Consumer,"Connect this actuator to the selected command core's data service.");
            foreach(var term in fuel.Consumers[actuator.Consumer].Terms)if(!term.Stores.Any(i=>fuel.Stores[i].Enabled&&fuel.Stores[i].Initial>0))Problem("FUEL_PATH",actuator.Key.Part,actuator.Key.Consumer,"Fill and enable a connected store for resource "+term.Resource+".");
            foreach(var i in actuator.RequiredLoads){var load=power.Modules[i];if(!load.Enabled||!power.Batteries[load.Bus].Any(b=>power.Modules[b].Enabled&&power.Modules[b].Initial>0))Problem("POWER_PATH",load.Key.Part,load.Key.Module,"Enable this load and provide charge through its explicit electrical bus.");}
        }
        foreach(var p in design.Parts)if(!p.Definition.Construction!.UnqualifiedHardware.IsEmpty)Problem("HARDWARE_UNQUALIFIED",p.Instance.Id,"hardware","Remove hardware outside this physical profile.");
        return new(design,dependencies,render,ports,fuel,power,mass,initial,Volumes(false),Volumes(true),support,actuators,diagnostics.Distinct().OrderBy(d=>d.Part,StringComparer.Ordinal).ThenBy(d=>d.Code,StringComparer.Ordinal).ThenBy(d=>d.Field,StringComparer.Ordinal).ToImmutableArray());
    }
}
