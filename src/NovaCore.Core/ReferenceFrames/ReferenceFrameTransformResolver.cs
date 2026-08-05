namespace NovaCore.Core.ReferenceFrames;

internal enum ReferenceFrameTransformResolutionStatus : byte
{
    Success = 0,
    SourceUnknown,
    TargetUnknown,
    UnrelatedFrames,
    SourcePathBufferTooSmall,
    TargetPathBufferTooSmall,
    TraversalPathBufferTooSmall,
}

/// <summary>Immutable source-frame pose and kinematics expressed in target-frame coordinates.</summary>
internal readonly record struct ResolvedReferenceFrameTransform(
    FrameTransform SourceToTarget,
    Double3 SourceOriginVelocityInTarget,
    Double3 SourceAngularVelocityInTarget)
{
    public static ResolvedReferenceFrameTransform Identity => new(FrameTransform.Identity, Double3.Zero, Double3.Zero);
    public Double3 ConvertPosition(in Double3 sourcePosition) => SourceToTarget.LocalToParent(sourcePosition);
    public Double3 ConvertDirection(in Double3 sourceDirection) => SourceToTarget.LocalDirectionToParent(sourceDirection);
    public DoubleQuaternion ConvertOrientation(in DoubleQuaternion sourceOrientation) => (SourceToTarget.Rotation * sourceOrientation).Normalized();
    public Double3 ConvertVelocity(in Double3 sourcePosition, in Double3 sourceVelocity) =>
        ReferenceFrameMath.TransformVelocity(SourceToTarget, SourceOriginVelocityInTarget, SourceAngularVelocityInTarget, sourcePosition, sourceVelocity);
}

/// <summary>Allocation-free, read-only resolution through immutable topology and evaluated local transforms.</summary>
internal static class ReferenceFrameTransformResolver
{
    public static ReferenceFrameTransformResolutionStatus TryResolveTransform(
        ReferenceFrameTransformSet transforms,
        ReferenceFrameId source,
        ReferenceFrameId target,
        Span<ReferenceFrameId> sourceRootPathBuffer,
        Span<ReferenceFrameId> targetRootPathBuffer,
        Span<ReferenceFrameId> traversalPathBuffer,
        out ResolvedReferenceFrameTransform result)
    {
        result = default;
        var graph = transforms.Graph;
        if (!graph.TryGetIndex(source, out _)) return ReferenceFrameTransformResolutionStatus.SourceUnknown;
        if (!graph.TryGetIndex(target, out _)) return ReferenceFrameTransformResolutionStatus.TargetUnknown;
        if (!ReferenceFramePathQuery.TryFindLowestCommonAncestor(graph, source, target, out _)) return ReferenceFrameTransformResolutionStatus.UnrelatedFrames;

        var traversal = ReferenceFramePathQuery.TryBuildTraversalPath(graph, source, target, traversalPathBuffer);
        if (!traversal.Succeeded) return traversal.Status == ReferenceFramePathStatus.DestinationBufferTooSmall
            ? ReferenceFrameTransformResolutionStatus.TraversalPathBufferTooSmall
            : ReferenceFrameTransformResolutionStatus.UnrelatedFrames;

        var sourcePath = ReferenceFramePathQuery.TryBuildRootToNodePath(graph, source, sourceRootPathBuffer);
        if (!sourcePath.Succeeded) return ReferenceFrameTransformResolutionStatus.SourcePathBufferTooSmall;
        var targetPath = ReferenceFramePathQuery.TryBuildRootToNodePath(graph, target, targetRootPathBuffer);
        if (!targetPath.Succeeded) return ReferenceFrameTransformResolutionStatus.TargetPathBufferTooSmall;

        var sourceRoot = ResolveToRoot(transforms, ReferenceFramePathQuery.EnumerateTransformPath(sourceRootPathBuffer, sourcePath));
        var targetRoot = ResolveToRoot(transforms, ReferenceFramePathQuery.EnumerateTransformPath(targetRootPathBuffer, targetPath));
        var targetRootToLocal = targetRoot.LocalToRoot.Inverse();
        var sourceToTarget = ReferenceFrameMath.ComposeLocalToRoot(targetRootToLocal, sourceRoot.LocalToRoot);
        var originVelocity = ReferenceFrameMath.ConvertRootVelocityToLocal(targetRoot.LocalToRoot, targetRoot.OriginVelocityInRoot, targetRoot.AngularVelocityInRoot, sourceRoot.LocalToRoot.Translation, sourceRoot.OriginVelocityInRoot);
        var angularVelocity = ReferenceFrameMath.ConvertRootDirectionToLocal(targetRoot.LocalToRoot, sourceRoot.AngularVelocityInRoot - targetRoot.AngularVelocityInRoot);
        result = new ResolvedReferenceFrameTransform(sourceToTarget, originVelocity, angularVelocity);
        return ReferenceFrameTransformResolutionStatus.Success;
    }

    private static RootKinematics ResolveToRoot(ReferenceFrameTransformSet transforms, ReadOnlySpan<ReferenceFrameId> rootPath)
    {
        var localToRoot = FrameTransform.Identity;
        var originVelocityInRoot = Double3.Zero;
        var angularVelocityInRoot = Double3.Zero;
        foreach (ref readonly var frame in rootPath)
        {
            transforms.Graph.TryGetIndex(frame, out var index);
            var evaluated = transforms.GetAt(index);
            originVelocityInRoot = ReferenceFrameMath.ComposeOriginVelocityInRoot(localToRoot, originVelocityInRoot, angularVelocityInRoot, evaluated.LocalToParent, evaluated.OriginVelocityInParent);
            angularVelocityInRoot = ReferenceFrameMath.ComposeAngularVelocityInRoot(localToRoot, angularVelocityInRoot, evaluated.AngularVelocityInParent);
            localToRoot = ReferenceFrameMath.ComposeLocalToRoot(localToRoot, evaluated.LocalToParent);
        }
        return new RootKinematics(localToRoot, originVelocityInRoot, angularVelocityInRoot);
    }

    private readonly record struct RootKinematics(FrameTransform LocalToRoot, Double3 OriginVelocityInRoot, Double3 AngularVelocityInRoot);
}
