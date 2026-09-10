using NovaCore.Core;
using NovaCore.Simulation.Celestial;
using NovaCore.Simulation.Spacecraft.Contact;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;
using static ContactKinematicsOracle;

internal static class QualifiedContactKinematicsTests
{
    private static void Check(bool value,string name){if(!value)throw new InvalidOperationException("Root kinematics: "+name);}
    internal static void Run()
    {
        // Synthetic all-nonzero Euler rates detect omission of RA/declination, even if a current coefficient is small.
        var synthetic=new EarthContactOrientation(7,.4,63,-.7,31,11,100,2);var up=new Double3(.3,.4,.8);
        foreach(var alpha in new[]{.5m,1/Sqrt(2)})
        {
            var t=(double)alpha;var interval=new FloridaBound(Math.BitDecrement(t),Math.BitIncrement(t));
            if(alpha!=.5m)Check(2*D(interval.Lower)*D(interval.Lower)<1&&2*D(interval.Upper)*D(interval.Upper)>1,"independent irrational root inclusion");
            var actual=FloridaContactMotion.RootDirection(synthetic,interval,FloridaVector.From(up)/FloridaVector.From(up).Norm);
            var(n,omega)=Orientation(synthetic,alpha,up);
            Check(Contains(actual,n),"independent rational/irrational normal at same alpha");
            // Feature trajectory: rotate a material point plus normal gap h(alpha).
            // h=.5-t or h=1-2t^2 has the chosen exact mathematical root; h' is the approaching speed.
            var speed=alpha==.5m?-1:-4*alpha;
            var material=new V(2,3,-1);var featureVelocity=V.Cross(omega,material)+n*speed;
            Check(Math.Abs(V.Dot(n,featureVelocity-V.Cross(omega,material))-speed)<1e-23m,"moving material-point velocity identity");
            // Exercise the actual production derivative with magnified nonzero pole rates.
            // The oracle uses root-axis omega composition; production differentiates inverse rotations.
            var fixedP=new Double3(2,3,-1);var fixedV=new Double3(-4,2,-1);
            FloridaContactMotion.BodyFixed(synthetic,interval,FloridaVector.From(fixedP),FloridaVector.From(fixedV),out _,out var derivative);
            var syntheticSpeed=FloridaVector.Dot(FloridaVector.From(up),derivative)/FloridaVector.From(up).Norm;
            Check(Contains(syntheticSpeed,V.Dot(n,V.From(fixedV)-V.Cross(omega,V.From(fixedP)))),"production derivative includes every Euler rate");
        }
        var system=SolAnalyticalDefinition.Instance;system.TryGetNode(SolarSystemBodyIds.Earth,out var earth);
        system.TryGetAnalyticalKepler(earth.Ephemeris.PayloadIndex,out var trajectory);CelestialBodyOrientationEvaluator.TryGetContactProofModel(out var model);
        var offset=new Double3(1,-2,3);var attitude=new DoubleQuaternion(0,0,.6,.8);
        var linear=new SpacecraftTranslationState(new(1),new(1),SimulationInstant.Zero,trajectory.StateAtEpoch.Position+new Double3(1000,2000,-3000),
            trajectory.StateAtEpoch.Velocity+new Double3(2,-3,5),new(2,-1,3));
        var angular=new SpacecraftRigidBodyRotationState(new(1),SimulationInstant.Zero,attitude,Double3.Zero,new(2,3,4),Double3.Zero,RigidBodyRotationModel.ConstantBodyTorqueV1);
        Check(FloridaContactMotion.TryCreate(system,linear,angular,new(8),offset,out var motion),"source model");
        Check(motion.ContactKinematics(0,up,out var normal,out var actualSpeed,out var lever),"seed kinematic fields");
        var expectedLever=Lever(attitude,offset);var(n0,omega0)=Orientation(model,0,up);
        var p=V.From(linear.PositionRoot)-V.From(trajectory.StateAtEpoch.Position)+expectedLever;
        var v=V.From(linear.VelocityRoot)-V.From(trajectory.StateAtEpoch.Velocity);
        Check(Contains(normal,n0)&&Contains(lever,expectedLever),"independent seed normal and matrix lever");
        Check(Contains(actualSpeed,V.Dot(n0,v-V.Cross(omega0,p))),"all-term material velocity vs analytic q derivative");
        Check(default(FloridaContactProvider.Proof).QualifyKinematics(new(1,1,1,1,0),default).Status==FloridaKinematicsStatus.Unsupported,"default root refused");
        Check(default(FloridaContactProvider.Proof.Kinematics).Read(default,default,out _)==FloridaKinematicsStatus.Unsupported,"default witness refused");
        Check(typeof(FloridaContactProvider.Proof.Kinematics).GetConstructors(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).All(x=>x.IsPrivate),"private construction");
        Check(!new FloridaKinematicsRequest(0,1,1,1,0).IsValid&&!new FloridaKinematicsRequest(1,double.NaN,1,1,0).IsValid&&
            !new FloridaKinematicsRequest(1,1,1,1,25).IsValid,"explicit request limits");
        Console.WriteLine("QUALIFIED_KINEMATICS_ANALYTICAL rational/irrational/same-alpha/material-velocity/normalized-lever/default PASS");
    }
}
