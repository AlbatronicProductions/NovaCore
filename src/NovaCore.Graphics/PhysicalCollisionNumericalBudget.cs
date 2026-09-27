using NovaCore.Core;
using NovaCore.Core.Surface;

namespace NovaCore.Graphics;

// Preparation-only forward error arithmetic. I encloses the ideal real
// expression with the stored binary64 constants; E bounds the finite source
// expression's absolute error. Error calculations themselves round outwards.
internal readonly record struct CollisionFinite(CollisionRange I,double E=0,int Rounds=1)
{
    internal double M=>I.Magnitude;
    internal static double Up(CollisionRange value)
    {
        if(!value.Finite||value.High<0)throw new InvalidDataException("Unresolved finite collision arithmetic.");
        return value.High;
    }
    internal static double Round(double magnitude)=>Up(((CollisionRange)Math.ScaleB(1d,-53))/(1-(CollisionRange)Math.ScaleB(1d,-53))*magnitude+double.Epsilon);
    public static implicit operator CollisionFinite(double value)=>new(value);
    public static CollisionFinite operator +(CollisionFinite a,CollisionFinite b)
    {
        var p=Up((CollisionRange)a.E+b.E);
        var rounds=Math.Max(a.Rounds,b.Rounds);
        return new(a.I+b.I,Up((CollisionRange)p+rounds*(CollisionRange)Round(Up((CollisionRange)a.M+b.M+p))),rounds);
    }
    public static CollisionFinite operator -(CollisionFinite a)=>new(-a.I,a.E,a.Rounds);
    public static CollisionFinite operator -(CollisionFinite a,CollisionFinite b)=>a+-b;
    public static CollisionFinite operator *(CollisionFinite a,CollisionFinite b)
    {
        var p=(CollisionRange)a.M*b.E+(CollisionRange)b.M*a.E+(CollisionRange)a.E*b.E;
        var rounds=Math.Max(a.Rounds,b.Rounds);
        return new(a.I*b.I,Up(p+rounds*(CollisionRange)Round(Up(((CollisionRange)a.M+a.E)*((CollisionRange)b.M+b.E)))),rounds);
    }
    public static CollisionFinite operator /(CollisionFinite a,CollisionFinite b)
    {
        var minimum=b.I.Low>0?b.I.Low:b.I.High<0?-b.I.High:0;
        if(minimum<=b.E)throw new InvalidDataException("Unresolved finite collision denominator.");
        var p=((CollisionRange)a.E+((CollisionRange)a.M/minimum)*b.E)/((CollisionRange)minimum-b.E);
        var rounds=Math.Max(a.Rounds,b.Rounds);
        return new(a.I/b.I,Up(p+rounds*(CollisionRange)Round(Up(((CollisionRange)a.M+a.E)/((CollisionRange)minimum-b.E)))),rounds);
    }
    internal CollisionFinite Square()
    {
        var p=2*(CollisionRange)M*E+((CollisionRange)E).Square();
        return new(I.Square().Clip(0,double.MaxValue),Up(p+Rounds*(CollisionRange)Round(Up(((CollisionRange)M+E).Square()))),Rounds);
    }
    internal CollisionFinite Sqrt()
    {
        if(I.Low<=E)throw new InvalidDataException("Unresolved finite collision square root.");
        var p=(CollisionRange)E/(((CollisionRange)I.Low).Sqrt()+((CollisionRange)I.Low-E).Sqrt());
        return new(I.Sqrt(),Up(p+Rounds*(CollisionRange)Round(Up(((CollisionRange)M+E).Sqrt()))),Rounds);
    }
    internal CollisionFinite HypotConstant(double epsilon)
    {
        if(!double.IsFinite(epsilon)||epsilon<=0)throw new InvalidDataException("Invalid smooth-ridge regularizer.");
        // sqrt(x*x+e*e) is one-Lipschitz in x. Its positive products and
        // sum have relative error <= gamma(2); retain that correlation instead
        // of dividing an unrelated max-|x| square error by minimum e.
        CollisionRange u=Math.ScaleB(1d,-53);var gamma=2*u/(1-2*u);
        var ideal=(I.Square()+((CollisionRange)epsilon).Square()).Sqrt();
        var maximum=(((CollisionRange)M+E).Square()+((CollisionRange)epsilon).Square()).Sqrt();
        var round=(gamma*maximum/(1+(1-gamma).Sqrt())+Round((maximum*(1+gamma).Sqrt()).High)+3*(CollisionRange)double.Epsilon/epsilon).High;
        return new(ideal,Up((CollisionRange)E+round));
    }
    internal CollisionFinite Tighten(CollisionRange ideal)=>new(ideal,E,Rounds);
    internal CollisionFinite Extra(double error)=>new(I,Up((CollisionRange)E+error),Rounds);
    internal CollisionFinite MaxZero()=>new(new(Math.Max(0,I.Low),Math.Max(0,I.High)),E,Rounds);
    internal CollisionFinite Abs()=>new(I.Low>=0?I:I.High<=0?-I:new(0,I.Magnitude),E,Rounds);
    internal static CollisionFinite Fade(CollisionFinite value)
    {
        // The clamped quintic is globally 15/8 Lipschitz, including joins.
        // Trace the polynomial's arithmetic at the finite clamped input;
        // propagate the input discrepancy independently through that function.
        CollisionFinite t=new(new(0,1));
        var polynomial=t*t*t*(t*(t*6-15)+10);
        return new(new(0,1),Up((CollisionRange)polynomial.E+1.875*(CollisionRange)value.E));
    }
    internal static CollisionFinite Lerp(CollisionFinite a,CollisionFinite b,CollisionFinite t)=>a+(b-a)*t;
}

internal readonly record struct CollisionFiniteVector(CollisionFinite X,CollisionFinite Y,CollisionFinite Z)
{
    public static implicit operator CollisionFiniteVector(Double3 value)=>new(value.X,value.Y,value.Z);
    public static CollisionFiniteVector operator +(CollisionFiniteVector a,CollisionFiniteVector b)=>new(a.X+b.X,a.Y+b.Y,a.Z+b.Z);
    public static CollisionFiniteVector operator *(CollisionFiniteVector a,CollisionFinite b)=>new(a.X*b,a.Y*b,a.Z*b);
    public static CollisionFiniteVector operator /(CollisionFiniteVector a,CollisionFinite b)=>new(a.X/b,a.Y/b,a.Z/b);
    internal CollisionFinite Dot(Double3 b)=>X*b.X+Y*b.Y+Z*b.Z;
    internal CollisionFiniteVector Normalized()=>this/(X.Square()+Y.Square()+Z.Square()).Sqrt();
    internal double Error=>CollisionRange.UpperNorm(X.E,Y.E,Z.E);
}

internal readonly record struct NaturalCollisionNumerics(CollisionFinite Macro,CollisionFinite Meso,CollisionFinite Near,
    double ControlError,double[] RawMesoErrors);

internal sealed class PhysicalCollisionEvaluationBudget
{
    private static readonly NaturalCollisionNumerics WholeDomainNatural=NaturalDomainBudget();
    internal CollisionFiniteVector Direction {get;}
    internal CollisionFiniteVector Second {get;}
    internal NaturalCollisionNumerics Natural {get;}
    private readonly double radius;
    internal PhysicalCollisionEvaluationBudget(PhysicalCollisionFrame frame,CollisionRange x,CollisionRange y)
    {
        radius=frame.Radius;
        if(radius!=PlanetaryPhysicalSurface.EarthReferenceRadiusMetres||x.Magnitude>PhysicalCollisionFrame.MaximumCoordinate||y.Magnitude>PhysicalCollisionFrame.MaximumCoordinate)
            throw new InvalidDataException("Collision arithmetic leaves its derived patch domain.");
        var ray=(CollisionFiniteVector)frame.Radial*(CollisionFinite)radius+(CollisionFiniteVector)frame.East*new CollisionFinite(x)+(CollisionFiniteVector)frame.North*new CollisionFinite(y);
        Direction=ray.Normalized();Second=Direction.Normalized();
        Natural=WholeDomainNatural;
    }
    private static NaturalCollisionNumerics NaturalDomainBudget()
    {
        CollisionRange u=Math.ScaleB(1d,-53);var gamma=3*u/(1-3*u);
        // Valid checks a three-term rounded squared norm against1±1e-12.
        // Recover a bound on the real norm before using the triangle inequality.
        var minimum=((1-(CollisionRange)1e-12)/(1+gamma)).Sqrt();
        var maximum=((1+(CollisionRange)1e-12)/(1-gamma)).Sqrt();
        var radius=PlanetaryPhysicalSurface.EarthReferenceRadiusMetres;var extent=PhysicalCollisionFrame.MaximumCoordinate;
        CollisionFinite axis=new(new(-maximum.High,maximum.High)),coordinate=new(new(-extent,extent));
        var ray=axis*radius+axis*coordinate+axis*coordinate;
        var low=(radius*minimum-2*extent*maximum).Low;var high=(radius*maximum+2*extent*maximum).High;
        var normSquared=(ray.Square()+ray.Square()+ray.Square()).Tighten(new(((CollisionRange)low).Square().Low,((CollisionRange)high).Square().High));
        var first=(ray/normSquared.Sqrt()).Tighten(new(-1,1));
        var second=(first/(first.Square()+first.Square()+first.Square()).Tighten(new(1,1)).Sqrt()).Tighten(new(-1,1));
        var body=new CollisionFinite(new(-1,1),Math.Max(first.E,second.E))*radius;
        return PlanetaryNaturalTerrainFamilies.CollisionNumerics(new(body,body,body));
    }
    internal double FullPlane()
    {
        var region=FloridaFacilitySupport.Region;var cos=Direction.Dot(region.Up);
        var x=Direction.Dot(region.East)*radius/cos;var y=Direction.Dot(region.North)*radius/cos;
        if(CollisionFinite.Up((CollisionRange)x.M+x.E)>region.InnerEastMetres||CollisionFinite.Up((CollisionRange)y.M+y.E)>region.InnerNorthMetres)
            throw new InvalidDataException("Full-weight grading is not uniform over finite source arithmetic.");
        var plane=((CollisionFinite)radius+region.PlaneAltitudeMetres)/cos-radius;
        // The finite natural base cancels exactly in the ideal B+(P-B)
        // expression. Only its magnitude and the two operation errors matter.
        var cap=CollisionFinite.Up((CollisionRange)11000+EarthLocalTerrainElevationDataset.ResidualUpperBound+Natural.Macro.M+Natural.Meso.M+Natural.Macro.E+Natural.Meso.E);
        var subtraction=CollisionFinite.Round(CollisionFinite.Up((CollisionRange)cap+plane.M+plane.E));
        var addition=CollisionFinite.Round(CollisionFinite.Up((CollisionRange)plane.M+plane.E+subtraction));
        return PointError(plane.Extra(CollisionFinite.Up((CollisionRange)subtraction+addition)));
    }
    internal (CollisionFinite X,CollisionFinite Y) GeographicAddresses(CollisionRange longitude)
    {
        var third=Second.Normalized();
        static double MinAbs(CollisionRange a)=>a.Low>0?a.Low:a.High<0?-a.High:0;
        var horizontal=(((CollisionRange)MinAbs(third.X.I)).Square()+((CollisionRange)MinAbs(third.Z.I)).Square()).Sqrt().Low;
        var ed=CollisionRange.UpperNorm(third.X.E,third.Z.E);
        if(horizontal<=ed)throw new InvalidDataException("Unresolved longitude conditioning.");
        var lonError=CollisionFinite.Up(2*(CollisionRange)ed/((CollisionRange)horizontal-ed)+PhysicalCollisionAngles.MaximumAdmittedError);
        var yy=(CollisionRange)Second.Y.M+Second.Y.E;var denominator=(1-yy.Square()).Sqrt().Low;
        if(denominator<=0)throw new InvalidDataException("Unresolved latitude conditioning.");
        var latError=CollisionFinite.Up((CollisionRange)Second.Y.E/denominator+PhysicalCollisionAngles.MaximumAdmittedError);
        var px=(new CollisionFinite(longitude,lonError)/Math.Tau+.5).Tighten(new(0,1)).Extra(CollisionFinite.Round(1))*EarthElevationDataset.Width-.5;
        var py=new CollisionFinite(new(0,PhysicalCollisionAngles.Pi.High),latError)/Math.PI*EarthElevationDataset.Height-.5;
        return(px,py);
    }
    internal double Complete((CollisionFinite X,CollisionFinite Y) address,CollisionRange global,CollisionRange residual,double regionalError,
        CollisionRange idealWeight,CollisionRange idealPlane)
    {
        CollisionFinite corner=new(new(EarthElevationDataset.MinimumElevationMetres,EarthElevationDataset.MaximumElevationMetres));
        CollisionFinite fraction=new(new(0,1),CollisionFinite.Round(1));
        var row=CollisionFinite.Lerp(corner,corner,fraction).Tighten(corner.I);
        var bilinear=CollisionFinite.Lerp(row,row,fraction).Tighten(corner.I);
        var addressError=CollisionFinite.Up((EarthElevationDataset.MaximumElevationMetres-(CollisionRange)EarthElevationDataset.MinimumElevationMetres)*((CollisionRange)address.X.E+address.Y.E));
        var geography=(new CollisionFinite(global,bilinear.E).Extra(addressError)+new CollisionFinite(residual,regionalError)).MaxZero();
        var baseHeight=(geography+Natural.Macro+Natural.Meso).MaxZero();
        var region=FloridaFacilitySupport.Region;var cos=Direction.Dot(region.Up);
        var east=Direction.Dot(region.East)*radius/cos;var north=Direction.Dot(region.North)*radius/cos;
        CollisionFinite Weight(CollisionFinite q,double inner)=>1-CollisionFinite.Fade((q.Abs()-inner)/region.BlendMetres);
        var weight=(Weight(east,region.InnerEastMetres)*Weight(north,region.InnerNorthMetres)).Tighten(idealWeight);
        var plane=((CollisionFinite)radius+region.PlaneAltitudeMetres)/cos-radius;
        if(idealWeight.High>0)plane=plane.Tighten(idealPlane);
        // Carry finite branch uncertainty even for ideal weight0: rounding
        // can move a coordinate across the compact grading edge. The source's
        // zero-weight early return is included in this error enclosure.
        var adapted=baseHeight+(plane-baseHeight)*weight;
        var height=(adapted+(1-weight)*Natural.Near).MaxZero();
        return PointError(height);
    }
    private double PointError(CollisionFinite height)
    {
        var point=Direction*((CollisionFinite)radius+height);
        return CollisionFinite.Up(2*(CollisionRange)point.Error);
    }
}

public static partial class PlanetaryNaturalTerrainFamilies
{
    private static readonly double CollisionNoiseRound=NoiseRound();
    private static double NoiseRound()
    {
        var s=Math.BitIncrement(PlanetaryNaturalTerrainField.MaximumUnitValue);
        CollisionFinite fraction=new(new(0,1),CollisionFinite.Round(1));
        var fade=fraction*fraction*fraction*(fraction*(fraction*6-15)+10);
        var complement=1-fade;
        CollisionFinite weight=new(new(0,1),Math.Max(fade.E,complement.E));
        var offset=fraction-1;
        CollisionFinite o=new(new(-1,1),Math.Max(fraction.E,offset.E));
        // SelectGradient chooses exact signed stored coefficients. Multiplying
        // the stored inverse sqrt(5) by two is exact in binary arithmetic.
        var g=PlanetaryNaturalTerrainField.SelectGradient(0).Y;
        var coefficient=Math.Abs(g);
        CollisionFinite gradient=new(new(-coefficient,coefficient));
        var corner=(gradient*o+gradient*o+gradient*o).Tighten(new(-s,s));
        var w=weight*weight*weight;CollisionFinite sum=0;
        for(var i=0;i<8;i++)sum=(sum+w*corner).Tighten(new(-s,s));
        return sum.E;
    }
    internal static NaturalCollisionNumerics CollisionNumerics(CollisionFiniteVector point)
    {
        var s=Math.BitIncrement(PlanetaryNaturalTerrainField.MaximumUnitValue);
        CollisionFinite Noise(CollisionFiniteVector p,double cell,double amplitude)
        {
            var q=p/(CollisionFinite)cell;
            var error=CollisionFinite.Up((CollisionRange)CollisionNoiseRound+PlanetaryNaturalTerrainField.MaximumUnitGradient*(CollisionRange)q.Error);
            return new CollisionFinite(new(-s,s),error)*amplitude;
        }
        var x=Noise(point,DomainWarpControlCellSizeMetres,1);var y=Noise(point,DomainWarpControlCellSizeMetres,1);
        var z=x*y*((CollisionFinite)1/PlanetaryNaturalTerrainField.MaximumUnitValue);
        var length=(x.Square()+y.Square()+z.Square()+(CollisionFinite)OrientationRegularizer*OrientationRegularizer).Sqrt();
        var inverse=(CollisionFinite)1/length;
        var controls=new CollisionFiniteVector(x,y,z);var orientation=controls*inverse;
        var coordinate=CollisionFinite.Fade((x/PlanetaryNaturalTerrainField.MaximumUnitValue+.42)/.84)*6;
        var rawMeso=new double[7];CollisionFinite macro=0,meso=0,near=0;
        static CollisionFinite Union(CollisionFinite a,CollisionFinite b)=>new(a.I.Hull(b.I),Math.Max(a.E,b.E));
        for(var i=0;i<7;i++)
        {
            var c=Configuration((PlanetaryNaturalTerrainFamily)(i+1));
            CollisionFinite Scale(double cell,double amplitude)
            {
                var baseScale=(CollisionFinite)WarpMagnitudeFraction*cell/3;
                var warped=point+controls*baseScale;
                if(c.Anisotropy!=0)warped+=orientation*(x*((CollisionFinite)c.Anisotropy*cell/PlanetaryNaturalTerrainField.MaximumUnitValue));
                return Noise(warped,cell,amplitude);
            }
            var ma=Scale(c.MacroCell,c.MacroAmplitude);var me=Scale(c.MesoCell,c.MesoAmplitude);var ne=Scale(c.NearCell,c.NearAmplitude);
            rawMeso[i]=me.E;
            if(c.ShapeRidge!=0||c.ShapeLinear!=1)
            {
                var cap=CollisionFinite.Up((CollisionRange)me.M*(Math.Abs(c.ShapeLinear)+(CollisionRange)Math.Abs(c.ShapeRidge)));
                me=(c.ShapeLinear*me+c.ShapeRidge*(me.HypotConstant(.01)-.01)).Tighten(new(-cap,cap));
            }
            macro=Union(macro,ma);meso=Union(meso,me);near=Union(near,ne);
        }
        CollisionFinite Blend(CollisionFinite value)
        {
            // Family-index changes are continuous; the composite function is
            // Lipschitz in the biome coordinate across every integer join.
            CollisionFinite fraction=new(new(0,1),CollisionFinite.Round(1));
            var weight=CollisionFinite.Fade(fraction);
            return (value*(1-weight)+value*weight).Tighten(value.I).Extra(
                CollisionFinite.Up(2*(CollisionRange)value.M*1.875*coordinate.E));
        }
        return new(Blend(macro),Blend(meso),Blend(near),x.E,rawMeso);
    }
}
