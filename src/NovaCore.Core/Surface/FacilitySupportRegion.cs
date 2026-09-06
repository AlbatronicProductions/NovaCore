namespace NovaCore.Core.Surface;

/// <summary>Immutable physical grading in a body-fixed rectangular site frame, independent of rendering.</summary>
public readonly record struct FacilitySupportRegion(ulong BodyId, uint Revision,
    Double3 Up, Double3 East, Double3 North, double RadiusMetres, double PlaneAltitudeMetres,
    double InnerEastMetres, double InnerNorthMetres, double BlendMetres)
{
    public ulong DeterministicHash
    {
        get
        {
            var hash=1469598103934665603UL;
            void Add(ulong value){hash=(hash^value)*1099511628211UL;}
            void Number(double value)=>Add((ulong)BitConverter.DoubleToInt64Bits(value));
            Add(BodyId);Add(Revision);
            Number(Up.X);Number(Up.Y);Number(Up.Z);Number(East.X);Number(East.Y);Number(East.Z);
            Number(North.X);Number(North.Y);Number(North.Z);Number(RadiusMetres);Number(PlaneAltitudeMetres);
            Number(InnerEastMetres);Number(InnerNorthMetres);Number(BlendMetres);return hash;
        }
    }
    public FacilitySupportSample Sample(in Double3 direction)
    {
        var cosine=Double3.Dot(direction,Up);
        // Conservative whole-site rejection; the exact compact rectangle is below.
        var outerEast=InnerEastMetres+BlendMetres;var outerNorth=InnerNorthMetres+BlendMetres;
        if(cosine<1d-(outerEast*outerEast+outerNorth*outerNorth)/(RadiusMetres*RadiusMetres))return default;
        var e=Double3.Dot(direction,East);var n=Double3.Dot(direction,North);
        var x=RadiusMetres*e/cosine;var y=RadiusMetres*n/cosine;
        var wx=Weight(x,InnerEastMetres,out var dx);var wy=Weight(y,InnerNorthMetres,out var dy);
        if(wx==0d||wy==0d)return default;
        var gradientX=(East*cosine-Up*e)/(cosine*cosine);
        var gradientY=(North*cosine-Up*n)/(cosine*cosine);
        return new(wx*wy,gradientX*(dx*wy)+gradientY*(dy*wx),
            (RadiusMetres+PlaneAltitudeMetres)/cosine-RadiusMetres);
    }
    private double Weight(double coordinate,double inner,out double derivative)
    {
        derivative=0d;var distance=Math.Abs(coordinate);
        if(distance<=inner)return 1d;if(distance>=inner+BlendMetres)return 0d;
        var t=(distance-inner)/BlendMetres;
        derivative=-30d*t*t*(t-1d)*(t-1d)*Math.Sign(coordinate)/BlendMetres;
        return 1d-t*t*t*(t*(t*6d-15d)+10d);
    }
    public double AdaptBase(in Double3 direction,double naturalBase)
    {
        var s=Sample(direction);return s.Weight==0d?naturalBase:naturalBase+(s.PlaneHeight-naturalBase)*s.Weight;
    }
    public Double3 AdaptBaseNormal(in Double3 direction,double naturalBase,in Double3 naturalNormal)
    {
        var s=Sample(direction);if(s.Weight==0d)return naturalNormal;
        if(s.Weight==1d)return Up;
        var naturalSlope=direction-naturalNormal/Math.Max(Double3.Dot(naturalNormal,direction),1e-9d);
        var planeSlope=direction-Up/Double3.Dot(Up,direction);
        var slope=naturalSlope*(1d-s.Weight)+planeSlope*s.Weight+s.WeightGradient*(s.PlaneHeight-naturalBase);
        return (direction-slope).Normalized();
    }
}

public readonly record struct FacilitySupportSample(double Weight,Double3 WeightGradient,double PlaneHeight);

/// <summary>Authored immutable Florida support v1. Changes require a new physical definition and renderer lifetime.</summary>
public static class FloridaFacilitySupport
{
    // Survey provenance: generation-4 natural terrain, center + 896 perimeter
    // intersections at 0.25 m spacing, minimum minus the existing 0.25 m embed.
    // This is the pre-support foundation bottom; neither slab nor footing moves.
    public const double ContactPlaneAltitudeMetres=15.134892258793116d;
    public static readonly FacilitySupportRegion Region=new(6,1,
        new(.1433224599406355d,.4788205718227514d,.8661348234979923d),
        new(.9865841313746494d,0d,-.163253642286255d),
        new(-.07816920235165153d,.8779127861008367d,-.47239677793606216d),
        6371008.8d,ContactPlaneAltitudeMetres,64d,56d,128d);
    public static readonly ulong DefinitionIdentity=Region.DeterministicHash;
}
