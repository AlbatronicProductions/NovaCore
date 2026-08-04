using NovaCore.Core;
using NovaCore.Graphics;

internal readonly record struct SampleRenderableState(UniversePosition Position, DoubleQuaternion Rotation, Double3 Scale, MeshHandle Mesh);
internal static class SampleSceneFactory
{
    public static SampleRenderableState[] Create(int count, UniversePosition center)
    {
        var result=new SampleRenderableState[count]; var side=(int)Math.Ceiling(Math.Sqrt(count));
        const double spacing=.075d, verticalFov=Math.PI/3d, aspect=16d/9d, margin=1d;
        var half=(side-1)*spacing*.5d;var required=Math.Max(half/Math.Tan(verticalFov*.5d),half/(Math.Tan(verticalFov*.5d)*aspect))+margin;
        var fieldCenter=center.Value+new Double3(0,0,-Math.Max(1d,required));
        for(var i=0;i<count;i++){var x=(i%side-(side-1)/2d)*spacing;var y=(i/side-(side-1)/2d)*spacing;var angle=i*.17d;result[i]=new(new UniversePosition(fieldCenter+new Double3(x,y,0),center.Frame),new DoubleQuaternion(0,0,Math.Sin(angle/2),Math.Cos(angle/2)),new Double3(.8+(i%3)*.1,.8+(i%5)*.05,1),MeshHandle.Triangle);}
        return result;
    }
}
