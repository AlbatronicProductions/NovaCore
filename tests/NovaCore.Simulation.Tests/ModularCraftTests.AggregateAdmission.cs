using System.Collections.Immutable;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;

internal static partial class ModularCraftTests
{
    private static PartDefinitionData DryFixture(params MassRegionData[] regions)
    {
        // Independent centre-first rational reconstruction, rather than production's
        // origin tensor and shared integer numerator/denominator construction.
        var q=OracleRational.Binary;var mass=q(0);var moment=new[]{q(0),q(0),q(0)};
        foreach(var r in regions){mass+=q(r.MassKg);var p=new[]{r.Com.X,r.Com.Y,r.Com.Z};
            for(var i=0;i<3;i++)moment[i]+=q(r.MassKg)*q(p[i]);}
        var center=moment.Select(x=>x/mass).ToArray();var tensor=Enumerable.Repeat(q(0),9).ToArray();
        foreach(var r in regions){var p=new[]{r.Com.X,r.Com.Y,r.Com.Z};var v=p.Select((x,i)=>q(x)-center[i]).ToArray();
            var own=new[]{r.InertiaAtCom.A,r.InertiaAtCom.B,r.InertiaAtCom.C,r.InertiaAtCom.D,r.InertiaAtCom.E,r.InertiaAtCom.F,r.InertiaAtCom.G,r.InertiaAtCom.H,r.InertiaAtCom.I};
            for(var row=0;row<3;row++)for(var col=0;col<3;col++){
                var parallel=row==col?v[(row+1)%3]*v[(row+1)%3]+v[(row+2)%3]*v[(row+2)%3]:q(0)-v[row]*v[col];
                tensor[3*row+col]+=q(own[3*row+col])+q(r.MassKg)*parallel;}}
        var a=tensor.Select(x=>x.Round()).ToArray();var d=DefinitionFixture();
        return d with {DryMassKg=mass.Round(),LocalCom=new(center[0].Round(),center[1].Round(),center[2].Round()),
            LocalInertia=new(a[0],a[1],a[2],a[3],a[4],a[5],a[6],a[7],a[8]),
            Construction=d.Construction! with {MassRegions=regions.ToImmutableArray()}};
    }
    internal static void PartStandardAggregateNumerics()
    {
        checks=0;var d=DefinitionFixture();var baseline=Catalog(d).Save();
        foreach(var scale in new[]{1e-100,1e-12,1d,1e100}){
            var tiny=DryFixture(new MassRegionData("body",scale,Double3.Zero,Matrix3.Identity*scale));
            Check(Catalog(tiny).Data.Definitions.Length==1,"independent dry aggregate scale");
            RefuseAt(()=>Catalog(tiny with {DryMassKg=scale/1000}),"DRY_AGGREGATE_MASS");
            RefuseAt(()=>Catalog(tiny with {LocalInertia=Matrix3.Identity*(scale/1000)}),"DRY_AGGREGATE_INERTIA");
            RefuseAt(()=>Catalog(tiny with {LocalCom=new(1e-10,0,0)}),"DRY_AGGREGATE_COM");
        }
        var signed=DryFixture(new("body",1,new(1,1,0),Matrix3.Identity),new("b",1,new(-1,-1,0),Matrix3.Identity));
        Check(signed.LocalInertia==new Matrix3(4,-2,0,-2,4,0,0,0,6),"hand-derived signed tensor");
        var admitted=Catalog(signed);Check(admitted.Save().SequenceEqual(AssemblyDefinitionCatalog.Load(admitted.Save()).Save()),"signed tensor roundtrip");
        Check(Catalog(signed with {Construction=signed.Construction! with {MassRegions=signed.Construction.MassRegions.Reverse().ToImmutableArray()}}).Digest==admitted.Digest,"constituent order independent");
        foreach(var offset in new[]{Math.ScaleB(1,500),double.MaxValue,-double.MaxValue}){
            var shifted=DryFixture(new("body",1,new(offset,0,0),Matrix3.Identity),new("b",1,new(offset,0,0),Matrix3.Identity));
            Check(shifted.LocalInertia==Matrix3.Identity*2,"independent exact translation cancellation");
            Check(Catalog(shifted).Data.Definitions.Length==1,"huge common translation with small tensor");
            RefuseAt(()=>Catalog(shifted with {LocalInertia=Matrix3.Identity*1e-12}),"DRY_AGGREGATE_INERTIA");
        }
        var separated=DryFixture(new("body",1,new(Math.ScaleB(1,50),1,0),Matrix3.Identity),new("b",1,new(Math.ScaleB(1,50),-1,0),Matrix3.Identity));
        Check(separated.LocalInertia==Matrix3.Diagonal(new(4,2,4)),"large offset plus resolved small separation");
        Check(Catalog(separated).Data.Definitions.Length==1,"no cancellation from translated separation");
        // Mass midpoint at 1 + 2^-53 rounds to even 1. A one-ULP larger
        // second mass crosses the exact boundary; a one-ULP smaller stays below.
        var half=Math.ScaleB(1,-53);
        foreach(var extra in new[]{Math.BitDecrement(half),half,Math.BitIncrement(half)}){
            var midpoint=DryFixture(new("body",1,Double3.Zero,Matrix3.Identity),new("b",extra,Double3.Zero,Matrix3.Identity));
            var expected=extra<=half?1:Math.BitIncrement(1d);
            Check(midpoint.DryMassKg==expected,"independent midpoint decision");
            Check(Catalog(midpoint).Data.Definitions.Length==1,"mass one-below/at/above midpoint");
            RefuseAt(()=>Catalog(midpoint with {DryMassKg=expected==1?Math.BitIncrement(1d):1}),"DRY_AGGREGATE_MASS");
        }
        foreach(var sign in new[]{-1,1}){
            var midpoint=DryFixture(new("body",1,new(sign,0,0),Matrix3.Identity),new("b",1,new(sign*Math.BitIncrement(1d),0,0),Matrix3.Identity));
            Check(midpoint.LocalCom.X==sign,"signed COM midpoint ties-even");
            Check(Catalog(midpoint).Data.Definitions.Length==1,"signed COM midpoint admission");
            RefuseAt(()=>Catalog(midpoint with {LocalCom=new(sign*Math.BitIncrement(1d),0,0)}),"DRY_AGGREGATE_COM");
        }
        var maximum=DryFixture(new MassRegionData("body",double.MaxValue,Double3.Zero,Matrix3.Identity));
        Check(Catalog(maximum).Data.Definitions.Length==1,"maximum finite dry mass");
        RefuseAt(()=>Catalog(maximum with {Construction=maximum.Construction! with {MassRegions=maximum.Construction.MassRegions.Add(new("b",double.Epsilon,Double3.Zero,Matrix3.Identity))}}),"DRY_AGGREGATE_MASS");
        var many=Enumerable.Range(0,4096).Select(i=>new MassRegionData(i==0?"body":"r"+i,1,new(double.MaxValue,double.MaxValue,double.MaxValue),Matrix3.Identity)).ToImmutableArray();
        var crowded=d with {DryMassKg=4096,LocalCom=many[0].Com,LocalInertia=Matrix3.Identity*4096,Construction=d.Construction! with {MassRegions=many}};
        Check(Catalog(crowded).Data.Definitions.Length==1,"4096 maximal coordinates exact cancellation bounded");
        Reject(()=>Catalog(crowded with {Construction=crowded.Construction! with {MassRegions=many.Add(new("overflow",1,Double3.Zero,Matrix3.Identity))}}),"region capacity enforced before arithmetic");
        Check(Catalog(d).Save().SequenceEqual(baseline),"aggregate refusals preserve source catalog bytes");
        Console.WriteLine($"Modular Gate 1 dry aggregate admission PASS: {checks} independent centre-first rational/boundary checks");
    }
}
