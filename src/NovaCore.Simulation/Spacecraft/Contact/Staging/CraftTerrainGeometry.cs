using System.Numerics;
using BepuPhysics.Collidables;
using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Simulation.Spacecraft.Assemblies;

namespace NovaCore.Simulation.Spacecraft.Contact.Staging;

/// <summary>Finite, watertight physics geometry from the acquired full-H owner.
/// No rendering mesh, cached normal or altitude sample becomes authority.</summary>
internal static class CraftTerrainGeometry
{
    internal const double TileSize=4;
    private const int Units=512,FinestEdge=2;
    private const double Quantum=TileSize/Units;
    internal readonly record struct Tile(int X,int Y):IComparable<Tile>
    {public int CompareTo(Tile b){var c=X.CompareTo(b.X);return c==0?Y.CompareTo(b.Y):c;}}
    private readonly record struct Node(int X,int Y);
    private readonly record struct Cell(int X,int Y,int Size);
    internal sealed record Prepared(Triangle[] Triangles,double MaximumError,double MaximumConversionError);
    internal static Prepared Prepare(AssemblyFloridaSite site,Double3 origin,Tile tile,double tolerance,Action<string,double,int>? measure=null)
    {
        if(ConstructionWorkProbe.Current is {} probe)probe.Tiles++;
        var started=measure is null?0:System.Diagnostics.Stopwatch.GetTimestamp();
        var source=site.CollisionSource;var region=FloridaFacilitySupport.Region;
        var frame=new PhysicalCollisionFrame(region.Up,region.East,region.North,region.RadiusMetres);
        var bx=checked(tile.X*Units);var by=checked(tile.Y*Units);
        var leaves=new List<Cell>();var maximumError=0d;var conversionError=0d;
        var proofs=new Dictionary<Cell,IPhysicalCollisionPatch>();
        PhysicalPatchCoordinate P(Node n)=>new(n.X*Quantum,n.Y*Quantum);
        void Divide(Cell cell,IPhysicalCollisionPatch? inherited=null,int inheritedLevels=0)
        {
            var a=new Node(cell.X,cell.Y);var b=new Node(cell.X+cell.Size,cell.Y);
            var c=new Node(cell.X+cell.Size,cell.Y+cell.Size);var d=new Node(cell.X,cell.Y+cell.Size);
            var center=new Node(cell.X+cell.Size/2,cell.Y+cell.Size/2);
            // Tighten the enclosure after one inherited subdivision. Carrying
            // a broad piecewise parent modulus to every descendant can create
            // many unnecessary triangles, despite remaining conservative.
            var proof=inheritedLevels>1?null:inherited;
            if(proof is null&&!(inherited?.TrySubpatch(P(a),P(c),out proof)??false))source.TryPreparePatch(frame,P(a),P(c),out proof);
            // The emitted representation is a centre fan, not two triangles
            // spanning the cell diagonal. Test that representation against
            // its existing final error budget. Every conforming subfan still
            // receives its own independent certificate below.
            bool Fan(Node u,Node v)=>proof is not null&&proof.TryTriangleError(P(center),P(u),P(v),out var error,tolerance/4)&&error<=tolerance/4;
            bool Certified()=>Fan(a,b)&&Fan(b,c)&&Fan(c,d)&&Fan(d,a);
            // Derivative enclosures on a parent rectangle also enclose every
            // child. Reuse them while shrinking the triangle diameter; exact H
            // samples and the same error limits remain authoritative.
            if(Certified())
            {leaves.Add(cell);proofs.Add(cell,proof!);return;}
            if(cell.Size<=FinestEdge)
            {
                if(inherited is not null&&inherited.TrySubpatch(P(a),P(c),out proof)&&Certified()){leaves.Add(cell);proofs.Add(cell,proof!);return;}
                throw new InvalidDataException($"Terrain triangle error requires finer unqualified collision geometry at ({P(a).X:R},{P(a).Y:R}), edge={cell.Size*Quantum:R}.");
            }
            var nextLevel=ReferenceEquals(proof,inherited)?inheritedLevels+1:1;
            var h=cell.Size/2;Divide(new(cell.X,cell.Y,h),proof,nextLevel);Divide(new(cell.X+h,cell.Y,h),proof,nextLevel);
            Divide(new(cell.X,cell.Y+h,h),proof,nextLevel);Divide(new(cell.X+h,cell.Y+h,h),proof,nextLevel);
        }
        Divide(new(bx,by,Units));
        measure?.Invoke("divide",System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds,leaves.Count);
        var divided=measure is null?0:System.Diagnostics.Stopwatch.GetTimestamp();
        var horizontal=new Dictionary<int,SortedSet<int>>();var vertical=new Dictionary<int,SortedSet<int>>();
        void Vertex(int x,int y)
        {
            if(!horizontal.TryGetValue(y,out var row))horizontal.Add(y,row=new());row.Add(x);
            if(!vertical.TryGetValue(x,out var column))vertical.Add(x,column=new());column.Add(y);
        }
        foreach(var c in leaves){Vertex(c.X,c.Y);Vertex(c.X+c.Size,c.Y);Vertex(c.X,c.Y+c.Size);Vertex(c.X+c.Size,c.Y+c.Size);}
        // Fixed tile-edge lattice makes independently prepared neighbors share
        // exactly the same H samples and FP32 vertices, even at different LODs.
        for(var k=0;k<=Units;k+=FinestEdge){Vertex(bx+k,by);Vertex(bx+k,by+Units);Vertex(bx,by+k);Vertex(bx+Units,by+k);}
        var points=new Dictionary<Node,Vector3>();
        Vector3 Position(Node n,IPhysicalCollisionPatch proof)
        {
            if(points.TryGetValue(n,out var position))return position;
            var exact=site.LocalToBodyFixed.Conjugate().Rotate(proof.CollisionPoint(P(n))-site.OriginBodyFixed)-origin;
            position=new((float)exact.X,(float)exact.Y,(float)exact.Z);
            var error=Math.Sqrt((exact-new Double3(position.X,position.Y,position.Z)).LengthSquared);
            if(!double.IsFinite(error)||error>tolerance/8)throw new InvalidDataException("Terrain native vertex conversion exceeds contact precision.");
            conversionError=Math.Max(conversionError,error);points.Add(n,position);return position;
        }
        var triangles=new List<Triangle>();
        foreach(var c in leaves)
        {
            var boundary=new List<Node>();
            foreach(var x in horizontal[c.Y].GetViewBetween(c.X,c.X+c.Size))if(x<c.X+c.Size)boundary.Add(new(x,c.Y));
            foreach(var y in vertical[c.X+c.Size].GetViewBetween(c.Y,c.Y+c.Size))if(y<c.Y+c.Size)boundary.Add(new(c.X+c.Size,y));
            foreach(var x in horizontal[c.Y+c.Size].GetViewBetween(c.X,c.X+c.Size).Reverse())if(x>c.X)boundary.Add(new(x,c.Y+c.Size));
            foreach(var y in vertical[c.X].GetViewBetween(c.Y,c.Y+c.Size).Reverse())if(y>c.Y)boundary.Add(new(c.X,y));
            var center=new Node(c.X+c.Size/2,c.Y+c.Size/2);
            for(var i=0;i<boundary.Count;i++)
            {
                var a=boundary[i];var b=boundary[(i+1)%boundary.Count];
                if(!proofs[c].TryTriangleError(P(center),P(a),P(b),out var error)||error>tolerance/4)
                    throw new InvalidDataException("Conforming terrain triangle error refused.");
                maximumError=Math.Max(maximumError,error);
                // BEPU triangle normal is cross(C-A,B-A), opposite the usual
                // right-hand vertex order. East x North is outward Up.
                var triangle=new Triangle(Position(center,proofs[c]),Position(b,proofs[c]),Position(a,proofs[c]));
                var normal=Vector3.Normalize(Vector3.Cross(triangle.C-triangle.A,triangle.B-triangle.A));
                var up=site.LocalToBodyFixed.Conjugate().Rotate(frame.Radial);
                if(!float.IsFinite(normal.LengthSquared())||!CraftTerrainContacts.AcceptsRecovery(normal,new((float)up.X,(float)up.Y,(float)up.Z)))
                    throw new InvalidDataException("Terrain face leaves the qualified patch recovery cone.");
                triangles.Add(triangle);
            }
        }
        if(triangles.Count==0||triangles.Count>4*256*256)throw new InvalidDataException("Finite terrain topology capacity exceeded.");
        measure?.Invoke("triangulate",System.Diagnostics.Stopwatch.GetElapsedTime(divided).TotalMilliseconds,points.Count);
        return new(triangles.ToArray(),maximumError,conversionError);
    }
}
