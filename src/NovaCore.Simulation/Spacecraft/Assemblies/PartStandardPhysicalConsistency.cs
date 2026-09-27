namespace NovaCore.Simulation.Spacecraft.Assemblies;

/// <summary>Cold binary64 enclosures of the admitted analytic store law. No unit-sized tolerance.</summary>
internal static class PartStandardPhysicalConsistency
{
    // Authored dimensions/density are the exact binary64 inputs to the physical law.
    // Each arithmetic result is enclosed outward by adjacent representable numbers.
    // A declared derived value may be rounded: its adjacent neighbours conservatively
    // enclose its round-to-nearest cell. No tolerance is borrowed from a different unit/scale.
    private readonly record struct Interval(double Low,double High)
    {
        internal static Interval Exact(double value)
        {
            if(value==0)return new(0,0);
            if(!double.IsNormal(value)||value<0)throw new InvalidDataException("positive finite normal inputs required");
            return new(value,value);
        }
        private static Interval Outward(double low,double high)
        {
            low=Math.BitDecrement(low);high=Math.BitIncrement(high);
            if(!double.IsNormal(low)||!double.IsNormal(high)||low<=0||high<low)
                throw new InvalidDataException("physical-law interval overflow, underflow or collapse");
            return new(low,high);
        }
        internal static Interval Rounded(double value)=>Outward(value,value);
        internal static Interval Add(Interval a,Interval b)=>Outward(a.Low+b.Low,a.High+b.High);
        internal static Interval Difference(double outer,double inner)=>Outward(outer-inner,outer-inner);
        internal static Interval Multiply(Interval a,Interval b)
        {
            if(a.High==0||b.High==0)return new(0,0);
            return Outward(a.Low*b.Low,a.High*b.High);
        }
        internal static Interval Divide(Interval a,double divisor)=>Outward(a.Low/divisor,a.High/divisor);
        internal bool ContainsRounded(double value)
        {
            if(!double.IsNormal(value)||value<=0)return false;
            var declared=Rounded(value);
            return Low<=declared.High&&declared.Low<=High;
        }
        internal Interval IntersectRounded(double value)
        {
            if(!ContainsRounded(value))throw new InvalidDataException("STORE_VOLUME_DENSITY");
            var declared=Rounded(value);
            return new(Math.Max(Low,declared.Low),Math.Min(High,declared.High));
        }
    }
    internal static void Validate(string definition,PartStoreLaw law,StoreData store,StoreGeometryData geometry)
    {
        var at=$"definition[{definition}].standard.storeLaws[{law.Store}]";
        try
        {
            var outer=Interval.Exact(law.OuterRadiusM);var inner=Interval.Exact(law.InnerRadiusM);
            var length=Interval.Exact(law.LengthM);var density=Interval.Exact(law.DensityKgM3);
            var area=Interval.Multiply(Interval.Difference(law.OuterRadiusM,law.InnerRadiusM),Interval.Add(outer,inner));
            var volume=Interval.Multiply(Interval.Multiply(Interval.Rounded(Math.PI),area),length)
                .IntersectRounded(geometry.UsableVolumeM3);
            AssemblyConstructionFacts.Require(Interval.Multiply(volume,density).ContainsRounded(store.CapacityKg),"STORE_VOLUME_DENSITY");
            var radial=Interval.Add(Interval.Multiply(outer,outer),Interval.Multiply(inner,inner));
            var axial=Interval.Divide(radial,2);
            var transverse=Interval.Add(Interval.Divide(radial,4),Interval.Divide(Interval.Multiply(length,length),12));
            var tensor=geometry.InertiaPerKg;
            AssemblyConstructionFacts.Require(axial.ContainsRounded(tensor.A)&&transverse.ContainsRounded(tensor.E)&&
                transverse.ContainsRounded(tensor.I)&&tensor.B==0&&tensor.C==0&&tensor.D==0&&tensor.F==0&&tensor.G==0&&tensor.H==0,
                "STORE_SPATIAL_INERTIA");
        }
        catch(InvalidDataException e){throw new InvalidDataException($"{at}: {e.Message}",e);}
    }
}
