using System.Numerics;
using BepuPhysics;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints.Contact;
using BepuUtilities.Memory;
using NovaCore.Simulation.Spacecraft.Contact.Staging;
using Row=NovaCore.Simulation.Spacecraft.Contact.Staging.CraftContactDescription<BepuPhysics.Constraints.Contact.Contact1OneBodyPrestepData,BepuPhysics.Constraints.Contact.Contact1AccumulatedImpulses,BepuPhysics.Constraints.Contact.Contact1OneBodyFunctions>;

internal static class SurfaceContactResponseTests
{
    internal static void Run()
    {
        var checks=0;
        void Need(bool value,string reason){checks++;if(!value)throw new InvalidDataException(reason);}
        var material=CraftSurfaceImpact.Material;
        Need(material.FrictionCoefficient==.95f&&material.MaximumRecoveryVelocity==1.5f&&material.SpringSettings.DampingRatio==1,"installed KSA ordinary response");
        Need(CraftContactManifolds.Material.FrictionCoefficient==.5f&&CraftContactManifolds.Material.MaximumRecoveryVelocity==2,"qualified static support unchanged");
        Need(!CraftTerrainContacts.AcceptsRecovery(Vector3.UnitX,Vector3.UnitY),"internal terrain edge cannot become upward compression");
        Need(!CraftTerrainContacts.AcceptsRecovery(-Vector3.UnitY,Vector3.UnitY),"terrain backface rejected");
        Need(CraftTerrainContacts.AcceptsRecovery(Vector3.UnitY,Vector3.UnitY),"ordinary terrain face retained");
        var domain=CraftSurfaceImpact.MinimumRecoverySaturationDepth;
        foreach(var dt in new[]{double.Epsilon,1e-12,1d/1024,1d/64})
            Need(domain<=(double)material.MaximumRecoveryVelocity*(dt+material.SpringSettings.TwiceDampingRatio/(double)material.SpringSettings.AngularFrequency),"domain below recovery saturation at every admitted duration");
        var edge=new Vector3(.9999999f,-.00044130764f,-.00013960742f);
        Need(!CraftTerrainContacts.AcceptsRecovery(edge,Vector3.UnitY),"exact rejected child12/triangle12728 causal witness");
        foreach(var alignment in new[]{MathF.BitDecrement(.1f),.1f,MathF.BitIncrement(.1f)})
            Need(CraftTerrainContacts.AcceptsRecovery(new(MathF.Sqrt(1-alignment*alignment),alignment,0),Vector3.UnitY)==(alignment>=.1f),"terrain cone exact boundary");
        // A prescribed one-normal-degree-of-freedom fixture isolates the
        // actual pinned BEPU spring from rotation/friction/compound reduction.
        // Its independent recurrence is derived from the spring coefficients,
        // not fitted to a spacecraft landing trajectory.
        const float h=1f/1024,g=9.81f;
        foreach(var initial in new[]{.2f,2f})
        {
            var pool=new BufferPool();var metrics=new LocalContactMetrics();
            var sim=BepuPhysics.Simulation.Create(pool,new LocalContactCallbacks(metrics),new LocalContactIntegrator(new(0,-g,0)),new SolveDescription(8,1));
            try
            {
                sim.Solver.Register<Row>();
                var body=sim.Bodies.Add(BodyDescription.CreateDynamic(new RigidPose(Vector3.Zero),new BodyVelocity(new(0,-initial,0)),new BodyInertia{InverseMass=1},default,new(-1)));
                var manifold=new ConvexContactManifold{Count=1,Normal=Vector3.UnitY};
                var handle=sim.Solver.Add(body,new Row{Manifold=manifold,Material=material});
                double depth=0,closing=initial,peak=0;var omega=(double)material.SpringSettings.AngularFrequency;var denominator=Math.Pow(1+omega*h,2);
                var previousEnergy=.5*initial*initial;
                for(var i=0;i<128;i++)
                {
                    manifold[0]=new(){Offset=Vector3.Zero,Normal=Vector3.UnitY,Depth=-sim.Bodies[body].Pose.Position.Y,FeatureId=0};
                    sim.Solver.ApplyDescription(handle,new Row{Manifold=manifold,Material=material});
                    var free=closing+g*h;
                    var bias=Math.Min(Math.Min(depth/h,depth*omega/(omega*h+2)),material.MaximumRecoveryVelocity);
                    closing=free-Math.Max(0,(1-1/denominator)*(free+bias));
                    depth+=h*closing;peak=Math.Max(peak,depth);
                    sim.Timestep(h);
                    Need(Math.Abs(-sim.Bodies[body].Pose.Position.Y-depth)<2e-6&&Math.Abs(-sim.Bodies[body].Velocity.Linear.Y-closing)<2e-5,$"pinned native normal response versus independent recurrence at {i}: native={sim.Bodies[body].Pose.Position.Y:R}/{sim.Bodies[body].Velocity.Linear.Y:R}, expected={depth:R}/{closing:R}");
                    var energy=.5*closing*closing+.5*omega*omega*depth*depth-g*depth;
                    Need(energy<=previousEnergy+1e-12,"passive critically damped recurrence never creates total mechanical energy");previousEnergy=energy;
                    var nativeDepth=-(double)sim.Bodies[body].Pose.Position.Y;var nativeSpeed=(double)sim.Bodies[body].Velocity.Linear.Y;
                    var nativeEnergy=.5*nativeSpeed*nativeSpeed+.5*omega*omega*nativeDepth*nativeDepth-g*nativeDepth;
                    var error=Math.Abs(closing)*2e-5+.5*2e-5*2e-5+(omega*omega*Math.Abs(depth)+g)*2e-6+.5*omega*omega*2e-6*2e-6;
                    Need(nativeEnergy<=energy+error,"native energy stays inside independently derived trajectory-error envelope");
                }
                if(initial==2)Need(peak>.0035&&peak<.004,"2 mm numerical tolerance is not the physical spring-compression ceiling");
                Console.WriteLine($"SURFACE_SPRING initial={initial:R} peak={peak:R} final={depth:R}");
            }
            finally{sim.Dispose();pool.Clear();}
        }
        foreach(var horizontal in new[]{new Vector3(1,0,0),new(-1,0,0),new(0,0,1),new(0,0,-1),new(1,0,1)})
        {
            var pool=new BufferPool();var metrics=new LocalContactMetrics();
            var sim=BepuPhysics.Simulation.Create(pool,new LocalContactCallbacks(metrics),new LocalContactIntegrator(new(0,-g,0)),new SolveDescription(8,1));
            try
            {
                sim.Solver.Register<Row>();
                var body=sim.Bodies.Add(BodyDescription.CreateDynamic(new RigidPose(Vector3.Zero),new BodyVelocity(horizontal),new BodyInertia{InverseMass=1},default,new(-1)));
                var manifold=new ConvexContactManifold{Count=1,Normal=Vector3.UnitY};var handle=sim.Solver.Add(body,new Row{Manifold=manifold,Material=material});
                for(var step=0;step<160;step++)
                {
                    var before=sim.Bodies[body].Velocity.Linear;manifold[0]=new(){Offset=Vector3.Zero,Normal=Vector3.UnitY,Depth=-sim.Bodies[body].Pose.Position.Y};
                    sim.Solver.ApplyDescription(handle,new Row{Manifold=manifold,Material=material});sim.Timestep(h);
                    var after=sim.Bodies[body].Velocity.Linear;var jn=(double)after.Y-before.Y+g*h;
                    var jx=(double)after.X-before.X;var jz=(double)after.Z-before.Z;var jt=Math.Sqrt(jx*jx+jz*jz);
                    var scale=Math.Max(1,Math.Max(before.Length(),after.Length()));var error=64*(MathF.BitIncrement(scale)-scale);
                    Need(jn>=-error&&jt<=material.FrictionCoefficient*Math.Max(0,jn)+error,"native tangential impulse obeys Coulomb cone");
                    Need(jx*before.X+jz*before.Z<=error,"native friction opposes both signed axes and diagonal sliding");
                    Need((double)after.X*after.X+(double)after.Z*after.Z<=(double)before.X*before.X+(double)before.Z*before.Z+error,"friction does not add tangential kinetic energy");
                }
            }
            finally{sim.Dispose();pool.Clear();}
        }
        // Recovery-speed saturation is a normal native solver branch, not a
        // material failure. Independently reconstruct one-step unilateral
        // impulses across its actual slice-dependent transition, including
        // separating, stationary and rapidly closing contacts.
        var capped=0;
        for(var refinement=0;refinement<=CraftSurfaceImpact.MaximumRefinement;refinement++)
        {
            var dt=MathF.ScaleB(h,-refinement);var omega=(double)material.SpringSettings.AngularFrequency;
            var transition=material.MaximumRecoveryVelocity*(dt+material.SpringSettings.TwiceDampingRatio/omega);
            var boundary=(float)transition;
            foreach(var initialDepth in new[]{MathF.BitDecrement(boundary),boundary,MathF.BitIncrement(boundary),boundary*4})
            foreach(var initialClosing in new[]{-2f,0f,2f,20f})
            {
                var pool=new BufferPool();var metrics=new LocalContactMetrics();
                var sim=BepuPhysics.Simulation.Create(pool,new LocalContactCallbacks(metrics),new LocalContactIntegrator(new(0,-g,0)),new SolveDescription(8,1));
                try
                {
                    sim.Solver.Register<Row>();
                    var body=sim.Bodies.Add(BodyDescription.CreateDynamic(new RigidPose(new Vector3(0,-initialDepth,0)),new BodyVelocity(new(0,-initialClosing,0)),new BodyInertia{InverseMass=1},default,new(-1)));
                    var manifold=new ConvexContactManifold{Count=1,Normal=Vector3.UnitY};var handle=sim.Solver.Add(body,new Row{Manifold=manifold,Material=material});
                    for(var step=0;step<32;step++)
                    {
                        var depth=-(double)sim.Bodies[body].Pose.Position.Y;var closing=-(double)sim.Bodies[body].Velocity.Linear.Y;
                        manifold[0]=new(){Offset=Vector3.Zero,Normal=Vector3.UnitY,Depth=(float)depth};
                        sim.Solver.ApplyDescription(handle,new Row{Manifold=manifold,Material=material});
                        var free=closing+g*dt;var unconstrained=depth*omega/(omega*dt+material.SpringSettings.TwiceDampingRatio);
                        var bias=Math.Min(Math.Min(depth/dt,unconstrained),material.MaximumRecoveryVelocity);
                        if(unconstrained>material.MaximumRecoveryVelocity)capped++;
                        var impulse=Math.Max(0,(1-1/Math.Pow(1+omega*dt,2))*(free+bias));var expected=free-impulse;
                        sim.Timestep(dt);
                        var scale=(float)Math.Max(1,Math.Abs(free)+Math.Abs(bias));var error=64d*(MathF.BitIncrement(scale)-scale);
                        Need(double.IsFinite(impulse)&&impulse>=0&&bias<=material.MaximumRecoveryVelocity,"ordinary capped unilateral branch is finite and bounded");
                        Need(Math.Abs(sim.Bodies[body].Velocity.Linear.Y+expected)<=error,"capped native velocity matches independent spring impulse without velocity clamp");
                        Need(Math.Abs(sim.Bodies[body].Pose.Position.Y+depth+dt*expected)<=error*dt+4*(MathF.BitIncrement((float)Math.Max(1,depth))-(float)Math.Max(1,depth)),"capped native pose matches unchanged integration");
                    }
                }
                finally{sim.Dispose();pool.Clear();}
            }
        }
        Need(capped>0,"positive coverage of recovery saturation");
        Console.WriteLine($"SURFACE_SATURATED_RESPONSE steps={capped}");
        Console.WriteLine($"SURFACE_RESPONSE_PASS checks={checks}");
    }
}
