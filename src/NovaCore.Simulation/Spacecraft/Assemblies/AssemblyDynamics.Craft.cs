using System.Collections.Immutable;
using NovaCore.Core;
using NovaCore.Simulation.Time;
using static NovaCore.Simulation.Spacecraft.Assemblies.AssemblyConstructionFacts;

namespace NovaCore.Simulation.Spacecraft.Assemblies;

internal readonly record struct CraftDynamicsResult(AssemblyMotion Motion,AssemblyGimbal Gimbal);
internal static partial class AssemblyDynamics
{
    // Numerical preparation only. This does not advance the canonical clock,
    // mutate a ledger, issue a command or own a live body.
    internal static CraftDynamicsResult Evaluate(CompiledCraftControl control,AssemblyMotion source,
        ConstructionPhysicalEvolution services,AssemblyGimbal gimbal,CraftControlRow row,Double3 gravityRoot=default,int refinement=0,
        AssemblyFloridaSite? site=null,SimulationInstant epoch=default)
    {
        var craft=control.Craft;Require(refinement is >=0 and <=5&&source.Finite&&gravityRoot.IsFinite&&Math.Abs(source.BodyToWorld.LengthSquared-1)<1e-12,
            "Physical dynamics source/refinement range.");
        Require(ReferenceEquals(services.Fuel.Network,craft.Fuel)&&ReferenceEquals(services.Power.Network,craft.Power),"Physical dynamics service identity.");
        var motion=source;var attempts=0;var phaseOffset=0d;
        foreach(var phase in services.Phases){
            Require(ReferenceEquals(phase.Before.Network,craft.Fuel)&&ReferenceEquals(phase.After.Network,craft.Fuel)&&
                phase.Active.Length==craft.Fuel.Consumers.Length&&phase.DeliveredLoads.Length==craft.Power.Modules.Length,"Physical dynamics phase identity/dimensions.");
            var duration=ConstructionNumerics.Seconds(phase.Ticks);Require(duration<=1d/64,"Physical dynamics requires bounded host intervals.");
            var before=ConstructionNumerics.Quantities(phase.Before);var after=ConstructionNumerics.Quantities(phase.After);
            for(var i=0;i<before.Length;i++)Require(after[i]<=before[i],"Physical dynamics cannot credit propellant.");
            var delivered=control.Main.DataReachable;foreach(var load in control.Main.RequiredLoads)delivered&=phase.DeliveredLoads[load];
            var startGimbal=gimbal;
            AssemblyMass Mass(double fraction){Span<double> values=stackalloc double[before.Length];for(var i=0;i<values.Length;i++)values[i]=fraction==0?before[i]:fraction==1?after[i]:Math.Clamp(before[i]+(after[i]-before[i])*fraction,after[i],before[i]);return craft.Mass.EvaluateDynamic(values);}
            AssemblyWrench Wrench(double fraction){var at=AssemblyActuation.Next(control.Main,startGimbal,row.TargetY,row.TargetZ,duration*fraction,delivered);return AssemblyActuation.Resolve(craft,phase.Active.AsSpan(),at);}
            AssemblyMotion Step(AssemblyMotion from,double time,double h){
                var a=Math.Clamp(time/duration,0,1);var b=Math.Clamp((time+h*.5)/duration,0,1);var c=Math.Clamp((time+h)/duration,0,1);
                var m1=Mass(a);var m2=Mass(b);var m4=Mass(c);
                var w1=Wrench(a);var w2=Wrench(b);var w4=Wrench(c);
                var k1=Derivative(from,m1,w1,time);
                var k2=Derivative(CraftAdd(from,k1,h*.5),m2,w2,time+h*.5);
                var k3=Derivative(CraftAdd(from,k2,h*.5),m2,w2,time+h*.5);
                var k4=Derivative(CraftAdd(from,k3,h),m4,w4,time+h);
                var result=CraftAdd(from,Combine(k1,k2,k3,k4),h/6);
                var q=result.BodyToWorld.Normalized();if(q.W<0)q=new(-q.X,-q.Y,-q.Z,-q.W);
                return result with {BodyToWorld=q};
            }
            AssemblyMotion Derivative(AssemblyMotion s,AssemblyMass m,AssemblyWrench w,double offset)=>site is null?
                CraftDerivative(s,m,w,gravityRoot):CraftSiteDerivative(s,m,w,site,site.At(epoch,phaseOffset+offset));
            var torqueBound=CraftTorqueBound(control,phase.Active.AsSpan());var aBound=torqueBound/control.MinimumInertia;var bBound=control.MaximumInertia/control.MinimumInertia;
            var elapsed=0d;
            while(elapsed<duration){
                // Bootstrap |w|<=U over this whole trial: h*(a+b*U^2)<U-|w0|.
                // Also keep angular travel small, preventing high-rate aliasing
                // from fooling the step-doubling comparison.
                var upper=CraftNorm(motion.AngularVelocityBody)+1;
                var derivativeBound=aBound+bBound*upper*upper;
                if(site is not null)derivativeBound=aBound+bBound*Math.Pow(upper+site.AngularSpeedBound,2)+site.AngularAccelerationBound+upper*site.AngularSpeedBound;
                var h=Math.Min(duration-elapsed,Math.Min(Math.ScaleB(1d/1024,-refinement),Math.Min(1/(32*upper),.5/derivativeBound)));
                Require(double.IsFinite(h)&&h>0&&elapsed+h>elapsed,"Physical angular/time precision envelope exceeded.");
                while(true){
                    Require(++attempts<=16384,"Physical numerical preparation capacity exceeded.");
                    var coarse=Step(motion,elapsed,h);var middle=Step(motion,elapsed,h*.5);var fine=Step(middle,elapsed+h*.5,h*.5);
                    if(CraftClose(coarse,fine)){motion=fine;elapsed=Math.Min(duration,elapsed+h);break;}
                    h*=.5;Require(h>0&&elapsed+h>elapsed,"Physical refinement precision envelope exceeded.");
                }
            }
            gimbal=AssemblyActuation.Next(control.Main,startGimbal,row.TargetY,row.TargetZ,duration,delivered);
            phaseOffset+=duration;
        }
        return new(motion,gimbal);
    }
    private static double CraftNorm(Double3 value){var max=Math.Max(Math.Abs(value.X),Math.Max(Math.Abs(value.Y),Math.Abs(value.Z)));return max==0?0:max*Math.Sqrt((value/max).LengthSquared);}
    internal static double CraftTorqueBound(CompiledCraftControl control,ReadOnlySpan<bool> active)
    {
        var craft=control.Craft;var force=Double3.Zero;var moment=Double3.Zero;
        foreach(var actuator in craft.Actuators)if(active[actuator.Consumer]&&actuator!=control.Main){var f=actuator.Axis*actuator.Thrust;force+=f;moment+=Double3.Cross(actuator.Point,f);}
        var bound=0d;var main=control.Main;var g=main.Gimbal!;var pivot=main.PartOrigin+main.PartRotation.Apply(g.Pivot);
        foreach(var x in new[]{craft.Mass.ComMinimum.X,craft.Mass.ComMaximum.X})foreach(var y in new[]{craft.Mass.ComMinimum.Y,craft.Mass.ComMaximum.Y})foreach(var z in new[]{craft.Mass.ComMinimum.Z,craft.Mass.ComMaximum.Z}){
            var com=new Double3(x,y,z);var torque=CraftNorm(moment-Double3.Cross(com,force));
            if(active[main.Consumer])torque+=main.Thrust*(Math.Abs(pivot.X-x)*Math.Min(1,Math.Sqrt(Math.Pow(Math.Sin(g.LimitY),2)+Math.Pow(Math.Sin(g.LimitZ),2)))+CraftNorm(new(0,pivot.Y-y,pivot.Z-z)));
            bound=Math.Max(bound,torque);
        }
        return Math.BitIncrement(bound*(1+16e-12));
    }
    private static bool CraftClose(AssemblyMotion a,AssemblyMotion b)
    {
        if(!a.Finite||!b.Finite)return false;
        const double epsilon=128*2.2204460492503131e-16;
        static bool Component(double a,double b)=>Math.Abs(a-b)<=epsilon*Math.Max(1,Math.Max(Math.Abs(a),Math.Abs(b)));
        static bool Vector(Double3 a,Double3 b)=>Component(a.X,b.X)&&Component(a.Y,b.Y)&&Component(a.Z,b.Z);
        var x=a.BodyToWorld;var y=b.BodyToWorld;
        var same=Math.Max(Math.Max(Math.Abs(x.X-y.X),Math.Abs(x.Y-y.Y)),Math.Max(Math.Abs(x.Z-y.Z),Math.Abs(x.W-y.W)));
        var opposite=Math.Max(Math.Max(Math.Abs(x.X+y.X),Math.Abs(x.Y+y.Y)),Math.Max(Math.Abs(x.Z+y.Z),Math.Abs(x.W+y.W)));
        return Vector(a.PositionO,b.PositionO)&&Vector(a.VelocityO,b.VelocityO)&&Vector(a.AngularVelocityBody,b.AngularVelocityBody)&&Math.Min(same,opposite)<=epsilon;
    }
    internal static AssemblyMotion CraftDerivative(AssemblyMotion s,AssemblyMass props,AssemblyWrench wrench,Double3 gravityRoot)
    {
        var omega=s.AngularVelocityBody;var tau=wrench.MomentAtOrigin-Double3.Cross(props.Com,wrench.Force);
        var alpha=props.Inertia.Inverse().Apply(tau-Double3.Cross(omega,props.Inertia.Apply(omega)));
        var a=wrench.Force/props.Mass-Double3.Cross(alpha,props.Com)-Double3.Cross(omega,Double3.Cross(omega,props.Com));
        var q=s.BodyToWorld*new DoubleQuaternion(omega.X,omega.Y,omega.Z,0);
        return new(s.VelocityO,s.BodyToWorld.Rotate(a)+gravityRoot,new(q.X*.5,q.Y*.5,q.Z*.5,q.W*.5),alpha);
    }
    internal static AssemblyMotion CraftSiteDerivative(AssemblyMotion s,AssemblyMass props,AssemblyWrench wrench,AssemblyFloridaSite site,AssemblySiteFrame frame)
    {
        var omega=s.AngularVelocityBody;var inverse=s.BodyToWorld.Conjugate();var frameBody=inverse.Rotate(frame.Omega);var absolute=omega+frameBody;
        var torque=wrench.MomentAtOrigin-Double3.Cross(props.Com,wrench.Force);
        // Euler's equation acts on absolute angular momentum. The derivative
        // of the rotating frame's body components is q^-1*alpha_f - w_rel x o.
        var alpha=props.Inertia.Inverse().Apply(torque-Double3.Cross(absolute,props.Inertia.Apply(absolute)))-inverse.Rotate(frame.Alpha)+Double3.Cross(omega,frameBody);
        var com=AssemblyContactProfile.ToCom(s,props.Com);
        var gravity=site.LinearAcceleration(frame,com.Position,com.Velocity);
        var a=wrench.Force/props.Mass-Double3.Cross(alpha,props.Com)-Double3.Cross(omega,Double3.Cross(omega,props.Com));
        var q=s.BodyToWorld*new DoubleQuaternion(omega.X,omega.Y,omega.Z,0);
        return new(s.VelocityO,s.BodyToWorld.Rotate(a)+gravity,new(q.X*.5,q.Y*.5,q.Z*.5,q.W*.5),alpha);
    }
    private static AssemblyMotion CraftAdd(AssemblyMotion s,AssemblyMotion k,double h)=>new(s.PositionO+k.PositionO*h,s.VelocityO+k.VelocityO*h,
        new(s.BodyToWorld.X+k.BodyToWorld.X*h,s.BodyToWorld.Y+k.BodyToWorld.Y*h,s.BodyToWorld.Z+k.BodyToWorld.Z*h,s.BodyToWorld.W+k.BodyToWorld.W*h),s.AngularVelocityBody+k.AngularVelocityBody*h);
}
