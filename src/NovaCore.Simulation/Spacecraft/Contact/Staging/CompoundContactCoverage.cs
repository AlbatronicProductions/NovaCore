using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;

namespace NovaCore.Simulation.Spacecraft.Contact.Staging;

/// <summary>Owned scratch for one admitted dynamic compound/static convex pair on the world's owner thread.</summary>
internal sealed class CompoundContactCoverage
{
    private readonly BepuPhysics.Simulation simulation;
    private readonly BodyHandle body;
    private readonly StaticHandle surface;
    private readonly TypedIndex shape;
    private readonly Vector3 acceleration;
    private readonly float resolution;
    private readonly bool[] visited;
    private readonly CompoundContactSelector.Candidate[] candidates;
    private readonly CompoundContactSelector.Scratch[] scratch;
    private readonly int[] lastSelected = new int[4];
    private readonly int[] lastNative = new int[4];
    private float dt;
    private int count, selectedCount;
    private bool parentSeen;
    internal bool Failed { get; private set; }

    internal CompoundContactCoverage(BepuPhysics.Simulation simulation, BodyHandle body, StaticHandle surface,
        TypedIndex shape, Vector3 acceleration, float resolution)
    {
        this.simulation = simulation; this.body = body; this.surface = surface; this.shape = shape;
        this.acceleration = acceleration; this.resolution = resolution;
        var childCount = simulation.Shapes.GetShape<Compound>(shape.Index).Children.Length;
        visited = new bool[childCount];
        candidates = new CompoundContactSelector.Candidate[checked(childCount * 4)];
        scratch = new CompoundContactSelector.Scratch[candidates.Length];
    }
    internal void Begin(float duration)
    { dt = duration; count = 0; selectedCount = 0; parentSeen = false; Failed = false; Array.Clear(visited); }

    private bool PairMatches(int worker, CollidablePair pair) => worker == 0 &&
        pair.A.Mobility == CollidableMobility.Dynamic && pair.A.BodyHandle == body &&
        pair.B.Mobility == CollidableMobility.Static && pair.B.StaticHandle == surface;

    internal bool Child(int worker, CollidablePair pair, int childA, int childB, ref ConvexContactManifold manifold)
    {
        if (Failed || !PairMatches(worker, pair) || parentSeen || (uint)childA >= visited.Length ||
            childB != 0 || visited[childA] || manifold.Count is < 0 or > 4 || count + manifold.Count > candidates.Length)
        { Failed = true; return false; }
        visited[childA] = true;
        var state = simulation.Bodies[body];
        ref var child = ref simulation.Shapes.GetShape<Compound>(shape.Index).Children[childA];
        Compound.GetRotatedChildPose(child.LocalPosition, child.LocalOrientation, state.Pose.Orientation, out var rotated);
        for (var i = 0; i < manifold.Count; i++)
        {
            var contact = manifold[i]; var rawFeature = contact.FeatureId;
            contact.Offset += rotated.Position;
            contact.FeatureId ^= (childA << 8) ^ (childB << 16);
            // Native mixing is not a general collision-free ID encoding. Reject ambiguous ownership.
            for (var j = 0; j < count; j++)
                if (candidates[j].Contact.FeatureId == contact.FeatureId) { Failed = true; return false; }
            candidates[count++] = new(contact, childA, childB, rawFeature);
        }
        return true;
    }

    internal bool Parent<T>(int worker, CollidablePair pair, ref T manifold) where T : unmanaged, IContactManifold<T>
    {
        if (Failed || !PairMatches(worker, pair) || parentSeen) { Failed = true; return false; }
        parentSeen = true;
        // BEPU's single-populated-child convex fallback retains its existing behavior.
        if (manifold.Convex || manifold.Count == 0) return true;
        if (manifold.Count > 4) { Failed = true; return false; }
        Span<int> native = stackalloc int[manifold.Count];
        Span<int> selected = stackalloc int[4];
        for (var i = 0; i < native.Length; i++)
        {
            native[i] = -1;
            for (var j = 0; j < count; j++)
                if (candidates[j].Contact.FeatureId == manifold[i].FeatureId)
                {
                    // Contact offsets are transported with the same BEPU child-pose operation.
                    var c = candidates[j].Contact; var p = manifold[i];
                    if (c.Offset != p.Offset || c.Normal != p.Normal || c.Depth != p.Depth) { Failed = true; return false; }
                    native[i] = j; break;
                }
            if (native[i] < 0) { Failed = true; return false; }
        }
        var state = simulation.Bodies[body];
        PoseIntegration.RotateInverseInertia(state.LocalInertia.InverseInertiaTensor, state.Pose.Orientation, out var inertia);
        var motion = new CompoundContactSelector.Motion(state.Velocity.Linear, state.Velocity.Angular,
            acceleration, state.LocalInertia.InverseMass, inertia);
        if (!CompoundContactSelector.Select(candidates.AsSpan(0, count), native, motion, default,
            simulation.Statics[surface].Pose.Position - state.Pose.Position, dt, resolution, scratch, selected, out selectedCount))
        { Failed = true; return false; }

        Span<int> slots = stackalloc int[4]; slots.Fill(-1);
        // Keep retained native features in their native slots. New rows fill holes by stable feature tuple.
        for (var i = 0; i < native.Length; i++)
            for (var j = 0; j < selectedCount; j++) if (native[i] == selected[j]) slots[i] = native[i];
        for (var i = 0; i < native.Length; i++)
        {
            if (slots[i] >= 0) continue;
            var best = -1;
            for (var j = 0; j < selectedCount; j++)
            {
                var present = false;
                for (var k = 0; k < native.Length; k++) if (slots[k] == selected[j]) present = true;
                if (!present && (best < 0 || CompoundContactSelector.Before(candidates[selected[j]], candidates[best]))) best = selected[j];
            }
            if (best < 0) { Failed = true; return false; }
            slots[i] = best;
        }
        for (var i = 0; i < native.Length; i++) manifold[i] = candidates[slots[i]].Contact;
        selected[..selectedCount].CopyTo(lastSelected);
        native.CopyTo(lastNative);
        return true;
    }

    // Copied value witness, after stepping only; no live solver capability escapes.
    internal int CopySelection(Span<CompoundContactSelector.Candidate> output, Span<double> prediction)
    {
        if (output.Length < selectedCount || prediction.Length < selectedCount) throw new ArgumentException("Witness capacity");
        for (var i = 0; i < selectedCount; i++)
        { output[i] = candidates[lastSelected[i]]; prediction[i] = scratch[lastSelected[i]].PredictedDepth; }
        return selectedCount;
    }
    internal int CopyRaw(Span<CompoundContactSelector.Candidate> output, Span<double> prediction, Span<int> native)
    {
        if (output.Length < count || prediction.Length < count || native.Length < selectedCount) throw new ArgumentException("Witness capacity");
        candidates.AsSpan(0,count).CopyTo(output);
        for(var i=0;i<count;i++) prediction[i]=scratch[i].PredictedDepth;
        for(var i=0;i<selectedCount;i++) native[i]=candidates[lastNative[i]].Contact.FeatureId;
        return count;
    }
}

internal sealed partial class LocalContactWorld
{
    internal int CopyCompoundSelectionForTest(Span<CompoundContactSelector.Candidate> output, Span<double> prediction) =>
        metrics.Coverage?.CopySelection(output, prediction) ?? 0;
    internal int CopyCompoundRawForTest(Span<CompoundContactSelector.Candidate> output, Span<double> prediction, Span<int> native) =>
        metrics.Coverage?.CopyRaw(output, prediction, native) ?? 0;
}
