using System.Diagnostics;
using System.Text.Json;
using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Core.Surface;
using NovaCore.Simulation.Celestial;
using NovaCore.Simulation.Clock;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Contact;
using NovaCore.Simulation.Spacecraft.Rotation.Transactions;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;

internal static class EarthRelativeObservationTests
{
    private const double U = 1.1102230246251565e-16;
    private static void Check(bool condition, string contract)
    { if (!condition) throw new InvalidOperationException("Earth exact event: " + contract); }
    internal static PhysicalEventEpoch E(long ticks, ulong n = 0, ulong d = 1)
    { Check(PhysicalEventEpoch.TryCreate(ticks, n, d, out var value) == PhysicalEventEpochStatus.Success, "epoch admission"); return value; }
    private static double Norm(Double3 v) => Math.Sqrt(v.LengthSquared);
    private static double Ulp(double x) { x = Math.Abs(x); return Math.BitIncrement(x) - x; }
    private static double PositionBar(Double3 root) => 16 * Ulp(Math.Max(Math.Abs(root.X), Math.Max(Math.Abs(root.Y), Math.Abs(root.Z)))) + 128 * U * 6_400_000;
    private static void Near(Double3 a, Double3 b, double bar, string label) => Check(Norm(a - b) <= bar, $"{label}: error={Norm(a-b):R}, bar={bar:R}");
    private static bool Bits(double a, double b) => BitConverter.DoubleToInt64Bits(a) == BitConverter.DoubleToInt64Bits(b);
    private static bool Bits(Double3 a, Double3 b) => Bits(a.X,b.X) && Bits(a.Y,b.Y) && Bits(a.Z,b.Z);
    private static bool Same(SpacecraftPhysicalEventContactObservation a, SpacecraftPhysicalEventContactObservation b) =>
        a.IsReady == b.IsReady && a.Feature == b.Feature && a.Epoch == b.Epoch && a.SourceRevision == b.SourceRevision &&
        a.System == b.System && a.Body == b.Body && a.Root == b.Root && a.TerrainAuthority == b.TerrainAuthority &&
        Bits(a.FeaturePositionRoot,b.FeaturePositionRoot) && Bits(a.TerrainDirectionBodyFixed,b.TerrainDirectionBodyFixed) &&
        Bits(a.TerrainPositionRoot,b.TerrainPositionRoot) && Bits(a.PhysicalNormalRoot,b.PhysicalNormalRoot) &&
        Bits(a.RadialSignedGapMetres,b.RadialSignedGapMetres) && Bits(a.FeatureVelocityRoot,b.FeatureVelocityRoot) &&
        Bits(a.TerrainVelocityRoot,b.TerrainVelocityRoot) && Bits(a.RelativeVelocityRoot,b.RelativeVelocityRoot);

    // Independent radial height control. Production receives the acquired M14.1 provider, never this fixture.
    private sealed class Surface(double height = 125, double slope = 20,
        PhysicalSurfaceQueryStatus status = PhysicalSurfaceQueryStatus.Ready, bool wrongIdentity = false, int nonFiniteField = 0) : IPhysicalSurfacePointQuery
    {
        public PhysicalSurfaceAuthorityIdentity Authority { get; } = new(6, new(1,2), 3, 6_371_008.8, "oracle-global", "oracle-regional", 4, 5, 1);
        public PhysicalSurfacePointResult Query(ulong bodyId, in Double3 direction)
        {
            if (status != PhysicalSurfaceQueryStatus.Ready) return new(status, Authority, default, default, 0, default, 0);
            if(nonFiniteField!=0)return new(status,Authority,direction,
                nonFiniteField==1?new(double.NaN,0,0):direction*Authority.ReferenceRadiusMetres,
                nonFiniteField==2?double.NaN:0,nonFiniteField==3?new(double.NaN,0,0):direction,1);
            var h = height + slope * direction.Z;
            var r = Authority.ReferenceRadiusMetres + h;
            var gradient = (Double3.UnitZ - direction * direction.Z) * slope;
            var normal = (direction - gradient / r).Normalized();
            return new(status, wrongIdentity ? Authority with { CompositionIdentity = 99 } : Authority,
                direction, direction * r, h, normal, 1);
        }
    }
    private sealed class Fixture
    {
        internal readonly CelestialSystemDefinition System = SolAnalyticalDefinition.Instance;
        internal readonly ReferenceFrameGraph Graph = ContactGenerationFixture.Graph();
        internal readonly ReferenceFrameEvaluation[] Evaluations, Staging;
        internal readonly FrameTransform[] Roots, StagingRoots;
        internal readonly Surface Query;
        internal readonly SpacecraftContactGeometry Geometry;
        internal readonly SimulationState State;
        internal Fixture(long start = 0, Double3? velocity = null, Double3? omega = null, Double3? offset = null, Surface? surface = null)
        {
            Query = surface ?? new();
            Evaluations = new ReferenceFrameEvaluation[System.Count]; Staging = new ReferenceFrameEvaluation[System.Count];
            Roots = new FrameTransform[System.Count]; StagingRoots = new FrameTransform[System.Count];
            Check(EarthPhysicalEventEvaluator.TryEvaluate(System, Graph, E(start), Evaluations, Roots, Staging, StagingRoots, out var earth) == EarthPhysicalEventStatus.Ready, "canonical Earth fixture");
            var direction = new Double3(.3,.4,.5).Normalized();
            var q = DoubleQuaternion.FromAxisAngle(new Double3(1,2,3), .7);
            var p = earth.BodyFixedToRoot.LocalToParent(direction * (Query.Authority.ReferenceRadiusMetres + 1000));
            State = ContactGenerationFixture.State(p, velocity ?? earth.VelocityRoot + new Double3(10,-20,30), q, omega ?? new Double3(.125,-.0625,.25), new(start));
            Check(SpacecraftContactGeometry.TryCreate(ContactGenerationFixture.Craft, 900, 1,
                [new(1, offset ?? new Double3(3,-2,5), ContactFeatureRole.LandingTip)], out var geometry), "one complete physical feature");
            Geometry = geometry!;
        }
        internal EarthRelativeObservationResult Evaluate(PhysicalEventEpoch epoch, out SpacecraftPhysicalEventContactObservation observation) =>
            Evaluate(State.CreateView(), State.CreateView().Revision, epoch, Geometry, 1, System, Query, Query.Authority, out observation);
        internal EarthRelativeObservationResult Evaluate(SimulationStateView view, StateRevision revision, PhysicalEventEpoch epoch,
            SpacecraftContactGeometry? geometry, ulong feature, CelestialSystemDefinition system, IPhysicalSurfacePointQuery? query,
            PhysicalSurfaceAuthorityIdentity authority, out SpacecraftPhysicalEventContactObservation observation) =>
            SpacecraftPhysicalEventObservationEvaluator.Evaluate(view, revision, epoch, geometry, feature, system, Graph, query,
                authority, Evaluations, Roots, Staging, StagingRoots, out observation);
        internal EarthPhysicalEventMotion Earth(PhysicalEventEpoch e)
        {
            Check(EarthPhysicalEventEvaluator.TryEvaluate(System, Graph, e, Evaluations, Roots, Staging, StagingRoots, out var earth) == EarthPhysicalEventStatus.Ready, "same-E Earth");
            return earth;
        }
    }

    internal static void Run()
    {
        CanonicalParity(); OrbitOracles(); OrientationOracles(); SameEpochWiring(); FractionalFrames(); Refusals(); ReplayAndAuthority(); Allocation();
        Console.WriteLine("PASS Earth exact-event observation: canonical bits/status, orbit/Euler oracles, same-E frames, refusals, replay, authority");
    }

    private static void CanonicalParity()
    {
        var checkedCount = 0;
        foreach (var start in new[] { 0L, -1_234_567L, 1_234_567L, 15_000_000_000_000_003L, -15_000_000_000_000_003L })
        {
            var f = new Fixture(start); var destination = new SpacecraftContactObservation[1];
            foreach (var step in new[] { 0L, 1L, 999L, 9999L })
            {
                var e = E(start + step); var earth = f.Earth(e); var view = f.State.CreateView();
                var a = SpacecraftContactGenerator.Generate(view, view.Revision, new(e.FloorTicks), f.Geometry,
                    earth.Canonical, f.System, f.Query, f.Query.Authority, destination);
                var b = f.Evaluate(e, out var exact); var old = destination[0];
                var expected = new SpacecraftPhysicalEventContactObservation(old.IsReady, old.Feature, e, old.SpacecraftRevision,
                    f.System.Id, SolarSystemBodyIds.Earth, old.Root, old.TerrainAuthority, old.FeaturePositionRoot,
                    old.TerrainDirectionBodyFixed, old.TerrainPositionRoot, old.PhysicalNormalRoot, old.RadialSignedGapMetres,
                    old.FeatureVelocityRoot, old.TerrainVelocityRoot, old.RelativeVelocityRoot);
                Check(a.Status == b.Status && b.Succeeded && Same(exact, expected), "canonical status and every corresponding bit"); checkedCount++;
            }
            foreach (var query in new IPhysicalSurfacePointQuery?[] { null, new Surface(status: PhysicalSurfaceQueryStatus.NormalUnqualified), new Surface(wrongIdentity: true) })
            {
                var view = f.State.CreateView(); var earth = f.Earth(E(start));
                var old = SpacecraftContactGenerator.Generate(view, view.Revision, new(start), f.Geometry, earth.Canonical, f.System, query, f.Query.Authority, destination);
                var exact = f.Evaluate(view,view.Revision,E(start),f.Geometry,1,f.System,query,f.Query.Authority,out var value);
                Check(old.Status == exact.Status && old.SurfaceStatus == exact.SurfaceStatus && !exact.Succeeded && value == default, "canonical refusal parity/default");
            }
        }
        Console.WriteLine($"EARTH_CANONICAL bit_parity={checkedCount} refusal_parity=15");
    }

    // Independent eccentric-anomaly increments; no central-force Taylor in the oracle.
    private static void OrbitOracles()
    {
        double maxP=0,maxV=0;
        const double mu=1.327124400412794e20, a=149_597_806_502.43307;
        foreach (var e in new[] {0d,.016705450456221425,.2})
        foreach (var anomaly in new[] {0d,.73,2.1})
        foreach (var h in new[] {.0000005,.25,.999999999999})
        {
            var b=a*Math.Sqrt(1-e*e); var n=Math.Sqrt(mu/(a*a*a));
            var c=Math.Cos(anomaly);var s=Math.Sin(anomaly);var k=n/(1-e*c);
            var p=new Double3(a*(c-e),b*s,0);var v=new Double3(-a*s*k,b*c*k,0);
            var du=n*h/(1-e*c);
            for(var j=0;j<8;j++)du-=(du-2*e*Math.Cos(anomaly+du/2)*Math.Sin(du/2)-n*h)/(1-e*Math.Cos(anomaly+du));
            var dp=new Double3(-2*a*Math.Sin(anomaly+du/2)*Math.Sin(du/2),2*b*Math.Cos(anomaly+du/2)*Math.Sin(du/2),0);
            var ca=c*Math.Cos(du)-s*Math.Sin(du);var sa=s*Math.Cos(du)+c*Math.Sin(du);var ka=n/(1-e*ca);
            var expectedV=new Double3(-a*sa*ka,b*ca*ka,0);
            Check(CelestialSystemEvaluator.TryAdvanceEarthLocal(new(p,v),mu,h,out var actual),"same-model local continuation");
            var radius=Norm(p)/2;var A=mu/(radius*radius);var V=Norm(v)+A*h;
            var snap=4*mu*mu/Math.Pow(radius,5)+24*mu*V*V/Math.Pow(radius,4);
            var pBar=snap*Math.Pow(h,4)/24+64*U*(Norm(p)+Norm(v)*h);
            var vBar=snap*Math.Pow(h,3)/6+64*U*(Norm(v)+A*h);
            Near(actual.Position,p+dp,pBar,"ellipse position bound"); Near(actual.Velocity,expectedV,vBar,"ellipse velocity bound");
            if(h>.1)Check(actual.Velocity!=v && actual.Position!=p,"not frozen orbit anchor");
            maxP=Math.Max(maxP,Norm(actual.Position-(p+dp)));maxV=Math.Max(maxV,Norm(actual.Velocity-expectedV));
        }
        Console.WriteLine($"EARTH_ORBIT independent_ellipse_cases=27 max_position_m={maxP:R} max_velocity_mps={maxV:R}");
    }

    private static Double3 Rx(Double3 v,double a) => new(v.X,Math.Cos(a)*v.Y-Math.Sin(a)*v.Z,Math.Sin(a)*v.Y+Math.Cos(a)*v.Z);
    private static Double3 Rz(Double3 v,double a) => new(Math.Cos(a)*v.X-Math.Sin(a)*v.Y,Math.Sin(a)*v.X+Math.Cos(a)*v.Y,v.Z);
    // Decimal phase reduction is independent of the incremental production quaternion construction.
    private static Double3 Euler(decimal seconds, Double3 v)
    {
        var centuries=seconds/86400m/36525m;
        double Rad(decimal angle)=>(double)(angle%360m)*Math.PI/180;
        return Rz(Rx(Rz(Rx(v,Math.PI/2),Rad(190.147m+360.9856235m*seconds/86400m)),Rad(.557m*centuries)),Rad(90m-.641m*centuries));
    }
    private static void OrientationOracles()
    {
        double maxDirection=0,maxOmega=0;
        foreach(var seconds in new[]{0L,1000L,-1000L,15_000_000_000L,-15_000_000_000L})
        foreach(var h in new[]{.0000005,.25,.999999999999})
        {
            Check(CelestialBodyOrientationEvaluator.TryEvaluateEarthLocal(SimulationInstant.FromWholeSeconds(seconds),h,out var q,out var omega),"fractional Earth model");
            var phase=Math.Abs(190.147+360.9856235*((double)seconds/86400));
            var orientationBar=16*Ulp(phase)*Math.PI/180+256*U;
            var t=(decimal)seconds+(decimal)h;
            foreach(var axis in new[]{Double3.UnitX,Double3.UnitY,Double3.UnitZ})
            {
                var err=Norm(q.Rotate(axis)-Euler(t,axis));maxDirection=Math.Max(maxDirection,err);
                Check(err<=orientationBar,"independent Euler orientation within inherited phase/operation bar");
            }
            // Rotation-matrix derivative of the SAME symmetric one-second relative rotation.
            var before=new[]{Euler(t-.5m,Double3.UnitX),Euler(t-.5m,Double3.UnitY),Euler(t-.5m,Double3.UnitZ)};
            var after=new[]{Euler(t+.5m,Double3.UnitX),Euler(t+.5m,Double3.UnitY),Euler(t+.5m,Double3.UnitZ)};
            double D(int row,int col) { double At(Double3 v,int i)=>i==0?v.X:i==1?v.Y:v.Z; return Enumerable.Range(0,3).Sum(k=>At(after[k],row)*At(before[k],col)); }
            var skew=new Double3(D(2,1)-D(1,2),D(0,2)-D(2,0),D(1,0)-D(0,1))/2;
            var angle=Math.Atan2(Norm(skew),(D(0,0)+D(1,1)+D(2,2)-1)/2);
            var expected=skew*(angle/Norm(skew));
            maxOmega=Math.Max(maxOmega,Norm(expected-omega));Near(omega,expected,2*orientationBar+256*U,"independent finite-stencil omega");
            Check(Norm(omega)>0,"nonzero rotation");
        }
        Check(CelestialBodyOrientationEvaluator.TryEvaluateEarthLocal(default,.5,out var atZero,out var w0),"pole control");
        var spin=atZero.Rotate(Double3.UnitY)*(360.9856235*Math.PI/180/86400);
        Check(Norm(w0-spin)>1e-13,"pole contributions cannot be replaced by axial spin alone");
        Console.WriteLine($"EARTH_ORIENTATION matrix_cases=15 max_direction_error={maxDirection:R} max_stencil_radps={maxOmega:R}");
    }

    private static void FractionalFrames()
    {
        double maxFrame=0;
        foreach(var start in new[]{0L,-1_234_567L,1_234_567L,15_000_000_000_000_003L,-15_000_000_000_000_003L})
        foreach(var fraction in new[]{(1UL,2UL),(1UL,3UL),(15625UL,32768UL),(ulong.MaxValue-1,ulong.MaxValue),(1UL,ulong.MaxValue)})
        {
            var f=new Fixture(start);var epoch=E(start,fraction.Item1,fraction.Item2);
            Check(f.Evaluate(epoch,out var observation).Succeeded,"fractional observation");var earth=f.Earth(epoch);
            Check(SpacecraftPhysicalEventMotionEvaluator.TryEvaluate(f.State.CreateView(),f.Geometry.Spacecraft,epoch,out var craft)==SpacecraftTranslationStatus.Success,"independent craft owner");
            Check(observation.Epoch==epoch && earth.Epoch==epoch && craft.Epoch==epoch,"three exact identities");
            var decimalTime=((decimal)epoch.FloorTicks+(decimal)epoch.Numerator/epoch.Denominator)/SimulationInstant.TicksPerSecond;
            var phase=Math.Abs(190.147+360.9856235*((double)decimalTime/86400));
            Near(earth.BodyFixedToRoot.Rotation.Rotate(Double3.UnitX),Euler(decimalTime,Double3.UnitX),
                16*Ulp(phase)*Math.PI/180+256*U,"integrated exact epoch orientation oracle");
            var offset=craft.BodyToRoot.Rotate(f.Geometry.GetFeature(0).OffsetFromComMetres);
            Near(observation.FeaturePositionRoot,craft.PositionRoot+offset,PositionBar(craft.PositionRoot),"feature metres/root");
            var relativeBody=earth.BodyFixedToRoot.Rotation.Conjugate().Rotate(observation.FeaturePositionRoot-earth.BodyFixedToRoot.Translation);
            var direction=relativeBody/Norm(relativeBody);var surface=f.Query.Query(6,direction);
            var terrainLever=earth.BodyFixedToRoot.Rotation.Rotate(surface.BodyFixedPositionMetres);
            var normal=earth.BodyFixedToRoot.Rotation.Rotate(surface.PhysicalNormal);
            var vf=craft.VelocityRoot+Double3.Cross(craft.BodyToRoot.Rotate(craft.AngularVelocityBody),offset);
            var vt=earth.VelocityRoot+Double3.Cross(earth.AngularVelocityRoot,terrainLever);
            var velocityBar=128*U*(Norm(vf)+Norm(vt)+1);
            Near(observation.FeatureVelocityRoot,vf,velocityBar,"craft body omega transported to root before cross");
            Near(observation.TerrainVelocityRoot,vt,velocityBar,"Earth root omega cross root witness lever");
            Near(observation.RelativeVelocityRoot,vf-vt,velocityBar,"relative sign/metres per second");
            Near(observation.PhysicalNormalRoot,normal,128*U,"qualified normal rotation");
            Near(observation.TerrainDirectionBodyFixed,direction,128*U,"query body-fixed direction");
            var error=Math.Abs(observation.RadialSignedGapMetres-((Norm(relativeBody)-f.Query.Authority.ReferenceRadiusMetres)-surface.HeightMetres));
            maxFrame=Math.Max(maxFrame,error);Check(error<=PositionBar(craft.PositionRoot),"unchanged radial gap meaning");
            Check(observation.TerrainAuthority==f.Query.Authority && observation.Body==SolarSystemBodyIds.Earth && observation.Root==craft.RootFrame,"provenance and frame identities");
        }
        // Numerical relative-motion controls: authored zero offset and zero craft spin isolate translation.
        var control=new Fixture(offset:Double3.Zero,omega:Double3.Zero,surface:new Surface(0,0));var e=E(0,1,2);var b=control.Earth(e);
        var center=b.BodyFixedToRoot.LocalToParent(Double3.UnitX*control.Query.Authority.ReferenceRadiusMetres);
        var terrainVelocity=b.VelocityRoot+Double3.Cross(b.AngularVelocityRoot,b.BodyFixedToRoot.Rotation.Rotate(Double3.UnitX*control.Query.Authority.ReferenceRadiusMetres));
        foreach(var speed in new[]{-10d,0d,10d})
        {
            var normal=b.BodyFixedToRoot.Rotation.Rotate(Double3.UnitX);var velocity=terrainVelocity+normal*speed;
            var state=ContactGenerationFixture.State(center-velocity*.0000005,velocity,DoubleQuaternion.Identity,Double3.Zero);
            var view=state.CreateView();Check(control.Evaluate(view,view.Revision,e,control.Geometry,1,control.System,control.Query,control.Query.Authority,out var o).Succeeded,"radial motion control");
            var bar=Norm(b.AngularVelocityRoot)*PositionBar(center)+128*U*Norm(terrainVelocity);
            Check(Math.Abs(Double3.Dot(o.RelativeVelocityRoot,normal)-speed)<=bar,"approach/separate/numerically stationary sign");
        }
        var tangent=b.BodyFixedToRoot.Rotation.Rotate(Double3.UnitZ)*12;
        var tangentState=ContactGenerationFixture.State(center-(terrainVelocity+tangent)*.0000005,terrainVelocity+tangent,DoubleQuaternion.Identity,Double3.Zero);
        var tv=tangentState.CreateView();Check(control.Evaluate(tv,tv.Revision,e,control.Geometry,1,control.System,control.Query,control.Query.Authority,out var to).Succeeded,"tangent control");
        Near(to.RelativeVelocityRoot,tangent,Norm(b.AngularVelocityRoot)*PositionBar(center)+128*U*Norm(terrainVelocity),"tangential velocity");
        Console.WriteLine($"EARTH_FRACTION cases=25 max_radial_gap_composition_error_m={maxFrame:R}; relative_controls=4");
    }

    private static void SameEpochWiring()
    {
        var f=new Fixture();var floor=f.Earth(E(500_000));var exact=f.Earth(E(500_000,1,2));
        const double dt=.0000005;
        var r=floor.BodyFixedToRoot.Translation;var v=floor.VelocityRoot;
        var mu=SolAnalyticalDefinition.GetElement(2).CentralGravitationalParameter;
        var a=r*(-mu/Math.Pow(Norm(r),3));
        var positionSignal=v*dt;var velocitySignal=a*dt;
        var pBar=PositionBar(r);var vBar=128*U*Norm(v);
        Check(Norm(positionSignal)>pBar && Norm(velocitySignal)>vBar,"orbital anti-floor signals resolve beyond numerical bars");
        Near(exact.BodyFixedToRoot.Translation-r,positionSignal,pBar,"integrated same-E orbital position");
        Near(exact.VelocityRoot-v,velocitySignal,vBar,"integrated same-E orbital velocity");
        var x=floor.BodyFixedToRoot.Rotation.Rotate(Double3.UnitX);
        var spinSignal=Double3.Cross(floor.AngularVelocityRoot,x)*dt;
        Check(Norm(spinSignal)>256*U,"orientation anti-floor signal resolves");
        Near(exact.BodyFixedToRoot.Rotation.Rotate(Double3.UnitX)-x,spinSignal,256*U,"integrated same-E orientation");
        Console.WriteLine($"EARTH_SAME_E orbital_position_signal_m={Norm(positionSignal):R} orbital_velocity_signal_mps={Norm(velocitySignal):R} orientation_signal={Norm(spinSignal):R}");
    }

    private static void Refusals()
    {
        var f=new Fixture();var view=f.State.CreateView();var e=E(0,1,2);
        void Refuse(EarthRelativeObservationFailure expected, SimulationStateView state, StateRevision revision,
            PhysicalEventEpoch epoch, SpacecraftContactGeometry? geometry, ulong feature, CelestialSystemDefinition system,
            IPhysicalSurfacePointQuery? query, PhysicalSurfaceAuthorityIdentity authority)
        {
            var status=f.Evaluate(state,revision,epoch,geometry,feature,system,query,authority,out var result);
            Check(!status.Succeeded && status.Failure==expected && result==default,$"refusal/default {expected}, actual {status}");
        }
        Refuse(EarthRelativeObservationFailure.InvalidGeometry,view,view.Revision,e,null,1,f.System,f.Query,f.Query.Authority);
        Refuse(EarthRelativeObservationFailure.InvalidGeometry,view,view.Revision,e,ContactGenerationFixture.Geometry(4),1,f.System,f.Query,f.Query.Authority);
        Refuse(EarthRelativeObservationFailure.InvalidFeature,view,view.Revision,e,f.Geometry,99,f.System,f.Query,f.Query.Authority);
        Refuse(EarthRelativeObservationFailure.SpacecraftMissing,new SimulationState().CreateView(),default,e,f.Geometry,1,f.System,f.Query,f.Query.Authority);
        Refuse(EarthRelativeObservationFailure.UnsupportedBody,view,view.Revision,e,f.Geometry,1,f.System,f.Query,f.Query.Authority with {BodyId=7});
        Refuse(EarthRelativeObservationFailure.UnsupportedCelestialModel,view,view.Revision,e,f.Geometry,1,SolAnalyticalDefinition.CreateForTest(),f.Query,f.Query.Authority);
        Refuse(EarthRelativeObservationFailure.UnsupportedCelestialModel,view,view.Revision,e,f.Geometry,1,ContactGenerationFixture.StaticSystem(Double3.Zero,Double3.Zero),f.Query,f.Query.Authority);
        Refuse(EarthRelativeObservationFailure.EpochOutsideRange,view,view.Revision,E(f.System.EphemerisMetadata.SupportedEndDomainTicks,1,2),f.Geometry,1,f.System,f.Query,f.Query.Authority);
        Refuse(EarthRelativeObservationFailure.SpacecraftEvaluation,view,view.Revision,E(-1,1,2),f.Geometry,1,f.System,f.Query,f.Query.Authority);
        Refuse(EarthRelativeObservationFailure.TerrainUnavailable,view,view.Revision,e,f.Geometry,1,f.System,null,f.Query.Authority);
        Refuse(EarthRelativeObservationFailure.NormalUnqualified,view,view.Revision,e,f.Geometry,1,f.System,new Surface(status:PhysicalSurfaceQueryStatus.NormalUnqualified),f.Query.Authority);
        Refuse(EarthRelativeObservationFailure.AuthorityMismatch,view,view.Revision,e,f.Geometry,1,f.System,new Surface(wrongIdentity:true),f.Query.Authority);
        Refuse(EarthRelativeObservationFailure.AuthorityMismatch,view,view.Revision,e,f.Geometry,1,f.System,f.Query,f.Query.Authority with {PhysicalGeneration=999});
        foreach(var field in new[]{1,2,3})Refuse(EarthRelativeObservationFailure.NonFinite,view,view.Revision,e,f.Geometry,1,f.System,new Surface(nonFiniteField:field),f.Query.Authority);
        var wrongRoot=ContactGenerationFixture.State(new(1,2,3),Double3.Zero,DoubleQuaternion.Identity,Double3.Zero,root:99).CreateView();
        Refuse(EarthRelativeObservationFailure.RootMismatch,wrongRoot,wrongRoot.Revision,e,f.Geometry,1,f.System,f.Query,f.Query.Authority);
        Check(!SpacecraftContactGeometry.TryCreate(ContactGenerationFixture.Craft,1,1,[new(1,new(double.NaN,0,0),ContactFeatureRole.LandingTip)],out _),"invalid feature refused at admission");
        Check(EarthPhysicalEventEvaluator.TryEvaluate(f.System,f.Graph,e,[],f.Roots,f.Staging,f.StagingRoots,out var shortResult)==EarthPhysicalEventStatus.InvalidBuffers && shortResult==default,"short scratch refused/default");
        Check(EarthPhysicalEventEvaluator.TryEvaluate(f.System,f.Graph,e,f.Evaluations,f.Roots,f.Evaluations,f.StagingRoots,out var overlap)==EarthPhysicalEventStatus.InvalidBuffers && overlap==default,"overlapping scratch refused/default");
        Check(PhysicalEventDuration.TryDifference(E(long.MaxValue,1,2),new(long.MinValue),out _)==false,"duration capacity refuses overflow");
        Console.WriteLine("EARTH_REFUSAL cases=21 complete_default=PASS");
    }

    private static void ReplayAndAuthority()
    {
        var f=new Fixture();var retained=f.State.CreateView();var clock=new SimulationClock(default,new SimulationTimeline());var engine=new SimulationTransactionEngine(clock,f.State,16);
        var epochs=new[]{E(0),E(1,1,2),E(100,1,3),E(9999,15625,32768)};
        var results=new SpacecraftPhysicalEventContactObservation[epochs.Length];
        retained.Spacecraft.TryGetTranslation(f.Geometry.Spacecraft,out var beforeLinear,out _);retained.Spacecraft.TryGetRigidBody(f.Geometry.Spacecraft,out var beforeAngular);
        for(var i=0;i<epochs.Length;i++)Check(f.Evaluate(epochs[i],out results[i]).Succeeded,"replay setup");
        var random=new Random(14708);
        foreach(var fps in new[]{30,60,144,270})for(var i=0;i<32;i++)
        { var pick=random.Next(epochs.Length);Check(f.Evaluate(epochs[pick],out var value).Succeeded && Same(value,results[pick]),$"query order and presentation cadence independence {fps}"); }
        var after=f.State.CreateView();after.Spacecraft.TryGetTranslation(f.Geometry.Spacecraft,out var afterLinear,out _);after.Spacecraft.TryGetRigidBody(f.Geometry.Spacecraft,out var afterAngular);
        Check(after.Revision==retained.Revision && beforeLinear==afterLinear && beforeAngular==afterAngular && clock.CurrentTime==default && clock.PendingSimulationDebt==default && engine.ProcessedCount==0,"all authority unchanged");
        var torque=RigidBodyTorqueTransactionEvaluator.TryCreateControlReplacement(engine.State,new(f.Geometry.Spacecraft,new(0,0,.5),clock.CurrentTime));
        Check(torque.Succeeded && engine.ValidateAndCommit(torque.Transaction!.Value).Committed,"real later canonical transaction");
        var status=f.Evaluate(retained,f.State.CreateView().Revision,epochs[1],f.Geometry,1,f.System,f.Query,f.Query.Authority,out var stale);
        Check(status.Failure==EarthRelativeObservationFailure.StaleRevision && stale==default,"live-backed stale view rejected");
        Check(results[1].SourceRevision==retained.Revision && f.Evaluate(epochs[1],out var fresh).Succeeded && fresh.SourceRevision!=results[1].SourceRevision,"retained result independent of later authority");
        Console.WriteLine("EARTH_REPLAY seeded_queries=128 mutation=NONE stale_view=REFUSED");
    }

    internal static void Allocation()
    {
        var f=new Fixture();var epochs=new[]{E(0),E(0,1,2),E(0,1,3),E(0,15625,32768)};
        for(var i=0;i<64;i++)Check(f.Evaluate(epochs[i%4],out _).Succeeded,"allocation warmup");
        var complete=0;using var measurement=new OrdinaryAllocationMeasurement("exact-event-earth-observation");
        for(var i=0;i<256;i++)if(f.Evaluate(epochs[i%4],out _).Succeeded)complete++;
        var bytes=measurement.Complete();OrdinaryAllocationMeasurement.RequireZero(bytes,"exact-event-earth-observation");
        Check(complete==256,"all allocation workload queries complete");
        OrdinaryAllocationMeasurement.PositiveControl();
        Console.WriteLine($"EARTH_ALLOCATION calls={complete} bytes={bytes} entry=PASS exit=PASS");
    }

    internal static void Performance()
    {
        var f=new Fixture();var canonical=E(0);var fractional=E(0,1,2);var earth=f.Earth(fractional);
        Check(SpacecraftPhysicalEventMotionEvaluator.TryEvaluate(f.State.CreateView(),f.Geometry.Spacecraft,fractional,out var craft)==SpacecraftTranslationStatus.Success,"component fixture");
        var sample=f.Query.Query(6,new Double3(.3,.4,.5).Normalized());
        var paths=new (string Name,int Count,Func<bool> Work)[] {
            ("canonical",1,()=>f.Evaluate(canonical,out _).Succeeded),
            ("fractional",1,()=>f.Evaluate(fractional,out _).Succeeded),
            ("batch8",8,()=>{var ok=true;for(var i=0;i<8;i++)ok &= f.Evaluate(E(i,1,3),out _).Succeeded;return ok;}),
            ("craft",1,()=>SpacecraftPhysicalEventMotionEvaluator.TryEvaluate(f.State.CreateView(),f.Geometry.Spacecraft,fractional,out _)==SpacecraftTranslationStatus.Success),
            ("earth",1,()=>EarthPhysicalEventEvaluator.TryEvaluate(f.System,f.Graph,fractional,f.Evaluations,f.Roots,f.Staging,f.StagingRoots,out _)==EarthPhysicalEventStatus.Ready),
            ("terrain-control",1,()=>f.Query.Query(6,sample.BodyFixedDirection).IsReady),
        };
        Console.WriteLine("EARTH_TIMING ordinary runtime; 64 warmups; 101 samples x16 calls; batch8 reports whole batch; analytical terrain control; setup excluded");
        foreach(var path in paths)
        {
            for(var i=0;i<64;i++)Check(path.Work(),"timing warmup");var ticks=new long[101];var complete=0;
            for(var i=0;i<ticks.Length;i++){var start=Stopwatch.GetTimestamp();for(var j=0;j<16;j++)if(path.Work())complete++;ticks[i]=Stopwatch.GetTimestamp()-start;}
            Check(complete==1616,"bounded timing completion");Array.Sort(ticks);
            double Ns(int i)=>ticks[i]*1e9/Stopwatch.Frequency/16;
            Console.WriteLine(JsonSerializer.Serialize(new{kind="earth-timing",path=path.Name,batch=path.Count,medianNs=Ns(50),p95Ns=Ns(95),p99Ns=Ns(99)}));
        }
    }
}
