namespace NovaCore.Simulation.Celestial;

/// <summary>Stable raw-value hash for authored celestial-system validation and fixture tests.</summary>
internal static class CelestialSystemDefinitionHash
{
    internal static ulong Compute(CelestialSystemDefinition definition)
    {
        ulong hash = 14695981039346656037UL;
        hash = Mix(hash, definition.Id.Value); hash = Mix(hash, definition.RootBody.Value); hash = Mix(hash, (ulong)definition.Count);
        var mapping = definition.TimeMapping; var epoch = mapping.DomainAnchor; var metadata = definition.EphemerisMetadata;
        hash = Mix(hash, (ulong)mapping.SimulationAnchor.Ticks); hash = Mix(hash, epoch.Domain.Value); hash = Mix(hash, (ulong)epoch.DomainTicks); hash = Mix(hash, (ulong)epoch.DomainTicksPerSecond); hash = Mix(hash, (ulong)mapping.ScaleNumerator); hash = Mix(hash, (ulong)mapping.ScaleDenominator);
        hash = Mix(hash, metadata.Source.Value); hash = Mix(hash, metadata.Version.Value); hash = Mix(hash, metadata.Domain.Value); hash = Mix(hash, (ulong)metadata.SupportedStartDomainTicks); hash = Mix(hash, (ulong)metadata.SupportedEndDomainTicks); hash = Mix(hash, metadata.CoordinateFrame.Value); hash = Mix(hash, metadata.ConstantsVersion.Value); hash = Mix(hash, metadata.ContentHash.High); hash = Mix(hash, metadata.ContentHash.Low); hash = Mix(hash, metadata.AuthoredModificationHash.High); hash = Mix(hash, metadata.AuthoredModificationHash.Low);
        for (var index = 0; index < definition.Count; index++)
        {
            var node = definition.GetNodeInTraversalOrder(index); var body = node.Body;
            hash = Mix(hash, body.Id.Value); hash = Mix(hash, body.PrimaryBody?.Value ?? 0UL); hash = Mix(hash, (ulong)body.InertialFrame.Value);
            hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(body.GravitationalParameter)); hash = Mix(hash, (ulong)node.TrajectoryModel);
        }
        return hash;
    }

    private static ulong Mix(ulong hash, ulong value) { for (var index = 0; index < 8; index++) { hash ^= (byte)value; hash *= 1099511628211UL; value >>= 8; } return hash; }
}
