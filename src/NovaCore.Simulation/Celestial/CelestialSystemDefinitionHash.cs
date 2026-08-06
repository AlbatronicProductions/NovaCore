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
        hash = Mix(hash, (ulong)definition.BodyCount);
        for (var index = 0; index < definition.BodyCount; index++)
        {
            var entry = definition.GetBody(index); var identity = entry.Identity; var physical = entry.PhysicalProperties;
            hash = Mix(hash, identity.Id.Value); HashString(ref hash, identity.DisplayName); hash = Mix(hash, (ulong)identity.Classification); hash = Mix(hash, identity.ParentBody?.Value ?? 0UL); hash = Mix(hash, identity.RotationReference.Value); hash = Mix(hash, identity.AtmosphereReference.Value); hash = Mix(hash, identity.VisualReference.Value); var aliases = identity.Aliases; hash = Mix(hash, (ulong)(aliases?.Count ?? 0)); if (aliases is not null) for (var alias = 0; alias < aliases.Count; alias++) HashString(ref hash, aliases.Get(alias));
            hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(physical.GravitationalParameter)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(physical.MeanRadius)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(physical.EquatorialRadius)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(physical.PolarRadius)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(physical.Flattening)); hash = Mix(hash, physical.SiderealRotationReference.Value); hash = Mix(hash, physical.AtmosphereReference.Value); hash = Mix(hash, physical.VisualReference.Value);
        }
        hash = Mix(hash, (ulong)definition.SourceCount);
        for (var index = 0; index < definition.SourceCount; index++) { var source = definition.GetSource(index); hash = Mix(hash, source.Id.Value); hash = Mix(hash, (ulong)source.Model); HashMetadata(ref hash, source.Metadata); }
        hash = Mix(hash, (ulong)definition.FixedBodyCount);
        for (var index = 0; index < definition.FixedBodyCount; index++) { var value = definition.GetFixedBody(index); HashVector(ref hash, value.Position); HashVector(ref hash, value.Velocity); HashQuaternion(ref hash, value.Orientation); HashVector(ref hash, value.AngularVelocity); }
        hash = Mix(hash, (ulong)definition.CircularOrbitCount);
        for (var index = 0; index < definition.CircularOrbitCount; index++) { var value = definition.GetCircularOrbit(index); hash = Mix(hash, (ulong)value.EpochDomainTicks); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.Radius)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.InitialPhaseRadians)); HashQuaternion(ref hash, value.PlaneOrientation); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.CentralGravitationalParameter)); }
        hash = Mix(hash, (ulong)definition.AnalyticalKeplerCount);
        for (var index = 0; index < definition.AnalyticalKeplerCount; index++) { var value = definition.GetAnalyticalKepler(index); hash = Mix(hash, value.CentralBody.Value); hash = Mix(hash, (ulong)value.Epoch.Ticks); HashVector(ref hash, value.StateAtEpoch.Position); HashVector(ref hash, value.StateAtEpoch.Velocity); hash = Mix(hash, (ulong)value.Model); }
        hash = Mix(hash, (ulong)definition.SampledEphemerisCount);
        for (var index = 0; index < definition.SampledEphemerisCount; index++) { var value = definition.GetSampledEphemeris(index); hash = Mix(hash, value.Domain.Value); hash = Mix(hash, (ulong)value.FirstSampleIndex); hash = Mix(hash, (ulong)value.SampleCount); hash = Mix(hash, (ulong)value.InterpolationModel); hash = Mix(hash, (ulong)value.SupportedStartDomainTick); hash = Mix(hash, (ulong)value.SupportedEndDomainTick); }
        hash = Mix(hash, (ulong)definition.SampleCount);
        for (var index = 0; index < definition.SampleCount; index++) { var value = definition.GetSample(index); hash = Mix(hash, (ulong)value.DomainTick); HashVector(ref hash, value.Position); HashVector(ref hash, value.Velocity); }
        for (var index = 0; index < definition.Count; index++)
        {
            var node = definition.GetNodeInTraversalOrder(index);
            hash = Mix(hash, node.Id.Value); hash = Mix(hash, (ulong)node.TrajectoryModel); hash = Mix(hash, node.Ephemeris.SourceId.Value); hash = Mix(hash, (ulong)node.Ephemeris.PayloadIndex);
        }
        return hash;
    }

    private static ulong Mix(ulong hash, ulong value) { for (var index = 0; index < 8; index++) { hash ^= (byte)value; hash *= 1099511628211UL; value >>= 8; } return hash; }
    private static void HashMetadata(ref ulong hash, CelestialEphemerisMetadata value) { hash = Mix(hash, value.Source.Value); hash = Mix(hash, value.Version.Value); hash = Mix(hash, value.Domain.Value); hash = Mix(hash, (ulong)value.SupportedStartDomainTicks); hash = Mix(hash, (ulong)value.SupportedEndDomainTicks); hash = Mix(hash, value.CoordinateFrame.Value); hash = Mix(hash, value.ConstantsVersion.Value); hash = Mix(hash, value.ContentHash.High); hash = Mix(hash, value.ContentHash.Low); hash = Mix(hash, value.AuthoredModificationHash.High); hash = Mix(hash, value.AuthoredModificationHash.Low); }
    private static void HashVector(ref ulong hash, NovaCore.Core.Double3 value) { hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.X)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.Y)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.Z)); }
    private static void HashQuaternion(ref ulong hash, NovaCore.Core.DoubleQuaternion value) { hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.X)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.Y)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.Z)); hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.W)); }
    private static void HashString(ref ulong hash, string value) { hash = Mix(hash, (ulong)value.Length); for (var index = 0; index < value.Length; index++) hash = Mix(hash, value[index]); }
}
