using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Actuation;
using NovaCore.Simulation.Spacecraft.Resources;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Transactions;

namespace NovaCore.Simulation.Spacecraft.Contact.Staging;

/// <summary>Finite qualification identities, not a continuous vehicle/thrust admission envelope.</summary>
internal enum PoweredContactFixture
{
    CenteredBaseline, LowThrust, SlidingBaseline, NearUnloading, YawBaseline,
    OffCom, NearEnd, MidEvent, TinyEvent, ZeroEvent, CenteredEndpoint
}

/// <summary>
/// Cold native resource owner. Its copied prestate constructs INITIAL canonical records. One successful
/// transfer starts the episode; no live canonical state, resource, clock or frontier is edited here.
/// </summary>
internal sealed class PoweredContactPreparation : IDisposable
{
    private readonly int ownerThread = Environment.CurrentManagedThreadId;
    private bool disposed, transferred;
    private BufferPool? Pool = new(16384);
    private LocalContactMetrics? Metrics = new();
    private LocalContactStepInput? Input = new() { Linear = new(0, -9.81f, 0) };
    private BepuPhysics.Simulation? Simulation;
    private readonly BodyHandle Body;
    private readonly StaticHandle Plane;
    private readonly TypedIndex BodyShape, SurfaceShape;
    internal readonly LocalContactConfiguration Configuration;
    internal readonly PropellantDefinition ResourceDefinition;
    internal readonly IdealEngineDefinition EngineDefinition;
    internal readonly PoweredKinematics Initial;
    internal readonly PoweredContactFixture Fixture;
    internal readonly double Throttle;
    internal readonly bool Ignite;
    internal readonly OrdinaryContactProjectionSchedule Projection;
    internal static Double3 GravityLocal => new(0, -9.81, 0);
    internal bool Available => !disposed && !transferred && ownerThread == Environment.CurrentManagedThreadId;
    internal readonly double PositionImportError, VelocityImportError;
    internal bool FrameImportFits => double.IsFinite(PositionImportError) && double.IsFinite(VelocityImportError) &&
        PositionImportError <= Configuration.ContactTolerance / 8 &&
        VelocityImportError * (16667d / 1_000_000) <= Configuration.ContactTolerance / 8;

    private PoweredContactPreparation(PoweredContactFixture fixture, LocalContactConfiguration config,
        PropellantDefinition resource, double thrust, double exhaust, Double3 axis, double mount,
        double vx, double wy, double throttle, bool ignite)
    {
        Fixture = fixture; Configuration = config; ResourceDefinition = resource; Throttle = throttle; Ignite = ignite;
        if (!OrdinaryContactProjectionSchedule.TryPrepare(out Projection))
            throw new InvalidOperationException("Invalid ordinary contact projection schedule.");
        Simulation = BepuPhysics.Simulation.Create(Pool, new LocalContactCallbacks(Metrics),
            new LocalContactIntegrator(default, Input), new SolveDescription(8, 1),
            initialAllocationSizes: new SimulationAllocationSizes(128, 32, 16, 128, 2048, 128, 8));
        try
        {
            BodyShape = Simulation.Shapes.Add(new Box(2, 1, 1));
            SurfaceShape = Simulation.Shapes.Add(new Box(16, 2, 16));
            var inertia = new BodyInertia { InverseMass = (float)(1 / resource.InitialTotalMassKilograms) };
            inertia.InverseInertiaTensor.XX = inertia.InverseInertiaTensor.YY = inertia.InverseInertiaTensor.ZZ = .5f;
            Body = Simulation.Bodies.Add(BodyDescription.CreateDynamic(new RigidPose(new Vector3(0, .5f, 0)),
                default, inertia, new CollidableDescription(BodyShape, .01f), new BodyActivityDescription(-1)));
            Plane = Simulation.Statics.Add(new StaticDescription(new Vector3(0, -1, 0), SurfaceShape));
            // Retained numerical fixture protocol, entirely BEFORE a canonical episode exists.
            for (var i = 0; i < 120; i++) Simulation.Timestep(1f / 60);
            var body = Simulation.Bodies.GetBodyReference(Body);
            body.Velocity.Linear.X = (float)vx;
            body.Velocity.Angular.Y = (float)wy;
            var native = body.Pose.Orientation;
            if (SpacecraftRigidBodyRotationEvaluator.TryCanonicalize(new(native.X, native.Y, native.Z, native.W), out var orientation)
                != SpacecraftRigidBodyRotationEvaluationStatus.Success) throw new InvalidOperationException("Invalid prepared orientation.");
            static Double3 Copy(Vector3 x) => new(x.X, x.Y, x.Z);
            Initial = new(config.OriginRoot + Copy(body.Pose.Position), config.OriginVelocityRoot + Copy(body.Velocity.Linear),
                orientation, orientation.Conjugate().Rotate(Copy(body.Velocity.Angular)));
            PositionImportError = Math.Sqrt((Initial.Position - config.OriginRoot - Copy(body.Pose.Position)).LengthSquared);
            VelocityImportError = Math.Sqrt((Initial.Velocity - config.OriginVelocityRoot - Copy(body.Velocity.Linear)).LengthSquared);
            // Author the finite fixture's desired initial local wrench in the body's declared frame.
            var rootToBody = orientation.Conjugate();
            if (IdealEngineDefinition.TryCreate(1, 1, rootToBody.Rotate(new(0, 0, mount)), rootToBody.Rotate(axis),
                thrust, exhaust, true, out var engine) != EnginePreparationStatus.Ready)
                throw new InvalidOperationException("Invalid prepared engine.");
            EngineDefinition = engine!;
        }
        catch { Simulation.Dispose(); Pool.Clear(); throw; }
    }

    internal static PoweredFlightStatus TryPrepare(PoweredContactFixture fixture, ReferenceFrameId root,
        Double3 origin, Double3 frameVelocity, out PoweredContactPreparation? result)
    {
        result = null;
        if (!Enum.IsDefined(fixture) ||
            LocalContactConfiguration.TryCreatePoweredFixture(root, origin, frameVelocity, out var config) != LocalContactStatus.Success)
            return PoweredFlightStatus.InvalidInput;
        double thrust = 80, exhaust = 5120, mount = 0, vx = 1, wy = 0, throttle = 1;
        var axis = new Double3(3, 4, 0);
        var fuel = .00013020833333333333;
        var ignite = true;
        switch (fixture)
        {
            case PoweredContactFixture.CenteredBaseline:
            case PoweredContactFixture.LowThrust:
                thrust = 8; axis = Double3.UnitY; vx = 0; fuel = 1.3020833333333334e-5;
                ignite = fixture != PoweredContactFixture.CenteredBaseline; break;
            case PoweredContactFixture.SlidingBaseline:
            case PoweredContactFixture.NearUnloading:
                axis = new(7, 24, 0); fuel = 1.6276041666666666e-5;
                ignite = fixture != PoweredContactFixture.SlidingBaseline; break;
            case PoweredContactFixture.YawBaseline: wy = .12; ignite = false; break;
            case PoweredContactFixture.OffCom: wy = .12; mount = .1; break;
            case PoweredContactFixture.NearEnd: thrust = 8; fuel = 2.5781250000000003e-5; break;
            case PoweredContactFixture.MidEvent: thrust = 40; fuel = 6.510416666666667e-5; break;
            case PoweredContactFixture.TinyEvent:
            case PoweredContactFixture.ZeroEvent:
                thrust = 16; exhaust = 8; mount = .1; wy = .12;
                fuel = fixture == PoweredContactFixture.TinyEvent ? double.Epsilon : 0; break;
            case PoweredContactFixture.CenteredEndpoint:
                thrust = 8; axis = Double3.UnitY; vx = 0; fuel = 33333d / 1073741824;
                throttle = 78125d / 131072; break;
        }
        if (PropellantDefinition.TryCreate(new(1, 1, 1, 1, 1, 1, 1, PropellantMassLaw.CentralPointReservoirV1,
            8, new(2, 2, 2)), fuel, out var resource) != PropellantPreparationStatus.Ready)
            return PoweredFlightStatus.InvalidInput;
        var prepared = new PoweredContactPreparation(fixture, config!, resource!, thrust, exhaust, axis, mount, vx, wy, throttle, ignite);
        if (!prepared.FrameImportFits) { prepared.Dispose(); return PoweredFlightStatus.OutsideModel; }
        result = prepared;
        return PoweredFlightStatus.Ready;
    }

    // Caller has completed all checks and owns the sole canonical publication phase.
    internal LocalContactWorld TransferTo(LocalContactSource source, SimulationTransactionEngine engine,
        PoweredFlightAuthority authority, SpacecraftPhysicalSource physical, SpacecraftDefinition definition,
        PoweredFlightObservation observation)
    {
        if (!Available || !engine.OwnsPersistentPublicationPhase)
            throw new InvalidOperationException("Prepared ownership transfer requires the admitted owner phase.");
        var world = new LocalContactWorld(source, this, engine, authority, physical, definition, observation,
            Pool!, Simulation!, Body, Plane, BodyShape, SurfaceShape, Metrics!, Input!);
        transferred = true;
        Pool = null; Simulation = null; Metrics = null; Input = null;
        return world;
    }
    public void Dispose()
    {
        if (ownerThread != Environment.CurrentManagedThreadId) throw new InvalidOperationException("Preparation owner thread required.");
        if (disposed || transferred) return;
        disposed = true;
        Simulation!.Dispose(); Pool!.Clear();
        Simulation = null; Pool = null; Metrics = null; Input = null;
    }
}
