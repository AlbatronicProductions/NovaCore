using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;

namespace NovaCore.Simulation.Spacecraft.Contact.Staging;

internal sealed partial class LocalContactWorld
{
    // Fixed coordinate convention for the admitted upright assembly, about the same COM.
    // xNative = B*xAssembly; qNative = qAssembly*inverse(B). Small support rotations
    // then occupy near-zero quaternion components rather than a rounded quarter turn.
    // This basis never changes during the retained episode and is undone on export.
    private static readonly Matrix3 AssemblyToNativeBody = new(0,-1,0,1,0,0,0,0,1);
    private static readonly DoubleQuaternion AssemblyToNativeRotation = AssemblyContactProfile.Upright;

    private sealed class AssemblyBinding(AssemblyFlightAuthority authority,StateRevision revision,ContinuationClockState clock)
    {
        internal readonly AssemblyFlightAuthority Authority=authority;
        internal StateRevision Revision=revision;
        internal ContinuationClockState Clock=clock;
        internal long AcknowledgedFrontier;
        internal bool Pending;
    }
    private AssemblyBinding? assembly;

    private static TypedIndex CreateAssemblyShape(Shapes shapes,BufferPool pool,AssemblyContactProfile profile)
    {
        pool.Take<CompoundChild>(profile.Children.Length,out var children);
        var created=0;
        try
        {
            foreach(var child in profile.Children)
            {
                var d=child.Dimensions;var q=AssemblyRotation(AssemblyToNativeBody*child.AtOrigin.Rotation);
                var shape=shapes.Add(new Box((float)d.X,(float)d.Y,(float)d.Z));
                children[created++]=new(new RigidPose(ToFloat(AssemblyToNativeBody.Apply(child.AtOrigin.Position-profile.Mass.Com)),q),shape);
            }
            return shapes.Add(new Compound(children));
        }
        catch
        {
            for(var i=0;i<created;i++)shapes.RemoveAndDispose(children[i].ShapeIndex,pool);
            pool.Return(ref children);throw;
        }
    }
    private static Quaternion AssemblyRotation(Matrix3 m)
    {
        double x,y,z,w;var trace=m.A+m.E+m.I;
        if(trace>0){var s=Math.Sqrt(trace+1)*2;w=s/4;x=(m.H-m.F)/s;y=(m.C-m.G)/s;z=(m.D-m.B)/s;}
        else if(m.A>m.E&&m.A>m.I){var s=Math.Sqrt(1+m.A-m.E-m.I)*2;w=(m.H-m.F)/s;x=s/4;y=(m.B+m.D)/s;z=(m.C+m.G)/s;}
        else if(m.E>m.I){var s=Math.Sqrt(1+m.E-m.A-m.I)*2;w=(m.C-m.G)/s;x=(m.B+m.D)/s;y=s/4;z=(m.F+m.H)/s;}
        else{var s=Math.Sqrt(1+m.I-m.A-m.E)*2;w=(m.D-m.B)/s;x=(m.C+m.G)/s;y=(m.F+m.H)/s;z=s/4;}
        return Quaternion.Normalize(new((float)x,(float)y,(float)z,(float)w));
    }
    internal LocalContactStatus CheckAssemblyPublication(SimulationTransactionEngine engine,Receipt receipt,AssemblyMass successorMass)
    {
        var status=Validate(engine,configuration,receipt,export.Motion.Time);
        if(status!=LocalContactStatus.Success)return status;
        return engine.OwnsPersistentPublicationPhase&&assembly is {Pending:true} p&&frontier==p.AcknowledgedFrontier+1&&assemblyMass==successorMass
            ?LocalContactStatus.Success:LocalContactStatus.FrontierMismatch;
    }
    // Fixed expectation writes only, in the same owner phase as the canonical commit.
    internal void AcknowledgeAssembly(StateRevision revision,ContinuationClockState clock)
    {
        var p=assembly!;p.Revision=revision;p.Clock=clock;p.AcknowledgedFrontier=frontier;p.Pending=false;
    }
    internal void AcknowledgeAssemblyCredit(ContinuationClockState clock)=>assembly!.Clock=clock;
    internal void InvalidateAssembly()=>invalidated=true;
    internal LocalContactStatus CheckAssemblyReady(SimulationTransactionEngine engine)
    {
        var receipt=new Receipt(this,frontier,receiptIdentity);
        return Validate(engine,configuration,receipt,export.Motion.Time);
    }
    internal (long Generation,long Frontier,int Body,int Shape,bool Pending,bool Invalidated) AssemblyIdentityForTest =>
        (Generation,frontier,body.Value,bodyShape.Index,assembly?.Pending??false,invalidated||disposed);
}
