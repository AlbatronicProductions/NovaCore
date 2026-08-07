using NovaCore.NaifEphemerisAdapter;
using NovaCore.Simulation.Celestial;

var root=Environment.CurrentDirectory;
Check(OfficialNaifBundle.VerifyRepositoryRoot(root),"pinned official DE440/CSPICE source bundle");
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
    var v2=SolAnalyticalDefinition.CreateV2ForTest();
    Check(CompactSolarOracleValidation.TryRun(active,v2,out var v2Baseline,out var v2BaselineError),$"compact Solar v2 baseline: {v2BaselineError}");
    Check(v2Baseline.DeterministicHash==0x0F08F6DE4502679EUL&&v2Baseline.AllocatedBytesPerAllBodyEvaluation==0,"compact Solar exact v2 baseline reproduction");
    Check(CompactSolarOracleValidation.TryRun(active,SolAnalyticalDefinition.Instance,out var final,out var finalError),$"compact Solar final: {finalError}");
    Check(CompactSolarOracleValidation.TryRun(active,SolAnalyticalDefinition.Instance,out var repeated,out var repeatedError),$"compact Solar repeated: {repeatedError}");
    Check(final.Bodies.Length==10&&final.Bodies.All(body=>double.IsFinite(body.MaximumPositionErrorMetres)&&double.IsFinite(body.MaximumVelocityErrorMetresPerSecond)),"compact Solar final finite");
    Check(final.DeterministicHash==0xD5E2E00FF5F1C2C2UL&&final.DeterministicHash==repeated.DeterministicHash&&final.Bodies.SequenceEqual(repeated.Bodies),"compact Solar fixed-epoch determinism");
    var correctedMoon=final.Bodies.Single(body=>body.BodyId==SolarSystemBodyIds.Moon.Value);
    Check(correctedMoon.MaximumPositionErrorMetres<35_000_000d&&correctedMoon.RmsPositionErrorMetres<11_000_000d&&correctedMoon.MaximumVelocityErrorMetresPerSecond<77d&&final.EarthMoon.MaximumSeparationErrorMetres<9_000_000d,"compact Solar periodic Moon material improvement");
    Check(final.AllocatedBytesPerAllBodyEvaluation==0&&repeated.AllocatedBytesPerAllBodyEvaluation==0,"compact Solar zero-allocation evaluation");
    Console.WriteLine(CompactSolarOracleValidation.Format("v2-baseline",v2Baseline));
    Console.WriteLine(CompactSolarOracleValidation.Format("final",final));
    Check(CompactSolarOracleValidation.TryFormatLunarDiagnostic(active,SolAnalyticalDefinition.Instance,out var lunarDiagnostic,out var lunarDiagnosticError),$"compact Solar lunar diagnostic: {lunarDiagnosticError}");
    Console.WriteLine(lunarDiagnostic);
    Check(CompactSolarOracleValidation.TryFitLunarCorrections(active,SolAnalyticalDefinition.Instance,out var lunarApproaches,out var lunarFitError),$"compact Solar lunar correction fit: {lunarFitError}");
    Check(CompactSolarOracleValidation.TryFitLunarCorrections(active,SolAnalyticalDefinition.Instance,out var repeatedLunarApproaches,out var repeatedLunarFitError),$"compact Solar repeated lunar correction fit: {repeatedLunarFitError}");
    Check(lunarApproaches.SequenceEqual(repeatedLunarApproaches),"compact Solar lunar correction fit determinism");
    var selectedLunar=lunarApproaches.Single(approach=>approach.Name=="fitted-linear-mean-node-periapsis");var productionLunar=SolAnalyticalDefinition.LunarCorrection;const double obliquity=23.439291111d*Math.PI/180d;var expectedNodeRate=selectedLunar.Correction.NodeRateRadiansPerSecond;var expectedPlaneY=-Math.Sin(obliquity)*expectedNodeRate;var expectedPlaneZ=Math.Cos(obliquity)*expectedNodeRate;Check(Math.Abs(productionLunar.TimeScale-selectedLunar.Correction.TimeScale)<1e-15d&&Math.Abs(productionLunar.PeriapsisRateRadiansPerSecond-selectedLunar.Correction.PeriapsisRateRadiansPerSecond)<1e-22d&&productionLunar.ReferencePlaneAngularVelocity.X==0d&&Math.Abs(productionLunar.ReferencePlaneAngularVelocity.Y-expectedPlaneY)<1e-22d&&Math.Abs(productionLunar.ReferencePlaneAngularVelocity.Z-expectedPlaneZ)<1e-22d&&selectedLunar.Validation.DeterministicHash==0xA9A6B965310DBFD2UL,"compact Solar fitted lunar parameters match production v2");
    var expectedPeriodic=new[]{new AnalyticalKeplerPeriodicTerm(2.6377142567586474e-6,13907036.829921206,13872024.118660487,0,0),new AnalyticalKeplerPeriodicTerm(2.6508409809764152e-6,-12141588.938906228,-14669570.81972078,0,0),new AnalyticalKeplerPeriodicTerm(4.925298388708774e-6,-2434249.1222010353,1732004.2527811143,0,0),new AnalyticalKeplerPeriodicTerm(2.6374618197544597e-6,0,0,-.07902480520654191,.07950143129855441),new AnalyticalKeplerPeriodicTerm(2.651093417980603e-6,0,0,.07511196305917033,-.06282794773814399),new AnalyticalKeplerPeriodicTerm(2.2860695099249988e-6,3608223.0155419977,732977.9825758068,-.004075551465891044,.020721697912036464),new AnalyticalKeplerPeriodicTerm(2.6316557686581393e-6,0,0,.013295345778247099,-.015460614439123546)};var productionPeriodic=SolAnalyticalDefinition.LunarPeriodicCorrection;Check(productionPeriodic.Count==expectedPeriodic.Length&&Enumerable.Range(0,productionPeriodic.Count).All(index=>productionPeriodic.GetTerm(index)==expectedPeriodic[index]),"compact Solar selected periodic parameters match production v3");
    Console.WriteLine(CompactSolarOracleValidation.FormatLunarCorrections(lunarApproaches));
    Check(CompactSolarOracleValidation.TryFormatLunarEpochComparison(active,SolAnalyticalDefinition.Instance,selectedLunar.Correction,out var lunarEpochComparison,out var lunarEpochError),$"compact Solar lunar epoch comparison: {lunarEpochError}");Console.WriteLine(lunarEpochComparison);
    Check(CompactSolarOracleValidation.TryAnalyzeLunarPeriodicity(active,v2,out var lunarPeriodicity,out var lunarPeriodicityError),$"compact Solar lunar periodicity: {lunarPeriodicityError}");
    Check(CompactSolarOracleValidation.TryAnalyzeLunarPeriodicity(active,v2,out var repeatedLunarPeriodicity,out var repeatedLunarPeriodicityError),$"compact Solar repeated lunar periodicity: {repeatedLunarPeriodicityError}");
    Check(lunarPeriodicity==repeatedLunarPeriodicity&&lunarPeriodicity.Contains("0x2831B4B39E68DAB9",StringComparison.Ordinal),"compact Solar periodic fit and validation determinism");Console.WriteLine(lunarPeriodicity);
    Check(CompactSolarOracleValidation.TryFormatLunarHorizonValidation(active,v2,out var lunarV2Horizons,out var lunarV2HorizonError),$"compact Solar v2 lunar horizons: {lunarV2HorizonError}");Console.WriteLine(lunarV2Horizons.Replace("lunar-horizon,","lunar-horizon-v2,"));
    Check(CompactSolarOracleValidation.TryFormatLunarHorizonValidation(active,SolAnalyticalDefinition.Instance,out var lunarHorizons,out var lunarHorizonError),$"compact Solar lunar horizons: {lunarHorizonError}");Console.WriteLine(lunarHorizons);
    Console.WriteLine($"compact-solar,definition,v2_hash=0x{CelestialSystemDefinitionHash.Compute(v2):X16},v3_hash=0x{CelestialSystemDefinitionHash.Compute(SolAnalyticalDefinition.Instance):X16}");
    var lunarPerformance=CompactSolarOracleValidation.MeasureLunarPerformance(v2);
    Console.WriteLine($"lunar-correction,performance,baseline_ns={lunarPerformance.BaselineNanoseconds:R},corrected_ns={lunarPerformance.CorrectedNanoseconds:R},baseline_allocated={lunarPerformance.BaselineAllocatedBytes},corrected_allocated={lunarPerformance.CorrectedAllocatedBytes},evaluations={lunarPerformance.Evaluations}");
    var lunarVersionPerformance=CompactSolarOracleValidation.MeasureLunarVersionPerformance(v2,SolAnalyticalDefinition.Instance);Check(lunarVersionPerformance.V2AllocatedBytes==0&&lunarVersionPerformance.V3AllocatedBytes==0,"compact Solar v2/v3 Moon zero allocation");Console.WriteLine($"lunar-periodic,performance,v2_ns={lunarVersionPerformance.V2Nanoseconds:R},v3_ns={lunarVersionPerformance.V3Nanoseconds:R},v2_allocated={lunarVersionPerformance.V2AllocatedBytes},v3_allocated={lunarVersionPerformance.V3AllocatedBytes},evaluations={lunarVersionPerformance.Evaluations}");
    Check(CompactSolarOracleValidation.TryDeriveEpochElements(active,out var epochElements,out var elementError),$"compact Solar epoch elements: {elementError}");
    Check(epochElements.Length==9&&epochElements.All(element=>element.SemiMajorAxisMetres>0&&element.Eccentricity is >0 and <1),"compact Solar derived element validation");
    for(var index=0;index<epochElements.Length;index++)
    {
        var derived=epochElements[index];var stored=SolAnalyticalDefinition.GetElement(index);
        Check(Relative(derived.SemiMajorAxisMetres,stored.SemiMajorAxisAu*SolAnalyticalDefinition.AstronomicalUnitMetres)<2e-15&&Math.Abs(derived.Eccentricity-stored.Eccentricity)<2e-15&&Math.Abs(derived.InclinationDegrees-stored.InclinationDegrees)<2e-12&&AngleDifference(derived.LongitudeOfAscendingNodeDegrees,stored.LongitudeOfAscendingNodeDegrees)<2e-12&&AngleDifference(derived.ArgumentOfPeriapsisDegrees,stored.ArgumentOfPeriapsisDegrees)<2e-12&&AngleDifference(derived.MeanAnomalyDegrees,stored.MeanAnomalyDegrees)<2e-12&&derived.CentralGravitationalParameter==stored.CentralGravitationalParameter,$"{derived.Name} deterministic DE440 state-to-element derivation");
    }
    Console.WriteLine(CompactSolarOracleValidation.FormatElements(epochElements));
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
static double Relative(double left,double right)=>Math.Abs(left-right)/Math.Max(Math.Abs(left),Math.Abs(right));
static double AngleDifference(double left,double right){var delta=Math.Abs(left-right)%360d;return Math.Min(delta,360d-delta);}
static bool Same(AdaptiveSamplingResult first,AdaptiveSamplingResult second)=>first.BodyId==second.BodyId&&first.ParentBodyId==second.ParentBodyId&&first.Coverage==second.Coverage&&first.AcceptedKnots.SequenceEqual(second.AcceptedKnots)&&first.AcceptedIntervals.SequenceEqual(second.AcceptedIntervals)&&first.SampleCount==second.SampleCount&&first.IntervalCount==second.IntervalCount&&first.MaximumPositionError==second.MaximumPositionError&&first.RmsPositionError==second.RmsPositionError&&first.MaximumVelocityError==second.MaximumVelocityError&&first.RmsVelocityError==second.RmsVelocityError&&first.WorstPositionErrorET==second.WorstPositionErrorET&&first.WorstVelocityErrorET==second.WorstVelocityErrorET&&first.MaximumSubdivisionDepth==second.MaximumSubdivisionDepth&&first.DeterministicHash==second.DeterministicHash;
