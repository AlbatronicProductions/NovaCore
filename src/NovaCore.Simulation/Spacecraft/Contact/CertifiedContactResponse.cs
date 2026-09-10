using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Rotation;

namespace NovaCore.Simulation.Spacecraft.Contact;

internal enum CertifiedResponseStatus : byte { Unresolved, Qualified, Unsupported, Stale }
internal enum CertifiedResponseFailure : byte
{
    None, InvalidWitness, InvalidRequest, ChangedAuthority, InvalidPhysicalState,
    ApproachUncertain, NumericalResolution, RequestedWidth,
}

/// <summary>Maximum full enclosure widths, never contact tolerances or permitted physical error.</summary>
internal readonly record struct CertifiedResponseRequest(double InverseMassPerKilogram,
    double ScalarImpulse, double LinearImpulseComponent, double AngularImpulseComponent)
{
    internal bool IsValid => Positive(InverseMassPerKilogram) && Positive(ScalarImpulse) &&
        Positive(LinearImpulseComponent) && Positive(AngularImpulseComponent);
    private static bool Positive(double x) => double.IsFinite(x) && x > 0;
}

/// <summary>
/// Enclosures of one exact response law at one alpha. Endpoints/components are not selectable commands.
/// This data value carries no proof or mutation authority; consume the privately issued proposal instead.
/// Angular impulse is about the spacecraft COM, expressed in its principal-inertia body frame.
/// </summary>
internal readonly record struct CertifiedResponseValues(FloridaBound EffectiveInverseMass,
    FloridaBound ScalarImpulse, FloridaVector LinearImpulseRoot, FloridaVector AngularImpulseBody);

internal readonly record struct CertifiedResponseResult(CertifiedResponseStatus Status,
    CertifiedResponseFailure Failure, FloridaContactProvider.Proof.ResponseProposal Proposal = default);

/// <summary>Pure enclosure arithmetic, not an admission/receipt factory. All inputs describe the SAME alpha.</summary>
internal static class CertifiedContactResponseMath
{
    internal static CertifiedResponseFailure Enclose(FloridaVector normal, FloridaBound speed, Double3 leverBody,
        DoubleQuaternion attitude, double mass, PrincipalMomentsOfInertia inertia,
        in CertifiedResponseRequest request, out CertifiedResponseValues result)
    {
        result = default;
        if (!request.IsValid) return CertifiedResponseFailure.InvalidRequest;
        if (!normal.IsFinite || !speed.IsFinite || !leverBody.IsFinite || !attitude.IsFinite ||
            !double.IsFinite(mass) || mass <= 0 || !inertia.IsFinite || !inertia.IsStrictlyPositive)
            return CertifiedResponseFailure.InvalidPhysicalState;
        if (speed.Upper >= 0) return CertifiedResponseFailure.ApproachUncertain;

        // Exact normalization of the stored Q bits. Rotation uses norm squared, without selecting a
        // rounded normalized quaternion. Conjugation gives Q^T; the authored lever stays body-local.
        var q = -FloridaVector.From(new(attitude.X, attitude.Y, attitude.Z));
        FloridaBound w = attitude.W;
        var normSquared = q.NormSquared + w.Square();
        if (!normSquared.IsFinite || normSquared.Lower <= 0) return CertifiedResponseFailure.NumericalResolution;
        var twice = FloridaVector.Cross(q, normal) * 2;
        var normalBody = normal + (twice * w + FloridaVector.Cross(q, twice)) / normSquared;
        var a = FloridaVector.Cross(FloridaVector.From(leverBody), normalBody);
        var inverseMass = (FloridaBound)1 / mass;
        var k = inverseMass + a.X.Square() / inertia.X + a.Y.Square() / inertia.Y + a.Z.Square() / inertia.Z;
        if (!a.IsFinite || !inverseMass.IsFinite || inverseMass.Lower <= 0 || !k.IsFinite || k.Lower <= 0)
            return CertifiedResponseFailure.NumericalResolution;
        var j = -speed / k;
        var linear = normal * j;
        var angular = a * j;
        if (!j.IsFinite || j.Lower <= 0 || !linear.IsFinite || !angular.IsFinite)
            return CertifiedResponseFailure.NumericalResolution;
        if (FloridaKinematicsRequest.Width(k) > request.InverseMassPerKilogram ||
            FloridaKinematicsRequest.Width(j) > request.ScalarImpulse ||
            !FloridaKinematicsRequest.Fits(linear, request.LinearImpulseComponent) ||
            !FloridaKinematicsRequest.Fits(angular, request.AngularImpulseComponent))
            return CertifiedResponseFailure.RequestedWidth;
        result = new(k, j, linear, angular);
        return CertifiedResponseFailure.None;
    }
}
