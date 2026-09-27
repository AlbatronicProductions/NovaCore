using System.Collections.Immutable;
using NovaCore.Core;
using static NovaCore.Simulation.Spacecraft.Assemblies.AssemblyConstructionFacts;

namespace NovaCore.Simulation.Spacecraft.Assemblies;

internal enum MechanicalKind { Stack, Radial }
internal enum MateRole { Socket, Plug }
internal enum StoreDepletionLaw { ProportionalSpatial }
internal sealed record PartMechanicalInterface(string Interface, MechanicalKind Kind, string Family,
    uint Revision, string Size, MateRole Role, ImmutableArray<int> ClockDegrees);
internal sealed record PartConvexVolume(string Id, ImmutableArray<Double3> Vertices, string Provenance);
internal sealed record PartSupportFoot(string Id, AssemblyPose Frame, double HalfWidthY, double HalfWidthZ,
    double MaximumLoadN, string MassRegion, string Provenance);
internal sealed record PartStoreLaw(string Store, StoreDepletionLaw Law, double DensityKgM3,
    double InnerRadiusM, double OuterRadiusM, double LengthM, string Provenance);
internal sealed record PartServicePort(string Id, ConstructionService Service, string? Resource,
    string? Interface, string? Store, string? Consumer, string? Electrical, bool Command);
internal sealed record PartInternalRoute(string Id, string From, string To, bool Bidirectional);
internal sealed record SocketPlacementSet(string Anchor, int Count, ImmutableArray<string> Sockets);
internal sealed record PartSocketGroup(string Id, AssemblyPose Axis, ImmutableArray<string> Sockets,
    ImmutableArray<SocketPlacementSet> Placements);
internal sealed record PartConfigurationPolicy(bool FillStores, bool ChargeBattery, bool EnableStores,
    bool EnableElectrical);
internal sealed record PartStandardData(string Schema, bool RootEligible, string Category, string Purpose,
    ImmutableArray<PartMechanicalInterface> Mechanical, ImmutableArray<PartSocketGroup> SocketGroups,
    ImmutableArray<PartConvexVolume> Collision, ImmutableArray<PartConvexVolume> Clearance,
    ImmutableArray<PartSupportFoot> Support, ImmutableArray<PartStoreLaw> StoreLaws,
    ImmutableArray<PartServicePort> Ports, ImmutableArray<PartInternalRoute> Routes,
    ImmutableArray<string> RequiredCommandLoads, PartConfigurationPolicy Configuration);

/// <summary>Cold immutable-definition validation. Visual assets have no physical authority.</summary>
internal static class PartStandard
{
    internal const string Schema="novacore.part-standard/1";
    internal static PartStandardData Canonical(PartStandardData s)=>s with {
        Mechanical=s.Mechanical.OrderBy(x=>x.Interface,StringComparer.Ordinal).Select(x=>x with {ClockDegrees=x.ClockDegrees.Order().ToImmutableArray()}).ToImmutableArray(),
        SocketGroups=s.SocketGroups.OrderBy(x=>x.Id,StringComparer.Ordinal).Select(x=>x with {
            Sockets=x.Sockets.Order(StringComparer.Ordinal).ToImmutableArray(),
            Placements=x.Placements.OrderBy(p=>p.Anchor,StringComparer.Ordinal).ThenBy(p=>p.Count).ToImmutableArray()}).ToImmutableArray(),
        Collision=Volumes(s.Collision),Clearance=Volumes(s.Clearance),Support=s.Support.OrderBy(x=>x.Id,StringComparer.Ordinal).ToImmutableArray(),
        StoreLaws=s.StoreLaws.OrderBy(x=>x.Store,StringComparer.Ordinal).ToImmutableArray(),
        Ports=s.Ports.OrderBy(x=>x.Id,StringComparer.Ordinal).ToImmutableArray(),Routes=s.Routes.OrderBy(x=>x.Id,StringComparer.Ordinal).ToImmutableArray(),
        RequiredCommandLoads=s.RequiredCommandLoads.Order(StringComparer.Ordinal).ToImmutableArray()};
    private static ImmutableArray<PartConvexVolume> Volumes(ImmutableArray<PartConvexVolume> values)=>values.OrderBy(x=>x.Id,StringComparer.Ordinal)
        .Select(x=>x with {Vertices=x.Vertices.OrderBy(v=>v.X).ThenBy(v=>v.Y).ThenBy(v=>v.Z).ToImmutableArray()}).ToImmutableArray();
    internal static void Validate(PartDefinitionData definition)
    {
        var s=definition.Standard??throw new InvalidDataException("PART_STANDARD_MISSING");
        var c=definition.Construction??throw new InvalidDataException("CONSTRUCTION_MISSING");
        Require(s.Schema==Schema,"PART_STANDARD_SCHEMA");
        Require(Text(s.Category,80)&&Text(s.Purpose,512)&&s.Configuration is not null,"PART_PRESENTATION_CONFIGURATION");
        Require(!s.RootEligible||c.Command,"ROOT_REQUIRES_COMMAND");
        Unique(s.Mechanical,x=>x.Interface,"mechanical interfaces");
        Require(s.Mechanical.Length==definition.Attachments.Length,"MECHANICAL_COVERAGE");
        foreach(var m in s.Mechanical)
        {
            Require(Enum.IsDefined(m.Kind)&&Enum.IsDefined(m.Role)&&Identifier(m.Family)&&m.Revision>0,
                "MECHANICAL_IDENTITY");
            Require(m.Kind==MechanicalKind.Stack ? m.Family=="nc.stack"&&m.Revision==1&&m.Size is "NC-1" or "NC-2" :
                m.Family=="nc.radial-equipment"&&m.Revision==1&&m.Size=="R-1","MECHANICAL_CLASS");
            Require(!m.ClockDegrees.IsDefaultOrEmpty&&m.ClockDegrees.Length<=4&&
                m.ClockDegrees.All(x=>x is 0 or 90 or 180 or 270)&&m.ClockDegrees.Distinct().Count()==m.ClockDegrees.Length,
                "MECHANICAL_CLOCK_POLICY");
            Require(m.Kind!=MechanicalKind.Radial||m.ClockDegrees.SequenceEqual([0]),"RADIAL_KEYED");
            var endpoint=definition.Attachments.SingleOrDefault(x=>x.Id==m.Interface);
            Require(endpoint is not null&&endpoint.Frame.Rigid&&endpoint.Family==FamilyKey(m),"MECHANICAL_ENDPOINT");
        }
        Unique(s.Collision,x=>x.Id,"collision volumes");
        Require(s.Collision.Length>0,"PHYSICAL_COLLISION_REQUIRED");
        Unique(s.Clearance,x=>x.Id,"clearance volumes");
        foreach(var v in s.Collision.Concat(s.Clearance)) ValidateVolume(v);
        Unique(s.Support,x=>x.Id,"support feet");
        foreach(var f in s.Support)
            Require(f.Frame.Rigid&&Positive(f.HalfWidthY)&&Positive(f.HalfWidthZ)&&Positive(f.MaximumLoadN)&&
                c.MassRegions.Any(r=>r.Id==f.MassRegion)&&Text(f.Provenance,1024),"SUPPORT_LOAD_PATH");
        Unique(s.StoreLaws,x=>x.Store,"store laws");
        Require(s.StoreLaws.Length==definition.Stores.Length,"STORE_LAW_COVERAGE");
        foreach(var law in s.StoreLaws)
        {
            var store=definition.Stores.SingleOrDefault(x=>x.Id==law.Store);
            var geometry=c.StoreGeometry.SingleOrDefault(x=>x.Store==law.Store);
            Require(store is not null&&geometry is not null&&Enum.IsDefined(law.Law)&&Positive(law.DensityKgM3)&&
                double.IsFinite(law.InnerRadiusM)&&law.InnerRadiusM>=0&&Positive(law.OuterRadiusM)&&
                law.OuterRadiusM>law.InnerRadiusM&&Positive(law.LengthM)&&Text(law.Provenance,1024),"STORE_LAW");
            PartStandardPhysicalConsistency.Validate(definition.Id,law,store,geometry);
        }
        // The admitted analytic stores are co-axial along part X. Positive-volume overlap is forbidden.
        for(var i=0;i<s.StoreLaws.Length;i++) for(var j=i+1;j<s.StoreLaws.Length;j++)
        {
            var a=s.StoreLaws[i];var b=s.StoreLaws[j];
            var pa=definition.Stores.Single(x=>x.Id==a.Store).Datum;var pb=definition.Stores.Single(x=>x.Id==b.Store).Datum;
            Require(pa.Y==pb.Y&&pa.Z==pb.Z,"STORE_NONCOAXIAL_UNQUALIFIED");
            var radial=Math.Min(a.OuterRadiusM,b.OuterRadiusM)>Math.Max(a.InnerRadiusM,b.InnerRadiusM);
            Require(!radial||!PartStandardExact.AxialOverlap(pa.X,a.LengthM,pb.X,b.LengthM),
                $"STORE_PHYSICAL_OVERLAP definition[{definition.Id}].stores[{a.Store},{b.Store}]");
        }
        ValidatePorts(definition,s,c);
        Unique(s.RequiredCommandLoads,x=>x,"required command loads");
        Require(s.RequiredCommandLoads.All(id=>c.Electrical.Any(e=>e.Id==id&&e.Role==ElectricalRole.Load)),"COMMAND_LOAD_BINDING");
        Require(!c.Command||s.RequiredCommandLoads.Length>0,"COMMAND_REQUIRES_POWER");
        Require(c.Consumers.Length==0||s.RequiredCommandLoads.Length>0,"ACTUATOR_REQUIRES_POWER");
        ValidateSocketGroups(definition,s);
    }
    private static void ValidatePorts(PartDefinitionData definition,PartStandardData s,PartConstructionData c)
    {
        Unique(s.Ports,x=>x.Id,"service ports"); Unique(s.Routes,x=>x.Id,"internal routes");
        foreach(var p in s.Ports)
        {
            Require(p.Service is ConstructionService.Propellant or ConstructionService.Electricity or ConstructionService.Data,
                "SERVICE_PORT_KIND");
            var bindings=(p.Interface is null?0:1)+(p.Store is null?0:1)+(p.Consumer is null?0:1)+(p.Electrical is null?0:1)+(p.Command?1:0);
            Require(bindings==1,"SERVICE_PORT_SINGLE_OWNER");
            Require(p.Service==ConstructionService.Propellant?Identifier(p.Resource):p.Resource is null,
                $"SERVICE_PORT_RESOURCE definition[{definition.Id}].standard.ports[{p.Id}].resource");
            if(p.Interface is {} endpoint) Require(c.Interfaces.Any(i=>i.Interface==endpoint&&i.Services.HasFlag(p.Service)),"SERVICE_INTERFACE_BINDING");
            if(p.Store is {} store) Require(p.Service==ConstructionService.Propellant&&definition.Stores.Any(x=>x.Id==store&&x.ResourceIdentity==p.Resource),"SERVICE_STORE_BINDING");
            if(p.Consumer is {} consumer) Require(c.Consumers.Any(x=>x.Id==consumer&&
                (p.Service!=ConstructionService.Propellant||x.Mixture.Any(m=>m.Resource==p.Resource))),"SERVICE_CONSUMER_BINDING");
            if(p.Electrical is {} electrical) Require(p.Service==ConstructionService.Electricity&&c.Electrical.Any(x=>x.Id==electrical),"SERVICE_ELECTRICAL_BINDING");
            if(p.Command) Require(p.Service==ConstructionService.Data&&c.Command,"SERVICE_COMMAND_BINDING");
        }
        foreach(var r in s.Routes)
        {
            var a=s.Ports.SingleOrDefault(p=>p.Id==r.From);var b=s.Ports.SingleOrDefault(p=>p.Id==r.To);
            Require(a is not null&&b is not null&&r.From!=r.To&&a.Service==b.Service&&a.Resource==b.Resource,
                "INTERNAL_ROUTE_BINDING");
            Require(a.Service==ConstructionService.Propellant||r.Bidirectional,"WIRE_ROUTE_BIDIRECTIONAL");
        }
        foreach(var store in definition.Stores) Require(s.Ports.Any(p=>p.Store==store.Id),"STORE_PORT_REQUIRED");
        foreach(var engine in c.Consumers)
        {
            foreach(var m in engine.Mixture) Require(s.Ports.Any(p=>p.Consumer==engine.Id&&p.Resource==m.Resource),"ACTUATOR_INLET_REQUIRED");
            Require(s.Ports.Any(p=>p.Consumer==engine.Id&&p.Service==ConstructionService.Data),"ACTUATOR_DATA_REQUIRED");
        }
        foreach(var e in c.Electrical) Require(s.Ports.Any(p=>p.Electrical==e.Id),"ELECTRICAL_PORT_REQUIRED");
        Require(!c.Command||s.Ports.Any(p=>p.Command),"COMMAND_PORT_REQUIRED");
    }
    private static void ValidateSocketGroups(PartDefinitionData definition,PartStandardData s)
    {
        var field=$"definition[{definition.Id}].standard.socketGroups";
        Unique(s.SocketGroups,x=>x.Id,field);var claimed=new HashSet<string>(StringComparer.Ordinal);
        foreach(var group in s.SocketGroups)
        {
            var path=$"{field}[{group.Id}]";
            Require(group.Axis.Rigid,$"SOCKET_GROUP_AXIS {path}.axis"); Unique(group.Sockets,x=>x,$"{path}.sockets");
            Require(group.Sockets.Length>0&&group.Sockets.All(id=>claimed.Add(id)&&s.Mechanical.Any(m=>m.Interface==id&&m.Kind==MechanicalKind.Radial&&m.Role==MateRole.Socket)),
                $"SOCKET_GROUP_MEMBERS {path}.sockets");
            Require(!group.Placements.IsDefaultOrEmpty&&group.Placements.Length<=4096,$"SOCKET_PLACEMENTS_REQUIRED {path}.placements");
            var keys=new HashSet<(string,int)>();
            for(var placement=0;placement<group.Placements.Length;placement++)
            {
                var set=group.Placements[placement];var at=$"{path}.placements[{placement}]";
                Require(set is not null,$"SOCKET_PLACEMENT_NULL {at}");
                Require(Identifier(set.Anchor)&&group.Sockets.Contains(set.Anchor),$"SOCKET_PLACEMENT_ANCHOR {at}.anchor");
                Require(keys.Add((set.Anchor,set.Count))&&set.Count is 1 or 2 or 4 or 8&&
                    !set.Sockets.IsDefault&&set.Sockets.Length==set.Count&&set.Sockets[0]==set.Anchor&&
                    set.Sockets.Distinct().Count()==set.Count&&set.Sockets.All(id=>Identifier(id)&&group.Sockets.Contains(id)),
                    $"SOCKET_PLACEMENT_SET {at}.count/sockets");
                var anchor=definition.Attachments.Single(x=>x.Id==set.Anchor).Frame;
                var inv=Inverse(group.Axis);
                for(var i=0;i<set.Count;i++)
                {
                    var expected=group.Axis.Then(new(Double3.Zero,Roll(360d*i/set.Count))).Then(inv).Then(anchor);
                    var actual=definition.Attachments.Single(x=>x.Id==set.Sockets[i]).Frame;
                    Require((expected.Position-actual.Position).LengthSquared<=1e-18&&
                        (expected.Rotation-actual.Rotation).Maximum<=1e-12,$"SOCKET_AUTHORED_ROTATIONAL_EQUIVALENCE {at}.sockets[{i}]");
                }
            }
        }
    }
    internal static string FamilyKey(PartMechanicalInterface m)=>$"{m.Family}/{m.Revision}/{m.Size}";
    internal static bool CanMate(PartMechanicalInterface a,PartMechanicalInterface b,int clockDegrees)=>
        a.Kind==b.Kind&&a.Family==b.Family&&a.Revision==b.Revision&&a.Size==b.Size&&a.Role!=b.Role&&
        a.ClockDegrees.Contains(clockDegrees)&&b.ClockDegrees.Contains(clockDegrees);
    internal static Matrix3 Roll(double degrees)
    {var r=degrees*(Math.PI/180);var c=Math.Cos(r);var s=Math.Sin(r);return new(1,0,0,0,c,-s,0,s,c);}
    internal static AssemblyPose Inverse(AssemblyPose p)
    {var r=p.Rotation.Transpose();return new(r.Apply(p.Position)*-1,r);}
    private static void ValidateVolume(PartConvexVolume v)
    {
        Require(!v.Vertices.IsDefault&&v.Vertices.Length is >=4 and <=64&&v.Vertices.All(x=>x.IsFinite)&&
            v.Vertices.Distinct().Count()==v.Vertices.Length&&Text(v.Provenance,1024),"PHYSICAL_CONVEX_VERTICES");
        // Explicit convex-hull vertices: reject rank-deficient geometry without a mesh-derived substitute.
        Require(PartStandardExact.FullRank(v.Vertices),$"PHYSICAL_VOLUME_DEGENERATE volume[{v.Id}]");
    }
    private static bool Positive(double x)=>double.IsFinite(x)&&x>0;
    private static bool Text(string? x,int cap)=>!string.IsNullOrWhiteSpace(x)&&x.Length<=cap;
}
