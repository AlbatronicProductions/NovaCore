using System.Numerics;
using BepuPhysics;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuPhysics.Constraints.Contact;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Spacecraft.Contact.Staging;

internal static partial class AssemblyContactAdmissionTests
{
    // Read the actual solved rows. No production thrust/resource/acceleration helper
    // participates in the expected impulse equation below.
    private struct SupportImpulse : ISolverContactPrestepAndImpulsesExtractor
    {
        internal double NormalImpulse;
        internal int Contacts;
        public void ConvexOneBody<TP,TI>(ref TP p,ref TI i)
            where TP:struct,IConvexContactPrestep<TP> where TI:struct,IConvexContactAccumulatedImpulses<TI>
        {
            ref var n=ref TP.GetNormal(ref p);
            Check(n.Y[0]>0&&float.IsFinite(n.X[0])&&float.IsFinite(n.Y[0])&&float.IsFinite(n.Z[0]),"finite upward native support normal");
            Contacts+=TP.ContactCount;
            for(var j=0;j<TP.ContactCount;j++)NormalImpulse+=(double)n.Y[0]*TI.GetPenetrationImpulseForContact(ref i,j)[0];
            // The native collision normal can have FP32 tilt/non-unit rounding.
            // Read the full actual vertical impulse, including tangent components.
            Helpers.BuildOrthonormalBasis(n,out var t1,out var t2);
            ref var friction=ref TI.GetTangentFriction(ref i);
            NormalImpulse+=(double)t1.Y[0]*friction.X[0]+(double)t2.Y[0]*friction.Y[0];
        }
        public void ConvexTwoBody<TP,TI>(ref TP p,ref TI i) where TP:struct,ITwoBodyConvexContactPrestep<TP> where TI:struct,IConvexContactAccumulatedImpulses<TI> => throw new InvalidOperationException("Unexpected second dynamic body.");
        public void NonconvexOneBody<TP,TI>(ref TP p,ref TI i) where TP:struct,INonconvexContactPrestep<TP> where TI:struct,INonconvexContactAccumulatedImpulses<TI> => throw new InvalidOperationException("Unexpected support row kind.");
        public void NonconvexTwoBody<TP,TI>(ref TP p,ref TI i) where TP:struct,ITwoBodyNonconvexContactPrestep<TP> where TI:struct,INonconvexContactAccumulatedImpulses<TI> => throw new InvalidOperationException("Unexpected support row kind.");
    }
    private static double ThrustResidual(AssemblyApplicationSession s,LocalContactWorld w,AssemblyFlightObservation before,double nativeVelocityBefore,bool requireThrust=true)
    {
        var sim=NativeField<Simulation>(w,"simulation");var body=sim.Bodies[NativeField<BodyHandle>(w,"body")];
        var rows=new SupportImpulse();Check(body.Constraints.Count==1,"one native support constraint");
        Check(sim.NarrowPhase.TryExtractSolverContactPrestepAndImpulses(body.Constraints[0].ConnectingConstraintHandle,ref rows)&&rows.Contacts>0,"read native support impulses");
        var dt=s.Launch.Plan[before.State.Frontier].Request.Ticks/1e6;
        var sourceMass=705-.1953125*before.State.Epoch.Ticks/1e6;
        var recoveredImpulse=sourceMass*(body.Velocity.Linear.Y-nativeVelocityBefore+9.81*dt)-rows.NormalImpulse;
        var error=Math.Abs(recoveredImpulse-600*dt);
        if(requireThrust)Check(error<=.01,"independent supported thrust: m*(dv+g*dt)-contact impulse == 600*dt within 0.01 N s");
        return error;
    }
    private static void CheckAssemblyExport(AssemblyApplicationSession s,LocalContactWorld w)
    {
        var storage=Field(s.Engine,"_assemblyFlight");var receipt=(LocalContactWorld.Receipt)Field(storage,"ContactReceipt");
        var config=NativeField<LocalContactConfiguration>(w,"configuration");
        Check(w.Read(s.Engine,config,receipt,out var e)==LocalContactStatus.Success,"issued assembly export readable");
        var expected=(AssemblyMass)Field(w,"assemblyMass");
        Check(e.AssemblyProperties==expected&&e.Motion.Properties.MassKilograms==expected.Mass&&
            e.Motion.Inertia.X==expected.Inertia.A&&e.Motion.Inertia.Y==expected.Inertia.E&&e.Motion.Inertia.Z==expected.Inertia.I,
            "export successor mass/COM/full tensor and diagonal compatibility properties");
        var initial=s.Launch.Initial.Mass;
        var transverseShift=734.4*734.4*(1/705d-1/expected.Mass);
        Check(Math.Abs(expected.Com.X-734.4/expected.Mass)<1e-14&&expected.Com.Y==0&&expected.Com.Z==0&&
            Math.Abs(expected.Inertia.A-initial.Inertia.A)<1e-10&&
            Math.Abs(expected.Inertia.E-(initial.Inertia.E+transverseShift))<1e-10&&
            Math.Abs(expected.Inertia.I-(initial.Inertia.I+transverseShift))<1e-10&&
            expected.Inertia.B==initial.Inertia.B&&expected.Inertia.C==initial.Inertia.C&&expected.Inertia.F==initial.Inertia.F,
            "independent point-store parallel-axis mass/COM/tensor successor");
        var current=Observe(s);
        if(!w.AssemblyIdentityForTest.Pending)
        {
            var origin=AssemblyContactProfile.ToOrigin(e.Motion.PositionRoot,e.Motion.VelocityRoot,e.Motion.BodyToRoot,e.Motion.AngularVelocityBody,expected.Com);
            Check((origin.PositionO-current.State.Motion.PositionO).LengthSquared<1e-26&&
                (origin.VelocityO-current.State.Motion.VelocityO).LengthSquared<1e-26&&Bits(origin.BodyToWorld,current.State.Motion.BodyToWorld),
                "successor COM export represents exact published material-origin endpoint within FP64 coordinate roundoff");
        }
    }
    private static void PoweredClosure()
    {
        using(var s=PoweredSession())using(var other=PoweredSession())
        {
            var w=s.Engine.AssemblyContactWorldForTest(s.Authority)!;var config=NativeField<LocalContactConfiguration>(w,"configuration");
            var storage=Field(s.Engine,"_assemblyFlight");var receipt=(LocalContactWorld.Receipt)Field(storage,"ContactReceipt");
            var before=Observe(s);var identity=w.AssemblyIdentityForTest;var points=NativeMaterialPoints(w);
            Check(s.Clock.PublicationPhase.TryEnter(s.Engine),"direct helper owner");
            try
            {
                Check(w.Step(s.Engine,config,receipt,new(16666),out _)==LocalContactStatus.InvalidSource,"step without prepared input refuses");
                Check(w.PrepareAssemblyPower(s.Engine,before.State.Mass,new(new(600,0,0),default))==LocalContactStatus.Success,"prepared input bound once");
                Check(w.PrepareAssemblyPower(s.Engine,before.State.Mass,new(new(600,0,0),default))==LocalContactStatus.InvalidSource,"duplicate preparation refuses");
                Check(w.Step(s.Engine,config,receipt,new(16667),out _)==LocalContactStatus.InvalidInterval,"out-of-order target refuses without consuming input");
                Check((long)Field(w,"assemblyPreparedFrontier")==1,"invalid target preserves prepared permission");
                Check(identity==w.AssemblyIdentityForTest&&points.SequenceEqual(NativeMaterialPoints(w)),"helper refusals no physical mutation");
            }
            finally{s.Clock.PublicationPhase.Exit();}
            Check(Observe(s)==before,"helper refusals canonical nonmutation after leaving exclusive owner phase");
            Check(other.Clock.PublicationPhase.TryEnter(other.Engine),"foreign owner phase");
            try
            {
                Check(w.PrepareAssemblyPower(other.Engine,before.State.Mass,default)==LocalContactStatus.ForeignEngine&&
                    w.RefreshAssemblyMass(other.Engine,before.State.Mass,before.State.Mass)==LocalContactStatus.ForeignEngine,"another owned phase grants no world authority");
            }
            finally{other.Clock.PublicationPhase.Exit();}
        }
        using(var s=PoweredSession())
        {
            var w=s.Engine.AssemblyContactWorldForTest(s.Authority)!;var sim=NativeField<Simulation>(w,"simulation");var body=sim.Bodies[NativeField<BodyHandle>(w,"body")];
            var maximum=0d;
            for(var i=0;i<64;i++)
            {
                var before=Observe(s);var vy=body.Velocity.Linear.Y;PoweredNext(s);
                maximum=Math.Max(maximum,ThrustResidual(s,w,before,vy));CheckAssemblyExport(s,w);
                Check((long)Field(w,"assemblyPreparedFrontier")==0,"step consumes prepared permission");
            }
            Console.WriteLine($"POWERED_CONTACT_THRUST nativeImpulseResidualMax={maximum:R} Ns expected=600*dt samples=64");
        }
        using(var s=PoweredSession())
        {
            var w=s.Engine.AssemblyContactWorldForTest(s.Authority)!;var config=NativeField<LocalContactConfiguration>(w,"configuration");
            var storage=Field(s.Engine,"_assemblyFlight");var receipt=(LocalContactWorld.Receipt)Field(storage,"ContactReceipt");
            var before=Observe(s);var sim=NativeField<Simulation>(w,"simulation");var body=sim.Bodies[NativeField<BodyHandle>(w,"body")];var vy=body.Velocity.Linear.Y;
            Check(s.Clock.PublicationPhase.TryEnter(s.Engine),"missing-thrust control owner");
            try
            {
                Check(w.PrepareAssemblyPower(s.Engine,before.State.Mass,default)==LocalContactStatus.Success&&
                    w.Step(s.Engine,config,receipt,new(16666),out _)==LocalContactStatus.Success,"one native supported zero-thrust control");
            }
            finally{s.Clock.PublicationPhase.Exit();}
            var missing=ThrustResidual(s,w,before,vy,false);
            Check(missing>9,"independent impulse oracle rejects omitted main thrust by over9 N s");
            Console.WriteLine($"POWERED_CONTACT_MISSING_THRUST_CONTROL residual={missing:R} Ns rejection=PASS canonicalUnchanged={Observe(s)==before}");
            Check(Observe(s)==before,"native control never publishes or creates resource authority");
        }
        // Numeric dry successor coverage at the actual refresh/export boundary. This
        // does not claim stock exhaustion occurs in its twenty-second live episode.
        using(var s=PoweredSession())
        {
            Credit(s,16666);Check(s.Engine.PrepareAssemblyContact(s.Authority,out _)==AssemblyFlightStatus.Prepared,"dry-property boundary prepared");
            var w=s.Engine.AssemblyContactWorldForTest(s.Authority)!;var mass=(AssemblyMass)Field(w,"assemblyMass");
            SetField(w,"assemblyMassFrontier",0L); // Re-arm only this isolated numerical test; never publish it.
            var dry=s.Launch.Design.ObserveMass(630);var before=Observe(s);var points=NativeMaterialPoints(w);
            Check(s.Clock.PublicationPhase.TryEnter(s.Engine),"dry-property owner");
            try{Check(w.RefreshAssemblyMass(s.Engine,mass,dry)==LocalContactStatus.Success,"dry successor refresh/export");}
            finally{s.Clock.PublicationPhase.Exit();}
            CheckAssemblyExport(s,w);
            var after=NativeMaterialPoints(w);
            for(var i=0;i<points.Length;i++)Check(Vector3.Distance(points[i].Position,after[i].Position)<=.000061,"dry COM refresh material-point continuity");
            Check(Observe(s)==before,"isolated dry-property check does not invent canonical resource consumption");
        }
        Console.WriteLine("POWERED_CONTACT_CLOSURE PASS prepared ownership/order; physical thrust; post-burn/dry numerical properties");
    }
}
