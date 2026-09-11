using System.Numerics;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Contact;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;
using R = CertifiedResponseOracle.R;
using B = CertifiedResponseOracle.B;

internal static class PrivateCanonicalPropagationTests
{
    internal static void Check(bool value,string contract)
    {if(!value)throw new InvalidOperationException("Private propagation: "+contract);}
    internal static void Run()
    {
        Translation(); Majorants(); Rotation(); NoncommutingAttitude(); RealizationAndRefusals();
        Console.WriteLine("PRIVATE_PROPAGATION_ANALYTICAL correlation/extrema/irrational/epoch/majorant/spin/normalization/refusal/recontact PASS");
    }
    private static SpacecraftTranslationState Linear(Double3 x,Double3 v,Double3 f,long epoch=0)=>new(new(1),new(1),new(epoch),x,v,f);
    private static void Translation()
    {
        var s=Linear(new(1.3e11,-7e10,1),new(3e4,-2e4,0),Double3.Zero);
        Check(PrivatePropagationMath.Translation(s,8,s.VelocityRoot,new(.1,.2),new(1_000_000),out var a)==PrivatePropagationFailure.None,"coast A");
        Check(PrivatePropagationMath.Translation(s,8,s.VelocityRoot,new(.8,.9),new(1_000_000),out var b)==PrivatePropagationFailure.None&&a==b,
            "exact no-impulse cancellation independent of root bracket");
        AssertTranslation(s,8,s.VelocityRoot,new(.1,.2),new(1_000_000),a);
        var irrational=new FloridaBound(.7071067811865475,.7071067811865476);
        var lo=R.From(irrational.Lower);var hi=R.From(irrational.Upper);
        Check(2*lo*lo<1&&2*hi*hi>1,"independent irrational root bracket");
        s=Linear(Double3.Zero,Double3.Zero,new(1,0,0));var plus=new Double3(.5,0,0);
        Check(PrivatePropagationMath.Translation(s,1,plus,irrational,new(1_000_000),out a)==PrivatePropagationFailure.None,"irrational continuation");
        AssertTranslation(s,1,plus,irrational,new(1_000_000),a);
        Check(CertifiedResponseOracle.Contains(a.Position.X,new R(3,2)-new R(3,2)*lo)&&
            CertifiedResponseOracle.Contains(a.Position.X,new R(3,2)-new R(3,2)*hi),"X=3/2-3alpha/2 at alpha²=1/2 enclosure");
        s=Linear(new(8,-3,1e11),new(-2,4,-3e4),new(2,-2,0),-200_001);
        plus=new(0,0,-3e4);var root=new FloridaBound(.2,.9);
        Check(PrivatePropagationMath.Translation(s,1,plus,root,new(1_000_000),out a)==PrivatePropagationFailure.None,"force and nonzero epoch");
        AssertTranslation(s,1,plus,root,new(1_000_000),a);
        s=Linear(new(double.MaxValue,0,0),new(double.MaxValue,0,0),Double3.Zero);
        Check(PrivatePropagationMath.Translation(s,1,s.VelocityRoot,root,new(1_000_000),out a)==PrivatePropagationFailure.NonFinite&&a==default,"overflow no partial result");
    }

    /// <summary>Exact rational whole-range oracle evaluates original root pose plus remainder, not production's rearrangement.</summary>
    internal static void AssertTranslation(in SpacecraftTranslationState source,double mass,Double3 plus,
        FloridaBound root,SimulationInstant target,PrivateTranslationEnclosure actual)
    {
        var t0=new R(source.Epoch.Ticks,SimulationInstant.TicksPerSecond);var t=new R(target.Ticks,SimulationInstant.TicksPerSecond);
        var l=R.From(root.Lower);var h=R.From(root.Upper);var m=R.From(mass);
        Component(source.PositionRoot.X,source.VelocityRoot.X,source.ConstantForceRoot.X,plus.X,actual.Position.X,actual.Velocity.X);
        Component(source.PositionRoot.Y,source.VelocityRoot.Y,source.ConstantForceRoot.Y,plus.Y,actual.Position.Y,actual.Velocity.Y);
        Component(source.PositionRoot.Z,source.VelocityRoot.Z,source.ConstantForceRoot.Z,plus.Z,actual.Position.Z,actual.Velocity.Z);
        void Component(double position,double velocity,double force,double after,FloridaBound x,FloridaBound v)
        {
            var p=R.From(position);var v0=R.From(velocity);var acc=R.From(force)/m;var vp=R.From(after);
            R X(R alpha){var e=alpha-t0;var d=t-alpha;return p+v0*e+acc*e*e/2+vp*d+acc*d*d/2;}
            var first=X(l);var last=X(h);var minimum=first<last?first:last;var maximum=first>last?first:last;
            if(acc!=(R)0)
            {var vertex=-(v0-vp-acc*(t0+t))/(2*acc);if(vertex>=l&&vertex<=h){var z=X(vertex);if(z<minimum)minimum=z;if(z>maximum)maximum=z;}}
            Check(CertifiedResponseOracle.Contains(x,minimum)&&CertifiedResponseOracle.Contains(x,maximum),"exact full quadratic extrema");
            Check(CertifiedResponseOracle.Contains(v,vp+acc*(t-l))&&CertifiedResponseOracle.Contains(v,vp+acc*(t-h)),"exact velocity range");
        }
    }
    private static R Pow(R x,int n){R p=1;for(var i=0;i<n;i++)p*=x;return p;}
    private static void Majorants()
    {
        foreach(var (omega,c,d) in new[]{(0d,0d,1d),(.3,0d,.5),(.4,.5,.52),(2d,1d,.125),(double.Epsilon,double.Epsilon,1d)})
        {
            Check(PrivatePropagationMath.RemainderBounds(omega,c,d,out var w,out var q),"finite uniform remainder");
            var o=R.From(omega);var k=R.From(c);var time=R.From(d);
            var wb=Pow(o,13)*Pow(k,12)*Pow(time,12);var qb=Pow(o,12)*Pow(time,12);
            for(var j=0;j<12;j++)qb=qb*(new R(3,2)+j*k)/(j+1);
            Check(R.From(w)>=wb&&R.From(q)>=qb,"independent exact closed-form derivative majorant");
        }
        Check(!PrivatePropagationMath.RemainderBounds(double.MaxValue,1,1,out _,out _),"overflowed majorant refuses");
    }
    private static void Rotation()
    {
        foreach(var inertia in new[]{new PrincipalMomentsOfInertia(2,3,4),new(2,2,2)})
        {
            var status=PrivatePropagationMath.Rotation(DoubleQuaternion.Identity,new(0,0,.25),inertia,new(.5,.5),out var value);
            Check(status==PrivatePropagationFailure.None,"principal-axis and spherical rotation");
            Check(CertifiedResponseOracle.Contains(value.Spin,CertifiedResponseOracle.V.From(new(0,0,.25))),"constant principal-axis spin");
            var angle=new R(1,16);var sin=Trig(angle,false);var cos=Trig(angle,true);
            Check(CertifiedResponseOracle.Contains(value.Attitude.Z,sin.L)&&CertifiedResponseOracle.Contains(value.Attitude.Z,sin.H)&&
                CertifiedResponseOracle.Contains(value.Attitude.W,cos.L)&&CertifiedResponseOracle.Contains(value.Attitude.W,cos.H),"exact-rational alternating-series quaternion oracle");
        }
        Check(PrivatePropagationMath.Rotation(new(0,0,0,2),Double3.Zero,new(2,3,4),new(1,1),out var zero)==PrivatePropagationFailure.None&&
            zero.Attitude.W.Contains(1)&&zero.Spin.X.Contains(0),"zero spin and exact normalized input");
        var spin=new Double3(.2,.1,.05);var ii=new PrincipalMomentsOfInertia(2,3,4);
        Check(PrivatePropagationMath.Rotation(DoubleQuaternion.Identity,spin,ii,new(.1,.1),out var asymmetric)==PrivatePropagationFailure.None,"asymmetric flow");
        Check(asymmetric.Spin.X.Upper<spin.X&&asymmetric.Spin.Y.Lower>spin.Y&&asymmetric.Spin.Z.Upper<spin.Z,"body spin actually evolves");
        var h=R.From(ii.X)*Pow(R.From(spin.X),2)+R.From(ii.Y)*Pow(R.From(spin.Y),2)+R.From(ii.Z)*Pow(R.From(spin.Z),2);
        foreach(var moment in new[]{ii.X,ii.Y,ii.Z})Check(Pow(R.From(asymmetric.GlobalSpinBound),2)>=h/R.From(moment),"energy bounds global spin");
        foreach(var coefficient in new[]{new R(-1,2),new R(2,3),new R(-1,4)})
            Check(R.From(asymmetric.EulerCoefficientBound)>=(coefficient<0?-coefficient:coefficient),"exact Euler coefficient bound");
        // Independent banked RK4 is a coarse numerical cross-check only, never the truncation certificate.
        var canonical=new SpacecraftRigidBodyRotationState(new(1),new(0),DoubleQuaternion.Identity,spin,ii,Double3.Zero,RigidBodyRotationModel.ConstantBodyTorqueV1);
        var comparison=SpacecraftRigidBodyRotationEvaluator.TryEvaluate(canonical,new(100_000));
        Check(comparison.Status==SpacecraftRigidBodyRotationEvaluationStatus.Success,"banked comparison");
        Check((comparison.AngularVelocityBody-new Double3((asymmetric.Spin.X.Lower+asymmetric.Spin.X.Upper)/2,
            (asymmetric.Spin.Y.Lower+asymmetric.Spin.Y.Upper)/2,(asymmetric.Spin.Z.Lower+asymmetric.Spin.Z.Upper)/2)).LengthSquared<1e-20,
            "supplemental banked physical-equation cross-check");
    }
    private static (R L,R H) Trig(R x,bool cosine)
    {
        R term=cosine?(R)1:x;var sum=term;
        for(var k=1;k<=20;k++){var n=cosine?2*k-1:2*k;term=-term*x*x/(n*(n+1));sum+=term;}
        var next=cosine?41:42;var remainder=-term*x*x/(next*(next+1));
        return remainder<0?(sum+remainder,sum):(sum,sum+remainder);
    }
    private static void NoncommutingAttitude()
    {
        // Exact unit q0; q0*qX differs from qX*q0. Component formulas are independent
        // of the production quaternion convolution and catch reversed frame/multiplication order.
        var source=Input(new(.5,.5,.5,.5),new(.25,0,0),new(.5,.5));
        var request=new PrivatePropagationRequest(1e-8,1e-8,1e-6,1e-6);
        Check(PrivatePropagationMath.Evaluate(source,source.SourceEnd,request,out _,out var v)==PrivatePropagationFailure.None,"noncommuting attitude realization");
        var sine=Trig(new R(1,16),false);var cosine=Trig(new R(1,16),true);
        var sum=new B((sine.L+cosine.L)/2,(sine.H+cosine.H)/2);
        var difference=new B((cosine.L-sine.H)/2,(cosine.H-sine.L)/2);
        B[] truth=[sum,sum,difference,difference];
        FloridaBound[] enclosed=[v.Rotation.Attitude.X,v.Rotation.Attitude.Y,v.Rotation.Attitude.Z,v.Rotation.Attitude.W];
        for(var i=0;i<4;i++)Check(CertifiedResponseOracle.Contains(enclosed[i],truth[i]),"independent noncommuting exact-series enclosure");
        var q=v.OrientationBodyToRoot;R[] bits=[R.From(q.X),R.From(q.Y),R.From(q.Z),R.From(q.W)];R normSquared=0;
        foreach(var x in bits)normSquared+=x*x;
        // Exact rational bisection encloses sqrt(sum(final stored bits²)); no production sqrt,
        // normalized-meaning helper or binary64 norm calculation participates in this oracle.
        R lo=0,hi=2;
        Check(hi*hi>=normSquared&&normSquared>0,"finite oracle norm bracket");
        for(var i=0;i<160;i++){var mid=(lo+hi)/2;if(mid*mid<=normSquared)lo=mid;else hi=mid;}
        var norm=new B(lo,hi);B minus=0,plus=0;
        for(var i=0;i<4;i++){var normalized=B.Point(bits[i])/norm;minus+=(truth[i]-normalized).Square();plus+=(truth[i]+normalized).Square();}
        var independentSquared=minus.H<plus.H?minus.H:plus.H;
        Check(Pow(R.From(v.AttitudeChordalError),2)>=independentSquared,"certificate contains independent FINAL-bit exact-normalization attitude error");
    }
    private static PrivatePostImpactStateValues Input(DoubleQuaternion q,Double3 w,FloridaBound root)=>
        new(default,root,Linear(new(2,3,4),new(1,0,0),Double3.Zero),
            new(new(1),new(0),q,Double3.Zero,new(2,3,4),Double3.Zero,RigidBodyRotationModel.ConstantBodyTorqueV1),
            new(8),default,default,new(0),new(1_000_000),default,default,new(1,0,0),w);
    private static void RealizationAndRefusals()
    {
        var request=new PrivatePropagationRequest(1e-8,1e-8,1e-6,1e-6);
        var source=Input(new(0,0,.6,.8),new(.2,.1,.05),new(.5,.500001));
        var f=PrivatePropagationMath.Evaluate(source,source.SourceEnd,request,out _,out var value);
        Check(f==PrivatePropagationFailure.None,"represented paired result: "+f);
        var opposite=source with {FrozenSourceRotation=source.FrozenSourceRotation with {OrientationLocalToParent=new(0,0,-.6,-.8)}};
        Check(PrivatePropagationMath.Evaluate(opposite,opposite.SourceEnd,request,out _,out var sign)==PrivatePropagationFailure.None&&
            value.OrientationBodyToRoot==sign.OrientationBodyToRoot,"final canonical-sign representation");
        var finalMeaning=PropagationQuaternion.NormalizedMeaning(value.OrientationBodyToRoot);
        var actual=Math.Min((value.Rotation.Attitude-finalMeaning).Norm.Upper,(value.Rotation.Attitude+finalMeaning).Norm.Upper);
        Check(actual==value.AttitudeChordalError&&actual<=request.AttitudeChordal,"certificate covers FINAL normalized quaternion bits");
        foreach(var invalid in new[]{default(PrivatePropagationRequest),request with {PositionMetres=double.NaN},request with {AttitudeChordal=1}})
            Check(PrivatePropagationMath.Evaluate(source,source.SourceEnd,invalid,out _,out var empty)==PrivatePropagationFailure.InvalidRequest&&empty==default,"invalid request no partial state");
        Check(PrivatePropagationMath.Evaluate(source,new(999_999),request,out _,out _)==PrivatePropagationFailure.UnsupportedTarget,"SourceEnd only");
        Check(PrivatePropagationMath.Evaluate(source,source.SourceEnd,request with {PositionMetres=1e-30},out _,out var missing)==PrivatePropagationFailure.EndpointResolution&&missing==default,"tight endpoint refusal");
        var fast=source with {InitialAngularVelocityBody=new(100,50,25)};
        Check(PrivatePropagationMath.Evaluate(fast,fast.SourceEnd,request,out _,out missing)==PrivatePropagationFailure.RotationWorkLimit&&missing==default,"fixed-order work-cap refusal");
        Check(PrivatePropagationMath.Rotation(DoubleQuaternion.Identity,Double3.Zero,new(2,3,4),new(0,1.1),out var none)==PrivatePropagationFailure.RotationWorkLimit&&none==default,"no extended horizon");
        var near=Input(DoubleQuaternion.Identity,Double3.Zero,new(Math.BitDecrement(1),1));
        Check(PrivatePropagationMath.Evaluate(near,near.SourceEnd,request,out var d,out _) == PrivatePropagationFailure.None&&d.Lower==0,
            "strict-root theorem permits outward duration enclosure touching zero; no nominal alpha");
        var after=source with {PoseRootEnclosure=new(1.1,1.2)};
        Check(PrivatePropagationMath.Evaluate(after,after.SourceEnd,request,out var absentDuration,out var absentEndpoint)==PrivatePropagationFailure.RootTargetOrder&&
            absentDuration==default&&absentEndpoint==default,"unproved root/target order has no partial result");
        var penetrating=Input(DoubleQuaternion.Identity,Double3.Zero,new(.5,.5));
        penetrating=penetrating with {FrozenSourceTranslation=Linear(Double3.Zero,Double3.Zero,new(-16,0,0)),InitialLinearVelocityRoot=new(.1,0,0)};
        Check(PrivatePropagationMath.Evaluate(penetrating,penetrating.SourceEnd,request,out _,out _) == PrivatePropagationFailure.None,"conditional free flow can qualify despite recontact");
        // Flat-plane isolated normal control: g(d)=.1d-d², beta-alpha=.1 < .5.
        Check(new R(1,10)*new R(1,2)-new R(1,4)<0,"later contact defeats publication, not numerical propagation");
    }
}
