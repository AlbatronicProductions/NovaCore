using NovaCore.Core;
using NovaCore.Core.Surface;

namespace NovaCore.Graphics;

public sealed partial class PlanetaryPhysicalSurfacePointQuery
{
    Double3 IPhysicalSurfaceCollisionSource.CollisionPoint(PhysicalCollisionFrame frame,PhysicalPatchCoordinate point)
    {
        if(!frame.Valid||frame.Radius!=Radius||!double.IsFinite(point.X+point.Y))throw new InvalidDataException("Invalid collision patch frame.");
        var d=frame.Ray(point).Normalized();return d*(Radius+Height(d));
    }
    bool IPhysicalSurfaceCollisionSource.TryTriangleError(PhysicalCollisionFrame frame,PhysicalPatchCoordinate a,PhysicalPatchCoordinate b,PhysicalPatchCoordinate c,out double errorMetres)
        =>TryCollisionTriangle(frame,a,b,c,out errorMetres,out _);
    bool IPhysicalSurfaceCollisionSource.TryPreparePatch(PhysicalCollisionFrame frame,PhysicalPatchCoordinate minimum,PhysicalPatchCoordinate maximum,out IPhysicalCollisionPatch? patch)
    {
        var ready=TryCollisionDomain(frame,minimum,new(maximum.X,minimum.Y),new(minimum.X,maximum.Y),out _,out _,out var prepared,true);
        patch=prepared;return ready;
    }
    internal bool TryCollisionTriangle(PhysicalCollisionFrame frame,PhysicalPatchCoordinate a,PhysicalPatchCoordinate b,PhysicalPatchCoordinate c,out double errorMetres,out string reason)
        =>TryCollisionDomain(frame,a,b,c,out errorMetres,out reason,out _);
    private sealed record RegionalCertificate(PhysicalCollisionFrame Frame,CollisionRange X,CollisionRange Y,CubeSphereFace Face,double Error)
    {
        internal bool Contains(PhysicalCollisionFrame frame,CollisionRange x,CollisionRange y)=>frame==Frame&&x.Low>=X.Low&&x.High<=X.High&&y.Low>=Y.Low&&y.High<=Y.High;
    }
    private bool TryCollisionDomain(PhysicalCollisionFrame frame,PhysicalPatchCoordinate a,PhysicalPatchCoordinate b,PhysicalPatchCoordinate c,out double errorMetres,out string reason,out CollisionPatch? prepared,bool prepareOnly=false,RegionalCertificate? parentRegional=null,CollisionSamples? samples=null)
    {
        errorMetres=double.PositiveInfinity;reason="";prepared=null;
        if(!frame.Valid||frame.Radius!=Radius||!double.IsFinite(a.X+a.Y+b.X+b.Y+c.X+c.Y))return false;
        try
        {
            var xr=new CollisionRange(Math.Min(a.X,Math.Min(b.X,c.X)),Math.Max(a.X,Math.Max(b.X,c.X)));
            var yr=new CollisionRange(Math.Min(a.Y,Math.Min(b.Y,c.Y)),Math.Max(a.Y,Math.Max(b.Y,c.Y)));
            samples??=new(this,frame);
            var x=CollisionJet.Variable(xr,true);var y=CollisionJet.Variable(yr,false);
            var ray=new CollisionVector((CollisionJet)frame.Radial.X*Radius+frame.East.X*x+frame.North.X*y,
                (CollisionJet)frame.Radial.Y*Radius+frame.East.Y*x+frame.North.Y*y,(CollisionJet)frame.Radial.Z*Radius+frame.East.Z*x+frame.North.Z*y);
            // Preserve shared x/y dependence in the norm and dot products.
            // Summing independently enclosed Cartesian components would lose
            // the tangent cancellation and invent an O(D/R) radial uncertainty.
            static CollisionJet ScalarDot(Double3 a,Double3 b)=>new((CollisionRange)a.X*b.X+(CollisionRange)a.Y*b.Y+(CollisionRange)a.Z*b.Z,0,0,0,0,0);
            var length=(ScalarDot(frame.Radial,frame.Radial)*Radius*Radius+ScalarDot(frame.East,frame.East)*x.Square()+ScalarDot(frame.North,frame.North)*y.Square()+
                2*ScalarDot(frame.Radial,frame.East)*Radius*x+2*ScalarDot(frame.Radial,frame.North)*Radius*y+2*ScalarDot(frame.East,frame.North)*x*y).Sqrt();
            CollisionJet Dot(Double3 axis)=>(ScalarDot(frame.Radial,axis)*Radius+ScalarDot(frame.East,axis)*x+ScalarDot(frame.North,axis)*y)/length;
            var direction=ray*length.Reciprocal();var region=FloridaFacilitySupport.Region;
            var numerics=new PhysicalCollisionEvaluationBudget(frame,xr,yr);
            // Preparation has no final triangle. Each consumer triangle must
            // still prove its own radial graph and plane amplification below.
            var radialAmplification=prepareOnly?1:RadialAmplification(frame,a,b,c,direction);
            var cosine=Dot(region.Up);CollisionJet weight=0,plane=0;
            var outerEast=region.InnerEastMetres+region.BlendMetres;var outerNorth=region.InnerNorthMetres+region.BlendMetres;
            var whole=1-(outerEast*outerEast+outerNorth*outerNorth)/(Radius*Radius);
            if(cosine.V.High>=whole)
            {
                if(cosine.V.Low<=0)return false;
                var east=Dot(region.East)*Radius/cosine;var north=Dot(region.North)*Radius/cosine;
                // Full H cancels to this exact authored plane on the whole domain.
                if(east.V.Magnitude<=region.InnerEastMetres&&north.V.Magnitude<=region.InnerNorthMetres)
                {var e=numerics.FullPlane();var range=((CollisionRange)region.PlaneAltitudeMetres).Inflate(((CollisionRange)e*CollisionRange.UpperNorm(region.Up.X,region.Up.Y,region.Up.Z)).High);
                 prepared=new(this,frame,xr,yr,direction,default,default,default,e,null,samples,range);errorMetres=(((CollisionRange)e)*radialAmplification).High;return true;}
                CollisionJet Weight(CollisionJet q,double inner)
                {
                    var t=((q.Abs()-inner)/region.BlendMetres).ClampUnit();
                    return 1-t*t*t*(t*(t*6-15)+10);
                }
                weight=Weight(east,region.InnerEastMetres)*Weight(north,region.InnerNorthMetres);
                plane=((CollisionJet)Radius+region.PlaneAltitudeMetres)/cosine-Radius;
                if(cosine.V.Low<whole&&weight.V.High>0){reason="grading early-reject branch";return false;}
            }
            if(ray.X.V.Low<=0&&ray.X.V.High>=0)return false;
            var center=new PhysicalPatchCoordinate((xr.Low+xr.High)*.5,(yr.Low+yr.High)*.5);
            var centerRay=frame.Ray(center);var centerDirection=centerRay.Normalized();
            var longitude=(-ray.Z/ray.X).Atan();
            if(centerRay.X<0)longitude+=new CollisionJet(centerRay.Z<=0?PhysicalCollisionAngles.Pi:-PhysicalCollisionAngles.Pi,0,0,0,0,0);
            var address=numerics.GeographicAddresses(longitude.V);
            var global=EarthElevationDataset.CollisionBounds((longitude/Math.Tau+.5)*EarthElevationDataset.Width-.5,
                direction.Y.Acos()/Math.PI*EarthElevationDataset.Height-.5,address.Y.E);
            var shared=parentRegional is {} prior&&prior.Contains(frame,xr,yr)?((CubeSphereFace Face,double Error)?)(prior.Face,prior.Error):null;
            if(!PhysicalCollisionAddressBounds.TryRegional(ray,numerics.Second,out var face,out var u,out var v,out var addressing,out _,shared)||
                !EarthLocalTerrainElevationDataset.TryCollisionBounds(face,u,v,addressing,out var residual,out var geographicError))return false;
            var regional=new RegionalCertificate(frame,xr,yr,face,addressing);
            var geographic=(global+residual).MaxZero();
            var ee=ScalarDot(frame.East,frame.East).V;var nn=ScalarDot(frame.North,frame.North).V;var en=ScalarDot(frame.East,frame.North).V;
            var axisNorm=(((ee+nn)+((ee-nn).Square()+4*en.Square()).Clip(0,double.MaxValue).Sqrt())*.5).Sqrt().High;
            var jacobian=(((CollisionRange)Radius)*axisNorm/length.V.Low).High;
            var secondDerivative=(6*(CollisionRange)Radius*axisNorm*axisNorm/((CollisionRange)length.V.Low).Square()).High;
            var dx=Math.Max(((CollisionRange)center.X-xr.Low).Magnitude,((CollisionRange)xr.High-center.X).Magnitude);
            var dy=Math.Max(((CollisionRange)center.Y-yr.Low).Magnitude,((CollisionRange)yr.High-center.Y).Magnitude);
            var reach=(((CollisionRange)CollisionRange.UpperNorm(dx,dy))*jacobian).High;
            var identity=new PlanetaryNaturalTerrainFamilyIdentity(6,PlanetaryNaturalTerrainFamilies.ProofGeneration,PlanetaryPhysicalSurface.NaturalTerrainCandidateSeed);
            var bodyPoint=centerDirection*Radius;
            var naturalSamples=PlanetaryNaturalTerrainFamilies.EvaluateComposed(bodyPoint,identity);
            var bounds=PlanetaryNaturalTerrainFamilies.CollisionBounds(bodyPoint,reach,identity,numerics.Natural);
            CollisionJet Natural(PlanetaryNaturalTerrainFieldSample sample,NaturalCollisionBounds limit,double evaluationError)
            {
                var g=(((CollisionRange)limit.Gradient)*jacobian).High;
                var h=(((CollisionRange)limit.Hessian)*jacobian*jacobian+((CollisionRange)limit.Gradient)*secondDerivative).High;
                var valueRadius=(((CollisionRange)limit.Gradient)*reach+evaluationError).High;
                return new(((CollisionRange)sample.Height).Inflate(valueRadius),new(-g,g),new(-g,g),new(-h,h),new(-h,h),new(-h,h));
            }
            var naturalBase=Natural(new(naturalSamples.Macro.Height+naturalSamples.Meso.Height,naturalSamples.Macro.BodyGradient+naturalSamples.Meso.BodyGradient),bounds.Base,(numerics.Natural.Macro+numerics.Natural.Meso).E);
            var near=Natural(naturalSamples.Near,bounds.Near,numerics.Natural.Near.E);
            var baseHeight=(geographic+naturalBase).MaxZero();var height=((1-weight)*(baseHeight+near)+weight*plane).MaxZero();
            var surface=direction*(Radius+height);
            static double Distance(PhysicalPatchCoordinate p,PhysicalPatchCoordinate q)=>CollisionRange.UpperNorm(((CollisionRange)p.X-q.X).Magnitude,((CollisionRange)p.Y-q.Y).Magnitude);
            var diameter=Math.Max(Distance(a,b),Math.Max(Distance(a,c),Distance(b,c)));
            var ex=CollisionRemainder.From(surface.X);var ey=CollisionRemainder.From(surface.Y);var ez=CollisionRemainder.From(surface.Z);
            // Source-ordered finite arithmetic, including grading's planetary
            // cancellation, is separate from native vertex conversion.
            // Project the Cartesian remainder onto the actual sampled plane.
            // For any barycentric point Q in that plane, radial disagreement
            // is |N.(P-Q)| / |N.direction|. The strictly positive denominator
            // also proves the plane is a radial graph on the complete domain.
            var allowance=numerics.Complete(address,global.V,residual.V,geographicError,weight.V,plane.V);
            var cartesian=(((CollisionRange)CollisionRange.UpperNorm(ex.Bound(diameter),ey.Bound(diameter),ez.Bound(diameter)))+allowance).High;
            var heightRange=(cosine.V*((CollisionRange)Radius+height.V)-Radius).Inflate(((CollisionRange)allowance*CollisionRange.UpperNorm(region.Up.X,region.Up.Y,region.Up.Z)).High);
            prepared=new(this,frame,xr,yr,direction,ex,ey,ez,allowance,regional,samples,heightRange);
            errorMetres=(((CollisionRange)cartesian)*radialAmplification).High;
            return double.IsFinite(errorMetres)&&errorMetres>=0;
        }
        catch(InvalidDataException ex){reason=ex.Message;return false;}
    }
    private double RadialAmplification(PhysicalCollisionFrame frame,PhysicalPatchCoordinate a,PhysicalPatchCoordinate b,PhysicalPatchCoordinate c,CollisionVector direction)
    {
        var source=(IPhysicalSurfaceCollisionSource)this;
        var pa=source.CollisionPoint(frame,a);var pb=source.CollisionPoint(frame,b);var pc=source.CollisionPoint(frame,c);
        return RadialAmplification(pa,pb,pc,direction);
    }
    private static double RadialAmplification(Double3 pa,Double3 pb,Double3 pc,CollisionVector direction)
    {
        var abx=(CollisionRange)pb.X-pa.X;var aby=(CollisionRange)pb.Y-pa.Y;var abz=(CollisionRange)pb.Z-pa.Z;
        var acx=(CollisionRange)pc.X-pa.X;var acy=(CollisionRange)pc.Y-pa.Y;var acz=(CollisionRange)pc.Z-pa.Z;
        var nx=aby*acz-abz*acy;var ny=abz*acx-abx*acz;var nz=abx*acy-aby*acx;
        var nd=nx*direction.X.V+ny*direction.Y.V+nz*direction.Z.V;
        if(nd.High<0)nd=-nd;
        if(nd.Low<=0)throw new InvalidDataException("Collision triangle is degenerate or not a radial graph.");
        return (((CollisionRange)CollisionRange.UpperNorm(nx.Magnitude,ny.Magnitude,nz.Magnitude))/nd).High;
    }
    private readonly record struct CollisionRemainder(double Linear,double Jump,double Quadratic)
    {
        internal static CollisionRemainder From(CollisionJet value)=>new(
            (((CollisionRange).5)*CollisionRange.UpperNorm(value.X.Width,value.Y.Width)).High,
            (((CollisionRange).5)*value.GradientVariation.Constant).High,(((CollisionRange).5)*value.GradientVariation.Slope).High);
        internal double Bound(double diameter)
        {
            var linear=(((CollisionRange)Linear)*diameter).High;
            // Integrate the gradient-oscillation modulus along each segment
            // from a barycentric point to a vertex. The common affine term
            // cancels. Half the full gradient range is the independent bound.
            return Math.Min(linear,(((CollisionRange)Jump)*diameter+((CollisionRange)Quadratic)*diameter*diameter).High);
        }
    }
    private sealed class CollisionSamples(PlanetaryPhysicalSurfacePointQuery source,PhysicalCollisionFrame frame)
    {
        // Preparation-scoped immutable samples. The capacity bounds scratch
        // memory only; a miss beyond capacity evaluates the same H directly.
        private const int Capacity=4096;
        private readonly Dictionary<(long X,long Y),Double3> values=new();
        internal Double3 Point(PhysicalPatchCoordinate p)
        {
            var key=(BitConverter.DoubleToInt64Bits(p.X),BitConverter.DoubleToInt64Bits(p.Y));
            lock(values)
            {
                if(values.TryGetValue(key,out var found))return found;
                var value=((IPhysicalSurfaceCollisionSource)source).CollisionPoint(frame,p);
                if(values.Count<Capacity)values.Add(key,value);
                return value;
            }
        }
    }
    private sealed class CollisionPatch(PlanetaryPhysicalSurfacePointQuery source,PhysicalCollisionFrame frame,
        CollisionRange x,CollisionRange y,CollisionVector direction,CollisionRemainder ex,CollisionRemainder ey,CollisionRemainder ez,double allowance,RegionalCertificate? regional,CollisionSamples samples,CollisionRange heightRange):IPhysicalCollisionPatch
    {
        public bool TryFloridaHeightRange(out double minimum,out double maximum)
        {minimum=heightRange.Low;maximum=heightRange.High;return heightRange.Finite;}
        public bool TrySubpatch(PhysicalPatchCoordinate minimum,PhysicalPatchCoordinate maximum,out IPhysicalCollisionPatch? patch)
        {
            patch=null;
            if(!double.IsFinite(minimum.X)||!double.IsFinite(minimum.Y)||!double.IsFinite(maximum.X)||!double.IsFinite(maximum.Y)||minimum.X<x.Low||maximum.X>x.High||minimum.Y<y.Low||maximum.Y>y.High||minimum.X>maximum.X||minimum.Y>maximum.Y)return false;
            var ready=source.TryCollisionDomain(frame,minimum,new(maximum.X,minimum.Y),new(minimum.X,maximum.Y),out _,out _,out var result,true,regional,samples);
            patch=result;return ready;
        }
        private bool Contains(PhysicalPatchCoordinate p)=>p.X>=x.Low&&p.X<=x.High&&p.Y>=y.Low&&p.Y<=y.High;
        public Double3 CollisionPoint(PhysicalPatchCoordinate p)=>Contains(p)?samples.Point(p):throw new InvalidDataException("Collision sample outside its prepared domain.");
        public bool TryTriangleError(PhysicalPatchCoordinate a,PhysicalPatchCoordinate b,PhysicalPatchCoordinate c,out double errorMetres,double maximumError=double.PositiveInfinity)
        {
            errorMetres=double.PositiveInfinity;
            if(!Contains(a)||!Contains(b)||!Contains(c))return false;
            try
            {
                static double Distance(PhysicalPatchCoordinate p,PhysicalPatchCoordinate q)=>CollisionRange.UpperNorm(((CollisionRange)p.X-q.X).Magnitude,((CollisionRange)p.Y-q.Y).Magnitude);
                var diameter=Math.Max(Distance(a,b),Math.Max(Distance(a,c),Distance(b,c)));
                var cartesian=(((CollisionRange)CollisionRange.UpperNorm(ex.Bound(diameter),ey.Bound(diameter),ez.Bound(diameter)))+allowance).High;
                // Radial amplification is at least one. A failed cheap lower
                // test cannot become a certificate; defer H until it can pass.
                if(cartesian>maximumError)return false;
                var pa=samples.Point(a);var pb=samples.Point(b);var pc=samples.Point(c);
                var amplification=RadialAmplification(pa,pb,pc,direction);
                errorMetres=(((CollisionRange)cartesian)*amplification).High;return double.IsFinite(errorMetres);
            }
            catch(InvalidDataException){return false;}
        }
    }
}
