using System.Numerics;
using System.Text;
using System.Text.Json.Nodes;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;

internal static partial class ModularCraftTests
{
    // Exact rational arithmetic is independent of the production FP64 interval operations.
    private readonly record struct OracleRational(BigInteger N,BigInteger D)
    {
        private static OracleRational Reduce(BigInteger n,BigInteger d)
        {var g=BigInteger.GreatestCommonDivisor(n,d);return new(n/g,d/g);}
        internal static OracleRational Binary(double value)
        {
            var bits=BitConverter.DoubleToUInt64Bits(value);var exponent=(int)((bits>>52)&2047);
            var mantissa=new BigInteger(bits&0xfffffffffffffUL);if(exponent!=0)mantissa+=BigInteger.One<<52;
            if((bits>>63)!=0)mantissa=-mantissa;
            var power=exponent==0?-1074:exponent-1023-52;
            return power>=0?new(mantissa<<power,BigInteger.One):Reduce(mantissa,BigInteger.One<<-power);
        }
        public static OracleRational operator +(OracleRational a,OracleRational b)=>Reduce(a.N*b.D+b.N*a.D,a.D*b.D);
        public static OracleRational operator -(OracleRational a,OracleRational b)=>Reduce(a.N*b.D-b.N*a.D,a.D*b.D);
        public static OracleRational operator *(OracleRational a,OracleRational b)=>Reduce(a.N*b.N,a.D*b.D);
        public static OracleRational operator /(OracleRational a,int b)=>Reduce(a.N,a.D*b);
        public static OracleRational operator /(OracleRational a,OracleRational b)=>Reduce(a.N*b.D,a.D*b.N);
        private int Compare(OracleRational b)=>(N*b.D).CompareTo(b.N*D);
        internal double Round()
        {
            if(N.IsZero)return 0;
            if(N.Sign<0)return -new OracleRational(-N,D).Round();
            if(Compare(Binary(double.MaxValue))>0)throw new InvalidDataException("Oracle range");
            // Search the ordered positive IEEE encoding: no approximate floating quotient,
            // shared production scaling, or cancellation decides the reference rounding.
            ulong low=0,high=0x7fefffffffffffff;
            while(low<high){var middle=low+(high-low+1)/2;
                if(Binary(BitConverter.UInt64BitsToDouble(middle)).Compare(this)<=0)low=middle;else high=middle-1;}
            var guess=BitConverter.UInt64BitsToDouble(low);
            if(guess==double.MaxValue)return guess;
            var next=Math.BitIncrement(guess);var midpoint=(Binary(guess)+Binary(next))/2;var cmp=Compare(midpoint);
            return cmp<0||cmp==0&&(BitConverter.DoubleToUInt64Bits(guess)&1)==0?guess:next;
        }
    }
    private static PartDefinitionData AnalyticStore(double inner,double outer,double length,double density)
    {
        var d=DefinitionFixture();var ri=OracleRational.Binary(inner);var ro=OracleRational.Binary(outer);var l=OracleRational.Binary(length);
        // A 50-decimal enclosure of pi, using the unfactored exact difference-of-squares formula.
        var pi=new OracleRational(BigInteger.Parse("314159265358979323846264338327950288419716939937510"),BigInteger.Pow(10,50));
        var piUpper=new OracleRational(pi.N+1,pi.D);
        var volume=pi*(ro*ro-ri*ri)*l;var volumeUpper=piUpper*(ro*ro-ri*ri)*l;
        Check(volume.Round()==volumeUpper.Round(),"independent pi enclosure determines rounded volume");
        var mass=volume*OracleRational.Binary(density);var massUpper=volumeUpper*OracleRational.Binary(density);
        Check(mass.Round()==massUpper.Round(),"independent pi enclosure determines rounded mass");
        var radial=ro*ro+ri*ri;var xx=(radial/2).Round();var transverse=(radial/4+l*l/12).Round();
        return d with {Stores=[d.Stores[0] with {CapacityKg=mass.Round()}],Construction=d.Construction! with {
            StoreGeometry=[new("store",volume.Round(),Matrix3.Diagonal(new(xx,transverse,transverse)))]},
            Standard=d.Standard! with {StoreLaws=[new("store",StoreDepletionLaw.ProportionalSpatial,density,inner,outer,length,"Independent exact analytic oracle")]}};
    }
    internal static void PartStandardNumerics()
    {
        checks=0;
        foreach(var k in new[]{-150,-100,-50,0,50,100,150})foreach(var annulus in new[]{false,true})
            foreach(var density in new[]{.001,1000,1e9})
            {
                var scale=Math.ScaleB(1,k);var d=AnalyticStore(annulus?.25*scale:0,.5*scale,scale,density);
                var c=Catalog(d);Check(c.Digest==AssemblyDefinitionCatalog.Load(c.Save()).Digest,"scale-independent law roundtrip");
                var s=d.Stores[0];Reject(()=>Catalog(d with {Stores=[s with {CapacityKg=s.CapacityKg*1.001}]}),"fractional capacity mismatch at any scale");
                var g=d.Construction!.StoreGeometry[0];
                Reject(()=>Catalog(d with {Construction=d.Construction with {StoreGeometry=[g with {InertiaPerKg=g.InertiaPerKg*1.001}]}}),"fractional tensor mismatch at any scale");
            }
        foreach(var inner in new[]{0d,.25,Math.BitDecrement(1d)})
            Check(Catalog(AnalyticStore(inner,1,1,1000)).Data.Definitions.Length==1,"cylinder and one-ulp thin annulus");
        foreach(var longTank in new[]{false,true})foreach(var outerStore in new[]{false,true})
        {
            var volume=longTank?.64:.32;var radius=Math.Sqrt(.125);var length=volume/(Math.PI*.125);
            var d=AnalyticStore(outerStore?radius:0,outerStore?.5:radius,length,outerStore?1500:1000);
            var g=d.Construction!.StoreGeometry[0];
            Check(Catalog(d with {Stores=[d.Stores[0] with {CapacityKg=volume*(outerStore?1500:1000)}],
                Construction=d.Construction with {StoreGeometry=[g with {UsableVolumeM3=volume}]}}).Data.Definitions.Length==1,"full precision planned .32/.64 store dimensions");
        }
        var fixture=DefinitionFixture();var law=fixture.Standard!.StoreLaws[0];
        foreach(var bad in new[]{law with {DensityKgM3=double.MaxValue},law with {DensityKgM3=double.Epsilon},
            law with {OuterRadiusM=double.Epsilon},law with {LengthM=double.Epsilon},law with {LengthM=double.MaxValue}})
            RefuseAt(()=>Catalog(fixture with {Standard=fixture.Standard with {StoreLaws=[bad]}}),"storeLaws[store]");
        // Coherent preceding metadata forces these failures to reach the named arithmetic boundary.
        PartDefinitionData Boundary(double outer,double length,double density,double volume,double capacity)=>fixture with {
            Stores=[fixture.Stores[0] with {CapacityKg=capacity}],
            Construction=fixture.Construction! with {StoreGeometry=[fixture.Construction.StoreGeometry[0] with {UsableVolumeM3=volume}]},
            Standard=fixture.Standard with {StoreLaws=[law with {OuterRadiusM=outer,LengthM=length,DensityKgM3=density}]}};
        foreach(var edge in new[]{
            Boundary(1e154,1,1000,1,1),
            Boundary(Math.ScaleB(1,-300),Math.ScaleB(1,-500),1000,1,1),
            Boundary(1,1,double.MaxValue,Math.PI,1),
            Boundary(Math.ScaleB(1,-300),Math.ScaleB(1,600),1,Math.PI,Math.PI),
            Boundary(1,Math.ScaleB(1,-600),1,Math.ScaleB(Math.PI,-600),Math.ScaleB(Math.PI,-600)),
            Boundary(.5,4/Math.PI,Math.ScaleB(1,-1022),1,Math.ScaleB(1,-1022))})
            RefuseAt(()=>Catalog(edge),"physical-law interval overflow, underflow or collapse");
        foreach(var density in new[]{Math.ScaleB(1,-1021),Math.ScaleB(1,1023)})
        {
            var length=4/Math.PI;var k=.0625+length*length/12;
            var edge=Boundary(.5,length,density,1,density);
            edge=edge with {Construction=edge.Construction! with {StoreGeometry=[new("store",1,Matrix3.Diagonal(new(.125,k,k)))]}};
            Check(Catalog(edge).Data.Definitions.Length==1,"near-boundary normal capacity admitted");
        }
        var small=AnalyticStore(0,1e-5,3e-5,1000);var geometry=small.Construction!.StoreGeometry[0];
        Reject(()=>Catalog(small with {Construction=small.Construction with {StoreGeometry=[geometry with {InertiaPerKg=geometry.InertiaPerKg*.5}]}}),"former absolute tensor tolerance floor");
        Reject(()=>Catalog(small with {Construction=small.Construction with {StoreGeometry=[geometry with {InertiaPerKg=geometry.InertiaPerKg with {B=1e-16,D=1e-16}}]}}),"analytic cylinder offdiagonal is exact zero");
        var exact=AnalyticStore(0,Math.Sqrt(.125),.32/(Math.PI*.125),1000);
        Reject(()=>Catalog(exact with {Standard=exact.Standard! with {StoreLaws=[exact.Standard.StoreLaws[0] with {LengthM=.815}]}}),"prose-rounded geometry is not exact .32 volume");
        Console.WriteLine($"Modular Gate 1 numerical admission PASS: {checks} independent-rational/scale/boundary checks");
    }
    internal static void PartStandardJsonRefusal()
    {
        checks=0;var bytes=Catalog(SocketFixture()).Save();
        foreach(var vector in new[]{"null","[]","false","0","{}","{\"x\":0,\"y\":0,\"bogus\":0}",
            "{\"x\":0,\"y\":0,\"z\":null}","{\"x\":0,\"y\":false,\"z\":0}","{\"x\":0,\"y\":0,\"z\":\"1\"}",
            "{\"x\":1e999,\"y\":0,\"z\":0}","{\"x\":0,\"y\":0,\"z\":0,\"w\":0}"})
        {
            var node=JsonNode.Parse(bytes)!;node["definitions"]![0]!["standard"]!["socketGroups"]![0]!["axis"]!["position"]=JsonNode.Parse(vector);
            RefuseAt(()=>AssemblyDefinitionCatalog.Load(Encoding.UTF8.GetBytes(node.ToJsonString())),"definitions[0].standard.socketGroups[0].axis.position");
        }
        foreach(var value in new[]{"null","false","\"1\"","1e999"})
        {
            var node=JsonNode.Parse(bytes)!;node["definitions"]![0]!["dryMassKg"]=JsonNode.Parse(value);
            RefuseAt(()=>AssemblyDefinitionCatalog.Load(Encoding.UTF8.GetBytes(node.ToJsonString())),"definitions[0].dryMassKg");
        }
        foreach(var value in new[]{"null","[]","{\"x\":0,\"y\":0,\"z\":0,\"bogus\":1}","{\"x\":0,\"y\":0,\"z\":0,\"w\":true}"})
            Reject(()=>AssemblyJson.Read<DoubleQuaternion>(Encoding.UTF8.GetBytes(value)),"malformed shared quaternion components");
        Check(Catalog(SocketFixture()).Save().SequenceEqual(bytes),"JSON refusal preserves source and deterministic serialization");
        Console.WriteLine($"Modular Gate 1 JSON refusal PASS: {checks} checks");
    }
}
