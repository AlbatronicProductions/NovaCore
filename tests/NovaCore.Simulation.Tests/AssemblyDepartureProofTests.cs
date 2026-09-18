using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;
using NovaCore.Simulation.Spacecraft.Contact.Staging;
using System.Text.Json;

internal static partial class AssemblyContactAdmissionTests
{
    internal static void DepartureProofs()
    {
        foreach(var speed in new[]{.2,.25})
        {
            using var s=DepartureSession(speed:speed);
            var world=s.Engine.AssemblyContactWorldForTest(s.Authority)!;
            var native=NativeField<BepuPhysics.Simulation>(world,"simulation");
            var body=native.Bodies[NativeField<BepuPhysics.BodyHandle>(world,"body")];
            Check(world.AssemblyIdentityForTest.Frontier==0&&body.Constraints.Count==0&&Observe(s).State==s.Launch.Initial,"cold workspace does not evolve body or canonical source");
            var source=Handoff(s);
            var tolerance=NativeField<LocalContactConfiguration>(world,"configuration").ContactTolerance;
            Check(s.Launch.Departure!.Clears(s.Launch,source.State,15625,tolerance,true,out var lower),"whole successor horizon clears");
            var m=source.State.Motion;
            Check(m.AngularVelocityBody.LengthSquared==0&&Math.Abs(m.BodyToWorld.Rotate(Double3.UnitX).Y-1)<1e-15,"axial oracle is zero-angular/coaxial");
            Credit(s,15625);Check(s.Engine.ServiceAssemblyDepartureDebt(s.Authority).PublishedCount==1,"oracle successor");
            var after=Observe(s);
            // Independent integration of a(t)=F/(m0-k*t)+g. No production RK stages.
            // Expanding 1/(1-x), integrating twice gives the denominators below.
            const double h=1d/64, k=25d/128, force=600, gravity=-9.81;
            var m0=source.State.Mass.Mass;var x=k*h/m0;
            double vp=0,pp=0,power=1;
            for(var n=0;n<=3;n++){vp+=power/(n+1);pp+=power/((n+1)*(n+2));power*=x;}
            var dv=force*h/m0*vp;var dp=force*h*h/m0*pp;
            var velocityTail=force*h/m0*power/(5*(1-x));
            var positionTail=force*h*h/m0*power/(30*(1-x));
            var initialPosition=m.PositionO.Y;var initialVelocity=m.VelocityO.Y*h;var gravityPosition=.5*gravity*h*h;
            var expectedPosition=initialPosition+initialVelocity+gravityPosition+dp;
            var expectedVelocity=m.VelocityO.Y+gravity*h+dv;
            var positionError=Math.Abs(after.State.Motion.PositionO.Y-expectedPosition);
            var velocityError=Math.Abs(after.State.Motion.VelocityO.Y-expectedVelocity);
            // Axial RK4 quadrature agrees through x^2 (position) / x^3 (velocity).
            // A conservative 1024-operation FP64 budget covers the scalar axial
            // stages, projection and quaternion arithmetic; no angular conditioning
            // enters this exactly zero-angular fixture. Include mass/axis rounding.
            const double u=1.1102230246251565e-16;
            var gamma=1024*u/(1-1024*u);
            var rkPosition=force*h*h/m0*11*x*x*x/(120*(1-x));
            var rkVelocity=force*Math.Pow(k,4)*Math.Pow(h,5)/(120*Math.Pow(m0-k*h,5));
            var positionBound=positionTail+rkPosition+gamma*(Math.Abs(initialPosition)+Math.Abs(initialVelocity)+Math.Abs(gravityPosition)+dp)+136*u*dp;
            var velocityBound=velocityTail+rkVelocity+gamma*(Math.Abs(m.VelocityO.Y)+Math.Abs(gravity*h)+dv)+136*u*dv;
            Check(positionTail<1e-25&&velocityTail<1e-22,"explicit positive geometric-series tail bound");
            Check(positionError<=positionBound&&velocityError<=velocityBound&&positionBound<tolerance/1000,"independent bounded position and velocity, far inside clearance reserve");
            Check(after.State.Mass.Mass==m0-k*h,"independent mass decrement");
            Console.WriteLine("DEPARTURE_POSITION "+JsonSerializer.Serialize(new{speed,initialPosition,initialVelocity,gravityPosition,thrustPosition=dp,positionTail,velocityTail,rkPosition,rkVelocity,positionBound,velocityBound,positionError,velocityError,clearanceMarginRatio=2*tolerance/positionBound,clearanceLower=lower,tolerance,gravityRoot=AssemblyDepartureProfile.Gravity,lastContact=source.State,firstFlight=after.State}));

            // Rotate toward the most vulnerable bottom corner, not an arbitrary axis.
            var min=double.MaxValue;var arm=Double3.Zero;var radius2=0d;
            foreach(var child in s.Launch.ContactProfile!.Children)for(var i=0;i<8;i++)
            {
                var r=m.BodyToWorld.Rotate(AssemblyContactProfile.CornerAtOrigin(child,i));
                var height=m.PositionO.Y+r.Y-AssemblyContactProfile.SupportPlaneAtOrigin;
                var horizontal=r.X*r.X+r.Z*r.Z;
                if(height<min-1e-12||(Math.Abs(height-min)<1e-12&&horizontal>radius2)){min=height;arm=r;radius2=horizontal;}
            }
            var axis=Double3.Cross(arm,-Double3.UnitY);axis/=Math.Sqrt(axis.LengthSquared);
            Check(Double3.Cross(axis,arm).Y<0,"angular fixture lowers vulnerable corner");
            foreach(var magnitude in new[]{.00005,.000099999,.000100001})
            {
                var omega=m.BodyToWorld.Conjugate().Rotate(axis*magnitude);
                var trial=source.State with{Motion=m with{AngularVelocityBody=omega}};
                var clear=s.Launch.Departure.Clears(s.Launch,trial,15625,tolerance,true,out var bound);
                Check(clear==(magnitude<.0001),"angular admission boundary");
                Console.WriteLine($"DEPARTURE_ANGULAR speed={speed:R} omega={magnitude:R} cornerVerticalSpeed={Double3.Cross(axis*magnitude,arm).Y:R} clear={clear} lower={bound:R}");
            }
            var nearPlane=source.State with{Motion=m with{PositionO=m.PositionO-new Double3(0,min-2*tolerance,0),VelocityO=default}};
            Check(!s.Launch.Departure.Clears(s.Launch,nearPlane,15625,tolerance,false,out _),"uncertifiable near-plane interval refuses");
            var stopped=source.State with{Motion=m with{VelocityO=default}};
            s.Launch.Departure.Clears(s.Launch,stopped,15625,tolerance,false,out var zeroBound);
            var reserve=Math.Sqrt(arm.LengthSquared)*.000099999*h;
            stopped=stopped with{Motion=stopped.Motion with{PositionO=stopped.Motion.PositionO+new Double3(0,reserve-zeroBound,0)}};
            Check(s.Launch.Departure.Clears(s.Launch,stopped,15625,tolerance,false,out var unrotatedBound),"small positive geometric reserve without rotation");
            var rotating=stopped with{Motion=stopped.Motion with{AngularVelocityBody=m.BodyToWorld.Conjugate().Rotate(axis*.000099999)}};
            Check(!s.Launch.Departure.Clears(s.Launch,rotating,15625,tolerance,false,out var rotatingBound),"downward corner motion defeats insufficient geometric reserve");
            Console.WriteLine($"DEPARTURE_ANGULAR_COVERAGE unrotated={unrotatedBound:R} rotating={rotatingBound:R}");
        }
        using(var stationary=DepartureSession(speed:.2))using(var moving=DepartureSession(new(.05,0,0),.2))
        {
            var a=Handoff(stationary);var b=Handoff(moving);
            var tolerance=NativeField<LocalContactConfiguration>(stationary.Engine.AssemblyContactWorldForTest(stationary.Authority)!,"configuration").ContactTolerance;
            var clearA=stationary.Launch.Departure!.Clears(stationary.Launch,a.State,15625,tolerance,true,out var ca);
            var clearB=moving.Launch.Departure!.Clears(moving.Launch,b.State,15625,tolerance,true,out var cb);
            Check(clearA&&clearB&&Math.Abs(ca-cb)<1e-14,"lower-speed moving frame clearance invariant");
            Credit(stationary,15625);Credit(moving,15625);
            Check(stationary.Engine.ServiceAssemblyDepartureDebt(stationary.Authority).PublishedCount==1&&moving.Engine.ServiceAssemblyDepartureDebt(moving.Authority).PublishedCount==1,"moving lower-speed successor");
            var sa=Observe(stationary);var sb=Observe(moving);var elapsed=sb.State.Epoch.Ticks/1e6;
            var pError=Math.Sqrt((sb.State.Motion.PositionO-sa.State.Motion.PositionO-new Double3(.05*elapsed,0,0)).LengthSquared);
            var vError=Math.Sqrt((sb.State.Motion.VelocityO-sa.State.Motion.VelocityO-new Double3(.05,0,0)).LengthSquared);
            Check(pError<1e-13&&vError<1e-14&&sa.State.Stores==sb.State.Stores&&sa.State.Mass==sb.State.Mass,"same material origin and stores in translating frame");
            Console.WriteLine($"DEPARTURE_FRAME speed=0.2 frameX=0.05 clearanceError={Math.Abs(ca-cb):R} positionError={pError:R} velocityError={vError:R}");
        }
        using(var s=DepartureSession())
        {
            Credit(s,16666);var before=Observe(s);var w=s.Engine.AssemblyContactWorldForTest(s.Authority)!;
            Check(s.Engine.PrepareAssemblyContact(s.Authority,out var proposal)==AssemblyFlightStatus.Prepared,"native abort prepared");
            Check(s.Engine.AbortAssemblyContact(s.Authority,proposal)==AssemblyFlightStatus.Invalidated,"native abort poisons without rollback");
            var after=Observe(s);
            Check(after.State==before.State&&after.Clock==before.Clock&&after.HistoryCount==0&&after.PrivateInvalidated&&w.AssemblyIdentityForTest.Frontier==1,"abort preserves actual native advance/canonical nonmutation");
            Check(s.Engine.ServiceAssemblyDepartureDebt(s.Authority).Status==AssemblyFlightStatus.Invalidated&&Observe(s)==after,"aborted native endpoint cannot repeat");
        }
        Console.WriteLine("DEPARTURE_PROOFS PASS independent position / angular limits / moving frame / native abort");
    }
}
