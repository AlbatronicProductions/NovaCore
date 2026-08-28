using NovaCore.Core;
using NovaCore.Core.Surface;

namespace NovaCore.Graphics;

[Flags]
public enum PlanetaryAnchoredBillboardResource : uint
{
    None = 0,
    CanonicalTopology = 1 << 0,
    PhysicalHeight = 1 << 1,
    DisplacedVertices = 1 << 2,
    PhysicalNormals = 1 << 3,
    Adjacency = 1 << 4,
    Bounds = 1 << 5,
    GpuBuffers = 1 << 6,
    Synchronization = 1 << 7,
    Complete = CanonicalTopology | PhysicalHeight | DisplacedVertices | PhysicalNormals |
        Adjacency | Bounds | GpuBuffers | Synchronization
}

public enum PlanetaryAnchoredBillboardTierState : byte
{
    Dormant,
    Demanded,
    Preparing,
    Ready,
    Promoting,
    Authoritative,
    Retiring,
    Failed
}

public enum PlanetaryAnchoredBillboardFailure : byte
{
    None,
    MissingChild,
    PhysicalHeight,
    PhysicalNormals,
    Visibility,
    Synchronization,
    UnavailableLocalPayload,
    IncompleteBounds,
    Preparation
}

/// <summary>
/// Canonical localized geographic footprint. Its sorted root patch set is the
/// complete represented region; camera, frame, residency, and backend state are absent.
/// </summary>
public sealed class PlanetaryAnchoredBillboardFootprint
{
    private readonly PlanetarySurfacePatchId[] _roots;

    private PlanetaryAnchoredBillboardFootprint(in PlanetaryAnchoredMeshAnchor anchor,
        PlanetarySurfacePatchId[] roots, ulong hash, double angularRadius)
    {
        Anchor = anchor; _roots = roots; DeterministicHash = hash;
        MaximumAngularRadiusRadians = angularRadius;
    }

    public PlanetaryAnchoredMeshAnchor Anchor { get; }
    public ReadOnlySpan<PlanetarySurfacePatchId> Roots => _roots;
    public int RootLevel => _roots[0].Level;
    public ulong DeterministicHash { get; }
    public double MaximumAngularRadiusRadians { get; }
    public double ApproximateDiameterMetres(double bodyRadiusMetres) =>
        2d * bodyRadiusMetres * MaximumAngularRadiusRadians;

    public static PlanetaryAnchoredBillboardFootprint AroundAnchor(in SurfaceAnchor anchor,
        int rootLevel)
    {
        if (!PlanetaryAnchoredMeshTierId.TryCreate(anchor, 0, rootLevel, out var identity))
            throw new ArgumentOutOfRangeException(nameof(anchor));
        Span<PlanetarySurfacePatchId> root = stackalloc PlanetarySurfacePatchId[1] { identity.AnchorCell };
        return Create(identity.Anchor, root);
    }

    /// <summary>
    /// Builds a footprint from a conservative structural patch set. Cross-face
    /// proofs pass every incident patch; consequently only complete geographic
    /// contributors participate in readiness and promotion.
    /// </summary>
    public static PlanetaryAnchoredBillboardFootprint Create(
        in PlanetaryAnchoredMeshAnchor anchor, ReadOnlySpan<PlanetarySurfacePatchId> roots)
    {
        if (!anchor.IsValid || roots.IsEmpty) throw new ArgumentOutOfRangeException();
        var values = roots.ToArray(); Array.Sort(values);
        var count = 0;
        for (var index = 0; index < values.Length; index++)
        {
            var patch = values[index];
            if (!patch.IsValid || patch.BodyId != anchor.SurfaceAnchor.BodyId ||
                patch.TerrainVersion != anchor.SurfaceAnchor.TerrainAuthorityVersion.Version ||
                patch.Level != values[0].Level) throw new ArgumentOutOfRangeException(nameof(roots));
            if (index == 0 || patch != values[index - 1]) values[count++] = patch;
        }
        if (count != values.Length) Array.Resize(ref values, count);
        var hash = AnchoredMeshHash.Mix(AnchoredMeshHash.OffsetBasis, anchor.DeterministicHash);
        hash = AnchoredMeshHash.Mix(hash, (uint)values[0].Level);
        var maximumAngle = 0d;
        foreach (var patch in values)
        {
            hash = HashPatch(hash, patch);
            for (var corner = 0; corner < 4; corner++)
            {
                var direction = RelaxedCubeSphereProjection.PatchPoint(patch, corner & 1,
                    corner >> 1, 1);
                maximumAngle = Math.Max(maximumAngle, Math.Acos(Math.Clamp(
                    Double3.Dot(anchor.BodyFixedDirection, direction), -1d, 1d)));
            }
        }
        return new(anchor, values, hash, maximumAngle);
    }

    internal static ulong HashPatch(ulong hash, in PlanetarySurfacePatchId patch)
    {
        hash = AnchoredMeshHash.Mix(hash, patch.BodyId);
        hash = AnchoredMeshHash.Mix(hash, patch.TerrainVersion);
        hash = AnchoredMeshHash.Mix(hash, (uint)patch.Face);
        hash = AnchoredMeshHash.Mix(hash, (uint)patch.Level);
        hash = AnchoredMeshHash.Mix(hash, (uint)patch.X);
        return AnchoredMeshHash.Mix(hash, (uint)patch.Y);
    }
}

public readonly record struct PlanetaryAnchoredBillboardTierId(
    PlanetaryAnchoredMeshAnchor Anchor,
    int Tier,
    int RootLevel,
    ulong FootprintHash,
    uint TopologyVersion)
{
    public const uint CurrentTopologyVersion = 1;
    public bool IsValid => Anchor.IsValid && Tier is >= 0 and <= 8 && RootLevel is >= 0 and <= 24 &&
        RootLevel + Tier <= 24 && FootprintHash != 0 && TopologyVersion != 0;
    public ulong DeterministicHash
    {
        get
        {
            var hash = AnchoredMeshHash.Mix(AnchoredMeshHash.OffsetBasis, Anchor.DeterministicHash);
            hash = AnchoredMeshHash.Mix(hash, (uint)Tier);
            hash = AnchoredMeshHash.Mix(hash, (uint)RootLevel);
            hash = AnchoredMeshHash.Mix(hash, FootprintHash);
            return AnchoredMeshHash.Mix(hash, TopologyVersion);
        }
    }
}

/// <summary>
/// Canonically deduplicated bounded topology for one localized tier. Shared
/// rational cube vertices make face edges and three-face corners one identity.
/// </summary>
public sealed class PlanetaryAnchoredBillboardTopology
{
    public const int ProofQuadsPerPatchSide = 4;
    private readonly PlanetaryAnchoredMeshVertexId[] _vertices;
    private readonly uint[] _indices;

    private PlanetaryAnchoredBillboardTopology(PlanetaryAnchoredMeshVertexId[] vertices,
        uint[] indices, ulong hash)
    {
        _vertices = vertices; _indices = indices; DeterministicHash = hash;
    }

    public ReadOnlySpan<PlanetaryAnchoredMeshVertexId> Vertices => _vertices;
    public ReadOnlySpan<uint> Indices => _indices;
    public ulong DeterministicHash { get; }
    public long PersistentBytes => (long)_vertices.Length * 32L + (long)_indices.Length * sizeof(uint);

    public static PlanetaryAnchoredBillboardTopology Create(
        in PlanetaryAnchoredBillboardTierId identity, ReadOnlySpan<PlanetarySurfacePatchId> patches)
    {
        if (!identity.IsValid || patches.IsEmpty) throw new ArgumentOutOfRangeException();
        var vertices = new List<PlanetaryAnchoredMeshVertexId>();
        var lookup = new Dictionary<PlanetaryAnchoredMeshVertexId, uint>();
        var indices = new uint[checked(patches.Length * ProofQuadsPerPatchSide *
            ProofQuadsPerPatchSide * 6)];
        Span<uint> grid = stackalloc uint[(ProofQuadsPerPatchSide + 1) *
            (ProofQuadsPerPatchSide + 1)];
        var cursor = 0;
        foreach (ref readonly var patch in patches)
        {
            for (var y = 0; y <= ProofQuadsPerPatchSide; y++)
                for (var x = 0; x <= ProofQuadsPerPatchSide; x++)
                {
                    var vertex = PlanetaryAnchoredMeshVertexId.FromPatchGrid(patch, x, y,
                        ProofQuadsPerPatchSide);
                    if (!lookup.TryGetValue(vertex, out var index))
                    {
                        index = (uint)vertices.Count; vertices.Add(vertex); lookup.Add(vertex, index);
                    }
                    grid[y * (ProofQuadsPerPatchSide + 1) + x] = index;
                }
            for (var y = 0; y < ProofQuadsPerPatchSide; y++)
                for (var x = 0; x < ProofQuadsPerPatchSide; x++)
                {
                    var row = ProofQuadsPerPatchSide + 1;
                    Add(grid[y * row + x], grid[(y + 1) * row + x + 1], grid[y * row + x + 1]);
                    Add(grid[y * row + x], grid[(y + 1) * row + x], grid[(y + 1) * row + x + 1]);
                }
        }
        var hash = AnchoredMeshHash.Mix(AnchoredMeshHash.OffsetBasis, identity.DeterministicHash);
        foreach (var vertex in vertices)
        {
            hash = AnchoredMeshHash.Mix(hash, (ulong)vertex.X);
            hash = AnchoredMeshHash.Mix(hash, (ulong)vertex.Y);
            hash = AnchoredMeshHash.Mix(hash, (ulong)vertex.Z);
            hash = AnchoredMeshHash.Mix(hash, (ulong)vertex.Denominator);
        }
        foreach (var index in indices) hash = AnchoredMeshHash.Mix(hash, index);
        return new(vertices.ToArray(), indices, hash);

        void Add(uint a, uint b, uint c)
        {
            var p0 = vertices[(int)a].BodyFixedDirection;
            var p1 = vertices[(int)b].BodyFixedDirection;
            var p2 = vertices[(int)c].BodyFixedDirection;
            if (Double3.Dot(Double3.Cross(p1 - p0, p2 - p0), (p0 + p1 + p2).Normalized()) < 0d)
                (b, c) = (c, b);
            indices[cursor++] = a; indices[cursor++] = b; indices[cursor++] = c;
        }
    }
}

public readonly record struct PlanetaryAnchoredBillboardTelemetry(
    int DemandedTier,
    int PreparingTier,
    int ReadyTier,
    int ActiveOwnerTier,
    int ParentFallbackCount,
    int IncompleteChildCount,
    int PromotionCount,
    int RetirementCount,
    bool GeographicCoverageComplete);

/// <summary>
/// Dormant backend-neutral ownership proof. Exactly one tier is authoritative;
/// preparation can mutate readiness only, never geographic ownership.
/// </summary>
public sealed class PlanetaryAnchoredBillboardHierarchy
{
    private readonly Tier[] _tiers;
    private int _demandedTier;
    private int _ownerTier;
    private int _parentFallbacks;
    private int _promotions;
    private int _retirements;

    public PlanetaryAnchoredBillboardHierarchy(PlanetaryAnchoredBillboardFootprint footprint,
        int tierCount = 3)
    {
        ArgumentNullException.ThrowIfNull(footprint);
        if (tierCount is < 2 or > 5 || footprint.RootLevel + tierCount - 1 > 24)
            throw new ArgumentOutOfRangeException(nameof(tierCount));
        Footprint = footprint; _tiers = new Tier[tierCount];
        var patches = footprint.Roots.ToArray();
        for (var tier = 0; tier < tierCount; tier++)
        {
            var identity = new PlanetaryAnchoredBillboardTierId(footprint.Anchor, tier,
                footprint.RootLevel, footprint.DeterministicHash,
                PlanetaryAnchoredBillboardTierId.CurrentTopologyVersion);
            _tiers[tier] = new Tier(identity, patches,
                PlanetaryAnchoredBillboardTopology.Create(identity, patches));
            if (tier + 1 < tierCount) patches = Refine(patches);
        }
        _tiers[0].PrepareAll(); _tiers[0].State = PlanetaryAnchoredBillboardTierState.Authoritative;
    }

    public PlanetaryAnchoredBillboardFootprint Footprint { get; }
    public int TierCount => _tiers.Length;
    public int ActiveOwnerTier => _ownerTier;
    public ReadOnlySpan<PlanetarySurfacePatchId> ActivePatches => _tiers[_ownerTier].Patches;
    public PlanetaryAnchoredBillboardTierId Identity(int tier) => Get(tier).Identity;
    public PlanetaryAnchoredBillboardTopology Topology(int tier) => Get(tier).Topology;
    public PlanetaryAnchoredBillboardTierState State(int tier) => Get(tier).State;
    public ReadOnlySpan<PlanetarySurfacePatchId> Patches(int tier) => Get(tier).Patches;
    public long PersistentBytes => _tiers.Sum(value => value.Topology.PersistentBytes +
        value.Patches.Length * 64L);

    public PlanetaryAnchoredBillboardTelemetry Telemetry
    {
        get
        {
            var preparing = Array.FindIndex(_tiers, value => value.State == PlanetaryAnchoredBillboardTierState.Preparing);
            var ready = Array.FindLastIndex(_tiers, value => value.State is PlanetaryAnchoredBillboardTierState.Ready or
                PlanetaryAnchoredBillboardTierState.Authoritative);
            return new(_demandedTier, preparing, ready, _ownerTier, _parentFallbacks,
                _demandedTier > _ownerTier ? _tiers[_ownerTier + 1].IncompleteCount : 0,
                _promotions, _retirements, HasCompleteCoverage);
        }
    }

    public bool HasCompleteCoverage => _tiers[_ownerTier].State ==
        PlanetaryAnchoredBillboardTierState.Authoritative &&
        (_ownerTier == 0 || ExactReplacement(_tiers[_ownerTier - 1].Patches,
            _tiers[_ownerTier].Patches));

    public int Request(in PlanetaryAnchoredMeshSubdivisionDemand demand)
    {
        if (!demand.IsValid) throw new ArgumentOutOfRangeException(nameof(demand));
        var factor = demand.BoundedFactor; var tier = 0;
        while (factor > 1 && tier + 1 < _tiers.Length) { factor = (factor + 1) >> 1; tier++; }
        RequestTier(tier); return tier;
    }

    public void RequestTier(int tier)
    {
        if ((uint)tier >= (uint)_tiers.Length) throw new ArgumentOutOfRangeException(nameof(tier));
        _demandedTier = tier;
        if (tier > _ownerTier)
        {
            var child = _tiers[_ownerTier + 1];
            if (child.State is PlanetaryAnchoredBillboardTierState.Dormant)
                child.State = PlanetaryAnchoredBillboardTierState.Demanded;
        }
        else if (tier < _ownerTier)
            _tiers[_ownerTier].State = PlanetaryAnchoredBillboardTierState.Retiring;
    }

    public void BeginPreparation(int tier)
    {
        var value = Get(tier);
        if (tier != _ownerTier + 1 || value.State is not (PlanetaryAnchoredBillboardTierState.Demanded or
            PlanetaryAnchoredBillboardTierState.Failed)) throw new InvalidOperationException();
        value.Reset(); value.State = PlanetaryAnchoredBillboardTierState.Preparing;
    }

    public void MarkPatchResourcesReady(int tier, int patchIndex,
        PlanetaryAnchoredBillboardResource resources)
    {
        var value = Get(tier);
        if (value.State != PlanetaryAnchoredBillboardTierState.Preparing ||
            (resources & ~PlanetaryAnchoredBillboardResource.Complete) != 0)
            throw new InvalidOperationException();
        value.MarkReady(patchIndex, resources);
        if (value.IncompleteCount == 0) value.State = PlanetaryAnchoredBillboardTierState.Ready;
    }

    public void PrepareAll(int tier)
    {
        var value = Get(tier);
        if (value.State == PlanetaryAnchoredBillboardTierState.Demanded) BeginPreparation(tier);
        if (value.State != PlanetaryAnchoredBillboardTierState.Preparing) throw new InvalidOperationException();
        value.PrepareAll(); value.State = PlanetaryAnchoredBillboardTierState.Ready;
    }

    public void FailPatch(int tier, int patchIndex, PlanetaryAnchoredBillboardFailure failure)
    {
        if (failure == PlanetaryAnchoredBillboardFailure.None) throw new ArgumentOutOfRangeException(nameof(failure));
        var value = Get(tier);
        if (value.State != PlanetaryAnchoredBillboardTierState.Preparing) throw new InvalidOperationException();
        value.Fail(patchIndex, failure); value.State = PlanetaryAnchoredBillboardTierState.Failed;
        _parentFallbacks++;
    }

    public bool TryPromote()
    {
        if (_demandedTier <= _ownerTier || _ownerTier + 1 >= _tiers.Length) return false;
        var parent = _tiers[_ownerTier]; var child = _tiers[_ownerTier + 1];
        if (child.State != PlanetaryAnchoredBillboardTierState.Ready ||
            !ExactReplacement(parent.Patches, child.Patches)) { _parentFallbacks++; return false; }
        child.State = PlanetaryAnchoredBillboardTierState.Promoting;
        parent.State = PlanetaryAnchoredBillboardTierState.Ready;
        child.State = PlanetaryAnchoredBillboardTierState.Authoritative;
        _ownerTier++; _promotions++; return true;
    }

    public bool TryRetire()
    {
        if (_demandedTier >= _ownerTier || _ownerTier == 0) return false;
        var child = _tiers[_ownerTier]; var parent = _tiers[_ownerTier - 1];
        if (!parent.IsComplete || !ExactReplacement(parent.Patches, child.Patches)) return false;
        child.State = PlanetaryAnchoredBillboardTierState.Retiring;
        parent.State = PlanetaryAnchoredBillboardTierState.Authoritative;
        child.State = PlanetaryAnchoredBillboardTierState.Ready;
        _ownerTier--; _retirements++; return true;
    }

    public static bool ExactReplacement(ReadOnlySpan<PlanetarySurfacePatchId> parents,
        ReadOnlySpan<PlanetarySurfacePatchId> children)
    {
        if (parents.IsEmpty || children.Length != parents.Length * 4) return false;
        foreach (ref readonly var parent in parents)
        {
            for (var childIndex = 0; childIndex < 4; childIndex++)
            {
                var expected = parent.Child(childIndex); var occurrences = 0;
                foreach (ref readonly var child in children) if (child == expected) occurrences++;
                if (occurrences != 1) return false;
            }
        }
        return true;
    }

    private Tier Get(int tier) => (uint)tier < (uint)_tiers.Length ? _tiers[tier] :
        throw new ArgumentOutOfRangeException(nameof(tier));

    private static PlanetarySurfacePatchId[] Refine(ReadOnlySpan<PlanetarySurfacePatchId> parents)
    {
        var children = new PlanetarySurfacePatchId[parents.Length * 4]; var index = 0;
        foreach (ref readonly var parent in parents)
            for (var child = 0; child < 4; child++) children[index++] = parent.Child(child);
        Array.Sort(children); return children;
    }

    private sealed class Tier
    {
        private readonly PlanetaryAnchoredBillboardResource[] _resources;
        private readonly PlanetaryAnchoredBillboardFailure[] _failures;

        public Tier(in PlanetaryAnchoredBillboardTierId identity, PlanetarySurfacePatchId[] patches,
            PlanetaryAnchoredBillboardTopology topology)
        {
            Identity = identity; Patches = patches; Topology = topology;
            _resources = new PlanetaryAnchoredBillboardResource[patches.Length];
            _failures = new PlanetaryAnchoredBillboardFailure[patches.Length];
        }

        public PlanetaryAnchoredBillboardTierId Identity { get; }
        public PlanetarySurfacePatchId[] Patches { get; }
        public PlanetaryAnchoredBillboardTopology Topology { get; }
        public PlanetaryAnchoredBillboardTierState State { get; set; }
        public bool IsComplete => IncompleteCount == 0;
        public int IncompleteCount
        {
            get
            {
                var count = 0;
                for (var index = 0; index < _resources.Length; index++)
                    if (_resources[index] != PlanetaryAnchoredBillboardResource.Complete ||
                        _failures[index] != PlanetaryAnchoredBillboardFailure.None) count++;
                return count;
            }
        }

        public void Reset()
        {
            Array.Clear(_resources); Array.Clear(_failures);
        }

        public void MarkReady(int index, PlanetaryAnchoredBillboardResource resources)
        {
            if ((uint)index >= (uint)_resources.Length || _failures[index] != PlanetaryAnchoredBillboardFailure.None)
                throw new ArgumentOutOfRangeException(nameof(index));
            _resources[index] |= resources;
        }

        public void PrepareAll()
        {
            Array.Fill(_resources, PlanetaryAnchoredBillboardResource.Complete);
            Array.Clear(_failures);
        }

        public void Fail(int index, PlanetaryAnchoredBillboardFailure failure)
        {
            if ((uint)index >= (uint)_failures.Length) throw new ArgumentOutOfRangeException(nameof(index));
            _failures[index] = failure;
        }
    }
}
