// Closed historical/current admission; intermediate transformations are not physical solves.
internal static class FinalPolicy
{
    internal static bool ArithmeticValid(ReadOnlySpan<Scaled> x)
    {foreach(var v in x)if(!ScaleMath.Valid(v))return false;return true;}
    internal static bool HistoricalClosed(in ContactGeometry g,ReadOnlySpan<Scaled> x)=>CurrentClosed(g,x);
    internal static bool CurrentClosed(in ContactGeometry g,ReadOnlySpan<Scaled> x)
    {
        if(!g.Valid||!ArithmeticValid(x))return false;
        Scaled cap=default,twistCap=default;
        for(int i=0;i<4;i++){
            if(x[i].Mantissa<=0)return false;
            cap=ScaleMath.Add(cap,x[i].Times(.125));
            twistCap=ScaleMath.Add(twistCap,x[i].Times(.125*g.Radius(i)));
        }
        return ScaleMath.Compare(ScaleMath.Length(x[4],x[5]),cap)<=0&&
            ScaleMath.Compare(ScaleMath.Abs(x[6]),twistCap)<=0;
    }
}

// Reporting only. Receipt authentication is checked by CoastDriver against preserved solve/
// install records; no reporting flag grants admission in the selected operator.
internal static class BoundaryPolicy
{
    internal static string Branch="";
    internal static bool AuthenticatedHistoricalReceipt;
    internal static int SolverAdmissions;
    internal readonly record struct Classification(bool Finite,bool PositiveNormals,CacheVector Cache,
        Scaled TangentMagnitude,Scaled TangentCap,Scaled TwistMagnitude,Scaled TwistCap,
        string Tangent,string Twist,bool ClosedFeasible);
    internal static Classification Classify(in ContactGeometry g,ReadOnlySpan<Scaled> x)
    {
        bool finite=FinalPolicy.ArithmeticValid(x),positive=true;
        Scaled cap=default,wcap=default;
        for(int i=0;i<4;i++){positive&=x[i].Mantissa>0;cap=ScaleMath.Add(cap,x[i].Times(.125));wcap=ScaleMath.Add(wcap,x[i].Times(.125*g.Radius(i)));}
        var t=ScaleMath.Length(x[4],x[5]);var w=ScaleMath.Abs(x[6]);
        string Name(int c)=>c<0?"INTERIOR":c==0?"BOUNDARY-SATURATED":"INFEASIBLE";
        return new(finite,positive,CacheVector.FromScales(x),t,cap,w,wcap,
            Name(ScaleMath.Compare(t,cap)),Name(ScaleMath.Compare(w,wcap)),FinalPolicy.CurrentClosed(g,x));
    }
}
