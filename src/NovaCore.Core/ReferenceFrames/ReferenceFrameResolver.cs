namespace NovaCore.Core.ReferenceFrames;

public sealed class ReferenceFrameResolver(ReferenceFrameSnapshot snapshot)
{
    public bool TryResolvePosition(in FramePosition source,out UniversePosition root)
    {
        if(!snapshot.TryGet(source.Frame,out var frame)){root=default;return false;} root=new UniversePosition(frame.LocalToRoot.LocalToParent(source.Value),snapshot.RootId);return true;
    }
    public bool TryConvertPosition(in FramePosition source,ReferenceFrameId target,out FramePosition result)
    {
        if(!snapshot.TryGet(source.Frame,out var from)||!snapshot.TryGet(target,out var to)){result=default;return false;}result=new FramePosition(target,to.LocalToRoot.ParentToLocal(from.LocalToRoot.LocalToParent(source.Value)));return true;
    }
    public bool TryConvertDirection(ReferenceFrameId source,ReferenceFrameId target,in Double3 direction,out Double3 result)
    {if(!snapshot.TryGet(source,out var from)||!snapshot.TryGet(target,out var to)){result=default;return false;}result=to.LocalToRoot.ParentDirectionToLocal(from.LocalToRoot.LocalDirectionToParent(direction));return true;}
    public bool TryConvertOrientation(ReferenceFrameId source,ReferenceFrameId target,in DoubleQuaternion orientation,out DoubleQuaternion result)
    {if(!snapshot.TryGet(source,out var from)||!snapshot.TryGet(target,out var to)){result=default;return false;}result=(to.LocalToRoot.Rotation.Conjugate()*from.LocalToRoot.Rotation*orientation).Normalized();return true;}
    public bool TryConvertVelocity(in FramePosition position,in FrameVelocity velocity,ReferenceFrameId target,out FrameVelocity result)
    {
        if(position.Frame!=velocity.Frame||!snapshot.TryGet(position.Frame,out var from)||!snapshot.TryGet(target,out var to)){result=default;return false;}
        var offset=from.LocalToRoot.Rotation.Rotate(position.Value);var rootVelocity=from.OriginVelocityInRoot+from.LocalToRoot.Rotation.Rotate(velocity.Value)+Double3.Cross(from.AngularVelocityInRoot,offset);
        var rootPosition=from.LocalToRoot.LocalToParent(position.Value);var targetOffset=rootPosition-to.LocalToRoot.Translation;var local=to.LocalToRoot.ParentDirectionToLocal(rootVelocity-to.OriginVelocityInRoot-Double3.Cross(to.AngularVelocityInRoot,targetOffset));result=new FrameVelocity(target,local);return true;
    }
}
