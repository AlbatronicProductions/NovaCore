using NovaCore.Core;

namespace NovaCore.Graphics;

/// <summary>Presentation-only neutral-pose triangle picking in a part's material
/// frame. This selects authored parts; it cannot create sockets or physics.</summary>
public static class PartVisualPicking
{
    /// <summary>One neutral-pose mesh per part for the cold editor. Constant PBR
    /// values remain per vertex; source assets and articulated flight meshes stay
    /// immutable. This bounds 1024 admitted parts to 1024 rendered objects.</summary>
    public static PartVisualAsset NeutralComposite(PartVisualAsset asset)
    {
        var vertices=new List<NovaCore.Interop.NativeVisualVertex>();var indices=new List<uint>();
        foreach(var mesh in asset.Meshes){
            var first=(uint)vertices.Count;
            foreach(var vertex in mesh.Vertices){var v=vertex;if(mesh.Gimballed){v.X+=(float)asset.GimbalPivot.X;v.Y+=(float)asset.GimbalPivot.Y;v.Z+=(float)asset.GimbalPivot.Z;}vertices.Add(v);}
            indices.AddRange(mesh.Indices.Select(i=>checked(i+first)));
        }
        return asset with {Meshes=[new PartVisualMesh("editor-neutral",false,vertices.ToArray(),indices.ToArray())]};
    }
    public static double? Intersect(PartVisualAsset asset,Double3 origin,Double3 direction)
    {
        if(!origin.IsFinite||!direction.IsFinite||Math.Abs(direction.LengthSquared-1)>1e-8)
            throw new ArgumentException("Finite unit picking ray required.");
        var nearest=double.PositiveInfinity;
        foreach(var mesh in asset.Meshes)
        {
            var start=origin-(mesh.Gimballed?asset.GimbalPivot:Double3.Zero);
            var vertices=mesh.Vertices;var indices=mesh.Indices;
            Double3 V(uint i)=>new(vertices[i].X,vertices[i].Y,vertices[i].Z);
            for(var i=0;i<indices.Length;i+=3)
            {
                var a=V(indices[i]);var b=V(indices[i+1]);var c=V(indices[i+2]);
                var e1=b-a;var e2=c-a;var p=Double3.Cross(direction,e2);var det=Double3.Dot(e1,p);
                // Both faces selectable: interior camera positions are a UI case.
                if(Math.Abs(det)<1e-14)continue;
                var relative=start-a;var u=Double3.Dot(relative,p)/det;if(u<0||u>1)continue;
                var q=Double3.Cross(relative,e1);var v=Double3.Dot(direction,q)/det;if(v<0||u+v>1)continue;
                var distance=Double3.Dot(e2,q)/det;if(distance>=0&&distance<nearest)nearest=distance;
            }
        }
        return double.IsFinite(nearest)?nearest:null;
    }
}
