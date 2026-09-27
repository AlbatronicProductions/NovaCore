using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;

internal static partial class ModularCraftTests
{
    private static PartDefinitionData TwoStores(double x,double otherX,bool radialTouch=false)
    {
        var a=DefinitionFixture();var b=radialTouch?AnalyticStore(.5,1,1,1000):a;
        return a with {Stores=[a.Stores[0] with {Datum=new(x,0,0)},b.Stores[0] with {Id="other",Datum=new(otherX,0,0)}],
            Construction=a.Construction! with {StoreGeometry=[a.Construction.StoreGeometry[0],b.Construction!.StoreGeometry[0] with {Store="other"}]},
            Standard=a.Standard! with {StoreLaws=[a.Standard.StoreLaws[0],b.Standard!.StoreLaws[0] with {Store="other"}],
                Ports=a.Standard.Ports.Add(new("other-port",ConstructionService.Propellant,"test.fluid",null,"other",null,null,false))}};
    }
    internal static void PartStandardPhysicalPredicates()
    {
        checks=0;var d=DefinitionFixture();var before=Catalog(d).Save();
        var impossible=new Matrix3(40099215,21367589,-5022800.999999999,21367589,20473538,10037821,-5022800.999999999,10037821,47776983);
        var q=OracleRational.Binary;
        var halfTrace=(q(impossible.A)+q(impossible.E)+q(impossible.I))/2;
        var a=halfTrace-q(impossible.A);var b=q(0)-q(impossible.B);var c=q(0)-q(impossible.C);
        var e=halfTrace-q(impossible.E);var f=q(0)-q(impossible.F);var i=halfTrace-q(impossible.I);
        var determinant=a*(e*i-f*f)-b*(b*i-c*f)+c*(b*f-c*e);
        Check(determinant.N<0,"independent rational covariance determinant is negative");
        Check(impossible.PhysicalInertia,"original floating cancellation witness retained");
        RefuseAt(()=>Catalog(d with {LocalInertia=impossible,Construction=d.Construction! with {
            MassRegions=[new("body",1,Double3.Zero,impossible)]}}),"definition[fixture].massRegions[body]");
        // Exact physical cone boundary and both neighbours. Strict positivity is
        // the existing volumetric-body policy; zero-thickness bodies remain excluded.
        foreach(var xx in new[]{Math.BitDecrement(2d),2,Math.BitIncrement(2d)}){
            var tensor=Matrix3.Diagonal(new(xx,1,1));
            var candidate=d with {LocalInertia=tensor,Construction=d.Construction! with {MassRegions=[new("body",1,Double3.Zero,tensor)]}};
            if(xx<2)Check(Catalog(candidate).Data.Definitions.Length==1,"positive covariance one below cone boundary");
            else RefuseAt(()=>Catalog(candidate),"massRegions[body]");
        }
        foreach(var scale in new[]{double.Epsilon,Math.ScaleB(1,-1022),1d,1e200,double.MaxValue}){
            var tensor=Matrix3.Identity*scale;
            var candidate=d with {LocalInertia=tensor,Construction=d.Construction! with {MassRegions=[new("body",1,Double3.Zero,tensor)]}};
            Check(Catalog(candidate).Data.Definitions.Length==1,"exact physical signs independent of exponent");
        }
        foreach(var x in new[]{0d,1e20,-1e20,double.MaxValue,-double.MaxValue})
            RefuseAt(()=>Catalog(TwoStores(x,x)),"STORE_PHYSICAL_OVERLAP");
        foreach(var otherX in new[]{Math.BitDecrement(1d),1d,Math.BitIncrement(1d)}){
            if(otherX<1)RefuseAt(()=>Catalog(TwoStores(0,otherX)),"STORE_PHYSICAL_OVERLAP");
            else Check(Catalog(TwoStores(0,otherX)).Data.Definitions.Length==1,"touching/disjoint axial store boundary");
        }
        Check(Catalog(TwoStores(1e20,Math.BitIncrement(1e20))).Data.Definitions.Length==1,"distinct far translated stores");
        Check(Catalog(TwoStores(0,0,true)).Data.Definitions.Length==1,"radial touching cylinder/annulus allowed");
        var factor=Math.ScaleB(1,-27);var u=new Double3(92136899,26342714,3532869)*factor;
        var v=new Double3(50783700,40800279,82292350)*factor;var w=new Double3(142920599,67142993,85825219)*factor;
        Check(u+v==w,"integer-derived coplanar witness exactly reconstructs");
        var fake=d.Standard!.Collision[0] with {Vertices=[Double3.Zero,u,v,w]};
        RefuseAt(()=>Catalog(d with {Standard=d.Standard with {Collision=[fake]}}),"PHYSICAL_VOLUME_DEGENERATE");
        RefuseAt(()=>Catalog(d with {Standard=d.Standard with {Clearance=[fake]}}),"PHYSICAL_VOLUME_DEGENERATE");
        foreach(var scale in new[]{double.Epsilon,1e-100,1d,1e100,double.MaxValue}){
            var tetra=fake with {Vertices=[Double3.Zero,new(scale,0,0),new(0,scale,0),new(0,0,scale)]};
            var valid=d with {Standard=d.Standard with {Collision=[tetra]}};
            Check(Catalog(valid).Data.Definitions.Length==1,"exact volume rank independent of unit scale");
            var flat=tetra with {Vertices=[Double3.Zero,new(scale,0,0),new(0,scale,0),new(scale,scale,0)]};
            RefuseAt(()=>Catalog(d with {Standard=d.Standard with {Collision=[flat]}}),"PHYSICAL_VOLUME_DEGENERATE");
        }
        Check(Catalog(d).Save().SequenceEqual(before),"predicate refusals preserve accepted bytes");
        Console.WriteLine($"Modular Gate 1 physical predicates PASS: {checks} independent sign/overlap/boundary checks");
    }
}
