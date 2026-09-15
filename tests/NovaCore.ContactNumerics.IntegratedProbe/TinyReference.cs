using System.Numerics;

// Cold independent reference admission only. Exact rational elimination at h=2^-1075,
// including all h terms: no epsilon duration replacement, FP32 step, iterative oracle or tuning.
internal readonly record struct Rational
{
    internal BigInteger N {get;}
    internal BigInteger D {get;}
    internal int Sign=>N.Sign;
    internal Rational(BigInteger n,BigInteger d)
    {
        if(d.IsZero)throw new DivideByZeroException();if(d.Sign<0){n=-n;d=-d;}
        var gcd=BigInteger.GreatestCommonDivisor(BigInteger.Abs(n),d);N=n/gcd;D=d/gcd;
    }
    internal static Rational From(double value)
    {
        if(!double.IsFinite(value))throw new ArithmeticException("Nonfinite reference input");
        if(value==0)return new(0,1);
        long bits=BitConverter.DoubleToInt64Bits(value);int field=(int)((bits>>52)&2047);
        long mantissa=bits&0xfffffffffffffL;int exponent=field==0?-1074:field-1023-52;
        if(field!=0)mantissa|=1L<<52;if(bits<0)mantissa=-mantissa;
        return exponent<0?new(mantissa,BigInteger.One<<-exponent):new(new BigInteger(mantissa)<<exponent,1);
    }
    public static Rational operator +(Rational a,Rational b)=>new(a.N*b.D+b.N*a.D,a.D*b.D);
    public static Rational operator -(Rational a,Rational b)=>new(a.N*b.D-b.N*a.D,a.D*b.D);
    public static Rational operator -(Rational a)=>new(-a.N,a.D);
    public static Rational operator *(Rational a,Rational b)=>new(a.N*b.N,a.D*b.D);
    public static Rational operator /(Rational a,Rational b)=>new(a.N*b.D,a.D*b.N);
    internal static Rational Min(Rational a,Rational b)=>(a-b).Sign<0?a:b;
    // Report an exponent-separated approximation; every admission comparison remains exact rational.
    internal Scaled Scale()
    {
        if(N.IsZero)return default;
        var n=BigInteger.Abs(N);int ne=(int)n.GetBitLength(),de=(int)D.GetBitLength();
        int ns=Math.Max(0,ne-54),ds=Math.Max(0,de-54);
        var m=Scaled.From((double)(n>>ns)/(double)(D>>ds));return new(N.Sign*m.Mantissa,m.Exponent+ns-ds);
    }
}

internal static partial class Qualification
{
    private static Rational Dot(D3 a,D3 b)=>Rational.From(a.X)*Rational.From(b.X)+Rational.From(a.Y)*Rational.From(b.Y)+Rational.From(a.Z)*Rational.From(b.Z);
    private static void Tiny()
    {
        Gate="tiny-exact-reference-admission";var duration=Exact(double.Epsilon,2);
        Require(!duration.Exact.IsZero&&duration.Numerical.Significand==1&&duration.Numerical.Exponent==-1075,"same exact tiny authority");
        using var world=new RetainedWorld(8);var accepted=world.Operator.Snapshot;var source=world.Velocity;
        var g=world.ObserveTinyGeometry(duration,8);
        var zero=Rational.From(0);var one=Rational.From(1);var two=Rational.From(2);var h=new Rational(1,BigInteger.One<<1075);
        var omega=Rational.From(g.Omega);var k=omega*h*(omega*h+two);var alpha=one/k;
        var matrix=new Rational[7,7];var rhs=new Rational[7];
        var linear=new D3(source.Linear.X,source.Linear.Y,source.Linear.Z);var angular=new D3(source.Angular.X,source.Angular.Y,source.Angular.Z);
        var acceleration=new D3(0,16d/8-9.81,0); // exact represented force/mass; no force at coast yet.
        for(int i=0;i<7;i++)
        {
            rhs[i]=-Dot(g.Linear(i),linear)-Dot(g.Angular(i),angular)-h*Dot(g.Linear(i),acceleration);
            for(int j=0;j<7;j++)matrix[i,j]=Rational.From(.125)*Dot(g.Linear(i),g.Linear(j))+Rational.From(.5)*Dot(g.Angular(i),g.Angular(j));
            if(i<4)
            {
                matrix[i,i]=matrix[i,i]*(one+alpha);var depth=Rational.From(g.Depth(i));
                rhs[i]+=Rational.Min(depth/h,Rational.Min(depth/(h+two/omega),two));
            }
        }
        var a=new Rational[7,8];for(int i=0;i<7;i++){for(int j=0;j<7;j++)a[i,j]=matrix[i,j];a[i,7]=rhs[i];}
        for(int col=0;col<7;col++)
        {
            int pivot=col;while(pivot<7&&a[pivot,col].Sign==0)pivot++;
            Require(pivot<7,"exact independent interior equations nonsingular");
            if(pivot!=col)for(int j=col;j<8;j++)(a[pivot,j],a[col,j])=(a[col,j],a[pivot,j]);
            var divisor=a[col,col];for(int j=col;j<8;j++)a[col,j]/=divisor;
            for(int r=0;r<7;r++)if(r!=col){var factor=a[r,col];for(int j=col;j<8;j++)a[r,j]-=factor*a[col,j];}
        }
        var x=new Rational[7];for(int i=0;i<7;i++)x[i]=a[i,7];
        bool exactResidual=true,positive=true;var sum=zero;
        for(int i=0;i<7;i++)
        {
            var residual=-rhs[i];for(int j=0;j<7;j++)residual+=matrix[i,j]*x[j];exactResidual&=residual.Sign==0;
            if(i<4){positive&=x[i].Sign>0;sum+=x[i];}
        }
        var tangentSquared=x[4]*x[4]+x[5]*x[5];var cap=Rational.From(.125)*sum;var capSquared=cap*cap;
        bool frictionFeasible=(tangentSquared-capSquared).Sign<0;
        var scales=x.Select(v=>v.Scale()).ToArray();var length=ScaleMath.Length(scales[4],scales[5]);
        Save("tiny-event.json",new{status=positive&&frictionFeasible?"REFERENCE ADMITTED":"REFERENCE DOMAIN FAILURE",
            exactPositive=!duration.Exact.IsZero,seconds=duration.Numerical,fp64Seconds=duration.Numerical.Value,fp32Seconds=(float)duration.Numerical.Value,
            fuel=double.Epsilon,flow=2,force=16,mass=8,source,geometry=g,accepted,
            reference="Exact rational elimination of current seven-row interior equations; all h terms retained",
            referenceImpulseScales=scales,tangentMagnitude=length,tangentCap=cap.Scale(),tangentSquared=tangentSquared.Scale(),capSquared=capSquared.Scale(),
            exactPositiveNormals=positive,exactEquationResidualZero=exactResidual,exactTangentCapFeasible=frictionFeasible,
            exactTangentCapComparison=(tangentSquared-capSquared).Sign,referenceAdmissible=positive&&frictionFeasible,
            acceptedTupleUnchanged=world.Operator.Snapshot==accepted,nativeGeometryRefreshed=true,world.Unsafe,
            candidateSolverCalls=0,endpointInstalls=0,cacheInstalls=0,coastCalls=0,canonicalMutations=0,
            meaning="An infeasible interior solution is not a physical contact reference; no candidate accuracy is inferred from it."});
        Require(exactResidual,"exact rational elimination residual");
        Require(positive&&frictionFeasible,"actual tiny retained event lies outside the qualified inactive-friction reference/response domain");
    }
}
