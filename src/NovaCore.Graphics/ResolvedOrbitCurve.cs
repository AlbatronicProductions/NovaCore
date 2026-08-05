using NovaCore.Core;

namespace NovaCore.Graphics;

/// <summary>Immutable derived root-space orbit geometry. It is presentation data, never simulation state or history.</summary>
public sealed class ResolvedOrbitCurve
{
    private readonly UniversePosition[] _positions;
    private ResolvedOrbitCurve(ReferenceFrameId rootFrame, UniversePosition[] positions) { RootFrame = rootFrame; _positions = positions; }
    public ReferenceFrameId RootFrame { get; }
    public int Count => _positions.Length;
    public ReadOnlySpan<UniversePosition> Positions => _positions;
    public static bool TryCreate(ReadOnlySpan<UniversePosition> positions, out ResolvedOrbitCurve? curve)
    {
        curve = null;
        if (positions.Length < 2) return false;
        var root = positions[0].Frame;
        for (var index = 0; index < positions.Length; index++) if (positions[index].Frame != root || !positions[index].Value.IsFinite) return false;
        curve = new ResolvedOrbitCurve(root, positions.ToArray());
        return true;
    }
}
