using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Spacecraft.Rotation;

namespace NovaCore.Simulation.Spacecraft.Contact.Staging;

internal sealed partial class LocalContactWorld
{
    // Private continuation expectations, not another state store or authority.
    private sealed class CraftBinding(StateRevision revision,ContinuationClockState clock)
    {
        internal StateRevision Revision=revision;
        internal ContinuationClockState Clock=clock;
        internal long AcknowledgedFrontier;
        internal bool Pending;
    }
    private CraftBinding? construction;
    private bool craftHasIntegrated;
    internal int CraftContactCount=>export.ContactPoints;
    internal bool CraftInvalidated=>invalidated;
    private string? craftContinuationFailure;
    internal bool CraftMotionRefinementRequested {get;private set;}
    internal string? CraftSupportFailure=>metrics.Craft is {Failed:true} c?c.Failure:craftContinuationFailure;
    internal void AcknowledgeCraftCredit(ContinuationClockState clock)=>construction!.Clock=clock;
    internal void AcknowledgeCraft(StateRevision revision,ContinuationClockState clock)
    {
        var c=construction!;c.Revision=revision;c.Clock=clock;c.AcknowledgedFrontier=frontier;c.Pending=false;
    }
    internal void InvalidateCraft()=>invalidated=true;
    private readonly record struct CraftSlice(float Seconds,AssemblySiteFrame Frame,AssemblyMass Mid,AssemblyMass End,AssemblyWrench Wrench);
    internal LocalContactStatus StepCraft(SimulationTransactionEngine engine,Receipt previous,SimulationInstant target,
        ConstructionPhysicalEvolution services,AssemblyGimbal initialGimbal,CraftControlRow row,
        out Receipt next,out AssemblyMotion motion,out AssemblyGimbal gimbal)
    {
        next=default;motion=default;gimbal=initialGimbal;
        var status=Validate(engine,configuration,previous,target);if(status!=LocalContactStatus.Success)return status;
        if(!engine.OwnsPersistentPublicationPhase||construction is not {Pending:false}||source.ConstructionAuthority?.Binding.Physical is not {} physical||
            assemblyInput is null||target<=export.Motion.Time||target.Ticks-export.Motion.Time.Ticks>15625||frontier==long.MaxValue)
            return LocalContactStatus.InvalidSource;
        if(!ReferenceEquals(services.Fuel.Network,physical.Craft.Fuel)||!ReferenceEquals(services.Power.Network,physical.Craft.Power))return LocalContactStatus.InvalidSource;
        var slices=new List<CraftSlice>();var elapsed=0d;var preparedGimbal=initialGimbal;
        try
        {
            using var preparationTiming=ConstructionWorkProbe.Time(ConstructionWorkProbe.Stage.SlicePreparation);
            foreach(var phase in services.Phases)
            {
                var seconds=ConstructionNumerics.Seconds(phase.Ticks);
                if(!double.IsFinite(seconds)||seconds<0||seconds>1d/64||phase.Ticks.Numerator<=0)return LocalContactStatus.InvalidInterval;
                var pieces=Math.Max(1,(int)Math.Ceiling(seconds/configuration.CraftStepSeconds));var h=seconds/pieces;var dt=(float)h;
                var before=ConstructionNumerics.Quantities(phase.Before);var after=ConstructionNumerics.Quantities(phase.After);
                var delivered=physical.Control.Main.DataReachable;foreach(var load in physical.Control.Main.RequiredLoads)delivered&=phase.DeliveredLoads[load];
                AssemblyMass Mass(double fraction)=>physical.Craft.Mass.EvaluateDynamic(before.Select((v,i)=>fraction==0?v:fraction==1?after[i]:Math.Clamp(v+(after[i]-v)*fraction,after[i],v)).ToArray());
                if(!float.IsNormal(dt)||!float.IsFinite(1/dt))
                {
                    // Exact resource events may be far shorter than a normal
                    // FP32 time. Never round their inventory or feed them to
                    // BEPU's reciprocal-dt solver. Omit only a physically
                    // unobservable interval with identical FP64 mass properties.
                    var upper=Math.BitIncrement(h);var speed=configuration.MaximumSpeed+1;var omega=configuration.MaximumAngularSpeed+physical.Site.AngularSpeedBound;
                    var r0=Norm(physical.Site.OriginBodyFixed);var reach=Norm(configuration.OriginRoot)+Math.Sqrt(3)*configuration.MaximumCoordinate;
                    var rmin=r0-reach;var rmax=r0+reach;var frameOmega=physical.Site.AngularSpeedBound;
                    var force=physical.Craft.Actuators.Sum(a=>a.Thrust)/physical.Craft.Mass.Dry.Mass+physical.Site.Mu/(rmin*rmin)+
                        rmax*(frameOmega*frameOmega+physical.Site.AngularAccelerationBound)+2*frameOmega*speed;
                    var torque=AssemblyDynamics.CraftTorqueBound(physical.Control,phase.Active.AsSpan());
                    var alpha=torque/physical.Control.MinimumInertia+physical.Control.MaximumInertia/physical.Control.MinimumInertia*4*omega*omega+physical.Site.AngularAccelerationBound;
                    var allowance=Math.ScaleB(configuration.ContactTolerance,-24)/256;
                    if(rmin<=0||Mass(0)!=Mass(1)||!double.IsFinite(force+alpha)||upper*force>=1||upper*alpha>=omega||
                        upper*(speed+force)>=allowance||upper*(omega+alpha)*configuration.BoundingRadius>=allowance)
                        return LocalContactStatus.PrecisionEnvelopeExceeded;
                    dt=0;
                }
                for(var i=0;i<pieces;i++)
                {
                    var at=AssemblyActuation.Next(physical.Control.Main,preparedGimbal,row.TargetY,row.TargetZ,(i+.5)*h,delivered);
                    var midpoint=Mass((i+.5)/pieces);var endpoint=Mass((i+1d)/pieces);
                    var wrench=AssemblyActuation.Resolve(physical.Craft,phase.Active.AsSpan(),at);
                    var frame=physical.Site.At(export.Motion.Time,elapsed+(i+.5)*h);
                    _=CraftInertia(midpoint);_=CraftInertia(endpoint);
                    slices.Add(new(dt,frame,midpoint,endpoint,wrench));
                }
                preparedGimbal=AssemblyActuation.Next(physical.Control.Main,preparedGimbal,row.TargetY,row.TargetZ,seconds,delivered);elapsed+=seconds;
            }
            if(slices.Count is 0 or >256||Math.Abs(elapsed-(target.Ticks-export.Motion.Time.Ticks)/1e6)>1e-15)return LocalContactStatus.InvalidInterval;
        }
        catch(Exception e) when(e is InvalidDataException or OverflowException){return LocalContactStatus.PrecisionEnvelopeExceeded;}
        // Fallible immutable arithmetic completed. Native mutation is private;
        // any later refusal poisons this continuation before canonical writes.
        invalidated=true;
        craftDiagnosticCount=0;
        try
        {
            var native=simulation.Bodies[body];
            foreach(var slice in slices)
            {
                if(ConstructionWorkProbe.Current is {} sliceProbe)sliceProbe.Slices++;
                Recenter(slice.Mid);
                if(slice.Seconds==0){Recenter(slice.End);continue;}
                assemblyInput.SiteFrame=slice.Frame;assemblyInput.SiteInertia=slice.Mid.Inertia;
                assemblyInput.CraftMass=slice.Mid;assemblyInput.CraftWrench=slice.Wrench;
                var fq=native.Pose.Orientation;var q=(new DoubleQuaternion(fq.X,fq.Y,fq.Z,fq.W)*AssemblyToNativeRotation).Normalized();
                var acceleration=physical.Site.LinearAcceleration(slice.Frame,FromFloat(native.Pose.Position)+configuration.OriginRoot,FromFloat(native.Velocity.Linear))+q.Rotate(slice.Wrench.Force/slice.Mid.Mass);
                if(!acceleration.IsFinite||!Finite(ToFloat(acceleration)))return LocalContactStatus.PrecisionEnvelopeExceeded;
                // Prepare geometry for the complete swept authored hull. The
                // tighter hull footprint avoids scaling terrain work with the
                // height of an upright long craft. One mesh retains native
                // smoothing across all internal tile boundaries.
                var reach=(2*configuration.MaximumSpeed+2)*slice.Seconds+
                    .5*Norm(acceleration)*slice.Seconds*slice.Seconds+configuration.MaximumSpeculativeMargin+2*configuration.ContactTolerance;
                using(ConstructionWorkProbe.Time(ConstructionWorkProbe.Stage.Terrain))
                    craftTerrain!.Ensure(configuration.OriginRoot+FromFloat(native.Pose.Position)+assemblyPositionResidue,q,slice.Mid,reach);
                metrics.Terrain!.Begin();
                metrics.Contacts=0;metrics.MaximumDepth=0;metrics.ArticleChildMask=0;
                metrics.Coverage?.SetPreparedAcceleration(ToFloat(acceleration));metrics.Coverage?.Begin(slice.Seconds);
                if(!craftHasIntegrated){
                    // Immediate ignition can change the COM before the first
                    // solve. Re-encode that midpoint with its current child
                    // offsets; never snap or re-encode an integrated trajectory.
                    var exact=FromFloat(native.Pose.Position)+assemblyPositionResidue;
                    native.Pose.Position=CraftSupportImport.Encode(simulation.Shapes,bodyShape,physical.Contact,exact,(float)configuration.SlabHalfThickness);
                    assemblyPositionResidue=exact-FromFloat(native.Pose.Position);
                    metrics.Craft!.VerifyInitialCoverage(slice.Mid);
                }
                metrics.Craft!.Begin(slice.Mid);
                var beforeVelocity=craftDiagnosticSamples is null?default:FromFloat(native.Velocity.Linear);
                var beforePosition=craftDiagnosticSamples is null?default:FromFloat(native.Pose.Position)+configuration.OriginRoot;
                using(ConstructionWorkProbe.Time(ConstructionWorkProbe.Stage.Solver))simulation.Timestep(slice.Seconds);
                craftHasIntegrated=true;
                if(!metrics.Craft.Observe(slice.Seconds))return LocalContactStatus.SupportRefused;
                metrics.Terrain.Observe();
                metrics.Contacts=metrics.Craft.ContactCount+metrics.Terrain.ContactCount;
                metrics.MaximumDepth=(float)Math.Max(metrics.Craft.MaximumDepth,metrics.Terrain.MaximumDepth);
                if(craftDiagnosticSamples is {} capture)
                {
                    capture[craftDiagnosticCount++]=new(slice.Seconds,slice.Mid,beforePosition,beforeVelocity,FromFloat(native.Velocity.Linear),q,slice.Frame,metrics.Craft.LinearImpulse+metrics.Terrain.LinearImpulse);
                }
                if(metrics.Coverage?.Failed==true)return LocalContactStatus.SolverFailure;
                Recenter(slice.End);
                if(!Within(configuration,FromFloat(native.Pose.Position),FromFloat(native.Velocity.Linear),FromFloat(native.Velocity.Angular)))
                {
                    CraftMotionRefinementRequested=configuration.CraftRefinement>=0&&Finite(native.Velocity.Linear)&&Finite(native.Velocity.Angular)&&
                        Norm(FromFloat(native.Velocity.Linear))+configuration.BoundingRadius*Norm(FromFloat(native.Velocity.Angular))>configuration.MaximumSpeed;
                    craftContinuationFailure=$"Native contact numerical envelope: position={native.Pose.Position}, velocity={native.Velocity.Linear}, angular={native.Velocity.Angular}, radius={configuration.BoundingRadius:R}, speedLimit={configuration.MaximumSpeed:R}.";
                    return LocalContactStatus.PrecisionEnvelopeExceeded;
                }
            }
            status=ValidateAuthority(engine,configuration,target);if(status!=LocalContactStatus.Success)return status;
            var orientation=native.Pose.Orientation;var bodyQ=new DoubleQuaternion(orientation.X,orientation.Y,orientation.Z,orientation.W)*AssemblyToNativeRotation;
            if(SpacecraftRigidBodyRotationEvaluator.TryCanonicalize(bodyQ,out var canonicalQ)!=SpacecraftRigidBodyRotationEvaluationStatus.Success)return LocalContactStatus.PrecisionEnvelopeExceeded;
            var p=configuration.OriginRoot+FromFloat(native.Pose.Position)+assemblyPositionResidue;var v=FromFloat(native.Velocity.Linear)+assemblyVelocityResidue;
            var omega=canonicalQ.Conjugate().Rotate(FromFloat(native.Velocity.Angular));
            motion=AssemblyContactProfile.ToOrigin(p,v,canonicalQ,omega,assemblyMass.Com);
            if(!motion.Finite||!LocalContactConfiguration.RootSpacingFits(p,configuration.ContactTolerance))return LocalContactStatus.PrecisionEnvelopeExceeded;
            frontier++;construction.Pending=true;gimbal=preparedGimbal;
            export=new(source.Motion with {Time=target,Revision=construction.Revision,PositionRoot=p,VelocityRoot=v,BodyToRoot=canonicalQ,AngularVelocityBody=omega,
                Properties=new(assemblyMass.Mass),Inertia=new(assemblyMass.Inertia.A,assemblyMass.Inertia.E,assemblyMass.Inertia.I)},source.Motion.Time,source.TimelineRevision,Generation,frontier,
                metrics.Contacts,metrics.MaximumDepth,Norm(FromFloat(native.Pose.Position)),Norm(FromFloat(native.Velocity.Linear)),ImportPositionError,ImportVelocityError,native.Constraints.Count,0,assemblyMass);
            invalidated=false;next=new(this,frontier,receiptIdentity);return LocalContactStatus.Success;
            void Recenter(AssemblyMass successor)
            {
                if(successor==assemblyMass)return;
                (assemblyPositionResidue,assemblyVelocityResidue)=RecenterCraftBody(simulation,native,bodyShape,craftCenters,assemblyMass,successor,assemblyPositionResidue,assemblyVelocityResidue,configuration.ContactTolerance);
                assemblyMass=successor;
            }
        }
        catch(Exception ex){craftContinuationFailure=ex.Message;return LocalContactStatus.SolverFailure;}
    }
    private static double Norm(Double3 value)=>Math.Sqrt(value.LengthSquared);
    internal LocalContactStatus CheckCraftPublication(SimulationTransactionEngine engine,Receipt receipt,AssemblyMass mass)
    {
        var status=Validate(engine,configuration,receipt,export.Motion.Time);if(status!=LocalContactStatus.Success)return status;
        return engine.OwnsPersistentPublicationPhase&&construction is {Pending:true} c&&frontier==c.AcknowledgedFrontier+1&&mass==assemblyMass?
            LocalContactStatus.Success:LocalContactStatus.FrontierMismatch;
    }
}
