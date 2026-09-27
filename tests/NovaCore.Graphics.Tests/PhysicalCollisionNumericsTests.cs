using System.Globalization;
using System.Numerics;
using System.Text.Json;
using NovaCore.Graphics;
using NovaCore.Core;
using NovaCore.Core.Surface;

internal static class PhysicalCollisionNumericsTests
{
    internal static void Run()
    {
        var count=0;
        void Need(bool result,string message){count++;if(!result)throw new InvalidDataException(message);}
        void Refuse(Action action,string message){try{action();}catch(InvalidDataException){count++;return;}throw new InvalidDataException(message);}
        Need(!EarthLocalTerrainElevationDataset.IsLoaded,"malformed regional load starts with no published snapshot");
        var package=File.ReadAllBytes(GraphicsTestHarness.RepositoryPath("tests","fixtures","terrain","local-payload2.nccube"));
        System.Buffers.Binary.BinaryPrimitives.WriteSingleLittleEndian(package.AsSpan(56),-float.MaxValue);
        System.Buffers.Binary.BinaryPrimitives.WriteSingleLittleEndian(package.AsSpan(60),float.MaxValue);
        Need(PlanetaryLocalTerrainPackContract.TryReadHeader(package,out _),"finite endpoints reach the source-float delta construction");
        var temporary=Path.Combine(Path.GetTempPath(),"novacore-contact-range-"+Guid.NewGuid().ToString("N")+".nccube");
        try
        {
            File.WriteAllBytes(temporary,package);
            Need(!EarthLocalTerrainElevationDataset.TryLoad(temporary,out var error)&&error.Contains("height range",StringComparison.Ordinal)&&!EarthLocalTerrainElevationDataset.IsLoaded,"overflowing source-float residual delta refuses without partial snapshot publication");
        }
        finally{File.Delete(temporary);}
        var path=Path.Combine(GraphicsTestHarness.RepositoryPath(),"tests/NovaCore.Graphics.Tests/fixtures/contact-angles.json");
        using var json=JsonDocument.Parse(File.ReadAllBytes(path));
        foreach(var row in json.RootElement.EnumerateArray())
        {
            double Read(string field)=>BitConverter.Int64BitsToDouble(unchecked((long)ulong.Parse(row.GetProperty(field).GetString()!,NumberStyles.HexNumber,CultureInfo.InvariantCulture)));
            var x=Read("x");var y=Read("y");var kind=row.GetProperty("kind").GetString();
            var enclosure=kind switch{"acos"=>PhysicalCollisionAngles.Acos(x),"atan"=>PhysicalCollisionAngles.Atan(x),_=>PhysicalCollisionAngles.Atan2(y,x)};
            var reference=Rational.Decimal(row.GetProperty("value").GetString()!);
            var uncertainty=reference.N.IsZero?Rational.Zero:new Rational(1,BigInteger.Pow(10,78));
            Need(Rational.Of(enclosure.Low)<=reference-uncertainty&&reference+uncertainty<=Rational.Of(enclosure.High),$"Independent 100-digit {kind} enclosure for ({x:R},{y:R})");
            var actual=kind switch{"acos"=>Math.Acos(x),"atan"=>Math.Atan(x),_=>Math.Atan2(y,x)};
            PhysicalCollisionAngles.Validate(actual,enclosure);count++;
            Refuse(()=>PhysicalCollisionAngles.Validate(Math.BitIncrement(enclosure.High),enclosure),"angle outside proved enclosure admitted");
        }
        foreach(var bad in new[]{double.NaN,double.PositiveInfinity,double.NegativeInfinity})
            Refuse(()=>PhysicalCollisionAngles.Validate(bad,new(-1,1)),"nonfinite angle admitted");
        Refuse(()=>PhysicalCollisionAngles.Validate(0,new(-1,1)),"uncertain angle enclosure admitted");
        var operands=new[]{0d,Math.ScaleB(1,-600),Math.ScaleB(1,400),-1d,.1,-.3,6371008.8,1e-14,Math.BitIncrement(1d)};
        foreach(var a in operands)foreach(var b in operands)
        {
            CollisionFinite aa=new(a),bb=new(b);
            void Check(double actual,CollisionFinite bound,Rational exact)
            {
                Need(Rational.Of(bound.I.Low)<=exact&&exact<=Rational.Of(bound.I.High),"ideal scalar interval encloses exact rational result");
                Need((Rational.Of(actual)-exact).Abs()<=Rational.Of(bound.E),"source-order error encloses exact rational discrepancy");
            }
            Check(a+b,aa+bb,Rational.Of(a)+Rational.Of(b));
            Check(a-b,aa-bb,Rational.Of(a)-Rational.Of(b));
            Check(a*b,aa*bb,Rational.Of(a)*Rational.Of(b));
            if(b!=0)Check(a/b,aa/bb,Rational.Of(a)/Rational.Of(b));
        }
        Refuse(()=>_=((CollisionFinite)1)/new CollisionFinite(new(-1,1)),"uncertain denominator admitted");
        Refuse(()=>_=new CollisionFinite(new(0,1),.1).Sqrt(),"uncertain square root admitted");
        foreach(var x in new[]{0d,Math.BitDecrement(.01),.01,Math.BitIncrement(.01),-.01,100d,-100d})foreach(var uncertainty in new[]{0d,1e-8})
        {
            var bound=new CollisionFinite(x,uncertainty).HypotConstant(.01);
            var exact=Rational.Of(x)*Rational.Of(x)+Rational.Of(.01)*Rational.Of(.01);
            Need(Rational.Of(bound.I.Low)*Rational.Of(bound.I.Low)<=exact&&exact<=Rational.Of(bound.I.High)*Rational.Of(bound.I.High),"ridge ideal squared enclosure");
            foreach(var moved in new[]{x-uncertainty,x+uncertainty})
            {
                var actual=Math.Sqrt(moved*moved+.01*.01);
                var low=Rational.Of(actual)-Rational.Of(bound.E);var high=Rational.Of(actual)+Rational.Of(bound.E);
                Need(low*low<=exact&&exact<=high*high,"ridge finite source discrepancy independently squared");
            }
        }
        var variable=CollisionJet.Variable(new(-.1,.1),true);
        foreach(var joined in new[]{variable.Abs(),variable.MaxZero(),(variable+.95).ClampUnit(),CollisionJet.Minimum(variable,-variable)})
        {
            var withAffine=joined+1000000*variable+100*variable.Square();
            Need(withAffine.GradientVariation.Constant<=joined.GradientVariation.Constant+1e-10,"smooth affine/quadratic does not acquire a slope jump");
            Need(withAffine.GradientVariation.Slope>=200,"smooth quadratic curvature retained at nonsmooth join");
        }
        CollisionJet tiny=Math.ScaleB(1,-54),one=1;
        var exactEndpoint=one+(tiny-one)*(CollisionJet)1;
        Need(exactEndpoint.V.Low<=Math.ScaleB(1,-54)&&exactEndpoint.V.High>=Math.ScaleB(1,-54),"raster coefficients promoted before subtracting rounded corners");
        foreach(var minimum in new[]{-1f,-12345.67f,0f})foreach(var maximum in new[]{.00001f,1f,98765.43f})
        {
            var cap=EarthLocalTerrainElevationDataset.CollisionResidualUpperBound(minimum,maximum);
            foreach(var fraction in new[]{0d,.5,Math.BitDecrement(1d),1d})
                Need(minimum+fraction*(maximum-minimum)<=cap,"regional upper bound follows finite float range subtraction");
        }
        Refuse(()=>EarthLocalTerrainElevationDataset.CollisionResidualUpperBound(-float.MaxValue,float.MaxValue),"overflowing stored float delta admitted");
        Refuse(()=>EarthLocalTerrainElevationDataset.CollisionResidualUpperBound(1,0),"reversed regional range admitted");
        var radius=PlanetaryPhysicalSurface.EarthReferenceRadiusMetres;
        foreach(var face in Enum.GetValues<CubeSphereFace>())foreach(var uv in new[]{(.27,.63),(.5,.5),(.73,.37)})
        {
            var direction=RelaxedCubeSphereProjection.UnitDirection(face,uv.Item1,uv.Item2);
            var ray=new CollisionVector(direction.X,direction.Y,direction.Z);
            Need(PhysicalCollisionAddressBounds.TryRegional(ray,out var certifiedFace,out var u,out var v,out var error,out var reason),$"six-face numerical inverse: {face}/{uv}: {reason}");
            Need(RelaxedCubeSphereProjection.TryAddress(direction.Normalized(),out var actualFace,out var actualU,out var actualV)&&actualFace==certifiedFace&&
                actualU>=u.V.Low-error&&actualU<=u.V.High+error&&actualV>=v.V.Low-error&&actualV<=v.V.High+error,"source Newton address enclosed across axis permutations");
        }
        foreach(var scale in new[]{Math.Sqrt(1-5e-13),1d,Math.Sqrt(1+5e-13)})foreach(var position in new[]{-PhysicalCollisionFrame.MaximumCoordinate,0,PhysicalCollisionFrame.MaximumCoordinate})
        {
            var frame=new PhysicalCollisionFrame(Double3.UnitX*scale,Double3.UnitY*scale,Double3.UnitZ*scale,radius);
            Need(frame.Valid,"extremal legal numerical frame");
            var budget=new PhysicalCollisionEvaluationBudget(frame,position,-position);
            var local=PlanetaryNaturalTerrainFamilies.CollisionNumerics(budget.Second*(CollisionFinite)radius);
            Need(budget.Natural.Macro.E>=local.Macro.E&&budget.Natural.Meso.E>=local.Meso.E&&budget.Natural.Near.E>=local.Near.E,"whole-domain numerical budget dominates derived local frame budget");
        }
        Console.WriteLine($"PHYSICAL_COLLISION_NUMERICS_PASS checks={count} angleAdmission={PhysicalCollisionAngles.MaximumAdmittedError:R}");
    }
    private readonly record struct Rational(BigInteger N,BigInteger D)
    {
        internal static Rational Zero=>new(0,1);
        internal static Rational Of(double value)
        {
            if(!double.IsFinite(value))throw new InvalidDataException("Nonfinite rational witness");
            var bits=BitConverter.DoubleToUInt64Bits(value);var exponent=(int)((bits>>52)&2047);var fraction=bits&0xfffffffffffff;
            var n=new BigInteger(exponent==0?fraction:fraction|(1UL<<52));var shift=exponent==0?-1074:exponent-1023-52;
            if((bits>>63)!=0)n=-n;return shift>=0?new(n<<shift,1):new(n,BigInteger.One<<-shift);
        }
        internal static Rational Decimal(string value){var parts=value.Split('.');return new(BigInteger.Parse(parts[0]+parts[1],CultureInfo.InvariantCulture),BigInteger.Pow(10,parts[1].Length));}
        internal Rational Abs()=>new(BigInteger.Abs(N),D);
        public static Rational operator +(Rational a,Rational b)=>new(a.N*b.D+b.N*a.D,a.D*b.D);
        public static Rational operator -(Rational a,Rational b)=>new(a.N*b.D-b.N*a.D,a.D*b.D);
        public static Rational operator *(Rational a,Rational b)=>new(a.N*b.N,a.D*b.D);
        public static Rational operator /(Rational a,Rational b)=>b.N.Sign<0?new(-a.N*b.D,-a.D*b.N):new(a.N*b.D,a.D*b.N);
        public static bool operator <=(Rational a,Rational b)=>a.N*b.D<=b.N*a.D;
        public static bool operator >=(Rational a,Rational b)=>a.N*b.D>=b.N*a.D;
    }
}
