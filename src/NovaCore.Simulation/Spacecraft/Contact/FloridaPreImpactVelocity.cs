using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Spacecraft.Contact;

internal readonly partial struct FloridaContactMotion
{
    /// <summary>One bounded evaluation on the supplied enclosure; no root selection or refinement.</summary>
    internal bool PreImpactVelocities(FloridaBound time, out FloridaVector comVelocity, out FloridaVector relativeVelocity)
    {
        comVelocity = relativeVelocity = default;
        if (!Evaluate(time, out _, out var bodyRelativeDerivative)) return false;
        var elapsed = time - FloridaBound.Integer(linear.Epoch.Ticks) / SimulationInstant.TicksPerSecond;
        var com = FloridaVector.From(linear.VelocityRoot) + craftAcceleration * elapsed;
        // Inverse rotation of the FULL body-relative derivative includes Earth translation and every
        // Euler rate. At alpha the feature and material point coincide. Craft spin remains exactly zero.
        var relative = RootDirection(orientation, time, bodyRelativeDerivative);
        if (!com.IsFinite || !relative.IsFinite) return false;
        comVelocity = com;
        relativeVelocity = relative;
        return true;
    }
}

internal sealed partial class FloridaContactProvider
{
    internal readonly partial struct Proof
    {
        internal CertifiedPreImpactVelocityResult QualifyPreImpactVelocity(in Kinematics kinematics,
            in CertifiedPreImpactVelocityRequest request, in FloridaContactUse current) =>
            PreImpactVelocity.Qualify(this, kinematics, request, current);

        /// <summary>
        /// Privately issued pre-impact evidence for later final-velocity realization. Not an executable
        /// command, mutable root-local state, or rational event epoch. Every use rechecks source authority.
        /// </summary>
        internal readonly struct PreImpactVelocity
        {
            internal const uint PolicyVersion = 1;
            internal const uint NumericalVersion = 1;
            private readonly Proof root;
            private readonly Kinematics source;
            private readonly CertifiedPreImpactVelocityRequest request;
            private readonly CertifiedPreImpactVelocityValues values;

            private PreImpactVelocity(in Proof root, in Kinematics source, in CertifiedPreImpactVelocityRequest request,
                in CertifiedPreImpactVelocityValues values)
            { this.root = root; this.source = source; this.request = request; this.values = values; }

            internal CertifiedPreImpactVelocityStatus Read(in Proof expectedRoot, in Kinematics expectedSource,
                in CertifiedPreImpactVelocityRequest expectedRequest, in FloridaContactUse current,
                out CertifiedPreImpactVelocityValues result)
            {
                result = default;
                // Reuse the banked exact root/kinematics tuple comparison, not response arithmetic.
                if (!root.IsRoot || !root.SameResponseInput(expectedRoot) ||
                    !source.MatchesResponseQualification(expectedSource) || request != expectedRequest)
                    return CertifiedPreImpactVelocityStatus.Unsupported;
                var status = source.Read(root, current, out _);
                if (status != FloridaKinematicsStatus.Qualified)
                    return status == FloridaKinematicsStatus.Stale
                        ? CertifiedPreImpactVelocityStatus.Stale : CertifiedPreImpactVelocityStatus.Unsupported;
                result = values;
                return CertifiedPreImpactVelocityStatus.Qualified;
            }

            internal static CertifiedPreImpactVelocityResult Qualify(in Proof root, in Kinematics source,
                in CertifiedPreImpactVelocityRequest request, in FloridaContactUse current)
            {
                var status = source.Read(root, current, out var input);
                if (status != FloridaKinematicsStatus.Qualified)
                    return status == FloridaKinematicsStatus.Stale
                        ? new(CertifiedPreImpactVelocityStatus.Stale, CertifiedPreImpactVelocityFailure.ChangedAuthority)
                        : new(CertifiedPreImpactVelocityStatus.Unsupported, CertifiedPreImpactVelocityFailure.InvalidWitness);
                if (!request.IsValid)
                    return new(CertifiedPreImpactVelocityStatus.Unsupported, CertifiedPreImpactVelocityFailure.InvalidRequest);
                // The witness may have narrowed alpha beyond the original proof. Consume ITS qualified
                // enclosure, preserving the root/source relation already used by the M14.11 proposal.
                if (!root.owner!.motion.PreImpactVelocities(input.RootEnclosure, out var com, out var relative))
                    return new(CertifiedPreImpactVelocityStatus.Unresolved, CertifiedPreImpactVelocityFailure.NumericalResolution);
                if (!FloridaKinematicsRequest.Fits(com, request.ComVelocityComponentMetresPerSecond) ||
                    !FloridaKinematicsRequest.Fits(relative, request.RelativeVelocityComponentMetresPerSecond))
                    return new(CertifiedPreImpactVelocityStatus.Unresolved, CertifiedPreImpactVelocityFailure.RequestedWidth);
                return new(CertifiedPreImpactVelocityStatus.Qualified, CertifiedPreImpactVelocityFailure.None,
                    new(root, source, request, new(com, relative)));
            }
        }
    }
}
