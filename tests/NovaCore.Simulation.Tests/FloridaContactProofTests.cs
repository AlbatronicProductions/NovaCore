using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Contact;
using NovaCore.Simulation.Celestial;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;

internal static class FloridaContactProofTests
{
    private static void Check(bool value,string name){if(!value)throw new InvalidOperationException("Florida proof: "+name);}
    internal static void Run()
    {
        Arithmetic();AnalyticalControls();SeedPositionAndDerivativeOracle();
        for(var i=0;i<32;i++)Work();
        using var measurement=new OrdinaryAllocationMeasurement("Florida mathematical enclosure");
        var complete=0;for(var i=0;i<128;i++)if(Work())complete++;
        var bytes=measurement.Complete();OrdinaryAllocationMeasurement.RequireZero(bytes,"Florida mathematical enclosure");
        Check(complete==128,"complete mathematical workload");OrdinaryAllocationMeasurement.PositiveControl();
        Check(default(FloridaContactProvider.Proof).IsRoot==false,"default has no root authority");
        Console.WriteLine("FLORIDA_ANALYTICAL rational/irrational/oversized-denominator/tangent/multiple/boundary/refinement controls PASS");
    }
    private static bool Work()=>FloridaBound.Sin(new(.2,.2001)).IsFinite&&FloridaBound.Cos(new(-3.4,-3.39)).IsFinite;
    private static void Arithmetic()
    {
        // Independent decimal Machin formula. Decimal truncation here is far below a binary64 ulp of pi.
        static decimal Atan(decimal x){decimal sum=0,p=x;for(var n=0;n<24;n++){sum+=(n%2==0?1:-1)*p/(2*n+1);p*=x*x;}return sum;}
        var pi=16*Atan(1m/5)-4*Atan(1m/239);
        Check(ExactDecimal(FloridaBound.Pi.Lower)<pi&&ExactDecimal(FloridaBound.Pi.Upper)>pi,"pi adjacent enclosure via independent Machin");
        foreach(var n in new[]{long.MinValue,long.MaxValue,9007199254740993L,-9007199254740993L})
        {var b=FloridaBound.Integer(n);Check(ExactDecimal(b.Lower)<=n&&ExactDecimal(b.Upper)>=n,"large exact epoch conversion");}
        var zero=FloridaVector.From(Double3.Zero).Norm;Check(zero.IsFinite&&zero.Contains(0),"subnormal outward norm");
        Check((new FloridaBound(-2,3).Square()).Contains(0)&&new FloridaBound(-2,3).Square().Contains(9),"square straddles zero");
        Check(!(FloridaBound.Point(1)/new FloridaBound(-1,1)).IsFinite,"zero denominator refused");
        foreach(var x in new[]{-3.9,-1.2,0,.4,3.8})
        {
            // Independent decimal Taylor at small arguments, with 24-term remainder < decimal roundoff.
            decimal t=(decimal)x,term=t,s=term,c=1,ct=1;
            for(var n=1;n<=24;n++){term*=-t*t/(2*n*(2*n+1));s+=term;ct*=-t*t/((2*n-1)*2*n);c+=ct;}
            var input=new FloridaBound(Math.BitDecrement(x),Math.BitIncrement(x));
            Check(FloridaBound.Sin(input).Contains((double)s)&&FloridaBound.Cos(input).Contains((double)c),"independent scalar trig");
        }
    }
    private static decimal ExactDecimal(double x)
    {
        if(x==0)return 0;
        var bits=BitConverter.DoubleToUInt64Bits(x);var rawExponent=(int)((bits>>52)&2047);
        var significand=(bits&0xfffffffffffffUL)|(rawExponent==0?0:1UL<<52);
        var exponent=(rawExponent==0?1:rawExponent)-1023-52;decimal result=significand;
        while(exponent<0){result/=2;exponent++;}while(exponent>0){result*=2;exponent--;}
        return (bits>>63)==0?result:-result;
    }
    private readonly record struct D3(decimal X,decimal Y,decimal Z)
    {
        internal static D3 From(Double3 x)=>new(ExactDecimal(x.X),ExactDecimal(x.Y),ExactDecimal(x.Z));
        public static D3 operator +(D3 a,D3 b)=>new(a.X+b.X,a.Y+b.Y,a.Z+b.Z);
        public static D3 operator -(D3 a,D3 b)=>new(a.X-b.X,a.Y-b.Y,a.Z-b.Z);
        public static D3 operator *(D3 a,decimal k)=>new(a.X*k,a.Y*k,a.Z*k);
        internal static D3 Cross(D3 a,D3 b)=>new(a.Y*b.Z-a.Z*b.Y,a.Z*b.X-a.X*b.Z,a.X*b.Y-a.Y*b.X);
    }
    private static void SeedPositionAndDerivativeOracle()
    {
        // Independent decimal Euler/derivative oracle at the authoritative seed, where Earth
        // position and velocity are exact initial conditions, not a numerical ephemeris sample.
        var system=SolAnalyticalDefinition.Instance;system.TryGetNode(SolarSystemBodyIds.Earth,out var node);
        system.TryGetAnalyticalKepler(node.Ephemeris.PayloadIndex,out var seed);
        CelestialBodyOrientationEvaluator.TryGetContactProofModel(out var model);
        var position=seed.StateAtEpoch.Position+new Double3(1000,2000,-3000);
        var velocity=seed.StateAtEpoch.Velocity+new Double3(2,-3,5);
        var linear=new SpacecraftTranslationState(new(1),new(1),SimulationInstant.Zero,position,velocity,Double3.Zero);
        var quaternion=new DoubleQuaternion(0,0,.6,.8);var feature=new Double3(1,-2,3);
        var angular=new SpacecraftRigidBodyRotationState(new(1),SimulationInstant.Zero,quaternion,Double3.Zero,new(2,3,4),Double3.Zero,RigidBodyRotationModel.ConstantBodyTorqueV1);
        Check(FloridaContactMotion.TryCreate(system,linear,angular,new(8),feature,out var motion)&&motion.Evaluate(0,out _,out _),"seed oracle admission");
        motion.Evaluate(0,out var actual,out var actualDerivative);
        decimal qz=ExactDecimal(quaternion.Z),qw=ExactDecimal(quaternion.W),norm=1;
        for(var i=0;i<8;i++)norm=(norm+(qz*qz+qw*qw)/norm)/2;
        var qv=new D3(0,0,qz/norm);qw/=norm;var v=D3.From(feature);var twice=D3.Cross(qv,v)*2;
        var p=D3.From(position)-D3.From(seed.StateAtEpoch.Position)+v+twice*qw+D3.Cross(qv,twice);
        var d=D3.From(velocity)-D3.From(seed.StateAtEpoch.Velocity);
        const decimal pi=3.1415926535897932384626433833m;var rad=pi/180;
        var century=ExactDecimal(model.SecondsPerDay)*ExactDecimal(model.DaysPerCentury);
        static (decimal Sin,decimal Cos) Trig(decimal x)
        {decimal s=x,c=1,st=x,ct=1;for(var k=1;k<=32;k++){st*=-x*x/(2*k*(2*k+1));ct*=-x*x/((2*k-1)*2*k);s+=st;c+=ct;}return(s,c);}
        static void Rotate(ref D3 p,ref D3 d,decimal angle,decimal rate,bool x)
        {
            var(s,c)=Trig(angle);
            D3 Apply(D3 a)=>x?new(a.X,c*a.Y-s*a.Z,s*a.Y+c*a.Z):new(c*a.X-s*a.Y,s*a.X+c*a.Y,a.Z);
            p=Apply(p);d=Apply(d)+(x?new D3(0,-p.Z,p.Y):new D3(-p.Y,p.X,0))*rate;
        }
        Rotate(ref p,ref d,-(ExactDecimal(model.Ra0)+90)*rad,-ExactDecimal(model.RaT)*rad/century,false);
        Rotate(ref p,ref d,-(90-ExactDecimal(model.Dec0))*rad,ExactDecimal(model.DecT)*rad/century,true);
        Rotate(ref p,ref d,-ExactDecimal(model.W0)*rad,-ExactDecimal(model.Wd)*rad/ExactDecimal(model.SecondsPerDay),false);
        p=new(p.X,p.Z,-p.Y);d=new(d.X,d.Z,-d.Y);
        static void Encloses(FloridaVector bounds,D3 expected,string name)
        {Check(ExactDecimal(bounds.X.Lower)<=expected.X&&expected.X<=ExactDecimal(bounds.X.Upper)&&
            ExactDecimal(bounds.Y.Lower)<=expected.Y&&expected.Y<=ExactDecimal(bounds.Y.Upper)&&
            ExactDecimal(bounds.Z.Lower)<=expected.Z&&expected.Z<=ExactDecimal(bounds.Z.Upper),name);}
        Encloses(actual,p,"independent decimal seed position");Encloses(actualDerivative,d,"independent decimal analytic derivative");
    }
    private static void AnalyticalControls()
    {
        FloridaContactStatus Kind(FloridaBound f,FloridaBound a,FloridaBound b,FloridaBound d)=>FloridaContactProvider.Classify(f,a,b,d,out _);
        var u=new FloridaBound(.5,1);var f=1-2*u.Square();
        Check(Kind(f,1-2*FloridaBound.Point(.5).Square(),1-2*FloridaBound.Point(1).Square(),-4*u)==FloridaContactStatus.CertifiedUniqueApproachingRoot,"irrational unique crossing");
        var lo=.5;var hi=1d;var steps=0;
        for(;steps<24;steps++){var m=lo+(hi-lo)*.5;var value=1-2*FloridaBound.Point(m).Square();if(value.Lower>0)lo=m;else if(value.Upper<0)hi=m;else break;}
        // Exact decimal inequalities independently prove the bracket contains +1/sqrt(2), no rational root conversion.
        Check(2*(decimal)lo*(decimal)lo<1&&2*(decimal)hi*(decimal)hi>1&&steps<=24,"irrational isolation and bounded refinement");
        Check(Kind(new(-1,1),1,-1,-2)==FloridaContactStatus.CertifiedUniqueApproachingRoot,"rational half root");
        var big=FloridaBound.Integer(ulong.MaxValue)+2;var tiny=new FloridaBound(0,Math.ScaleB(1,-63));
        Check(Kind(1-big*tiny,1,1-big*FloridaBound.Point(tiny.Upper),-big)==FloridaContactStatus.CertifiedUniqueApproachingRoot,"root denominator 2^64+1");
        Check(Kind(new(1,2),1,2,new(-1,1))==FloridaContactStatus.CertifiedClear,"range-proven clear without monotonicity");
        Check(Kind(new(-.1,1),1,1,new(-2,2))==FloridaContactStatus.Unresolved,"tangent cannot become clear from endpoint signs");
        Check(Kind(new(-1,1),1,1,new(-4,4))==FloridaContactStatus.Unresolved,"two crossings cannot become clear");
        Check(Kind(new(-1,0),0,-1,-1)==FloridaContactStatus.Unresolved,"initial boundary requires admission");
        // Independent linear oracle: f(u)=1-1.000001u, root 1000000/1000001 near u=1.
        Check(Kind(new(-.000002,1),1,new(-.000002,-.0000005),new(-1.000002,-1))==FloridaContactStatus.CertifiedUniqueApproachingRoot,"near canonical boundary crossing");
        Check(Kind(new(-1,1),1,new(-double.Epsilon,double.Epsilon),-1)==FloridaContactStatus.Unresolved,"unproved end equality");
        Check(Kind(new(-1,1),1,-1,new(-1,double.Epsilon))==FloridaContactStatus.Unresolved,"weak derivative");
    }
}
