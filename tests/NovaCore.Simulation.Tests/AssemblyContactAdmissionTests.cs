using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Transactions;
using NovaCore.Simulation.Spacecraft.Contact.Staging;

internal static partial class AssemblyContactAdmissionTests
{
    private static T NativeField<T>(LocalContactWorld world,string name) =>
        (T)typeof(LocalContactWorld).GetField(name,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(world)!;

    private static void NativeBasis()
    {
        using var s=Session();var world=s.Engine.AssemblyContactWorldForTest(s.Authority)!;
        var sim=NativeField<BepuPhysics.Simulation>(world,"simulation");
        var body=sim.Bodies[NativeField<BepuPhysics.BodyHandle>(world,"body")];
        var shape=NativeField<BepuPhysics.Collidables.TypedIndex>(world,"bodyShape");
        var profile=s.Launch.ContactProfile!;var canonical=s.Launch.Initial.Motion;
        Check(body.Pose.Orientation==System.Numerics.Quaternion.Identity,"fixed body basis starts at identity, not a rounded quarter turn");
        ref var compound=ref sim.Shapes.GetShape<BepuPhysics.Collidables.Compound>(shape.Index);
        for(var i=0;i<compound.Children.Length;i++)
        {
            ref var child=ref compound.Children[i];ref var box=ref sim.Shapes.GetShape<BepuPhysics.Collidables.Box>(child.ShapeIndex.Index);
            for(var k=0;k<8;k++)
            {
                var point=new System.Numerics.Vector3(((k&1)==0?-.5f:.5f)*box.Width,((k&2)==0?-.5f:.5f)*box.Height,((k&4)==0?-.5f:.5f)*box.Length);
                var native=body.Pose.Position+System.Numerics.Vector3.Transform(child.LocalPosition+System.Numerics.Vector3.Transform(point,child.LocalOrientation),body.Pose.Orientation);
                var authored=AssemblyContactProfile.CornerAtOrigin(profile.Children[i],k);
                // Independent exact upright signed permutation plus slab-origin translation.
                var expected=new Double3(-authored.Y,authored.X+1.7,authored.Z);
                Check((new Double3(native.X,native.Y,native.Z)-expected).LengthSquared<1e-11,"native world corner preserves authored geometry");
            }
        }
        var tensor=body.LocalInertia.InverseInertiaTensor;var inverse=profile.Mass.Inertia.Inverse();
        foreach(var torque in new[]{new Double3(1,2,3),new Double3(-3,5,7)})
        {
            var expected=canonical.BodyToWorld.Rotate(inverse.Apply(canonical.BodyToWorld.Conjugate().Rotate(torque)));
            var actual=new Double3(tensor.XX*torque.X+tensor.YX*torque.Y+tensor.ZX*torque.Z,
                tensor.YX*torque.X+tensor.YY*torque.Y+tensor.ZY*torque.Z,tensor.ZX*torque.X+tensor.ZY*torque.Y+tensor.ZZ*torque.Z);
            Check((actual-expected).LengthSquared<1e-16,"native inverse inertia preserves world torque response");
        }
        Check(Observe(s).State==s.Launch.Initial&&body.Velocity.Linear==System.Numerics.Vector3.Zero&&body.Velocity.Angular==System.Numerics.Vector3.Zero,"basis preparation no canonical or velocity mutation");
        Console.WriteLine("ASSEMBLY_CONTACT_NATIVE_BASIS PASS 64 world corners and independent inertia response");
    }
    private static void Check(bool condition, string label)
    { if (!condition) throw new InvalidOperationException("ASSEMBLY CONTACT: " + label); }

    internal static void Geometry()
    {
        var design = AssemblyStockCatalog.LoadDefault().Resolve("novacore.stock.SRV01.FourHorn");
        var profile = AssemblyContactProfile.Create(design);
        Check(profile.Children.Length == 8 && design.Parts.Length == 7, "eight physical children / seven unchanged parts");
        Check(profile.Mass.Mass == 705 && Math.Abs(profile.Mass.Com.X - 734.4 / 705) < 1e-14, "independent analytical wet mass/COM");
        var touching = 0;
        // Independent scalar geometry: upright maps assembly X to world Y.
        // No call to the production corner or quaternion conversion helper.
        foreach (var child in profile.Children)
        {
            for (var k = 0; k < 8; k++)
            {
                var x = ((k & 1) == 0 ? -.5 : .5) * child.Dimensions.X;
                var y = ((k & 2) == 0 ? -.5 : .5) * child.Dimensions.Y;
                var z = ((k & 4) == 0 ? -.5 : .5) * child.Dimensions.Z;
                var p = child.AtOrigin; var r = p.Rotation;
                var height = p.Position.X + r.A*x + r.B*y + r.C*z + 1.7;
                Check(height >= -1e-14, "no initial physical intersection");
                if (Math.Abs(height) < 1e-14)
                {
                    Check(child.Part == design.Main.Instance.Id && child.Dimensions == new Double3(.639,.52,.52), "only declared aft bell envelope supports");
                    Check(Math.Abs(y) == .26 && Math.Abs(z) == .26, "independent support footprint");
                    touching++;
                }
            }
        }
        Check(touching == 4 && profile.Mass.Com.Y == 0 && profile.Mass.Com.Z == 0, "COM projection strictly inside four-corner support");
        Console.WriteLine($"ASSEMBLY_CONTACT_GEOMETRY PASS parts=7 children=8 supportCorners={touching} mass={profile.Mass.Mass:R} com={profile.Mass.Com} minFeature={profile.SmallestFeature:R} digest={profile.Digest}");
    }

    private static AssemblyApplicationSession Session(Double3 moving=default,int capacity=1200)
    {
        var profile=AssemblyContactProfile.Create(AssemblyStockCatalog.LoadDefault().Resolve("novacore.stock.SRV01.FourHorn"));
        return AssemblyApplicationSession.CreateSupported(AssemblyLaunch.CreateSupported(profile,new(new(201),new(1),new(2),"SRV-01 supported"),"reference-launch-01",moving),capacity);
    }
    private static AssemblyFlightObservation Observe(AssemblyApplicationSession s)
    {
        var status=s.Engine.ObserveAssemblyFlight(s.Authority,out var value);
        Check(status is AssemblyFlightStatus.Ready or AssemblyFlightStatus.Invalidated,"copied observation");return value;
    }
    private static void Credit(AssemblyApplicationSession s,long ticks)
    {
        var before=Observe(s);
        Check(s.Engine.AdmitAssemblyHostTime(s.Authority,before.HostSequence+1,new(ticks)).Status==AssemblyFlightStatus.AcceptedCredit,"owner host credit");
        var after=Observe(s);
        Check(after.State==before.State&&after.StateRevision==before.StateRevision&&after.HistoryCount==before.HistoryCount&&
            after.Clock.Debt.Ticks==before.Clock.Debt.Ticks+ticks,"credit changes accounting only");
    }
    private static AssemblyFlightObservation Next(AssemblyApplicationSession s)
    {
        var before=Observe(s);Credit(s,s.Launch.Plan[before.State.Frontier].Request.Ticks);
        Check(s.Engine.PrepareAssemblyContact(s.Authority,out var receipt)==AssemblyFlightStatus.Prepared,"private step/preparation");
        Check(Observe(s).State==before.State,"native step leaves canonical state unchanged");
        Check(s.Engine.PublishAssemblyContact(s.Authority,receipt).Status==AssemblyFlightStatus.Published,"typed canonical commit/ack");
        var result=Observe(s);
        Check(result.State.Stores==s.Launch.Initial.Stores&&result.State.Mass==s.Launch.Initial.Mass&&result.State.ResourceRevision==0,"exact resource and mass carry");
        Check(result.State.Actual==new AssemblyRealization(false,0,AssemblyFeedState.NoDemand),"no propulsion");
        Check(result.StateRevision.Value==before.StateRevision.Value+1&&result.HistoryCount==before.HistoryCount+1&&result.TimelineRevision==before.TimelineRevision,"one revision/history, unchanged timeline");
        Check(result.Clock.Debt.Ticks==before.Clock.Debt.Ticks,"exact admitted interval subtraction");
        Check(s.Engine.TryGetAssemblyHistory(s.Authority,result.State.Frontier-1,out var record)&&record.ContactProfile==s.Launch.ContactProfile!.Digest&&record.Successor==result.State,"typed provenance history");
        return result;
    }
    private static double Penetration(AssemblyApplicationSession s,AssemblyFlightObservation v)
    {
        var minimum=double.MaxValue;var q=v.State.Motion.BodyToWorld;var p=v.State.Motion.PositionO;
        // Explicit independent rotation matrix from the copied canonical quaternion.
        var ry0=2*(q.X*q.Y+q.Z*q.W);var ry1=1-2*(q.X*q.X+q.Z*q.Z);var ry2=2*(q.Y*q.Z-q.X*q.W);
        foreach(var child in s.Launch.ContactProfile!.Children)
        for(var k=0;k<8;k++)
        {
            var x=((k&1)==0?-.5:.5)*child.Dimensions.X;var y=((k&2)==0?-.5:.5)*child.Dimensions.Y;var z=((k&4)==0?-.5:.5)*child.Dimensions.Z;
            var m=child.AtOrigin.Rotation;var o=child.AtOrigin.Position;
            var ax=o.X+m.A*x+m.B*y+m.C*z;var ay=o.Y+m.D*x+m.E*y+m.F*z;var az=o.Z+m.G*x+m.H*y+m.I*z;
            var frameY=s.Launch.Initial.Motion.VelocityO.Y*(v.State.Epoch.Ticks-s.Launch.Initial.Epoch.Ticks)/1e6;
            minimum=Math.Min(minimum,p.Y+ry0*ax+ry1*ay+ry2*az+1.7-frameY);
        }
        return -minimum;
    }
    internal static void Cheap()
    {
        Geometry();
        NativeBasis();
        var c=new Double3(1.2,.3,-.4);var q=DoubleQuaternion.FromAxisAngle(Double3.UnitZ,Math.PI/2);
        var motion=new AssemblyMotion(new(4,5,6),new(.1,.2,.3),q,new(0,0,.02));
        var com=AssemblyContactProfile.ToCom(motion,c);
        Check((com.Position-new Double3(3.7,6.2,5.6)).LengthSquared<1e-28,"independent O-to-C position");
        Check((com.Velocity-new Double3(.076,.194,.3)).LengthSquared<1e-28,"independent O-to-C rotational velocity offset");
        var origin=AssemblyContactProfile.ToOrigin(new(3.7,6.2,5.6),new(.076,.194,.3),q,new(0,0,.02),c);
        Check((origin.PositionO-motion.PositionO).LengthSquared<1e-28&&(origin.VelocityO-motion.VelocityO).LengthSquared<1e-28,"independent C-to-O export");
        using var s=Session();var initial=Observe(s);var world=s.Engine.AssemblyContactWorldForTest(s.Authority)!;var identity=world.AssemblyIdentityForTest;
        Check(initial.Clock.Debt.Ticks==0&&identity.Frontier==0,"READY has zero debt/no hidden settle");
        Credit(s,16665);
        Check(s.Engine.ServiceAssemblyContactDebt(s.Authority).Status==AssemblyFlightStatus.AwaitingDebt&&world.AssemblyIdentityForTest==identity,"insufficient credit no native step");
        Credit(s,1);
        Check(s.Engine.ServiceAssemblyContactDebt(s.Authority).PublishedCount==1,"exact first boundary");
        var first=Observe(s);Check(first.State.Epoch.Ticks==16666,"first exact tick");
        var second=Next(s);var third=Next(s);
        Check(second.State.Epoch.Ticks==33333&&third.State.Epoch.Ticks==50000,"original lattice continued");
        double peak=Math.Max(Penetration(s,first),Math.Max(Penetration(s,second),Penetration(s,third)));
        for(var i=3;i<120;i++)peak=Math.Max(peak,Penetration(s,Next(s)));
        var final=Observe(s);var current=world.AssemblyIdentityForTest;
        Console.WriteLine($"ASSEMBLY_CONTACT_CHEAP intervals=120 peakPenetration={peak:R} finalPenetration={Penetration(s,final):R} position={final.State.Motion.PositionO} velocity={final.State.Motion.VelocityO} omega={final.State.Motion.AngularVelocityBody}");
        Check(peak<=.020,"unchanged 20 mm penetration ceiling");
        Check(current.Generation==identity.Generation&&current.Body==identity.Body&&current.Shape==identity.Shape&&!current.Pending,"retained same world/body/shape acknowledged");
        Check(final.Clock.Time.Ticks==2_000_000&&final.Clock.Debt.Ticks==0,"120-step exact clock/debt");
        try{s.Save();throw new Exception("Contact save was accepted");}catch(InvalidDataException){ }
        Check(s.Engine.PrepareAssemblyFlight(s.Authority,out _)==AssemblyFlightStatus.InvalidAuthority&&s.Engine.ServiceAssemblyFlightDebt(s.Authority).Status==AssemblyFlightStatus.InvalidAuthority,"free-flight consumer rejected");
        Console.WriteLine("ASSEMBLY_CONTACT_CHEAP PASS");
    }

    internal static void Authority(bool powered=false)
    {
        AssemblyApplicationSession Session(Double3 moving=default,int capacity=1200)=>powered?PoweredSession(moving,capacity):AssemblyContactAdmissionTests.Session(moving,capacity);
        AssemblyFlightObservation Next(AssemblyApplicationSession value)=>powered?PoweredNext(value):AssemblyContactAdmissionTests.Next(value);
        using var s=Session();using var foreign=Session();var e=s.Engine;var a=s.Authority;var world=e.AssemblyContactWorldForTest(a)!;
        var before=Observe(s);var native=world.AssemblyIdentityForTest;
        Check(e.AdmitAssemblyHostTime(a,1,new(-1)).Status==AssemblyFlightStatus.InvalidInput,"negative credit");
        Check(e.AdmitAssemblyHostTime(a,1,new(0)).Status==AssemblyFlightStatus.NoWork,"zero credit");
        Check(e.AdmitAssemblyHostTime(a,2,new(1)).Status==AssemblyFlightStatus.InvalidSequence,"future credit sequence");
        Check(e.PrepareAssemblyContact(foreign.Authority,out _)==AssemblyFlightStatus.InvalidAuthority,"foreign engine authority");
        Check(e.PublishAssemblyContact(a,default).Status==AssemblyFlightStatus.InvalidProposal,"default proposal");
        Check(e.PublishAssemblyContact(a,new(1)).Status==AssemblyFlightStatus.InvalidProposal,"fabricated proposal");
        Check(Observe(s)==before&&world.AssemblyIdentityForTest==native,"input refusals canonical/native nonmutation");
        Check(Task.Run(()=>e.PrepareAssemblyContact(a,out _)).GetAwaiter().GetResult()==AssemblyFlightStatus.WrongOwnerThread,"wrong thread");
        Check(s.Clock.PublicationPhase.TryEnter(e),"owner test entry");
        try{Check(e.PrepareAssemblyContact(a,out _)==AssemblyFlightStatus.Reentrant&&e.AdmitAssemblyHostTime(a,1,new(1)).Status==AssemblyFlightStatus.Reentrant,"reentrancy");}
        finally{s.Clock.PublicationPhase.Exit();}
        Credit(s,16666);before=Observe(s);
        Check(e.AdmitAssemblyHostTime(a,1,new(1)).Status==AssemblyFlightStatus.InvalidSequence,"duplicate credit");
        Check(e.PrepareAssemblyContact(a,out _,true)==AssemblyFlightStatus.PreparationRefused&&Observe(s)==before,"preparation refusal");
        Check(e.PrepareAssemblyContact(a,out var receipt)==AssemblyFlightStatus.Prepared,"native preparation");native=world.AssemblyIdentityForTest;
        Check(e.PrepareAssemblyContact(a,out _)==AssemblyFlightStatus.OutstandingProposal,"one outstanding");
        Check(e.AdmitAssemblyHostTime(a,2,new(1)).Status==AssemblyFlightStatus.OutstandingProposal,"pending credit refused");
        Check(e.PublishAssemblyContact(a,receipt,refuseForTest:true).Status==AssemblyFlightStatus.PreparationRefused,"retryable publication refusal");
        Check(Observe(s)==before&&world.AssemblyIdentityForTest==native,"refusal retains genuine pending endpoint");
        Check(e.PublishAssemblyContact(a,receipt).Status==AssemblyFlightStatus.Published,"retry without restep");before=Observe(s);
        Check(e.PublishAssemblyContact(a,receipt).Status==AssemblyFlightStatus.InvalidProposal&&Observe(s)==before,"consumed/duplicate receipt");
        Check(world.AssemblyIdentityForTest.Frontier==native.Frontier&&!world.AssemblyIdentityForTest.Pending,"same native endpoint acknowledged");

        foreach(var kind in new[]{"debt","clock","pause","rate","event-before","event-at"})
        {
            using var f=Session();Credit(f,16666);
            switch(kind)
            {
                case "debt": f.Clock.AdvanceByHostDuration(new(1));break;
                case "clock": f.Clock.AdvanceTo(new(1));break;
                case "pause": f.Clock.Pause();break;
                case "rate": f.Clock.TrySetRate(new(2,1));break;
                default: f.Clock.Timeline.Schedule(f.Clock.CurrentTime,new(new(1),new(kind=="event-at"?16666:1),0,NovaCore.Simulation.Timeline.SimulationEventKind.Marker));break;
            }
            var snapshot=Observe(f);var w=f.Engine.AssemblyContactWorldForTest(f.Authority)!;var wi=w.AssemblyIdentityForTest;
            Check(f.Engine.PrepareAssemblyContact(f.Authority,out _)==AssemblyFlightStatus.StaleSource,"external "+kind);
            Check(Observe(f)==snapshot&&w.AssemblyIdentityForTest==wi,"external refusal nonmutation "+kind);
        }
        using(var f=Session())
        {
            Credit(f,16666);Check(f.Engine.PrepareAssemblyContact(f.Authority,out var p)==AssemblyFlightStatus.Prepared,"terminal source");
            Check(f.Engine.PublishAssemblyContact(f.Authority,p,true).Status==AssemblyFlightStatus.CanonicalCommittedPrivateInvalidated,"physical commit / ack failure");
            var o=Observe(f);Check(o.State.Frontier==1&&o.HistoryCount==1&&o.StateRevision.Value==1&&o.Clock.Debt.Ticks==0&&o.PrivateInvalidated,"physical commit stands");
            Check(f.Engine.PublishAssemblyContact(f.Authority,p).Status==AssemblyFlightStatus.Invalidated,"terminal cannot retry");
        }
        using(var f=Session())
        {
            Check(f.Engine.AdmitAssemblyHostTime(f.Authority,1,new(16666),true).Status==AssemblyFlightStatus.CanonicalCommittedPrivateInvalidated,"accounting commit / ack failure");
            var o=Observe(f);Check(o.Clock.Debt.Ticks==16666&&o.HostSequence==1&&o.State.Frontier==0&&o.HistoryCount==0&&o.PrivateInvalidated,"credit commit stands");
        }
        using(var f=Session())
        {
            Credit(f,16666);Check(f.Engine.PrepareAssemblyContact(f.Authority,out var p)==AssemblyFlightStatus.Prepared,"abort source");var snapshot=Observe(f);
            Check(f.Engine.AbortAssemblyFlight(f.Authority,p)==AssemblyFlightStatus.InvalidAuthority,"wrong-consumer abort refuses");
            Check(f.Engine.AbortAssemblyContact(f.Authority,p)==AssemblyFlightStatus.Invalidated,"advanced native abort is terminal");
            Check(Observe(f).State==snapshot.State&&Observe(f).Clock==snapshot.Clock,"abort canonical nonmutation");
        }
        using(var f=Session(capacity:1))
        {
            Next(f);Credit(f,16667);var snapshot=Observe(f);
            Check(f.Engine.PrepareAssemblyContact(f.Authority,out _)==AssemblyFlightStatus.HistoryCapacity&&Observe(f)==snapshot,"capacity checked before solve");
        }
        using(var f=Session())
        {
            for(var i=1;i<=4801;i++)Check(f.Engine.AdmitAssemblyHostTime(f.Authority,i,new(1)).Status==AssemblyFlightStatus.AcceptedCredit,"bounded contact credits");
            var snapshot=Observe(f);Check(f.Engine.AdmitAssemblyHostTime(f.Authority,4802,new(1)).Status==AssemblyFlightStatus.HistoryCapacity&&Observe(f)==snapshot,"4802 refuses before mutation");
        }
        Console.WriteLine("ASSEMBLY_CONTACT_AUTHORITY PASS input/owner/consumer/pending/retry/terminal/capacity/external-clock-event");
    }

    internal static void Physical()
    {
        using var s=Session();var tolerance=s.Launch.ContactProfile!.SmallestFeature/1000;
        var peak=0d;var maxSpeed=0d;var maxAngular=0d;var maxHeight=0d;var drift=0d;var supported=0;AssemblyFlightObservation reference=default;
        var shortIntervals=0;var longIntervals=0;
        Double3 driftVector=default;var driftInterval=0;var poseDrift=0d;var orientationDrift=0d;
        var world=s.Engine.AssemblyContactWorldForTest(s.Authority)!;var identity=world.AssemblyIdentityForTest;
        var sim=NativeField<BepuPhysics.Simulation>(world,"simulation");var handle=NativeField<BepuPhysics.BodyHandle>(world,"body");
        var raw=new CompoundContactSelector.Candidate[32];var predictions=new double[32];var selected=new int[4];
        var featureSet=new int[32];var previousSet=new int[32];var previousCount=0;var featureSetChanges=0;var minContacts=int.MaxValue;
        for(var n=1;n<=1200;n++)
        {
            var v=Next(s);var penetration=Penetration(s,v);peak=Math.Max(peak,penetration);
            Check(v.State.Epoch.Ticks==(long)n*1_000_000/60,"T0 lattice");
            if(s.Launch.Plan[n-1].Request.Ticks==16666)shortIntervals++;else longIntervals++;
            if(n==600)reference=v;
            if(n>600)
            {
                maxHeight=Math.Max(maxHeight,Math.Abs(penetration));
                var delta=v.State.Motion.PositionO-reference.State.Motion.PositionO;
                var horizontal=Math.Sqrt(delta.X*delta.X+delta.Z*delta.Z);
                if(horizontal>drift){drift=horizontal;driftVector=delta;driftInterval=n;}
                poseDrift=Math.Max(poseDrift,Math.Sqrt(delta.LengthSquared));
                var dq=reference.State.Motion.BodyToWorld.Conjugate()*v.State.Motion.BodyToWorld;
                orientationDrift=Math.Max(orientationDrift,2*Math.Atan2(Math.Sqrt(dq.X*dq.X+dq.Y*dq.Y+dq.Z*dq.Z),Math.Abs(dq.W)));
                maxSpeed=Math.Max(maxSpeed,Math.Sqrt(v.State.Motion.VelocityO.LengthSquared));
                maxAngular=Math.Max(maxAngular,Math.Sqrt(v.State.Motion.AngularVelocityBody.LengthSquared));
                if(Math.Abs(penetration)<=2*tolerance)supported++;
                var count=world.CopyCompoundRawForTest(raw,predictions,selected);minContacts=Math.Min(minContacts,count);
                for(var i=0;i<count;i++)featureSet[i]=raw[i].Contact.FeatureId;
                Array.Sort(featureSet,0,count);
                if(previousCount!=0&&(count!=previousCount||!featureSet.AsSpan(0,count).SequenceEqual(previousSet.AsSpan(0,previousCount))))featureSetChanges++;
                featureSet.AsSpan(0,count).CopyTo(previousSet);previousCount=count;
                Check(sim.Bodies[handle].Constraints.Count>0,"retained solver constraint in support window");
            }
        }
        var final=Observe(s);
        Console.WriteLine($"ASSEMBLY_CONTACT_PHYSICAL peak={peak:R} finalHeightMax={maxHeight:R} final600Drift={drift:R} maxSpeed={maxSpeed:R} maxAngular={maxAngular:R} support={supported}/600 tolerance={tolerance:R} ticks={final.State.Epoch.Ticks} intervals16666={shortIntervals} intervals16667={longIntervals}");
        Console.WriteLine($"ASSEMBLY_CONTACT_DRIFT interval={driftInterval} X={driftVector.X:R} Z={driftVector.Z:R} verticalY={driftVector.Y:R} poseDrift={poseDrift:R} orientationDriftRadians={orientationDrift:R} finalPosition={final.State.Motion.PositionO} finalVelocity={final.State.Motion.VelocityO} finalAngular={final.State.Motion.AngularVelocityBody} minContacts={minContacts} sortedFeatureSetChanges={featureSetChanges}");
        Check(peak<=.020,"20mm peak penetration");
        Check(maxHeight<=2*tolerance&&supported==600,"final600 independent support height");
        Check(drift<=tolerance,"final600 drift <= one feature-derived tolerance");
        Check(maxSpeed<=tolerance/.016667&&maxAngular*s.Launch.ContactProfile.BoundingRadius<=tolerance/.016667,"settled linear/angular surface speed");
        Check(shortIntervals==400&&longIntervals==800&&final.StateRevision.Value==1200&&final.HistoryCount==1200&&final.Clock.Debt.Ticks==0,"exact long accounting");
        var finalIdentity=world.AssemblyIdentityForTest;
        Check(finalIdentity.Generation==identity.Generation&&finalIdentity.Body==identity.Body&&finalIdentity.Shape==identity.Shape&&!finalIdentity.Pending,"retained world/body/shape across long witness");
        Console.WriteLine("ASSEMBLY_CONTACT_PHYSICAL PASS");
    }
}
