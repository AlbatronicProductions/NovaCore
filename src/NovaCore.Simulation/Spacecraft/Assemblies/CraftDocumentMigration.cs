using System.Collections.Immutable;
using static NovaCore.Simulation.Spacecraft.Assemblies.AssemblyConstructionFacts;

namespace NovaCore.Simulation.Spacecraft.Assemblies;

internal sealed record CraftDefinitionMigration(DefinitionReference Source,DefinitionReference Target);
internal sealed record CraftClockMigration(string Connection,int ClockDegrees);
internal sealed record CraftSymmetryMigration(string Group,AuthoredSocketPlacement Placement,ImmutableArray<string> MemberOrder);
internal sealed record CraftMigrationRecipe(string Id,string SourceDocumentDigest,string SourceDependencyDigest,
    ImmutableArray<CraftDefinitionMigration> Definitions,ImmutableArray<CraftClockMigration> Clocks,
    ImmutableArray<CraftSymmetryMigration> Symmetry);

/// <summary>Named, pinned, read-only migration into the existing design owner.</summary>
internal static class CraftDocumentMigration
{
    internal static CompiledConstructionDesign Migrate(AssemblyDefinitionCatalog sourceCatalog,ReadOnlySpan<byte> source,
        AssemblyDefinitionCatalog targetCatalog,CraftMigrationRecipe recipe,string playerName)
    {
        // Always validate source bytes under the original schema and owners first.
        var old=CompiledConstructionDesign.Load(sourceCatalog,source);var data=old.Data;
        Require(recipe is not null&&Identifier(recipe.Id)&&recipe.SourceDocumentDigest==old.Digest&&
            recipe.SourceDependencyDigest==data.DependencyDigest,"Migration recipe/source identity mismatch.");
        Require(targetCatalog.Data.Schema==AssemblyDefinitionCatalog.PartStandardSchema,"Migration requires qualified Part Standard definitions.");
        Require(data.Actions.Length==0,"Legacy executable action metadata has no admitted migration.");
        Require(!recipe.Definitions.IsDefault&&recipe.Definitions.Length<=1024,"Migration definition mapping required.");
        var used=data.Instances.Select(i=>i.Definition).Distinct().ToHashSet();var mappings=new Dictionary<DefinitionReference,DefinitionReference>();
        foreach(var mapping in recipe.Definitions){
            Require(mapping is not null&&mapping.Source is not null&&mapping.Target is not null&&used.Contains(mapping.Source)&&
                mappings.TryAdd(mapping.Source,mapping.Target),"Missing/duplicate/unused migration definition identity.");
            var prior=sourceCatalog.Resolve(mapping.Source);var next=targetCatalog.Resolve(mapping.Target);
            Require(prior.Stores.Length==next.Stores.Length&&prior.Stores.All(s=>next.Stores.Any(t=>t.Id==s.Id&&t.ResourceIdentity==s.ResourceIdentity)),
                "Migration must preserve each store resource identity beneath its quantity.");
            Require(prior.Construction!.Electrical.Length==next.Construction!.Electrical.Length&&
                prior.Construction.Electrical.All(e=>next.Construction.Electrical.Any(t=>t.Id==e.Id&&t.Role==e.Role)),
                "Migration must preserve each electrical module role beneath its configuration.");
        }
        Require(mappings.Count==used.Count,"Every used definition requires an exact migration mapping.");
        var instances=data.Instances.Select(i=>i with {Definition=mappings[i.Definition]}).ToImmutableArray();
        // Resource definitions cannot change beneath preserved numeric quantities.
        var oldResources=Resources(sourceCatalog,data.Instances);var newResources=Resources(targetCatalog,instances);
        Require(oldResources.SequenceEqual(newResources),"Migration cannot substitute resource identity/revision/content.");
        Unique(recipe.Clocks,c=>c.Connection,"migration clocks");
        Require(recipe.Clocks.Length==data.Connections.Length,"Every connection needs explicit migration clock provenance.");
        var connections=data.Connections.Select(edge=>{
            var clock=recipe.Clocks.SingleOrDefault(c=>c.Connection==edge.Construction!.Id);
            Require(clock is not null,"Missing migration connection mapping.");
            return edge with {Construction=edge.Construction! with {ClockDegrees=clock.ClockDegrees}};
        }).ToImmutableArray();
        Unique(recipe.Symmetry,s=>s.Group,"migration symmetry");
        Require(recipe.Symmetry.Length==data.Symmetry.Length,"Every old symmetry group needs explicit authored provenance.");
        var symmetry=data.Symmetry.Select(group=>{
            var mapping=recipe.Symmetry.SingleOrDefault(s=>s.Group==group.Id);
            Require(mapping is not null&&mapping.Placement is not null,"Missing migration symmetry mapping.");
            Unique(mapping.MemberOrder,x=>x,"migration member ordering");
            Require(mapping.MemberOrder.Length==group.Members.Length,"Migration must preserve exact membership.");
            var members=mapping.MemberOrder.Select(id=>group.Members.SingleOrDefault(m=>m.Part==id)??
                throw new InvalidDataException("Migration member not present in source.")).ToImmutableArray();
            return group with {Placement=mapping.Placement,Members=members};
        }).ToImmutableArray();
        return CompiledConstructionDesign.Compile(targetCatalog,data with {Schema=CompiledConstructionDesign.CraftSchema,
            Craft=new(playerName,1),Instances=instances,DependencyDigest=targetCatalog.DependencyDigest(instances),
            Connections=connections,Symmetry=symmetry});
    }
    private static ImmutableArray<ResourceTypeData> Resources(AssemblyDefinitionCatalog catalog,ImmutableArray<PartInstanceData> instances)
    {
        var definitions=instances.Select(i=>catalog.Resolve(i.Definition)).ToArray();
        var ids=definitions.SelectMany(d=>d.Stores.Select(s=>s.ResourceIdentity)
            .Concat(d.Construction!.Consumers.SelectMany(c=>c.Mixture.Select(m=>m.Resource)))
            .Concat(d.Standard is {} standard?standard.Ports.Where(p=>p.Resource is not null).Select(p=>p.Resource!):[]))
            .ToHashSet(StringComparer.Ordinal);
        return catalog.Data.Resources.Where(r=>ids.Contains(r.Id)).OrderBy(r=>r.Id,StringComparer.Ordinal).ToImmutableArray();
    }
}
