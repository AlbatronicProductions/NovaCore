using System.Collections.Immutable;
using NovaCore.Core;
using static NovaCore.Simulation.Spacecraft.Assemblies.AssemblyConstructionFacts;

namespace NovaCore.Simulation.Spacecraft.Assemblies;

internal sealed record CraftPlacementRequest(DefinitionReference Definition,string Parent,string Target,string Mount,
    int Clock,int Count,string InstancePrefix);

internal sealed partial class ConstructionEditorSession
{
    // A candidate is uncommitted design data. The frontend may paint refused ghosts,
    // but only PreviewEdit can install a validated preview and AcceptPreview commit it.
    internal ConstructionDesignData PreparePlacement(long expected,CraftPlacementRequest request)
    {
        Verify(expected);Require(PlayerDocument&&request is not null,"Player placement request required.");
        Require(Identifier(request.InstancePrefix)&&request.InstancePrefix.Length<=100,"Invalid placement identity.");
        var design=Document().Design;var data=design.Data;var parent=design.Parts[design.Index(request.Parent)];var child=Catalog.Resolve(request.Definition);
        var mechanical=parent.Definition.Standard!.Mechanical.SingleOrDefault(m=>m.Interface==request.Target);
        Require(mechanical is not null,"Unknown target socket.");
        PartSocketGroup? group=null;SocketPlacementSet? set=null;ImmutableArray<string> targets;
        if(mechanical.Kind==MechanicalKind.Radial)
        {
            group=parent.Definition.Standard.SocketGroups.SingleOrDefault(g=>g.Sockets.Contains(request.Target));
            Require(group is not null,"Radial target requires an authored socket group.");
            set=group.Placements.SingleOrDefault(s=>s.Anchor==request.Target&&s.Count==request.Count);
            Require(set is not null,"Unsupported authored symmetry count.");targets=set.Sockets;
        }
        else {Require(request.Count==1,"Stack placement is singular.");targets=[request.Target];}
        var instances=data.Instances;var connections=data.Connections;var configurations=data.Configuration;
        var added=ImmutableArray.CreateBuilder<PartInstanceData>();
        for(var i=0;i<targets.Length;i++)
        {
            var id=request.InstancePrefix+"-"+i;
            var pose=PartCompatibilityEvaluator.Snap(parent,targets[i],child,request.Mount,request.Clock);
            var part=new PartInstanceData(id,instances.Length,request.Definition,pose);instances=instances.Add(part);added.Add(part);
            var services=parent.Definition.Construction!.Interfaces.Single(a=>a.Interface==targets[i]).Services&child.Construction!.Interfaces.Single(a=>a.Interface==request.Mount).Services;
            connections=connections.Add(new(request.Parent,targets[i],id,request.Mount,new("joint-"+id,false,services,request.Clock)));
            configurations=configurations.Add(EmptyConfiguration(id,child));
        }
        var symmetry=data.Symmetry;
        if(group is not null&&set is not null)
        {
            var basis=added[0];var inverse=PartStandard.Inverse(basis.Pose);
            var members=added.Select(p=>new ConstructionSymmetryMember(p.Id,p.Id==basis.Id?new(Double3.Zero,Matrix3.Identity):inverse.Then(p.Pose))).ToImmutableArray();
            symmetry=symmetry.Add(new("group-"+request.InstancePrefix,basis.Id,members,new(request.Parent,group.Id,set.Anchor,set.Count,set.Sockets,group.Axis,request.Definition)));
        }
        return data with {Instances=instances,Connections=connections,Configuration=configurations,Symmetry=symmetry};
    }
    internal void PreviewPlacement(long expected,CraftPlacementRequest request)=>PreviewEdit(expected,PreparePlacement(expected,request));
    internal ConstructionDesignData PrepareReconnect(long expected,string selected,string parentId,string targetId,int clock)
    {
        Verify(expected);Require(PlayerDocument,"Player reconnection required.");var design=Document().Design;var data=design.Data;
        Require(selected!=data.Root,"Root has no incoming connection.");
        var parent=design.Parts[design.Index(parentId)];var grouped=data.Symmetry.SingleOrDefault(g=>g.Members.Any(m=>m.Part==selected));
        var members=grouped?.Members.Select(m=>m.Part).ToImmutableArray()??[selected];
        PartSocketGroup? targetGroup=null;SocketPlacementSet? targetSet=null;
        if(grouped is not null)
        {
            targetGroup=parent.Definition.Standard!.SocketGroups.SingleOrDefault(g=>g.Sockets.Contains(targetId));
            targetSet=targetGroup?.Placements.SingleOrDefault(s=>s.Anchor==targetId&&s.Count==members.Length);
            Require(targetGroup is not null&&targetSet is not null,"Moving a group requires one complete authored target set.");
        }
        var instances=data.Instances;var connections=data.Connections;
        for(var i=0;i<members.Length;i++)
        {
            var subtree=Descendants(design,members[i]);Require(!subtree.Contains(parentId),"Cannot reconnect into a moved subtree.");
            var moving=design.Parts[design.Index(members[i])];var incoming=connections.Single(e=>e.Child==members[i]);
            var target=targetSet?.Sockets[i]??targetId;
            var snapped=PartCompatibilityEvaluator.Snap(parent,target,moving.Definition,incoming.ChildEndpoint,clock);
            var delta=snapped.Rotation*moving.Instance.Pose.Rotation.Transpose();var transform=new AssemblyPose(snapped.Position-delta.Apply(moving.Instance.Pose.Position),delta);
            instances=instances.Select(p=>subtree.Contains(p.Id)?p with {Pose=transform.Then(p.Pose)}:p).ToImmutableArray();
            var services=parent.Definition.Construction!.Interfaces.Single(a=>a.Interface==target).Services&moving.Definition.Construction!.Interfaces.Single(a=>a.Interface==incoming.ChildEndpoint).Services;
            connections=connections.Replace(incoming,incoming with {Parent=parentId,ParentEndpoint=target,Construction=incoming.Construction! with {ClockDegrees=clock,Services=services}});
        }
        var symmetry=data.Symmetry;
        if(grouped is not null)
        {
            var basis=instances.Single(p=>p.Id==grouped.BasePart);var inverse=PartStandard.Inverse(basis.Pose);
            var changed=grouped with {Members=grouped.Members.Select(m=>new ConstructionSymmetryMember(m.Part,m.Part==basis.Id?new(Double3.Zero,Matrix3.Identity):inverse.Then(instances.Single(p=>p.Id==m.Part).Pose))).ToImmutableArray(),
                Placement=grouped.Placement! with {Host=parentId,SocketGroup=targetGroup!.Id,Anchor=targetId,Sockets=targetSet!.Sockets,Axis=targetGroup.Axis}};
            symmetry=symmetry.Replace(grouped,changed);
        }
        return data with {Instances=instances,Connections=connections,Symmetry=symmetry};
    }
    internal void PreviewCraftReconnect(long expected,string selected,string parent,string target,int clock)
        =>PreviewEdit(expected,PrepareReconnect(expected,selected,parent,target,clock));
    internal void PreviewClock(long expected,string selected,int clock)
    {
        Verify(expected);var data=Document().Design.Data;
        var group=data.Symmetry.SingleOrDefault(g=>g.Members.Any(m=>m.Part==selected));
        var incoming=data.Connections.SingleOrDefault(e=>e.Child==(group?.BasePart??selected));
        Require(incoming is not null,"Select a connected part to clock.");
        if(incoming.Construction!.ClockDegrees==clock){PreviewEdit(expected,data);return;}
        PreviewCraftReconnect(expected,selected,incoming.Parent,incoming.ParentEndpoint,clock);
    }
    internal ImmutableArray<string> SelectionMembers(string selected)
    {
        var data=Document().Design.Data;Require(data.Instances.Any(p=>p.Id==selected),"Unknown selection.");
        return data.Symmetry.SingleOrDefault(g=>g.Members.Any(m=>m.Part==selected))?.Members.Select(m=>m.Part).ToImmutableArray()??[selected];
    }
    internal void FillForLaunch(long expected)
    {
        Verify(expected);var data=Document().Design.Data;
        SetConfigurations(expected,data.Instances.Select(p=>{
            var part=Catalog.Resolve(p.Definition);var before=data.Configuration.Single(c=>c.Part==p.Id);var policy=part.Standard!.Configuration;
            return before with {
                Stores=before.Stores.Select(s=>s with {QuantityKg=policy.FillStores?part.Stores.Single(t=>t.Id==s.Store).CapacityKg:s.QuantityKg,Enabled=policy.EnableStores||s.Enabled}).ToImmutableArray(),
                Electrical=before.Electrical.Select(e=>e with {ChargeJ=policy.ChargeBattery?part.Construction!.Electrical.Single(t=>t.Id==e.Module).CapacityJ:e.ChargeJ,Enabled=policy.EnableElectrical||e.Enabled}).ToImmutableArray()};
        }).ToImmutableArray());
    }
    internal void ConfigureSelection(long expected,string selected,double fillFraction,double batteryFraction,bool storesEnabled,bool electricalEnabled)
    {
        Verify(expected);Require(double.IsFinite(fillFraction)&&fillFraction is >=0 and <=1&&double.IsFinite(batteryFraction)&&batteryFraction is >=0 and <=1,"Configuration fractions must be between zero and one.");
        var members=SelectionMembers(selected);var data=Document().Design.Data;
        SetConfigurations(expected,members.Select(id=>{
            var p=Catalog.Resolve(data.Instances.Single(p=>p.Id==id).Definition);var before=data.Configuration.Single(c=>c.Part==id);var policy=p.Standard!.Configuration;
            return before with {Stores=before.Stores.Select(s=>s with {QuantityKg=policy.FillStores?p.Stores.Single(x=>x.Id==s.Store).CapacityKg*fillFraction:s.QuantityKg,Enabled=policy.EnableStores?storesEnabled:s.Enabled}).ToImmutableArray(),
                Electrical=before.Electrical.Select(e=>e with {ChargeJ=policy.ChargeBattery?p.Construction!.Electrical.Single(x=>x.Id==e.Module).CapacityJ*batteryFraction:e.ChargeJ,Enabled=policy.EnableElectrical?electricalEnabled:e.Enabled}).ToImmutableArray()};
        }).ToImmutableArray());
    }
}
