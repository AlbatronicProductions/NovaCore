using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Interop;

namespace NovaCore.Graphics;

public enum FloridaVerticalSliceVisibleOwner : byte
{
    ProductionFallback,
    AnchoredT0,
    AnchoredT1,
    AnchoredT2
}

public readonly record struct FloridaVerticalSliceTelemetry(
    FloridaVerticalSliceVisibleOwner VisibleOwner,
    bool FallbackActive,
    int AnchoredTier,
    int DemandedTier,
    int ReadyTier,
    PlanetaryAnchoredBillboardTierState PromotionState,
    bool CoverageComplete,
    int ActivePatchCount,
    int ActiveTriangleCount,
    int ParentFallbackCount,
    int PreparationFailures,
    double MaximumPositionDisagreementMetres);

/// <summary>
/// Bounded 11B-7F Florida integration contract. The terrain-v5 production
/// surface remains complete beneath this replacement; an anchored tier is
/// published only as one opaque, complete transaction.
/// </summary>
public sealed class FloridaAnchoredVerticalSlice
{
    public const double PreparationAltitudeMetres = 900_000d;
    public const double VisibilityAltitudeMetres = 700_000d;
    public const double VisibilityReleaseAltitudeMetres = 800_000d;
    public const double Tier1AltitudeMetres = 100_000d;
    public const double Tier1ReleaseAltitudeMetres = 150_000d;
    public const double Tier2AltitudeMetres = 10_000d;
    public const double Tier2ReleaseAltitudeMetres = 15_000d;
    private const double AltitudeBoundaryToleranceMetres = 0.001d;
    private readonly PlanetaryAnchoredBillboardHierarchy _hierarchy;
    private readonly NativeAnchoredTerrainVertex[][] _tiers;
    private readonly NativeAnchoredTerrainVertex[] _submission;
    private readonly double _maximumPositionDisagreement;
    private int _nextPreparationPatch;
    private int _preparationFailures;
    private int _activeVertexCount;
    private bool _visible;

    private FloridaAnchoredVerticalSlice(PlanetaryAnchoredBillboardHierarchy hierarchy,
        NativeAnchoredTerrainVertex[][] tiers, double maximumPositionDisagreement)
    {
        _hierarchy = hierarchy; _tiers = tiers;
        _submission = new NativeAnchoredTerrainVertex[tiers.Max(value => value.Length)];
        _maximumPositionDisagreement = maximumPositionDisagreement;
    }

    public PlanetaryAnchoredBillboardHierarchy Hierarchy => _hierarchy;
    public SurfaceAnchor Anchor => _hierarchy.Footprint.Anchor.SurfaceAnchor;
    public NativeAnchoredTerrainVertex[] NativeVertices => _submission;
    public int ActiveVertexCount => _activeVertexCount;
    public ReadOnlySpan<NativeAnchoredTerrainVertex> ActiveVertices => _submission.AsSpan(0, _activeVertexCount);
    public bool Visible => _visible;

    public void ApplyOwnership(ref NativePlanetaryEyeball input)
    {
        if (!_visible || _activeVertexCount == 0) return;
        var root = _hierarchy.Footprint.Roots[0];
        input.AnchoredFace = (uint)root.Face;
        input.AnchoredLevel = (uint)root.Level;
        input.AnchoredX = (uint)root.X;
        input.AnchoredYEnabled = (uint)root.Y | 0x80000000u;
    }

    public FloridaVerticalSliceTelemetry Telemetry
    {
        get
        {
            var hierarchy = _hierarchy.Telemetry;
            var tier = _hierarchy.ActiveOwnerTier;
            var stateTier = Math.Min(_hierarchy.TierCount - 1, Math.Max(tier, hierarchy.DemandedTier));
            var owner = !_visible ? FloridaVerticalSliceVisibleOwner.ProductionFallback :
                (FloridaVerticalSliceVisibleOwner)(tier + 1);
            return new(owner, true, _visible ? tier : -1, hierarchy.DemandedTier,
                hierarchy.ReadyTier, _hierarchy.State(stateTier),
                !_visible || _hierarchy.HasCompleteCoverage, _visible ? _hierarchy.ActivePatches.Length : 0,
                _visible ? _tiers[tier].Length / 3 : 0, hierarchy.ParentFallbackCount,
                _preparationFailures, _maximumPositionDisagreement);
        }
    }

    public static bool TryCreate(ulong earthBodyId, double earthRadiusMetres,
        in PlanetaryTerrainDefinition terrain, out FloridaAnchoredVerticalSlice? slice,
        out string error)
    {
        slice = null;
        if (!FloridaLaunchSite.TryCreate(earthBodyId, earthRadiusMetres, terrain, out var site))
        { error = "The canonical Florida SurfaceAnchor could not be created."; return false; }
        var footprint = PlanetaryAnchoredBillboardFootprint.AroundAnchor(site.Object.Anchor, 8);
        var hierarchy = new PlanetaryAnchoredBillboardHierarchy(footprint);
        var tiers = new NativeAnchoredTerrainVertex[hierarchy.TierCount][];
        var maximumError = 0d;
        for (var tier = 0; tier < hierarchy.TierCount; tier++)
            tiers[tier] = BuildTier(hierarchy.Topology(tier), earthRadiusMetres, terrain,
                ref maximumError);
        slice = new(hierarchy, tiers, maximumError); error = string.Empty; return true;
    }

    public void Update(double altitudeMetres)
    {
        UpdateOwnership(altitudeMetres); Publish();
    }

    private void UpdateOwnership(double altitudeMetres)
    {
        if (!double.IsFinite(altitudeMetres)) throw new ArgumentOutOfRangeException(nameof(altitudeMetres));
        if (!AtOrBelow(altitudeMetres, VisibilityReleaseAltitudeMetres))
        {
            _visible = false; DemandAndRetire(0); return;
        }
        if (AtOrBelow(altitudeMetres, PreparationAltitudeMetres) && !_visible &&
            AtOrBelow(altitudeMetres, VisibilityAltitudeMetres)) _visible = true;

        var demand = AtOrBelow(altitudeMetres, Tier2AltitudeMetres) ? 2 :
            AtOrBelow(altitudeMetres, Tier1AltitudeMetres) ? 1 : 0;
        if (_hierarchy.ActiveOwnerTier == 2 && !AtOrBelow(altitudeMetres, Tier2ReleaseAltitudeMetres)) demand = 1;
        if (_hierarchy.ActiveOwnerTier >= 1 && !AtOrBelow(altitudeMetres, Tier1ReleaseAltitudeMetres)) demand = 0;
        if (demand < _hierarchy.ActiveOwnerTier) { DemandAndRetire(demand); return; }
        if (demand <= _hierarchy.ActiveOwnerTier) return;
        var next = _hierarchy.ActiveOwnerTier + 1;
        _hierarchy.RequestTier(demand);
        if (_hierarchy.State(next) is PlanetaryAnchoredBillboardTierState.Demanded or
            PlanetaryAnchoredBillboardTierState.Failed)
        { _hierarchy.BeginPreparation(next); _nextPreparationPatch = 0; }
        if (_hierarchy.State(next) != PlanetaryAnchoredBillboardTierState.Preparing) return;
        if (_nextPreparationPatch < _hierarchy.Patches(next).Length)
            _hierarchy.MarkPatchResourcesReady(next, _nextPreparationPatch++,
                PlanetaryAnchoredBillboardResource.Complete);
        if (_hierarchy.State(next) == PlanetaryAnchoredBillboardTierState.Ready)
            _hierarchy.TryPromote();
    }

    public bool InjectPreparationFailure(PlanetaryAnchoredBillboardFailure failure)
    {
        if (failure == PlanetaryAnchoredBillboardFailure.None ||
            _hierarchy.ActiveOwnerTier + 1 >= _hierarchy.TierCount) return false;
        var next = _hierarchy.ActiveOwnerTier + 1;
        _hierarchy.RequestTier(next);
        if (_hierarchy.State(next) is PlanetaryAnchoredBillboardTierState.Demanded or
            PlanetaryAnchoredBillboardTierState.Failed) _hierarchy.BeginPreparation(next);
        _hierarchy.FailPatch(next, 0, failure); _preparationFailures++;
        return !_hierarchy.TryPromote() && _hierarchy.HasCompleteCoverage;
    }

    private void DemandAndRetire(int demand)
    {
        _hierarchy.RequestTier(demand);
        while (_hierarchy.ActiveOwnerTier > demand && _hierarchy.TryRetire()) { }
    }

    private void Publish()
    {
        if (!_visible) { _activeVertexCount = 0; return; }
        var source = _tiers[_hierarchy.ActiveOwnerTier];
        source.AsSpan().CopyTo(_submission); _activeVertexCount = source.Length;
    }

    private static bool AtOrBelow(double value, double boundary) =>
        value <= boundary + AltitudeBoundaryToleranceMetres;

    private static NativeAnchoredTerrainVertex[] BuildTier(
        PlanetaryAnchoredBillboardTopology topology, double radius,
        in PlanetaryTerrainDefinition terrain, ref double maximumError)
    {
        var positions = new Double3[topology.Vertices.Length];
        var normals = new Double3[topology.Vertices.Length];
        var colors = new Double3[topology.Vertices.Length];
        for (var index = 0; index < topology.Vertices.Length; index++)
        {
            var direction = topology.Vertices[index].BodyFixedDirection;
            var height = terrain.SampleHeight(direction, 24);
            positions[index] = direction * (radius + height);
            var rawElevation = EarthElevationDataset.SampleElevation(direction);
            colors[index] = rawElevation <= 0d ? new(.025d, .12d, .28d) :
                height > 1_800d ? new(.42d, .39d, .32d) : new(.16d, .34d, .12d);
        }
        for (var index = 0; index < topology.Indices.Length; index += 3)
        {
            var a = topology.Indices[index]; var b = topology.Indices[index + 1];
            var c = topology.Indices[index + 2];
            var normal = Double3.Cross(positions[(int)b] - positions[(int)a],
                positions[(int)c] - positions[(int)a]);
            normals[(int)a] += normal; normals[(int)b] += normal; normals[(int)c] += normal;
        }
        var expanded = new NativeAnchoredTerrainVertex[topology.Indices.Length];
        for (var index = 0; index < expanded.Length; index++)
        {
            var source = (int)topology.Indices[index]; var encoded = EncodedPosition.Encode(positions[source]);
            maximumError = Math.Max(maximumError,
                Math.Sqrt((encoded.Reconstruct() - positions[source]).LengthSquared));
            var normal = normals[source].Normalized(); var color = colors[source];
            expanded[index] = new()
            {
                BodyPosition = new() { HighX = encoded.HighX, HighY = encoded.HighY,
                    HighZ = encoded.HighZ, LowX = encoded.LowX, LowY = encoded.LowY,
                    LowZ = encoded.LowZ },
                NormalX = (float)normal.X, NormalY = (float)normal.Y, NormalZ = (float)normal.Z,
                ColorR = (float)color.X, ColorG = (float)color.Y, ColorB = (float)color.Z,
                ColorA = 1f
            };
        }
        return expanded;
    }
}
