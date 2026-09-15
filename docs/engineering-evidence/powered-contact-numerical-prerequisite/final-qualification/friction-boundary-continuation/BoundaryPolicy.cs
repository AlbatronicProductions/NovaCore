// Diagnostic policy only. Never linked by the original prerequisite or production projects.
internal static class BoundaryPolicy
{
    internal static string Branch="";
    internal static bool AuthenticatedHistoricalReceipt;
    internal static int SolverAdmissions;
    internal readonly record struct Classification(bool Finite,bool PositiveNormals,CacheVector Cache,
        Scaled TangentMagnitude,Scaled TangentCap,Scaled TwistMagnitude,Scaled TwistCap,
        string Tangent,string Twist,bool ClosedFeasible);
    internal static bool ArithmeticValid(ReadOnlySpan<Scaled> x)
    {foreach(var v in x)if(!ScaleMath.Valid(v))return false;return true;}
    internal static Classification Classify(in ContactGeometry g,ReadOnlySpan<Scaled> x)
    {
        bool finite=ArithmeticValid(x),positive=true;for(int i=0;i<4;i++)positive&=x[i].Mantissa>0;
        Scaled cap=default,twistCap=default;
        for(int i=0;i<4;i++){cap=ScaleMath.Add(cap,x[i].Times(.125));twistCap=ScaleMath.Add(twistCap,x[i].Times(.125*g.Radius(i)));}
        var length=ScaleMath.Length(x[4],x[5]);var twist=ScaleMath.Abs(x[6]);
        int t=ScaleMath.Compare(length,cap),w=ScaleMath.Compare(twist,twistCap);
        string Name(int c)=>c<0?"INTERIOR":c==0?"BOUNDARY-SATURATED":"INFEASIBLE";
        return new(finite,positive,CacheVector.FromScales(x),length,cap,twist,twistCap,Name(t),Name(w),finite&&positive&&t<=0&&w<=0);
    }
    internal static void Observe(string stage,in ContactGeometry g,ReadOnlySpan<Scaled> x)=>
        Qualification.Save(Branch+"-"+stage+".json",new{stage,geometry=g,classification=Classify(g,x),solverAdmissions=SolverAdmissions});
    internal static bool HistoricalClosed(in ContactGeometry g,ReadOnlySpan<Scaled> x)=>
        AuthenticatedHistoricalReceipt&&g.Valid&&Classify(g,x).ClosedFeasible;
    internal static bool CurrentClosed(in ContactGeometry g,ReadOnlySpan<Scaled> x)=>g.Valid&&Classify(g,x).ClosedFeasible;
    internal static bool BeforeSolve()
    {
        Console.WriteLine("CURRENT_FEASIBLE "+Branch+": review saved preparation; enter SOLVE or STOP.");
        if(Console.ReadLine()!="SOLVE")return false;SolverAdmissions++;return true;
    }
}
