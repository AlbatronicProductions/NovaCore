namespace NovaCore.Core.ReferenceFrames;

public sealed class ReferenceFrameResolver(ReferenceFrameSnapshot snapshot)
{
    public ReferenceFrameId RootFrame => snapshot.RootId;

    public bool TryResolvePosition(in FramePosition source,out UniversePosition root)
    {
        if(!snapshot.TryGet(source.Frame,out var frame)){root=default;return false;} root=new UniversePosition(ReferenceFrameMath.ResolvePositionToRoot(frame.LocalToRoot,source.Value),snapshot.RootId);return true;
    }
    public bool TryConvertPosition(in FramePosition source,ReferenceFrameId target,out FramePosition result)
    {
        if(!snapshot.TryGet(source.Frame,out var from)||!snapshot.TryGet(target,out var to)){result=default;return false;}var rootPosition=ReferenceFrameMath.ResolvePositionToRoot(from.LocalToRoot,source.Value);result=new FramePosition(target,ReferenceFrameMath.ConvertRootPositionToLocal(to.LocalToRoot,rootPosition));return true;
    }
    public bool TryConvertDirection(ReferenceFrameId source,ReferenceFrameId target,in Double3 direction,out Double3 result)
    {if(!snapshot.TryGet(source,out var from)||!snapshot.TryGet(target,out var to)){result=default;return false;}var rootDirection=ReferenceFrameMath.ResolveDirectionToRoot(from.LocalToRoot,direction);result=ReferenceFrameMath.ConvertRootDirectionToLocal(to.LocalToRoot,rootDirection);return true;}
    public bool TryConvertOrientation(ReferenceFrameId source,ReferenceFrameId target,in DoubleQuaternion orientation,out DoubleQuaternion result)
    {if(!snapshot.TryGet(source,out var from)||!snapshot.TryGet(target,out var to)){result=default;return false;}result=ReferenceFrameMath.ConvertOrientation(from.LocalToRoot.Rotation,to.LocalToRoot.Rotation,orientation);return true;}
    public bool TryConvertVelocity(in FramePosition position,in FrameVelocity velocity,ReferenceFrameId target,out FrameVelocity result)
    {
        if(position.Frame!=velocity.Frame||!snapshot.TryGet(position.Frame,out var from)||!snapshot.TryGet(target,out var to)){result=default;return false;}
        var rootVelocity=ReferenceFrameMath.ResolveVelocityToRoot(from.LocalToRoot,from.OriginVelocityInRoot,from.AngularVelocityInRoot,position.Value,velocity.Value);
        var rootPosition=ReferenceFrameMath.ResolvePositionToRoot(from.LocalToRoot,position.Value);var local=ReferenceFrameMath.ConvertRootVelocityToLocal(to.LocalToRoot,to.OriginVelocityInRoot,to.AngularVelocityInRoot,rootPosition,rootVelocity);result=new FrameVelocity(target,local);return true;
    }
}
