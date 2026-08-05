namespace NovaCore.Core.ReferenceFrames;

/// <summary>Controlled outcomes for read-only, caller-buffered reference-frame path queries.</summary>
internal enum ReferenceFramePathStatus : byte
{
    Success = 0,
    SourceUnknown,
    TargetUnknown,
    UnrelatedFrames,
    DestinationBufferTooSmall,
}

/// <summary>Metadata for a caller-owned traversal buffer; no transform data is evaluated or stored.</summary>
internal readonly record struct ReferenceFramePathResult(
    ReferenceFramePathStatus Status,
    ReferenceFrameId LowestCommonAncestor,
    int NodeCount,
    int UpwardNodeCount)
{
    public bool Succeeded => Status == ReferenceFramePathStatus.Success;
}

/// <summary>
/// Allocation-free read-only graph queries. Paths are written into caller-provided spans in traversal order.
/// This layer owns neither topology nor evaluated transforms.
/// </summary>
internal static class ReferenceFramePathQuery
{
    public static bool TryFindLowestCommonAncestor(
        ReferenceFrameGraph graph,
        ReferenceFrameId source,
        ReferenceFrameId target,
        out ReferenceFrameId lowestCommonAncestor)
    {
        lowestCommonAncestor = default;
        if (!graph.TryGetIndex(source, out var sourceIndex) || !graph.TryGetIndex(target, out var targetIndex)) return false;
        var common = FindLowestCommonAncestorIndex(graph, sourceIndex, targetIndex);
        if (common < 0) return false;
        lowestCommonAncestor = graph.GetNodeAt(common).Id;
        return true;
    }

    /// <summary>Writes an inclusive root-to-node path in root-first order.</summary>
    public static ReferenceFramePathResult TryBuildRootToNodePath(
        ReferenceFrameGraph graph,
        ReferenceFrameId target,
        Span<ReferenceFrameId> destination)
    {
        if (!graph.TryGetIndex(target, out var targetIndex)) return new(ReferenceFramePathStatus.TargetUnknown, default, 0, 0);
        var required = checked(graph.GetDepthAt(targetIndex) + 1);
        if (destination.Length < required) return new(ReferenceFramePathStatus.DestinationBufferTooSmall, default, required, 0);

        var current = targetIndex;
        for (var offset = required - 1; offset >= 0; offset--)
        {
            destination[offset] = graph.GetNodeAt(current).Id;
            current = graph.GetParentIndexAt(current);
        }
        return new(ReferenceFramePathStatus.Success, destination[0], required, 0);
    }

    /// <summary>
    /// Writes an inclusive source-to-target path. The initial segment climbs from source through the LCA;
    /// the final segment descends from the LCA's child to target.
    /// </summary>
    public static ReferenceFramePathResult TryBuildTraversalPath(
        ReferenceFrameGraph graph,
        ReferenceFrameId source,
        ReferenceFrameId target,
        Span<ReferenceFrameId> destination)
    {
        if (!graph.TryGetIndex(source, out var sourceIndex)) return new(ReferenceFramePathStatus.SourceUnknown, default, 0, 0);
        if (!graph.TryGetIndex(target, out var targetIndex)) return new(ReferenceFramePathStatus.TargetUnknown, default, 0, 0);
        var lcaIndex = FindLowestCommonAncestorIndex(graph, sourceIndex, targetIndex);
        if (lcaIndex < 0) return new(ReferenceFramePathStatus.UnrelatedFrames, default, 0, 0);

        var upwardNodeCount = checked(graph.GetDepthAt(sourceIndex) - graph.GetDepthAt(lcaIndex) + 1);
        var nodeCount = checked(upwardNodeCount + graph.GetDepthAt(targetIndex) - graph.GetDepthAt(lcaIndex));
        if (destination.Length < nodeCount) return new(ReferenceFramePathStatus.DestinationBufferTooSmall, graph.GetNodeAt(lcaIndex).Id, nodeCount, upwardNodeCount);

        var current = sourceIndex;
        for (var offset = 0; offset < upwardNodeCount; offset++)
        {
            destination[offset] = graph.GetNodeAt(current).Id;
            current = graph.GetParentIndexAt(current);
        }
        current = targetIndex;
        for (var offset = nodeCount - 1; offset >= upwardNodeCount; offset--)
        {
            destination[offset] = graph.GetNodeAt(current).Id;
            current = graph.GetParentIndexAt(current);
        }
        return new(ReferenceFramePathStatus.Success, graph.GetNodeAt(lcaIndex).Id, nodeCount, upwardNodeCount);
    }

    /// <summary>Returns the populated caller buffer as a read-only transform-path sequence.</summary>
    public static ReadOnlySpan<ReferenceFrameId> EnumerateTransformPath(Span<ReferenceFrameId> destination, in ReferenceFramePathResult result) =>
        result.Succeeded ? destination[..result.NodeCount] : ReadOnlySpan<ReferenceFrameId>.Empty;

    private static int FindLowestCommonAncestorIndex(ReferenceFrameGraph graph, int source, int target)
    {
        var sourceDepth = graph.GetDepthAt(source);
        var targetDepth = graph.GetDepthAt(target);
        while (sourceDepth > targetDepth) { source = graph.GetParentIndexAt(source); sourceDepth--; }
        while (targetDepth > sourceDepth) { target = graph.GetParentIndexAt(target); targetDepth--; }
        while (source != target)
        {
            source = graph.GetParentIndexAt(source);
            target = graph.GetParentIndexAt(target);
            if (source < 0 || target < 0) return -1;
        }
        return source;
    }
}
