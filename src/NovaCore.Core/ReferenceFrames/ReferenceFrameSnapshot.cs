using System.Collections.ObjectModel;

namespace NovaCore.Core.ReferenceFrames;

/// <summary>Immutable, validated evaluated graph for one caller-defined simulation instant.</summary>
public sealed class ReferenceFrameSnapshot
{
    private readonly Dictionary<ReferenceFrameId, Entry> _entries;
    private readonly ReadOnlyDictionary<ReferenceFrameId, Entry> _readOnlyEntries;
    internal readonly record struct Entry(ReferenceFrameDefinition Definition, EvaluatedReferenceFrame Evaluated, FrameTransform LocalToRoot, Double3 OriginVelocityInRoot, Double3 AngularVelocityInRoot, bool IsInertial);
    public ReferenceFrameId RootId { get; }
    internal IReadOnlyDictionary<ReferenceFrameId, Entry> Entries => _readOnlyEntries;

    public ReferenceFrameSnapshot(IEnumerable<(ReferenceFrameDefinition Definition, EvaluatedReferenceFrame Evaluated)> frames)
    {
        var input=new Dictionary<ReferenceFrameId,(ReferenceFrameDefinition Definition,EvaluatedReferenceFrame Evaluated)>();
        foreach(var item in frames)
        {
            if(!input.TryAdd(item.Definition.Id,item)) throw new ArgumentException("Duplicate reference-frame ID.");
            if(string.IsNullOrWhiteSpace(item.Definition.DiagnosticName)) throw new ArgumentException("Frame diagnostic name is required.");
            if(!item.Evaluated.LocalToParent.IsFinite||!item.Evaluated.OriginVelocityInParent.IsFinite||!item.Evaluated.AngularVelocityInParent.IsFinite||Math.Abs(item.Evaluated.LocalToParent.Rotation.LengthSquared-1d)>1e-10d) throw new ArgumentException("Frame evaluation contains non-finite or non-normalized data.");
        }
        var roots=input.Values.Where(x=>x.Definition.ParentId is null).ToArray();
        if(roots.Length!=1||roots[0].Definition.Kind!=ReferenceFrameKind.Ecl||roots[0].Evaluated.LocalToParent!=FrameTransform.Identity||roots[0].Evaluated.OriginVelocityInParent!=Double3.Zero||roots[0].Evaluated.AngularVelocityInParent!=Double3.Zero||!roots[0].Evaluated.IsInertial) throw new ArgumentException("Snapshot requires one identity inertial ECL root.");
        RootId=roots[0].Definition.Id;
        foreach(var item in input.Values) if(item.Definition.ParentId is { } parent&&!input.ContainsKey(parent)) throw new ArgumentException("Frame parent is missing.");
        _entries=new Dictionary<ReferenceFrameId,Entry>(input.Count);
        foreach(var id in input.Keys) ResolveEntry(id,input,new HashSet<ReferenceFrameId>());
        _readOnlyEntries=new ReadOnlyDictionary<ReferenceFrameId,Entry>(_entries);
    }
    private Entry ResolveEntry(ReferenceFrameId id,Dictionary<ReferenceFrameId,(ReferenceFrameDefinition Definition,EvaluatedReferenceFrame Evaluated)> input,HashSet<ReferenceFrameId> path)
    {
        if(_entries.TryGetValue(id,out var cached))return cached;
        if(!path.Add(id))throw new ArgumentException("Reference-frame cycle detected.");
        var current=input[id]; Entry result;
        if(current.Definition.ParentId is null) result=new(current.Definition,current.Evaluated,FrameTransform.Identity,Double3.Zero,Double3.Zero,current.Evaluated.IsInertial);
        else {var parent=ResolveEntry(current.Definition.ParentId.Value,input,path);var transform=ReferenceFrameMath.ComposeLocalToRoot(parent.LocalToRoot,current.Evaluated.LocalToParent);var originVelocity=ReferenceFrameMath.ComposeOriginVelocityInRoot(parent.LocalToRoot,parent.OriginVelocityInRoot,parent.AngularVelocityInRoot,current.Evaluated.LocalToParent,current.Evaluated.OriginVelocityInParent);var omega=ReferenceFrameMath.ComposeAngularVelocityInRoot(parent.LocalToRoot,parent.AngularVelocityInRoot,current.Evaluated.AngularVelocityInParent);result=new(current.Definition,current.Evaluated,transform,originVelocity,omega,parent.IsInertial&&current.Evaluated.IsInertial);}
        path.Remove(id);_entries.Add(id,result);return result;
    }
    internal bool TryGet(ReferenceFrameId id,out Entry entry)=>_entries.TryGetValue(id,out entry);
}
