using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Actuation;

namespace NovaCore.Simulation.Spacecraft.Assemblies;

internal readonly record struct AssemblyMotion(Double3 PositionO,Double3 VelocityO,DoubleQuaternion BodyToWorld,Double3 AngularVelocityBody)
{
    internal bool Finite=>PositionO.IsFinite&&VelocityO.IsFinite&&BodyToWorld.IsFinite&&AngularVelocityBody.IsFinite;
}

/// <summary>Qualified short-step co-moving point-removal law at fixed material O.
/// Exact store amounts are only observed here. This evaluator cannot spend or publish.</summary>
internal static class AssemblyDynamics
{
    internal static AssemblyMotion Evaluate(CompiledAssemblyDesign d,AssemblyMotion source,AssemblyConsumption c,AssemblyWrench poweredWrench)
    {
        var motion=source;
        for(var phase=0;phase<2;phase++)
        {
            var powered=phase==0;
            var duration=powered?c.Powered:c.Unpowered;
            if(duration.IsZero)continue;
            if(!PoweredFlightNumerics.TrySeconds(duration,out var h))throw new InvalidDataException("Invalid exact duration.");
            var final=AssemblyResources.Add(c.After.Fuel,c.After.Oxidizer);
            var dry=AssemblyResources.Mass(d.DryMass);
            double m1,m2,m4;
            if(powered)
            {
                // D=2, total mass 630..830 => weighted mass 1260..1660 kg,
                // normal binary64; exact integer product <=1106 bits. This is
                // separately bounded, not inherited from the old dry-8 fixture.
                if(!PoweredFlightNumerics.TryStageMass(dry,final,c.ConsumedTotal,0,2,out m1)||
                   !PoweredFlightNumerics.TryStageMass(dry,final,c.ConsumedTotal,1,2,out m2)||
                   !PoweredFlightNumerics.TryStageMass(dry,final,c.ConsumedTotal,2,2,out m4))throw new InvalidDataException("Exact stage mass failed.");
            }
            else
            {
                if(!AssemblyResources.Add(dry,final).TryToKilograms(out m1))throw new InvalidDataException("Mass observation failed.");
                m2=m4=m1;
            }
            var w=powered?poweredWrench:default;
            var k1=Derivative(d,motion,m1,w);var k2=Derivative(d,Add(motion,k1,h,2),m2,w);
            var k3=Derivative(d,Add(motion,k2,h,2),m2,w);var k4=Derivative(d,Add(motion,k3,h,1),m4,w);
            motion=Add(motion,Combine(k1,k2,k3,k4),h,6);
            var q=motion.BodyToWorld.Normalized();if(q.W<0)q=new(-q.X,-q.Y,-q.Z,-q.W);
            motion=motion with {BodyToWorld=q};
            if(!InEnvelope(motion))throw new InvalidDataException("Motion outside declared envelope.");
        }
        return motion;
    }
    internal static bool InEnvelope(AssemblyMotion s)=>s.Finite&&s.VelocityO.LengthSquared<=25&&s.AngularVelocityBody.LengthSquared<=4&&
        s.PositionO.LengthSquared<=10000&&Math.Abs(s.BodyToWorld.LengthSquared-1)<=1e-12;
    private static AssemblyMotion Derivative(CompiledAssemblyDesign d,AssemblyMotion s,double mass,AssemblyWrench wrench)
    {
        var props=d.ObserveMass(mass);var omega=s.AngularVelocityBody;
        var tau=wrench.MomentAtOrigin-Double3.Cross(props.Com,wrench.Force);
        var alpha=props.Inertia.Inverse().Apply(tau-Double3.Cross(omega,props.Inertia.Apply(omega)));
        var a=wrench.Force/mass-Double3.Cross(alpha,props.Com)-Double3.Cross(omega,Double3.Cross(omega,props.Com));
        var q=s.BodyToWorld*new DoubleQuaternion(omega.X,omega.Y,omega.Z,0);
        return new(s.VelocityO,s.BodyToWorld.Rotate(a),new(q.X*.5,q.Y*.5,q.Z*.5,q.W*.5),alpha);
    }
    private static Double3 Scaled(Double3 v,PoweredBinaryScale h,double divisor)=>new(h.MultiplyDivide(v.X,divisor),h.MultiplyDivide(v.Y,divisor),h.MultiplyDivide(v.Z,divisor));
    private static AssemblyMotion Add(AssemblyMotion s,AssemblyMotion k,PoweredBinaryScale h,double n)=>new(s.PositionO+Scaled(k.PositionO,h,n),s.VelocityO+Scaled(k.VelocityO,h,n),
        new(s.BodyToWorld.X+h.MultiplyDivide(k.BodyToWorld.X,n),s.BodyToWorld.Y+h.MultiplyDivide(k.BodyToWorld.Y,n),s.BodyToWorld.Z+h.MultiplyDivide(k.BodyToWorld.Z,n),s.BodyToWorld.W+h.MultiplyDivide(k.BodyToWorld.W,n)),s.AngularVelocityBody+Scaled(k.AngularVelocityBody,h,n));
    private static AssemblyMotion Combine(AssemblyMotion a,AssemblyMotion b,AssemblyMotion c,AssemblyMotion d)=>new(a.PositionO+b.PositionO*2+c.PositionO*2+d.PositionO,a.VelocityO+b.VelocityO*2+c.VelocityO*2+d.VelocityO,
        new(a.BodyToWorld.X+2*b.BodyToWorld.X+2*c.BodyToWorld.X+d.BodyToWorld.X,a.BodyToWorld.Y+2*b.BodyToWorld.Y+2*c.BodyToWorld.Y+d.BodyToWorld.Y,a.BodyToWorld.Z+2*b.BodyToWorld.Z+2*c.BodyToWorld.Z+d.BodyToWorld.Z,a.BodyToWorld.W+2*b.BodyToWorld.W+2*c.BodyToWorld.W+d.BodyToWorld.W),a.AngularVelocityBody+b.AngularVelocityBody*2+c.AngularVelocityBody*2+d.AngularVelocityBody);
}
