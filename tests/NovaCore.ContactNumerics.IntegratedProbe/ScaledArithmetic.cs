// Signed complete products and sums. Scale is private numerical representation, never a new clock.
internal static class ScaleMath
{
    internal static Scaled Add(Scaled a,Scaled b)
    {
        if(a.Mantissa==0)return b;if(b.Mantissa==0)return a;
        int e=Math.Max(a.Exponent,b.Exponent);
        var m=Scaled.From(Math.ScaleB(a.Mantissa,a.Exponent-e)+Math.ScaleB(b.Mantissa,b.Exponent-e));
        return m.Mantissa==0?default:new(m.Mantissa,checked(e+m.Exponent));
    }
    internal static Scaled Negate(Scaled a)=>new(-a.Mantissa,a.Exponent);
    internal static Scaled Subtract(Scaled a,Scaled b)=>Add(a,Negate(b));
    internal static Scaled Divide(Scaled a,Scaled b)
    {
        if(b.Mantissa==0)throw new ArithmeticException("Zero scaled denominator");
        if(a.Mantissa==0)return default;
        var m=Scaled.From(a.Mantissa/b.Mantissa);return new(m.Mantissa,checked(a.Exponent-b.Exponent+m.Exponent));
    }
    internal static Scaled Abs(Scaled a)=>new(Math.Abs(a.Mantissa),a.Exponent);
    internal static int Compare(Scaled a,Scaled b)
    {
        if(a.Mantissa==0||b.Mantissa==0||Math.Sign(a.Mantissa)!=Math.Sign(b.Mantissa))return a.Mantissa.CompareTo(b.Mantissa);
        return a.Exponent==b.Exponent?a.Mantissa.CompareTo(b.Mantissa):Math.Sign(a.Mantissa)*a.Exponent.CompareTo(b.Exponent);
    }
    internal static Scaled Min(Scaled a,Scaled b)=>Compare(a,b)<0?a:b;
    internal static Scaled Length(Scaled a,Scaled b)
    {
        int e=Math.Max(a.Mantissa==0?int.MinValue:a.Exponent,b.Mantissa==0?int.MinValue:b.Exponent);
        if(e==int.MinValue)return default;
        double x=Math.ScaleB(a.Mantissa,a.Exponent-e),y=Math.ScaleB(b.Mantissa,b.Exponent-e);
        var m=Scaled.From(Math.Sqrt(x*x+y*y));return new(m.Mantissa,e+m.Exponent);
    }
    internal static bool Valid(Scaled a)=>double.IsFinite(a.Mantissa)&&a.Exponent is >=-16384 and <=16384;
}
internal readonly record struct ScaledVector(Scaled X,Scaled Y,Scaled Z)
{
    internal static ScaledVector Product(Scaled s,D3 v)=>new(s.Times(v.X),s.Times(v.Y),s.Times(v.Z));
    public static ScaledVector operator +(ScaledVector a,ScaledVector b)=>new(ScaleMath.Add(a.X,b.X),ScaleMath.Add(a.Y,b.Y),ScaleMath.Add(a.Z,b.Z));
    internal Scaled Dot(D3 v)=>ScaleMath.Add(ScaleMath.Add(X.Times(v.X),Y.Times(v.Y)),Z.Times(v.Z));
    internal PieceKernel.V Projection=>new(X.Value,Y.Value,Z.Value);
}
