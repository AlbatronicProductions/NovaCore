using System.Numerics;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Contact;
using NovaCore.Simulation.Spacecraft.Rotation;
using static CertifiedResponseOracle;

internal static class CertifiedContactResponseTests
{
    private static void Check(bool value,string name){if(!value)throw new InvalidOperationException("Certified response: "+name);}
    internal static readonly CertifiedResponseRequest Fine=new(1e-4,1e-4,1e-4,1e-4);
    private static readonly CertifiedResponseRequest Broad=new(double.MaxValue,double.MaxValue,double.MaxValue,double.MaxValue);
    internal static void Run()
    {
        Fixture(Double3.UnitX,-3,Double3.Zero,DoubleQuaternion.Identity,2,new(2,3,4),new(1,2),6,"central");
        Fixture(Double3.UnitY,-1,Double3.UnitX,DoubleQuaternion.Identity,1,new(1,1,.5),3,new(1,3),"off-center");
        Fixture(Double3.UnitX,-1.5,new(1,2,3),new(.5,.5,.5,.5),2,new(2,3,5),new(17,6),new(9,17),"asymmetric transpose");
        foreach(var normal in new[]{Double3.UnitX,Double3.UnitY,Double3.UnitZ})
        {
            var exact=Solve(V.From(normal),-2,new(1,-2,3),new(0,0,.6,.8),8,new(2,3,4));
            Fixture(normal,-2,new(1,-2,3),new(0,0,.6,.8),8,new(2,3,4),exact.K,exact.J,"stored-bit normalized attitude");
        }
        // Exact rational unit normal; the binary64 values .6/.8 alone are not mathematically unit.
        var rationalNormal=new V(new(3,5),new(4,5),0);
        var normalBox=new FloridaVector(new(Math.BitDecrement(.6),Math.BitIncrement(.6)),new(Math.BitDecrement(.8),Math.BitIncrement(.8)),0);
        var oracle=Solve(rationalNormal,-2,new(1,-2,3),new(0,0,.6,.8),8,new(2,3,4));
        Check(CertifiedContactResponseMath.Enclose(normalBox,-2,new(1,-2,3),new(0,0,.6,.8),8,new(2,3,4),Fine,out var enclosed)==CertifiedResponseFailure.None&&
            Contains(enclosed.EffectiveInverseMass,oracle.K)&&Contains(enclosed.ScalarImpulse,oracle.J)&&Contains(enclosed.LinearImpulseRoot,oracle.Linear)&&
            Contains(enclosed.AngularImpulseBody,oracle.Angular),"rational unit normal enclosed under stored-bit normalized attitude");
        RootAndDependency();Refusals();Energy();Representation();AuthorityShape();
        Console.WriteLine("CERTIFIED_RESPONSE_ANALYTICAL exact-rational/central/off-center/normalization/root/dependency/energy/rounding/refusal PASS");
    }
    private static void Fixture(Double3 n,double u,Double3 r,DoubleQuaternion q,double m,PrincipalMomentsOfInertia inertia,R expectedK,R expectedJ,string name)
    {
        var exact=Solve(V.From(n),R.From(u),r,q,m,inertia);
        Check(exact.K==expectedK&&exact.J==expectedJ,name+" independent expected law");
        var status=CertifiedContactResponseMath.Enclose(FloridaVector.From(n),u,r,q,m,inertia,Fine,out var result);
        Check(status==CertifiedResponseFailure.None,name+" qualifies");
        Check(Contains(result.EffectiveInverseMass,exact.K)&&Contains(result.ScalarImpulse,exact.J)&&
            Contains(result.LinearImpulseRoot,exact.Linear)&&Contains(result.AngularImpulseBody,exact.Angular),name+" exact response containment");
        Check(R.From(u)+exact.K*exact.J==0,name+" exact normal stop");
        Check(V.Dot(V.Cross(exact.Linear,V.From(n)),V.Cross(exact.Linear,V.From(n)))==0,name+" no tangential impulse");
    }
    private static void RootAndDependency()
    {
        // These independently certified analytical roots test arithmetic, never issue production witnesses.
        foreach(var rational in new[]{true,false})
        {
            var center=rational?.5:1/Math.Sqrt(2);var lo=R.From(Math.BitDecrement(center));var hi=R.From(Math.BitIncrement(center));
            Check(rational?lo<new R(1,2)&&hi>new R(1,2):2*lo*lo<1&&2*hi*hi>1,"independent root bracket");
            var speed=new FloridaBound(-2*Math.BitIncrement(center),-2*Math.BitDecrement(center));
            Check(CertifiedContactResponseMath.Enclose(FloridaVector.From(Double3.UnitX),speed,Double3.Zero,DoubleQuaternion.Identity,1,new(1,1,1),Fine,out var result)==CertifiedResponseFailure.None,"root-local response");
            if(rational)Check(Contains(result.ScalarImpulse,(R)1),"rational-root impulse");
            else Check(R.From(result.ScalarImpulse.Lower)*R.From(result.ScalarImpulse.Lower)<2&&
                R.From(result.ScalarImpulse.Upper)*R.From(result.ScalarImpulse.Upper)>2,"irrational impulse containment by exact square");
        }
        var nbox=new FloridaVector(new(0,1),new(0,1),0);
        Check(CertifiedContactResponseMath.Enclose(nbox,new(-2,-1),Double3.UnitX,DoubleQuaternion.Identity,1,new(1,1,1),Broad,out var box)==CertifiedResponseFailure.None,"correlated family enclosure");
        for(var i=0;i<=8;i++)
        {
            R t=new(i,8);var n=new V((1-t*t)/(1+t*t),2*t/(1+t*t),0);var u=-(1+n.Y*n.Y);
            var exact=Solve(n,u,Double3.UnitX,DoubleQuaternion.Identity,1,new(1,1,1));
            Check(exact.J==1&&Contains(box.ScalarImpulse,exact.J)&&Contains(box.LinearImpulseRoot,exact.Linear)&&Contains(box.AngularImpulseBody,exact.Angular),"correlated same-root family");
        }
        Check(box.ScalarImpulse.Upper-box.ScalarImpulse.Lower>1,"dependency widening retained");
    }
    private static void Refusals()
    {
        CertifiedResponseFailure Try(FloridaVector n,FloridaBound u,Double3 r,double m,PrincipalMomentsOfInertia inertia,CertifiedResponseRequest request)=>
            CertifiedContactResponseMath.Enclose(n,u,r,DoubleQuaternion.Identity,m,inertia,request,out _);
        var n=FloridaVector.From(Double3.UnitX);
        Check(Try(n,-1,Double3.Zero,1,new(1,1,1),default)==CertifiedResponseFailure.InvalidRequest,"invalid widths");
        foreach(var u in new[]{new FloridaBound(-1,0),new(-1,1),new(0,1)})Check(Try(n,u,Double3.Zero,1,new(1,1,1),Fine)==CertifiedResponseFailure.ApproachUncertain,"uncertain/nonapproach");
        Check(Try(new(new(-1,1),new(-1,1),new(-1,1)),-1,new(1,2,3),1,new(1,2,3),Fine)==CertifiedResponseFailure.RequestedWidth,"wide normal");
        Check(Try(n,-1,Double3.Zero,1,new(1,1,1),new(1e-30,1e-30,1e-30,1e-30))==CertifiedResponseFailure.RequestedWidth,"arithmetic floor");
        foreach(var m in new[]{0d,-1,double.NaN})Check(Try(n,-1,Double3.Zero,m,new(1,1,1),Fine)==CertifiedResponseFailure.InvalidPhysicalState,"invalid mass");
        foreach(var m in new[]{1e-100,1e100})Check(Try(n,-1,Double3.Zero,m,new(1,2,3),Broad)==CertifiedResponseFailure.None,"finite mass range");
        Check(Try(n,-1,new(1e308,1e308,1e308),1,new(1,1,1),Broad)==CertifiedResponseFailure.NumericalResolution,"unrepresentable lever arithmetic");
        Check(Try(n,-1,Double3.UnitY,1,new(1,1,double.Epsilon),Broad)==CertifiedResponseFailure.NumericalResolution,"reciprocal floor");
        Check(Try(n,-1,Double3.Zero,double.Epsilon,new(1,1,1),Broad)==CertifiedResponseFailure.NumericalResolution,"inverse mass overflow");
        Check(Try(n,new(-double.Epsilon,-double.Epsilon),Double3.Zero,1,new(1,1,1),Broad)==CertifiedResponseFailure.NumericalResolution,"positive impulse unrepresentable");
    }
    private static void Energy()
    {
        foreach(var surfaceSpeed in new[]{0,1,3})
        {
            R s=surfaceSpeed;R u=new(-3,2);var q=new DoubleQuaternion(.5,.5,.5,.5);var inertia=new PrincipalMomentsOfInertia(2,3,5);
            var exact=Solve(new(1,0,0),u,new(1,2,3),q,2,inertia);
            var before=new V(s+u,0,0);var after=before+exact.Linear*new R(1,2);
            var delta=V.Dot(after,after)-V.Dot(before,before)+(exact.Angular.X*exact.Angular.X/2+exact.Angular.Y*exact.Angular.Y/3+exact.Angular.Z*exact.Angular.Z/5)/2;
            Check(delta-exact.J*s==-u*u/(2*exact.K)&&delta-exact.J*s==new R(-27,68),"independent work-adjusted kinetic energies");
            if(surfaceSpeed==3)Check(delta>0,"positive inertial surface work allowed");
        }
        var impact=Solve(new(1,0,0),-1,Double3.Zero,DoubleQuaternion.Identity,1,new(1,1,1));
        Check(impact.J==1&&impact.J*impact.J/2==new R(1,2)&&impact.J*impact.J/2-impact.J==new R(-1,2),"moving surface strikes stationary craft");
    }
    private static void Representation()
    {
        var nearest=R.From(1d/3);var upward=R.From(Math.BitIncrement(1d/3));
        Check(-1+3*nearest==new R(-1,BigInteger.One<<54)&&-1+3*upward==new R(1,BigInteger.One<<53),"rounded scalar not exact inelastic application");
        var x=R.From(1/Math.Sqrt(3));var y=R.From(Math.Sqrt(2)/Math.Sqrt(3));Check(y*y-2*x*x!=0,"rounded normal not exactly parallel");
    }
    private static void AuthorityShape()
    {
        var root=default(FloridaContactProvider.Proof);var witness=default(FloridaContactProvider.Proof.Kinematics);
        Check(root.QualifyResponse(witness,Fine,default).Status==CertifiedResponseStatus.Unsupported,"default issuance");
        Check(default(FloridaContactProvider.Proof.ResponseProposal).Read(root,witness,Fine,default,out var data)==CertifiedResponseStatus.Unsupported&&data==default,"default receipt");
        Check(typeof(FloridaContactProvider.Proof.ResponseProposal).GetConstructors(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).All(x=>x.IsPrivate),"private receipt constructor");
        Check(!typeof(FloridaContactProvider.Proof.ResponseProposal).GetMethods(System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic)
            .SelectMany(x=>x.GetParameters()).Select(p=>p.ParameterType.IsByRef?p.ParameterType.GetElementType():p.ParameterType)
            .Any(t=>t==typeof(FloridaContactKinematics)||t==typeof(CertifiedResponseValues)),"copied snapshot cannot issue proof");
    }
}
