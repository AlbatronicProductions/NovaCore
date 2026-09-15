internal static class StudyControl
{
    internal static int Sweeps=8;
    internal static bool UseD=true,Trace,AllowCold;
    internal static double[]? TangentOverride;
    internal static readonly List<object> Rows=new();
    private static CurrentPatch? patch;
    private static PieceKernel.State free;
    private static double h;
    internal static bool ArithmeticValid(ReadOnlySpan<Scaled> x)
    {foreach(var v in x)if(!ScaleMath.Valid(v))return false;return true;}
    internal static bool HistoricalFeasible(in ContactGeometry g,ReadOnlySpan<Scaled> x)=>Feasible(g,x,AllowCold);
    internal static bool CurrentFeasible(in ContactGeometry g,ReadOnlySpan<Scaled> x)=>Feasible(g,x,AllowCold);
    private static bool Feasible(in ContactGeometry g,ReadOnlySpan<Scaled> x,bool cold)
    {
        if(!g.Valid||!ArithmeticValid(x))return false;
        Scaled cap=default,wcap=default;
        for(int i=0;i<4;i++){if(cold?x[i].Mantissa<0:x[i].Mantissa<=0)return false;cap=ScaleMath.Add(cap,x[i].Times(.125));wcap=ScaleMath.Add(wcap,x[i].Times(.125*g.Radius(i)));}
        return ScaleMath.Compare(ScaleMath.Length(x[4],x[5]),cap)<=0&&ScaleMath.Compare(ScaleMath.Abs(x[6]),wcap)<=0;
    }
    internal static void Override(Span<Scaled> x)
    {if(TangentOverride is not null){x[4]=Scaled.From(TangentOverride[0]);x[5]=Scaled.From(TangentOverride[1]);}}
    internal static void Begin(CurrentPatch p,PieceKernel.State f,double duration,ReadOnlySpan<Scaled> x)
    {if(!Trace)return;patch=p;free=f;h=duration;Rows.Clear();}
    internal static void Observe(string stage,int sweep,int row,ReadOnlySpan<Scaled> x,int nc,int tc,int wc)
    {
        if(!Trace)return;
        double[] values=x.ToArray().Select(v=>v.Value).ToArray();
        double[] residual=new double[7];var p=patch!;var g=p.Geometry;
        double alpha=1/(g.Omega*h*(g.Omega*h+2));
        for(int i=0;i<7;i++){
            residual[i]=p.J(i,free);for(int j=0;j<7;j++)residual[i]+=p.K[i,j]*values[j];
            if(i<4)residual[i]+=alpha*p.K[i,i]*values[i]-Math.Min(g.Depth(i)/h,Math.Min(g.Depth(i)/(h+2/g.Omega),2));
        }
        Rows.Add(new{stage,sweep,row,impulses=values,residual,nc,tc,wc});
    }
}
