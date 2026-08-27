using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Graphics;
using NovaCore.Interop;

/// <summary>Isolated 11B-7E proof driver. It is never constructed by Earth or Solar scenes.</summary>
internal sealed class PlanetaryAnchoredBillboardDiagnostic
{
    private readonly PlanetaryAnchoredBillboardHierarchy _hierarchy;
    private readonly NativePlanetaryPatch[] _patches;
    private int _frame;
    private int _nextPatch;
    private int _lastOwner = -1;
    private PlanetaryAnchoredBillboardTierState _lastPreparationState = (PlanetaryAnchoredBillboardTierState)255;

    private PlanetaryAnchoredBillboardDiagnostic(PlanetaryAnchoredBillboardHierarchy hierarchy,
        NativePlanetaryPatch[] patches)
    {
        _hierarchy = hierarchy; _patches = patches; RefreshPatches(); PrintTelemetry();
    }

    public NativePlanetaryPatch[] NativePatches => _patches;
    public int ActivePatchCount => _hierarchy.ActivePatches.Length;
    public Double3 AnchorDirection => _hierarchy.Footprint.Anchor.BodyFixedDirection;

    public static bool TryCreate(out PlanetaryAnchoredBillboardDiagnostic? diagnostic,
        out string error)
    {
        diagnostic = null;
        var latitude = FloridaLaunchSite.Latitude * Math.PI / 180d;
        var longitude = FloridaLaunchSite.Longitude * Math.PI / 180d;
        var direction = BodyFixedGeography.DirectionFromLatitudeLongitude(latitude, longitude);
        var terrain = PlanetaryTerrainDefinition.EarthProductionCubeV5;
        if (SurfaceAnchor.TryCreate(6, new(terrain.SourceId, terrain.Version), direction, 0d,
                out var anchor) != SurfaceAnchorCreationStatus.Success)
        {
            error = "The canonical Florida diagnostic SurfaceAnchor could not be created."; return false;
        }
        var footprint = PlanetaryAnchoredBillboardFootprint.AroundAnchor(anchor, 3);
        var hierarchy = new PlanetaryAnchoredBillboardHierarchy(footprint);
        diagnostic = new(hierarchy, new NativePlanetaryPatch[hierarchy.Patches(2).Length]);
        error = string.Empty; return true;
    }

    public void Advance()
    {
        _frame++;
        if (_frame == 90) Begin(1);
        if (_frame is >= 90 and < 210 && _frame % 24 == 0) PrepareNext(1);
        if (_frame == 210) { Complete(1); _hierarchy.TryPromote(); RefreshPatches(); }
        if (_frame == 300) Begin(2);
        if (_frame is >= 300 and < 460 && _frame % 10 == 0) PrepareNext(2);
        if (_frame == 460) { Complete(2); _hierarchy.TryPromote(); RefreshPatches(); }
        if (_frame == 600) { _hierarchy.RequestTier(0); _hierarchy.TryRetire(); RefreshPatches(); }
        if (_frame == 660) { _hierarchy.TryRetire(); RefreshPatches(); }
        PrintTelemetry();
    }

    public void UpdateCameraRelativeCenter(in Double3 cameraPosition)
    {
        var relative = -cameraPosition;
        for (var index = 0; index < ActivePatchCount; index++)
        {
            _patches[index].CenterX = (float)relative.X;
            _patches[index].CenterY = (float)relative.Y;
            _patches[index].CenterZ = (float)relative.Z;
        }
    }

    private void Begin(int tier)
    {
        var topology = _hierarchy.Topology(_hierarchy.ActiveOwnerTier);
        var edge = PlanetaryAnchoredMeshEdgeId.Create(topology.Vertices[0], topology.Vertices[1]);
        var pixels = tier == 1 ? 32d : 64d;
        _hierarchy.Request(PlanetaryAnchoredMeshSubdivisionDemand.FromPixels(edge, pixels, 16d, 4));
        _hierarchy.BeginPreparation(tier); _nextPatch = 0;
    }

    private void PrepareNext(int tier)
    {
        if (_nextPatch >= _hierarchy.Patches(tier).Length) return;
        _hierarchy.MarkPatchResourcesReady(tier, _nextPatch++,
            PlanetaryAnchoredBillboardResource.Complete);
    }

    private void Complete(int tier)
    {
        while (_nextPatch < _hierarchy.Patches(tier).Length) PrepareNext(tier);
    }

    private void RefreshPatches()
    {
        var active = _hierarchy.ActivePatches; var tier = _hierarchy.ActiveOwnerTier;
        var colors = tier switch { 0 => (1f, .62f, .08f), 1 => (.16f, 1f, .35f), _ => (.12f, .68f, 1f) };
        for (var index = 0; index < active.Length; index++)
        {
            var patch = active[index];
            _patches[index] = new NativePlanetaryPatch
            {
                Face = (uint)patch.Face, Level = (uint)patch.Level, X = (uint)patch.X, Y = (uint)patch.Y,
                Radius = 1f, ColorR = colors.Item1, ColorG = colors.Item2, ColorB = colors.Item3,
                ColorA = -1f
            };
        }
        for (var index = active.Length; index < _patches.Length; index++) _patches[index] = default;
    }

    private void PrintTelemetry()
    {
        var telemetry = _hierarchy.Telemetry;
        var nextTier = Math.Min(_hierarchy.TierCount - 1, _hierarchy.ActiveOwnerTier + 1);
        var preparationState = _hierarchy.State(nextTier);
        if (_lastOwner == telemetry.ActiveOwnerTier && _lastPreparationState == preparationState) return;
        _lastOwner = telemetry.ActiveOwnerTier; _lastPreparationState = preparationState;
        Console.WriteLine($"11B-7E ownership: demanded={telemetry.DemandedTier}; preparing={telemetry.PreparingTier}; " +
            $"ready={telemetry.ReadyTier}; owner={telemetry.ActiveOwnerTier}; fallback={telemetry.ParentFallbackCount}; " +
            $"incomplete={telemetry.IncompleteChildCount}; promotions={telemetry.PromotionCount}; " +
            $"retirements={telemetry.RetirementCount}; coverage={telemetry.GeographicCoverageComplete}; " +
            $"patches={_hierarchy.ActivePatches.Length}; state={preparationState}");
    }
}
