using System.Runtime.CompilerServices;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Spacecraft.Contact;

internal enum PrivatePropagationStatus : byte { Unresolved, Ready, Unsupported, Stale }
internal enum PrivatePropagationFailure : byte
{
    None, InvalidSource, InvalidRequest, ChangedAuthority, UnsupportedTarget, RootTargetOrder,
    UnsupportedArithmetic, NonFinite, RotationWorkLimit, EndpointResolution,
}
internal enum PrivatePropagationCoverage : byte { Unknown }

/// <summary>Maximum absolute component errors; attitude is sign-invariant unit-quaternion chordal distance.</summary>
internal readonly record struct PrivatePropagationRequest(double PositionMetres, double VelocityMetresPerSecond,
    double AngularVelocityRadiansPerSecond, double AttitudeChordal)
{
    internal bool IsValid => Positive(PositionMetres) && Positive(VelocityMetresPerSecond) &&
        Positive(AngularVelocityRadiansPerSecond) && Positive(AttitudeChordal) && AttitudeChordal < 1;
    private static bool Positive(double x) => double.IsFinite(x) && x > 0;
}

internal readonly record struct PropagationQuaternion(FloridaBound X, FloridaBound Y, FloridaBound Z, FloridaBound W)
{
    internal bool IsFinite => X.IsFinite && Y.IsFinite && Z.IsFinite && W.IsFinite;
    internal FloridaBound Norm
    {
        get { var s = X.Square() + Y.Square() + Z.Square() + W.Square(); return new FloridaBound(Math.Max(0, s.Lower), s.Upper).Sqrt(); }
    }
    internal static PropagationQuaternion NormalizedMeaning(DoubleQuaternion q)
    { var v = new PropagationQuaternion(q.X, q.Y, q.Z, q.W); var n = v.Norm; return new(v.X / n, v.Y / n, v.Z / n, v.W / n); }
    internal PropagationQuaternion Expand(double r) => new(X.Expand(r), Y.Expand(r), Z.Expand(r), W.Expand(r));
    public static PropagationQuaternion operator +(PropagationQuaternion a, PropagationQuaternion b) => new(a.X+b.X,a.Y+b.Y,a.Z+b.Z,a.W+b.W);
    public static PropagationQuaternion operator -(PropagationQuaternion a, PropagationQuaternion b) => new(a.X-b.X,a.Y-b.Y,a.Z-b.Z,a.W-b.W);
    public static PropagationQuaternion operator *(PropagationQuaternion a, FloridaBound b) => new(a.X*b,a.Y*b,a.Z*b,a.W*b);
    public static PropagationQuaternion operator /(PropagationQuaternion a, double b) => new(a.X/b,a.Y/b,a.Z/b,a.W/b);
    internal static PropagationQuaternion DerivativeProduct(PropagationQuaternion q, FloridaVector w) =>
        new(q.W*w.X+q.Y*w.Z-q.Z*w.Y, q.W*w.Y+q.Z*w.X-q.X*w.Z,
            q.W*w.Z+q.X*w.Y-q.Y*w.X, -(q.X*w.X+q.Y*w.Y+q.Z*w.Z));
}

internal readonly record struct PrivateTranslationEnclosure(FloridaVector Position, FloridaVector Velocity);
internal readonly record struct PrivateRotationEnclosure(PropagationQuaternion PolynomialAttitude, FloridaVector PolynomialSpin,
    PropagationQuaternion Attitude, FloridaVector Spin, double SpinRemainder, double AttitudeRemainder,
    double GlobalSpinBound, double EulerCoefficientBound, int ConvolutionTerms);
internal readonly record struct PrivatePropagationEndpoint(Double3 PositionRoot, Double3 VelocityRoot,
    DoubleQuaternion OrientationBodyToRoot, Double3 AngularVelocityBody,
    PrivateTranslationEnclosure Translation, PrivateRotationEnclosure Rotation,
    Double3 PositionError, Double3 VelocityError, Double3 AngularVelocityError, double AttitudeChordalError);

/// <summary>Snapshot data, not a publication capability. All endpoint bits refer to Initial.Root's same alpha and Target.</summary>
internal readonly record struct PrivateCanonicalStateValues(PrivatePostImpactStateValues Initial, SimulationInstant Target,
    FloridaBound RemainderSeconds, PrivatePropagationEndpoint Endpoint, PrivatePropagationRequest Request,
    PrivatePropagationCoverage EventCoverage, int ArithmeticVersion);
internal readonly record struct PrivatePropagationResult(PrivatePropagationStatus Status, PrivatePropagationFailure Failure,
    FloridaContactProvider.Proof.PrivateCanonicalState State = default);

/// <summary>
/// Version 1: degree eleven, uniform twelfth-derivative remainder, no substeps/retries/refinement.
/// Only numerical kernels: constructible inputs never grant provider, event-coverage or publication authority.
/// </summary>
internal static class PrivatePropagationMath
{
    internal const int Version = 1;
    internal const int Degree = 11;
    internal const int RemainderOrder = 12;
    internal const int ConvolutionTerms = 66;
    internal const int WorkspaceBytes = 1760; // 12 vectors + 12 quaternions + two 13-bound majorant arrays.

    internal static PrivatePropagationFailure Translation(in SpacecraftTranslationState s, double mass, Double3 plus,
        FloridaBound root, SimulationInstant target, out PrivateTranslationEnclosure result)
    {
        result = default;
        if (!s.PositionRoot.IsFinite || !s.VelocityRoot.IsFinite || !s.ConstantForceRoot.IsFinite || !plus.IsFinite ||
            !double.IsFinite(mass) || mass <= 0 || !root.IsFinite) return PrivatePropagationFailure.InvalidSource;
        // Convert each integral epoch outward, never perform an overflowing integral subtraction.
        var t = FloridaBound.Integer(target.Ticks) / SimulationInstant.TicksPerSecond;
        var tau = t - FloridaBound.Integer(s.Epoch.Ticks) / SimulationInstant.TicksPerSecond;
        var d = t - root;
        var p = FloridaVector.From(s.PositionRoot); var v = FloridaVector.From(s.VelocityRoot);
        FloridaVector position, velocity;
        if (s.ConstantForceRoot == Double3.Zero && plus == s.VelocityRoot)
        {
            // Exact symbolic cancellation. Do not multiply widened numerical zero by a root interval.
            position = p + v * tau; velocity = FloridaVector.From(plus);
        }
        else
        {
            var a = s.ConstantForceRoot == Double3.Zero ? default : FloridaVector.From(s.ConstantForceRoot) / mass;
            var preVelocity = v + a * tau;
            var prePosition = p + v * tau + a * (tau.Square() / 2);
            position = prePosition + (FloridaVector.From(plus) - preVelocity) * d + a * d.Square();
            velocity = FloridaVector.From(plus) + a * d;
        }
        if (!position.IsFinite || !velocity.IsFinite) return PrivatePropagationFailure.NonFinite;
        result = new(position, velocity); return PrivatePropagationFailure.None;
    }

    internal static PrivatePropagationFailure Rotation(DoubleQuaternion initial, Double3 spin,
        PrincipalMomentsOfInertia inertia, FloridaBound d, out PrivateRotationEnclosure result)
    {
        result = default;
        if (!initial.IsFinite || !spin.IsFinite || !inertia.IsFinite || !inertia.IsStrictlyPositive ||
            !d.IsFinite || d.Lower < 0) return PrivatePropagationFailure.InvalidSource;
        if (d.Upper > 1) return PrivatePropagationFailure.RotationWorkLimit;
        var q0 = PropagationQuaternion.NormalizedMeaning(initial);
        if (!q0.IsFinite) return PrivatePropagationFailure.InvalidSource;
        var c = new FloridaVector(((FloridaBound)inertia.Y-inertia.Z)/inertia.X,
            ((FloridaBound)inertia.Z-inertia.X)/inertia.Y, ((FloridaBound)inertia.X-inertia.Y)/inertia.Z);
        var energy = ((FloridaBound)spin.X).Square()*inertia.X + ((FloridaBound)spin.Y).Square()*inertia.Y +
            ((FloridaBound)spin.Z).Square()*inertia.Z;
        var energyBound = new FloridaBound(0, energy.Upper);
        var ox = NonnegativeSqrt(energyBound / inertia.X);
        var oy = NonnegativeSqrt(energyBound / inertia.Y);
        var oz = NonnegativeSqrt(energyBound / inertia.Z);
        var omega = Math.Max(ox.Upper, Math.Max(oy.Upper, oz.Upper));
        var coefficient = Math.Max(c.X.Magnitude, Math.Max(c.Y.Magnitude, c.Z.Magnitude));
        if (!double.IsFinite(omega) || !double.IsFinite(coefficient)) return PrivatePropagationFailure.NonFinite;
        if (!RemainderBounds(omega, coefficient, d.Upper, out var rw, out var rq)) return PrivatePropagationFailure.RotationWorkLimit;
        Span<FloridaVector> w = stackalloc FloridaVector[Degree+1];
        Span<PropagationQuaternion> q = stackalloc PropagationQuaternion[Degree+1];
        w[0] = FloridaVector.From(spin); q[0] = q0;
        for (var n=0; n<Degree; n++)
        {
            FloridaVector ws = default; PropagationQuaternion qs = default;
            for (var k=0; k<=n; k++)
            {
                var a=w[k]; var b=w[n-k];
                ws += new FloridaVector(c.X*a.Y*b.Z, c.Y*a.Z*b.X, c.Z*a.X*b.Y);
                qs += PropagationQuaternion.DerivativeProduct(q[k], b);
            }
            w[n+1]=ws/(n+1); q[n+1]=qs/(2*(n+1));
        }
        var wp=w[Degree]; var qp=q[Degree];
        for(var n=Degree-1; n>=0; n--) { wp=wp*d+w[n]; qp=qp*d+q[n]; }
        var angular=wp.Expand(rw); var attitude=qp.Expand(rq);
        if (!angular.IsFinite || !attitude.IsFinite) return PrivatePropagationFailure.NonFinite;
        result=new(qp,wp,attitude,angular,rw,rq,omega,coefficient,ConvolutionTerms);
        return PrivatePropagationFailure.None;
    }

    private static FloridaBound NonnegativeSqrt(FloridaBound x) => new FloridaBound(Math.Max(0,x.Lower),x.Upper).Sqrt();

    /// <summary>
    /// Uniform derivative bounds at EVERY intermediate time, from energy and unit q. b_n bounds
    /// omega^(n)/n!, a_n bounds each q^(n)/n!. Taylor-Lagrange remainder uses n=12, not step doubling.
    /// Closed-form independent test oracles: b_n=O^(n+1) C^n;
    /// a_n=O^n/n! product(j=0..n-1)(3/2+jC). No division by C in the spherical case.
    /// </summary>
    internal static bool RemainderBounds(double omega, double coefficient, double seconds, out double spin, out double attitude)
    {
        spin=attitude=double.NaN;
        if (!double.IsFinite(omega)||omega<0||!double.IsFinite(coefficient)||coefficient<0||
            !double.IsFinite(seconds)||seconds<0||seconds>1) return false;
        Span<FloridaBound> b=stackalloc FloridaBound[RemainderOrder+1];
        Span<FloridaBound> a=stackalloc FloridaBound[RemainderOrder+1];
        b[0]=omega; a[0]=1;
        for(var n=0;n<RemainderOrder;n++)
        {
            FloridaBound bs=0, qs=0;
            for(var k=0;k<=n;k++) { bs+=b[k]*b[n-k]; qs+=a[k]*b[n-k]; }
            // Nonnegative upper boxes keep signed rounding noise out of this positive majorant.
            b[n+1]=new(0,(bs*coefficient/(n+1)).Upper);
            a[n+1]=new(0,(qs*3/(2*(n+1))).Upper);
        }
        var power=FloridaBound.Pow(seconds,RemainderOrder);
        spin=(b[RemainderOrder]*power).Upper; attitude=(a[RemainderOrder]*power).Upper;
        return double.IsFinite(spin)&&spin>=0&&double.IsFinite(attitude)&&attitude>=0;
    }

    internal static PrivatePropagationFailure Evaluate(in PrivatePostImpactStateValues source, SimulationInstant target,
        in PrivatePropagationRequest request, out FloridaBound remainder, out PrivatePropagationEndpoint result)
    {
        remainder=default; result=default;
        if (!request.IsValid) return PrivatePropagationFailure.InvalidRequest;
        if (!CertifiedPostImpactVelocityMath.ArithmeticSupported()) return PrivatePropagationFailure.UnsupportedArithmetic;
        if (target!=source.SourceEnd) return PrivatePropagationFailure.UnsupportedTarget;
        if (!source.Properties.IsValid || source.FrozenSourceRotation.ConstantBodyTorque!=Double3.Zero ||
            !source.PoseRootEnclosure.IsFinite || source.SourceStart>=source.SourceEnd)
            return PrivatePropagationFailure.InvalidSource;
        // Checked provider admission proves alpha is STRICTLY inside its one-second source cell.
        // Intersection uses that theorem, not a selected alpha or convenient numerical clamping.
        var raw=FloridaBound.Integer(target.Ticks)/SimulationInstant.TicksPerSecond-source.PoseRootEnclosure;
        var d=new FloridaBound(Math.Max(0,raw.Lower),Math.Min(1,raw.Upper));
        if (!d.IsFinite || raw.Upper<=0) return PrivatePropagationFailure.RootTargetOrder;
        var f=Translation(source.FrozenSourceTranslation,source.Properties.MassKilograms,source.InitialLinearVelocityRoot,
            source.PoseRootEnclosure,target,out var linear);
        if(f!=PrivatePropagationFailure.None)return f;
        f=Rotation(source.FrozenSourceRotation.OrientationLocalToParent,source.InitialAngularVelocityBody,
            source.FrozenSourceRotation.PrincipalInertia,d,out var angular);
        if(f!=PrivatePropagationFailure.None)return f;
        if(angular.SpinRemainder>request.AngularVelocityRadiansPerSecond || angular.AttitudeRemainder>request.AttitudeChordal)
            return PrivatePropagationFailure.RotationWorkLimit;
        var x=Center(linear.Position); var v=Center(linear.Velocity); var w=Center(angular.Spin);
        var qraw=new DoubleQuaternion(Center(angular.Attitude.X),Center(angular.Attitude.Y),Center(angular.Attitude.Z),Center(angular.Attitude.W));
        if(SpacecraftRigidBodyRotationEvaluator.TryCanonicalize(qraw,out var q)!=SpacecraftRigidBodyRotationEvaluationStatus.Success)
            return PrivatePropagationFailure.NonFinite;
        var meaning=PropagationQuaternion.NormalizedMeaning(q);
        var chord=Math.Min((angular.Attitude-meaning).Norm.Upper,(angular.Attitude+meaning).Norm.Upper);
        var xe=Error(linear.Position,x);var ve=Error(linear.Velocity,v);var we=Error(angular.Spin,w);
        if(!Fits(xe,request.PositionMetres)||!Fits(ve,request.VelocityMetresPerSecond)||
            !Fits(we,request.AngularVelocityRadiansPerSecond)||!double.IsFinite(chord)||chord>request.AttitudeChordal)
            return PrivatePropagationFailure.EndpointResolution;
        remainder=d;result=new(x,v,q,w,linear,angular,xe,ve,we,chord);return PrivatePropagationFailure.None;
    }

    // Endpoint-output selection only. Explicit rounding boundaries; never used to choose alpha.
    [MethodImpl(MethodImplOptions.NoInlining)] private static double Half(double x)=>x*.5;
    [MethodImpl(MethodImplOptions.NoInlining)] private static double Add(double a,double b)=>a+b;
    private static double Center(FloridaBound b) { var x=Add(Half(b.Lower),Half(b.Upper));return x==0?0:x; }
    private static Double3 Center(FloridaVector b)=>new(Center(b.X),Center(b.Y),Center(b.Z));
    private static Double3 Error(FloridaVector b,Double3 v)=>new((b.X-v.X).Magnitude,(b.Y-v.Y).Magnitude,(b.Z-v.Z).Magnitude);
    private static bool Fits(Double3 e,double limit)=>e.IsFinite&&e.X<=limit&&e.Y<=limit&&e.Z<=limit;
}

internal sealed partial class FloridaContactProvider
{
    internal readonly partial struct Proof
    {
        internal PrivatePropagationResult PreparePrivatePropagation(in PrivatePostImpactState source,
            in PostImpactVelocity realizationKey,in PrivatePostImpactPoseRequest poseKey,SimulationInstant target,
            in PrivatePropagationRequest request,in FloridaContactUse current)=>
            PrivateCanonicalState.Prepare(this,source,realizationKey,poseKey,target,request,current);

        /// <summary>Immutable conditional free continuation. Not consumed, event-complete or publishable.</summary>
        internal readonly struct PrivateCanonicalState
        {
            private readonly Proof root;
            private readonly PrivatePostImpactState initial;
            private readonly PrivatePostImpactPoseRequest poseKey;
            private readonly PrivateCanonicalStateValues values;
            private PrivateCanonicalState(Proof root,PrivatePostImpactState initial,PrivatePostImpactPoseRequest poseKey,PrivateCanonicalStateValues values)
            {this.root=root;this.initial=initial;this.poseKey=poseKey;this.values=values;}

            internal PrivatePropagationStatus Read(in Proof expectedRoot,in PrivatePostImpactState expectedInitial,
                in PostImpactVelocity realizationKey,in PrivatePostImpactPoseRequest expectedPoseKey,SimulationInstant target,
                in PrivatePropagationRequest request,in FloridaContactUse current,out PrivateCanonicalStateValues result)
            {
                result=default;
                if(!root.IsRoot||!root.SameResponseInput(expectedRoot)||poseKey!=expectedPoseKey||
                    values.Target!=target||values.Request!=request)return PrivatePropagationStatus.Unsupported;
                var s=initial.Read(expectedRoot,realizationKey,poseKey,current,out var held);
                if(s!=PrivatePostImpactStateStatus.Ready)return Map(s);
                s=expectedInitial.Read(expectedRoot,realizationKey,poseKey,current,out var supplied);
                if(s!=PrivatePostImpactStateStatus.Ready)return Map(s);
                // Proof is opaque and intentionally not IEquatable: never box it via record equality.
                if(!held.Root.SameResponseInput(supplied.Root)||held.PoseRootEnclosure!=supplied.PoseRootEnclosure||
                    held.FrozenSourceTranslation!=supplied.FrozenSourceTranslation||held.FrozenSourceRotation!=supplied.FrozenSourceRotation||
                    held.Properties!=supplied.Properties||held.SourceRevision!=supplied.SourceRevision||
                    held.SourceTimelineRevision!=supplied.SourceTimelineRevision||held.SourceStart!=supplied.SourceStart||held.SourceEnd!=supplied.SourceEnd||
                    held.TerrainAuthority!=supplied.TerrainAuthority||held.PositionRoot!=supplied.PositionRoot||
                    held.InitialLinearVelocityRoot!=supplied.InitialLinearVelocityRoot||held.InitialAngularVelocityBody!=supplied.InitialAngularVelocityBody)
                    return PrivatePropagationStatus.Unsupported;
                result=values;return PrivatePropagationStatus.Ready;
            }

            internal static PrivatePropagationResult Prepare(in Proof root,in PrivatePostImpactState source,
                in PostImpactVelocity realizationKey,in PrivatePostImpactPoseRequest poseKey,SimulationInstant target,
                in PrivatePropagationRequest request,in FloridaContactUse current)
            {
                // The old realization/pose arguments are opaque READ KEYS only. All physical input
                // comes from M14.14; no response selection, root refinement or older-chain reconstruction.
                var s=source.Read(root,realizationKey,poseKey,current,out var initial);
                if(s!=PrivatePostImpactStateStatus.Ready)return new(Map(s),s==PrivatePostImpactStateStatus.Stale?
                    PrivatePropagationFailure.ChangedAuthority:s==PrivatePostImpactStateStatus.Unresolved?
                    PrivatePropagationFailure.UnsupportedArithmetic:PrivatePropagationFailure.InvalidSource);
                var f=PrivatePropagationMath.Evaluate(initial,target,request,out var d,out var endpoint);
                if(f!=PrivatePropagationFailure.None)return new(f is PrivatePropagationFailure.InvalidSource or
                    PrivatePropagationFailure.InvalidRequest or PrivatePropagationFailure.UnsupportedTarget?
                    PrivatePropagationStatus.Unsupported:PrivatePropagationStatus.Unresolved,f);
                var captured=new PrivateCanonicalStateValues(initial,target,d,endpoint,request,PrivatePropagationCoverage.Unknown,PrivatePropagationMath.Version);
                return new(PrivatePropagationStatus.Ready,PrivatePropagationFailure.None,new(root,source,poseKey,captured));
            }
            private static PrivatePropagationStatus Map(PrivatePostImpactStateStatus s)=>s switch
            {PrivatePostImpactStateStatus.Ready=>PrivatePropagationStatus.Ready,PrivatePostImpactStateStatus.Stale=>PrivatePropagationStatus.Stale,
                PrivatePostImpactStateStatus.Unresolved=>PrivatePropagationStatus.Unresolved,_=>PrivatePropagationStatus.Unsupported};
        }
    }
}
