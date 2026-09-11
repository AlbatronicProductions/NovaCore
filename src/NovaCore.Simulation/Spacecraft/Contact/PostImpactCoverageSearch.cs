namespace NovaCore.Simulation.Spacecraft.Contact;

internal enum CoverageStatus : byte { Unresolved, EventFreeThroughTarget, Unsupported, Stale }
internal enum CoverageFailure : byte
{
    None, InvalidSource, InvalidRequest, ChangedAuthority, UnsupportedArithmetic, NonFinite,
    GradingDomain, DepartureUnproved, RootUnresolved, WorkLimit, InvalidProof, StrictCrossingDetected,
}

/// <summary>Plane residual and PHYSICAL-TIME derivatives, not derivatives with respect to sigma.</summary>
internal readonly record struct CoverageGap(FloridaBound Value, FloridaBound First, FloridaBound Second)
{
    internal bool IsFinite => Value.IsFinite && First.IsFinite && Second.IsFinite;
}

/// <summary>Numerical inclusion only. Implementations do not grant receipt or root authority.</summary>
internal interface IPostImpactCoverageGap
{
    CoverageFailure Enclose(FloridaBound sigma, out CoverageGap gap);
}

internal readonly record struct CoverageSearchResult(CoverageStatus Status, CoverageFailure Failure,
    int Visits, int MaximumDepth, double ClearPrefix, int Evaluations = 0);

/// <summary>
/// Bounded numerical theorem kernel. Caller must prove exact f(0)=0, f'(alpha)>=0,
/// D=T-alpha>0, and full interval inclusion of its model. Constructible results are not authority.
/// </summary>
internal static class PostImpactCoverageSearch
{
    internal const int Version = 2; // Checked singleton clearance only; no next-root authority.
    internal const int MaximumVisits = 255;
    internal const int MaximumDepth = 24;
    private readonly record struct Node(double Lower, double Upper, int Depth);
    internal const int SearchStackBytes = 25 * 24; // Node payload: two doubles, int, padding.

    internal static CoverageSearchResult Evaluate<T>(in T model) where T : struct, IPostImpactCoverageGap
    {
        Span<Node> stack = stackalloc Node[MaximumDepth + 1];
        var count = 1; stack[0] = new(0, 1, 0);
        var visits = 0; var depth = 0; var evaluations = 0; double prefix = 0;
        while (count > 0)
        {
            if (visits == MaximumVisits)
                return new(CoverageStatus.Unresolved, CoverageFailure.WorkLimit, visits, depth, prefix, evaluations);
            var node = stack[--count]; visits++; depth = Math.Max(depth, node.Depth);
            var failure = model.Enclose(new(node.Lower, node.Upper), out var whole); evaluations++;
            if (failure == CoverageFailure.None && !whole.IsFinite) failure = CoverageFailure.NonFinite;
            if (failure == CoverageFailure.None)
            {
                if (node.Lower == 0)
                {
                    // This theorem is valid ONLY at the inherited root, never at a later node.
                    if (whole.First.Lower > 0 || whole.Second.Lower > 0)
                    { prefix = node.Upper; continue; }
                }
                else
                {
                    if (whole.Value.Lower > 0) { prefix = node.Upper; continue; }
                    var lf = model.Enclose(FloridaBound.Point(node.Lower), out var left);
                    var rf = model.Enclose(FloridaBound.Point(node.Upper), out var right); evaluations += 2;
                    if (lf == CoverageFailure.None && rf == CoverageFailure.None && left.IsFinite && right.IsFinite)
                    {
                        if ((whole.First.Lower >= 0 && left.Value.Lower > 0) ||
                            (whole.First.Upper <= 0 && right.Value.Lower > 0))
                        { prefix = node.Upper; continue; }
                        if (prefix == node.Lower && left.Value.Lower > 0 && right.Value.Upper < 0 && whole.First.Upper < 0)
                            // Contact evidence denies clearance; it does not issue a root capability.
                            return new(CoverageStatus.Unresolved, CoverageFailure.StrictCrossingDetected,
                                visits, depth, prefix, evaluations);
                    }
                }
            }
            if (node.Depth == MaximumDepth)
                return new(CoverageStatus.Unresolved, failure != CoverageFailure.None ? failure :
                    node.Lower == 0 ? CoverageFailure.DepartureUnproved : CoverageFailure.RootUnresolved,
                    visits, depth, prefix, evaluations);
            // Left-first DFS: no later interval can bypass an unresolved earlier interval.
            var mid = (node.Lower + node.Upper) * .5;
            stack[count++] = new(mid, node.Upper, node.Depth + 1);
            stack[count++] = new(node.Lower, mid, node.Depth + 1);
        }
        return new(CoverageStatus.EventFreeThroughTarget, CoverageFailure.None, visits, depth, 1, evaluations);
    }
}
