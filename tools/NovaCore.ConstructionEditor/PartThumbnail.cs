using System.Drawing.Drawing2D;
using NovaCore.Core;
using NovaCore.Graphics;

namespace NovaCore.ConstructionEditor;
// Cold, bounded catalog rasterization of NovaCore-owned qualified geometry.
// It is an image cache, not a scene/physics renderer or an attachment oracle.
internal static class PartThumbnail
{
    internal static Bitmap Draw(PartVisualAsset asset)
    {
        var faces=new List<(PointF[] Points,float Depth,Color Color)>();
        var right=new Double3(0,.8,-.6);var up=new Double3(.94,-.204,-.272);var forward=Double3.Cross(right,up);
        var points=new List<Double3>();
        foreach(var mesh in asset.Meshes)foreach(var v in mesh.VertexData)points.Add(new Double3(v.X,v.Y,v.Z)+(mesh.Gimballed?asset.GimbalPivot:Double3.Zero));
        var minX=points.Min(p=>Double3.Dot(p,right));var maxX=points.Max(p=>Double3.Dot(p,right));var minY=points.Min(p=>Double3.Dot(p,up));var maxY=points.Max(p=>Double3.Dot(p,up));
        var scale=76/Math.Max(maxX-minX,maxY-minY);var cx=(minX+maxX)/2;var cy=(minY+maxY)/2;
        foreach(var mesh in asset.Meshes){var vertices=mesh.VertexData;var indices=mesh.IndexData;
            for(var i=0;i<indices.Length;i+=3){var triangle=new PointF[3];float depth=0;var light=.4;
                for(var j=0;j<3;j++){var v=vertices[(int)indices[i+j]];var p=new Double3(v.X,v.Y,v.Z)+(mesh.Gimballed?asset.GimbalPivot:Double3.Zero);triangle[j]=new((float)(44+(Double3.Dot(p,right)-cx)*scale),(float)(44-(Double3.Dot(p,up)-cy)*scale));depth+=(float)Double3.Dot(p,forward);light=Math.Clamp(.4+.6*Math.Abs(v.Nx*.3+v.Ny*.6+v.Nz*.74),.2,1);}
                var value=(int)(205*light);faces.Add((triangle,depth,Color.FromArgb(value,Math.Min(255,value+15),Math.Min(255,value+25))));
            }
        }
        var bitmap=new Bitmap(88,88);using var graphics=System.Drawing.Graphics.FromImage(bitmap);graphics.SmoothingMode=SmoothingMode.AntiAlias;graphics.Clear(Color.FromArgb(37,46,59));
        foreach(var face in faces.OrderBy(x=>x.Depth)){using var brush=new SolidBrush(face.Color);graphics.FillPolygon(brush,face.Points);}
        return bitmap;
    }
}
