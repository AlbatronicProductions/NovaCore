using System.Numerics;
using BepuPhysics;
using BepuUtilities;

// A value owned by exactly one accepted cache generation. No references to refresh scratch.
internal readonly record struct ContactGeometry(Features Features,D3 Normal,D3 Tangent0,D3 Tangent1,
    D3 Lever0,D3 Lever1,D3 Lever2,D3 Lever3,double Depth0,double Depth1,double Depth2,double Depth3,
    double Omega,double Friction,double Recovery,double TwiceDamping)
{
    internal D3 Lever(int i)=>i switch{0=>Lever0,1=>Lever1,2=>Lever2,3=>Lever3,_=>throw new ArgumentOutOfRangeException(nameof(i))};
    internal double Depth(int i)=>i switch{0=>Depth0,1=>Depth1,2=>Depth2,3=>Depth3,_=>throw new ArgumentOutOfRangeException(nameof(i))};
    internal D3 Center=>.25*(Lever0+Lever1+Lever2+Lever3);
    internal D3 Linear(int i)=>i<4?Normal:i==4?Tangent0:i==5?Tangent1:default;
    internal D3 Angular(int i)=>i<4?D3.Cross(Lever(i),Normal):i==6?Normal:D3.Cross(Center,Linear(i));
    internal double Radius(int i)=>(Lever(i)-Center).Length;
    internal bool Valid
    {
        get
        {
            if(!Features.Unique||Omega<=0||!double.IsFinite(Omega)||Friction!=.5||Recovery!=2||TwiceDamping!=2)return false;
            Span<D3> axes=stackalloc D3[]{Normal,Tangent0,Tangent1};
            for(int i=0;i<3;i++)
            {
                if(!axes[i].Finite||Math.Abs(D3.Dot(axes[i],axes[i])-1)>8*Math.ScaleB(1d,-23))return false;
                for(int j=0;j<i;j++)if(Math.Abs(D3.Dot(axes[i],axes[j]))>8*Math.ScaleB(1d,-23))return false;
            }
            if(D3.Dot(Normal,D3.Cross(Tangent0,Tangent1))>=0)return false;
            for(int i=0;i<4;i++)if(!Lever(i).Finite||!double.IsFinite(Depth(i))||Depth(i)<=0||Depth(i)>.020)return false;
            return true;
        }
    }
    internal static D3 From(Vector3 v)=>new(v.X,v.Y,v.Z);
    internal static ContactGeometry From(Capture c)
    {
        if(c.Count!=4)throw new ArgumentException("Four current convex rows required");
        for(int i=1;i<4;i++)if(c.Contacts[i].Normal!=c.Contacts[0].Normal)throw new ArgumentException("Shared normal required");
        Helpers.BuildOrthonormalBasis(c.Contacts[0].Normal,out var t0,out var t1);
        return new(new(c.Features[0],c.Features[1],c.Features[2],c.Features[3]),From(c.Contacts[0].Normal),From(t0),From(t1),
            From(c.Contacts[0].Offset),From(c.Contacts[1].Offset),From(c.Contacts[2].Offset),From(c.Contacts[3].Offset),
            c.Contacts[0].Depth,c.Contacts[1].Depth,c.Contacts[2].Depth,c.Contacts[3].Depth,c.Omega,c.Friction,c.Recovery,c.TwiceDamping);
    }
}

// Cold fixed buffers, current full inverse inertia. No per-piece arrays or historical inference.
internal sealed class CurrentPatch
{
    internal ContactGeometry Geometry;
    internal InverseBody Body;
    internal readonly D3[] Linear=new D3[7],Angular=new D3[7];
    internal readonly double[,] K=new double[7,7];
    internal void Set(in ContactGeometry geometry,in InverseBody body)
    {
        Geometry=geometry;Body=body;
        for(int i=0;i<7;i++){Linear[i]=geometry.Linear(i);Angular[i]=geometry.Angular(i);}
        for(int i=0;i<7;i++)for(int j=0;j<7;j++)
            K[i,j]=body.Mass*D3.Dot(Linear[i],Linear[j])+D3.Dot(Angular[i],body.Apply(Angular[j]));
    }
    internal double J(int i,in PieceKernel.State s)=>D3.Dot(Linear[i],new(s.Linear.X,s.Linear.Y,s.Linear.Z))+
        D3.Dot(Angular[i],new(s.Angular.X,s.Angular.Y,s.Angular.Z));
}
