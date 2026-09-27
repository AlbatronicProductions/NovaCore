using NovaCore.Core;

namespace NovaCore.Graphics;

// Directed-rounding interval arithmetic used only during finite collider
// preparation. No evaluation here replaces the production H sample path.
internal readonly record struct CollisionRange(double Low,double High)
{
    internal bool Finite=>double.IsFinite(Low)&&double.IsFinite(High)&&Low<=High;
    internal double Magnitude=>Math.Max(Math.Abs(Low),Math.Abs(High));
    internal double Width=>Math.BitIncrement(High-Low);
    internal static double UpperNorm(double a,double b,double c=0,double d=0)
    {
        var aa=Math.BitIncrement(a*a);var bb=Math.BitIncrement(b*b);
        var cc=Math.BitIncrement(c*c);var dd=Math.BitIncrement(d*d);
        return Math.BitIncrement(Math.Sqrt(Math.BitIncrement(Math.BitIncrement(aa+bb)+Math.BitIncrement(cc+dd))));
    }
    public static implicit operator CollisionRange(double x)=>new(x,x);
    internal static CollisionRange Enclose(double a,double b)=>new(Math.BitDecrement(a),Math.BitIncrement(b));
    internal CollisionRange Inflate(double x)=>Enclose(Low-x,High+x);
    internal CollisionRange Hull(CollisionRange b)=>new(Math.Min(Low,b.Low),Math.Max(High,b.High));
    internal CollisionRange Clip(double lo,double hi)=>new(Math.Max(lo,Low),Math.Min(hi,High));
    public static CollisionRange operator +(CollisionRange a,CollisionRange b)=>Enclose(a.Low+b.Low,a.High+b.High);
    public static CollisionRange operator -(CollisionRange a)=>new(-a.High,-a.Low);
    public static CollisionRange operator -(CollisionRange a,CollisionRange b)=>a+-b;
    public static CollisionRange operator *(CollisionRange a,CollisionRange b){var x=a.Low*b.Low;var y=a.Low*b.High;var z=a.High*b.Low;var w=a.High*b.High;return Enclose(Math.Min(Math.Min(x,y),Math.Min(z,w)),Math.Max(Math.Max(x,y),Math.Max(z,w)));}
    public static CollisionRange operator /(CollisionRange a,CollisionRange b){if(b.Low<=0&&b.High>=0)throw new InvalidDataException("Unresolved collision bound denominator.");return a*Enclose(1/b.High,1/b.Low);}
    internal CollisionRange Square()=>Enclose(Low<=0&&High>=0?0:Math.Min(Low*Low,High*High),Math.Max(Low*Low,High*High));
    internal CollisionRange Sqrt(){if(Low<0)throw new InvalidDataException("Unresolved collision bound square root.");return Enclose(Math.Sqrt(Low),Math.Sqrt(High));}
}

/// <summary>Second-order interval jet in the two patch coordinates. Piecewise
/// continuous branches retain gradient unions and explicitly lose smoothness.</summary>
internal readonly record struct CollisionGradientVariation(double Constant,double Slope)
{
    public static CollisionGradientVariation operator +(CollisionGradientVariation a,CollisionGradientVariation b)=>new(
        ((CollisionRange)a.Constant+b.Constant).High,((CollisionRange)a.Slope+b.Slope).High);
    public static CollisionGradientVariation operator *(CollisionGradientVariation a,double scale)=>new(
        ((CollisionRange)a.Constant*scale).High,((CollisionRange)a.Slope*scale).High);
}

internal readonly record struct CollisionJet(CollisionRange V,CollisionRange X,CollisionRange Y,CollisionRange XX,CollisionRange XY,CollisionRange YY,bool Smooth=true,CollisionGradientVariation? Variation=null)
{
    public static implicit operator CollisionJet(double x)=>new(x,0,0,0,0,0);
    internal static CollisionJet Variable(CollisionRange v,bool x)=>new(v,x?1:0,x?0:1,0,0,0);
    internal double GradientBound=>CollisionRange.UpperNorm(X.Magnitude,Y.Magnitude);
    internal double HessianBound=>CollisionRange.UpperNorm(XX.Magnitude,XY.Magnitude,XY.Magnitude,YY.Magnitude);
    // For any two points distance d apart, gradient oscillation is at most
    // Constant + Slope*d. A piecewise join contributes only its own gradient
    // union; adding a smooth field must not turn that field into an O(d) error.
    internal CollisionGradientVariation GradientVariation=>Smooth?new(0,HessianBound):Variation!.Value;
    internal CollisionJet WithRange(CollisionRange range)=>this with {V=range};
    internal CollisionJet Union(CollisionJet b)=>new(V.Hull(b.V),X.Hull(b.X),Y.Hull(b.Y),XX.Hull(b.XX),XY.Hull(b.XY),YY.Hull(b.YY),false,
        new(CollisionRange.UpperNorm(X.Hull(b.X).Width,Y.Hull(b.Y).Width),0));
    public static CollisionJet operator +(CollisionJet a,CollisionJet b)=>new(a.V+b.V,a.X+b.X,a.Y+b.Y,a.XX+b.XX,a.XY+b.XY,a.YY+b.YY,a.Smooth&&b.Smooth,
        a.Smooth&&b.Smooth?null:a.GradientVariation+b.GradientVariation);
    public static CollisionJet operator -(CollisionJet a)=>new(-a.V,-a.X,-a.Y,-a.XX,-a.XY,-a.YY,a.Smooth,a.Variation);
    public static CollisionJet operator -(CollisionJet a,CollisionJet b)=>a+-b;
    public static CollisionJet operator *(CollisionJet a,CollisionJet b)=>new(a.V*b.V,a.X*b.V+a.V*b.X,a.Y*b.V+a.V*b.Y,
        a.XX*b.V+2*a.X*b.X+a.V*b.XX,a.XY*b.V+a.X*b.Y+a.Y*b.X+a.V*b.XY,a.YY*b.V+2*a.Y*b.Y+a.V*b.YY,a.Smooth&&b.Smooth,
        a.Smooth&&b.Smooth?null:a.GradientVariation*b.V.Magnitude+b.GradientVariation*a.V.Magnitude+
            new CollisionGradientVariation(0,(2*(CollisionRange)a.GradientBound*b.GradientBound).High));
    public static CollisionJet operator /(CollisionJet a,CollisionJet b)=>a*b.Reciprocal();
    private CollisionJet Compose(CollisionRange value,CollisionRange first,CollisionRange second)=>new(value,first*X,first*Y,
        second*X*X+first*XX,second*X*Y+first*XY,second*Y*Y+first*YY,Smooth,
        Smooth?null:GradientVariation*first.Magnitude+new CollisionGradientVariation(0,((CollisionRange)second.Magnitude*GradientBound*GradientBound).High));
    internal CollisionJet Square()=>Compose(V.Square(),2*V,2);
    internal CollisionJet Reciprocal(){var v=1/V;return Compose(v,-v*v,2*v*v*v);}
    internal CollisionJet Sqrt(){var s=V.Sqrt();return Compose(s,.5/s,-.25/(V*s));}
    internal CollisionJet Acos(){var q=1-V.Square();var s=q.Sqrt();return Compose(new(PhysicalCollisionAngles.Acos(V.High).Low,PhysicalCollisionAngles.Acos(V.Low).High),-1/s,-V/(q*s));}
    internal CollisionJet Atan(){var q=1+V.Square();return Compose(new(PhysicalCollisionAngles.Atan(V.Low).Low,PhysicalCollisionAngles.Atan(V.High).High),1/q,-2*V/(q*q));}
    internal CollisionJet MaxZero()=>V.Low>=0?this:V.High<=0?0:Union(0).WithRange(new(0,V.High));
    internal static CollisionJet Minimum(CollisionJet a,CollisionJet b)=>a.V.High<b.V.Low?a:b.V.High<a.V.Low?b:a.Union(b).WithRange(new(Math.Min(a.V.Low,b.V.Low),Math.Min(a.V.High,b.V.High)));
    internal CollisionJet ClampUnit(){if(V.Low>=1)return 1;if(V.High<=0)return 0;var result=V.Low<0?Union(0):this;if(V.High>1)result=result.Union(1);return result.WithRange(V.Clip(0,1));}
    internal CollisionJet Abs()=>V.Low>=0?this:V.High<=0?-this:Union(-this).WithRange(new(0,V.Magnitude));
}

internal readonly record struct CollisionVector(CollisionJet X,CollisionJet Y,CollisionJet Z)
{
    public static CollisionVector operator +(CollisionVector a,CollisionVector b)=>new(a.X+b.X,a.Y+b.Y,a.Z+b.Z);
    public static CollisionVector operator *(CollisionVector a,CollisionJet b)=>new(a.X*b,a.Y*b,a.Z*b);
    internal CollisionJet Dot(Double3 b)=>X*b.X+Y*b.Y+Z*b.Z;
    internal CollisionJet Length=> (X.Square()+Y.Square()+Z.Square()).Sqrt();
    internal CollisionVector Normalized()=>this*Length.Reciprocal();
}
