using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Spacecraft.Contact.Staging;
using NovaCore.Simulation.Spacecraft.Resources;

internal static partial class AssemblyContactAdmissionTests
{
    private static AssemblyApplicationSession PoweredSession(Double3 moving=default,int capacity=1200)
    {
        var profile=AssemblyContactProfile.Create(AssemblyStockCatalog.LoadDefault().Resolve("novacore.stock.SRV01.FourHorn"));
        return AssemblyApplicationSession.CreateSupported(AssemblyLaunch.CreatePoweredSupported(profile,
            new(new(201),new(1),new(2),"SRV-01 powered supported"),"powered-supported-01",moving),capacity);
    }
    private static AssemblyFlightObservation PoweredNext(AssemblyApplicationSession s)
    {
        var before=Observe(s);Credit(s,s.Launch.Plan[before.State.Frontier].Request.Ticks);
        Check(s.Engine.PrepareAssemblyContact(s.Authority,out var receipt)==AssemblyFlightStatus.Prepared,"powered prepare");
        Check(Observe(s).State==before.State,"powered prepare canonical nonmutation");
        Check(s.Engine.PublishAssemblyContact(s.Authority,receipt).Status==AssemblyFlightStatus.Published,"powered atomic publish/ack");
        var after=Observe(s);
        Check(after.StateRevision.Value==before.StateRevision.Value+1&&after.HistoryCount==before.HistoryCount+1&&after.State.ResourceRevision==before.State.ResourceRevision+1,
            "one physical/resource/history successor");
        Check(after.TimelineRevision==before.TimelineRevision&&after.Clock.Debt==before.Clock.Debt,"timeline preserved / exact debt consumed");
        Check(after.State.Actual==new AssemblyRealization(true,0,AssemblyFeedState.Available),"main active / RCS off / feed available");
        var consumedFuel=AssemblyResources.Times(AssemblyResources.Rate(.078125),(ulong)after.State.Epoch.Ticks);
        var consumedOx=AssemblyResources.Times(AssemblyResources.Rate(.1171875),(ulong)after.State.Epoch.Ticks);
        Check(after.State.Stores==new AssemblyStores(AssemblyResources.Subtract(AssemblyResources.Mass(30),consumedFuel),AssemblyResources.Subtract(AssemblyResources.Mass(45),consumedOx)),"independent exact stock species debit");
        Check(after.State.Mass==AssemblyLaunch.ObserveMass(s.Launch.Design,after.State.Stores),"mass/COM/full tensor derived from committed stores");
        return after;
    }
    private static (Vector3 Position,Vector3 Velocity)[] NativeMaterialPoints(LocalContactWorld world)
    {
        var sim=NativeField<Simulation>(world,"simulation");var b=sim.Bodies[NativeField<BodyHandle>(world,"body")];
        ref var compound=ref sim.Shapes.GetShape<Compound>(NativeField<TypedIndex>(world,"bodyShape").Index);
        var points=new (Vector3,Vector3)[compound.Children.Length*8];
        for(var i=0;i<compound.Children.Length;i++)
        {
            var child=compound.Children[i];ref var box=ref sim.Shapes.GetShape<Box>(child.ShapeIndex.Index);
            for(var k=0;k<8;k++)
            {
                var p=new Vector3(((k&1)==0?-.5f:.5f)*box.Width,((k&2)==0?-.5f:.5f)*box.Height,((k&4)==0?-.5f:.5f)*box.Length);
                var r=Vector3.Transform(child.LocalPosition+Vector3.Transform(p,child.LocalOrientation),b.Pose.Orientation);
                points[i*8+k]=(b.Pose.Position+r,b.Velocity.Linear+Vector3.Cross(b.Velocity.Angular,r));
            }
        }
        return points;
    }
    internal static void PoweredCheap()
    {
        // Independent numerical coordinate test: deliberately use a rotating native pose,
        // without solving or publishing. Reflection is confined to this test fixture.
        using(var s=PoweredSession())
        {
            var world=s.Engine.AssemblyContactWorldForTest(s.Authority)!;var identity=world.AssemblyIdentityForTest;
            var sim=NativeField<Simulation>(world,"simulation");var body=sim.Bodies[NativeField<BodyHandle>(world,"body")];
            body.Pose.Orientation=Quaternion.Normalize(new(.1f,.07f,-.05f,1));body.Velocity.Angular=new(.02f,-.03f,.01f);body.Velocity.Linear=new(.03f,.02f,-.01f);
            var original=NativeMaterialPoints(world);var orientation=body.Pose.Orientation;var mass=s.Launch.Initial.Mass;
            var maxPosition=0d;var maxVelocity=0d;
            Check(s.Clock.PublicationPhase.TryEnter(s.Engine),"coordinate test owner phase");
            try
            {
                SetField(Field(world,"assembly"),"Pending",true);
                for(var n=1;n<=1200;n++)
                {
                    SetField(world,"frontier",(long)n);
                    SetField(Field(world,"assembly"),"AcknowledgedFrontier",(long)n-1);
                    var next=s.Launch.Design.ObserveMass(705-.1953125*((long)n*1_000_000/60)/1_000_000d);
                    Check(world.RefreshAssemblyMass(s.Engine,mass,next)==LocalContactStatus.Success,"same-body property refresh");
                    var points=NativeMaterialPoints(world);
                    for(var k=0;k<points.Length;k++)
                    {
                        maxPosition=Math.Max(maxPosition,Vector3.Distance(points[k].Position,original[k].Position));
                        maxVelocity=Math.Max(maxVelocity,Vector3.Distance(points[k].Velocity,original[k].Velocity));
                    }
                    Check(world.RefreshAssemblyMass(s.Engine,next,next)==LocalContactStatus.InvalidSource,"duplicate refresh refused");
                    mass=next;
                }
            }
            finally{s.Clock.PublicationPhase.Exit();}
            Console.WriteLine($"POWERED_CONTACT_COORDINATES positionError={maxPosition:R} velocityError={maxVelocity:R}");
            Check(maxPosition<=.000061&&maxVelocity<=.000061,"1200 coordinate updates preserve 64 material positions and velocities within existing local tolerance");
            Check(body.Pose.Orientation==orientation&&world.AssemblyIdentityForTest.Body==identity.Body&&world.AssemblyIdentityForTest.Shape==identity.Shape&&world.Generation==identity.Generation,"orientation/body/shape/generation retained");
            Check(Observe(s).State==s.Launch.Initial,"coordinate test has no canonical authority");
        }
        using(var s=PoweredSession())
        {
            var world=s.Engine.AssemblyContactWorldForTest(s.Authority)!;
            var sim=NativeField<Simulation>(world,"simulation");var body=sim.Bodies[NativeField<BodyHandle>(world,"body")];
            static Double3 D(Vector3 v)=>new(v.X,v.Y,v.Z);
            var expectedP=D(body.Pose.Position);var expectedV=D(body.Velocity.Linear);var mass=s.Launch.Initial.Mass;
            var maxP=0d;var maxV=0d;
            Check(s.Clock.PublicationPhase.TryEnter(s.Engine),"interleaved reference test owner");
            try
            {
                SetField(Field(world,"assembly"),"Pending",true);
                for(var n=1;n<=1200;n++)
                {
                    // Arbitrary native solver-like increments, deliberately independent of
                    // canonical state. The oracle counts their actual FP32 result exactly once.
                    var oldP=D(body.Pose.Position);var oldV=D(body.Velocity.Linear);
                    body.Pose.Position+=new Vector3(.00007f,.00001f,-.00002f);
                    body.Velocity.Linear+=new Vector3(.000001f,-.000002f,.000003f);
                    body.Pose.Orientation=Quaternion.CreateFromYawPitchRoll(n*.0001f,n*.00013f,-n*.00007f);
                    body.Velocity.Angular=new(.02f,-.03f,.01f);
                    var fq=body.Pose.Orientation;var q=new DoubleQuaternion(fq.X,fq.Y,fq.Z,fq.W);
                    var next=s.Launch.Design.ObserveMass(705-.1953125*((long)n*1_000_000/60)/1_000_000d);
                    var delta=q.Rotate(new(0,next.Com.X-mass.Com.X,0));
                    expectedP+=D(body.Pose.Position)-oldP+delta;
                    expectedV+=D(body.Velocity.Linear)-oldV+Double3.Cross(D(body.Velocity.Angular),delta);
                    SetField(world,"frontier",(long)n);
                    SetField(Field(world,"assembly"),"AcknowledgedFrontier",(long)n-1);
                    Check(world.RefreshAssemblyMass(s.Engine,mass,next)==LocalContactStatus.Success,"interleaved refresh");
                    maxP=Math.Max(maxP,Math.Sqrt((D(body.Pose.Position)-expectedP).LengthSquared));
                    maxV=Math.Max(maxV,Math.Sqrt((D(body.Velocity.Linear)-expectedV).LengthSquared));
                    Check(body.Pose.Orientation==fq,"coordinate refresh does not rewrite native orientation");
                    Check(NativeField<Double3>(world,"assemblyPositionResidue").LengthSquared<=1e-12&&NativeField<Double3>(world,"assemblyVelocityResidue").LengthSquared<=1e-12,"roundoff-only bounded private residues");
                    mass=next;
                }
            }
            finally{s.Clock.PublicationPhase.Exit();}
            Console.WriteLine($"POWERED_CONTACT_INTERLEAVED positionError={maxP:R} velocityError={maxV:R}");
            Check(maxP<=.000001&&maxV<=.000001,"native motion preserved / only refresh rounding compensated");
        }
        using(var s=PoweredSession())
        {
            RejectCold(()=>AssemblyResources.Calculate(s.Launch.Design,s.Launch.Initial.Stores,s.Launch.Plan[0].ExtentRate,16666),"free-flight duration guard unchanged");
            var first=PoweredNext(s);var second=PoweredNext(s);
            Check(first.State.Epoch.Ticks==16666&&second.State.Epoch.Ticks==33333,"original integer lattice");
            var before=Observe(s);Credit(s,16667);before=Observe(s);
            Check(s.Engine.PrepareAssemblyContact(s.Authority,out var receipt)==AssemblyFlightStatus.Prepared,"retry prepared");
            var world=s.Engine.AssemblyContactWorldForTest(s.Authority)!;var points=NativeMaterialPoints(world);var identity=world.AssemblyIdentityForTest;
            Check(s.Engine.PublishAssemblyContact(s.Authority,receipt,refuseForTest:true).Status==AssemblyFlightStatus.PreparationRefused&&Observe(s)==before,"precommit refusal leaves canonical resources/mass/state unchanged");
            Check(s.Engine.PrepareAssemblyContact(s.Authority,out _)==AssemblyFlightStatus.OutstandingProposal,"pending blocks second solve");
            Check(s.Engine.PublishAssemblyContact(s.Authority,receipt).Status==AssemblyFlightStatus.Published,"retry publishes existing successor");
            Check(points.SequenceEqual(NativeMaterialPoints(world))&&world.AssemblyIdentityForTest.Frontier==identity.Frontier,"retry no solver or second coordinate update");
            Check(s.Engine.PublishAssemblyContact(s.Authority,receipt).Status==AssemblyFlightStatus.InvalidProposal,"duplicate no debit");
        }
        using(var s=PoweredSession())
        {
            Credit(s,16666);Check(s.Engine.PrepareAssemblyContact(s.Authority,out var receipt)==AssemblyFlightStatus.Prepared,"terminal prepare");
            Check(s.Engine.PublishAssemblyContact(s.Authority,receipt,failAcknowledgementForTest:true).Status==AssemblyFlightStatus.CanonicalCommittedPrivateInvalidated,"explicit committed/private invalidated");
            var after=Observe(s);Check(after.State.Frontier==1&&after.State.ResourceRevision==1&&after.HistoryCount==1&&after.State.Stores!=s.Launch.Initial.Stores&&after.PrivateInvalidated,"terminal preserves actual committed resource/mass/physical state");
        }
        PoweredClosure();
        Console.WriteLine("POWERED_CONTACT_CHEAP PASS");
    }
    internal static void PoweredPhysical()
    {
        using var s=PoweredSession();var world=s.Engine.AssemblyContactWorldForTest(s.Authority)!;var identity=world.AssemblyIdentityForTest;
        var peak=0d;var drift=0d;var reference=default(AssemblyMotion);var support=0;var ticks16666=0;var ticks16667=0;
        var tolerance=s.Launch.ContactProfile!.SmallestFeature/1000;
        var height=0d;var speed=0d;var angular=0d;var angle=0d;var thrustResidual=0d;
        var sim=NativeField<Simulation>(world,"simulation");var body=sim.Bodies[NativeField<BodyHandle>(world,"body")];
        var raw=new CompoundContactSelector.Candidate[32];var predictions=new double[32];var selected=new int[4];
        var features=new int[32];var previous=new int[32];var priorCount=0;var featureChanges=0;var minContacts=int.MaxValue;
        for(var i=0;i<1200;i++)
        {
            var before=Observe(s);var vy=body.Velocity.Linear.Y;
            var v=PoweredNext(s);var penetration=Penetration(s,v);peak=Math.Max(peak,penetration);
            thrustResidual=Math.Max(thrustResidual,ThrustResidual(s,world,before,vy));CheckAssemblyExport(s,world);
            Check(v.State.Epoch.Ticks==(long)(i+1)*1_000_000/60,"exact powered frontier");
            if(s.Launch.Plan[i].Request.Ticks==16666)ticks16666++;else ticks16667++;
            if(i==599)reference=v.State.Motion;
            if(i>=600)
            {
                var motion=v.State.Motion;var delta=motion.PositionO-reference.PositionO;
                drift=Math.Max(drift,Math.Sqrt(delta.X*delta.X+delta.Z*delta.Z));
                height=Math.Max(height,Math.Abs(penetration));if(Math.Abs(penetration)<=2*tolerance)support++;
                speed=Math.Max(speed,Math.Sqrt(motion.VelocityO.LengthSquared));angular=Math.Max(angular,Math.Sqrt(motion.AngularVelocityBody.LengthSquared));
                var dq=reference.BodyToWorld.Conjugate()*motion.BodyToWorld;
                angle=Math.Max(angle,2*Math.Atan2(Math.Sqrt(dq.X*dq.X+dq.Y*dq.Y+dq.Z*dq.Z),Math.Abs(dq.W)));
                var count=world.CopyCompoundRawForTest(raw,predictions,selected);minContacts=Math.Min(minContacts,count);
                for(var j=0;j<count;j++)features[j]=raw[j].Contact.FeatureId;
                Array.Sort(features,0,count);
                if(priorCount!=0&&(priorCount!=count||!features.AsSpan(0,count).SequenceEqual(previous.AsSpan(0,priorCount))))featureChanges++;
                features.AsSpan(0,count).CopyTo(previous);priorCount=count;
                Check(body.Constraints.Count==1&&count>0,"independent native support persistence");
            }
        }
        var final=Observe(s);
        Console.WriteLine($"POWERED_CONTACT_PHYSICAL peak={peak:R} final600Drift={drift:R} height={height:R} speed={speed:R} angular={angular:R} orientationRadians={angle:R} supported={support} minContacts={minContacts} featureChanges={featureChanges} thrustResidualNs={thrustResidual:R} fuel={28.4375:R} oxidizer={42.65625:R} mass={final.State.Mass.Mass:R}");
        Check(peak<=.020&&drift<=tolerance&&height<=2*tolerance&&support==600,"independent powered physical support bars");
        Check(speed<=tolerance/.016667&&angular*s.Launch.ContactProfile.BoundingRadius<=tolerance/.016667&&angle*s.Launch.ContactProfile.BoundingRadius<=tolerance,"settled speed and orientation surface-displacement bars");
        Check(final.State.Stores==new AssemblyStores(AssemblyResources.Mass(28.4375),AssemblyResources.Mass(42.65625))&&final.State.Mass.Mass==701.09375,"independent exact 20-second outcome");
        Check(final.StateRevision.Value==1200&&final.HistoryCount==1200&&final.Clock.Debt.Ticks==0&&ticks16666==400&&ticks16667==800,"exact revision/history/debt/lattice");
        Check(world.AssemblyIdentityForTest.Generation==identity.Generation&&world.AssemblyIdentityForTest.Body==identity.Body&&world.AssemblyIdentityForTest.Shape==identity.Shape,"same retained native ownership");
    }
    internal static void PoweredSchedules()
    {
        var reference=new AssemblyFlightRecord[1200];
        using(var s=PoweredSession())
        {
            Credit(s,20_000_000);
            for(var i=0;i<300;i++)Check(s.Engine.ServiceAssemblyContactDebt(s.Authority).PublishedCount==4,"powered reference budget");
            for(var i=0;i<1200;i++)Check(s.Engine.TryGetAssemblyHistory(s.Authority,i,out reference[i]),"powered reference history");
        }
        foreach(var fps in new[]{30,60,150,240,0})
        {
            using var s=PoweredSession();long elapsed=0;var frames=fps==0?40:20*fps;var count=0;
            for(var frame=1;frame<=frames;frame++)
            {
                var target=(long)frame*20_000_000/frames;Credit(s,target-elapsed);elapsed=target;
                var result=s.Engine.ServiceAssemblyContactDebt(s.Authority);count+=result.PublishedCount;
                Check(result.Status is AssemblyFlightStatus.AwaitingDebt or AssemblyFlightStatus.Completed or AssemblyFlightStatus.BudgetExhausted,"powered partition status");
                Check(result.PublishedCount<=4&&Observe(s).Clock.Debt.Ticks==elapsed-Observe(s).State.Epoch.Ticks,"bounded service / accounting conservation");
            }
            while(count<1200){var result=s.Engine.ServiceAssemblyContactDebt(s.Authority);Check(result.PublishedCount is >0 and <=4,"backlog progress");count+=result.PublishedCount;}
            for(var i=0;i<1200;i++)
            {
                Check(s.Engine.TryGetAssemblyHistory(s.Authority,i,out var actual),"powered partition history");
                Check(actual==reference[i]&&MotionBits(actual.Successor.Motion,reference[i].Successor.Motion),"every powered frontier bit and exact resource matches");
            }
            Console.WriteLine($"POWERED_CONTACT_SCHEDULE fps={fps} frontiers=1200 exactBits=PASS");
        }
        using(var s=PoweredSession(new(.1,0,-.05)))
        {
            Credit(s,20_000_000);for(var i=0;i<300;i++)Check(s.Engine.ServiceAssemblyContactDebt(s.Authority).PublishedCount==4,"moving powered reference");
            for(var i=0;i<1200;i++)
            {
                Check(s.Engine.TryGetAssemblyHistory(s.Authority,i,out var actual),"moving history");
                var a=actual.Successor.Motion;var b=reference[i].Successor.Motion;var dt=actual.Successor.Epoch.Ticks/1e6;
                Check((a.PositionO-b.PositionO-new Double3(.1,0,-.05)*dt).LengthSquared<1e-24&&
                    (a.VelocityO-b.VelocityO-new Double3(.1,0,-.05)).LengthSquared<1e-24&&
                    Bits(a.BodyToWorld,b.BodyToWorld)&&Bits(a.AngularVelocityBody,b.AngularVelocityBody),"original moving epoch / source identity and native trajectory preserved");
            }
            Console.WriteLine("POWERED_CONTACT_MOVING_FRAME PASS 1200 intervals");
        }
    }
}
