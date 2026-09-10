namespace NovaCore.Simulation.Spacecraft.Contact;

internal sealed partial class FloridaContactProvider
{
    internal readonly partial struct Proof
    {
        // Exact receipt matching adds no mutable root version and does not revoke immutable refinements.
        private bool SameResponseInput(in Proof other) => ReferenceEquals(owner, other.owner) &&
            RootEnclosure == other.RootEnclosure && Function == other.Function && Derivative == other.Derivative &&
            RefinementCount == other.RefinementCount;

        internal readonly partial struct Kinematics
        {
            private bool SameResponseInput(in Kinematics other) => original.SameResponseInput(other.original) &&
                values.RootEnclosure == other.values.RootEnclosure && values.NormalRoot == other.values.NormalRoot &&
                values.NormalVelocityMetresPerSecond == other.values.NormalVelocityMetresPerSecond &&
                values.AuthoredLeverBodyMetres == other.values.AuthoredLeverBodyMetres &&
                values.LeverRootMetres == other.values.LeverRootMetres && values.FixedAttitude == other.values.FixedAttitude &&
                values.MassKilograms == other.values.MassKilograms && values.PrincipalInertia == other.values.PrincipalInertia &&
                values.TerrainAuthority == other.values.TerrainAuthority && values.AdditionalRefinements == other.values.AdditionalRefinements;

            internal bool MatchesResponseQualification(in Kinematics other) => SameResponseInput(other);
        }

        internal CertifiedResponseResult QualifyResponse(in Kinematics kinematics, in CertifiedResponseRequest request,
            in FloridaContactUse current) => ResponseProposal.Qualify(this, kinematics, request, current);

        /// <summary>
        /// Current natural-terrain-only, complete single-feature isolated specialization. Composition with
        /// competing collision systems is unsupported; this receipt is not a universal isolation certificate.
        /// Retains the exact root/qualification tuple. No numerical command, event time or replacement state.
        /// </summary>
        internal readonly struct ResponseProposal
        {
            internal const uint PolicyVersion = 1;
            internal const uint NumericalVersion = 1;
            private readonly Proof root;
            private readonly Kinematics source;
            private readonly CertifiedResponseRequest request;
            private readonly CertifiedResponseValues values;

            private ResponseProposal(in Proof root, in Kinematics source, in CertifiedResponseRequest request,
                in CertifiedResponseValues values)
            { this.root = root; this.source = source; this.request = request; this.values = values; }

            internal CertifiedResponseStatus Read(in Proof expectedRoot, in Kinematics expectedSource,
                in CertifiedResponseRequest expectedRequest, in FloridaContactUse current, out CertifiedResponseValues result)
            {
                result = default;
                if (!root.IsRoot || !root.SameResponseInput(expectedRoot) ||
                    !source.MatchesResponseQualification(expectedSource) || request != expectedRequest)
                    return CertifiedResponseStatus.Unsupported;
                var status = source.Read(root, current, out _);
                if (status != FloridaKinematicsStatus.Qualified)
                    return status == FloridaKinematicsStatus.Stale ? CertifiedResponseStatus.Stale : CertifiedResponseStatus.Unsupported;
                result = values;
                return CertifiedResponseStatus.Qualified;
            }

            internal static CertifiedResponseResult Qualify(in Proof root, in Kinematics source,
                in CertifiedResponseRequest request, in FloridaContactUse current)
            {
                var status = source.Read(root, current, out var input);
                if (status != FloridaKinematicsStatus.Qualified)
                    return status == FloridaKinematicsStatus.Stale
                        ? new(CertifiedResponseStatus.Stale, CertifiedResponseFailure.ChangedAuthority)
                        : new(CertifiedResponseStatus.Unsupported, CertifiedResponseFailure.InvalidWitness);
                var failure = CertifiedContactResponseMath.Enclose(input.NormalRoot, input.NormalVelocityMetresPerSecond,
                    input.AuthoredLeverBodyMetres, input.FixedAttitude, input.MassKilograms, input.PrincipalInertia, request, out var values);
                if (failure != CertifiedResponseFailure.None)
                    return new(failure is CertifiedResponseFailure.InvalidRequest or CertifiedResponseFailure.InvalidPhysicalState
                        ? CertifiedResponseStatus.Unsupported : CertifiedResponseStatus.Unresolved, failure);
                return new(CertifiedResponseStatus.Qualified, CertifiedResponseFailure.None, new(root, source, request, values));
            }
        }
    }
}
