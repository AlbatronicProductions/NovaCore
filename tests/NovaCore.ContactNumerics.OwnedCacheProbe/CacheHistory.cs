using NovaCore.Simulation.Spacecraft.Actuation;
using NovaCore.Simulation.Spacecraft.Resources;

// Private numerical prerequisite. No canonical object, resource or actuator capability is held.
internal enum DurationOrigin { None, ActualSolverBinary, ExactEvent }
internal enum PieceKind { Preparation, Powered, Coast }
internal readonly record struct CacheDuration
{
    internal DurationOrigin Origin { get; }
    internal PropellantDuration Exact { get; }
    internal ulong SolverBits { get; }
    internal PoweredBinaryScale Numerical { get; }
    private CacheDuration(DurationOrigin origin,PropellantDuration exact,ulong bits,PoweredBinaryScale numerical)
    {Origin=origin;Exact=exact;SolverBits=bits;Numerical=numerical;}
    internal bool Valid=>Origin!=DurationOrigin.None&&double.IsFinite(Numerical.Significand)&&
        Numerical.Significand>=1&&Numerical.Significand<2&&Numerical.Exponent is >=-4096 and <=0;
    internal static bool FromSolver(double actual,out CacheDuration result)
    {
        result=default;if(!double.IsFinite(actual)||actual<=0||actual>=1)return false;
        var e=Math.ILogB(actual);result=new(DurationOrigin.ActualSolverBinary,default,
            BitConverter.DoubleToUInt64Bits(actual),new(Math.ScaleB(actual,-e),e));return result.Valid;
    }
    internal static bool FromExact(in PropellantDuration exact,out CacheDuration result)
    {
        result=default;if(!exact.IsValid||exact.IsZero||!PoweredFlightNumerics.TrySeconds(exact,out var scale))return false;
        result=new(DurationOrigin.ExactEvent,exact,0,scale);return result.Valid;
    }
}
internal readonly record struct CacheIdentity(long World,int Body,long BodyGeneration,int Constraint,long ManifoldGeneration)
{
    internal bool Valid=>World>0&&Body>=0&&BodyGeneration>0&&Constraint>=0&&ManifoldGeneration>0;
}
internal readonly record struct Features(int A,int B,int C,int D)
{
    internal int At(int i)=>i switch{0=>A,1=>B,2=>C,3=>D,_=>throw new ArgumentOutOfRangeException(nameof(i))};
    internal bool Unique=>A!=B&&A!=C&&A!=D&&B!=C&&B!=D&&C!=D;
}
internal readonly record struct CacheVector(Scaled N0,Scaled N1,Scaled N2,Scaled N3,Scaled T0,Scaled T1,Scaled Twist)
{
    internal Scaled At(int i)=>i switch{0=>N0,1=>N1,2=>N2,3=>N3,4=>T0,5=>T1,6=>Twist,_=>throw new ArgumentOutOfRangeException(nameof(i))};
    internal static CacheVector From(ReadOnlySpan<double> v)=>new(Scaled.From(v[0]),Scaled.From(v[1]),Scaled.From(v[2]),
        Scaled.From(v[3]),Scaled.From(v[4]),Scaled.From(v[5]),Scaled.From(v[6]));
    internal static CacheVector FromScales(ReadOnlySpan<Scaled> v)=>new(v[0],v[1],v[2],v[3],v[4],v[5],v[6]);
    internal bool Valid
    {
        get
        {
            for(var i=0;i<7;i++)
            {
                var x=At(i);var a=Math.Abs(x.Mantissa);
                if(!double.IsFinite(a)||(a!=0&&(a<1||a>=2))||x.Exponent is <-16384 or >16384||
                    (i<4&&x.Mantissa<0))return false;
            }
            return true;
        }
    }
    internal void Project(Span<double> values){for(var i=0;i<7;i++)values[i]=At(i).Value;}
    internal bool MatchesTransport(ReadOnlySpan<double> actual)
    {
        if(actual.Length!=7)return false;
        for(var i=0;i<7;i++)if(BitConverter.DoubleToUInt64Bits(actual[i])!=
            BitConverter.DoubleToUInt64Bits((double)(float)At(i).Value))return false;
        return true;
    }
}
internal static class DurationInitialization
{
    internal static bool TryScale(in CacheVector cache,in CacheDuration previous,in CacheDuration next,out CacheVector result)
    {
        result=default;if(!cache.Valid||!previous.Valid||!next.Valid)return false;
        Span<Scaled> values=stackalloc Scaled[7];
        values.Clear();
        try
        {
            for(var i=0;i<7;i++)
            {
                var old=cache.At(i);if(old.Mantissa==0)continue;
                // Form the complete signed product before projection, never a projected duration ratio.
                var m=Scaled.From(old.Mantissa*next.Numerical.Significand/previous.Numerical.Significand);
                values[i]=new(m.Mantissa,checked(old.Exponent+next.Numerical.Exponent-previous.Numerical.Exponent+m.Exponent));
            }
        }
        catch(OverflowException){return false;}
        result=CacheVector.FromScales(values);return result.Valid;
    }
}
internal enum CacheStatus { Ready,WrongThread,Busy,Invalidated,IdentityMismatch,StalePiece,InvalidDuration,
    UnsupportedSupport,CacheTransportChanged,ArithmeticFailure,UnrepresentableConsumer,InstallMismatch }
internal readonly record struct AcceptedCache(CacheIdentity Identity,Features Features,D3 Normal,long Piece,PieceKind Kind,
    CacheDuration Duration,CacheVector Impulses,Load Load,PieceKernel.State Endpoint);

internal sealed class CacheHistory
{
    private readonly int thread=Environment.CurrentManagedThreadId;
    private AcceptedCache accepted;
    private AcceptedCache installing;
    private Prepared installingPreparation;
    private bool invalidated,busy;
    internal AcceptedCache Snapshot=>accepted;
    internal bool Invalidated=>invalidated;
    internal readonly struct Prepared
    {
        private readonly CacheHistory owner;
        private readonly AcceptedCache source;
        internal readonly CacheDuration Duration;
        internal readonly PieceKind Kind;
        internal readonly Load Load;
        internal readonly CacheVector Scaled;
        internal readonly CommonPrediction Common;
        internal Prepared(CacheHistory owner,AcceptedCache source,CacheDuration duration,PieceKind kind,Load load,
            CacheVector scaled,CommonPrediction common)
        {this.owner=owner;this.source=source;Duration=duration;Kind=kind;Load=load;Scaled=scaled;Common=common;}
        internal bool Matches(CacheHistory other)=>ReferenceEquals(owner,other)&&source==other.accepted;
        internal bool SameAs(in Prepared other)=>ReferenceEquals(owner,other.owner)&&source==other.source&&
            Duration==other.Duration&&Kind==other.Kind&&Load==other.Load&&Scaled==other.Scaled&&Common==other.Common;
        internal long Piece=>source.Piece+1;
        internal CacheDuration PreviousDuration=>source.Duration;
        internal void Project(Span<double> target)
        {Scaled.Project(target);for(var i=0;i<4;i++)target[i]=Common.Materialize(target[i]);}
    }
    internal CacheHistory(in AcceptedCache installed)
    {
        if(!installed.Identity.Valid||!installed.Features.Unique||!installed.Duration.Valid||!installed.Impulses.Valid||
            !installed.Load.Finite||installed.Piece<0)throw new ArgumentException("Invalid installed cache provenance");
        accepted=installed;
    }
    internal void Invalidate()
    {if(Environment.CurrentManagedThreadId!=thread)throw new InvalidOperationException("Wrong cache owner");invalidated=true;busy=false;}
    private CacheStatus Access()
    {
        if(Environment.CurrentManagedThreadId!=thread)return CacheStatus.WrongThread;
        if(busy)return CacheStatus.Busy;
        return invalidated?CacheStatus.Invalidated:CacheStatus.Ready;
    }
    internal CacheStatus Prepare(CacheIdentity identity,Features features,long piece,PieceKind kind,CacheDuration duration,
        ReadOnlySpan<double> observedTransport,ReadOnlySpan<NormalRow> rows,ReadOnlySpan<double> depths,
        in PieceKernel.State source,in InverseBody body,in Load load,double omega,out Prepared prepared)
    {
        prepared=default;var status=Access();if(status!=CacheStatus.Ready)return status;
        if(identity!=accepted.Identity||features!=accepted.Features)return CacheStatus.IdentityMismatch;
        if(accepted.Piece==long.MaxValue||piece!=accepted.Piece+1||kind==PieceKind.Preparation)return CacheStatus.StalePiece;
        if(kind is not PieceKind.Powered and not PieceKind.Coast)return CacheStatus.StalePiece;
        if(!duration.Valid||duration.Numerical.Value>16667d/1e6)return CacheStatus.InvalidDuration;
        if(source!=accepted.Endpoint)return CacheStatus.IdentityMismatch;
        if(!accepted.Impulses.MatchesTransport(observedTransport))return CacheStatus.CacheTransportChanged;
        if(!Supported(features,rows,depths,source,body,accepted.Load,load,accepted.Impulses,accepted.Normal,omega))
            return CacheStatus.UnsupportedSupport;
        if(!DurationInitialization.TryScale(accepted.Impulses,accepted.Duration,duration,out var scaled))return CacheStatus.ArithmeticFailure;
        Span<double> projection=stackalloc double[7];scaled.Project(projection);
        for(var i=0;i<7;i++)if(!double.IsFinite(projection[i])||projection[i]==0&&scaled.At(i).Mantissa!=0)
            return CacheStatus.UnrepresentableConsumer;
        CommonPrediction common=default;
        if(accepted.Load!=load)
        {
            var prediction=CommonNormal.Prepare(rows,projection[..4],body,accepted.Load,load,duration.Numerical,omega,out common);
            if(prediction!=PredictionStatus.Ready)return CacheStatus.ArithmeticFailure;
        }
        prepared=new(this,accepted,duration,kind,load,scaled,common);return CacheStatus.Ready;
    }
    private static bool Supported(Features features,ReadOnlySpan<NormalRow> rows,ReadOnlySpan<double> depths,
        in PieceKernel.State source,in InverseBody body,in Load previous,in Load next,in CacheVector cache,D3 normal,double omega)
    {
        if(rows.Length!=4||depths.Length!=4||!body.Valid||!previous.Finite||!next.Finite||omega<=0||!double.IsFinite(omega))return false;
        var v=new D3(source.Linear.X,source.Linear.Y,source.Linear.Z);var w=new D3(source.Angular.X,source.Angular.Y,source.Angular.Z);
        if(!v.Finite||!w.Finite)return false;
        var a0=previous.Gravity+body.Mass*previous.Force;var a1=next.Gravity+body.Mass*next.Force;
        var w0=body.Apply(previous.Torque);var w1=body.Apply(next.Torque);
        if(!a0.Finite||!a1.Finite||!w0.Finite||!w1.Finite)return false;
        var center=default(D3);var total=0d;var twistCap=0d;
        for(var i=0;i<4;i++)center+=rows[i].Lever;
        center=.25*center;
        for(var i=0;i<4;i++)
        {
            var row=rows[i];
            if(row.Identity!=features.At(i)||row.Normal!=normal||!row.Normal.Finite||!row.Lever.Finite||
                Math.Abs(D3.Dot(row.Normal,row.Normal)-1)>8*Math.ScaleB(1d,-23)||
                !double.IsFinite(depths[i])||depths[i]<=0||depths[i]>.020||
                D3.Dot(row.Normal,a0)+D3.Dot(row.Moment,w0)>=0||
                D3.Dot(row.Normal,a1)+D3.Dot(row.Moment,w1)>=0||
                Math.Abs(D3.Dot(row.Normal,v)+D3.Dot(row.Moment,w))>.0005/(16667d/1e6)||cache.At(i).Mantissa<=0)return false;
            total+=cache.At(i).Value;twistCap+=.125*cache.At(i).Value*(row.Lever-center).Length;
        }
        var t0=cache.T0.Value;var t1=cache.T1.Value;
        return Math.Sqrt(t0*t0+t1*t1)<.125*total&&Math.Abs(cache.Twist.Value)<twistCap;
    }
    internal CacheStatus BeginInstall(in Prepared prepared,in CacheVector installed,in PieceKernel.State endpoint)
    {
        var status=Access();if(status!=CacheStatus.Ready)return status;
        if(!prepared.Matches(this))return CacheStatus.StalePiece;
        if(!installed.Valid||!double.IsFinite(endpoint.Linear.Length)||!double.IsFinite(endpoint.Angular.Length))return CacheStatus.InstallMismatch;
        installing=accepted with{Piece=prepared.Piece,Kind=prepared.Kind,Duration=prepared.Duration,
            Impulses=installed,Load=prepared.Load,Endpoint=endpoint};
        installingPreparation=prepared;
        // Any unexpected failure from here poisons private continuation; no ordinary rollback claim.
        busy=true;invalidated=true;return CacheStatus.Ready;
    }
    internal CacheStatus CompleteInstall(in Prepared prepared)
    {
        if(Environment.CurrentManagedThreadId!=thread)return CacheStatus.WrongThread;
        if(!busy)return CacheStatus.InstallMismatch;
        if(!prepared.Matches(this)||!prepared.SameAs(installingPreparation))
        {busy=false;invalidated=true;return CacheStatus.InstallMismatch;}
        accepted=installing;
        busy=false;invalidated=false;return CacheStatus.Ready;
    }
}
