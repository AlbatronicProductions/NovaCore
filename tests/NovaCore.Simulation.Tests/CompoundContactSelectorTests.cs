using System.Diagnostics;
using System.Numerics;
using BepuUtilities;
using NovaCore.Simulation.Spacecraft.Contact.Staging;
using Candidate = NovaCore.Simulation.Spacecraft.Contact.Staging.CompoundContactSelector.Candidate;
using Motion = NovaCore.Simulation.Spacecraft.Contact.Staging.CompoundContactSelector.Motion;
using Scratch = NovaCore.Simulation.Spacecraft.Contact.Staging.CompoundContactSelector.Scratch;

internal static class CompoundContactSelectorTests
{
    private const float Dt = 1f / 60, Resolution = .0005f;
    private static readonly Motion Rest = new(default, default, default, 1,
        new Symmetric3x3 { XX = 1, YY = 1, ZZ = 1 });
    private static Candidate Point(int id, float x, float z, float depth = 0, Vector3? normal = null) =>
        new(new() { Offset = new(x, -.5f, z), Normal = normal ?? Vector3.UnitY, Depth = depth, FeatureId = id }, 0, 0, id);
    private static void Check(bool condition, string message)
    { if (!condition) throw new InvalidOperationException("Compound selection: " + message); }
    private static int[] Select(Candidate[] points, Motion a, Motion b = default, Vector3 offsetB = default, bool sort = true)
    {
        var scratch = new Scratch[points.Length]; Span<int> native = stackalloc int[4];
        for (var i = 0; i < 4; i++) native[i] = Array.FindIndex(points, c => c.RawFeature == i);
        Span<int> selected = stackalloc int[4];
        Check(CompoundContactSelector.Select(points, native, a, b, offsetB, Dt, Resolution, scratch, selected, out var count), "valid selection");
        Check(count == 4, "four-contact budget");
        var features = selected.ToArray().Select(i => points[i].RawFeature).ToArray();
        if (sort) Array.Sort(features);
        return features;
    }
    internal static void Run()
    {
        Candidate[] approach = [Point(0,-1,0,.1f), Point(1,-1,.001f,.09f), Point(2,-1,-.001f,.08f),
            Point(3,-.999f,0,.07f), Point(4,2,0,-.01f), Point(5,0,1,0)];
        var rotating = Rest with { Angular = new(0,0,-6) };
        var selected = Select(approach, rotating);
        Check(selected.Contains(0) && selected.Contains(4), "deep separating anchor and angular imminent outer both protected");
        Console.WriteLine("SELECTOR synthetic=deep-cluster/angular-approach/deep-separating PASS");

        var mirrored = approach.Select(c => c with { Contact = new() { Offset = new(-c.Contact.Offset.X,c.Contact.Offset.Y,c.Contact.Offset.Z),
            Normal = c.Contact.Normal, Depth = c.Contact.Depth, FeatureId = c.Contact.FeatureId } }).ToArray();
        Check(Select(mirrored, Rest with { Angular = new(0,0,6) }).SequenceEqual(selected), "mirrored geometry/motion selection");
        var permutation = approach.ToArray();
        for (var pass = 0; pass < 30; pass++)
        {
            (permutation[pass % permutation.Length],permutation[(pass*3+1)%permutation.Length]) =
                (permutation[(pass*3+1)%permutation.Length],permutation[pass % permutation.Length]);
            Check(Select(permutation,rotating).SequenceEqual(selected), "input permutation stable feature selection");
        }
        Console.WriteLine("SELECTOR synthetic=mirror/30-permutations PASS");

        Candidate[] duplicates = [Point(0,-1,-1),Point(1,-1,-1),Point(2,-1,-1),Point(3,-1,-1),
            Point(4,1,-1),Point(5,-1,1),Point(6,1,1),Point(7,100,0,-1)];
        var spread = Select(duplicates,Rest);
        Check(spread.SequenceEqual(new[]{0,4,5,6}), "duplicate cluster replaced by distinct relevant support; far speculative point excluded");
        for(var i=0;i<100;i++)
            Check(Select(duplicates,Rest with { Linear=new(0,(i%2==0?1:-1)*1e-6f,0) }).SequenceEqual(spread), "sub-resolution rest noise no churn");
        Console.WriteLine("SELECTOR synthetic=duplicates/separating-speculative/100-rest-noise PASS");

        Candidate[] directions = [Point(0,0,0,.01f),Point(1,0,0,.009f),Point(2,0,0,.008f),Point(3,0,0,.007f),
            Point(4,0,0,-.001f,-Vector3.UnitX),Point(5,0,0,-.001f,-Vector3.UnitY)];
        var multi = Select(directions,Rest with { Linear=new(2,2,0) });
        Check(multi.Contains(0) && multi.Contains(4) && multi.Contains(5), "two independent imminent normals retained");
        Console.WriteLine("SELECTOR synthetic=multiple-imminent-normal-directions PASS");

        // Common rigid motion at each contact cancels, including distinct COM offsets and angular motion.
        var w = new Vector3(0,0,-6); var offset = new Vector3(3,0,0); var linear = new Vector3(1,2,3);
        Check(Select(approach,Rest with { Linear=linear,Angular=w },
            Rest with { Linear=linear+Vector3.Cross(w,offset),Angular=w },offset)
            .SequenceEqual(Select(approach,Rest,Rest,offset)), "opposing-body contact velocity cancellation");
        var accelerated = Rest with { Acceleration=new(0,-9.81f,0) };
        Candidate[] resting=[Point(0,1,0),Point(1,0,1),Point(2,-1,0),Point(3,0,-1),Point(4,1,1),Point(5,-1,-1)];
        var restingSelection=Select(resting,accelerated);
        foreach(var noise in new[]{new Vector3(1e-6f,0,0),new Vector3(-1e-6f,0,0),new Vector3(7e-7f,0,-7e-7f)})
            Check(Select(resting,accelerated with { Angular=noise }).SequenceEqual(restingSelection),"gravity plus unresolved angular noise retains resting set");
        Console.WriteLine("SELECTOR synthetic=gravity/angular-noise-rest-regression PASS");
        var scratch = new Scratch[approach.Length]; Span<int> result = stackalloc int[4];
        Check(CompoundContactSelector.Select(approach,[0,1,2,3],accelerated,default,default,Dt,Resolution,scratch,result,out _), "acceleration selection");
        Check(Math.Abs(scratch[0].ClosingDisplacement - 9.81 * Dt * Dt) < 1e-9, "semi-implicit acceleration counted exactly once");
        Check(!CompoundContactSelector.Select(approach,[0,1,2,3],Rest,default,default,Dt,Resolution,[],result,out _), "insufficient scratch rejected");
        var malformed=approach.ToArray();malformed[0]=Point(0,float.NaN,0);
        Check(!CompoundContactSelector.Select(malformed,[0,1,2,3],Rest,default,default,Dt,Resolution,scratch,result,out _), "invalid contact rejected");
        Console.WriteLine("SELECTOR synthetic=relative-motion/acceleration/invalid-data PASS");
        EstablishedSupportStability();
        EstablishedSupportPrecision();
    }

    private static void EstablishedSupportStability()
    {
        // Red-team regression: unchanged coplanar support, with only one unresolved depth difference.
        // Exact physical depths remain unmodified; only anchor ranking uses the admitted resolution.
        Candidate[] resting=[Point(0,1,0,.0004f),Point(1,0,1,.0004f),Point(2,-1,0,.0004f),
            Point(3,0,-1,.0004f),Point(4,1,1,.0004f),Point(5,-1,-1,.0004f)];
        var motion=Rest with { Acceleration=new(0,-9.81f,0) };
        var before=Select(resting,motion);
        resting[4]=Point(4,1,1,.00041f);
        var after=Select(resting,motion);
        Console.WriteLine($"SELECTOR_REST_DEPTH before=[{string.Join(',',before)}] after=[{string.Join(',',after)}] depth_change_m=.00001 resolution_m={Resolution:R}");
        Check(after.SequenceEqual(before),"unresolved established-depth change must not replace distinct resting support rows");
    }

    private static void EstablishedSupportPrecision()
    {
        Candidate[] rows=[Point(0,1,0,.0004f),Point(1,0,1,.0004f),Point(2,-1,0,.0004f),
            Point(3,0,-1,.0004f),Point(4,1,1,.0004f),Point(5,-1,-1,.0004f)];
        var motion=Rest with { Acceleration=new(0,-9.81f,0) };
        var expected=Select(rows,motion);
        var perturbations=new[]{-0.00049f,-0.0001f,-0.00001f,-0.000001f,-0.00000001f,0f,
            .00000001f,.000001f,.00001f,.0001f,.00049f};
        foreach(var change in perturbations)
        {
            var variant=rows.ToArray();variant[4]=Point(4,1,1,.0004f+change);
            var original=variant.ToArray();
            Check(Select(variant,motion).SequenceEqual(expected),"below-resolution fixed-cohort sweep");
            for(var i=0;i<variant.Length;i++)
                Check(SameContactBits(variant[i],original[i]),"original contact geometry and depth bits preserved");
        }
        var deeper=rows.ToArray();deeper[4]=Point(4,1,1,.0004f+2*Resolution);
        Check(Select(deeper,motion,sort:false)[0]==4 && !Select(deeper,motion).SequenceEqual(expected),
            "resolved depth difference changes anchor and topology");
        deeper[4]=Point(4,1,1,.1f);
        Check(Select(deeper,Rest with { Linear=new(0,100,0) },sort:false)[0]==4,"deep separating penetration protected");

        // Boundary uses representable FP32 inputs and the unchanged FP32 resolution promoted to double.
        foreach(var depth in new[]{MathF.BitDecrement(Resolution),Resolution,MathF.BitIncrement(Resolution)})
        {
            var boundary=rows.Select(c=>Point(c.RawFeature,c.Contact.Offset.X,c.Contact.Offset.Z,0)).ToArray();
            boundary[4]=Point(4,1,1,depth);
            var gap=(double)depth;
            Check(Select(boundary,motion,sort:false)[0]==(gap<Resolution?0:4),"strict exact resolution boundary");
        }
        // A contextual class is not transitive epsilon chaining: A~B, B~C never admits distant A.
        var chain=rows.Select(c=>Point(c.RawFeature,c.Contact.Offset.X,c.Contact.Offset.Z,0)).ToArray();
        chain[1]=Point(1,0,1,.75f*Resolution);chain[4]=Point(4,1,1,1.5f*Resolution);
        var chainOrder=Select(chain,motion,sort:false);
        Check(chainOrder[0]==1,"common-maximum cohort excludes chained lower contact");
        var permutations=0;
        VisitPermutations(chain,points=>
        {
            Check(Select(points,motion,sort:false).SequenceEqual(chainOrder),"all raw permutations preserve full selection order");
            permutations++;
        });
        Check(permutations==720,"complete six-contact permutation coverage");

        foreach(var baseDepth in new[]{-.01f,-.0002f,0f,.01f})
        {
            var sign=rows.Select(c=>Point(c.RawFeature,c.Contact.Offset.X,c.Contact.Offset.Z,baseDepth)).ToArray();
            sign[4]=Point(4,1,1,baseDepth+.0004f);
            Check(Select(sign,motion,sort:false)[0]==0,"nearest/support cohort has no artificial depth-zero boundary");
        }
        var mirrored=chain.Select(c=>Point(c.RawFeature,-c.Contact.Offset.X,c.Contact.Offset.Z,c.Contact.Depth)).ToArray();
        Check(Select(mirrored,motion,sort:false).SequenceEqual(chainOrder),"cohort mirror symmetry with stable feature identities");
        foreach(var invalid in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity})
        {
            var bad=rows.ToArray();bad[5]=Point(5,-1,-1,invalid);
            var scratch=new Scratch[bad.Length];var chosen=new int[4];
            Check(!CompoundContactSelector.Select(bad,[0,1,2,3],motion,default,default,Dt,Resolution,scratch,chosen,out _),
                "nonfinite depth refused before cohort selection");
        }
        Console.WriteLine($"SELECTOR_PRECISION ten_um=PASS below_resolution={perturbations.Length}/11 above_resolution=PASS exact_boundary=PASS chain=PASS permutations={permutations}/720 mirror=PASS deep=PASS signed_depth=PASS original_bits=PASS nonfinite=PASS");
    }

    private static void VisitPermutations(Candidate[] rows, Action<Candidate[]> visit, int start=0)
    {
        if(start==rows.Length){visit(rows);return;}
        for(var i=start;i<rows.Length;i++)
        {
            (rows[start],rows[i])=(rows[i],rows[start]);
            VisitPermutations(rows,visit,start+1);
            (rows[start],rows[i])=(rows[i],rows[start]);
        }
    }
    private static bool SameContactBits(Candidate a, Candidate b)
    {
        static bool Bits(float x,float y)=>BitConverter.SingleToInt32Bits(x)==BitConverter.SingleToInt32Bits(y);
        return a.ChildA==b.ChildA && a.ChildB==b.ChildB && a.RawFeature==b.RawFeature && a.Contact.FeatureId==b.Contact.FeatureId &&
            Bits(a.Contact.Depth,b.Contact.Depth) && Bits(a.Contact.Offset.X,b.Contact.Offset.X) &&
            Bits(a.Contact.Offset.Y,b.Contact.Offset.Y) && Bits(a.Contact.Offset.Z,b.Contact.Offset.Z) &&
            Bits(a.Contact.Normal.X,b.Contact.Normal.X) && Bits(a.Contact.Normal.Y,b.Contact.Normal.Y) && Bits(a.Contact.Normal.Z,b.Contact.Normal.Z);
    }
    internal static void Cost(bool allocationOnly = false)
    {
        Candidate[] points=[Point(0,-1,0,.1f),Point(1,-1,.001f,.09f),Point(2,-1,-.001f,.08f),Point(3,-.999f,0,.07f),
            Point(4,2,0,-.01f),Point(5,0,1),Point(6,1,-1),Point(7,1,1),Point(8,0,-1),Point(9,-1,1),Point(10,-1,-1),Point(11,0,0)];
        var scratch=new Scratch[points.Length];var motion=Rest with { Angular=new(0,0,-6) };Span<int> result=stackalloc int[4];
        ReadOnlySpan<int> native=[0,1,2,3];var valid=true;
        for(var i=0;i<128;i++) valid &= CompoundContactSelector.Select(points,native,motion,default,default,Dt,Resolution,scratch,result,out _);
        using(var measurement=new OrdinaryAllocationMeasurement("compound-contact-selection"))
        {
            for(var i=0;i<1024;i++) valid &= CompoundContactSelector.Select(points,native,motion,default,default,Dt,Resolution,scratch,result,out _);
            OrdinaryAllocationMeasurement.RequireZero(measurement.Complete(),"compound-contact-selection");
        }
        Check(valid,"repeated valid selections");
        if (allocationOnly)
        {
            OrdinaryAllocationMeasurement.PositiveControl();
            Console.WriteLine("SELECTOR_ALLOCATION candidates=12 warm=128 measured=1024 PASS");
            return;
        }
        var ticks=new long[1024];
        for(var i=0;i<ticks.Length;i++)
        {var start=Stopwatch.GetTimestamp();valid &= CompoundContactSelector.Select(points,native,motion,default,default,Dt,Resolution,scratch,result,out _);ticks[i]=Stopwatch.GetTimestamp()-start;}
        Check(valid,"timed valid selections");Array.Sort(ticks);
        Console.WriteLine($"SELECTOR_COST candidates=12 iterations=1024 median_us={ticks[512]*1e6/Stopwatch.Frequency:R} p95_us={ticks[972]*1e6/Stopwatch.Frequency:R} p99_us={ticks[1013]*1e6/Stopwatch.Frequency:R} max_us={ticks[^1]*1e6/Stopwatch.Frequency:R}");
        OrdinaryAllocationMeasurement.PositiveControl();
    }
}
