using NovaCore.Core;
using NovaCore.Graphics;

internal readonly record struct SampleRenderableState(UniversePosition Position, DoubleQuaternion Rotation, Double3 Scale, MeshHandle Mesh);
internal static class SampleSceneFactory
{
    public static SampleRenderableState[] Create(int count, UniversePosition center)
    {
        var result=new SampleRenderableState[count]; var side=(int)Math.Ceiling(Math.Sqrt(count));
        for(var i=0;i<count;i++){var x=(i%side-(side-1)/2d)*.075d;var y=(i/side-(side-1)/2d)*.075d;var angle=i*.17d;result[i]=new(new UniversePosition(center.Value+new Double3(x,y,0),center.Frame),new DoubleQuaternion(0,0,Math.Sin(angle/2),Math.Cos(angle/2)),new Double3(.8+(i%3)*.1,.8+(i%5)*.05,1),MeshHandle.Triangle);}
        return result;
    }
}
