using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Simulation.Celestial;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Spacecraft.Contact;

internal readonly record struct CoverageTrajectory(FloridaBound Time, FloridaBound Elapsed,
    FloridaVector RelativePosition, FloridaVector RelativeVelocity, FloridaVector RelativeAcceleration,
    FloridaVector BodyPosition, FloridaVector BodyVelocity, FloridaVector BodyAcceleration,
    PrivateRotationEnclosure Rotation);

internal readonly partial struct FloridaContactMotion
{
    internal CoverageFailure PostImpact(in PrivatePostImpactStateValues source, Double3 feature,
        FloridaBound sigma, out CoverageTrajectory trajectory)
    {
        trajectory = default;
        if (!sigma.IsFinite || sigma.Lower < 0 || sigma.Upper > 1) return CoverageFailure.InvalidRequest;
        var root = source.PoseRootEnclosure;
        var target = FloridaBound.Integer(source.SourceEnd.Ticks) / SimulationInstant.TicksPerSecond;
        var raw = target - root;
        if (!raw.IsFinite || raw.Upper <= 0) return CoverageFailure.InvalidSource;
        // The checked source theorem proves 0<D<=1; enclosure intersection is not time selection.
        var duration = new FloridaBound(Math.Max(0, raw.Lower), Math.Min(1, raw.Upper));
        var elapsed = sigma.Lower == 0 && sigma.Upper == 0 ? FloridaBound.Point(0) : sigma * duration;
        elapsed = new(Math.Max(0, elapsed.Lower), Math.Min(1, elapsed.Upper));
        var time = sigma.Lower == 0 && sigma.Upper == 0 ? root :
            sigma.Lower == 1 && sigma.Upper == 1 ? target : root + elapsed;
        // Convex linked time lies in the captured source cell, including its endpoints.
        var start=FloridaBound.Integer(source.SourceStart.Ticks)/SimulationInstant.TicksPerSecond;
        time=new(Math.Max(start.Lower,time.Lower),Math.Min(target.Upper,time.Upper));
        if (!time.IsFinite || time.Magnitude > CoverageSeconds) return CoverageFailure.InvalidSource;
        var f = PrivatePropagationMath.Rotation(source.FrozenSourceRotation.OrientationLocalToParent,
            source.InitialAngularVelocityBody, source.FrozenSourceRotation.PrincipalInertia, elapsed, out var rotation);
        if (f != PrivatePropagationFailure.None) return f == PrivatePropagationFailure.RotationWorkLimit ? CoverageFailure.WorkLimit : CoverageFailure.NonFinite;
        var t0 = FloridaBound.Integer(linear.Epoch.Ticks) / SimulationInstant.TicksPerSecond;
        var dt = time - t0;
        var v0 = FloridaVector.From(linear.VelocityRoot);
        var ve = FloridaVector.From(seed.Velocity);
        var plus = FloridaVector.From(source.InitialLinearVelocityRoot);
        // Algebraically M14.15: Xpre(t)+(Vplus-Vpre(t))*s+a*s², minus Earth(t).
        // Subtract common orbital positions/velocities BEFORE widening their time products.
        var relative = FloridaVector.From(linear.PositionRoot) - FloridaVector.From(seed.Position) - v0 * t0 +
            (v0 - ve) * time + craftAcceleration * (dt.Square() / 2) - earthAcceleration * (time.Square() / 2) -
            earthJerk * (time * time * time / 6) + (plus - (v0 + craftAcceleration * dt)) * elapsed + craftAcceleration * elapsed.Square();
        var velocity = plus - ve + craftAcceleration * elapsed - earthAcceleration * time - earthJerk * (time.Square() / 2);
        var acceleration = craftAcceleration - earthAcceleration - earthJerk * time;
        var magnitude = FloridaBound.Point(time.Magnitude);
        relative = relative.Expand((earthSnap * FloridaBound.Pow(magnitude, 4) / 24).Upper);
        velocity = velocity.Expand((earthSnap * FloridaBound.Pow(magnitude, 3) / 6).Upper);
        acceleration = acceleration.Expand((earthSnap * magnitude.Square() / 2).Upper);
        var w = rotation.Spin; var inertia = source.FrozenSourceRotation.PrincipalInertia;
        var dw = new FloridaVector(((FloridaBound)inertia.Y-inertia.Z)*w.Y*w.Z/inertia.X,
            ((FloridaBound)inertia.Z-inertia.X)*w.Z*w.X/inertia.Y,
            ((FloridaBound)inertia.X-inertia.Y)*w.X*w.Y/inertia.Z);
        var lever = FloridaVector.From(feature);
        var cross = FloridaVector.Cross(w, lever);
        relative += RotateFlow(rotation.Attitude, lever);
        velocity += RotateFlow(rotation.Attitude, cross);
        acceleration += RotateFlow(rotation.Attitude, FloridaVector.Cross(dw, lever) + FloridaVector.Cross(w, cross));
        BodyFixedJet(orientation, time, relative, velocity, acceleration, out var q, out var dq, out var ddq);
        if (!relative.IsFinite || !velocity.IsFinite || !acceleration.IsFinite || !q.IsFinite || !dq.IsFinite || !ddq.IsFinite)
            return CoverageFailure.NonFinite;
        trajectory = new(time, elapsed, relative, velocity, acceleration, q, dq, ddq, rotation);
        return CoverageFailure.None;
    }

    // Every exact ODE quaternion is unit. Use its inclusion box directly; do not select/normalize a sample.
    internal static FloridaVector RotateFlow(PropagationQuaternion q, FloridaVector v)
    {
        var vector = new FloridaVector(q.X, q.Y, q.Z);
        var twice = FloridaVector.Cross(vector, v) * 2;
        return v + twice * q.W + FloridaVector.Cross(vector, twice);
    }

    internal static void BodyFixedJet(in EarthContactOrientation model, FloridaBound t,
        FloridaVector p, FloridaVector v, FloridaVector a, out FloridaVector q, out FloridaVector dq, out FloridaVector ddq)
    {
        Angles(model, t, out var ra, out var tilt, out var w);
        var rad = FloridaBound.Pi / 180; var day = (FloridaBound)model.SecondsPerDay; var century = day * model.DaysPerCentury;
        AxisJet(p,v,a,-ra,-model.RaT*rad/century,false,out q,out dq,out ddq);
        AxisJet(q,dq,ddq,-tilt,model.DecT*rad/century,true,out q,out dq,out ddq);
        AxisJet(q,dq,ddq,-w,-model.Wd*rad/day,false,out q,out dq,out ddq);
        q = new(q.X,q.Z,-q.Y); dq = new(dq.X,dq.Z,-dq.Y); ddq = new(ddq.X,ddq.Z,-ddq.Y);
    }
    private static void AxisJet(FloridaVector p, FloridaVector v, FloridaVector a, FloridaBound angle,
        FloridaBound rate, bool x, out FloridaVector q, out FloridaVector dq, out FloridaVector ddq)
    {
        var c=FloridaBound.Cos(angle); var s=FloridaBound.Sin(angle);
        static FloridaVector Rotate(FloridaVector z,FloridaBound c,FloridaBound s,bool x)=>x ?
            new(z.X,c*z.Y-s*z.Z,s*z.Y+c*z.Z) : new(c*z.X-s*z.Y,s*z.X+c*z.Y,z.Z);
        static FloridaVector Cross(FloridaVector z,FloridaBound rate,bool x)=>x ?
            new(0,-rate*z.Z,rate*z.Y) : new(-rate*z.Y,rate*z.X,0);
        q=Rotate(p,c,s,x);var rv=Rotate(v,c,s,x);
        dq=rv+Cross(q,rate,x);
        ddq=Rotate(a,c,s,x)+Cross(rv,rate,x)*2+Cross(Cross(q,rate,x),rate,x);
    }

    internal static bool CoverageDomain(FloridaVector q,in FacilitySupportRegion region)
    {
        var up=FloridaVector.From(region.Up);var east=FloridaVector.From(region.East);var north=FloridaVector.From(region.North);
        var projection=FloridaVector.Dot(up,q);var k=up.NormSquared;FloridaBound radius=region.RadiusMetres;
        if(!q.IsFinite || projection.Lower<=0)return false;
        if((radius*FloridaVector.Dot(east,q).Magnitude).Upper>(region.InnerEastMetres*projection).Lower ||
            (radius*FloridaVector.Dot(north,q).Magnitude).Upper>(region.InnerNorthMetres*projection).Lower)return false;
        var outerE=(FloridaBound)region.InnerEastMetres+region.BlendMetres;var outerN=(FloridaBound)region.InnerNorthMetres+region.BlendMetres;
        var guard=1-(outerE.Square()+outerN.Square())/radius.Square();
        var perpendicular=q-up*(projection/k);
        var left=projection.Square()*(k-guard.Square());var right=guard.Square()*k*perpendicular.NormSquared;
        return guard.Lower>0 && left.Lower>=right.Upper &&
            (radius+region.PlaneAltitudeMetres).Lower>(radius*k.Sqrt()).Upper;
    }
}
