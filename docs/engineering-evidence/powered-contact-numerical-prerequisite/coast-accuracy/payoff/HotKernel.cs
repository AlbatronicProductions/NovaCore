// Diagnostic storage adapter only. Solve body mechanically copied from the pinned PieceKernel.
// Caller owns/reset cache; arithmetic, row order, clamps and velocity reconstruction are unchanged.
using Patch=PieceKernel.Patch;
using State=PieceKernel.State;

internal static class HotKernel
{
    internal const int Rows=7;
    internal readonly record struct Result(State Velocity,int NormalClamps,int TangentClamps,int TwistClamps);
    internal enum Admission { Ready,InvalidEquation,InvalidInputCache,NonpositiveOutput,CurrentFrictionCap }
    internal readonly record struct Preparation(Admission Status,double Delta,double Numerator,double Denominator,
        double Tangent,double TangentCap,double Twist,double TwistCap);
    static bool FinitePositiveNormals(ReadOnlySpan<double> x)
    {
        for(int i=0;i<7;i++)if(!double.IsFinite(x[i])||(i<4&&x[i]<=0))return false;
        return true;
    }
    static bool Feasible(Patch p,ReadOnlySpan<double> x,out double t,out double cap,out double w,out double wcap)
    {
        t=Math.Sqrt(x[4]*x[4]+x[5]*x[5]);cap=.125*(x[0]+x[1]+x[2]+x[3]);w=Math.Abs(x[6]);wcap=0;
        for(int i=0;i<4;i++)wcap+=.125*x[i]*p.Radii[i];
        return double.IsFinite(cap)&&double.IsFinite(wcap)&&t<cap&&w<wcap;
    }
    // Current A/b are derived here from current patch/free/h/omega, exactly as retained D.
    // No reference, previous source state, history lookup or oracle parameter.
    internal static Preparation Initialize(Patch patch,State free,double omega,double h,Span<double> initial)
    {
        if(initial.Length!=7||!double.IsFinite(h)||h<=0||!double.IsFinite(omega)||omega<=0)
            return new(Admission.InvalidEquation,0,0,0,0,0,0,0);
        if(!FinitePositiveNormals(initial)||!Feasible(patch,initial,out _,out _,out _,out _))
            return new(Admission.InvalidInputCache,0,0,0,0,0,0,0);
        var a=omega*h;double alpha=1/(a*(a+2)),numerator=0,denominator=0;
        if(!double.IsFinite(alpha))return new(Admission.InvalidEquation,0,0,0,0,0,0,0);
        for(int i=0;i<4;i++)
        {
            double rhs=Math.Min(patch.Depth[i]/h,Math.Min(patch.Depth[i]/(h+2/omega),2))-patch.J(i,free);
            double residual=-rhs;for(int j=0;j<7;j++)
            {
                double coefficient=patch.K[i,j]+(i==j?alpha*patch.K[i,i]:0);
                residual+=coefficient*initial[j];if(j<4)denominator+=coefficient;
            }
            numerator+=residual;
        }
        if(!double.IsFinite(denominator)||denominator<=0||!double.IsFinite(numerator))
            return new(Admission.InvalidEquation,0,numerator,denominator,0,0,0,0);
        double delta=-numerator/denominator;
        if(!double.IsFinite(delta))return new(Admission.InvalidEquation,delta,numerator,denominator,0,0,0,0);
        for(int i=0;i<4;i++)initial[i]+=delta;
        if(!FinitePositiveNormals(initial))return new(Admission.NonpositiveOutput,delta,numerator,denominator,0,0,0,0);
        bool feasible=Feasible(patch,initial,out var tangent,out var cap,out var twist,out var twistCap);
        return new(feasible?Admission.Ready:Admission.CurrentFrictionCap,delta,numerator,denominator,tangent,cap,twist,twistCap);
    }

    internal static Result Solve(Patch patch,State free,Span<double> lambda,double omega,int sweeps,double h)
    {
        var a=omega*h;var k=a*(a+2);var d=1+k;var c=k/d;
        var nc=0;var tc=0;var wc=0;
        for(var sweep=0;sweep<sweeps;sweep++)
        {
            for(var i=0;i<4;i++)
            {
                // Algebraically remove only the row's own warm kick; keep every other iterate.
                var others=patch.J(i,free);
                for(var j=0;j<Rows;j++)if(j!=i)others+=patch.K[i,j]*lambda[j];
                var depth=patch.Depth[i];
                var weightedBias=Math.Min(depth*omega*(a+2)/d,Math.Min(depth*omega*a/d,c*2));
                var next=(weightedBias-c*others)/patch.K[i,i];
                if(next<0)nc++;lambda[i]=Math.Max(0,next);
            }
            // Exact tangent block cancellation before solving; no nearly equal warm kick subtraction.
            var v0=patch.J(4,free);var v1=patch.J(5,free);
            for(var j=0;j<Rows;j++)if(j is not 4 and not 5)
            {v0+=patch.K[4,j]*lambda[j];v1+=patch.K[5,j]*lambda[j];}
            var determinant=patch.K[4,4]*patch.K[5,5]-patch.K[4,5]*patch.K[5,4];
            var x=(-patch.K[5,5]*v0+patch.K[4,5]*v1)/determinant;
            var y=(patch.K[5,4]*v0-patch.K[4,4]*v1)/determinant;
            var cap=.125*(lambda[0]+lambda[1]+lambda[2]+lambda[3]);
            var length=Math.Sqrt(x*x+y*y);
            // Preserve the pinned physical-unit denominator guard as well as the Coulomb cap.
            var scale=Math.Min(1,cap/Math.Max((double)1e-16f,length));
            if(scale<1)tc++;lambda[4]=x*scale;lambda[5]=y*scale;
            var spin=patch.J(6,free);
            for(var j=0;j<6;j++)spin+=patch.K[6,j]*lambda[j];
            var twist=-spin/patch.K[6,6];var twistCap=0d;
            for(var j=0;j<4;j++)twistCap+=.125*lambda[j]*patch.Radii[j];
            if(Math.Abs(twist)>twistCap)wc++;lambda[6]=Math.Clamp(twist,-twistCap,twistCap);
        }
        return new(patch.Reconstruct(free,lambda),nc,tc,wc);
    }
}
