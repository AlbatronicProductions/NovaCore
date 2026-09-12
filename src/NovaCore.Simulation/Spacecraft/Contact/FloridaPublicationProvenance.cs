namespace NovaCore.Simulation.Spacecraft.Contact;

/// <summary>Original qualification inputs for deterministic reconstruction, never a capability.</summary>
internal readonly record struct ContinuationQualificationInputs(FloridaKinematicsRequest Kinematics,
    CertifiedResponseRequest Response, CertifiedPreImpactVelocityRequest Preimpact,
    PrivatePostImpactPoseRequest Pose, int RealizationCandidateIndex);

internal sealed partial class FloridaContactProvider
{
    internal readonly partial struct Proof
    {
        internal readonly partial struct PrivatePostImpactState
        {
            // The publisher uses this only after Read succeeds. No live provider enters history.
            internal ContinuationQualificationInputs PublicationInputs => new(witness.CapturedRequest,
                responseRequest, preimpactRequest, poseRequest, realization.RecordedCandidateIndex);
        }
    }
}
