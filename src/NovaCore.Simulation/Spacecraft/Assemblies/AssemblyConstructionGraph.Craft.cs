using System.Collections.Immutable;
using System.Text.Json;
using static NovaCore.Simulation.Spacecraft.Assemblies.AssemblyConstructionFacts;

namespace NovaCore.Simulation.Spacecraft.Assemblies;

internal sealed partial class CompiledConstructionDesign
{
    private static void RejectLegacyCraftFields(ReadOnlySpan<byte> bytes)
    {
        // Presence, including explicit null, was an unknown field to the original reader.
        using var json=JsonDocument.Parse(bytes.ToArray());var root=json.RootElement;
        Require(!root.TryGetProperty("craft",out _),"Legacy document has craft metadata; explicit migration required.");
        var connections=root.GetProperty("connections");var symmetry=root.GetProperty("symmetry");
        Require(connections.ValueKind==JsonValueKind.Array&&symmetry.ValueKind==JsonValueKind.Array,"Legacy connection/symmetry arrays required.");
        foreach(var edge in connections.EnumerateArray())
            if(edge.ValueKind==JsonValueKind.Object&&edge.TryGetProperty("construction",out var c)&&c.ValueKind==JsonValueKind.Object)
                Require(!c.TryGetProperty("clockDegrees",out _),"Legacy connection has indexed clock metadata.");
        foreach(var group in symmetry.EnumerateArray())
            if(group.ValueKind==JsonValueKind.Object)Require(!group.TryGetProperty("placement",out _),"Legacy symmetry has authored placement metadata.");
    }
    private static void ValidateCraftBindings(ConstructionDesignData data,ImmutableArray<CompiledPart> parts)
    {
        var members=data.Symmetry.SelectMany(g=>g.Members.Select(m=>m.Part)).ToHashSet(StringComparer.Ordinal);
        foreach(var group in data.Symmetry){
            var at=$"symmetry[{group.Id}]";var placement=group.Placement!;
            var host=parts.SingleOrDefault(p=>p.Instance.Id==placement.Host);
            Require(host.Definition is not null&&!members.Contains(placement.Host),$"{at}.host: missing or nested symmetry");
            var ancestor=placement.Host;
            while(ancestor!=data.Root){
                var parent=data.Connections.Single(e=>e.Child==ancestor).Parent;
                Require(!members.Contains(parent),$"{at}.host: nested symmetry branch");ancestor=parent;
            }
            var sockets=host.Definition.Standard!.SocketGroups.SingleOrDefault(g=>g.Id==placement.SocketGroup);
            Require(sockets is not null&&placement.Axis==sockets.Axis,$"{at}.socketGroup/axis");
            var set=sockets.Placements.SingleOrDefault(p=>p.Anchor==placement.Anchor&&p.Count==placement.Count);
            Require(set is not null&&!placement.Sockets.IsDefault&&set.Sockets.SequenceEqual(placement.Sockets)&&
                group.Members.Length==placement.Count&&group.BasePart==group.Members[0].Part,$"{at}.authoredPlacement");
            for(var i=0;i<group.Members.Length;i++){
                var part=parts.Single(p=>p.Instance.Id==group.Members[i].Part);
                Require(placement.Definition is not null&&part.Instance.Definition==placement.Definition,$"{at}.definition");
                var edge=data.Connections.SingleOrDefault(e=>e.Child==part.Instance.Id);
                Require(edge is not null&&edge.Parent==placement.Host&&edge.ParentEndpoint==placement.Sockets[i]&&
                    edge.Construction!.ClockDegrees==0,$"{at}.members[{i}].targetSocket");
                var mate=part.Definition.Standard!.Mechanical.Single(m=>m.Interface==edge.ChildEndpoint);
                Require(mate.Kind==MechanicalKind.Radial&&mate.Role==MateRole.Plug,$"{at}.members[{i}].radialPlug");
            }
        }
    }
    // A strict player-document entry point; legacy bytes are only read by their original path.
    internal static CompiledConstructionDesign LoadCraft(AssemblyDefinitionCatalog catalog,ReadOnlySpan<byte> bytes)
    {
        var data=AssemblyJson.Read<ConstructionDesignData>(bytes,MaximumDocumentBytes);
        Require(data.Schema==CraftSchema,"CraftDocument schema required; use an explicit named migration.");
        return Compile(catalog,data);
    }
}
