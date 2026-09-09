using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Simulation.Celestial;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Spacecraft.Contact;

/// <summary>Derived, immutable body kinematics. Only fresh celestial evaluation can construct this sample.</summary>
internal readonly struct ContactBodyMotion
{
    private ContactBodyMotion(CelestialSystemDefinition system, CelestialBodyId body, ReferenceFrameId root,
        SimulationInstant time, FrameTransform pose, Double3 velocity, Double3 omega)
    { System = system; Body = body; Root = root; Time = time; BodyFixedToRoot = pose; VelocityRoot = velocity; AngularVelocityRoot = omega; }
    internal CelestialSystemDefinition? System { get; }
    internal CelestialBodyId Body { get; }
    internal ReferenceFrameId Root { get; }
    internal SimulationInstant Time { get; }
    internal FrameTransform BodyFixedToRoot { get; }
    internal Double3 VelocityRoot { get; }
    internal Double3 AngularVelocityRoot { get; }
    internal bool IsReady => System is not null;

    /// <summary>Evaluate once per body/instant; caller owns nonoverlapping work buffers. Never relabel cached transforms with a new time.</summary>
    internal static ContactGenerationStatus TryEvaluate(CelestialSystemDefinition system, ReferenceFrameGraph graph,
        CelestialBodyId body, SimulationInstant time, Span<ReferenceFrameEvaluation> evaluations,
        Span<FrameTransform> roots, Span<ReferenceFrameEvaluation> staging, Span<FrameTransform> stagingRoots,
        out ContactBodyMotion result)
    {
        result = default;
        if (graph.RootCount != 1 || graph.GetNodeAt(0).Kind != ReferenceFrameKind.Ecl || graph.GetNodeAt(0).ParentId is not null)
            return ContactGenerationStatus.RootMismatch;
        if (!system.TryGetBody(body, out _) || !CelestialBodyOrientationEvaluator.TryEvaluate(body, time, out var orientation))
            return ContactGenerationStatus.UnsupportedBody;
        if (evaluations.Overlaps(staging) || roots.Overlaps(stagingRoots)) return ContactGenerationStatus.InvalidFrame;
        var evaluation = CelestialSystemEvaluator.TryEvaluateSystem(system, time, evaluations, roots, staging, stagingRoots);
        if (!evaluation.Succeeded) return ContactGenerationStatus.BodyEvaluationFailed;
        var target = -1;
        for (var i = 0; i < system.Count; i++) if (system.GetNodeInTraversalOrder(i).Id == body) { target = i; break; }
        if (target < 0) return ContactGenerationStatus.UnsupportedBody;

        // Transport the target origin's velocity through every ancestor, using the existing rigid-frame equations.
        var local = evaluations[target].Value;
        var position = local.LocalToParent.Translation;
        var velocity = local.OriginVelocityInParent;
        var current = target;
        while (true)
        {
            system.TryGetBody(system.GetNodeInTraversalOrder(current).Id, out var definition);
            if (definition.Identity.ParentBody is not { } parent) break;
            var parentIndex = -1;
            for (var i = 0; i < current; i++) if (system.GetNodeInTraversalOrder(i).Id == parent) { parentIndex = i; break; }
            if (parentIndex < 0) return ContactGenerationStatus.InvalidFrame;
            var frame = evaluations[parentIndex].Value;
            velocity = ReferenceFrameMath.TransformVelocity(frame.LocalToParent, frame.OriginVelocityInParent,
                frame.AngularVelocityInParent, position, velocity);
            position = frame.LocalToParent.LocalToParent(position);
            current = parentIndex;
        }
        if (!position.IsFinite || !velocity.IsFinite) return ContactGenerationStatus.NonFiniteResult;
        result = new(system, body, graph.GetNodeAt(0).Id, time,
            new(roots[target].Translation, orientation.BodyFixedToInertial), velocity, orientation.AngularVelocityInInertial);
        return ContactGenerationStatus.Ready;
    }
}
