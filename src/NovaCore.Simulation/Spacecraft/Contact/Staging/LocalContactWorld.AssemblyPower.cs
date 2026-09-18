using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Transactions;

namespace NovaCore.Simulation.Spacecraft.Contact.Staging;

internal sealed partial class LocalContactWorld
{
    private LocalContactStepInput? assemblyInput;
    private AssemblyMass assemblyMass;
    private long assemblyMassFrontier;
    private long assemblyPreparedFrontier;
    // Rounding residue from reference-point changes only, in the fixed local frame.
    // Solver-generated motion is never accumulated here or recomputed.
    private Double3 assemblyPositionResidue,assemblyVelocityResidue;

    internal LocalContactStatus PrepareAssemblyPower(SimulationTransactionEngine engine,AssemblyMass mass,AssemblyWrench wrench)
    {
        var status=CheckAssemblyReady(engine);
        if(status!=LocalContactStatus.Success)return status;
        if(!engine.OwnsPersistentPublicationPhase||assembly is not {Pending:false}||assemblyInput is null||mass!=assemblyMass||assemblyMassFrontier!=frontier||assemblyPreparedFrontier!=0)
            return LocalContactStatus.InvalidSource;
        var native=simulation.Bodies[body];var fq=native.Pose.Orientation;
        var q=new DoubleQuaternion(fq.X,fq.Y,fq.Z,fq.W)*AssemblyToNativeRotation;
        var linear=q.Rotate(wrench.Force/mass.Mass)+new Double3(0,-9.81,0);
        var torque=wrench.MomentAtOrigin-Double3.Cross(mass.Com,wrench.Force);
        // BEPU already performs gyroscopic integration; only applied torque belongs here.
        var angular=q.Rotate(mass.Inertia.Inverse().Apply(torque));
        if(!linear.IsFinite||!angular.IsFinite||!Finite(ToFloat(linear))||!Finite(ToFloat(angular)))return LocalContactStatus.PrecisionEnvelopeExceeded;
        assemblyInput.Linear=ToFloat(linear);assemblyInput.Angular=ToFloat(angular);
        metrics.Coverage?.SetPreparedAcceleration(assemblyInput.Linear);
        assemblyPreparedFrontier=frontier+1;
        return LocalContactStatus.Success;
    }

    // Called once while the solved endpoint is pending, never on publication retry.
    // Numerical change of reference point only: no canonical pose import or solver call.
    internal LocalContactStatus RefreshAssemblyMass(SimulationTransactionEngine engine,AssemblyMass expected,AssemblyMass successor)
    {
        var status=CheckAssemblyReady(engine);
        if(status!=LocalContactStatus.Success)return status;
        if(!engine.OwnsPersistentPublicationPhase||assembly is not {Pending:true}||assemblyInput is null||
            expected!=assemblyMass||assemblyMassFrontier!=frontier-1||invalidated||disposed)return LocalContactStatus.InvalidSource;
        var profile=configuration.AssemblyProfile!;
        var native=simulation.Bodies[body];var fq=native.Pose.Orientation;
        var q=new DoubleQuaternion(fq.X,fq.Y,fq.Z,fq.W);
        var delta=q.Rotate(AssemblyToNativeBody.Apply(successor.Com-expected.Com));
        var p=FromFloat(native.Pose.Position)+delta+assemblyPositionResidue;
        var v=FromFloat(native.Velocity.Linear)+Double3.Cross(FromFloat(native.Velocity.Angular),delta)+assemblyVelocityResidue;
        var fp=ToFloat(p);var fv=ToFloat(v);
        var positionResidue=p-FromFloat(fp);var velocityResidue=v-FromFloat(fv);
        var inverse=AssemblyToNativeBody*successor.Inertia.Inverse()*AssemblyToNativeBody.Transpose();
        var inertia=new BodyInertia{InverseMass=(float)(1/successor.Mass)};
        inertia.InverseInertiaTensor.XX=(float)inverse.A;inertia.InverseInertiaTensor.YX=(float)inverse.D;
        inertia.InverseInertiaTensor.YY=(float)inverse.E;inertia.InverseInertiaTensor.ZX=(float)inverse.G;
        inertia.InverseInertiaTensor.ZY=(float)inverse.H;inertia.InverseInertiaTensor.ZZ=(float)inverse.I;
        Span<Vector3> positions=stackalloc Vector3[profile.Children.Length];
        for(var i=0;i<positions.Length;i++)
        {
            var precise=AssemblyToNativeBody.Apply(profile.Children[i].AtOrigin.Position-successor.Com);
            positions[i]=ToFloat(precise);
            if(!precise.IsFinite||!Finite(positions[i])||Math.Sqrt((FromFloat(positions[i])-precise).LengthSquared)>configuration.ContactTolerance/8)
                return LocalContactStatus.PrecisionEnvelopeExceeded;
        }
        if(!Within(configuration,p,v,FromFloat(native.Velocity.Angular))||
            Math.Sqrt(positionResidue.LengthSquared)>configuration.ContactTolerance/8||
            Math.Sqrt(velocityResidue.LengthSquared)>configuration.ContactTolerance/8||!PositiveFloat(1/successor.Mass)||
            !PositiveFloat(inverse.A)||!PositiveFloat(inverse.E)||!PositiveFloat(inverse.I)||
            !double.IsFinite(inverse.D)||!double.IsFinite(inverse.G)||!double.IsFinite(inverse.H))return LocalContactStatus.PrecisionEnvelopeExceeded;
        invalidated=true;
        try
        {
            ref var compound=ref simulation.Shapes.GetShape<Compound>(bodyShape.Index);
            native.Pose.Position=fp;native.Velocity.Linear=fv;
            native.SetLocalInertia(inertia);
            for(var i=0;i<positions.Length;i++)compound.Children[i].LocalPosition=positions[i];
            native.UpdateBounds();
            assemblyMass=successor;
            assemblyMassFrontier=frontier;
            assemblyPositionResidue=positionResidue;assemblyVelocityResidue=velocityResidue;
            // Preserve the solved material-origin endpoint; expose its COM coordinates
            // with successor properties. This is not a second native read/import.
            var m=export.Motion;
            var rootDelta=m.BodyToRoot.Rotate(successor.Com-expected.Com);
            export=export with {Motion=m with {
                PositionRoot=m.PositionRoot+rootDelta,
                VelocityRoot=m.VelocityRoot+m.BodyToRoot.Rotate(Double3.Cross(m.AngularVelocityBody,successor.Com-expected.Com)),
                Properties=new(successor.Mass),
                Inertia=new(successor.Inertia.A,successor.Inertia.E,successor.Inertia.I)},AssemblyProperties=successor};
            invalidated=false;return LocalContactStatus.Success;
        }
        catch(Exception){return LocalContactStatus.SolverFailure;}
    }
}
