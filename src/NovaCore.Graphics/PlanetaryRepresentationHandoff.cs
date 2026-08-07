using NovaCore.Core;

namespace NovaCore.Graphics;

/// <summary>Presentation-only whole-body/detail renderer state.</summary>
public enum PlanetaryRenderRegime : byte
{
    DistantOnly,
    Transition,
    DetailedOnly,
}

public readonly record struct PlanetaryRepresentationHandoffConfiguration(
    double DetailedOnlyMaximumDistanceRadii,
    double DistantOnlyMinimumDistanceRadii,
    double HysteresisRadii)
{
    public bool IsValid =>
        double.IsFinite(DetailedOnlyMaximumDistanceRadii) && DetailedOnlyMaximumDistanceRadii > 1d &&
        double.IsFinite(DistantOnlyMinimumDistanceRadii) && DistantOnlyMinimumDistanceRadii > DetailedOnlyMaximumDistanceRadii &&
        double.IsFinite(HysteresisRadii) && HysteresisRadii >= 0d &&
        HysteresisRadii * 2d < DistantOnlyMinimumDistanceRadii - DetailedOnlyMaximumDistanceRadii;
}

public readonly record struct PlanetaryRepresentationBlend(
    PlanetaryRenderRegime Regime,
    double DistanceRadii,
    float DistantAlpha,
    float DetailedAlpha)
{
    public bool DrawDistant => Regime is not PlanetaryRenderRegime.DetailedOnly;
    public bool DrawDetailed => Regime is not PlanetaryRenderRegime.DistantOnly;
}

/// <summary>Deterministic camera/presentation-only two-regime selector with boundary hysteresis.</summary>
public sealed class PlanetaryRepresentationHandoff
{
    private readonly PlanetaryRepresentationHandoffConfiguration _configuration;
    private bool _initialized;
    private PlanetaryRenderRegime _regime;

    public PlanetaryRepresentationHandoff(in PlanetaryRepresentationHandoffConfiguration configuration)
    {
        if (!configuration.IsValid) throw new ArgumentOutOfRangeException(nameof(configuration));
        _configuration = configuration;
    }

    public PlanetaryRepresentationBlend Update(in PlanetRenderProxy body, in Double3 cameraRootPosition)
    {
        var distance = Math.Sqrt((cameraRootPosition - body.Position.Value).LengthSquared);
        var metric = distance / body.RadiusMetres;
        if (!double.IsFinite(metric)) throw new ArgumentOutOfRangeException(nameof(cameraRootPosition));

        if (!_initialized)
        {
            _regime = metric <= _configuration.DetailedOnlyMaximumDistanceRadii
                ? PlanetaryRenderRegime.DetailedOnly
                : metric >= _configuration.DistantOnlyMinimumDistanceRadii
                    ? PlanetaryRenderRegime.DistantOnly
                    : PlanetaryRenderRegime.Transition;
            _initialized = true;
        }
        else
        {
            _regime = _regime switch
            {
                PlanetaryRenderRegime.DistantOnly when metric < _configuration.DetailedOnlyMaximumDistanceRadii => PlanetaryRenderRegime.DetailedOnly,
                PlanetaryRenderRegime.DistantOnly when metric < _configuration.DistantOnlyMinimumDistanceRadii - _configuration.HysteresisRadii => PlanetaryRenderRegime.Transition,
                PlanetaryRenderRegime.DetailedOnly when metric > _configuration.DistantOnlyMinimumDistanceRadii => PlanetaryRenderRegime.DistantOnly,
                PlanetaryRenderRegime.DetailedOnly when metric > _configuration.DetailedOnlyMaximumDistanceRadii + _configuration.HysteresisRadii => PlanetaryRenderRegime.Transition,
                PlanetaryRenderRegime.Transition when metric <= _configuration.DetailedOnlyMaximumDistanceRadii - _configuration.HysteresisRadii => PlanetaryRenderRegime.DetailedOnly,
                PlanetaryRenderRegime.Transition when metric >= _configuration.DistantOnlyMinimumDistanceRadii + _configuration.HysteresisRadii => PlanetaryRenderRegime.DistantOnly,
                _ => _regime,
            };
        }

        var detailed = (float)Math.Clamp(
            (_configuration.DistantOnlyMinimumDistanceRadii - metric) /
            (_configuration.DistantOnlyMinimumDistanceRadii - _configuration.DetailedOnlyMaximumDistanceRadii),
            0d,
            1d);
        if (_regime == PlanetaryRenderRegime.DistantOnly) detailed = 0f;
        else if (_regime == PlanetaryRenderRegime.DetailedOnly) detailed = 1f;
        return new(_regime, metric, 1f - detailed, detailed);
    }
}
