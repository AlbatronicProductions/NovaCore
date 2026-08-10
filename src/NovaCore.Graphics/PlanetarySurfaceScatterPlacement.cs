using NovaCore.Core;

namespace NovaCore.Graphics;

/// <summary>Deterministic near-surface scatter placement contract.</summary>
public readonly record struct PlanetarySurfaceScatterConfiguration(
    double ScatterRadiusMetres,
    double CellSizeMetres,
    int MaximumCandidateCells,
    int MaximumInstances,
    float MinimumScaleMetres,
    float MaximumScaleMetres,
    uint Seed)
{
    public bool IsValid =>
        double.IsFinite(ScatterRadiusMetres) && ScatterRadiusMetres > 0d &&
        double.IsFinite(CellSizeMetres) && CellSizeMetres > 0d &&
        MaximumCandidateCells > 0 && MaximumInstances > 0 &&
        float.IsFinite(MinimumScaleMetres) && MinimumScaleMetres > 0d &&
        float.IsFinite(MaximumScaleMetres) && MaximumScaleMetres >= MinimumScaleMetres;
}

/// <summary>
/// Pre-authoring instance for near-surface scatter (position/orientation/scale only).
/// Identity remains stable in body-fixed ENU across camera motion and body rotation.
/// </summary>
public readonly record struct PlanetarySurfaceScatterInstance(
    ulong IdentityHash,
    int CellX,
    int CellY,
    Double3 LocalEnuPosition,
    Double3 BodyLocalPosition,
    double YawRadians,
    float ScaleMetres)
{
    public bool IsFinite =>
        double.IsFinite(YawRadians) &&
        LocalEnuPosition.IsFinite &&
        BodyLocalPosition.IsFinite &&
        float.IsFinite(ScaleMetres);
}

/// <summary>
/// Deterministic bounded scatter-placement authority for future clutter/impostor streams.
/// </summary>
public static class PlanetarySurfaceScatterPlacement
{
    public static PlanetarySurfaceScatterInstance[] Generate(
        in PlanetRenderProxy body,
        in SurfaceAnchorFocus anchor,
        in PlanetaryTerrainDefinition terrain,
        in PlanetarySurfaceScatterConfiguration configuration,
        UniversePosition? cameraRoot = null,
        Double3? cameraForwardRoot = null,
        bool enableCameraRelevanceCulling = false)
    {
        if (!body.IsValid || !anchor.IsValid || body.BodyId != anchor.BodyId || !terrain.IsValid || !configuration.IsValid)
            throw new ArgumentOutOfRangeException();

        if (enableCameraRelevanceCulling)
        {
            if (cameraRoot is null || cameraForwardRoot is null)
                throw new ArgumentOutOfRangeException(nameof(enableCameraRelevanceCulling),
                    "camera root and forward are required for camera relevance culling");
            if (cameraRoot.Value.Frame != body.Position.Frame || !cameraRoot.Value.Value.IsFinite ||
                !cameraForwardRoot.Value.IsFinite || cameraForwardRoot.Value.LengthSquared <= 0d)
                throw new ArgumentOutOfRangeException("camera root/forward must be finite and in the same reference frame");
        }

        var basis = anchor.LocalTangentBasis;
        var cellsRadius = (int)Math.Ceiling(configuration.ScatterRadiusMetres / configuration.CellSizeMetres);
        if (cellsRadius <= 0) cellsRadius = 1;

        var cameraCell = default(Double3);
        var cameraForwardLocal = default(Double3);
        if (enableCameraRelevanceCulling)
        {
            var cameraBody = body.BodyFixedToRoot.Conjugate().Normalized().Rotate(cameraRoot!.Value.Value - body.Position.Value);
            cameraCell = ToLocalVector(basis, cameraBody - anchor.BodyLocalPosition);
            var forwardBody = body.BodyFixedToRoot.Conjugate().Normalized().Rotate(cameraForwardRoot!.Value);
            cameraForwardLocal = ToLocalVector(basis, forwardBody);
            if (!cameraCell.IsFinite || !cameraForwardLocal.IsFinite || cameraForwardLocal.LengthSquared <= 0d)
                throw new ArgumentOutOfRangeException("camera basis conversion produced non-finite values");
        }

        var scatterRadiusSquared = configuration.ScatterRadiusMetres * configuration.ScatterRadiusMetres;
        var maxCellsByBudget = (int)Math.Max(0d, Math.Floor((Math.Sqrt(configuration.MaximumCandidateCells) - 1d) / 2d));
        var cellHalfWidth = Math.Min(cellsRadius, Math.Max(1, maxCellsByBudget));

        var result = new PlanetarySurfaceScatterInstance[Math.Min(configuration.MaximumInstances, configuration.MaximumCandidateCells)];
        var count = 0;
        var cellsVisited = 0;

        for (var offsetY = -cellHalfWidth; count < configuration.MaximumInstances && cellsVisited < configuration.MaximumCandidateCells; offsetY++)
        {
            for (var offsetX = -cellHalfWidth; offsetX <= cellHalfWidth && count < configuration.MaximumInstances && cellsVisited < configuration.MaximumCandidateCells; offsetX++)
            {
                if (offsetX * offsetX + offsetY * offsetY > cellsRadius * cellsRadius) continue;
                cellsVisited++;

                var state = BuildCellSeed(anchor.BodyId, offsetX, offsetY, configuration.Seed);

                var localX = (offsetX * configuration.CellSizeMetres) + Next01(ref state) * configuration.CellSizeMetres;
                var localY = (offsetY * configuration.CellSizeMetres) + Next01(ref state) * configuration.CellSizeMetres;
                if (localX * localX + localY * localY > scatterRadiusSquared) continue;

                if (enableCameraRelevanceCulling)
                {
                    var localCandidateRelative = new Double3(localX, localY, 0d);
                    if (Dot(localCandidateRelative - cameraCell, cameraForwardLocal) <= 0d) continue;
                }

                var localCandidateEnu = new Double3(localX, localY, 0d);
                var bodyLocal = basis.ToBodyFixed(localCandidateEnu, anchor.BodyLocalPosition);
                var bodyDirection = bodyLocal.Normalized();
                if (!bodyDirection.IsFinite) continue;

                var radius = PlanetaryTerrainQuery.SurfaceRadius(body.RadiusMetres, bodyDirection, terrain);
                var onSurfaceBodyLocal = bodyDirection * radius;
                var localOnSurface = ToLocalVector(basis, onSurfaceBodyLocal - anchor.BodyLocalPosition);

                if (localOnSurface.X * localOnSurface.X + localOnSurface.Y * localOnSurface.Y > scatterRadiusSquared + 1e-9d)
                    continue;

                var yaw = 2d * Math.PI * Next01(ref state);
                var scale = (float)(configuration.MinimumScaleMetres +
                    (configuration.MaximumScaleMetres - configuration.MinimumScaleMetres) * Next01(ref state));

                result[count++] = new PlanetarySurfaceScatterInstance(
                    BuildInstanceIdentity(anchor.BodyId, configuration.Seed, offsetX, offsetY),
                    offsetX,
                    offsetY,
                    localOnSurface,
                    onSurfaceBodyLocal,
                    yaw,
                    scale);
            }
        }

        if (count == result.Length) return result;
        Array.Resize(ref result, count);
        return result;
    }

    private static ulong BuildCellSeed(ulong bodyId, int cellX, int cellY, uint seed)
    {
        var hash = SplitMix64((ulong)seed + 0x243F6A8885A308D3UL);
        hash ^= SplitMix64(bodyId ^ ((ulong)(uint)cellX << 32) ^ (ulong)(uint)cellY);
        return hash;
    }

    private static ulong BuildInstanceIdentity(ulong bodyId, uint seed, int cellX, int cellY)
    {
        var hash = SplitMix64(bodyId ^ (ulong)seed);
        hash ^= SplitMix64((uint)cellX * 0x9E3779B97F4A7C15UL + (uint)cellY);
        return SplitMix64(hash);
    }

    private static ulong SplitMix64(ulong value)
    {
        value += 0x9E3779B97F4A7C15UL;
        var z = value;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    private static double Next01(ref ulong state)
    {
        state = SplitMix64(state);
        return (state >> 11) * (1d / 9007199254740992d);
    }

    private static Double3 ToLocalVector(in LocalSurfaceTangentBasis basis, in Double3 bodyVector)
        => new(Double3.Dot(bodyVector, basis.East), Double3.Dot(bodyVector, basis.North), Double3.Dot(bodyVector, basis.Up));

    private static double Dot(in Double3 left, in Double3 right)
        => left.X * right.X + left.Y * right.Y + left.Z * right.Z;
}
