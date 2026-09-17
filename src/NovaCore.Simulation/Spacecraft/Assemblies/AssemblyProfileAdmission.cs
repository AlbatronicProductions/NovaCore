using NovaCore.Core;

namespace NovaCore.Simulation.Spacecraft.Assemblies;

internal readonly record struct AssemblyWrench(Double3 Force,Double3 MomentAtOrigin);
internal readonly record struct AssemblyGimbal(double ActualY,double ActualZ,double TargetY,double TargetZ);

internal static class AssemblyActuation
{
    internal static AssemblyWrench Jet(CompiledPart p)
    {
        var e=p.Definition.Propulsion!;var f=p.Instance.Pose.Rotation.Apply(e.Axis)*e.FullThrustN;
        return new(f,Double3.Cross(p.Instance.Pose.Point(e.Point),f));
    }
    internal static AssemblyWrench Jet(CompiledAssemblyJet jet)
    {
        var e=jet.Propulsion;var pose=jet.Instance.Pose;var f=pose.Rotation.Apply(e.Axis)*e.FullThrustN;
        return new(f,Double3.Cross(pose.Point(e.Point),f));
    }
    internal static AssemblyWrench Main(CompiledAssemblyDesign d,double y,double z)
    {
        var e=d.Main.Definition.Propulsion!;var g=d.Main.Definition.Gimbal!;
        var sy=Math.Sin(y);var cy=Math.Cos(y);var sz=Math.Sin(z);var cz=Math.Cos(z);
        var r=new Matrix3(cz*cy,-sz,cz*sy,sz*cy,cz,sz*sy,-sy,0,cy);
        var f=d.Main.Instance.Pose.Rotation.Apply(r.Apply(e.Axis))*e.FullThrustN;
        // The admitted nozzle offset is exactly coaxial with force, so its
        // moment is identically zero. Avoid subtracting rounded large products.
        return new(f,Double3.Cross(d.Main.Instance.Pose.Point(g.Pivot),f));
    }
    internal static AssemblyGimbal Next(CompiledAssemblyDesign d,AssemblyGimbal old,double y,double z,long ticks)
    {
        if(!double.IsFinite(y)||!double.IsFinite(z))throw new InvalidDataException("Nonfinite gimbal target.");
        var g=d.Main.Definition.Gimbal!;y=Math.Clamp(y,-g.LimitY,g.LimitY);z=Math.Clamp(z,-g.LimitZ,g.LimitZ);
        var step=g.SlewRate*ticks/1_000_000d;
        static double Move(double a,double b,double step)=>a+Math.Clamp(b-a,-step,step);
        return new(Move(old.ActualY,y,step),Move(old.ActualZ,z,step),y,z);
    }
}

internal static class AssemblyProfileAdmission
{
    internal static void Validate(CompiledAssemblyDesign d)
    {
        AssemblyResources.Validate(d,new(AssemblyResources.Mass(d.Data.Design.InitialFuelKg),AssemblyResources.Mass(d.Data.Design.InitialOxidizerKg)));
        var dry=d.ObserveMass(d.DryMass);var wet=d.ObserveMass(d.DryMass+100);
        if(!dry.Inertia.PhysicalInertia||!wet.Inertia.PhysicalInertia||dry.Com.X<0||dry.Com.X>1.17||Math.Min(dry.Inertia.A,Math.Min(dry.Inertia.E,dry.Inertia.I))<190||Math.Max(wet.Inertia.A,Math.Max(wet.Inertia.E,wet.Inertia.I))>758)
            throw new InvalidDataException("Outside proven mass/inertia envelope.");
        var g=d.Main.Definition.Gimbal!;
        if(d.Main.Instance.Pose.Rotation!=Matrix3.Identity||d.Main.Instance.Pose.Point(g.Pivot).Y!=0||d.Main.Instance.Pose.Point(g.Pivot).Z!=0||Math.Abs(d.Main.Instance.Pose.Point(g.Pivot).X)>1.1)
            throw new InvalidDataException("Outside proven axial main-pivot envelope.");
        var largestPairTorque=0d;
        foreach(var mass in new[]{dry,wet})foreach(var pair in d.Data.Design.Pairs)
        {
            var a=AssemblyActuation.Jet(d.Jet(pair.First,pair.FirstActuator));var b=AssemblyActuation.Jet(d.Jet(pair.Second,pair.SecondActuator));
            var f=a.Force+b.Force;var tau=a.MomentAtOrigin+b.MomentAtOrigin-Double3.Cross(mass.Com,f);
            var alpha=mass.Inertia.Inverse().Apply(tau);var x=new[]{alpha.X,alpha.Y,alpha.Z};
            if(x[pair.Axis]*pair.Sign<.005||Enumerable.Range(0,3).Any(i=>i!=pair.Axis&&Math.Abs(x[i])>1e-12)||(!d.HasIndependentBlockJets&&f.LengthSquared==0))
                throw new InvalidDataException("Insufficient or coupled signed-axis authority.");
            largestPairTorque=Math.Max(largestPairTorque,Math.Sqrt(tau.LengthSquared));
        }
        // Each signed-axis response is affine/affine in mass under fixed S/IO,
        // with positive denominator. Its derivative cannot change sign, so the
        // endpoint tests prove the interval response. Norm of affine torque in
        // 1/M is convex: its maximum is likewise at an endpoint.
        var tilt=Math.Sqrt(1-Math.Pow(Math.Cos(.05),4));
        var torque=d.Main.Definition.Propulsion!.FullThrustN*(1.1+dry.Com.X)*tilt+largestPairTorque;
        var minI=Math.Min(dry.Inertia.A,Math.Min(dry.Inertia.E,dry.Inertia.I));
        var maxI=Math.Max(wet.Inertia.A,Math.Max(wet.Inertia.E,wet.Inertia.I));
        var omega=(maxI*.05+torque*2)/minI;
        var speed=.25+.05*dry.Com.X+645*2/d.DryMass+omega*(dry.Com.X-wet.Com.X)+dry.Com.X*omega;
        if(!double.IsFinite(omega)||omega>=1.72||!double.IsFinite(speed)||speed>=4.65)throw new InvalidDataException("Dynamic bound fails qualified envelope.");
    }
}
