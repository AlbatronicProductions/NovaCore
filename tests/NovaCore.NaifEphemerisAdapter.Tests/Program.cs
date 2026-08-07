using NovaCore.NaifEphemerisAdapter;

var root=Environment.CurrentDirectory;
var shim=Path.Combine(root,"external","naif","build","cspice-shim","NovaCore.CSpiceShim.dll");
var kernels=new[]{"de440.bsp","gm_de440.tpc","pck00010.tpc","naif0012.tls"}.Select(name=>Path.Combine(root,"external","naif","kernels",name)).ToArray();
Check(CspiceSession.TryCreate(shim,out var session,out _),"explicit shim load");
var active=session??throw new InvalidOperationException("session missing after success");
using(active)
{
    Check(active.TryLoadKernels(kernels),"canonical kernel load");
    Check(!active.TryQuery(999999,0,out var failed,out var diagnostic),"invalid target rejected");
    Check(failed==default,"failed query default state");
    Check(diagnostic.Status==CspiceSessionStatus.QueryFailure&&diagnostic.Operation=="query","query diagnostic status");
    Check(diagnostic.ShortMessage.Length>0&&diagnostic.LongMessage.Length>0,"short and long diagnostics");
    Check(active.TryQuery(10,0,out var sun,out _),"valid query after reset");
    Check(double.IsFinite(sun.X)&&Math.Abs(sun.X+1067706.8053809535)<1e-6,"Sun ET=0 km state");
    Check(active.Clear(),"kernel clear");
}
Check(CspiceSession.TryCreate(shim,out session,out _),"sampler session");
active=session??throw new InvalidOperationException("sampler session missing");
using(active)
{
    Check(active.TryLoadKernels(kernels),"sampler kernel load");
    IAdaptiveStateSource source=new CspiceRelativeStateSource(active);
    Check(AdaptiveBodySamplingConfigurations.Current.Select(configuration=>configuration.Name).SequenceEqual(["Moon","Earth","Sun"]),"validated body configuration table");
    foreach(var configuration in AdaptiveBodySamplingConfigurations.Current)
    {
        Check(AdaptiveHermiteSampler.TrySample(source,configuration.Input,out var first),$"{configuration.Name} sample");
        Check(AdaptiveHermiteSampler.TrySample(source,configuration.Input,out var second),$"{configuration.Name} repeat");
        Check(first.BodyId==configuration.Input.BodyId&&first.ParentBodyId==configuration.Input.ParentBodyId&&first.Coverage==new AdaptiveSamplingCoverage(configuration.Input.CoverageStart,configuration.Input.CoverageEnd),$"{configuration.Name} result identity");
        Check(first.SampleCount==first.AcceptedKnots.Length&&first.IntervalCount==first.AcceptedIntervals.Length&&first.SampleCount==first.IntervalCount+1,$"{configuration.Name} interval counts");
        Check(first.AcceptedKnots[0].Et==configuration.Input.CoverageStart&&first.AcceptedKnots[^1].Et==configuration.Input.CoverageEnd,$"{configuration.Name} exact coverage");
        Check(first.AcceptedKnots.Zip(first.AcceptedKnots.Skip(1)).All(pair=>pair.First.Et<pair.Second.Et),$"{configuration.Name} strict knots");
        Check(first.AcceptedIntervals.Zip(first.AcceptedIntervals.Skip(1)).All(pair=>pair.First.EndEt==pair.Second.StartEt&&pair.First.StartEt<pair.First.EndEt),$"{configuration.Name} contiguous intervals");
        Check(first.MaximumPositionError<=configuration.Input.MaximumPositionErrorMetres&&first.MaximumVelocityError<=configuration.Input.MaximumVelocityErrorMetresPerSecond,$"{configuration.Name} threshold compliance");
        Check(Same(first,second),$"{configuration.Name} repeat identity");
        Console.WriteLine($"{configuration.Name} adaptive: samples={first.SampleCount} intervals={first.IntervalCount} maxPosition={first.MaximumPositionError:R} rmsPosition={first.RmsPositionError:R} maxVelocity={first.MaximumVelocityError:R} rmsVelocity={first.RmsVelocityError:R} hash=0x{first.DeterministicHash:X16}");
    }
    Check(active.Clear(),"sampler clear");
}

static void Check(bool condition,string name){if(!condition)throw new InvalidOperationException(name);}
static bool Same(AdaptiveSamplingResult first,AdaptiveSamplingResult second)=>first.BodyId==second.BodyId&&first.ParentBodyId==second.ParentBodyId&&first.Coverage==second.Coverage&&first.AcceptedKnots.SequenceEqual(second.AcceptedKnots)&&first.AcceptedIntervals.SequenceEqual(second.AcceptedIntervals)&&first.SampleCount==second.SampleCount&&first.IntervalCount==second.IntervalCount&&first.MaximumPositionError==second.MaximumPositionError&&first.RmsPositionError==second.RmsPositionError&&first.MaximumVelocityError==second.MaximumVelocityError&&first.RmsVelocityError==second.RmsVelocityError&&first.WorstPositionErrorET==second.WorstPositionErrorET&&first.WorstVelocityErrorET==second.WorstVelocityErrorET&&first.MaximumSubdivisionDepth==second.MaximumSubdivisionDepth&&first.DeterministicHash==second.DeterministicHash;
