namespace NovaCore.Core.ReferenceFrames;

/// <summary>Rigid local-to-parent transform. Quaternion is XYZW Hamilton local-to-parent rotation.</summary>
public readonly record struct FrameTransform(Double3 Translation, DoubleQuaternion Rotation)
{
    public static FrameTransform Identity => new(Double3.Zero, DoubleQuaternion.Identity);
    public bool IsFinite => Translation.IsFinite && Rotation.IsFinite;
    public Double3 LocalToParent(in Double3 position) => Translation + Rotation.Rotate(position);
    public Double3 ParentToLocal(in Double3 position) => Rotation.Conjugate().Normalized().Rotate(position - Translation);
    public Double3 LocalDirectionToParent(in Double3 direction) => Rotation.Rotate(direction);
    public Double3 ParentDirectionToLocal(in Double3 direction) => Rotation.Conjugate().Normalized().Rotate(direction);
    public FrameTransform Inverse() { var r=Rotation.Conjugate().Normalized(); return new FrameTransform(r.Rotate(-Translation),r); }
    public static FrameTransform Compose(in FrameTransform parentToRoot, in FrameTransform localToParent) => new(parentToRoot.Translation + parentToRoot.Rotation.Rotate(localToParent.Translation), (parentToRoot.Rotation * localToParent.Rotation).Normalized());
}
