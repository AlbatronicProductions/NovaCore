namespace NovaCore.Simulation.Celestial.Transactions;

/// <summary>Controlled pure-evaluation outcomes for one exact-time inertial impulse intent.</summary>
internal enum CelestialImpulseEvaluationStatus : byte
{
    Success = 0, WrongEventKind, PayloadMismatch, EventTimeMismatch, InvalidSubject, InvalidDeltaVelocity,
    ZeroDeltaVelocity, SubjectNotFound, RootBody, NoCurrentTrajectory, PropagationFailed, NonFiniteResult,
    UnsupportedResultingOrbit, ReplacementCandidateRejected,
}
