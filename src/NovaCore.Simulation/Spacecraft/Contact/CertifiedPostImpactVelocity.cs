using NovaCore.Core;

namespace NovaCore.Simulation.Spacecraft.Contact;

internal enum CertifiedPostImpactVelocityStatus : byte { Unresolved, Qualified, Unsupported, Stale }
internal enum CertifiedPostImpactVelocityFailure : byte
{
    None, InvalidWitness, ChangedAuthority, UnsupportedArithmetic, InvalidNumericalInput,
    NonFinite, UnresolvedResponse, NoAdmissibleCandidate,
}

/// <summary>A priori ceilings from source intervals and the fixed candidate recipe, never fitted to a survivor.</summary>
internal readonly record struct CertifiedPostImpactVelocityBudgets(Double3 LinearComponent, Double3 AngularComponent,
    double NormalResidual, double MassWeightedError, double Tangential, double Coupling, double EnergyDefect,
    double IncomingSpeedLower, double DissipationLower);

/// <summary>
/// FINAL velocity values, not increments or an impulse command. Constructible diagnostic data is not
/// source authority, mutation permission, or a consumed token. Only the provider's checked receipt qualifies it.
/// All intervals describe the same original alpha; their endpoints are not independently selectable states.
/// </summary>
internal readonly record struct CertifiedPostImpactVelocityValues(
    Double3 LinearVelocityRoot, Double3 AngularVelocityBody,
    FloridaVector IdealLinearVelocityRoot, FloridaVector IdealAngularVelocityBody,
    FloridaVector LinearError, FloridaVector AngularError,
    FloridaVector EffectiveLinearMomentumRoot, FloridaVector EffectiveAngularMomentumBody,
    FloridaVector LinearMomentumDefectRoot, FloridaVector AngularMomentumDefectBody,
    FloridaBound NormalResidual, FloridaBound WorkAdjustedEnergy, FloridaBound EnergyDefect,
    double TangentialBound, double CouplingBound, double MassWeightedErrorBound,
    CertifiedPostImpactVelocityBudgets Budgets,
    int CandidateIndex, int CandidatesEvaluated, int AdmissibleCandidates);

internal readonly record struct CertifiedPostImpactVelocityResult(CertifiedPostImpactVelocityStatus Status,
    CertifiedPostImpactVelocityFailure Failure, FloridaContactProvider.Proof.PostImpactVelocity Realization = default);
