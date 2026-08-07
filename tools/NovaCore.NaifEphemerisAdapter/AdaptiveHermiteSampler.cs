using System.Collections.Immutable;

namespace NovaCore.NaifEphemerisAdapter;

internal readonly record struct AdaptiveSamplingCoverage(long StartEt,long EndEt)
{
    internal bool IsValid=>EndEt>StartEt;
}

internal readonly record struct AdaptiveSamplerInput(int BodyId,int ParentBodyId,long CoverageStart,long CoverageEnd,long InitialCadenceSeconds,double MaximumPositionErrorMetres,double MaximumVelocityErrorMetresPerSecond,long MinimumIntervalSeconds,int MaximumSubdivisionDepth)
{
    internal bool IsValid=>BodyId>=0&&ParentBodyId>=0&&CoverageEnd>CoverageStart&&InitialCadenceSeconds>0&&MaximumPositionErrorMetres>0&&MaximumVelocityErrorMetresPerSecond>0&&MinimumIntervalSeconds>0&&MaximumSubdivisionDepth>=0;
}

internal readonly record struct AdaptiveSourceState(double X,double Y,double Z,double Vx,double Vy,double Vz);
internal readonly record struct AdaptiveKnot(long Et,AdaptiveSourceState State);
internal readonly record struct AdaptiveInterval(long StartEt,long EndEt);
internal readonly record struct AdaptiveSamplingResult(int BodyId,int ParentBodyId,AdaptiveSamplingCoverage Coverage,ImmutableArray<AdaptiveKnot> AcceptedKnots,ImmutableArray<AdaptiveInterval> AcceptedIntervals,int SampleCount,int IntervalCount,double MaximumPositionError,double RmsPositionError,double MaximumVelocityError,double RmsVelocityError,long WorstPositionErrorET,long WorstVelocityErrorET,int MaximumSubdivisionDepth,ulong DeterministicHash);
internal readonly record struct AdaptiveSeedCadenceSearchConfig(long MinimumSeedSeconds,long MaximumSeedSeconds,long ResolutionSeconds){internal bool IsValid=>MinimumSeedSeconds>0&&MaximumSeedSeconds>=MinimumSeedSeconds&&ResolutionSeconds>0&&(MaximumSeedSeconds-MinimumSeedSeconds)%ResolutionSeconds==0;}
internal readonly record struct AdaptiveSeedCadenceSearchResult(long LargestPassingSeedSeconds,long NextFailingSeedSeconds,AdaptiveSamplingResult Sample);
internal readonly record struct AdaptiveBodySamplingConfiguration(string Name,AdaptiveSamplerInput Input);

internal interface IAdaptiveStateSource
{
    bool TryGetState(int bodyId,int parentBodyId,long et,out AdaptiveSourceState state);
}

internal sealed class CspiceRelativeStateSource(CspiceSession session):IAdaptiveStateSource
{
    public bool TryGetState(int bodyId,int parentBodyId,long et,out AdaptiveSourceState state)
    {
        state=default;
        if(!session.TryQuery(bodyId,et,out var body)||!session.TryQuery(parentBodyId,et,out var parent))return false;
        state=new((body.X-parent.X)*1000,(body.Y-parent.Y)*1000,(body.Z-parent.Z)*1000,(body.Vx-parent.Vx)*1000,(body.Vy-parent.Vy)*1000,(body.Vz-parent.Vz)*1000);
        return true;
    }
}

internal static class AdaptiveBodySamplingConfigurations
{
    internal static readonly ImmutableArray<AdaptiveBodySamplingConfiguration> Current=
    [
        new("Moon",new(301,3,0,86400,22500,10,.001,1,20)),
        new("Earth",new(399,3,0,86400,21600,10,.001,1,20)),
        new("Sun",new(10,0,0,86400,86400,10,.001,1,20))
    ];
}

internal static class AdaptiveHermiteSampler
{
    internal static bool TrySample(IAdaptiveStateSource source,AdaptiveSamplerInput input,out AdaptiveSamplingResult result)
    {
        result=default;
        if(source is null||!input.IsValid)return false;
        var cache=new SortedDictionary<long,AdaptiveSourceState>();
        var acceptedKnots=new SortedDictionary<long,AdaptiveSourceState>();
        var acceptedIntervals=new List<AdaptiveInterval>();
        var residuals=new List<Residual>();
        var order=0;
        bool Get(long et,out AdaptiveSourceState state)
        {
            if(cache.TryGetValue(et,out state))return true;
            if(!source.TryGetState(input.BodyId,input.ParentBodyId,et,out state))return false;
            cache.Add(et,state);
            return true;
        }
        var maximumDepth=0;
        bool Walk(long start,long end,int depth)
        {
            if(!Get(start,out var first)||!Get(end,out var last))return false;
            maximumDepth=Math.Max(maximumDepth,depth);
            var pass=true;
            foreach(var numerator in new[]{1,3,5,7})
            {
                var et=start+(end-start)*numerator/8;
                if(!Get(et,out var actual))return false;
                var interpolated=Hermite(first,last,(double)(et-start)/(end-start),end-start);
                var positionError=Distance(interpolated,actual,false);
                var velocityError=Distance(interpolated,actual,true);
                residuals.Add(new(et,positionError,velocityError,order++));
                if(positionError>input.MaximumPositionErrorMetres||velocityError>input.MaximumVelocityErrorMetresPerSecond)pass=false;
            }
            if(pass)
            {
                acceptedKnots[start]=first;
                acceptedKnots[end]=last;
                acceptedIntervals.Add(new(start,end));
                return true;
            }
            if(depth>=input.MaximumSubdivisionDepth||end-start<=input.MinimumIntervalSeconds)return false;
            var midpoint=start+(end-start)/2;
            return Walk(start,midpoint,depth+1)&&Walk(midpoint,end,depth+1);
        }
        for(var start=input.CoverageStart;start<input.CoverageEnd;)
        {
            var end=Math.Min(start+input.InitialCadenceSeconds,input.CoverageEnd);
            if(!Walk(start,end,0))return false;
            start=end;
        }
        residuals.Sort((left,right)=>{var comparison=left.Et.CompareTo(right.Et);return comparison!=0?comparison:left.Order.CompareTo(right.Order);});
        double positionSquares=0,velocitySquares=0,maximumPosition=0,maximumVelocity=0;
        var worstPositionEt=input.CoverageStart;
        var worstVelocityEt=input.CoverageStart;
        foreach(var residual in residuals)
        {
            positionSquares+=residual.Position*residual.Position;
            velocitySquares+=residual.Velocity*residual.Velocity;
            if(residual.Position>maximumPosition){maximumPosition=residual.Position;worstPositionEt=residual.Et;}
            if(residual.Velocity>maximumVelocity){maximumVelocity=residual.Velocity;worstVelocityEt=residual.Et;}
        }
        var knots=acceptedKnots.Select(pair=>new AdaptiveKnot(pair.Key,pair.Value)).ToImmutableArray();
        var intervals=acceptedIntervals.ToImmutableArray();
        var rmsPosition=Math.Sqrt(positionSquares/residuals.Count);
        var rmsVelocity=Math.Sqrt(velocitySquares/residuals.Count);
        var hash=Hash(input,knots,intervals,maximumPosition,rmsPosition,maximumVelocity,rmsVelocity,worstPositionEt,worstVelocityEt,maximumDepth);
        result=new(input.BodyId,input.ParentBodyId,new(input.CoverageStart,input.CoverageEnd),knots,intervals,knots.Length,intervals.Length,maximumPosition,rmsPosition,maximumVelocity,rmsVelocity,worstPositionEt,worstVelocityEt,maximumDepth,hash);
        return true;
    }

    internal static bool TryFindLargestUniformSeed(IAdaptiveStateSource source,AdaptiveSamplerInput input,AdaptiveSeedCadenceSearchConfig search,out AdaptiveSeedCadenceSearchResult result)
    {
        result=default;
        if(!input.IsValid||!search.IsValid)return false;
        for(var seed=search.MaximumSeedSeconds;seed>=search.MinimumSeedSeconds;seed-=search.ResolutionSeconds)
        {
            var uniform=input with{InitialCadenceSeconds=seed,MaximumSubdivisionDepth=0};
            if(!TrySample(source,uniform,out var sample))continue;
            var next=seed+search.ResolutionSeconds;
            if(next>search.MaximumSeedSeconds||TrySample(source,uniform with{InitialCadenceSeconds=next},out _))return false;
            result=new(seed,next,sample);
            return true;
        }
        return false;
    }

    static AdaptiveSourceState Hermite(AdaptiveSourceState first,AdaptiveSourceState last,double u,double intervalSeconds)
    {
        double Position(double p,double v,double q,double w)=>(2*u*u*u-3*u*u+1)*p+(u*u*u-2*u*u+u)*intervalSeconds*v+(-2*u*u*u+3*u*u)*q+(u*u*u-u*u)*intervalSeconds*w;
        double Velocity(double p,double v,double q,double w)=>(6*u*u-6*u)/intervalSeconds*p+(3*u*u-4*u+1)*v+(-6*u*u+6*u)/intervalSeconds*q+(3*u*u-2*u)*w;
        return new(Position(first.X,first.Vx,last.X,last.Vx),Position(first.Y,first.Vy,last.Y,last.Vy),Position(first.Z,first.Vz,last.Z,last.Vz),Velocity(first.X,first.Vx,last.X,last.Vx),Velocity(first.Y,first.Vy,last.Y,last.Vy),Velocity(first.Z,first.Vz,last.Z,last.Vz));
    }

    static double Distance(AdaptiveSourceState first,AdaptiveSourceState last,bool velocity)
    {
        var x=(velocity?first.Vx:first.X)-(velocity?last.Vx:last.X);
        var y=(velocity?first.Vy:first.Y)-(velocity?last.Vy:last.Y);
        var z=(velocity?first.Vz:first.Z)-(velocity?last.Vz:last.Z);
        return Math.Sqrt(x*x+y*y+z*z);
    }

    static ulong Hash(AdaptiveSamplerInput input,ImmutableArray<AdaptiveKnot> knots,ImmutableArray<AdaptiveInterval> intervals,double maximumPosition,double rmsPosition,double maximumVelocity,double rmsVelocity,long worstPositionEt,long worstVelocityEt,int maximumDepth)
    {
        ulong hash=14695981039346656037;
        void Add(long value){unchecked{hash^=(ulong)value;hash*=1099511628211;}}
        Add(input.BodyId);Add(input.ParentBodyId);Add(input.CoverageStart);Add(input.CoverageEnd);Add(input.InitialCadenceSeconds);Add(BitConverter.DoubleToInt64Bits(input.MaximumPositionErrorMetres));Add(BitConverter.DoubleToInt64Bits(input.MaximumVelocityErrorMetresPerSecond));Add(input.MinimumIntervalSeconds);Add(input.MaximumSubdivisionDepth);
        foreach(var knot in knots){Add(knot.Et);Add(BitConverter.DoubleToInt64Bits(knot.State.X));Add(BitConverter.DoubleToInt64Bits(knot.State.Y));Add(BitConverter.DoubleToInt64Bits(knot.State.Z));Add(BitConverter.DoubleToInt64Bits(knot.State.Vx));Add(BitConverter.DoubleToInt64Bits(knot.State.Vy));Add(BitConverter.DoubleToInt64Bits(knot.State.Vz));}
        foreach(var interval in intervals){Add(interval.StartEt);Add(interval.EndEt);}
        Add(BitConverter.DoubleToInt64Bits(maximumPosition));Add(BitConverter.DoubleToInt64Bits(rmsPosition));Add(BitConverter.DoubleToInt64Bits(maximumVelocity));Add(BitConverter.DoubleToInt64Bits(rmsVelocity));Add(worstPositionEt);Add(worstVelocityEt);Add(maximumDepth);
        return hash;
    }

    readonly record struct Residual(long Et,double Position,double Velocity,int Order);
}
