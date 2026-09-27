using NovaCore.Core;

namespace NovaCore.Graphics;

// This validates the existing runtime's returned bits; it never substitutes a
// second geographic height. No UCRT transcendental ULP promise is assumed.
internal static class PhysicalCollisionAngles
{
    private static readonly int Terms=SeriesTerms();
    internal static readonly CollisionRange Pi=16*Direct((CollisionRange)1/5)-4*Direct((CollisionRange)1/239);
    private const int AnchorIntervals=16;
    private static readonly CollisionRange[] Anchors=Enumerable.Range(0,AnchorIntervals+1).Select(i=>Direct((CollisionRange)i/AnchorIntervals)).ToArray();
    internal static readonly double MaximumAdmittedError=AdmissionBudget();
    private static int SeriesTerms()
    {
        // Range reduction puts |t|<=1/2. Bound the alternating-series tail
        // below 1/16 of binary64 unit roundoff before interval arithmetic.
        var power=(CollisionRange).5;var n=0;
        do{n++;power*=.25;}while((power/(2*n+1)).High>Math.ScaleB(1d,-57));
        return n;
    }
    private static CollisionRange Direct(CollisionRange x)
    {
        var t=x/(1+(1+x.Square()).Sqrt());var square=t.Square();var power=t;CollisionRange sum=0;var tail=double.PositiveInfinity;
        for(var k=0;k<Terms;k++)
        {
            var term=power/(2*k+1);sum=k%2==0?sum+term:sum-term;power*=square;
            tail=(((CollisionRange)power.Magnitude)/(2*k+3)).High;
            if(tail<=Math.ScaleB(1d,-57))break;
        }
        return 2*sum.Inflate(tail);
    }
    private static CollisionRange Reduced(CollisionRange x)
    {
        // atan(x)=atan(a)+atan((x-a)/(1+a*x)), with exact binary anchors.
        // The immutable anchor intervals use the same proved direct series.
        // A residual <=1/32 needs far fewer terms on each physical H query;
        // the same analytic remainder test decides termination.
        var index=Math.Clamp((int)Math.Round((x.Low+x.High)*(.5*AnchorIntervals)),0,AnchorIntervals);
        CollisionRange anchor=(double)index/AnchorIntervals;
        var t=(x-anchor)/(1+anchor*x);var square=t.Square();var power=t;CollisionRange sum=0;var tail=double.PositiveInfinity;
        for(var k=0;k<Terms;k++)
        {
            var term=power/(2*k+1);sum=k%2==0?sum+term:sum-term;power*=square;
            tail=(((CollisionRange)power.Magnitude)/(2*k+3)).High;
            if(tail<=Math.ScaleB(1d,-57))break;
        }
        return Anchors[index]+sum.Inflate(tail);
    }
    internal static CollisionRange Atan(double value)
    {
        if(!double.IsFinite(value))throw new InvalidDataException("Unqualified angle input.");
        var x=(CollisionRange)Math.Abs(value);
        var result=x.High<=1?Reduced(x):Pi/2-Reduced(1/x);
        return value<0?-result:result;
    }
    internal static CollisionRange Atan2(double y,double x)
    {
        if(!double.IsFinite(x)||!double.IsFinite(y))throw new InvalidDataException("Unqualified angle input.");
        if(x==0&&y==0)
        {
            // Longitude at the exact pole follows the existing signed-zero
            // convention. It is not an ordinary nonzero-vector atan2 limit.
            var axis=BitConverter.DoubleToInt64Bits(x)<0?Pi:(CollisionRange)0;
            return BitConverter.DoubleToInt64Bits(y)<0?-axis:axis;
        }
        var ax=(CollisionRange)Math.Abs(x);var ay=(CollisionRange)Math.Abs(y);
        var theta=ax.High>=ay.High?Reduced(ay/ax):Pi/2-Reduced(ax/ay);
        if(x<0)theta=Pi-theta;
        return BitConverter.DoubleToInt64Bits(y)<0?-theta:theta;
    }
    internal static CollisionRange Acos(double value)
    {
        if(!double.IsFinite(value)||value is <-1 or >1)throw new InvalidDataException("Unqualified angle input.");
        CollisionRange y=Math.Abs(value);
        var ratio=((1-y)/(1+y)).Clip(0,1);
        // sqrt(0) is exact; clamp only the directed interval's negative
        // roundoff at zero, never a production coordinate or height.
        var result=2*Reduced(ratio.Sqrt().Clip(0,1));
        return value<0?Pi-result:result;
    }
    private static double AdmissionBudget()
    {
        // Three roundoff units enclose a rounded interval endpoint followed
        // by its outward adjacent representable value. This is a derived
        // admission target, checked for every actual interval below.
        CollisionFinite x=new(new(0,1),3*CollisionFinite.Round(1),3);
        var t=x/(1+(1+x.Square()).Sqrt());var square=t.Square();var power=t;CollisionFinite sum=0;
        for(var k=0;k<Terms;k++){var term=power/(2*k+1);sum=(k%2==0?sum+term:sum-term).Tighten(new(-1,1));power*=square;}
        var result=sum*2;
        return CollisionFinite.Up(2*(CollisionRange)result.E+2*(CollisionRange)Pi.Width+4*(CollisionRange)CollisionFinite.Round(2*Math.PI)+Math.ScaleB(1d,-55));
    }
    internal static void Validate(double actual,CollisionRange certified)
    {
        if(!double.IsFinite(actual)||!certified.Finite||certified.Width>MaximumAdmittedError||actual<certified.Low||actual>certified.High)
            throw new InvalidDataException("Runtime geographic angle is outside its independently certified numerical domain.");
    }
    internal static void ValidatePhysicalDirection(Double3 input)
    {
        // Follow SampleElevation and LongitudeRadians exactly, including the
        // latter's additional normalization. The caller already supplied H's
        // input direction; its source-order error belongs to the patch budget.
        var second=input.Normalized();var third=second.Normalized();
        Validate(Math.Atan2(-third.Z,third.X),Atan2(-third.Z,third.X));
        var y=Math.Clamp(second.Y,-1,1);Validate(Math.Acos(y),Acos(y));
    }
}
