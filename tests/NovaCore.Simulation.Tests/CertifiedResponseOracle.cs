using System.Numerics;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Contact;
using NovaCore.Simulation.Spacecraft.Rotation;

// Test-only exact rational arithmetic, independent of production interval arithmetic.
internal static class CertifiedResponseOracle
{
    internal readonly record struct R
    {
        internal BigInteger N { get; }
        internal BigInteger D { get; }
        internal R(BigInteger n, BigInteger d)
        {
            if(d.IsZero)throw new DivideByZeroException();
            if(d.Sign<0){n=-n;d=-d;}var gcd=BigInteger.GreatestCommonDivisor(n,d);N=n/gcd;D=d/gcd;
        }
        internal static R From(double value)
        {
            if(!double.IsFinite(value))throw new ArgumentOutOfRangeException(nameof(value));
            var bits=BitConverter.DoubleToUInt64Bits(value);var exponent=(int)((bits>>52)&2047);
            BigInteger n=(bits&0xfffffffffffffUL)|(exponent==0?0:1UL<<52);if((bits>>63)!=0)n=-n;
            var power=(exponent==0?1:exponent)-1023-52;
            return power>=0?new(n<<power,1):new(n,BigInteger.One<<-power);
        }
        internal static R From(decimal x)
        {
            var bits=decimal.GetBits(x);var n=(BigInteger)(uint)bits[0]+((BigInteger)(uint)bits[1]<<32)+((BigInteger)(uint)bits[2]<<64);
            return new(bits[3]<0?-n:n,BigInteger.Pow(10,(bits[3]>>16)&255));
        }
        public static implicit operator R(int x)=>new(x,1);
        public static R operator +(R a,R b)=>new(a.N*b.D+b.N*a.D,a.D*b.D);
        public static R operator -(R a)=>new(-a.N,a.D);
        public static R operator -(R a,R b)=>a+-b;
        public static R operator *(R a,R b)=>new(a.N*b.N,a.D*b.D);
        public static R operator /(R a,R b)=>new(a.N*b.D,a.D*b.N);
        public static bool operator <(R a,R b)=>a.N*b.D<b.N*a.D;
        public static bool operator >(R a,R b)=>b<a;
        public static bool operator <=(R a,R b)=>!(a>b);
        public static bool operator >=(R a,R b)=>!(a<b);
        public override string ToString()=>$"{N}/{D}";
    }
    internal readonly record struct V(R X,R Y,R Z)
    {
        internal static V From(Double3 x)=>new(R.From(x.X),R.From(x.Y),R.From(x.Z));
        public static V operator +(V a,V b)=>new(a.X+b.X,a.Y+b.Y,a.Z+b.Z);
        public static V operator -(V a,V b)=>new(a.X-b.X,a.Y-b.Y,a.Z-b.Z);
        public static V operator *(V a,R b)=>new(a.X*b,a.Y*b,a.Z*b);
        internal static V Cross(V a,V b)=>new(a.Y*b.Z-a.Z*b.Y,a.Z*b.X-a.X*b.Z,a.X*b.Y-a.Y*b.X);
        internal static R Dot(V a,V b)=>a.X*b.X+a.Y*b.Y+a.Z*b.Z;
    }
    internal static V InverseRotate(DoubleQuaternion q,V n)
    {
        var x=R.From(q.X);var y=R.From(q.Y);var z=R.From(q.Z);var w=R.From(q.W);var d=x*x+y*y+z*z+w*w;
        return new(((w*w+x*x-y*y-z*z)*n.X+2*(x*y+w*z)*n.Y+2*(x*z-w*y)*n.Z)/d,
            (2*(x*y-w*z)*n.X+(w*w-x*x+y*y-z*z)*n.Y+2*(y*z+w*x)*n.Z)/d,
            (2*(x*z+w*y)*n.X+2*(y*z-w*x)*n.Y+(w*w-x*x-y*y+z*z)*n.Z)/d);
    }
    internal static (R K,R J,V Linear,V Angular,V A) Solve(V n,R u,Double3 lever,DoubleQuaternion q,double mass,PrincipalMomentsOfInertia inertia)
    {
        var a=V.Cross(V.From(lever),InverseRotate(q,n));
        var k=1/R.From(mass)+a.X*a.X/R.From(inertia.X)+a.Y*a.Y/R.From(inertia.Y)+a.Z*a.Z/R.From(inertia.Z);var j=-u/k;
        return(k,j,n*j,a*j,a);
    }
    internal static bool Contains(FloridaBound b,R x)=>b.IsFinite&&R.From(b.Lower)<=x&&R.From(b.Upper)>=x;
    internal static bool Contains(FloridaVector b,V x)=>Contains(b.X,x.X)&&Contains(b.Y,x.Y)&&Contains(b.Z,x.Z);

    // Independent exact interval reference, used with the separately bounded real-root oracle.
    internal readonly record struct B(R L,R H)
    {
        internal static B Point(R x)=>new(x,x);
        internal static B From(FloridaBound x)=>new(R.From(x.Lower),R.From(x.Upper));
        public static implicit operator B(int x)=>Point(x);
        public static B operator +(B x,B y)=>new(x.L+y.L,x.H+y.H);
        public static B operator -(B x)=>new(-x.H,-x.L);
        public static B operator -(B x,B y)=>x+-y;
        public static B operator *(B x,B y)
        {
            R[] p=[x.L*y.L,x.L*y.H,x.H*y.L,x.H*y.H];var lo=p[0];var hi=p[0];
            foreach(var v in p){if(v<lo)lo=v;if(v>hi)hi=v;}return new(lo,hi);
        }
        public static B operator /(B x,B y)
        {if(y.L<=0)throw new InvalidOperationException("oracle positive denominator");return x*new B(1/y.H,1/y.L);}
        internal B Square()=>new(L<=0&&H>=0?0:(L*L<H*H?L*L:H*H),L*L>H*H?L*L:H*H);
    }
    internal static (B K,B J,B[] Linear,B[] Angular) Enclose(B[] n,B u,Double3 lever,DoubleQuaternion q,double mass,PrincipalMomentsOfInertia inertia)
    {
        var x=R.From(q.X);var y=R.From(q.Y);var z=R.From(q.Z);var w=R.From(q.W);var d=x*x+y*y+z*z+w*w;
        R[,] transpose={{(w*w+x*x-y*y-z*z)/d,2*(x*y+w*z)/d,2*(x*z-w*y)/d},
            {2*(x*y-w*z)/d,(w*w-x*x+y*y-z*z)/d,2*(y*z+w*x)/d},
            {2*(x*z+w*y)/d,2*(y*z-w*x)/d,(w*w-x*x-y*y+z*z)/d}};
        B[] nb=[0,0,0];for(var i=0;i<3;i++)for(var j=0;j<3;j++)nb[i]+=B.Point(transpose[i,j])*n[j];var r=V.From(lever);
        B[] a=[B.Point(r.Y)*nb[2]-B.Point(r.Z)*nb[1],B.Point(r.Z)*nb[0]-B.Point(r.X)*nb[2],B.Point(r.X)*nb[1]-B.Point(r.Y)*nb[0]];
        var k=B.Point(1/R.From(mass))+a[0].Square()/B.Point(R.From(inertia.X))+a[1].Square()/B.Point(R.From(inertia.Y))+a[2].Square()/B.Point(R.From(inertia.Z));var impulse=-u/k;
        return(k,impulse,n.Select(v=>v*impulse).ToArray(),a.Select(v=>v*impulse).ToArray());
    }
    internal static bool Contains(FloridaBound outer,B inner)=>Contains(outer,inner.L)&&Contains(outer,inner.H);
    internal static bool Contains(FloridaVector outer,B[] inner)=>Contains(outer.X,inner[0])&&Contains(outer.Y,inner[1])&&Contains(outer.Z,inner[2]);
}
