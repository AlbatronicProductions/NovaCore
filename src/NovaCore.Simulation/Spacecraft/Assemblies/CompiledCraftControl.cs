using System.Collections.Immutable;
using NovaCore.Core;
using static NovaCore.Simulation.Spacecraft.Assemblies.AssemblyConstructionFacts;

namespace NovaCore.Simulation.Spacecraft.Assemblies;

internal sealed record CraftControlRow(CompiledCraft Craft,ImmutableArray<bool> Consumers,double TargetY,double TargetZ);
/// <summary>Prepared actuator rows consumed by the existing control owner. It
/// cannot admit player commands or mutate resource/actuator state.</summary>
internal sealed class CompiledCraftControl
{
    internal CompiledCraft Craft {get;}
    internal CraftActuator Main {get;}
    internal double MinimumInertia {get;}
    internal double MaximumInertia {get;}
    private readonly CraftControlRow[] rows=new CraftControlRow[54];
    private static int Index(bool main,AssemblyPilotDemand p)=>(main?27:0)+(p.Pitch+1)*9+(p.Yaw+1)*3+p.Roll+1;
    internal CraftControlRow Resolve(AssemblyControlRequest request){Require(request.Pilot.IsValid,"Invalid physical pilot demand.");return rows[Index(request.MainOn,request.Pilot)];}
    internal CompiledCraftControl(CompiledCraft craft)
    {
        Craft=craft;Require(craft.Function,"Physical control requires FUNCTION.");Main=craft.Actuators.Single(a=>a.Model=="nc.actuator.main/1");
        Require(craft.Actuators.All(a=>a==Main||a.Gimbal is null),"Physical attitude jets require fixed authored directions.");
        var g=Main.Gimbal;Require(g is not null,"Physical control requires the prepared main gimbal.");
        var dry=craft.Mass.Dry.Inertia;
        MinimumInertia=Math.BitDecrement(Math.Min(dry.A-Math.Abs(dry.B)-Math.Abs(dry.C),Math.Min(dry.E-Math.Abs(dry.D)-Math.Abs(dry.F),dry.I-Math.Abs(dry.G)-Math.Abs(dry.H)))-64*Math.ScaleB(1d,-52)*dry.Maximum);
        var origin=craft.Mass.OriginInertiaMaximum;MaximumInertia=Math.BitIncrement(Math.BitIncrement(origin.A+origin.E)+origin.I);
        Require(double.IsFinite(MinimumInertia)&&MinimumInertia>0&&double.IsFinite(MaximumInertia)&&MaximumInertia>=MinimumInertia,"Physical tensor conditioning bound is unqualified.");
        Require(double.IsFinite(g.LimitY)&&double.IsFinite(g.LimitZ)&&double.IsFinite(g.SlewRate)&&g.LimitY>0&&g.LimitY<Math.PI/2&&g.LimitZ>0&&g.LimitZ<Math.PI/2&&g.SlewRate>0,
            "Physical control requires positive finite slew and forward monotonic gimbal limits.");
        var localAxis=Main.PartRotation.Transpose().Apply(Main.Axis);var pivot=Main.PartOrigin+Main.PartRotation.Apply(g.Pivot);
        Require((localAxis-Double3.UnitX).LengthSquared<=1e-24&&(Main.Axis-Double3.UnitX).LengthSquared<=1e-24&&
            Math.Abs(pivot.Y)+Math.Abs(pivot.Z)<=1e-12&&pivot.X<craft.Mass.ComMinimum.X&&
            Math.Abs(craft.Mass.ComMinimum.Y)+Math.Abs(craft.Mass.ComMaximum.Y)+Math.Abs(craft.Mass.ComMinimum.Z)+Math.Abs(craft.Mass.ComMaximum.Z)<=1e-11,
            "Main gimbal is outside the qualified axial physical profile.");
        Require(Double3.Cross(g.NozzleOffset,localAxis).LengthSquared<=1e-24&&
            Double3.Cross(Main.Point-pivot,Main.Axis).LengthSquared<=1e-24,"Physical main nozzle is not coaxial with its authored pivot.");
        var limit=Math.Min(g.LimitY,g.LimitZ);
        for(var engine=0;engine<2;engine++)for(sbyte pitch=-1;pitch<=1;pitch++)for(sbyte yaw=-1;yaw<=1;yaw++)for(sbyte roll=-1;roll<=1;roll++){
            var main=engine!=0;var demand=new AssemblyPilotDemand(pitch,yaw,roll);
            var enabled=new bool[craft.Fuel.Consumers.Length];enabled[Main.Consumer]=main;
            void Add(int axis,int sign){if(sign!=0)foreach(var j in craft.Allocation.AxisJets[axis*2+(sign>0?1:0)])enabled[craft.Actuators[craft.Allocation.JetActuators[j]].Consumer]=true;}
            Add(0,roll);if(!main){Add(1,pitch);Add(2,yaw);}
            // Desired material-frame direction; inverse authored clocking maps
            // that direction to the actual part-local two-axis mechanism.
            var y=0d;var z=0d;
            if(main){var cy=Math.Cos(pitch*limit);var direction=new Double3(Math.Cos(yaw*limit)*cy,-Math.Sin(yaw*limit)*cy,Math.Sin(pitch*limit));
                var local=Main.PartRotation.Transpose().Apply(direction);y=Math.Atan2(-local.Z,Math.Sqrt(local.X*local.X+local.Y*local.Y));z=Math.Atan2(local.Y,local.X);
                // The finite Euler composition can require slightly more than
                // one local limit after a 90-degree clock. Scale both targets,
                // preserving their signs, to the actual actuator rectangle.
                var factor=Math.Max(1,Math.Max(Math.Abs(y)/g.LimitY,Math.Abs(z)/g.LimitZ));y=Math.Clamp(y/factor,-g.LimitY,g.LimitY);z=Math.Clamp(z/factor,-g.LimitZ,g.LimitZ);
            }
            Require(double.IsFinite(y)&&double.IsFinite(z)&&Math.Abs(y)<=g.LimitY&&Math.Abs(z)<=g.LimitZ,"Prepared physical gimbal target range.");
            var active=enabled.ToImmutableArray();var wrench=AssemblyActuation.Resolve(craft,active.AsSpan(),new(y,z,y,z));
            foreach(var cx in new[]{craft.Mass.ComMinimum.X,craft.Mass.ComMaximum.X})foreach(var cy in new[]{craft.Mass.ComMinimum.Y,craft.Mass.ComMaximum.Y})foreach(var cz in new[]{craft.Mass.ComMinimum.Z,craft.Mass.ComMaximum.Z}){
                var torque=wrench.MomentAtOrigin-Double3.Cross(new(cx,cy,cz),wrench.Force);
                bool Signed(double value,int sign)=>sign==0?Math.Abs(value)<=craft.Allocation.TorqueTolerance:value*sign>craft.Allocation.TorqueTolerance;
                Require(torque.IsFinite&&Signed(torque.X,roll)&&Signed(torque.Y,pitch)&&Signed(torque.Z,yaw),"Prepared physical control lacks signed axis authority.");
            }
            rows[Index(main,demand)]=new(craft,active,y,z);
        }
    }
}

internal static partial class AssemblyActuation
{
    internal static AssemblyWrench Main(CraftActuator main,double y,double z)
    {
        var g=main.Gimbal!;Require(double.IsFinite(y)&&double.IsFinite(z)&&Math.Abs(y)<=g.LimitY&&Math.Abs(z)<=g.LimitZ,"Physical gimbal angle range.");
        var sy=Math.Sin(y);var cy=Math.Cos(y);var sz=Math.Sin(z);var cz=Math.Cos(z);
        var rotation=new Matrix3(cz*cy,-sz,cz*sy,sz*cy,cz,sz*sy,-sy,0,cy);
        var axis=main.PartRotation.Transpose().Apply(main.Axis);var force=main.PartRotation.Apply(rotation.Apply(axis))*main.Thrust;
        var pivot=main.PartOrigin+main.PartRotation.Apply(g.Pivot);
        return new(force,Double3.Cross(pivot,force));
    }
    internal static AssemblyGimbal Next(CraftActuator main,AssemblyGimbal old,double y,double z,double seconds,bool delivered)
    {
        var g=main.Gimbal!;
        Require(double.IsFinite(seconds)&&seconds>=0&&double.IsFinite(y)&&double.IsFinite(z)&&double.IsFinite(old.ActualY)&&double.IsFinite(old.ActualZ)&&
            Math.Abs(old.ActualY)<=g.LimitY&&Math.Abs(old.ActualZ)<=g.LimitZ,"Physical gimbal source/range.");
        y=Math.Clamp(y,-g.LimitY,g.LimitY);z=Math.Clamp(z,-g.LimitZ,g.LimitZ);
        if(!delivered)return old;
        var step=g.SlewRate*seconds;Require(double.IsFinite(step),"Physical gimbal slew range.");
        return new(old.ActualY+Math.Clamp(y-old.ActualY,-step,step),old.ActualZ+Math.Clamp(z-old.ActualZ,-step,step),y,z);
    }
    internal static AssemblyWrench Resolve(CompiledCraft craft,ReadOnlySpan<bool> active,AssemblyGimbal gimbal)
    {
        Require(active.Length==craft.Fuel.Consumers.Length,"Physical actuator dimensions.");var force=Double3.Zero;var moment=Double3.Zero;
        foreach(var a in craft.Actuators)if(active[a.Consumer]){
            var w=a.Gimbal is not null?Main(a,gimbal.ActualY,gimbal.ActualZ):new AssemblyWrench(a.Axis*a.Thrust,Double3.Cross(a.Point,a.Axis*a.Thrust));
            force+=w.Force;moment+=w.MomentAtOrigin;
        }
        Require(force.IsFinite&&moment.IsFinite,"Physical wrench range.");return new(force,moment);
    }
}
