using System.Collections.Immutable;
using NovaCore.Core;
using static NovaCore.Simulation.Spacecraft.Assemblies.AssemblyConstructionFacts;

namespace NovaCore.Simulation.Spacecraft.Assemblies;

internal readonly record struct CraftPortKey(int Part,string Port);
internal sealed record CraftPort(CraftPortKey Key,PartServicePort Definition);
internal readonly record struct CraftPortRoute(int From,int To,int Crossings,string Owner);

/// <summary>Immutable explicit service graph. A structural joint alone creates no service.</summary>
internal sealed class CompiledCraftPorts
{
    internal const int MaximumPorts=32768,MaximumDirectedRoutes=262144;
    internal CompiledConstructionDesign Design {get;}
    internal ImmutableArray<CraftPort> Ports {get;}
    internal ImmutableArray<CraftPortRoute> Routes {get;}
    private readonly ImmutableArray<ImmutableArray<(int From,int Cost)>> incoming;
    private CompiledCraftPorts(CompiledConstructionDesign design,ImmutableArray<CraftPort> ports,ImmutableArray<CraftPortRoute> routes)
    {
        Design=design;Ports=ports;Routes=routes;
        var adjacency=Enumerable.Range(0,ports.Length).Select(_=>ImmutableArray.CreateBuilder<(int,int)>()).ToArray();
        foreach(var r in routes)adjacency[r.To].Add((r.From,r.Crossings));incoming=adjacency.Select(a=>a.ToImmutable()).ToImmutableArray();
    }
    internal static CompiledCraftPorts Compile(CompiledConstructionDesign design)
    {
        Require(design.Data.Schema==CompiledConstructionDesign.CraftSchema,"Explicit ports require CraftDocument.");
        Require(design.Parts.Sum(p=>p.Definition.Standard!.Ports.Length)<=MaximumPorts,"Craft port capacity exceeded.");
        var ports=design.Parts.SelectMany((p,i)=>p.Definition.Standard!.Ports.Select(v=>new CraftPort(new(i,v.Id),v))).ToImmutableArray();
        var lookup=ports.Select((p,i)=>(p.Key,i)).ToDictionary(v=>v.Key,v=>v.i);var routes=ImmutableArray.CreateBuilder<CraftPortRoute>();
        void Add(int from,int to,int crossings,string owner){Require(routes.Count<MaximumDirectedRoutes,"Craft route capacity exceeded.");routes.Add(new(from,to,crossings,owner));}
        for(var p=0;p<design.Parts.Length;p++)foreach(var r in design.Parts[p].Definition.Standard!.Routes){
            var from=lookup[new(p,r.From)];var to=lookup[new(p,r.To)];Add(from,to,0,$"{design.Parts[p].Instance.Id}/{r.Id}");if(r.Bidirectional)Add(to,from,0,$"{design.Parts[p].Instance.Id}/{r.Id}");
        }
        foreach(var joint in design.Connections){
            var a=ports.Select((p,i)=>(p,i)).Where(x=>x.p.Key.Part==joint.Parent&&x.p.Definition.Interface==joint.ParentInterface);
            var b=ports.Select((p,i)=>(p,i)).Where(x=>x.p.Key.Part==joint.Child&&x.p.Definition.Interface==joint.ChildInterface).ToArray();
            foreach(var x in a)foreach(var y in b)if(x.p.Definition.Service==y.p.Definition.Service&&x.p.Definition.Resource==y.p.Definition.Resource&&joint.Services.HasFlag(x.p.Definition.Service)){
                Add(x.i,y.i,1,joint.Id);Add(y.i,x.i,1,joint.Id);
            }
        }
        return new(design,ports,routes.OrderBy(r=>r.From).ThenBy(r=>r.To).ThenBy(r=>r.Owner,StringComparer.Ordinal).ToImmutableArray());
    }
    // Internal routes cost zero; crossing an explicit attached interface costs
    // one. Nearest/farthest store policy therefore remains a part-hop policy.
    internal ImmutableArray<int> SupplierDistances(int part,Func<PartServicePort,bool> terminal)
    {
        var distance=Enumerable.Repeat(int.MaxValue,Ports.Length).ToArray();var queue=new PriorityQueue<int,(int,int)>();
        for(var i=0;i<Ports.Length;i++)if(Ports[i].Key.Part==part&&terminal(Ports[i].Definition)){distance[i]=0;queue.Enqueue(i,(0,i));}
        while(queue.TryDequeue(out var node,out var priority)){
            if(priority.Item1!=distance[node])continue;
            foreach(var edge in incoming[node]){var next=checked(distance[node]+edge.Cost);if(next<distance[edge.From]){distance[edge.From]=next;queue.Enqueue(edge.From,(next,edge.From));}}
        }
        return distance.ToImmutableArray();
    }
    internal int StoreDistance(ImmutableArray<int> distance,int part,string store)
    {
        var best=int.MaxValue;for(var i=0;i<Ports.Length;i++)if(Ports[i].Key.Part==part&&Ports[i].Definition.Store==store)best=Math.Min(best,distance[i]);return best;
    }
    internal ImmutableArray<int> Components(ConstructionService service,out int count)
    {
        Require(service is ConstructionService.Electricity or ConstructionService.Data,"Explicit undirected service required.");
        var parent=Enumerable.Range(0,Ports.Length).ToArray();int Root(int p){while(parent[p]!=p)p=parent[p];return p;}
        foreach(var r in Routes)if(Ports[r.From].Definition.Service==service){var a=Root(r.From);var b=Root(r.To);parent[Math.Max(a,b)]=Math.Min(a,b);}
        var labels=new Dictionary<int,int>();var result=ImmutableArray.CreateBuilder<int>(Ports.Length);
        for(var p=0;p<Ports.Length;p++){if(Ports[p].Definition.Service!=service){result.Add(-1);continue;}var root=Root(p);if(!labels.TryGetValue(root,out var label)){label=labels.Count;labels.Add(root,label);}result.Add(label);}
        count=labels.Count;return result.MoveToImmutable();
    }
    internal int ElectricalBus(ImmutableArray<int> components,int part,string module)
    {
        var buses=Ports.Select((p,i)=>(p,i)).Where(x=>x.p.Key.Part==part&&x.p.Definition.Electrical==module).Select(x=>components[x.i]).Distinct().ToArray();
        Require(buses.Length==1&&buses[0]>=0,"Electrical module ports require one explicitly connected bus.");return buses[0];
    }
    internal bool CanCommand(int part,string? consumer)
    {
        if(Design.ControlIndex<0)return false;
        var distances=SupplierDistances(part,p=>p.Service==ConstructionService.Data&&(consumer is null?p.Command:p.Consumer==consumer));
        for(var i=0;i<Ports.Length;i++)if(Ports[i].Key.Part==Design.ControlIndex&&Ports[i].Definition.Command&&distances[i]!=int.MaxValue)return true;
        return false;
    }
}
