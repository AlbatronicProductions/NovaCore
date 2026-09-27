using System.Numerics;
using BepuPhysics;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuPhysics.Constraints.Contact;
using BepuUtilities;
using BepuUtilities.Memory;

namespace NovaCore.Simulation.Spacecraft.Contact.Staging;

// Project-owned handles must not use native contact IDs: their removal would
// incorrectly retire a NarrowPhase pair-cache entry. These are type identities,
// not a limit on craft parts, contacts, support sites or solver handles.
internal struct CraftContactDescription<TP,TI,TF> : IOneBodyConstraintDescription<CraftContactDescription<TP,TI,TF>>
    where TP:unmanaged,IConvexContactPrestep<TP>
    where TI:unmanaged,IConvexContactAccumulatedImpulses<TI>
    where TF:unmanaged,IOneBodyConstraintFunctions<TP,TI>
{
    internal ConvexContactManifold Manifold;
    internal PairMaterialProperties Material;
    public static int ConstraintTypeId=>256+TP.ContactCount;
    public static Type TypeProcessorType=>typeof(Processor);
    public static TypeProcessor CreateTypeProcessor()=>new Processor();
    public sealed class Processor:OneBodyContactTypeProcessor<TP,TI,TF> { }
    public readonly void ApplyDescription(ref TypeBatch batch,int bundle,int lane)
    {
        ref var p=ref GatherScatter.GetOffsetInstance(ref Buffer<TP>.Get(ref batch.PrestepData,bundle),lane);
        for(var j=0;j<TP.ContactCount;j++){
            ref var c=ref TP.GetContact(ref p,j);var row=Manifold[j];
            Vector3Wide.WriteFirst(row.Offset,ref c.OffsetA);GatherScatter.GetFirst(ref c.Depth)=row.Depth;
        }
        Vector3Wide.WriteFirst(Manifold.Normal,ref TP.GetNormal(ref p));
        ref var m=ref TP.GetMaterialProperties(ref p);
        // Pinned convex functions divide by manifold arity; the retired
        // nonconvex rows used mu per row. Preserve physical Coulomb mu and
        // distributed torsion: nativeMu/N * sum(Jn) == physicalMu * sum(Jn).
        GatherScatter.GetFirst(ref m.FrictionCoefficient)=Material.FrictionCoefficient*TP.ContactCount;
        SpringSettingsWide.WriteFirst(in Material.SpringSettings,ref m.SpringSettings);
        GatherScatter.GetFirst(ref m.MaximumRecoveryVelocity)=Material.MaximumRecoveryVelocity;
    }
    public static void BuildDescription(ref TypeBatch batch,int bundle,int lane,out CraftContactDescription<TP,TI,TF> value)
    {
        value=default;value.Manifold.Count=TP.ContactCount;
        ref var p=ref GatherScatter.GetOffsetInstance(ref Buffer<TP>.Get(ref batch.PrestepData,bundle),lane);
        Vector3Wide.ReadFirst(in TP.GetNormal(ref p),out value.Manifold.Normal);
        for(var j=0;j<TP.ContactCount;j++){
            ref var c=ref TP.GetContact(ref p,j);Vector3Wide.ReadFirst(in c.OffsetA,out var offset);
            value.Manifold[j]=new(){Offset=offset,Normal=value.Manifold.Normal,Depth=c.Depth[0]};
        }
        ref var m=ref TP.GetMaterialProperties(ref p);
        value.Material.FrictionCoefficient=m.FrictionCoefficient[0]/TP.ContactCount;
        SpringSettingsWide.ReadFirst(in m.SpringSettings,out value.Material.SpringSettings);
        value.Material.MaximumRecoveryVelocity=m.MaximumRecoveryVelocity[0];
    }
    internal static void ReadImpulses(Solver solver,ConstraintHandle handle,Span<float> normal,out Vector2 tangent,out float twist)
    {
        var reference=solver.GetConstraintReference(handle);
        BundleIndexing.GetBundleIndices(reference.IndexInTypeBatch,out var bundle,out var lane);
        ref var i=ref GatherScatter.GetOffsetInstance(ref Buffer<TI>.Get(ref reference.TypeBatch.AccumulatedImpulses,bundle),lane);
        for(var j=0;j<TP.ContactCount;j++)normal[j]=TI.GetPenetrationImpulseForContact(ref i,j)[0];
        ref var t=ref TI.GetTangentFriction(ref i);tangent=new(t.X[0],t.Y[0]);twist=TI.GetTwistFriction(ref i)[0];
    }
    internal static void WriteImpulses(Solver solver,ConstraintHandle handle,ReadOnlySpan<float> normal,Vector2 tangent,float twist)
    {
        var reference=solver.GetConstraintReference(handle);
        BundleIndexing.GetBundleIndices(reference.IndexInTypeBatch,out var bundle,out var lane);
        ref var i=ref GatherScatter.GetOffsetInstance(ref Buffer<TI>.Get(ref reference.TypeBatch.AccumulatedImpulses,bundle),lane);
        for(var j=0;j<TP.ContactCount;j++)GatherScatter.GetFirst(ref TI.GetPenetrationImpulseForContact(ref i,j))=normal[j];
        ref var t=ref TI.GetTangentFriction(ref i);GatherScatter.GetFirst(ref t.X)=tangent.X;GatherScatter.GetFirst(ref t.Y)=tangent.Y;
        GatherScatter.GetFirst(ref TI.GetTwistFriction(ref i))=twist;
    }
}
