using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Spacecraft.Assemblies;

/// <summary>Conservative finite-interval occupied-hull bound. This observes
/// immutable craft/site facts; it neither changes contact nor owns motion.</summary>
internal sealed class CraftClearance
{
    private readonly CompiledCraft craft;
    private readonly CompiledCraftControl control;
    private readonly CompiledCraftContact contact;
    private readonly AssemblyFloridaSite site;
    private readonly double radius,force,torque,comRate;
    internal CraftSweptTerrainClearance Swept {get;}
    internal CraftClearance(CompiledCraft craft,CompiledCraftControl control,CompiledCraftContact contact,AssemblyFloridaSite site)
    {
        this.craft=craft;this.control=control;this.contact=contact;this.site=site;
        Swept=new(site);
        var arms=0d;var jetTorque=0d;
        foreach(var x in new[]{craft.Mass.ComMinimum.X,craft.Mass.ComMaximum.X})
        foreach(var y in new[]{craft.Mass.ComMinimum.Y,craft.Mass.ComMaximum.Y})
        foreach(var z in new[]{craft.Mass.ComMinimum.Z,craft.Mass.ComMaximum.Z})
        {
            var c=new Double3(x,y,z);
            foreach(var hull in craft.Collision)foreach(var v in hull.Vertices)radius=Math.Max(radius,Norm(v-c));
            foreach(var store in craft.Mass.Stores)arms=Math.Max(arms,Norm(store.Datum-c));
            jetTorque=Math.Max(jetTorque,craft.Actuators.Where(a=>a!=control.Main).Sum(a=>Norm(Double3.Cross(a.Point-c,a.Axis*a.Thrust))));
        }
        force=craft.Actuators.Sum(a=>a.Thrust);
        var mainOnly=new bool[craft.Fuel.Consumers.Length];mainOnly[control.Main.Consumer]=true;
        torque=Outward(AssemblyDynamics.CraftTorqueBound(control,mainOnly)+jetTorque);
        // All consumers may spend simultaneously; routing can only reduce this
        // sum. q' <= total flow, |c'| <= sum |q'_i| |datum_i-c| / dry mass.
        var flow=craft.Fuel.Consumers.Sum(c=>ConstructionNumerics.Observe(c.Rate,System.Numerics.BigInteger.One<<1074));
        comRate=Outward(flow*arms/craft.Mass.Dry.Mass);radius=Outward(radius);force=Outward(force);
    }
    private static double Norm(Double3 p)=>Math.Sqrt(p.LengthSquared);
    private static double Outward(double x)=>Math.BitIncrement(x*(1+128*Math.ScaleB(1d,-52)));
    internal bool Clears(AssemblyMotion motion,AssemblyMass mass,SimulationInstant epoch,long ticks,out double lower)
    {
        using var timing=ConstructionWorkProbe.Time(ConstructionWorkProbe.Stage.Clearance);
        lower=double.NegativeInfinity;
        if(!motion.Finite||ticks<=0||ticks>15625||!site.Applicable)return false;
        var h=ticks/1e6;var frame=site.At(epoch);var com=AssemblyContactProfile.ToCom(motion,mass.Com);
        var absolute=motion.AngularVelocityBody+motion.BodyToWorld.Conjugate().Rotate(frame.Omega);
        // Spatially proportional depletion has dI/dq = I_store/kg +
        // Parallel(datum-c), positive semidefinite. Thus E'<=|tau| |w_abs|;
        // sqrt(2E/Imin) grows by at most |tau|/Imin per second.
        var spin=Outward(Math.Sqrt(Double3.Dot(absolute,mass.Inertia.Apply(absolute))/control.MinimumInertia)+torque*h/control.MinimumInertia+site.AngularSpeedBound);
        var earth=site.OriginBodyFixed+site.LocalToBodyFixed.Rotate(com.Position);var r=Norm(earth);
        var omega=site.AngularSpeedBound;var alpha=site.AngularAccelerationBound;
        var constant=force/craft.Mass.Dry.Mass+4*site.Mu/(r*r)+1.5*r*(omega*omega+alpha)+spin*comRate;
        var speed=Outward((Norm(com.Velocity)+constant*h)/(1-2*omega*h));
        var acceleration=Outward(constant+2*omega*speed);var travel=Outward(h*(speed+comRate));
        if(!double.IsFinite(travel+spin+acceleration)||1-2*omega*h<=0||travel>=r*.5)return false;
        var minimum=double.PositiveInfinity;
        foreach(var hull in craft.Collision)foreach(var v in hull.Vertices)
            minimum=Math.Min(minimum,(motion.PositionO+motion.BodyToWorld.Rotate(v)).Y-site.SupportPlane);
        var tolerance=2*contact.ContactTolerance;
        lower=minimum+Math.Min(0,com.Velocity.Y*h)-.5*acceleration*h*h-2*comRate*h-radius*spin*h-tolerance;
        // This branch covers every radial direction of the swept body inside
        // the authenticated full-weight plane, with a conservative 3D ball.
        var region=FloridaFacilitySupport.Region;var reach=radius+travel+tolerance;
        var center=site.OriginBodyFixed+site.LocalToBodyFixed.Rotate(com.Position);
        var up=Double3.Dot(center,region.Up)-reach;
        var e=Math.Abs(Double3.Dot(center,region.East))+reach;
        var n=Math.Abs(Double3.Dot(center,region.North))+reach;
        if(lower>0&&up>=region.RadiusMetres&&
            region.RadiusMetres*e/up<region.InnerEastMetres-tolerance&&
            region.RadiusMetres*n/up<region.InnerNorthMetres-tolerance)return true;
        // Enclose complete vertex trajectories, not just endpoint poses. The
        // tolerance exceeds admitted terrain radial error + native conversion
        // and numerical contact margin. Uncertainty still imports BEPU before
        // consuming this exact interval; no altitude/speed threshold is used.
        var expansion=Outward(.5*acceleration*h*h+2*comRate*h+radius*spin*h+tolerance);
        var swept=CraftSweptTerrainClearance.SweptBox(craft,motion,com.Velocity,h,expansion);
        if(Swept.Clears(swept,out lower))return true;
        return site.RadialBallClear(center,reach,out lower);
    }
}
