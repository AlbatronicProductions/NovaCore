namespace NovaCore.Simulation.Celestial;

/// <summary>Controlled outcomes for pure two-body Cartesian evaluation.</summary>
internal enum TwoBodyPropagationStatus : byte
{
    Success = 0,
    BodyNotFound,
    NoTrajectory,
    InvalidTrajectory,
    CentralBodyNotFound,
    PrimaryCentralMismatch,
    InvalidGravitationalParameter,
    UnsupportedModel,
    NonFiniteState,
    DegenerateRadius,
    DegenerateAngularMomentum,
    HyperbolicUnsupported,
    NearParabolicUnsupported,
    EvaluationSpanExceeded,
    NonFiniteIntermediate,
    NonConvergent,
    NonFiniteOutput,
}
