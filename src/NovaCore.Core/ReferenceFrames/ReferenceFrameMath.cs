namespace NovaCore.Core.ReferenceFrames;

/// <summary>
/// Internal, allocation-free rigid-frame equations shared by immutable snapshot construction and
/// contextual conversion. This type has no clock, timeline, renderer, or authoritative-state ownership.
/// </summary>
internal static class ReferenceFrameMath
{
    public static FrameTransform ComposeLocalToRoot(in FrameTransform parentLocalToRoot, in FrameTransform localToParent) =>
        FrameTransform.Compose(parentLocalToRoot, localToParent);

    public static Double3 ResolvePositionToRoot(in FrameTransform localToRoot, in Double3 localPosition) =>
        localToRoot.LocalToParent(localPosition);

    public static Double3 ResolveDirectionToRoot(in FrameTransform localToRoot, in Double3 localDirection) =>
        localToRoot.LocalDirectionToParent(localDirection);

    public static Double3 ConvertRootPositionToLocal(in FrameTransform localToRoot, in Double3 rootPosition) =>
        localToRoot.ParentToLocal(rootPosition);

    public static Double3 ConvertRootDirectionToLocal(in FrameTransform localToRoot, in Double3 rootDirection) =>
        localToRoot.ParentDirectionToLocal(rootDirection);

    public static DoubleQuaternion ConvertOrientation(
        in DoubleQuaternion sourceLocalToRoot,
        in DoubleQuaternion targetLocalToRoot,
        in DoubleQuaternion sourceLocalOrientation) =>
        (targetLocalToRoot.Conjugate() * sourceLocalToRoot * sourceLocalOrientation).Normalized();

    public static Double3 ComposeOriginVelocityInRoot(
        in FrameTransform parentLocalToRoot,
        in Double3 parentOriginVelocityInRoot,
        in Double3 parentAngularVelocityInRoot,
        in FrameTransform localToParent,
        in Double3 originVelocityInParent)
    {
        var offset = parentLocalToRoot.Rotation.Rotate(localToParent.Translation);
        return parentOriginVelocityInRoot
            + parentLocalToRoot.Rotation.Rotate(originVelocityInParent)
            + Double3.Cross(parentAngularVelocityInRoot, offset);
    }

    public static Double3 ComposeAngularVelocityInRoot(
        in FrameTransform parentLocalToRoot,
        in Double3 parentAngularVelocityInRoot,
        in Double3 angularVelocityInParent) =>
        parentAngularVelocityInRoot + parentLocalToRoot.Rotation.Rotate(angularVelocityInParent);

    public static Double3 TransformVelocity(
        in FrameTransform localToParent,
        in Double3 originVelocityInParent,
        in Double3 angularVelocityInParent,
        in Double3 localPosition,
        in Double3 localVelocity)
    {
        var offset = localToParent.Rotation.Rotate(localPosition);
        return originVelocityInParent
            + localToParent.Rotation.Rotate(localVelocity)
            + Double3.Cross(angularVelocityInParent, offset);
    }

    public static Double3 ResolveVelocityToRoot(in FrameTransform localToRoot, in Double3 originVelocityInRoot, in Double3 angularVelocityInRoot, in Double3 localPosition, in Double3 localVelocity) =>
        TransformVelocity(localToRoot, originVelocityInRoot, angularVelocityInRoot, localPosition, localVelocity);

    public static Double3 ConvertRootVelocityToLocal(
        in FrameTransform targetLocalToRoot,
        in Double3 targetOriginVelocityInRoot,
        in Double3 targetAngularVelocityInRoot,
        in Double3 rootPosition,
        in Double3 rootVelocity)
    {
        var targetOffset = rootPosition - targetLocalToRoot.Translation;
        return targetLocalToRoot.ParentDirectionToLocal(
            rootVelocity
            - targetOriginVelocityInRoot
            - Double3.Cross(targetAngularVelocityInRoot, targetOffset));
    }
}
