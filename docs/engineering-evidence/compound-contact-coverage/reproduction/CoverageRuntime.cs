// Disposable single-transition contact-retention experiment. Never compiled by ordinary builds.
using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuPhysics.Constraints.Contact;
using BepuUtilities;
using NovaCore.Core;
using SolverPoint = BepuPhysics.CollisionDetection.Contact;

namespace NovaCore.Simulation.Spacecraft.Contact.Staging;

internal sealed class CoverageProbe
{
    internal readonly record struct Candidate(int Child, int RawFeature, SolverPoint Contact, float PredictedDepth);
    internal readonly record struct SolverContact(Vector3 Offset, Vector3 Normal, float Depth, float Impulse, Vector2 Tangent);
    internal readonly record struct SolverSnapshot(int TypeId, bool Convex, SolverContact[] Contacts);
    internal readonly record struct Pose(Vector3 P, Quaternion Q, Vector3 V, Vector3 W, int Body, int Static, long Generation, long Frontier);
    private readonly BepuPhysics.Simulation simulation;
    private readonly BodyHandle body;
    private readonly TypedIndex shape;
    private readonly string arm;
    internal int Step, CandidateCount, Replacements;
    internal float Dt;
    internal readonly Candidate[] Candidates = new Candidate[12];
    internal SolverPoint[] Reduced = [], Presented = [];
    internal SolverSnapshot[] BeforeSolve = [], AfterSolve = [];
    internal int SelectedRawFeature, SelectedChild = -1;
    internal float SelectedPrediction;
    internal float MaximumTransportDifference;

    internal CoverageProbe(BepuPhysics.Simulation simulation, BodyHandle body, TypedIndex shape, string arm)
    {
        if (arm is not ("normal" or "right" or "bus")) throw new InvalidOperationException("Unknown arm");
        this.simulation=simulation; this.body=body; this.shape=shape; this.arm=arm;
        if(simulation.Timestepper is not DefaultTimestepper timestepper) throw new InvalidOperationException("Unexpected timestepper");
        timestepper.CollisionsDetected += (_,_) => BeforeSolve=Extract();
        timestepper.ConstraintsSolved += (_,_) => AfterSolve=Extract();
    }
    internal void Begin(int step, float dt)
    { Step=step;Dt=dt;CandidateCount=0;Reduced=[];Presented=[];BeforeSolve=[];AfterSolve=[]; }
    internal void Child(CollidablePair pair, int childA, int childB, ref ConvexContactManifold manifold)
    {
        if(pair.A.Mobility!=CollidableMobility.Dynamic) throw new InvalidOperationException("Unexpected ordering");
        var state=simulation.Bodies[body];
        ref var child=ref simulation.Shapes.GetShape<Compound>(shape.Index).Children[childA];
        Compound.GetRotatedChildPose(child.LocalPosition,child.LocalOrientation,state.Pose.Orientation,out var rotated);
        for(var i=0;i<manifold.Count;i++)
        {
            if(CandidateCount==12)throw new InvalidOperationException("Candidate bound");
            var contact=manifold[i];var rawFeature=contact.FeatureId;
            contact.Offset+=rotated.Position;
            contact.FeatureId ^= (childA<<8) ^ (childB<<16);
            var normalVelocity=Vector3.Dot(state.Velocity.Linear+Vector3.Cross(state.Velocity.Angular,contact.Offset),contact.Normal);
            Candidates[CandidateCount++]=new(childA,rawFeature,contact,contact.Depth-normalVelocity*Dt);
        }
    }
    internal void Parent<T>(ref T manifold) where T:unmanaged,IContactManifold<T>
    {
        Reduced=new SolverPoint[manifold.Count];
        for(var i=0;i<manifold.Count;i++)Reduced[i]=manifold[i];
        if(!manifold.Convex)
        {
            for(var i=0;i<manifold.Count;i++)
            {
                var found=false;
                for(var j=0;j<CandidateCount;j++)
                    if(Candidates[j].Contact.FeatureId==manifold[i].FeatureId)
                    {
                        var a=Candidates[j].Contact;var b=manifold[i];
                        var difference=Vector3.Distance(a.Offset,b.Offset);
                        MaximumTransportDifference=Math.Max(MaximumTransportDifference,difference);
                        if(difference>1e-6f || a.Depth!=b.Depth || a.Normal!=b.Normal)
                            throw new InvalidOperationException("Child-parent transport mismatch");
                        found=true;break;
                    }
                if(!found)throw new InvalidOperationException("Unmapped parent feature");
            }
        }
        if(Step==35)
        {
            if(manifold.Convex || manifold.Count!=4 || CandidateCount!=12 || manifold[3].FeatureId!=-20)
                throw new InvalidOperationException("Interval35 structural mismatch");
            if(arm!="normal")
            {
                var requested=arm=="right"?2:0;var selected=-1;var score=float.NegativeInfinity;
                for(var i=0;i<CandidateCount;i++)
                {
                    var candidate=Candidates[i];if(candidate.Child!=requested)continue;
                    var retained=false;foreach(var c in Reduced)if(c.FeatureId==candidate.Contact.FeatureId)retained=true;
                    if(!retained && candidate.PredictedDepth>score){score=candidate.PredictedDepth;selected=i;}
                }
                if(selected<0)throw new InvalidOperationException("No missing candidate");
                var chosen=Candidates[selected];
                // The predictor selects a real candidate; its original depth/normal/position are unchanged.
                manifold[3]=chosen.Contact;SelectedChild=chosen.Child;SelectedRawFeature=chosen.RawFeature;
                SelectedPrediction=score;Replacements++;
            }
        }
        Presented=new SolverPoint[manifold.Count];
        for(var i=0;i<manifold.Count;i++)Presented[i]=manifold[i];
    }
    private SolverSnapshot[] Extract()
    {
        var state=simulation.Bodies[body];var result=new SolverSnapshot[state.Constraints.Count];
        for(var i=0;i<state.Constraints.Count;i++)
        {
            var handle=state.Constraints[i].ConnectingConstraintHandle;
            var extractor=new Extractor();
            if(!simulation.NarrowPhase.TryExtractSolverContactData(handle,ref extractor))throw new InvalidOperationException("Not contact constraint");
            var type=simulation.Solver.HandleToConstraint[handle.Value].TypeId;
            result[i]=new(type,extractor.Convex,extractor.Contacts!);
        }
        return result;
    }
    private struct Extractor:ISolverContactDataExtractor
    {
        internal SolverContact[]? Contacts;internal bool Convex;
        private void C<P,I>(ref P p,ref I impulses) where P:struct,IConvexContactPrestep<P> where I:struct,IConvexContactAccumulatedImpulses<I>
        {
            Convex=true;Contacts=new SolverContact[P.ContactCount];Vector3Wide.ReadFirst(P.GetNormal(ref p),out var normal);
            for(var i=0;i<Contacts.Length;i++)
            {ref var c=ref P.GetContact(ref p,i);Vector3Wide.ReadFirst(c.OffsetA,out var offset);Contacts[i]=new(offset,normal,c.Depth[0],I.GetPenetrationImpulseForContact(ref impulses,i)[0],default);}
        }
        private void N<P,I>(ref P p,ref I impulses) where P:struct,INonconvexContactPrestep<P> where I:struct,INonconvexContactAccumulatedImpulses<I>
        {
            Convex=false;Contacts=new SolverContact[P.ContactCount];
            for(var i=0;i<Contacts.Length;i++)
            {ref var c=ref P.GetContact(ref p,i);Vector3Wide.ReadFirst(c.Offset,out var offset);Vector3Wide.ReadFirst(c.Normal,out var normal);
                ref var impulse=ref I.GetImpulsesForContact(ref impulses,i);Vector2Wide.ReadFirst(impulse.Tangent,out var tangent);
                Contacts[i]=new(offset,normal,c.Depth[0],impulse.Penetration[0],tangent);}
        }
        public void ConvexOneBody<P,I>(BodyHandle a,ref P p,ref I i) where P:struct,IConvexContactPrestep<P> where I:struct,IConvexContactAccumulatedImpulses<I> => C(ref p,ref i);
        public void ConvexTwoBody<P,I>(BodyHandle a,BodyHandle b,ref P p,ref I i) where P:struct,ITwoBodyConvexContactPrestep<P> where I:struct,IConvexContactAccumulatedImpulses<I> => C(ref p,ref i);
        public void NonconvexOneBody<P,I>(BodyHandle a,ref P p,ref I i) where P:struct,INonconvexContactPrestep<P> where I:struct,INonconvexContactAccumulatedImpulses<I> => N(ref p,ref i);
        public void NonconvexTwoBody<P,I>(BodyHandle a,BodyHandle b,ref P p,ref I i) where P:struct,ITwoBodyNonconvexContactPrestep<P> where I:struct,INonconvexContactAccumulatedImpulses<I> => N(ref p,ref i);
    }
}
internal sealed partial class LocalContactWorld
{
    internal CoverageProbe EnableCoverageProbe(string arm)=>metrics.Coverage=new(simulation,body,bodyShape,arm);
    internal CoverageProbe.Pose CoveragePose()
    {var s=simulation.Bodies[body];return new(s.Pose.Position,s.Pose.Orientation,s.Velocity.Linear,s.Velocity.Angular,body.Value,plane.Value,Generation,frontier);}
}
