using System.Diagnostics;
using System.Runtime.CompilerServices;
using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.NaifEphemerisAdapter;
using NovaCore.Simulation.Celestial;
using NovaCore.Simulation.Time;

internal readonly record struct CompactSolarBodyMetrics(
    string Name,
    ulong BodyId,
    int Target,
    int ParentTarget,
    double MaximumPositionErrorMetres,
    double RmsPositionErrorMetres,
    double MaximumVelocityErrorMetresPerSecond,
    double RmsVelocityErrorMetresPerSecond,
    double MaximumRadialErrorMetres,
    double MaximumTransverseErrorMetres,
    double EpochPositionErrorMetres,
    double EpochVelocityErrorMetresPerSecond,
    long WorstPositionEt,
    long WorstVelocityEt);

internal readonly record struct CompactSolarEarthMoonMetrics(
    double EarthRootMaximumPositionErrorMetres,
    double EarthRootMaximumVelocityErrorMetresPerSecond,
    double MoonParentMaximumPositionErrorMetres,
    double MoonParentMaximumVelocityErrorMetresPerSecond,
    double MoonRootMaximumPositionErrorMetres,
    double MoonRootMaximumVelocityErrorMetresPerSecond,
    double MaximumSeparationErrorMetres);

internal sealed record CompactSolarValidationReport(
    CompactSolarBodyMetrics[] Bodies,
    CompactSolarEarthMoonMetrics EarthMoon,
    long EvaluationCount,
    double NanosecondsPerAllBodyEvaluation,
    long AllocatedBytesPerAllBodyEvaluation,
    int LogicalOrbitalDefinitionBytes,
    int RuntimeTrajectoryStorageBytes,
    int AuthoredElementStorageBytes,
    int PeriodicComponentCount,
    int PeriodicParameterBytes,
    ulong DeterministicHash);

internal readonly record struct CompactSolarDerivedElements(
    string Name,
    double SemiMajorAxisMetres,
    double Eccentricity,
    double InclinationDegrees,
    double LongitudeOfAscendingNodeDegrees,
    double ArgumentOfPeriapsisDegrees,
    double MeanAnomalyDegrees,
    double CentralGravitationalParameter);

internal readonly record struct CompactLunarCorrection(double GravitationalParameterScale, double TimeScale, double NodeRateRadiansPerSecond, double PeriapsisRateRadiansPerSecond);
internal readonly record struct CompactLunarCorrectionMetrics(double MaximumPositionMetres, double RmsPositionMetres, double MaximumVelocityMetresPerSecond, double RmsVelocityMetresPerSecond, double MaximumSeparationMetres, long WorstPositionEt, ulong DeterministicHash);
internal readonly record struct CompactLunarCorrectionApproach(string Name, CompactLunarCorrection Correction, CompactLunarCorrectionMetrics Fit, CompactLunarCorrectionMetrics Validation);
internal readonly record struct CompactLunarPerformance(double BaselineNanoseconds, double CorrectedNanoseconds, long BaselineAllocatedBytes, long CorrectedAllocatedBytes, long Evaluations);
internal readonly record struct CompactLunarVersionPerformance(double V2Nanoseconds, double V3Nanoseconds, long V2AllocatedBytes, long V3AllocatedBytes, long Evaluations);
internal readonly record struct CompactLunarPeriodicTerm(double AngularFrequencyRadiansPerSecond, double SineAmplitudeMetres, double CosineAmplitudeMetres);

internal static class CompactSolarOracleValidation
{
    internal const long Day = 86_400;
    internal const long JulianYear = 31_557_600;
    internal static readonly long[] Epochs =
    [
        -25 * JulianYear, -10 * JulianYear, -5 * JulianYear, -JulianYear, -30 * Day, -Day,
        0,
        Day, 30 * Day, JulianYear, 5 * JulianYear, 10 * JulianYear, 25 * JulianYear
    ];

    internal static readonly long[] LunarDiagnosticEpochs =
    [
        -25 * JulianYear, -10 * JulianYear, -5 * JulianYear, -2 * JulianYear, -JulianYear,
        -180 * Day, -90 * Day, -30 * Day, -7 * Day, -Day, 0, Day, 7 * Day, 30 * Day,
        90 * Day, 180 * Day, JulianYear, 2 * JulianYear, 5 * JulianYear, 10 * JulianYear, 25 * JulianYear
    ];

    private static readonly BodyContract[] Bodies =
    [
        new("Sun", SolarSystemBodyIds.Sun, 10, 0),
        new("Mercury", SolarSystemBodyIds.Mercury, 1, 10),
        new("Venus", SolarSystemBodyIds.Venus, 2, 10),
        new("Earth", SolarSystemBodyIds.Earth, 399, 10),
        new("Moon", SolarSystemBodyIds.Moon, 301, 399),
        new("Mars", SolarSystemBodyIds.Mars, 4, 10),
        new("Jupiter", SolarSystemBodyIds.Jupiter, 5, 10),
        new("Saturn", SolarSystemBodyIds.Saturn, 6, 10),
        new("Uranus", SolarSystemBodyIds.Uranus, 7, 10),
        new("Neptune", SolarSystemBodyIds.Neptune, 8, 10),
    ];

    internal static bool TryRun(CspiceSession session, CelestialSystemDefinition system, out CompactSolarValidationReport report, out string error)
    {
        report = null!;
        error = string.Empty;
        var evaluations = new ReferenceFrameEvaluation[system.Count];
        var roots = new FrameTransform[system.Count];
        var staging = new ReferenceFrameEvaluation[system.Count];
        var stagingRoots = new FrameTransform[system.Count];
        var accumulators = new Accumulator[Bodies.Length];
        for (var i = 0; i < accumulators.Length; i++) accumulators[i] = new Accumulator(Bodies[i]);

        double earthRootPositionMax = 0, earthRootVelocityMax = 0, moonRootPositionMax = 0, moonRootVelocityMax = 0, separationMax = 0;
        foreach (var et in Epochs)
        {
            var instant = SimulationInstant.FromWholeSeconds(et);
            var result = CelestialSystemEvaluator.TryEvaluateSystem(system, instant, evaluations, roots, staging, stagingRoots);
            if (!result.Succeeded) { error = $"Runtime evaluation failed at ET {et}: {result.Status}."; return false; }
            var oracle = new Dictionary<int, State>();
            foreach (var body in Bodies)
            {
                if (!TryState(session, body.Target, et, oracle, out _) || (body.ParentTarget != 0 && !TryState(session, body.ParentTarget, et, oracle, out _)))
                { error = $"CSPICE query failed for {body.Name} at ET {et}."; return false; }
            }

            for (var bodyIndex = 0; bodyIndex < Bodies.Length; bodyIndex++)
            {
                var body = Bodies[bodyIndex];
                var traversalIndex = FindTraversalIndex(system, body.Id);
                if (traversalIndex < 0) { error = $"Runtime body {body.Name} is missing."; return false; }
                var actual = oracle[body.Target];
                var parent = body.ParentTarget == 0 ? default : oracle[body.ParentTarget];
                var reference = actual - parent;
                var runtime = new State(evaluations[traversalIndex].Value.LocalToParent.Translation, evaluations[traversalIndex].Value.OriginVelocityInParent);
                accumulators[bodyIndex].Add(et, runtime, reference);
            }

            var sun = oracle[10];
            var earth = oracle[399];
            var moon = oracle[301];
            var earthIndex = FindTraversalIndex(system, SolarSystemBodyIds.Earth);
            var moonIndex = FindTraversalIndex(system, SolarSystemBodyIds.Moon);
            var runtimeEarthRoot = new State(roots[earthIndex].Translation, evaluations[earthIndex].Value.OriginVelocityInParent);
            var runtimeMoonRoot = new State(roots[moonIndex].Translation, evaluations[earthIndex].Value.OriginVelocityInParent + evaluations[moonIndex].Value.OriginVelocityInParent);
            earthRootPositionMax = Math.Max(earthRootPositionMax, Magnitude(runtimeEarthRoot.Position - (earth - sun).Position));
            earthRootVelocityMax = Math.Max(earthRootVelocityMax, Magnitude(runtimeEarthRoot.Velocity - (earth - sun).Velocity));
            moonRootPositionMax = Math.Max(moonRootPositionMax, Magnitude(runtimeMoonRoot.Position - (moon - sun).Position));
            moonRootVelocityMax = Math.Max(moonRootVelocityMax, Magnitude(runtimeMoonRoot.Velocity - (moon - sun).Velocity));
            var runtimeSeparation = Magnitude(evaluations[moonIndex].Value.LocalToParent.Translation);
            var referenceSeparation = Magnitude((moon - earth).Position);
            separationMax = Math.Max(separationMax, Math.Abs(runtimeSeparation - referenceSeparation));
        }

        var metrics = accumulators.Select(x => x.Build()).ToArray();
        var moonMetrics = metrics.Single(x => x.BodyId == SolarSystemBodyIds.Moon.Value);
        var performance = MeasurePerformance(system, evaluations, roots, staging, stagingRoots);
        var hash = Hash(metrics);
        var logicalBytes = system.AnalyticalKeplerCount * (sizeof(ulong) + sizeof(long) + 6 * sizeof(double) + sizeof(byte));
        var periodicCount = 0; for (var index = 0; index < system.AnalyticalKeplerCount; index++) periodicCount += system.GetAnalyticalPeriodicCorrection(index).Count;
        report = new CompactSolarValidationReport(metrics, new(
            earthRootPositionMax, earthRootVelocityMax,
            moonMetrics.MaximumPositionErrorMetres, moonMetrics.MaximumVelocityErrorMetresPerSecond,
            moonRootPositionMax, moonRootVelocityMax, separationMax),
            performance.Count, performance.Nanoseconds, performance.AllocatedBytes, logicalBytes,
            system.AnalyticalKeplerCount * Unsafe.SizeOf<TwoBodyTrajectory>(),
            SolAnalyticalDefinition.ElementCount * Unsafe.SizeOf<SolAnalyticalOrbitalElements>(), periodicCount, periodicCount * 5 * sizeof(double), hash);
        return true;
    }

    internal static string Format(string label, CompactSolarValidationReport report)
    {
        var lines = new List<string> { $"compact-solar,{label},body,max_position_m,rms_position_m,max_velocity_mps,rms_velocity_mps,max_radial_m,max_transverse_m,epoch_position_m,epoch_velocity_mps,worst_position_et,worst_velocity_et" };
        foreach (var body in report.Bodies)
            lines.Add($"compact-solar,{label},{body.Name},{body.MaximumPositionErrorMetres:R},{body.RmsPositionErrorMetres:R},{body.MaximumVelocityErrorMetresPerSecond:R},{body.RmsVelocityErrorMetresPerSecond:R},{body.MaximumRadialErrorMetres:R},{body.MaximumTransverseErrorMetres:R},{body.EpochPositionErrorMetres:R},{body.EpochVelocityErrorMetresPerSecond:R},{body.WorstPositionEt},{body.WorstVelocityEt}");
        lines.Add($"compact-solar,{label},earth-moon,earth_root_max_position_m={report.EarthMoon.EarthRootMaximumPositionErrorMetres:R},earth_root_max_velocity_mps={report.EarthMoon.EarthRootMaximumVelocityErrorMetresPerSecond:R},moon_parent_max_position_m={report.EarthMoon.MoonParentMaximumPositionErrorMetres:R},moon_parent_max_velocity_mps={report.EarthMoon.MoonParentMaximumVelocityErrorMetresPerSecond:R},moon_root_max_position_m={report.EarthMoon.MoonRootMaximumPositionErrorMetres:R},moon_root_max_velocity_mps={report.EarthMoon.MoonRootMaximumVelocityErrorMetresPerSecond:R},separation_max_error_m={report.EarthMoon.MaximumSeparationErrorMetres:R}");
        lines.Add($"compact-solar,{label},performance,all_body_ns={report.NanosecondsPerAllBodyEvaluation:R},allocated_bytes={report.AllocatedBytesPerAllBodyEvaluation},evaluations={report.EvaluationCount},logical_orbit_bytes={report.LogicalOrbitalDefinitionBytes},runtime_trajectory_bytes={report.RuntimeTrajectoryStorageBytes},authored_element_bytes={report.AuthoredElementStorageBytes},periodic_components={report.PeriodicComponentCount},periodic_parameter_bytes={report.PeriodicParameterBytes},hash=0x{report.DeterministicHash:X16}");
        return string.Join(Environment.NewLine, lines);
    }

    internal static bool TryDeriveEpochElements(CspiceSession session, out CompactSolarDerivedElements[] elements, out string error)
    {
        elements = new CompactSolarDerivedElements[Bodies.Length - 1];
        error = string.Empty;
        var cache = new Dictionary<int, State>();
        for (var bodyIndex = 1; bodyIndex < Bodies.Length; bodyIndex++)
        {
            var body = Bodies[bodyIndex];
            // The ten-node runtime intentionally has no EMB node. Seed Earth's heliocentric coast from the EMB state so the
            // monthly Earth-about-EMB velocity does not become a false long-term heliocentric phase term.
            var derivationTarget = body.Id == SolarSystemBodyIds.Earth ? 3 : body.Target;
            if (!TryState(session, derivationTarget, 0, cache, out var child) || !TryState(session, body.ParentTarget, 0, cache, out var parent))
            { error = $"CSPICE epoch query failed for {body.Name}."; return false; }
            var mu = body.Id == SolarSystemBodyIds.Moon ? 3.986004355070226e14d : 1.327124400412794e20d;
            if (!TryConvertStateToElements(body.Name, child - parent, mu, out elements[bodyIndex - 1]))
            { error = $"State-to-element conversion failed for {body.Name}."; return false; }
        }
        return true;
    }

    internal static string FormatElements(IEnumerable<CompactSolarDerivedElements> elements) => string.Join(Environment.NewLine, elements.Select(element =>
        $"compact-solar,epoch-elements,{element.Name},a_m={element.SemiMajorAxisMetres:R},e={element.Eccentricity:R},i_deg={element.InclinationDegrees:R},node_deg={element.LongitudeOfAscendingNodeDegrees:R},periapsis_deg={element.ArgumentOfPeriapsisDegrees:R},mean_deg={element.MeanAnomalyDegrees:R},mu={element.CentralGravitationalParameter:R}"));

    internal static bool TryFormatLunarDiagnostic(CspiceSession session, CelestialSystemDefinition system, out string text, out string error)
    {
        text = string.Empty;
        error = string.Empty;
        const double earthMoonMu = 4.035032355070226e14d;
        var moonIndex = FindTraversalIndex(system, SolarSystemBodyIds.Moon);
        if (moonIndex < 0) { error = "Runtime Moon is missing."; return false; }
        var evaluations = new ReferenceFrameEvaluation[system.Count];
        var roots = new FrameTransform[system.Count];
        var staging = new ReferenceFrameEvaluation[system.Count];
        var stagingRoots = new FrameTransform[system.Count];
        var lines = new List<string>
        {
            "lunar-diagnostic,et,position_m,radial_m,along_track_m,cross_track_m,phase_deg,plane_deg,separation_m,velocity_mps,reference_node_deg,runtime_node_deg,reference_periapsis_deg,runtime_periapsis_deg,reference_mean_deg,runtime_mean_deg"
        };
        foreach (var et in LunarDiagnosticEpochs)
        {
            var cache = new Dictionary<int, State>();
            if (!TryState(session, 301, et, cache, out var moon) || !TryState(session, 399, et, cache, out var earth))
            { error = $"CSPICE lunar diagnostic query failed at ET {et}."; return false; }
            var result = CelestialSystemEvaluator.TryEvaluateSystem(system, SimulationInstant.FromWholeSeconds(et), evaluations, roots, staging, stagingRoots);
            if (!result.Succeeded) { error = $"Runtime lunar diagnostic failed at ET {et}: {result.Status}."; return false; }
            var reference = moon - earth;
            var runtime = new State(evaluations[moonIndex].Value.LocalToParent.Translation, evaluations[moonIndex].Value.OriginVelocityInParent);
            var delta = runtime.Position - reference.Position;
            var referenceRadius = Magnitude(reference.Position);
            var radialAxis = reference.Position / referenceRadius;
            var referenceNormal = Double3.Cross(reference.Position, reference.Velocity);
            referenceNormal /= Magnitude(referenceNormal);
            var alongAxis = Double3.Cross(referenceNormal, radialAxis);
            var runtimeNormal = Double3.Cross(runtime.Position, runtime.Velocity);
            runtimeNormal /= Magnitude(runtimeNormal);
            var radial = Double3.Dot(delta, radialAxis);
            var along = Double3.Dot(delta, alongAxis);
            var cross = Double3.Dot(delta, referenceNormal);
            var position = Magnitude(delta);
            var phase = Degrees(Math.Atan2(Double3.Dot(Double3.Cross(reference.Position, runtime.Position), referenceNormal), Double3.Dot(reference.Position, runtime.Position)));
            var plane = Degrees(Math.Acos(Math.Clamp(Double3.Dot(referenceNormal, runtimeNormal), -1d, 1d)));
            var separation = Magnitude(runtime.Position) - referenceRadius;
            var velocity = Magnitude(runtime.Velocity - reference.Velocity);
            if (!TryConvertStateToElements("reference", reference, earthMoonMu, out var referenceElements) || !TryConvertStateToElements("runtime", runtime, earthMoonMu, out var runtimeElements))
            { error = $"Lunar diagnostic element conversion failed at ET {et}."; return false; }
            lines.Add($"lunar-diagnostic,{et},{position:R},{radial:R},{along:R},{cross:R},{phase:R},{plane:R},{separation:R},{velocity:R},{referenceElements.LongitudeOfAscendingNodeDegrees:R},{runtimeElements.LongitudeOfAscendingNodeDegrees:R},{referenceElements.ArgumentOfPeriapsisDegrees:R},{runtimeElements.ArgumentOfPeriapsisDegrees:R},{referenceElements.MeanAnomalyDegrees:R},{runtimeElements.MeanAnomalyDegrees:R}");
        }
        text = string.Join(Environment.NewLine, lines);
        return true;
    }

    internal static bool TryFitLunarCorrections(CspiceSession session, CelestialSystemDefinition system, out CompactLunarCorrectionApproach[] approaches, out string error)
    {
        approaches = [];
        error = string.Empty;
        var moonIndex = FindTraversalIndex(system, SolarSystemBodyIds.Moon);
        if (moonIndex < 0 || !system.TryGetPhysicalProperties(SolarSystemBodyIds.Earth, out var earthProperties)) { error = "Moon/Earth analytical data is missing."; return false; }
        var moonNode = system.GetNodeInTraversalOrder(moonIndex);
        if (!system.TryGetAnalyticalKepler(moonNode.Ephemeris.PayloadIndex, out var moonTrajectory)) { error = "Moon analytical payload is missing."; return false; }
        var fitEpochs = new SortedSet<long>();
        for (var day = -3_600; day <= 3_600; day += 60) fitEpochs.Add(day * Day);
        fitEpochs.Add(-10 * JulianYear); fitEpochs.Add(0); fitEpochs.Add(10 * JulianYear);
        var validationEpochs = new SortedSet<long>();
        for (var day = -3_570; day <= 3_570; day += 60) validationEpochs.Add(day * Day);
        foreach (var epoch in LunarDiagnosticEpochs) if (!fitEpochs.Contains(epoch)) validationEpochs.Add(epoch);
        validationEpochs.Add(-25 * JulianYear); validationEpochs.Add(25 * JulianYear);
        if (!TryBuildLunarReferences(session, fitEpochs, out var fitReferences) || !TryBuildLunarReferences(session, validationEpochs, out var validationReferences)) { error = "CSPICE lunar fit/reference query failed."; return false; }

        var baseline = new CompactLunarCorrection(1d, 1d, 0d, 0d);
        var physicalMean = new CompactLunarCorrection((3.9860043550702266e14d + 4.9028001184575496e12d) / 3.9860043550702266e14d, 1d, 0d, 0d);
        var fittedMu = OptimizeLunarCorrection(moonTrajectory, earthProperties.GravitationalParameter, fitReferences, physicalMean, true, false, false, false);
        var fittedMean = OptimizeLunarCorrection(moonTrajectory, earthProperties.GravitationalParameter, fitReferences, baseline with { TimeScale = Math.Sqrt(physicalMean.GravitationalParameterScale) }, false, true, false, false);
        var fittedMeanNode = OptimizeLunarCorrection(moonTrajectory, earthProperties.GravitationalParameter, fitReferences, fittedMean with { NodeRateRadiansPerSecond = RadiansPerSecond(-19.35d) }, false, true, true, false);
        var fittedLinear = OptimizeLunarCorrection(moonTrajectory, earthProperties.GravitationalParameter, fitReferences, fittedMeanNode with { PeriapsisRateRadiansPerSecond = RadiansPerSecond(40.7d) }, false, true, true, true);
        var fittedMuLinear = OptimizeLunarCorrection(moonTrajectory, earthProperties.GravitationalParameter, fitReferences, fittedMu with { NodeRateRadiansPerSecond = RadiansPerSecond(-19.35d), PeriapsisRateRadiansPerSecond = RadiansPerSecond(40.7d) }, true, false, true, true);
        var candidates = new[]
        {
            ("baseline", baseline),
            ("physical-relative-mu", physicalMean),
            ("fitted-effective-mu", fittedMu),
            ("fitted-mean-anomaly-rate", fittedMean),
            ("fitted-mean-plus-node-rate", fittedMeanNode),
            ("fitted-linear-mean-node-periapsis", fittedLinear),
            ("fitted-effective-mu-node-periapsis", fittedMuLinear),
        };
        approaches = candidates.Select(candidate => new CompactLunarCorrectionApproach(candidate.Item1, candidate.Item2,
            MeasureLunarCorrection(moonTrajectory, earthProperties.GravitationalParameter, fitReferences, candidate.Item2),
            MeasureLunarCorrection(moonTrajectory, earthProperties.GravitationalParameter, validationReferences, candidate.Item2))).ToArray();
        return true;
    }

    internal static string FormatLunarCorrections(IEnumerable<CompactLunarCorrectionApproach> approaches) => string.Join(Environment.NewLine, approaches.Select(approach =>
        $"lunar-correction,{approach.Name},mu_scale={approach.Correction.GravitationalParameterScale:R},time_scale={approach.Correction.TimeScale:R},mean_delta_deg_day={MeanDeltaDegreesPerDay(approach.Correction.TimeScale):R},node_deg_year={DegreesPerYear(approach.Correction.NodeRateRadiansPerSecond):R},periapsis_deg_year={DegreesPerYear(approach.Correction.PeriapsisRateRadiansPerSecond):R},fit_max_position_m={approach.Fit.MaximumPositionMetres:R},fit_rms_position_m={approach.Fit.RmsPositionMetres:R},fit_max_velocity_mps={approach.Fit.MaximumVelocityMetresPerSecond:R},validation_max_position_m={approach.Validation.MaximumPositionMetres:R},validation_rms_position_m={approach.Validation.RmsPositionMetres:R},validation_max_velocity_mps={approach.Validation.MaximumVelocityMetresPerSecond:R},validation_separation_m={approach.Validation.MaximumSeparationMetres:R},validation_worst_et={approach.Validation.WorstPositionEt},hash=0x{approach.Validation.DeterministicHash:X16}"));

    internal static bool TryAnalyzeLunarPeriodicity(CspiceSession session, CelestialSystemDefinition system, out string text, out string error)
    {
        text = string.Empty;
        error = string.Empty;
        var fitEpochs = new List<long>();
        var validationEpochs = new SortedSet<long>();
        for (var day = -3_600; day <= 3_600; day += 2) fitEpochs.Add(day * Day);
        for (var day = -3_599; day <= 3_599; day += 2) validationEpochs.Add(day * Day);
        foreach (var epoch in LunarDiagnosticEpochs) if (!fitEpochs.Contains(epoch)) validationEpochs.Add(epoch);
        validationEpochs.Add(-25 * JulianYear);
        validationEpochs.Add(25 * JulianYear);
        if (!TryBuildLunarResidualSamples(session, system, fitEpochs, out var fit) || !TryBuildLunarResidualSamples(session, system, validationEpochs, out var validation))
        { error = "Dense CSPICE lunar residual sampling failed."; return false; }

        var lines = new List<string>
        {
            "lunar-periodic-analysis,kind,count,period_days,frequency_rad_s,sine_m,cosine_m,variance_reduction,fit_max_position_m,fit_rms_position_m,fit_max_velocity_mps,fit_rms_velocity_mps,fit_max_separation_m,validation_max_position_m,validation_rms_position_m,validation_max_velocity_mps,validation_rms_velocity_mps,validation_max_separation_m,hash"
        };
        var terms = new List<CompactLunarPeriodicTerm>();
        var baselineFit = MeasurePeriodic(fit, terms);
        var baselineValidation = MeasurePeriodic(validation, terms);
        lines.Add(PeriodicLine("baseline", 0, 0d, 0d, 0d, 0d, 0d, baselineFit, baselineValidation));
        var previousObjective = SeparationObjective(fit, terms);
        for (var component = 1; component <= 8; component++)
        {
            var frequency = FindBestFrequency(fit, terms);
            terms.Add(new(frequency, 0d, 0d));
            FitPeriodicTerms(fit, terms);
            var objective = SeparationObjective(fit, terms);
            var reduction = previousObjective <= 0d ? 0d : 1d - objective / previousObjective;
            previousObjective = objective;
            var selected = terms[^1];
            var fitMetrics = MeasurePeriodic(fit, terms);
            var validationMetrics = MeasurePeriodic(validation, terms);
            lines.Add(PeriodicLine("radial", component, 2d * Math.PI / selected.AngularFrequencyRadiansPerSecond / Day, selected.AngularFrequencyRadiansPerSecond, selected.SineAmplitudeMetres, selected.CosineAmplitudeMetres, reduction, fitMetrics, validationMetrics));
        }

        var phaseTerms = new List<CompactLunarPeriodicTerm>();
        var previousPhaseObjective = PhaseObjective(fit, phaseTerms);
        for (var component = 1; component <= 8; component++)
        {
            var frequency = FindBestPhaseFrequency(fit, phaseTerms);
            phaseTerms.Add(new(frequency, 0d, 0d));
            FitPhaseTerms(fit, phaseTerms);
            var objective = PhaseObjective(fit, phaseTerms);
            var reduction = previousPhaseObjective <= 0d ? 0d : 1d - objective / previousPhaseObjective;
            previousPhaseObjective = objective;
            var selected = phaseTerms[^1];
            var fitMetrics = MeasurePeriodic(fit, [], phaseTerms);
            var validationMetrics = MeasurePeriodic(validation, [], phaseTerms);
            lines.Add(PeriodicLine("phase", component, 2d * Math.PI / selected.AngularFrequencyRadiansPerSecond / Day, selected.AngularFrequencyRadiansPerSecond, selected.SineAmplitudeMetres, selected.CosineAmplitudeMetres, reduction, fitMetrics, validationMetrics));
        }

        foreach (var perDomain in new[] { 1, 2, 3, 4 })
        {
            var radialCandidate = terms.Take(perDomain).ToList(); FitPeriodicTerms(fit, radialCandidate);
            var phaseCandidate = phaseTerms.Take(perDomain).ToList(); FitPhaseTerms(fit, phaseCandidate);
            var fitMetrics = MeasurePeriodic(fit, radialCandidate, phaseCandidate);
            var validationMetrics = MeasurePeriodic(validation, radialCandidate, phaseCandidate);
            lines.Add(PeriodicLine("radial-phase", perDomain * 2, 0d, 0d, 0d, 0d, 0d, fitMetrics, validationMetrics));
            if (perDomain == 4)
            {
                for (var index = 0; index < radialCandidate.Count; index++)
                {
                    var term = radialCandidate[index];
                    lines.Add($"lunar-periodic-analysis,selected-radial,{index + 1},{2d * Math.PI / term.AngularFrequencyRadiansPerSecond / Day:R},{term.AngularFrequencyRadiansPerSecond:R},{term.SineAmplitudeMetres:R},{term.CosineAmplitudeMetres:R}");
                }
                for (var index = 0; index < phaseCandidate.Count; index++)
                {
                    var term = phaseCandidate[index];
                    lines.Add($"lunar-periodic-analysis,selected-phase,{index + 1},{2d * Math.PI / term.AngularFrequencyRadiansPerSecond / Day:R},{term.AngularFrequencyRadiansPerSecond:R},{term.SineAmplitudeMetres:R},{term.CosineAmplitudeMetres:R}");
                }
            }
        }

        lines.Add("lunar-periodic-analysis,components,index,period_days,frequency_rad_s,sine_m,cosine_m");
        for (var index = 0; index < terms.Count; index++)
        {
            var term = terms[index];
            lines.Add($"lunar-periodic-analysis,component,{index + 1},{2d * Math.PI / term.AngularFrequencyRadiansPerSecond / Day:R},{term.AngularFrequencyRadiansPerSecond:R},{term.SineAmplitudeMetres:R},{term.CosineAmplitudeMetres:R}");
        }
        for (var index = 0; index < phaseTerms.Count; index++)
        {
            var term = phaseTerms[index];
            lines.Add($"lunar-periodic-analysis,phase-component,{index + 1},{2d * Math.PI / term.AngularFrequencyRadiansPerSecond / Day:R},{term.AngularFrequencyRadiansPerSecond:R},{term.SineAmplitudeMetres:R},{term.CosineAmplitudeMetres:R}");
        }
        text = string.Join(Environment.NewLine, lines);
        return true;
    }

    internal static bool TryFormatLunarHorizonValidation(CspiceSession session, CelestialSystemDefinition system, out string text, out string error)
    {
        text = string.Empty;
        error = string.Empty;
        var epochs = new SortedSet<long>();
        for (var day = -9_131; day <= 9_131; day += 5) epochs.Add(day * Day);
        var horizons = new[] { 30L * Day, 180L * Day, JulianYear, 5L * JulianYear, 10L * JulianYear, 25L * JulianYear };
        foreach (var horizon in horizons) { epochs.Add(-horizon); epochs.Add(horizon); }
        epochs.Add(0);
        if (!TryBuildLunarResidualSamples(session, system, epochs, out var samples)) { error = "Lunar horizon sampling failed."; return false; }
        var lines = new List<string> { "lunar-horizon,seconds,samples,max_position_m,rms_position_m,max_velocity_mps,rms_velocity_mps,max_separation_m,max_radial_m,max_along_m,max_cross_m,hash" };
        foreach (var horizon in horizons)
        {
            var selected = samples.Where(sample => Math.Abs(sample.Et) <= horizon).ToArray();
            var metrics = MeasurePeriodic(selected, []);
            var radial = selected.Max(sample => Math.Abs(sample.RadialResidual));
            var along = selected.Max(sample => Math.Abs(sample.AlongResidual));
            var cross = selected.Max(sample => Math.Abs(sample.CrossResidual));
            lines.Add($"lunar-horizon,{horizon},{selected.Length},{metrics.MaximumPosition:R},{metrics.RmsPosition:R},{metrics.MaximumVelocity:R},{metrics.RmsVelocity:R},{metrics.MaximumSeparation:R},{radial:R},{along:R},{cross:R},0x{metrics.Hash:X16}");
        }
        text = string.Join(Environment.NewLine, lines);
        return true;
    }

    private static string PeriodicLine(string kind, int count, double period, double frequency, double sine, double cosine, double reduction, PeriodicMetrics fit, PeriodicMetrics validation) =>
        $"lunar-periodic-analysis,{kind},{count},{period:R},{frequency:R},{sine:R},{cosine:R},{reduction:R},{fit.MaximumPosition:R},{fit.RmsPosition:R},{fit.MaximumVelocity:R},{fit.RmsVelocity:R},{fit.MaximumSeparation:R},{validation.MaximumPosition:R},{validation.RmsPosition:R},{validation.MaximumVelocity:R},{validation.RmsVelocity:R},{validation.MaximumSeparation:R},0x{validation.Hash:X16}";

    private static bool TryBuildLunarResidualSamples(CspiceSession session, CelestialSystemDefinition system, IEnumerable<long> epochs, out LunarResidualSample[] samples)
    {
        var moonIndex = FindTraversalIndex(system, SolarSystemBodyIds.Moon);
        var evaluations = new ReferenceFrameEvaluation[system.Count];
        var roots = new FrameTransform[system.Count];
        var staging = new ReferenceFrameEvaluation[system.Count];
        var stagingRoots = new FrameTransform[system.Count];
        var result = new List<LunarResidualSample>();
        foreach (var et in epochs)
        {
            var cache = new Dictionary<int, State>();
            if (!TryState(session, 301, et, cache, out var moon) || !TryState(session, 399, et, cache, out var earth) ||
                !CelestialSystemEvaluator.TryEvaluateSystem(system, SimulationInstant.FromWholeSeconds(et), evaluations, roots, staging, stagingRoots).Succeeded)
            { samples = []; return false; }
            var reference = moon - earth;
            var runtime = new CartesianState(evaluations[moonIndex].Value.LocalToParent.Translation, evaluations[moonIndex].Value.OriginVelocityInParent);
            var radial = reference.Position / Magnitude(reference.Position);
            var normal = Double3.Cross(reference.Position, reference.Velocity); normal /= Magnitude(normal);
            var transverse = Double3.Cross(normal, radial);
            var delta = runtime.Position - reference.Position;
            var runtimeNormal = Double3.Cross(runtime.Position, runtime.Velocity); runtimeNormal /= Magnitude(runtimeNormal);
            var desiredPhase = Math.Atan2(Double3.Dot(Double3.Cross(runtime.Position, reference.Position), runtimeNormal), Double3.Dot(runtime.Position, reference.Position));
            result.Add(new(et, runtime, new(reference.Position, reference.Velocity), Double3.Dot(delta, radial), Double3.Dot(delta, transverse), Double3.Dot(delta, normal), Magnitude(runtime.Position) - Magnitude(reference.Position), desiredPhase));
        }
        samples = result.ToArray();
        return true;
    }

    private static double FindBestFrequency(LunarResidualSample[] samples, List<CompactLunarPeriodicTerm> existing)
    {
        var spanSeconds = (samples[^1].Et - samples[0].Et) + 2d * Day;
        var bestFrequency = 0d;
        var bestObjective = double.PositiveInfinity;
        var trial = new List<CompactLunarPeriodicTerm>(existing.Count + 1);
        var maximumBin = (int)Math.Floor(spanSeconds / (10d * Day));
        for (var bin = 1; bin <= maximumBin; bin++)
        {
            var frequency = 2d * Math.PI * bin / spanSeconds;
            if (existing.Any(term => Math.Abs(term.AngularFrequencyRadiansPerSecond - frequency) <= 1e-15d)) continue;
            trial.Clear(); trial.AddRange(existing); trial.Add(new(frequency, 0d, 0d));
            FitPeriodicTerms(samples, trial);
            var objective = SeparationObjective(samples, trial);
            if (objective < bestObjective) { bestObjective = objective; bestFrequency = frequency; }
        }
        var binWidth = 2d * Math.PI / spanSeconds;
        var coarse = bestFrequency;
        for (var step = -20; step <= 20; step++)
        {
            var frequency = coarse + step * binWidth / 40d;
            if (frequency <= 0d) continue;
            trial.Clear(); trial.AddRange(existing); trial.Add(new(frequency, 0d, 0d));
            FitPeriodicTerms(samples, trial);
            var objective = SeparationObjective(samples, trial);
            if (objective < bestObjective) { bestObjective = objective; bestFrequency = frequency; }
        }
        return bestFrequency;
    }

    private static double FindBestPhaseFrequency(LunarResidualSample[] samples, List<CompactLunarPeriodicTerm> existing)
    {
        var spanSeconds = (samples[^1].Et - samples[0].Et) + 2d * Day;
        var bestFrequency = 0d;
        var bestObjective = double.PositiveInfinity;
        var trial = new List<CompactLunarPeriodicTerm>(existing.Count + 1);
        var maximumBin = (int)Math.Floor(spanSeconds / (10d * Day));
        for (var bin = 1; bin <= maximumBin; bin++)
        {
            var frequency = 2d * Math.PI * bin / spanSeconds;
            if (existing.Any(term => Math.Abs(term.AngularFrequencyRadiansPerSecond - frequency) <= 1e-15d)) continue;
            trial.Clear(); trial.AddRange(existing); trial.Add(new(frequency, 0d, 0d));
            FitPhaseTerms(samples, trial);
            var objective = PhaseObjective(samples, trial);
            if (objective < bestObjective) { bestObjective = objective; bestFrequency = frequency; }
        }
        var binWidth = 2d * Math.PI / spanSeconds;
        var coarse = bestFrequency;
        for (var step = -20; step <= 20; step++)
        {
            var frequency = coarse + step * binWidth / 40d;
            if (frequency <= 0d) continue;
            trial.Clear(); trial.AddRange(existing); trial.Add(new(frequency, 0d, 0d));
            FitPhaseTerms(samples, trial);
            var objective = PhaseObjective(samples, trial);
            if (objective < bestObjective) { bestObjective = objective; bestFrequency = frequency; }
        }
        return bestFrequency;
    }

    private static void FitPeriodicTerms(LunarResidualSample[] samples, List<CompactLunarPeriodicTerm> terms)
        => FitScalarTerms(samples, terms, static sample => -sample.SeparationResidual);

    private static void FitPhaseTerms(LunarResidualSample[] samples, List<CompactLunarPeriodicTerm> terms)
        => FitScalarTerms(samples, terms, static sample => sample.DesiredPhaseRadians);

    private static void FitScalarTerms(LunarResidualSample[] samples, List<CompactLunarPeriodicTerm> terms, Func<LunarResidualSample, double> target)
    {
        var count = terms.Count * 2;
        var normal = new double[count, count];
        var right = new double[count];
        var basis = new double[count];
        foreach (var sample in samples)
        {
            for (var term = 0; term < terms.Count; term++)
            {
                var angle = terms[term].AngularFrequencyRadiansPerSecond * sample.Et;
                basis[term * 2] = Math.Sin(angle);
                basis[term * 2 + 1] = Math.Cos(angle) - 1d;
            }
            var desired = target(sample);
            for (var row = 0; row < count; row++)
            {
                right[row] += basis[row] * desired;
                for (var column = 0; column < count; column++) normal[row, column] += basis[row] * basis[column];
            }
        }
        if (!TrySolve(normal, right, out var solution)) return;
        for (var term = 0; term < terms.Count; term++) terms[term] = terms[term] with { SineAmplitudeMetres = solution[term * 2], CosineAmplitudeMetres = solution[term * 2 + 1] };
    }

    private static bool TrySolve(double[,] matrix, double[] right, out double[] solution)
    {
        var count = right.Length;
        solution = new double[count];
        for (var pivot = 0; pivot < count; pivot++)
        {
            var selected = pivot;
            for (var row = pivot + 1; row < count; row++) if (Math.Abs(matrix[row, pivot]) > Math.Abs(matrix[selected, pivot])) selected = row;
            if (Math.Abs(matrix[selected, pivot]) < 1e-20d) return false;
            if (selected != pivot)
            {
                for (var column = pivot; column < count; column++) (matrix[pivot, column], matrix[selected, column]) = (matrix[selected, column], matrix[pivot, column]);
                (right[pivot], right[selected]) = (right[selected], right[pivot]);
            }
            var divisor = matrix[pivot, pivot];
            for (var column = pivot; column < count; column++) matrix[pivot, column] /= divisor;
            right[pivot] /= divisor;
            for (var row = 0; row < count; row++)
            {
                if (row == pivot) continue;
                var factor = matrix[row, pivot];
                for (var column = pivot; column < count; column++) matrix[row, column] -= factor * matrix[pivot, column];
                right[row] -= factor * right[pivot];
            }
        }
        Array.Copy(right, solution, count);
        return true;
    }

    private static double SeparationObjective(LunarResidualSample[] samples, List<CompactLunarPeriodicTerm> terms)
    {
        double sum = 0d;
        foreach (var sample in samples)
        {
            var corrected = ApplyPeriodic(sample.Runtime, sample.Et, terms);
            var error = Magnitude(corrected.Position) - Magnitude(sample.Reference.Position);
            sum += error * error;
        }
        return sum / samples.Length;
    }

    private static double PhaseObjective(LunarResidualSample[] samples, List<CompactLunarPeriodicTerm> terms)
    {
        double sum = 0d;
        foreach (var sample in samples)
        {
            var phase = 0d;
            foreach (var term in terms)
            {
                var angle = term.AngularFrequencyRadiansPerSecond * sample.Et;
                phase += term.SineAmplitudeMetres * Math.Sin(angle) + term.CosineAmplitudeMetres * (Math.Cos(angle) - 1d);
            }
            var error = phase - sample.DesiredPhaseRadians;
            sum += error * error;
        }
        return sum / samples.Length;
    }

    private static PeriodicMetrics MeasurePeriodic(LunarResidualSample[] samples, List<CompactLunarPeriodicTerm> terms, List<CompactLunarPeriodicTerm>? phaseTerms = null)
    {
        double maxPosition = 0d, positionSquares = 0d, maxVelocity = 0d, velocitySquares = 0d, maxSeparation = 0d;
        ulong hash = 14695981039346656037UL;
        foreach (var sample in samples)
        {
            var corrected = ApplyPeriodic(sample.Runtime, sample.Et, terms, phaseTerms);
            var position = Magnitude(corrected.Position - sample.Reference.Position);
            var velocity = Magnitude(corrected.Velocity - sample.Reference.Velocity);
            var separation = Math.Abs(Magnitude(corrected.Position) - Magnitude(sample.Reference.Position));
            maxPosition = Math.Max(maxPosition, position); positionSquares += position * position;
            maxVelocity = Math.Max(maxVelocity, velocity); velocitySquares += velocity * velocity;
            maxSeparation = Math.Max(maxSeparation, separation);
            HashLong(ref hash, sample.Et); HashLong(ref hash, BitConverter.DoubleToInt64Bits(position)); HashLong(ref hash, BitConverter.DoubleToInt64Bits(velocity)); HashLong(ref hash, BitConverter.DoubleToInt64Bits(separation));
        }
        return new(maxPosition, Math.Sqrt(positionSquares / samples.Length), maxVelocity, Math.Sqrt(velocitySquares / samples.Length), maxSeparation, hash);
    }

    private static CartesianState ApplyPeriodic(in CartesianState state, long et, List<CompactLunarPeriodicTerm> terms, List<CompactLunarPeriodicTerm>? phaseTerms = null)
    {
        double offset = 0d, rate = 0d;
        foreach (var term in terms)
        {
            var angle = term.AngularFrequencyRadiansPerSecond * et;
            offset += term.SineAmplitudeMetres * Math.Sin(angle) + term.CosineAmplitudeMetres * (Math.Cos(angle) - 1d);
            rate += term.AngularFrequencyRadiansPerSecond * (term.SineAmplitudeMetres * Math.Cos(angle) - term.CosineAmplitudeMetres * Math.Sin(angle));
        }
        var radius = Magnitude(state.Position);
        var radial = state.Position / radius;
        var radialSpeed = Double3.Dot(radial, state.Velocity);
        var radialDerivative = (state.Velocity - radial * radialSpeed) / radius;
        var correctedPosition = state.Position + radial * offset;
        var correctedVelocity = state.Velocity + radial * rate + radialDerivative * offset;
        if (phaseTerms is null || phaseTerms.Count == 0) return new(correctedPosition, correctedVelocity);
        double phase = 0d, phaseRate = 0d;
        foreach (var term in phaseTerms)
        {
            var angle = term.AngularFrequencyRadiansPerSecond * et;
            phase += term.SineAmplitudeMetres * Math.Sin(angle) + term.CosineAmplitudeMetres * (Math.Cos(angle) - 1d);
            phaseRate += term.AngularFrequencyRadiansPerSecond * (term.SineAmplitudeMetres * Math.Cos(angle) - term.CosineAmplitudeMetres * Math.Sin(angle));
        }
        var orbitNormal = Double3.Cross(correctedPosition, correctedVelocity); orbitNormal /= Magnitude(orbitNormal);
        var rotatedPosition = Rotate(correctedPosition, orbitNormal, phase);
        var rotatedVelocity = Rotate(correctedVelocity, orbitNormal, phase) + Double3.Cross(orbitNormal * phaseRate, rotatedPosition);
        return new(rotatedPosition, rotatedVelocity);
    }

    private readonly record struct LunarResidualSample(long Et, CartesianState Runtime, CartesianState Reference, double RadialResidual, double AlongResidual, double CrossResidual, double SeparationResidual, double DesiredPhaseRadians);
    private readonly record struct PeriodicMetrics(double MaximumPosition, double RmsPosition, double MaximumVelocity, double RmsVelocity, double MaximumSeparation, ulong Hash);

    internal static CompactLunarPerformance MeasureLunarPerformance(CelestialSystemDefinition system)
    {
        const int count = 100_000;
        var moonIndex = FindTraversalIndex(system, SolarSystemBodyIds.Moon);
        var moonNode = system.GetNodeInTraversalOrder(moonIndex);
        system.TryGetAnalyticalKepler(moonNode.Ephemeris.PayloadIndex, out var trajectory);
        system.TryGetAnalyticalCorrection(moonNode.Ephemeris.PayloadIndex, out var correction);
        system.TryGetPhysicalProperties(SolarSystemBodyIds.Earth, out var earth);
        for (var index = 0; index < 256; index++) Evaluate(index * Day, true);
        var allocationBefore = GC.GetAllocatedBytesForCurrentThread();
        var stopwatch = Stopwatch.StartNew();
        for (var index = 0; index < count; index++) Evaluate((index % 10_001 - 5_000) * Day, false);
        stopwatch.Stop();
        var baselineAllocated = GC.GetAllocatedBytesForCurrentThread() - allocationBefore;
        var baselineNanoseconds = stopwatch.Elapsed.TotalNanoseconds / count;
        allocationBefore = GC.GetAllocatedBytesForCurrentThread();
        stopwatch.Restart();
        for (var index = 0; index < count; index++) Evaluate((index % 10_001 - 5_000) * Day, true);
        stopwatch.Stop();
        var correctedAllocated = GC.GetAllocatedBytesForCurrentThread() - allocationBefore;
        return new(baselineNanoseconds, stopwatch.Elapsed.TotalNanoseconds / count, baselineAllocated / count, correctedAllocated / count, count);

        CartesianState Evaluate(long et, bool applyCorrection)
        {
            var requested = SimulationInstant.FromWholeSeconds(et);
            var propagationTime = requested;
            if (applyCorrection && !AnalyticalKeplerSecularCorrectionEvaluator.TryScaleTime(trajectory.Epoch, requested, correction, out propagationTime)) return default;
            var propagation = UniversalVariableTwoBodyPropagator.TryEvaluate(trajectory.StateAtEpoch, trajectory.Epoch, propagationTime, earth.GravitationalParameter);
            if (!propagation.Succeeded) return default;
            if (!applyCorrection) return propagation.State;
            return AnalyticalKeplerSecularCorrectionEvaluator.TryApply(propagation.State, trajectory.StateAtEpoch, trajectory.Epoch, requested, correction, out var corrected) ? corrected : default;
        }
    }

    internal static CompactLunarVersionPerformance MeasureLunarVersionPerformance(CelestialSystemDefinition v2, CelestialSystemDefinition v3)
    {
        const int count = 100_000;
        var v2Inputs = Inputs(v2); var v3Inputs = Inputs(v3);
        for (var index = 0; index < 256; index++) { _ = Evaluate(v2Inputs, index * Day); _ = Evaluate(v3Inputs, index * Day); }
        var allocationBefore = GC.GetAllocatedBytesForCurrentThread();
        var stopwatch = Stopwatch.StartNew();
        for (var index = 0; index < count; index++) _ = Evaluate(v2Inputs, (index % 10_001 - 5_000) * Day);
        stopwatch.Stop();
        var v2Allocated = GC.GetAllocatedBytesForCurrentThread() - allocationBefore;
        var v2Nanoseconds = stopwatch.Elapsed.TotalNanoseconds / count;
        allocationBefore = GC.GetAllocatedBytesForCurrentThread();
        stopwatch.Restart();
        for (var index = 0; index < count; index++) _ = Evaluate(v3Inputs, (index % 10_001 - 5_000) * Day);
        stopwatch.Stop();
        var v3Allocated = GC.GetAllocatedBytesForCurrentThread() - allocationBefore;
        return new(v2Nanoseconds, stopwatch.Elapsed.TotalNanoseconds / count, v2Allocated / count, v3Allocated / count, count);

        static (TwoBodyTrajectory Trajectory, double Mu, AnalyticalKeplerSecularCorrection Secular, AnalyticalKeplerPeriodicCorrection Periodic) Inputs(CelestialSystemDefinition system)
        {
            var moonIndex = FindTraversalIndex(system, SolarSystemBodyIds.Moon); var node = system.GetNodeInTraversalOrder(moonIndex);
            system.TryGetAnalyticalKepler(node.Ephemeris.PayloadIndex, out var trajectory); system.TryGetPhysicalProperties(SolarSystemBodyIds.Earth, out var earth);
            system.TryGetAnalyticalCorrection(node.Ephemeris.PayloadIndex, out var secular); system.TryGetAnalyticalPeriodicCorrection(node.Ephemeris.PayloadIndex, out var periodic);
            return (trajectory, earth.GravitationalParameter, secular, periodic);
        }

        static CartesianState Evaluate((TwoBodyTrajectory Trajectory, double Mu, AnalyticalKeplerSecularCorrection Secular, AnalyticalKeplerPeriodicCorrection Periodic) inputs, long et)
        {
            var requested = SimulationInstant.FromWholeSeconds(et);
            if (!AnalyticalKeplerSecularCorrectionEvaluator.TryScaleTime(inputs.Trajectory.Epoch, requested, inputs.Secular, out var propagationTime)) return default;
            var propagation = UniversalVariableTwoBodyPropagator.TryEvaluate(inputs.Trajectory.StateAtEpoch, inputs.Trajectory.Epoch, propagationTime, inputs.Mu);
            return propagation.Succeeded && AnalyticalKeplerSecularCorrectionEvaluator.TryApply(propagation.State, inputs.Trajectory.StateAtEpoch, inputs.Trajectory.Epoch, requested, inputs.Secular, inputs.Periodic, out var corrected) ? corrected : default;
        }
    }

    internal static bool TryFormatLunarEpochComparison(CspiceSession session, CelestialSystemDefinition system, CompactLunarCorrection selected, out string text, out string error)
    {
        text = string.Empty; error = string.Empty;
        var moonIndex = FindTraversalIndex(system, SolarSystemBodyIds.Moon); var moonNode = system.GetNodeInTraversalOrder(moonIndex);
        if (!system.TryGetAnalyticalKepler(moonNode.Ephemeris.PayloadIndex, out var trajectory) || !system.TryGetPhysicalProperties(SolarSystemBodyIds.Earth, out var earth) || !TryBuildLunarReferences(session, LunarDiagnosticEpochs, out var references)) { error = "Lunar epoch comparison inputs are unavailable."; return false; }
        var baseline = new CompactLunarCorrection(1d, 1d, 0d, 0d);
        var lines = new List<string> { "lunar-epoch,et,baseline_position_m,corrected_position_m,baseline_velocity_mps,corrected_velocity_mps,baseline_separation_m,corrected_separation_m" };
        foreach (var reference in references)
        {
            var before = EvaluateLunarCorrection(trajectory, earth.GravitationalParameter, reference.Et, baseline);
            var after = EvaluateLunarCorrection(trajectory, earth.GravitationalParameter, reference.Et, selected);
            lines.Add($"lunar-epoch,{reference.Et},{Magnitude(before.Position-reference.State.Position):R},{Magnitude(after.Position-reference.State.Position):R},{Magnitude(before.Velocity-reference.State.Velocity):R},{Magnitude(after.Velocity-reference.State.Velocity):R},{Math.Abs(Magnitude(before.Position)-Magnitude(reference.State.Position)):R},{Math.Abs(Magnitude(after.Position)-Magnitude(reference.State.Position)):R}");
        }
        text = string.Join(Environment.NewLine, lines); return true;
    }

    internal static bool TryConvertStateToElements(string name, in State state, double mu, out CompactSolarDerivedElements elements)
    {
        elements = default;
        var r = state.Position; var v = state.Velocity;
        var radius = Magnitude(r); var velocitySquared = v.LengthSquared;
        var h = Double3.Cross(r, v); var hMagnitude = Magnitude(h);
        var n = Double3.Cross(Double3.UnitZ, h); var nMagnitude = Magnitude(n);
        if (!r.IsFinite || !v.IsFinite || !double.IsFinite(mu) || mu <= 0d || radius <= 0d || hMagnitude <= 0d || nMagnitude <= 0d) return false;
        var eccentricityVector = r * ((velocitySquared - mu / radius) / mu) - v * (Double3.Dot(r, v) / mu);
        var eccentricity = Magnitude(eccentricityVector);
        var reciprocalA = 2d / radius - velocitySquared / mu;
        if (!double.IsFinite(eccentricity) || eccentricity is <= 0d or >= 1d || !double.IsFinite(reciprocalA) || reciprocalA <= 0d) return false;
        var semiMajor = 1d / reciprocalA;
        var inclination = Math.Acos(Math.Clamp(h.Z / hMagnitude, -1d, 1d));
        var node = Normalize(Math.Atan2(n.Y, n.X));
        var periapsis = Normalize(Math.Atan2(Double3.Dot(Double3.Cross(n, eccentricityVector), h) / (nMagnitude * eccentricity * hMagnitude), Double3.Dot(n, eccentricityVector) / (nMagnitude * eccentricity)));
        var trueAnomaly = Normalize(Math.Atan2(Double3.Dot(Double3.Cross(eccentricityVector, r), h) / (eccentricity * radius * hMagnitude), Double3.Dot(eccentricityVector, r) / (eccentricity * radius)));
        var eccentricAnomaly = Math.Atan2(Math.Sqrt(1d - eccentricity * eccentricity) * Math.Sin(trueAnomaly), eccentricity + Math.Cos(trueAnomaly));
        var meanAnomaly = Normalize(eccentricAnomaly - eccentricity * Math.Sin(eccentricAnomaly));
        elements = new(name, semiMajor, eccentricity, Degrees(inclination), Degrees(node), Degrees(periapsis), Degrees(meanAnomaly), mu);
        return double.IsFinite(semiMajor) && double.IsFinite(inclination) && double.IsFinite(node) && double.IsFinite(periapsis) && double.IsFinite(meanAnomaly);
    }

    private static (long Count, double Nanoseconds, long AllocatedBytes) MeasurePerformance(CelestialSystemDefinition system, ReferenceFrameEvaluation[] evaluations, FrameTransform[] roots, ReferenceFrameEvaluation[] staging, FrameTransform[] stagingRoots)
    {
        const int count = 100_000;
        for (var i = 0; i < 256; i++) _ = CelestialSystemEvaluator.TryEvaluateSystem(system, SimulationInstant.FromWholeSeconds(i * 31_557L), evaluations, roots, staging, stagingRoots);
        var allocationBefore = GC.GetAllocatedBytesForCurrentThread();
        var stopwatch = Stopwatch.StartNew();
        for (var i = 0; i < count; i++)
        {
            var time = SimulationInstant.FromWholeSeconds((i % 10_001 - 5_000) * 86_400L);
            if (!CelestialSystemEvaluator.TryEvaluateSystem(system, time, evaluations, roots, staging, stagingRoots).Succeeded) throw new InvalidOperationException("Performance evaluation failed.");
        }
        stopwatch.Stop();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocationBefore;
        return (count, stopwatch.Elapsed.TotalNanoseconds / count, allocated / count);
    }

    private static bool TryBuildLunarReferences(CspiceSession session, IEnumerable<long> epochs, out LunarReference[] references)
    {
        var values = new List<LunarReference>();
        foreach (var et in epochs)
        {
            var cache = new Dictionary<int, State>();
            if (!TryState(session, 301, et, cache, out var moon) || !TryState(session, 399, et, cache, out var earth)) { references = []; return false; }
            values.Add(new(et, moon - earth));
        }
        references = values.ToArray();
        return true;
    }

    private static CompactLunarCorrection OptimizeLunarCorrection(TwoBodyTrajectory trajectory, double mu, LunarReference[] references, CompactLunarCorrection start, bool optimizeMu, bool optimizeTime, bool optimizeNode, bool optimizePeriapsis)
    {
        var current = start;
        var currentError = LunarObjective(trajectory, mu, references, current);
        var muStep = .004d;
        var timeStep = .002d;
        var nodeStep = RadiansPerSecond(8d);
        var periapsisStep = RadiansPerSecond(12d);
        for (var level = 0; level < 11; level++)
        {
            if (optimizeMu) Improve(c => c with { GravitationalParameterScale = c.GravitationalParameterScale + muStep });
            if (optimizeTime) Improve(c => c with { TimeScale = c.TimeScale + timeStep });
            if (optimizeNode) Improve(c => c with { NodeRateRadiansPerSecond = c.NodeRateRadiansPerSecond + nodeStep });
            if (optimizePeriapsis) Improve(c => c with { PeriapsisRateRadiansPerSecond = c.PeriapsisRateRadiansPerSecond + periapsisStep });
            muStep *= .25d; timeStep *= .25d; nodeStep *= .25d; periapsisStep *= .25d;
        }
        return current;

        void Improve(Func<CompactLunarCorrection, CompactLunarCorrection> increment)
        {
            var plus = increment(current);
            var delta = new CompactLunarCorrection(2d * current.GravitationalParameterScale - plus.GravitationalParameterScale, 2d * current.TimeScale - plus.TimeScale, 2d * current.NodeRateRadiansPerSecond - plus.NodeRateRadiansPerSecond, 2d * current.PeriapsisRateRadiansPerSecond - plus.PeriapsisRateRadiansPerSecond);
            var plusError = LunarObjective(trajectory, mu, references, plus);
            var minusError = LunarObjective(trajectory, mu, references, delta);
            if (plusError < currentError && plusError <= minusError) { current = plus; currentError = plusError; }
            else if (minusError < currentError) { current = delta; currentError = minusError; }
        }
    }

    private static double LunarObjective(TwoBodyTrajectory trajectory, double mu, LunarReference[] references, CompactLunarCorrection correction)
    {
        double sum = 0d;
        foreach (var reference in references)
        {
            var candidate = EvaluateLunarCorrection(trajectory, mu, reference.Et, correction);
            if (!candidate.IsFinite) return double.PositiveInfinity;
            var error = candidate.Position - reference.State.Position;
            sum += error.LengthSquared;
        }
        return sum / references.Length;
    }

    private static CompactLunarCorrectionMetrics MeasureLunarCorrection(TwoBodyTrajectory trajectory, double mu, LunarReference[] references, CompactLunarCorrection correction)
    {
        double maxPosition = 0d, maxVelocity = 0d, maxSeparation = 0d, positionSquares = 0d, velocitySquares = 0d;
        long worstEt = 0;
        ulong hash = 14695981039346656037UL;
        foreach (var reference in references)
        {
            var candidate = EvaluateLunarCorrection(trajectory, mu, reference.Et, correction);
            var position = Magnitude(candidate.Position - reference.State.Position);
            var velocity = Magnitude(candidate.Velocity - reference.State.Velocity);
            var separation = Math.Abs(Magnitude(candidate.Position) - Magnitude(reference.State.Position));
            positionSquares += position * position; velocitySquares += velocity * velocity;
            if (position > maxPosition) { maxPosition = position; worstEt = reference.Et; }
            maxVelocity = Math.Max(maxVelocity, velocity); maxSeparation = Math.Max(maxSeparation, separation);
            HashLong(ref hash, reference.Et); HashLong(ref hash, BitConverter.DoubleToInt64Bits(position)); HashLong(ref hash, BitConverter.DoubleToInt64Bits(velocity));
        }
        return new(maxPosition, Math.Sqrt(positionSquares / references.Length), maxVelocity, Math.Sqrt(velocitySquares / references.Length), maxSeparation, worstEt, hash);
    }

    private static CartesianState EvaluateLunarCorrection(TwoBodyTrajectory trajectory, double mu, long et, CompactLunarCorrection correction)
    {
        var scaledTime = SimulationInstant.FromSecondsRounded(et * correction.TimeScale);
        var propagation = UniversalVariableTwoBodyPropagator.TryEvaluate(trajectory.StateAtEpoch, trajectory.Epoch, scaledTime, mu * correction.GravitationalParameterScale);
        if (!propagation.Succeeded) return default;
        var seconds = et;
        var epochNormal = Double3.Cross(trajectory.StateAtEpoch.Position, trajectory.StateAtEpoch.Velocity);
        epochNormal /= Magnitude(epochNormal);
        var periapsisAngle = correction.PeriapsisRateRadiansPerSecond * seconds;
        var positionAfterPeriapsis = Rotate(propagation.State.Position, epochNormal, periapsisAngle);
        var velocityAfterPeriapsis = Rotate(propagation.State.Velocity * correction.TimeScale, epochNormal, periapsisAngle) + Double3.Cross(epochNormal * correction.PeriapsisRateRadiansPerSecond, positionAfterPeriapsis);
        var nodeAngle = correction.NodeRateRadiansPerSecond * seconds;
        var nodeAxis = EclipticNorth;
        var position = Rotate(positionAfterPeriapsis, nodeAxis, nodeAngle);
        var velocity = Rotate(velocityAfterPeriapsis, nodeAxis, nodeAngle) + Double3.Cross(nodeAxis * correction.NodeRateRadiansPerSecond, position);
        return new(position, velocity);
    }

    private static Double3 Rotate(Double3 value, Double3 axis, double angle)
    {
        var sine = Math.Sin(angle); var cosine = Math.Cos(angle);
        return value * cosine + Double3.Cross(axis, value) * sine + axis * (Double3.Dot(axis, value) * (1d - cosine));
    }

    private static double RadiansPerSecond(double degreesPerJulianYear) => degreesPerJulianYear * Math.PI / 180d / JulianYear;
    private static double DegreesPerYear(double radiansPerSecond) => radiansPerSecond * JulianYear * 180d / Math.PI;
    private static double MeanDeltaDegreesPerDay(double scale)
    {
        const double baseMeanMotionRadiansPerSecond = 2.649062428385065e-6d;
        return (scale - 1d) * baseMeanMotionRadiansPerSecond * Day * 180d / Math.PI;
    }
    private static void HashLong(ref ulong hash, long value) { unchecked { for (var index = 0; index < 8; index++) { hash ^= (byte)value; hash *= 1099511628211UL; value >>= 8; } } }
    private static Double3 EclipticNorth { get { const double obliquity = 23.439291111d * Math.PI / 180d; return new(0d, -Math.Sin(obliquity), Math.Cos(obliquity)); } }

    private static bool TryState(CspiceSession session, int target, long et, Dictionary<int, State> cache, out State state)
    {
        if (target == 0) { state = default; return true; }
        if (cache.TryGetValue(target, out state)) return true;
        if (!session.TryQuery(target, et, out var source)) return false;
        state = new(new(source.X * 1000d, source.Y * 1000d, source.Z * 1000d), new(source.Vx * 1000d, source.Vy * 1000d, source.Vz * 1000d));
        cache.Add(target, state);
        return true;
    }

    private static int FindTraversalIndex(CelestialSystemDefinition system, CelestialBodyId id)
    {
        for (var i = 0; i < system.Count; i++) if (system.GetNodeInTraversalOrder(i).Id == id) return i;
        return -1;
    }

    private static double Magnitude(Double3 value) => Math.Sqrt(value.LengthSquared);
    private static double Normalize(double angle) { var value = angle % (2d * Math.PI); return value < 0d ? value + 2d * Math.PI : value; }
    private static double Degrees(double radians) => radians * 180d / Math.PI;

    private static ulong Hash(IEnumerable<CompactSolarBodyMetrics> metrics)
    {
        ulong hash = 14695981039346656037UL;
        void Add(long value) { unchecked { hash ^= (ulong)value; hash *= 1099511628211UL; } }
        foreach (var body in metrics)
        {
            Add((long)body.BodyId); Add(BitConverter.DoubleToInt64Bits(body.MaximumPositionErrorMetres)); Add(BitConverter.DoubleToInt64Bits(body.RmsPositionErrorMetres));
            Add(BitConverter.DoubleToInt64Bits(body.MaximumVelocityErrorMetresPerSecond)); Add(BitConverter.DoubleToInt64Bits(body.RmsVelocityErrorMetresPerSecond)); Add(body.WorstPositionEt); Add(body.WorstVelocityEt);
        }
        return hash;
    }

    private readonly record struct BodyContract(string Name, CelestialBodyId Id, int Target, int ParentTarget);
    private readonly record struct LunarReference(long Et, State State);
    internal readonly record struct State(Double3 Position, Double3 Velocity)
    {
        public static State operator -(State left, State right) => new(left.Position - right.Position, left.Velocity - right.Velocity);
    }

    private sealed class Accumulator(BodyContract body)
    {
        private double _positionSquares, _velocitySquares, _maximumPosition, _maximumVelocity, _maximumRadial, _maximumTransverse, _epochPosition, _epochVelocity;
        private long _worstPositionEt, _worstVelocityEt;
        private int _count;

        internal void Add(long et, State runtime, State reference)
        {
            var positionDelta = runtime.Position - reference.Position;
            var velocityDelta = runtime.Velocity - reference.Velocity;
            var position = Magnitude(positionDelta);
            var velocity = Magnitude(velocityDelta);
            var referenceRadius = Magnitude(reference.Position);
            var radialSigned = referenceRadius > 0 ? Double3.Dot(positionDelta, reference.Position) / referenceRadius : position;
            var radial = Math.Abs(radialSigned);
            var transverse = Math.Sqrt(Math.Max(0d, position * position - radialSigned * radialSigned));
            _positionSquares += position * position; _velocitySquares += velocity * velocity; _count++;
            if (et == 0) { _epochPosition = position; _epochVelocity = velocity; }
            if (position > _maximumPosition) { _maximumPosition = position; _worstPositionEt = et; }
            if (velocity > _maximumVelocity) { _maximumVelocity = velocity; _worstVelocityEt = et; }
            _maximumRadial = Math.Max(_maximumRadial, radial); _maximumTransverse = Math.Max(_maximumTransverse, transverse);
        }

        internal CompactSolarBodyMetrics Build() => new(body.Name, body.Id.Value, body.Target, body.ParentTarget,
            _maximumPosition, Math.Sqrt(_positionSquares / _count), _maximumVelocity, Math.Sqrt(_velocitySquares / _count),
            _maximumRadial, _maximumTransverse, _epochPosition, _epochVelocity, _worstPositionEt, _worstVelocityEt);
    }
}
