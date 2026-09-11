namespace NovaCore.Simulation.Spacecraft.Contact;

internal sealed partial class FloridaContactProvider
{
    internal readonly partial struct Proof
    {
        internal CertifiedPostImpactVelocityResult QualifyPostImpactVelocity(in Kinematics witness,
            in ResponseProposal response, in CertifiedResponseRequest responseRequest,
            in PreImpactVelocity preimpact, in CertifiedPreImpactVelocityRequest preimpactRequest,
            in FloridaContactUse current) =>
            PostImpactVelocity.Qualify(this, witness, response, responseRequest, preimpact, preimpactRequest, current);

        /// <summary>
        /// Privately issued numerical evidence. Read before execution under the existing single-writer
        /// applicability contract. Does not advance time, install state, emit an intent, or grant mutation rights.
        /// A future executor must install the final bits without reapplying an impulse.
        /// </summary>
        internal readonly struct PostImpactVelocity
        {
            internal const uint PolicyVersion = 1;
            internal const uint NumericalVersion = 1;
            internal const uint RecipeVersion = 1;
            private readonly Proof root;
            private readonly Kinematics witness;
            private readonly ResponseProposal response;
            private readonly CertifiedResponseRequest responseRequest;
            private readonly PreImpactVelocity preimpact;
            private readonly CertifiedPreImpactVelocityRequest preimpactRequest;
            private readonly CertifiedPostImpactVelocityValues values;

            private PostImpactVelocity(in Proof root, in Kinematics witness, in ResponseProposal response,
                in CertifiedResponseRequest responseRequest, in PreImpactVelocity preimpact,
                in CertifiedPreImpactVelocityRequest preimpactRequest, in CertifiedPostImpactVelocityValues values)
            {
                this.root = root; this.witness = witness; this.response = response; this.responseRequest = responseRequest;
                this.preimpact = preimpact; this.preimpactRequest = preimpactRequest; this.values = values;
            }

            internal CertifiedPostImpactVelocityStatus Read(in Proof expectedRoot, in Kinematics expectedWitness,
                in ResponseProposal expectedResponse, in CertifiedResponseRequest expectedResponseRequest,
                in PreImpactVelocity expectedPreimpact, in CertifiedPreImpactVelocityRequest expectedPreimpactRequest,
                in FloridaContactUse current, out CertifiedPostImpactVelocityValues result)
            {
                result = default;
                if (!root.IsRoot || !root.SameResponseInput(expectedRoot) ||
                    !witness.MatchesResponseQualification(expectedWitness) || responseRequest != expectedResponseRequest ||
                    preimpactRequest != expectedPreimpactRequest) return CertifiedPostImpactVelocityStatus.Unsupported;
                var status = ReadInputs(root, witness, response, responseRequest, preimpact, preimpactRequest, current,
                    out _, out var storedResponse, out var storedPreimpact);
                if (status != CertifiedPostImpactVelocityStatus.Qualified) return status;
                status = ReadInputs(expectedRoot, expectedWitness, expectedResponse, expectedResponseRequest,
                    expectedPreimpact, expectedPreimpactRequest, current, out _, out var suppliedResponse, out var suppliedPreimpact);
                if (status != CertifiedPostImpactVelocityStatus.Qualified) return status;
                if (storedResponse != suppliedResponse || storedPreimpact != suppliedPreimpact)
                    return CertifiedPostImpactVelocityStatus.Unsupported;
                if (!CertifiedPostImpactVelocityMath.ArithmeticSupported()) return CertifiedPostImpactVelocityStatus.Unresolved;
                result = values;
                return CertifiedPostImpactVelocityStatus.Qualified;
            }

            private static CertifiedPostImpactVelocityStatus ReadInputs(in Proof root, in Kinematics witness,
                in ResponseProposal response, in CertifiedResponseRequest responseRequest,
                in PreImpactVelocity preimpact, in CertifiedPreImpactVelocityRequest preimpactRequest,
                in FloridaContactUse current, out FloridaContactKinematics input, out CertifiedResponseValues law,
                out CertifiedPreImpactVelocityValues before)
            {
                input = default; law = default; before = default;
                var r = response.Read(root, witness, responseRequest, current, out law);
                var v = preimpact.Read(root, witness, preimpactRequest, current, out before);
                if (r == CertifiedResponseStatus.Stale || v == CertifiedPreImpactVelocityStatus.Stale)
                    return CertifiedPostImpactVelocityStatus.Stale;
                if (r != CertifiedResponseStatus.Qualified || v != CertifiedPreImpactVelocityStatus.Qualified)
                    return CertifiedPostImpactVelocityStatus.Unsupported;
                var k = witness.Read(root, current, out input);
                return k == FloridaKinematicsStatus.Qualified ? CertifiedPostImpactVelocityStatus.Qualified :
                    k == FloridaKinematicsStatus.Stale ? CertifiedPostImpactVelocityStatus.Stale : CertifiedPostImpactVelocityStatus.Unsupported;
            }

            internal static CertifiedPostImpactVelocityResult Qualify(in Proof root, in Kinematics witness,
                in ResponseProposal response, in CertifiedResponseRequest responseRequest,
                in PreImpactVelocity preimpact, in CertifiedPreImpactVelocityRequest preimpactRequest,
                in FloridaContactUse current)
            {
                var status = ReadInputs(root, witness, response, responseRequest, preimpact, preimpactRequest, current,
                    out var input, out var law, out var before);
                if (status != CertifiedPostImpactVelocityStatus.Qualified)
                    return new(status, status == CertifiedPostImpactVelocityStatus.Stale
                        ? CertifiedPostImpactVelocityFailure.ChangedAuthority : CertifiedPostImpactVelocityFailure.InvalidWitness);
                var failure = CertifiedPostImpactVelocityMath.Select(input, law, before, out var selected);
                if (failure != CertifiedPostImpactVelocityFailure.None)
                    return new(CertifiedPostImpactVelocityStatus.Unresolved, failure);
                return new(CertifiedPostImpactVelocityStatus.Qualified, CertifiedPostImpactVelocityFailure.None,
                    new(root, witness, response, responseRequest, preimpact, preimpactRequest, selected));
            }
        }
    }
}
