using NovaCore.Graphics;

internal static class PlanetarySphericalBillboardNaturalTerrainTests
{
    public static void Run()
    {
        var root = PlanetarySphericalBillboardGpuProof.FindRepositoryRoot(AppContext.BaseDirectory);
        var report = PlanetarySphericalBillboardNaturalTerrainProof.Run(root);
        Require(report.Levels.Count == 3, "all three banked P2S2 levels consume canonical terrain");
        Require(report.ReusedCanonicalSamples > 0 && report.UniqueCanonicalSamples > 0,
            "shared level directions reuse topology-independent canonical sample identity");
        Require(report.MaximumSharedLevelHeightDeltaMetres == 0d &&
            report.MaximumSharedLevelNormalDeltaRadians <= 5e-8,
            "shared billboard-level directions have identical height and physical normal");
        var cullingEnvelope = EarthElevationDataset.MaximumElevationMetres +
            PlanetaryLocalTerrainPackContract.DefaultResidualMaximumMetres +
            PlanetaryNaturalTerrainFamilies.ComposedBounds().TotalHeight;
        Require(report.MaximumPhysicalHeightMetres <= cullingEnvelope,
            "analytic global, NCCUBE2, and natural-terrain displacement envelope contains every prepared vertex");
        Require(typeof(PlanetaryCanonicalPhysicalSampleIdentity).GetProperties().All(property =>
            !property.Name.Contains("Patch", StringComparison.OrdinalIgnoreCase) &&
            !property.Name.Contains("Topology", StringComparison.OrdinalIgnoreCase) &&
            !property.Name.Contains("Camera", StringComparison.OrdinalIgnoreCase)),
            "canonical physical preparation identity contains no patch, topology, or camera authority");
        foreach (var level in report.Levels)
        {
            var frame = level.Frame;
            Require(level.MaximumCpuHeightErrorMetres <= 1e-3,
                $"{level.Level} CPU/shared-GPU H(direction) parity");
            Require(level.MaximumCpuNormalErrorRadians <= 5e-4,
                $"{level.Level} canonical GPU/CPU physical-normal parity");
            Require(level.Publication.Readiness == 3 && frame.Readiness == 63,
                $"{level.Level} readiness progresses topology -> physical -> vertex -> normal -> cull -> draw");
            Require(frame.PhysicalGeneration == PlanetarySphericalBillboardNaturalTerrainProof.PhysicalGeneration &&
                frame.TerrainDataGeneration == PlanetarySphericalBillboardNaturalTerrainProof.TerrainDataGeneration,
                $"{level.Level} consumes one matching physical/data generation");
            Require(frame.PreparedPhysicalSamples == frame.BaseVertexCount &&
                frame.PhysicalPreparationDispatchCount > 0 && frame.PhysicalReuseCount >= frame.BaseVertexCount,
                $"{level.Level} immutable physical publication is complete and reused");
            Require(frame.VisibleTriangles > 0 && frame.IndirectDrawCount == 1 &&
                frame.IndirectIndexCount == frame.VisibleTriangles * 3 && frame.InvalidCommands == 0,
                $"{level.Level} retains actual-triangle culling, compaction, and one indirect draw");
            Require(frame.VisibleTriangles + frame.BackfaceRejected + frame.FrustumRejected +
                frame.InvalidRejected == frame.BaseTriangleCount,
                $"{level.Level} every displaced triangle receives a conservative visibility classification");
            Require(frame.ValidationErrors == 0 && frame.NonFinitePhysicalOutputs == 0,
                $"{level.Level} canonical physical output is finite and validation-clean");
            Require(frame.StaleGenerationRejections > 0,
                $"{level.Level} rejects a mismatched physical/data generation before drawing");
            Require(level.CameraUpdate.TopologyUploadCount == frame.TopologyUploadCount &&
                level.CameraUpdate.PhysicalPreparationDispatchCount == frame.PhysicalPreparationDispatchCount,
                $"{level.Level} camera movement regenerates neither topology nor physical truth");
            Console.WriteLine($"P2S4 {level.Level}: vertices={frame.BaseVertexCount}; triangles={frame.BaseTriangleCount}; " +
                $"physicalSamples={frame.PreparedPhysicalSamples}; dispatched={level.PreparedCanonicalSamples}; sharedReuse={level.ReusedCanonicalSamples}; " +
                $"heightMax={level.MaximumCpuHeightErrorMetres:E17}m; normalMax={level.MaximumCpuNormalErrorRadians:E17}rad; " +
                $"visible={frame.VisibleTriangles}; physicalPrepare={level.PhysicalPreparation.GpuMilliseconds:F6}ms; " +
                $"billboardFinalize={frame.PreparationMilliseconds:F6}ms; normalPublish={frame.NormalMilliseconds:F6}ms; " +
                $"cull={frame.CullingMilliseconds:F6}ms; compact={frame.CompactionMilliseconds:F6}ms; draw={frame.DrawMilliseconds:F6}ms; validation=0");
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
