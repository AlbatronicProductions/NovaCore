using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Simulation.Celestial;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Spacecraft.Contact;

/// <summary>Source-seed Taylor enclosure of the current Earth ODE; no sampled anchor becomes authority.</summary>
internal readonly struct FloridaContactMotion
{
    internal const long CoverageSeconds=3600;
    private readonly CartesianState seed;
    private readonly EarthContactOrientation orientation;
    private readonly SpacecraftTranslationState linear;
    private readonly FloridaVector craftAcceleration,offset,earthAcceleration,earthJerk;
    private readonly FloridaBound earthSnap,earthAccelerationBound,omega;
    internal double PositionRemainderAtCoverage => (earthSnap*FloridaBound.Pow(CoverageSeconds,4)/24).Upper;

    private FloridaContactMotion(CartesianState seed,EarthContactOrientation orientation,SpacecraftTranslationState linear,
        FloridaVector craftAcceleration,FloridaVector offset,FloridaVector a,FloridaVector j,FloridaBound snap,FloridaBound accelerationBound,FloridaBound omega)
    {this.seed=seed;this.orientation=orientation;this.linear=linear;this.craftAcceleration=craftAcceleration;this.offset=offset;
        earthAcceleration=a;earthJerk=j;earthSnap=snap;earthAccelerationBound=accelerationBound;this.omega=omega;}

    internal static bool TryCreate(CelestialSystemDefinition system,in SpacecraftTranslationState linear,
        in SpacecraftRigidBodyRotationState angular,in SpacecraftPhysicalProperties properties,Double3 feature,out FloridaContactMotion result)
    {
        result=default;
        if(!ReferenceEquals(system,SolAnalyticalDefinition.Instance)||angular.AngularVelocityBody!=Double3.Zero||angular.ConstantBodyTorque!=Double3.Zero||
            !properties.IsValid||!system.TryGetNode(SolarSystemBodyIds.Earth,out var node)||node.TrajectoryModel!=CelestialTrajectoryModel.AnalyticalKepler||
            !system.TryGetAnalyticalKepler(node.Ephemeris.PayloadIndex,out var trajectory)||trajectory.Epoch!=SimulationInstant.Zero||
            !system.TryGetPhysicalProperties(SolarSystemBodyIds.Sun,out var sun)||!CelestialBodyOrientationEvaluator.TryGetContactProofModel(out var orientation))return false;
        var seed=trajectory.StateAtEpoch;var r=FloridaVector.From(seed.Position);var v=FloridaVector.From(seed.Velocity);
        FloridaBound mu=sun.GravitationalParameter;var radius=r.Norm;var half=radius/2;var aHalf=mu/half.Square();
        FloridaBound h=CoverageSeconds;var displacement=v.Norm*h+aHalf*h.Square()/2;
        if(displacement.Upper>=half.Lower)return false; // first-exit bootstrap, also for negative time
        var rMin=radius-displacement;var maxV=v.Norm+aHalf*h;var aBound=mu/rMin.Square();
        var snap=4*mu.Square()/FloridaBound.Pow(rMin,5)+24*mu*maxV.Square()/FloridaBound.Pow(rMin,4);
        var cube=radius*radius*radius;var a=r*(-mu/cube);
        var j=(v-r*(3*FloridaVector.Dot(r,v)/radius.Square()))*(-mu/cube);
        var acceleration=FloridaVector.From(linear.ConstantForceRoot)/(FloridaBound)properties.MassKilograms;
        var fixedOffset=FloridaVector.RotateUnitQuaternion(angular.OrientationLocalToParent,feature);
        var rates=((FloridaBound)Math.Abs(orientation.RaT)+Math.Abs(orientation.DecT))/(FloridaBound)orientation.DaysPerCentury/(FloridaBound)orientation.SecondsPerDay+
            Math.Abs(orientation.Wd)/(FloridaBound)orientation.SecondsPerDay;
        var omega=rates*FloridaBound.Pi/180;
        if(!snap.IsFinite||!a.IsFinite||!j.IsFinite||!acceleration.IsFinite||!fixedOffset.IsFinite)return false;
        result=new(seed,orientation,linear,acceleration,fixedOffset,a,j,snap,aBound,omega);return true;
    }

    internal bool Evaluate(FloridaBound time,out FloridaVector q,out FloridaVector derivative)
    {
        q=derivative=default;
        if(!time.IsFinite||time.Magnitude>CoverageSeconds)return false;
        var midpoint=time.Lower+(time.Upper-time.Lower)*.5;var h=(time-(FloridaBound)midpoint).Magnitude;
        FloridaBound t=midpoint;var dt=t-FloridaBound.Integer(linear.Epoch.Ticks)/SimulationInstant.TicksPerSecond;
        // Difference the fixed root positions before interval evaluation. No independent 30 km interval boxes.
        var relative=FloridaVector.From(linear.PositionRoot)-FloridaVector.From(seed.Position)+offset+
            FloridaVector.From(linear.VelocityRoot)*dt+craftAcceleration*(dt.Square()/2)-
            FloridaVector.From(seed.Velocity)*t-earthAcceleration*(t.Square()/2)-earthJerk*(t*t*t/6);
        var relativeVelocity=FloridaVector.From(linear.VelocityRoot)-FloridaVector.From(seed.Velocity)+craftAcceleration*dt-
            earthAcceleration*t-earthJerk*(t.Square()/2);
        relative=relative.Expand((earthSnap*FloridaBound.Pow(FloridaBound.Point(Math.Abs(midpoint)),4)/24).Upper);
        relativeVelocity=relativeVelocity.Expand((earthSnap*FloridaBound.Pow(FloridaBound.Point(Math.Abs(midpoint)),3)/6).Upper);
        var maxAcceleration=craftAcceleration.Norm+earthAccelerationBound;
        var maxVelocity=relativeVelocity.Norm+maxAcceleration*h;
        var maxPosition=relative.Norm+relativeVelocity.Norm*h+maxAcceleration*(FloridaBound.Point(h).Square()/2);
        // For a product of three constant-rate axial rotations, |Omega'| <= |rates|_1^2.
        var second=maxAcceleration+2*omega*maxVelocity+2*omega.Square()*maxPosition;
        BodyFixed(t,relative,relativeVelocity,out var center,out var first);
        var delta=time-t;
        q=(center+first*delta).Expand((second*FloridaBound.Point(h).Square()/2).Upper);
        derivative=first.Expand((second*h).Upper);
        return q.IsFinite&&derivative.IsFinite;
    }

    private void BodyFixed(FloridaBound t,FloridaVector p,FloridaVector v,out FloridaVector q,out FloridaVector dq)
    {
        var rad=FloridaBound.Pi/180;var day=(FloridaBound)orientation.SecondsPerDay;var century=day*orientation.DaysPerCentury;
        var ra=((FloridaBound)orientation.Ra0+orientation.RaT*t/century+90)*rad;
        var tilt=(90-(FloridaBound)orientation.Dec0-orientation.DecT*t/century)*rad;
        var w=((FloridaBound)orientation.W0+orientation.Wd*t/day)*rad;
        RotateZ(p,v,-ra,-orientation.RaT*rad/century,out q,out dq);
        RotateX(q,dq,-tilt,orientation.DecT*rad/century,out q,out dq);
        RotateZ(q,dq,-w,-orientation.Wd*rad/day,out q,out dq);
        // Inverse of banked fixed +90-degree X basis conversion, exactly represented.
        q=new(q.X,q.Z,-q.Y);dq=new(dq.X,dq.Z,-dq.Y);
    }
    private static void RotateZ(FloridaVector p,FloridaVector v,FloridaBound a,FloridaBound rate,out FloridaVector q,out FloridaVector dq)
    {var c=FloridaBound.Cos(a);var s=FloridaBound.Sin(a);q=new(c*p.X-s*p.Y,s*p.X+c*p.Y,p.Z);
        dq=new(c*v.X-s*v.Y,s*v.X+c*v.Y,v.Z);dq+=new FloridaVector(-q.Y,q.X,0)*rate;}
    private static void RotateX(FloridaVector p,FloridaVector v,FloridaBound a,FloridaBound rate,out FloridaVector q,out FloridaVector dq)
    {var c=FloridaBound.Cos(a);var s=FloridaBound.Sin(a);q=new(p.X,c*p.Y-s*p.Z,s*p.Y+c*p.Z);
        dq=new(v.X,c*v.Y-s*v.Z,s*v.Y+c*v.Z);dq+=new FloridaVector(0,-q.Z,q.Y)*rate;}

    internal bool ProveDomain(FloridaBound time,in FacilitySupportRegion region,out FloridaBound gap,out FloridaBound slope)
    {
        gap=slope=default;
        if(!Evaluate(time,out var q,out var dq))return false;
        var up=FloridaVector.From(region.Up);var east=FloridaVector.From(region.East);var north=FloridaVector.From(region.North);
        var projection=FloridaVector.Dot(up,q);var k=up.NormSquared;FloridaBound radius=region.RadiusMetres;
        gap=projection-(radius+region.PlaneAltitudeMetres);slope=FloridaVector.Dot(up,dq);
        if(projection.Lower<=0||!gap.IsFinite||!slope.IsFinite)return false;
        if((radius*FloridaVector.Dot(east,q).Magnitude).Upper> (region.InnerEastMetres*projection).Lower||
            (radius*FloridaVector.Dot(north,q).Magnitude).Upper>(region.InnerNorthMetres*projection).Lower)return false;
        var outerE=(FloridaBound)region.InnerEastMetres+region.BlendMetres;var outerN=(FloridaBound)region.InnerNorthMetres+region.BlendMetres;
        var guard=1-(outerE.Square()+outerN.Square())/radius.Square();
        var perpendicular=q-up*(projection/k);
        var left=projection.Square()*(k-guard.Square());var right=guard.Square()*k*perpendicular.NormSquared;
        if(guard.Lower<=0||left.Lower<right.Upper)return false;
        // c <= |Up|, so the FINAL Max clamp is inactive. Earlier finite base clamps cancel at full weight.
        return (radius+region.PlaneAltitudeMetres).Lower>(radius*k.Sqrt()).Upper;
    }
}
