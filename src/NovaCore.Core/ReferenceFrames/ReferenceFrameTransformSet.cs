namespace NovaCore.Core.ReferenceFrames;

/// <summary>One immutable evaluated local-to-parent transform associated with a structural frame ID.</summary>
internal readonly record struct ReferenceFrameEvaluation(ReferenceFrameId Frame, EvaluatedReferenceFrame Value);

/// <summary>
/// Immutable evaluated transform state aligned with one immutable graph. The graph remains topology-only;
/// this set carries no simulation-time, propagation, or mutation ownership.
/// </summary>
internal sealed class ReferenceFrameTransformSet
{
    private readonly EvaluatedReferenceFrame[] _evaluations;

    public ReferenceFrameTransformSet(ReferenceFrameGraph graph, ReadOnlySpan<ReferenceFrameEvaluation> evaluations)
    {
        Graph = graph ?? throw new ArgumentNullException(nameof(graph));
        if (evaluations.Length != graph.Count) throw new ArgumentException("Every graph frame requires exactly one evaluated transform.", nameof(evaluations));
        _evaluations = new EvaluatedReferenceFrame[graph.Count];
        var assigned = new bool[graph.Count];
        foreach (ref readonly var evaluation in evaluations)
        {
            if (!graph.TryGetIndex(evaluation.Frame, out var index)) throw new ArgumentException("Evaluated transform references an unknown frame.", nameof(evaluations));
            if (assigned[index]) throw new ArgumentException("Duplicate evaluated frame transform.", nameof(evaluations));
            Validate(evaluation.Value);
            _evaluations[index] = evaluation.Value;
            assigned[index] = true;
        }
        for (var index = 0; index < assigned.Length; index++)
        {
            if (!assigned[index]) throw new ArgumentException("Evaluated transform is missing.", nameof(evaluations));
            if (graph.GetParentIndexAt(index) == -1 && (_evaluations[index].LocalToParent != FrameTransform.Identity || _evaluations[index].OriginVelocityInParent != Double3.Zero || _evaluations[index].AngularVelocityInParent != Double3.Zero))
                throw new ArgumentException("A graph root requires an identity zero-velocity evaluated transform.", nameof(evaluations));
        }
    }

    public ReferenceFrameGraph Graph { get; }
    internal EvaluatedReferenceFrame GetAt(int index) => _evaluations[index];

    private static void Validate(in EvaluatedReferenceFrame value)
    {
        if (!value.LocalToParent.IsFinite || !value.OriginVelocityInParent.IsFinite || !value.AngularVelocityInParent.IsFinite || Math.Abs(value.LocalToParent.Rotation.LengthSquared - 1d) > 1e-10d)
            throw new ArgumentException("Evaluated transform contains non-finite or non-normalized data.");
    }
}
