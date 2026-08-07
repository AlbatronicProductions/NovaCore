using NovaCore.Core;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Celestial;

/// <summary>Immutable node-to-catalog reference. Nodes carry no model payload.</summary>
internal readonly record struct CelestialEphemerisBinding(CelestialTrajectoryModel Model, CelestialEphemerisSourceId SourceId, int PayloadIndex)
{
    internal bool IsDefault => !SourceId.IsValid;
}

/// <summary>One declared immutable source and the one model catalog it supplies.</summary>
internal readonly record struct CelestialEphemerisSource(CelestialEphemerisSourceId Id, CelestialTrajectoryModel Model, CelestialEphemerisMetadata Metadata);

/// <summary>Parent-relative static state. Orientation is local-to-parent.</summary>
internal readonly record struct FixedBodyEphemerisPayload(Double3 Position, Double3 Velocity, DoubleQuaternion Orientation, Double3 AngularVelocity)
{
    internal bool IsFinite => Position.IsFinite && Velocity.IsFinite && Orientation.IsFinite && AngularVelocity.IsFinite;
    internal bool IsCanonical => IsFinite && Orientation.LengthSquared > 0d && Math.Abs(Orientation.LengthSquared - 1d) <= 1e-12d &&
        (Orientation.W > 0d || (Orientation.W == 0d && (Orientation.X > 0d || (Orientation.X == 0d && (Orientation.Y > 0d || (Orientation.Y == 0d && Orientation.Z >= 0d))))));
    internal static FixedBodyEphemerisPayload Identity => new(Double3.Zero, Double3.Zero, DoubleQuaternion.Identity, Double3.Zero);
}

/// <summary>Immutable circular-orbit parameters in the owning system's ephemeris domain.</summary>
internal readonly record struct CircularOrbitEphemerisPayload(long EpochDomainTicks, double Radius, double InitialPhaseRadians, DoubleQuaternion PlaneOrientation, double CentralGravitationalParameter)
{
    internal bool IsValid => double.IsFinite(Radius) && Radius > 0d && double.IsFinite(InitialPhaseRadians) && PlaneOrientation.IsFinite &&
        PlaneOrientation.LengthSquared > 0d && Math.Abs(PlaneOrientation.LengthSquared - 1d) <= 1e-12d && double.IsFinite(CentralGravitationalParameter) && CentralGravitationalParameter > 0d;

    /// <summary>Builds the epoch Cartesian state without assigning trajectory ownership to a hierarchy node.</summary>
    internal CartesianState ToCartesianState()
    {
        var localPosition = new Double3(Radius * Math.Cos(InitialPhaseRadians), Radius * Math.Sin(InitialPhaseRadians), 0d);
        var speed = Math.Sqrt(CentralGravitationalParameter / Radius);
        var localVelocity = new Double3(-speed * Math.Sin(InitialPhaseRadians), speed * Math.Cos(InitialPhaseRadians), 0d);
        return new CartesianState(PlaneOrientation.Rotate(localPosition), PlaneOrientation.Rotate(localVelocity));
    }
}

/// <summary>Optional compact secular evolution applied around an analytical two-body seed.</summary>
internal readonly record struct AnalyticalKeplerSecularCorrection(double TimeScaleDelta, Double3 ReferencePlaneAngularVelocity, double PeriapsisRateRadiansPerSecond)
{
    internal double TimeScale => 1d + TimeScaleDelta;
    internal bool IsIdentity => TimeScaleDelta == 0d && ReferencePlaneAngularVelocity == Double3.Zero && PeriapsisRateRadiansPerSecond == 0d;
    internal bool IsValid => double.IsFinite(TimeScaleDelta) && TimeScale > 0d && ReferencePlaneAngularVelocity.IsFinite && double.IsFinite(PeriapsisRateRadiansPerSecond);
}

/// <summary>One bounded epoch-relative periodic radial/in-plane correction term.</summary>
internal readonly record struct AnalyticalKeplerPeriodicTerm(
    double AngularFrequencyRadiansPerSecond,
    double RadialSineAmplitudeMetres,
    double RadialCosineAmplitudeMetres,
    double PhaseSineAmplitudeRadians,
    double PhaseCosineAmplitudeRadians)
{
    internal bool IsValid => double.IsFinite(AngularFrequencyRadiansPerSecond) && AngularFrequencyRadiansPerSecond > 0d &&
        double.IsFinite(RadialSineAmplitudeMetres) && double.IsFinite(RadialCosineAmplitudeMetres) &&
        double.IsFinite(PhaseSineAmplitudeRadians) && double.IsFinite(PhaseCosineAmplitudeRadians);
}

/// <summary>Small immutable periodic catalog entry parallel to one analytical trajectory.</summary>
internal sealed class AnalyticalKeplerPeriodicCorrection
{
    private readonly AnalyticalKeplerPeriodicTerm[] _terms;

    internal AnalyticalKeplerPeriodicCorrection(ReadOnlySpan<AnalyticalKeplerPeriodicTerm> terms)
    {
        if (terms.Length > 8) throw new ArgumentOutOfRangeException(nameof(terms));
        _terms = terms.ToArray();
    }

    internal static AnalyticalKeplerPeriodicCorrection Identity { get; } = new([]);
    internal int Count => _terms.Length;
    internal bool IsIdentity => _terms.Length == 0;
    internal bool IsValid { get { for (var index = 0; index < _terms.Length; index++) if (!_terms[index].IsValid) return false; return true; } }
    internal AnalyticalKeplerPeriodicTerm GetTerm(int index) => _terms[index];
}
