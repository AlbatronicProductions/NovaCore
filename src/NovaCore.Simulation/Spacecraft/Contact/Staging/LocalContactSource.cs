using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;

namespace NovaCore.Simulation.Spacecraft.Contact.Staging;

internal enum LocalContactStatus : byte
{
    Success, InvalidConfiguration, InvalidSource, UnsupportedForceTorqueState,
    ForeignEngine, ChangedAuthority, TimelineConflict, PendingEvent, InvalidInterval,
    ConfigurationMismatch, GenerationMismatch, FrontierMismatch, WrongThread,
    Disposed, Invalidated, PrecisionEnvelopeExceeded, SolverFailure, PublicationPending, SupportRefused,
}

/// <summary>Immutable qualification geometry and fixed inertial transport; not gameplay collision authority.</summary>
internal sealed class LocalContactConfiguration
{
    internal long Revision { get; }
    internal ReferenceFrameId RootFrame { get; }
    internal Double3 OriginRoot { get; }
    internal Double3 OriginVelocityRoot { get; }
    internal DoubleQuaternion LocalToRoot { get; }
    internal Double3 BoxDimensions { get; }
    internal EngineeringContactArticle? Article { get; }
    internal AssemblyContactProfile? AssemblyProfile { get; private init; }
    internal CompiledCraftContact? CraftProfile {get;private init;}
    internal bool ResourceAwareNumericalFixture { get; private init; }
    internal AssemblyFloridaSite? Site {get; private init;}
    // The new article uses the original 2 m thick slab. Legacy box fixtures retain their exact geometry.
    internal double SlabHalfThickness => Site?.Slab is {} slab ? slab.Dimensions.Y*.5 : Article is null && AssemblyProfile is null && !ResourceAwareNumericalFixture ? BoxDimensions.Y : 1;
    internal Double3 SupportDimensions => Site?.Slab?.Dimensions ?? new(2*PlaneHalfExtent,2*SlabHalfThickness,2*PlaneHalfExtent);
    internal double PlaneHalfExtent { get; }
    internal double ContactTolerance { get; }
    internal double MaximumCoordinate { get; }
    internal double MaximumSpeed { get; private init; }
    internal double MaximumAngularSpeed { get; private init; }
    internal double Margin => 20 * ContactTolerance;
    // Speculative reach must cover the admitted surface speed over a whole exact step.
    // Margin alone is a geometric/depth cushion, not the motion horizon.
    private double MaximumNativeSlice {get;init;}=16667d/1_000_000;
    internal int CraftRefinement {get;private init;}=-1;
    internal double CraftStepSeconds=>CraftRefinement<0?CraftSurfaceImpact.MaximumNativeSliceSeconds:MaximumNativeSlice;
    internal double MaximumSpeculativeMargin => ResourceAwareNumericalFixture ? .01 : MaximumSpeed * MaximumNativeSlice + Margin;
    private double? assemblyRadius;
    internal double BoundingRadius => assemblyRadius ?? AssemblyProfile?.BoundingRadius ?? Article?.BoundingRadius ?? Math.Sqrt(BoxDimensions.LengthSquared) * .5;

    private LocalContactConfiguration(long revision, ReferenceFrameId root, Double3 origin, Double3 velocity,
        DoubleQuaternion rotation, Double3 dimensions, double planeHalfExtent, double tolerance, double coordinate,
        EngineeringContactArticle? article)
    {
        Revision = revision; RootFrame = root; OriginRoot = origin; OriginVelocityRoot = velocity;
        LocalToRoot = rotation; BoxDimensions = dimensions; PlaneHalfExtent = planeHalfExtent;
        Article = article;
        ContactTolerance = tolerance; MaximumCoordinate = coordinate;
        // At most half the smallest extent per largest exact step, including rotation at the box radius.
        MaximumSpeed = (article?.SmallestFeature ?? Math.Min(dimensions.X, Math.Min(dimensions.Y, dimensions.Z))) * .5 / (16667d / 1_000_000);
        MaximumAngularSpeed = MaximumSpeed / BoundingRadius;
    }

    internal static LocalContactStatus TryCreate(long revision, ReferenceFrameId root, Double3 origin,
        Double3 velocity, DoubleQuaternion rotation, Double3 dimensions, double planeHalfExtent,
        out LocalContactConfiguration? configuration)
        => TryCreateCore(revision, root, origin, velocity, rotation, dimensions, planeHalfExtent, null, out configuration);

    internal static LocalContactStatus TryCreateAssembly(AssemblyLaunch launch, out LocalContactConfiguration? configuration)
    {
        configuration=null;
        if(launch.Consumer!=AssemblyPhysicalConsumer.SupportedContact||launch.ContactProfile is not {} profile)
            return LocalContactStatus.InvalidConfiguration;
        var tolerance=profile.SmallestFeature/1000;
        var bound=Math.Pow(2,Math.Floor(Math.Log2(tolerance/8))+23);
        var speed=profile.SmallestFeature*.5/(16667d/1_000_000);
        var origin=new Double3(0,AssemblyContactProfile.SupportPlaneAtOrigin,0);
        var radius=launch.PoweredSupport?Math.Max(profile.BoundingRadius,profile.RadiusAbout(launch.Design.ObserveMass(launch.Design.DryMass).Com)):profile.BoundingRadius;
        if(!RootSpacingFits(origin,tolerance)||radius*2>=8)return LocalContactStatus.InvalidConfiguration;
        foreach(var child in profile.Children)
            if(!FloatGeometryFits(child.Dimensions,8,tolerance))return LocalContactStatus.InvalidConfiguration;
        configuration=new(1,launch.Spacecraft.CarrierFrame,origin,launch.Departure?.FrameVelocity??launch.Initial.Motion.VelocityO,DoubleQuaternion.Identity,
            new(1,1,1),8,tolerance,bound,null){AssemblyProfile=profile,assemblyRadius=radius,MaximumSpeed=speed,MaximumAngularSpeed=speed/radius,Site=launch.Site};
        return LocalContactStatus.Success;
    }

    internal static LocalContactConfiguration CreateCraft(ConstructionRuntimeBinding binding,bool postFlight=false,AssemblyMotion? motion=null,int refinement=0,AssemblyMass? mass=null)
    {
        var physical=binding.Physical??throw new InvalidDataException("Missing physical craft.");var profile=physical.Contact;
        var tolerance=profile.ContactTolerance;var bound=Math.Pow(2,Math.Floor(Math.Log2(tolerance/8))+23);
        var origin=new Double3(0,profile.SupportPlane,0);
        // Conservatively cover every admitted COM, not just the cold fill.
        var radius=physical.Craft.Collision.SelectMany(c=>c.Vertices).Max(v=>{
            var low=physical.Craft.Mass.ComMinimum;var high=physical.Craft.Mass.ComMaximum;
            var p=new Double3(Math.Max(Math.Abs(v.X-low.X),Math.Abs(v.X-high.X)),Math.Max(Math.Abs(v.Y-low.Y),Math.Abs(v.Y-high.Y)),Math.Max(Math.Abs(v.Z-low.Z),Math.Abs(v.Z-high.Z)));
            return Math.Sqrt(p.LengthSquared);
        });
        var slab=physical.Site.Slab!;var extent=Math.Max(slab.Dimensions.X,slab.Dimensions.Z)*.5;
        if(!RootSpacingFits(origin,tolerance)||!FloatGeometryFits(slab.Dimensions,extent,tolerance)||radius*2>=extent||extent>=bound)
            throw new InvalidDataException("Craft contact precision envelope exceeded.");
        // Use the authored pad width as the smallest contact feature.
        var feature=physical.Craft.Support.Min(s=>2*Math.Min(s.Foot.HalfWidthY,s.Foot.HalfWidthZ));
        if(refinement is <0 or >CraftSurfaceImpact.MaximumRefinement)throw new InvalidDataException("Finite contact subdivision capacity exceeded.");
        var nativeSlice=postFlight?Math.ScaleB(CraftSurfaceImpact.MaximumNativeSliceSeconds,-refinement):16667d/1_000_000;
        var speed=postFlight?profile.MinimumNativeHalfWidth/nativeSlice:feature*.5/nativeSlice;
        if(postFlight)
        {
            var endpoint=motion??binding.Initial.Physical!.Motion;
            var com=AssemblyContactProfile.ToCom(endpoint,(mass??binding.Initial.ReferenceMass!.Value).Com);
            var surfaceSpeed=Math.Sqrt(com.Velocity.LengthSquared)+radius*Math.Sqrt(endpoint.AngularVelocityBody.LengthSquared);
            while(surfaceSpeed>speed&&refinement<CraftSurfaceImpact.MaximumRefinement){refinement++;nativeSlice*=.5;speed*=2;}
        }
        return new(1,binding.Spacecraft.CarrierFrame,origin,default,DoubleQuaternion.Identity,new(1,1,1),extent,tolerance,bound,null)
            {CraftProfile=profile,Site=physical.Site,assemblyRadius=radius,MaximumSpeed=speed,MaximumAngularSpeed=speed/radius,MaximumNativeSlice=nativeSlice,CraftRefinement=postFlight?refinement:-1};
    }

    internal static LocalContactStatus TryCreateArticle(long revision, ReferenceFrameId root, Double3 origin,
        Double3 velocity, DoubleQuaternion rotation, EngineeringContactArticle? article, double planeHalfExtent,
        out LocalContactConfiguration? configuration)
    {
        configuration = null;
        return article is null ? LocalContactStatus.InvalidConfiguration :
            TryCreateCore(revision, root, origin, velocity, rotation, article.Dimensions, planeHalfExtent, article, out configuration);
    }

    // Separate named numerical fixture, not a new interpretation of homogeneous-box/article inertia.
    internal static LocalContactStatus TryCreatePoweredFixture(ReferenceFrameId root, Double3 origin,
        Double3 velocity, out LocalContactConfiguration? configuration)
    {
        var status = TryCreateCore(1, root, origin, velocity, DoubleQuaternion.Identity,
            new(2, 1, 1), 8, null, out configuration);
        if (status == LocalContactStatus.Success)
            configuration = new(1, root, origin, velocity, DoubleQuaternion.Identity, new(2, 1, 1), 8,
                configuration!.ContactTolerance, configuration.MaximumCoordinate, null) { ResourceAwareNumericalFixture = true };
        return status;
    }

    private static LocalContactStatus TryCreateCore(long revision, ReferenceFrameId root, Double3 origin,
        Double3 velocity, DoubleQuaternion rotation, Double3 dimensions, double planeHalfExtent,
        EngineeringContactArticle? article, out LocalContactConfiguration? configuration)
    {
        configuration = null;
        if (revision <= 0 || root.Value == 0 || !origin.IsFinite || !velocity.IsFinite || !rotation.IsFinite ||
            Math.Abs(rotation.LengthSquared - 1) > 1e-12 || !dimensions.IsFinite ||
            dimensions.X <= 0 || dimensions.Y <= 0 || dimensions.Z <= 0 || !double.IsFinite(planeHalfExtent))
            return LocalContactStatus.InvalidConfiguration;
        var minimum = article?.SmallestFeature ?? Math.Min(dimensions.X, Math.Min(dimensions.Y, dimensions.Z));
        var tolerance = minimum / 1000; // Qualification requires one-thousandth of the smallest body dimension.
        // Eight float spacings fit inside the contact tolerance. Power-of-two bound is exclusive.
        var bound = Math.Pow(2, Math.Floor(Math.Log2(tolerance / 8)) + 23);
        if (!double.IsFinite(bound) || bound <= 0 || planeHalfExtent <= (article is null ? Math.Sqrt(dimensions.LengthSquared) : 2 * article.BoundingRadius) ||
            planeHalfExtent >= bound || (float)minimum <= 0 || !float.IsFinite((float)bound) ||
            !double.IsFinite(dimensions.LengthSquared) || tolerance <= 0 ||
            !double.IsFinite(minimum / (16667d / 1_000_000)) ||
            !FloatGeometryFits(dimensions, planeHalfExtent, tolerance) ||
            !RootSpacingFits(origin, tolerance))
            return LocalContactStatus.InvalidConfiguration;
        if (article is not null)
            for (var i = 0; i < EngineeringContactArticle.ChildCount; i++)
                if (!FloatGeometryFits(article.Child(i).Dimensions, planeHalfExtent, tolerance))
                    return LocalContactStatus.InvalidConfiguration;
        configuration = new(revision, root, origin, velocity, rotation, dimensions, planeHalfExtent, tolerance, bound, article);
        return LocalContactStatus.Success;
    }

    // Canonical FP64 addition itself must resolve the qualified contact tolerance.
    internal static bool RootSpacingFits(Double3 p, double tolerance) => p.IsFinite &&
        Math.Abs(Math.BitIncrement(p.X) - p.X) <= tolerance / 8 &&
        Math.Abs(Math.BitIncrement(p.Y) - p.Y) <= tolerance / 8 &&
        Math.Abs(Math.BitIncrement(p.Z) - p.Z) <= tolerance / 8;

    private static bool FloatGeometryFits(Double3 dimensions, double halfExtent, double tolerance)
    {
        var d = new System.Numerics.Vector3((float)dimensions.X, (float)dimensions.Y, (float)dimensions.Z);
        var edge = (float)(2 * halfExtent);
        var area = edge * edge;
        // BEPU broadphase/radius and triangle normal arithmetic use squared float lengths.
        // Refuse finite inputs whose derived geometry overflows or underflows before allocating a world.
        return float.IsNormal(d.LengthSquared()) && float.IsNormal(area * area) &&
            float.IsNormal((float)tolerance) && float.IsNormal((float)(20 * tolerance));
    }

    internal PrincipalMomentsOfInertia BoxInertia(double mass) => new(
        mass * (BoxDimensions.Y * BoxDimensions.Y + BoxDimensions.Z * BoxDimensions.Z) / 12,
        mass * (BoxDimensions.X * BoxDimensions.X + BoxDimensions.Z * BoxDimensions.Z) / 12,
        mass * (BoxDimensions.X * BoxDimensions.X + BoxDimensions.Y * BoxDimensions.Y) / 12);

    internal bool AdmitsMassInertia(double mass, PrincipalMomentsOfInertia inertia) => Article is { } article
        ? mass == article.MassKilograms && inertia == article.PrincipalInertia
        : !ResourceAwareNumericalFixture && inertia == BoxInertia(mass);
}

/// <summary>Copied immutable source. Never retains a borrowed state view or claims solver cache serialization.</summary>
internal sealed class LocalContactSource
{
    private readonly SimulationTransactionEngine engine;
    private readonly SpacecraftTranslationState linear;
    private readonly SpacecraftRigidBodyRotationState angular;
    internal AssemblyFlightAuthority? AssemblyAuthority { get; private init; }
    internal ConstructionServiceAuthority? ConstructionAuthority {get;private init;}
    internal ConstructionRuntimeState? ConstructionState {get;private init;}
    internal ContinuationClockState AssemblyClock {get;private init;}
    private Double3 assemblyForce;
    internal SpacecraftMotion Motion { get; }
    internal LocalContactConfiguration Configuration { get; }
    internal SimulationInstant End { get; }
    internal TimelineRevision TimelineRevision { get; }
    internal Double3 ForceRoot => AssemblyAuthority is null ? linear.ConstantForceRoot : assemblyForce;

    private LocalContactSource(SimulationTransactionEngine engine, LocalContactConfiguration configuration,
        SpacecraftTranslationState linear, SpacecraftRigidBodyRotationState angular, SpacecraftMotion motion,
        TimelineRevision timelineRevision, SimulationInstant end)
    {
        this.engine = engine; Configuration = configuration; this.linear = linear; this.angular = angular;
        Motion = motion; TimelineRevision = timelineRevision; End = end;
    }

    internal static LocalContactStatus Capture(SimulationTransactionEngine engine, SpacecraftId subject,
        LocalContactConfiguration? configuration, SimulationInstant end, out LocalContactSource? source)
    {
        source = null;
        if (!engine.IsContactProofOwnerThread) return LocalContactStatus.WrongThread;
        if (configuration is null) return LocalContactStatus.InvalidConfiguration;
        var start = engine.ContactProofCurrentTime;
        if (end <= start || (Int128)end.Ticks - start.Ticks > long.MaxValue) return LocalContactStatus.InvalidInterval;
        if (engine.HasContactProofBoundaryThrough(end)) return LocalContactStatus.PendingEvent;
        var view = engine.State;
        if (!view.Spacecraft.TryGetTranslation(subject, out var linear, out var mass) ||
            !view.Spacecraft.TryGetRigidBody(subject, out var angular) || !mass.IsValid ||
            linear.RootFrame != configuration.RootFrame || !configuration.AdmitsMassInertia(mass.MassKilograms, angular.PrincipalInertia))
            return LocalContactStatus.InvalidSource;
        if (angular.ConstantBodyTorque != Double3.Zero || angular.Model != RigidBodyRotationModel.ConstantBodyTorqueV1)
            return LocalContactStatus.UnsupportedForceTorqueState;
        if (SpacecraftMotionEvaluator.TryEvaluate(view, subject, start, out var motion) != SpacecraftTranslationStatus.Success)
            return LocalContactStatus.InvalidSource;
        source = new(engine, configuration, linear, angular, motion, engine.ContactProofTimelineRevision, end);
        return LocalContactStatus.Success;
    }

    internal LocalContactStatus Validate(SimulationTransactionEngine current, LocalContactConfiguration configuration,
        SimulationInstant target)
    {
        if (!ReferenceEquals(current, engine)) return LocalContactStatus.ForeignEngine;
        if (!current.IsContactProofOwnerThread) return LocalContactStatus.WrongThread;
        if (!ReferenceEquals(configuration, Configuration)) return LocalContactStatus.ConfigurationMismatch;
        if(ConstructionAuthority is {} construction)
        {
            if(target<Motion.Time||target>End)return LocalContactStatus.InvalidInterval;
            if(current.CheckConstructionContactSource(construction)!=ConstructionServiceStatus.Ready)return LocalContactStatus.ChangedAuthority;
            return current.HasContactProofBoundaryThrough(target)?LocalContactStatus.PendingEvent:LocalContactStatus.Success;
        }
        if(AssemblyAuthority is {} assembly)
        {
            if(target<Motion.Time||target>End)return LocalContactStatus.InvalidInterval;
            if(current.CheckAssemblyContactSource(assembly)!=AssemblyFlightStatus.Ready)return LocalContactStatus.ChangedAuthority;
            return current.HasContactProofBoundaryThrough(target)?LocalContactStatus.PendingEvent:LocalContactStatus.Success;
        }
        if (current.ContactProofTimelineRevision != TimelineRevision) return LocalContactStatus.TimelineConflict;
        var view = current.State;
        if (view.Revision != Motion.Revision || current.ContactProofCurrentTime != Motion.Time ||
            !view.Spacecraft.TryGetTranslation(Motion.Spacecraft, out var nowLinear, out var mass) ||
            !view.Spacecraft.TryGetRigidBody(Motion.Spacecraft, out var nowAngular) ||
            nowLinear != linear || nowAngular != angular || mass != Motion.Properties)
            return LocalContactStatus.ChangedAuthority;
        if (target < Motion.Time || target > End) return LocalContactStatus.InvalidInterval;
        return current.HasContactProofBoundaryThrough(target) ? LocalContactStatus.PendingEvent : LocalContactStatus.Success;
    }

    internal static LocalContactStatus CapturePoweredPreparation(SimulationTransactionEngine engine, SpacecraftId subject,
        PoweredContactPreparation prepared, SimulationInstant end, out LocalContactSource? source)
    {
        source = null;
        if (!engine.OwnsPersistentPublicationPhase) return LocalContactStatus.WrongThread;
        if (!prepared.Available || !prepared.Configuration.ResourceAwareNumericalFixture) return LocalContactStatus.InvalidSource;
        var view = engine.State;
        var start = engine.ContactProofCurrentTime;
        if (end <= start || (Int128)end.Ticks - start.Ticks > long.MaxValue) return LocalContactStatus.InvalidInterval;
        if (!view.Spacecraft.TryGetTranslation(subject, out var linear, out var mass) ||
            !view.Spacecraft.TryGetRigidBody(subject, out var angular) || linear.RootFrame != prepared.Configuration.RootFrame ||
            linear.Epoch != start || angular.Epoch != start || linear.ConstantForceRoot != Double3.Zero ||
            angular.ConstantBodyTorque != Double3.Zero || angular.Model != RigidBodyRotationModel.ConstantBodyTorqueV1 ||
            mass.MassKilograms != prepared.ResourceDefinition.InitialTotalMassKilograms || angular.PrincipalInertia != new PrincipalMomentsOfInertia(2, 2, 2))
            return LocalContactStatus.InvalidSource;
        var actual = new SpacecraftAppliedEndpoint(subject, linear.RootFrame, start, linear.PositionRoot, linear.VelocityRoot,
            angular.OrientationLocalToParent, angular.AngularVelocityBody, mass, angular.PrincipalInertia, AppliedEndpointValidity.EndpointOnly);
        var wanted = actual with { PositionRoot = prepared.Initial.Position, VelocityRoot = prepared.Initial.Velocity,
            BodyToRoot = prepared.Initial.Orientation, AngularVelocityBody = prepared.Initial.AngularVelocity };
        if (!actual.SameBits(wanted)) return LocalContactStatus.ChangedAuthority;
        var motion = new SpacecraftMotion(subject, start, view.Revision, linear.RootFrame, actual.PositionRoot,
            actual.VelocityRoot, actual.BodyToRoot, actual.AngularVelocityBody, mass, angular.PrincipalInertia);
        source = new(engine, prepared.Configuration, linear, angular, motion, engine.ContactProofTimelineRevision, end);
        return LocalContactStatus.Success;
    }

    internal bool TryEndpoint(long step, out SimulationInstant endpoint)
    {
        endpoint = default;
        if (step < 0) return false;
        var ticks = (Int128)Motion.Time.Ticks + (Int128)step * SimulationInstant.TicksPerSecond / 60;
        if (ticks < long.MinValue || ticks > long.MaxValue || ticks > End.Ticks) return false;
        endpoint = new((long)ticks); return true;
    }

    internal static LocalContactSource CaptureAssembly(SimulationTransactionEngine engine,AssemblyFlightAuthority authority,LocalContactConfiguration configuration)
    {
        if(!engine.OwnsPersistentPublicationPhase||engine.CheckAssemblyContactSource(authority)!=AssemblyFlightStatus.Ready)
            throw new InvalidOperationException("Assembly source requires prepared owner authority.");
        var launch=authority.Launch;var state=launch.Initial;var mass=state.Mass;
        var com=AssemblyContactProfile.ToCom(state.Motion,mass.Com);
        var motion=new SpacecraftMotion(launch.Spacecraft.Id,state.Epoch,engine.State.Revision,launch.Spacecraft.CarrierFrame,
            com.Position,com.Velocity,state.Motion.BodyToWorld,state.Motion.AngularVelocityBody,new(mass.Mass),new(mass.Inertia.A,mass.Inertia.E,mass.Inertia.I));
        return new(engine,configuration,default,default,motion,engine.ContactProofTimelineRevision,launch.End)
        {AssemblyAuthority=authority,AssemblyClock=engine.CaptureContinuationClock(),assemblyForce=new(0,-9.81*mass.Mass,0)};
    }
    internal static LocalContactSource CaptureCraft(SimulationTransactionEngine engine,ConstructionServiceAuthority authority,LocalContactConfiguration configuration)
    {
        if(!engine.OwnsPersistentPublicationPhase||engine.CheckConstructionContactSource(authority)!=ConstructionServiceStatus.Ready)
            throw new InvalidOperationException("Craft source requires prepared owner authority.");
        var binding=authority.Binding;var state=engine.CaptureConstructionContactState(authority);var mass=state.ReferenceMass!.Value;var physical=state.Physical!;
        var com=AssemblyContactProfile.ToCom(physical.Motion,mass.Com);
        var motion=new SpacecraftMotion(binding.Spacecraft.Id,state.Epoch,engine.State.Revision,binding.Spacecraft.CarrierFrame,
            com.Position,com.Velocity,physical.Motion.BodyToWorld,physical.Motion.AngularVelocityBody,new(mass.Mass),new(mass.Inertia.A,mass.Inertia.E,mass.Inertia.I));
        return new(engine,configuration,default,default,motion,engine.ContactProofTimelineRevision,binding.Physical!.Site.End)
            {ConstructionAuthority=authority,ConstructionState=state,AssemblyClock=engine.CaptureContinuationClock()};
    }
}
