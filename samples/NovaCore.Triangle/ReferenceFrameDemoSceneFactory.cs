using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Graphics;

internal static class ReferenceFrameDemoSceneFactory
{
    public static SampleRenderableState[] Create(UniversePosition root)
    {
        var ecl=new ReferenceFrameDefinition(root.Frame,null,ReferenceFrameKind.Ecl,"ECL");
        var orbId=new ReferenceFrameId(2); var cceId=new ReferenceFrameId(3); var cciId=new ReferenceFrameId(4); var ccfId=new ReferenceFrameId(5);
        var snapshot=new ReferenceFrameSnapshot([
            (ecl,CelestialFrameFactory.RootEcl()),
            (new ReferenceFrameDefinition(orbId,root.Frame,ReferenceFrameKind.Orb,"ORB"),CelestialFrameFactory.Orb(root.Value+new Double3(-.35,.25,0),Double3.Zero,new OrbitalFrameGeometry(0,0,0))),
            (new ReferenceFrameDefinition(cceId,root.Frame,ReferenceFrameKind.Cce,"CCE"),CelestialFrameFactory.Cce(root.Value+new Double3(.35,0,0),Double3.Zero)),
            (new ReferenceFrameDefinition(cciId,cceId,ReferenceFrameKind.Cci,"CCI"),CelestialFrameFactory.Cci(Double3.Zero,Double3.Zero,Double3.UnitZ,Double3.UnitX)),
            (new ReferenceFrameDefinition(ccfId,cciId,ReferenceFrameKind.Ccf,"CCF"),CelestialFrameFactory.Ccf(Math.PI/4d,.1d))]);
        var resolver=new ReferenceFrameResolver(snapshot);
        var local=new[]{new FramePosition(root.Frame,Double3.Zero),new FramePosition(orbId,new Double3(.1,0,0)),new FramePosition(cceId,Double3.Zero),new FramePosition(cciId,new Double3(0,.1,0)),new FramePosition(ccfId,new Double3(.1,0,0))};
        var result=new SampleRenderableState[local.Length];
        for(var i=0;i<result.Length;i++){resolver.TryResolvePosition(local[i],out var pos);result[i]=new(pos,DoubleQuaternion.Identity,new Double3(1,1,1),MeshHandle.Triangle);}
        return result;
    }
}
