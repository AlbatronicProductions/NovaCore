using NovaCore.Core;

namespace NovaCore.Simulation.Spacecraft.Contact;

/// <summary>Private-purpose binary64 outward arithmetic for the bounded Florida proof. No runtime GC/config changes.</summary>
internal readonly record struct FloridaBound(double Lower, double Upper)
{
    internal bool IsFinite => double.IsFinite(Lower) && double.IsFinite(Upper) && Lower <= Upper;
    internal double Magnitude => Math.Max(Math.Abs(Lower), Math.Abs(Upper));
    internal bool Contains(double x) => Lower <= x && x <= Upper;
    internal static FloridaBound Point(double value) => new(value, value);
    internal static FloridaBound Hull(double lower, double upper) => new(lower, upper);
    internal static FloridaBound Integer(long value) => value is >= -9007199254740992L and <= 9007199254740992L
        ? Point(value) : new(Math.BitDecrement((double)value),Math.BitIncrement((double)value));
    internal static FloridaBound Integer(ulong value) => value<=9007199254740992UL
        ? Point(value) : new(Math.BitDecrement((double)value),Math.BitIncrement((double)value));
    public static implicit operator FloridaBound(double value) => Point(value);
    private static double Down(double x) => Math.BitDecrement(x);
    private static double Up(double x) => Math.BitIncrement(x);
    public static FloridaBound operator +(FloridaBound a, FloridaBound b) => new(Down(a.Lower+b.Lower), Up(a.Upper+b.Upper));
    public static FloridaBound operator -(FloridaBound a) => new(-a.Upper,-a.Lower);
    public static FloridaBound operator -(FloridaBound a, FloridaBound b) => a+-b;
    public static FloridaBound operator *(FloridaBound a, FloridaBound b)
    {
        var x=a.Lower*b.Lower; var y=a.Lower*b.Upper; var z=a.Upper*b.Lower; var w=a.Upper*b.Upper;
        return new(Down(Math.Min(Math.Min(x,y),Math.Min(z,w))),Up(Math.Max(Math.Max(x,y),Math.Max(z,w))));
    }
    public static FloridaBound operator /(FloridaBound a, FloridaBound b) => b.Contains(0)
        ? new(double.NaN,double.NaN) : a*new FloridaBound(Down(1/b.Upper),Up(1/b.Lower));
    internal FloridaBound Square() => new(Contains(0)?0:Math.Max(0,Down(Math.Min(Lower*Lower,Upper*Upper))),Up(Math.Max(Lower*Lower,Upper*Upper)));
    internal FloridaBound Sqrt() => Lower < 0 ? new(double.NaN,double.NaN) : new(Lower==0?0:Down(Math.Sqrt(Lower)),Up(Math.Sqrt(Upper)));
    internal FloridaBound Expand(double radius) => this+new FloridaBound(-radius,radius);
    internal static FloridaBound Pow(FloridaBound x,int exponent)
    { FloridaBound y=1; for(var i=0;i<exponent;i++)y*=x;return y; }

    // pi is enclosed by the adjacent binary64 values around its mathematical value.
    // Independent permanent test verifies this with Machin's alternating arctangent series.
    internal static readonly FloridaBound Pi = new(Math.PI,Math.BitIncrement(Math.PI));
    internal static FloridaBound Sin(FloridaBound angle) => Trig(angle,false);
    internal static FloridaBound Cos(FloridaBound angle) => Trig(angle,true);
    private static FloridaBound Trig(FloridaBound angle,bool cosine)
    {
        // Coverage is one hour about J2000; no large-argument reduction or libm accuracy assumption.
        var turns=Math.Round((angle.Lower+(angle.Upper-angle.Lower)*.5)/(2*Math.PI));
        if(!angle.IsFinite || Math.Abs(turns)>2)return new(double.NaN,double.NaN);
        var x=angle-2*turns*Pi;
        if(x.Magnitude>4)return new(double.NaN,double.NaN);
        FloridaBound term=cosine?1:x;var sum=term;var square=x.Square();
        for(var k=1;k<=24;k++)
        {
            var a=cosine?2*k-1:2*k;
            term=-term*square/(a*(a+1));sum+=term;
        }
        // Taylor's theorem: all real sin/cos derivatives have absolute value <=1.
        var order=cosine?49:50;var error=Pow(Point(x.Magnitude),order);
        for(var k=2;k<=order;k++)error/=k;
        return sum.Expand(error.Upper);
    }
}

internal readonly record struct FloridaVector(FloridaBound X,FloridaBound Y,FloridaBound Z)
{
    internal static FloridaVector From(Double3 v)=>new(v.X,v.Y,v.Z);
    internal bool IsFinite=>X.IsFinite&&Y.IsFinite&&Z.IsFinite;
    internal FloridaBound NormSquared { get { var value=X.Square()+Y.Square()+Z.Square();return new(Math.Max(0,value.Lower),value.Upper); } }
    internal FloridaBound Norm=>NormSquared.Sqrt();
    internal FloridaVector Expand(double error)=>new(X.Expand(error),Y.Expand(error),Z.Expand(error));
    public static FloridaVector operator +(FloridaVector a,FloridaVector b)=>new(a.X+b.X,a.Y+b.Y,a.Z+b.Z);
    public static FloridaVector operator -(FloridaVector a,FloridaVector b)=>new(a.X-b.X,a.Y-b.Y,a.Z-b.Z);
    public static FloridaVector operator -(FloridaVector a)=>new(-a.X,-a.Y,-a.Z);
    public static FloridaVector operator *(FloridaVector a,FloridaBound b)=>new(a.X*b,a.Y*b,a.Z*b);
    public static FloridaVector operator /(FloridaVector a,FloridaBound b)=>new(a.X/b,a.Y/b,a.Z/b);
    internal static FloridaBound Dot(FloridaVector a,FloridaVector b)=>a.X*b.X+a.Y*b.Y+a.Z*b.Z;
    internal static FloridaVector Cross(FloridaVector a,FloridaVector b)=>new(a.Y*b.Z-a.Z*b.Y,a.Z*b.X-a.X*b.Z,a.X*b.Y-a.Y*b.X);
    internal static FloridaVector RotateUnitQuaternion(DoubleQuaternion q,Double3 offset)
    {
        // The physical attitude is the exact normalization of the stored quaternion bits.
        var vector=From(new(q.X,q.Y,q.Z));FloridaBound w=q.W;
        var norm=(vector.NormSquared+w.Square()).Sqrt();vector/=norm;w/=norm;
        var v=From(offset);var twice=Cross(vector,v)*2;
        return v+twice*w+Cross(vector,twice);
    }
}
