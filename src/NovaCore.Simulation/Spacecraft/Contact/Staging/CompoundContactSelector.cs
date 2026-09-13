using System.Numerics;
using BepuUtilities;

namespace NovaCore.Simulation.Spacecraft.Contact.Staging;

/// <summary>Value-only four-row selection. Prediction ranks real contacts; it never changes their geometry.</summary>
internal static class CompoundContactSelector
{
    internal readonly record struct Candidate(BepuPhysics.CollisionDetection.Contact Contact, int ChildA, int ChildB, int RawFeature);
    internal readonly record struct Motion(Vector3 Linear, Vector3 Angular, Vector3 Acceleration,
        float InverseMass, Symmetric3x3 InverseInertia);
    internal struct Scratch
    {
        internal Vector3 MomentA, MomentB, ResponseA, ResponseB;
        internal double Diagonal, PredictedDepth, ClosingDisplacement;
    }

    // Native indices supply only padding when fewer than four resolved relevant rows exist.
    // Caller owns immutable bounded storage. No heap, callbacks, solver mutation or timestep selection.
    internal static bool Select(ReadOnlySpan<Candidate> candidates, ReadOnlySpan<int> native,
        in Motion a, in Motion b, Vector3 offsetB, float dt, float resolution,
        Span<Scratch> scratch, Span<int> selected, out int count)
    {
        count = 0;
        if (candidates.IsEmpty || native.IsEmpty || native.Length > 4 || selected.Length < native.Length ||
            scratch.Length < candidates.Length || !float.IsFinite(dt) || dt <= 0 ||
            !float.IsFinite(resolution) || resolution <= 0) return false;
        for (var i = 0; i < native.Length; i++) if ((uint)native[i] >= candidates.Length) return false;
        var maximumDepth = float.NegativeInfinity;
        var imminent = -1;
        for (var i = 0; i < candidates.Length; i++)
        {
            ref readonly var c = ref candidates[i];
            var n = c.Contact.Normal;
            var rA = c.Contact.Offset;
            var rB = rA - offsetB;
            if (!Finite(n) || !Finite(rA) || !float.IsFinite(c.Contact.Depth) || n.LengthSquared() <= 0) return false;
            ref var s = ref scratch[i];
            s.MomentA = Vector3.Cross(rA, n); s.MomentB = Vector3.Cross(rB, n);
            Symmetric3x3.TransformWithoutOverlap(s.MomentA, a.InverseInertia, out s.ResponseA);
            Symmetric3x3.TransformWithoutOverlap(s.MomentB, b.InverseInertia, out s.ResponseB);
            s.Diagonal = (a.InverseMass + b.InverseMass) * Dot(n, n) +
                Dot(s.MomentA, s.ResponseA) + Dot(s.MomentB, s.ResponseB);
            // Narrow phase precedes force integration. Use the known semi-implicit linear velocity
            // of this actual step; current angular velocity gives a first-order approach estimate.
            var velocity = a.Linear + a.Acceleration * dt + Vector3.Cross(a.Angular, rA) -
                b.Linear - b.Acceleration * dt - Vector3.Cross(b.Angular, rB);
            s.ClosingDisplacement = -Dot(velocity, n) * dt;
            s.PredictedDepth = c.Contact.Depth + s.ClosingDisplacement;
            if (!double.IsFinite(s.Diagonal) || s.Diagonal <= 0 || !double.IsFinite(s.PredictedDepth)) return false;
            maximumDepth = Math.Max(maximumDepth, c.Contact.Depth);
            // Resolution is the admitted world's existing contact precision, not a tuned speed epsilon.
            if (s.ClosingDisplacement > resolution && s.PredictedDepth > resolution &&
                (imminent < 0 || s.PredictedDepth > scratch[imminent].PredictedDepth ||
                (s.PredictedDepth == scratch[imminent].PredictedDepth && Before(c, candidates[imminent])))) imminent = i;
        }
        // One contextual top-depth class, measured against the same maximum for every candidate.
        // Unlike pairwise epsilon ordering, membership cannot chain A~B~C into a wider class.
        // A computed binary64 gap equal to resolution is distinguishable and outside the class.
        // Original FP32 contact depths are neither rounded nor changed for the solver.
        var deepest = -1;
        for (var i = 0; i < candidates.Length; i++)
            if ((double)maximumDepth - candidates[i].Contact.Depth < resolution &&
                (deepest < 0 || Before(candidates[i], candidates[deepest]))) deepest = i;
        selected[count++] = deepest;
        // Common gravity at rest is not evidence for a second distinct anchor. Require a resolved
        // predicted-severity advantage over support already protected, so sub-resolution angular
        // noise cannot consume a coverage slot merely by breaking a gravity-induced tie.
        if (imminent >= 0 && imminent != deepest && count < native.Length &&
            scratch[imminent].PredictedDepth - scratch[deepest].PredictedDepth > resolution) selected[count++] = imminent;
        while (count < native.Length)
        {
            var best = -1; var bestDistance = double.NegativeInfinity;
            for (var i = 0; i < candidates.Length; i++)
            {
                if (Contains(selected[..count], i) ||
                    (candidates[i].Contact.Depth < -resolution && scratch[i].PredictedDepth < -resolution)) continue;
                var minimum = double.PositiveInfinity;
                for (var j = 0; j < count; j++)
                    minimum = Math.Min(minimum, Distance(candidates, scratch, i, selected[j], a.InverseMass + b.InverseMass));
                if (minimum > bestDistance || (minimum == bestDistance && (best < 0 || Before(candidates[i], candidates[best]))))
                { best = i; bestDistance = minimum; }
            }
            if (best < 0)
            {
                // Keep resolved anchors. Padding cannot replace them with irrelevant speculative rows.
                for (var i = 0; i < native.Length; i++)
                    if (!Contains(selected[..count], native[i]) &&
                        (best < 0 || Before(candidates[native[i]], candidates[best]))) best = native[i];
            }
            if (best < 0) return false;
            selected[count++] = best;
        }
        return true;
    }

    private static double Distance(ReadOnlySpan<Candidate> c, ReadOnlySpan<Scratch> s, int i, int j, float inverseMass)
    {
        // Squared distance of normalized normal-Jacobian rows in kinetic response space:
        // ||M^-1/2 Ji / sqrt(Kii) - M^-1/2 Jj / sqrt(Kjj)||². No dimensional weights.
        var kij = inverseMass * Dot(c[i].Contact.Normal, c[j].Contact.Normal) +
            Dot(s[i].MomentA, s[j].ResponseA) + Dot(s[i].MomentB, s[j].ResponseB);
        return Math.Clamp(2 - 2 * kij / Math.Sqrt(s[i].Diagonal * s[j].Diagonal), 0, 4);
    }
    internal static bool Before(in Candidate a, in Candidate b) => a.ChildA != b.ChildA ? a.ChildA < b.ChildA :
        a.ChildB != b.ChildB ? a.ChildB < b.ChildB : a.RawFeature < b.RawFeature;
    private static bool Contains(ReadOnlySpan<int> values, int value)
    { foreach (var v in values) if (v == value) return true; return false; }
    private static double Dot(Vector3 a, Vector3 b) => (double)a.X * b.X + (double)a.Y * b.Y + (double)a.Z * b.Z;
    private static bool Finite(Vector3 v) => float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);
}
