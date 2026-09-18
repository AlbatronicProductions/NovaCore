using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Core.Surface;
using NovaCore.Simulation.Celestial;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Spacecraft.Assemblies;

/// <summary>Cold, bounded rotating-site authority. No spacecraft values, solver handles or mutable stores.</summary>
internal sealed class AssemblyFloridaSite
{
    internal const string Identity = "srv01-florida-graded-ground/1";
    private readonly IPhysicalGradingProofSource terrain;
    private readonly EarthContactOrientation model;
    internal PhysicalSurfaceAuthorityIdentity Authority { get; }
    internal SimulationInstant Start { get; }
    internal SimulationInstant End { get; }
    internal ReferenceFrameId EarthFrame { get; }
    internal Double3 OriginBodyFixed { get; }
    internal DoubleQuaternion LocalToBodyFixed { get; }
    internal double Mu { get; }
    internal string Digest { get; }
    internal double EastMetres { get; }
    internal FloridaSlabSupport? Slab { get; }
    internal bool Applicable => terrain.Authority == Authority && terrain.GradingRegion == FloridaFacilitySupport.Region;

    private AssemblyFloridaSite(IPhysicalGradingProofSource terrain, SimulationInstant start,
        ReferenceFrameId earthFrame, double east, EarthContactOrientation model, double mu, FloridaSlabSupport? slab = null)
    {
        this.terrain=terrain;this.model=model;Authority=terrain.Authority;Start=start;
        End=new(checked(start.Ticks+20_000_000));EarthFrame=earthFrame;EastMetres=east;Mu=mu;
        var region=terrain.GradingRegion;
        OriginBodyFixed=region.Up*(region.RadiusMetres+region.PlaneAltitudeMetres-AssemblyContactProfile.SupportPlaneAtOrigin)+region.East*east;
        LocalToBodyFixed=Basis(region.East,region.Up,-region.North);
        Slab=slab;
        if(slab is not null)OriginBodyFixed=slab.TopBodyFixed-region.Up*AssemblyContactProfile.SupportPlaneAtOrigin;
        Digest=slab is null ? AssemblyJson.Digest(new {Identity,Authority,Start,End,EarthFrame,EastMetres,OriginBodyFixed,LocalToBodyFixed,model,Mu}) :
            AssemblyJson.Digest(new {Identity=FloridaSlabSupport.Identity,Authority,Start,End,EarthFrame,OriginBodyFixed,LocalToBodyFixed,model,Mu,slab.RootRadius,slab.FoundationDepth,slab.Dimensions});
    }

    internal static AssemblyFloridaSite CreateSlab(IPhysicalSurfacePointQuery query, SimulationInstant start,
        ReferenceFrameId earthFrame, FloridaSlabSupport slab)
    {
        ArgumentNullException.ThrowIfNull(slab);
        var ground=Create(query,start,earthFrame,0); // Authenticate Earth/site/epoch, never use terrain as the contact shape.
        if(slab.Authority!=ground.Authority)throw new InvalidDataException("Foreign slab authority.");
        return new(ground.terrain,start,earthFrame,0,ground.model,ground.Mu,slab);
    }

    internal static AssemblyFloridaSite Create(IPhysicalSurfacePointQuery query, SimulationInstant start,
        ReferenceFrameId earthFrame, double east)
    {
        if(query is not IPhysicalGradingProofSource t || t.GradingRegion!=FloridaFacilitySupport.Region ||
            query.Authority.FacilitySupportIdentity!=FloridaFacilitySupport.DefinitionIdentity ||
            query.Authority.ReferenceRadiusMetres!=t.GradingRegion.RadiusMetres || earthFrame.Value==0 ||
            !double.IsFinite(east) || Math.Abs(east)+8>=t.GradingRegion.InnerEastMetres ||
            !CelestialBodyOrientationEvaluator.TryGetContactProofModel(out var model) ||
            !SolAnalyticalDefinition.Instance.TryGetPhysicalProperties(SolarSystemBodyIds.Earth,out var earth))
            throw new InvalidDataException("A current, wholly graded physical Florida patch is required.");
        var coverage=SolAnalyticalDefinition.Instance.EphemerisMetadata;
        if(start.Ticks<coverage.SupportedStartDomainTicks || (Int128)start.Ticks+20_000_000>coverage.SupportedEndDomainTicks)
            throw new InvalidDataException("Site epoch outside current Earth coverage.");
        var region=t.GradingRegion;
        // The trusted capability proves the whole full-weight rectangular branch is the plane.
        // Corner samples check numerical domain and publication, not an invented interpolation proof.
        foreach(var x in new[]{east-8,east+8}) foreach(var z in new[]{-8d,8d})
        {
            var p=region.Up*(region.RadiusMetres+region.PlaneAltitudeMetres)+region.East*x+region.North*z;
            var direction=p.Normalized();var sample=query.Query(6,direction);
            if(!t.IsNumericalFullWeight(direction)||!sample.IsReady||sample.Authority!=query.Authority||
                Math.Abs(Double3.Dot(sample.BodyFixedPositionMetres,region.Up)-(region.RadiusMetres+region.PlaneAltitudeMetres))>1e-6)
                throw new InvalidDataException("Florida patch coverage or authority refused.");
        }
        return new(t,start,earthFrame,east,model,earth.GravitationalParameter);
    }

    internal bool AdmitsGraph(ReferenceFrameGraph graph, SpacecraftDefinition craft) =>
        graph.RootCount==1 && graph.TryGetNode(EarthFrame,out var root) && root.ParentId is null && root.Kind==ReferenceFrameKind.Ecl &&
        graph.TryGetNode(craft.CarrierFrame,out var site) && site.ParentId==EarthFrame && site.Kind==ReferenceFrameKind.Ccf &&
        graph.TryGetNode(craft.BodyFrame,out var body) && body.ParentId==site.Id && craft.BodyFrame!=EarthFrame;

    internal AssemblySiteFrame At(SimulationInstant time)
    {
        if(time<Start||time>End||!Applicable)throw new InvalidDataException("Stale site or epoch.");
        // Current linear IAU owner, analytically differentiated. The banked finite-stencil
        // angular-velocity observation is deliberately not relabelled an exact derivative.
        var seconds=time.SecondsSinceEpoch;var century=model.SecondsPerDay*model.DaysPerCentury;
        var rad=Math.PI/180;var ra=(model.Ra0+model.RaT*seconds/century+90)*rad;
        var tilt=(90-model.Dec0-model.DecT*seconds/century)*rad;
        var a=model.RaT*rad/century;var b=-model.DecT*rad/century;var c=model.Wd*rad/model.SecondsPerDay;
        var z=DoubleQuaternion.FromAxisAngle(Double3.UnitZ,ra);
        var x=DoubleQuaternion.FromAxisAngle(Double3.UnitX,tilt);
        var axis1=z.Rotate(Double3.UnitX);var axis2=(z*x).Rotate(Double3.UnitZ);
        var omega=Double3.UnitZ*a+axis1*b+axis2*c;
        var alpha=Double3.Cross(Double3.UnitZ*a,axis1)*b+Double3.Cross(Double3.UnitZ*a+axis1*b,axis2)*c;
        var whole=time.Ticks/1_000_000;if(time.Ticks%1_000_000<0)whole--;
        if(!CelestialBodyOrientationEvaluator.TryEvaluateEarthLocal(new(whole*1_000_000),(time.Ticks-whole*1_000_000)/1e6,out var earthQ,out _))
            throw new InvalidDataException("Earth orientation refused.");
        var q=(earthQ*LocalToBodyFixed).Normalized();var p=earthQ.Rotate(OriginBodyFixed);
        return new(time,p,Double3.Cross(omega,p),q,q.Conjugate().Rotate(omega),q.Conjugate().Rotate(alpha));
    }

    internal AssemblyMotion ToEarth(in AssemblyMotion local,SimulationInstant time)
    {
        var f=At(time);var p=f.Position+f.Orientation.Rotate(local.PositionO);
        var v=f.Velocity+f.Orientation.Rotate(local.VelocityO+Double3.Cross(f.Omega,local.PositionO));
        var q=f.Orientation*local.BodyToWorld;
        return new(p,v,q,local.AngularVelocityBody+local.BodyToWorld.Conjugate().Rotate(f.Omega));
    }

    internal AssemblyMotion FromEarth(in AssemblyMotion earth,SimulationInstant time)
    {
        var f=At(time);var inverse=f.Orientation.Conjugate();var p=inverse.Rotate(earth.PositionO-f.Position);
        var q=inverse*earth.BodyToWorld;
        return new(p,inverse.Rotate(earth.VelocityO-f.Velocity)-Double3.Cross(f.Omega,p),q,
            earth.AngularVelocityBody-q.Conjugate().Rotate(f.Omega));
    }

    internal Double3 LinearAcceleration(in AssemblySiteFrame frame,Double3 positionCom,Double3 velocity)
    {
        var r=LocalToBodyFixed.Rotate(positionCom)+OriginBodyFixed;
        var radius=Math.Sqrt(r.LengthSquared);
        var physical=LocalToBodyFixed.Conjugate().Rotate(r*(-Mu/(radius*radius*radius)));
        // Earth-centred two-body gravity in the rotating site frame. The origin is fixed
        // in CCF; full centrifugal/Coriolis/Euler terms include its offset from Earth.
        var absoluteLocal=LocalToBodyFixed.Conjugate().Rotate(OriginBodyFixed)+positionCom;
        return physical-Double3.Cross(frame.Omega,Double3.Cross(frame.Omega,absoluteLocal))-
            Double3.Cross(frame.Alpha,absoluteLocal)-Double3.Cross(frame.Omega,velocity)*2;
    }

    internal static Double3 AngularCorrection(in AssemblySiteFrame frame,DoubleQuaternion q,Double3 relative,Matrix3 inertia)
    {
        var inv=q.Conjugate();var w=inv.Rotate(relative);var o=inv.Rotate(frame.Omega);var absolute=w+o;
        return q.Rotate(-inertia.Inverse().Apply(Double3.Cross(absolute,inertia.Apply(absolute))-Double3.Cross(w,inertia.Apply(w))))-
            Double3.Cross(frame.Omega,relative)-frame.Alpha;
    }

    private static DoubleQuaternion Basis(Double3 x,Double3 y,Double3 z)
    {
        var trace=x.X+y.Y+z.Z;double a,b,c,d;
        if(trace>0){var s=Math.Sqrt(trace+1)*2;d=s/4;a=(y.Z-z.Y)/s;b=(z.X-x.Z)/s;c=(x.Y-y.X)/s;}
        else if(x.X>y.Y&&x.X>z.Z){var s=Math.Sqrt(1+x.X-y.Y-z.Z)*2;d=(y.Z-z.Y)/s;a=s/4;b=(y.X+x.Y)/s;c=(z.X+x.Z)/s;}
        else if(y.Y>z.Z){var s=Math.Sqrt(1+y.Y-x.X-z.Z)*2;d=(z.X-x.Z)/s;a=(y.X+x.Y)/s;b=s/4;c=(z.Y+y.Z)/s;}
        else{var s=Math.Sqrt(1+z.Z-x.X-y.Y)*2;d=(x.Y-y.X)/s;a=(z.X+x.Z)/s;b=(z.Y+y.Z)/s;c=s/4;}
        return new DoubleQuaternion(a,b,c,d).Normalized();
    }
}

internal readonly record struct AssemblySiteFrame(SimulationInstant Epoch,Double3 Position,Double3 Velocity,
    DoubleQuaternion Orientation,Double3 Omega,Double3 Alpha);
