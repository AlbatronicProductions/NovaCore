using NovaCore.Core;

namespace NovaCore.Simulation.Spacecraft.Contact;

internal enum CertifiedPreImpactVelocityStatus : byte { Unresolved, Qualified, Unsupported, Stale }
internal enum CertifiedPreImpactVelocityFailure : byte
{
    None, InvalidWitness, InvalidRequest, ChangedAuthority, NumericalResolution, RequestedWidth,
}

/// <summary>Maximum FULL component widths in m/s. These are qualification limits, not contact tolerances.</summary>
internal readonly record struct CertifiedPreImpactVelocityRequest(double ComVelocityComponentMetresPerSecond,
    double RelativeVelocityComponentMetresPerSecond)
{
    internal bool IsValid => Positive(ComVelocityComponentMetresPerSecond) && Positive(RelativeVelocityComponentMetresPerSecond);
    private static bool Positive(double value) => double.IsFinite(value) && value > 0;
}

/// <summary>
/// Bounds on the COM root velocity and full feature/material-relative root velocity at the SAME alpha.
/// Under the current admitted zero-spin branch, V_surface = V_COM - V_relative. Endpoints are not
/// independently selectable states. This constructible data is not proof or replacement authority.
/// </summary>
internal readonly record struct CertifiedPreImpactVelocityValues(FloridaVector ComVelocityRoot,
    FloridaVector RelativeVelocityRoot)
{
    // This exact fact belongs to checked source admission; copying the data does not establish it.
    internal Double3 AngularVelocityBody => Double3.Zero;
}

internal readonly record struct CertifiedPreImpactVelocityResult(CertifiedPreImpactVelocityStatus Status,
    CertifiedPreImpactVelocityFailure Failure, FloridaContactProvider.Proof.PreImpactVelocity Tuple = default);
