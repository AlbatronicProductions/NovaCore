using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using NovaCore.Core;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Spacecraft.Resources;

namespace NovaCore.Simulation.Spacecraft.Assemblies;

// Closed, qualified assembly profile. No visual bounds, inferred feed, editor mutation,
// contact, staging or implicit runtime capability is admitted by these records.
internal readonly record struct Matrix3(double A, double B, double C, double D, double E, double F, double G, double H, double I)
{
    internal static Matrix3 Identity => new(1,0,0,0,1,0,0,0,1);
    internal static Matrix3 Mate => new(-1,0,0,0,-1,0,0,0,1);
    internal Matrix3 Transpose() => new(A,D,G,B,E,H,C,F,I);
    internal Double3 Apply(Double3 v) => new(A*v.X+B*v.Y+C*v.Z,D*v.X+E*v.Y+F*v.Z,G*v.X+H*v.Y+I*v.Z);
    internal static Matrix3 Outer(Double3 p) => new(p.X*p.X,p.X*p.Y,p.X*p.Z,p.Y*p.X,p.Y*p.Y,p.Y*p.Z,p.Z*p.X,p.Z*p.Y,p.Z*p.Z);
    internal static Matrix3 Parallel(Double3 p) => Identity*p.LengthSquared-Outer(p);
    internal static Matrix3 Diagonal(Double3 p) => new(p.X,0,0,0,p.Y,0,0,0,p.Z);
    internal double Determinant => A*(E*I-F*H)-B*(D*I-F*G)+C*(D*H-E*G);
    internal double Maximum => Math.Max(Math.Max(Math.Max(Math.Abs(A),Math.Abs(B)),Math.Max(Math.Abs(C),Math.Abs(D))),Math.Max(Math.Max(Math.Abs(E),Math.Abs(F)),Math.Max(Math.Abs(G),Math.Max(Math.Abs(H),Math.Abs(I)))));
    internal bool Finite => double.IsFinite(A)&&double.IsFinite(B)&&double.IsFinite(C)&&double.IsFinite(D)&&double.IsFinite(E)&&double.IsFinite(F)&&double.IsFinite(G)&&double.IsFinite(H)&&double.IsFinite(I);
    internal bool Rigid => Finite && Determinant>0 && (Transpose()*this-Identity).Maximum<=1e-12;
    internal bool Symmetric => B==D && C==G && F==H;
    internal bool Positive => Finite && Symmetric && A>0 && A*E-B*B>0 && Determinant>0;
    internal bool PhysicalInertia => Positive && (Identity*((A+E+I)/2)-this).Positive;
    internal Matrix3 Inverse()
    {
        var d=Determinant; if(!Finite || d<=0) throw new InvalidDataException("Singular physical tensor.");
        return new Matrix3(E*I-F*H,C*H-B*I,B*F-C*E,F*G-D*I,A*I-C*G,C*D-A*F,D*H-E*G,B*G-A*H,A*E-B*D)*(1/d);
    }
    public static Matrix3 operator +(Matrix3 a,Matrix3 b)=>new(a.A+b.A,a.B+b.B,a.C+b.C,a.D+b.D,a.E+b.E,a.F+b.F,a.G+b.G,a.H+b.H,a.I+b.I);
    public static Matrix3 operator -(Matrix3 a,Matrix3 b)=>a+b*(-1);
    public static Matrix3 operator *(Matrix3 a,double s)=>new(a.A*s,a.B*s,a.C*s,a.D*s,a.E*s,a.F*s,a.G*s,a.H*s,a.I*s);
    public static Matrix3 operator *(Matrix3 a,Matrix3 b)
    {
        var x=a.Apply(new(b.A,b.D,b.G));var y=a.Apply(new(b.B,b.E,b.H));var z=a.Apply(new(b.C,b.F,b.I));
        return new(x.X,y.X,z.X,x.Y,y.Y,z.Y,x.Z,y.Z,z.Z);
    }
}
internal readonly record struct AssemblyPose(Double3 Position,Matrix3 Rotation)
{
    internal Double3 Point(Double3 p)=>Position+Rotation.Apply(p);
    internal AssemblyPose Then(AssemblyPose local)=>new(Point(local.Position),Rotation*local.Rotation);
    internal bool Rigid=>Position.IsFinite&&Rotation.Rigid;
}
internal enum AssemblyRole { Command, Tank, MainEngine, RcsJet, RcsBlock }
internal sealed record AttachmentData(string Id,string Family,AssemblyPose Frame);
internal sealed record StoreData(string Id,string Species,string ResourceIdentity,Double3 Datum,double CapacityKg);
internal sealed record PropulsionData(string Id,string Model,string FuelIdentity,string OxidizerIdentity,double FullThrustN,double ExtentRateKgS,double ExhaustSpeed,Double3 Point,Double3 Axis);
internal sealed record GimbalData(string Id,Double3 Pivot,Double3 NozzleOffset,double LimitY,double LimitZ,double SlewRate);
internal sealed record PartDefinitionData(string Id,uint Revision,string VisualReference,AssemblyRole Role,double DryMassKg,Double3 LocalCom,Matrix3 LocalInertia,
    ImmutableArray<AttachmentData> Attachments,ImmutableArray<StoreData> Stores,PropulsionData? Propulsion,GimbalData? Gimbal,
    [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] ImmutableArray<PropulsionData>? JetActuators=null);
internal sealed record DefinitionReference(string Id,uint Revision,string Digest);
internal sealed record PartInstanceData(string Id,int Order,DefinitionReference Definition,AssemblyPose Pose);
internal sealed record StructuralEdgeData(string Parent,string ParentEndpoint,string Child,string ChildEndpoint);
internal sealed record FeedEdgeData(string Source,string Store,string Consumer,string Species,
    [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] string? Actuator=null);
internal sealed record ControlPairData(string Name,string First,string Second,int Axis,int Sign,
    [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] string? FirstActuator=null,
    [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] string? SecondActuator=null);
internal sealed record DesignData(string Id,uint Revision,string Profile,string CommandRoot,ImmutableArray<PartInstanceData> Instances,
    ImmutableArray<StructuralEdgeData> Attachments,ImmutableArray<FeedEdgeData> Feeds,ImmutableArray<ControlPairData> Pairs,double InitialFuelKg,double InitialOxidizerKg);
internal sealed record CatalogDesignData(string Schema,ImmutableArray<PartDefinitionData> Definitions,DesignData Design);
internal readonly record struct CompiledPart(PartInstanceData Instance,PartDefinitionData Definition,Double3 Com,Matrix3 InertiaAtOwnCom);
internal readonly record struct CompiledAssemblyJet(CompiledPart Part,PropulsionData Propulsion)
{
    internal PartInstanceData Instance=>Part.Instance;
    internal PartDefinitionData Definition=>Part.Definition;
    internal bool Matches(string part,string? actuator)=>Instance.Id==part && (actuator is null ? Definition.Role==AssemblyRole.RcsJet : Propulsion.Id==actuator);
}
internal readonly record struct AssemblyMass(double Mass,Double3 Com,Matrix3 Inertia);

internal static class AssemblyJson
{
    internal static readonly JsonSerializerOptions Options = MakeOptions();
    private static JsonSerializerOptions MakeOptions()
    {
        var x=new JsonSerializerOptions{PropertyNamingPolicy=JsonNamingPolicy.CamelCase,UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow,RespectRequiredConstructorParameters=true};
        x.Converters.Add(new CanonicalDoubleConverter());x.Converters.Add(new VectorConverter());x.Converters.Add(new QuaternionConverter());
        x.Converters.Add(new ExactIntegerConverter());x.Converters.Add(new InstantConverter());
        x.Converters.Add(new JsonStringEnumConverter(allowIntegerValues:false)); x.MakeReadOnly(populateMissingResolver:true);return x;
    }
    private sealed class ExactIntegerConverter:JsonConverter<PropellantInteger>
    {
        public override PropellantInteger Read(ref Utf8JsonReader r,Type t,JsonSerializerOptions o)=>AssemblyResources.ParseHex(r.GetString()??throw new JsonException("Missing exact units."));
        public override void Write(Utf8JsonWriter w,PropellantInteger v,JsonSerializerOptions o)=>w.WriteStringValue(AssemblyResources.Hex(v));
    }
    private sealed class InstantConverter:JsonConverter<SimulationInstant>
    {
        public override SimulationInstant Read(ref Utf8JsonReader r,Type t,JsonSerializerOptions o)=>new(r.GetInt64());
        public override void Write(Utf8JsonWriter w,SimulationInstant v,JsonSerializerOptions o)=>w.WriteNumberValue(v.Ticks);
    }
    private static double Number(JsonElement e){var v=e.GetDouble();return v==0?0:v;}
    private sealed class CanonicalDoubleConverter:JsonConverter<double>
    {
        public override double Read(ref Utf8JsonReader r,Type t,JsonSerializerOptions o){var v=r.GetDouble();return v==0?0:v;}
        public override void Write(Utf8JsonWriter w,double v,JsonSerializerOptions o)=>w.WriteNumberValue(v==0?0:v);
    }
    private sealed class VectorConverter:JsonConverter<Double3>
    {
        public override Double3 Read(ref Utf8JsonReader r,Type t,JsonSerializerOptions o)
        {using var d=JsonDocument.ParseValue(ref r);var e=d.RootElement;if(e.EnumerateObject().Count()!=3)throw new JsonException("Three vector fields required.");return new(Number(e.GetProperty("x")),Number(e.GetProperty("y")),Number(e.GetProperty("z")));}
        public override void Write(Utf8JsonWriter w,Double3 v,JsonSerializerOptions o){w.WriteStartObject();w.WriteNumber("x",v.X==0?0:v.X);w.WriteNumber("y",v.Y==0?0:v.Y);w.WriteNumber("z",v.Z==0?0:v.Z);w.WriteEndObject();}
    }
    private sealed class QuaternionConverter:JsonConverter<DoubleQuaternion>
    {
        public override DoubleQuaternion Read(ref Utf8JsonReader r,Type t,JsonSerializerOptions o)
        {using var d=JsonDocument.ParseValue(ref r);var e=d.RootElement;if(e.EnumerateObject().Count()!=4)throw new JsonException("Four quaternion fields required.");return new(Number(e.GetProperty("x")),Number(e.GetProperty("y")),Number(e.GetProperty("z")),Number(e.GetProperty("w")));}
        public override void Write(Utf8JsonWriter w,DoubleQuaternion v,JsonSerializerOptions o){w.WriteStartObject();w.WriteNumber("x",v.X==0?0:v.X);w.WriteNumber("y",v.Y==0?0:v.Y);w.WriteNumber("z",v.Z==0?0:v.Z);w.WriteNumber("w",v.W==0?0:v.W);w.WriteEndObject();}
    }
    internal static byte[] Write<T>(T value)=>JsonSerializer.SerializeToUtf8Bytes(value,Options);
    internal static string Digest<T>(T value)=>Convert.ToHexStringLower(SHA256.HashData(Write(value)));
    internal static T Read<T>(ReadOnlySpan<byte> bytes,int maximumBytes=256_000)
    {
        if(bytes.Length>maximumBytes)throw new InvalidDataException("Bounded document size exceeded.");
        using var doc=JsonDocument.Parse(bytes.ToArray(),new JsonDocumentOptions{MaxDepth=32});
        RejectDuplicates(doc.RootElement);
        return doc.RootElement.Deserialize<T>(Options)??throw new InvalidDataException("Missing document.");
    }
    private static void RejectDuplicates(JsonElement e)
    {
        if(e.ValueKind==JsonValueKind.Object)
        {
            var seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(var p in e.EnumerateObject()){if(!seen.Add(p.Name))throw new InvalidDataException("Duplicate JSON field.");RejectDuplicates(p.Value);}
        }
        else if(e.ValueKind==JsonValueKind.Array)foreach(var v in e.EnumerateArray())RejectDuplicates(v);
    }
}

/// <summary>Immutable compiled catalog/design. Compilation never constructs live state.</summary>
internal sealed partial class CompiledAssemblyDesign
{
    internal const string Profile="assembled-point-stores/1";
    internal CatalogDesignData Data {get;}
    internal ImmutableArray<CompiledPart> Parts {get;}
    internal string Digest {get;}
    internal AssemblyDevelopmentPropulsion? Development {get;}
    internal double MaximumMass {get;}
    internal double DryMass {get;}
    internal Double3 FirstMoment {get;}
    internal Matrix3 OriginInertia {get;}
    internal CompiledPart Main {get;}
    internal CompiledPart Tank {get;}
    internal ImmutableArray<CompiledAssemblyJet> Jets {get;}
    internal bool HasIndependentBlockJets=>Data.Schema=="novacore.assembly/2";
    internal byte[] Save()=>Development is null?AssemblyJson.Write(Data):throw new InvalidDataException("Development configuration requires its explicit profile; not a stock catalog document.");
    private CompiledAssemblyDesign(CatalogDesignData data,ImmutableArray<CompiledPart> parts,double dry,Double3 first,Matrix3 origin,AssemblyDevelopmentPropulsion? development=null)
    {
        Data=data;Parts=parts;DryMass=dry;FirstMoment=first;OriginInertia=origin;Development=development;
        Digest=development is null?AssemblyJson.Digest(data):AssemblyJson.Digest(new {Profile=development.Data,Effective=data});
        MaximumMass=dry+(development is null?100:development.Data.FuelCapacityKg+development.Data.OxidizerCapacityKg);
        Main=parts.Single(p=>p.Definition.Role==AssemblyRole.MainEngine);Tank=parts.Single(p=>p.Definition.Role==AssemblyRole.Tank);
        Jets=parts.Where(p=>p.Definition.Role is AssemblyRole.RcsJet or AssemblyRole.RcsBlock)
            .SelectMany(p=>p.Definition.JetActuators is {} jets ? jets.Select(j=>new CompiledAssemblyJet(p,j)) : [new CompiledAssemblyJet(p,p.Definition.Propulsion!)])
            .ToImmutableArray();
    }
    internal AssemblyMass ObserveMass(double totalMass)
    {
        if(!double.IsFinite(totalMass)||totalMass<DryMass||totalMass>MaximumMass)throw new InvalidDataException("Mass outside qualified profile.");
        var c=FirstMoment/totalMass;return new(totalMass,c,OriginInertia-Matrix3.Parallel(c)*totalMass);
    }
    internal CompiledPart Part(string id)=>Parts.Single(p=>p.Instance.Id==id);
    internal CompiledAssemblyJet Jet(string part,string? actuator)=>Jets.Single(j=>j.Matches(part,actuator));
    internal ControlPairData Pair(string name)=>Data.Design.Pairs.Single(p=>p.Name==name);
    private static void Require(bool value,string why){if(!value)throw new InvalidDataException(why);}
    private static bool Identifier(string? s)=>s is {Length:>0 and <=96}&&s.All(c=>char.IsAsciiLetterOrDigit(c)||c is '_' or '-' or '.' or '/' or '+');
    internal static CompiledAssemblyDesign Compile(ReadOnlySpan<byte> json)
    {
        var raw=AssemblyJson.Read<CatalogDesignData>(json);
        Require(raw.Schema is "novacore.assembly-proof/1" or "novacore.assembly/2"&&raw.Design is not null,"Unknown schema.");
        var blocks=raw.Schema=="novacore.assembly/2";
        var d=raw.Design!;
        Require(Identifier(d.Id)&&d.Revision>0&&d.Profile==Profile,"Invalid design/profile.");
        Require(!raw.Definitions.IsDefault&&raw.Definitions.Length==4&&raw.Definitions.Select(x=>x.Id).Distinct(StringComparer.Ordinal).Count()==4,"Exactly four reusable definitions.");
        var defs=raw.Definitions.OrderBy(x=>x.Id,StringComparer.Ordinal).Select(def=>def with {
            Attachments=def.Attachments.OrderBy(a=>a.Id,StringComparer.Ordinal).ToImmutableArray(),Stores=def.Stores.OrderBy(s=>s.Id,StringComparer.Ordinal).ToImmutableArray(),
            JetActuators=def.JetActuators?.OrderBy(j=>j.Id,StringComparer.Ordinal).ToImmutableArray()}).ToImmutableArray();
        foreach(var def in defs)
        {
            Require(Identifier(def.Id)&&def.Revision>0&&Identifier(def.VisualReference),"Invalid definition identity/reference.");
            Require(double.IsFinite(def.DryMassKg)&&def.DryMassKg>0&&def.LocalCom.IsFinite&&def.LocalInertia.PhysicalInertia,"Invalid authored dry mass/COM/inertia.");
            Require(!def.Attachments.IsDefault&&def.Attachments.Length>0&&def.Attachments.Select(a=>a.Id).Distinct(StringComparer.Ordinal).Count()==def.Attachments.Length,"Invalid endpoint identity.");
            foreach(var e in def.Attachments)Require(Identifier(e.Id)&&Identifier(e.Family)&&e.Frame.Rigid,"Invalid rigid endpoint.");
            Require(!def.Stores.IsDefault,"Missing explicit store list.");
            if(def.Role==AssemblyRole.Tank)
            {
                Require(def.Stores.Length==2&&def.Stores.Select(s=>s.Id).Distinct().Count()==2&&def.Stores.Select(s=>s.Species).Order().SequenceEqual(new[]{"FUEL","OXIDIZER"}),"Two separate species stores required.");
                foreach(var s in def.Stores)Require(Identifier(s.Id)&&s.ResourceIdentity==(s.Species=="FUEL"?"novacore.fixture.MMH_like/1":"novacore.fixture.NTO_like/1")&&s.Datum.IsFinite&&double.IsFinite(s.CapacityKg)&&s.CapacityKg>0&&s.CapacityKg<=(s.Species=="FUEL"?40:60),"Store outside bounded species/capacity profile.");
            }
            else Require(def.Stores.Length==0,"Store ownership must be tank capability.");
            var powered=def.Role is AssemblyRole.MainEngine or AssemblyRole.RcsJet;
            Require(powered==(def.Propulsion is not null),"Propulsion capability mismatch.");
            Require((def.Role==AssemblyRole.RcsBlock)==(def.JetActuators is not null),"Independent jet ownership mismatch.");
            Require(blocks?def.Role!=AssemblyRole.RcsJet:def.Role!=AssemblyRole.RcsBlock,"Actuator topology/schema mismatch.");
            if(def.JetActuators is {} jets)
                Require(!jets.IsDefault&&jets.Length==4&&jets.Select(j=>j.Id).Distinct(StringComparer.Ordinal).Count()==4,"Four separately identified jets required per block.");
            Require((def.Role==AssemblyRole.MainEngine)==(def.Gimbal is not null),"Exactly main owns gimbal.");
            foreach(var p in def.Propulsion is {} single ? new[]{single} : def.JetActuators?.ToArray()??[])
            {
                Require(p.Model=="novacore.ideal.MMH-NTO-like/1"&&p.FuelIdentity=="novacore.fixture.MMH_like/1"&&p.OxidizerIdentity=="novacore.fixture.NTO_like/1","Incompatible consumer species/model.");
                Require(Identifier(p.Id)&&p.Point.IsFinite&&p.Axis.IsFinite&&Math.Abs(p.Axis.LengthSquared-1)<=1e-12,"Invalid physical propulsion datum.");
                if(def.Role==AssemblyRole.RcsBlock)Require(p.Point.LengthSquared<=.3*.3,"Independent jet exceeds the admitted 0.3 m local endpoint radius.");
                Require(double.IsFinite(p.FullThrustN)&&p.FullThrustN>0&&p.FullThrustN<=(def.Role==AssemblyRole.MainEngine?600:22.5)&&p.ExtentRateKgS>0&&double.IsFinite(p.ExtentRateKgS)&&p.ExhaustSpeed==3072&&p.FullThrustN==p.ExtentRateKgS*5*p.ExhaustSpeed,"Incompatible exact ideal thrust/flow law.");
                Require(AssemblyResources.Rate(p.FullThrustN)==AssemblyResources.Times(AssemblyResources.Rate(p.ExtentRateKgS),15360),"Thrust/flow relationship must agree before binary64 rounding.");
            }
            if(def.Gimbal is {} g)
                Require(Identifier(g.Id)&&g.Id!=def.Propulsion!.Id&&g.Pivot.IsFinite&&g.NozzleOffset.IsFinite&&g.NozzleOffset.X is >=-.8 and <=0&&g.LimitY==.05&&g.LimitZ==.05&&g.SlewRate==.1&&def.Propulsion!.Axis==Double3.UnitX&&def.Propulsion.Point==g.Pivot+g.NozzleOffset&&Double3.Cross(g.NozzleOffset,Double3.UnitX)==Double3.Zero,"Unsupported gimbal law/datum/aliased capability identity.");
        }
        Require(defs.Select(x=>x.Role).Distinct().Count()==4,"One reusable definition for each role.");
        Require(!d.Instances.IsDefault&&d.Instances.Length==7&&d.Instances.Select(i=>i.Id).Distinct(StringComparer.Ordinal).Count()==7,"Seven distinct instances required.");
        var instances=d.Instances.OrderBy(i=>i.Order).ToImmutableArray();
        Require(instances.Select(i=>i.Order).SequenceEqual(Enumerable.Range(0,7)),"Canonical order must be explicit and complete.");
        var parts=ImmutableArray.CreateBuilder<CompiledPart>(7);
        foreach(var inst in instances)
        {
            Require(Identifier(inst.Id)&&inst.Pose.Rigid,"Invalid instance/pose.");
            var def=defs.SingleOrDefault(x=>x.Id==inst.Definition.Id);
            Require(def is not null&&def.Revision==inst.Definition.Revision&&AssemblyJson.Digest(def)==inst.Definition.Digest,"Unresolved or altered definition dependency.");
            parts.Add(new(inst,def!,inst.Pose.Point(def!.LocalCom),inst.Pose.Rotation*def.LocalInertia*inst.Pose.Rotation.Transpose()));
            if(def.Role==AssemblyRole.RcsBlock)
                foreach(var jet in def.JetActuators!.Value)Require(inst.Pose.Point(jet.Point).LengthSquared<=1.3*1.3,"Independent jet exceeds the admitted 1.3 m assembly endpoint radius.");
        }
        Require(parts.Count(p=>p.Definition.Role==(blocks?AssemblyRole.RcsBlock:AssemblyRole.RcsJet))==4&&parts.Count(p=>p.Definition.Role==AssemblyRole.MainEngine)==1&&parts.Count(p=>p.Definition.Role==AssemblyRole.Tank)==1&&parts.Count(p=>p.Definition.Role==AssemblyRole.Command)==1,"Incorrect capability/instance counts.");
        var command=parts.Single(p=>p.Definition.Role==AssemblyRole.Command);Require(d.CommandRoot==command.Instance.Id,"Wrong structural command root.");
        Require(!d.Attachments.IsDefault&&d.Attachments.Length==6,"Six structural edges required.");
        var occupied=new HashSet<string>(StringComparer.Ordinal);var parents=new Dictionary<string,string>(StringComparer.Ordinal);
        foreach(var e in d.Attachments)
        {
            var parent=parts.Single(p=>p.Instance.Id==e.Parent);var child=parts.Single(p=>p.Instance.Id==e.Child);
            Require((child.Definition.Role==AssemblyRole.Tank&&parent.Definition.Role==AssemblyRole.Command)||
                (child.Definition.Role is AssemblyRole.MainEngine or AssemblyRole.RcsJet or AssemblyRole.RcsBlock&&parent.Definition.Role==AssemblyRole.Tank),"Unsupported structural role relationship.");
            Require(e.Parent!=e.Child&&e.Child!=d.CommandRoot&&parents.TryAdd(e.Child,e.Parent),"Structural cycle/duplicate parent.");
            Require(occupied.Add(e.Parent+":"+e.ParentEndpoint)&&occupied.Add(e.Child+":"+e.ChildEndpoint),"Occupied endpoint reused.");
            var pe=parent.Definition.Attachments.Single(a=>a.Id==e.ParentEndpoint);var ce=child.Definition.Attachments.Single(a=>a.Id==e.ChildEndpoint);
            var a=parent.Instance.Pose.Then(pe.Frame);var b=child.Instance.Pose.Then(ce.Frame);
            Require(pe.Family==ce.Family&&(a.Position-b.Position).LengthSquared<=1e-20&&(a.Rotation*Matrix3.Mate-b.Rotation).Maximum<=1e-12,"Mating frame mismatch.");
        }
        foreach(var part in parts)
        {
            var id=part.Instance.Id;var seen=new HashSet<string>(StringComparer.Ordinal);
            while(id!=d.CommandRoot){Require(seen.Add(id)&&parents.ContainsKey(id),"Disconnected/cyclic structural graph.");id=parents[id];}
        }
        var tank=parts.Single(p=>p.Definition.Role==AssemblyRole.Tank);
        foreach(var store in tank.Definition.Stores)Require(tank.Instance.Pose.Point(store.Datum)==Double3.Zero,"This qualified point-store profile requires authored common assembly datum O; no recentering.");
        Require(!d.Feeds.IsDefault&&d.Feeds.Length==(blocks?34:10),"Exactly two explicit typed feeds per actuator required.");
        var consumers=parts.SelectMany(p=>p.Definition.Propulsion is {} single ? new[]{(Part:p.Instance.Id,Actuator:single.Id)} : p.Definition.JetActuators?.Select(j=>(Part:p.Instance.Id,Actuator:j.Id)).ToArray()??[]).ToArray();
        var feedkeys=new HashSet<(string Part,string Actuator,string Species)>();
        foreach(var f in d.Feeds)
        {
            var consumer=consumers.SingleOrDefault(c=>c.Part==f.Consumer&&(blocks?c.Actuator==f.Actuator:f.Actuator is null));
            Require(f.Source==tank.Instance.Id&&tank.Definition.Stores.Any(s=>s.Id==f.Store&&s.Species==f.Species)&&consumer.Part is not null,"Invalid typed feed.");
            Require(feedkeys.Add((consumer.Part!,consumer.Actuator,f.Species)),"Duplicate species feed.");
        }
        foreach(var p in consumers)foreach(var species in new[]{"FUEL","OXIDIZER"})Require(feedkeys.Contains((p.Part,p.Actuator,species)),"Missing explicit species feed.");
        Require(!d.Pairs.IsDefault&&d.Pairs.Length==6&&d.Pairs.Select(p=>p.Name).Distinct().Count()==6,"Six named sign commands required.");
        Require(d.Pairs.Select(p=>(p.Axis,p.Sign)).Distinct().Count()==6&&d.Pairs.All(p=>p.Axis is >=0 and <3&&p.Sign is -1 or 1),"Complete signed axes required.");
        bool IsJet(string part,string? actuator)=>parts.Any(x=>x.Instance.Id==part&&(blocks?x.Definition.Role==AssemblyRole.RcsBlock&&x.Definition.JetActuators!.Value.Any(j=>j.Id==actuator):x.Definition.Role==AssemblyRole.RcsJet&&actuator is null));
        foreach(var p in d.Pairs)Require(Identifier(p.Name)&&(p.First,p.FirstActuator)!=(p.Second,p.SecondActuator)&&IsJet(p.First,p.FirstActuator)&&IsJet(p.Second,p.SecondActuator),"A sign row must select two distinct full-on jets.");
        var dry=0d;var first=Double3.Zero;var origin=default(Matrix3);
        foreach(var p in parts){dry+=p.Definition.DryMassKg;first+=p.Com*p.Definition.DryMassKg;origin+=p.InertiaAtOwnCom+Matrix3.Parallel(p.Com)*p.Definition.DryMassKg;}
        Require(dry>=630&&dry<=730&&first.Y==0&&first.Z==0&&origin.B==0&&origin.C==0&&origin.D==0&&origin.F==0&&origin.G==0&&origin.H==0,"Outside fixed-axis point-store qualification; full tensor not discarded.");
        Require(double.IsFinite(d.InitialFuelKg)&&double.IsFinite(d.InitialOxidizerKg)&&d.InitialFuelKg>=0&&d.InitialFuelKg<=40&&d.InitialOxidizerKg>=0&&d.InitialOxidizerKg<=60,"Initial amounts outside capacity.");
        var canonical=raw with {Definitions=defs,Design=d with {Instances=instances,
            Attachments=d.Attachments.OrderBy(x=>x.Child,StringComparer.Ordinal).ToImmutableArray(),
            Feeds=d.Feeds.OrderBy(x=>x.Consumer,StringComparer.Ordinal).ThenBy(x=>x.Actuator,StringComparer.Ordinal).ThenBy(x=>x.Species,StringComparer.Ordinal).ToImmutableArray(),
            Pairs=d.Pairs.OrderBy(x=>x.Name,StringComparer.Ordinal).ToImmutableArray()}};
        var result=new CompiledAssemblyDesign(canonical,parts.ToImmutable(),dry,first,origin);
        AssemblyProfileAdmission.Validate(result);
        return result;
    }
}
