using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Interop;

namespace NovaCore.Graphics;

/// <summary>
/// Deterministic transport from body-fixed FP64 authority to the isolated GPU
/// physical-height verifier used by the current parity suite.
/// </summary>
public static class PlanetaryGpuPhysicalHeightQuery
{
    public const uint TopologyVersion = 1;
    public const uint PhysicalSourcePolicy = 1;
    public const double MaximumLocalDeltaMetres = 100_000d;

    public static bool TryCreate(ulong bodyId, uint terrainVersion, uint anchoredTier,
        in Double3 bodyFixedAnchor, in Double3 localDelta,
        out NativePlanetaryHeightQuery query, out Double3 reconstructed)
    {
        query = default; reconstructed = default;
        if (bodyId == 0 || terrainVersion == 0 || anchoredTier > 3 ||
            !bodyFixedAnchor.IsFinite || bodyFixedAnchor.LengthSquared <= 0d || !localDelta.IsFinite ||
            Math.Abs(localDelta.X) > MaximumLocalDeltaMetres ||
            Math.Abs(localDelta.Y) > MaximumLocalDeltaMetres ||
            Math.Abs(localDelta.Z) > MaximumLocalDeltaMetres) return false;

        var encoded = EncodedPosition.Encode(bodyFixedAnchor);
        var deltaX = (float)localDelta.X; var deltaY = (float)localDelta.Y; var deltaZ = (float)localDelta.Z;
        reconstructed = encoded.Reconstruct() + new Double3(deltaX, deltaY, deltaZ);
        if (!reconstructed.IsFinite || reconstructed.LengthSquared <= 0d) return false;
        var direction = reconstructed.Normalized();
        var oracleU = BodyFixedGeography.LongitudeRadians(direction) / Math.Tau + .5d;
        oracleU -= Math.Floor(oracleU);
        var oracleV = Math.Acos(Math.Clamp(direction.Y, -1d, 1d)) / Math.PI;

        query = new NativePlanetaryHeightQuery
        {
            AnchorHighX = encoded.HighX, AnchorHighY = encoded.HighY, AnchorHighZ = encoded.HighZ,
            AnchorHighPadding = 0f,
            AnchorLowX = encoded.LowX, AnchorLowY = encoded.LowY, AnchorLowZ = encoded.LowZ,
            AnchorLowPadding = 0f,
            LocalDeltaX = deltaX, LocalDeltaY = deltaY, LocalDeltaZ = deltaZ, LocalDeltaPadding = 0f,
            OracleU = oracleU, OracleV = oracleV,
            BodyIdLow = (uint)bodyId, BodyIdHigh = (uint)(bodyId >> 32),
            TerrainVersion = terrainVersion, AnchoredTier = anchoredTier,
            TopologyVersion = TopologyVersion, SourcePolicy = PhysicalSourcePolicy,
            Reserved0 = 0, Reserved1 = 0,
        };
        return true;
    }
}
