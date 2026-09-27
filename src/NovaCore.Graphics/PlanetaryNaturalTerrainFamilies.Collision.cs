using NovaCore.Core;

namespace NovaCore.Graphics;

internal readonly record struct NaturalCollisionBounds(double Value,double Gradient,double Hessian);

public static partial class PlanetaryNaturalTerrainFamilies
{
    internal static (NaturalCollisionBounds Base,NaturalCollisionBounds Near) CollisionBounds(Double3 point,double radius,PlanetaryNaturalTerrainFamilyIdentity identity,NaturalCollisionNumerics numerics)
    {
        CollisionRange s=Math.BitIncrement(PlanetaryNaturalTerrainField.MaximumUnitValue);CollisionRange divisor=PlanetaryNaturalTerrainField.MaximumUnitValue;CollisionRange cell=DomainWarpControlCellSizeMetres;
        // |fade'|<=15/8, |fade''|<=10/sqrt(3). Tensor weight
        // differentiation gives ||noise Hessian|| <=20+45/sqrt(5)+225sqrt(3)/8 <90.
        const double primitiveHessian=90;
        var g=PlanetaryNaturalTerrainField.MaximumUnitGradient/cell;var h=primitiveHessian/(cell*cell);
        var zG=2*s*g/divisor;var jc=(2*g*g+zG*zG).Sqrt();var zH=2*s*h/divisor+2*g*g/divisor;var kc=(2*h*h+zH*zH).Sqrt();
        var jo=jc/OrientationRegularizer;var ko=kc/OrientationRegularizer+6*jc*jc/(OrientationRegularizer*OrientationRegularizer);
        const double p=1.875;var q=10/((CollisionRange)3).Sqrt();var sigma=.84*divisor;
        var gt=6*p*g/sigma;var ht=6*(p*h/sigma+q*g*g/(sigma*sigma));var gw=p*gt;var hw=p*ht+q*gt*gt;
        var controls=EvaluateWarpControls(point,identity);var orientation=RegularizedOrientation(controls);
        var control=((CollisionRange)controls.X.Height).Inflate((g*radius+numerics.ControlError).High);
        var t=((control/PlanetaryNaturalTerrainField.MaximumUnitValue+.42)/.84);
        t=new(Math.Clamp(t.Low,0,1),Math.Clamp(t.High,0,1));
        var blend=(6*t*t*t*(t*(t*6-15)+10));
        var low=Math.Clamp((int)Math.Floor(blend.Low),0,5);var high=Math.Clamp((int)Math.Floor(blend.High),0,5)+1;
        NaturalCollisionBounds baseBound=default,nearBound=default;
        for(var i=low;i<=high;i++)
        {
            var family=(PlanetaryNaturalTerrainFamily)(i+1);var c=Configuration(family);
            NaturalCollisionBounds Scale(double length,double amplitude)
            {
                var b=(CollisionRange)WarpMagnitudeFraction*length/3;var d=(CollisionRange)c.Anisotropy*length/divisor;
                var j=1+b*jc+d*(g+s*jo);var k=b*kc+d*(h+2*g*jo+s*ko);
                return new((s*amplitude).High,((CollisionRange)PlanetaryNaturalTerrainField.MaximumUnitGradient*amplitude/length*j).High,
                    (((CollisionRange)primitiveHessian*amplitude/((CollisionRange)length*length))*j*j+(CollisionRange)PlanetaryNaturalTerrainField.MaximumUnitGradient*amplitude/length*k).High);
            }
            var macro=Scale(c.MacroCell,c.MacroAmplitude);var meso=Scale(c.MesoCell,c.MesoAmplitude);var near=Scale(c.NearCell,c.NearAmplitude);
            var raw=EvaluateScale(point,family,1,c.MesoCell,c.MesoAmplitude,c,controls,orientation,identity);
            var minimum=Math.Max(0,((CollisionRange)Math.Abs(raw.Height)-(CollisionRange)meso.Gradient*radius-numerics.RawMesoErrors[i]).Low);
            var shape=(CollisionRange)Math.Abs(c.ShapeLinear)+Math.Abs(c.ShapeRidge);
            var epsilonSquared=(CollisionRange).01*.01;var denominator=((CollisionRange)minimum).Square()+epsilonSquared;
            var second=Math.Abs(c.ShapeRidge)*epsilonSquared/(denominator*denominator.Sqrt());
            meso=new((meso.Value*shape).High,(meso.Gradient*shape).High,(meso.Hessian*shape+second*meso.Gradient*meso.Gradient).High);
            static NaturalCollisionBounds Max(NaturalCollisionBounds a,NaturalCollisionBounds b)=>new(Math.Max(a.Value,b.Value),Math.Max(a.Gradient,b.Gradient),Math.Max(a.Hessian,b.Hessian));
            baseBound=Max(baseBound,new(((CollisionRange)macro.Value+meso.Value).High,((CollisionRange)macro.Gradient+meso.Gradient).High,((CollisionRange)macro.Hessian+meso.Hessian).High));nearBound=Max(nearBound,near);
        }
        NaturalCollisionBounds Blend(NaturalCollisionBounds b)
        {
            return new(b.Value,(((CollisionRange)b.Gradient)+2*(CollisionRange)b.Value*gw).High,(((CollisionRange)b.Hessian)+4*(CollisionRange)b.Gradient*gw+2*(CollisionRange)b.Value*hw).High);
        }
        return(Blend(baseBound),Blend(nearBound));
    }
}
