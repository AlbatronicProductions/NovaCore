using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Simulation.Clock;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Rotation.Transactions;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;

internal static class SpacecraftPhysicalEventMotionTests
{
    private static readonly SpacecraftId Craft = new(71);
    private static readonly ReferenceFrameId Root = new(1), Body = new(72);
    private static readonly SpacecraftPhysicalProperties Mass = new(8);
    private static readonly SpacecraftTranslationState Linear = new(Craft, Root, new(0), new(1024, -32, 8), new(2, -1, .5), new(4, 2, -1));
    private static readonly SpacecraftRigidBodyRotationState Angular = new(Craft, new(0), DoubleQuaternion.Identity,
        new(.125, -.0625, .25), new(2, 3, 4), new(.125, -.25, .375), RigidBodyRotationModel.ConstantBodyTorqueV1);
    private const double U = 1.1102230246251565e-16;

    internal static void Run()
    {
        Duration(); CanonicalParity(); TranslationOracle(); RotationOracle(); Failures(); CoherenceReplayImmutability(); Allocation();
        Console.WriteLine("PASS Exact-event spacecraft motion: exact duration, canonical bits, rational oracle, rotation, failures, replay, immutability, allocation");
    }
    private static void Check(bool value, string contract) { if (!value) throw new InvalidOperationException("Exact-event motion: " + contract); }
    private static PhysicalEventEpoch E(long ticks, Int128 n = default, Int128 d = default)
    {
        Check(PhysicalEventEpoch.TryCreate(ticks, n, d == 0 ? 1 : d, out var e) == PhysicalEventEpochStatus.Success, "epoch fixture");
        return e;
    }
    private static (SimulationState State, SimulationClock Clock, SimulationTransactionEngine Engine) Fixture(
        SpacecraftTranslationState? linear = null, SpacecraftRigidBodyRotationState? angular = null)
    {
        var graph = new ReferenceFrameGraphBuilder();
        graph.Add(new ReferenceFrameNode(Root, null, ReferenceFrameKind.Ecl, "Root")); graph.Add(new ReferenceFrameNode(Body, Root, ReferenceFrameKind.Ccf, "Craft"));
        Check(SpacecraftStateStore.TryCreateTranslating([new(Craft, Root, Body, "Craft")], [angular ?? Angular],
            [Mass], [linear ?? Linear], graph.Build(), out var store, out _), "authority fixture");
        var state = new SimulationState(spacecraft: store); var clock = new SimulationClock((linear ?? Linear).Epoch, new SimulationTimeline());
        return (state, clock, new(clock, state, 16));
    }
    private static SpacecraftPhysicalEventMotion Evaluate(SimulationState state, PhysicalEventEpoch e)
    {
        Check(SpacecraftPhysicalEventMotionEvaluator.TryEvaluate(state.CreateView(), Craft, e, out var motion) == SpacecraftTranslationStatus.Success, "motion success");
        return motion;
    }
    private static bool Bits(double a, double b) => BitConverter.DoubleToInt64Bits(a) == BitConverter.DoubleToInt64Bits(b);
    private static bool Bits(Double3 a, Double3 b) => Bits(a.X, b.X) && Bits(a.Y, b.Y) && Bits(a.Z, b.Z);
    private static bool Bits(DoubleQuaternion a, DoubleQuaternion b) => Bits(a.X, b.X) && Bits(a.Y, b.Y) && Bits(a.Z, b.Z) && Bits(a.W, b.W);
    private static bool Bits(SpacecraftPhysicalEventMotion a, SpacecraftPhysicalEventMotion b) =>
        a.Spacecraft == b.Spacecraft && a.Epoch == b.Epoch && a.Revision == b.Revision && a.RootFrame == b.RootFrame &&
        Bits(a.PositionRoot,b.PositionRoot) && Bits(a.VelocityRoot,b.VelocityRoot) && Bits(a.BodyToRoot,b.BodyToRoot) &&
        Bits(a.AngularVelocityBody,b.AngularVelocityBody) && Bits(a.Properties.MassKilograms,b.Properties.MassKilograms) &&
        Bits(a.Inertia.X,b.Inertia.X) && Bits(a.Inertia.Y,b.Inertia.Y) && Bits(a.Inertia.Z,b.Inertia.Z);

    private static void Duration()
    {
        var random = new Random(74106);
        for (var i=0;i<1000;i++)
        {
            var origin = random.NextInt64(long.MinValue, long.MaxValue);
            var floor = i%3 == 0 ? origin : random.NextInt64(long.MinValue,long.MaxValue);
            var den = i%7 == 0 ? ulong.MaxValue : (ulong)random.NextInt64(2,long.MaxValue);
            var e = E(floor, (ulong)random.NextInt64() % den, den);
            var exactN = ((BigInteger)e.FloorTicks-origin)*e.Denominator+e.Numerator;
            var fits = exactN >= (BigInteger)long.MinValue*e.Denominator && exactN <= (BigInteger)long.MaxValue*e.Denominator;
            Check(PhysicalEventDuration.TryDifference(e,new(origin),out var duration)==fits,"duration capacity against BigInteger");
            if (!fits) continue;
            var actualN = ((BigInteger)duration.WholeMagnitudeTicks*duration.Denominator+duration.Numerator)*(duration.IsNegative?-1:1);
            Check(actualN*e.Denominator == exactN*duration.Denominator,"exact signed duration, no wrap");
            var seconds=(double)exactN/((double)e.Denominator*SimulationInstant.TicksPerSecond);
            Check(Math.Abs(duration.Seconds-seconds)<=4*U*Math.Abs(seconds),"local conversion rounding bound");
        }
        Check(PhysicalEventDuration.TryDifference(E(long.MinValue),new(0),out var minimum) && minimum.WholeMagnitudeTicks==1UL<<63 && minimum.IsNegative,"Int64 minimum magnitude");
        Check(!PhysicalEventDuration.TryDifference(E(long.MaxValue,1,2),new(0),out _),"positive fractional duration capacity");
        Check(PhysicalEventDuration.TryDifference(E(-1,2,3),new(0),out var negative) && negative.IsNegative && negative.WholeMagnitudeTicks==0 && negative.Numerator==1 && negative.Denominator==3,"negative fraction magnitude");
        Check(PhysicalEventDuration.TryDifference(E(long.MaxValue,1,3),new(long.MaxValue),out var late) && late.Seconds>0 && late.WholeMagnitudeTicks==0,"maximum floor local precision");
    }

    private static void CanonicalParity()
    {
        var count=0;
        foreach(var start in new[]{0L,-50_000L,long.MinValue,long.MaxValue-100_000})
        foreach(var spherical in new[]{false,true})
        {
            var l=Linear with {Epoch=new(start)};
            var r=Angular with {Epoch=new(start),PrincipalInertia=spherical?new(2,2,2):Angular.PrincipalInertia,ConstantBodyTorque=spherical?Double3.Zero:Angular.ConstantBodyTorque};
            var f=Fixture(l,r);
            foreach(var delta in new[]{0L,1L,17L,9999L,10_000L,10_001L,20_000L,31_337L})
            {
                var time=new SimulationInstant(start+delta);
                var status=SpacecraftMotionEvaluator.TryEvaluate(f.State.CreateView(),Craft,time,out var canonical);
                var other=SpacecraftPhysicalEventMotionEvaluator.TryEvaluate(f.State.CreateView(),Craft,PhysicalEventEpoch.FromCanonical(time),out var value);
                var expected=new SpacecraftPhysicalEventMotion(canonical.Spacecraft,PhysicalEventEpoch.FromCanonical(time),canonical.Revision,canonical.RootFrame,
                    canonical.PositionRoot,canonical.VelocityRoot,canonical.BodyToRoot,canonical.AngularVelocityBody,canonical.Properties,canonical.Inertia);
                Check(status==other && Bits(expected,value),"all canonical result fields bit-identical"); count++;
            }
        }
        var refusal=Fixture();
        foreach(var tick in new[]{-1L,long.MaxValue,10_000_000_000L})
        {
            var a=SpacecraftMotionEvaluator.TryEvaluate(refusal.State.CreateView(),Craft,new SimulationInstant(tick),out var old);
            var b=SpacecraftPhysicalEventMotionEvaluator.TryEvaluate(refusal.State.CreateView(),Craft,E(tick),out var exact);
            Check(a==b && a!=SpacecraftTranslationStatus.Success && old==default && exact==default,"canonical refusal status/default parity");
        }
        Console.WriteLine($"EXACT_EVENT canonical_bit_parity={count}");
    }

    // Independent exact rational arithmetic; production uses no BigInteger. Input doubles are decoded exactly.
    private readonly record struct Q(BigInteger N, BigInteger D)
    {
        internal static Q Of(double x)
        {
            if (x == 0) return new(0,1);
            var bits=BitConverter.DoubleToUInt64Bits(x); var exponent=(int)((bits>>52)&2047); var mantissa=bits&0xfffffffffffffUL;
            var power=exponent==0 ? -1074 : exponent-1023-52;
            if(exponent!=0) mantissa|=1UL<<52;
            BigInteger n=mantissa; if((bits>>63)!=0)n=-n;
            return power>=0 ? new(n<<power,1) : new(n,BigInteger.One<<-power);
        }
        public static Q operator +(Q a,Q b)=>new(a.N*b.D+b.N*a.D,a.D*b.D);
        public static Q operator *(Q a,Q b)=>new(a.N*b.N,a.D*b.D);
        public static Q operator /(Q a,Q b)=>new(a.N*b.D,a.D*b.N);
        internal double Value=>(double)N/(double)D;
    }
    private static void TranslationOracle()
    {
        double maxPosition=0,maxVelocity=0;
        var fractions=new (ulong N,ulong D)[]{(1,2),(1,3),(15625,32768),(1,ulong.MaxValue),(ulong.MaxValue-1,ulong.MaxValue)};
        foreach(var origin in new[]{0L,-2L,long.MinValue,long.MaxValue-100})
        foreach(var offset in new[]{0L,1L,31L})
        foreach(var fraction in fractions)
        foreach(var force in new[]{Double3.Zero,new Double3(4,-2,1)})
        {
            var state=Linear with {Epoch=new(origin),PositionRoot=new(1e12,-32,8),ConstantForceRoot=force};
            var e=E(origin+offset,fraction.N,fraction.D);
            Check(SpacecraftPhysicalEventTranslationEvaluator.TryEvaluate(state,Mass,e,out var p,out var v)==SpacecraftTranslationStatus.Success,"rational translation success");
            var dt=new Q(((BigInteger)e.FloorTicks-origin)*e.Denominator+e.Numerator,(BigInteger)e.Denominator*SimulationInstant.TicksPerSecond);
            var ps=new[]{state.PositionRoot.X,state.PositionRoot.Y,state.PositionRoot.Z}; var vs=new[]{state.VelocityRoot.X,state.VelocityRoot.Y,state.VelocityRoot.Z};
            var fs=new[]{force.X,force.Y,force.Z}; var actualP=new[]{p.X,p.Y,p.Z}; var actualV=new[]{v.X,v.Y,v.Z};
            for(var axis=0;axis<3;axis++)
            {
                var a=Q.Of(fs[axis])/Q.Of(Mass.MassKilograms); var dv=a*dt; var drift=Q.Of(vs[axis])*dt; var accel=Q.Of(.5)*a*dt*dt;
                var ep=(Q.Of(ps[axis])+drift+accel).Value; var ev=(Q.Of(vs[axis])+dv).Value;
                var gamma=32*U/(1-32*U);
                var pb=gamma*(Math.Abs(ps[axis])+Math.Abs(drift.Value)+Math.Abs(accel.Value));
                var vb=gamma*(Math.Abs(vs[axis])+Math.Abs(dv.Value));
                maxPosition=Math.Max(maxPosition,Math.Abs(actualP[axis]-ep)); maxVelocity=Math.Max(maxVelocity,Math.Abs(actualV[axis]-ev));
                Check(Math.Abs(actualP[axis]-ep)<=pb && Math.Abs(actualV[axis]-ev)<=vb,
                    $"independent rational force oracle gamma32 bound origin={origin} offset={offset} fraction={fraction} axis={axis} p={actualP[axis]:R}/{ep:R} v={actualV[axis]:R}/{ev:R} bounds={pb:R}/{vb:R}");
            }
        }
        var witness=Linear with {PositionRoot=Double3.Zero,VelocityRoot=new(8,0,0),ConstantForceRoot=Double3.Zero};
        Check(SpacecraftPhysicalEventTranslationEvaluator.TryEvaluate(witness,Mass,E(0,15625,32768),out var exact,out _)==SpacecraftTranslationStatus.Success && Bits(exact.X,Math.ScaleB(1,-18)),"binary-exact sub-tick witness");
        Console.WriteLine($"EXACT_EVENT translation_max_position_error_m={maxPosition:R} velocity_error_m_s={maxVelocity:R}");
    }

    private static double Distance(DoubleQuaternion a,DoubleQuaternion b)=>Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Y-b.Y)*(a.Y-b.Y)+(a.Z-b.Z)*(a.Z-b.Z)+(a.W-b.W)*(a.W-b.W));
    private static double Norm(Double3 v)=>Math.Sqrt(Double3.Dot(v,v));
    private static Double3 Axis(int axis,double v)=>axis==0?new(v,0,0):axis==1?new(0,v,0):new(0,0,v);
    private static void RotationOracle()
    {
        double maxAnalytic=0;
        foreach(var axis in new[]{0,1,2})
        foreach(var ticks in new[]{0L,9999L,10000L,20000L})
        {
            var r=Angular with {AngularVelocityBody=Axis(axis,.125),ConstantBodyTorque=Axis(axis,.25)};
            var e=E(ticks,1,3); var t=(ticks+1d/3)/1e6; var inertia=axis==0?2d:axis==1?3d:4d;
            var theta=.125*t+.5*(.25/inertia)*t*t;
            var s=Axis(axis,Math.Sin(theta*.5)); var q=new DoubleQuaternion(s.X,s.Y,s.Z,Math.Cos(theta*.5));
            Check(SpacecraftPhysicalEventRotationEvaluator.TryEvaluate(r,e,out var actual,out var omega,out var steps)==SpacecraftRigidBodyRotationEvaluationStatus.Success,"axis torque evaluation");
            var error=Distance(actual,q); maxAnalytic=Math.Max(maxAnalytic,error);
            // Fixture-derived fifth-derivative envelope for q'=q*omega/2 with constant acceleration,
            // plus 128 rounding operations per normalized RK4 step. No fixed absolute tolerance.
            var b=(.125+.25/inertia*t)*.5; var c=.25/inertia*.5;
            var derivative5=Math.Pow(b,5)+10*Math.Pow(b,3)*c+15*b*c*c;
            var qBound=steps*(Math.Pow(.01,5)*derivative5+128*U);
            var wBound=32*U*steps*(.125+.25/inertia*t);
            Check(error<=qBound && Norm(omega-Axis(axis,.125+.25/inertia*t))<=wBound,"analytical principal-axis rotation");
            Check(steps==(ticks+9999)/10000+1,"fractional endpoint step count");
        }
        var target=E(31337,15625,32768); var seconds=(31337d+15625d/32768)/1e6;
        Check(SpacecraftPhysicalEventRotationEvaluator.TryEvaluate(Angular,target,out var result,out var w,out _)==SpacecraftRigidBodyRotationEvaluationStatus.Success,"mixed torque result");
        var coarse=MidpointReference(Angular,seconds,1024); var fine=MidpointReference(Angular,seconds,2048); var finer=MidpointReference(Angular,seconds,4096);
        var coarseError=Distance(coarse.Q,fine.Q)+Norm(coarse.W-fine.W);
        var fineError=Distance(fine.Q,finer.Q)+Norm(fine.W-finer.W);
        Check(fineError<coarseError*.4+128*U,"independent midpoint reference convergence");
        // O(h^2) midpoint residual estimated by refinement / 3, with factor four allowance;
        // 128 FP64 operations per reference step bound its accumulated rounding scale.
        var referenceBound=4*fineError/3+128*U*4096*(1+Norm(finer.W));
        Check(Distance(result,finer.Q)<=referenceBound && Norm(w-finer.W)<=referenceBound,"mixed RK4 against converged independent midpoint solution");
        foreach(var t in new[]{E(0,1,2),E(1,1,3),E(-1,2,3)})
        {
            var r=Angular with {PrincipalInertia=new(2,2,2),ConstantBodyTorque=Double3.Zero};
            Check(SpacecraftPhysicalEventRotationEvaluator.TryEvaluate(r,t,out var q,out var rates,out var steps)==SpacecraftRigidBodyRotationEvaluationStatus.Success && steps==0,"spherical analytical path");
            var dt=(t.FloorTicks+(double)t.Numerator/t.Denominator)/1e6; var speed=Norm(r.AngularVelocityBody); var vec=r.AngularVelocityBody*(Math.Sin(speed*dt*.5)/speed);
            Check(Distance(q,new(vec.X,vec.Y,vec.Z,Math.Cos(speed*dt*.5)))<32*U && rates==r.AngularVelocityBody,"constant-rate quaternion oracle");
        }
        var zero=Angular with {AngularVelocityBody=Double3.Zero,ConstantBodyTorque=Double3.Zero};
        Check(SpacecraftPhysicalEventRotationEvaluator.TryEvaluate(zero,E(10000,1,2),out var identity,out var zeroW,out _)==SpacecraftRigidBodyRotationEvaluationStatus.Success && identity==DoubleQuaternion.Identity && zeroW==Double3.Zero,"stationary rotation");
        foreach(var endpoint in new[]{E(0,1,2),E(9999,ulong.MaxValue-1,ulong.MaxValue),E(10000,1,ulong.MaxValue),E(-1,1,2),E(-10001,ulong.MaxValue-1,ulong.MaxValue)})
        {
            Check(SpacecraftPhysicalEventRotationEvaluator.TryEvaluate(Angular,endpoint,out var q,out var rates,out var steps)==SpacecraftRigidBodyRotationEvaluationStatus.Success,"near substep boundary evaluation");
            var expectedSteps=(Math.Abs(endpoint.FloorTicks)+9999)/10000+1;
            Check(steps==expectedSteps && q.IsFinite && rates.IsFinite && Math.Abs(q.LengthSquared-1)<16*U,"exact step partition, finite normalized quaternion");
            var t=((double)endpoint.FloorTicks+(double)endpoint.Numerator/endpoint.Denominator)/1e6;
            var reference=MidpointReference(Angular,t,4096); var half=MidpointReference(Angular,t,2048);
            var bound=4*(Distance(reference.Q,half.Q)+Norm(reference.W-half.W))/3+128*U*4096*(1+Norm(reference.W));
            Check(Distance(q,reference.Q)<=bound && Norm(rates-reference.W)<=bound,"signed boundary independent rotation reference");
        }
        Console.WriteLine($"EXACT_EVENT rotation_axis_max_q_error={maxAnalytic:R} mixed_q_error={Distance(result,finer.Q):R} mixed_omega_error={Norm(w-finer.W):R} reference_refinement={fineError:R}");
    }

    // Independent explicit midpoint solver, not the production RK4 step or production derivative helper.
    private static (DoubleQuaternion Q,Double3 W) MidpointReference(SpacecraftRigidBodyRotationState s,double seconds,int steps)
    {
        var y=new[]{s.OrientationLocalToParent.X,s.OrientationLocalToParent.Y,s.OrientationLocalToParent.Z,s.OrientationLocalToParent.W,s.AngularVelocityBody.X,s.AngularVelocityBody.Y,s.AngularVelocityBody.Z};
        var k=new double[7]; var mid=new double[7]; var d=new double[7]; var h=seconds/steps;
        for(var step=0;step<steps;step++)
        {
            Derivative(y,k); for(var j=0;j<7;j++)mid[j]=y[j]+h*.5*k[j]; Derivative(mid,d);
            for(var j=0;j<7;j++)y[j]+=h*d[j];
        }
        return (new DoubleQuaternion(y[0],y[1],y[2],y[3]).Normalized(),new(y[4],y[5],y[6]));
        void Derivative(double[] a,double[] b)
        {
            b[0]=.5*(a[3]*a[4]+a[1]*a[6]-a[2]*a[5]); b[1]=.5*(a[3]*a[5]+a[2]*a[4]-a[0]*a[6]);
            b[2]=.5*(a[3]*a[6]+a[0]*a[5]-a[1]*a[4]); b[3]=-.5*(a[0]*a[4]+a[1]*a[5]+a[2]*a[6]);
            b[4]=(s.ConstantBodyTorque.X+(s.PrincipalInertia.Y-s.PrincipalInertia.Z)*a[5]*a[6])/s.PrincipalInertia.X;
            b[5]=(s.ConstantBodyTorque.Y+(s.PrincipalInertia.Z-s.PrincipalInertia.X)*a[6]*a[4])/s.PrincipalInertia.Y;
            b[6]=(s.ConstantBodyTorque.Z+(s.PrincipalInertia.X-s.PrincipalInertia.Y)*a[4]*a[5])/s.PrincipalInertia.Z;
        }
    }

    private static void Failures()
    {
        foreach(var e in new[]{E(-1,1,2),E(-2),E(long.MinValue)})
            Check(SpacecraftPhysicalEventTranslationEvaluator.TryEvaluate(Linear,Mass,e,out var p,out var v)==SpacecraftTranslationStatus.TimeBeforeEpoch && p==default && v==default,"before translation epoch, default output");
        Check(SpacecraftPhysicalEventTranslationEvaluator.TryEvaluate(Linear,new(double.NaN),E(0,1,2),out _,out _)==SpacecraftTranslationStatus.InvalidMass,"invalid mass");
        foreach(var invalid in new[]{Linear with {Spacecraft=default},Linear with {RootFrame=default},Linear with {PositionRoot=new(double.NaN,0,0)},Linear with {VelocityRoot=new(0,double.NaN,0)},Linear with {ConstantForceRoot=new(0,0,double.NaN)}})
            Check(SpacecraftPhysicalEventTranslationEvaluator.TryEvaluate(invalid,Mass,E(0,1,3),out var p,out var v)==SpacecraftTranslationStatus.InvalidState && p==default && v==default,"invalid linear state");
        Check(SpacecraftPhysicalEventTranslationEvaluator.TryEvaluate(Linear with {Epoch=new(long.MinValue)},Mass,E(0,1,2),out _,out _)==SpacecraftTranslationStatus.DurationOverflow,"linear duration overflow");
        Check(SpacecraftPhysicalEventTranslationEvaluator.TryEvaluate(Linear with {ConstantForceRoot=new(double.MaxValue,0,0)},new(double.Epsilon),E(0,1,3),out var badP,out var badV)==SpacecraftTranslationStatus.NonFiniteResult && badP==default && badV==default,"nonfinite acceleration");
        Check(SpacecraftPhysicalEventTranslationEvaluator.TryEvaluate(Linear with {PositionRoot=new(double.MaxValue,0,0),VelocityRoot=new(double.MaxValue,0,0)},Mass,E(2_000_000,1,3),out _,out _)==SpacecraftTranslationStatus.NonFiniteResult,"nonfinite evolved position");
        var f=Fixture();
        foreach(var e in new[]{E(-1,1,2),E(-1),E(long.MaxValue,1,2)})
        {
            Check(SpacecraftPhysicalEventMotionEvaluator.TryEvaluate(f.State.CreateView(),Craft,e,out var value)!=SpacecraftTranslationStatus.Success && value==default,"coherent refusal is default");
        }
        Check(SpacecraftPhysicalEventMotionEvaluator.TryEvaluate(f.State.CreateView(),new(999),E(0,1,2),out var missing)==SpacecraftTranslationStatus.SubjectNotFound && missing==default,"missing craft");
        Check(SpacecraftPhysicalEventRotationEvaluator.TryEvaluate(Angular,E(SpacecraftRigidBodyRotationEvaluator.MaximumEvaluationTicks,1,ulong.MaxValue),out _,out _,out _)==SpacecraftRigidBodyRotationEvaluationStatus.DurationBoundExceeded,"exact fractional rotation bound");
        Check(SpacecraftPhysicalEventRotationEvaluator.TryEvaluate(Angular,E(10_000_000_000,1,3),out _,out _,out _)==SpacecraftRigidBodyRotationEvaluationStatus.ExcessiveStepCount,"bounded RK4 work refusal");
        Check(SpacecraftPhysicalEventRotationEvaluator.TryEvaluate(Angular,E(9_999_990_001,1,3),out _,out _,out _)==SpacecraftRigidBodyRotationEvaluationStatus.ExcessiveStepCount,"terminal step reserves capacity before floor work");
        var normalization=Angular with {AngularVelocityBody=new(1e50,0,0),ConstantBodyTorque=Double3.Zero};
        Check(SpacecraftPhysicalEventRotationEvaluator.TryEvaluate(normalization,E(0,1,2),out var normQ,out var normW,out var normSteps)==SpacecraftRigidBodyRotationEvaluationStatus.QuaternionNormalizationFailure && normQ==default && normW==default && normSteps==0,"terminal normalization refusal and default result");
        var boundedSphere=Angular with {PrincipalInertia=new(2,2,2),ConstantBodyTorque=Double3.Zero,AngularVelocityBody=Double3.Zero};
        var limit=SpacecraftRigidBodyRotationEvaluator.MaximumEvaluationTicks;
        foreach(var e in new[]{E(limit-1,ulong.MaxValue-1,ulong.MaxValue),E(-limit,1,ulong.MaxValue)})
            Check(SpacecraftPhysicalEventRotationEvaluator.TryEvaluate(boundedSphere,e,out _,out _,out _)==SpacecraftRigidBodyRotationEvaluationStatus.Success,"fraction immediately inside exact duration bound");
        Check(SpacecraftPhysicalEventRotationEvaluator.TryEvaluate(boundedSphere,E(-limit-1,ulong.MaxValue-1,ulong.MaxValue),out _,out _,out _)==SpacecraftRigidBodyRotationEvaluationStatus.DurationBoundExceeded,"fraction immediately outside negative duration bound");
        Check(SpacecraftPhysicalEventRotationEvaluator.TryEvaluate(Angular with {Epoch=new(long.MinValue)},E(0,1,3),out _,out _,out _)==SpacecraftRigidBodyRotationEvaluationStatus.DurationOverflow,"rotation difference overflow");
        foreach(var bad in new[]{Angular with {PrincipalInertia=new(0,1,1)},Angular with {OrientationLocalToParent=default},Angular with {AngularVelocityBody=new(double.NaN,0,0)},Angular with {ConstantBodyTorque=new(double.NaN,0,0)},Angular with {Model=(RigidBodyRotationModel)255}})
            Check(SpacecraftPhysicalEventRotationEvaluator.TryEvaluate(bad,E(0,1,3),out var q,out var w,out var steps)!=SpacecraftRigidBodyRotationEvaluationStatus.Success && q==default && w==default && steps==0,"rotation invalid/default");
        // Default PhysicalEventEpoch is a VALID canonical zero, not an invalid sentinel.
        Check(Evaluate(f.State,default).Epoch==PhysicalEventEpoch.FromCanonical(new(0)),"valid default epoch");
        var later=Fixture(); later.Clock.AdvanceTo(new(10));
        var torque=RigidBodyTorqueTransactionEvaluator.TryCreateControlReplacement(later.Engine.State,new(Craft,new(0,0,.5),later.Clock.CurrentTime));
        Check(torque.Succeeded && later.Engine.ValidateAndCommit(torque.Transaction!.Value).Committed,"later rotation authority segment");
        Check(SpacecraftPhysicalEventMotionEvaluator.TryEvaluate(later.State.CreateView(),Craft,E(9,1,2),out var none)==SpacecraftTranslationStatus.TimeBeforeEpoch && none==default,"before rotation epoch");
        Check(SpacecraftPhysicalEventMotionEvaluator.TryEvaluate(f.State.CreateView(),Craft,E(10_000_000_000,1,3),out var rotationFailure)==SpacecraftTranslationStatus.RotationEvaluationFailed && rotationFailure==default,"coherent rotation refusal is not partial");
    }

    private static void CoherenceReplayImmutability()
    {
        var l=Linear with {Epoch=new(-100)}; var f=Fixture(l,Angular with {Epoch=new(-100)});
        f.Clock.AdvanceTo(new(-10));
        var torque=RigidBodyTorqueTransactionEvaluator.TryCreateControlReplacement(f.Engine.State,new(Craft,new(0,0,.5),f.Clock.CurrentTime));
        Check(torque.Succeeded && f.Engine.ValidateAndCommit(torque.Transaction!.Value).Committed,"independent rotation segment through canonical transaction");
        Check(f.State.CreateView().Spacecraft.TryGetRigidBody(Craft,out var r) && r.Epoch.Ticks==-10,"different stored segment epochs");
        f.Clock.AdvanceTo(new(0));
        Check(SimulationEventRequest.TryCreateSpacecraftForce(new(1),0,new(Craft,new(0),new(8,0,0)),out var request),"future mutation fixture");
        Check(f.Clock.Timeline.Schedule(f.Clock.CurrentTime,request).Succeeded,"pending event fixture");
        var before=f.State.CreateView(); var timelineRevision=f.Clock.Timeline.Revision; var clock=f.Clock.CurrentTime; var debt=f.Clock.PendingSimulationDebt;
        var historyCount=f.Engine.ProcessedCount; var torqueCount=f.Engine.ProcessedRigidBodyTorqueCount;
        Check(f.Clock.Timeline.TryPeekPending(out var pending),"pending before queries");
        var epochs=new PhysicalEventEpoch[128]; var results=new SpacecraftPhysicalEventMotion[128]; var random=new Random(61014);
        for(var i=0;i<epochs.Length;i++){epochs[i]=E(random.Next(0,30000),random.Next(1,1000),1001);results[i]=Evaluate(f.State,epochs[i]);}
        for(var i=epochs.Length-1;i>=0;i--)
        {
            var value=Evaluate(f.State,epochs[i]); Check(Bits(value,results[i]),"reverse query order preserves bits");
            Check(SpacecraftPhysicalEventTranslationEvaluator.TryEvaluate(l,Mass,epochs[i],out var p,out var v)==SpacecraftTranslationStatus.Success && Bits(value.PositionRoot,p) && Bits(value.VelocityRoot,v),"linear epoch coherence");
            Check(SpacecraftPhysicalEventRotationEvaluator.TryEvaluate(r,epochs[i],out var q,out var w,out _)==SpacecraftRigidBodyRotationEvaluationStatus.Success && Bits(value.BodyToRoot,q) && Bits(value.AngularVelocityBody,w),"angular epoch coherence");
            Check(value.Revision==before.Revision && value.RootFrame==Root && value.Spacecraft==Craft,"one source identity/revision");
        }
        for(var i=0;i<512;i++){var index=random.Next(epochs.Length);Check(Bits(Evaluate(f.State,epochs[index]),results[index]),"seeded arrival replay");}
        Check(f.State.CreateView()==before && f.State.CreateView().Spacecraft.TryGetTranslation(Craft,out var afterLinear,out var props) && afterLinear==l && props==Mass &&
            f.State.CreateView().Spacecraft.TryGetRigidBody(Craft,out var afterAngular) && afterAngular==r,"authority segments untouched");
        Check(f.Clock.CurrentTime==clock && f.Clock.PendingSimulationDebt==debt && f.Clock.Timeline.Revision==timelineRevision && f.Clock.Timeline.TryPeekPending(out var afterPending) && afterPending==pending && f.Engine.ProcessedCount==historyCount && f.Engine.ProcessedContactImpulseCount==0 && f.Engine.ProcessedRigidBodyTorqueCount==torqueCount && f.Engine.ProcessedSpacecraftForceCount==0,"clock timeline history untouched");
        var retained=results[0]; var saved=retained;
        Check(f.Engine.ExecuteCanonicalPendingEvent().Committed,"existing canonical authority replacement");
        var fresh=Evaluate(f.State,epochs[0]);
        Check(Bits(retained,saved) && fresh.Revision!=retained.Revision && fresh.VelocityRoot!=retained.VelocityRoot,"immutable result retains old provenance; not a current mutation receipt");
        var witness=Fixture(Linear with {PositionRoot=Double3.Zero,VelocityRoot=new(8,0,0),ConstantForceRoot=Double3.Zero},Angular with {AngularVelocityBody=Double3.Zero,ConstantBodyTorque=Double3.Zero});
        var fraction=Evaluate(witness.State,E(0,15625,32768)); var floor=Evaluate(witness.State,E(0));
        Check(fraction.PositionRoot!=floor.PositionRoot,"never evaluate fractional request at floor");
        Check(!RuntimeHelpers.IsReferenceOrContainsReferences<SpacecraftPhysicalEventMotion>(),"value result retains no live state or authority reference");
    }

    private static void Allocation()
    {
        var f=Fixture(); var view=f.State.CreateView(); var e=E(20000,1,3); var good=true; double checksum=0;
        for(var i=0;i<1024;i++)good&=SpacecraftPhysicalEventMotionEvaluator.TryEvaluate(view,Craft,e,out _)==SpacecraftTranslationStatus.Success;
        using var measure=new OrdinaryAllocationMeasurement("exact-event-spacecraft-motion");
        for(var i=0;i<8192;i++){good&=SpacecraftPhysicalEventMotionEvaluator.TryEvaluate(view,Craft,e,out var value)==SpacecraftTranslationStatus.Success; checksum+=value.PositionRoot.X+value.BodyToRoot.W;}
        var bytes=measure.Complete(); OrdinaryAllocationMeasurement.RequireZero(bytes,"exact-event-spacecraft-motion");
        Check(good,"allocation workload completed"); Check(checksum>0,"allocation workload checksum");
    }

    internal static void Performance()
    {
        // Predeclared: 41 samples x 256 calls/case after 4096 warmup calls. Timing and checked allocation are separate.
        foreach(var analytic in new[]{true,false})
        {
            var r=analytic?Angular with {PrincipalInertia=new(2,2,2),ConstantBodyTorque=Double3.Zero}:Angular;
            var f=Fixture(Linear,r); var view=f.State.CreateView();
            var graph=new ReferenceFrameGraphBuilder(); graph.Add(new ReferenceFrameNode(Root,null,ReferenceFrameKind.Ecl,"Root"));
            var definitions=new SpacecraftDefinition[32]; var rotations=new SpacecraftRigidBodyRotationState[32];
            var translations=new SpacecraftTranslationState[32]; var properties=new SpacecraftPhysicalProperties[32];
            for(var i=0;i<32;i++)
            {
                var id=new SpacecraftId((ulong)(100+i)); var body=new ReferenceFrameId(1000+i);
                graph.Add(new ReferenceFrameNode(body,Root,ReferenceFrameKind.Ccf,"Batch craft"));
                definitions[i]=new(id,Root,body,"Batch craft"); rotations[i]=r with {Spacecraft=id,AngularVelocityBody=r.AngularVelocityBody*(1+i/32d)};
                translations[i]=Linear with {Spacecraft=id,PositionRoot=Linear.PositionRoot+new Double3(i*100,0,0)}; properties[i]=Mass;
            }
            Check(SpacecraftStateStore.TryCreateTranslating(definitions,rotations,properties,translations,graph.Build(),out var batchStore,out _),"batch authority setup");
            var batchView=new SimulationState(spacecraft:batchStore).CreateView();
            foreach(var mode in new[]{0,1,2})
            {
                var epochs=Enumerable.Range(0,32).Select(i=>E(mode==2?(i%4)*10000:20000,1,3)).ToArray();
                var samples=new double[41]; double checksum=0;
                for(var i=0;i<4096;i++)checksum+=One(i);
                for(var sample=0;sample<samples.Length;sample++)
                {
                    var start=Stopwatch.GetTimestamp(); for(var i=0;i<256;i++)checksum+=One(i);
                    samples[sample]=(Stopwatch.GetTimestamp()-start)*(1e9/Stopwatch.Frequency)/256;
                }
                Array.Sort(samples); Check(double.IsFinite(checksum)&&checksum>0,"performance workload");
                Console.WriteLine($"EXACT_EVENT_PERF model={(analytic?"analytical":"RK4")} mode={(mode==0?"canonical":mode==1?"rational":"batch32")} samples=41 calls_per_sample=256 median_ns={samples[20]:F3} p95_ns={samples[38]:F3} p99_ns={samples[40]:F3}");
                using(var measurement=new OrdinaryAllocationMeasurement("exact-event-performance-case"))
                {
                    for(var i=0;i<8192;i++)checksum+=One(i);
                    var bytes=measurement.Complete();
                    OrdinaryAllocationMeasurement.RequireZero(bytes,"exact-event-performance-case");
                }
                Check(double.IsFinite(checksum),"allocation performance checksum");
                double One(int i)
                {
                    if(mode==0){Check(SpacecraftPhysicalEventMotionEvaluator.TryEvaluate(view,Craft,PhysicalEventEpoch.FromCanonical(new(20000)),out var value)==SpacecraftTranslationStatus.Success,"canonical wrapper perf status");return value.PositionRoot.X+value.BodyToRoot.W;}
                    var index=mode==2?i%32:0;
                    Check(SpacecraftPhysicalEventMotionEvaluator.TryEvaluate(mode==2?batchView:view,mode==2?definitions[index].Id:Craft,epochs[index],out var other)==SpacecraftTranslationStatus.Success,"rational perf status"); return other.PositionRoot.X+other.BodyToRoot.W;
                }
            }
        }
        Console.WriteLine($"EXACT_EVENT_SIZE canonical={Unsafe.SizeOf<SpacecraftMotion>()} rational={Unsafe.SizeOf<SpacecraftPhysicalEventMotion>()} duration={Unsafe.SizeOf<PhysicalEventDuration>()}");
        Allocation(); OrdinaryAllocationMeasurement.PositiveControl();
    }
}
